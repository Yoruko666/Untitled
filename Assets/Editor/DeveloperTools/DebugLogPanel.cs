using UnityEditor;
using UnityEngine;

namespace Untitled.Editor
{
    // Reusable Editor view; results and history are owned by DebugCommandService.
    internal sealed class DebugLogPanel
    {
        private Vector2 scroll;
        private bool expanded = true;

        public void Draw(DebugCommandService service)
        {
            expanded = EditorGUILayout.Foldout(expanded, $"命令执行日志（最近 {service.History.Count}/100 条）", true);
            if (!expanded) return;
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(190));
            for (int i = service.History.Count - 1; i >= 0; i--)
            {
                var result = service.History[i];
                EditorGUILayout.LabelField($"{result.Time:HH:mm:ss} [{result.Status}] {result.Command}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField(result.Message, EditorStyles.wordWrappedLabel);
                GUILayout.Space(5);
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
