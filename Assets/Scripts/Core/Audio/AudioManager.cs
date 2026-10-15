using UnityEngine;
using UnityEngine.SceneManagement;

namespace Untitled
{
    public sealed class AudioManager : ManagerBase<AudioManager>
    {
        private const string VolumeKey = "Untitled.Audio.";
        private readonly float[] volumes = new float[5];
        private static readonly string[] parameters =
            { "MasterVolume", "BGMVolume", "SFXVolume", "UIVolume", "AmbienceVolume" };
        private AudioRuntime runtime;
        private AudioSettings settings;
        private SceneAudioProfile currentProfile;
        private bool hasProfile;
        private bool mixerReady;

        public static AudioManager EnsureInitialized()
        {
            if (!TryGet(out var manager))
            {
                manager = new AudioManager();
                manager.Initialize();
            }
            return manager;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAudio()
        {
            if (TryGet(out var manager)) manager.Dispose();
            ResetInstance();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeBeforeScene() => EnsureInitialized();

        public override void Initialize()
        {
            if (IsInitialized) return;
            settings = Resources.Load<AudioSettings>(AudioSettings.ResourcePath);
            if (settings == null)
            {
                Debug.LogError("Audio settings missing: Resources/" + AudioSettings.ResourcePath);
                return;
            }

            // One startup scan also cleans up a surviving host with scene/domain reload disabled.
            foreach (var stale in Resources.FindObjectsOfTypeAll<AudioRuntime>())
                if (stale.gameObject.scene.IsValid())
                {
                    stale.enabled = false;
                    Object.Destroy(stale.gameObject);
                }

            base.Initialize();
            for (int i = 0; i < volumes.Length; i++)
                volumes[i] = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey + parameters[i], 1f));
            runtime = new GameObject("AudioManager").AddComponent<AudioRuntime>();
            Object.DontDestroyOnLoad(runtime.gameObject);
            runtime.Initialize(this, settings);
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        internal void StartAudio()
        {
            mixerReady = true;
            ApplyVolumes();
            ApplyScene(SceneManager.GetActiveScene());
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single || scene == SceneManager.GetActiveScene()) ApplyScene(scene);
        }

        private void OnActiveSceneChanged(Scene previous, Scene next) => ApplyScene(next);
        private void ApplyScene(Scene scene) => PlayProfile(settings.ProfileFor(scene.path));

        public void PlayProfile(SceneAudioProfile profile)
        {
            if (runtime == null || (hasProfile && currentProfile == profile)) return;
            hasProfile = true;
            currentProfile = profile;
            float fade = profile != null ? profile.FadeSeconds : 1.5f;
            runtime.PlayBgm(profile != null ? profile.Bgm : null, fade);
            runtime.PlayAmbience(profile != null ? profile.Ambience : null, fade);
        }

        public void PlayBgm(SoundDefinition sound, float fadeSeconds = 1.5f)
        {
            if (runtime == null) return;
            hasProfile = false;
            runtime.PlayBgm(sound, Mathf.Max(0f, fadeSeconds));
        }

        public void StopBgm(float fadeSeconds = 1.5f) => PlayBgm(null, fadeSeconds);

        // Returns an opaque handle; 0 means the sound could not be played.
        public int Play(SoundDefinition sound, Vector3 position = default, Transform follow = null)
        {
            return runtime != null ? runtime.Play(sound, position, follow, null) : 0;
        }

        public int Play2D(SoundDefinition sound) => runtime != null ? runtime.Play(sound, default, null, 0f) : 0;
        public int Play3D(SoundDefinition sound, Vector3 position, Transform follow = null)
            => runtime != null ? runtime.Play(sound, position, follow, 1f) : 0;
        public void Stop(int handle) { if (runtime != null) runtime.Stop(handle); }

        public float MasterVolume => volumes[0];
        public float GetVolume(AudioCategory category) => volumes[(int)category + 1];
        public void SetMasterVolume(float value) => SetVolumeAt(0, value);
        public void SetVolume(AudioCategory category, float value) => SetVolumeAt((int)category + 1, value);

        private void SetVolumeAt(int index, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return;
            volumes[index] = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(VolumeKey + parameters[index], volumes[index]);
            if (mixerReady) ApplyVolume(index);
        }

        private void ApplyVolumes()
        {
            for (int i = 0; i < volumes.Length; i++) ApplyVolume(i);
        }

        private void ApplyVolume(int index)
        {
            float decibels = volumes[index] <= 0.0001f ? -80f : 20f * Mathf.Log10(volumes[index]);
            if (settings.Mixer == null || !settings.Mixer.SetFloat(parameters[index], decibels))
                Debug.LogWarning("Audio mixer parameter missing: " + parameters[index]);
        }

        // Call after a settings screen is confirmed. Changes are already applied in memory.
        public void SaveVolumes() => PlayerPrefs.Save();

        internal void OnHostDestroyed(AudioRuntime host)
        {
            if (runtime != host) return;
            runtime = null;
            Dispose();
        }

        public void Dispose()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            if (runtime != null)
            {
                runtime.enabled = false;
                runtime.StopAll();
                Object.Destroy(runtime.gameObject);
                runtime = null;
            }
            mixerReady = false;
            if (TryGet(out var manager) && manager == this) ResetInstance();
        }
    }
}
