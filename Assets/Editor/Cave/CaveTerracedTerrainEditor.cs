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
    public static class CaveTerracedTerrainEditor
    {
        private const string ScenePath = "Assets/Scenes/CaveTerracedTerrain.unity";
        private const string Models = "Assets/Art/Models/CaveTerraced";
        private const string Materials = "Assets/Art/Materials/CaveTerraced";
        private const string Prefabs = "Assets/Prefabs/Terrain/CaveTerraced";
        private const string ProfilePath = "Assets/Settings/CaveTerracedPresentation.asset";
        private static readonly Vector3 Origin = new Vector3(0, 0, 200);

        [MenuItem("Untitled/Cave/Create Terraced Terrain %#t")]
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
            var root = new GameObject("CaveTerrain | River -1.2 / Main 0 / Hall +2.4");
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
                renderer.shadowCastingMode = renderer.name == "RiverSurface" || renderer.name.Contains("Sediment")
                    ? ShadowCastingMode.Off : ShadowCastingMode.On;
                renderer.receiveShadows = true;
                var filter = renderer.GetComponent<MeshFilter>();
                bool smallDetail = renderer.name.StartsWith("WaterlineStone") || renderer.name.StartsWith("DampMoss")
                    || renderer.name.Contains("EdgeStone") || renderer.name.StartsWith("LooseFloorStone")
                    || renderer.name.Contains("Sediment");
                if (renderer.name != "RiverSurface" && !smallDetail)
                    renderer.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.BatchingStatic);
                // Keep optional route overlays available for layout review; natural ground is the default.
                if (renderer.name.Contains("Sediment")) renderer.gameObject.SetActive(false);
            }
            AddRiverBarriers(root.transform);
            var landmarks = new GameObject("LayoutAnchors | terrain only");
            landmarks.transform.SetParent(root.transform, false);
            Anchor(landmarks.transform, "Entrance", new Vector3(4, 0, -18));
            Anchor(landmarks.transform, "CentralClearing", new Vector3(4, 0, -1));
            Anchor(landmarks.transform, "HallTerrace", new Vector3(4, 2.4f, 18));
            Anchor(landmarks.transform, "ElderTerrace", new Vector3(12, 2.4f, 21));
            Anchor(landmarks.transform, "Riverbank", new Vector3(-22, -1.2f, 2));
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
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.20f, 0.27f, 0.31f);
            RenderSettings.ambientEquatorColor = new Color(0.13f, 0.16f, 0.18f);
            RenderSettings.ambientGroundColor = new Color(0.05f, 0.07f, 0.08f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.065f, 0.089f, 0.10f);
            RenderSettings.fogDensity = 0.008f;
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
            const string textureRoot = "Assets/Art/Textures/CaveHD2D/";
            Terrain("rock", textureRoot + "cave_cliff_rock_candidate.png", new Color(0.55f,0.62f,0.68f), 0.75f, 0.18f);
            Terrain("earth", textureRoot + "cave_ground_path_candidate.png", new Color(0.75f,0.65f,0.48f), 0.70f, 0.28f);
            Terrain("wet", textureRoot + "cave_ground_wet_candidate.png", new Color(0.42f,0.54f,0.50f), 0.85f, 0.26f);
            Terrain("stone", textureRoot + "cave_cliff_rock_candidate.png", new Color(0.78f,0.78f,0.72f), 0.45f, 0.35f);
            Terrain("moss", textureRoot + "cave_ground_wet_candidate.png", new Color(0.24f,0.39f,0.24f), 0.25f, 0.3f);
            Terrain("path", textureRoot + "cave_ground_path_candidate.png", new Color(0.79f,0.69f,0.51f), 0.70f, 0.28f);
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
            color.postExposure.Override(0.75f);
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

        private static void AddRiverBarriers(Transform root)
        {
            var group = new GameObject("Collision | water banks and open-cutaway boundary");
            group.transform.SetParent(root, false);
            var course = new[] { new Vector2(-12,-24), new Vector2(-13,-19), new Vector2(-13,-14),
                new Vector2(-14,-9), new Vector2(-12,-3), new Vector2(-10,3), new Vector2(-12,9),
                new Vector2(-16,15), new Vector2(-17,20), new Vector2(-15,26) };
            float Center(float z)
            {
                for (int i = 1; i < course.Length; i++)
                    if (z <= course[i].y) return Mathf.Lerp(course[i-1].x, course[i].x,
                        Mathf.InverseLerp(course[i-1].y, course[i].y, z));
                return course[course.Length-1].x;
            }
            void Barrier(string name, Vector3 a, Vector3 b)
            {
                var go = new GameObject(name);
                go.transform.SetParent(group.transform, false);
                go.transform.localPosition = (a+b)*0.5f;
                go.transform.localRotation = Quaternion.LookRotation(b-a);
                var collider = go.AddComponent<BoxCollider>();
                collider.size = new Vector3(0.18f,3,(b-a).magnitude+0.12f);
            }
            for (float z=-23; z<25; z+=0.5f)
            {
                // Bridge openings coincide with their 2.2m deck width.
                if (Mathf.Abs(z+9.2f)<1.45f || Mathf.Abs(z-7.4f)<1.45f) continue;
                foreach (float side in new[] {-1f,1f})
                {
                    Vector3 Bank(float value) => new Vector3(Center(value)+side*(2.6f+0.2f*Mathf.Sin(value*0.65f)),0,value);
                    Barrier("River bank",Bank(z),Bank(z+0.5f));
                }
            }
            Barrier("Southern cutaway safety",new Vector3(-30,0,-24),new Vector3(30,0,-24));
        }

        private static void CheckHeights(GameObject terrain)
        {
            Physics.SyncTransforms();
            foreach (var expected in new[] { new Vector3(4,0,-1), new Vector3(-22,-1.2f,2),
                         new Vector3(4,2.4f,18), new Vector3(19,1.2f,7) })
            {
                if (!Physics.Raycast(Origin+expected+Vector3.up*15,Vector3.down,out var hit,20)
                    || !hit.transform.IsChildOf(terrain.transform) || Mathf.Abs(hit.point.y-expected.y)>0.08f)
                    throw new InvalidOperationException("Unexpected landform collision height at " + expected
                        + "; hit " + (hit.collider == null ? "none" : hit.transform.name + " " + hit.point));
            }
            foreach (var renderer in terrain.GetComponentsInChildren<MeshRenderer>())
                if (renderer.sharedMaterials.Any(m => m == null || ShaderUtil.ShaderHasError(m.shader)))
                    throw new InvalidOperationException("Missing material or shader error on " + renderer.name);
            var points = new List<Vector3>();
            for (int i=0;i<16;i++) points.Add(new Vector3(1,(i+1)*0.15f,4.8f+i*0.43f+0.30f));
            for (int i=0;i<8;i++) points.Add(new Vector3(-9.8f+i*0.36f+0.20f,-1.2f+(i+1)*0.15f,-9.2f));
            points.AddRange(new[] {new Vector3(-7.5f,-1.18f,6),new Vector3(-6.3f,-0.65f,9),
                new Vector3(-6,0.03f,11),new Vector3(20,1.35f,18),new Vector3(17.5f,2.42f,20.5f),
                new Vector3(19,0.62f,1.5f),new Vector3(-14,-0.92f,-9.2f),new Vector3(-11.47f,-0.92f,7.4f)});
            foreach (var point in points)
            {
                if (!Physics.Raycast(Origin+point+Vector3.up*10,Vector3.down,out var hit,15)
                    || !hit.transform.IsChildOf(terrain.transform) || Mathf.Abs(hit.point.y-point.y)>0.09f)
                    throw new InvalidOperationException("Route height mismatch at " + point
                        + "; hit " + (hit.collider == null ? "none" : hit.transform.name + " " + hit.point));
            }
            Debug.Log("Cave route checks passed: 24 stair treads, supply and east ascents, residential ramp and both crossings.");
            Debug.Log("Cave terrain checks passed: main, riverbank, hall and residential collision heights; material shaders.");
        }

        [MenuItem("Untitled/Cave/Reimport Terraced Landform %#u")]
        public static void ReimportLandform()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before replacing the imported landform.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) throw new InvalidOperationException("Open CaveTerracedTerrain first.");
            var root = scene.GetRootGameObjects().Single(g => g.name.StartsWith("CaveTerrain"));
            var map = AssetDatabase.FindAssets("t:Material",new[] {Materials})
                .Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)))
                .ToDictionary(m => m.name);
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
                renderer.shadowCastingMode = renderer.name == "RiverSurface" || renderer.name.Contains("Sediment")
                    ? ShadowCastingMode.Off : ShadowCastingMode.On;
                bool small = renderer.name.StartsWith("WaterlineStone") || renderer.name.StartsWith("DampMoss")
                    || renderer.name.Contains("EdgeStone") || renderer.name.StartsWith("LooseFloorStone")
                    || renderer.name.Contains("Sediment");
                if (renderer.name != "RiverSurface" && !small)
                    renderer.gameObject.AddComponent<MeshCollider>().sharedMesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,StaticEditorFlags.BatchingStatic);
                if (renderer.name.Contains("Sediment")) renderer.gameObject.SetActive(false);
            }
            Undo.RegisterCreatedObjectUndo(terrain,"Imported cave landform");
            Undo.CollapseUndoOperations(undo);
            CheckHeights(terrain);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Cave landform imported and saved; scene lights, cameras and anchors retained.");
        }

        [MenuItem("Untitled/Cave/Export Terraced Terrain Preview %#y")]
        public static void ExportPreview()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) throw new InvalidOperationException("Open CaveTerracedTerrain first.");
            string folder = Path.Combine(Path.GetTempPath(), "untitled-cave-terraced");
            Directory.CreateDirectory(folder);
            var otherLights = UnityEngine.Object.FindObjectsOfType<Light>().Where(l => l.gameObject.scene != scene && l.enabled).ToArray();
            foreach (var light in otherLights) light.enabled = false;
            try
            {
                foreach (var camera in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>()))
                {
                    var target = RenderTexture.GetTemporary(1600,900,24,RenderTextureFormat.ARGB32);
                    var previous = RenderTexture.active;
                    Texture2D pixels = null;
                    try
                    {
                        var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
                        RenderPipeline.SubmitRenderRequest(camera, request);
                        RenderTexture.active = target;
                        pixels = new Texture2D(1600,900,TextureFormat.RGB24,false);
                        pixels.ReadPixels(new Rect(0,0,1600,900),0,0);
                        pixels.Apply();
                        string name = camera.enabled ? "cave_overview.png" : "cave_detail.png";
                        File.WriteAllBytes(Path.Combine(folder,name),pixels.EncodeToPNG());
                    }
                    finally
                    {
                        RenderTexture.active = previous;
                        if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
                        RenderTexture.ReleaseTemporary(target);
                    }
                }
            }
            finally { foreach (var light in otherLights) light.enabled = true; }
            Debug.Log("Cave terrain previews exported to " + folder);
        }

    }
}
