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
    public static partial class TavernDemoBuilder
    {
        private static void BuildRoom(Transform room)
        {
            Transform shell=Group("01_RoomShell_Cutaway",room);
            var floor=new Geometry();
            floor.Box(V(0,-0.5f,0),V(33.6f,0.9f,33.6f),Finish.StoneDark,Quaternion.identity,0.3f);
            var random=new System.Random(17);
            for(int row=0;row<22;row++)
            {
                float z=-15.3f+row*1.45f;
                for(int col=0;col<17;col++)
                {
                    float x=-15f+col*1.9f+(row%2)*0.9f;
                    if(x>15.5f)continue;
                    float h=(float)random.NextDouble()*0.027f;
                    Finish f=(Finish)((int)Finish.Stone+(col+row*3)%3);
                    floor.Box(V(x,-0.04f+h,z),V(1.81f,0.14f,1.35f),f,Quaternion.Euler(0,(float)random.NextDouble()*2-1,0),0.075f);
                }
            }
            var fg=MeshObject("tavern_stone_floor",floor,shell);
            BoxCollision(fg.transform,V(0,-0.18f,0),V(33,0.34f,33));
            BuildWall(shell,"north_cave_wall",V(0,0,16),32,3.65f,0,false);
            BuildWall(shell,"east_cave_wall",V(16,0,0),32,3.65f,90,false);
            BuildWall(shell,"west_cave_wall_north",V(-16,0,10.8f),10.4f,3.65f,90,false);
            BuildWall(shell,"west_cave_wall_south",V(-16,0,-6.2f),16.9f,3.65f,90,false);
            BuildWall(shell,"south_cutaway_wall_west",V(-11,0,-16),10,0.75f,0,true);
            BuildWall(shell,"south_cutaway_wall_east",V(6.7f,0,-16),18.6f,0.75f,0,true);
            foreach(var p in new[]{V(-16,0,16),V(16,0,16),V(-16,0,-16),V(16,0,-16),V(-16,0,5.65f),V(-16,0,2.2f)})
            {
                var g=new Geometry();
                g.Box(p+V(0,1.7f,0),V(0.65f,3.4f,0.65f),Finish.OakDark,Quaternion.identity,0.07f);
                foreach(float y in new[]{0.35f,2.65f})g.Box(p+V(0,y,0),V(0.72f,0.17f,0.72f),Finish.Iron,Quaternion.identity,0.02f);
                MeshObject("tavern_post_"+p.x+"_"+p.z,g,shell);
            }
            var entrance=new Geometry();
            entrance.Box(V(-16,3.7f,3.9f),V(0.75f,0.55f,4.2f),Finish.OakDark,Quaternion.identity,0.08f);
            for(int i=0;i<4;i++)entrance.Box(V(-17.2f,0.05f,2.5f+i*0.85f),V(2.8f,0.14f,0.79f),Finish.Oak,Quaternion.identity,0.03f);
            entrance.Box(V(-4.55f,0.05f,-16.9f),V(3.3f,0.14f,2.6f),Finish.Oak,Quaternion.identity,0.04f);
            MeshObject("tavern_door_thresholds",entrance,shell);
            BoxCollision(shell,V(-4.55f,-0.08f,-17),V(3.3f,0.22f,2.8f));
            BoxCollision(shell,V(-17,-0.08f,3.9f),V(3,0.22f,3.5f));

            Transform deck=Group("02_BarTimberFloor",room);
            var boards=new Geometry();
            for(int i=0;i<18;i++)boards.Box(V(10.5f,0.09f,6.3f+i*0.53f),V(9.7f,0.16f,0.5f),i%4==0?Finish.OakLight:Finish.OakDark,Quaternion.identity,0.025f);
            MeshObject("tavern_bar_floorboards",boards,deck);
            BoxCollision(deck,V(10.5f,0.04f,10.8f),V(9.7f,0.23f,9.5f));
        }

        private static void BuildWall(Transform parent,string name,Vector3 origin,float length,float height,float yaw,bool cutaway)
        {
            var g=new Geometry();Quaternion q=Quaternion.Euler(0,yaw,0);
            int rows=Mathf.CeilToInt(height/0.72f),cols=Mathf.CeilToInt(length/1.85f);
            for(int row=0;row<rows;row++)for(int col=0;col<cols;col++)
            {
                float width=length/cols-0.055f;
                Vector3 p=origin+q*V(-length/2+(col+0.5f)*length/cols,0.35f+row*0.68f,0);
                g.Box(p,V(width,Mathf.Min(0.64f,height-row*0.68f),0.85f),((col+row)%4==0)?Finish.StoneLight:Finish.Stone,q,0.09f);
            }
            for(int i=0;i<cols;i++)
            {
                Vector3 p=origin+q*V(-length/2+(i+0.5f)*length/cols,height+0.2f,0.2f);
                g.Rock(p,V(length/cols+0.4f,cutaway?0.55f:1.4f,1.5f),Finish.StoneDark,i*0.7f);
            }
            Transform wall=MeshObject(name,g,parent).transform;
            // Mesh positions are baked in module space; the collision follows the wall direction.
            var collision=Group(name+"_Collision",wall);collision.localPosition=origin;collision.localRotation=q;
            BoxCollision(collision,V(0,1.8f,0),V(length,3.6f,0.95f));
        }

        private static void BuildLayout(Transform room)
        {
            Transform dining=Group("03_SixRoundTables",room);
            Vector3[] centers={V(-10,0,9),V(-3,0,9),V(-10,0,-5.8f),V(-3,0,-5.8f),V(4,0,-5.8f),V(11,0,-5.8f)};
            for(int i=0;i<centers.Length;i++)
            {
                Transform set=Group("TableSet_"+(i+1).ToString("00"),dining);set.localPosition=centers[i];
                Place(set,"round_table_3d",Vector3.zero,i*11);
                for(int s=0;s<4;s++)
                {
                    float a=(s*90+35+(i%2)*9)*Mathf.Deg2Rad;
                    Place(set,"wooden_stool_3d",V(Mathf.Cos(a)*2.9f,0,Mathf.Sin(a)*2.9f),s*90+20);
                }
                Place(set,"oil_lantern_3d",V(0,1.575f,0),i*28);
            }
            Transform bar=Group("04_LBarAndWineRacks",room);
            Place(bar,"bar_counter_module_3d",V(7.4f,0.16f,6.4f));
            Place(bar,"bar_counter_module_3d",V(11.52f,0.16f,6.4f));
            Place(bar,"bar_counter_module_3d",V(14.44f,0.16f,6.4f)).localScale=V(0.42f,1,1);
            Place(bar,"bar_counter_module_3d",V(5.62f,0.16f,9.25f),90);
            Place(bar,"bar_counter_module_3d",V(5.62f,0.16f,12.1f),90).localScale=V(0.45f,1,1);
            Place(bar,"saloon_half_gate_3d",V(5.62f,0.16f,14.12f),90);
            Place(bar,"wine_rack_3d",V(12.35f,0.16f,14.75f));
            Place(bar,"wine_rack_3d",V(14.84f,0.16f,11.68f),90);
            foreach(float x in new[]{7.15f,9.65f,12.15f})Place(bar,"wooden_stool_3d",V(x,0,4.45f));
            foreach(float z in new[]{8.5f,10.9f})Place(bar,"wooden_stool_3d",V(3.55f,0,z));
            Place(bar,"oil_lantern_3d",V(6.2f,2.04f,6.4f));
            Place(bar,"oil_lantern_3d",V(12.7f,2.04f,6.4f));
            Place(bar,"ale_barrel_3d",V(7.7f,0.18f,13.4f));
            Place(bar,"ale_barrel_3d",V(9.1f,0.18f,13.5f));
            Place(bar,"supply_crate_3d",V(8.1f,0.18f,11.5f),-12);
            Place(bar,"supply_crate_3d",V(8.1f,1.03f,11.5f),8);

            Transform storage=Group("05_BarrelsCratesAndBenches",room);
            foreach(var p in new[]{V(-14.2f,0,-2.3f),V(-14.2f,0,-4.05f),V(-14.2f,0,-5.8f),V(-14.2f,0,12.7f),V(-12.7f,0,13.3f),V(14.1f,0,0.5f),V(14.1f,0,2.2f),V(12.55f,0,1.3f),V(13.9f,0,-13.8f),V(12.3f,0,-13.8f)})
                Place(storage,"ale_barrel_3d",p,p.x*13);
            Place(storage,"ale_barrel_3d",V(13.25f,1.85f,-13.8f),12,0.8f);
            foreach(var p in new[]{V(-14,0,-13.6f),V(-12.25f,0,-13.75f),V(-13.95f,0.85f,-13.55f),V(14.2f,0,13.85f)})Place(storage,"supply_crate_3d",p,p.z*7);
            Place(storage,"wall_bench_3d",V(-9.2f,0,-14.25f));
            Place(storage,"wall_bench_3d",V(0.25f,0,-14.25f));
            Place(storage,"wall_bench_3d",V(14.25f,0,-9.8f),90);
            Place(storage,"wall_bench_3d",V(-14.1f,0,-9.8f),90);
            BuildRug(storage);
            BuildHearth(room);
            BuildGoblinDetails(room);
            Transform lamps=Group("08_FireAndOilLighting",room);
            foreach(float x in new[]{-13.2f,-0.3f,8f})Place(lamps,"wall_brazier_3d",V(x,2.5f,15.42f));
            foreach(float z in new[]{-10.8f,0.2f,10.2f})Place(lamps,"wall_brazier_3d",V(15.42f,2.5f,z),90);
            foreach(float z in new[]{-8.5f,-0.4f,8f})Place(lamps,"wall_brazier_3d",V(-15.42f,2.5f,z),-90);
            Place(lamps,"iron_chandelier_3d",V(-6.5f,4.6f,8.8f));
            Place(lamps,"iron_chandelier_3d",V(3.2f,4.8f,-5.6f));
            Place(lamps,"iron_chandelier_3d",V(-0.5f,5.2f,1.4f));
            Place(lamps,"oil_lantern_3d",V(-8.1f,0.86f,-14.25f));
        }

        private static void BuildRug(Transform parent)
        {
            var g=new Geometry();
            g.Box(V(-1.8f,0.105f,1.55f),V(21f,0.035f,2.7f),Finish.Cloth,Quaternion.identity,0.01f);
            foreach(float z in new[]{0.33f,2.77f})g.Box(V(-1.8f,0.13f,z),V(20.7f,0.018f,0.08f),Finish.Cork,Quaternion.identity,0.003f);
            for(int i=0;i<14;i++)
            {
                float x=-11.4f+i*1.47f;
                g.Box(V(x,0.133f,1.55f),V(0.37f,0.02f,0.37f),Finish.Cork,Quaternion.Euler(0,45,0),0.005f);
                foreach(float end in new[]{-12.45f,8.85f})g.Beam(V(end,0.13f,0.4f+i*0.18f),V(end+0.22f,0.13f,0.42f+i*0.18f),0.02f,Finish.Bone);
            }
            g.Box(V(-4.5f,0.105f,-13.25f),V(3f,0.025f,4.7f),Finish.Cloth,Quaternion.identity,0.01f);
            MeshObject("tavern_woven_runners",g,parent);
        }

        private static void BuildHearth(Transform room)
        {
            Transform hearth=Group("06_StoneHearthAndFirewood",room);
            var g=new Geometry();
            g.Box(V(-8.9f,0.18f,14.25f),V(4.3f,0.35f,2.35f),Finish.StoneDark,Quaternion.identity,0.13f);
            foreach(float x in new[]{-10.5f,-7.3f})for(int y=0;y<4;y++)g.Box(V(x,0.65f+y*0.63f,14.8f),V(0.7f,0.58f,1.2f),Finish.StoneLight,Quaternion.identity,0.095f);
            g.Box(V(-8.9f,2.87f,14.8f),V(4.05f,0.5f,1.35f),Finish.StoneLight,Quaternion.identity,0.11f);
            g.Box(V(-8.9f,3.52f,15.15f),V(2.65f,0.84f,0.85f),Finish.StoneDark,Quaternion.identity,0.07f);
            g.Box(V(-8.9f,1.2f,15.1f),V(2.5f,2f,0.2f),Finish.Iron,Quaternion.identity,0.05f);
            for(int i=0;i<7;i++)g.Beam(V(-10.2f+i*0.43f,0.48f,13.65f),V(-10.2f+i*0.43f,1.18f,13.65f),0.045f,Finish.Iron);
            foreach(float y in new[]{0.58f,1.05f})g.Beam(V(-10.2f,y,13.65f),V(-7.6f,y,13.65f),0.045f,Finish.Iron);
            for(int i=0;i<4;i++)
            {
                g.Beam(V(-9.7f,0.50f+i*0.08f,14+i*0.23f),V(-8f,0.5f+i*0.08f,14.3f+i*0.1f),0.22f,Finish.OakDark);
                g.Lathe(V(-9.65f+i*0.5f,0.57f,14.25f),new[]{V(0.2f,0,0),V(0.23f,0.28f,0),V(0.1f,0.58f+i*0.08f,0),V(0.006f,0.85f+i*0.09f,0)},i%2==0?Finish.Flame:Finish.Ember,9);
            }
            var model=MeshObject("tavern_stone_hearth",g,hearth);
            BoxCollision(hearth,V(-8.9f,1.65f,14.55f),V(4.25f,3.3f,2.4f));
            var fire=Group("HearthWarmLight",hearth);fire.localPosition=V(-8.9f,1.4f,13.7f);
            var light=fire.gameObject.AddComponent<Light>();light.type=LightType.Point;light.color=warm;light.range=13;light.intensity=8;light.shadows=LightShadows.Soft;light.shadowNormalBias=0.1f;
            var flicker=fire.gameObject.AddComponent<HybridFireLight>();var settings=new SerializedObject(flicker);settings.FindProperty("baseIntensity").floatValue=8;settings.FindProperty("baseRange").floatValue=13;settings.ApplyModifiedPropertiesWithoutUndo();
            Reuse(hearth,"Kitchen/firewood_stack_hd2d",V(-5.75f,0,14.6f),1.45f,0);
            Reuse(hearth,"Kitchen/firewood_bundle_hd2d",V(-6.2f,0,13.4f),1.15f,8);
        }

        private static void BuildGoblinDetails(Transform room)
        {
            Transform details=Group("07_GoblinTrophiesAndMusicNook",room);
            Place(details,"tavern_ale_sign_3d",V(-15.8f,2.35f,6.9f),-90);
            Place(details,"tavern_ale_sign_3d",V(10.4f,3.4f,15.2f));
            Reuse(details,"TotemsBones/rune_banner_hd2d",V(-2f,2.2f,15.37f),1.8f,0);
            Reuse(details,"TotemsBones/rune_shield_hd2d",V(1.2f,2.3f,15.37f),1.25f,0);
            Reuse(details,"TotemsBones/bone_pile_hd2d",V(14.1f,0,7.95f),0.8f,0);
            var g=new Geometry();
            // A small musician's corner breaks up the southern edge without closing the entrance lane.
            for(int i=0;i<5;i++)g.Box(V(6.7f,0.24f,-13.25f+i*0.40f),V(5.3f,0.35f,0.38f),Finish.OakDark,Quaternion.identity,0.03f);
            g.Lathe(V(7.8f,0.4f,-12.25f),new[]{V(0.5f,0,0),V(0.43f,0.4f,0),V(0.65f,0.95f,0),V(0.69f,1.05f,0)},Finish.Oak,16);
            g.Lathe(V(7.8f,1.44f,-12.25f),new[]{V(0.69f,0,0),V(0.69f,0.03f,0)},Finish.Bone,24);
            g.Ring(V(7.8f,1.4f,-12.25f),0.73f,0.68f,0.1f,Finish.Iron,24);
            for(int i=0;i<12;i++){float a=i*Mathf.PI/6;g.Beam(V(7.8f+Mathf.Cos(a)*0.49f,0.45f,-12.25f+Mathf.Sin(a)*0.49f),V(7.8f+Mathf.Cos(a)*0.70f,1.4f,-12.25f+Mathf.Sin(a)*0.70f),0.022f,Finish.Cork);}
            g.Beam(V(7.45f,1.49f,-12.65f),V(8.18f,1.50f,-11.98f),0.045f,Finish.Bone);
            g.Beam(V(7.44f,1.50f,-12f),V(8.18f,1.51f,-12.51f),0.045f,Finish.Bone);
            g.Box(V(3.4f,0.75f,14.4f),V(1.6f,1.5f,0.8f),Finish.OakDark,Quaternion.identity,0.1f);
            for(int i=0;i<7;i++)g.Rock(V(3.4f+Mathf.Cos(i)*0.65f,1.57f+0.07f*Mathf.Sin(i),14.4f+Mathf.Sin(i)*0.26f),V(0.25f,0.14f,0.32f),Finish.Bone,i);
            g.Beam(V(2.9f,1.8f,14.5f),V(2.45f,2.55f,14.5f),0.12f,Finish.Bone);
            g.Beam(V(3.9f,1.8f,14.5f),V(4.35f,2.55f,14.5f),0.12f,Finish.Bone);
            MeshObject("tavern_drum_and_bone_trophy",g,details);
            Place(details,"wooden_stool_3d",V(5.7f,0.42f,-12.35f));
            Place(details,"oil_lantern_3d",V(4.6f,0.44f,-12.7f));
        }

        private static void Reuse(Transform parent,string path,Vector3 position,float scale,float yaw)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/"+path+".prefab");
            if(prefab==null)throw new InvalidOperationException("Missing existing library prop: "+path);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);
            go.transform.localPosition=position;go.transform.localScale=Vector3.one*scale;go.transform.localRotation=Quaternion.Euler(0,yaw,0);
        }

        private static Camera BuildPresentation()
        {
            Transform presentation=Group("DemoPresentation_CamerasAndFill",null);
            var cameraGo=new GameObject("Main Camera");cameraGo.transform.SetParent(presentation,false);cameraGo.tag="MainCamera";
            var camera=cameraGo.AddComponent<Camera>();cameraGo.AddComponent<AudioListener>();
            camera.fieldOfView=34;camera.nearClipPlane=0.1f;camera.farClipPlane=160;camera.allowHDR=true;camera.backgroundColor=new Color(0.036f,0.051f,0.052f);camera.clearFlags=CameraClearFlags.SolidColor;
            SetPose(camera,V(0,1.3f,1.1f),48,0,68);
            var data=cameraGo.AddComponent<UniversalAdditionalCameraData>();data.renderPostProcessing=true;data.requiresDepthTexture=true;data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            RenderSettings.skybox=null;RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(0.33f,0.38f,0.37f);RenderSettings.ambientEquatorColor=new Color(0.24f,0.27f,0.24f);RenderSettings.ambientGroundColor=new Color(0.15f,0.12f,0.085f);RenderSettings.reflectionIntensity=0;
            var fill=Group("CoolCaveBounce",presentation);fill.rotation=Quaternion.Euler(50,-35,0);
            var light=fill.gameObject.AddComponent<Light>();light.type=LightType.Directional;light.color=new Color(0.59f,0.69f,0.71f);light.intensity=1.35f;light.shadows=LightShadows.Soft;light.shadowBias=0.025f;light.shadowNormalBias=0.15f;RenderSettings.sun=light;
            var front=Group("SoftFrontBounce",presentation);front.rotation=Quaternion.Euler(35,155,0);
            var frontLight=front.gameObject.AddComponent<Light>();frontLight.type=LightType.Directional;frontLight.color=new Color(0.67f,0.58f,0.42f);frontLight.intensity=0.4f;
            var volume=Group("TavernPresentation",presentation).gameObject.AddComponent<Volume>();volume.isGlobal=true;
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.name="GoblinTavernPresentation";
            var bloom=profile.Add<Bloom>(true);bloom.intensity.Override(0.3f);bloom.threshold.Override(1.15f);bloom.scatter.Override(0.62f);
            var color=profile.Add<ColorAdjustments>(true);color.postExposure.Override(0.8f);color.contrast.Override(8);color.saturation.Override(-6);
            var tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.ACES);
            var vignette=profile.Add<Vignette>(true);vignette.intensity.Override(0.16f);vignette.smoothness.Override(0.6f);
            volume.sharedProfile=SaveAsset(profile,"Assets/Settings/GoblinTavernPresentation.asset");
            foreach(VolumeComponent component in volume.sharedProfile.components)
                if(!AssetDatabase.Contains(component))AssetDatabase.AddObjectToAsset(component,volume.sharedProfile);
            EditorUtility.SetDirty(volume.sharedProfile);
            return camera;
        }

        private static void SetPose(Camera camera,Vector3 focus,float pitch,float yaw,float distance)
        {
            Quaternion rotation=Quaternion.Euler(pitch,yaw,0);camera.transform.SetPositionAndRotation(focus-rotation*Vector3.forward*distance,rotation);
        }

        public static void Capture()
        {
            var scene=EditorSceneManager.GetActiveScene();if(scene.path!=ScenePath)scene=EditorSceneManager.OpenScene(ScenePath);
            Camera camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).Single();
            Vector3 position=camera.transform.position;Quaternion rotation=camera.transform.rotation;float fov=camera.fieldOfView;
            string output=Path.GetFullPath("Logs/TavernPreview");Directory.CreateDirectory(output);
            try
            {
                Render(camera,output+"/tavern_overview.png",1800,1400);
                SetPose(camera,V(9.5f,1.6f,10.7f),36,-18,24);camera.fieldOfView=44;
                Render(camera,output+"/tavern_bar.png",1600,1200);
                SetPose(camera,V(-6f,1.1f,-5.8f),30,-12,26);camera.fieldOfView=42;
                Render(camera,output+"/tavern_cave30.png",1600,1000);
            }
            finally{camera.transform.SetPositionAndRotation(position,rotation);camera.fieldOfView=fov;}
            Debug.Log("TAVERN_CAPTURE_OK: "+output);
        }

        private static void Render(Camera camera,string path,int width,int height)
        {
            var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32){antiAliasing=1};target.Create();
            var previous=RenderTexture.active;float aspect=camera.aspect;camera.aspect=(float)width/height;
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            try
            {
                // Camera.Render initializes URP in an offline batch editor; the request then includes its post processing.
                camera.targetTexture=target;camera.Render();
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                RenderTexture.active=target;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
                File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally{camera.targetTexture=null;camera.aspect=aspect;RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);}
        }
    }
}
