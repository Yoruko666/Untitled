using UnityEngine;

namespace Untitled
{
    [CreateAssetMenu(menuName = "Untitled/Audio/Scene Profile", fileName = "scene_audio")]
    public sealed class SceneAudioProfile : ScriptableObject
    {
        [SerializeField] private SoundDefinition bgm;
        [SerializeField] private SoundDefinition ambience;
        [SerializeField, Min(0f)] private float fadeSeconds = 1.5f;

        public SoundDefinition Bgm => bgm;
        public SoundDefinition Ambience => ambience;
        public float FadeSeconds => Mathf.Max(0f, fadeSeconds);
    }
}
