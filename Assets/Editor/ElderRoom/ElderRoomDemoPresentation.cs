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
    public static partial class ElderRoomDemoBuilder
    {
        private static Camera BuildPresentation()
        {
            Transform presentation = Group("DemoPresentation_CamerasAndFill", null);
            Transform cameraRoot = Group("Main Camera", presentation); cameraRoot.gameObject.tag = "MainCamera";
            Camera camera = cameraRoot.gameObject.AddComponent<Camera>(); cameraRoot.gameObject.AddComponent<AudioListener>();
            camera.fieldOfView = 34; camera.nearClipPlane = .1f; camera.farClipPlane = 120; camera.allowHDR = true;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.022f, .028f, .030f);
            SetPose(camera, V(0, 1.0f, 1.45f), 52, 0, 39);
            var data = cameraRoot.gameObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true; data.requiresDepthTexture = true; data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            RenderSettings.skybox = null; RenderSettings.fog = false; RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.31f, .37f, .35f); RenderSettings.ambientEquatorColor = new Color(.25f, .30f, .27f); RenderSettings.ambientGroundColor = new Color(.14f, .12f, .08f); RenderSettings.reflectionIntensity = 0;
            Transform fill = Group("CoolCaveBounce", presentation); fill.rotation = Quaternion.Euler(51, -32, 0);
            Light light = fill.gameObject.AddComponent<Light>(); light.type = LightType.Directional;
            light.color = new Color(.50f, .62f, .59f); light.intensity = 1.45f; light.shadows = LightShadows.Soft; light.shadowBias = .025f; light.shadowNormalBias = .11f; RenderSettings.sun = light;
            Transform front = Group("WarmFrontBounce", presentation); front.rotation = Quaternion.Euler(32, 152, 0);
            Light bounce = front.gameObject.AddComponent<Light>(); bounce.type = LightType.Directional; bounce.color = new Color(.65f, .51f, .33f); bounce.intensity = .53f;
            var volume = Group("ElderRoomPresentation", presentation).gameObject.AddComponent<Volume>(); volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>(); profile.name = "GoblinElderRoomPresentation";
            var bloom = profile.Add<Bloom>(true); bloom.intensity.Override(.22f); bloom.threshold.Override(1.1f); bloom.scatter.Override(.6f);
            var color = profile.Add<ColorAdjustments>(true); color.postExposure.Override(.95f); color.contrast.Override(9); color.saturation.Override(-4);
            profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
            var vignette = profile.Add<Vignette>(true); vignette.intensity.Override(.13f); vignette.smoothness.Override(.6f);
            volume.sharedProfile = SaveAsset(profile, "Assets/Settings/GoblinElderRoomPresentation.asset");
            foreach (VolumeComponent component in volume.sharedProfile.components) if (!AssetDatabase.Contains(component)) AssetDatabase.AddObjectToAsset(component, volume.sharedProfile);
            EditorUtility.SetDirty(volume.sharedProfile); return camera;
        }

        private static void SetPose(Camera camera, Vector3 focus, float pitch, float yaw, float distance)
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            camera.transform.SetPositionAndRotation(focus - rotation * Vector3.forward * distance, rotation);
        }

        // Explicit saved-asset correction after the first visual review. Never runs on scene load.
        public static void ApplyReviewAdjustments()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            string roomPath = Prefabs + "/goblin_elder_room.prefab";
            GameObject contents = PrefabUtility.LoadPrefabContents(roomPath);
            try
            {
                Transform visitors = contents.transform.Find("07_VisitorsTeaAndHerbalSupplies");
                foreach (string name in new[] { "herbal_tincture_bottles_3d", "books_scrolls_and_ink_3d", "oil_lantern_3d" })
                {
                    Transform item = visitors.Find(name);
                    Vector3 position = item.localPosition; position.y = 1.096f; item.localPosition = position;
                }
                PrefabUtility.SaveAsPrefabAsset(contents, roomPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            EditorSceneManager.OpenScene(ScenePath);
            GameObject presentation = EditorSceneManager.GetActiveScene().GetRootGameObjects().Single(g => g.name == "DemoPresentation_CamerasAndFill");
            presentation.transform.Find("CoolCaveBounce").GetComponent<Light>().intensity = 1.45f;
            presentation.transform.Find("WarmFrontBounce").GetComponent<Light>().intensity = .53f;
            var profile = presentation.GetComponentInChildren<Volume>().sharedProfile;
            if (profile.TryGet<ColorAdjustments>(out var color)) color.postExposure.Override(.95f);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            EditorSceneManager.OpenScene(ScenePath);
            GameObject room = EditorSceneManager.GetActiveScene().GetRootGameObjects().Single(g => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g) == roomPath);
            if (room.GetComponentsInChildren<MeshFilter>().Any(f => f.sharedMesh == null)) throw new InvalidOperationException("Missing saved mesh.");
            if (room.GetComponentsInChildren<MeshRenderer>().Any(r => r.sharedMaterials.Any(m => m == null))) throw new InvalidOperationException("Missing saved material.");
            if (room.GetComponentsInChildren<Camera>().Length > 0 || room.GetComponentsInChildren<Volume>().Length > 0) throw new InvalidOperationException("Presentation leaked into reusable room.");
            Transform bed = room.transform.Find("04_NorthEastSleepingAlcove/LargeBedReplacementSocket/ExistingWoodenBed_Enlarged_2_15x");
            if (bed == null || bed.GetComponent<HybridProp>().AssetId != "Wooden_Bed") throw new InvalidOperationException("Library bed reference missing.");
            Debug.Log("ELDER_RELOAD_OK: saved meshes/materials; original enlarged bed; separate presentation; " + room.GetComponentsInChildren<Light>().Length + " local lights");
            Capture();
            Debug.Log("ELDER_REVIEW_ADJUSTMENTS_OK");
        }
        public static void Capture()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath);
            Camera camera = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>()).Single();
            Vector3 position = camera.transform.position; Quaternion rotation = camera.transform.rotation; float fov = camera.fieldOfView;
            string output = Path.GetFullPath("Logs/ElderRoomPreview"); Directory.CreateDirectory(output);
            try
            {
                Render(camera, output + "/elder_overview.png", 1800, 1600);
                SetPose(camera, V(-4.05f, 1.25f, 4.55f), 38, -12, 16); camera.fieldOfView = 44;
                Render(camera, output + "/elder_study.png", 1500, 1200);
                SetPose(camera, V(5.65f, 1.1f, 8.42f), 48, -18, 17); camera.fieldOfView = 42;
                Render(camera, output + "/elder_bed_and_secret_passage.png", 1500, 1300);
                // Current CaveHD2DLocal saved camera: 30-degree pitch, 34-degree field of view.
                SetPose(camera, V(0, 1.30f, .2f), 30, 0, 38); camera.fieldOfView = 34;
                Render(camera, output + "/elder_map_camera30.png", 1800, 1100);
            }
            finally { camera.transform.SetPositionAndRotation(position, rotation); camera.fieldOfView = fov; }
            Debug.Log("ELDER_CAPTURE_OK: " + output);
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
