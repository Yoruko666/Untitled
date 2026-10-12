using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.SceneManagement;
using UnityEditor.AddressableAssets.Settings;
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
        private const float PlayPitch = 30;
        private const float PlayYaw = -8;
        private const float PlayDistance = 20;
        private static readonly Vector3 PlayFocusOffset = new Vector3(0,1.1f,2);

        [Serializable]
        private sealed class LayoutPoint
        {
            public int Id;
            public string Name;
            public float X;
            public float Z;
            public float Height;
            public float HalfWidth;
            public float Width;
            public float Depth;
        }

        [Serializable]
        private sealed class LayoutData
        {
            public float PlanScale = 1;
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
            public float MainlandHeight;
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
            var root = new GameObject("CaveTerrain");
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
                if (renderer.name == "RiverSurface") renderer.gameObject.layer = 4;
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
            RenderSettings.fogDensity = 0.0018f;
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
            Terrain("wood", "Assets/Art/Textures/Props/Tavern/tavern_oak_grain.png", new Color(0.50f,0.32f,0.16f), 0.65f, 0.6f);
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
            RenderSettings.fogDensity = 0.0018f;
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
            camera.farClipPlane = 500;
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
            root.name = $"CaveTerrain | River {data.LowlandHeight} / Main {data.MainlandHeight} / Hall {data.HighlandHeight}";
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
            colorAdjustments.postExposure.Override(1.65f);
            EditorUtility.SetDirty(colorAdjustments);
            RenderSettings.fogDensity = 0.0018f;
            RenderSettings.ambientSkyColor = new Color(0.30f,0.38f,0.40f);
            RenderSettings.ambientEquatorColor = new Color(0.25f,0.29f,0.29f);
            RenderSettings.ambientGroundColor = new Color(0.13f,0.16f,0.14f);
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
                focus.x *= data.PlanScale;
                focus.z *= data.PlanScale;
                camera.farClipPlane = 500;
                camera.transform.localPosition = focus - rotation * Vector3.forward * (distance * data.PlanScale);
                camera.transform.localRotation = rotation;
            }
            Place(cameras.Single(c => c.name.StartsWith("Main Camera")), new Vector3(1, 1, -2.5f), 42, -5, 124);
            Place(cameras.Single(c => c.name.StartsWith("Detail Camera")), new Vector3(-1, 6, 22), 32, -4, 50);
            var riverCamera = cameras.FirstOrDefault(c => c.name.StartsWith("River Camera"));
            if (riverCamera == null)
            {
                riverCamera = CameraAt(root, "River Camera | both banks and valley", new Vector3(-28,-1,-12), 38, -12, 40);
                Undo.RegisterCreatedObjectUndo(riverCamera.gameObject, "Add riverbank review camera");
            }
            Place(riverCamera, new Vector3(-13.5f,-1,-8), 38, -12, 23);
            riverCamera.enabled = false;
            // Saved review views for the two concept-image compositions.
            void ConceptView(string name, Vector3 focus, float pitch, float yaw, float distance)
            {
                var view = cameras.FirstOrDefault(c => c.name == name);
                if (view == null)
                {
                    view = CameraAt(root, name, Vector3.zero, pitch, yaw, distance);
                    Undo.RegisterCreatedObjectUndo(view.gameObject, "Add concept review camera");
                }
                Place(view, focus, pitch, yaw, distance);
                view.enabled = false;
            }
            ConceptView("Concept A Camera | entrance toward hearth", new Vector3(1, data.MainlandHeight + 1.2f, -7), 16, 0, 23);
            ConceptView("Concept B Camera | bridge toward kitchen", new Vector3(-8, 1, -12), 24, 54, 16);
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
            top.orthographicSize = 54 * data.PlanScale;
            top.GetUniversalAdditionalCameraData().renderPostProcessing = false;

            foreach (var light in root.GetComponentsInChildren<Light>())
            {
                Vector3 position = light.transform.localPosition;
                switch (light.name)
                {
                    case "Daylight through river arch": position = new Vector3(-13, 2, 33); break;
                    case "River reflected fill": position = new Vector3(-22, 0, 0); break;
                    case "Main clearing warm bounce": position = new Vector3(1.5f, data.MainlandHeight + 4, -2.9f); break;
                    case "Upper terrace warm bounce": position = new Vector3(-1.5f, data.HighlandHeight + 3.2f, 26.5f); break;
                    case "East shelf warm bounce": position = new Vector3(18.3f, data.HighlandHeight + 2.2f, 27.2f); break;
                }
                Undo.RecordObject(light.transform, "Follow cave regions with lighting");
                position.x *= data.PlanScale;
                position.z *= data.PlanScale;
                light.transform.localPosition = position;
                if (light.type == LightType.Point)
                {
                    light.range = light.name.Contains("clearing") ? 42 : 35;
                    if (light.name.Contains("river") || light.name.Contains("River")) light.intensity = 12;
                }
                if (light.type == LightType.Directional)
                {
                    Undo.RecordObject(light, "Expose cliff relief in soft side light");
                    light.transform.localRotation = Quaternion.Euler(48, -42, 0);
                    light.intensity = 1.20f;
                    light.color = new Color(0.80f,0.84f,0.80f);
                }
            }
            ApplyPixelMaterials();
            var water = AssetDatabase.LoadAssetAtPath<Material>(Materials + "/water.mat");
            Undo.RecordObject(water, "Use continuous reflective river water");
            water.shader = Shader.Find("Untitled/HD2D/Terraced River Water");
            water.SetFloat("_CellSize", data.RiverHalfWidth * 2);
            water.SetFloat("_FlowSpeed", 0.48f);
            water.SetFloat("_Smoothness", 0.91f);
            water.SetColor("_DeepColor", new Color(0.035f,0.16f,0.18f));
            water.SetColor("_ShallowColor", new Color(0.13f,0.32f,0.29f));
            water.SetColor("_FoamColor", new Color(0.35f,0.49f,0.43f));
            EditorUtility.SetDirty(water);
            var player = root.GetComponentInChildren<PlayerEntity>(true);
            if (player != null)
            {
                Undo.RecordObject(player.transform, "Move player to expanded entrance");
                var entrance = data.Zones.Single(p => p.Id == 2);
                player.transform.localPosition = new Vector3(entrance.X, entrance.Height + 0.06f, entrance.Z);
                var controller = player.GetComponent<CharacterController>();
                Undo.RecordObject(controller, "Match existing stone stair risers");
                controller.stepOffset = 0.28f;
            }
            AddRiverReflections(root);
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.sceneLighting = true;
                SceneView.lastActiveSceneView.LookAt(root.position + new Vector3(4, 1, 2), Quaternion.Euler(42, -5, 0), 152);
            }
            DressTerrain(root, data);
            ApplyHd2dPresentation(root, profile, data);
        }

        private static void ApplyHd2dPresentation(Transform root, VolumeProfile profile, LayoutData data)
        {
            foreach (var component in profile.components) Undo.RecordObject(component, "Tune cave HD-2D presentation");
            profile.TryGet<ColorAdjustments>(out var grading);
            grading.postExposure.Override(2.05f);
            grading.contrast.Override(8);
            grading.saturation.Override(-3);
            profile.TryGet<Bloom>(out var bloom);
            bloom.threshold.Override(.9f);
            bloom.intensity.Override(.22f);
            bloom.scatter.Override(.6f);
            profile.TryGet<Vignette>(out var vignette);
            vignette.intensity.Override(.16f);
            vignette.smoothness.Override(.55f);
            profile.TryGet<DepthOfField>(out var depth);
            depth.gaussianStart.Override(24);
            depth.gaussianEnd.Override(48);
            depth.gaussianMaxRadius.Override(1.05f);
            foreach (var component in profile.components) EditorUtility.SetDirty(component);
            RenderSettings.ambientSkyColor = new Color(.30f,.40f,.52f);
            RenderSettings.ambientEquatorColor = new Color(.25f,.30f,.36f);
            RenderSettings.ambientGroundColor = new Color(.15f,.15f,.14f);
            RenderSettings.fogColor = new Color(.09f,.14f,.19f);
            RenderSettings.fogDensity = .0022f;
            foreach (var light in root.GetComponentsInChildren<Light>())
            {
                Undo.RecordObject(light, "Balance cool cave fill and warm gathering areas");
                if (light.type == LightType.Directional)
                {
                    light.color = new Color(.66f,.77f,.9f);
                    light.intensity = 1.15f;
                    light.shadowStrength = .70f;
                }
                else if (light.name == "Main clearing warm bounce") light.intensity = 34;
                else if (light.name == "Upper terrace warm bounce") light.intensity = 30;
                else if (light.name == "Daylight through river arch")
                {
                    light.color = new Color(.32f,.70f,1);
                    light.intensity = 45;
                    light.range = 45;
                }
            }
            var main = root.GetComponentsInChildren<Camera>().Single(c => c.name.StartsWith("Main Camera"));
            Undo.RecordObject(main, "Set exploration field of view");
            main.fieldOfView = 38;
            var follow = main.GetComponent<CameraController>();
            if (follow != null)
            {
                Undo.RecordObject(follow, "Frame pixel character against layered terrain");
                follow.SetViewAngle(PlayPitch);
                follow.SetYawAngle(PlayYaw);
                follow.SetDistance(PlayDistance);
                follow.SetFocusOffset(PlayFocusOffset);
            }
            // Review camera uses the same pose as the playable follow camera.
            var player = root.GetComponentInChildren<PlayerEntity>();
            var review = root.GetComponentsInChildren<Camera>().FirstOrDefault(c => c.name.StartsWith("Player Review"));
            if (player != null)
            {
                if (review == null)
                {
                    review = CameraAt(root, "Player Review Camera | playable framing", Vector3.zero, PlayPitch,PlayYaw,PlayDistance);
                    Undo.RegisterCreatedObjectUndo(review.gameObject, "Add playable framing review");
                }
                Undo.RecordObject(review.transform, "Match playable camera pose");
                var rotation = Quaternion.Euler(PlayPitch,PlayYaw,0);
                review.transform.position = player.transform.position + PlayFocusOffset - rotation * Vector3.forward * PlayDistance;
                review.transform.rotation = rotation;
                review.fieldOfView = main.fieldOfView;
                review.enabled = false;
            }
            var old = root.Find("Wayfinding | warm pixel braziers");
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            var group = new GameObject("Wayfinding | warm pixel braziers");
            group.transform.SetParent(root,false);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/Lighting/timber_brazier_hd2d.prefab");
            if (prefab == null) throw new InvalidOperationException("Pixel brazier prefab is missing.");
            Physics.SyncTransforms();
            var terrain = root.Find("EditableLandform");
            var positions = new[] {new Vector2(10,-43),new Vector2(-2,-30),new Vector2(9,-13),
                new Vector2(10,2),new Vector2(14,13),new Vector2(-9,31),new Vector2(13,39),
                new Vector2(12,47),new Vector2(0,49),new Vector2(15,58),new Vector2(45.5f,52),
                new Vector2(-38,-18),new Vector2(-22,-14),new Vector2(-36,31)};
            foreach (var position in positions)
            {
                var start = root.position + new Vector3(position.x,30,position.y);
                var hits = Physics.RaycastAll(start,Vector3.down,40)
                    .Where(hit => hit.transform.IsChildOf(terrain)).OrderBy(hit => hit.distance).ToArray();
                if (hits.Length == 0) continue;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab,root.gameObject.scene);
                go.transform.SetParent(group.transform,false);
                go.transform.position = hits[0].point + Vector3.up * .02f;
                go.transform.localRotation = Quaternion.Euler(0,-8,0);
                // Wayfinding lamps stay clear of navigation collision and building reservations.
                foreach (var collider in go.GetComponentsInChildren<Collider>()) collider.enabled = false;
                foreach (var fire in go.GetComponentsInChildren<HybridFireLight>())
                {
                    var settings = new SerializedObject(fire);
                    settings.FindProperty("baseIntensity").floatValue = 4;
                    settings.FindProperty("baseRange").floatValue = 10;
                    settings.ApplyModifiedProperties();
                }
                foreach (var lamp in go.GetComponentsInChildren<Light>())
                {
                    lamp.intensity = 4;
                    lamp.range = 10;
                    lamp.color = new Color(1,.57f,.24f);
                }
            }
            Undo.RegisterCreatedObjectUndo(group,"Place pixel route lamps");
            foreach (var material in new[] {"earth","path","wet","moss","rock","stone","wood"})
            {
                var surface = AssetDatabase.LoadAssetAtPath<Material>(Materials + "/" + material + ".mat");
                surface.SetFloat("_TextureStrength", material == "earth" ? .55f : material == "path" ? .48f : .8f);
                EditorUtility.SetDirty(surface);
            }
        }

        private static void ApplyPixelMaterials()
        {
            const string cave = "Assets/Art/Textures/TerrainBasic/";
            void Apply(string name, string source, Color tint, float scale, float strength, Vector4 crop)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(Materials + "/" + name + ".mat");
                Undo.RecordObject(material, "Apply native cave palette and consistent world pixels");
                material.shader = Shader.Find("Untitled/HD2D/Terraced Pixel Terrain");
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(source);
                if (texture == null || texture.width != 64 || texture.height != 64 || texture.filterMode != FilterMode.Point)
                    throw new InvalidOperationException("Cave surface requires a point-filtered 64 x 64 tile: " + source);
                material.SetTexture("_BaseMap", texture);
                material.SetTextureOffset("_BaseMap", new Vector2(crop.x,crop.y));
                material.SetTextureScale("_BaseMap", new Vector2(crop.z,crop.w));
                material.SetColor("_BaseColor",tint);
                material.SetFloat("_TileScale",scale);
                material.SetFloat("_TextureStrength",strength);
                material.SetFloat("_PixelDensity",16);
                material.SetFloat("_PaletteSteps",256);
                material.SetFloat("_Smoothness",0.02f);
                EditorUtility.SetDirty(material);
            }
            // All surfaces use actual 64px tiles: one four-metre repeat, 16 source pixels/metre.
            var full = new Vector4(0,0,1,1);
            Apply("earth",cave+"cave_earth_dry.png",new Color(.9f,.86f,.8f),.25f,1,full);
            Apply("path","Assets/Art/Textures/CaveHD2D/cave_ground_path_candidate.png",new Color(.91f,.86f,.78f),.25f,1,full);
            Apply("wet",cave+"cave_river_gravel.png",new Color(.70f,.77f,.75f),.25f,1,full);
            Apply("moss",cave+"cave_moss_soil.png",new Color(.8f,.86f,.78f),.25f,1,full);
            Apply("rock",cave+"cave_rock_face.png",new Color(.8f,.84f,.85f),.25f,1,full);
            Apply("stone",cave+"cave_stone_paving.png",new Color(.86f,.85f,.8f),.25f,1,full);
            Apply("wood",cave+"cave_wood_planks.png",new Color(.87f,.8f,.7f),.25f,1,full);
        }

        private static void AddRiverReflections(Transform root)
        {
            var old = root.Find("River Reflections");
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            var group = new GameObject("River Reflections");
            group.transform.SetParent(root,false);
            foreach (float z in new[] {-39f,17f,56f})
            {
                var go = new GameObject("Cavern environment reflection " + z);
                go.transform.SetParent(group.transform,false);
                go.transform.localPosition = new Vector3(-28,-1,z);
                var probe = go.AddComponent<ReflectionProbe>();
                probe.mode = ReflectionProbeMode.Realtime;
                probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
                probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
                probe.resolution = 128;
                probe.cullingMask = ~(1 << 4);
                probe.size = new Vector3(60,22,65);
                probe.boxProjection = true;
                probe.blendDistance = 12;
                probe.intensity = 0.65f;
                probe.clearFlags = ReflectionProbeClearFlags.SolidColor;
                probe.backgroundColor = new Color(0.055f,0.08f,0.09f);
                probe.nearClipPlane = 0.2f;
                probe.farClipPlane = 130;
            }
            Undo.RegisterCreatedObjectUndo(group, "Capture cavern reflections for the river");
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

        [MenuItem("Untitled/Cave/Add Player Preview To Combined Terrain V2")]
        public static void AddPlayerPreview()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before adding the cave player.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                throw new InvalidOperationException("Open CaveTerracedTerrainV2 before adding the player.");

            var rootObject = scene.GetRootGameObjects()
                .Single(go => go.name.StartsWith("CaveTerrain | River"));
            var root = rootObject.transform;
            var camera = root.GetComponentsInChildren<Camera>(true)
                .Single(item => item.name.StartsWith("Main Camera | terrain overview"));
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var configGroup = settings == null ? null : settings.FindGroup("Configs");
            if (configGroup == null)
                throw new InvalidOperationException("The Addressables Configs group is missing.");

            var tableNames = new[] {"BubbleTable", "DialogTable", "DialogOptionTable"};
            foreach (string tableName in tableNames)
                if (!File.Exists("Assets/Configs/Database/" + tableName + ".bytes"))
                    throw new InvalidOperationException("Missing generated table " + tableName + ".bytes.");

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add playable cave preview");
            var player = root.GetComponentInChildren<PlayerEntity>(true);
            if (player == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/FirstRoom/GoblinPlayer.prefab");
                if (prefab == null) throw new InvalidOperationException("GoblinPlayer prefab is missing.");
                var playerObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                Undo.RegisterCreatedObjectUndo(playerObject, "Add cave player");
                playerObject.name = "GoblinPlayer";
                playerObject.transform.SetParent(root, false);
                var entrance = ReadLayout().Zones.Single(point => point.Id == 2);
                playerObject.transform.localPosition = new Vector3(entrance.X, entrance.Height + 0.06f, entrance.Z);
                player = playerObject.GetComponent<PlayerEntity>();
                if (player == null)
                {
                    player = Undo.AddComponent<PlayerEntity>(playerObject);
                    var playerData = new SerializedObject(player);
                    playerData.FindProperty("Speed").floatValue = 7.625f;
                    playerData.FindProperty("useGravity").boolValue = true;
                    playerData.ApplyModifiedProperties();
                }
            }

            var billboard = player.GetComponentInChildren<SpriteBillboard>(true);
            if (billboard != null)
            {
                Undo.RecordObject(billboard, "Face the cave play camera");
                var billboardData = new SerializedObject(billboard);
                billboardData.FindProperty("targetCamera").objectReferenceValue = camera;
                billboardData.ApplyModifiedProperties();
            }

            var follow = camera.GetComponent<CameraController>();
            if (follow == null) follow = Undo.AddComponent<CameraController>(camera.gameObject);
            Undo.RecordObject(follow, "Follow the cave player in Play Mode");
            var followData = new SerializedObject(follow);
            followData.FindProperty("target").objectReferenceValue = player.transform;
            followData.FindProperty("viewAngle").floatValue = PlayPitch;
            followData.FindProperty("yawAngle").floatValue = PlayYaw;
            followData.FindProperty("distance").floatValue = PlayDistance;
            followData.FindProperty("damping").floatValue = 8f;
            followData.FindProperty("focusOffset").vector3Value = PlayFocusOffset;
            followData.ApplyModifiedProperties();

            if (root.GetComponent<Bootstrap>() == null)
                Undo.AddComponent<Bootstrap>(rootObject);

            foreach (string tableName in tableNames)
            {
                string assetPath = "Assets/Configs/Database/" + tableName + ".bytes";
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                var entry = settings.CreateOrMoveEntry(guid, configGroup, false, false);
                entry.address = tableName;
            }
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = player.gameObject;
            Debug.Log("Cave player added at the southern entrance. Press Play to move with WASD or arrow keys.");
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
                    if (ReferenceEquals(course, data.RiverCourse) && data.BridgeZs.Any(b => Mathf.Abs(z-b) < 1.82f)) continue;
                    foreach (float side in new[] {-1f, 1f})
                    {
                        Vector3 Bank(float value) => new Vector3(Center(course,value) + side * Half(course,value), data.LowlandHeight + 1.2f, value);
                        Barrier("River bank", Bank(z), Bank(Mathf.Min(z+0.7f, course[course.Length-1].Z)));
                    }
                }
            }
            Barrier("Southern cutaway safety", new Vector3(-86,0,-83), new Vector3(86,0,-83));
        }

        private static void CheckHeights(GameObject terrain)
        {
            var data = ReadLayout();
            var origin = terrain.transform.parent.position;
            Physics.SyncTransforms();
            var failures = new List<string>();
            foreach (var point in data.Zones.Concat(data.RouteSamples))
            {
                Vector3 expected = origin + new Vector3(point.X, point.Height, point.Z);
                // Check the landform, excluding the player standing at the entrance.
                var hits = Physics.RaycastAll(expected + Vector3.up * 16, Vector3.down, 24)
                    .Where(item => item.transform.IsChildOf(terrain.transform)).OrderBy(item => item.distance).ToArray();
                var hit = hits.FirstOrDefault();
                if (hits.Length == 0 || Mathf.Abs(hit.point.y-expected.y)>0.11f)
                    failures.Add("Cave layout collision mismatch at " + point.Name + " " + expected
                        + "; hit " + (hit.collider == null ? "none" : hit.transform.name + " " + hit.point));
            }
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("\n",failures));
            foreach (var renderer in terrain.GetComponentsInChildren<MeshRenderer>(true))
                if (renderer.sharedMaterials.Any(m => m == null || ShaderUtil.ShaderHasError(m.shader)))
                    throw new InvalidOperationException("Missing material or shader error on " + renderer.name);
            var details = terrain.transform.parent.Find("Terrain Details | existing pixel cave kit");
            foreach (var renderer in details.GetComponentsInChildren<MeshRenderer>())
                if (renderer.sharedMaterials.Any(m => m == null || ShaderUtil.ShaderHasError(m.shader)))
                    throw new InvalidOperationException("Missing pixel detail material on " + renderer.name);
            Debug.Log("Cave plan checks passed: 15 region heights, " + data.RouteSamples.Length
                + " route samples, " + data.Decorations.Length + " pixel details, "
                + data.LowlandHeight + "/" + data.MainlandHeight + "/" + data.HighlandHeight + " elevations, "
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
            if (!map.ContainsKey("wood"))
            {
                var wood = new Material(map["earth"]) { name = "wood" };
                AssetDatabase.CreateAsset(wood, Materials + "/wood.mat");
                map.Add("wood", wood);
            }
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
                if (renderer.name == "RiverSurface") renderer.gameObject.layer = 4;
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
                var presentation = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Volume>()).First().sharedProfile;
                presentation.TryGet<DepthOfField>(out var depthOfField);
                // Refresh the main batch preview after SRP has drawn the detail and plan views.
                foreach (var camera in cameras.Concat(cameras.Where(c => c.enabled)))
                {
                    bool top = camera.name.StartsWith("Top Camera");
                    int width = top ? 1200 : 1600;
                    int height = top ? 1400 : 900;
                    var target = RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32);
                    var previous = RenderTexture.active;
                    bool previousFog = RenderSettings.fog;
                    bool previousDepth = depthOfField.active;
                    Texture2D pixels = null;
                    try
                    {
                        if (top) RenderSettings.fog = false;
                        // A close-range play profile must not blur the distant map-review cameras.
                        depthOfField.active = previousDepth && camera.name.StartsWith("Player Review");
                        camera.aspect = width / (float)height;
                        var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
                        RenderPipeline.SubmitRenderRequest(camera, request);
                        RenderTexture.active = target;
                        pixels = new Texture2D(width,height,TextureFormat.RGB24,false);
                        pixels.ReadPixels(new Rect(0,0,width,height),0,0);
                        pixels.Apply();
                        string name = camera.name.StartsWith("Player Review") ? "cave_player.png"
                            : camera.name.StartsWith("Concept A") ? "cave_concept_entrance.png"
                            : camera.name.StartsWith("Concept B") ? "cave_concept_bridge.png"
                            : top ? "cave_plan.png" : camera.name.StartsWith("River Camera") ? "cave_riverbanks.png"
                            : camera.enabled ? "cave_overview.png" : "cave_detail.png";
                        File.WriteAllBytes(Path.Combine(folder,name),pixels.EncodeToPNG());
                    }
                    finally
                    {
                        RenderTexture.active = previous;
                        RenderSettings.fog = previousFog;
                        depthOfField.active = previousDepth;
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

        public static void BatchAddPlayerPreview()
        {
            if (!File.Exists(ScenePath))
                throw new InvalidOperationException("The saved CaveTerracedTerrainV2 scene is required.");
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AddPlayerPreview();
        }

    }
}
