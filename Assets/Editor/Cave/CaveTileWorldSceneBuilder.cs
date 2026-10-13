using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GiantGrey.TileWorldCreator;
using StylizedWater2;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Untitled.EditorTools
{
    /// <summary>Explicit authoring only. The saved scene never generates terrain in Play Mode.</summary>
    public static class CaveTileWorldSceneBuilder
    {
        private const string SourceScene = "Assets/Scenes/CaveTerracedTerrainV2.unity";
        public const string ScenePath = "Assets/Scenes/CaveTileWorld.unity";
        private const string Models = "Assets/Art/Models/CaveTileWorld";
        private const string Tiles = "Assets/Prefabs/Terrain/CaveTileWorld";
        private const string Configs = "Assets/Configs/CaveTileWorld";
        private const float Cell = 4;
        private static readonly Vector3 GridOrigin = new Vector3(-92, 0, 112);
        private static readonly string[] Roles = { "dead_end", "single", "fill", "corner_way", "corner_fill",
            "interior_corner", "double_corner", "edge_way", "edge_fill", "three_way", "three_way_fill",
            "edge_corner_fill", "three_corner", "four_way" };
        private static Scene scene;
        private static TileWorldCreatorManager manager;
        private static double started;
        private static bool batch;
        private static int paintedCells;

        private struct Vertex
        {
            public Vector3 P;
            public Vector2 UV;
            public static Vertex Lerp(Vertex a, Vertex b, float t) => new Vertex
                { P = Vector3.Lerp(a.P, b.P, t), UV = Vector2.Lerp(a.UV, b.UV, t) };
        }

        [MenuItem("Untitled/Cave/Create TileWorld Cave")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before authoring.");
            if (File.Exists(ScenePath))
                throw new InvalidOperationException("CaveTileWorld already exists. Edit its saved configuration and scene.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Begin();
        }

        public static void BatchCreate()
        {
            batch = true;
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Creation never overwrites a saved map.");
            Begin();
        }

        private static void Begin()
        {
            Directory.CreateDirectory(Tiles);
            Directory.CreateDirectory(Configs);
            Directory.CreateDirectory(Models + "/meshes");
            AssetDatabase.Refresh();
            AssetDatabase.CopyAsset(SourceScene, ScenePath);
            scene = EditorSceneManager.OpenScene(ScenePath);
            var terrain = scene.GetRootGameObjects().Single(g => g.name.StartsWith("CaveTerrain"));
            var landform = terrain.GetComponentsInChildren<Transform>().Single(t => t.name == "EditableLandform");
            ClearBuildingFootprints(terrain.transform);
            var topCamera=terrain.GetComponentsInChildren<Camera>().First(c=>c.name.StartsWith("Top Camera"));
            topCamera.transform.position=new Vector3(4,180,202);
            topCamera.orthographicSize=120;
            var presets = CreateTilePresets();
            var config = ScriptableObject.CreateInstance<Configuration>();
            config.name = "cave_tileworld_configuration";
            config.cellSizeOld = 0;
            config.cellSize = config.lastCellSize = Cell;
            config.width = 52; config.height = 46;
            config.clusterCellSize = 5;
            config.useGlobalRandomSeed = true; config.globalRandomSeed = 731;
            config.mergeTiles = true; config.colliderType = Configuration.ColliderType.meshCollider;
            config.useParallel = false;
            AssetDatabase.CreateAsset(config, Configs + "/cave_tileworld_configuration.asset");
            var go = new GameObject("TileWorldCreator | authored cave paving");
            go.transform.position = GridOrigin;
            manager = go.AddComponent<TileWorldCreatorManager>();
            manager.configuration = config;
            // Disabled authoring component: its saved mesh children remain active at runtime.
            manager.enabled = false;
            config.SetManager(manager);
            var blueprints = new Dictionary<string, BlueprintLayer>();
            paintedCells = 0;
            Physics.SyncTransforms();
            foreach (var filter in landform.GetComponentsInChildren<MeshFilter>().ToArray())
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                var collider = filter.GetComponent<MeshCollider>();
                if (collider == null || renderer == null || renderer.bounds.size.x * renderer.bounds.size.z < 30) continue;
                if (!renderer.sharedMaterials.Any(m => m != null && m.name == "rock")) continue;
                string surface = renderer.sharedMaterials[0].name;
                if (!presets.ContainsKey(surface)) continue;
                var cells = SampleCells(collider, renderer.bounds);
                if (cells.Count == 0) continue;
                foreach (var heightGroup in cells.GroupBy(p => p.Value))
                {
                    string key = surface + "_" + heightGroup.Key.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                    if (!blueprints.TryGetValue(key, out var blueprint))
                    {
                        blueprint = manager.AddNewBlueprintLayer(key);
                        blueprint.defaultLayerHeight = heightGroup.Key;
                        blueprint.layerColor = surface == "wet" ? new Color(.25f,.55f,.6f) : new Color(.7f,.55f,.35f);
                        var build = manager.AddNewBuildLayer<TilesBuildLayer>(key);
                        build.assignedBlueprintLayerGuid = blueprint.guid;
                        build.useDualGrid = false;
                        build.scaleTileToCellSize = false;
                        build.tilePresetsTop.Add(new TilesBuildLayer.TilePresetSelection { preset = presets[surface], weight = 1 });
                        build.shadowCastingMode = ShadowCastingMode.On;
                        build.asset = config;
                        blueprints.Add(key, blueprint);
                    }
                    blueprint.AddCells(new HashSet<Vector2>(heightGroup.Select(p => (Vector2)p.Key)));
                    EditorUtility.SetDirty(blueprint);
                }
                // Replace only the covered top faces. Original curved skirts and vertical cliffs survive.
                var remainder = CutTileFootprints(filter, cells);
                SaveMesh(remainder, "transition_" + filter.name);
                filter.sharedMesh = remainder;
                collider.sharedMesh = remainder;
                paintedCells += cells.Count;
            }
            if (paintedCells < 100) throw new InvalidOperationException("No usable platform masks were found.");
            ReplaceWater(landform);
            landform.name = "Curved boundaries, cliffs, ramps and wooden bridges";
            foreach (var camera in terrain.GetComponentsInChildren<Camera>())
            {
                var data = camera.GetUniversalAdditionalCameraData();
                data.requiresDepthTexture = true;
                data.requiresColorTexture = true;
            }
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            started = EditorApplication.timeSinceStartup;
            manager.ExecuteBlueprintLayers();
            manager.ExecuteBuildLayers(ExecutionMode.FromScratch);
            EditorApplication.update += FinishWhenReady;
        }

        private static Dictionary<string, TilePreset> CreateTilePresets()
        {
            string modelPath = Models + "/cave_floor_tiles.fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            importer.isReadable = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var result = new Dictionary<string, TilePreset>();
            foreach (string surface in new[] { "earth", "wet", "moss", "stone" })
            {
                var preset = ScriptableObject.CreateInstance<TilePreset>();
                preset.gridtype = TilePreset.GridType.standard;
                preset.name = "cave_" + surface;
                foreach (string role in Roles)
                {
                    var child = model.GetComponentsInChildren<MeshFilter>().Single(f => f.name == "cave_tile_" + role);
                    var root = new GameObject("cave_" + surface + "_" + role);
                    var part = Object.Instantiate(child.gameObject, root.transform);
                    part.name = child.name;
                    part.GetComponent<MeshRenderer>().sharedMaterials = new[]
                    {
                        AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/CaveTerracedV2/" + surface + ".mat"),
                        AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/CaveTerracedV2/rock.mat")
                    };
                    var prefab = PrefabUtility.SaveAsPrefabAsset(root, Tiles + "/" + root.name + ".prefab");
                    Object.DestroyImmediate(root);
                    string field = "NRMGRD_" + string.Concat(role.Split('_').Select((s,i) => i == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1))) + "Tile";
                    typeof(TilePreset).GetField(field).SetValue(preset, prefab);
                }
                AssetDatabase.CreateAsset(preset, Configs + "/" + preset.name + "_preset.asset");
                result.Add(surface, preset);
            }
            return result;
        }

        private static void ClearBuildingFootprints(Transform terrain)
        {
            var layout=JsonUtility.FromJson<Layout>(File.ReadAllText("Assets/Art/Models/CaveTerracedV2/cave_terraced_layout.json"));
            var zones=layout.Zones.Where(z=>new[] {3,5,6,7,10}.Contains(z.Id)).ToArray();
            bool InRoom(Vector3 position)
            {
                var p=position-terrain.position;
                return zones.Any(z=>Mathf.Abs(p.x-z.X)<z.Width*.5f+1 && Mathf.Abs(p.z-z.Z)<z.Depth*.5f+1);
            }
            // Old terrain reservation dressing must not reappear inside the retained room interiors.
            foreach (var renderer in terrain.GetComponentsInChildren<MeshRenderer>())
            {
                string name=renderer.name;
                if ((name.Contains("ReservationEdgeRock") || name.StartsWith("MineralCluster") ||
                     name.StartsWith("InteriorRockSpur") || name.StartsWith("LooseFloorStone")) && InRoom(renderer.transform.position))
                    renderer.gameObject.SetActive(false);
            }
            var lamps=terrain.Find("Wayfinding | warm pixel braziers");
            if (lamps!=null) foreach (Transform lamp in lamps)
                if (InRoom(lamp.position)) lamp.gameObject.SetActive(false);
        }

        private static Dictionary<Vector2Int,float> SampleCells(MeshCollider collider, Bounds bounds)
        {
            var cells = new Dictionary<Vector2Int,float>();
            for (int x = Mathf.CeilToInt((bounds.min.x-GridOrigin.x+2)/Cell); x <= Mathf.FloorToInt((bounds.max.x-GridOrigin.x-2)/Cell); x++)
                for (int z = Mathf.CeilToInt((bounds.min.z-GridOrigin.z+2)/Cell); z <= Mathf.FloorToInt((bounds.max.z-GridOrigin.z-2)/Cell); z++)
                {
                    float? y = null;
                    bool valid = true;
                    for (int a = -2; a <= 2 && valid; a++)
                        for (int b = -2; b <= 2 && valid; b++)
                        {
                            var start = GridOrigin + new Vector3(x*Cell+a*.999f, 40, z*Cell+b*.999f);
                            valid = collider.Raycast(new Ray(start, Vector3.down), out var hit, 80) && hit.normal.y > .999f;
                            if (!valid) continue;
                            if (y.HasValue && Mathf.Abs(y.Value-hit.point.y) > .015f) valid = false;
                            y = hit.point.y;
                        }
                    if (valid && y.HasValue) cells.Add(new Vector2Int(x,z), Mathf.Round(y.Value*100)/100);
                }
            return cells;
        }

        private static Mesh CutTileFootprints(MeshFilter filter, Dictionary<Vector2Int,float> cells)
        {
            Mesh source = filter.sharedMesh;
            var verts = source.vertices; var uv = source.uv;
            var output = new List<Vector3>(); var texcoords = new List<Vector2>();
            var indices = new List<int>[source.subMeshCount];
            for (int sub = 0; sub < source.subMeshCount; sub++)
            {
                indices[sub] = new List<int>();
                var tris = source.GetTriangles(sub);
                for (int t = 0; t < tris.Length; t += 3)
                {
                    var polygon = Enumerable.Range(0,3).Select(i => new Vertex
                    { P = filter.transform.TransformPoint(verts[tris[t+i]]), UV = uv.Length == verts.Length ? uv[tris[t+i]] : Vector2.zero }).ToList();
                    var fragments = new List<List<Vertex>> { polygon };
                    if (polygon.Max(v=>v.P.y)-polygon.Min(v=>v.P.y) < .015f)
                    {
                        int minX = Mathf.CeilToInt((polygon.Min(v=>v.P.x)-GridOrigin.x-2)/Cell);
                        int maxX = Mathf.FloorToInt((polygon.Max(v=>v.P.x)-GridOrigin.x+2)/Cell);
                        int minZ = Mathf.CeilToInt((polygon.Min(v=>v.P.z)-GridOrigin.z-2)/Cell);
                        int maxZ = Mathf.FloorToInt((polygon.Max(v=>v.P.z)-GridOrigin.z+2)/Cell);
                        for (int x=minX; x<=maxX; x++) for (int z=minZ; z<=maxZ; z++)
                        {
                            if (!cells.TryGetValue(new Vector2Int(x,z), out float y) || Mathf.Abs(y-polygon[0].P.y)>.015f) continue;
                            float cx=GridOrigin.x+x*Cell, cz=GridOrigin.z+z*Cell;
                            var remaining = new List<List<Vertex>>();
                            foreach (var fragment in fragments)
                            {
                                var inside = fragment;
                                foreach (var plane in new[] { new Vector3(1,0,cx-2), new Vector3(-1,0,-cx-2), new Vector3(0,1,cz-2), new Vector3(0,-1,-cz-2) })
                                {
                                    var outside = Clip(inside, plane, false);
                                    if (outside.Count>=3) remaining.Add(outside);
                                    inside = Clip(inside, plane, true);
                                    if (inside.Count<3) break;
                                }
                            }
                            fragments = remaining;
                        }
                    }
                    foreach (var fragment in fragments) Append(fragment, filter.transform.worldToLocalMatrix, output, texcoords, indices[sub]);
                }
            }
            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(output); mesh.SetUVs(0,texcoords); mesh.subMeshCount=indices.Length;
            for (int sub=0; sub<indices.Length; sub++) mesh.SetTriangles(indices[sub],sub);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        // Clip to n.x*x + n.y*z >= n.z. Reversing the inequality extracts the outside half.
        private static List<Vertex> Clip(List<Vertex> polygon, Vector3 plane, bool positive)
        {
            var result = new List<Vertex>();
            if (polygon.Count==0) return result;
            float Distance(Vertex v) => (v.P.x*plane.x+v.P.z*plane.y-plane.z)*(positive?1:-1);
            Vertex a=polygon[polygon.Count-1]; float da=Distance(a);
            foreach (var b in polygon)
            {
                float db=Distance(b);
                if ((da>=0)!=(db>=0)) result.Add(Vertex.Lerp(a,b,da/(da-db)));
                if (db>=0) result.Add(b);
                a=b; da=db;
            }
            return result;
        }

        private static void Append(List<Vertex> polygon, Matrix4x4 transform, List<Vector3> verts, List<Vector2> uv, List<int> tris)
        {
            for (int i=1; i<polygon.Count-1; i++)
            {
                if (Vector3.Cross(polygon[i].P-polygon[0].P,polygon[i+1].P-polygon[0].P).sqrMagnitude<.00000001f) continue;
                foreach (var vertex in new[] { polygon[0],polygon[i],polygon[i+1] })
                { tris.Add(verts.Count); verts.Add(transform.MultiplyPoint3x4(vertex.P)); uv.Add(vertex.UV); }
            }
        }

        private static void ReplaceWater(Transform landform)
        {
            var filter=landform.GetComponentsInChildren<MeshFilter>().Single(f=>f.name=="RiverSurface");
            var source=filter.sharedMesh;
            var verts=source.vertices; var sourceUV=source.uv; var tris=source.triangles;
            var output=new List<Vector3>(); var uv=new List<Vector2>(); var indices=new List<int>();
            for (int t=0; t<tris.Length; t+=3)
            {
                var polygon=Enumerable.Range(0,3).Select(i=>new Vertex { P=filter.transform.TransformPoint(verts[tris[t+i]]),
                    UV=new Vector2(sourceUV[tris[t+i]].x,filter.transform.TransformPoint(verts[tris[t+i]]).z*.15f) }).ToList();
                for (int x=Mathf.FloorToInt(polygon.Min(v=>v.P.x)/2); x<=Mathf.FloorToInt(polygon.Max(v=>v.P.x)/2); x++)
                    for (int z=Mathf.FloorToInt(polygon.Min(v=>v.P.z)/2); z<=Mathf.FloorToInt(polygon.Max(v=>v.P.z)/2); z++)
                    {
                        var part=polygon;
                        foreach (var plane in new[] { new Vector3(1,0,x*2),new Vector3(-1,0,-x*2-2),new Vector3(0,1,z*2),new Vector3(0,-1,-z*2-2) })
                            part=Clip(part,plane,true);
                        Append(part,filter.transform.worldToLocalMatrix,output,uv,indices);
                    }
            }
            var mesh=new Mesh { indexFormat=IndexFormat.UInt32 };
            mesh.SetVertices(output); mesh.SetUVs(0,uv); mesh.SetTriangles(indices,0);
            mesh.colors=Enumerable.Repeat(Color.clear,output.Count).ToArray();
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            SaveMesh(mesh,"river_surface"); filter.sharedMesh=mesh;
            var material=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/StylizedWater2/Materials/StylizedWater2_River.mat"));
            material.name="cave_stylized_water";
            material.SetColor("_BaseColor",new Color(.035f,.14f,.17f,.82f));
            material.SetColor("_ShallowColor",new Color(.19f,.4f,.36f,.2f));
            material.SetColor("_FoamColor",new Color(.64f,.76f,.65f,.5f));
            material.SetColor("_IntersectionColor",new Color(.55f,.72f,.64f,.42f));
            foreach (var value in new Dictionary<string,float> { {"_WaveHeight",.055f},{"_WaveSpeed",.35f},{"_Speed",.2f},
                {"_FoamBaseAmount",.025f},{"_FoamWaveAmount",.06f},{"_NormalStrength",.23f},{"_NormalSpeed",.55f},
                {"_DepthVertical",1.5f},{"_DepthHorizontal",.7f},{"_EdgeFade",.35f},{"_IntersectionLength",.6f},
                {"_IntersectionFalloff",.6f},{"_ReflectionStrength",.35f},{"_SunReflectionStrength",.45f},
                {"_PointSpotLightReflectionStrength",.8f},{"_RefractionStrength",.06f},{"_CausticsBrightness",.5f},
                {"_VertexColorDepth",0},{"_VertexColorFoam",0},{"_VertexColorWaveFlattening",0},{"_WorldSpaceUV",0} })
                material.SetFloat(value.Key,value.Value);
            AssetDatabase.CreateAsset(material,Configs+"/cave_stylized_water.mat");
            var renderer=filter.GetComponent<MeshRenderer>(); renderer.sharedMaterial=material;
            var water=filter.gameObject.AddComponent<WaterObject>();
            water.meshFilter=filter; water.meshRenderer=renderer; water.material=material;
            filter.name="RiverSurface | Stylized Water 2";
        }

        private static void SaveMesh(Mesh mesh, string name)
        {
            mesh.name=name;
            AssetDatabase.CreateAsset(mesh,Models+"/meshes/"+name+".asset");
        }

        private static void FinishWhenReady()
        {
            try
            {
                var layers=manager.configuration.buildLayerFolders.SelectMany(f=>f.buildLayers).ToArray();
                if (layers.Any(l=>l.isExecuting))
                {
                    if (EditorApplication.timeSinceStartup-started>180) throw new TimeoutException("TWC generation timed out.");
                    return;
                }
                EditorApplication.update-=FinishWhenReady;
                var filters=manager.GetComponentsInChildren<MeshFilter>();
                if (filters.Length==0) throw new InvalidOperationException("TWC generated no saved terrain.");
                PersistGeneratedMeshes();
                foreach (var layer in layers) EditorUtility.SetDirty(layer);
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                // Reload catches unsaved transient meshes or invalid script references before delivery.
                scene=EditorSceneManager.OpenScene(ScenePath);
                ValidateSavedScene();
                ExportPreviews();
                Debug.Log($"CAVE_TILEWORLD_SAVED: {paintedCells} painted cells, {layers.Length} layers, {filters.Length} merged mesh clusters. " + ScenePath);
                if (batch) EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                EditorApplication.update-=FinishWhenReady;
                Debug.LogException(error);
                if (batch) EditorApplication.Exit(1);
            }
        }

        [MenuItem("Untitled/Cave/Save TileWorld Cave Meshes")]
        public static void PersistGeneratedMeshes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path!=ScenePath)
                throw new InvalidOperationException("Open the TileWorld scene outside Play Mode.");
            var creator=Object.FindObjectOfType<TileWorldCreatorManager>(true);
            var map=new Dictionary<Mesh,Mesh>();
            foreach (var filter in creator.GetComponentsInChildren<MeshFilter>())
            {
                var source=filter.sharedMesh;
                if (source==null || AssetDatabase.Contains(source)) continue;
                var clone=Object.Instantiate(source);
                string name="cluster_"+filter.transform.parent.name+"_"+filter.name;
                string path=Models+"/meshes/"+name.Replace(" ","_")+".asset";
                var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (existing!=null) { EditorUtility.CopySerialized(clone,existing); Object.DestroyImmediate(clone); clone=existing; }
                else SaveMesh(clone,Path.GetFileNameWithoutExtension(path));
                map[source]=clone; filter.sharedMesh=clone;
            }
            foreach (var collider in creator.GetComponentsInChildren<MeshCollider>())
                if (collider.sharedMesh!=null && map.TryGetValue(collider.sharedMesh,out var saved)) collider.sharedMesh=saved;
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private static void ValidateSavedScene()
        {
            Physics.SyncTransforms();
            var layout=JsonUtility.FromJson<Layout>(File.ReadAllText("Assets/Art/Models/CaveTerracedV2/cave_terraced_layout.json"));
            var origin=new Vector3(0,0,200);
            var landform=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>())
                .Single(t=>t.name.StartsWith("Curved boundaries"));
            var creator=Object.FindObjectOfType<TileWorldCreatorManager>(true);
            foreach (var point in layout.RouteSamples)
            {
                var ray=new Ray(origin+new Vector3(point.X,point.Height+16,point.Z),Vector3.down);
                var hits=Physics.RaycastAll(ray,24).Where(h=>h.collider is MeshCollider &&
                    (h.transform.IsChildOf(landform) || h.transform.IsChildOf(creator.transform))).OrderBy(h=>h.distance).ToArray();
                if (hits.Length==0 || Mathf.Abs(hits[0].point.y-point.Height)>.11f)
                    throw new InvalidOperationException("Route surface missing: "+point.Name+" at "+point.X+","+point.Z+
                        "; hit "+(hits.Length>0?hits[0].collider.name+" y="+hits[0].point.y:"none"));
            }
            foreach (var root in scene.GetRootGameObjects()) foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
                if (filter.sharedMesh==null) throw new InvalidOperationException("Missing mesh: "+filter.name);
            foreach (var root in scene.GetRootGameObjects()) foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
                if (renderer.sharedMaterials.Any(m=>m==null || ShaderUtil.ShaderHasError(m.shader)))
                    throw new InvalidOperationException("Invalid material: "+renderer.name);
            Debug.Log("CAVE_TILEWORLD_VALIDATED: 910 saved route surfaces, persistent meshes and shader references.");
        }

        [Serializable] private class Point { public string Name; public float X,Z,Height; }
        [Serializable] private class Zone { public int Id; public float X,Z,Width,Depth; }
        [Serializable] private class Layout { public Point[] RouteSamples; public Zone[] Zones; }

        private static void ExportPreviews()
        {
            string folder=Path.Combine(Path.GetTempPath(),"untitled-cave-tileworld"); Directory.CreateDirectory(folder);
            var roots=scene.GetRootGameObjects();
            var cameras=roots.SelectMany(g=>g.GetComponentsInChildren<Camera>()).Where(c=>
                c.name.StartsWith("Top Camera") || c.name.StartsWith("Concept A") || c.name.StartsWith("Concept B") || c.name.StartsWith("River Camera")).ToArray();
            var profile=roots.SelectMany(g=>g.GetComponentsInChildren<Volume>()).First().sharedProfile;
            profile.TryGet<DepthOfField>(out var depth); bool oldDepth=depth.active; bool oldFog=RenderSettings.fog;
            try
            {
                depth.active=false;
                foreach (var camera in cameras)
                {
                    bool top=camera.name.StartsWith("Top Camera"); RenderSettings.fog=top?false:oldFog;
                    var target=RenderTexture.GetTemporary(top?1200:1600,top?1400:900,24,RenderTextureFormat.ARGB32);
                    var previous=RenderTexture.active; Texture2D pixels=null;
                    try
                    {
                        camera.aspect=(float)target.width/target.height;
                        RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest { destination=target });
                        RenderTexture.active=target; pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                        pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0); pixels.Apply();
                        string name=top?"plan":camera.name.StartsWith("Concept A")?"entrance":camera.name.StartsWith("Concept B")?"bridge":"river";
                        File.WriteAllBytes(Path.Combine(folder,"cave_tileworld_"+name+".png"),pixels.EncodeToPNG());
                    }
                    finally { RenderTexture.active=previous; camera.ResetAspect(); if (pixels!=null) Object.DestroyImmediate(pixels); RenderTexture.ReleaseTemporary(target); }
                }
            }
            finally { depth.active=oldDepth; RenderSettings.fog=oldFog; }
        }
    }
}
