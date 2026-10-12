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

namespace Untitled.Editor
{
    // Explicit creation only; saved scene edits are never replaced on load or Play.
    public static class CaveTerracedTerrainV2Editor
    {
        private const string ScenePath = "Assets/Scenes/CaveTerracedTerrainV2.unity";
        private const string Models = "Assets/Art/Models/CaveTerracedV2";
        private const string Materials = "Assets/Art/Materials/CaveTerracedV2";
        private const string Prefabs = "Assets/Prefabs/Terrain/CaveTerracedV2";
        private const string ProfilePath = "Assets/Settings/CaveTerracedV2Presentation.asset";
        private static readonly Vector3 Origin = new Vector3(0, 0, 200);

        [Serializable]
        private sealed class LayoutPoint
        {
            public int Id;
            public string Name;
            public float X;
            public float Z;
            public float Height;
            public float HalfWidth;
        }

        [Serializable]
        private sealed class LayoutData
        {
            public LayoutPoint[] Zones;
            public LayoutPoint[] RouteSamples;
            public LayoutPoint[] RouteAnchors;
            public LayoutPoint[] RiverCourse;
            public LayoutPoint[] WestFork;
            public LayoutPoint[] EastFork;
            public float RiverStart;
            public float RiverEnd;
            public float RiverHalfWidth;
            public float[] BridgeZs;
            public DecorationPoint[] Decorations;
            public float LowlandHeight;
            public float HighlandHeight;
            public float WaterHeight;
        }

        [Serializable]
        private sealed class DecorationPoint
        {
            public string Prefab;
            public string Name;
            public float X;
            public float Z;
            public float Height;
            public float Scale;
            public float Yaw;
        }

        private static LayoutData ReadLayout()
        {
            var data = JsonUtility.FromJson<LayoutData>(File.ReadAllText(Models + "/cave_terraced_layout.json"));
            if (data == null || data.Zones == null || data.Zones.Length != 15 || data.RiverCourse == null)
                throw new InvalidOperationException("Export the Blender layout and metadata before importing.");
            return data;
        }

        private static bool IsRouteOverlay(string name)
        {
            return name.Contains("Sediment") || name == "A_CentralPublicApproach"
                || name == "C_EasternPerimeterRoute" || name == "B_WestBankSupplyRoute";
        }

        [MenuItem("Untitled/Cave/Create Combined Terrain V2")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before creating terrain.");
            if (File.Exists(ScenePath))
                throw new InvalidOperationException("Open the saved CaveTerracedTerrain scene to edit it. Creation does not overwrite it.");
            Directory.CreateDirectory(Materials);
            Directory.CreateDirectory(Prefabs);
            AssetDatabase.Refresh();
            var materialMap = CreateMaterials();
            ConfigureModel(Models + "/cave_terraced_landform.fbx", materialMap);
            ConfigureModel(Models + "/cave_rock_modules.fbx", materialMap);
            CreateModulePrefabs(materialMap);

            // Additive creation preserves the user's unsaved scene in memory.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var root = new GameObject("CaveTerrain | River -2.4 / Main 0 / Hall +4.8");
            root.transform.position = Origin;
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "/cave_terraced_landform.fbx");
            var terrain = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
            PrefabUtility.UnpackPrefabInstance(terrain, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            terrain.transform.SetParent(root.transform, false);
            terrain.transform.localRotation = Quaternion.Euler(0,180,0) * terrain.transform.localRotation;
            terrain.name = "EditableLandform";
            foreach (var renderer in terrain.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m => materialMap[m.name]).ToArray();
                renderer.shadowCastingMode = renderer.name == "RiverSurface" || IsRouteOverlay(renderer.name)
                    ? ShadowCastingMode.Off : ShadowCastingMode.On;
                renderer.receiveShadows = true;
                var filter = renderer.GetComponent<MeshFilter>();
                bool smallDetail = renderer.name.StartsWith("WaterlineStone") || renderer.name.StartsWith("DampMoss")
                    || renderer.name.Contains("EdgeStone") || renderer.name.StartsWith("LooseFloorStone")
                    || IsRouteOverlay(renderer.name);
                if (renderer.name != "RiverSurface" && !smallDetail)
                    renderer.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.BatchingStatic);
                // Keep optional route overlays available for layout review; natural ground is the default.
                if (IsRouteOverlay(renderer.name)) renderer.gameObject.SetActive(false);
            }
            AddRiverBarriers(root.transform);
            var landmarks = new GameObject("LayoutAnchors | terrain only");
            landmarks.transform.SetParent(root.transform, false);
            Anchor(landmarks.transform, "Entrance", new Vector3(4, 0, -18));
            Anchor(landmarks.transform, "CentralClearing", new Vector3(4, 0, -1));
            Anchor(landmarks.transform, "HallTerrace", new Vector3(-1.5f, 4.8f, 26.5f));
            Anchor(landmarks.transform, "ElderTerrace", new Vector3(18.3f, 4.8f, 27.2f));
            Anchor(landmarks.transform, "Riverbank", new Vector3(-24, -2.4f, 2));
            Anchor(landmarks.transform, "ResidentialShelf", new Vector3(19, 1.2f, 7));

            Lighting(root.transform);
            Presentation(root.transform);
            Camera overview = CameraAt(root.transform, "Main Camera | terrain overview", new Vector3(0, 1.2f, 3), 39, -8, 81);
            overview.tag = "MainCamera";
            overview.depth = 20;
            Camera detail = CameraAt(root.transform, "Detail Camera | central terrace", new Vector3(2, 1, 8), 33, -5, 38);
            detail.enabled = false;
            var listener = overview.gameObject.AddComponent<AudioListener>();
            listener.enabled = false;
            UpdateLayoutScene(root.transform);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.22f, 0.30f, 0.34f);
            RenderSettings.ambientEquatorColor = new Color(0.18f, 0.22f, 0.25f);
            RenderSettings.ambientGroundColor = new Color(0.06f, 0.09f, 0.10f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.065f, 0.089f, 0.10f);
            RenderSettings.fogDensity = 0.0035f;
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = root;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.sceneLighting = true;
                SceneView.lastActiveSceneView.LookAt(Origin + new Vector3(0, 1, 4), Quaternion.Euler(39, -8, 0), 63);
            }
            CheckHeights(terrain);
            Debug.Log("CaveTerracedTerrain saved: editable Blender landform, three elevation levels, two crossings, stairs and winding ascent.");
        }

        private static Dictionary<string, Material> CreateMaterials()
        {
            var result = new Dictionary<string, Material>();
            void Terrain(string name, string texture, Color tint, float contrast, float scale)
            {
                var material = new Material(Shader.Find("Untitled/HD2D/Cave Terrain")) { name = name };
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texture));
                material.SetColor("_BaseColor", tint);
                material.SetFloat("_TextureStrength", contrast);
                material.SetFloat("_TileScale", scale);
                material.SetFloat("_Smoothness", 0.04f);
                material.enableInstancing = true;
                AssetDatabase.CreateAsset(material, Materials + "/" + name + ".mat");
                result.Add(name, material);
            }
            const string textureRoot = "Assets/Art/Textures/TerrainBasic/";
            Terrain("rock", textureRoot + "cave_rock_face.png", new Color(0.65f,0.69f,0.72f), 0.90f, 0.13f);
            Terrain("earth", textureRoot + "cave_earth_dry.png", new Color(0.82f,0.74f,0.61f), 0.90f, 0.21f);
            Terrain("wet", textureRoot + "cave_river_gravel.png", new Color(0.63f,0.70f,0.62f), 0.80f, 0.22f);
            Terrain("stone", textureRoot + "cave_stone_paving.png", new Color(0.74f,0.73f,0.66f), 0.75f, 0.25f);
            Terrain("moss", textureRoot + "cave_moss_soil.png", new Color(0.62f,0.71f,0.57f), 0.80f, 0.18f);
            Terrain("path", textureRoot + "cave_earth_dry.png", new Color(0.86f,0.77f,0.65f), 0.90f, 0.21f);
            var water = new Material(Shader.Find("Untitled/HD2D/Cave Water")) { name = "water" };
            water.SetColor("_DeepColor", new Color(0.025f,0.15f,0.18f));
            water.SetColor("_ShallowColor", new Color(0.10f,0.38f,0.39f));
            water.SetColor("_FoamColor", new Color(0.40f,0.65f,0.61f));
            water.SetFloat("_CellSize", 5.2f);
            water.SetFloat("_PixelsPerUnit", 32);
            water.SetFloat("_FlowSpeed", 0.35f);
            water.SetFloat("_Smoothness", 0.78f);
            AssetDatabase.CreateAsset(water, Materials + "/water.mat");
            result.Add("water", water);
            return result;
        }

        private static void ConfigureModel(string path, Dictionary<string, Material> materials)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.isReadable = true;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.importNormals = ModelImporterNormals.Import;
            foreach (var entry in materials)
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), entry.Key), entry.Value);
            importer.SaveAndReimport();
        }

        private static void CreateModulePrefabs(Dictionary<string, Material> materials)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "/cave_rock_modules.fbx");
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
            {
                string meshPath = Models + "/" + filter.name + ".asset";
                var mesh = UnityEngine.Object.Instantiate(filter.sharedMesh);
                mesh.name = filter.name;
                Matrix4x4 conversion = Matrix4x4.Rotate(Quaternion.Euler(0,180,0)) * filter.transform.localToWorldMatrix;
                mesh.vertices = mesh.vertices.Select(v => conversion.MultiplyPoint3x4(v)).ToArray();
                mesh.normals = mesh.normals.Select(n => conversion.MultiplyVector(n).normalized).ToArray();
                mesh.RecalculateBounds();
                var saved = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (saved == null) AssetDatabase.CreateAsset(mesh, meshPath);
                else
                {
                    EditorUtility.CopySerialized(mesh, saved);
                    UnityEngine.Object.DestroyImmediate(mesh);
                    mesh = saved;
                }
                var module = new GameObject(filter.name);
                module.AddComponent<MeshFilter>().sharedMesh = mesh;
                module.AddComponent<MeshRenderer>().sharedMaterial = materials["rock"];
                module.AddComponent<MeshCollider>().sharedMesh = mesh;
                PrefabUtility.SaveAsPrefabAsset(module, Prefabs + "/" + filter.name + ".prefab");
                UnityEngine.Object.DestroyImmediate(module);
            }
        }

        private static void Anchor(Transform parent, string name, Vector3 position)
        {
            var anchor = new GameObject(name);
            anchor.transform.SetParent(parent, false);
            anchor.transform.localPosition = position;
        }

        private static void Lighting(Transform parent)
        {
            var group = new GameObject("Lighting | cool source and warm stone");
            group.transform.SetParent(parent, false);
            var key = new GameObject("Soft overhead cavern light");
            key.transform.SetParent(group.transform, false);
            key.transform.localRotation = Quaternion.Euler(57, -32, 0);
            var sun = key.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(0.63f, 0.71f, 0.73f);
            sun.intensity = 0.65f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.72f;
            RenderSettings.sun = sun;
            void Point(string name, Vector3 position, Color color, float intensity, float range)
            {
                var go = new GameObject(name);
                go.transform.SetParent(group.transform, false);
                go.transform.localPosition = position;
                var light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = color;
                light.intensity = intensity;
                light.range = range;
                light.shadows = LightShadows.None;
            }
            Point("Daylight through river arch", new Vector3(-15, 3, 24), new Color(0.39f,0.77f,0.91f), 4, 22);
            Point("River reflected fill", new Vector3(-20, 2, 0), new Color(0.31f,0.67f,0.68f), 4.0f, 19);
            Point("Main clearing warm bounce", new Vector3(3, 4, -2), new Color(1,0.66f,0.35f), 18, 25);
            Point("Upper terrace warm bounce", new Vector3(4, 6, 18), new Color(1,0.72f,0.43f), 16, 19);
            Point("East shelf warm bounce", new Vector3(20, 4, 7), new Color(1,0.73f,0.48f), 10, 14);
        }

        private static void Presentation(Transform parent)
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
            profile.Add<Tonemapping>().mode.Override(TonemappingMode.ACES);
            var bloom = profile.Add<Bloom>();
            bloom.threshold.Override(1.15f);
            bloom.intensity.Override(0.12f);
            bloom.scatter.Override(0.55f);
            var color = profile.Add<ColorAdjustments>();
            color.postExposure.Override(1.15f);
            color.contrast.Override(9);
            color.saturation.Override(-8);
            var vignette = profile.Add<Vignette>();
            vignette.intensity.Override(0.20f);
            vignette.smoothness.Override(0.38f);
            // Keep navigation readable; use only distant Gaussian softness.
            var dof = profile.Add<DepthOfField>();
            dof.mode.Override(DepthOfFieldMode.Gaussian);
            dof.gaussianStart.Override(110);
            dof.gaussianEnd.Override(150);
            dof.gaussianMaxRadius.Override(0.6f);
            foreach (var component in profile.components)
                AssetDatabase.AddObjectToAsset(component, profile);
            var go = new GameObject("Cave Presentation Volume");
            go.transform.SetParent(parent, false);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 30;
            volume.sharedProfile = profile;
            RenderSettings.fogDensity = 0.0035f;
        }

        private static Camera CameraAt(Transform parent, string name, Vector3 focus, float pitch, float yaw, float distance)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rotation = Quaternion.Euler(pitch, yaw, 0);
            go.transform.localPosition = focus - rotation * Vector3.forward * distance;
            go.transform.localRotation = rotation;
            var camera = go.AddComponent<Camera>();
            camera.fieldOfView = 36;
            camera.nearClipPlane = 0.15f;
            camera.farClipPlane = 200;
            camera.backgroundColor = new Color(0.027f, 0.041f, 0.047f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.allowHDR = true;
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.requiresDepthTexture = true;
            data.antialiasing = AntialiasingMode.None;
            return camera;
        }

        private static void UpdateLayoutScene(Transform root)
        {
            var data = ReadLayout();
            var scene = root.gameObject.scene;
            Undo.RecordObject(root.gameObject, "Label cave elevation levels");
            root.name = "CaveTerrain | River -2.4 / Main 0 / Hall +4.8";
            var volume = root.GetComponentInChildren<Volume>();
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(volume.sharedProfile), ProfilePath);
                profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            }
            Undo.RecordObject(volume, "Keep V2 presentation separate");
            volume.sharedProfile = profile;
            if (!profile.TryGet<ColorAdjustments>(out var colorAdjustments))
                throw new InvalidOperationException("The cave presentation profile needs ColorAdjustments.");
            Undo.RecordObject(colorAdjustments, "Read terrain detail in cave lighting");
            colorAdjustments.postExposure.Override(1.15f);
            EditorUtility.SetDirty(colorAdjustments);
            RenderSettings.fogDensity = 0.0035f;
            RenderSettings.ambientSkyColor = new Color(0.22f,0.30f,0.34f);
            RenderSettings.ambientEquatorColor = new Color(0.18f,0.22f,0.25f);
            RenderSettings.ambientGroundColor = new Color(0.06f,0.09f,0.10f);
            var oldAnchors = root.Cast<Transform>().FirstOrDefault(t => t.name.StartsWith("LayoutAnchors"));
            if (oldAnchors != null) Undo.DestroyObjectImmediate(oldAnchors.gameObject);
            var anchors = new GameObject("LayoutAnchors | areas 01-15 and route junctions");
            anchors.transform.SetParent(root, false);
            foreach (var point in data.Zones)
                Anchor(anchors.transform, point.Id.ToString("00") + "_" + point.Name, new Vector3(point.X, point.Height, point.Z));
            foreach (var point in data.RouteAnchors)
                Anchor(anchors.transform, point.Name, new Vector3(point.X, point.Height, point.Z));
            Undo.RegisterCreatedObjectUndo(anchors, "Update area anchors");
            var oldBarriers = root.Cast<Transform>().FirstOrDefault(t => t.name.StartsWith("Collision |"));
            if (oldBarriers != null) Undo.DestroyObjectImmediate(oldBarriers.gameObject);
            AddRiverBarriers(root);

            var cameras = root.GetComponentsInChildren<Camera>();
            void Place(Camera camera, Vector3 focus, float pitch, float yaw, float distance)
            {
                Undo.RecordObject(camera.transform, "Frame revised cave layout");
                Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
                camera.transform.localPosition = focus - rotation * Vector3.forward * distance;
                camera.transform.localRotation = rotation;
            }
            Place(cameras.Single(c => c.name.StartsWith("Main Camera")), new Vector3(1, 1, -2.5f), 42, -5, 124);
            Place(cameras.Single(c => c.name.StartsWith("Detail Camera")), new Vector3(-1, 2, 22), 32, -4, 50);
            var riverCamera = cameras.FirstOrDefault(c => c.name.StartsWith("River Camera"));
            if (riverCamera == null)
            {
                riverCamera = CameraAt(root, "River Camera | both banks and valley", new Vector3(-14,-1,-6), 38, -12, 34);
                Undo.RegisterCreatedObjectUndo(riverCamera.gameObject, "Add riverbank review camera");
            }
            riverCamera.enabled = false;
            var top = cameras.FirstOrDefault(c => c.name.StartsWith("Top Camera"));
            if (top == null)
            {
                top = CameraAt(root, "Top Camera | terrain plan", new Vector3(2, 0, 0.5f), 90, 0, 75);
                Undo.RegisterCreatedObjectUndo(top.gameObject, "Add plan review camera");
            }
            Undo.RecordObject(top, "Configure plan review camera");
            Place(top, new Vector3(-1,0,-2), 90, 0, 75);
            top.enabled = false;
            top.orthographic = true;
            top.orthographicSize = 54;
            top.GetUniversalAdditionalCameraData().renderPostProcessing = false;

            foreach (var light in root.GetComponentsInChildren<Light>())
            {
                Vector3 position = light.transform.localPosition;
                switch (light.name)
                {
                    case "Daylight through river arch": position = new Vector3(-13, 2, 33); break;
                    case "River reflected fill": position = new Vector3(-22, 0, 0); break;
                    case "Main clearing warm bounce": position = new Vector3(1.5f, 4, -2.9f); break;
                    case "Upper terrace warm bounce": position = new Vector3(-1.5f, 8, 26.5f); break;
                    case "East shelf warm bounce": position = new Vector3(18.3f, 7, 27.2f); break;
                }
                Undo.RecordObject(light.transform, "Follow cave regions with lighting");
                light.transform.localPosition = position;
                if (light.type == LightType.Directional)
                {
                    Undo.RecordObject(light, "Expose cliff relief in soft side light");
                    light.transform.localRotation = Quaternion.Euler(48, -42, 0);
                    light.intensity = 1.05f;
                }
            }
            void Material(string name, string texture, Color tint, float strength, float scale)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(Materials + "/" + name + ".mat");
                Undo.RecordObject(material, "Use existing pixel terrain textures");
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/TerrainBasic/" + texture));
                material.SetColor("_BaseColor", tint);
                material.SetFloat("_TextureStrength", strength);
                material.SetFloat("_TileScale", scale);
                EditorUtility.SetDirty(material);
            }
            Material("rock", "cave_rock_face.png", new Color(0.65f,0.69f,0.72f), 0.90f, 0.13f);
            Material("earth", "cave_earth_dry.png", new Color(0.82f,0.74f,0.61f), 0.90f, 0.21f);
            Material("wet", "cave_river_gravel.png", new Color(0.63f,0.70f,0.62f), 0.80f, 0.22f);
            Material("stone", "cave_stone_paving.png", new Color(0.74f,0.73f,0.66f), 0.75f, 0.25f);
            Material("moss", "cave_moss_soil.png", new Color(0.62f,0.71f,0.57f), 0.80f, 0.18f);
            var water = AssetDatabase.LoadAssetAtPath<Material>(Materials + "/water.mat");
            Undo.RecordObject(water, "Follow river width");
            water.SetFloat("_CellSize", data.RiverHalfWidth * 2);
            EditorUtility.SetDirty(water);
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.sceneLighting = true;
                SceneView.lastActiveSceneView.LookAt(root.position + new Vector3(2, 1, 1), Quaternion.Euler(42, -5, 0), 76);
            }
            DressTerrain(root, data);
        }

        private static void DressTerrain(Transform root, LayoutData data)
        {
            var previous = root.Find("Terrain Details | existing pixel cave kit");
            if (previous != null) Undo.DestroyObjectImmediate(previous.gameObject);
            var group = new GameObject("Terrain Details | existing pixel cave kit");
            group.transform.SetParent(root, false);
            var categories = new Dictionary<string, Transform>();
            foreach (var point in data.Decorations ?? Array.Empty<DecorationPoint>())
            {
                if (!categories.TryGetValue(point.Name, out var category))
                {
                    category = new GameObject(point.Name).transform;
                    category.SetParent(group.transform, false);
                    categories.Add(point.Name, category);
                }
                string path = "Assets/Prefabs/Terrain/CaveDetails/" + point.Prefab + ".prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) throw new InvalidOperationException("Missing selected cave detail: " + path);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.gameObject.scene);
                instance.transform.SetParent(category, false);
                instance.transform.localPosition = new Vector3(point.X, point.Height, point.Z);
                instance.transform.localRotation = Quaternion.Euler(0, point.Yaw, 0);
                instance.transform.localScale = Vector3.one * point.Scale;
            }
            Undo.RegisterCreatedObjectUndo(group, "Dress banks and terraces with pixel assets");
        }

        private static void AddRiverBarriers(Transform root)
        {
            var data = ReadLayout();
            var group = new GameObject("Collision | river edges and southern cutaway");
            group.transform.SetParent(root, false);
            Undo.RegisterCreatedObjectUndo(group, "Update cave boundaries");
            float Center(LayoutPoint[] course, float z)
            {
                for (int i = 1; i < course.Length; i++)
                    if (z <= course[i].Z) return Mathf.Lerp(course[i-1].X, course[i].X,
                        Mathf.InverseLerp(course[i-1].Z, course[i].Z, z));
                return course[course.Length-1].X;
            }
            float Half(LayoutPoint[] course, float z)
            {
                for (int i = 1; i < course.Length; i++)
                    if (z <= course[i].Z) return Mathf.Lerp(course[i-1].HalfWidth, course[i].HalfWidth,
                        Mathf.InverseLerp(course[i-1].Z, course[i].Z, z));
                return course[course.Length-1].HalfWidth;
            }
            void Barrier(string name, Vector3 a, Vector3 b)
            {
                var go = new GameObject(name);
                go.transform.SetParent(group.transform, false);
                go.transform.localPosition = (a+b)*0.5f;
                go.transform.localRotation = Quaternion.LookRotation(b-a);
                go.AddComponent<BoxCollider>().size = new Vector3(0.18f, 3, (b-a).magnitude+0.12f);
            }
            foreach (var course in new[] {data.RiverCourse, data.WestFork, data.EastFork})
            {
                for (float z = course[0].Z; z < course[course.Length-1].Z; z += 0.7f)
                {
                    if (ReferenceEquals(course, data.RiverCourse) && data.BridgeZs.Any(b => Mathf.Abs(z-b) < 1.85f)) continue;
                    foreach (float side in new[] {-1f, 1f})
                    {
                        Vector3 Bank(float value) => new Vector3(Center(course,value) + side * Half(course,value), data.LowlandHeight + 1.2f, value);
                        Barrier("River bank", Bank(z), Bank(Mathf.Min(z+0.7f, course[course.Length-1].Z)));
                    }
                }
            }
            Barrier("Southern cutaway safety", new Vector3(-43,0,-41.5f), new Vector3(43,0,-41.5f));
        }

        private static void CheckHeights(GameObject terrain)
        {
            var data = ReadLayout();
            var origin = terrain.transform.parent.position;
            Physics.SyncTransforms();
            foreach (var point in data.Zones.Concat(data.RouteSamples))
            {
                Vector3 expected = origin + new Vector3(point.X, point.Height, point.Z);
                if (!Physics.Raycast(expected + Vector3.up * 16, Vector3.down, out var hit, 24)
                    || !hit.transform.IsChildOf(terrain.transform) || Mathf.Abs(hit.point.y-expected.y)>0.11f)
                    throw new InvalidOperationException("Cave layout collision mismatch at " + point.Name + " " + expected
                        + "; hit " + (hit.collider == null ? "none" : hit.transform.name + " " + hit.point));
            }
            foreach (var renderer in terrain.GetComponentsInChildren<MeshRenderer>(true))
                if (renderer.sharedMaterials.Any(m => m == null || ShaderUtil.ShaderHasError(m.shader)))
                    throw new InvalidOperationException("Missing material or shader error on " + renderer.name);
            var details = terrain.transform.parent.Find("Terrain Details | existing pixel cave kit");
            foreach (var renderer in details.GetComponentsInChildren<MeshRenderer>())
                if (renderer.sharedMaterials.Any(m => m == null || ShaderUtil.ShaderHasError(m.shader)))
                    throw new InvalidOperationException("Missing pixel detail material on " + renderer.name);
            Debug.Log("Cave plan checks passed: 15 region heights, " + data.RouteSamples.Length
                + " route samples, " + data.Decorations.Length + " pixel details, -2.4/0/+4.8 elevations, "
                + "three hall approaches, two river bridges and a shared elder neck; material shaders.");
        }

        [MenuItem("Untitled/Cave/Reimport Combined Terrain V2")]
        public static void ReimportLandform()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before replacing the imported landform.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) throw new InvalidOperationException("Open CaveTerracedTerrain first.");
            var root = scene.GetRootGameObjects().Single(g => g.name.StartsWith("CaveTerrain"));
            Directory.CreateDirectory(Materials);
            Directory.CreateDirectory(Prefabs);
            AssetDatabase.Refresh();
            var map = AssetDatabase.FindAssets("t:Material",new[] {Materials})
                .Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)))
                .ToDictionary(m => m.name);
            if (map.Count == 0) map = CreateMaterials();
            if (!map.ContainsKey("path"))
            {
                var path = new Material(map["earth"]) { name = "path" };
                path.SetColor("_BaseColor",new Color(0.80f,0.76f,0.65f));
                AssetDatabase.CreateAsset(path,Materials + "/path.mat");
                map.Add("path",path);
            }
            ConfigureModel(Models + "/cave_terraced_landform.fbx",map);
            ConfigureModel(Models + "/cave_rock_modules.fbx",map);
            CreateModulePrefabs(map);
            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Reimport Blender cave landform");
            Undo.DestroyObjectImmediate(root.transform.Find("EditableLandform").gameObject);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "/cave_terraced_landform.fbx");
            var terrain = (GameObject)PrefabUtility.InstantiatePrefab(model,scene);
            PrefabUtility.UnpackPrefabInstance(terrain,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            terrain.transform.SetParent(root.transform,false);
            terrain.transform.localRotation = Quaternion.Euler(0,180,0)*terrain.transform.localRotation;
            terrain.name = "EditableLandform";
            foreach (var renderer in terrain.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m => map[m.name]).ToArray();
                renderer.shadowCastingMode = renderer.name == "RiverSurface" || IsRouteOverlay(renderer.name)
                    ? ShadowCastingMode.Off : ShadowCastingMode.On;
                bool small = renderer.name.StartsWith("WaterlineStone") || renderer.name.StartsWith("DampMoss")
                    || renderer.name.Contains("EdgeStone") || renderer.name.StartsWith("LooseFloorStone")
                    || IsRouteOverlay(renderer.name);
                if (renderer.name != "RiverSurface" && !small)
                    renderer.gameObject.AddComponent<MeshCollider>().sharedMesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,StaticEditorFlags.BatchingStatic);
                if (IsRouteOverlay(renderer.name)) renderer.gameObject.SetActive(false);
            }
            Undo.RegisterCreatedObjectUndo(terrain,"Imported cave landform");
            UpdateLayoutScene(root.transform);
            Undo.CollapseUndoOperations(undo);
            CheckHeights(terrain);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Cave landform imported and saved; scene lights, cameras and anchors retained.");
        }

        [MenuItem("Untitled/Cave/Export Combined Terrain V2 Preview")]
        public static void ExportPreview()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) throw new InvalidOperationException("Open CaveTerracedTerrain first.");
            string folder = Path.Combine(Path.GetTempPath(), "untitled-cave-combined-v2");
            Directory.CreateDirectory(folder);
            var otherLights = UnityEngine.Object.FindObjectsOfType<Light>().Where(l => l.gameObject.scene != scene && l.enabled).ToArray();
            foreach (var light in otherLights) light.enabled = false;
            try
            {
                var cameras = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>()).ToArray();
                // Refresh the main batch preview after SRP has drawn the detail and plan views.
                foreach (var camera in cameras.Concat(cameras.Where(c => c.enabled)))
                {
                    bool top = camera.name.StartsWith("Top Camera");
                    int width = top ? 1200 : 1600;
                    int height = top ? 1400 : 900;
                    var target = RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32);
                    var previous = RenderTexture.active;
                    bool previousFog = RenderSettings.fog;
                    Texture2D pixels = null;
                    try
                    {
                        if (top) RenderSettings.fog = false;
                        camera.aspect = width / (float)height;
                        var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
                        RenderPipeline.SubmitRenderRequest(camera, request);
                        RenderTexture.active = target;
                        pixels = new Texture2D(width,height,TextureFormat.RGB24,false);
                        pixels.ReadPixels(new Rect(0,0,width,height),0,0);
                        pixels.Apply();
                        string name = top ? "cave_plan.png" : camera.name.StartsWith("River Camera") ? "cave_riverbanks.png"
                            : camera.enabled ? "cave_overview.png" : "cave_detail.png";
                        File.WriteAllBytes(Path.Combine(folder,name),pixels.EncodeToPNG());
                    }
                    finally
                    {
                        RenderTexture.active = previous;
                        RenderSettings.fog = previousFog;
                        camera.ResetAspect();
                        if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
                        RenderTexture.ReleaseTemporary(target);
                    }
                }
            }
            finally { foreach (var light in otherLights) light.enabled = true; }
            Debug.Log("Cave terrain previews exported to " + folder);
        }

        public static void BatchApply()
        {
            if (File.Exists(ScenePath)) EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            else
            {
                var source = EditorSceneManager.OpenScene("Assets/Scenes/CaveTerracedTerrain.unity", OpenSceneMode.Single);
                EditorSceneManager.SaveScene(source, ScenePath);
            }
            ReimportLandform();
            ExportPreview();
        }

    }
}
