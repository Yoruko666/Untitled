using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Untitled
{
    [CreateAssetMenu(menuName = "Untitled/Audio/Settings", fileName = "audio_settings")]
    public sealed class AudioSettings : ScriptableObject
    {
        public const string ResourcePath = "Audio/audio_settings";

        [Serializable]
        private struct SceneEntry
        {
            public string ScenePath;
            public SceneAudioProfile Profile;
        }

        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioMixerGroup bgmGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup uiGroup;
        [SerializeField] private AudioMixerGroup ambienceGroup;
        [SerializeField, Range(1, 64)] private int sfxVoices = 16;
        [SerializeField, Range(1, 16)] private int uiVoices = 4;
        [SerializeField] private SceneEntry[] scenes;

        public AudioMixer Mixer => mixer;
        public int SfxVoices => Mathf.Clamp(sfxVoices, 1, 64);
        public int UiVoices => Mathf.Clamp(uiVoices, 1, 16);

        public AudioMixerGroup Group(AudioCategory category)
        {
            switch (category)
            {
                case AudioCategory.BGM: return bgmGroup;
                case AudioCategory.UI: return uiGroup;
                case AudioCategory.Ambience: return ambienceGroup;
                default: return sfxGroup;
            }
        }

        public SceneAudioProfile ProfileFor(string scenePath)
        {
            if (scenes != null)
                foreach (var entry in scenes)
                    if (entry.ScenePath == scenePath) return entry.Profile;
            return null;
        }
    }
}
