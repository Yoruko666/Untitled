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
    public static class MilkTeaWorkstationLayout
    {
        public const string ScenePath = "Assets/Scenes/BussinessWorkstation.unity";
        private const string Props = "Assets/Prefabs/BussinessShop/";
        private const string Materials = "Assets/Art/Materials/BussinessShop/";
        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        [MenuItem("Untitled/Milk Tea Shop/Create Player Centered Workstation")]
        public static void Create()
        {
            if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().path != "Assets/Scenes/Bussiness.unity")
                throw new InvalidOperationException("Open Bussiness in Edit Mode first.");
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Workstation scene already exists; edit its saved layout.");
            Directory.CreateDirectory("Logs/MilkTeaWorkstation");
            var source = EditorSceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(source, "Logs/MilkTeaWorkstation/before_redesign.unity", true);
            EditorSceneManager.SaveScene(source, ScenePath, true);
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var bindings = Object.FindObjectOfType<ShopSceneBindings>();
            var player = bindings.Player;
            var oldRoom = scene.GetRootGameObjects().Single(g => g.name == "BussinessShop_Competition_5.8x4.4");
            Object.DestroyImmediate(oldRoom);
            CreateGreenMaterial();
            var room = Group("MilkTeaWorkstation_U", null);
            Architecture(Group("Architecture", room));
            var tables = Group("U_Worktops", room);
            Prop("work_counter", tables, V(0, 0, 1.22f), 0, true);
            Prop("work_counter", tables, V(-1.2275f, 0, -0.006f), -90, true);
            Prop("work_counter", tables, V(1.2275f, 0, -0.006f), 90, true);
            var points = Group("LayoutPoints", room);
            var spawn = Point("PlayerSpawn", points, V(0, 0.025f, -0.05f));
            var customer = Point("CustomerSpawn", points, V(0, 0.025f, -0.75f));
            var stationRoot = Group("Stations", room);
            var stations = new List<ShopStation>();

            var preparation = Station("PreparationStation", ShopStationKind.Preparation, "制作区 · 调配奶茶",
                "查看茶底、小料与调配台。", spawn.position, stationRoot, points, stations);
            Prop("tea_dispenser_01", preparation, V(-0.60f, 0.9f, 1.28f), 0, true);
            Prop("tea_dispenser_02", preparation, V(0.63f, 0.9f, 1.28f), 0, true);
            Prop("ingredient_bins", preparation, V(0.05f, 0.9f, 1.28f), 0, true);
            Box("PreparationMat", preparation, V(0, 0.916f, 0.99f), V(0.72f, 0.032f, 0.13f), "cream");
            Box("IngredientShelfSupport", preparation, V(0, 1.565f, 1.47f), V(.83f, .07f, .33f), "walnut", false);
            Prop("display_shelf", preparation, V(0, 1.6f, 1.47f));

            var order = Station("OrderBoard", ShopStationKind.OrderBoard, "订单板 · 查看订单",
                "查看待办订单。", spawn.position, stationRoot, points, stations);
            Prop("notice_board", order, V(-1.86f, 1.12f, 0.55f), -90, true);
            var storage = Station("StoragePoint", ShopStationKind.Storage, "仓储柜 · 查看原料",
                "查看库存与补货入口。", spawn.position, stationRoot, points, stations);
            Prop("storage_cabinet", storage, V(-1.75f, 0, -0.50f), -90, true);
            Prop("display_shelf", storage, V(-1.75f, 1.125f, -0.50f), -90, true);
            Prop("paper_cups", tables, V(-1.23f, 0.9f, 0.43f), -90);

            var sealing = Station("SealingStation", ShopStationKind.Sealing, "封杯区 · 封装奶茶",
                "检查封杯设备与包装材料。", spawn.position, stationRoot, points, stations);
            Prop("cup_sealing_machine", sealing, V(1.20f, 0.9f, 0.25f), 90, true);
            Prop("paper_cups", sealing, V(1.22f, 0.9f, 0.68f), 90);
            Prop("takeout_bags", sealing, V(1.38f, 0.9f, -0.20f), 90);
            var delivery = Station("DeliveryPoint", ShopStationKind.Delivery, "出杯区 · 交付奶茶",
                "检查出杯托盘。", spawn.position, stationRoot, points, stations);
            Box("DeliveryTray", delivery, V(1.03f, 0.922f, -0.55f), V(0.24f, 0.044f, 0.57f), "workstation_deep_green");
            Prop("finished_boba", delivery, V(1.03f, 0.944f, -0.55f), 90, true);

            var dungeon = Station("DungeonEntrance", ShopStationKind.DungeonEntrance, "地下入口 · 前往洞穴",
                "地下入口已预留，等待接入洞穴切换。", V(0, 0.025f, -0.75f), stationRoot, points, stations);
            var portal = Prop("basement_portal", dungeon, V(0, -0.28f, -1.695f), 180).transform;
            var collisions = Group("EntranceCollisions", portal);
            Solid("LeftJamb", collisions, V(-0.79f, 0.93f, 0.025f), V(0.27f, 1.3f, 0.4f));
            Solid("RightJamb", collisions, V(0.79f, 0.93f, 0.025f), V(0.27f, 1.3f, 0.4f));
            Solid("ArchHeader", collisions, V(0, 2.305f, 0.025f), V(1.85f, 0.15f, 0.42f));
            Solid("PassageBlocker", collisions, V(0, 1.11f, 0.57f), V(1.28f, 2.2f, 0.06f));
            for (int i = 0; i < 4; i++)
                Solid("DescendingStep_" + i, collisions, V(0, 0.26f - i * 0.07f, 0.12f + i * 0.14f), V(1.22f, 0.1f, 0.145f));
            Lighting(Group("Lighting", room));
            AddReferenceDetails(room);

            player.transform.SetPositionAndRotation(spawn.position, Quaternion.identity);
            player.Eye.localPosition = V(0, 1.6f, 0);
            player.Eye.localRotation = Quaternion.Euler(12, 0, 0);
            PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(player.Eye);
            var data = new SerializedObject(bindings);
            data.FindProperty("playerSpawn").objectReferenceValue = spawn;
            data.FindProperty("customerSpawn").objectReferenceValue = customer;
            var array = data.FindProperty("stations");
            array.arraySize = stations.Count;
            for (int i = 0; i < stations.Count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = stations[i];
            data.ApplyModifiedPropertiesWithoutUndo();
            Physics.SyncTransforms();
            var overlaps = Physics.OverlapCapsule(spawn.position + V(0, .32f, 0), spawn.position + V(0, 1.5f, 0), .28f);
            if (overlaps.Any(c => !(c is CharacterController))) throw new InvalidOperationException("Blocked operation center.");
            PrefabUtility.SaveAsPrefabAssetAndConnect(room.gameObject, Props + "milk_tea_workstation_room.prefab", InteractionMode.AutomatedAction);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = room.gameObject;
            Debug.Log("WORKSTATION_CREATED: player centered U, 3.65x3.55m, six station references, saved.");
        }

        [MenuItem("Untitled/Milk Tea Shop/Dress Workstation From Reference")]
        public static void DressSavedWorkstation()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != ScenePath)
                throw new InvalidOperationException("Open workstation in Edit Mode.");
            var room = scene.GetRootGameObjects().Single(g => g.name == "MilkTeaWorkstation_U").transform;
            AddReferenceDetails(room);
            PrefabUtility.ApplyPrefabInstance(room.gameObject, InteractionMode.AutomatedAction);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void AddReferenceDetails(Transform room)
        {
            if (room.Find("ReferenceDetails") != null) return;
            var decor = Group("ReferenceDetails", room);
            room.Find("U_Worktops/paper_cups").position = V(-1.20f, .9f, -.10f);
            var preparation = room.GetComponentsInChildren<ShopStation>().Single(s => s.StationId == "PreparationStation").transform;
            Prop("syrup_pumps", preparation, V(-1.02f, .9f, .35f), -90);
            Prop("straw_holder", decor, V(-1.03f, .9f, -.56f));
            var plant = Prop("potted_plant", decor, V(-1.34f, .9f, .68f));
            plant.transform.localScale = Vector3.one * .45f;
            Box("RightCupShelfSupport", decor, V(1.465f, 1.565f, -.40f), V(.33f, .07f, .84f), "walnut", false);
            Prop("cup_shelf", decor, V(1.45f, 1.6f, -.40f), 90);
            Prop("wall_vines", decor, V(1.58f, 1.7f, -1.14f), 90);
            var pendant = Prop("pendant_lamp_b", decor, V(.95f, 2.8f, -.15f));
            pendant.name = "SidePendant";
            var light = Group("RightWarmTaskFill", decor).gameObject.AddComponent<Light>();
            light.transform.position = V(.85f, 2.2f, -.05f);
            light.type = LightType.Point;
            light.color = new Color(1, .85f, .64f);
            light.intensity = .75f;
            light.range = 2;
            light.shadows = LightShadows.None;
        }

        private static void CreateGreenMaterial()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name = "workstation_deep_green";
            material.SetColor("_BaseColor", new Color(.13f, .23f, .17f));
            material.SetFloat("_Smoothness", .18f);
            AssetDatabase.CreateAsset(material, Materials + "workstation_deep_green.mat");
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

        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, string material, bool collision = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Materials + material + ".mat");
            if (!collision) Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            return go;
        }

        private static void Solid(string name, Transform parent, Vector3 position, Vector3 size)
        {
            var go = Group(name, parent);
            go.localPosition = position;
            go.gameObject.AddComponent<BoxCollider>().size = size;
        }

        private static GameObject Prop(string asset, Transform parent, Vector3 position, float yaw = 0, bool collision = false)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Props + asset + ".prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.position = position;
            if (collision)
            {
                var renderers = go.GetComponentsInChildren<Renderer>();
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                var collider = go.AddComponent<BoxCollider>();
                collider.center = bounds.center - position;
                collider.size = bounds.size;
            }
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            return go;
        }

        private static Transform Station(string id, ShopStationKind kind, string prompt, string feedback, Vector3 approach,
            Transform parent, Transform points, List<ShopStation> stations)
        {
            var root = Group("Station_" + id, parent);
            var station = root.gameObject.AddComponent<ShopStation>();
            var data = new SerializedObject(station);
            data.FindProperty("stationId").stringValue = id;
            data.FindProperty("kind").enumValueIndex = (int)kind;
            data.FindProperty("prompt").stringValue = prompt;
            data.FindProperty("feedback").stringValue = feedback;
            data.FindProperty("approachPoint").objectReferenceValue = Point(id, points, approach);
            data.ApplyModifiedPropertiesWithoutUndo();
            stations.Add(station);
            return root;
        }

        private static void Architecture(Transform parent)
        {
            Box("Floor", parent, V(-.175f, -.1f, -.115f), V(3.65f, .2f, 3.53f), "grout");
            Box("BackFloor_Left", parent, V(-1.32f, -.1f, -1.89f), V(1.36f, .2f, .02f), "grout");
            Box("BackFloor_Right", parent, V(1.145f, -.1f, -1.89f), V(1.01f, .2f, .02f), "grout");
            for (int x = 0; x < 5; x++)
                for (int z = 0; z < 5; z++)
                    Box($"FloorTile_{x}_{z}", parent, V(-1.635f + x * .73f, .002f, -1.545f + z * .71f + (z == 0 ? .00825f : 0)),
                        V(.723f, .004f, z == 0 ? .6865f : .703f), (x + z) % 3 == 0 ? "floorshade" : "floorlight", false);
            Box("WallLeft", parent, V(-2.12f, 1.425f, -.125f), V(.24f, 2.85f, 4.03f), "wallcream");
            Box("WallRight", parent, V(1.77f, 1.425f, -.125f), V(.24f, 2.85f, 4.03f), "wallcream");
            Box("WallFront", parent, V(-.175f, 1.425f, 1.77f), V(3.65f, 2.85f, .24f), "wallcream");
            Box("WallBack_Left", parent, V(-1.44f, 1.425f, -2.02f), V(1.6f, 2.85f, .24f), "wallcream");
            Box("WallBack_Right", parent, V(1.265f, 1.425f, -2.02f), V(1.25f, 2.85f, .24f), "wallcream");
            Box("WallBack_Header", parent, V(0, 2.39f, -2.02f), V(1.28f, .92f, .24f), "wallcream");
            Box("Ceiling", parent, V(-.175f, 2.95f, -.125f), V(4.13f, .2f, 4.03f), "wallcream");
            Box("GreenFront", parent, V(-.175f, .85f, 1.625f), V(3.65f, 1.7f, .035f), "workstation_deep_green", false);
            Box("GreenLeft", parent, V(-1.975f, .85f, -.125f), V(.035f, 1.7f, 3.55f), "workstation_deep_green", false);
            Box("GreenRight", parent, V(1.625f, .85f, -.125f), V(.035f, 1.7f, 3.55f), "workstation_deep_green", false);
            foreach (float x in new[] { -1.5f, 1.15f })
                Box("TimberBeam", parent, V(x, 2.77f, -.125f), V(.13f, .16f, 3.6f), "walnut", false);
            Box("RecessLeft", parent, V(-.70f, .825f, -2.16f), V(.12f, 2.21f, .56f), "wallcream");
            Box("RecessRight", parent, V(.70f, .825f, -2.16f), V(.12f, 2.21f, .56f), "wallcream");
            Box("RecessCap", parent, V(0, 1.97f, -2.16f), V(1.4f, .08f, .56f), "wallcream");
            Box("RecessEnd", parent, V(0, .825f, -2.44f), V(1.4f, 2.21f, .12f), "wallcream");
            Box("RecessFloor", parent, V(0, -.33f, -2.16f), V(1.28f, .1f, .56f), "grout");
        }

        private static void Lighting(Transform parent)
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.30f, .29f, .25f);
            RenderSettings.fog = false;
            var bounce = Group("RoomFill", parent).gameObject.AddComponent<Light>();
            bounce.type = LightType.Directional;
            bounce.transform.rotation = Quaternion.Euler(55, -25, 0);
            bounce.color = new Color(1, .93f, .83f);
            bounce.intensity = .32f;
            RenderSettings.sun = bounce;
            Prop("pendant_lamp_b", parent, V(-.60f, 2.8f, 1.22f));
            var key = Group("PreparationKey", parent).gameObject.AddComponent<Light>();
            key.transform.position = V(-.10f, 2.30f, .60f);
            key.type = LightType.Point;
            key.color = new Color(1, .87f, .70f);
            key.intensity = 2.3f;
            key.range = 2.7f;
            key.shadows = LightShadows.Soft;
            var fill = Group("SideWorktopFill", parent).gameObject.AddComponent<Light>();
            fill.transform.position = V(0, 2.45f, -.1f);
            fill.type = LightType.Point;
            fill.color = new Color(1, .89f, .74f);
            fill.intensity = .65f;
            fill.range = 2.8f;
            var volume = Group("ShopPresentation", parent).gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/BussinessShopPresentation.asset");
        }
    }
}
