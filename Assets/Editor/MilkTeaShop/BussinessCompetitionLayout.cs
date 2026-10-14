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
    // Explicit, one-time conversion. Saved layout remains editable and is never rebuilt at runtime.
    public static class BussinessCompetitionLayout
    {
        private const string ScenePath = "Assets/Scenes/Bussiness.unity";
        private const string Prefabs = "Assets/Prefabs/BussinessShop/";
        private const string Materials = "Assets/Art/Materials/BussinessShop/";
        private const string RoomPath = Prefabs + "bussiness_shop_competition_room.prefab";
        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        [MenuItem("Untitled/Milk Tea Shop/Compact Bussiness For Competition")]
        public static void Apply()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != ScenePath || scene.isDirty)
                throw new InvalidOperationException("Open saved Bussiness.unity in Edit Mode first.");
            var oldRoom = scene.GetRootGameObjects().SingleOrDefault(g => g.name == "BussinessShop_8x9");
            var bindings = Object.FindObjectOfType<ShopSceneBindings>();
            if (oldRoom == null || bindings == null || bindings.Player == null)
                throw new InvalidOperationException("Expected the existing 8x9 shop and its FPS bindings. Do not reapply to a compact layout.");

            Directory.CreateDirectory("Logs/BussinessCompetition");
            EditorSceneManager.SaveScene(scene, "Logs/BussinessCompetition/before_compaction.unity", true);
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Compact Bussiness shop");
            var room = Group("BussinessShop_Competition_5.8x4.4", null);
            Architecture(Group("Architecture", room));
            var worktops = Group("Worktops", room);
            Prop("work_counter", worktops, V(-1.35f, 0, 1.65f), 0, true);
            Prop("work_counter", worktops, V(0.45f, 0, 1.65f), 0, true);
            var points = Group("LayoutPoints", room);
            var spawn = Point("PlayerSpawn", points, V(-0.65f, 0.025f, 0.15f));
            var customer = Point("CustomerSpawn", points, V(1.2f, 0.025f, -1.65f));
            var stationRoot = Group("Stations", room);
            var stations = new List<ShopStation>();

            var preparation = Station("PreparationStation", ShopStationKind.Preparation,
                "制作区 · 调配奶茶", "查看茶底、配料与调配台。", V(-1f, 0.025f, 0.45f), stationRoot, points, stations);
            Prop("tea_dispenser_01", preparation, V(-1.94f, 0.9f, 1.73f), 0, true);
            Prop("tea_dispenser_02", preparation, V(-1.43f, 0.9f, 1.73f), 0, true);
            Prop("ingredient_bins", preparation, V(-0.65f, 0.9f, 1.72f), 0, true);
            // A clear, low-profile surface in front of the ingredients is the actual preparation target.
            Box("PreparationSurface", preparation, V(-1.28f, 0.916f, 1.41f), V(0.68f, 0.032f, 0.16f), "cream");

            var sealing = Station("SealingStation", ShopStationKind.Sealing,
                "封杯区 · 封装奶茶", "检查封杯设备与包装材料。", V(0.45f, 0.025f, 0.45f), stationRoot, points, stations);
            Prop("cup_sealing_machine", sealing, V(0.45f, 0.9f, 1.65f), 0, true);
            Prop("paper_cups", sealing, V(-0.12f, 0.9f, 1.56f));
            Prop("takeout_bags", sealing, V(1.03f, 0.9f, 1.8f));

            var delivery = Station("DeliveryPoint", ShopStationKind.Delivery,
                "出杯区 · 交付奶茶", "检查出杯托盘。", V(1.05f, 0.025f, 0.45f), stationRoot, points, stations);
            Box("DeliveryTray", delivery, V(1.05f, 0.922f, 1.42f), V(0.57f, 0.044f, 0.24f), "wallmatcha");
            Prop("finished_boba", delivery, V(1.05f, 0.944f, 1.42f), 0, true);

            var order = Station("OrderBoard", ShopStationKind.OrderBoard,
                "订单板 · 查看订单", "查看当前待办订单。", V(-0.65f, 0.025f, 0.45f), stationRoot, points, stations);
            Prop("notice_board", order, V(-1.10f, 1.65f, 2.12f), 0, true);

            var storage = Station("StoragePoint", ShopStationKind.Storage,
                "仓储柜 · 查看原料", "查看库存与补货入口。", V(1.65f, 0.025f, -1.15f), stationRoot, points, stations);
            Prop("storage_cabinet", storage, V(2.62f, 0, -1.15f), 90, true);
            Prop("display_shelf", storage, V(2.62f, 1.125f, -1.15f), 90, true);

            var dungeon = Station("DungeonEntrance", ShopStationKind.DungeonEntrance,
                "地下入口 · 前往洞穴", "地下入口已预留，等待接入洞穴切换。", V(1.2f, 0.025f, -0.1f), stationRoot, points, stations);
            Prop("basement_portal", dungeon, V(2.695f, -0.28f, 0.55f), 90);
            Lighting(Group("Lighting", room));
            AlignEntrance(room);
            Undo.RegisterCreatedObjectUndo(room.gameObject, "Create compact environment");
            Undo.DestroyObjectImmediate(oldRoom);

            var player = bindings.Player;
            Undo.RecordObjects(new Object[] { player.transform, player.Eye, bindings }, "Set compact shop references");
            player.transform.SetPositionAndRotation(spawn.position, Quaternion.identity);
            player.Eye.localRotation = Quaternion.Euler(15, 0, 0);
            PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(player.Eye);
            var settings = new SerializedObject(bindings);
            settings.FindProperty("playerSpawn").objectReferenceValue = spawn;
            settings.FindProperty("customerSpawn").objectReferenceValue = customer;
            var array = settings.FindProperty("stations");
            array.arraySize = stations.Count;
            for (int i = 0; i < stations.Count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = stations[i];
            settings.ApplyModifiedProperties();
            Physics.SyncTransforms();
            CheckSpace(bindings);
            PrefabUtility.SaveAsPrefabAssetAndConnect(room.gameObject, RoomPath, InteractionMode.AutomatedAction);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = room.gameObject;
            Debug.Log("BUSSINESS_COMPACT_OK: 5.8x4.4m, six station interfaces, scene saved.");
        }

        [MenuItem("Untitled/Milk Tea Shop/Align Competition Dungeon Entrance")]
        public static void AlignSavedEntrance()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != ScenePath)
                throw new InvalidOperationException("Open compact Bussiness in Edit Mode.");
            var room = scene.GetRootGameObjects().Single(g => g.name == "BussinessShop_Competition_5.8x4.4").transform;
            Undo.RegisterFullObjectHierarchyUndo(room.gameObject, "Align underground threshold");
            AlignEntrance(room);
            Physics.SyncTransforms();
            CheckSpace(Object.FindObjectOfType<ShopSceneBindings>());
            PrefabUtility.ApplyPrefabInstance(room.gameObject, InteractionMode.AutomatedAction);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("COMPETITION_ENTRANCE_ALIGNED: grounded frame, wall opening, recessed steps, saved.");
        }

        private static void AlignEntrance(Transform room)
        {
            var architecture = room.Find("Architecture");
            if (architecture.Find("RightWall_Front") != null)
            {
                SealRecess(architecture);
                return;
            }
            var station = room.GetComponentsInChildren<ShopStation>().Single(s => s.StationId == "DungeonEntrance").transform;
            var portal = station.Find("basement_portal");
            // Export was grounded on the lowest (-0.28m) descending step, not the shop threshold.
            // Restore the timber base to Y=0 and embed its rear edge 0.02m into the side wall.
            portal.position = V(2.695f, -0.28f, 0.55f);
            var oldCollider = portal.GetComponent<BoxCollider>();
            if (oldCollider != null) Object.DestroyImmediate(oldCollider);
            var oldDoor = station.Find("DungeonDoor_Closed");
            if (oldDoor != null) Object.DestroyImmediate(oldDoor.gameObject);
            var shell = Group("EntranceCollisions", portal);
            Solid("LeftJamb", shell, V(-0.79f, 0.93f, 0.025f), V(0.27f, 1.3f, 0.4f));
            Solid("RightJamb", shell, V(0.79f, 0.93f, 0.025f), V(0.27f, 1.3f, 0.4f));
            Solid("ArchHeader", shell, V(0, 2.305f, 0.025f), V(1.85f, 0.15f, 0.42f));
            // Preserve the model's arched darkness, steps and green eyes; only the blocker is invisible.
            Solid("PassageBlocker", shell, V(0, 1.11f, 0.57f), V(1.28f, 2.2f, 0.06f));
            for (int i = 0; i < 4; i++)
                Solid("DescendingStep_" + i, shell, V(0, 0.26f - i * 0.07f, 0.12f + i * 0.14f), V(1.22f, 0.1f, 0.145f));

            foreach (var child in architecture.Cast<Transform>().Where(t => t.name == "SideWall" && t.position.x > 0).ToArray())
                Object.DestroyImmediate(child.gameObject);
            foreach (var child in architecture.Cast<Transform>().Where(t => t.name == "MatchaSidePanel" && t.position.x > 0).ToArray())
                Object.DestroyImmediate(child.gameObject);
            Box("RightWall_Front", architecture, V(3.02f, 1.6f, -1.265f), V(0.24f, 3.2f, 2.35f), "wallcream");
            Box("RightWall_Back", architecture, V(3.02f, 1.6f, 1.815f), V(0.24f, 3.2f, 1.25f), "wallcream");
            Box("RightWall_Header", architecture, V(3.02f, 2.565f, 0.55f), V(0.24f, 1.27f, 1.28f), "wallcream");
            Box("RightPanel_Front", architecture, V(2.875f, 0.38f, -1.145f), V(0.035f, 0.76f, 2.11f), "wallmatcha", false);
            Box("RightPanel_Back", architecture, V(2.875f, 0.38f, 1.695f), V(0.035f, 0.76f, 1.01f), "wallmatcha", false);

            var floor = architecture.Find("Floor_5.8x4.4");
            floor.localPosition = V(-0.01f, -0.1f, 0);
            floor.localScale = V(5.78f, 0.2f, 4.4f);
            Box("FloorEdge_Front", architecture, V(2.89f, -0.1f, -1.145f), V(0.02f, 0.2f, 2.11f), "grout");
            Box("FloorEdge_Back", architecture, V(2.89f, -0.1f, 1.695f), V(0.02f, 0.2f, 1.01f), "grout");
            for (int z = 0; z < 6; z++)
            {
                var tile = architecture.Find("FloorTile_7_" + z);
                tile.localPosition += V(-0.00825f, 0, 0);
                tile.localScale = V(0.7015f, 0.004f, 4.4f / 6 - 0.007f);
            }
            Box("RecessFrontCheek", architecture, V(3.16f, 0.965f, -0.15f), V(0.56f, 1.93f, 0.12f), "wallcream");
            Box("RecessBackCheek", architecture, V(3.16f, 0.965f, 1.25f), V(0.56f, 1.93f, 0.12f), "wallcream");
            Box("RecessCap", architecture, V(3.16f, 1.97f, 0.55f), V(0.56f, 0.08f, 1.4f), "wallcream");
            Box("RecessBack", architecture, V(3.44f, 0.825f, 0.55f), V(0.12f, 2.21f, 1.4f), "wallcream");
            SealRecess(architecture);
        }

        private static void SealRecess(Transform architecture)
        {
            foreach (var name in new[] { "RecessFrontCheek", "RecessBackCheek" })
            {
                var side = architecture.Find(name);
                side.localPosition = V(side.localPosition.x, 0.825f, side.localPosition.z);
                side.localScale = V(0.56f, 2.21f, 0.12f);
            }
            if (architecture.Find("RecessFloor") == null)
                Box("RecessFloor", architecture, V(3.16f, -0.33f, 0.55f), V(0.56f, 0.1f, 1.28f), "grout");
        }

        private static void Solid(string name, Transform parent, Vector3 center, Vector3 size)
        {
            var root = Group(name, parent);
            root.localPosition = center;
            root.gameObject.AddComponent<BoxCollider>().size = size;
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

        private static GameObject Prop(string asset, Transform parent, Vector3 position, float yaw = 0, bool collision = false)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + asset + ".prefab");
            if (prefab == null) throw new InvalidOperationException("Missing prop: " + asset);
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
            var settings = new SerializedObject(station);
            settings.FindProperty("stationId").stringValue = id;
            settings.FindProperty("kind").enumValueIndex = (int)kind;
            settings.FindProperty("prompt").stringValue = prompt;
            settings.FindProperty("feedback").stringValue = feedback;
            settings.FindProperty("approachPoint").objectReferenceValue = Point(id, points, approach);
            settings.ApplyModifiedPropertiesWithoutUndo();
            stations.Add(station);
            return root;
        }

        private static void Architecture(Transform parent)
        {
            Box("Floor_5.8x4.4", parent, V(0, -0.1f, 0), V(5.8f, 0.2f, 4.4f), "grout");
            for (int x = 0; x < 8; x++)
                for (int z = 0; z < 6; z++)
                    Box($"FloorTile_{x}_{z}", parent, V(-2.5375f + x * 0.725f, 0.002f, -1.833333f + z * (4.4f / 6)),
                        V(0.718f, 0.004f, 4.4f / 6 - 0.007f), (x + z) % 3 == 0 ? "floorshade" : "floorlight", false);
            foreach (float x in new[] { -3.02f, 3.02f })
                Box("SideWall", parent, V(x, 1.6f, 0), V(0.24f, 3.2f, 4.88f), "wallcream");
            foreach (float z in new[] { -2.32f, 2.32f })
                Box("EndWall", parent, V(0, 1.6f, z), V(5.8f, 3.2f, 0.24f), "wallcream");
            Box("Ceiling", parent, V(0, 3.3f, 0), V(6.28f, 0.2f, 4.88f), "wallcream");
            foreach (float x in new[] { -2.875f, 2.875f })
                Box("MatchaSidePanel", parent, V(x, 0.38f, 0), V(0.035f, 0.76f, 4.4f), "wallmatcha", false);
            foreach (float z in new[] { -2.175f, 2.175f })
                Box("MatchaEndPanel", parent, V(0, 0.38f, z), V(5.8f, 0.76f, 0.035f), "wallmatcha", false);
            foreach (float z in new[] { -1.4f, 1.35f })
                Box("TimberCrossBeam", parent, V(0, 3.12f, z), V(5.85f, 0.16f, 0.16f), "walnut", false);
            Box("TimberLongBeam", parent, V(0, 3.13f, 0), V(0.12f, 0.12f, 4.45f), "walnut", false);
            Prop("glass_double_door", parent, V(1.2f, 0, -2.13f), 180);
            Prop("wood_window", parent, V(-1.15f, 0.85f, -2.11f), 180);
            Prop("store_sign", parent, V(1.2f, 2.48f, -2.09f), 180);
        }

        private static void Lighting(Transform parent)
        {
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.30f, 0.29f, 0.25f);
            var bounce = Group("WarmInteriorFill", parent).gameObject.AddComponent<Light>();
            bounce.type = LightType.Directional;
            bounce.transform.rotation = Quaternion.Euler(55, -25, 0);
            bounce.color = new Color(1, 0.92f, 0.80f);
            bounce.intensity = 0.35f;
            bounce.shadows = LightShadows.None;
            RenderSettings.sun = bounce;
            for (int i = 0; i < 2; i++)
            {
                var position = V(i == 0 ? -1.25f : 0.7f, 3.15f, 1.2f);
                Prop("pendant_lamp_b", parent, position);
                var light = Group(i == 0 ? "PreparationKey" : "SealingFill", parent).gameObject.AddComponent<Light>();
                light.transform.position = position + V(0, -0.94f, 0);
                light.type = LightType.Point;
                light.color = new Color(1, 0.85f, 0.65f);
                light.intensity = i == 0 ? 2.6f : 1.8f;
                light.range = i == 0 ? 3.2f : 2.7f;
                light.shadows = i == 0 ? LightShadows.Soft : LightShadows.None;
            }
            var volume = Group("ShopPresentation", parent).gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/BussinessShopPresentation.asset");
        }

        private static void CheckSpace(ShopSceneBindings bindings)
        {
            foreach (var point in bindings.Stations.Select(s => s.ApproachPoint).Concat(new[] { bindings.PlayerSpawn, bindings.CustomerSpawn }))
            {
                var hits = Physics.OverlapCapsule(point.position + V(0, 0.32f, 0), point.position + V(0, 1.5f, 0), 0.28f);
                if (hits.Any(h => !(h is CharacterController)))
                    throw new InvalidOperationException("Blocked standing point: " + point.name + " / " + string.Join(",", hits.Select(h => h.name)));
            }
        }
    }
}
