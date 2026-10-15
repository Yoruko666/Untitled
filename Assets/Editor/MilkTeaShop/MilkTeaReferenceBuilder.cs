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
    // Explicit authoring only. The saved scene and nested prefabs own the layout.
    public static class MilkTeaReferenceBuilder
    {
        public const string ScenePath = "Assets/Scenes/MilkTeaShopReference.unity";
        private const string Models = "Assets/Art/Models/MilkTeaReference/";
        private const string Materials = "Assets/Art/Materials/MilkTeaReference/";
        private const string Prefabs = "Assets/Prefabs/MilkTeaReference/";
        private const string ProfilePath = "Assets/Settings/MilkTeaReferencePresentation.asset";
        private static readonly Dictionary<string, Material> palette = new Dictionary<string, Material>();
        private static readonly Dictionary<string, GameObject> props = new Dictionary<string, GameObject>();
        private static readonly List<ShopStation> stations = new List<ShopStation>();

        [Serializable] private sealed class SourceInfo { public MaterialInfo[] materials; public AssetInfo[] assets; }
        [Serializable] private sealed class MaterialInfo { public string name; public float[] color; public float metallic; public float roughness; }
        [Serializable] private sealed class AssetInfo { public string name; public float[] dimensions; }
        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        [MenuItem("Untitled/Milk Tea Shop/Create Reference Interior")]
        public static void Create()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Reference scene exists. Edit the saved scene and prefabs.");
            if (EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the current scene first.");
            Directory.CreateDirectory(Materials);
            Directory.CreateDirectory(Prefabs);
            AssetDatabase.Refresh();
            ImportAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var room = Group("MilkTeaShop_Reference_3x7m", null);
            Architecture(Group("01_Architecture", room));
            var markers = Group("02_LayoutPoints", room);
            Transform spawn = Point("PlayerSpawn", markers, V(0, .025f, -2.16f));
            Transform customer = Point("CustomerSpawn", markers, V(.1f, .025f, -4.4f));
            var work = Group("03_Workstations", room);
            stations.Clear();
            Workstations(work, markers);
            Dress(Group("04_ShelvesAndProps", room));
            Lighting(Group("05_Lighting", room));
            PrefabUtility.SaveAsPrefabAssetAndConnect(room.gameObject, Prefabs + "milk_tea_reference_room.prefab", InteractionMode.AutomatedAction);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MilkTeaShop/milk_tea_shop_player.prefab"));
            player.name = "MilkTeaReference_FirstPersonPlayer";
            player.transform.SetPositionAndRotation(spawn.position, Quaternion.identity);
            var controller = player.GetComponent<ShopFirstPersonController>();
            controller.Eye.localPosition = V(0, 1.6f, 0);
            controller.Eye.localRotation = Quaternion.Euler(12, 0, 0);
            var camera = player.GetComponentInChildren<Camera>();
            camera.fieldOfView = 64;
            camera.nearClipPlane = .04f;
            camera.farClipPlane = 35;
            camera.allowHDR = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.34f, .39f, .37f);
            var cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cameraData.antialiasingQuality = AntialiasingQuality.High;
            PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(controller.Eye);
            PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
            PrefabUtility.RecordPrefabInstancePropertyModifications(cameraData);

            var bindings = Group("MilkTeaReference_SystemBindings", null).gameObject.AddComponent<ShopSceneBindings>();
            var serialized = new SerializedObject(bindings);
            serialized.FindProperty("player").objectReferenceValue = controller;
            serialized.FindProperty("playerSpawn").objectReferenceValue = spawn;
            serialized.FindProperty("customerSpawn").objectReferenceValue = customer;
            var array = serialized.FindProperty("stations");
            array.arraySize = stations.Count;
            for (int i = 0; i < stations.Count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = stations[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Reference scene could not be saved.");
            Selection.activeGameObject = room.gameObject;
            SceneView.lastActiveSceneView?.LookAt(V(0, 1.3f, -.4f), Quaternion.Euler(23, 0, 0), 6);
            Debug.Log("MILK_TEA_REFERENCE_CREATED: 3x7m, 1.6m aisle, " + stations.Count + " station bindings.");
        }

        [MenuItem("Untitled/Milk Tea Shop/Open Reference Interior")]
        public static void Open()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
        }

        private static void ImportAssets()
        {
            palette.Clear();
            props.Clear();
            var source = JsonUtility.FromJson<SourceInfo>(File.ReadAllText(Models + "source_manifest.json"));
            foreach (var info in source.materials)
            {
                string materialPath = Materials + info.name.ToLowerInvariant() + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = info.name };
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                material.SetColor("_BaseColor", new Color(info.color[0], info.color[1], info.color[2], 1).gamma);
                material.SetFloat("_Metallic", info.metallic);
                material.SetFloat("_Smoothness", 1 - info.roughness);
                if (info.name == "BeigePlaster") material.SetColor("_BaseColor", new Color(.63f, .57f, .47f));
                if (info.name == "CeilingCream") material.SetColor("_BaseColor", new Color(.68f, .62f, .52f));
                if (info.name == "AmberGlow" || info.name == "LampGlow")
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", new Color(1, .63f, .22f) * 4);
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                }
                if (info.name == "CupClear")
                {
                    material.SetColor("_BaseColor", new Color(.79f, .86f, .83f, .14f));
                    material.SetFloat("_Surface", 1);
                    material.SetFloat("_Blend", 0);
                    material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.SetFloat("_Cull", (float)CullMode.Off);
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    material.renderQueue = (int)RenderQueue.Transparent;
                }
                EditorUtility.SetDirty(material);
                palette[info.name] = material;
            }
            var windowTint = AssetDatabase.LoadAssetAtPath<Material>(Materials + "window_tint.mat");
            if (windowTint == null)
            {
                windowTint = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "window_tint" };
                AssetDatabase.CreateAsset(windowTint, Materials + "window_tint.mat");
            }
            windowTint.SetColor("_BaseColor", new Color(.13f, .25f, .21f));
            windowTint.SetFloat("_Smoothness", .44f);
            palette["WindowTint"] = windowTint;
            foreach (var info in source.assets)
            {
                string path = Models + info.name + ".fbx";
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                if (importer == null) throw new InvalidOperationException("Missing exported FBX: " + path);
                importer.globalScale = 1;
                importer.useFileScale = true;
                importer.bakeAxisConversion = true;
                importer.importNormals = ModelImporterNormals.Import;
                importer.importAnimation = false;
                importer.importLights = false;
                importer.importCameras = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.SaveAndReimport();
                var root = new GameObject(info.name);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), root.transform);
                model.name = "Visual";
                foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>())
                {
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(m =>
                    {
                        if (info.name == "wood_window" && m != null && m.name == "Glass") return palette["WindowTint"];
                        if (m != null && palette.TryGetValue(m.name, out var mapped)) return mapped;
                        throw new InvalidOperationException("Unmapped material on " + info.name + ": " + (m == null ? "null" : m.name));
                    }).ToArray();
                }
                Bounds bounds = BoundsOf(root);
                Vector3 expected = V(info.dimensions[0], info.dimensions[2], info.dimensions[1]);
                if (Vector3.Distance(expected, bounds.size) > .035f)
                    throw new InvalidOperationException("FBX meter conversion mismatch: " + info.name + " " + bounds.size + " expected " + expected);
                props[info.name] = PrefabUtility.SaveAsPrefabAsset(root, Prefabs + info.name + ".prefab");
                Object.DestroyImmediate(root);
            }
            props["pos_terminal"] = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BussinessShop/pos_terminal.prefab");
            props["menu_blackboard"] = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BussinessShop/menu_blackboard.prefab");
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
            point.position = position;
            return point;
        }

        private static Bounds BoundsOf(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static GameObject Prop(string name, Transform parent, Vector3 position, float yaw = 0, bool collision = false, Vector3? scale = null)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(props[name], parent);
            go.transform.localScale = scale ?? Vector3.one;
            go.transform.SetPositionAndRotation(position, Quaternion.identity);
            if (collision)
            {
                Bounds bounds = BoundsOf(go);
                var box = go.AddComponent<BoxCollider>();
                box.center = go.transform.InverseTransformPoint(bounds.center);
                box.size = new Vector3(bounds.size.x / go.transform.lossyScale.x, bounds.size.y / go.transform.lossyScale.y, bounds.size.z / go.transform.lossyScale.z);
            }
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            return go;
        }

        private static void FitMin(GameObject go, Vector3 minimum)
        {
            go.transform.position += minimum - BoundsOf(go).min;
        }

        private static GameObject Box(string name, Transform parent, Vector3 center, Vector3 size, string mat, bool collision = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = palette[mat];
            if (!collision) Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            return go;
        }

        private static void Solid(string name, Transform parent, Vector3 center, Vector3 size)
        {
            var go = Group(name, parent).gameObject;
            go.transform.position = center;
            go.AddComponent<BoxCollider>().size = size;
        }

        private static void Architecture(Transform parent)
        {
            for (int x = 0; x < 3; x++)
                for (int z = 0; z < 7; z++)
                {
                    var tile = Prop("floor_tile", parent, Vector3.zero, 0, true);
                    tile.name = "Floor_" + x + "_" + z;
                    FitMin(tile, V(-1.5f + x, -.08f, -4 + z));
                }
            foreach (float side in new[] { -1f, 1f })
                for (int segment = 0; segment < 4; segment++)
                {
                    float length = segment == 0 ? .4f : segment == 3 ? 1 : 2;
                    var wall = Prop("wall_module", parent, Vector3.zero, side < 0 ? -90 : 90, true, V(length / 2, 1, 1));
                    wall.name = (side < 0 ? "LeftWall_" : "RightWall_") + segment;
                    FitMin(wall, V(side < 0 ? -1.7545f : 1.5f, 0, segment == 0 ? -2.4f : -4 + segment * 2));
                }
            for (int x = 0; x < 2; x++)
                for (int z = 0; z < 4; z++)
                {
                    var ceiling = Prop("ceiling_module", parent, Vector3.zero, 0, true, V(.75f, 1, z == 0 ? .3f : z == 3 ? .5f : 1));
                    ceiling.name = "Ceiling_" + x + "_" + z;
                    FitMin(ceiling, V(-1.5f + x * 1.5f, 3.2f, z == 0 ? -2.6f : -4 + z * 2));
                }
            for (int x = 0; x < 5; x++)
                for (int z = 0; z < 2; z++)
                {
                    var tile = Prop("floor_tile", parent, Vector3.zero, 0, true);
                    tile.name = "CustomerFloor_" + x + "_" + z;
                    FitMin(tile, V(-2.5f + x, -.08f, -6 + z));
                }
            foreach (float x in new[] { -2.5f, 1.5f })
                FitMin(Prop("floor_tile", parent, Vector3.zero, 0, true), V(x, -.08f, -4));
            Box("CourtyardGround", parent, V(0, -.16f, -8), V(16, .12f, 12), "Grout", false);
            var bounds = Group("CustomerAreaBounds", parent);
            Solid("ForecourtEnd", bounds, V(0, 1, -6.05f), V(5.2f, 2, .1f));
            foreach (float side in new[] { -1f, 1f })
            {
                Solid("ForecourtSide", bounds, V(side * 2.55f, 1, -4.5f), V(.1f, 2, 3));
                Solid("ForecourtShoulder", bounds, V(side * 2, 1, -2.95f), V(1, 2, .1f));
                Solid("OpeningSide", bounds, V(side * 1.55f, 1, -2.7f), V(.1f, 2, .6f));
                Box("FrontTimberPost", parent, V(side * 1.48f, 1.6f, -2.5f), V(.12f, 3.2f, .12f), "Walnut");
            }
            Box("RearWallLeft", parent, V(-1.19f, 1.6f, 3.10f), V(.83f, 3.2f, .20f), "BeigePlaster");
            Box("RearWallRight", parent, V(1.19f, 1.6f, 3.10f), V(.83f, 3.2f, .20f), "BeigePlaster");
            Box("RearDoorHeader", parent, V(0, 2.83f, 3.10f), V(1.55f, .74f, .20f), "BeigePlaster");
            Prop("arch_wall_infill", parent, V(0, 0, 2.96f));
            foreach (float side in new[] { -1f, 1f })
            {
                Box("TimberCrownMoulding", parent, V(side * 1.49f, 3.145f, .2f), V(.14f, .12f, 5.6f), "Walnut", false);
            }
            foreach (float z in new[] { -2.1f, .2f, 2.5f })
            {
                var beam = Prop("wooden_beam", parent, Vector3.zero, 0, false, V(1.5f, 1, 1));
                FitMin(beam, V(-1.5f, 2.97f, z - .12f));
            }
            foreach (float x in new[] { -.92f, .92f })
                for (int i = 0; i < 4; i++)
                {
                    var beam = Prop("wooden_beam", parent, Vector3.zero, -90, false, V(i == 0 ? .3f : i == 3 ? .5f : 1, 1, 1));
                    FitMin(beam, V(x - .12f, 3.02f, i == 0 ? -2.6f : -4 + i * 2));
                }
            FitMin(Prop("wooden_beam", parent, Vector3.zero, 0, false, V(1.5f, 1, 1)), V(-1.5f, 2.97f, -2.67f));
        }

        private static Transform Station(string id, ShopStationKind kind, string prompt, Vector3 approach, Transform parent, Transform points)
        {
            var station = Group(id, parent).gameObject.AddComponent<ShopStation>();
            var data = new SerializedObject(station);
            data.FindProperty("stationId").stringValue = id;
            data.FindProperty("kind").enumValueIndex = (int)kind;
            data.FindProperty("prompt").stringValue = prompt;
            data.FindProperty("feedback").stringValue = kind == ShopStationKind.DungeonEntrance
                ? "洞口与阶梯已搭建；洞穴场景切换尚未接入。" : prompt + "，工作站已响应。";
            data.FindProperty("approachPoint").objectReferenceValue = Point(id + "_Approach", points, approach);
            data.ApplyModifiedPropertiesWithoutUndo();
            stations.Add(station);
            return station.transform;
        }

        private static void Workstations(Transform parent, Transform points)
        {
            var preparation = Station("PreparationStation", ShopStationKind.Preparation, "小料台 · 查看备料", V(-.28f, .025f, -.9f), parent, points);
            Prop("inset_topping_counter", preparation, V(-1.15f, 0, -1f), -90, true);
            var prepBack = Prop("galley_counter", preparation, V(-1.15f, 0, .8175f), -90, true);
            prepBack.name = "TeaPreparationCounter";
            Prop("tea_dispenser", preparation, V(-1.25f, .9f, .63f), -90, true);
            Prop("tea_dispenser_green", preparation, V(-1.25f, .9f, 1.10f), -90, true);
            Prop("syrup_pumps", preparation, V(-1.35f, .9f, .15f), -90);
            Prop("blender", preparation, V(-1.22f, .90f, 1.52f), -90);
            Box("PreparationMat", preparation, V(-.98f, .914f, .15f), V(.25f, .025f, .40f), "CabinetInset", false);

            var water = Station("WaterStation", ShopStationKind.Water, "清洗区 · 检查水槽", V(-.25f, .025f, 2.18f), parent, points);
            Prop("sink_counter", water, V(-1.15f, 0, 2.34f), -90, true, V(.82f, 1, 1));
            var order = Station("OrderBoard", ShopStationKind.OrderBoard, "饮品菜单 · 查看订单入口", V(.1f, .025f, -3.8f), parent, points);
            Prop("menu_triptych", order, V(.24f, 1.98f, -2.72f), 0, true, Vector3.one * .83f);

            var sealing = Station("SealingStation", ShopStationKind.Sealing, "封杯机 · 检查设备", V(.25f, .025f, -.15f), parent, points);
            Prop("galley_counter", sealing, V(1.15f, 0, -.95f), 90, true);
            Prop("galley_counter", sealing, V(1.15f, 0, .845f), 90, true);
            Prop("cup_sealing_machine", sealing, V(1.15f, .9f, -.10f), 90, true);
            Prop("clear_cup_stack", sealing, V(.99f, .9f, -.67f));
            Prop("clear_cup_stack", sealing, V(1.28f, .9f, -.67f));
            Prop("straw_holder", sealing, V(1.15f, .9f, -.95f));

            var delivery = Station("DeliveryPoint", ShopStationKind.Delivery, "取餐区 · 交付成品", V(-.28f, .025f, -2.2f), parent, points);
            Box("WoodenDeliveryTray", delivery, V(-.28f, 1.076f, -2.98f), V(.59f, .04f, .38f), "Oak", false);
            Prop("finished_boba", delivery, V(-.28f, 1.098f, -2.98f), 0, true);
            Prop("takeout_bags", sealing, V(1.25f, .9f, -1.45f), 90, true);

            var storage = Station("StoragePoint", ShopStationKind.Storage, "冰箱 · 查看原料仓储", V(.14f, .025f, 2.16f), parent, points);
            Prop("tall_refrigerator", storage, V(1.14f, 0, 2.62f), 0, true);
            var dungeon = Station("DungeonEntrance", ShopStationKind.DungeonEntrance, "地下入口 · 查看洞口", V(0, .025f, 2.35f), parent, points);
            Prop("cave_entrance", dungeon, V(0, 0, 2.96f));
            Solid("EntranceStop", dungeon, V(0, 1.1f, 3.18f), V(.91f, 2.2f, .07f));
            Solid("StoneJambLeft", dungeon, V(-.57f, .67f, 3.04f), V(.24f, 1.34f, .38f));
            Solid("StoneJambRight", dungeon, V(.57f, .67f, 3.04f), V(.24f, 1.34f, .38f));
            Prop("cave_lantern", dungeon, V(-.49f, 1.10f, 3.66f), -55);

            var counter = Station("OrderCounter", ShopStationKind.OrderCounter, "收银区 · 查看接单位置", V(.45f, .025f, -2.2f), parent, points);
            Prop("serving_counter", counter, V(.42f, 0, -2.95f), 0, true, V(.8f, 1, 1));
            Prop("pos_terminal", counter, V(.46f, 1.055f, -2.84f), 180, true);
            Point("StaffPassage", points, V(-1.07f, .025f, -2.95f));
            Point("CustomerQueue", points, V(.1f, .025f, -4.4f));
            Point("CustomerPickup", points, V(-.28f, .025f, -3.55f));
        }

        private static void Dress(Transform parent)
        {
            Prop("wall_storage_shelf", parent, V(-1.44f, 1.25f, -1.13f), -90);
            Prop("ingredient_jars", parent, V(-1.32f, 1.3325f, -1.2f), -90);
            Prop("ingredient_jars", parent, V(-1.32f, 1.7525f, -1.54f), -90);
            Prop("takeout_bags", parent, V(-1.32f, 1.7525f, -.93f), -90);
            Prop("potted_plant", parent, V(-1.30f, 1.7525f, -.53f), 0, false, Vector3.one * .4f);
            Prop("wall_vines", parent, V(-1.445f, 1.3212f, -.53f), -90, false, Vector3.one * .58f);
            Prop("wall_storage_shelf", parent, V(1.44f, 1.25f, 1.0f), 90);
            Prop("ingredient_jars", parent, V(1.32f, 1.3325f, .70f), 90);
            Prop("takeout_bags", parent, V(1.32f, 1.7525f, .57f), 90);
            Prop("cardboard_box", parent, V(1.30f, 1.3325f, 1.36f), 15, false, Vector3.one * .36f);
            Prop("potted_plant", parent, V(1.29f, 1.7525f, 1.49f), 0, false, Vector3.one * .43f);
            Prop("wall_vines", parent, V(1.445f, 1.2663f, 1.49f), 90, false, Vector3.one * .64f);
            Prop("mascot_figurine", parent, V(1.32f, 1.7525f, 1.08f), 90, false, Vector3.one * .45f);
            Prop("menu_blackboard", parent, V(1.445f, 1.34f, -1.23f), 90);
            Prop("tissue_dispenser", parent, V(1.02f, 1.055f, -2.98f), 0, false, Vector3.one * .6f);
            Prop("potted_plant", parent, V(-2.15f, 0, -4.5f), 0, true, Vector3.one * .8f);
            Prop("potted_plant", parent, V(2.15f, 0, -4.5f), 15, true, Vector3.one * .8f);
            Prop("straw_holder", parent, V(1.27f, 1.055f, -3f));
            Prop("wooden_crate", parent, V(1.16f, 0, 1.95f), 0, false, Vector3.one * .6f);
        }

        private static void Lighting(Transform parent)
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.20f, .18f, .145f);
            RenderSettings.fog = false;
            var fill = Group("SoftWindowBounce", parent).gameObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.transform.rotation = Quaternion.Euler(48, -32, 0);
            fill.color = new Color(1, .91f, .77f);
            fill.intensity = .34f;
            fill.shadows = LightShadows.None;
            RenderSettings.sun = fill;
            Vector3[] lamps = { V(-.68f, 3.12f, -1.34f), V(.68f, 3.12f, -.06f), V(-.62f, 3.12f, 1.39f), V(.59f, 3.12f, 2.35f) };
            for (int i = 0; i < lamps.Length; i++)
            {
                Prop(i % 2 == 0 ? "pendant_lamp" : "pendant_lamp_small", parent, lamps[i]);
                var point = Group("PendantWarmLight_" + i, parent).gameObject.AddComponent<Light>();
                point.type = LightType.Point;
                point.transform.position = lamps[i] - V(0, i % 2 == 0 ? 1.05f : .87f, 0);
                point.color = new Color(1, .79f, .49f);
                point.intensity = i == 0 ? 2.2f : 1.8f;
                point.range = 3.6f;
                point.shadows = i < 2 ? LightShadows.Soft : LightShadows.None;
                point.shadowBias = .025f;
                point.shadowNormalBias = .06f;
            }
            var lantern = Group("CaveLanternLight", parent).gameObject.AddComponent<Light>();
            lantern.type = LightType.Point;
            lantern.transform.position = V(-.34f, 1.26f, 3.46f);
            lantern.color = new Color(1, .53f, .12f);
            lantern.intensity = 2;
            lantern.range = 2.4f;
            lantern.shadows = LightShadows.Soft;
            lantern.shadowBias = .02f;
            lantern.shadowNormalBias = .05f;
            var caveFill = Group("CaveStairBounce", parent).gameObject.AddComponent<Light>();
            caveFill.type = LightType.Point;
            caveFill.transform.position = V(0, .80f, 3.95f);
            caveFill.color = new Color(1, .64f, .27f);
            caveFill.intensity = 1.1f;
            caveFill.range = 2.1f;
            var front = Group("ServingCounterBounce", parent).gameObject.AddComponent<Light>();
            front.type = LightType.Point;
            front.transform.position = V(0, 2.4f, -2.8f);
            front.color = new Color(1, .81f, .58f);
            front.intensity = 1.5f;
            front.range = 2.6f;
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            bool newProfile = profile == null;
            if (newProfile)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            if (!profile.TryGet<Tonemapping>(out var tonemapping)) tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);
            if (!profile.TryGet<Bloom>(out var bloom)) bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1.15f);
            bloom.intensity.Override(.18f);
            bloom.scatter.Override(.6f);
            if (!profile.TryGet<ColorAdjustments>(out var color)) color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(-.35f);
            color.contrast.Override(9);
            color.saturation.Override(-5);
            if (!profile.TryGet<Vignette>(out var vignette)) vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(.16f);
            vignette.smoothness.Override(.5f);
            if (newProfile)
                foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            EditorUtility.SetDirty(profile);
            var volume = Group("WarmInteriorPresentation", parent).gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }
    }
}
