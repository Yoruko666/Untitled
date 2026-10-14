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

namespace Untitled.EditorTools
{
    // Explicit, one-time authoring. Runtime only reads the saved scene.
    public static class MilkTeaShopBuilder
    {
        public const string ScenePath = "Assets/Scenes/MilkTeaShop.unity";
        private const string Models = "Assets/Art/Models/MilkTeaShop/";
        private const string Materials = "Assets/Art/Materials/MilkTeaShop/";
        private const string Prefabs = "Assets/Prefabs/MilkTeaShop/";
        private static readonly Dictionary<string, Material> palette = new Dictionary<string, Material>();
        private static readonly Dictionary<string, GameObject> furniture = new Dictionary<string, GameObject>();
        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        [MenuItem("Untitled/Milk Tea Shop/Open Day 1 Scene")]
        public static void Open()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath)) EditorSceneManager.OpenScene(ScenePath);
            else Build();
        }

        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Scene exists. Edit the saved scene directly.");
            if (EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save current scene before building.");
            Directory.CreateDirectory(Materials);
            Directory.CreateDirectory(Prefabs);
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PrepareAssets();
            Transform room = Group("MilkTeaShop_8m_x_10m", null);
            BuildRoom(Group("01_Architecture", room));
            Transform props = Group("02_Furniture", room);
            Place("milk_tea_counter", props, V(-0.65f, 0, 1.25f));
            Place("milk_tea_workbench", props, V(-0.65f, 0, 4.27f));
            Place("milk_tea_shelf", props, V(-2.92f, 0, 4.54f));
            Place("milk_tea_shelf", props, V(-3.63f, 0, 2.22f), -90);
            Place("wooden_crate", props, V(3.25f, 0, 4.22f));
            Place("wooden_crate", props, V(2.23f, 0, 4.22f));
            Place("wooden_barrel", props, V(3.30f, 0, 3.20f));
            BuildHatch(Group("03_UndergroundEntrancePlaceholder", room));
            Transform markers = Group("04_ReservedPoints", room);
            Marker("PlayerSpawn", markers, V(0.65f, 0.02f, -2.85f));
            Marker("CustomerStand_01", markers, V(-0.65f, 0.02f, 0.18f));
            Marker("CustomerStand_02", markers, V(-0.65f, 0.02f, -1.02f));
            Marker("CustomerStand_03", markers, V(-0.65f, 0.02f, -2.22f));
            Marker("PreparationStation", markers, V(-0.65f, 0.02f, 3.24f));
            Marker("OrderCounter", markers, V(-0.65f, 0.02f, 2.35f));
            Marker("DungeonEntrance", markers, V(2.20f, 0.02f, 1.50f));
            Marker("CustomerSpawn", markers, V(-3.15f, 0.02f, -3.30f));
            Marker("DeliveryPoint", markers, V(0.9f, 0.02f, 1.25f));
            BuildLighting(Group("06_Lighting", room));
            MilkTeaShopFirstPersonSetup.ConfigureInterior(room);
            MilkTeaShopFirstPersonSetup.ConfigureInteractionPrefabs();
            MilkTeaShopFirstPersonSetup.ConfigureHatch(room);
            MilkTeaShopFirstPersonSetup.SaveRoomPrefab(room);
            MilkTeaShopFirstPersonSetup.CreatePlayer(markers.Find("PlayerSpawn").position);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Scene save failed.");
            Selection.activeGameObject = room.gameObject;
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.LookAt(V(0, 0.5f, 0.3f), Quaternion.Euler(50, 0, 0), 12f);
            Validate();
            Capture();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("MILK_TEA_BUILD_OK: " + ScenePath);
        }

        private static Material Material(string name, string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", 0.18f);
            AssetDatabase.CreateAsset(mat, Materials + name.ToLowerInvariant() + ".mat");
            palette.Add(name, mat);
            return mat;
        }

        private static void PrepareAssets()
        {
            palette.Clear();
            furniture.Clear();
            string[] names = { "Shop_Cream", "Shop_LightWood", "Shop_WoodLight", "Shop_WoodShade", "Shop_Matcha", "Shop_MatchaLight", "Shop_MatchaDark", "Tea_Amber", "Tea_Pearl", "Dungeon_DarkWood", "Dungeon_WoodLight", "Dungeon_WoodShade", "Dungeon_GreyPurple", "Dungeon_PurpleDark", "Window_Glass", "Floor_Grout" };
            string[] colors = { "F3EAD6", "CDA46E", "DFBD8C", "B78959", "789365", "A3B582", "455B43", "B57742", "45352D", "654937", "7D5940", "503B30", "777284", "514D60", "BCD6C8", "DBD1B9" };
            for (int i = 0; i < names.Length; i++) Material(names[i], colors[i]);
            var sizes = new Dictionary<string, Vector3>
            {
                { "milk_tea_counter", V(2.3f, 1.36639f, 0.9715f) },
                { "milk_tea_workbench", V(2.28f, 1.511f, 0.883f) },
                { "milk_tea_shelf", V(1.57f, 1.995f, 0.551f) },
                { "wooden_crate", V(0.871f, 0.7f, 0.85454f) },
                { "wooden_barrel", V(0.732f, 0.9f, 0.732f) }
            };
            foreach (var item in sizes)
            {
                string path = Models + item.Key + ".fbx";
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.globalScale = 1f;
                importer.useFileScale = true;
                importer.bakeAxisConversion = true;
                importer.importNormals = ModelImporterNormals.Import;
                importer.importAnimation = false;
                importer.importCameras = false;
                importer.importLights = false;
                importer.SaveAndReimport();
                var root = new GameObject(item.Key);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), root.transform);
                model.name = "Visual";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
                foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>())
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(m => palette.TryGetValue(m.name, out Material mapped) ? mapped : throw new InvalidOperationException("Unknown source material: " + m.name)).ToArray();
                Bounds bounds = BoundsOf(root);
                if (Vector3.Distance(bounds.size, item.Value) > 0.025f || Mathf.Abs(bounds.min.y) > 0.015f)
                    throw new InvalidOperationException(item.Key + " unexpected meter dimensions: " + bounds);
                var collider = root.AddComponent<BoxCollider>();
                collider.center = bounds.center;
                collider.size = bounds.size;
                furniture.Add(item.Key, PrefabUtility.SaveAsPrefabAsset(root, Prefabs + item.Key + ".prefab"));
                Object.DestroyImmediate(root);
            }
        }

        private static Transform Group(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, string material, bool collision = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = palette[material];
            if (!collision) Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            return go;
        }

        private static void Boundary(string name, Transform parent, Vector3 center, Vector3 size)
        {
            var collider = Group(name, parent).gameObject.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
        }

        private static void BuildRoom(Transform parent)
        {
            Box("Floor_8x10_TopY0", parent, V(0, -0.12f, 0), V(8, 0.24f, 10), "Floor_Grout");
            for (int x = 0; x < 8; x++)
                for (int z = 0; z < 10; z++)
                    Box($"Tile_{x}_{z}", parent, V(-3.5f + x, 0.002f, -4.5f + z), V(0.985f, 0.004f, 0.985f), (x + z) % 3 == 0 ? "Shop_WoodLight" : "Shop_Cream", false);
            // Rear wall with a real 2.0 x 1.35m window opening.
            Box("BackWall_WindowSill", parent, V(0, 0.6f, 5.12f), V(8.24f, 1.2f, 0.24f), "Shop_Cream");
            Box("BackWall_WindowHeader", parent, V(0, 2.675f, 5.12f), V(8.24f, 0.25f, 0.24f), "Shop_Cream");
            Box("BackWall_Left", parent, V(-2.15f, 1.875f, 5.12f), V(3.94f, 1.35f, 0.24f), "Shop_Cream");
            Box("BackWall_Right", parent, V(2.97f, 1.875f, 5.12f), V(2.30f, 1.35f, 0.24f), "Shop_Cream");
            Box("Window_Glass_Closed", parent, V(0.82f, 1.875f, 5.16f), V(2, 1.35f, 0.035f), "Window_Glass");
            foreach (float x in new[] { -0.22f, 0.82f, 1.86f })
                Box("Window_VerticalFrame", parent, V(x, 1.875f, 4.97f), V(0.065f, 1.45f, 0.09f), "Shop_LightWood", false);
            foreach (float y in new[] { 1.16f, 1.875f, 2.59f })
                Box("Window_HorizontalFrame", parent, V(0.82f, y, 4.97f), V(2.16f, 0.065f, 0.09f), "Shop_LightWood", false);
            // Side walls are cut away visually; full collision keeps the room enclosed.
            Box("RightWall_Cutaway", parent, V(4.12f, 0.325f, 0), V(0.24f, 0.65f, 10), "Shop_Cream");
            Box("LeftWall_Rear", parent, V(-4.12f, 0.325f, 1.2f), V(0.24f, 0.65f, 7.6f), "Shop_Cream");
            Box("LeftWall_Front", parent, V(-4.12f, 0.325f, -4.5f), V(0.24f, 0.65f, 1), "Shop_Cream");
            Boundary("RightBoundary_FullHeight", parent, V(4.12f, 1.4f, 0), V(0.24f, 2.8f, 10));
            Boundary("LeftBoundary_FullHeight", parent, V(-4.12f, 1.4f, 0), V(0.24f, 2.8f, 10));
            Boundary("FrontBoundary_Cutaway", parent, V(0, 1.4f, -5.12f), V(8.24f, 2.8f, 0.24f));
            Box("Door_Closed_Day1", parent, V(-4.10f, 1.05f, -3.3f), V(0.075f, 2.10f, 1.35f), "Shop_Matcha");
            foreach (float z in new[] { -4.02f, -2.58f })
                Box("DoorFrame", parent, V(-4.06f, 1.12f, z), V(0.15f, 2.24f, 0.09f), "Shop_LightWood", false);
            Box("DoorLintel", parent, V(-4.06f, 2.24f, -3.3f), V(0.15f, 0.09f, 1.52f), "Shop_LightWood", false);
            Box("DoorHandle", parent, V(-4.02f, 1.01f, -2.87f), V(0.06f, 0.08f, 0.14f), "Shop_Cream", false);
            Box("BackWall_MatchaSkirting", parent, V(0, 0.25f, 4.975f), V(8, 0.5f, 0.035f), "Shop_Matcha", false);
            foreach (float x in new[] { -3.975f, 3.975f })
                Box("SideWall_WoodCap", parent, V(x, 0.665f, 0), V(0.29f, 0.055f, 10), "Shop_LightWood", false);
            Box("EntryMat", parent, V(-2.95f, 0.008f, -3.3f), V(1.25f, 0.016f, 1.35f), "Shop_MatchaLight", false);
        }

        private static void Place(string asset, Transform parent, Vector3 position, float yaw = 0)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(furniture[asset], parent);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        }

        private static void BuildHatch(Transform parent)
        {
            // Flush closed hatch, no hole or scene transition in this phase.
            Box("ClosedHatchFrame", parent, V(2.2f, 0.007f, 2.7f), V(1.25f, 0.014f, 1.4f), "Dungeon_PurpleDark", false);
            for (int i = 0; i < 5; i++)
                Box("HatchPlank", parent, V(1.74f + i * 0.23f, 0.018f, 2.7f), V(0.215f, 0.02f, 1.27f), "Shop_WoodShade", false);
            foreach (float z in new[] { 2.25f, 3.15f })
                Box("HatchBrace", parent, V(2.2f, 0.035f, z), V(1.1f, 0.014f, 0.065f), "Dungeon_GreyPurple", false);
            Box("HatchHandle", parent, V(2.2f, 0.05f, 2.35f), V(0.20f, 0.025f, 0.06f), "Dungeon_PurpleDark", false);
        }

        private static void Marker(string name, Transform parent, Vector3 position)
        {
            Group(name, parent).localPosition = position;
        }

        private static void BuildLighting(Transform parent)
        {
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.53f, 0.48f, 0.38f);
            RenderSettings.reflectionIntensity = 0;
            var sun = Group("WarmIndoorKey", parent);
            sun.rotation = Quaternion.Euler(55, -25, 0);
            var key = sun.gameObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(1f, 0.89f, 0.72f);
            key.intensity = 1.1f;
            key.shadows = LightShadows.Soft;
            key.shadowBias = 0.025f;
            key.shadowNormalBias = 0.15f;
            RenderSettings.sun = key;
            foreach (var position in new[] { V(-1.8f, 2.65f, 2.6f), V(2, 2.65f, -1.7f) })
            {
                var go = Group("WarmCeilingFill", parent);
                go.localPosition = position;
                var light = go.gameObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1, 0.81f, 0.59f);
                light.intensity = 1.6f;
                light.range = 6.5f;
                light.shadows = LightShadows.None;
            }
        }

        private static Bounds BoundsOf(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        [MenuItem("Untitled/Milk Tea Shop/Check Saved Scene")]
        public static void Validate()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath) throw new InvalidOperationException("Open MilkTeaShop first.");
            var roots = scene.GetRootGameObjects();
            var all = roots.SelectMany(g => g.GetComponentsInChildren<Transform>()).ToArray();
            var lines = new List<string> { "MilkTeaShop: 1 Unity unit = 1m; XZ floor = 8 x 10; Y=0 floor top." };
            foreach (string name in new[] { "milk_tea_counter", "milk_tea_workbench", "milk_tea_shelf", "wooden_crate", "wooden_barrel" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + name + ".prefab");
                if (prefab == null) throw new InvalidOperationException("Missing prefab: " + name);
                lines.Add(name + " bounds (X,Y,Z): " + BoundsOf(prefab).size.ToString("F4"));
            }
            Physics.SyncTransforms();
            string[] points = { "PlayerSpawn", "CustomerSpawn", "OrderCounter", "PreparationStation", "DeliveryPoint", "DungeonEntrance" };
            foreach (string name in points)
            {
                Vector3 p = all.Single(t => t.name == name).position;
                Collider[] hits = Physics.OverlapCapsule(p + V(0, 0.32f, 0), p + V(0, 1.5f, 0), 0.28f);
                if (hits.Any(c => !(c is CharacterController))) throw new InvalidOperationException("Blocked reserved point: " + name);
                lines.Add(name + " clear at " + p.ToString("F2"));
            }
            if (all.Any(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) != 0))
                throw new InvalidOperationException("Missing script in scene.");
            if (all.SelectMany(t => t.GetComponents<Renderer>()).Any(r => r.sharedMaterials.Any(m => m == null || m.shader == null)))
                throw new InvalidOperationException("Missing material/shader.");
            if (scene.isDirty) throw new InvalidOperationException("Scene has unsaved changes.");
            string folder = Path.GetFullPath("Logs/MilkTeaShop");
            Directory.CreateDirectory(folder);
            File.WriteAllLines(folder + "/checks.txt", lines);
            Debug.Log("MILK_TEA_CHECK_OK: saved scene, meter dimensions, clear reserved points, script/material references.");
        }

        [MenuItem("Untitled/Milk Tea Shop/Capture Scene Camera")]
        public static void Capture()
        {
            if (EditorSceneManager.GetActiveScene().path != ScenePath) throw new InvalidOperationException("Open MilkTeaShop first.");
            Camera camera = Camera.main;
            var target = new RenderTexture(1600, 1200, 24);
            var image = new Texture2D(1600, 1200, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            float aspect = camera.aspect;
            try
            {
                target.Create();
                camera.aspect = 4f / 3f;
                camera.targetTexture = target;
                camera.Render();
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1600, 1200), 0, 0);
                image.Apply();
                Directory.CreateDirectory("Logs/MilkTeaShop");
                File.WriteAllBytes("Logs/MilkTeaShop/milk_tea_shop.png", image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                camera.aspect = aspect;
                RenderTexture.active = previousActive;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(image);
            }
        }
    }
}
