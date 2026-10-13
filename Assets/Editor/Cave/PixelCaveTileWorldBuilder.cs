using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    // Explicit authoring only. Saved TWC paint layers and scene objects own the layout.
    public static class PixelCaveTileWorldBuilder
    {
        public const string ScenePath = "Assets/Scenes/PixelCaveTileWorld.unity";
        private const string ConfigFolder = "Assets/Configs/PixelCaveTileWorld";
        private const string MeshFolder = "Assets/Art/Models/PixelCaveTileWorld";
        private const string MeshPath = MeshFolder + "/generated_terrain.asset";
        private const string Tiles = "Assets/Prefabs/Terrain/PixelCaveTiles/";
        private const string Connectors = "Assets/Prefabs/Terrain/PixelCaveConnectors/";
        private const float Cell = 4;
        private const int MapLayer = 29;
        private static readonly Vector3 GridOrigin = new Vector3(-80, 0, -80);
        private static readonly string PreviewFolder = Path.Combine(Path.GetTempPath(), "untitled-pixel-cave-map");
        private static Scene mapScene;
        private static TileWorldCreatorManager manager;
        private static Camera overview;
        private static Camera plan;
        private static double started;
        private static readonly Dictionary<string, HashSet<Vector2>> masks = new Dictionary<string, HashSet<Vector2>>();

        [MenuItem("Untitled/Cave/Create Pixel TileWorld Map")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before authoring.");
            if (File.Exists(ScenePath) || Directory.Exists(ConfigFolder))
                throw new InvalidOperationException("The map already exists. Edit the saved scene and TWC paint layers.");
            foreach (string folder in new[] { ConfigFolder, MeshFolder, PreviewFolder }) Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            mapScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(mapScene);
            masks.Clear();
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.47f, .51f, .58f);
            RenderSettings.ambientEquatorColor = new Color(.30f, .32f, .34f);
            RenderSettings.ambientGroundColor = new Color(.18f, .17f, .15f);
            RenderSettings.fog = false;
            var config = ScriptableObject.CreateInstance<Configuration>();
            config.name = "pixel_cave_settlement";
            config.cellSizeOld = 0;
            config.cellSize = config.lastCellSize = Cell;
            config.width = 44; config.height = 44; config.clusterCellSize = 8;
            config.useGlobalRandomSeed = true; config.globalRandomSeed = 731;
            config.mergeTiles = true; config.useParallel = false;
            config.colliderType = Configuration.ColliderType.meshCollider;
            AssetDatabase.CreateAsset(config, ConfigFolder + "/pixel_cave_settlement.asset");
            var terrain = Group("TileWorldCreator | painted cave terraces");
            terrain.position = GridOrigin;
            manager = terrain.gameObject.AddComponent<TileWorldCreatorManager>();
            manager.configuration = config; manager.enabled = false;
            config.SetManager(manager);
            var earth = AssetDatabase.LoadAssetAtPath<TilePreset>("Assets/Configs/PixelCaveTiles/pixel_cliffs_earth.asset");
            var moss = AssetDatabase.LoadAssetAtPath<TilePreset>("Assets/Configs/PixelCaveTiles/pixel_cliffs_moss.asset");
            var main = Mask((x, z) => z >= -68 && z <= 40 && x >= -20 && x <= 84
                && !(z < -60 && (x < -4 || x > 60)) && !(z < -52 && (x < -12 || x > 76))
                && !(z > 32 && x > 76));
            var high = Mask((x, z) => z >= 44 && z <= 88 && x >= -28 && x <= 60
                && !(z > 80 && (x < -20 || x > 52)) && !(z == 44 && x > 44));
            var low = Mask((x, z) => z >= -64 && z <= 72 && x >= -76 && x <= -28
                && x != -52 && x != -48 && !(z < -56 && x < -68) && !(z > 64 && x < -68));
            AddLayer("01 Main settlement +2.4m", main, 2.4f, earth, true, 1.5f);
            AddLayer("02 Sacred terrace +9.6m", high, 9.6f, earth, true, 2.25f);
            AddLayer("03 River lowland -2.4m", low, -2.4f, moss, true, 1);
            var pathPreset = FloorPreset("path", Connectors + "tile_floor_path.prefab");
            var gravelPreset = FloorPreset("gravel", Connectors + "tile_floor_gravel.prefab");
            var stonePreset = FloorPreset("stone", Tiles + "tile_floor_stone.prefab");
            var routes = new HashSet<Vector2>(main.Where(p =>
            {
                Vector3 v = GridOrigin + new Vector3(p.x * Cell, 0, p.y * Cell);
                return (v.x == 0 && v.z < -28) || (v.x >= 24 && v.x <= 28 && v.z >= -52 && v.z <= 28)
                    || (v.z == 12 && v.x >= -16 && v.x <= 76)
                    || (v.z == -52 && v.x >= 0 && v.x <= 56)
                    || (v.z == 28 && v.x >= -20 && v.x <= 40);
            }));
            AddLayer("04 Living lanes", routes, 2.425f, pathPreset, false, 1);
            AddLayer("05 Gravel riverbanks", Mask((x, z) => low.Contains(Grid(x, z)) && (x == -56 || x == -44)),
                -2.375f, gravelPreset, false, 1);
            AddLayer("06 Ritual hall 40x28m", Mask((x, z) => x >= -16 && x <= 20 && z >= 52 && z <= 76),
                9.625f, stonePreset, false, 1);
            AddLayer("07 Sacred forecourt", Mask((x, z) => high.Contains(Grid(x, z)) && z <= 48 && (x == 0 || x == -20 || x == 36)),
                9.625f, pathPreset, false, 1);
            Connections();
            Buildings();
            Hall();
            River();
            Dressing();
            Boundaries();
            LightAt("Soft cave key", LightType.Directional, Vector3.zero, new Color(1, .86f, .69f), 1.2f, new Vector3(48, -28, 0));
            LightAt("Cool cave fill", LightType.Directional, Vector3.zero, new Color(.59f, .72f, 1), .3f, new Vector3(35, 145, 0));
            overview = CameraAt("Overview Camera | disabled review", new Vector3(4, 2, 8), new Vector3(120, 150, -180), 100, false);
            plan = CameraAt("Plan Camera | disabled review", new Vector3(4, 0, 8), new Vector3(0, 200, 0), 94, false);
            Player();
            manager.ExecuteBlueprintLayers();
            manager.ExecuteBuildLayers(ExecutionMode.FromScratch);
            started = EditorApplication.timeSinceStartup;
            EditorApplication.update += Finish;
        }

        private static Vector2 Grid(float x, float z) => new Vector2((x - GridOrigin.x) / Cell, (z - GridOrigin.z) / Cell);

        private static HashSet<Vector2> Mask(Func<float, float, bool> contains)
        {
            var cells = new HashSet<Vector2>();
            for (int z = 0; z < 44; z++)
                for (int x = 0; x < 44; x++)
                    if (contains(GridOrigin.x + x * Cell, GridOrigin.z + z * Cell)) cells.Add(new Vector2(x, z));
            return cells;
        }

        private static void AddLayer(string name, HashSet<Vector2> cells, float height, TilePreset preset, bool dual, float depthScale)
        {
            if (preset == null) throw new InvalidOperationException("Missing tile preset: " + name);
            masks.Add(name, cells);
            var blueprint = manager.AddNewBlueprintLayer(name);
            blueprint.defaultLayerHeight = height;
            blueprint.AddCells(cells);
            var layer = manager.AddNewBuildLayer<TilesBuildLayer>(name);
            layer.asset = manager.configuration; layer.assignedBlueprintLayerGuid = blueprint.guid;
            layer.useDualGrid = dual; layer.scaleTileToCellSize = false;
            // Extend the existing cliff skirts vertically to meet the next terrace.
            layer.scaleOffset = new Vector3(1, depthScale, 1);
            layer.meshGenerationOverride = true; layer.mergeTiles = true;
            layer.objectLayer = MapLayer; layer.colliderType = Configuration.ColliderType.meshCollider;
            layer.shadowCastingMode = ShadowCastingMode.On;
            layer.tilePresetsTop.Add(new TilesBuildLayer.TilePresetSelection { preset = preset, weight = 1 });
            EditorUtility.SetDirty(blueprint); EditorUtility.SetDirty(layer);
        }

        private static TilePreset FloorPreset(string name, string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("Missing tile: " + path);
            var preset = ScriptableObject.CreateInstance<TilePreset>();
            preset.name = "pixel_floor_" + name; preset.gridtype = TilePreset.GridType.standard;
            foreach (var field in typeof(TilePreset).GetFields().Where(f => f.Name.StartsWith("NRMGRD_") && f.FieldType == typeof(GameObject)))
                field.SetValue(preset, prefab);
            AssetDatabase.CreateAsset(preset, ConfigFolder + "/" + preset.name + ".asset");
            return preset;
        }

        private static Transform Group(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, mapScene);
            if (parent != null) go.transform.SetParent(parent, false);
            go.layer = MapLayer;
            return go.transform;
        }

        private static GameObject Place(string path, Transform parent, Vector3 position, float yaw = 0)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("Missing prefab: " + path);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, mapScene);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            SetLayer(go);
            return go;
        }

        private static void SetLayer(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = MapLayer;
            foreach (var light in go.GetComponentsInChildren<Light>(true)) light.cullingMask = 1 << MapLayer;
        }

        private static void Connections()
        {
            var root = Group("Connections | existing ramp bridge and bank tiles");
            foreach (float x in new[] { -20f, 0f, 36f })
                for (int i = 0; i < 3; i++)
                    Place(Connectors + "tile_ramp_240.prefab", root, new Vector3(x, 2.4f + i * 2.4f, 22 + i * 8));
            foreach (float z in new[] { -32f, 28f })
            {
                for (int i = 0; i < 2; i++)
                    Place(Connectors + "tile_ramp_240.prefab", root, new Vector3(-38 + i * 8, -2.4f + i * 2.4f, z), 90);
                Place(Connectors + "tile_floor_path.prefab", root, new Vector3(-24, 2.4f, z));
                for (float x = -56; x <= -44; x += 4)
                    Place(Connectors + (x == -44 ? "tile_bridge_landing.prefab" : "tile_bridge_deck.prefab"), root,
                        new Vector3(x, -2.375f, z), 90);
                for (float bank = -56; bank <= -44; bank += 12)
                    Place(Connectors + "tile_floor_gravel.prefab", root, new Vector3(bank, -2.37f, z + 4));
            }
            for (float z = -60; z <= 68; z += 4)
            {
                if (Mathf.Abs(z + 32) < 4 || Mathf.Abs(z - 28) < 4) continue;
                Place(Connectors + "tile_bank_straight.prefab", root, new Vector3(-55, -2.37f, z), 90);
                Place(Connectors + "tile_bank_straight.prefab", root, new Vector3(-45, -2.37f, z), -90);
            }
        }

        private static void Buildings()
        {
            var root = Group("SettlementBuildings | compact living district");
            void Room(string name, Vector3 at) => Place("Assets/Prefabs/Levels/CaveSettlement/cave_" + name + ".prefab", root, at);
            Room("tavern", new Vector3(4, 2.4f, -10));
            Room("casino", new Vector3(44, 2.4f, -10));
            Room("kitchen", new Vector3(42, 2.4f, -38));
            Room("training", new Vector3(62, 2.4f, 30));
            Room("elder", new Vector3(42, 9.6f, 64));
            var residence = Group("Residence | three compact building plots", root);
            foreach (float x in new[] { 8f, 16f, 24f })
            {
                for (float z = -44; z <= -36; z += 4)
                    Place(Tiles + "tile_floor_earth.prefab", residence, new Vector3(x, 2.425f, z));
                Place(Tiles + "tile_wall_straight.prefab", residence, new Vector3(x, 2.4f, -46));
                Place("Assets/Prefabs/Props/Tavern/supply_crate_3d.prefab", residence, new Vector3(x - 1, 2.4f, -43));
            }
            Place("Assets/Prefabs/Props/Lighting/bone_fire_stand_hd2d.prefab", root, new Vector3(25, 2.43f, 4));
            foreach (var at in new[] { new Vector3(24, 2.4f, 28), new Vector3(32, 2.4f, 30), new Vector3(-64, -2.4f, -20) })
                Place("Assets/Prefabs/Props/Tavern/round_table_3d.prefab", root, at);
            var anchors = Group("LayoutAnchors | saved scene positions");
            foreach (var pair in new[]
            {
                ("Entrance", new Vector3(0, 2.4f, -60)), ("Residence", new Vector3(16, 2.4f, -40)),
                ("Hearth", new Vector3(25, 2.4f, 4)), ("Feast", new Vector3(28, 2.4f, 28)),
                ("RitualHall_40x28m", new Vector3(2, 9.6f, 64)), ("RiverPlanting", new Vector3(-64, -2.4f, 20))
            }) Group(pair.Item1, anchors).localPosition = pair.Item2;
        }

        private static void Hall()
        {
            var root = Group("RitualHall | 40x28m assembly floor");
            // The enlarged shell uses the preview walls; furniture retains its original scale.
            for (float x = -16; x <= 20; x += 4)
                Place(Tiles + "tile_wall_straight.prefab", root, new Vector3(x, 9.6f, 78));
            for (float z = 52; z <= 76; z += 4)
                foreach (float x in new[] { -18f, 22f })
                    if (z != 56) Place(Tiles + "tile_wall_straight.prefab", root, new Vector3(x, 9.6f, z), 90);
            for (float x = -16; x <= 20; x += 4)
                if (x < -4 || x > 4) Place(Connectors + "tile_retaining_edge_120.prefab", root, new Vector3(x, 9.6f, 50));
            const string props = "Assets/Prefabs/Props/RitualHall/";
            Place(props + "carved_bone_sacrificial_altar_3d.prefab", root, new Vector3(2, 9.64f, 69));
            Place(props + "horned_ancestor_goblin_idol_3d.prefab", root, new Vector3(2, 9.64f, 74));
            foreach (float x in new[] { -10f, 14f })
            {
                for (float z = 54; z <= 74; z += 10)
                    Place(props + "rune_carved_ritual_column_3d.prefab", root, new Vector3(x, 9.64f, z));
                Place(props + "carved_offering_table_module_3d.prefab", root, new Vector3(x, 9.64f, 68));
                Place(props + "bone_bound_procession_torch_3d.prefab", root, new Vector3(x, 9.64f, 52));
            }
            foreach (float x in new[] { -4f, 2f, 8f })
                foreach (float z in new[] { 56f, 60f, 64f })
                    Place(props + "elder_grey_woven_kneeling_mat_3d.prefab", root, new Vector3(x, 9.66f, z));
        }

        private static void River()
        {
            var root = Group("River | between two tiled lowland banks");
            var mesh = new Mesh { name = "river_surface" };
            mesh.vertices = new[] { new Vector3(-54, -3.02f, -66), new Vector3(-54, -3.02f, 74),
                new Vector3(-46, -3.02f, 74), new Vector3(-46, -3.02f, -66) };
            mesh.uv = new[] { Vector2.zero, new Vector2(0, 35), new Vector2(2, 35), new Vector2(2, 0) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 }; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, MeshFolder + "/river_surface.asset");
            root.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/PixelCaveTiles/wet.mat");
            var material = new Material(source) { name = "pixel_cave_river" };
            material.SetColor("_BaseColor", new Color(.36f, .67f, .77f));
            material.SetFloat("_Smoothness", .55f);
            AssetDatabase.CreateAsset(material, ConfigFolder + "/pixel_cave_river.mat");
            root.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void Dressing()
        {
            var root = Group("Cave rim | pixel wall and rock modules");
            for (float z = -60; z <= 68; z += 4)
                Place(Tiles + "tile_wall_straight.prefab", root, new Vector3(-78, -2.4f, z), 90);
            for (float z = -48; z <= 28; z += 4)
                Place(Tiles + "tile_wall_straight.prefab", root, new Vector3(86, 2.4f, z), 90);
            for (float x = -16; x <= 48; x += 4)
                Place(Tiles + "tile_wall_straight.prefab", root, new Vector3(x, 9.6f, 90));
            foreach (var at in new[] { new Vector3(-70, -2.4f, -48), new Vector3(-70, -2.4f, 44),
                new Vector3(-30, 9.6f, 72), new Vector3(58, 9.6f, 80), new Vector3(80, 2.4f, -44) })
                Place(Tiles + "tile_rock_tall.prefab", root, at, at.z);
            foreach (var at in new[] { new Vector3(-68, -2.4f, 16), new Vector3(-64, -2.4f, 24), new Vector3(-68, -2.4f, 32) })
                Place(Tiles + "tile_floor_moss.prefab", root, at + Vector3.up * .03f);
            foreach (var at in new[] { new Vector3(0, 2.4f, -56), new Vector3(26, 2.4f, 14),
                new Vector3(-58, -2.4f, -32), new Vector3(-58, -2.4f, 28), new Vector3(0, 9.6f, 48) })
                Place("Assets/Prefabs/Props/Lighting/wood_tripod_lit_hd2d.prefab", root, at);
        }

        private static void Boundaries()
        {
            var root = Group("Collision | terrace void and river boundaries");
            var directions = new[] { Vector2.left, Vector2.right, Vector2.up, Vector2.down };
            int index = 0;
            foreach (var pair in masks.Take(3))
            {
                float height = pair.Key.StartsWith("01") ? 2.4f : pair.Key.StartsWith("02") ? 9.6f : -2.4f;
                foreach (var cell in pair.Value)
                    foreach (var direction in directions)
                    {
                        if (pair.Value.Contains(cell + direction)) continue;
                        Vector3 p = GridOrigin + new Vector3(cell.x * Cell + direction.x * 2, height + 1.5f, cell.y * Cell + direction.y * 2);
                        bool ramp = height == 9.6f && p.z < 46 && new[] { -20f, 0f, 36f }.Any(x => Mathf.Abs(p.x - x) <= 2.1f);
                        bool bridge = (height == -2.4f || height == 2.4f) && p.x > -60 && p.x < -18
                            && new[] { -32f, 28f }.Any(z => Mathf.Abs(p.z - z) <= 2.1f);
                        if (ramp || bridge) continue;
                        var wall = Group("Safety boundary " + index++, root);
                        wall.localPosition = p;
                        wall.gameObject.AddComponent<BoxCollider>().size = direction.x == 0
                            ? new Vector3(4.05f, 3, .12f) : new Vector3(.12f, 3, 4.05f);
                    }
            }
        }

        private static void LightAt(string name, LightType type, Vector3 position, Color color, float intensity, Vector3 rotation)
        {
            var root = Group(name); root.position = position; root.rotation = Quaternion.Euler(rotation);
            var light = root.gameObject.AddComponent<Light>();
            light.type = type; light.color = color; light.intensity = intensity; light.cullingMask = 1 << MapLayer;
            light.shadows = intensity > 1 ? LightShadows.Soft : LightShadows.None;
        }

        private static Camera CameraAt(string name, Vector3 target, Vector3 offset, float size, bool enabled)
        {
            var root = Group(name); root.position = target + offset; root.LookAt(target);
            var camera = root.gameObject.AddComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = size; camera.farClipPlane = 500;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.035f, .045f, .055f);
            camera.cullingMask = 1 << MapLayer; camera.enabled = enabled;
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false; data.antialiasing = AntialiasingMode.None;
            return camera;
        }

        private static void Player()
        {
            var root = Group("MapPlayer | WASD exploration"); root.position = new Vector3(0, 2.5f, -60);
            var controller = root.gameObject.AddComponent<CharacterController>();
            controller.height = 1.8f; controller.radius = .35f; controller.center = Vector3.up * .9f;
            controller.stepOffset = .25f; controller.skinWidth = .03f;
            var visual = Group("Visual", root);
            visual.gameObject.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Models/GoblinBillboard.asset");
            visual.gameObject.AddComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/FirstRoom/GoblinLit.mat");
            var camera = CameraAt("Play Camera | WASD exploration", root.position + Vector3.up, new Vector3(0, 14, -22), 15, true);
            camera.tag = "MainCamera";
            camera.gameObject.AddComponent<AudioListener>();
            visual.rotation = camera.transform.rotation;
            var billboard = visual.gameObject.AddComponent<SpriteBillboard>();
            var data = new SerializedObject(billboard); data.FindProperty("targetCamera").objectReferenceValue = camera; data.ApplyModifiedPropertiesWithoutUndo();
            var walker = root.gameObject.AddComponent<MapSamplePlayer>();
            data = new SerializedObject(walker);
            data.FindProperty("sampleCamera").objectReferenceValue = camera;
            data.FindProperty("sampleRoot").objectReferenceValue = Group("MapOrigin | XZ metres");
            data.FindProperty("spawnPoint").vector3Value = root.position;
            data.FindProperty("minCoord").vector2Value = new Vector2(-80, -72);
            data.FindProperty("maxCoord").vector2Value = new Vector2(88, 92);
            data.FindProperty("resetBelowHeight").floatValue = -7;
            data.FindProperty("cameraOffset").vector3Value = new Vector3(0, 14, -22);
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Finish()
        {
            try
            {
                if (manager.configuration.buildLayerFolders.SelectMany(f => f.buildLayers).Any(l => l.isExecuting))
                {
                    if (EditorApplication.timeSinceStartup - started > 120) throw new TimeoutException("TWC generation timed out.");
                    return;
                }
                EditorApplication.update -= Finish;
                SetLayer(manager.gameObject);
                PersistMeshes(manager);
                Physics.SyncTransforms();
                ValidateConnections();
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(manager.configuration))) EditorUtility.SetDirty(asset);
                SaveMapAssets(manager);
                EditorSceneManager.SaveScene(mapScene, ScenePath);
                Selection.activeGameObject = manager.gameObject;
                SceneView.lastActiveSceneView?.LookAt(new Vector3(4, 2, 8), Quaternion.Euler(52, 0, 0), 130);
                Capture(overview, "overview.png", 1600, 1200);
                Capture(plan, "plan.png", 1440, 1600);
                File.WriteAllText(Path.Combine(PreviewFolder, "complete.txt"), "Saved pixel cave map: 7 TWC layers, 40x28m ritual hall, compact full-size rooms, 3 ascending routes and 2 bridges. Connection collision checks passed.\n");
                Debug.Log("PIXEL_CAVE_MAP_SAVED: " + ScenePath);
            }
            catch (Exception error)
            {
                EditorApplication.update -= Finish;
                File.WriteAllText(Path.Combine(PreviewFolder, "error.txt"), error.ToString());
                Debug.LogException(error);
            }
        }

        [MenuItem("Untitled/Cave/Save Pixel TileWorld Generated Meshes")]
        public static void SaveGeneratedMeshes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path != ScenePath)
                throw new InvalidOperationException("Open PixelCaveTileWorld outside Play Mode.");
            var owner = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TileWorldCreatorManager>(true)).Single();
            PersistMeshes(owner); SaveMapAssets(owner);
            EditorSceneManager.MarkSceneDirty(owner.gameObject.scene);
            EditorSceneManager.SaveScene(owner.gameObject.scene);
        }

        [MenuItem("Untitled/Cave/Use Pixel Map Walk Camera")]
        public static void UseWalkCamera()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath)
                throw new InvalidOperationException("Open PixelCaveTileWorld outside Play Mode.");
            var cameras = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>()).ToArray();
            var playCamera = cameras.Single(c => c.name.StartsWith("Play Camera"));
            foreach (var camera in cameras)
            {
                Undo.RecordObject(camera, "Select cave walk camera");
                camera.enabled = camera == playCamera;
            }
            playCamera.tag = "MainCamera";
            if (playCamera.GetComponent<AudioListener>() == null) Undo.AddComponent<AudioListener>(playCamera.gameObject);
            var visual = scene.GetRootGameObjects().Single(g => g.name.StartsWith("MapPlayer"))
                .GetComponentInChildren<SpriteBillboard>();
            Undo.RecordObject(visual.transform, "Face the walk camera");
            visual.transform.rotation = playCamera.transform.rotation;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void SaveMapAssets(TileWorldCreatorManager owner)
        {
            // Save only this map's assets; other open scenes can have dirty TWC configurations.
            AssetDatabase.SaveAssetIfDirty(owner.configuration);
            var meshAsset = AssetDatabase.LoadMainAssetAtPath(MeshPath);
            if (meshAsset != null) AssetDatabase.SaveAssetIfDirty(meshAsset);
        }

        private static void PersistMeshes(TileWorldCreatorManager owner)
        {
            var copies = new Dictionary<Mesh, Mesh>();
            var existing = AssetDatabase.LoadAllAssetsAtPath(MeshPath).OfType<Mesh>().ToDictionary(m => m.name);
            foreach (var filter in owner.GetComponentsInChildren<MeshFilter>())
            {
                var source = filter.sharedMesh;
                if (source == null || AssetDatabase.Contains(source)) continue;
                if (!copies.TryGetValue(source, out var saved))
                {
                    string name = filter.transform.parent.parent.name + "_" + filter.transform.parent.name + "_" + filter.name;
                    var copy = Object.Instantiate(source); copy.name = name;
                    if (existing.TryGetValue(name, out saved))
                    {
                        EditorUtility.CopySerialized(copy, saved); Object.DestroyImmediate(copy); EditorUtility.SetDirty(saved);
                    }
                    else
                    {
                        saved = copy;
                        if (AssetDatabase.LoadMainAssetAtPath(MeshPath) == null) AssetDatabase.CreateAsset(saved, MeshPath);
                        else AssetDatabase.AddObjectToAsset(saved, MeshPath);
                        existing.Add(name, saved);
                    }
                    copies.Add(source, saved);
                }
                filter.sharedMesh = saved;
            }
            foreach (var collider in owner.GetComponentsInChildren<MeshCollider>())
                if (collider.sharedMesh != null && copies.TryGetValue(collider.sharedMesh, out var saved)) collider.sharedMesh = saved;
        }

        private static void ValidateConnections()
        {
            var colliders = mapScene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshCollider>()).ToArray();
            void Check(Vector3 p)
            {
                var ray = new Ray(p + Vector3.up * 30, Vector3.down);
                if (!colliders.Any(c => c.Raycast(ray, out var hit, 32) && Mathf.Abs(hit.point.y - p.y) < .08f))
                    throw new InvalidOperationException("Missing walk surface near " + p);
            }
            foreach (float x in new[] { -20f, 0f, 36f })
                for (float z = 18.1f; z < 42; z += .8f) Check(new Vector3(x, 2.4f + (z - 18) * .3f, z));
            foreach (float z in new[] { -32f, 28f })
                for (float x = -58; x <= -20; x += .8f)
                    Check(new Vector3(x, x < -42 ? -2.375f : x < -26 ? -2.4f + (x + 42) * .3f : 2.4f, z));
            Check(new Vector3(0, 2.4f, -60));
            Check(new Vector3(2, 9.625f, 54));
        }

        private static void Capture(Camera camera, string name, int width, int height)
        {
            var otherLights = Object.FindObjectsOfType<Light>().Where(l => l.gameObject.scene != mapScene && l.enabled).ToArray();
            foreach (var light in otherLights) light.enabled = false;
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            Texture2D pixels = null;
            try
            {
                camera.aspect = width / (float)height;
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target; pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
                File.WriteAllBytes(Path.Combine(PreviewFolder, name), pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous; camera.ResetAspect();
                if (pixels != null) Object.DestroyImmediate(pixels);
                RenderTexture.ReleaseTemporary(target);
                foreach (var light in otherLights) light.enabled = true;
            }
        }
    }
}
