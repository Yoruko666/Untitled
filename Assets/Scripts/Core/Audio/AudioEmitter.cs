using UnityEngine;

namespace Untitled
{
    // Optional Inspector/UnityEvent entry point. Does not create its own AudioSource.
    public sealed class AudioEmitter : MonoBehaviour
    {
        [SerializeField] private SoundDefinition sound;
        [SerializeField] private bool playOnEnable;
        [SerializeField] private bool followTransform = true;
        [SerializeField] private bool stopOnDisable = true;
        private int handle;

        private void OnEnable() { if (playOnEnable) Play(); }

        public void Play()
        {
            var manager = AudioManager.EnsureInitialized();
            if (!AudioManager.IsInitialized) return;
            manager.Stop(handle);
            handle = manager.Play(sound, transform.position, followTransform ? transform : null);
        }

        public void Stop()
        {
            if (AudioManager.TryGet(out var manager)) manager.Stop(handle);
            handle = 0;
        }

        private void OnDisable() { if (stopOnDisable) Stop(); }
    }
}
