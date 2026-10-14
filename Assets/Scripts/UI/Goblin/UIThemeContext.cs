using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Untitled.UI
{
    // A local Canvas scope, with no global mutable theme or game state.
    public sealed class UIThemeContext : MonoBehaviour
    {
        [SerializeField] private UIThemeDefinition dayTheme;
        [SerializeField] private UIThemeDefinition nightTheme;
        [SerializeField] private bool useNight;
        [SerializeField] private bool followGameplayScenes;
        public event Action<UIThemeDefinition> Changed;
        public UIThemeDefinition Current => useNight ? nightTheme : dayTheme;
        public bool IsNight => useNight;

        public void Configure(UIThemeDefinition day, UIThemeDefinition night, bool followScenes = false)
        {
            dayTheme = day;
            nightTheme = night;
            followGameplayScenes = followScenes;
            Apply(useNight);
            if (followGameplayScenes) ApplyScene(gameObject.scene.name);
        }

        public void ApplyDay() => Apply(false);
        public void ApplyNight() => Apply(true);
        public void Toggle() => Apply(!useNight);
        public void Apply(bool night)
        {
            if ((night ? nightTheme : dayTheme) == null) return;
            useNight = night;
            Changed?.Invoke(Current);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            if (followGameplayScenes) ApplyScene(gameObject.scene.name);
        }
        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (followGameplayScenes) ApplyScene(scene.name);
        }
        private void ApplyScene(string sceneName)
        {
            if (sceneName == "Bussiness") ApplyDay();
            else if (sceneName == "Adventure" || sceneName == "Cave") ApplyNight();
        }
    }
}
