using UnityEngine;

namespace Untitled
{
    [CreateAssetMenu(menuName = "Untitled/Audio/Sound", fileName = "sound")]
    public sealed class SoundDefinition : ScriptableObject
    {
        [SerializeField] private AudioCategory category = AudioCategory.SFX;
        [SerializeField] private AudioClip[] clips;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private Vector2 pitchRange = Vector2.one;
        [SerializeField, Range(0f, 1f)] private float spatialBlend;
        [SerializeField, Min(0.01f)] private float minDistance = 1f;
        [SerializeField, Min(0.01f)] private float maxDistance = 20f;

        public AudioCategory Category => category;
        public float Volume => Mathf.Clamp01(volume);
        public float SpatialBlend => category == AudioCategory.UI ? 0f : spatialBlend;
        public float MinDistance => Mathf.Max(0.01f, minDistance);
        public float MaxDistance => Mathf.Max(MinDistance, maxDistance);
        public float RandomPitch => Random.Range(Mathf.Clamp(Mathf.Min(pitchRange.x, pitchRange.y), 0.1f, 3f),
            Mathf.Clamp(Mathf.Max(pitchRange.x, pitchRange.y), 0.1f, 3f));

        public AudioClip PickClip()
        {
            if (clips == null || clips.Length == 0) return null;
            // Reservoir selection ignores missing entries without allocating a filtered array.
            AudioClip selected = null;
            int count = 0;
            foreach (var clip in clips)
                if (clip != null && Random.Range(0, ++count) == 0) selected = clip;
            return selected;
        }
    }
}
