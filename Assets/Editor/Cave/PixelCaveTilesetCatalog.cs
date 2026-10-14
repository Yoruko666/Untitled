using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GiantGrey.TileWorldCreator;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Untitled.EditorTools
{
    /// <summary>Registers existing cave prefabs and renders their actual materials for the TWC selector.</summary>
    public static class PixelCaveTilesetCatalog
    {
        private const string Catalog = "Assets/Configs/PixelCaveTilesets";
        private const string Images = "Assets/Art/Previews/PixelCaveTilesets";
        private const int PreviewLayer = 30;
        private static readonly string Result = Path.Combine(Path.GetTempPath(), "untitled-pixel-cave-tilesets");
        private static readonly List<TilePreset> presets = new List<TilePreset>();
        private static readonly FieldInfo[] standardSlots = typeof(TilePreset).GetFields()
            .Where(f => f.Name.StartsWith("NRMGRD_") && f.FieldType == typeof(GameObject)).ToArray();
        private static Scene scene;
        private static Camera camera;
        private static double started;

        [InitializeOnLoadMethod]
        private static void CheckRequest()
        {
            EditorApplication.delayCall += () =>
            {
                string request = Path.Combine(Result, "build.request");
                if (!File.Exists(request)) return;
                File.Delete(request);
                try { Build(); }
                catch (Exception error)
                {
                    Directory.CreateDirectory(Result);
                    File.WriteAllText(Path.Combine(Result, "error.txt"), error.ToString());
                    Debug.LogException(error);
                }
            };
        }

        [MenuItem("Untitled/Cave/Refresh Pixel Cave Tilesets and Previews")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before refreshing the catalog.");
            Directory.CreateDirectory(Result);
            presets.Clear();
            foreach (string folder in new[] { Catalog, Images }) Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            // Preserve existing preset GUIDs and the maps that already reference them.
            foreach (string path in new[] {
                "Assets/Configs/PixelCaveTiles/pixel_cliffs_earth.asset",
                "Assets/Configs/PixelCaveTiles/pixel_cliffs_moss.asset",
                "Assets/Configs/PixelCaveConnectedRocks/pixel_rock_cliffs_240.asset",
                "Assets/Configs/PixelCaveConnectedRocks/pixel_rock_cliffs_480.asset" })
            {
                var preset = AssetDatabase.LoadAssetAtPath<TilePreset>(path);
                if (preset == null) throw new InvalidOperationException("Missing preset: " + path);
                if (Models(preset).Count() != 5) throw new InvalidOperationException("Incomplete dual preset: " + path);
                presets.Add(preset);
            }
            foreach (string family in new[] { "PixelCaveTiles", "PixelCaveConnectors", "PixelCaveWalls", "PixelCaveConnectedRocks" })
            {
                string folder = Catalog + "/" + family;
                Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
                foreach (string path in Directory.GetFiles("Assets/Prefabs/Terrain/" + family, "*.prefab").OrderBy(p => p))
                {
                    // All five cliff roles are already represented by their complete dual-grid preset.
                    string module = Path.GetFileNameWithoutExtension(path);
                    if (module.Contains("cliff_")) continue;
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path.Replace('\\', '/'));
                    if (prefab == null) throw new InvalidOperationException("Missing prefab: " + path);
                    bool floor = module.StartsWith("tile_floor_");
                    string name = "pixel_cave_" + (floor ? "floor_" : "stamp_") + family.ToLowerInvariant() + "_" + module.Substring(5);
                    string assetPath = folder + "/" + name + ".asset";
                    var preset = AssetDatabase.LoadAssetAtPath<TilePreset>(assetPath);
                    if (preset == null)
                    {
                        preset = ScriptableObject.CreateInstance<TilePreset>();
                        AssetDatabase.CreateAsset(preset, assetPath);
                    }
                    preset.name = name;
                    preset.gridtype = TilePreset.GridType.standard;
                    // Stamp presets repeat the chosen module; they do not claim to supply missing junction meshes.
                    foreach (var field in standardSlots) field.SetValue(preset, prefab);
                    EditorUtility.SetDirty(preset);
                    presets.Add(preset);
                }
            }
            File.WriteAllText(Catalog + "/TWCFilter_PixelCave.twcf", "pixel_cliffs_\npixel_rock_cliffs_\npixel_cave_\n");
            AssetDatabase.SaveAssets();
            scene = EditorSceneManager.NewPreviewScene();
            var cameraObject = NewObject("Tileset thumbnail camera");
            camera = cameraObject.AddComponent<Camera>();
            camera.scene = scene;
            camera.enabled = false;
            camera.orthographic = true;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 200;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.12f, .15f, .19f);
            camera.cullingMask = 1 << PreviewLayer;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            AddLight("Warm key", new Vector3(45, -35, 0), new Color(1, .9f, .77f), 1.6f);
            AddLight("Cool fill", new Vector3(35, 145, 0), new Color(.7f, .82f, 1), .7f);
            started = EditorApplication.timeSinceStartup;
            EditorApplication.update += Finish;
        }

        private static GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.layer = PreviewLayer;
            return go;
        }

        private static void AddLight(string name, Vector3 rotation, Color color, float intensity)
        {
            var go = NewObject(name);
            go.transform.rotation = Quaternion.Euler(rotation);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            light.cullingMask = 1 << PreviewLayer;
        }

        private static IEnumerable<GameObject> Models(TilePreset preset)
        {
            return typeof(TilePreset).GetFields().Where(f => f.FieldType == typeof(GameObject))
                .Select(f => f.GetValue(preset) as GameObject).Where(p => p != null).Distinct();
        }

        private static void Finish()
        {
            if (EditorApplication.timeSinceStartup - started < 4) return;
            EditorApplication.update -= Finish;
            try
            {
                var manifest = new List<string>();
                foreach (var preset in presets)
                {
                    string imagePath = Images + "/" + preset.name + ".png";
                    Render(preset, imagePath);
                    AssetDatabase.ImportAsset(imagePath, ImportAssetOptions.ForceSynchronousImport);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(imagePath);
                    importer.textureType = TextureImporterType.Default;
                    importer.mipmapEnabled = false;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.filterMode = FilterMode.Point;
                    importer.maxTextureSize = 256;
                    importer.SaveAndReimport();
                    preset.previewThumbnail = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);
                    EditorUtility.SetDirty(preset);
                    manifest.Add(preset.name + "\t" + AssetDatabase.GetAssetPath(preset) + "\t" + imagePath);
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                File.WriteAllLines(Path.Combine(Result, "catalog.tsv"), manifest);
                File.WriteAllText(Path.Combine(Result, "success.txt"), "Rendered and registered " + presets.Count + " tile presets.");
                Debug.Log("Pixel Cave catalog complete: " + presets.Count + " presets with thumbnails.");
                EditorSceneManager.ClosePreviewScene(scene);
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                File.WriteAllText(Path.Combine(Result, "error.txt"), error.ToString());
                Debug.LogException(error);
                if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static void Render(TilePreset preset, string path)
        {
            var instances = new List<GameObject>();
            var target = RenderTexture.GetTemporary(256, 256, 24, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            Texture2D pixels = null;
            try
            {
                var models = Models(preset).ToArray();
                for (int i = 0; i < models.Length; i++)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(models[i], scene);
                    instances.Add(instance);
                    instance.transform.position = models.Length == 1 ? Vector3.zero : new Vector3((i % 3) * 5, 0, (i / 3) * 5);
                    foreach (var child in instance.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = PreviewLayer;
                }
                var renderers = instances.SelectMany(i => i.GetComponentsInChildren<Renderer>()).ToArray();
                if (renderers.Length == 0) throw new InvalidOperationException("No renderer: " + preset.name);
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                var direction = new Vector3(1, .85f, -1).normalized;
                camera.transform.position = bounds.center + direction * 70;
                camera.transform.LookAt(bounds.center);
                camera.aspect = 1;
                float extent = 0;
                for (int x = -1; x <= 1; x += 2)
                    for (int y = -1; y <= 1; y += 2)
                        for (int z = -1; z <= 1; z += 2)
                        {
                            var corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                            var local = camera.transform.InverseTransformPoint(corner);
                            extent = Mathf.Max(extent, Mathf.Abs(local.x), Mathf.Abs(local.y));
                        }
                camera.orthographicSize = Mathf.Max(.5f, extent * 1.18f);
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                pixels = new Texture2D(256, 256, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                if (pixels != null) Object.DestroyImmediate(pixels);
                RenderTexture.ReleaseTemporary(target);
                foreach (var instance in instances) Object.DestroyImmediate(instance);
            }
        }
    }
}
