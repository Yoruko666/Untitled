using System;
using System.Linq;
using UnityEngine;

namespace Untitled.Editor
{
    public static partial class RitualHallDemoBuilder
    {
        private static void BuildShell(Transform room)
        {
            Transform shell=Group("01_SquareCaveShell_CutawayWalls",room);
            // Continuous solid floor except the actual stairwell: x 2.8..4.6, z 5..7.8.
            Solid("ritual_floor_west",shell,V(-5.10f,-.29f,0),V(15.80f,.58f,26),Finish.StoneDark);
            Solid("ritual_floor_east",shell,V(8.80f,-.29f,0),V(8.40f,.58f,26),Finish.StoneDark);
            Solid("ritual_floor_middle_south",shell,V(3.70f,-.29f,-4),V(1.80f,.58f,18),Finish.StoneDark);
            Solid("ritual_floor_middle_north",shell,V(3.70f,-.29f,10.40f),V(1.80f,.58f,5.20f),Finish.StoneDark);
            for(int batch=0;batch<4;batch++)
            {
                var tiles=new Geometry();
                for(int row=batch*6;row<(batch+1)*6;row++)for(int col=0;col<24;col++)
                {
                    float x=-12.42f+col*1.08f,z=-12.42f+row*1.08f;
                    if(x+.53f>2.8f&&x-.53f<4.6f&&z+.53f>5&&z-.53f<7.8f)continue;
                    float wobble=.009f*Mathf.Sin(col*7.1f+row*3.4f);
                    tiles.Box(V(x,.024f+wobble,z),V(1.038f,.055f,1.038f),(col+row*3)%7==0?Finish.StoneDark:(col*2+row)%6==0?Finish.StoneLight:Finish.Stone,Q,.025f);
                }
                MeshObject("ritual_worn_floor_tiles_"+batch,tiles,shell);
            }
            Solid("ritual_north_back_wall",shell,V(0,2.35f,13.15f),V(27.10f,4.70f,.85f),Finish.StoneDark);
            Solid("ritual_west_upper_wall",shell,V(-13.15f,2.12f,3.10f),V(.85f,4.24f,20.20f),Finish.StoneDark);
            Solid("ritual_west_entry_lower_jamb",shell,V(-13.15f,.65f,-11.20f),V(.85f,1.30f,3.60f),Finish.StoneDark);
            // Foreground/east display walls stay low so the project's 12-degree camera can read the room.
            Solid("ritual_east_cutaway_wall",shell,V(13.15f,.62f,0),V(.85f,1.24f,26.80f),Finish.StoneDark);
            Solid("ritual_south_west_cutaway",shell,V(-7.55f,.43f,-13.15f),V(11.70f,.86f,.85f),Finish.StoneDark);
            Solid("ritual_south_east_cutaway",shell,V(7.55f,.43f,-13.15f),V(11.70f,.86f,.85f),Finish.StoneDark);
            var joints=new Geometry();
            for(int i=0;i<27;i++)for(int row=0;row<4;row++)
            {
                float x=-12.75f+i*.98f+(row%2)*.28f;
                joints.Box(V(x,.60f+row*1.12f,12.69f),V(.026f,1.02f,.018f),Finish.Stone,Q,.003f);
            }
            for(int row=0;row<4;row++)joints.Box(V(0,.08f+row*1.12f,12.69f),V(26.50f,.030f,.022f),Finish.Stone,Q,.004f);
            MeshObject("ritual_back_wall_masonry_joints",joints,shell);
            var rocks=new Geometry();
            for(int i=0;i<20;i++)
            {
                float x=-12.7f+i*1.34f;
                rocks.Rock(V(x,4.30f,13.06f),V(1.70f,1.00f,1.16f),i%3==0?Finish.StoneLight:Finish.Stone,i*1.7f);
            }
            for(int i=0;i<13;i++)rocks.Rock(V(-13.12f,3.9f,-4.4f+i*1.36f),V(1.10f,.97f,1.62f),Finish.Stone,i);
            MeshObject("ritual_cave_shoulder_rocks",rocks,shell);
            Transform architecture=Group("02_RuneColumnsAndBackBanners",room);
            foreach(float x in new[] {-10.72f,10.72f})foreach(float z in new[] {-8.35f,-.20f,8.0f})
                Place(architecture,"rune_carved_ritual_column_3d",V(x,.045f,z),x<0?20:-20);
            foreach(float x in new[] {-8.2f,8.2f})Place(architecture,"ancestor_sigil_hide_banner_3d",V(x,.22f,12.37f));
            Solid("ritual_south_entry_threshold",shell,V(0,.085f,-12.90f),V(3.35f,.16f,.84f),Finish.StoneLight);
            Solid("ritual_west_entry_threshold",shell,V(-12.90f,.085f,-8.40f),V(.84f,.16f,2.60f),Finish.StoneLight);
        }

        private static void BuildSanctuary(Transform room)
        {
            Transform sanctuary=Group("03_NorthAltar_AncestorIdol",room);
            Solid("ritual_altar_dais",sanctuary,V(0,.36f,8.77f),V(5.18f,.72f,4.78f),Finish.StoneDark);
            Solid("ritual_dais_top_slab",sanctuary,V(0,.75f,8.77f),V(5.28f,.06f,4.78f),Finish.StoneLight);
            for(int i=0;i<4;i++)
            {
                float height=.18f*(i+1),z=5.20f+i*.36f;
                Solid("ritual_altar_step_"+i,sanctuary,V(0,height*.5f,z),V(2.72f,height,.40f),Finish.StoneLight);
            }
            Place(sanctuary,"carved_bone_sacrificial_altar_3d",V(0,.78f,8.05f));
            Place(sanctuary,"horned_ancestor_goblin_idol_3d",V(0,.78f,10.53f));
            Reuse(sanctuary,"ElderRoom/elder_bone_sceptre_3d",V(-2.0f,.79f,10.02f),.9f,16);
            Place(sanctuary,"brass_incense_and_ash_bowl_3d",V(-2.0f,.79f,6.87f));
            Fire(sanctuary,"AltarCandleWarmth",V(0,2.42f,7.6f),1.9f,6.0f,new Color(1,.54f,.20f));
            Fire(sanctuary,"JadeRelicMutedGlow",V(0,3.22f,10.1f),.60f,4.0f,new Color(.28f,.64f,.42f));
            Transform offerings=Group("04_UShapedOfferings_EnclosingAltar",room);
            string[] gifts={"mushroom_and_roots_offering_3d","skull_and_candles_offering_3d","jade_and_coins_offering_3d"};
            int n=0;
            foreach(float x in new[] {-5.54f,5.54f})foreach(float z in new[] {5.10f,7.54f,9.98f})
            {
                float yaw=x<0?90:-90;
                Place(offerings,"carved_offering_table_module_3d",V(x,.045f,z),yaw);
                Place(offerings,gifts[n++%3],V(x,1.188f,z),yaw);
            }
            foreach(float x in new[] {-4.28f,-1.98f,1.98f,4.28f})
            {
                Place(offerings,"carved_offering_table_module_3d",V(x,.045f,3.0f));
                Place(offerings,gifts[n++%3],V(x,1.188f,3.0f));
            }
            // Low stone curb links individual tables into the U from the reference.
            Solid("ritual_u_left_plinth",offerings,V(-5.54f,.055f,7.54f),V(1.60f,.10f,7.60f),Finish.StoneDark);
            Solid("ritual_u_right_plinth",offerings,V(5.54f,.055f,7.54f),V(1.60f,.10f,7.60f),Finish.StoneDark);
            foreach(float x in new[] {-3.27f,3.27f})Solid("ritual_u_front_plinth_"+(x<0?"west":"east"),offerings,V(x,.055f,3),V(4.50f,.10f,1.60f),Finish.StoneDark);
        }

        private static void BuildSecretPassage(Transform room)
        {
            Transform passage=Group("05_SecretStairwell_BetweenAltarAndOfferings",room);
            // Open stair entrance is an actual floor opening, with eight descending treads and a raised stone cover.
            foreach(float x in new[] {2.69f,4.71f})Solid("ritual_hatch_side_curb_"+(x<3?"west":"east"),passage,V(x,.09f,6.4f),V(.20f,.18f,3.02f),Finish.StoneLight);
            Solid("ritual_hatch_north_curb",passage,V(3.70f,.09f,7.91f),V(2.22f,.18f,.20f),Finish.StoneLight);
            Solid("ritual_hatch_south_threshold",passage,V(3.70f,.065f,4.90f),V(1.80f,.13f,.20f),Finish.StoneLight);
            for(int i=0;i<8;i++)
            {
                float top=.005f-(i+1)*.29f,z=5.16f+i*.333f;
                Solid("ritual_secret_descending_tread_"+i,passage,V(3.70f,top-.13f,z),V(1.76f,.30f,.35f),Finish.Stone);
            }
            Solid("ritual_stairwell_west_lining",passage,V(2.71f,-1.23f,6.44f),V(.18f,2.40f,2.98f),Finish.StoneDark);
            Solid("ritual_stairwell_east_lining",passage,V(4.69f,-1.23f,6.0f),V(.18f,2.40f,2.04f),Finish.StoneDark);
            var lid=new Geometry();
            lid.Box(V(0,0,0),V(1.78f,.17f,2.66f),Finish.StoneDark,Q,.06f);
            lid.Box(V(0,.095f,0),V(1.58f,.035f,2.43f),Finish.Stone,Q,.025f);
            foreach(float x in new[] {-.62f,.62f})lid.Box(V(x,.12f,0),V(.08f,.025f,2.37f),Finish.Brass,Q,.009f);
            lid.Ring(V(0,.14f,-.93f),.12f,.08f,.036f,Finish.Iron,16,Quaternion.Euler(90,0,0));
            Transform cover=MeshObject("ritual_half_open_secret_stone_cover",lid,passage).transform;
            cover.localPosition=V(3.70f,1.04f,8.10f); cover.localRotation=Quaternion.Euler(-67,0,0);
            // Covered under-floor passage runs east; ends are a future connection to the elder's private room.
            Solid("ritual_secret_eastbound_corridor_floor",passage,V(8.75f,-2.43f,7.38f),V(10.25f,.20f,1.68f),Finish.StoneDark);
            foreach(float z in new[] {6.47f,8.29f})Solid("ritual_secret_corridor_wall_"+(z<7?"south":"north"),passage,V(9.44f,-1.44f,z),V(9.08f,2.10f,.18f),Finish.StoneDark);
            Solid("ritual_secret_demo_end",passage,V(14.04f,-1.44f,7.38f),V(.18f,2.10f,1.96f),Finish.StoneDark);
            Group("FutureConnection_ToElderRoom_UnderfloorEast",passage).localPosition=V(13.85f,-2.33f,7.38f);
            Lamp(passage,"Tavern/oil_lantern_3d",V(4.0f,-1.45f,7.55f),.75f,3.8f,.64f);
            Fire(passage,"DimUnderfloorAmber",V(6.1f,-1.20f,7.38f),.52f,3.2f,new Color(.91f,.45f,.16f));
            var scuffs=new Geometry();
            foreach(float x in new[] {2.83f,4.57f})scuffs.Box(V(x,.07f,4.48f),V(.025f,.012f,.69f),Finish.Ash,Q,.002f);
            scuffs.Rock(V(4.69f,.09f,4.66f),V(.19f,.12f,.27f),Finish.StoneLight,1);
            MeshObject("ritual_secret_cover_slide_scuffs",scuffs,passage);
        }

        private static void BuildProcession(Transform room)
        {
            Transform ritual=Group("06_ElderKneelingAndProcessionAxis",room);
            Solid("ritual_elder_grey_kneeling_slab",ritual,V(0,.085f,.36f),V(3.20f,.16f,2.16f),Finish.StoneDark);
            Place(ritual,"elder_grey_woven_kneeling_mat_3d",V(0,.168f,.36f));
            Reuse(ritual,"ElderRoom/elder_meditation_cushion_3d",V(0,.295f,.44f),.74f);
            Place(ritual,"brass_incense_and_ash_bowl_3d",V(0,.046f,1.90f),0,.66f);
            var runner=new Geometry();
            for(int i=0;i<5;i++)
            {
                float z=-10.88f+i*1.72f;
                runner.Box(V(0,.067f,z),V(2.44f,.035f,1.67f),Finish.Burgundy,Q,.013f);
                foreach(float x in new[] {-1.04f,1.04f})runner.Box(V(x,.089f,z),V(.052f,.009f,1.64f),Finish.Brass,Q,.003f);
                // Repeated angular linework on the worn central runner.
                foreach(float side in new[] {-1f,1f})
                {
                    runner.Beam(V(side*.72f,.089f,z-.23f),V(0,.089f,z+.14f),.037f,Finish.Hide);
                    runner.Beam(V(0,.089f,z+.14f),V(side*.72f,.089f,z+.43f),.037f,Finish.Hide);
                }
            }
            MeshObject("ritual_worn_procession_runner",runner,ritual);
            Transform torches=Group("07_TenTorches_TwoRowsOfFive",room);
            int index=0;
            foreach(float x in new[] {-6.85f,6.85f})foreach(float z in new[] {-9.40f,-4.80f,-.20f,4.40f,9.0f})
            {
                Transform torch=Place(torches,"bone_bound_procession_torch_3d",V(x,.045f,z));
                torch.name=(x<0?"West":"East")+"ProcessionTorch_"+(index++%5+1);
                Fire(torch,"WarmTorchFlicker",V(0,2.17f,0),2.45f,6.1f,new Color(1,.50f,.14f));
                var ash=new Geometry(); ash.Lathe(V(x,.060f,z),new[] {V(.69f,0,0),V(.69f,.008f,0)},Finish.Ash,20);
                MeshObject("ritual_torch_ash_patch_"+index,ash,torches);
            }
        }

        private static void BuildSideRituals(Transform room)
        {
            Transform sides=Group("08_DrumsBoneChimesAndWitnessMats",room);
            foreach(float x in new[] {-9.1f,9.1f})
            {
                Place(sides,"rope_laced_ceremonial_drum_3d",V(x,.045f,-4.15f),x<0?16:-16);
                Place(sides,"rope_laced_ceremonial_drum_3d",V(x,.045f,-5.74f),x<0?-10:10,.70f);
                Place(sides,"hanging_ancestor_bone_chimes_3d",V(x,.045f,3.82f),x<0?35:-35);
                for(int i=0;i<3;i++)Place(sides,"elder_grey_woven_kneeling_mat_3d",V(x,.045f,-1.20f+i*1.72f),x<0?12:-12,.69f);
                Reuse(sides,"TotemsBones/bone_pile_hd2d",V(x,.06f,5.97f),1.1f);
            }
            Transform stores=Group("09_OfferingsStorageAndRitualTools",room);
            foreach(float side in new[] {-1f,1f})
            {
                float x=side*8.47f;
                Reuse(stores,"Tavern/supply_crate_3d",V(x,.045f,10.02f),.84f,side*13);
                Reuse(stores,"Tavern/supply_crate_3d",V(x+.48f*side,.045f,11.31f),.76f,-side*8);
                Reuse(stores,"Tavern/supply_crate_3d",V(x,.88f,10.14f),.65f,side*7);
                Reuse(stores,"Kitchen/open_grain_sack_3d",V(side*9.70f,.045f,10.10f),.90f,side*20);
                Reuse(stores,"Kitchen/roots_and_mushrooms_tub_3d",V(side*8.18f,.045f,8.30f),.80f);
                Reuse(stores,"Kitchen/lidded_spice_crock_3d",V(side*9.33f,.045f,8.61f),1.1f);
                Lamp(stores,"Tavern/oil_lantern_3d",V(x+.50f*side,1.63f,11.31f),1.25f,4.2f,.80f);
            }
            Reuse(stores,"ElderRoom/carved_bedside_chest_3d",V(-8.8f,.045f,-10.88f),.86f,12);
            Reuse(stores,"ElderRoom/herbal_tincture_bottles_3d",V(-8.7f,1.12f,-10.88f),.75f);
            Reuse(stores,"ElderRoom/books_scrolls_and_ink_3d",V(-10.4f,.05f,-11.4f),.70f,10);
            Reuse(stores,"Tavern/ale_barrel_3d",V(9.38f,.045f,-10.7f),.78f);
            Reuse(stores,"Kitchen/firewood_bundle_hd2d",V(8.06f,.055f,-11.7f),1.15f);
            Lamp(stores,"Tavern/oil_lantern_3d",V(-8.88f,1.10f,-10.70f),1.15f,4.8f,.76f);
            Lamp(stores,"Tavern/oil_lantern_3d",V(9.35f,1.13f,-10.68f),1.15f,4.8f,.76f);
            Transform entry=Group("10_CaveEntrancesAndRitualThreshold",room);
            foreach(float x in new[] {-2.25f,2.25f})
            {
                Place(entry,"rune_carved_ritual_column_3d",V(x,.045f,-12.48f),0,.62f);
                Place(entry,"skull_and_candles_offering_3d",V(x,2.75f,-12.48f),0,.55f);
            }
            Lamp(entry,"Tavern/wall_brazier_3d",V(-12.43f,2.10f,-5.88f),1.40f,5.0f,.93f,90);
            Lamp(entry,"Tavern/wall_brazier_3d",V(-12.43f,1.42f,-10.03f),1.40f,5.0f,.85f,90);
        }

        private static void BuildWear(Transform room)
        {
            Transform wear=Group("11_WallRubbleMossAndRitualUse",room);
            var debris=new Geometry();
            for(int i=0;i<120;i++)
            {
                bool north=i%3==0;
                float run=-11.7f+(i%39)*.60f;
                float edge=11.60f+.72f*Mathf.Sin(i*2.35f);
                Vector3 p=north?V(run,.07f,edge):V(i%2==0?edge:-edge,.07f,run);
                float s=.13f+.20f*(.5f+.5f*Mathf.Sin(i*1.67f));
                debris.Rock(p,V(s,s*.7f,s*1.5f),i%4==0?Finish.Moss:i%3==0?Finish.StoneLight:Finish.StoneDark,i);
                if(i%5==0)debris.Lathe(p+V(.28f,-.012f,.15f),new[] {V(.34f,0,0),V(.34f,.007f,0)},Finish.Moss,9);
            }
            for(int i=0;i<20;i++)
            {
                float x=-2.10f+i*.22f;
                debris.Box(V(x,.057f,4.24f),V(.031f,.014f,.12f+(i%3)*.08f),Finish.Ash,Quaternion.Euler(0,i*21,0),.003f);
            }
            // Wax pools are small and confined to the sanctuary; no clutter across the main route.
            foreach(float x in new[] {-4.9f,4.9f})for(int i=0;i<5;i++)debris.Rock(V(x+.18f*Mathf.Sin(i),.075f,3.74f+i*.58f),V(.16f,.035f,.25f),Finish.Wax,i);
            MeshObject("ritual_edge_rubble_moss_wax_and_scuffs",debris,wear);
        }
    }
}
