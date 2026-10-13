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
    /// <summary>Saved HD-2D scenery, authored explicitly with TileWorldCreator.</summary>
    public static class CaveHD2DArtPass
    {
        public const string ScenePath = "Assets/Scenes/CaveTileWorldHD2D.unity";
        private const string Art = "Assets/Art/Models/CaveTileWorldHD2D";
        private const string Textures = "Assets/Art/Textures/CaveTileWorldHD2D";
        private const string Prefabs = "Assets/Prefabs/Terrain/CaveTileWorldHD2D";
        private const string Configs = "Assets/Configs/CaveTileWorldHD2D";
        private const string Materials = "Assets/Art/Materials/CaveTileWorldHD2D";
        private static readonly Vector3 Origin = new Vector3(0,0,200);
        private static readonly Dictionary<Material,Material> materialCopies = new Dictionary<Material,Material>();
        private static readonly Dictionary<string,GameObject> modules = new Dictionary<string,GameObject>();
        private static Scene scene;
        private static TileWorldCreatorManager manager;
        private static Layout layout;
        private static double started;
        private static bool batch;
        private static int shelfCells;

        [Serializable] private class Point { public string Name; public float X,Z,Height; }
        [Serializable] private class Zone { public int Id; public float X,Z,Height,Width,Depth; }
        [Serializable] private class Layout { public Point[] RouteSamples; public Zone[] Zones; }

        [MenuItem("Untitled/Cave/Create HD2D TileWorld Art Version")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Begin();
        }

        public static void BatchCreate() { batch=true; Begin(); }

        private static void Begin()
        {
            if (File.Exists(ScenePath)) throw new InvalidOperationException("The HD2D scene already exists; edit the saved version.");
            foreach (string folder in new[] { Prefabs, Configs, Materials, Art+"/meshes" }) Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            layout=JsonUtility.FromJson<Layout>(File.ReadAllText("Assets/Art/Models/CaveTerracedV2/cave_terraced_layout.json"));
            materialCopies.Clear(); modules.Clear();
            PrepareTextures();
            BuildModules();
            RefreshPalette();
            AssetDatabase.CopyAsset("Assets/Scenes/CaveTileWorld.unity",ScenePath);
            scene=EditorSceneManager.OpenScene(ScenePath);
            manager=Object.FindObjectOfType<TileWorldCreatorManager>(true);
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(manager.configuration),Configs+"/hd2d_cave_configuration.asset");
            manager.configuration=AssetDatabase.LoadAssetAtPath<Configuration>(Configs+"/hd2d_cave_configuration.asset");
            manager.configuration.name="hd2d_cave_configuration";
            manager.configuration.SetManager(manager);
            manager.name="TileWorldCreator | HD2D shelves and paths";
            manager.enabled=false;
            CloneGroundPresets();
            Physics.SyncTransforms();
            AddRockShelves();
            AddCaveFrameBlocks();
            AddWornPaths();
            var scenery=new GameObject("HD2D Scenery | composed rock silhouettes");
            scenery.transform.position=Origin;
            DressCave(scenery.transform);
            RepaintScene();
            Presentation();
            EditorUtility.SetDirty(manager.configuration);
            AssetDatabase.SaveAssets();
            started=EditorApplication.timeSinceStartup;
            manager.ExecuteBlueprintLayers();
            manager.ExecuteBuildLayers(ExecutionMode.FromScratch);
            EditorApplication.update+=Finish;
        }

        private static void PrepareTextures()
        {
            foreach (string file in Directory.GetFiles(Textures).Where(p=>p.EndsWith(".png") || p.EndsWith(".tga")))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(file.Replace('\\','/'));
                importer.filterMode=FilterMode.Point;
                importer.mipmapEnabled=false;
                importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.wrapMode=file.Contains("reference_")?TextureWrapMode.Clamp:TextureWrapMode.Repeat;
                importer.SaveAndReimport();
            }
        }

        private static Material PixelSurface(string family)
        {
            string path=Materials+"/hd2d_"+family+".mat";
            var existing=AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing!=null) return existing;
            var basis=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/CaveTerracedV2/"+(family=="path"?"earth":family)+".mat");
            var material=new Material(basis) { name="hd2d_"+family };
            string textureFamily=family=="stone"?"rock":family=="path"?"earth":family;
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Textures+"/hd2d_"+textureFamily+"_64.png"));
            material.SetColor("_BaseColor",family=="path"?new Color(1.14f,1.10f,1.03f):family=="earth"?new Color(1.05f,1,.96f):Color.white);
            material.SetFloat("_TextureStrength",family=="rock"?.86f:.7f);
            material.SetFloat("_PixelDensity",16); material.SetFloat("_TileScale",.25f);
            material.SetFloat("_Smoothness",family=="wet"?.13f:.025f);
            AssetDatabase.CreateAsset(material,path);
            return material;
        }

        private static void RefreshPalette()
        {
            foreach (string family in new[] {"earth","rock","moss","wet","stone","path"})
            {
                var material=PixelSurface(family);
                material.SetColor("_BaseColor",family=="path"?new Color(1.14f,1.10f,1.03f):family=="earth"?new Color(1.05f,1,.96f):Color.white);
                material.SetFloat("_TextureStrength",family=="rock"?.75f:.62f);
                EditorUtility.SetDirty(material);
            }
        }

        private static Material ReferenceSurface(string family)
        {
            string path=Materials+"/"+family+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material!=null) return material;
            material=new Material(Shader.Find("Universal Render Pipeline/Lit")) { name=family };
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Textures+"/"+family+".tga"));
            material.SetColor("_BaseColor",new Color(.8f,.85f,.81f));
            material.SetFloat("_Smoothness",.025f); material.enableInstancing=true;
            AssetDatabase.CreateAsset(material,path);
            return material;
        }

        private static void BuildModules()
        {
            string path=Art+"/hd2d_cave_modules.fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.isReadable=true; importer.materialImportMode=ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (var source in model.GetComponentsInChildren<MeshFilter>())
            {
                var root=new GameObject(source.name);
                var part=Object.Instantiate(source.gameObject,root.transform);
                part.name=source.name;
                string reference=source.name.StartsWith("ref_boulder")?"reference_rock":source.name.StartsWith("ref_stalagmite")?"reference_cave":"reference_moss";
                var renderer=part.GetComponent<MeshRenderer>();
                renderer.sharedMaterials=source.name.StartsWith("ref_")?new[] {ReferenceSurface(reference)}:
                    source.name.StartsWith("hd2d_shelf")?new[] {PixelSurface("earth"),PixelSurface("rock")}:
                    new[] {PixelSurface("rock")};
                var prefab=PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"/"+source.name+".prefab");
                modules.Add(source.name,prefab);
                Object.DestroyImmediate(root);
            }
            var wall=(GameObject)PrefabUtility.InstantiatePrefab(modules["hd2d_shelf_fill"]);
            PrefabUtility.UnpackPrefabInstance(wall,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            wall.name="hd2d_wall_block";
            foreach (var renderer in wall.GetComponentsInChildren<MeshRenderer>())
                renderer.sharedMaterials=Enumerable.Repeat(PixelSurface("rock"),renderer.sharedMaterials.Length).ToArray();
            modules.Add(wall.name,PrefabUtility.SaveAsPrefabAsset(wall,Prefabs+"/"+wall.name+".prefab"));
            Object.DestroyImmediate(wall);
        }

        private static void CloneGroundPresets()
        {
            var copies=new Dictionary<TilePreset,TilePreset>();
            foreach (var build in manager.configuration.buildLayerFolders.SelectMany(f=>f.buildLayers).OfType<TilesBuildLayer>())
                foreach (var selection in build.tilePresetsTop)
                {
                    var original=selection.preset;
                    if (!copies.TryGetValue(original,out var copy))
                    {
                        copy=Object.Instantiate(original); copy.name="hd2d_"+original.name;
                        foreach (var field in typeof(TilePreset).GetFields().Where(f=>f.FieldType==typeof(GameObject)))
                        {
                            var source=(GameObject)field.GetValue(original);
                            if (source==null) continue;
                            var root=(GameObject)PrefabUtility.InstantiatePrefab(source);
                            PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                            root.name="hd2d_"+source.name;
                            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
                                renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>MapMaterial(m)).ToArray();
                            var saved=PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"/"+root.name+".prefab");
                            field.SetValue(copy,saved); Object.DestroyImmediate(root);
                        }
                        AssetDatabase.CreateAsset(copy,Configs+"/"+copy.name+".asset"); copies.Add(original,copy);
                    }
                    selection.preset=copy;
                    build.asset=manager.configuration;
                    EditorUtility.SetDirty(build);
                }
        }

        private static TilesBuildLayer AddDualLayer(string name, float y, HashSet<Vector2> cells, bool path=false)
        {
            var blueprint=manager.AddNewBlueprintLayer(name);
            blueprint.defaultLayerHeight=y;
            blueprint.layerColor=path?new Color(.75f,.62f,.35f):new Color(.35f,.5f,.42f);
            blueprint.AddCells(cells);
            var preset=ScriptableObject.CreateInstance<TilePreset>();
            preset.name=name+"_preset"; preset.gridtype=TilePreset.GridType.dual;
            foreach (var role in new[] {"corner","edge","interior","double","fill"})
            {
                var prefab=modules["hd2d_shelf_"+role];
                if (path)
                {
                    var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                    go.name="hd2d_path_"+role;
                    foreach (var renderer in go.GetComponentsInChildren<MeshRenderer>())
                        renderer.sharedMaterials=Enumerable.Repeat(PixelSurface("path"),renderer.sharedMaterials.Length).ToArray();
                    prefab=PrefabUtility.SaveAsPrefabAsset(go,Prefabs+"/"+go.name+".prefab"); Object.DestroyImmediate(go);
                }
                if (role=="corner") preset.DUALGRD_cornerTile=prefab;
                if (role=="edge") preset.DUALGRD_edgeTile=prefab;
                if (role=="interior") preset.DUALGRD_invertedCornerTile=prefab;
                if (role=="double") preset.DUALGRD_doubleInteriorCornerTile=prefab;
                if (role=="fill") preset.DUALGRD_fillTile=prefab;
            }
            AssetDatabase.CreateAsset(preset,Configs+"/"+preset.name.Replace("|","_")+".asset");
            var build=manager.AddNewBuildLayer<TilesBuildLayer>(name);
            build.assignedBlueprintLayerGuid=blueprint.guid; build.asset=manager.configuration;
            build.useDualGrid=true; build.scaleTileToCellSize=false;
            build.tilePresetsTop.Add(new TilesBuildLayer.TilePresetSelection {preset=preset,weight=1});
            build.meshGenerationOverride=true; build.mergeTiles=true;
            build.colliderType=path?Configuration.ColliderType.none:Configuration.ColliderType.meshCollider;
            build.shadowCastingMode=path?ShadowCastingMode.Off:ShadowCastingMode.On;
            if (path) build.scaleOffset=new Vector3(1,.012f,1);
            return build;
        }

        private static bool ClearSpace(Vector2 p, float margin)
        {
            if (layout.Zones.Any(z=>Mathf.Abs(p.x-z.X)<z.Width*.5f+margin && Mathf.Abs(p.y-z.Z)<z.Depth*.5f+margin)) return false;
            return !layout.RouteSamples.Any(r=>Vector2.Distance(p,new Vector2(r.X,r.Z))<margin+3.1f);
        }

        private static float Ground(Vector2 p)
        {
            var hits=Physics.RaycastAll(new Ray(Origin+new Vector3(p.x,30,p.y),Vector3.down),50)
                .Where(h=>h.collider is MeshCollider && h.normal.y>.95f).OrderBy(h=>h.distance).ToArray();
            return hits.Length>0?hits[0].point.y:float.NaN;
        }

        private static void AddRockShelves()
        {
            var cells=new HashSet<Vector2>();
            var hills=new[] {new Vector4(34,-29,8,6),new Vector4(41,15,7,7),new Vector4(46,37,9,7),
                new Vector4(8,34,8,6),new Vector4(81,61,10,7),new Vector4(89,-46,8,8),
                new Vector4(15,43,9,7),new Vector4(14,-68,8,5),new Vector4(-12,-32,6,7),new Vector4(3,78,10,7)};
            for (int x=0;x<52;x++) for (int z=0;z<46;z++)
            {
                var p=new Vector2(manager.transform.position.x+x*4,manager.transform.position.z-Origin.z+z*4);
                if (!hills.Any(h=>Mathf.Pow((p.x-h.x)/h.z,2)+Mathf.Pow((p.y-h.y)/h.w,2)<1)) continue;
                if (!ClearSpace(p,3)) continue;
                float y=Ground(p);
                if (float.IsNaN(y) || Mathf.Abs(y-2.4f)>.16f) continue;
                cells.Add(new Vector2(x,z));
            }
            shelfCells=cells.Count;
            AddSolidLayer("RockKnolls | full-block living terraces",5.6f,cells);
            Debug.Log("HD2D_SHELF_CELLS: "+shelfCells);
        }

        private static void AddSolidLayer(string name,float y,HashSet<Vector2> cells,string module="hd2d_shelf_fill")
        {
            var blueprint=manager.AddNewBlueprintLayer(name); blueprint.defaultLayerHeight=y;
            blueprint.layerColor=new Color(.37f,.45f,.42f); blueprint.AddCells(cells);
            var preset=ScriptableObject.CreateInstance<TilePreset>();
            preset.name=name.Replace("|","_")+"_preset"; preset.gridtype=TilePreset.GridType.standard;
            foreach (var field in typeof(TilePreset).GetFields().Where(f=>f.Name.StartsWith("NRMGRD_") && f.FieldType==typeof(GameObject)))
                field.SetValue(preset,modules[module]);
            AssetDatabase.CreateAsset(preset,Configs+"/"+preset.name+".asset");
            var build=manager.AddNewBuildLayer<TilesBuildLayer>(name);
            build.assignedBlueprintLayerGuid=blueprint.guid; build.asset=manager.configuration;
            build.useDualGrid=false; build.scaleTileToCellSize=false;
            build.tilePresetsTop.Add(new TilesBuildLayer.TilePresetSelection {preset=preset,weight=1});
            build.meshGenerationOverride=true; build.mergeTiles=true; build.colliderType=Configuration.ColliderType.meshCollider;
            build.shadowCastingMode=ShadowCastingMode.On;
        }

        private static void AddCaveFrameBlocks()
        {
            var masses=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>())
                .Where(t=>t.name.StartsWith("CaveWallMass_")).OrderBy(t=>t.name).ToArray();
            var west=new HashSet<Vector2>(); var east=new HashSet<Vector2>();
            for (int i=0;i<masses.Length-1;i++)
            {
                if (i==9) continue; // Keep the northern river aperture open.
                var a=masses[i].position-Origin; var b=masses[i+1].position-Origin;
                int steps=Mathf.CeilToInt(Vector2.Distance(new Vector2(a.x,a.z),new Vector2(b.x,b.z))/2);
                for (int j=0;j<=steps;j++)
                {
                    var p=Vector3.Lerp(a,b,(float)j/Mathf.Max(1,steps));
                    int gx=Mathf.RoundToInt((p.x-manager.transform.position.x)/4);
                    int gz=Mathf.RoundToInt((p.z+Origin.z-manager.transform.position.z)/4);
                    for (int dx=-1;dx<=1;dx++) for (int dz=-1;dz<=1;dz++)
                    {
                        var cell=new Vector2(gx+dx,gz+dz);
                        var world=new Vector2(manager.transform.position.x+cell.x*4,manager.transform.position.z-Origin.z+cell.y*4);
                        if (!ClearSpace(world,3)) continue;
                        (i<9?west:east).Add(cell);
                    }
                }
            }
            foreach (var group in new[] {("WestCaveBlocks",west,-2.4f),("NorthEastCaveBlocks",east,2.4f)})
                for (int tier=0;tier<4;tier++) AddSolidLayer(group.Item1+"_Tier"+tier,group.Item3+3.2f*(tier+1),group.Item2,"hd2d_wall_block");
            Debug.Log("HD2D_FULL_BLOCK_WALLS: "+(west.Count+east.Count)+" columns in four saved tiers.");
        }

        private static void AddWornPaths()
        {
            var cells=new HashSet<Vector2>();
            for (int x=0;x<52;x++) for (int z=0;z<46;z++)
            {
                var p=new Vector2(manager.transform.position.x+x*4,manager.transform.position.z-Origin.z+z*4);
                if (!layout.RouteSamples.Any(r=>Mathf.Abs(r.Height-2.4f)<.12f && Vector2.Distance(p,new Vector2(r.X,r.Z))<2.7f)) continue;
                if (layout.Zones.Where(q=>new[] {3,5,6,7,10}.Contains(q.Id)).Any(q=>Mathf.Abs(p.x-q.X)<q.Width*.5f+2 && Mathf.Abs(p.y-q.Z)<q.Depth*.5f+2)) continue;
                float ground=Ground(p);
                if (float.IsNaN(ground) || Mathf.Abs(ground-2.4f)>.08f) continue;
                cells.Add(new Vector2(x,z));
            }
            AddDualLayer("WornPath | public and supply circulation",2.425f,cells,true);
        }

        private static GameObject Place(Transform root,string module,Vector3 p,float yaw=0,float scale=1,bool collide=false)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(modules[module],root.gameObject.scene);
            go.transform.SetParent(root,false); go.transform.localPosition=p;
            go.transform.localRotation=Quaternion.Euler(0,yaw,0); go.transform.localScale=Vector3.one*scale;
            if (collide) foreach (var filter in go.GetComponentsInChildren<MeshFilter>())
                filter.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;
            return go;
        }

        private static void DressCave(Transform root)
        {
            var ribs=new GameObject("Cave ribs | irregular silhouette and depth").transform;
            ribs.SetParent(root,false);
            var random=new System.Random(731);
            // Place real volume beside the river and along the northern/eastern cave shell.
            for (int i=0;i<16;i++)
            {
                var p=new Vector2(91+3*Mathf.Sin(i*.67f),-61+i*6.4f);
                if (!ClearSpace(p,1.5f)) continue;
                float y=Ground(p); if (float.IsNaN(y)) continue;
                Place(ribs,"hd2d_cave_rib_"+(i%3),new Vector3(p.x,y-.12f,p.y),random.Next(360),1.15f+(float)random.NextDouble()*.6f,true);
            }
            foreach (var p in new[] {new Vector3(-12,2.4f,-43),new Vector3(12,2.4f,-52),new Vector3(-23,9.6f,64),
                new Vector3(-14,9.6f,68),new Vector3(5,9.6f,68),new Vector3(16,9.6f,75),new Vector3(38,9.6f,82)})
            {
                var point=new Vector2(p.x,p.z); float y=Ground(point);
                if (!float.IsNaN(y) && y<=15.3f && ClearSpace(point,2))
                    Place(ribs,"hd2d_cave_rib_1",new Vector3(p.x,y-.1f,p.z),random.Next(360),1.25f,true);
            }

            var props=new GameObject("Resource-library rocks | painted UVs").transform;
            props.SetParent(root,false);
            int placed=0;
            // Art clusters sit beside routes; their center and radius must clear every reserved building.
            for (int i=0;i<500;i++)
            {
                var p=new Vector2(-60+(float)random.NextDouble()*151,-72+(float)random.NextDouble()*149);
                if (!ClearSpace(p,2.5f)) continue;
                float y=Ground(p); if (float.IsNaN(y) || y< -2.6f || y>10) continue;
                string module=i%4==0?"ref_stalagmite_"+(i%6).ToString("00"):i%4==1?"ref_moss_rock_00":"ref_boulder_"+(i%11).ToString("00");
                float scale=i%4==0?.9f:.75f;
                Place(props,module,new Vector3(p.x,y-.035f,p.y),random.Next(360),scale,true);
                if (++placed>=70) break;
            }
            var outlook=new GameObject("Ritual skyline | standing-stone landmark").transform;
            outlook.SetParent(root,false);
            for (int i=0;i<5;i++) Place(outlook,"hd2d_cave_rib_"+(i%3),new Vector3(-10+i*3.5f,9.6f,64),i*43,.75f,true);
            LightAt(outlook,"Altar cold focal light",new Vector3(-3,13.2f,62),new Color(.32f,.62f,.76f),4,13);
            LightAt(outlook,"Altar warm approach",new Vector3(-3,11.5f,56),new Color(1,.57f,.22f),2.8f,8);
        }

        private static Material MapMaterial(Material source)
        {
            if (source==null) throw new InvalidOperationException("Missing source material.");
            if (source.name.StartsWith("hd2d_") || source.name.StartsWith("reference_")) return source;
            if (new[] {"earth","rock","wet","moss","stone","path"}.Contains(source.name)) return PixelSurface(source.name);
            if (source.shader.name=="Untitled/HD2D Pixel Prop")
            {
                if (materialCopies.TryGetValue(source,out var spriteCopy)) return spriteCopy;
                string path=Materials+"/hd2d_sprite_"+source.name+".mat";
                spriteCopy=AssetDatabase.LoadAssetAtPath<Material>(path);
                if (spriteCopy==null)
                {
                    spriteCopy=new Material(source) {name="hd2d_sprite_"+source.name};
                    // Existing scene-lit pixel shader computes fog in the vertex stage, keeping
                    // near sprites readable instead of treating fragment depth as world distance.
                    spriteCopy.shader=Shader.Find("Untitled/HD2D/Cave Pixel Prop");
                    spriteCopy.SetColor("_BaseColor",Color.white);
                    if (source.GetFloat("_Emission")>0) spriteCopy.SetFloat("_Emission",1.7f);
                    AssetDatabase.CreateAsset(spriteCopy,path);
                }
                materialCopies.Add(source,spriteCopy); return spriteCopy;
            }
            if (source.shader.name!="Untitled/HD2D/Terraced Pixel Terrain") return source;
            if (materialCopies.TryGetValue(source,out var cached)) return cached;
            var copy=new Material(source) { name="hd2d_"+source.name };
            if (source.name.Contains("stone"))
            {
                copy.SetTexture("_BaseMap",PixelSurface("rock").GetTexture("_BaseMap"));
                copy.SetColor("_BaseColor",new Color(.9f,.86f,.75f)); copy.SetFloat("_TextureStrength",.8f);
            }
            AssetDatabase.CreateAsset(copy,Materials+"/"+copy.name+".mat"); materialCopies.Add(source,copy);
            return copy;
        }

        private static void RepaintScene()
        {
            foreach (var renderer in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)))
                renderer.sharedMaterials=renderer.sharedMaterials.Select(MapMaterial).ToArray();
            // The former tiny evenly scattered triangles flatten the space visually. Larger authored
            // groups above take over the silhouette; low moss/plant sprites are retained.
            var terrain=scene.GetRootGameObjects().Single(g=>g.name.StartsWith("CaveTerrain"));
            foreach (var renderer in terrain.GetComponentsInChildren<MeshRenderer>())
            {
                if (renderer.name.StartsWith("LooseFloorStone") || renderer.name.StartsWith("ReservationEdgeRock"))
                    renderer.gameObject.SetActive(false);
                if (renderer.name.StartsWith("CaveWallMass_") || renderer.name.StartsWith("ContinuousCaveRock") || renderer.name.StartsWith("WallLowerStratum_"))
                    renderer.enabled=false;
            }
        }

        private static void LightAt(Transform root,string name,Vector3 p,Color color,float intensity,float range)
        {
            var go=new GameObject(name); go.transform.SetParent(root,false); go.transform.localPosition=p;
            var light=go.AddComponent<Light>(); light.type=LightType.Point; light.color=color; light.intensity=intensity;
            light.range=range; light.shadows=LightShadows.None;
        }

        private static void Presentation()
        {
            var volume=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Volume>()).Single();
            string path=Configs+"/hd2d_cave_presentation.asset";
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(path)==null)
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(volume.sharedProfile),path);
            volume.sharedProfile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            volume.sharedProfile.TryGet<ColorAdjustments>(out var grading);
            grading.postExposure.Override(1.9f); grading.contrast.Override(12); grading.saturation.Override(-7);
            grading.colorFilter.Override(new Color(1,.97f,.9f));
            volume.sharedProfile.TryGet<Bloom>(out var bloom); bloom.intensity.Override(.3f); bloom.threshold.Override(.95f);
            volume.sharedProfile.TryGet<Vignette>(out var vignette); vignette.intensity.Override(.23f);
            volume.sharedProfile.TryGet<DepthOfField>(out var depth);
            depth.gaussianStart.Override(31); depth.gaussianEnd.Override(63); depth.gaussianMaxRadius.Override(.9f);
            foreach (var light in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>()))
                if (light.type==LightType.Directional)
                { light.transform.rotation=Quaternion.Euler(44,-35,0); light.intensity=.9f; light.color=new Color(.70f,.79f,.87f); }
            RenderSettings.ambientSkyColor=new Color(.23f,.31f,.38f);
            RenderSettings.ambientEquatorColor=new Color(.21f,.24f,.26f);
            RenderSettings.ambientGroundColor=new Color(.12f,.11f,.095f);
            RenderSettings.fogColor=new Color(.07f,.10f,.115f); RenderSettings.fogDensity=.0055f;
            var water=Object.FindObjectOfType<WaterObject>();
            var material=new Material(water.material) {name="hd2d_cave_water"};
            material.SetColor("_BaseColor",new Color(.025f,.11f,.13f,.9f));
            material.SetColor("_ShallowColor",new Color(.16f,.28f,.24f,.25f));
            material.SetColor("_FoamColor",new Color(.53f,.65f,.55f,.4f));
            material.SetFloat("_CausticsBrightness",.2f); material.SetFloat("_ReflectionStrength",.22f);
            var savedWater=AssetDatabase.LoadAssetAtPath<Material>(Configs+"/hd2d_cave_water.mat");
            if (savedWater==null) {AssetDatabase.CreateAsset(material,Configs+"/hd2d_cave_water.mat");savedWater=material;}
            else {EditorUtility.CopySerialized(material,savedWater);Object.DestroyImmediate(material);EditorUtility.SetDirty(savedWater);}
            water.AssignMaterial(savedWater);
            foreach (var component in volume.sharedProfile.components) EditorUtility.SetDirty(component);
            EditorUtility.SetDirty(volume.sharedProfile);
            var follow=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CameraController>()).Single();
            var settings=new SerializedObject(follow);
            settings.FindProperty("viewAngle").floatValue=26;
            settings.FindProperty("distance").floatValue=27;
            settings.FindProperty("yawAngle").floatValue=-5;
            settings.FindProperty("focusOffset").vector3Value=new Vector3(0,1.9f,0);
            settings.ApplyModifiedPropertiesWithoutUndo();
            var camera=follow.GetComponent<Camera>(); camera.fieldOfView=35;
            camera.transform.SetPositionAndRotation(follow.Target.position+new Vector3(0,1.9f,0)+
                Quaternion.Euler(26,-5,0)*Vector3.back*27,Quaternion.Euler(26,-5,0));
        }

        private static void Finish()
        {
            try
            {
                var layers=manager.configuration.buildLayerFolders.SelectMany(f=>f.buildLayers).ToArray();
                if (layers.Any(l=>l.isExecuting))
                { if (EditorApplication.timeSinceStartup-started>180) throw new TimeoutException("HD2D TileWorld generation timed out."); return; }
                EditorApplication.update-=Finish;
                RepaintScene();
                SaveMeshes();
                foreach (var layer in layers) EditorUtility.SetDirty(layer);
                EditorUtility.SetDirty(manager.configuration);
                AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                Debug.Log("HD2D_ART_SAVED: complete block cave walls and shelves, worn paths, resource-library props. "+shelfCells+" rock shelf cells.");
                if (batch) EditorApplication.Exit(0);
            }
            catch (Exception error)
            { EditorApplication.update-=Finish; Debug.LogException(error); if (batch) EditorApplication.Exit(1); }
        }

        [MenuItem("Untitled/Cave/Save HD2D TileWorld Meshes")]
        public static void SaveMeshes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path!=ScenePath)
                throw new InvalidOperationException("Open the HD2D scene outside Play Mode.");
            var creator=Object.FindObjectOfType<TileWorldCreatorManager>(true);
            var copies=new Dictionary<Mesh,Mesh>();
            foreach (var filter in creator.GetComponentsInChildren<MeshFilter>())
            {
                var mesh=filter.sharedMesh;
                if (mesh==null || AssetDatabase.Contains(mesh)) continue;
                string path=Art+"/meshes/"+(filter.transform.parent.name+"_"+filter.name).Replace(" ","_").Replace("|","_")+".asset";
                var copy=Object.Instantiate(mesh); copy.name=Path.GetFileNameWithoutExtension(path);
                var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (existing!=null) {EditorUtility.CopySerialized(copy,existing); Object.DestroyImmediate(copy); copy=existing;}
                else AssetDatabase.CreateAsset(copy,path);
                copies[mesh]=copy; filter.sharedMesh=copy;
            }
            foreach (var collider in creator.GetComponentsInChildren<MeshCollider>())
                if (collider.sharedMesh!=null && copies.TryGetValue(collider.sharedMesh,out var copy)) collider.sharedMesh=copy;
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }
}
