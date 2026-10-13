using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Untitled.Editor
{
    // Offline authoring only: scene/prefabs remain editable and are never rebuilt at runtime.
    public static partial class RitualHallDemoBuilder
    {
        public const string ScenePath = "Assets/Scenes/Props/GoblinRitualHallDemo.unity";
        private const string Models = "Assets/Art/Models/Props/RitualHall";
        private const string Materials = "Assets/Art/Materials/Props/RitualHall";
        private const string Textures = "Assets/Art/Textures/Props/RitualHall";
        private const string Prefabs = "Assets/Prefabs/Props/RitualHall";
        private static Material[] palette;
        private static readonly Dictionary<string, GameObject> furniture = new Dictionary<string, GameObject>();
        private enum Finish { Stone, StoneLight, StoneDark, Obsidian, Bone, Iron, Brass, Oak, OakDark, Rope, Burgundy, Cloth, Moss, Wax, Flame, Ember, Jade, Clay, Ash, Gold, Mushroom, Flesh, Hide, Charcoal }
        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        private static readonly Quaternion Q = Quaternion.identity;

        [MenuItem("Untitled/Ritual Hall/Open Scene Demo")]
        public static void OpenDemo()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath)) EditorSceneManager.OpenScene(ScenePath);
            else Build();
        }

        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Ritual hall exists. Edit its saved scene/prefab directly.");
            foreach (string folder in new[] { Models, Materials, Textures, Prefabs, "Assets/Scenes/Props" }) Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            PrepareMaterials();
            PrepareFurniture();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Transform room = Group("GoblinRitualHall_Module_26x26", null);
            BuildShell(room);
            BuildSanctuary(room);
            BuildSecretPassage(room);
            BuildProcession(room);
            BuildSideRituals(room);
            BuildWear(room);
            PrefabUtility.SaveAsPrefabAsset(room.gameObject, Prefabs + "/goblin_ritual_hall_room.prefab");
            Object.DestroyImmediate(room.gameObject);
            room = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/goblin_ritual_hall_room.prefab"))).transform;
            BuildPresentation();
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"RITUAL_BUILD_OK: {furniture.Count} original props; {room.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh.triangles.Length / 3)} placed triangles.");
            EditorSceneManager.OpenScene(ScenePath);
            ValidateSavedScene();
            Capture();
        }

        private static void PrepareMaterials()
        {
            Color[] colors = {
                new Color(.33f,.37f,.32f), new Color(.48f,.49f,.39f), new Color(.21f,.25f,.23f), new Color(.10f,.14f,.13f),
                new Color(.79f,.74f,.53f), new Color(.19f,.23f,.22f), new Color(.54f,.37f,.13f),
                new Color(.42f,.25f,.11f), new Color(.22f,.12f,.06f), new Color(.48f,.40f,.24f),
                new Color(.36f,.08f,.055f), new Color(.17f,.29f,.20f), new Color(.18f,.25f,.12f), new Color(.82f,.70f,.41f),
                new Color(1,.61f,.16f), new Color(.86f,.15f,.015f), new Color(.19f,.61f,.41f), new Color(.47f,.28f,.15f),
                new Color(.29f,.30f,.25f), new Color(.83f,.56f,.18f), new Color(.61f,.39f,.16f), new Color(.48f,.13f,.08f),
                new Color(.52f,.40f,.23f), new Color(.025f,.032f,.028f)
            };
            Texture2D stone = CreateTexture("ritual_weathered_stone", false);
            Texture2D woven = CreateTexture("ritual_woven_fibre", true);
            Texture2D wood = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/Props/Tavern/tavern_oak_grain.png");
            palette = new Material[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                Finish f = (Finish)i;
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "ritual_" + f.ToString().ToLowerInvariant() };
                mat.SetColor("_BaseColor", colors[i]); mat.SetFloat("_Smoothness", f == Finish.Jade ? .43f : f == Finish.Brass || f == Finish.Gold ? .40f : .09f);
                mat.SetFloat("_Metallic", f == Finish.Iron ? .50f : f == Finish.Brass || f == Finish.Gold ? .75f : 0);
                if (i <= 3) mat.SetTexture("_BaseMap", stone);
                if (f == Finish.Oak || f == Finish.OakDark) mat.SetTexture("_BaseMap", wood);
                if (f == Finish.Burgundy || f == Finish.Cloth || f == Finish.Hide) mat.SetTexture("_BaseMap", woven);
                if (f == Finish.Flame || f == Finish.Ember || f == Finish.Jade)
                {
                    mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", colors[i] * (f == Finish.Flame ? 3.2f : f == Finish.Jade ? .40f : 1.6f));
                }
                palette[i] = SaveAsset(mat, Materials + "/" + mat.name + ".mat");
            }
        }

        private static Texture2D CreateTexture(string name, bool woven)
        {
            var tex = new Texture2D(128, 128, TextureFormat.RGB24, false);
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
            {
                float value = woven ? .70f + .14f * Mathf.Sin(x * Mathf.PI / 2) * Mathf.Sin(y * Mathf.PI / 2) + .09f * Mathf.PerlinNoise(x * .41f, y * .41f)
                    : .68f + .23f * Mathf.PerlinNoise(x * .12f, y * .12f) + .07f * Mathf.PerlinNoise(x * .8f, y * .8f);
                tex.SetPixel(x, y, new Color(value, value, value));
            }
            tex.Apply(); string path = Textures + "/" + name + ".png"; File.WriteAllBytes(path, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path); var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default; importer.filterMode = FilterMode.Point; importer.wrapMode = TextureWrapMode.Repeat;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.mipmapEnabled = true; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static T SaveAsset<T>(T asset, string path) where T : Object
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(asset, existing); Object.DestroyImmediate(asset); EditorUtility.SetDirty(existing); return existing;
            }
            AssetDatabase.CreateAsset(asset, path); return asset;
        }

        private static Transform Group(string name, Transform parent)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform;
        }

        private static GameObject MeshObject(string name, Geometry geometry, Transform parent)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = SaveAsset(geometry.ToMesh(name), Models + "/" + name.ToLowerInvariant() + ".asset");
            go.AddComponent<MeshRenderer>().sharedMaterials = geometry.UsedMaterials.Select(i => palette[i]).ToArray(); return go;
        }

        private static void SaveFurniture(string name, Geometry geometry, Vector3 center, Vector3 size)
        {
            GameObject go = MeshObject(name, geometry, null);
            if (size != Vector3.zero) BoxCollision(go.transform, center, size);
            furniture[name] = PrefabUtility.SaveAsPrefabAsset(go, Prefabs + "/" + name + ".prefab"); Object.DestroyImmediate(go);
        }

        private static Transform Place(Transform parent, string name, Vector3 position, float yaw = 0, float scale = 1)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(furniture[name], parent);
            go.transform.localPosition = position; go.transform.localRotation = Quaternion.Euler(0, yaw, 0); go.transform.localScale = Vector3.one * scale;
            return go.transform;
        }

        private static Transform Reuse(Transform parent, string path, Vector3 position, float scale = 1, float yaw = 0)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/" + path + ".prefab");
            if (prefab == null) throw new InvalidOperationException("Missing library prop: " + path);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.localPosition = position; go.transform.localScale = Vector3.one * scale; go.transform.localRotation = Quaternion.Euler(0, yaw, 0); return go.transform;
        }

        private static void BoxCollision(Transform parent, Vector3 center, Vector3 size)
        {
            var c = parent.gameObject.AddComponent<BoxCollider>(); c.center = center; c.size = size;
        }

        private static Transform Solid(string name, Transform parent, Vector3 center, Vector3 size, Finish finish)
        {
            var g = new Geometry(); g.Box(center, size, finish, Q, .07f);
            Transform t = MeshObject(name, g, parent).transform; BoxCollision(t, center, size); return t;
        }

        private static Light Fire(Transform parent, string name, Vector3 position, float intensity, float range, Color color)
        {
            Transform t = Group(name, parent); t.localPosition = position; Light light = t.gameObject.AddComponent<Light>();
            light.type = LightType.Point; light.color = color; light.intensity = intensity; light.range = range; light.shadows = LightShadows.None;
            var flicker = t.gameObject.AddComponent<HybridFireLight>(); var settings = new SerializedObject(flicker);
            settings.FindProperty("baseIntensity").floatValue = intensity; settings.FindProperty("baseRange").floatValue = range; settings.ApplyModifiedPropertiesWithoutUndo();
            return light;
        }

        private static void Lamp(Transform parent, string path, Vector3 position, float intensity, float range, float scale = 1, float yaw = 0)
        {
            Transform t = Reuse(parent, path, position, scale, yaw); Light light = t.GetComponentInChildren<Light>();
            if (light == null) throw new InvalidOperationException("Missing lamp light: " + path);
            light.intensity = intensity; light.range = range; light.shadows = LightShadows.None;
            HybridFireLight flicker = light.GetComponent<HybridFireLight>();
            if (flicker != null)
            {
                var s = new SerializedObject(flicker); s.FindProperty("baseIntensity").floatValue = intensity;
                s.FindProperty("baseRange").floatValue = range / Mathf.Max(.01f, light.transform.lossyScale.x); s.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
