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
    public static class BussinessShopBuilder
    {
        private const string ScenePath = "Assets/Scenes/Bussiness.unity";
        private const string Models = "Assets/Art/Models/BussinessShop/";
        private const string Materials = "Assets/Art/Materials/BussinessShop/";
        private const string Prefabs = "Assets/Prefabs/BussinessShop/";
        private static readonly Dictionary<string, Material> palette = new Dictionary<string, Material>();
        private static readonly Dictionary<string, GameObject> props = new Dictionary<string, GameObject>();
        private static readonly List<ShopStation> stations = new List<ShopStation>();
        [Serializable] private sealed class SourceData { public MaterialInfo[] materials; public ModelInfo[] assets; }
        [Serializable] private sealed class MaterialInfo { public string name; public float[] color; public float metallic; public float roughness; }
        [Serializable] private sealed class ModelInfo { public string name; public string anchor; public float[] dimensions; }
        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        [MenuItem("Untitled/Milk Tea Shop/Assemble Bussiness From Package")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath) throw new InvalidOperationException("Open Bussiness.unity first.");
            if (scene.GetRootGameObjects().Any(g => g.name == "BussinessShop_8x9"))
                throw new InvalidOperationException("Shop already assembled. Edit the saved scene/prefab directly.");
            Directory.CreateDirectory("Logs/BussinessShop");
            EditorSceneManager.SaveScene(scene, "Logs/BussinessShop/before_assembly.unity", true);
            EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory(Materials);
            Directory.CreateDirectory(Prefabs);
            AssetDatabase.Refresh();
            PrepareAssets();
            foreach (var go in scene.GetRootGameObjects().Where(g => g.name == "Main Camera" || g.name == "Directional Light"))
                Object.DestroyImmediate(go);
            stations.Clear();
            Transform shop = Group("BussinessShop_8x9", null);
            BuildArchitecture(Group("01_Architecture", shop));
            Transform markers = Group("02_LayoutPoints", shop);
            Transform playerSpawn = Point("PlayerSpawn", markers, V(-0.75f, 0.025f, 2.15f));
            Transform customerSpawn = Point("CustomerSpawn", markers, V(1.65f, 0.025f, -3.7f));
            BuildStations(Group("03_Stations", shop), markers);
            BuildCustomerArea(Group("04_CustomerArea", shop));
            BuildLighting(Group("05_Lighting", shop));
            PrefabUtility.SaveAsPrefabAssetAndConnect(shop.gameObject, Prefabs + "bussiness_shop_room.prefab", InteractionMode.AutomatedAction);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MilkTeaShop/milk_tea_shop_player.prefab"));
            player.name = "Bussiness_FirstPersonPlayer";
            player.transform.SetPositionAndRotation(playerSpawn.position, Quaternion.Euler(0, 180, 0));
            var camera = player.GetComponentInChildren<Camera>();
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var bindings = Group("BussinessShop_SystemBindings", null).gameObject.AddComponent<ShopSceneBindings>();
            var settings = new SerializedObject(bindings);
            settings.FindProperty("player").objectReferenceValue = player.GetComponent<ShopFirstPersonController>();
            settings.FindProperty("playerSpawn").objectReferenceValue = playerSpawn;
            settings.FindProperty("customerSpawn").objectReferenceValue = customerSpawn;
            var array = settings.FindProperty("stations");
            array.arraySize = stations.Count;
            for (int i = 0; i < stations.Count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = stations[i];
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Validate();
            Selection.activeGameObject = shop.gameObject;
            Debug.Log("BUSSINESS_SHOP_BUILD_OK: 8x9m, " + stations.Count + " station interfaces.");
        }

        private static Material SaveMaterial(string name, Color color, float metallic = 0, float smoothness = 0.2f)
        {
            string path = Materials + name.ToLowerInvariant() + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.name = name.ToLowerInvariant();
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            if (name == "AmberGlow" || name == "Glass" || name == "PortalGlow")
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color.linear * (name == "AmberGlow" ? 2f : name == "Glass" ? 0.3f : 0.5f));
            }
            palette[name] = mat;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void PrepareAssets()
        {
            palette.Clear();
            props.Clear();
            var source = JsonUtility.FromJson<SourceData>(File.ReadAllText(Models + "source_manifest.json"));
            foreach (var m in source.materials)
                SaveMaterial(m.name, new Color(m.color[0], m.color[1], m.color[2], 1).gamma, m.metallic, 1 - m.roughness);
            SaveMaterial("WallCream", new Color(0.88f, 0.85f, 0.75f));
            SaveMaterial("WallMatcha", new Color(0.38f, 0.46f, 0.32f));
            SaveMaterial("FloorLight", new Color(0.73f, 0.63f, 0.46f));
            SaveMaterial("FloorShade", new Color(0.69f, 0.58f, 0.41f));
            SaveMaterial("Grout", new Color(0.40f, 0.33f, 0.24f));
            foreach (var asset in source.assets)
            {
                string path = Models + asset.name + ".fbx";
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.globalScale = 1;
                importer.useFileScale = true;
                importer.bakeAxisConversion = true;
                importer.importNormals = ModelImporterNormals.Import;
                importer.importAnimation = false;
                importer.importLights = false;
                importer.importCameras = false;
                importer.SaveAndReimport();
                var root = new GameObject(asset.name);
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), root.transform);
                visual.name = "Visual";
                foreach (var renderer in visual.GetComponentsInChildren<MeshRenderer>())
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(m => palette[m.name]).ToArray();
                Bounds bounds = BoundsOf(root);
                Vector3 expected = V(asset.dimensions[0], asset.dimensions[2], asset.dimensions[1]);
                if (Vector3.Distance(expected, bounds.size) > 0.04f)
                    throw new InvalidOperationException("Meter conversion mismatch: " + asset.name + " " + bounds.size);
                props[asset.name] = PrefabUtility.SaveAsPrefabAsset(root, Prefabs + asset.name + ".prefab");
                Object.DestroyImmediate(root);
            }
        }

        private static Bounds BoundsOf(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static Transform Group(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static Transform Point(string name, Transform parent, Vector3 position)
        {
            var point = Group(name, parent);
            point.localPosition = position;
            return point;
        }

        private static GameObject Prop(string name, Transform parent, Vector3 position, float yaw = 0, bool collision = false)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(props[name], parent);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            if (collision)
            {
                Bounds world = BoundsOf(go);
                // Calculate local bounds with yaw temporarily neutral, keeping meter scale.
                Quaternion rotation = go.transform.rotation;
                go.transform.rotation = Quaternion.identity;
                world = BoundsOf(go);
                var collider = go.AddComponent<BoxCollider>();
                collider.center = world.center - go.transform.position;
                collider.size = world.size;
                go.transform.rotation = rotation;
            }
            return go;
        }

        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, string material, bool collision = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = palette[material];
            if (!collision) Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            return go;
        }

        private static void BuildArchitecture(Transform parent)
        {
            Box("Floor_Collision_8x9", parent, V(0, -0.1f, 0), V(8, 0.2f, 9), "Grout");
            for (int x = 0; x < 10; x++)
                for (int z = 0; z < 12; z++)
                    Box($"FloorTile_{x}_{z}", parent, V(-3.6f + x * 0.8f, 0.002f, -4.125f + z * 0.75f), V(0.793f, 0.004f, 0.743f), (x + z) % 3 == 0 ? "FloorShade" : "FloorLight", false);
            foreach (float x in new[] { -4.12f, 4.12f })
                Box("SideWall", parent, V(x, 1.6f, 0), V(0.24f, 3.2f, 9.48f), "WallCream");
            foreach (float z in new[] { -4.62f, 4.62f })
                Box("EndWall_Sealed", parent, V(0, 1.6f, z), V(8, 3.2f, 0.24f), "WallCream");
            Box("Ceiling", parent, V(0, 3.3f, 0), V(8.48f, 0.2f, 9.48f), "WallCream");
            foreach (float x in new[] { -3.975f, 3.975f })
                Box("MatchaSidePanel", parent, V(x, 0.38f, 0), V(0.035f, 0.76f, 9), "WallMatcha", false);
            foreach (float z in new[] { -4.475f, 4.475f })
                Box("MatchaEndPanel", parent, V(0, 0.38f, z), V(8, 0.76f, 0.035f), "WallMatcha", false);
            foreach (float z in new[] { -3f, -0.4f, 2.4f })
                Box("CeilingCrossBeam", parent, V(0, 3.12f, z), V(8.1f, 0.16f, 0.16f), "Walnut", false);
            foreach (float x in new[] { -2.7f, 0f, 2.7f })
                Box("CeilingLongBeam", parent, V(x, 3.13f, 0), V(0.12f, 0.12f, 9.1f), "Walnut", false);
            Prop("glass_double_door", parent, V(1.65f, 0, -4.43f), 180);
            Prop("wood_window", parent, V(-2.55f, 0.92f, -4.41f), 180);
            Prop("wood_window", parent, V(-0.85f, 0.92f, -4.41f), 180);
            Prop("wood_window", parent, V(-3.91f, 0.92f, -2.55f), -90);
            Prop("wood_window", parent, V(-3.91f, 0.92f, -0.45f), -90);
            Prop("store_sign", parent, V(1.65f, 2.42f, -4.39f), 180);
        }

        private static Transform Station(string id, ShopStationKind kind, string prompt, Vector3 approach, Transform parent, Transform markers)
        {
            var root = Group("Station_" + id, parent);
            var station = root.gameObject.AddComponent<ShopStation>();
            var data = new SerializedObject(station);
            data.FindProperty("stationId").stringValue = id;
            data.FindProperty("kind").enumValueIndex = (int)kind;
            data.FindProperty("prompt").stringValue = prompt;
            string[] feedback = { "收银台已准备就绪。", "查看订单板上的待办信息。", "查看备料台上的茶底与小料。", "检查封杯设备。", "检查取餐托盘。", "查看分类收纳的原料。", "检查水槽与清洗工具。", "地下门暂时关闭。" };
            data.FindProperty("feedback").stringValue = feedback[(int)kind];
            data.FindProperty("approachPoint").objectReferenceValue = Point(id, markers, approach);
            data.ApplyModifiedPropertiesWithoutUndo();
            stations.Add(station);
            return root;
        }

        private static void BuildStations(Transform parent, Transform markers)
        {
            var preparation = Station("PreparationStation", ShopStationKind.Preparation, "备料台 · 调配奶茶", V(-1.7f, 0.025f, 2.75f), parent, markers);
            Prop("work_counter", preparation, V(-2.6f, 0, 3.65f), 0, true);
            Prop("work_counter", preparation, V(-0.8f, 0, 3.65f), 0, true);
            for (int i = 0; i < 3; i++) Prop("tea_dispenser_0" + (i + 1), preparation, V(-3.1f + i * 0.53f, 0.9f, 3.68f), 0, true);
            Prop("topping_bin", preparation, V(-1.55f, 0.9f, 3.58f), 0, true);
            Prop("ingredient_bins", preparation, V(-0.9f, 0.9f, 3.45f), 0, true);
            Prop("syrup_pumps", preparation, V(-0.85f, 0.9f, 3.89f));
            Prop("blender", preparation, V(-0.22f, 0.9f, 3.67f), 0, true);
            var sealing = Station("SealingStation", ShopStationKind.Sealing, "封杯台 · 封装奶茶", V(-0.75f, 0.025f, 1.75f), parent, markers);
            Prop("work_counter", sealing, V(-2.6f, 0, 0.85f), 180, true);
            Prop("work_counter", sealing, V(-0.8f, 0, 0.85f), 180, true);
            Prop("cup_sealing_machine", sealing, V(-0.75f, 0.9f, 0.85f), 180, true);
            Prop("cup_shelf", sealing, V(-2.7f, 0.9f, 0.84f), 180, true);
            Prop("ice_bin", sealing, V(-1.9f, 0.9f, 0.85f), 180, true);
            Prop("paper_cups", sealing, V(-1.15f, 0.9f, 0.9f));
            Prop("takeout_bags", sealing, V(-1.5f, 0.9f, 0.65f), 180);
            var cashier = Station("OrderCounter", ShopStationKind.OrderCounter, "收银台 · 接单", V(0.65f, 0.025f, 1.75f), parent, markers);
            Prop("work_counter", cashier, V(1f, 0, 0.85f), 180, true);
            Prop("pos_terminal", cashier, V(0.6f, 0.9f, 0.86f), 180, true);
            Prop("straw_holder", cashier, V(1.78f, 0.9f, 0.85f));
            var delivery = Station("DeliveryPoint", ShopStationKind.Delivery, "取餐区 · 交付奶茶", V(1.4f, 0.025f, 1.75f), parent, markers);
            Prop("finished_boba", delivery, V(1.4f, 0.9f, 0.85f), 0, true);
            var board = Station("OrderBoard", ShopStationKind.OrderBoard, "订单板 · 查看待办", V(0.45f, 0.025f, 2.75f), parent, markers);
            Prop("notice_board", board, V(0.4f, 1.25f, 4.4f), 0, true);
            var water = Station("WaterStation", ShopStationKind.Water, "清洗台 · 取水与清洗", V(-2.65f, 0.025f, 2.2f), parent, markers);
            Prop("sink_counter", water, V(-3.62f, 0, 2.2f), -90, true);
            var storage = Station("StoragePoint", ShopStationKind.Storage, "仓储柜 · 查看原料", V(2.8f, 0.025f, 2.25f), parent, markers);
            Prop("storage_cabinet", storage, V(3.7f, 0, 2.25f), 90, true);
            Prop("display_shelf", storage, V(3.7f, 1.125f, 2.25f), 90, true);
            Prop("wooden_crate", storage, V(3.45f, 0, 3.2f), 0, true);
            Prop("ingredient_sack", storage, V(3.3f, 0, 3.8f), 0, true);
            Prop("cardboard_box", storage, V(3.42f, 0, 1.35f), 0, true);
            Prop("fruit_crate", storage, V(3.42f, 0.47f, 1.35f));
            Prop("ingredient_jars", storage, V(3.72f, 1.13f, 2.05f), 90);
            var dungeon = Station("DungeonEntrance", ShopStationKind.DungeonEntrance, "地下入口 · 检查门扉", V(2.1f, 0.025f, 2.9f), parent, markers);
            Prop("basement_portal", dungeon, V(2.1f, 0, 3.9f), 0, true);
            Box("DungeonDoor_Closed", dungeon, V(2.1f, 1.12f, 4.27f), V(1.28f, 2.24f, 0.12f), "Black");
            Prop("wall_vines", dungeon, V(3.25f, 1.6f, 4.36f));
            Transform decor = Group("StaffDetails", parent);
            Prop("menu_blackboard", decor, V(-3.85f, 1.3f, 0.15f), -90);
            Prop("world_log", decor, V(-3.86f, 1.38f, 3.53f), -90);
            Prop("trash_bin", decor, V(-3.55f, 0, 1.13f), 0, true);
        }

        private static void BuildCustomerArea(Transform parent)
        {
            Prop("square_table_set", parent, V(-2.6f, 0, -3.05f), 0, true);
            Prop("round_table_set", parent, V(-1.2f, 0, -1.35f), 0, true);
            Prop("tissue_dispenser", parent, V(-1.45f, 0.75f, -1.4f));
            Prop("wooden_bench", parent, V(3.63f, 0, -1.75f), 90, true);
            Prop("potted_plant", parent, V(-3.45f, 0, -3.95f), 0, true);
            Prop("potted_plant", parent, V(3.45f, 0, -3.83f), 0, true);
            Prop("potted_plant", parent, V(3.42f, 0, -0.25f), 0, true);
        }

        private static void BuildLighting(Transform parent)
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.38f, 0.36f, 0.30f);
            RenderSettings.fog = false;
            var key = Group("WarmInteriorBounce", parent).gameObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.transform.rotation = Quaternion.Euler(50, -25, 0);
            key.color = new Color(1, 0.91f, 0.78f);
            key.intensity = 0.7f;
            key.shadows = LightShadows.None;
            RenderSettings.sun = key;
            for (int i = 0; i < 5; i++)
            {
                Vector3 p = i < 3 ? V(-2.6f + i * 2.0f, 3.15f, 0.8f) : V(i == 3 ? -2f : 1.8f, 3.15f, -2.5f);
                Prop("pendant_lamp_" + (i % 3 == 0 ? "a" : i % 3 == 1 ? "b" : "c"), parent, p);
                var light = Group("PendantWarmLight", parent).gameObject.AddComponent<Light>();
                light.transform.position = p + V(0, -0.95f, 0);
                light.type = LightType.Point;
                light.color = new Color(1f, 0.79f, 0.53f);
                light.intensity = 2.5f;
                light.range = 4.5f;
                light.shadows = LightShadows.Soft;
            }
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.14f);
            bloom.threshold.Override(1.25f);
            string path = "Assets/Settings/BussinessShopPresentation.asset";
            AssetDatabase.CreateAsset(profile, path);
            foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            var volume = Group("ShopColorAndWarmLampGlow", parent).gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }

        [MenuItem("Untitled/Milk Tea Shop/Check Bussiness Layout")]
        public static void Validate()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath) throw new InvalidOperationException("Open Bussiness first.");
            var bindings = Object.FindObjectOfType<ShopSceneBindings>();
            Physics.SyncTransforms();
            var lines = new List<string> { "Interior 8x9x3.2m. Staff aisle approx 2.1m. Right access lane approx 2.1m before storage." };
            var points = bindings.Stations.Select(s => s.ApproachPoint).Concat(new[] { bindings.PlayerSpawn, bindings.CustomerSpawn });
            foreach (var point in points)
            {
                Collider[] hits = Physics.OverlapCapsule(point.position + V(0, 0.32f, 0), point.position + V(0, 1.5f, 0), 0.28f);
                if (hits.Any(h => !(h is CharacterController))) throw new InvalidOperationException("Blocked approach: " + point.name + ": " + string.Join(",", hits.Select(h => h.name)));
                lines.Add(point.name + " clear at " + point.position.ToString("F3"));
            }
            if (scene.isDirty) throw new InvalidOperationException("Unsaved scene.");
            File.WriteAllLines("Logs/BussinessShop/layout_checks.txt", lines);
            Debug.Log("BUSSINESS_SHOP_LAYOUT_OK");
        }
    }
}
