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
    public static partial class CasinoDemoBuilder
    {
        private static void BuildShell(Transform room)
        {
            Transform shell=Group("01_CaveRoom_Cutaway",room);
            var baseGeometry=new Geometry();
            baseGeometry.Box(V(0,-.45f,0),V(29.6f,.82f,29.6f),Finish.StoneDark,Quaternion.identity,.24f);
            Transform floor=MeshObject("casino_floor_foundation",baseGeometry,shell).transform;
            BoxCollision(floor,V(0,-.19f,0),V(29.2f,.38f,29.2f));
            var random=new System.Random(42);
            // Split the floor into quadrants to keep warm local lights relevant to each mesh.
            for(int quadrant=0;quadrant<4;quadrant++)
            {
                var tiles=new Geometry();
                for(int z=0;z<10;z++)for(int x=0;x<10;x++)
                {
                    float px=-13.3f+x*1.4f+(quadrant%2)*14;
                    float pz=-13.3f+z*1.4f+(quadrant/2)*14;
                    var finish=(Finish)((int)Finish.Stone+(x+z*3+quadrant)%3);
                    tiles.Box(V(px,-.025f+(float)random.NextDouble()*.02f,pz),V(1.34f,.105f,1.34f),finish,Quaternion.Euler(0,(float)random.NextDouble()*1.4f-.7f,0),.065f);
                }
                MeshObject("casino_floor_tiles_"+quadrant,tiles,shell);
            }
            Wall(shell,"casino_north_wall",V(0,0,14.25f),28.5f,2.9f,0);
            Wall(shell,"casino_east_wall",V(14.25f,0,0),28.5f,2.9f,90);
            Wall(shell,"casino_west_north_wall",V(-14.25f,0,8.2f),12.1f,2.9f,90);
            Wall(shell,"casino_west_south_wall",V(-14.25f,0,-8.2f),12.1f,2.9f,90);
            Wall(shell,"casino_south_cutaway_wall",V(0,0,-14.25f),28.5f,.55f,0);
            var threshold=new Geometry();
            threshold.Box(V(-14.8f,-.01f,0),V(3.1f,.15f,3.75f),Finish.StoneLight,Quaternion.identity,.08f);
            MeshObject("casino_entrance_threshold",threshold,shell);
            BoxCollision(shell,V(-14.8f,-.1f,0),V(3.1f,.2f,3.75f));
            var bracing=new Geometry();
            foreach(Vector3 p in new[]{V(-14,0,14),V(14,0,14),V(-14,0,2),V(-14,0,-2),V(14,0,-14),V(-14,0,-14)})
            {
                bracing.Box(p+V(0,1.55f,0),V(.50f,3.1f,.50f),Finish.OakDark,Quaternion.identity,.06f);
                foreach(float y in new[]{.30f,2.6f})bracing.Box(p+V(0,y,0),V(.56f,.15f,.56f),Finish.Iron,Quaternion.identity,.02f);
            }
            bracing.Box(V(-14.1f,3.1f,0),V(.62f,.42f,4.35f),Finish.OakDark,Quaternion.identity,.06f);
            MeshObject("casino_timber_bracing",bracing,shell);

            Transform office=Group("02_OfficePartitions_WestDoor",room);
            Wall(office,"office_west_north_partition",V(2,0,10.7f),7.1f,2.15f,90);
            Wall(office,"office_west_door_pier",V(2,0,4.8f),1.55f,2.15f,90);
            Wall(office,"office_south_cutaway_partition",V(8.1f,0,4),12.2f,.85f,0);
            var door=new Geometry();
            foreach(float z in new[]{5.62f,7.15f})door.Box(V(2,1.45f,z),V(.34f,2.9f,.34f),Finish.OakDark,Quaternion.identity,.04f);
            door.Box(V(2,2.9f,6.4f),V(.4f,.28f,1.88f),Finish.Oak,Quaternion.identity,.05f);
            door.Box(V(2,.08f,6.4f),V(1.7f,.15f,1.53f),Finish.StoneLight,Quaternion.identity,.04f);
            // A gathered curtain leaves the office entrance visibly open.
            for(int i=0;i<5;i++)door.Box(V(1.9f,1.73f,7.0f-i*.07f),V(.09f,2.1f,.09f),Finish.Burgundy,Quaternion.identity,.03f);
            MeshObject("casino_boss_door_frame",door,office);
        }

        private static void Wall(Transform parent,string name,Vector3 origin,float length,float height,float yaw)
        {
            var g=new Geometry();
            Quaternion q=Quaternion.Euler(0,yaw,0);
            int rows=Mathf.CeilToInt(height/.62f),columns=Mathf.CeilToInt(length/1.5f);
            for(int row=0;row<rows;row++)for(int col=0;col<columns;col++)
            {
                float h=Mathf.Min(.57f,height-row*.62f);
                float x=-length*.5f+(col+.5f)*length/columns;
                g.Box(origin+q*V(x,row*.62f+h*.5f,0),V(length/columns-.045f,h,.7f),(col+row)%4==0?Finish.StoneLight:Finish.Stone,q,.08f);
            }
            for(int i=0;i<columns;i++)
            {
                float x=-length*.5f+(i+.5f)*length/columns;
                g.Rock(origin+q*V(x,height+.14f,.10f),V(length/columns+.23f,height<1?.42f:.65f,.98f),Finish.StoneDark,i*.37f);
                if(i%5==1)g.Rock(origin+q*V(x,.16f,-.4f),V(.65f,.16f,.18f),Finish.Moss,i);
            }
            Transform wall=MeshObject(name,g,parent).transform;
            Transform blocker=Group("FullHeightBoundaryCollider",wall);
            blocker.localPosition=origin;
            blocker.localRotation=q;
            BoxCollision(blocker,V(0,1.65f,0),V(length,3.3f,.78f));
        }

        private static void BuildGambling(Transform room)
        {
            Transform games=Group("03_ThreeGamblingCircles",room);
            Vector3[] centers={V(-7,0,7),V(-7,0,-7),V(7,0,-7)};
            string[] names={"01_BoneDiceCircle","02_MarkedCardsCircle","03_RuneWheelCircle"};
            string[] gamesProps={"bone_dice_and_cup_3d","marked_card_hand_3d","rune_luck_wheel_3d"};
            for(int i=0;i<3;i++)
            {
                Transform set=Group(names[i],games);
                set.localPosition=centers[i];
                Place(set,"low_stone_gambling_table_3d",Vector3.zero);
                for(int side=0;side<4;side++)
                {
                    Quaternion q=Quaternion.Euler(0,side*90,0);
                    Transform mat=Place(set,"woven_gambling_mat_3d",q*V(0,0,3.1f),side*90);
                    mat.name="SeatMat_"+(side+1);
                }
                Place(set,gamesProps[i],V(0,.76f,0));
                Place(set,"coins_and_chip_stacks_3d",V(-.74f,.76f,-.76f),12,.64f);
                Place(set,"coins_and_chip_stacks_3d",V(.80f,.76f,.69f),-24,.58f);
                Place(set,"tied_coin_sack_3d",V(2.1f,0,-2.20f),0,.8f);
                Transform lantern=Reuse(set,"Tavern/oil_lantern_3d",V(-2.02f,0,2.04f),.85f);
                SetLamp(lantern,3.4f,7.5f);
                // Static pixel inhabitants set scale and mood, without player/NPC logic.
                Goblin(set,"Dealer",V(.15f,.15f,3.05f),1.8f);
                Goblin(set,"Gambler",V(-3.12f,.15f,.2f),1.6f);
            }
        }

        private static void Goblin(Transform parent,string name,Vector3 position,float scale)
        {
            Transform actor=Group(name+"_StaticPixelFigure",parent);
            actor.localPosition=position;
            actor.localScale=Vector3.one*scale;
            actor.gameObject.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Models/GoblinBillboard.asset");
            var renderer=actor.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/FirstRoom/GoblinLit.mat");
            renderer.shadowCastingMode=ShadowCastingMode.TwoSided;
            var billboard=actor.gameObject.AddComponent<SpriteBillboard>();
            var settings=new SerializedObject(billboard);
            settings.FindProperty("keepUpright").boolValue=true;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildOffice(Transform room)
        {
            Transform office=Group("04_BossOfficeAndGoldVault",room);
            var rug=new Geometry();
            rug.Box(V(7.5f,.072f,9.4f),V(7.6f,.04f,7.9f),Finish.Burgundy,Quaternion.identity,.04f);
            foreach(float x in new[]{3.9f,11.1f})rug.Box(V(x,.103f,9.4f),V(.055f,.015f,7.5f),Finish.Straw,Quaternion.identity,.005f);
            foreach(float z in new[]{5.65f,13.15f})rug.Box(V(7.5f,.103f,z),V(7.22f,.015f,.055f),Finish.Straw,Quaternion.identity,.005f);
            for(int i=0;i<11;i++)rug.Box(V(4.0f+i*.68f,.113f,5.85f),V(.18f,.012f,.18f),Finish.Gold,Quaternion.Euler(0,45,0),.003f);
            MeshObject("casino_boss_embroidered_rug",rug,office);
            Place(office,"boss_ledger_desk_3d",V(6.9f,.1f,8.1f));
            Place(office,"boss_bone_crest_chair_3d",V(6.9f,.1f,10.12f));
            Goblin(office,"CasinoBoss",V(6.9f,1.07f,10.05f),1.7f);
            Place(office,"coins_and_chip_stacks_3d",V(5.7f,1.56f,8.05f),0,.53f);
            Transform deskLamp=Reuse(office,"Tavern/oil_lantern_3d",V(8.16f,1.56f,8.2f),.55f);
            SetLamp(deskLamp,2.6f,6.5f);
            Place(office,"riveted_vault_safe_3d",V(11.6f,.06f,12.0f));
            var vaultBase=new Geometry();
            vaultBase.Box(V(11.8f,.17f,8.25f),V(3.1f,.28f,2.7f),Finish.OakDark,Quaternion.identity,.06f);
            for(int i=0;i<4;i++)vaultBase.Box(V(11.8f,.325f,7.28f+i*.65f),V(3.08f,.04f,.61f),Finish.Oak,Quaternion.identity,.01f);
            MeshObject("casino_gold_pallet",vaultBase,office);
            Place(office,"stacked_gold_reserve_3d",V(11.8f,.35f,8.25f),0,.92f);
            foreach(Vector3 p in new[]{V(9.6f,.11f,11.45f),V(9.45f,.1f,12.5f),V(12.7f,0,6.1f)})Place(office,"tied_coin_sack_3d",p,15,1.12f);
            Reuse(office,"Tavern/supply_crate_3d",V(4.1f,.12f,12.8f),.8f,8);
            Reuse(office,"Tavern/supply_crate_3d",V(4.1f,.8f,12.8f),.7f,-8);
            Reuse(office,"TotemsBones/rune_banner_hd2d",V(7,.95f,13.82f),1.9f);
            Reuse(office,"TotemsBones/rune_shield_hd2d",V(4.5f,1.25f,13.82f),1.12f);
        }

        private static void BuildClutter(Transform room)
        {
            Transform edge=Group("05_CashierWaitingAndStorage",room);
            Place(edge,"caged_cashier_counter_3d",V(-.28f,0,11.3f));
            Place(edge,"crossed_dice_casino_sign_3d",V(-.28f,3.52f,11.35f),0,.8f);
            Goblin(edge,"Cashier",V(-.28f,0,12.25f),1.6f);
            Place(edge,"coins_and_chip_stacks_3d",V(-.77f,1.46f,10.8f),0,.58f);
            Place(edge,"tied_coin_sack_3d",V(-1.16f,0,12.4f));
            Place(edge,"tied_coin_sack_3d",V(.60f,0,12.7f));
            Transform cashierLamp=Reuse(edge,"Tavern/oil_lantern_3d",V(.75f,1.46f,11.5f),.52f);
            SetLamp(cashierLamp,2.1f,5.5f);
            foreach(Vector3 p in new[]{V(-12.35f,0,12.2f),V(-10.65f,0,12.7f),V(12.3f,0,1.8f),V(12.4f,0,.1f),V(10.8f,0,.7f),V(-12.45f,0,-12.5f)})
                Reuse(edge,"Tavern/ale_barrel_3d",p,.83f,p.z*7);
            foreach(Vector3 p in new[]{V(-12.4f,0,-.25f),V(-12.2f,0,1.30f),V(12.1f,0,-12.5f),V(10.6f,0,-12.65f),V(12.1f,.75f,-12.5f)})
                Reuse(edge,"Tavern/supply_crate_3d",p,.8f,p.x*3);
            Reuse(edge,"Tavern/wall_bench_3d",V(-.2f,0,-12.7f),.92f);
            Reuse(edge,"Tavern/wall_bench_3d",V(-12.75f,0,-4.8f),.85f,90);
            Reuse(edge,"Tavern/wall_bench_3d",V(12.65f,0,-4.4f),.85f,90);
            Reuse(edge,"Tavern/wooden_stool_3d",V(-2.2f,0,9.35f),.85f);
            Reuse(edge,"Tavern/wooden_stool_3d",V(11.65f,0,-1.8f),.8f);
            Place(edge,"tied_coin_sack_3d",V(-.7f,0,-12.3f),0,.85f);
            Place(edge,"crossed_dice_casino_sign_3d",V(-13.95f,2.45f,3.15f),-90,.83f);
            Reuse(edge,"TotemsBones/rune_banner_hd2d",V(-9.4f,.95f,13.85f),1.6f);
            Reuse(edge,"TotemsBones/bone_pile_hd2d",V(-12.7f,0,-10.7f),.68f);
            Reuse(edge,"Kitchen/firewood_bundle_hd2d",V(12.6f,0,2.55f),.8f);
            Transform trail=Group("06_RunnersSpilledCoinsAndCaveDebris",room);
            var g=new Geometry();
            g.Box(V(-6.1f,.068f,0),V(15.25f,.04f,2.1f),Finish.Burgundy,Quaternion.identity,.025f);
            foreach(float z in new[]{-.95f,.95f})g.Box(V(-6.1f,.094f,z),V(15f,.015f,.07f),Finish.Straw,Quaternion.identity,.005f);
            for(int i=0;i<18;i++)
            {
                Coin(g,V(-1.8f+Mathf.Sin(i*2.7f)*.5f,.035f,9.6f-i*.13f),.065f);
                if(i%3==0)g.Rock(V(13.48f,.06f,-11.5f+i*1.3f),V(.26f,.16f,.42f),Finish.StoneDark,i);
            }
            for(int i=0;i<13;i++)
            {
                float z=-11.7f+i*1.65f;
                if(Mathf.Abs(z)<2.5f)continue;
                g.Rock(V(-13.55f,.07f,z),V(.28f,.18f,.5f),Finish.StoneDark,i);
            }
            MeshObject("casino_worn_runners_and_debris",g,trail);
        }

        private static void BuildLights(Transform room)
        {
            Transform lights=Group("07_OilLanternsAndWallBraziers",room);
            foreach(float x in new[]{-11.7f,-4.5f,7.1f,12.5f})
                SetLamp(Reuse(lights,"Tavern/wall_brazier_3d",V(x,1.95f,13.9f)),3.7f,8.5f);
            foreach(float z in new[]{-10.7f,-.2f,8.2f})
                SetLamp(Reuse(lights,"Tavern/wall_brazier_3d",V(13.9f,1.9f,z),1,90),3.7f,8.5f);
            foreach(float z in new[]{-9.0f,4.0f,10.9f})
                SetLamp(Reuse(lights,"Tavern/wall_brazier_3d",V(-13.9f,1.9f,z),1,-90),3.7f,8.5f);
            SetLamp(Reuse(lights,"Tavern/oil_lantern_3d",V(-.2f,.89f,-12.7f),.8f),2.8f,7.5f);
            SetLamp(Reuse(lights,"Tavern/iron_chandelier_3d",V(0,4.7f,5.4f),.72f),5.5f,13,true);
            SetLamp(Reuse(lights,"Tavern/iron_chandelier_3d",V(0,4.7f,-6.8f),.65f),4.3f,10,true);
        }

        private static Camera BuildPresentation()
        {
            Transform presentation=Group("DemoPresentation_CamerasAndFill",null);
            Transform cameraRoot=Group("Main Camera",presentation);
            cameraRoot.gameObject.tag="MainCamera";
            Camera camera=cameraRoot.gameObject.AddComponent<Camera>();
            cameraRoot.gameObject.AddComponent<AudioListener>();
            camera.fieldOfView=34; camera.nearClipPlane=.1f; camera.farClipPlane=160;
            camera.allowHDR=true; camera.backgroundColor=new Color(.027f,.037f,.035f); camera.clearFlags=CameraClearFlags.SolidColor;
            SetPose(camera,V(0,.9f,0),50,0,61);
            var data=cameraRoot.gameObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing=true; data.requiresDepthTexture=true; data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            RenderSettings.skybox=null; RenderSettings.fog=false; RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.30f,.37f,.34f);
            RenderSettings.ambientEquatorColor=new Color(.23f,.28f,.23f);
            RenderSettings.ambientGroundColor=new Color(.13f,.12f,.085f); RenderSettings.reflectionIntensity=0;
            Transform fill=Group("CoolCaveBounce",presentation); fill.rotation=Quaternion.Euler(52,-32,0);
            Light light=fill.gameObject.AddComponent<Light>(); light.type=LightType.Directional;
            light.color=new Color(.54f,.66f,.65f); light.intensity=1.45f; light.shadows=LightShadows.Soft; light.shadowBias=.03f; light.shadowNormalBias=.13f;
            RenderSettings.sun=light;
            Transform bounce=Group("SoftFrontBounce",presentation); bounce.rotation=Quaternion.Euler(30,150,0);
            Light front=bounce.gameObject.AddComponent<Light>(); front.type=LightType.Directional;
            front.color=new Color(.66f,.56f,.37f); front.intensity=.55f;
            var volume=Group("CasinoPresentation",presentation).gameObject.AddComponent<Volume>(); volume.isGlobal=true;
            var profile=ScriptableObject.CreateInstance<VolumeProfile>(); profile.name="GoblinCasinoPresentation";
            var bloom=profile.Add<Bloom>(true); bloom.intensity.Override(.26f); bloom.threshold.Override(1.1f); bloom.scatter.Override(.6f);
            var color=profile.Add<ColorAdjustments>(true); color.postExposure.Override(.85f); color.contrast.Override(9); color.saturation.Override(-5);
            profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
            var vignette=profile.Add<Vignette>(true); vignette.intensity.Override(.14f); vignette.smoothness.Override(.6f);
            volume.sharedProfile=SaveAsset(profile,"Assets/Settings/GoblinCasinoPresentation.asset");
            foreach(VolumeComponent component in volume.sharedProfile.components)
                if(!AssetDatabase.Contains(component))AssetDatabase.AddObjectToAsset(component,volume.sharedProfile);
            EditorUtility.SetDirty(volume.sharedProfile);
            return camera;
        }

        private static void SetPose(Camera camera,Vector3 focus,float pitch,float yaw,float distance)
        {
            Quaternion rotation=Quaternion.Euler(pitch,yaw,0);
            camera.transform.SetPositionAndRotation(focus-rotation*Vector3.forward*distance,rotation);
        }

        public static void Capture()
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(scene.path!=ScenePath) scene=EditorSceneManager.OpenScene(ScenePath);
            Camera camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).Single();
            Vector3 position=camera.transform.position; Quaternion rotation=camera.transform.rotation; float fov=camera.fieldOfView;
            string output=Path.GetFullPath("Logs/CasinoPreview"); Directory.CreateDirectory(output);
            try
            {
                Render(camera,output+"/casino_overview.png",1800,1600);
                SetPose(camera,V(8.2f,1.1f,9.2f),38,0,23); camera.fieldOfView=44;
                Render(camera,output+"/casino_boss_vault.png",1600,1200);
                SetPose(camera,V(-7,.6f,7),33,0,17); camera.fieldOfView=42;
                Render(camera,output+"/casino_dice_circle.png",1600,1100);
                SetPose(camera,V(-7,1.1f,-7),12.09f,0,18.5f); camera.fieldOfView=34;
                Render(camera,output+"/casino_map_camera12.png",1600,1000);
            }
            finally { camera.transform.SetPositionAndRotation(position,rotation); camera.fieldOfView=fov; }
            Debug.Log("CASINO_CAPTURE_OK: "+output);
        }

        private static void Render(Camera camera,string path,int width,int height)
        {
            var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32){antiAliasing=1}; target.Create();
            var previous=RenderTexture.active; float aspect=camera.aspect; camera.aspect=(float)width/height;
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target; camera.Render();
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                RenderTexture.active=target; image.ReadPixels(new Rect(0,0,width,height),0,0); image.Apply();
                File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally { camera.targetTexture=null; camera.aspect=aspect; RenderTexture.active=previous; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image); }
        }
    }
}
