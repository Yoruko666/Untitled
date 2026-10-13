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
    public static partial class RitualHallDemoBuilder
    {
        private static void BuildPresentation()
        {
            Transform presentation=Group("DemoPresentation_CameraAndCaveBounce",null);
            Transform cameraRoot=Group("Main Camera",presentation); cameraRoot.gameObject.tag="MainCamera";
            Camera camera=cameraRoot.gameObject.AddComponent<Camera>(); cameraRoot.gameObject.AddComponent<AudioListener>();
            camera.fieldOfView=38; camera.nearClipPlane=.10f; camera.farClipPlane=140; camera.allowHDR=true;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.019f,.027f,.025f);
            SetPose(camera,V(0,.9f,.75f),49,0,49);
            var data=cameraRoot.gameObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing=true; data.requiresDepthTexture=true; data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            RenderSettings.skybox=null; RenderSettings.fog=false; RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.31f,.37f,.34f); RenderSettings.ambientEquatorColor=new Color(.25f,.30f,.27f);
            RenderSettings.ambientGroundColor=new Color(.13f,.13f,.08f); RenderSettings.reflectionIntensity=0;
            Transform fill=Group("CoolCaveReflectedFill",presentation); fill.rotation=Quaternion.Euler(56,-30,0);
            Light light=fill.gameObject.AddComponent<Light>(); light.type=LightType.Directional;
            light.color=new Color(.50f,.66f,.62f); light.intensity=1.32f; light.shadows=LightShadows.Soft;
            light.shadowBias=.025f; light.shadowNormalBias=.10f; RenderSettings.sun=light;
            Transform front=Group("SoftFrontBounce",presentation); front.rotation=Quaternion.Euler(32,151,0);
            Light bounce=front.gameObject.AddComponent<Light>(); bounce.type=LightType.Directional; bounce.color=new Color(.69f,.51f,.30f); bounce.intensity=.27f;
            var volume=Group("RitualHallPresentation",presentation).gameObject.AddComponent<Volume>(); volume.isGlobal=true;
            var profile=ScriptableObject.CreateInstance<VolumeProfile>(); profile.name="GoblinRitualHallPresentation";
            var bloom=profile.Add<Bloom>(true); bloom.intensity.Override(.24f); bloom.threshold.Override(1.08f); bloom.scatter.Override(.57f);
            var color=profile.Add<ColorAdjustments>(true); color.postExposure.Override(.75f); color.contrast.Override(12); color.saturation.Override(-5);
            profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
            var vignette=profile.Add<Vignette>(true); vignette.intensity.Override(.16f); vignette.smoothness.Override(.64f);
            volume.sharedProfile=SaveAsset(profile,"Assets/Settings/GoblinRitualHallPresentation.asset");
            foreach(VolumeComponent c in volume.sharedProfile.components)if(!AssetDatabase.Contains(c))AssetDatabase.AddObjectToAsset(c,volume.sharedProfile);
            EditorUtility.SetDirty(volume.sharedProfile);
        }

        private static void SetPose(Camera camera,Vector3 focus,float pitch,float yaw,float distance)
        {
            Quaternion rotation=Quaternion.Euler(pitch,yaw,0); camera.transform.SetPositionAndRotation(focus-rotation*Vector3.forward*distance,rotation);
        }

        private static void ValidateSavedScene()
        {
            var scene=EditorSceneManager.GetActiveScene();
            GameObject room=scene.GetRootGameObjects().Single(g=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g)==Prefabs+"/goblin_ritual_hall_room.prefab");
            if(room.GetComponentsInChildren<MeshFilter>().Any(f=>f.sharedMesh==null))throw new InvalidOperationException("Missing saved ritual mesh.");
            if(room.GetComponentsInChildren<MeshRenderer>().Any(r=>r.sharedMaterials.Any(m=>m==null)))throw new InvalidOperationException("Missing saved ritual material.");
            if(room.GetComponentsInChildren<Camera>().Length!=0||room.GetComponentsInChildren<Volume>().Length!=0)throw new InvalidOperationException("Presentation must stay outside the reusable room.");
            int torches=room.GetComponentsInChildren<MeshFilter>().Count(f=>f.sharedMesh.name=="bone_bound_procession_torch_3d");
            int offerings=room.GetComponentsInChildren<MeshFilter>().Count(f=>f.sharedMesh.name=="carved_offering_table_module_3d");
            if(torches!=10||offerings!=10)throw new InvalidOperationException("Reference layout requires ten torches and ten U-shaped offering modules.");
            Physics.SyncTransforms();
            if(!Physics.Raycast(V(3.7f,2,6.4f),Vector3.down,out RaycastHit hit,6)||hit.point.y>-.75f||!hit.collider.name.StartsWith("ritual_secret_descending_tread_"))throw new InvalidOperationException("Secret stairwell must be a real open floor hole.");
            Debug.Log($"RITUAL_RELOAD_OK: {torches} torches, {offerings} offering modules, {room.GetComponentsInChildren<Light>().Length} practical lights; saved meshes/materials; open stairwell hit Y={hit.point.y:F2}.");
        }

        // Focused correction after actual camera renders. Never runs automatically.
        public static void ApplyVisualReview()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
            palette=Enum.GetValues(typeof(Finish)).Cast<Finish>().Select(f=>AssetDatabase.LoadAssetAtPath<Material>(Materials+"/ritual_"+f.ToString().ToLowerInvariant()+".mat")).ToArray();
            string path=Prefabs+"/goblin_ritual_hall_room.prefab";
            GameObject contents=PrefabUtility.LoadPrefabContents(path);
            try
            {
                Transform passage=contents.transform.Find("05_SecretStairwell_BetweenAltarAndOfferings");
                Object.DestroyImmediate(passage.gameObject);
                BuildSecretPassage(contents.transform);
                PrefabUtility.SaveAsPrefabAsset(contents,path);
            }
            finally {PrefabUtility.UnloadPrefabContents(contents);}
            EditorSceneManager.OpenScene(ScenePath);
            Transform presentation=EditorSceneManager.GetActiveScene().GetRootGameObjects().Single(g=>g.name=="DemoPresentation_CameraAndCaveBounce").transform;
            presentation.Find("CoolCaveReflectedFill").GetComponent<Light>().intensity=1.32f;
            RenderSettings.ambientSkyColor=new Color(.31f,.37f,.34f);
            RenderSettings.ambientEquatorColor=new Color(.25f,.30f,.27f);
            RenderSettings.ambientGroundColor=new Color(.13f,.13f,.08f);
            VolumeProfile profile=presentation.GetComponentInChildren<Volume>().sharedProfile;
            if(profile.TryGet<ColorAdjustments>(out var color))color.postExposure.Override(.75f);
            EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            EditorSceneManager.OpenScene(ScenePath);Capture();
            Debug.Log("RITUAL_VISUAL_REVIEW_OK: clear descending stairwell, lower tunnel, lifted cave bounce.");
        }
        public static void Capture()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
            var scene=EditorSceneManager.GetActiveScene(); if(scene.path!=ScenePath)scene=EditorSceneManager.OpenScene(ScenePath);
            ValidateSavedScene();
            Camera camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).Single();
            Vector3 position=camera.transform.position; Quaternion rotation=camera.transform.rotation; float fov=camera.fieldOfView;
            string output=Path.GetFullPath("Logs/RitualHallPreview"); Directory.CreateDirectory(output);
            try
            {
                Render(camera,output+"/ritual_hall_overview.png",1800,1700);
                SetPose(camera,V(0,1.95f,7.45f),35,-8,23); camera.fieldOfView=43;
                Render(camera,output+"/ritual_hall_altar.png",1800,1300);
                SetPose(camera,V(3.7f,.08f,6.45f),62,-19,11.4f); camera.fieldOfView=43;
                Render(camera,output+"/ritual_hall_secret_stair.png",1500,1300);
                // Matches the enabled Cinemachine camera in the revised CaveHD2DLocal scene.
                SetPose(camera,V(0,1.6f,5.0f),12.09f,0,24.6f); camera.fieldOfView=34;
                Render(camera,output+"/ritual_hall_map_camera12.png",1800,1100);
            }
            finally {camera.transform.SetPositionAndRotation(position,rotation);camera.fieldOfView=fov;}
            Debug.Log("RITUAL_CAPTURE_OK: "+output);
        }

        private static void Render(Camera camera,string path,int width,int height)
        {
            var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32){antiAliasing=1}; target.Create();
            RenderTexture previous=RenderTexture.active; float aspect=camera.aspect; camera.aspect=(float)width/height;
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target; camera.Render();
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                RenderTexture.active=target; image.ReadPixels(new Rect(0,0,width,height),0,0); image.Apply(); File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally {camera.targetTexture=null;camera.aspect=aspect;RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);}
        }
    }
}
