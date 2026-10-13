using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Untitled.Editor
{
    [InitializeOnLoad]
    public static class MapRiverbankTask
    {
        private const string TaskFolder = "C:/Users/TempAdmin/AppData/Local/Temp/untitled-riverbank-sample";
        private const string MaterialFolder = "Assets/Art/Materials/RiverbankSample";
        private const string MeshPath = "Assets/Art/Models/CaveHD2D/riverbank_sample_meshes.asset";
        private static readonly Vector3 Origin = new Vector3(140f, 0f, 320f);
        private static Terrain terrain;
        private static Material rock, moss, earth, wet, timber, cord, stem, cap;
        private static Mesh boulder, mushroomCap;

        static MapRiverbankTask()
        {
            EditorApplication.delayCall += Run;
        }

        [Serializable]
        private class TerrainSnapshot
        {
            public Vector3 Position;
            public Vector3 Size;
            public int Resolution;
            public string DataPath;
            public float[] Heights;
        }

        private static void Run()
        {
            bool repair = File.Exists(TaskFolder + "/finish.request");
            bool build = File.Exists(TaskFolder + "/build.request");
            string request = TaskFolder + (repair ? "/finish.request" : build ? "/build.request" : "/inspect.request");
            if (!File.Exists(request)) return;
            File.Delete(request);
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException("Exit Play Mode first.");
                var scene = EditorSceneManager.GetActiveScene();
                if (scene.path != "Assets/Scenes/Map.unity")
                    throw new InvalidOperationException("Map must be the active scene.");
                terrain = UnityEngine.Object.FindObjectOfType<Terrain>();
                if (repair)
                {
                    FinishSample();
                    return;
                }
                if (build)
                {
                    BuildSample();
                    return;
                }
                var data = terrain.terrainData;
                int resolution = data.heightmapResolution;
                var heights = data.GetHeights(0, 0, resolution, resolution);
                var flat = new float[resolution * resolution];
                for (int z = 0; z < resolution; z++)
                    for (int x = 0; x < resolution; x++)
                        flat[z * resolution + x] = heights[z, x] * data.size.y;
                File.WriteAllText(TaskFolder + "/terrain.json", JsonUtility.ToJson(new TerrainSnapshot
                {
                    Position = terrain.transform.position,
                    Size = data.size,
                    Resolution = resolution,
                    DataPath = AssetDatabase.GetAssetPath(data),
                    Heights = flat
                }));
                Debug.Log("Riverbank sample: Terrain snapshot saved.");
            }
            catch (Exception exception)
            {
                File.WriteAllText(TaskFolder + "/error.txt", exception.ToString());
                Debug.LogException(exception);
            }
        }

        private static Material Clone(string source, string name, Color color)
        {
            var material = new Material(AssetDatabase.LoadAssetAtPath<Material>(source)) { name = name };
            material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.06f);
            AssetDatabase.CreateAsset(material, MaterialFolder + "/" + name + ".mat");
            return material;
        }

        private static Transform Group(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static GameObject Model(string name, Mesh mesh, Material material, Transform parent)
        {
            var go = Group(name, parent).gameObject;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool collide = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (!collide) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static float Height(float x, float z) => terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y;

        private static void SaveMesh(Mesh mesh)
        {
            mesh.RecalculateBounds();
            if (AssetDatabase.Contains(mesh))
            {
                EditorUtility.SetDirty(mesh);
                return;
            }
            if (AssetDatabase.LoadMainAssetAtPath(MeshPath) == null) AssetDatabase.CreateAsset(mesh, MeshPath);
            else AssetDatabase.AddObjectToAsset(mesh, MeshPath);
        }

        private static GameObject MakeRock(string name, Material material)
        {
            var go = Model(name, boulder, material, null);
            go.AddComponent<MeshCollider>().sharedMesh = boulder;
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Terrain/" + name + ".prefab");
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject Place(GameObject prefab, Transform parent, Vector3 position, Vector3 scale, float yaw = 0f)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = scale;
            return go;
        }

        private static float Shore(float z, float height, bool right)
        {
            float result = 0f;
            for (float x = 80f; x <= 210f; x += 0.2f)
            {
                if (Height(x, z) >= height) continue;
                result = x;
                if (!right) break;
            }
            return result;
        }

        private static void BuildSample()
        {
            if (GameObject.Find("MapRiverbankSample") != null)
                throw new InvalidOperationException("The sample already exists; preserve manual edits.");
            if (AssetDatabase.LoadMainAssetAtPath(MeshPath) != null)
                throw new InvalidOperationException("Sample assets already exist; do not overwrite.");
            Directory.CreateDirectory(MaterialFolder);
            Directory.CreateDirectory("Assets/Prefabs/Props/Nature");
            AssetDatabase.Refresh();
            const string cave = "Assets/Art/Materials/CaveHD2D/";
            rock = Clone(cave + "cliff_module_rock.mat", "riverbank_rock", new Color(0.56f, 0.53f, 0.45f));
            rock.SetFloat("_TextureStrength", 0.40f);
            moss = Clone(cave + "cliff_module_rock.mat", "riverbank_moss_rock", new Color(0.31f, 0.38f, 0.26f));
            moss.SetFloat("_TextureStrength", 0.25f);
            earth = Clone(cave + "Soil.mat", "riverbank_earth", new Color(0.48f, 0.36f, 0.23f));
            earth.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/CaveHD2D/cave_ground_path_candidate.png"));
            earth.SetFloat("_TextureStrength", 0.35f);
            earth.SetFloat("_TileScale", 0.25f);
            wet = Clone(cave + "WetGround.mat", "riverbank_wet", new Color(0.29f, 0.35f, 0.27f));
            wet.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/CaveHD2D/cave_ground_wet_candidate.png"));
            wet.SetFloat("_TextureStrength", 0.27f);
            wet.SetFloat("_TileScale", 0.25f);
            timber = Clone(cave + "Timber.mat", "riverbank_timber", new Color(0.38f, 0.26f, 0.14f));
            cord = Clone(cave + "settlement_rope.mat", "riverbank_rope", new Color(0.55f, 0.45f, 0.28f));
            stem = Clone(cave + "settlement_mushroom.mat", "riverbank_mushroom_stem", new Color(0.54f, 0.48f, 0.33f));
            cap = Clone(cave + "settlement_mushroom.mat", "riverbank_mushroom_cap", new Color(0.40f, 0.47f, 0.28f));
            var modules = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Models/CaveHD2D/settlement_modules.asset");
            boulder = modules.OfType<Mesh>().Single(m => m.name == "FacetedRock");
            mushroomCap = modules.OfType<Mesh>().Single(m => m.name == "MushroomCap");
            GameObject rockPrefab = MakeRock("riverbank_rock_01", rock);
            GameObject mossPrefab = MakeRock("riverbank_moss_rock_01", moss);
            GameObject mushroomPrefab = MakeMushrooms();

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Place riverbank scene sample");
            var root = new GameObject("MapRiverbankSample");
            root.transform.position = Origin;
            CreateGround(root.transform);
            CreateWater(root.transform);
            CreateBridge(root.transform);

            var rocks = Group("RockBanks", root.transform);
            var plants = Group("MossAndMushrooms", root.transform);
            for (int i = 0; i < 10; i++)
            {
                float z = 289f + i * 7f;
                if (Mathf.Abs(z - 320f) < 6f) continue;
                for (int side = 0; side < 2; side++)
                {
                    float x = Shore(z, 19f, side == 1) + (side == 0 ? -0.4f : 0.4f);
                    float size = 1.9f + (i % 3) * 0.75f;
                    Place(i % 3 == 0 ? mossPrefab : rockPrefab, rocks,
                        new Vector3(x, Height(x, z) - 0.7f, z), new Vector3(size, size * 0.65f, size * 1.1f), i * 37f + side * 83f);
                }
            }
            for (int i = 0; i < 5; i++)
            {
                float z = 294f + i * 13f;
                if (Mathf.Abs(z - 320f) < 6f) continue;
                float x = Shore(z, 27f, true);
                Place(rockPrefab, rocks, new Vector3(x + 1.6f, Height(x, z) - 5f, z),
                    new Vector3(5.0f, 8.0f, 6.0f), 22f + i * 51f);
            }
            foreach (Vector2 coord in new[] { new Vector2(102, 307), new Vector2(104, 334), new Vector2(92, 345),
                new Vector2(185, 301), new Vector2(188, 342), new Vector2(193, 333) })
            {
                Place(mushroomPrefab, plants, new Vector3(coord.x, Height(coord.x, coord.y), coord.y), Vector3.one, coord.x);
                Place(mossPrefab, plants, new Vector3(coord.x + 2f, Height(coord.x + 2f, coord.y) - 0.15f, coord.y),
                    new Vector3(1.8f, 0.3f, 1.4f), coord.y);
            }

            var props = Group("BridgeheadProps", root.transform);
            string propsPath = "Assets/Prefabs/Props/";
            foreach (Vector2 coord in new[] { new Vector2(100, 315), new Vector2(184, 324) })
                Place(AssetDatabase.LoadAssetAtPath<GameObject>(propsPath + "Lighting/wood_tripod_lit_hd2d.prefab"), props,
                    new Vector3(coord.x, Height(coord.x, coord.y), coord.y), Vector3.one * 1.4f);
            Place(AssetDatabase.LoadAssetAtPath<GameObject>(propsPath + "Kitchen/firewood_stack_hd2d.prefab"), props,
                new Vector3(96, Height(96, 312), 312), Vector3.one * 1.3f);
            Place(AssetDatabase.LoadAssetAtPath<GameObject>(propsPath + "TotemsBones/rune_banner_hd2d.prefab"), props,
                new Vector3(188, Height(188, 327), 327), Vector3.one * 1.8f);

            var player = new GameObject("SamplePlayer");
            player.transform.SetParent(root.transform, true);
            Vector3 spawn = new Vector3(95f, Height(95f, 320f) + 0.08f, 320f);
            player.transform.position = spawn;
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.5f, 0f);
            controller.stepOffset = 0.2f;
            controller.skinWidth = 0.025f;
            var visual = Model("Visual", AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Models/GoblinBillboard.asset"),
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/FirstRoom/GoblinLit.mat"), player.transform);
            visual.AddComponent<SpriteBillboard>();
            var walker = player.AddComponent<MapSamplePlayer>();
            var serialized = new SerializedObject(walker);
            serialized.FindProperty("spawnPoint").vector3Value = spawn;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAssetAndConnect(root, "Assets/Prefabs/Terrain/riverbank_sample_01.prefab", InteractionMode.AutomatedAction);
            Camera camera = Camera.main;
            Undo.RecordObject(camera.transform, "Frame riverbank sample");
            Undo.RecordObject(camera, "Frame riverbank sample");
            camera.fieldOfView = 40f;
            Vector3 cameraOffset = new Vector3(0f, 12f, -20f);
            camera.transform.SetPositionAndRotation(spawn + Vector3.up + cameraOffset, Quaternion.LookRotation(-cameraOffset));
            visual.transform.rotation = camera.transform.rotation;
            serialized = new SerializedObject(walker);
            serialized.FindProperty("sampleCamera").objectReferenceValue = camera;
            serialized.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(walker);
            Undo.RegisterCreatedObjectUndo(root, "Place riverbank scene sample");
            Undo.CollapseUndoOperations(undoGroup);
            AssetDatabase.SaveAssets();
            Physics.SyncTransforms();
            ValidateBridge();
            EditorSceneManager.MarkSceneDirty(root.scene);
            EditorSceneManager.SaveScene(root.scene);
            Selection.activeGameObject = root;
            SceneView.lastActiveSceneView?.LookAt(new Vector3(142f, 24f, 320f), Quaternion.Euler(35f, 0f, 0f), 58f);
            RenderPreview(camera, TaskFolder + "/detail.png");
            Vector3 savedPosition = camera.transform.position;
            Quaternion savedRotation = camera.transform.rotation;
            camera.transform.SetPositionAndRotation(new Vector3(143f, 73f, 226f), Quaternion.LookRotation(new Vector3(0f, -49f, 94f)));
            RenderPreview(camera, TaskFolder + "/overview.png");
            camera.transform.SetPositionAndRotation(savedPosition, savedRotation);
            File.WriteAllText(TaskFolder + "/complete.txt", "Sample saved in Map.unity. TerrainData untouched. Bridge collision rays passed.\n");
            Debug.Log("Riverbank sample saved; bridge collision rays passed.");
        }

        private static GameObject MakeMushrooms()
        {
            var root = new GameObject("riverbank_mushroom_cluster_01");
            for (int i = 0; i < 3; i++)
            {
                float size = 0.65f + i * 0.22f;
                var cluster = Group("Mushroom_" + i, root.transform);
                cluster.localPosition = new Vector3(i * 0.7f, 0f, (i % 2) * 0.6f);
                var stalk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stalk.name = "Stem";
                stalk.transform.SetParent(cluster, false);
                stalk.transform.localPosition = Vector3.up * size * 0.4f;
                stalk.transform.localScale = new Vector3(size * 0.28f, size * 0.4f, size * 0.28f);
                stalk.GetComponent<MeshRenderer>().sharedMaterial = stem;
                UnityEngine.Object.DestroyImmediate(stalk.GetComponent<Collider>());
                var head = Model("Cap", mushroomCap, cap, cluster);
                head.transform.localPosition = Vector3.up * size * 0.75f;
                head.transform.localScale = Vector3.one * size;
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Props/Nature/riverbank_mushroom_cluster_01.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static void CreateGround(Transform parent)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new[] { new List<int>(), new List<int>(), new List<int>() };
            float step = terrain.terrainData.size.x / (terrain.terrainData.heightmapResolution - 1);
            int startX = Mathf.FloorToInt(85f / step), startZ = Mathf.FloorToInt(286f / step);
            int width = Mathf.CeilToInt(196f / step) - startX;
            int depth = Mathf.CeilToInt(355f / step) - startZ;
            for (int z = 0; z <= depth; z++)
                for (int x = 0; x <= width; x++)
                {
                    float worldX = (startX + x) * step, worldZ = (startZ + z) * step;
                    vertices.Add(new Vector3(worldX, Height(worldX, worldZ) + 0.25f, worldZ) - Origin);
                    normals.Add(terrain.terrainData.GetInterpolatedNormal(worldX / 500f, worldZ / 500f));
                    uv.Add(new Vector2(worldX, worldZ));
                }
            for (int z = 0; z < depth; z++)
                for (int x = 0; x < width; x++)
                {
                    float worldX = (startX + x + 0.5f) * step, worldZ = (startZ + z + 0.5f) * step;
                    float edge = Mathf.Pow((worldX - 140f) / 56f, 2f) + Mathf.Pow((worldZ - 320f) / 36f, 2f);
                    if (edge > 1f + 0.06f * Mathf.Sin(worldX * 0.45f + worldZ * 0.27f)) continue;
                    float height = Height(worldX, worldZ);
                    float slope = terrain.terrainData.GetSteepness(worldX / 500f, worldZ / 500f);
                    int material = slope > 35f ? 2 : height < 19.4f ? 1 : 0;
                    int a = z * (width + 1) + x, b = a + 1, c = a + width + 1, d = c + 1;
                    triangles[material].AddRange(new[] { a, c, b, b, c, d });
                }
            var mesh = AssetDatabase.LoadAllAssetsAtPath(MeshPath).OfType<Mesh>().FirstOrDefault(m => m.name == "RiverbankTerrainOverlay");
            if (mesh == null) mesh = new Mesh { name = "RiverbankTerrainOverlay" };
            else mesh.Clear();
            mesh.subMeshCount = 3;
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv);
            mesh.SetColors(Enumerable.Repeat(Color.black, vertices.Count).ToList());
            for (int i = 0; i < 3; i++) mesh.SetTriangles(triangles[i], i);
            SaveMesh(mesh);
            var existing = parent.Find("TerrainSurfaceSample");
            var go = existing != null ? existing.gameObject : Model("TerrainSurfaceSample", mesh, earth, parent);
            go.GetComponent<MeshRenderer>().sharedMaterials = new[] { earth, wet, rock };
            go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        private static void CreateWater(Transform parent)
        {
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            for (int i = 0; i <= 80; i++)
            {
                float z = 280f + i;
                float left = Shore(z, 18.25f, false), right = Shore(z, 18.25f, true);
                vertices.Add(new Vector3(left, 18.25f, z) - Origin);
                vertices.Add(new Vector3(right, 18.25f, z) - Origin);
                uv.Add(new Vector2(0f, i / 80f)); uv.Add(new Vector2(1f, i / 80f));
                if (i < 80) triangles.AddRange(new[] { i * 2, i * 2 + 2, i * 2 + 1, i * 2 + 1, i * 2 + 2, i * 2 + 3 });
            }
            var mesh = new Mesh { name = "RiverbankWaterRibbon" };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
            mesh.SetColors(Enumerable.Repeat(new Color(1, 1, 0, 0), vertices.Count).ToList());
            mesh.RecalculateNormals(); SaveMesh(mesh);
            var material = Clone("Assets/Art/Materials/CaveHD2D/River.mat", "riverbank_water", Color.white);
            material.SetFloat("_CellSize", 50f);
            material.SetFloat("_PixelsPerUnit", 8f);
            material.SetFloat("_Smoothness", 0.45f);
            var go = Model("RiverWaterSample", mesh, material, parent);
            go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        private static float DeckY(float x) => Mathf.Lerp(20.14f, 33.14f, (x - 100f) / 70f);

        private static void CreateBridge(Transform parent)
        {
            var bridge = Group("riverbank_bridge_01", parent);
            bridge.position = new Vector3(100f, 20.14f, 320f);
            float angle = Mathf.Atan2(13f, 70f) * Mathf.Rad2Deg;
            float length = Mathf.Sqrt(70f * 70f + 13f * 13f);
            var slope = Group("SlopedDeck", bridge);
            slope.localRotation = Quaternion.Euler(0f, 0f, angle);
            int count = Mathf.CeilToInt(length / 1.6f);
            for (int i = 0; i < count; i++)
                Box("Plank_" + i.ToString("00"), slope, new Vector3((i + 0.5f) * length / count, -0.14f, 0f),
                    new Vector3(length / count - 0.035f, 0.28f, 5.4f), timber);
            var deckCollider = slope.gameObject.AddComponent<BoxCollider>();
            deckCollider.center = new Vector3(length * 0.5f, -0.14f, 0f);
            deckCollider.size = new Vector3(length, 0.28f, 5.4f);
            for (int side = -1; side <= 1; side += 2)
            {
                Box("TimberBeam", slope, new Vector3(length * 0.5f, -0.48f, side * 2.1f), new Vector3(length, 0.45f, 0.38f), timber);
                for (int i = 0; i <= 14; i++)
                {
                    float x = 70f * i / 14f, y = 13f * i / 14f;
                    Box("RailPost", bridge, new Vector3(x, y + 0.62f, side * 2.8f), new Vector3(0.26f, 1.55f, 0.26f), timber);
                    if (i == 14) continue;
                    for (int section = 0; section < 4; section++)
                    {
                        float t0 = (i + section / 4f) / 14f, t1 = (i + (section + 1) / 4f) / 14f;
                        Vector3 a = new Vector3(70f * t0, 13f * t0 + 1.25f - Mathf.Sin(section / 4f * Mathf.PI) * 0.25f, side * 2.8f);
                        Vector3 b = new Vector3(70f * t1, 13f * t1 + 1.25f - Mathf.Sin((section + 1) / 4f * Mathf.PI) * 0.25f, side * 2.8f);
                        var rail = Box("Rope", bridge, (a + b) * 0.5f, new Vector3(Vector3.Distance(a, b), 0.10f, 0.10f), cord);
                        rail.transform.localRotation = Quaternion.FromToRotation(Vector3.right, b - a);
                    }
                }
                var guard = Group("SideCollision", slope).gameObject.AddComponent<BoxCollider>();
                guard.center = new Vector3(length * 0.5f, 0.6f, side * 2.8f);
                guard.size = new Vector3(length, 1.5f, 0.18f);
            }
            for (int i = 1; i <= 5; i++)
            {
                float x = 70f * i / 6f;
                foreach (float z in new[] { -2.05f, 2.05f })
                {
                    float bottom = Height(100f + x, 320f + z) - 0.7f - 20.14f;
                    float top = 13f * i / 6f - 0.35f;
                    Box("BridgePile", bridge, new Vector3(x, (bottom + top) * 0.5f, z), new Vector3(0.65f, top - bottom, 0.65f), timber);
                }
            }
            PrefabUtility.SaveAsPrefabAssetAndConnect(bridge.gameObject, "Assets/Prefabs/Terrain/riverbank_bridge_01.prefab", InteractionMode.AutomatedAction);
        }

        private static void ValidateBridge()
        {
            int checks = 0;
            for (int i = 0; i <= 35; i++)
            {
                float x = 100f + i * 2f;
                foreach (float z in new[] { 318f, 320f, 322f })
                {
                    if (!Physics.Raycast(new Vector3(x, DeckY(x) + 2f, z), Vector3.down, out RaycastHit hit, 4f)
                        || Mathf.Abs(hit.point.y - DeckY(x)) > 0.15f)
                        throw new InvalidOperationException("Bridge surface blocked or missing at " + x + "," + z);
                    checks++;
                }
            }
            File.WriteAllText(TaskFolder + "/validation.txt", checks + " bridge surface rays passed. Grade: 10.5 degrees.\n");
        }

        private static void RenderPreview(Camera camera, string path)
        {
            var target = new RenderTexture(1280, 720, 24);
            RenderTexture previous = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previous;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void FinishSample()
        {
            var root = GameObject.Find("MapRiverbankSample");
            earth = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/riverbank_earth.mat");
            wet = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/riverbank_wet.mat");
            rock = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/riverbank_rock.mat");
            CreateGround(root.transform);
            AssetDatabase.SaveAssets();
            Physics.SyncTransforms();
            var probe = new GameObject("Temporary bridge crossing probe");
            var sampleController = root.transform.Find("SamplePlayer").GetComponent<CharacterController>();
            sampleController.enabled = false;
            try
            {
                probe.transform.position = new Vector3(95f, Height(95f, 320f) + 0.08f, 320f);
                var controller = probe.AddComponent<CharacterController>();
                controller.height = 1f; controller.radius = 0.3f;
                controller.center = new Vector3(0f, 0.5f, 0f);
                controller.stepOffset = 0.2f; controller.skinWidth = 0.025f;
                float fallingSpeed = 0f;
                for (int i = 0; i < 850; i++)
                {
                    fallingSpeed = controller.isGrounded && fallingSpeed < 0f ? -2f : fallingSpeed + Physics.gravity.y * 0.016f;
                    controller.Move(new Vector3(7.625f * 0.016f, fallingSpeed * 0.016f, 0f));
                    if (probe.transform.position.y < 18.25f)
                        throw new InvalidOperationException("Character fell during bridge crossing.");
                    if (probe.transform.position.x >= 179f) break;
                }
                if (probe.transform.position.x < 179f)
                    throw new InvalidOperationException("Character blocked during bridge crossing: " + probe.transform.position);
                for (int i = 0; i < 850; i++)
                {
                    fallingSpeed = controller.isGrounded && fallingSpeed < 0f ? -2f : fallingSpeed + Physics.gravity.y * 0.016f;
                    controller.Move(new Vector3(-7.625f * 0.016f, fallingSpeed * 0.016f, 0f));
                    if (probe.transform.position.y < 18.25f)
                        throw new InvalidOperationException("Character fell on return crossing.");
                    if (probe.transform.position.x <= 95f) break;
                }
                if (probe.transform.position.x > 95f)
                    throw new InvalidOperationException("Character blocked on return crossing.");
                File.AppendAllText(TaskFolder + "/validation.txt", "CharacterController crossed from X=95 to X=179 and back, with gravity.\n");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(probe);
                sampleController.enabled = true;
            }
            var camera = Camera.main;
            const string samplePath = "Assets/Prefabs/Terrain/riverbank_sample_01.prefab";
            var prefabContents = PrefabUtility.LoadPrefabContents(samplePath);
            try
            {
                var playerSettings = new SerializedObject(prefabContents.GetComponentInChildren<MapSamplePlayer>());
                playerSettings.FindProperty("sampleRoot").objectReferenceValue = prefabContents.transform;
                playerSettings.FindProperty("spawnPoint").vector3Value = new Vector3(-45f, 20.076f, 0f);
                playerSettings.FindProperty("minCoord").vector2Value = new Vector2(-55f, -34f);
                playerSettings.FindProperty("maxCoord").vector2Value = new Vector2(56f, 35f);
                playerSettings.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefabContents, samplePath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabContents);
            }
            var scenePlayerSettings = new SerializedObject(root.GetComponentInChildren<MapSamplePlayer>());
            scenePlayerSettings.FindProperty("sampleRoot").objectReferenceValue = root.transform;
            scenePlayerSettings.FindProperty("spawnPoint").vector3Value = root.transform.InverseTransformPoint(root.transform.Find("SamplePlayer").position);
            scenePlayerSettings.FindProperty("minCoord").vector2Value = new Vector2(-55f, -34f);
            scenePlayerSettings.FindProperty("maxCoord").vector2Value = new Vector2(56f, 35f);
            scenePlayerSettings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(root.GetComponentInChildren<MapSamplePlayer>());
            PrefabUtility.RecordPrefabInstancePropertyModifications(root.transform.Find("SamplePlayer/Visual"));
            EditorSceneManager.SaveScene(root.scene);
            RenderPreview(camera, TaskFolder + "/detail.png");
            Vector3 savedPosition = camera.transform.position;
            Quaternion savedRotation = camera.transform.rotation;
            camera.transform.SetPositionAndRotation(new Vector3(143f, 73f, 226f), Quaternion.LookRotation(new Vector3(0f, -49f, 94f)));
            RenderPreview(camera, TaskFolder + "/overview.png");
            camera.transform.SetPositionAndRotation(savedPosition, savedRotation);
            File.WriteAllText(TaskFolder + "/finished.txt", "Terrain overlay aligned; bidirectional crossing passed.\n");
            Debug.Log("Riverbank sample: terrain overlay aligned and both bridge crossings passed.");
        }
    }
}
