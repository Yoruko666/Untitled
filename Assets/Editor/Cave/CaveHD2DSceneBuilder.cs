using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Untitled.Editor
{
    public static partial class CaveHD2DSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/CaveHD2D.unity";
        private const string MaterialFolder = "Assets/Art/Materials/CaveHD2D";
        private const string MeshPath = "Assets/Art/Models/CaveHD2D/Architecture.asset";
        private static readonly Color Warm = new Color(1f, 0.57f, 0.25f);
        private static readonly Color Cool = new Color(0.31f, 0.60f, 0.65f);
        private static CaveMap map;
        private static Material stone;
        private static Material wood;
        private static Material soil;
        private static Mesh cube;
        private static Mesh column;
        private static int propCount;

        [MenuItem("Untitled/Cave/Build HD2D Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath && active.isDirty)
                throw new InvalidOperationException("Save your current scene before opening CaveHD2D.");
            var scene = active.path == ScenePath ? active : EditorSceneManager.OpenScene(ScenePath);
            map = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CaveMap>()).Single();
            if (map.TryGetComponent<CaveTerrainStyle>(out var existing))
            {
                // Subsequent uses preserve members' scene placement and only refresh derived terrain.
                CaveTerrainBuilder.Rebuild(map, existing);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("CaveHD2D terrain rebuilt; scene props and architecture preserved.");
                return;
            }
            CaveMapBuilder.Validate(map);
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Build CaveHD2D scene");
            Directory.CreateDirectory(MaterialFolder);
            Directory.CreateDirectory("Assets/Art/Models/CaveHD2D");
            AssetDatabase.Refresh();

            var style = Undo.AddComponent<CaveTerrainStyle>(map.gameObject);
            style.Soil = Clone(map.GroundMaterial, "Soil", new Color(0.95f, 0.88f, 0.73f));
            style.Settlement = Clone(map.GroundMaterial, "Settlement", new Color(1.20f, 1.09f, 0.88f));
            style.WetGround = Clone(map.GroundMaterial, "WetGround", new Color(0.73f, 1.02f, 0.98f));
            style.Hall = Clone(map.GroundMaterial, "Hall", new Color(1.12f, 1.09f, 0.99f));
            style.RockCap = Clone(map.GroundMaterial, "RockCap", new Color(0.67f, 0.74f, 0.70f));
            Material walls = Clone(map.WallMaterial, "RockWall", new Color(1.08f, 1.07f, 0.99f));
            Material water = Clone(map.RiverMaterial, "River", Color.white);
            water.SetColor("_DeepColor", new Color(0.025f, 0.12f, 0.14f));
            water.SetColor("_ShallowColor", new Color(0.10f, 0.32f, 0.31f));
            water.SetColor("_FoamColor", new Color(0.42f, 0.66f, 0.58f));
            EditorUtility.SetDirty(water);
            stone = Clone(map.WallMaterial, "CarvedStone", new Color(1.32f, 1.24f, 1.05f));
            wood = Clone(map.GroundMaterial, "Timber", new Color(0.78f, 0.53f, 0.30f));
            soil = Clone(map.GroundMaterial, "GardenSoil", new Color(0.58f, 0.47f, 0.31f));
            var settings = new SerializedObject(map);
            settings.FindProperty("groundMaterial").objectReferenceValue = style.Settlement;
            settings.FindProperty("wallMaterial").objectReferenceValue = walls;
            settings.FindProperty("riverMaterial").objectReferenceValue = water;
            settings.ApplyModifiedProperties();
            EditorUtility.SetDirty(style);
            CaveTerrainBuilder.Rebuild(map, style);
            cube = PrimitiveMesh(PrimitiveType.Cube, "StoneBlock");
            column = PrimitiveMesh(PrimitiveType.Cylinder, "StoneColumn");

            // Physical architecture participates in the map's dynamic wall reveal.
            Transform architecture = Group("Architecture", map.transform);
            Transform props = Group("PlacedProps", map.transform.parent);
            propCount = 0;
            Entrance(props, architecture);
            Residence(props, architecture);
            Tavern(props, architecture);
            Workshop(props, architecture);
            RiverAndGarden(props, architecture);
            Storage(props, architecture);
            Feast(props, architecture);
            GreatHall(props, architecture);
            Kitchen(props, architecture);
            Warehouse(props, architecture);
            Training(props, architecture);
            ElderRoom(props, architecture);
            Doorways(props, architecture);
            SetPresentation(scene);
            ApplySceneLighting();
            // Remove the old standalone test torch from the copied scene only.
            foreach (var light in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Light>()))
                if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(light.gameObject) == "Assets/Prefabs/FirstRoom/Torch.prefab")
                {
                    Undo.DestroyObjectImmediate(PrefabUtility.GetOutermostPrefabInstanceRoot(light.gameObject));
                    break;
                }
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = map.gameObject;
            Debug.Log($"CaveHD2D saved: {CaveMap.ReadCells(map.FloorTilemap).Count} floor cells, {CaveMap.ReadCells(map.RiverTilemap).Count} river cells, {propCount} linked props; continuous rock, recessed river and walkable hall steps.");
        }

        [MenuItem("Untitled/Cave/Apply HD2D Scene Lighting")]
        public static void ApplySceneLighting()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != ScenePath)
                throw new InvalidOperationException("Open CaveHD2D in Edit Mode first.");
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/CaveTerrain.shader");
            if (shader == null || ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException("Cave terrain shader did not compile.");
            Shader pixelShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/CavePixelProp.shader");
            if (pixelShader == null || ShaderUtil.ShaderHasError(pixelShader))
                throw new InvalidOperationException("Cave pixel shader did not compile.");
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { MaterialFolder }))
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (material.shader.name != "Untitled/HD2D/Cave Wall Reveal") continue;
                Undo.RecordObject(material, "Use cave world-space texture mapping");
                material.shader = shader;
                EditorUtility.SetDirty(material);
            }
            foreach (var prop in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<HybridProp>()))
            {
                if (!prop.IsPixel || prop.Visual == null) continue;
                var renderer = prop.Visual.GetComponent<MeshRenderer>();
                if (renderer == null) continue;
                Material source = renderer.sharedMaterial;
                if (source.shader == pixelShader) continue;
                string path = MaterialFolder + "/Prop_" + prop.AssetId + ".mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(source) { name = "Prop_" + prop.AssetId, shader = pixelShader };
                    material.SetFloat("_Cull", 0f);
                    material.SetFloat("_Smoothness", 0.05f);
                    material.SetFloat("_AlphaClip", 1f);
                    material.EnableKeyword("_ALPHATEST_ON");
                    AssetDatabase.CreateAsset(material, path);
                }
                Undo.RecordObject(renderer, "Use scene URP pixel material");
                renderer.sharedMaterial = material;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            Transform root = scene.GetRootGameObjects().Single().transform;
            Transform existingFill = root.Find("Cameras/FrontFill");
            if (existingFill == null)
            {
                var fill = Group("FrontFill", root.Find("Cameras"));
                fill.rotation = Quaternion.Euler(35, 0, 0);
                var light = Undo.AddComponent<Light>(fill.gameObject);
                light.type = LightType.Directional;
                light.color = new Color(0.81f, 0.84f, 0.78f);
                light.intensity = 0.65f;
                light.shadows = LightShadows.None;
            }
            RenderSettings.sun = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Light>())
                .First(l => l.type == LightType.Directional && l.name != "FrontFill");
            RenderSettings.ambientSkyColor = new Color(0.42f, 0.46f, 0.43f);
            RenderSettings.ambientEquatorColor = new Color(0.28f, 0.30f, 0.26f);
            RenderSettings.ambientGroundColor = new Color(0.17f, 0.15f, 0.12f);
            DynamicGI.UpdateEnvironment();
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            Debug.Log("CaveHD2D lighting saved: front fill, intact key shadows and consistent world-space texture density.");
        }

        private static void Entrance(Transform props, Transform architecture)
        {
            Transform entrance = Group("01_EntranceAndHub", props);
            Prop(entrance, "TotemsBones/rune_banner_hd2d", 56, 88, 1.1f);
            Prop(entrance, "Camp/spike_barrier_hd2d", 53, 92, 1.4f);
            Prop(entrance, "Camp/spike_barrier_hd2d", 74, 92, 1.4f);
            Fire(entrance, "Lighting/wood_tripod_lit_hd2d", 57, 78, 1.1f, 7f);
            Fire(entrance, "Lighting/timber_brazier_hd2d", 71, 72, 1.1f, 7f);
            Prop(entrance, "TotemsBones/beast_totem_hd2d", 53, 73, 1.2f);
            Prop(entrance, "TotemsBones/rune_banner_hd2d", 74, 77, 1.1f);
            Pad(architecture, "HubGatheringStone", 63, 76, 5f, 4f, 0.045f, stone);
            Block(architecture, "WestRouteThreshold", 51, 77, 1f, 0.045f, 4f, wood);
            Block(architecture, "NorthRouteThreshold", 63, 70, 4f, 0.045f, 1f, stone);
            Block(architecture, "EastRouteThreshold", 75, 77, 1f, 0.045f, 4f, stone);
            Spot(entrance, "EntranceDaylight", 63, 91, 5f, new Color(0.58f, 0.65f, 0.66f), 1.5f, 16f);
        }

        private static void Residence(Transform props, Transform architecture)
        {
            Transform area = Group("02_Residence", props);
            foreach (float y in new[] { 47f, 53f, 60f })
                foreach (float x in new[] { 51f, 58f, 70f, 78f })
                {
                    Pad(architecture, "SleepingTerrace", x, y, 4f, 3.2f, 0.12f, wood);
                    Prop(area, "Camp/hide_tent_hd2d", x - 0.4f, y - 0.6f, 1.65f, 0.12f);
                    Prop(area, "Camp/straw_bed_hd2d", x + 1.0f, y + 0.9f, 1.15f, 0.12f, 90f);
                }
            Fire(area, "Lighting/campfire_hd2d", 65, 55, 1.25f, 9f);
            Fire(area, "Lighting/timber_brazier_hd2d", 48.5f, 58, 1f, 7f);
            Fire(area, "Lighting/timber_brazier_hd2d", 82, 50, 1f, 7f);
            Prop(area, "TotemsBones/rune_banner_hd2d", 81, 63, 1.05f);
        }

        private static void Tavern(Transform props, Transform architecture)
        {
            Transform area = Group("03_Tavern", props);
            Pad(architecture, "TavernWoodFloor", 31, 76, 21f, 10f, 0.08f, wood);
            foreach (float y in new[] { 72f, 80f })
                foreach (float x in new[] { 27f, 35f, 40f })
                {
                    Prop(area, "Kitchen/chopping_table_hd2d", x, y, 1.8f, 0.08f);
                    Prop(area, "Camp/straw_bed_hd2d", x, y + 1.7f, 0.8f, 0.08f, 90f);
                }
            Fire(area, "Lighting/iron_bowl_fire_hd2d", 22, 76, 1f, 8f);
            Prop(area, "Kitchen/cauldron_b_3d", 23, 79, 1.35f, 0.08f);
            Prop(area, "Kitchen/stone_cistern_standard_3d", 22, 71, 1.3f, 0.08f);
        }

        private static void Workshop(Transform props, Transform architecture)
        {
            Transform area = Group("04_WorkshopAndWaterworks", props);
            Prop(area, "Kitchen/chopping_table_hd2d", 28, 59, 2.4f);
            Prop(area, "Kitchen/firewood_stack_hd2d", 25, 56, 1.8f);
            Prop(area, "Kitchen/firewood_bundle_hd2d", 24, 62, 1.7f);
            Prop(area, "TotemsBones/rune_shield_hd2d", 32, 56, 1.2f);
            Fire(area, "Lighting/iron_cage_fire_hd2d", 32, 62, 0.9f, 7f);
            Prop(area, "Kitchen/stone_cistern_wide_3d", 12, 53, 1.45f);
            Prop(area, "Kitchen/stone_cistern_narrow_3d", 12, 59, 1.1f);
            Spot(area, "WaterworksCoolBounce", 10, 50, 3f, Cool, 1.0f, 9f);
        }

        private static void RiverAndGarden(Transform props, Transform architecture)
        {
            Transform area = Group("05_RiverAndCultivation", props);
            foreach (float y in new[] { 6f, 11f, 16f, 21f })
            {
                Pad(architecture, "CultivationBed", 17, y, 10f, 2.8f, 0.16f, soil);
                for (int i = 0; i < 4; i++)
                    Prop(area, i % 2 == 0 ? "Kitchen/produce_leaves_hd2d" : "Kitchen/produce_roots_hd2d",
                        13.5f + i * 2.2f, y, 1.1f, 0.16f);
            }
            for (int y = 10; y < 60; y += 12)
                Spot(area, "RiverCoolBounce", 7, y, 2.8f, Cool, 0.9f, 10f);
            Fire(area, "Lighting/wall_torch_stone_hd2d", 23, 17, 0.8f, 7f, 1.3f);
            Fire(area, "Lighting/wall_torch_stone_hd2d", 10, 48, 0.8f, 7f, 1.3f);
            Prop(area, "Kitchen/stone_cistern_standard_3d", 11, 22, 1.2f);
        }

        private static void Storage(Transform props, Transform architecture)
        {
            Transform area = Group("06_UpperStorage", props);
            foreach (var p in new[] { new Vector2(33, 15), new Vector2(38, 15), new Vector2(38, 26) })
            {
                Prop(area, "Kitchen/firewood_stack_hd2d", p.x, p.y, 2f);
                Prop(area, "Kitchen/firewood_bundle_hd2d", p.x - 1, p.y + 1, 1.4f);
            }
            Fire(area, "Lighting/wall_torch_wood_hd2d", 31, 20, 0.85f, 7f, 1.2f);
        }

        private static void Feast(Transform props, Transform architecture)
        {
            Transform area = Group("07_FeastHall", props);
            foreach (float y in new[] { 30f, 37f })
                foreach (float x in new[] { 55f, 71f })
                {
                    Pad(architecture, "DiningIsland", x, y, 6f, 3f, 0.08f, wood);
                    Prop(area, "Kitchen/chopping_table_hd2d", x - 1.3f, y, 2.1f, 0.08f);
                    Prop(area, "Kitchen/chopping_table_hd2d", x + 1.3f, y, 2.1f, 0.08f);
                    Prop(area, "Kitchen/produce_garden_hd2d", x, y - 0.7f, 0.7f, 0.9f);
                }
            Fire(area, "Lighting/iron_bowl_fire_hd2d", 63, 33, 1.25f, 10f);
            Fire(area, "Lighting/timber_brazier_hd2d", 49, 33, 1f, 8f);
            Fire(area, "Lighting/timber_brazier_hd2d", 78, 33, 1f, 8f);
        }

        private static void GreatHall(Transform props, Transform architecture)
        {
            Transform area = Group("08_GreatHall", props);
            Pad(architecture, "ChieftainDais", 63, 5, 8f, 8f, 1.08f, stone);
            for (int i = 0; i < 6; i++)
                Block(architecture, "DaisStep_" + (i + 1), 63, 11.75f - i * 0.5f,
                    5f, 0.18f * (i + 1), 0.5f, stone);
            Prop(area, "TotemsBones/beast_totem_hd2d", 63, 4, 2.0f, 1.08f);
            Prop(area, "TotemsBones/rune_banner_hd2d", 60, 6, 1.4f, 1.08f);
            Prop(area, "TotemsBones/rune_banner_hd2d", 66, 6, 1.4f, 1.08f);
            foreach (float x in new[] { 54f, 72f })
                foreach (float y in new[] { 4f, 8f, 12f, 16f, 20f })
                {
                    Column(architecture, "HallStoneColumn", x, y, 0.8f, 2.8f);
                    Pad(architecture, "ColumnFoot", x, y, 1.15f, 1.15f, 0.16f, stone);
                }
            foreach (float x in new[] { 58f, 68f })
                Fire(area, "Lighting/bone_fire_stand_hd2d", x, 8, 1.25f, 10f);
            Fire(area, "Lighting/iron_cage_fire_hd2d", 49, 15, 1.15f, 9f);
            Fire(area, "Lighting/iron_cage_fire_hd2d", 79, 15, 1.15f, 9f);
            Spot(area, "HallOverheadShaft", 63, 8, 7f, new Color(0.67f, 0.72f, 0.66f), 2.2f, 20f);
        }

        private static void Kitchen(Transform props, Transform architecture)
        {
            Transform area = Group("09_Kitchen", props);
            Prop(area, "Kitchen/cooking_stove_hd2d", 90, 29, 1.6f);
            Prop(area, "Kitchen/chopping_table_hd2d", 94, 31, 1.7f);
            Prop(area, "Kitchen/meat_rack_hd2d", 91, 37, 1.35f);
            Prop(area, "Kitchen/cauldron_a_3d", 99, 37, 1.35f);
            Prop(area, "Kitchen/stone_cistern_wide_3d", 108, 29, 1.5f);
            Prop(area, "Kitchen/firewood_stack_hd2d", 108, 38, 1.4f);
            Prop(area, "Kitchen/produce_onions_hd2d", 103, 38, 1.3f);
            Fire(area, "Lighting/campfire_hd2d", 99, 37, 0.65f, 8f);
            Fire(area, "Lighting/wall_torch_stone_hd2d", 87, 28, 0.8f, 7f, 1.2f);
            Pad(architecture, "KitchenWetWorkingPad", 102, 31, 9f, 5f, 0.07f, stone);
        }

        private static void Warehouse(Transform props, Transform architecture)
        {
            Transform area = Group("10_Warehouse", props);
            foreach (var p in new[] { new Vector2(97, 51), new Vector2(104, 51), new Vector2(97, 64),
                new Vector2(104, 64), new Vector2(112, 64), new Vector2(119, 64) })
            {
                Pad(architecture, "StoragePlinth", p.x, p.y, 3.7f, 3.7f, 0.12f, wood);
                Prop(area, "Kitchen/firewood_stack_hd2d", p.x, p.y, 2.1f, 0.12f);
                Prop(area, "Kitchen/produce_garden_hd2d", p.x + 1, p.y + 1, 1.1f, 0.12f);
            }
            Prop(area, "Camp/hide_tent_hd2d", 119, 51, 1.6f);
            Fire(area, "Lighting/wood_tripod_lit_hd2d", 94, 59, 1.05f, 9f);
            Fire(area, "Lighting/wood_tripod_lit_hd2d", 120, 59, 1.05f, 9f);
            Prop(area, "TotemsBones/rune_banner_hd2d", 108, 70, 1.05f);
        }

        private static void Training(Transform props, Transform architecture)
        {
            Transform area = Group("11_Training", props);
            foreach (float y in new[] { 82f, 86f, 90f })
            {
                Prop(area, "Camp/training_dummy_hd2d", 94, y, 1.2f);
                Prop(area, "TotemsBones/ring_training_rack_hd2d", 105, y, 1.3f);
            }
            Fire(area, "Lighting/iron_cage_fire_hd2d", 88, 86, 1.05f, 10f);
            Fire(area, "Lighting/iron_cage_fire_hd2d", 115, 86, 1.05f, 10f);
            Pad(architecture, "TrainingRing", 101, 85, 19f, 8f, 0.06f, soil);
        }

        private static void ElderRoom(Transform props, Transform architecture)
        {
            Transform area = Group("12_ElderRoom", props);
            Pad(architecture, "ElderTerrace", 99, 4, 5f, 4f, 0.18f, wood);
            Prop(area, "Camp/wooden_bed_hd2d", 100, 3, 1.2f, 0.18f);
            Prop(area, "TotemsBones/beast_totem_hd2d", 94, 3, 1.25f);
            Prop(area, "Kitchen/chopping_table_hd2d", 97, 8, 1.4f);
            Fire(area, "Lighting/timber_brazier_hd2d", 102, 9, 1f, 8f);
        }

        private static void Doorways(Transform props, Transform architecture)
        {
            Transform area = Group("13_Passages", props);
            foreach (var p in new[] { new Vector2(63, 24), new Vector2(63, 43), new Vector2(63, 68),
                new Vector2(101, 76), new Vector2(31, 32), new Vector2(89, 10), new Vector2(83, 34) })
            {
                Fire(area, "Lighting/wall_torch_wood_hd2d", p.x == 31 ? 31 : p.x - 1.35f, p.y, 0.6f, 6f, 1.1f);
                // Doorposts lie outside the 4-cell passage; no overhead lintel blocks the camera.
                if (p.x == 63 || p.x == 101)
                    foreach (float side in new[] { -2.3f, 2.3f })
                        Column(architecture, "PassageRockShoulder", p.x + side, p.y, 0.6f, 2.1f);
            }
        }

        private static void SetPresentation(UnityEngine.SceneManagement.Scene scene)
        {
            var follow = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CameraController>()).Single();
            Camera camera = follow.GetComponent<Camera>();
            Undo.RecordObjects(new UnityEngine.Object[] { follow, camera, camera.transform }, "Set HD2D camera");
            var settings = new SerializedObject(follow);
            settings.FindProperty("viewAngle").floatValue = 32f;
            settings.FindProperty("distance").floatValue = 21.5f;
            settings.FindProperty("focusOffset").vector3Value = new Vector3(0, 1.2f, 0);
            settings.ApplyModifiedProperties();
            camera.fieldOfView = 36f;
            camera.backgroundColor = new Color(0.023f, 0.031f, 0.033f);
            camera.orthographic = false;
            Quaternion rotation = Quaternion.Euler(32, 0, 0);
            camera.transform.SetPositionAndRotation(follow.Target.position + Vector3.up * 1.2f - rotation * Vector3.forward * 21.5f, rotation);
            foreach (var billboard in follow.Target.GetComponentsInChildren<SpriteBillboard>())
            {
                Undo.RecordObject(billboard.transform, "Stand character upright");
                billboard.transform.localRotation = Quaternion.identity;
                var serialized = new SerializedObject(billboard);
                serialized.FindProperty("keepUpright").boolValue = true;
                serialized.ApplyModifiedProperties();
            }
            var reveal = new SerializedObject(map.GetComponent<CaveWallReveal>());
            reveal.FindProperty("revealRadius").floatValue = 2.0f;
            reveal.ApplyModifiedProperties();
            var volume = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Volume>()).Single();
            const string profilePath = "Assets/Settings/CaveHD2DPresentation.asset";
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath) == null)
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(volume.sharedProfile), profilePath);
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (volume.sharedProfile.TryGet<Bloom>(out var bloom)) bloom.intensity.Override(0.3f);
            if (volume.sharedProfile.TryGet<DepthOfField>(out var depth))
            {
                depth.focalLength.Override(55f);
                depth.aperture.Override(5.6f);
                depth.focusDistance.Override(21.9f);
            }
            EditorUtility.SetDirty(volume.sharedProfile);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.27f, 0.31f, 0.32f);
            RenderSettings.ambientEquatorColor = new Color(0.17f, 0.19f, 0.18f);
            RenderSettings.ambientGroundColor = new Color(0.10f, 0.09f, 0.07f);
            RenderSettings.fogColor = camera.backgroundColor;
            RenderSettings.fogStartDistance = 30f;
            RenderSettings.fogEndDistance = 55f;
            foreach (var light in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Light>()))
                if (light.type == LightType.Directional)
                {
                    Undo.RecordObject(light, "Set cave fill light");
                    light.color = new Color(0.67f, 0.73f, 0.75f);
                    light.intensity = 0.6f;
                }
        }

        private static GameObject Prop(Transform parent, string path, float x, float y, float scale = 1f, float height = 0, float angle = 0)
        {
            if (!map.IsWalkable(Position(x, y))) throw new InvalidOperationException("Prop outside floor: " + path + " at " + x + "," + y);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/" + path + ".prefab");
            if (prefab == null) throw new InvalidOperationException("Missing scene prefab: " + path);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            Undo.RegisterCreatedObjectUndo(go, "Place cave prop");
            go.transform.position = Position(x, y, height);
            go.transform.localScale = Vector3.one * scale;
            go.transform.rotation = Quaternion.Euler(0, angle, 0);
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
            propCount++;
            return go;
        }

        private static void Fire(Transform parent, string prefab, float x, float y, float scale, float range, float height = 0)
        {
            var go = Prop(parent, prefab, x, y, scale, height);
            foreach (var light in go.GetComponentsInChildren<Light>())
            {
                light.color = Warm;
                light.intensity = 1.5f;
                light.range = range;
                light.shadows = LightShadows.None;
                PrefabUtility.RecordPrefabInstancePropertyModifications(light);
                var flicker = light.GetComponent<HybridFireLight>();
                if (flicker == null) continue;
                var settings = new SerializedObject(flicker);
                settings.FindProperty("baseIntensity").floatValue = 1.5f;
                settings.FindProperty("baseRange").floatValue = range / scale;
                settings.ApplyModifiedProperties();
            }
        }

        private static void Spot(Transform parent, string name, float x, float y, float height, Color color, float intensity, float range)
        {
            Transform go = Group(name, parent);
            go.position = Position(x, y, height);
            go.rotation = Quaternion.Euler(90, 0, 0);
            var light = Undo.AddComponent<Light>(go.gameObject);
            light.type = LightType.Spot;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.spotAngle = 105f;
            light.innerSpotAngle = 60f;
            light.shadows = LightShadows.None;
        }

        private static Vector3 Position(float x, float y, float height = 0)
        {
            Vector3 origin = map.LayoutGrid.transform.localPosition;
            float surfaceHeight = map.Elevation != null ? map.Elevation.Height(x, y) : 0f;
            return map.transform.TransformPoint(origin + new Vector3(x * map.CellSize, surfaceHeight + height * map.CellSize, -y * map.CellSize));
        }

        private static void Pad(Transform parent, string name, float x, float y, float width, float length, float height, Material material)
            => Block(parent, name, x, y, width, height, length, material);

        private static void Block(Transform parent, string name, float x, float y, float width, float height, float length, Material material)
        {
            Transform go = Group(name, parent);
            go.position = Position(x, y, height * 0.5f);
            go.localScale = new Vector3(width, height, length) * map.CellSize;
            go.gameObject.AddComponent<MeshFilter>().sharedMesh = cube;
            go.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
            go.gameObject.AddComponent<BoxCollider>();
        }

        private static void Column(Transform parent, string name, float x, float y, float width, float height)
        {
            Transform go = Group(name, parent);
            go.position = Position(x, y, height * 0.5f);
            go.localScale = new Vector3(width, height * 0.5f, width) * map.CellSize;
            go.gameObject.AddComponent<MeshFilter>().sharedMesh = column;
            go.gameObject.AddComponent<MeshRenderer>().sharedMaterial = stone;
            var collider = go.gameObject.AddComponent<CapsuleCollider>();
            collider.height = 2f;
            collider.radius = 0.5f;
        }

        private static Transform Group(string name, Transform parent)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create cave scene group");
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static Material Clone(Material source, string name, Color tint)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            var result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (result != null) return result;
            result = new Material(source) { name = name };
            if (result.HasProperty("_BaseColor")) result.SetColor("_BaseColor", tint);
            if (source.shader.name == "Untitled/HD2D/Cave Wall Reveal")
                result.shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/CaveTerrain.shader");
            AssetDatabase.CreateAsset(result, path);
            return result;
        }

        private static Mesh PrimitiveMesh(PrimitiveType type, string name)
        {
            var saved = AssetDatabase.LoadAllAssetsAtPath(MeshPath).OfType<Mesh>().FirstOrDefault(m =>
                m.name == name || type == PrimitiveType.Cube && m.name == Path.GetFileNameWithoutExtension(MeshPath));
            if (saved != null) return saved;
            var primitive = GameObject.CreatePrimitive(type);
            var mesh = UnityEngine.Object.Instantiate(primitive.GetComponent<MeshFilter>().sharedMesh);
            UnityEngine.Object.DestroyImmediate(primitive);
            mesh.name = name;
            // All opaque architectural surfaces support the same local wall reveal.
            mesh.colors = Enumerable.Repeat(Color.red, mesh.vertexCount).ToArray();
            if (AssetDatabase.LoadMainAssetAtPath(MeshPath) == null)
            {
                mesh.name = Path.GetFileNameWithoutExtension(MeshPath);
                AssetDatabase.CreateAsset(mesh, MeshPath);
            }
            else AssetDatabase.AddObjectToAsset(mesh, MeshPath);
            return mesh;
        }
    }
}
