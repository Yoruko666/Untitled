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
    /// <summary>Authors the small tile library and its separate review scene.</summary>
    public static class PixelCaveTilePreview
    {
        private const string ScenePath = "Assets/Scenes/PixelCaveTilePreview.unity";
        private const string Models = "Assets/Art/Models/PixelCaveTiles";
        private const string Materials = "Assets/Art/Materials/PixelCaveTiles";
        private const string Prefabs = "Assets/Prefabs/Terrain/PixelCaveTiles";
        private const string Configs = "Assets/Configs/PixelCaveTiles";
        private const int PreviewLayer = 31;
        private const int LibraryLayer = 30;
        [Serializable] private class ModuleRecord { public string Name; public int[] Slots; }
        [Serializable] private class ModuleManifest { public ModuleRecord[] Modules; }
        private static readonly string TaskFolder = Path.Combine(Path.GetTempPath(), "untitled-pixel-cave-tiles");
        private static readonly Dictionary<string, GameObject> modules = new Dictionary<string, GameObject>();
        private static readonly Dictionary<string, Material> palette = new Dictionary<string, Material>();
        private static readonly List<TileWorldCreatorManager> managers = new List<TileWorldCreatorManager>();
        private static Scene previousScene;
        private static Scene previewScene;
        private static Camera libraryCamera;
        private static Camera assemblyCamera;
        private static double started;

        [MenuItem("Untitled/Cave/Create Pixel Tile Preview")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before authoring tiles.");
            if (File.Exists(ScenePath))
                throw new InvalidOperationException("The preview exists. Open its scene and edit its saved layers.");
            foreach (string folder in new[] { Materials, Prefabs, Configs, Models + "/meshes" })
                Directory.CreateDirectory(folder);
            Directory.CreateDirectory(TaskFolder);
            AssetDatabase.Refresh();
            modules.Clear(); palette.Clear(); managers.Clear();
            BuildPalette();
            ImportModules();
            var earth = CreatePreset("earth", false);
            var moss = CreatePreset("moss", true);
            previousScene = SceneManager.GetActiveScene();
            // Additive creation preserves unsaved work in the user's current scene.
            previewScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(previewScene);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.49f, .56f, .66f);
            RenderSettings.ambientEquatorColor = new Color(.36f, .38f, .40f);
            RenderSettings.ambientGroundColor = new Color(.23f, .22f, .20f);
            RenderSettings.fog = false;
            LightAt("Soft key", new Vector3(6, 16, -9), LightType.Directional,
                new Color(1, .88f, .72f), 1.35f, new Vector3(48, -35, 0));
            LightAt("Cool fill", new Vector3(15, 12, 25), LightType.Directional,
                new Color(.64f, .76f, 1), .35f, new Vector3(38, 150, 0));
            Library();
            Assembly(earth, moss);
            libraryCamera = CameraAt("Tile Library Camera", new Vector3(9, 1.4f, 9),
                new Vector3(24, 33, -35), 17.6f, true);
            assemblyCamera = CameraAt("Assembly Camera", new Vector3(54, 1.8f, 17),
                new Vector3(23, 23, -31), 18.5f, false);
            Selection.activeGameObject = libraryCamera.gameObject;
            foreach (var manager in managers)
            {
                manager.ExecuteBlueprintLayers();
                manager.ExecuteBuildLayers(ExecutionMode.FromScratch);
            }
            started = EditorApplication.timeSinceStartup;
            EditorApplication.update += Finish;
        }

        private static Material Surface(string name, string texture, Color tint, float smoothness = .025f)
        {
            string path = Materials + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool create = material == null;
            if (create) material = new Material(Shader.Find("Untitled/HD2D/Terraced Pixel Terrain")) { name = name };
            var image = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Art/Textures/PixelCaveTiles/goblin_cave_" + texture + "_64.png");
            if (image == null) throw new InvalidOperationException("Missing pixel texture: " + texture);
            material.SetTexture("_BaseMap", image);
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_TextureStrength", 1);
            material.SetFloat("_PixelDensity", 16);
            material.SetFloat("_TileScale", .25f);
            material.SetFloat("_PaletteSteps", 32);
            material.SetFloat("_Smoothness", smoothness);
            material.enableInstancing = true;
            if (create) AssetDatabase.CreateAsset(material, path);
            else EditorUtility.SetDirty(material);
            palette.Add(name, material);
            return material;
        }

        private static void BuildPalette()
        {
            Surface("earth", "earth", new Color(1.16f, 1.06f, .93f));
            Surface("stone", "rock", new Color(1.1f, 1.12f, 1.16f));
            Surface("moss", "moss", new Color(1.16f, 1.22f, 1.06f));
            Surface("wet", "wet", new Color(.88f, 1.02f, 1.07f), .16f);
            Surface("rock", "rock", new Color(.96f, .96f, .98f));
            Surface("rock_shadow", "rock", new Color(.69f, .72f, .76f));
            Surface("rock_light", "rock", new Color(1.17f, 1.13f, 1.03f));
            Surface("rim", "earth", new Color(.87f, .82f, .7f));
            string path = Materials + "/sample_pedestal.mat";
            var pedestal = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool create = pedestal == null;
            if (create) pedestal = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "sample_pedestal" };
            pedestal.SetColor("_BaseColor", new Color(.075f, .10f, .13f));
            pedestal.SetFloat("_Smoothness", .1f);
            if (create) AssetDatabase.CreateAsset(pedestal, path);
            palette.Add("sample_pedestal", pedestal);
        }

        private static Material[] ModuleMaterials(string name, bool moss)
        {
            string surface = name.StartsWith("tile_floor_") ? name.Substring("tile_floor_".Length)
                : name.StartsWith("tile_wall_") || name.StartsWith("tile_rock_") ? "stone" : moss ? "moss" : "earth";
            return new[] { palette[surface], palette["rock"], palette["rock_shadow"],
                palette["rock_light"], palette[moss ? "moss" : "rim"] };
        }

        private static void ImportModules()
        {
            string path = Models + "/pixel_cave_tiles.fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.isReadable = true;
            importer.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var records = JsonUtility.FromJson<ModuleManifest>(File.ReadAllText(Models + "/pixel_cave_tiles.json"))
                .Modules.ToDictionary(record => record.Name);
            foreach (var source in model.GetComponentsInChildren<MeshFilter>())
            {
                var root = new GameObject(source.name);
                var mesh = Object.Instantiate(source.sharedMesh);
                mesh.name = source.name;
                // Blender's imported XY plane faces -XZ. Bake the conversion once;
                // all authored prefabs then retain an identity Transform in metres.
                var matrix = Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0)) * source.transform.localToWorldMatrix;
                mesh.vertices = mesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray();
                mesh.normals = mesh.normals.Select(n => matrix.inverse.transpose.MultiplyVector(n).normalized).ToArray();
                mesh.RecalculateBounds();
                string meshPath = Models + "/meshes/" + source.name + ".asset";
                var saved = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (saved == null) AssetDatabase.CreateAsset(mesh, meshPath);
                else
                {
                    EditorUtility.CopySerialized(mesh, saved);
                    Object.DestroyImmediate(mesh);
                    mesh = saved;
                }
                int[] slots = records[source.name].Slots;
                if (mesh.subMeshCount != slots.Length)
                    throw new InvalidOperationException("Material slot mismatch for " + source.name);
                var allMaterials = ModuleMaterials(source.name, false);
                root.AddComponent<MeshFilter>().sharedMesh = mesh;
                root.AddComponent<MeshRenderer>().sharedMaterials = slots.Select(index => allMaterials[index]).ToArray();
                root.AddComponent<MeshCollider>().sharedMesh = mesh;
                modules.Add(source.name, PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "/" + source.name + ".prefab"));
                Object.DestroyImmediate(root);
            }
            if (modules.Count != 16) throw new InvalidOperationException("Expected 16 imported tile meshes.");
        }

        private static TilePreset CreatePreset(string family, bool moss)
        {
            string path = Configs + "/pixel_cliffs_" + family + ".asset";
            var preset = AssetDatabase.LoadAssetAtPath<TilePreset>(path);
            bool create = preset == null;
            if (create) preset = ScriptableObject.CreateInstance<TilePreset>();
            preset.name = "pixel_cliffs_" + family;
            preset.gridtype = TilePreset.GridType.dual;
            var roles = new Dictionary<string, GameObject>();
            foreach (string role in new[] { "fill", "edge", "outer_corner", "inner_corner", "double_corner" })
            {
                var prefab = modules["tile_cliff_" + role];
                if (moss)
                {
                    var copy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    PrefabUtility.UnpackPrefabInstance(copy, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    copy.name = "tile_moss_cliff_" + role;
                    var renderer = copy.GetComponentInChildren<MeshRenderer>();
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(material =>
                        material == palette["earth"] || material == palette["rim"] ? palette["moss"] : material).ToArray();
                    prefab = PrefabUtility.SaveAsPrefabAsset(copy, Prefabs + "/" + copy.name + ".prefab");
                    Object.DestroyImmediate(copy);
                }
                roles.Add(role, prefab);
            }
            preset.DUALGRD_fillTile = roles["fill"];
            preset.DUALGRD_edgeTile = roles["edge"];
            preset.DUALGRD_cornerTile = roles["outer_corner"];
            preset.DUALGRD_invertedCornerTile = roles["inner_corner"];
            preset.DUALGRD_doubleInteriorCornerTile = roles["double_corner"];
            // V4 configuration 7 leaves the bottom-right quadrant empty.
            preset.invertedCornerTileYRotationOffset = 0;
            if (create) AssetDatabase.CreateAsset(preset, path);
            else EditorUtility.SetDirty(preset);
            return preset;
        }

        private static Transform Group(string name)
        {
            var root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, previewScene);
            root.layer = PreviewLayer;
            return root.transform;
        }

        private static void SetPreviewLayer(GameObject root, int layer = PreviewLayer)
        {
            foreach (var item in root.GetComponentsInChildren<Transform>(true)) item.gameObject.layer = layer;
        }

        private static GameObject Place(string name, Transform parent, Vector3 position, float yaw = 0)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(modules[name], parent);
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.Euler(0, yaw, 0);
            SetPreviewLayer(instance);
            return instance;
        }

        private static void Label(string text, Transform parent, Vector3 position, float size = .25f)
        {
            var root = new GameObject(text);
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(90, 0, 0);
            root.layer = PreviewLayer;
            var label = root.AddComponent<TextMesh>();
            label.text = text;
            label.fontSize = 64;
            label.characterSize = size;
            label.anchor = TextAnchor.MiddleCenter;
            label.color = new Color(.78f, .86f, .91f);
        }

        private static void Library()
        {
            var root = Group("01 Tile library | 4 m modules");
            var order = new[] { "floor_earth", "floor_stone", "floor_moss", "floor_wet",
                "cliff_fill", "cliff_edge", "cliff_outer_corner", "cliff_inner_corner",
                "cliff_double_corner", "wall_straight", "wall_corner", "wall_end",
                "ramp", "steps", "rock_low", "rock_tall" };
            var labels = new[] { "EARTH", "STONE", "MOSS", "WET", "FILL", "EDGE", "OUTER", "INNER",
                "DOUBLE", "WALL", "WALL L", "WALL END", "RAMP", "STEPS", "ROCK LOW", "ROCK TALL" };
            for (int i = 0; i < order.Length; i++)
            {
                var position = new Vector3((i % 4) * 6, 0, (i / 4) * 6);
                var pedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pedestal.name = "Sample " + (i + 1).ToString("D2");
                pedestal.transform.SetParent(root, false);
                pedestal.transform.position = position + Vector3.down * .2f;
                pedestal.transform.localScale = new Vector3(5.2f, .4f, 5.2f);
                pedestal.GetComponent<MeshRenderer>().sharedMaterial = palette["sample_pedestal"];
                SetPreviewLayer(pedestal);
                Object.DestroyImmediate(pedestal.GetComponent<Collider>());
                float elevation = order[i].StartsWith("cliff_") ? 3.23f
                    : order[i].StartsWith("floor_") ? .38f : .03f;
                Place("tile_" + order[i], root, position + Vector3.up * elevation);
                Label((i + 1).ToString("D2") + "  " + labels[i], root,
                    position + new Vector3(0, .012f, -2.33f), .065f);
            }
            Label("PIXEL CAVE / TILE LIBRARY", root, new Vector3(9, .02f, -5.2f), .14f);
            SetPreviewLayer(root.gameObject, LibraryLayer);
        }

        private static void Assembly(TilePreset earth, TilePreset moss)
        {
            var root = Group("02 Connected tile sample");
            for (int z = 0; z < 8; z++)
                for (int x = 0; x < 9; x++)
                    Place(x < 3 ? "tile_floor_moss" : "tile_floor_earth", root, new Vector3(38 + x * 4, 0, z * 4));
            var island = new HashSet<Vector2>();
            for (int z = 3; z <= 6; z++)
                for (int x = 3; x <= 7; x++)
                    if (!(x == 3 && z >= 5) && !(x == 7 && z == 3)) island.Add(new Vector2(x, z));
            TileWorld("Earth plateau", new Vector3(38, 0, 0), island, earth);
            TileWorld("Moss plateau", new Vector3(38, 0, 0), new HashSet<Vector2>
                { new Vector2(0, 4), new Vector2(1, 4), new Vector2(0, 5), new Vector2(1, 5), new Vector2(0, 6) }, moss);
            Place("tile_ramp", root, new Vector3(54, 0, 4));
            Place("tile_ramp", root, new Vector3(54, 1.6f, 8));
            for (int i = 0; i < 6; i++) Place("tile_wall_straight", root, new Vector3(44 + i * 4, 0, 29));
            Place("tile_wall_end", root, new Vector3(40, 0, 29), 180);
            Place("tile_wall_corner", root, new Vector3(68, 0, 29), 180);
            Place("tile_rock_low", root, new Vector3(43, 0, 10));
            Place("tile_rock_low", root, new Vector3(68, 0, 13), 90);
            Place("tile_rock_tall", root, new Vector3(46, 0, 27), 35);
            Place("tile_rock_tall", root, new Vector3(68, 0, 24), 140);
            LightAt("Warm cave accent", new Vector3(47, 4.7f, 24), LightType.Point,
                new Color(1, .56f, .25f), 5, Vector3.zero);
        }

        private static void TileWorld(string name, Vector3 origin, HashSet<Vector2> cells, TilePreset preset)
        {
            var config = ScriptableObject.CreateInstance<Configuration>();
            config.name = name.Replace(" ", "_").ToLowerInvariant();
            config.cellSizeOld = 0;
            config.cellSize = config.lastCellSize = 4;
            config.width = 10; config.height = 9;
            config.clusterCellSize = 4;
            config.useGlobalRandomSeed = true; config.globalRandomSeed = 137;
            config.mergeTiles = true;
            config.colliderType = Configuration.ColliderType.meshCollider;
            config.useParallel = false;
            AssetDatabase.CreateAsset(config, Configs + "/" + config.name + ".asset");
            var root = Group("TileWorldCreator | " + name);
            root.position = origin;
            var manager = root.gameObject.AddComponent<TileWorldCreatorManager>();
            manager.configuration = config; manager.enabled = false;
            config.SetManager(manager);
            var blueprint = manager.AddNewBlueprintLayer(name);
            blueprint.defaultLayerHeight = 3.2f;
            blueprint.AddCells(cells);
            var layer = manager.AddNewBuildLayer<TilesBuildLayer>(name);
            layer.assignedBlueprintLayerGuid = blueprint.guid;
            layer.asset = config;
            layer.useDualGrid = true;
            layer.scaleTileToCellSize = false;
            layer.meshGenerationOverride = true;
            layer.mergeTiles = true;
            // TWC stores a layer index here despite the LayerMask field type.
            layer.objectLayer = PreviewLayer;
            layer.shadowCastingMode = ShadowCastingMode.On;
            layer.colliderType = Configuration.ColliderType.meshCollider;
            layer.tilePresetsTop.Add(new TilesBuildLayer.TilePresetSelection { preset = preset, weight = 1 });
            managers.Add(manager);
        }

        private static void LightAt(string name, Vector3 position, LightType type, Color color, float intensity, Vector3 angles)
        {
            var root = Group(name);
            root.position = position; root.rotation = Quaternion.Euler(angles);
            var light = root.gameObject.AddComponent<Light>();
            light.type = type; light.color = color; light.intensity = intensity;
            light.cullingMask = (1 << PreviewLayer) | (1 << LibraryLayer);
            light.range = 16;
            light.shadows = type == LightType.Directional && intensity > 1 ? LightShadows.Soft : LightShadows.None;
        }

        private static Camera CameraAt(string name, Vector3 target, Vector3 offset, float size, bool enabled)
        {
            var root = Group(name);
            root.position = target + offset;
            root.LookAt(target);
            var camera = root.gameObject.AddComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = size;
            camera.nearClipPlane = .1f; camera.farClipPlane = 160;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.035f, .05f, .07f);
            camera.cullingMask = 1 << (name == "Tile Library Camera" ? LibraryLayer : PreviewLayer);
            camera.enabled = enabled;
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false;
            data.antialiasing = AntialiasingMode.None;
            return camera;
        }

        private static void Finish()
        {
            try
            {
                if (managers.SelectMany(m => m.configuration.buildLayerFolders)
                    .SelectMany(f => f.buildLayers).Any(l => l.isExecuting))
                {
                    if (EditorApplication.timeSinceStartup - started > 90)
                        throw new TimeoutException("Tile preview generation timed out.");
                    return;
                }
                EditorApplication.update -= Finish;
                foreach (var manager in managers)
                {
                    SetPreviewLayer(manager.gameObject);
                    SaveMeshes(manager);
                    foreach (var layer in manager.configuration.blueprintLayerFolders.SelectMany(f => f.blueprintLayers))
                        EditorUtility.SetDirty(layer);
                    foreach (var layer in manager.configuration.buildLayerFolders.SelectMany(f => f.buildLayers))
                        EditorUtility.SetDirty(layer);
                    EditorUtility.SetDirty(manager.configuration);
                }
                var errors = palette.Values.Where(m => ShaderUtil.ShaderHasError(m.shader)).ToArray();
                if (errors.Length > 0) throw new InvalidOperationException("Tile material shader compilation failed.");
                Physics.SyncTransforms();
                foreach (var manager in managers)
                    foreach (var blueprint in manager.configuration.blueprintLayerFolders.SelectMany(folder => folder.blueprintLayers))
                        foreach (var cell in blueprint.GetAllCellPositions(new HashSet<Vector2>()))
                            foreach (var offset in new[] { Vector2.zero, new Vector2(.65f, .65f), new Vector2(-.65f, -.65f) })
                            {
                                var point = manager.transform.position + new Vector3(cell.x * 4 + offset.x, 12, cell.y * 4 + offset.y);
                                if (!Physics.Raycast(point, Vector3.down, out var ground, 20, 1 << PreviewLayer)
                                    || Mathf.Abs(ground.point.y - blueprint.defaultLayerHeight) > .03f)
                                    throw new InvalidOperationException("Tile surface has a gap near " + point);
                            }
                foreach (float z in new[] { 2.05f, 4, 5.95f, 6.05f, 8, 9.95f, 10.05f, 14 })
                {
                    float expected = z < 6 ? (z - 2) * .4f : z < 10 ? 1.6f + (z - 6) * .4f : 3.2f;
                    if (!Physics.Raycast(new Vector3(54, 12, z), Vector3.down, out var hit, 20, 1 << PreviewLayer)
                        || Mathf.Abs(hit.point.y - expected) > .03f)
                        throw new InvalidOperationException("Ramp connection has a gap at Z=" + z);
                }
                AssetDatabase.SaveAssets();
                EditorSceneManager.SaveScene(previewScene, ScenePath);
                Capture(libraryCamera, "pixel_cave_tiles_library.png", 1440, 1100);
                Capture(assemblyCamera, "pixel_cave_tiles_assembly.png", 1440, 1000);
                File.WriteAllText(Path.Combine(TaskFolder, "complete.txt"),
                    "16 source meshes; 21 prefabs; 2 dual-grid presets; saved TWC assembly and colliders.\n");
                Debug.Log("PIXEL_CAVE_TILES_SAVED: " + ScenePath);
            }
            catch (Exception error) { Fail(error); }
        }

        [MenuItem("Untitled/Cave/Save Pixel Tile Preview Meshes")]
        public static void SavePreviewMeshes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path != ScenePath)
                throw new InvalidOperationException("Open PixelCaveTilePreview outside Play Mode.");
            foreach (var manager in Object.FindObjectsOfType<TileWorldCreatorManager>(true))
                if (manager.gameObject.scene.path == ScenePath) SaveMeshes(manager);
            AssetDatabase.SaveAssets();
        }

        private static void SaveMeshes(TileWorldCreatorManager manager)
        {
            var copies = new Dictionary<Mesh, Mesh>();
            foreach (var filter in manager.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = filter.sharedMesh;
                if (mesh == null || AssetDatabase.Contains(mesh)) continue;
                string path = Models + "/meshes/" + manager.configuration.name + "_" +
                    filter.transform.parent.name + "_" + filter.name + ".asset";
                path = path.Replace(" ", "_").Replace("|", "_");
                var copy = Object.Instantiate(mesh);
                var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (saved != null)
                {
                    EditorUtility.CopySerialized(copy, saved);
                    Object.DestroyImmediate(copy);
                    copy = saved;
                }
                else AssetDatabase.CreateAsset(copy, path);
                copies[mesh] = copy;
                filter.sharedMesh = copy;
            }
            foreach (var collider in manager.GetComponentsInChildren<MeshCollider>())
                if (collider.sharedMesh != null && copies.TryGetValue(collider.sharedMesh, out var copy))
                    collider.sharedMesh = copy;
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        }

        private static void Capture(Camera camera, string name, int width, int height)
        {
            var otherLights = Object.FindObjectsOfType<Light>().Where(l => l.gameObject.scene != previewScene && l.enabled).ToArray();
            foreach (var light in otherLights) light.enabled = false;
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            Texture2D pixels = null;
            try
            {
                camera.aspect = width / (float)height;
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(Path.Combine(TaskFolder, name), pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                camera.ResetAspect();
                if (pixels != null) Object.DestroyImmediate(pixels);
                RenderTexture.ReleaseTemporary(target);
                foreach (var light in otherLights) light.enabled = true;
            }
        }

        private static void Fail(Exception error)
        {
            EditorApplication.update -= Finish;
            Directory.CreateDirectory(TaskFolder);
            File.WriteAllText(Path.Combine(TaskFolder, "error.txt"), error.ToString());
            Debug.LogException(error);
            if (previewScene.IsValid())
            {
                if (previousScene.IsValid()) SceneManager.SetActiveScene(previousScene);
                EditorSceneManager.CloseScene(previewScene, true);
            }
        }
    }
}
