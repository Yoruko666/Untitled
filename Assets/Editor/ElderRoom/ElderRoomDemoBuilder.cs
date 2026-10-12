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
    // Explicit offline authoring. The saved scene and prefabs are the runtime source of truth.
    public static partial class ElderRoomDemoBuilder
    {
        public const string ScenePath = "Assets/Scenes/Props/GoblinElderRoomDemo.unity";
        private const string Models = "Assets/Art/Models/Props/ElderRoom";
        private const string Materials = "Assets/Art/Materials/Props/ElderRoom";
        private const string Textures = "Assets/Art/Textures/Props/ElderRoom";
        private const string Prefabs = "Assets/Prefabs/Props/ElderRoom";
        private static Material[] palette;
        private static readonly Dictionary<string, GameObject> furniture = new Dictionary<string, GameObject>();
        private enum Finish { Oak, OakLight, OakDark, Iron, Brass, Stone, StoneLight, StoneDark, Cloth, Bone, Flame, Ember, Bottle, Cork, Moss, Gold, Burgundy, Charcoal, Parchment, Straw, RedChip, BlueChip, Purple, Teal, Wax, Crystal }
        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        [MenuItem("Untitled/ElderRoom/Open Scene Demo")]
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
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Elder room demo already exists. Edit the saved scene directly.");
            foreach (string folder in new[] { Models, Materials, Textures, Prefabs, "Assets/Scenes/Props" }) Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            PrepareMaterials();
            PrepareFurniture();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Transform room = Group("GoblinElder_Module_18x18", null);
            BuildShell(room);
            BuildStudy(room);
            BuildSleeping(room);
            BuildRitual(room);
            BuildTreasury(room);
            BuildClutter(room);
            BuildLights(room);
            PrefabUtility.SaveAsPrefabAsset(room.gameObject, Prefabs + "/goblin_elder_room.prefab");
            Object.DestroyImmediate(room.gameObject);
            room = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/goblin_elder_room.prefab"))).transform;
            BuildPresentation();
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"ELDER_BUILD_OK: {furniture.Count} furniture prefabs; {room.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh.triangles.Length / 3)} placed triangles; {ScenePath}");
            EditorSceneManager.OpenScene(ScenePath);
            Capture();
        }

        private static void PrepareMaterials()
        {
            Color[] colors = {
                new Color(.47f,.27f,.12f), new Color(.63f,.41f,.22f), new Color(.24f,.13f,.07f),
                new Color(.24f,.28f,.26f), new Color(.50f,.34f,.12f),
                new Color(.32f,.36f,.32f), new Color(.43f,.46f,.39f), new Color(.22f,.27f,.25f),
                new Color(.12f,.28f,.22f), new Color(.77f,.72f,.52f),
                new Color(1,.65f,.22f), new Color(.82f,.17f,.03f), new Color(.10f,.24f,.17f),
                new Color(.42f,.29f,.14f), new Color(.15f,.22f,.10f), new Color(.85f,.55f,.12f),
                new Color(.34f,.075f,.065f), new Color(.035f,.045f,.042f), new Color(.75f,.64f,.39f),
                new Color(.51f,.46f,.24f), new Color(.65f,.17f,.12f), new Color(.15f,.36f,.43f), new Color(.30f,.13f,.32f), new Color(.14f,.33f,.28f), new Color(.87f,.73f,.40f), new Color(.22f,.75f,.58f)
            };
            Texture2D stone = CreateTexture("elder_stone_grain", false);
            Texture2D weave = CreateTexture("elder_woven_fibre", true);
            Texture2D wood = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/Props/Tavern/tavern_oak_grain.png");
            palette = new Material[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "elder_" + ((Finish)i).ToString().ToLowerInvariant() };
                mat.SetColor("_BaseColor", colors[i]);
                mat.SetFloat("_Smoothness", i == 15 ? .56f : i == 3 || i == 4 ? .30f : .08f);
                mat.SetFloat("_Metallic", i == 15 ? .78f : i == 3 ? .38f : i == 4 ? .65f : 0);
                if (i < 3) mat.SetTexture("_BaseMap", wood);
                if (i >= 5 && i <= 7) mat.SetTexture("_BaseMap", stone);
                if (i == 8 || i == 16 || i == 19 || i == 22 || i == 23) mat.SetTexture("_BaseMap", weave);
                if (i == 10 || i == 11)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", colors[i] * (i == 10 ? 3 : 1.5f));
                }
                palette[i] = SaveAsset(mat, Materials + "/" + mat.name + ".mat");
            }
        }

        private static Texture2D CreateTexture(string name, bool woven)
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGB24, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float value = woven
                        ? .72f + .16f * Mathf.Sin(x * Mathf.PI / 2) * Mathf.Sin(y * Mathf.PI / 2) + .06f * Mathf.PerlinNoise(x * .43f, y * .4f)
                        : .71f + .23f * Mathf.PerlinNoise(x * .15f, y * .15f) + .06f * Mathf.PerlinNoise(x * .85f, y * .85f);
                    texture.SetPixel(x, y, new Color(value, value, value));
                }
            texture.Apply();
            string path = Textures + "/" + name + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static T SaveAsset<T>(T asset, string path) where T : Object
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(asset, existing);
                Object.DestroyImmediate(asset);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static Transform Group(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static GameObject MeshObject(string name, Geometry geometry, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = SaveAsset(geometry.ToMesh(name), Models + "/" + name.ToLowerInvariant() + ".asset");
            go.AddComponent<MeshRenderer>().sharedMaterials = geometry.UsedMaterials.Select(i => palette[i]).ToArray();
            return go;
        }

        private static void SaveFurniture(string name, Geometry geometry, Vector3 center, Vector3 size)
        {
            GameObject go = MeshObject(name, geometry, null);
            if (size != Vector3.zero) BoxCollision(go.transform, center, size);
            furniture[name] = PrefabUtility.SaveAsPrefabAsset(go, Prefabs + "/" + name + ".prefab");
            Object.DestroyImmediate(go);
        }

        private static Transform Place(Transform parent, string name, Vector3 position, float yaw = 0, float scale = 1)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(furniture[name], parent);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale = Vector3.one * scale;
            return go.transform;
        }

        private static Transform Reuse(Transform parent, string path, Vector3 position, float scale = 1, float yaw = 0)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/" + path + ".prefab");
            if (prefab == null) throw new InvalidOperationException("Missing library prop: " + path);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.localPosition = position;
            go.transform.localScale = Vector3.one * scale;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            return go.transform;
        }

        private static void BoxCollision(Transform parent, Vector3 center, Vector3 size)
        {
            var collider = parent.gameObject.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
        }

        private static void SetLamp(Transform lamp, float intensity, float range, bool shadows = false)
        {
            Light light = lamp.GetComponentInChildren<Light>();
            if (light == null) throw new InvalidOperationException("Lamp has no Light: " + lamp.name);
            light.intensity = intensity;
            light.range = range;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            HybridFireLight flicker = light.GetComponent<HybridFireLight>();
            if (flicker != null)
            {
                var settings = new SerializedObject(flicker);
                settings.FindProperty("baseIntensity").floatValue = intensity;
                settings.FindProperty("baseRange").floatValue = range / Mathf.Max(.01f, light.transform.lossyScale.x);
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
