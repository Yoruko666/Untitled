using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Untitled.Editor
{
    public static partial class KitchenDemoBuilder
    {
        private static Camera BuildPresentation()
        {
            Transform presentation = Group("DemoPresentation_CamerasAndFill", null);
            Transform cameraRoot = Group("Main Camera", presentation); cameraRoot.gameObject.tag = "MainCamera";
            Camera camera = cameraRoot.gameObject.AddComponent<Camera>(); cameraRoot.gameObject.AddComponent<AudioListener>();
            camera.fieldOfView = 34; camera.nearClipPlane = .1f; camera.farClipPlane = 120; camera.allowHDR = true;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.022f, .028f, .030f);
            SetPose(camera, V(0, .85f, .75f), 47, 0, 35);
            var data = cameraRoot.gameObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true; data.requiresDepthTexture = true; data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            RenderSettings.skybox = null; RenderSettings.fog = false; RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.31f, .37f, .35f); RenderSettings.ambientEquatorColor = new Color(.25f, .30f, .27f); RenderSettings.ambientGroundColor = new Color(.14f, .12f, .08f); RenderSettings.reflectionIntensity = 0;
            Transform fill = Group("CoolCaveBounce", presentation); fill.rotation = Quaternion.Euler(51, -32, 0);
            Light light = fill.gameObject.AddComponent<Light>(); light.type = LightType.Directional;
            light.color = new Color(.50f, .62f, .59f); light.intensity = 1.32f; light.shadows = LightShadows.Soft; light.shadowBias = .025f; light.shadowNormalBias = .11f; RenderSettings.sun = light;
            Transform front = Group("WarmFrontBounce", presentation); front.rotation = Quaternion.Euler(32, 152, 0);
            Light bounce = front.gameObject.AddComponent<Light>(); bounce.type = LightType.Directional; bounce.color = new Color(.65f, .51f, .33f); bounce.intensity = .46f;
            var volume = Group("KitchenPresentation", presentation).gameObject.AddComponent<Volume>(); volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>(); profile.name = "GoblinKitchenPresentation";
            var bloom = profile.Add<Bloom>(true); bloom.intensity.Override(.22f); bloom.threshold.Override(1.1f); bloom.scatter.Override(.6f);
            var color = profile.Add<ColorAdjustments>(true); color.postExposure.Override(.75f); color.contrast.Override(9); color.saturation.Override(-4);
            profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
            var vignette = profile.Add<Vignette>(true); vignette.intensity.Override(.13f); vignette.smoothness.Override(.6f);
            volume.sharedProfile = SaveAsset(profile, "Assets/Settings/GoblinKitchenPresentation.asset");
            foreach (VolumeComponent component in volume.sharedProfile.components) if (!AssetDatabase.Contains(component)) AssetDatabase.AddObjectToAsset(component, volume.sharedProfile);
            EditorUtility.SetDirty(volume.sharedProfile); return camera;
        }

        private static void SetPose(Camera camera, Vector3 focus, float pitch, float yaw, float distance)
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            camera.transform.SetPositionAndRotation(focus - rotation * Vector3.forward * distance, rotation);
        }

        // One saved-prefab correction after reviewing the first actual render.
        public static void ApplyVisualReview()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            palette = Enum.GetValues(typeof(Finish)).Cast<Finish>().Select(f => AssetDatabase.LoadAssetAtPath<Material>(Materials + "/kitchen_" + f.ToString().ToLowerInvariant() + ".mat")).ToArray();
            BuildPrepCart();
            string path = Prefabs + "/goblin_kitchen_room.prefab";
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Transform serving = contents.transform.Find("06_ServingBenchAndSupplyCorners");
                if (serving.Find("CentralIngredientPrepCart") != null) throw new InvalidOperationException("Visual review already applied. Edit the saved prefab directly.");
                BuildCentralCartLayout(serving);
                BuildRightPrepLamp(contents.transform.Find("02_LongChoppingTable_ExistingHD2D"));
                BuildRoastLamp(contents.transform.Find("08_WarmOilAndWallFire"));
                contents.transform.Find("04_GrainJarsHerbsAndReceivingCrates/hanging_garlic_and_cave_herbs_3d").localPosition = V(-3.0f, 3.19f, 7.59f);
                contents.transform.Find("03_CauldronSpitRoastAndMeatRack/cleavers_pan_and_ladle_rail_3d").localPosition = V(.25f, 3.18f, 7.61f);
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            EditorSceneManager.OpenScene(ScenePath);
            var presentation = EditorSceneManager.GetActiveScene().GetRootGameObjects().Single(g => g.name == "DemoPresentation_CamerasAndFill");
            presentation.transform.Find("CoolCaveBounce").GetComponent<Light>().intensity = 1.32f;
            AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            EditorSceneManager.OpenScene(ScenePath); ValidateSavedScene(); Capture();
            Debug.Log("KITCHEN_VISUAL_REVIEW_OK: central food cart, usable passage, preparation lights and visible roast.");
        }

        private static void ValidateSavedScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            string path = Prefabs + "/goblin_kitchen_room.prefab";
            GameObject room = scene.GetRootGameObjects().Single(g => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g) == path);
            if (room.GetComponentsInChildren<MeshFilter>().Any(f => f.sharedMesh == null)) throw new InvalidOperationException("Missing saved kitchen mesh.");
            if (room.GetComponentsInChildren<MeshRenderer>().Any(r => r.sharedMaterials.Any(m => m == null))) throw new InvalidOperationException("Missing saved kitchen material.");
            if (room.GetComponentsInChildren<Camera>().Length > 0 || room.GetComponentsInChildren<Volume>().Length > 0) throw new InvalidOperationException("Presentation leaked into reusable room.");
            Transform table = room.transform.Find("02_LongChoppingTable_ExistingHD2D/ExistingChoppingTable_LongChefWorkstation");
            Transform cauldron = room.transform.Find("03_CauldronSpitRoastAndMeatRack/ExistingLargeCauldron_HD2D");
            if (table.GetComponent<HybridProp>().AssetId != "Chopping_Table" || cauldron.GetComponent<HybridProp>().AssetId != "Cauldron_B_HD2D") throw new InvalidOperationException("Original kitchen assets missing.");
            Debug.Log("KITCHEN_RELOAD_OK: saved meshes and materials; original chopping table and HD2D cauldron; " + room.GetComponentsInChildren<Light>().Length + " practical lights; reusable room has no camera or global volume.");
        }

        public static void Capture()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath);
            Camera camera = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>()).Single();
            Vector3 position = camera.transform.position; Quaternion rotation = camera.transform.rotation; float fov = camera.fieldOfView;
            string output = Path.GetFullPath("Logs/KitchenPreview"); Directory.CreateDirectory(output);
            try
            {
                Render(camera, output + "/kitchen_overview.png", 1800, 1600);
                SetPose(camera, V(.95f, 1.12f, 3.97f), 32, -8, 17); camera.fieldOfView = 43;
                Render(camera, output + "/kitchen_cooking_line.png", 1600, 1200);
                SetPose(camera, V(-5.1f, 1.12f, 3.39f), 39, -14, 16); camera.fieldOfView = 43;
                Render(camera, output + "/kitchen_pantry_and_washing.png", 1500, 1200);
                // Current Cinemachine virtual camera in CaveHD2DLocal, not its stale physical-camera pose.
                SetPose(camera, V(0, 1.6f, 2.2f), 12.09f, 0, 24.6f); camera.fieldOfView = 34;
                Render(camera, output + "/kitchen_map_camera12.png", 1800, 1100);
            }
            finally { camera.transform.SetPositionAndRotation(position, rotation); camera.fieldOfView = fov; }
            Debug.Log("KITCHEN_CAPTURE_OK: " + output);
        }

        private static void Render(Camera camera, string path, int width, int height)
        {
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 }; target.Create();
            var previous = RenderTexture.active; float aspect = camera.aspect; camera.aspect = (float)width / height;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target; camera.Render();
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply(); File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally { camera.targetTexture = null; camera.aspect = aspect; RenderTexture.active = previous; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image); }
        }
    }
}
