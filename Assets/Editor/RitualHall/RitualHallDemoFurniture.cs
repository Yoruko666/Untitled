using UnityEngine;

namespace Untitled.Editor
{
    public static partial class RitualHallDemoBuilder
    {
        private static void PrepareFurniture()
        {
            BuildAltar(); BuildIdol(); BuildOfferingTable(); BuildOfferings(); BuildTorch();
            BuildColumn(); BuildBanner(); BuildDrum(); BuildIncense(); BuildKneelingMat(); BuildBoneChimes();
        }

        private static void Skull(Geometry g, Vector3 p, float s)
        {
            g.Rock(p + V(0,.27f,0)*s, V(.68f,.53f,.49f)*s, Finish.Bone, 2);
            foreach (float x in new[] { -.17f,.17f })
                g.Rock(p + V(x,.30f,-.231f)*s, V(.17f,.18f,.064f)*s, Finish.Charcoal, x);
            g.Rock(p + V(0,.16f,-.262f)*s, V(.09f,.11f,.035f)*s, Finish.Charcoal, 3);
            for (int i=0;i<5;i++) g.Box(p + V(-.15f+i*.075f,.05f,-.21f)*s, V(.054f,.12f,.082f)*s, Finish.Bone,Q,.009f);
            foreach (float x in new[] { -.29f,.29f })
                g.Lathe(p + V(x,.42f,.025f)*s, new[] { V(.083f*s,0,0),V(.063f*s,.23f*s,0),V(0,.49f*s,0) },Finish.Bone,12,Quaternion.Euler(0,0,x<0?35:-35));
        }

        private static void Candle(Geometry g, Vector3 p, float height)
        {
            g.Lathe(p,new[] { V(.15f,0,0),V(.16f,.05f,0),V(.07f,.10f,0) },Finish.Brass,16);
            g.Lathe(p+V(0,.09f,0),new[] { V(.075f,0,0),V(.076f,height,0) },Finish.Wax,14);
            for(int i=0;i<4;i++) g.Rock(p+V(Mathf.Sin(i*1.7f)*.07f,height*(.65f+i*.06f),Mathf.Cos(i*1.7f)*.07f),V(.033f,.13f,.033f),Finish.Wax,i);
            g.Lathe(p+V(0,height+.09f,0),new[] { V(.025f,0,0),V(.051f,.07f,0),V(0,.21f,0) },Finish.Flame,10);
        }

        private static void Bowl(Geometry g, Vector3 p, float s, Finish finish)
        {
            g.Lathe(p,new[] { V(.22f*s,0,0),V(.34f*s,.13f*s,0),V(.44f*s,.31f*s,0),V(.41f*s,.32f*s,0),V(.28f*s,.12f*s,0),V(0,.075f*s,0) },finish,20);
            g.Ring(p+V(0,.30f*s,0),.455f*s,.40f*s,.045f*s,Finish.Brass,20);
        }

        private static void Jar(Geometry g,Vector3 p,float s)
        {
            g.Lathe(p,new[] { V(.24f*s,0,0),V(.34f*s,.13f*s,0),V(.35f*s,.44f*s,0),V(.19f*s,.65f*s,0),V(.20f*s,.72f*s,0),V(.16f*s,.72f*s,0),V(.15f*s,.63f*s,0) },Finish.Clay,20);
            g.Ring(p+V(0,.36f*s,0),.355f*s,.335f*s,.085f*s,Finish.Rope,20);
            g.Ring(p+V(0,.67f*s,0),.207f*s,.187f*s,.08f*s,Finish.Brass,20);
        }

        private static void Rune(Geometry g,Vector3 p,float s,Finish finish)
        {
            // Angular goblin mark, assembled as inset strips rather than printed text.
            g.Beam(p+V(-.18f,.23f,0)*s,p+V(0,0,0)*s,.037f*s,finish);
            g.Beam(p+V(0,0,0)*s,p+V(.18f,.23f,0)*s,.037f*s,finish);
            g.Beam(p+V(0,-.22f,0)*s,p+V(0,.28f,0)*s,.042f*s,finish);
            g.Beam(p+V(-.18f,-.16f,0)*s,p+V(.18f,-.16f,0)*s,.037f*s,finish);
        }

        private static void BuildAltar()
        {
            var g=new Geometry();
            g.Box(V(0,.13f,0),V(4.60f,.26f,2.15f),Finish.StoneDark,Q,.11f);
            foreach(float x in new[] {-1.65f,1.65f})
            {
                g.Box(V(x,.65f,0),V(.73f,1.05f,1.60f),Finish.Stone,Q,.10f);
                g.Box(V(x,.74f,-.83f),V(.46f,.68f,.065f),Finish.Obsidian,Q,.055f);
                Rune(g,V(x,.75f,-.876f),1.05f,Finish.Brass);
            }
            g.Box(V(0,1.19f,0),V(4.85f,.30f,2.28f),Finish.StoneLight,Q,.11f);
            g.Box(V(0,1.37f,0),V(4.53f,.09f,2.00f),Finish.Obsidian,Q,.045f);
            for(int i=0;i<13;i++)
            {
                float x=-2.02f+i*.337f;
                g.Box(V(x,1.18f,-1.15f),V(.15f,.20f,.08f),Finish.Bone,Quaternion.Euler(0,0,i%2==0?13:-13),.023f);
            }
            g.Box(V(0,1.426f,-.40f),V(1.66f,.02f,1.00f),Finish.Burgundy,Q,.005f);
            g.Box(V(0,1.10f,-1.17f),V(1.65f,.66f,.025f),Finish.Burgundy,Q,.006f);
            Rune(g,V(0,1.12f,-1.20f),1.1f,Finish.Gold);
            Skull(g,V(-1.54f,1.43f,.04f),.87f); Skull(g,V(1.54f,1.43f,.04f),.87f);
            Bowl(g,V(0,1.44f,-.12f),1.20f,Finish.Brass);
            foreach(float x in new[] {-2.02f,2.02f}) {Candle(g,V(x,1.43f,.68f),.47f);Candle(g,V(x,1.43f,-.71f),.32f);}
            SaveFurniture("carved_bone_sacrificial_altar_3d",g,V(0,.72f,0),V(4.85f,1.44f,2.28f));
        }

        private static void BuildIdol()
        {
            var g=new Geometry();
            g.Box(V(0,.14f,0),V(2.65f,.28f,1.70f),Finish.StoneDark,Q,.12f);
            g.Box(V(0,.32f,0),V(2.33f,.12f,1.48f),Finish.Brass,Q,.05f);
            g.Lathe(V(0,.36f,0),new[] {V(.79f,0,0),V(.66f,1.05f,0),V(.94f,1.78f,0),V(.73f,2.06f,0)},Finish.Obsidian,10);
            g.Rock(V(0,2.38f,-.08f),V(1.96f,1.55f,1.11f),Finish.Stone,7);
            foreach(float side in new[] {-1f,1f})
            {
                g.Rock(V(side*1.02f,2.53f,-.05f),V(1.14f,.69f,.33f),Finish.Stone,side);
                g.Box(V(side*.45f,2.48f,-.63f),V(.43f,.19f,.072f),Finish.Jade,Quaternion.Euler(0,0,side*19),.028f);
                g.Beam(V(side*.74f,2.65f,-.61f),V(side*.16f,2.62f,-.65f),.13f,Finish.Obsidian);
                g.Lathe(V(side*.68f,2.93f,.05f),new[] {V(.16f,0,0),V(.13f,.43f,0),V(.071f,.78f,0),V(0,1.14f,0)},Finish.Bone,14,Quaternion.Euler(-12,0,-side*27));
                g.Lathe(V(side*.53f,1.94f,-.52f),new[] {V(.12f,0,0),V(.09f,.30f,0),V(0,.65f,0)},Finish.Bone,12,Quaternion.Euler(-22,0,side*13));
                g.Beam(V(side*.72f,1.53f,-.11f),V(side*1.14f,.86f,-.29f),.27f,Finish.Stone);
            }
            g.Rock(V(0,2.30f,-.66f),V(.40f,.47f,.39f),Finish.StoneLight,4);
            g.Box(V(0,2.05f,-.64f),V(.91f,.10f,.075f),Finish.Charcoal,Q,.016f);
            for(int i=0;i<4;i++)g.Box(V(-.28f+i*.18f,2.06f,-.691f),V(.12f,.15f,.055f),Finish.Bone,Q,.008f);
            for(int i=0;i<9;i++)
            {
                float a=i*Mathf.PI/8;
                g.Rock(V(Mathf.Cos(a)*.61f,1.70f-Mathf.Sin(a)*.28f,-.68f),V(.13f,.18f,.10f),Finish.Bone,i);
            }
            Rune(g,V(0,.91f,-.77f),1.6f,Finish.Brass);
            SaveFurniture("horned_ancestor_goblin_idol_3d",g,V(0,1.70f,0),V(2.65f,3.40f,1.70f));
        }

        private static void BuildOfferingTable()
        {
            var g=new Geometry();
            g.Box(V(0,.12f,0),V(2.10f,.24f,1.22f),Finish.StoneDark,Q,.07f);
            foreach(float x in new[] {-.70f,.70f})g.Box(V(x,.54f,0),V(.39f,.66f,.78f),Finish.Stone,Q,.06f);
            g.Box(V(0,.96f,0),V(2.18f,.26f,1.27f),Finish.StoneLight,Q,.08f);
            g.Box(V(0,1.11f,0),V(1.98f,.06f,1.08f),Finish.Obsidian,Q,.025f);
            foreach(float x in new[] {-.77f,.77f})
            {
                g.Box(V(x,.97f,-.65f),V(.10f,.20f,.06f),Finish.Brass,Q,.009f);
                g.Rock(V(x,.73f,-.57f),V(.16f,.23f,.065f),Finish.Bone,x);
            }
            Rune(g,V(0,.72f,-.56f),.9f,Finish.Brass);
            SaveFurniture("carved_offering_table_module_3d",g,V(0,.57f,0),V(2.18f,1.14f,1.27f));
        }

        private static void BuildOfferings()
        {
            var food=new Geometry(); Bowl(food,V(-.43f,0,0),.83f,Finish.Clay);
            for(int i=0;i<6;i++)
            {
                Vector3 p=V(-.43f+Mathf.Sin(i*2.1f)*.20f,.19f,Mathf.Cos(i*2.1f)*.17f);
                food.Lathe(p,new[] {V(.032f,0,0),V(.035f,.12f,0)},Finish.Bone,10);
                food.Lathe(p+V(0,.105f,0),new[] {V(.035f,0,0),V(.15f,.035f,0),V(.11f,.09f,0),V(0,.13f,0)},Finish.Mushroom,12);
            }
            Jar(food,V(.39f,0,.14f),.60f); Candle(food,V(.74f,0,-.25f),.19f);
            SaveFurniture("mushroom_and_roots_offering_3d",food,Vector3.zero,Vector3.zero);
            var relic=new Geometry(); Skull(relic,V(0,.065f,0),.95f);
            relic.Box(V(0,.035f,0),V(1.25f,.07f,.73f),Finish.Burgundy,Q,.008f);
            foreach(float x in new[] {-.60f,.60f})Candle(relic,V(x,0,.22f),.28f);
            SaveFurniture("skull_and_candles_offering_3d",relic,Vector3.zero,Vector3.zero);
            var jewels=new Geometry(); Bowl(jewels,V(-.32f,0,0),.90f,Finish.Brass);
            for(int i=0;i<12;i++)
            {
                jewels.Lathe(V(-.32f+Mathf.Sin(i*2.5f)*.22f,.19f+(i%3)*.014f,Mathf.Cos(i*2.5f)*.20f),new[] {V(.07f,0,0),V(.07f,.016f,0)},Finish.Gold,12);
            }
            jewels.Lathe(V(.43f,.03f,.03f),new[] {V(.18f,0,0),V(.24f,.17f,0),V(.12f,.54f,0),V(0,.79f,0)},Finish.Jade,6,Quaternion.Euler(0,0,-8));
            Candle(jewels,V(.75f,0,-.23f),.17f);
            SaveFurniture("jade_and_coins_offering_3d",jewels,Vector3.zero,Vector3.zero);
        }

        private static void BuildTorch()
        {
            var g=new Geometry();
            g.Box(V(0,.13f,0),V(.93f,.26f,.93f),Finish.StoneDark,Q,.08f);
            g.Lathe(V(0,.26f,0),new[] {V(.31f,0,0),V(.24f,.13f,0),V(.16f,1.16f,0),V(.25f,1.28f,0)},Finish.Stone,10);
            for(int i=0;i<3;i++)g.Ring(V(0,.52f+i*.36f,0),.21f,.16f,.065f,Finish.Brass,12);
            Bowl(g,V(0,1.56f,0),1.14f,Finish.Iron);
            g.Ring(V(0,1.89f,0),.53f,.46f,.07f,Finish.Brass,16);
            foreach(float x in new[] {-.31f,.31f})g.Beam(V(x,.50f,-.05f),V(x,1.57f,-.05f),.10f,Finish.Bone);
            g.Lathe(V(0,1.71f,0),new[] {V(.30f,0,0),V(.24f,.12f,0)},Finish.Ember,12);
            for(int i=0;i<5;i++)
            {
                float a=i*Mathf.PI*2/5;
                g.Lathe(V(Mathf.Cos(a)*.14f,1.80f,Mathf.Sin(a)*.14f),new[] {V(.105f,0,0),V(.145f,.24f,0),V(.065f,.51f,0),V(0,.80f+(i%2)*.17f,0)},Finish.Flame,10,Quaternion.Euler(i*3-6,0,i*5-10));
            }
            SaveFurniture("bone_bound_procession_torch_3d",g,V(0,.82f,0),V(.90f,1.64f,.90f));
        }

        private static void BuildColumn()
        {
            var g=new Geometry();
            g.Box(V(0,.18f,0),V(1.57f,.36f,1.57f),Finish.StoneDark,Q,.11f);
            for(int i=0;i<5;i++)
            {
                float y=.68f+i*.70f;
                g.Box(V(0,y,0),V(1.15f,.66f,1.17f),i%2==0?Finish.Stone:Finish.StoneDark,Quaternion.Euler(0,i%2==0?0:3,0),.085f);
                if(i==1||i==3)g.Box(V(0,y,-.62f),V(.76f,.50f,.08f),Finish.Obsidian,Q,.045f);
            }
            g.Box(V(0,4.06f,0),V(1.52f,.28f,1.49f),Finish.StoneLight,Q,.075f);
            g.Box(V(0,4.28f,0),V(1.32f,.16f,1.29f),Finish.Brass,Q,.035f);
            for(int i=0;i<3;i++)Rune(g,V(0,1.18f+i*.90f,-.675f),1.08f,i==1?Finish.Jade:Finish.Brass);
            Skull(g,V(0,3.72f,-.70f),.74f);
            SaveFurniture("rune_carved_ritual_column_3d",g,V(0,2.18f,0),V(1.57f,4.36f,1.57f));
        }

        private static void BuildBanner()
        {
            var g=new Geometry();
            g.Beam(V(-1.03f,3.82f,0),V(1.03f,3.82f,0),.12f,Finish.OakDark);
            g.Box(V(0,2.64f,.03f),V(1.66f,2.14f,.043f),Finish.Burgundy,Q,.008f);
            for(int i=0;i<7;i++)g.Box(V(-.71f+i*.235f,1.48f+(i%3)*.06f,.03f),V(.225f,.30f+(i%3)*.12f,.043f),Finish.Burgundy,Q,.006f);
            foreach(float x in new[] {-.76f,.76f})g.Box(V(x,2.69f,-.007f),V(.068f,2.10f,.019f),Finish.Brass,Q,.003f);
            Rune(g,V(0,2.70f,-.05f),2.50f,Finish.Bone);
            g.Ring(V(0,2.67f,-.048f),.58f,.55f,.013f,Finish.Brass,20,Quaternion.Euler(90,0,0));
            foreach(float x in new[] {-.87f,.87f})g.Beam(V(x,3.73f,0),V(x,4.16f,.21f),.057f,Finish.Rope);
            SaveFurniture("ancestor_sigil_hide_banner_3d",g,Vector3.zero,Vector3.zero);
        }

        private static void BuildDrum()
        {
            var g=new Geometry();
            g.Lathe(Vector3.zero,new[] {V(.62f,0,0),V(.83f,.25f,0),V(.79f,1.02f,0),V(.66f,1.33f,0)},Finish.Oak,20);
            for(int i=0;i<14;i++)
            {
                float a=i*Mathf.PI*2/14;
                Vector3 a1=V(Mathf.Cos(a)*.69f,.12f,Mathf.Sin(a)*.69f);
                Vector3 a2=V(Mathf.Cos(a+.16f)*.75f,1.25f,Mathf.Sin(a+.16f)*.75f);
                g.Beam(a1,a2,.032f,Finish.Rope);
            }
            g.Lathe(V(0,1.30f,0),new[] {V(.70f,0,0),V(.71f,.055f,0)},Finish.Hide,24);
            g.Ring(V(0,1.25f,0),.76f,.67f,.13f,Finish.Brass,24);
            g.Ring(V(0,.08f,0),.68f,.61f,.09f,Finish.Iron,20);
            g.Beam(V(-.44f,1.40f,-.19f),V(.44f,1.40f,.23f),.06f,Finish.Bone);
            g.Rock(V(.44f,1.40f,.23f),V(.16f,.15f,.16f),Finish.Hide,4);
            SaveFurniture("rope_laced_ceremonial_drum_3d",g,V(0,.69f,0),V(1.64f,1.38f,1.64f));
        }

        private static void BuildIncense()
        {
            var g=new Geometry();
            g.Lathe(Vector3.zero,new[] {V(.28f,0,0),V(.30f,.09f,0),V(.10f,.19f,0),V(.13f,.40f,0)},Finish.Brass,20);
            Bowl(g,V(0,.36f,0),.86f,Finish.Brass);
            g.Lathe(V(0,.48f,0),new[] {V(.27f,0,0),V(.27f,.024f,0)},Finish.Ash,16);
            for(int i=0;i<7;i++)g.Beam(V(Mathf.Sin(i*2.4f)*.13f,.51f,Mathf.Cos(i*2.4f)*.13f),V(Mathf.Sin(i*2.4f)*.18f,1.03f,Mathf.Cos(i*2.4f)*.18f),.015f,Finish.OakDark);
            SaveFurniture("brass_incense_and_ash_bowl_3d",g,Vector3.zero,Vector3.zero);
        }

        private static void BuildKneelingMat()
        {
            var g=new Geometry();
            g.Box(V(0,.055f,0),V(2.60f,.11f,1.62f),Finish.Cloth,Q,.055f);
            g.Box(V(0,.117f,0),V(2.33f,.016f,1.37f),Finish.StoneDark,Q,.008f);
            foreach(float x in new[] {-1.12f,1.12f})g.Box(V(x,.132f,0),V(.058f,.015f,1.38f),Finish.Brass,Q,.003f);
            for(int i=0;i<16;i++)foreach(float z in new[] {-.88f,.88f})g.Beam(V(-1.14f+i*.152f,.055f,z),V(-1.14f+i*.152f,.045f,z*1.11f),.025f,Finish.Rope);
            SaveFurniture("elder_grey_woven_kneeling_mat_3d",g,Vector3.zero,Vector3.zero);
        }

        private static void BuildBoneChimes()
        {
            var g=new Geometry();
            foreach(float x in new[] {-1.05f,1.05f})
            {
                g.Beam(V(x,0,0),V(x,2.76f,0),.15f,Finish.OakDark);
                g.Beam(V(x-.30f,.08f,0),V(x+.30f,.08f,0),.17f,Finish.Oak);
            }
            g.Beam(V(-1.23f,2.72f,0),V(1.23f,2.72f,0),.17f,Finish.Oak);
            for(int i=0;i<8;i++)
            {
                float x=-.84f+i*.24f, y=1.45f+(i%3)*.15f;
                g.Beam(V(x,2.70f,0),V(x,y+.40f,0),.019f,Finish.Rope);
                g.Beam(V(x,y,0),V(x,y+.41f,0),.084f,Finish.Bone);
                g.Rock(V(x,y,0),V(.16f,.13f,.13f),Finish.Bone,i);
                g.Rock(V(x,y+.41f,0),V(.16f,.13f,.13f),Finish.Bone,i+2);
            }
            Skull(g,V(0,2.73f,0),.75f);
            SaveFurniture("hanging_ancestor_bone_chimes_3d",g,V(0,1.35f,0),V(2.60f,2.70f,.65f));
        }
    }
}
