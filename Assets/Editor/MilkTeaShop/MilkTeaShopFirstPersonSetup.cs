using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Untitled.EditorTools
{
    public static class MilkTeaShopFirstPersonSetup
    {
        private const string Prefabs = "Assets/Prefabs/MilkTeaShop/";
        private const string Materials = "Assets/Art/Materials/MilkTeaShop/";
        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        [MenuItem("Untitled/Milk Tea Shop/Convert Existing Shop To First Person")]
        public static void ConvertExisting()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != MilkTeaShopBuilder.ScenePath) throw new InvalidOperationException("Open MilkTeaShop first.");
            if (Object.FindObjectOfType<ShopFirstPersonController>() != null)
                throw new InvalidOperationException("Already converted. Edit the saved scene and prefabs directly.");
            Directory.CreateDirectory("Logs/MilkTeaShop");
            // Save a copy including unsaved user layout changes before changing the shop.
            EditorSceneManager.SaveScene(scene, "Logs/MilkTeaShop/before_first_person.unity", true);
            EditorSceneManager.SaveScene(scene);
            Transform room = scene.GetRootGameObjects().Single(g => g.name == "MilkTeaShop_8m_x_10m").transform;
            Transform oldPlayer = room.Find("05_PlayerAndFixedCamera");
            if (oldPlayer != null) Object.DestroyImmediate(oldPlayer.gameObject);
            Transform markers = room.Find("04_ReservedPoints_NoGameplay");
            markers.name = "04_ReservedPoints";
            Rename(markers, "ShopEntrance_ClosedForDay1", "CustomerSpawn");
            Rename(markers, "CashierInteraction_PlayerSide", "OrderCounter");
            Rename(markers, "MilkTeaInteraction_PlayerSide", "PreparationStation");
            Rename(markers, "UndergroundEntrance_Approach", "DungeonEntrance");
            var delivery = new GameObject("DeliveryPoint");
            delivery.transform.SetParent(markers, false);
            delivery.transform.localPosition = V(0.9f, 0.02f, 1.25f);
            ConfigureInterior(room);
            ConfigureInteractionPrefabs();
            ConfigureHatch(room);
            SaveRoomPrefab(room);
            CreatePlayer(markers.Find("PlayerSpawn").position);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            MilkTeaShopBuilder.Validate();
            Debug.Log("MILK_TEA_FPS_SCENE_OK");
        }

        private static void Rename(Transform parent, string oldName, string newName)
        {
            parent.Find(oldName).name = newName;
        }

        private static void Resize(Transform parent, string name, Vector3 center, Vector3 size)
        {
            Transform target = parent.Find(name);
            target.localPosition = center;
            target.localScale = size;
        }

        private static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, string material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Materials + material + ".mat");
            return go;
        }

        public static void ConfigureInterior(Transform room)
        {
            Transform walls = room.Find("01_Architecture");
            Resize(walls, "RightWall_Cutaway", V(4.12f, 1.4f, 0), V(0.24f, 2.8f, 10));
            walls.Find("RightWall_Cutaway").name = "RightWall";
            Resize(walls, "LeftWall_Rear", V(-4.12f, 1.4f, 1.2f), V(0.24f, 2.8f, 7.6f));
            Resize(walls, "LeftWall_Front", V(-4.12f, 1.4f, -4.5f), V(0.24f, 2.8f, 1));
            foreach (string name in new[] { "RightBoundary_FullHeight", "LeftBoundary_FullHeight", "FrontBoundary_Cutaway" })
                Object.DestroyImmediate(walls.Find(name).gameObject);
            foreach (var cap in walls.Cast<Transform>().Where(t => t.name == "SideWall_WoodCap").ToArray())
                Object.DestroyImmediate(cap.gameObject);
            Box(walls, "FrontWall", V(0, 1.4f, -5.12f), V(8.48f, 2.8f, 0.24f), "shop_cream");
            Box(walls, "DoorHeader", V(-4.12f, 2.45f, -3.3f), V(0.24f, 0.7f, 1.4f), "shop_cream");
            walls.Find("Door_Closed_Day1").name = "CustomerEntrance_Closed";
            Box(walls, "Ceiling", V(0, 2.9f, 0), V(8.48f, 0.2f, 10.48f), "shop_cream");
            Box(walls, "RightWall_MatchaSkirting", V(3.975f, 0.25f, 0), V(0.035f, 0.5f, 10), "shop_matcha");
            Box(walls, "LeftWall_RearSkirting", V(-3.975f, 0.25f, 1.2f), V(0.035f, 0.5f, 7.6f), "shop_matcha");
            Box(walls, "LeftWall_FrontSkirting", V(-3.975f, 0.25f, -4.5f), V(0.035f, 0.5f, 1), "shop_matcha");
            Box(walls, "FrontWall_MatchaSkirting", V(0, 0.25f, -4.975f), V(8, 0.5f, 0.035f), "shop_matcha");
            Transform lighting = room.Find("06_Lighting");
            var key = lighting.Find("WarmIndoorKey").GetComponent<Light>();
            key.intensity = 0.55f;
            key.shadows = LightShadows.None;
            foreach (var light in lighting.GetComponentsInChildren<Light>().Where(l => l.type == LightType.Point))
            {
                light.intensity = 2.2f;
                light.color = new Color(1, 0.9f, 0.76f);
                light.range = 7f;
                Box(lighting, "CeilingFixture", light.transform.localPosition + V(0, 0.1f, 0), V(0.5f, 0.06f, 0.5f), "shop_cream");
            }
        }

        private static void SetInteraction(GameObject go, string prompt, string feedback)
        {
            var component = go.GetComponent<ShopPlaceholderInteractable>();
            if (component == null) component = go.AddComponent<ShopPlaceholderInteractable>();
            var settings = new SerializedObject(component);
            settings.FindProperty("prompt").stringValue = prompt;
            settings.FindProperty("feedback").stringValue = feedback;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void ConfigureInteractionPrefabs()
        {
            string[] assets = { "milk_tea_counter", "milk_tea_workbench" };
            string[] prompts = { "收银柜台 · 查看订单", "制作台 · 制作奶茶" };
            string[] feedback = { "柜台已响应。订单系统待接入。", "制作台已响应。奶茶制作系统待接入。" };
            for (int i = 0; i < assets.Length; i++)
            {
                string path = Prefabs + assets[i] + ".prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    SetInteraction(root, prompts[i], feedback[i]);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }

        public static void ConfigureHatch(Transform room)
        {
            Transform hatch = room.Find("03_UndergroundEntrancePlaceholder");
            var collider = hatch.gameObject.AddComponent<BoxCollider>();
            collider.center = V(2.2f, 0.025f, 2.7f);
            collider.size = V(1.25f, 0.05f, 1.4f);
            SetInteraction(hatch.gameObject, "地下入口 · 检查盖板", "地下入口已响应。本阶段暂不进入地下城。");
        }

        public static void SaveRoomPrefab(Transform room)
        {
            PrefabUtility.SaveAsPrefabAssetAndConnect(room.gameObject, Prefabs + "milk_tea_shop_room.prefab", InteractionMode.AutomatedAction);
        }

        public static void CreatePlayer(Vector3 spawn)
        {
            var go = new GameObject("MilkTeaShop_FirstPersonPlayer");
            go.layer = LayerMask.NameToLayer("Ignore Raycast");
            go.transform.position = spawn;
            var controller = go.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.center = V(0, 0.9f, 0);
            controller.radius = 0.28f;
            controller.skinWidth = 0.025f;
            controller.stepOffset = 0.15f;
            var eye = new GameObject("FirstPersonEye");
            eye.tag = "MainCamera";
            eye.transform.SetParent(go.transform, false);
            eye.transform.localPosition = V(0, 1.65f, 0);
            var camera = eye.AddComponent<Camera>();
            camera.fieldOfView = 65f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 60f;
            eye.AddComponent<AudioListener>();
            eye.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
            var fps = go.AddComponent<ShopFirstPersonController>();
            var settings = new SerializedObject(fps);
            settings.FindProperty("eye").objectReferenceValue = eye.transform;
            settings.ApplyModifiedPropertiesWithoutUndo();
            var interactor = go.AddComponent<ShopRaycastInteractor>();
            settings = new SerializedObject(interactor);
            settings.FindProperty("viewCamera").objectReferenceValue = camera;
            settings.FindProperty("player").objectReferenceValue = fps;
            settings.ApplyModifiedPropertiesWithoutUndo();
            // Prefab position is neutral; the scene instance owns its spawn placement.
            go.transform.position = Vector3.zero;
            PrefabUtility.SaveAsPrefabAssetAndConnect(go, Prefabs + "milk_tea_shop_player.prefab", InteractionMode.AutomatedAction);
            go.transform.position = spawn;
        }
    }
}
