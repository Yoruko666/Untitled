using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Untitled.Editor
{
    // Explicit authoring operation: ordinary scene loading and Play never rebuild buildings.
    public static class CaveSettlementIntegration
    {
        private const string ScenePath = "Assets/Scenes/CaveTerracedTerrainV2.unity";
        private const string Output = "Assets/Prefabs/Levels/CaveSettlement";
        private const string Art = "Assets/Art/Materials/CaveSettlement";
        private static readonly Vector3 Origin = new Vector3(0, 0, 200);
        private static readonly Dictionary<string, Material> Palette = new Dictionary<string, Material>();

        [Serializable] private sealed class Zone { public int Id; public float X, Z, Height; }
        [Serializable] private sealed class Layout { public Zone[] Zones; }

        [MenuItem("Untitled/Cave/Integrate Settlement Buildings")]
        public static void Integrate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before assembling buildings.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) throw new InvalidOperationException("Open CaveTerracedTerrainV2 first.");
            if (scene.GetRootGameObjects().Any(g => g.name == "SettlementBuildings"))
                throw new InvalidOperationException("Buildings already exist. Edit the saved instances; integration does not overwrite them.");
            foreach (string folder in new[] { Output, Art }) Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            Palette.Clear();
            CreateWeave();
            var layout = JsonUtility.FromJson<Layout>(File.ReadAllText("Assets/Art/Models/CaveTerracedV2/cave_terraced_layout.json"));
            Vector3 Position(int id)
            {
                var zone = layout.Zones.Single(p => p.Id == id);
                return new Vector3(zone.X, zone.Height, zone.Z);
            }
            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Assemble cave settlement and stealth spaces");
            var buildings = new GameObject("SettlementBuildings");
            buildings.transform.position = Origin;
            Undo.RegisterCreatedObjectUndo(buildings, "Add saved settlement buildings");
            Room(buildings.transform, "Tavern", "Tavern/goblin_tavern_room", Position(6));
            Room(buildings.transform, "Kitchen", "Kitchen/goblin_kitchen_room", Position(5));
            Room(buildings.transform, "Casino", "Casino/goblin_casino_room", Position(7));
            Room(buildings.transform, "Training", "Training/goblin_training_room", Position(3));
            Room(buildings.transform, "Elder", "ElderRoom/goblin_elder_room", Position(10));
            var spaces = new GameObject("SettlementGameplay | authored stealth spaces");
            spaces.transform.position = Origin;
            Undo.RegisterCreatedObjectUndo(spaces, "Add physical cover and route markers");
            DressSpaces(spaces.transform);
            RemoveObsoleteDressing(scene);
            FrameReviews(scene);
            Physics.SyncTransforms();
            ValidateReferences(buildings);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Undo.CollapseUndoOperations(undo);
            Debug.Log("CAVE_SETTLEMENT_SAVED: five full-size rooms, physical exits, cover and connected secret return; no AI added.");
        }

        private static void Room(Transform parent, string name, string source, Vector3 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/" + source + ".prefab");
            if (prefab == null) throw new InvalidOperationException("Missing demo prefab: " + source);
            var room = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(room, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            room.name = "cave_" + name.ToLowerInvariant();
            room.transform.position = Vector3.zero;
            room.transform.rotation = Quaternion.identity;
            room.transform.localScale = Vector3.one;
            UnifyMaterials(room);
            AdaptEntrances(room.transform, name);
            // A narrow rear canopy gives a silhouette without covering the playable interior.
            float width = name == "Tavern" ? 32 : name == "Training" ? 36 : name == "Casino" ? 28 : name == "Kitchen" ? 16 : 18;
            float depth = name == "Training" ? 28 : width;
            Box(room.transform, "rear_canvas_canopy", new Vector3(0, 3.7f, depth * .5f - 1),
                new Vector3(width - 1, .12f, 1.6f), Surface("cloth"), false);
            BalanceLights(room);
            string path = Output + "/" + room.name + ".prefab";
            var saved = PrefabUtility.SaveAsPrefabAsset(room, path);
            if (saved == null) throw new InvalidOperationException("Could not save map prefab: " + path);
            Object.DestroyImmediate(room);
            var placed = (GameObject)PrefabUtility.InstantiatePrefab(saved, parent.gameObject.scene);
            placed.transform.SetParent(parent, false);
            placed.transform.localPosition = position;
            placed.transform.localScale = Vector3.one;
        }

        private static Transform Find(Transform room, string name)
        {
            return room.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);
        }

        private static void Remove(Transform room, string name)
        {
            Object.DestroyImmediate(Find(room, name).gameObject);
        }

        private static void AdaptEntrances(Transform room, string name)
        {
            var access = new GameObject("MapEntrances | real openings").transform;
            access.SetParent(room, false);
            if (name == "Kitchen")
            {
                Remove(room, "kitchen_north_cave_wall");
                // Exit at x=-2 avoids the east cooking line; move the hanging meat rack clear.
                Wall(access, "kitchen_north_west", new Vector3(-5.75f, 0, 8), 4.5f, 3.3f, false);
                Wall(access, "kitchen_north_east", new Vector3(3.75f, 0, 8), 8.5f, 3.3f, false);
                var rack = Find(room, "meat_rack_hd2d");
                rack.localPosition = new Vector3(5.3f, rack.localPosition.y, -.5f);
                // Preserve the worktable and pot sizes; a 2.6m aisle separates them.
                foreach (string prop in new[] { "cauldron_stone_fire_ring_3d", "ExistingLargeCauldron_HD2D", "CauldronCookingFire" })
                    Find(room, prop).localPosition += Vector3.right * 1.4f;
                DoorMarker(access, "WestReceiving", new Vector3(-8, 0, -4.65f));
                DoorMarker(access, "SouthUnloading", new Vector3(0, 0, -8));
                DoorMarker(access, "NorthServing", new Vector3(-2, 0, 8));
            }
            else if (name == "Casino")
            {
                Remove(room, "casino_east_wall");
                Wall(access, "casino_east_north", new Vector3(14.25f, 0, 4.25f), 20, 2.9f, true);
                Wall(access, "casino_east_south", new Vector3(14.25f, 0, -11.9f), 4.7f, .7f, true);
                Remove(room, "office_west_north_partition");
                Remove(room, "office_west_door_pier");
                Remove(room, "casino_boss_door_frame");
                Wall(access, "office_north_partition", new Vector3(2, 0, 11.075f), 6.35f, 2.15f, true);
                Wall(access, "office_south_pier", new Vector3(2, 0, 4.45f), .9f, .85f, true);
                DoorMarker(access, "WestPublic", new Vector3(-14.25f, 0, 0));
                DoorMarker(access, "EastService", new Vector3(14.25f, 0, -8));
                DoorMarker(access, "BossOffice", new Vector3(2, 0, 6.4f));
            }
            else if (name == "Training")
            {
                Remove(room, "training_east_north_wall");
                Remove(room, "training_east_south_wall");
                Wall(access, "training_east_upper", new Vector3(18, 0, 8.9f), 10.2f, 3.05f, true);
                Wall(access, "training_east_lower", new Vector3(18, 0, -6.8f), 14.4f, .75f, true);
                foreach (var t in room.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("training_brace_18_")))
                {
                    if (t.name.EndsWith("3.4")) t.localPosition += Vector3.forward * .55f;
                    if (t.name.EndsWith("0.8")) t.localPosition -= Vector3.forward * .55f;
                }
                DoorMarker(access, "SouthPublic", new Vector3(0, 0, -14));
                DoorMarker(access, "EastPerimeter", new Vector3(18, 0, 2.1f));
            }
            else if (name == "Elder")
            {
                Remove(room, "secret_corridor_demo_end");
                Find(room, "secret_corridor_west_wall").localPosition -= Vector3.right * .2f;
                Remove(room, "elder_north_wall_before_secret_door");
                Wall(access, "elder_north_before_passage", new Vector3(-1.6f, 0, 9), 14.8f, 3.32f, false);
                Find(room, "ConcealingBookcase_PartiallySlidAside").localPosition -= Vector3.right * 1.5f;
                Find(room, "LargeBedReplacementSocket").localPosition -= Vector3.right * .45f;
                var chest = Find(room, "carved_bedside_chest_3d");
                chest.localPosition = new Vector3(.8f, chest.localPosition.y, chest.localPosition.z);
                foreach (var crate in room.GetComponentsInChildren<Transform>().Where(t => t.name == "supply_crate_3d"))
                {
                    Vector3 p = room.InverseTransformPoint(crate.position);
                    if (Mathf.Abs(p.x - 5.97f) < .1f && Mathf.Abs(p.z - 3.10f) < .1f)
                        crate.position = room.TransformPoint(p + new Vector3(-5.57f, 0, 1.3f));
                }
                DoorMarker(access, "WestMain", new Vector3(-9, 0, -6.75f));
                DoorMarker(access, "NorthSecret", new Vector3(7.46f, 0, 13.25f));
            }
            else
            {
                DoorMarker(access, "WestPublic", new Vector3(-16, 0, 3.9f));
                DoorMarker(access, "SouthService", new Vector3(-4.55f, 0, -16));
            }
        }

        private static void Wall(Transform parent, string name, Vector3 basePosition, float length, float height, bool alongZ)
        {
            var wall = new GameObject(name).transform;
            wall.SetParent(parent, false);
            wall.localPosition = basePosition;
            if (alongZ) wall.localRotation = Quaternion.Euler(0, 90, 0);
            for (int row = 0; row < Mathf.CeilToInt(height / .55f); row++)
            {
                int count = Mathf.CeilToInt(length / 1.1f);
                float h = Mathf.Min(.52f, height - row * .55f);
                for (int col = 0; col < count; col++)
                    Box(wall, "stone_" + row + "_" + col,
                        new Vector3(-length / 2 + (col + .5f) * length / count, row * .55f + h / 2, 0),
                        new Vector3(length / count - .025f, h, .65f), Surface("stone"), false);
            }
            var collision = new GameObject("FullHeightCollision");
            collision.transform.SetParent(wall, false);
            var collider = collision.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, 1.8f, 0);
            collider.size = new Vector3(length, 3.6f, .67f);
        }

        private static void DoorMarker(Transform parent, string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
        }

        private static void UnifyMaterials(GameObject room)
        {
            foreach (var renderer in room.GetComponentsInChildren<MeshRenderer>(true))
            {
                // Sprite atlases and direction-frame materials remain on their own UV contract.
                if (renderer.GetComponentInParent<HybridProp>() != null) continue;
                renderer.sharedMaterials = renderer.sharedMaterials.Select(source =>
                {
                    if (source == null) throw new InvalidOperationException("Missing material on " + renderer.name);
                    if (source.renderQueue >= 2450 || source.IsKeywordEnabled("_ALPHATEST_ON")) return source;
                    string finish = source.name.Substring(source.name.LastIndexOf('_') + 1).ToLowerInvariant();
                    if (finish == "flame" || finish == "ember") return source;
                    return Surface(finish, source);
                }).ToArray();
            }
        }

        private static Material Surface(string finish, Material source = null)
        {
            if (Palette.TryGetValue(finish, out var cached)) return cached;
            string family = finish.StartsWith("oak") || finish == "cork" ? "wood"
                : finish.StartsWith("stone") || finish == "sand" ? "stone" : "cloth";
            var basis = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/CaveTerracedV2/" + (family == "cloth" ? "earth" : family) + ".mat");
            var material = new Material(basis) { name = "settlement_" + finish };
            if (family == "cloth")
            {
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/settlement_weave_64.png"));
                material.SetColor("_BaseColor", source != null && source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : new Color(.45f, .28f, .18f));
                material.SetFloat("_TileScale", .25f);
            }
            else
            {
                Color tint = family == "wood" ? new Color(.85f,.75f,.62f) : new Color(.74f,.76f,.72f);
                if (finish.EndsWith("dark")) tint *= .7f;
                if (finish.EndsWith("light")) tint *= 1.12f;
                material.SetColor("_BaseColor", tint);
            }
            material.SetFloat("_PixelDensity", 16);
            material.SetFloat("_TextureStrength", .65f);
            material.SetFloat("_Smoothness", finish == "iron" || finish == "brass" || finish == "gold" ? .18f : .03f);
            material.SetFloat("_Metallic", finish == "iron" || finish == "brass" || finish == "gold" ? .55f : 0);
            string path = Art + "/" + material.name + ".mat";
            var saved = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (saved == null) AssetDatabase.CreateAsset(material, path);
            else { EditorUtility.CopySerialized(material, saved); Object.DestroyImmediate(material); material = saved; }
            Palette.Add(finish, material);
            return material;
        }

        private static void CreateWeave()
        {
            var texture = new Texture2D(64, 64, TextureFormat.RGB24, false);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                int shade = ((x / 2 + y / 2) % 2 == 0 ? 3 : 2) + (x % 8 == 0 || y % 8 == 0 ? -1 : 0);
                float value = .70f + shade * .07f;
                texture.SetPixel(x, y, new Color(value, value * .97f, value * .91f));
            }
            texture.Apply();
            string path = Art + "/settlement_weave_64.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void BalanceLights(GameObject room)
        {
            foreach (var light in room.GetComponentsInChildren<Light>(true))
            {
                light.intensity = Mathf.Min(light.intensity, 3.5f);
                light.range = Mathf.Min(light.range, 9);
                light.shadows = LightShadows.None;
            }
            foreach (var fire in room.GetComponentsInChildren<HybridFireLight>(true))
            {
                var settings = new SerializedObject(fire);
                settings.FindProperty("baseIntensity").floatValue = 3;
                settings.FindProperty("baseRange").floatValue = 8;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool solid = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (!solid) Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            return go;
        }

        private static void DressSpaces(Transform root)
        {
            var cover = new GameObject("PhysicalCover | walls crates and rock shoulders").transform;
            cover.SetParent(root, false);
            var cratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/Tavern/supply_crate_3d.prefab");
            foreach (var p in new[] { new Vector3(38,2.4f,-55), new Vector3(72,2.4f,-35), new Vector3(87,2.4f,-25),
                new Vector3(96,2.4f,8), new Vector3(44,2.4f,13), new Vector3(48,2.4f,31), new Vector3(83,2.4f,53) })
            {
                for (int i = 0; i < 3; i++)
                {
                    var crate = (GameObject)PrefabUtility.InstantiatePrefab(cratePrefab, root.gameObject.scene);
                    crate.transform.SetParent(cover, false);
                    crate.transform.localPosition = p + new Vector3(i == 1 ? 1.2f : 0, i == 2 ? 1 : 0, 0);
                    crate.transform.localRotation = Quaternion.Euler(0, i * 8, 0);
                    UnifyMaterials(crate);
                }
            }
            foreach (var p in new[] {new Vector3(31,2.4f,-15), new Vector3(49,2.4f,3), new Vector3(46,2.4f,42), new Vector3(20,9.6f,58)})
                Box(cover, "broken_screen_wall", p + Vector3.up * .9f, new Vector3(3.5f, 1.8f, .65f), Surface("stone"));
            var markers = new GameObject("ObservationAndGuardAnchors | planning only").transform;
            markers.SetParent(root, false);
            foreach (var p in new[] {new Vector3(12,2.4f,-34),new Vector3(-48,-2.4f,28.94f),new Vector3(92,2.4f,4),
                new Vector3(0,6,43),new Vector3(25,9.6f,54)}) DoorMarker(markers,"Observation_"+markers.childCount,p);
            foreach (var p in new[] {new Vector3(0,2.4f,-42),new Vector3(-28,-2.4f,28.94f),new Vector3(92,2.4f,34),
                new Vector3(-10,9.6f,51),new Vector3(27,9.6f,55.25f)}) DoorMarker(markers,"Guard_"+markers.childCount,p);
            // Populate the food hall with existing tables, preserving its western circulation strip.
            var dining = new GameObject("FeastFurniture").transform;
            dining.SetParent(root, false);
            var table = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/Tavern/round_table_3d.prefab");
            foreach (var p in new[] {new Vector3(25,2.4f,26),new Vector3(30,2.4f,29)})
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(table,root.gameObject.scene);
                go.transform.SetParent(dining,false);go.transform.localPosition=p;UnifyMaterials(go);
            }
            var hearth = new GameObject("PublicHearth").transform;
            hearth.SetParent(root,false);hearth.localPosition=new Vector3(38,2.4f,-8);
            var firePrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/Lighting/bone_fire_stand_hd2d.prefab");
            if(firePrefab!=null)
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(firePrefab,root.gameObject.scene);
                go.transform.SetParent(hearth,false);BalanceLights(go);
            }
            var marker=new GameObject("PrivateStoreObjectiveAnchor | no task behaviour");
            marker.transform.SetParent(root,false);marker.transform.localPosition=new Vector3(44.4f,9.7f,59.15f);
        }

        private static void RemoveObsoleteDressing(Scene scene)
        {
            var terrain = scene.GetRootGameObjects().Single(g => g.name.StartsWith("CaveTerrain")).transform;
            var rectangles = new[] {new Rect(-10,-26,36,36),new Rect(46,-50,20,20),new Rect(54,-24,32,32),new Rect(50,16,40,32),new Rect(27,51,22,26)};
            foreach (var renderer in terrain.GetComponentsInChildren<MeshRenderer>().ToArray())
            {
                string n=renderer.name;
                if (!(n.Contains("ReservationEdgeRock") || n.StartsWith("MineralCluster") || n.StartsWith("InteriorRockSpur") || n.StartsWith("LooseFloorStone"))) continue;
                var p=renderer.transform.position-Origin;
                if(rectangles.Any(r=>r.Contains(new Vector2(p.x,p.z)))) renderer.gameObject.SetActive(false);
            }
            var lamps=terrain.Find("Wayfinding | warm pixel braziers");
            if(lamps!=null) foreach(Transform lamp in lamps)
            {
                var p=lamp.position-Origin;
                if(rectangles.Any(r=>r.Contains(new Vector2(p.x,p.z)))) lamp.gameObject.SetActive(false);
            }
        }

        private static void FrameReviews(Scene scene)
        {
            var root=scene.GetRootGameObjects().Single(g=>g.name.StartsWith("CaveTerrain")).transform;
            var top=root.GetComponentsInChildren<Camera>().First(c=>c.name.StartsWith("Top Camera"));
            top.transform.position=Origin+new Vector3(17,180,4);top.orthographicSize=112;
            var detail=root.GetComponentsInChildren<Camera>().First(c=>c.name.StartsWith("Detail Camera"));
            detail.transform.rotation=Quaternion.Euler(35,-5,0);
            detail.transform.position=Origin+new Vector3(35,4,0)-detail.transform.forward*165;
            foreach(var old in root.GetComponentsInChildren<Camera>().Where(c=>c.name.StartsWith("Building Review")).ToArray()) Object.DestroyImmediate(old.gameObject);
            foreach(var sample in new[] {new Vector3(8,2.4f,-19),new Vector3(56,2.4f,-43),new Vector3(70,2.4f,-18),
                new Vector3(70,2.4f,21),new Vector3(38,9.6f,57)})
            {
                var go=new GameObject("Building Review "+root.GetComponentsInChildren<Camera>().Length);
                go.transform.SetParent(root,false);
                var camera=go.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=36.98f;
                camera.nearClipPlane=.15f;camera.farClipPlane=500;camera.allowHDR=true;
                camera.transform.rotation=Quaternion.Euler(18.1f,-5,0);
                camera.transform.position=Origin+sample+new Vector3(0,1.74f,0)-camera.transform.forward*25.72f;
                camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
                camera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;
            }
        }

        private static void ValidateReferences(GameObject buildings)
        {
            foreach(var renderer in buildings.GetComponentsInChildren<MeshRenderer>(true))
                if(renderer.sharedMaterials.Any(m=>m==null || ShaderUtil.ShaderHasError(m.shader)))
                    throw new InvalidOperationException("Invalid settlement material: "+renderer.name);
            if(buildings.GetComponentsInChildren<Camera>(true).Length!=0 || buildings.GetComponentsInChildren<Volume>(true).Length!=0)
                throw new InvalidOperationException("Demo presentation must not enter the map prefab.");
        }

        public static void BatchIntegrate()
        {
            EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            CaveTerracedTerrainV2Editor.ReimportLandform();
            Integrate();
            CaveTerracedTerrainV2Editor.ExportPreview();
        }
    }
}
