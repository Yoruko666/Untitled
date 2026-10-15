using UnityEngine;

namespace Untitled
{
    // Isolated audio audition scene; never attached to gameplay scenes.
    public sealed class AudioSystemDemo : MonoBehaviour
    {
        [SerializeField] private SceneAudioProfile milkTea;
        [SerializeField] private SceneAudioProfile cave;
        [SerializeField] private SoundDefinition uiClick;
        [SerializeField] private SoundDefinition step;
        private static readonly AudioCategory[] categories =
            { AudioCategory.BGM, AudioCategory.SFX, AudioCategory.UI, AudioCategory.Ambience };
        private float sourceX = 2f;

        private void OnGUI()
        {
            if (!AudioManager.TryGet(out var audio)) return;
            GUILayout.BeginArea(new Rect(20, 20, 460, 600), GUI.skin.box);
            GUILayout.Label("Untitled Audio Demo - synthesized placeholders");
            GUILayout.Label("Default: 24 AudioSources (4 loops + 16 SFX + 4 UI)");
            if (GUILayout.Button("Milk tea: BGM + ambience")) audio.PlayProfile(milkTea);
            if (GUILayout.Button("Cave: crossfade BGM + ambience")) audio.PlayProfile(cave);
            if (GUILayout.Button("Fade out BGM")) audio.StopBgm();
            if (GUILayout.Button("UI click (2D)")) audio.Play2D(uiClick);
            if (GUILayout.Button("Random step (2D)")) audio.Play2D(step);
            GUILayout.Label("3D step X position (listener at origin): " + sourceX.ToString("F1"));
            sourceX = GUILayout.HorizontalSlider(sourceX, -20f, 20f);
            if (GUILayout.Button("Random step (3D)")) audio.Play3D(step, new Vector3(sourceX, 0f, 0f));
            if (GUILayout.Button("Pool limit: play 40 steps + UI click"))
            {
                for (int i = 0; i < 40; i++) audio.Play2D(step);
                audio.Play2D(uiClick);
            }
            DrawVolume(audio, "Master", audio.MasterVolume, true, default);
            foreach (var category in categories)
                DrawVolume(audio, category.ToString(), audio.GetVolume(category), false, category);
            if (GUILayout.Button("Save volume settings")) audio.SaveVolumes();
            GUILayout.EndArea();
        }

        private static void DrawVolume(AudioManager audio, string label, float value, bool master, AudioCategory category)
        {
            GUILayout.Label(label + " " + Mathf.RoundToInt(value * 100f) + "%");
            float next = GUILayout.HorizontalSlider(value, 0f, 1f);
            if (Mathf.Approximately(next, value)) return;
            if (master) audio.SetMasterVolume(next);
            else audio.SetVolume(category, next);
        }
    }
}
