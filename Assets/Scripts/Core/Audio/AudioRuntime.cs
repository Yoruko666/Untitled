using UnityEngine;

namespace Untitled
{
    // Owned by AudioManager. Never add this component to scenes or prefabs.
    [AddComponentMenu("")]
    public sealed class AudioRuntime : MonoBehaviour
    {
        private sealed class Voice
        {
            public AudioSource Source;
            public Transform Follow;
            public bool HadFollow;
            public int Handle;
            public ulong Order;
        }

        private sealed class LoopChannel
        {
            private readonly AudioSource[] sources;
            private readonly float[] from = new float[2];
            private readonly float[] to = new float[2];
            private int current = -1;
            private float elapsed;
            private float duration;
            private SoundDefinition definition;

            public LoopChannel(AudioSource first, AudioSource second)
            {
                sources = new[] { first, second };
            }

            public void Play(SoundDefinition sound, AudioCategory category, float seconds)
            {
                if (sound != null && sound.Category != category)
                {
                    Debug.LogWarning($"{sound.name} must use the {category} category.");
                    return;
                }
                if (sound == definition && current >= 0 && sources[current].isPlaying) return;
                AudioClip clip = sound != null ? sound.PickClip() : null;
                if (sound != null && clip == null)
                {
                    Debug.LogWarning("No audio clips assigned: " + sound.name);
                    return;
                }

                definition = sound;
                duration = Mathf.Max(0f, seconds);
                elapsed = 0f;
                for (int i = 0; i < 2; i++)
                {
                    from[i] = sources[i].volume;
                    to[i] = 0f;
                }
                if (clip != null)
                {
                    // During a rapid transition, retain the louder side and reuse the quieter one.
                    current = sources[0].volume <= sources[1].volume ? 0 : 1;
                    var source = sources[current];
                    source.Stop();
                    source.clip = clip;
                    source.loop = true;
                    source.pitch = sound.RandomPitch;
                    source.volume = from[current] = 0f;
                    source.Play();
                    to[current] = sound.Volume;
                }
                else current = -1;
                Tick(0f);
            }

            public void Tick(float deltaTime)
            {
                elapsed += deltaTime;
                float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                for (int i = 0; i < 2; i++)
                {
                    sources[i].volume = Mathf.Lerp(from[i], to[i], t);
                    if (t >= 1f && to[i] == 0f && sources[i].clip != null)
                    {
                        sources[i].Stop();
                        sources[i].clip = null;
                    }
                }
            }
        }

        private AudioManager owner;
        private AudioSettings settings;
        private LoopChannel bgm;
        private LoopChannel ambience;
        private Voice[] effects;
        private Voice[] ui;
        private int nextHandle;
        private ulong nextOrder;

        internal void Initialize(AudioManager manager, AudioSettings config)
        {
            owner = manager;
            settings = config;
            bgm = new LoopChannel(CreateSource("BGM A", AudioCategory.BGM), CreateSource("BGM B", AudioCategory.BGM));
            ambience = new LoopChannel(CreateSource("Ambience A", AudioCategory.Ambience), CreateSource("Ambience B", AudioCategory.Ambience));
            effects = CreateVoices(settings.SfxVoices, "SFX", AudioCategory.SFX);
            ui = CreateVoices(settings.UiVoices, "UI", AudioCategory.UI);
        }

        private AudioSource CreateSource(string sourceName, AudioCategory category)
        {
            var child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = 0f;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.outputAudioMixerGroup = settings.Group(category);
            source.ignoreListenerPause = category == AudioCategory.UI;
            return source;
        }

        private Voice[] CreateVoices(int count, string prefix, AudioCategory category)
        {
            var voices = new Voice[count];
            for (int i = 0; i < count; i++)
                voices[i] = new Voice { Source = CreateSource(prefix + " " + i, category) };
            return voices;
        }

        private void Start() => owner?.StartAudio();

        private void Update()
        {
            bgm?.Tick(Time.unscaledDeltaTime);
            ambience?.Tick(Time.unscaledDeltaTime);
            UpdateVoices(effects);
            UpdateVoices(ui);
        }

        private static void UpdateVoices(Voice[] voices)
        {
            if (voices == null) return;
            foreach (var voice in voices)
            {
                if (voice.Handle == 0) continue;
                if (voice.HadFollow && voice.Follow == null)
                {
                    Release(voice);
                    continue;
                }
                if (voice.Follow != null) voice.Source.transform.position = voice.Follow.position;
                if (!voice.Source.isPlaying && (!AudioListener.pause || voice.Source.ignoreListenerPause))
                    Release(voice);
            }
        }

        internal void PlayBgm(SoundDefinition sound, float fadeSeconds) => bgm.Play(sound, AudioCategory.BGM, fadeSeconds);
        internal void PlayAmbience(SoundDefinition sound, float fadeSeconds) => ambience.Play(sound, AudioCategory.Ambience, fadeSeconds);

        internal int Play(SoundDefinition sound, Vector3 position, Transform follow, float? spatialOverride)
        {
            if (sound == null) return 0;
            if (sound.Category == AudioCategory.BGM)
            {
                Debug.LogWarning("Use AudioManager.PlayBgm for background music.");
                return 0;
            }
            var clip = sound.PickClip();
            if (clip == null)
            {
                Debug.LogWarning("No audio clips assigned: " + sound.name);
                return 0;
            }

            var voices = sound.Category == AudioCategory.UI ? ui : effects;
            Voice selected = null;
            foreach (var voice in voices)
            {
                bool paused = AudioListener.pause && !voice.Source.ignoreListenerPause;
                if (voice.Handle == 0 || (!paused && !voice.Source.isPlaying))
                {
                    selected = voice;
                    break;
                }
                if (selected == null || voice.Order < selected.Order) selected = voice;
            }

            Release(selected);
            var source = selected.Source;
            source.outputAudioMixerGroup = settings.Group(sound.Category);
            source.clip = clip;
            source.loop = false;
            source.volume = sound.Volume;
            source.pitch = sound.RandomPitch;
            source.spatialBlend = sound.Category == AudioCategory.UI ? 0f : spatialOverride ?? sound.SpatialBlend;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = sound.MinDistance;
            source.maxDistance = sound.MaxDistance;
            source.transform.position = follow != null ? follow.position : position;
            selected.Follow = follow;
            selected.HadFollow = follow != null;
            unchecked { nextHandle++; }
            if (nextHandle == 0) nextHandle++;
            selected.Handle = nextHandle;
            selected.Order = ++nextOrder;
            source.Play();
            return selected.Handle;
        }

        internal void Stop(int handle)
        {
            if (handle == 0) return;
            foreach (var voice in effects) if (voice.Handle == handle) Release(voice);
            foreach (var voice in ui) if (voice.Handle == handle) Release(voice);
        }

        private static void Release(Voice voice)
        {
            voice.Source.Stop();
            voice.Source.clip = null;
            voice.Follow = null;
            voice.HadFollow = false;
            voice.Handle = 0;
        }

        internal void StopAll()
        {
            foreach (var source in GetComponentsInChildren<AudioSource>()) source.Stop();
        }

        private void OnApplicationPause(bool paused) { if (paused) owner?.SaveVolumes(); }
        private void OnApplicationQuit() => owner?.SaveVolumes();
        private void OnDestroy() => owner?.OnHostDestroyed(this);
    }
}
