using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GiantGrey.TileWorldCreator;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using StylizedWater2;
using Object = UnityEngine.Object;

namespace Untitled.EditorTools
{
    // Refines a copy of the saved map. Neither scene loading nor Play regenerates it.
    public static class CaveTileWorldRefinement
    {
        public const string ScenePath = "Assets/Scenes/CaveTileWorldRefined.unity";
        private const string SourceScene = "Assets/Scenes/CaveTileWorld.unity";
        private const string Configs = "Assets/Configs/CaveTileWorldRefined";
        private const string Models = "Assets/Art/Models/CaveTileWorldRefined";
        private const string Prefabs = "Assets/Prefabs/Terrain/CaveTileWorldRefined";
        private const string Materials = "Assets/Art/Materials/CaveTileWorldRefined";
        private const string MeshPath = Models + "/tile_clusters.asset";
        private const string TileFolder = "Assets/Prefabs/Terrain/PixelCaveTiles/";
        private const string ConnectorFolder = "Assets/Prefabs/Terrain/PixelCaveConnectors/";
        private const string WallFolder = "Assets/Prefabs/Terrain/PixelCaveWalls/";
        private static readonly string ReviewFolder = Path.Combine(Path.GetTempPath(), "untitled-cave-tileworld-refined");
        private static Scene scene;
        private static Transform terrain;
        private static Transform landform;
        private static TileWorldCreatorManager manager;
        private static Layout layout;
        private static MeshCollider[] floorColliders;
        private static Dictionary<Object, string> inherited;
        private static double started;
        private static int wallCount;
        private static int connectorCount;
        private static int rockCells;
        private static readonly Dictionary<string, HashSet<Vector2>> groundMasks = new Dictionary<string, HashSet<Vector2>>();
        private static readonly Dictionary<string, GameObject> tileVariants = new Dictionary<string, GameObject>();
        private static readonly Dictionary<string, Material> palette = new Dictionary<string, Material>();
        private static Transform fullTileRoot;
        private static readonly List<(Vector3 Point, float Height)> tileChecks = new List<(Vector3, float)>();
        private static readonly List<Bounds> connectionBounds = new List<Bounds>();
        private static (Vector3 Position, string Surface)[] originalPaving;

        [Serializable] private sealed class Point { public string Name; public float X, Z, Height; }
        [Serializable] private sealed class Zone { public int Id; public string Name; public float X, Z, Height, Width, Depth; }
        [Serializable] private sealed class Layout { public Point[] RouteSamples; public Zone[] Zones; }
        private sealed class Edge { public Vector3 A, B; public int Count; }

        [MenuItem("Untitled/Cave/Create Refined TileWorld Cave")]
        public static void Create()
        {
            if (File.Exists(ScenePath)) throw new InvalidOperationException("The saved full tile map already exists.");
            BuildEntireMapFromKit();
        }

        // Used for this explicit correction of the agent-authored scene, never on scene load.
        public static void BuildEntireMapFromKit()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (string path in new[] { Configs, Models, Prefabs, Materials, ReviewFolder }) Directory.CreateDirectory(path);
            File.Copy(SourceScene, ScenePath, true);
            AssetDatabase.ImportAsset(ScenePath);
            scene = EditorSceneManager.OpenScene(ScenePath);
            terrain = scene.GetRootGameObjects().Single(g => g.name.StartsWith("CaveTerrain")).transform;
            landform = terrain.GetComponentsInChildren<Transform>().Single(t => t.name.StartsWith("Curved boundaries"));
            var sourceManager = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TileWorldCreatorManager>(true)).Single();
            originalPaving = sourceManager.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.StartsWith("cave_") && t.name.EndsWith("(Clone)") && !t.name.StartsWith("cave_tile_"))
                .Select(t => (t.position, t.name.Split('_')[1])).ToArray();
            layout = JsonUtility.FromJson<Layout>(File.ReadAllText("Assets/Art/Models/CaveTerracedV2/cave_terraced_layout.json"));
            var characterRoots = terrain.GetComponentsInChildren<CharacterController>(true).Select(c => c.transform).ToArray();
            inherited = InheritedObjects().Where(o => !(o is SpriteBillboard billboard)
                || characterRoots.Any(t => billboard.transform.IsChildOf(t))).ToDictionary(o => o, o => EditorJsonUtility.ToJson(o));
            tileVariants.Clear(); palette.Clear(); groundMasks.Clear(); tileChecks.Clear(); connectionBounds.Clear();
            wallCount = connectorCount = rockCells = 0;
            foreach (string name in new[] { "earth", "wet", "moss", "stone", "rock", "path", "wood" })
                palette.Add(name, AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/CaveTerracedV2/" + name + ".mat"));
            foreach (var pair in new[] { ("rock_shadow", .72f), ("rock_light", 1.12f) })
            {
                string path = Materials + "/" + pair.Item1 + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(palette["rock"]) { name = pair.Item1 };
                    Color color = material.GetColor("_BaseColor");
                    material.SetColor("_BaseColor", new Color(color.r * pair.Item2, color.g * pair.Item2, color.b * pair.Item2, color.a));
                    AssetDatabase.CreateAsset(material, path);
                }
                palette.Add(pair.Item1, material);
            }
            palette.Add("gravel", palette["wet"]); palette.Add("planks", palette["wood"]); palette.Add("rim", palette["earth"]);
            Physics.SyncTransforms();
            floorColliders = landform.GetComponentsInChildren<MeshCollider>().Where(IsFloor)
                .Concat(sourceManager.GetComponentsInChildren<MeshCollider>())
                .Where(c => c.enabled && c.gameObject.activeInHierarchy).ToArray();
            fullTileRoot = Group("FullTileTerrain | four requested kits only");
            Bounds bounds = floorColliders[0].bounds;
            foreach (var c in floorColliders.Skip(1)) bounds.Encapsulate(c.bounds);
            Vector3 origin = new Vector3(Mathf.Floor(bounds.min.x / 4) * 4 - 4, 0, Mathf.Floor(bounds.min.z / 4) * 4 - 4);
            var config = ScriptableObject.CreateInstance<Configuration>();
            config.name = "cave_full_tile_configuration"; config.cellSizeOld = 0; config.cellSize = config.lastCellSize = 4;
            config.width = Mathf.CeilToInt((bounds.max.x - origin.x) / 4) + 2;
            config.height = Mathf.CeilToInt((bounds.max.z - origin.z) / 4) + 2;
            config.clusterCellSize = 8; config.useParallel = false; config.mergeTiles = true;
            config.useGlobalRandomSeed = true; config.globalRandomSeed = 731;
            config.colliderType = Configuration.ColliderType.meshCollider;
            string configPath = Configs + "/cave_full_tile_configuration.asset";
            if (AssetDatabase.LoadMainAssetAtPath(configPath) != null) AssetDatabase.DeleteAsset(configPath);
            AssetDatabase.CreateAsset(config, configPath);
            var go = new GameObject("TileWorldCreator | complete tiled reference map");
            SceneManager.MoveGameObjectToScene(go, scene); go.transform.position = origin;
            manager = go.AddComponent<TileWorldCreatorManager>(); manager.configuration = config; manager.enabled = false; config.SetManager(manager);
            BuildFullGround();
            AddConnectedRocks();
            AddCurvedWallFacing();
            BuildTiledRoutes();
            BuildTiledBridges();
            CopyWaterSurface();
            ReplaceDetailRocks();
            Reservations();
            RemoveLegacyGeometry(sourceManager);
            BuildTileBoundaries();
            EditorUtility.SetDirty(config); AssetDatabase.SaveAssetIfDirty(config);
            started = EditorApplication.timeSinceStartup;
            manager.ExecuteBlueprintLayers(); manager.ExecuteBuildLayers(ExecutionMode.FromScratch);
            EditorApplication.update += FinishFullTiles;
        }

        private static Dictionary<string, Point[]> SlopingRoutes() => layout.RouteSamples.GroupBy(p => p.Name)
            .Where(g => g.Max(p => p.Height) - g.Min(p => p.Height) > .2f).ToDictionary(g => g.Key, g => g.ToArray());

        private static void BuildFullGround()
        {
            var slopes = SlopingRoutes().Values.SelectMany(p => p).ToArray();
            var bridges = BridgeBounds();
            var groups = new Dictionary<string, (float Height, string Surface, HashSet<Vector2> Cells)>();
            for (int x = 0; x < manager.configuration.width; x++)
                for (int z = 0; z < manager.configuration.height; z++)
                {
                    Vector3 p = manager.transform.position + new Vector3(x * 4, 0, z * 4);
                    if (OverBridge(p, bridges) || slopes.Any(q => Vector2.Distance(new Vector2(p.x - terrain.position.x, p.z - terrain.position.z), new Vector2(q.X, q.Z)) < 2.1f)) continue;
                    var ray = new Ray(p + Vector3.up * 40, Vector3.down);
                    RaycastHit best = default; bool found = false;
                    foreach (var collider in floorColliders)
                        if (collider.Raycast(ray, out var hit, 60) && hit.normal.y > .98f && (!found || hit.point.y > best.point.y)) { best = hit; found = true; }
                    if (!found) continue;
                    float raw = best.point.y;
                    float height = raw < -1 ? -2.4f : raw > 8 ? 9.6f : raw > 5 ? 6 : raw > 3.5f ? 4.2f : raw > 2.7f ? 2.85f : 2.4f;
                    var renderer = best.collider.GetComponent<MeshRenderer>();
                    string surface = renderer != null ? renderer.sharedMaterials[0].name : "earth";
                    if (renderer == null)
                        for (var parent = best.collider.transform; parent != null; parent = parent.parent)
                        {
                            if (parent.name.Contains("wet_")) { surface = "wet"; break; }
                            if (parent.name.Contains("moss_")) { surface = "moss"; break; }
                            if (parent.name.Contains("stone_")) { surface = "stone"; break; }
                        }
                    if (!new[] { "wet", "moss", "stone", "earth" }.Contains(surface)) surface = height < 0 ? "wet" : "earth";
                    if (height < 0 && surface != "moss") surface = "wet";
                    Vector3 local = p - terrain.position;
                    if (layout.Zones.Where(q => new[] { 3, 4, 5, 6, 7, 9, 10, 12 }.Contains(q.Id)).Any(q =>
                        Mathf.Abs(local.x - q.X) < q.Width * .5f - 1 && Mathf.Abs(local.z - q.Z) < q.Depth * .5f - 1)) surface = "stone";
                    string key = surface + "_" + height.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                    if (!groups.TryGetValue(key, out var group)) { group = (height, surface, new HashSet<Vector2>()); groups.Add(key, group); }
                    group.Cells.Add(new Vector2(x, z));
                }
            // Saved generated tile positions are authoritative even if a collider has no Renderer
            // or cannot be resampled at an imported mesh boundary.
            foreach (var tile in originalPaving)
            {
                Vector3 p = tile.Position;
                if (OverBridge(p, bridges) || slopes.Any(q => Vector2.Distance(new Vector2(p.x - terrain.position.x, p.z - terrain.position.z), new Vector2(q.X, q.Z)) < 2.1f)) continue;
                Vector2 cell = new Vector2(Mathf.Round((p.x - manager.transform.position.x) / 4), Mathf.Round((p.z - manager.transform.position.z) / 4));
                float height = p.y < -1 ? -2.4f : p.y > 8 ? 9.6f : p.y > 2.7f ? 2.85f : 2.4f;
                string surface = tile.Surface;
                Vector3 local = p - terrain.position;
                if (layout.Zones.Where(q => new[] { 3, 4, 5, 6, 7, 9, 10, 12 }.Contains(q.Id)).Any(q =>
                    Mathf.Abs(local.x - q.X) < q.Width * .5f - 1 && Mathf.Abs(local.z - q.Z) < q.Depth * .5f - 1)) surface = "stone";
                foreach (var group in groups.Values) group.Cells.Remove(cell);
                string key = surface + "_" + height.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                if (!groups.TryGetValue(key, out var target)) { target = (height, surface, new HashSet<Vector2>()); groups.Add(key, target); }
                target.Cells.Add(cell);
            }
            foreach (var pair in groups.Where(p => p.Value.Cells.Count > 0))
            {
                var data = pair.Value;
                var preset = ScriptableObject.CreateInstance<TilePreset>(); preset.name = "full_" + pair.Key; preset.gridtype = TilePreset.GridType.dual;
                var roles = new Dictionary<string, string> { { "DUALGRD_fillTile", "fill" }, { "DUALGRD_edgeTile", "edge" },
                    { "DUALGRD_cornerTile", "outer_corner" }, { "DUALGRD_invertedCornerTile", "inner_corner" }, { "DUALGRD_doubleInteriorCornerTile", "double_corner" } };
                foreach (var role in roles) typeof(TilePreset).GetField(role.Key).SetValue(preset, SurfaceVariant(TileFolder + "tile_cliff_" + role.Value + ".prefab", data.Surface));
                AssetDatabase.CreateAsset(preset, Configs + "/" + preset.name + ".asset");
                var blueprint = manager.AddNewBlueprintLayer(pair.Key); blueprint.defaultLayerHeight = data.Height; blueprint.AddCells(data.Cells);
                groundMasks.Add(pair.Key, data.Cells);
                var build = manager.AddNewBuildLayer<TilesBuildLayer>(pair.Key);
                build.asset = manager.configuration; build.assignedBlueprintLayerGuid = blueprint.guid;
                build.useDualGrid = true; build.scaleTileToCellSize = false;
                float depth = data.Height == 9.6f ? 7.2f : data.Height == 6 ? 3.6f : data.Height == 4.2f ? 1.8f : data.Height == 2.85f ? .45f : data.Height == 2.4f ? 4.8f : 3.2f;
                build.scaleOffset = new Vector3(1, depth / 3.2f, 1);
                build.meshGenerationOverride = true; build.mergeTiles = true; build.colliderType = Configuration.ColliderType.meshCollider;
                build.tilePresetsTop.Add(new TilesBuildLayer.TilePresetSelection { preset = preset, weight = 1 });
                EditorUtility.SetDirty(blueprint); EditorUtility.SetDirty(build);
            }
        }

        private static GameObject SurfaceVariant(string path, string surface)
        {
            string key = path + "|" + surface;
            if (tileVariants.TryGetValue(key, out var saved)) return saved;
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var copy = (GameObject)PrefabUtility.InstantiatePrefab(source, scene);
            foreach (var renderer in copy.GetComponentsInChildren<MeshRenderer>())
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m => m.name == "earth" || m.name == "rim" ? palette[surface] : palette.TryGetValue(m.name, out var reference) ? reference : m).ToArray();
            saved = PrefabUtility.SaveAsPrefabAsset(copy, Prefabs + "/" + source.name + "_" + surface + ".prefab");
            Object.DestroyImmediate(copy); tileVariants.Add(key, saved); return saved;
        }

        private static void BuildTiledRoutes()
        {
            var root = Group("TileRamps | original winding ascent and river approaches");
            root.SetParent(fullTileRoot, true);
            foreach (var pair in SlopingRoutes())
            {
                var points = pair.Value.Select(p => terrain.position + new Vector3(p.X, p.Height, p.Z)).ToArray();
                var anchors = new List<Vector3> { points[0] }; float accumulated = 0;
                for (int i = 1; i < points.Length; i++)
                {
                    Vector3 a = points[i - 1], b = points[i]; float distance = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
                    if (distance < .0001f) continue;
                    while (accumulated + distance >= 4)
                    {
                        float t = (4 - accumulated) / distance; a = Vector3.Lerp(a, b, t); anchors.Add(a);
                        distance = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z)); accumulated = 0;
                    }
                    accumulated += distance;
                }
                if (Vector3.Distance(anchors.Last(), points.Last()) > .1f) anchors.Add(points.Last());
                var group = new GameObject(pair.Key).transform; group.SetParent(root, false);
                for (int i = 1; i < anchors.Count; i++)
                {
                    Vector3 a = anchors[i - 1], b = anchors[i]; float rise = b.y - a.y;
                    Vector3 delta = b - a; delta.y = 0; float length = delta.magnitude;
                    if (length < .1f) continue;
                    bool slope = Mathf.Abs(rise) > .025f;
                    string tile = slope ? (Mathf.Abs(rise) < .6f ? "tile_ramp_045" : "tile_ramp_120") : "tile_floor_path";
                    Vector3 direction = rise < 0 ? -delta : delta;
                    float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                    var go = Place(ConnectorFolder + tile + ".prefab", group, new Vector3((a.x + b.x) * .5f, Mathf.Min(a.y, b.y), (a.z + b.z) * .5f), yaw);
                    go.transform.localScale = new Vector3(1.15f, slope ? Mathf.Abs(rise) / (tile.EndsWith("045") ? .45f : 1.2f) : 1, length / 4);
                    var collider = go.GetComponentInChildren<MeshCollider>(); connectionBounds.Add(collider.bounds);
                    for (int step = 0; step <= 4; step++) { Vector3 p = Vector3.Lerp(a, b, Mathf.Clamp(step / 4f, .01f, .99f)); tileChecks.Add((p, p.y)); }
                    connectorCount++;
                }
            }
        }

        private static void BuildTiledBridges()
        {
            var root = Group("TileBridges | two original crossings"); root.SetParent(fullTileRoot, true);
            foreach (var bounds in BridgeBounds())
            {
                float length = bounds.size.x + 8;
                int count = Mathf.CeilToInt(length / 4);
                float size = length / count;
                for (int i = 0; i < count; i++)
                {
                    Vector3 p = new Vector3(bounds.min.x - 4 + size * (i + .5f), bounds.max.y, bounds.center.z);
                    var tile = Place(ConnectorFolder + (i == 0 || i == count - 1 ? "tile_bridge_landing.prefab" : "tile_bridge_deck.prefab"), root, p, i == 0 ? 90 : -90);
                    tile.transform.localScale = new Vector3(bounds.size.z / 4, 1, size / 4);
                    connectionBounds.Add(tile.GetComponentInChildren<MeshCollider>().bounds);
                    tileChecks.Add((p, p.y)); connectorCount++;
                }
            }
        }

        private static void CopyWaterSurface()
        {
            var source = landform.GetComponentsInChildren<WaterObject>().Single();
            var sample = source.GetComponent<MeshCollider>();
            if (sample == null) sample = source.gameObject.AddComponent<MeshCollider>();
            sample.sharedMesh = source.meshFilter.sharedMesh;
            Physics.SyncTransforms();
            var cells = new HashSet<Vector2>();
            for (int x = 0; x < manager.configuration.width; x++)
                for (int z = 0; z < manager.configuration.height; z++)
                {
                    var ray = new Ray(manager.transform.position + new Vector3(x * 4, 40, z * 4), Vector3.down);
                    if (sample.Raycast(ray, out _, 60)) cells.Add(new Vector2(x, z));
                }
            var material = new Material(source.material) { name = "full_tile_river" };
            material.SetFloat("_WorldSpaceUV", 1);
            AssetDatabase.CreateAsset(material, Materials + "/full_tile_river.mat");
            var copy = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(TileFolder + "tile_floor_wet.prefab"), scene);
            foreach (var renderer in copy.GetComponentsInChildren<MeshRenderer>())
                renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
            var prefab = PrefabUtility.SaveAsPrefabAsset(copy, Prefabs + "/tile_water_wet.prefab"); Object.DestroyImmediate(copy);
            var preset = ScriptableObject.CreateInstance<TilePreset>(); preset.name = "full_water"; preset.gridtype = TilePreset.GridType.standard;
            foreach (var field in typeof(TilePreset).GetFields().Where(f => f.Name.StartsWith("NRMGRD_") && f.FieldType == typeof(GameObject))) field.SetValue(preset, prefab);
            AssetDatabase.CreateAsset(preset, Configs + "/full_water.asset");
            var blueprint = manager.AddNewBlueprintLayer("River water | wet tiles and original channel mask");
            blueprint.defaultLayerHeight = source.meshRenderer.bounds.center.y; blueprint.AddCells(cells);
            var build = manager.AddNewBuildLayer<TilesBuildLayer>(blueprint.layerName);
            build.asset = manager.configuration; build.assignedBlueprintLayerGuid = blueprint.guid;
            build.useDualGrid = false; build.scaleTileToCellSize = false;
            build.meshGenerationOverride = true; build.mergeTiles = true; build.colliderType = Configuration.ColliderType.none;
            build.shadowCastingMode = ShadowCastingMode.Off;
            build.tilePresetsTop.Add(new TilesBuildLayer.TilePresetSelection { preset = preset, weight = 1 });
            EditorUtility.SetDirty(blueprint); EditorUtility.SetDirty(build);
        }

        private static void ReplaceDetailRocks()
        {
            var root = Group("TileRockDetails | reference positions"); root.SetParent(fullTileRoot, true);
            foreach (var renderer in landform.GetComponentsInChildren<MeshRenderer>().Where(r => r.name.StartsWith("InteriorRockSpur") || r.name.StartsWith("LooseFloorStone") || r.name.StartsWith("FoothillWeatheredCrag")))
            {
                Vector3 p = renderer.bounds.center;
                if (!ClearSpace(p, 1.5f)) continue;
                float height = Ground(p); if (float.IsNaN(height)) continue; p.y = height;
                Place(TileFolder + (renderer.bounds.size.y > 2 ? "tile_rock_tall.prefab" : "tile_rock_low.prefab"), root, p, renderer.transform.eulerAngles.y);
            }
            foreach (var renderer in landform.GetComponentsInChildren<MeshRenderer>().Where(r => r.name.StartsWith("CaveWallMass_")))
            {
                Vector3 p = renderer.bounds.center;
                if (!ClearSpace(p, 3)) continue;
                float bottom = p.x < terrain.position.x - 35 ? -2.4f : 2.4f;
                p.y = bottom + Mathf.Max(1, Mathf.Floor((renderer.bounds.max.y - bottom) / 4.8f)) * 4.8f;
                Place(TileFolder + "tile_rock_tall.prefab", root, p, renderer.transform.eulerAngles.y);
            }
        }

        private static void RemoveLegacyGeometry(TileWorldCreatorManager oldManager)
        {
            var remove = new List<GameObject> { landform.gameObject, oldManager.gameObject };
            remove.AddRange(scene.GetRootGameObjects().Where(g => g.name.StartsWith("SettlementBuildings") || g.name.StartsWith("SettlementGameplay")));
            remove.AddRange(terrain.Cast<Transform>().Where(t => t.name.StartsWith("Terrain Details") || t.name.StartsWith("Collision |") || t.name.StartsWith("Wayfinding")).Select(t => t.gameObject));
            foreach (var root in remove)
            {
                foreach (var camera in root.GetComponentsInChildren<Camera>(true)) camera.transform.SetParent(terrain, true);
                foreach (var light in root.GetComponentsInChildren<Light>(true))
                {
                    var go = new GameObject(light.name); go.transform.SetParent(terrain, false); go.transform.SetPositionAndRotation(light.transform.position, light.transform.rotation);
                    EditorUtility.CopySerialized(light, go.AddComponent<Light>());
                }
                Object.DestroyImmediate(root);
            }
            foreach (var originalTerrain in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Terrain>(true)).ToArray())
                Object.DestroyImmediate(originalTerrain.gameObject);
        }

        private static void BuildTileBoundaries()
        {
            var root = Group("Collision | tile shoreline and void");
            foreach (var blueprint in manager.configuration.blueprintLayerFolders.SelectMany(f => f.blueprintLayers).Where(b => groundMasks.ContainsKey(b.layerName)))
            {
                var cells = groundMasks[blueprint.layerName];
                foreach (var cell in cells)
                    foreach (var direction in new[] { Vector2.left, Vector2.right, Vector2.up, Vector2.down })
                    {
                        if (cells.Contains(cell + direction) || groundMasks.Any(p => p.Key.EndsWith(blueprint.layerName.Substring(blueprint.layerName.LastIndexOf('_'))) && p.Value.Contains(cell + direction))) continue;
                        Vector3 p = manager.transform.position + new Vector3(cell.x * 4 + direction.x * 2, blueprint.defaultLayerHeight + 1.2f, cell.y * 4 + direction.y * 2);
                        if (connectionBounds.Any(b => p.x >= b.min.x - 1 && p.x <= b.max.x + 1 && p.z >= b.min.z - 1 && p.z <= b.max.z + 1)) continue;
                        var go = new GameObject("Tile edge barrier"); go.transform.SetParent(root, true); go.transform.position = p;
                        go.AddComponent<BoxCollider>().size = direction.x == 0 ? new Vector3(4.05f, 2.4f, .1f) : new Vector3(.1f, 2.4f, 4.05f);
                    }
            }
        }

        private static void FinishFullTiles()
        {
            try
            {
                if (manager.configuration.buildLayerFolders.SelectMany(f => f.buildLayers).Any(l => l.isExecuting))
                {
                    if (EditorApplication.timeSinceStartup - started > 180) throw new TimeoutException("Full tile generation timed out.");
                    return;
                }
                EditorApplication.update -= FinishFullTiles;
                PersistMeshes(); Physics.SyncTransforms();
                foreach (var pair in inherited)
                    if (pair.Key == null || EditorJsonUtility.ToJson(pair.Key) != pair.Value) throw new InvalidOperationException("Inherited runtime object changed: " + pair.Key);
                var colliders = fullTileRoot.GetComponentsInChildren<MeshCollider>().Concat(manager.GetComponentsInChildren<MeshCollider>()).ToArray();
                foreach (var check in tileChecks)
                {
                    var ray = new Ray(check.Point + Vector3.up * 30, Vector3.down);
                    if (!colliders.Any(c => c.Raycast(ray, out var hit, 35) && Mathf.Abs(hit.point.y - check.Height) < .12f))
                        throw new InvalidOperationException("Tile connection gap near " + check.Point);
                }
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(manager.configuration))) EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssetIfDirty(manager.configuration); AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath(MeshPath));
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                CaptureReviews();
                File.WriteAllText(Path.Combine(ReviewFolder, "complete.txt"), "FULL TILE MAP: source landform, old walls, old bridges, old scenery and old room geometry removed.\n12 original cameras, player and debug inherited.\n"
                    + wallCount + " wall modules; " + rockCells + " connected rock cells; " + connectorCount + " bridge/ramp tiles; " + tileChecks.Count + " connection samples passed.\n"
                    + string.Join(", ", groundMasks.Select(p => p.Key + "=" + p.Value.Count)) + "\n");
                Debug.Log("CAVE_FULL_TILE_MAP_SAVED: " + ScenePath);
            }
            catch (Exception error)
            {
                EditorApplication.update -= FinishFullTiles;
                File.WriteAllText(Path.Combine(ReviewFolder, "full_error.txt"), error.ToString()); Debug.LogException(error);
            }
        }

        private static IEnumerable<Object> InheritedObjects()
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null) continue;
                    string type = component.GetType().Name;
                    if (component is Camera || component is CharacterController || type.Contains("Debug")
                        || type.Contains("Cinemachine") || type == "CameraController" || type == "PlayerEntity"
                        || type == "Bootstrap" || type == "SpriteBillboard") yield return component;
                }
        }

        private static GameObject TileVariant(string path)
        {
            if (tileVariants.TryGetValue(path, out var saved)) return saved;
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (source == null) throw new InvalidOperationException("Missing requested kit asset: " + path);
            var copy = (GameObject)PrefabUtility.InstantiatePrefab(source, scene);
            foreach (var renderer in copy.GetComponentsInChildren<MeshRenderer>())
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m => palette.TryGetValue(m.name, out var reference) ? reference : m).ToArray();
            saved = PrefabUtility.SaveAsPrefabAsset(copy, Prefabs + "/" + source.name + ".prefab");
            Object.DestroyImmediate(copy); tileVariants.Add(path, saved);
            return saved;
        }

        private static Transform Group(string name)
        {
            var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.SetParent(terrain, false);
            return go.transform;
        }

        private static bool ClearSpace(Vector3 world, float margin)
        {
            Vector3 p = world - terrain.position;
            return !layout.Zones.Any(z => Mathf.Abs(p.x - z.X) < z.Width * .5f + margin && Mathf.Abs(p.z - z.Z) < z.Depth * .5f + margin)
                && !layout.RouteSamples.Any(r => Vector2.Distance(new Vector2(p.x, p.z), new Vector2(r.X, r.Z)) < margin + 2.1f);
        }

        private static void Reservations()
        {
            var root = Group("BuildingReservations | original 15 districts");
            foreach (var zone in layout.Zones)
            {
                var go = new GameObject(zone.Id.ToString("D2") + "_" + zone.Name + " | keep footprint clear");
                go.transform.SetParent(root, false);
                go.transform.localPosition = new Vector3(zone.X, zone.Height, zone.Z);
                // Disabled editor selection outlines, never invisible obstacles during Play.
                var box = go.AddComponent<BoxCollider>();
                box.size = new Vector3(zone.Width, .15f, zone.Depth); box.isTrigger = true; box.enabled = false;
            }
        }

        private static bool IsFloor(MeshCollider collider)
        {
            var renderer = collider.GetComponent<MeshRenderer>();
            if (renderer == null) return false;
            return renderer.sharedMaterials.Any(m => m != null && new[] { "earth", "wet", "moss", "stone", "path", "wood" }.Contains(m.name));
        }

        private static float Ground(Vector3 world)
        {
            var ray = new Ray(new Vector3(world.x, 40, world.z), Vector3.down);
            float height = float.NaN;
            foreach (var collider in floorColliders)
                if (collider != null && collider.Raycast(ray, out var hit, 60) && hit.normal.y > .96f
                    && (float.IsNaN(height) || hit.point.y > height)) height = hit.point.y;
            return height;
        }

        private static void AddConnectedRocks()
        {
            var rocks = landform.GetComponentsInChildren<MeshCollider>().Where(c => c.name.StartsWith("ContinuousCaveRock")
                || c.name.StartsWith("CaveWallMass_")).ToArray();
            var layers = new Dictionary<float, HashSet<Vector2>>();
            float cellSize = manager.configuration.cellSize;
            for (int x = 0; x < manager.configuration.width; x++)
                for (int z = 0; z < manager.configuration.height; z++)
                {
                    Vector3 world = manager.transform.position + new Vector3(x * cellSize, 0, z * cellSize);
                    if (!ClearSpace(world, 3)) continue;
                    float roof = float.PositiveInfinity;
                    bool valid = true;
                    foreach (var offset in new[] { Vector2.zero, new Vector2(-1.9f, -1.9f), new Vector2(1.9f, -1.9f),
                        new Vector2(-1.9f, 1.9f), new Vector2(1.9f, 1.9f) })
                    {
                        var ray = new Ray(world + new Vector3(offset.x, 40, offset.y), Vector3.down);
                        float top = float.NegativeInfinity;
                        foreach (var rock in rocks)
                            if (rock.Raycast(ray, out var hit, 60)) top = Mathf.Max(top, hit.point.y);
                        if (float.IsNegativeInfinity(top)) { valid = false; break; }
                        roof = Mathf.Min(roof, top);
                    }
                    if (!valid) continue;
                    float ground = world.x < terrain.position.x - 35 ? -2.4f : 2.4f;
                    // Existing curved masses retain their outer silhouette and irregular caps.
                    // These continuous tile tiers fill their interior, never inventing a new perimeter.
                    for (float top = ground + 4.8f; top <= roof + .05f; top += 4.8f)
                    {
                        if (!layers.TryGetValue(top, out var cells)) { cells = new HashSet<Vector2>(); layers.Add(top, cells); }
                        cells.Add(new Vector2(x, z));
                    }
                    rockCells++;
                }
            var preset = Object.Instantiate(AssetDatabase.LoadAssetAtPath<TilePreset>("Assets/Configs/PixelCaveConnectedRocks/pixel_rock_cliffs_480.asset"));
            preset.name = "refined_connected_rock_480";
            foreach (var field in typeof(TilePreset).GetFields().Where(f => f.Name.StartsWith("DUALGRD_") && f.FieldType == typeof(GameObject)))
            {
                var source = (GameObject)field.GetValue(preset);
                if (source != null) field.SetValue(preset, TileVariant(AssetDatabase.GetAssetPath(source)));
            }
            AssetDatabase.CreateAsset(preset, Configs + "/" + preset.name + ".asset");
            foreach (var pair in layers.OrderBy(p => p.Key))
            {
                var blueprint = manager.AddNewBlueprintLayer("Connected rock strata " + pair.Key.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
                blueprint.defaultLayerHeight = pair.Key; blueprint.AddCells(pair.Value);
                blueprint.layerColor = new Color(.27f, .32f, .35f);
                var build = manager.AddNewBuildLayer<TilesBuildLayer>(blueprint.layerName);
                build.asset = manager.configuration; build.assignedBlueprintLayerGuid = blueprint.guid;
                build.useDualGrid = true; build.scaleTileToCellSize = false;
                build.meshGenerationOverride = true; build.mergeTiles = true;
                build.colliderType = Configuration.ColliderType.meshCollider;
                build.tilePresetsTop.Add(new TilesBuildLayer.TilePresetSelection { preset = preset, weight = 1 });
                EditorUtility.SetDirty(blueprint); EditorUtility.SetDirty(build);
            }
        }

        private static string VertexKey(Vector3 v) => Mathf.RoundToInt(v.x * 100) + ":" + Mathf.RoundToInt(v.z * 100);

        private static void AddCurvedWallFacing()
        {
            var root = Group("PixelRockWalls | facing original curved cave rim");
            var positions = new List<Vector3>();
            foreach (var filter in landform.GetComponentsInChildren<MeshFilter>().Where(f => f.name.StartsWith("ContinuousCaveRock")))
            {
                var edges = new Dictionary<string, Edge>();
                var vertices = filter.sharedMesh.vertices.Select(filter.transform.TransformPoint).ToArray();
                var indices = filter.sharedMesh.triangles;
                for (int i = 0; i < indices.Length; i += 3)
                {
                    Vector3 a = vertices[indices[i]], b = vertices[indices[i + 1]], c = vertices[indices[i + 2]];
                    if (Vector3.Cross(b - a, c - a).normalized.y < .7f) continue;
                    foreach (var pair in new[] { (a, b), (b, c), (c, a) })
                    {
                        string ka = VertexKey(pair.Item1), kb = VertexKey(pair.Item2);
                        string key = string.CompareOrdinal(ka, kb) < 0 ? ka + "|" + kb : kb + "|" + ka;
                        if (edges.TryGetValue(key, out var edge)) edge.Count++;
                        else edges.Add(key, new Edge { A = pair.Item1, B = pair.Item2, Count = 1 });
                    }
                }
                foreach (var edge in edges.Values.Where(e => e.Count == 1))
                {
                    Vector3 tangent = edge.B - edge.A; tangent.y = 0;
                    if (tangent.sqrMagnitude < .05f) continue;
                    Vector3 p = (edge.A + edge.B) * .5f;
                    Vector3 inward = terrain.position + new Vector3(4, 0, 0) - p; inward.y = 0; inward.Normalize();
                    float height = float.NaN;
                    for (float inset = .6f; inset <= 4 && float.IsNaN(height); inset += .6f)
                    {
                        p = (edge.A + edge.B) * .5f + inward * inset;
                        height = Ground(p);
                    }
                    if (float.IsNaN(height) || height > 10 || !ClearSpace(p, 3)
                        || positions.Any(q => Vector2.Distance(new Vector2(p.x, p.z), new Vector2(q.x, q.z)) < 3.85f)) continue;
                    p.y = height;
                    float yaw = Mathf.Atan2(-tangent.z, tangent.x) * Mathf.Rad2Deg;
                    if (Vector3.Dot(Quaternion.Euler(0, yaw, 0) * Vector3.back, inward) < 0) yaw += 180;
                    string tile = wallCount % 13 == 12 ? "tile_wall_moss_480" : wallCount % 5 == 4 ? "tile_wall_broken_480" : "tile_wall_straight_480";
                    Place(WallFolder + tile + ".prefab", root, p, yaw);
                    positions.Add(p); wallCount++;
                }
            }
            if (wallCount == 0) throw new InvalidOperationException("No original rim locations accepted wall modules.");
        }

        private static GameObject Place(string path, Transform root, Vector3 world, float yaw = 0)
        {
            var prefab = TileVariant(path);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            go.transform.SetParent(root, true); go.transform.SetPositionAndRotation(world, Quaternion.Euler(0, yaw, 0));
            return go;
        }

        private static Bounds[] BridgeBounds()
        {
            var result = new List<Bounds>();
            foreach (string name in new[] { "EntranceBridge_Plank_", "SupplyBridge_Plank_" })
            {
                var renderers = landform.GetComponentsInChildren<MeshRenderer>().Where(r => r.name.StartsWith(name)).ToArray();
                if (renderers.Length == 0) continue;
                Bounds bounds = renderers[0].bounds;
                foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                result.Add(bounds);
            }
            return result.ToArray();
        }

        private static bool OverBridge(Vector3 p, Bounds[] bridges) => bridges.Any(b =>
            Mathf.Abs(p.x - b.center.x) < b.extents.x + 2 && Mathf.Abs(p.z - b.center.z) < b.extents.z + 2);

        private static void PersistMeshes()
        {
            var copies = new Dictionary<Mesh, Mesh>();
            var existing = AssetDatabase.LoadAllAssetsAtPath(MeshPath).OfType<Mesh>().ToDictionary(m => m.name);
            foreach (var filter in manager.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = filter.sharedMesh;
                if (mesh == null || AssetDatabase.Contains(mesh)) continue;
                if (!copies.TryGetValue(mesh, out var saved))
                {
                    string name = filter.transform.parent.name + "_" + filter.name;
                    var copy = Object.Instantiate(mesh); copy.name = name;
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
                    copies.Add(mesh, saved);
                }
                filter.sharedMesh = saved;
            }
            foreach (var collider in manager.GetComponentsInChildren<MeshCollider>())
                if (collider.sharedMesh != null && copies.TryGetValue(collider.sharedMesh, out var saved)) collider.sharedMesh = saved;
        }

        private static void CaptureReviews()
        {
            var cameras = terrain.GetComponentsInChildren<Camera>();
            var profiles = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Volume>())
                .Where(v => v.sharedProfile != null).Select(v => v.profile).Distinct().ToArray();
            var depths = profiles.SelectMany(p => p.components).OfType<DepthOfField>().ToDictionary(d => d, d => d.active);
            bool fog = RenderSettings.fog;
            try
            {
                foreach (var depth in depths.Keys) depth.active = false;
                foreach (var camera in cameras.Where(c => c.name.StartsWith("Top Camera") || c.name.StartsWith("River Camera")))
                {
                    bool top = camera.name.StartsWith("Top Camera"); RenderSettings.fog = top ? false : fog;
                    var target = RenderTexture.GetTemporary(top ? 1440 : 1600, top ? 1600 : 900, 24, RenderTextureFormat.ARGB32);
                    var previous = RenderTexture.active; Texture2D pixels = null;
                    try
                    {
                        camera.aspect = (float)target.width / target.height;
                        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                        RenderTexture.active = target; pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                        pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply();
                        File.WriteAllBytes(Path.Combine(ReviewFolder, top ? "plan.png" : "river.png"), pixels.EncodeToPNG());
                    }
                    finally
                    {
                        RenderTexture.active = previous; camera.ResetAspect();
                        if (pixels != null) Object.DestroyImmediate(pixels); RenderTexture.ReleaseTemporary(target);
                    }
                }
            }
            finally { RenderSettings.fog = fog; foreach (var pair in depths) pair.Key.active = pair.Value; }
        }
    }
}
