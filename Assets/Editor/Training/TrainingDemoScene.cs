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
    public static partial class TrainingDemoBuilder
    {
        private static void BuildShell(Transform room)
        {
            Transform shell=Group("01_StoneShellAndTimberBracing",room);
            var foundation=new Geometry();
            foundation.Box(V(0,-.36f,0),V(37.4f,.72f,29.4f),Finish.StoneDark,Quaternion.identity,.18f);
            Transform floor=MeshObject("training_floor_foundation",foundation,shell).transform;
            BoxCollision(floor,V(0,-.18f,0),V(36.5f,.36f,28.5f));
            BuildFloorTiles(shell);
            Wall(shell,"training_north_wall",V(0,0,14),36,3.05f,0);
            Wall(shell,"training_west_wall",V(-18,0,0),28,3.05f,90);
            Wall(shell,"training_east_north_wall",V(18,0,8.7f),10.6f,3.05f,90);
            Wall(shell,"training_east_south_wall",V(18,0,-7.5f),13,2.15f,90);
            Wall(shell,"training_south_cutaway_west",V(-10,0,-14),16,.48f,0);
            Wall(shell,"training_south_cutaway_east",V(10,0,-14),16,.48f,0);
            var threshold=new Geometry();
            threshold.Box(V(0,.045f,-14.35f),V(4,.11f,2),Finish.StoneLight,Quaternion.identity,.05f);
            threshold.Box(V(18.35f,.045f,2.1f),V(2,.11f,2.7f),Finish.StoneLight,Quaternion.identity,.05f);
            MeshObject("training_entrance_thresholds",threshold,shell);
            BoxCollision(shell,V(0,-.12f,-14.35f),V(4,.24f,2));
            BoxCollision(shell,V(18.35f,-.12f,2.1f),V(2,.24f,2.7f));
            // Visible cutaway walls retain full-height collision at the module boundary.
            foreach(Vector3 p in new[]{V(-18,0,-14),V(18,0,-14),V(-18,0,14),V(18,0,14),V(-9.4f,0,14),V(-.5f,0,14),V(18,0,3.4f),V(18,0,.8f),V(-2,0,-14),V(2,0,-14)})
            {
                var brace=new Geometry();
                brace.Box(p+V(0,1.70f,0),V(.37f,3.4f,.37f),Finish.OakDark,Quaternion.identity,.045f);
                foreach(float y in new[]{.28f,2.88f})brace.Box(p+V(0,y,0),V(.43f,.15f,.43f),Finish.Iron,Quaternion.identity,.022f);
                MeshObject("training_brace_"+p.x+"_"+p.z,brace,shell);
            }
            var lintel=new Geometry();
            lintel.Box(V(0,3.46f,-14),V(4.6f,.36f,.45f),Finish.OakDark,Quaternion.identity,.05f);
            lintel.Box(V(18,3.46f,2.1f),V(.45f,.36f,3.15f),Finish.OakDark,Quaternion.identity,.05f);
            MeshObject("training_gate_lintels",lintel,shell);
            Place(shell,"crossed_axes_training_sign_3d",V(0,3.15f,-14.3f));
            Transform partitions=Group("02_CellBlockAndOfficePartitions",room);
            Wall(partitions,"cellblock_east_north",V(-9.4f,0,6.5f),15,2.1f,90);
            Wall(partitions,"cellblock_east_south_cutaway",V(-9.4f,0,-9.1f),9.8f,.92f,90);
            Wall(partitions,"office_south_cutaway",V(-4.95f,0,5.0f),8.9f,.72f,0);
            Wall(partitions,"office_east_north",V(-.5f,0,11.65f),4.7f,2.1f,90);
            Wall(partitions,"office_east_south",V(-.5f,0,5.7f),1.4f,1.15f,90);
            var door=new Geometry();
            foreach(float z in new[]{6.4f,9.3f})door.Box(V(-.5f,1.45f,z),V(.26f,2.9f,.26f),Finish.OakDark,Quaternion.identity,.03f);
            door.Box(V(-.5f,2.9f,7.85f),V(.32f,.23f,3.2f),Finish.Oak,Quaternion.identity,.04f);
            for(int i=0;i<4;i++)door.Box(V(-.61f,1.8f,9.12f-i*.075f),V(.08f,2.0f,.08f),Finish.Cloth,Quaternion.identity,.022f);
            MeshObject("training_office_open_door",door,partitions);
        }

        private static void BuildFloorTiles(Transform parent)
        {
            var random=new System.Random(712);
            // Local floor chunks keep the practical lamps visible under URP's per-object light limit.
            for(int chunk=0;chunk<12;chunk++)
            {
                var tiles=new Geometry();
                for(int z=0;z<6;z++)for(int x=0;x<6;x++)
                {
                    float px=-17.2f+x*1.45f+(chunk%4)*8.7f;
                    float pz=-13.2f+z*1.48f+(chunk/4)*8.88f;
                    tiles.Box(V(px,.008f,pz),V(1.40f,.075f,1.43f),(random.Next(100)<72?Finish.Stone:random.Next(100)<55?Finish.StoneLight:Finish.StoneDark),Quaternion.Euler(0,(float)random.NextDouble()*1.2f-.6f,0),.06f);
                }
                MeshObject("training_floor_tiles_"+chunk.ToString("00"),tiles,parent);
            }
        }

        private static void Wall(Transform parent,string name,Vector3 origin,float length,float height,float yaw)
        {
            var g=new Geometry();Quaternion q=Quaternion.Euler(0,yaw,0);
            int rows=Mathf.CeilToInt(height/.58f),cols=Mathf.CeilToInt(length/1.3f);
            for(int row=0;row<rows;row++)for(int col=0;col<cols;col++)
            {
                float h=Mathf.Min(.535f,height-row*.58f),x=-length*.5f+(col+.5f)*length/cols;
                g.Box(origin+q*V(x,row*.58f+h*.5f,0),V(length/cols-.04f,h,.62f),(col+row)%4==0?Finish.StoneLight:Finish.Stone,q,.07f);
            }
            for(int i=0;i<cols;i++)
            {
                float x=-length*.5f+(i+.5f)*length/cols;
                g.Rock(origin+q*V(x,height+.11f,.04f),V(length/cols+.14f,height<1?.27f:.46f,.82f),Finish.StoneDark,i*.73f);
                if(i%4==0)g.Rock(origin+q*V(x,.18f,-.34f),V(.54f,.22f,.10f),Finish.Moss,i);
            }
            Transform wall=MeshObject(name,g,parent).transform;
            Transform collision=Group("FullHeightSolidBoundary",wall);collision.localPosition=origin;collision.localRotation=q;
            BoxCollision(collision,V(0,1.8f,0),V(length,3.6f,.66f));
        }

        private static void BuildCages(Transform room)
        {
            Transform cellblock=Group("03_ThreeIronCagesAndGuardLane",room);
            for(int i=0;i<3;i++)
            {
                Transform cell=Group("Cage_0"+(i+1),cellblock);cell.localPosition=V(-15.0f,0,8.7f-i*8.7f);
                Place(cell,"riveted_iron_cage_3d",Vector3.zero,-90);
                // Each cell has different surrounding stores; no prisoners or NPCs are placed.
                if(i==0)Reuse(cell,"Camp/straw_bed_hd2d",V(-.3f,.41f,.7f),.75f);
                if(i==1)Reuse(cell,"TotemsBones/bone_pile_hd2d",V(-.9f,.40f,.55f),.48f);
                if(i==2)Reuse(cell,"Kitchen/firewood_scattered_hd2d",V(-.5f,.4f,.55f),.60f);
                Reuse(cell,"Tavern/supply_crate_3d",V(0,0,-3.5f),.7f,i*7);
                var number=new Geometry();
                number.Box(V(2.12f,2.82f,0),V(.09f,.40f,.72f),Finish.OakDark,Quaternion.identity,.025f);
                for(int j=0;j<=i;j++)number.Box(V(2.18f,2.83f,-.2f+j*.2f),V(.02f,.23f,.035f),Finish.Bone,Quaternion.identity,.003f);
                MeshObject("training_cage_number_0"+(i+1),number,cell);
            }
            Reuse(cellblock,"Tavern/wooden_stool_3d",V(-10.85f,0,-5.2f),.8f);
            Reuse(cellblock,"Tavern/ale_barrel_3d",V(-16.45f,0,-12.3f),.68f);
            SetLamp(Reuse(cellblock,"Tavern/oil_lantern_3d",V(-10.75f,.61f,-5.1f),.60f),2.0f,5);
        }

        private static void BuildOffice(Transform room)
        {
            Transform office=Group("04_CommandersStrategyOffice",room);
            var rug=new Geometry();rug.Box(V(-5.0f,.07f,9.4f),V(7.4f,.035f,7.3f),Finish.Cloth,Quaternion.identity,.045f);
            foreach(float x in new[]{-8.55f,-1.45f})rug.Box(V(x,.096f,9.4f),V(.045f,.012f,7.0f),Finish.Straw,Quaternion.identity,.003f);
            foreach(float z in new[]{5.9f,12.9f})rug.Box(V(-5,.096f,z),V(7.15f,.012f,.045f),Finish.Straw,Quaternion.identity,.003f);
            for(int i=0;i<12;i++)rug.Box(V(-8.1f+i*.56f,.108f,6.15f),V(.11f,.013f,.15f),Finish.Bone,Quaternion.Euler(0,45,0),.003f);
            MeshObject("training_office_woven_rug",rug,office);
            Place(office,"war_strategy_table_3d",V(-6.05f,.10f,10.3f));
            Reuse(office,"Casino/boss_bone_crest_chair_3d",V(-6.05f,.10f,12.15f),.8f);
            Reuse(office,"Tavern/wooden_stool_3d",V(-6.8f,.10f,8.3f),.76f);
            Reuse(office,"Tavern/wooden_stool_3d",V(-4.8f,.10f,8.4f),.76f);
            Place(office,"chalk_tactics_board_3d",V(-3.8f,2.04f,13.60f),0,.83f);
            Reuse(office,"TotemsBones/rune_banner_hd2d",V(-7.95f,1.32f,13.63f),1.24f);
            Reuse(office,"TotemsBones/rune_shield_hd2d",V(-1.85f,1.50f,13.62f),1.0f);
            Place(office,"armour_and_axe_workbench_3d",V(-2.0f,.11f,10.6f),90,.68f);
            Reuse(office,"Tavern/supply_crate_3d",V(-8.1f,.10f,6.6f),.77f,2);
            Reuse(office,"Tavern/supply_crate_3d",V(-8.1f,.78f,6.6f),.67f,-8);
            Place(office,"stitched_sandbag_stack_3d",V(-2.1f,.1f,6.35f),0,.68f);
            SetLamp(Reuse(office,"Tavern/oil_lantern_3d",V(-7.63f,1.66f,10.97f),.44f),2.3f,5.5f);
            SetLamp(Reuse(office,"Tavern/wall_brazier_3d",V(-5.75f,1.93f,13.65f),.8f),2.8f,6);
        }

        private static void BuildTraining(Transform room)
        {
            Transform drills=Group("05_EightTrainingDummyStations",room);
            for(int i=0;i<8;i++)
            {
                Vector3 p=i<4?V(2.2f+i*4.4f,0,9.3f):V(.2f+(i-4)*4.6f,0,-7.2f);
                Transform station=Group("DummyStation_0"+(i+1),drills);station.localPosition=p;
                Place(station,"worn_dummy_training_pad_3d",Vector3.zero,0,1.13f);
                Transform dummy=Reuse(station,"Camp/training_dummy_hd2d",V(0,.064f,0),1.07f);
                dummy.name="ExistingPixelTrainingDummy";
                var settings=new SerializedObject(dummy.GetComponent<HybridProp>());settings.FindProperty("directionFrame").intValue=i<4?0:1;settings.ApplyModifiedPropertiesWithoutUndo();
                var marks=new Geometry();
                marks.Box(V(0,.06f,-2.1f),V(1.45f,.019f,.075f),Finish.Straw,Quaternion.identity,.008f);
                foreach(float x in new[]{-.6f,.6f})marks.Box(V(x,.061f,-2.26f),V(.075f,.019f,.33f),Finish.Straw,Quaternion.identity,.008f);
                MeshObject("training_station_marks_"+i,marks,station);
            }
            Transform arena=Group("06_SparringCircleAndArchery",room);
            var circle=new Geometry();
            circle.Lathe(V(6.5f,.038f,-.2f),new[]{V(4.25f,0,0),V(4.25f,.025f,0)},Finish.Sand,72);
            circle.Ring(V(6.5f,.066f,-.2f),4.3f,4.21f,.015f,Finish.Straw,72);
            circle.Ring(V(6.5f,.067f,-.2f),1.1f,1.04f,.013f,Finish.Straw,40);
            foreach(float x in new[]{3.1f,9.9f})circle.Box(V(x,.08f,-.2f),V(.12f,.013f,.75f),Finish.Straw,Quaternion.identity,.005f);
            for(int i=0;i<18;i++)
            {
                float a=i*.86f;Vector3 p=V(6.5f+Mathf.Cos(a)*2.7f,.077f,-.2f+Mathf.Sin(a)*2.7f);
                circle.Beam(p,p+V(.25f,.002f,.12f),.024f,Finish.OakDark);
            }
            MeshObject("training_scuffed_sand_sparring_circle",circle,arena);
            Place(arena,"straw_archery_target_3d",V(15.85f,0,5.0f),-17);
            Place(arena,"straw_archery_target_3d",V(15.9f,0,-2.4f),-8);
            Reuse(arena,"TotemsBones/ring_training_rack_hd2d",V(-5.65f,0,.1f),1.38f);
            Place(arena,"stitched_sandbag_stack_3d",V(-6.5f,0,2.4f));
            // Small practice obstacle in the western strip, clear of the cellblock doorway.
            var course=new Geometry();
            foreach(float z in new[]{-5.6f,-8.15f})
            {
                foreach(float x in new[]{-6.8f,-3.5f})course.Box(V(x,.65f,z),V(.12f,1.3f,.12f),Finish.OakDark,Quaternion.identity,.016f);
                course.Beam(V(-6.8f,.83f,z),V(-3.5f,.83f,z),.10f,Finish.Oak);
            }
            MeshObject("training_hurdle_course",course,arena);
        }

        private static void BuildClutter(Transform room)
        {
            Transform stores=Group("07_WeaponsRepairAndSupplyCorners",room);
            Place(stores,"spears_and_shields_rack_3d",V(5.2f,0,12.45f));
            Place(stores,"spears_and_shields_rack_3d",V(16.55f,0,-5.7f),90,.85f);
            Place(stores,"armour_and_axe_workbench_3d",V(16.65f,0,-10.45f),90);
            Reuse(stores,"TotemsBones/rune_banner_hd2d",V(10.45f,1.3f,13.62f),1.48f);
            Reuse(stores,"TotemsBones/rune_shield_hd2d",V(14.2f,1.38f,13.60f),1.15f);
            Reuse(stores,"TotemsBones/beast_totem_hd2d",V(16.7f,0,12.3f),1.15f);
            foreach(Vector3 p in new[]{V(1.1f,0,12.5f),V(2.2f,0,12.6f),V(1.1f,.83f,12.5f),V(14.8f,0,-12.3f),V(16.2f,0,-12.2f),V(16.1f,.83f,-12.2f),V(-6.9f,0,-12.25f)})
                Reuse(stores,"Tavern/supply_crate_3d",p,.88f,p.x*2);
            Place(stores,"stitched_sandbag_stack_3d",V(11.6f,0,12.35f),0,.88f);
            Place(stores,"stitched_sandbag_stack_3d",V(-4.8f,0,-12.3f),0,.88f);
            Reuse(stores,"Tavern/ale_barrel_3d",V(-7.45f,0,3.4f),.8f);
            Reuse(stores,"Kitchen/firewood_bundle_hd2d",V(-6.4f,0,3.55f),.78f);
            Reuse(stores,"Kitchen/firewood_stack_hd2d",V(12.3f,0,-12.4f),.8f);
            Transform rest=Group("08_RestWaterAndRallyPoint",room);
            Reuse(rest,"Tavern/wall_bench_3d",V(6.5f,0,-12.65f),1.07f);
            Reuse(rest,"Tavern/wall_bench_3d",V(-5.4f,0,-10.5f),.85f);
            Reuse(rest,"Kitchen/stone_cistern_narrow_3d",V(9.7f,0,-12.4f),.75f);
            Reuse(rest,"Tavern/ale_barrel_3d",V(10.5f,0,-10.5f),.63f);
            Reuse(rest,"Tavern/wooden_stool_3d",V(4.2f,0,-11.2f),.76f);
            Reuse(rest,"TotemsBones/bone_pile_hd2d",V(-7.7f,0,-9.6f),.54f);
            Place(rest,"crossed_axes_training_sign_3d",V(-6.6f,2.2f,4.59f),0,.75f);
            var debris=new Geometry();
            for(int i=0;i<30;i++)
            {
                float x=i%2==0?17.3f:-8.55f,z=-12.7f+(i/2)*1.8f;
                if(x<0&&z>-4.5f&&z<-.9f)continue;
                debris.Rock(V(x,.06f,z),V(.19f+(i%3)*.11f,.16f,.32f),Finish.StoneDark,i);
                if(i%3==0)debris.Beam(V(x-.3f,.075f,z),V(x+.1f,.075f,z+.13f),.017f,Finish.Straw);
            }
            MeshObject("training_edge_rubble_and_straw",debris,stores);
        }

        private static void BuildLights(Transform room)
        {
            Transform lights=Group("09_PracticalFireAndOilLighting",room);
            foreach(float x in new[]{2.5f,8.2f,15.3f})SetLamp(Reuse(lights,"Tavern/wall_brazier_3d",V(x,1.95f,13.65f),.90f),3.3f,7.4f);
            foreach(float z in new[]{-11.8f,-4.6f,5.2f,11.5f})SetLamp(Reuse(lights,"Tavern/wall_brazier_3d",V(-17.65f,1.95f,z),.88f,-90),2.8f,6.3f);
            foreach(float z in new[]{-10,7.3f})SetLamp(Reuse(lights,"Tavern/wall_brazier_3d",V(17.65f,1.95f,z),.90f,90),3.3f,7.4f);
            SetLamp(Reuse(lights,"Lighting/iron_bowl_fire_hd2d",V(-1.7f,0,2.9f),1.35f),3.5f,7.2f);
            SetLamp(Reuse(lights,"Lighting/timber_brazier_hd2d",V(12.6f,0,3.1f),1.12f),3.7f,7.3f);
            SetLamp(Reuse(lights,"Lighting/bone_fire_stand_hd2d",V(-3.0f,0,-11.8f),1.28f),3.2f,7.0f);
            SetLamp(Reuse(lights,"Tavern/oil_lantern_3d",V(6.5f,.89f,-12.65f),.67f),2.5f,6.5f);
            SetLamp(Reuse(lights,"Tavern/oil_lantern_3d",V(16.7f,1.15f,-9.5f),.51f),2.3f,5.5f);
            // Only two broad fire pools cast shadows; wall lights provide smaller warm pools.
            SetLamp(Reuse(lights,"Tavern/iron_chandelier_3d",V(5.9f,4.4f,2.7f),.68f),4.8f,11.5f,true);
            SetLamp(Reuse(lights,"Tavern/iron_chandelier_3d",V(6.5f,4.4f,-7.8f),.55f),3.8f,9.4f,true);
        }

        private static Camera BuildPresentation()
        {
            Transform presentation=Group("DemoPresentation_CamerasAndFill",null);
            Transform cameraRoot=Group("Main Camera",presentation);cameraRoot.gameObject.tag="MainCamera";
            Camera camera=cameraRoot.gameObject.AddComponent<Camera>();cameraRoot.gameObject.AddComponent<AudioListener>();
            camera.fieldOfView=34;camera.nearClipPlane=.1f;camera.farClipPlane=180;camera.allowHDR=true;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.023f,.030f,.029f);
            SetPose(camera,V(0,.7f,0),52,0,69);
            var data=cameraRoot.gameObject.AddComponent<UniversalAdditionalCameraData>();data.renderPostProcessing=true;data.requiresDepthTexture=true;data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            RenderSettings.skybox=null;RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.36f,.42f,.39f);RenderSettings.ambientEquatorColor=new Color(.29f,.34f,.30f);RenderSettings.ambientGroundColor=new Color(.17f,.15f,.10f);RenderSettings.reflectionIntensity=0;
            Transform fill=Group("CoolSubterraneanBounce",presentation);fill.rotation=Quaternion.Euler(51,-32,0);
            Light light=fill.gameObject.AddComponent<Light>();light.type=LightType.Directional;light.color=new Color(.50f,.63f,.61f);light.intensity=1.65f;light.shadows=LightShadows.Soft;light.shadowBias=.025f;light.shadowNormalBias=.13f;RenderSettings.sun=light;
            Transform front=Group("SoftFireBounce",presentation);front.rotation=Quaternion.Euler(32,152,0);
            Light bounce=front.gameObject.AddComponent<Light>();bounce.type=LightType.Directional;bounce.color=new Color(.66f,.54f,.35f);bounce.intensity=.60f;
            var volume=Group("TrainingPresentation",presentation).gameObject.AddComponent<Volume>();volume.isGlobal=true;
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.name="GoblinTrainingPresentation";
            var bloom=profile.Add<Bloom>(true);bloom.intensity.Override(.26f);bloom.threshold.Override(1.1f);bloom.scatter.Override(.6f);
            var color=profile.Add<ColorAdjustments>(true);color.postExposure.Override(.90f);color.contrast.Override(9);color.saturation.Override(-5);
            profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
            var vignette=profile.Add<Vignette>(true);vignette.intensity.Override(.13f);vignette.smoothness.Override(.6f);
            volume.sharedProfile=SaveAsset(profile,"Assets/Settings/GoblinTrainingPresentation.asset");
            foreach(VolumeComponent component in volume.sharedProfile.components)if(!AssetDatabase.Contains(component))AssetDatabase.AddObjectToAsset(component,volume.sharedProfile);
            EditorUtility.SetDirty(volume.sharedProfile);return camera;
        }

        private static void SetPose(Camera camera,Vector3 focus,float pitch,float yaw,float distance)
        {
            Quaternion rotation=Quaternion.Euler(pitch,yaw,0);camera.transform.SetPositionAndRotation(focus-rotation*Vector3.forward*distance,rotation);
        }

        // One explicit authoring pass on the saved demo; never invoked at runtime or scene load.
        public static void ApplyFinalAppearance()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
            palette=System.Enum.GetValues(typeof(Finish)).Cast<Finish>().Select(f=>AssetDatabase.LoadAssetAtPath<Material>(Materials+"/training_"+f.ToString().ToLowerInvariant()+".mat")).ToArray();
            string roomPath=Prefabs+"/goblin_training_room.prefab";
            GameObject contents=PrefabUtility.LoadPrefabContents(roomPath);
            try
            {
                Transform shell=contents.transform.Find("01_StoneShellAndTimberBracing");
                foreach(Transform child in shell.Cast<Transform>().Where(t=>t.name.StartsWith("training_floor_tiles_")).ToArray())Object.DestroyImmediate(child.gameObject);
                BuildFloorTiles(shell);
                foreach(Light light in contents.GetComponentsInChildren<Light>())
                {
                    light.intensity*=1.18f;
                    var flicker=light.GetComponent<HybridFireLight>();
                    if(flicker!=null){var settings=new SerializedObject(flicker);settings.FindProperty("baseIntensity").floatValue=light.intensity;settings.ApplyModifiedPropertiesWithoutUndo();}
                }
                PrefabUtility.SaveAsPrefabAsset(contents,roomPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(contents);}
            Material sand=palette[(int)Finish.Sand];sand.SetColor("_BaseColor",new Color(.42f,.39f,.25f));sand.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Textures+"/training_stone_grain.png"));EditorUtility.SetDirty(sand);
            EditorSceneManager.OpenScene(ScenePath);
            GameObject presentation=EditorSceneManager.GetActiveScene().GetRootGameObjects().Single(g=>g.name=="DemoPresentation_CamerasAndFill");
            presentation.transform.Find("CoolSubterraneanBounce").GetComponent<Light>().intensity=1.65f;
            presentation.transform.Find("SoftFireBounce").GetComponent<Light>().intensity=.60f;
            RenderSettings.ambientSkyColor=new Color(.36f,.42f,.39f);RenderSettings.ambientEquatorColor=new Color(.29f,.34f,.30f);RenderSettings.ambientGroundColor=new Color(.17f,.15f,.10f);
            var profile=presentation.GetComponentInChildren<Volume>().sharedProfile;
            if(profile.TryGet<ColorAdjustments>(out var color))color.postExposure.Override(.90f);
            EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            // Reload verifies that assets and placements survive serialization before final previews.
            EditorSceneManager.OpenScene(ScenePath);Capture();Debug.Log("TRAINING_FINAL_APPEARANCE_OK");
        }

        public static void Capture()
        {
            var scene=EditorSceneManager.GetActiveScene();if(scene.path!=ScenePath)scene=EditorSceneManager.OpenScene(ScenePath);
            Camera camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).Single();
            Vector3 position=camera.transform.position;Quaternion rotation=camera.transform.rotation;float fov=camera.fieldOfView;
            string output=Path.GetFullPath("Logs/TrainingPreview");Directory.CreateDirectory(output);
            try
            {
                Render(camera,output+"/training_overview.png",1900,1500);
                SetPose(camera,V(-13.7f,1.25f,6.9f),33,-35,20);camera.fieldOfView=43;
                Render(camera,output+"/training_iron_cages.png",1600,1200);
                SetPose(camera,V(-5.3f,1.0f,10.0f),40,-12,17);camera.fieldOfView=43;
                Render(camera,output+"/training_strategy_office.png",1600,1200);
                // The latest large map uses this fixed low-pitch view. Show the module at that pitch too.
                SetPose(camera,V(6.5f,1.3f,1.1f),12.09f,0,24.6f);camera.fieldOfView=34;
                Render(camera,output+"/training_map_camera12.png",1600,1000);
            }
            finally{camera.transform.SetPositionAndRotation(position,rotation);camera.fieldOfView=fov;}
            Debug.Log("TRAINING_CAPTURE_OK: "+output);
        }

        private static void Render(Camera camera,string path,int width,int height)
        {
            var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32){antiAliasing=1};target.Create();
            var previous=RenderTexture.active;float aspect=camera.aspect;camera.aspect=(float)width/height;
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target;camera.Render();
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                RenderTexture.active=target;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally{camera.targetTexture=null;camera.aspect=aspect;RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);}
        }
    }
}
