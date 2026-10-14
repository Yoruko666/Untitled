using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Untitled.EditorTools
{
    internal static class CaveRefinementTask
    {
        [InitializeOnLoadMethod]
        private static void OnReload()
        {
            EditorApplication.delayCall += () =>
            {
                string folder = Path.Combine(Path.GetTempPath(), "untitled-cave-tileworld-refined");
                string request = Path.Combine(folder, "full.request");
                if (!File.Exists(request)) return;
                File.Delete(request);
                try { CaveTileWorldRefinement.BuildEntireMapFromKit(); }
                catch (Exception error)
                {
                    File.WriteAllText(Path.Combine(folder, "bridge_error.txt"), error.ToString());
                    Debug.LogException(error);
                }
            };
        }
    }
}
