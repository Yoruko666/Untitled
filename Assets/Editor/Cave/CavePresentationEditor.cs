using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Untitled.Editor
{
    public static class CavePresentationEditor
    {
        private const string ProfilePath = "Assets/Settings/CavePresentation.asset";

        [MenuItem("Untitled/Cave/Apply HD2D Presentation %&h")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/Gameplay/Cave.unity") throw new InvalidOperationException("Open Cave.unity first.");
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/CaveWater.shader");
            if (shader == null || ShaderUtil.ShaderHasError(shader) || !shader.isSupported)
                throw new InvalidOperationException("Water shader is unavailable: " + (shader == null ? "missing asset" : string.Join("; ", ShaderUtil.GetShaderMessages(shader).Select(message => message.message))));
            var map = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<CaveMap>(true)).Single();
            if (!map.HasEditableLayout) throw new InvalidOperationException("Enable scene Tilemap editing first.");
            var follow = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<CameraController>(true)).Single();
            Camera camera = follow.GetComponent<Camera>();
            if (follow.Target == null) throw new InvalidOperationException("Assign the camera target first.");
            Undo.RecordObjects(new UnityEngine.Object[] { camera, camera.transform, follow }, "Apply cave camera");
            // EmberSanctum uses a low perspective camera rather than a 45-degree orthographic view.
            var settings = new SerializedObject(follow);
            settings.FindProperty("viewAngle").floatValue = 29.293f;
            settings.FindProperty("distance").floatValue = 20.5f;
            settings.FindProperty("focusOffset").vector3Value = new Vector3(0f, 1.4f, 0f);
            settings.ApplyModifiedProperties();
            camera.orthographic = false;
            camera.fieldOfView = 34f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 80f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.021f, 0.034f, 0.041f);
            camera.allowHDR = true;
            Quaternion rotation = Quaternion.Euler(follow.ViewAngle, 0f, 0f);
            camera.transform.SetPositionAndRotation(follow.Target.position + new Vector3(0f, 1.4f, 0f) - rotation * Vector3.forward * follow.Distance, rotation);
            var cameraData = camera.GetUniversalAdditionalCameraData();
            Undo.RecordObject(cameraData, "Apply cave post processing");
            cameraData.renderPostProcessing = true;
            cameraData.requiresDepthTexture = true;
            cameraData.antialiasing = AntialiasingMode.None;

            foreach (var billboard in follow.Target.GetComponentsInChildren<SpriteBillboard>())
            {
                var serialized = new SerializedObject(billboard);
                serialized.FindProperty("keepUpright").boolValue = true;
                serialized.ApplyModifiedProperties();
                Undo.RecordObject(billboard.transform, "Stand pixel character upright");
                billboard.transform.rotation = Quaternion.identity;
            }

            Material water = map.RiverMaterial;
            Undo.RecordObject(water, "Apply animated cave water");
            water.shader = shader;
            water.SetColor("_DeepColor", new Color(0.025f, 0.13f, 0.17f));
            water.SetColor("_ShallowColor", new Color(0.09f, 0.31f, 0.34f));
            water.SetColor("_FoamColor", new Color(0.32f, 0.57f, 0.55f));
            water.SetFloat("_PixelsPerUnit", 32f / map.CellSize);
            water.SetFloat("_CellSize", map.CellSize);
            water.SetFloat("_FlowSpeed", 0.65f);
            water.SetFloat("_Smoothness", 0.86f);
            water.renderQueue = -1;
            water.shaderKeywords = Array.Empty<string>();
            EditorUtility.SetDirty(water);

            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            else Undo.RegisterCompleteObjectUndo(profile, "Apply cave presentation profile");
            var bloom = GetOrAdd<Bloom>(profile);
            bloom.threshold.Override(1.05f);
            bloom.intensity.Override(0.6f);
            bloom.scatter.Override(0.65f);
            GetOrAdd<Tonemapping>(profile).mode.Override(TonemappingMode.ACES);
            var color = GetOrAdd<ColorAdjustments>(profile);
            color.postExposure.Override(0.25f);
            color.contrast.Override(12f);
            color.saturation.Override(-5f);
            var vignette = GetOrAdd<Vignette>(profile);
            vignette.intensity.Override(0.18f);
            vignette.smoothness.Override(0.65f);
            var depth = GetOrAdd<DepthOfField>(profile);
            depth.mode.Override(DepthOfFieldMode.Bokeh);
            depth.focusDistance.Override(Vector3.Dot(follow.Target.position + Vector3.up * 0.4f - camera.transform.position, camera.transform.forward));
            depth.aperture.Override(4f);
            depth.focalLength.Override(160f);
            EditorUtility.SetDirty(profile);
            var volume = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Volume>()).FirstOrDefault();
            if (volume == null)
            {
                var go = new GameObject("GlobalVolume", typeof(Volume));
                Undo.RegisterCreatedObjectUndo(go, "Create cave global volume");
                go.transform.SetParent(map.transform.root, false);
                volume = go.GetComponent<Volume>();
            }
            Undo.RecordObject(volume, "Assign cave presentation");
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.29f, 0.38f, 0.47f);
            RenderSettings.ambientEquatorColor = new Color(0.16f, 0.21f, 0.25f);
            RenderSettings.ambientGroundColor = new Color(0.09f, 0.10f, 0.11f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.024f, 0.039f, 0.048f);
            RenderSettings.fogStartDistance = 24f;
            RenderSettings.fogEndDistance = 46f;
            CaveMapBuilder.Rebuild(map);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Cave presentation applied: perspective camera, upright sprites, animated river and Volume.");
        }

        private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet<T>(out var component))
            {
                component = profile.Add<T>(true);
                AssetDatabase.AddObjectToAsset(component, profile);
            }
            else Undo.RecordObject(component, "Update cave volume component");
            EditorUtility.SetDirty(component);
            return component;
        }
    }
}
