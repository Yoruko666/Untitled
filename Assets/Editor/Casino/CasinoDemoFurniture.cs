using UnityEngine;

namespace Untitled.Editor
{
    public static partial class CasinoDemoBuilder
    {
        private static void PrepareFurniture()
        {
            furniture.Clear();
            BuildTable();
            BuildMat();
            BuildGamingPieces();
            BuildSafe();
            BuildGold();
            BuildDesk();
            BuildChair();
            BuildCashier();
            BuildSign();
            BuildSack();
        }

        private static void BuildTable()
        {
            var g = new Geometry();
            g.Box(V(0,.22f,0), V(2.1f,.44f,2.1f), Finish.StoneDark, Quaternion.identity, .14f);
            foreach (float x in new[] { -.90f, .90f }) foreach (float z in new[] { -.90f, .90f })
                g.Box(V(x,.43f,z), V(.48f,.38f,.48f), Finish.Stone, Quaternion.identity, .08f);
            g.Box(V(0,.59f,0), V(3.0f,.26f,3.0f), Finish.StoneLight, Quaternion.identity, .15f);
            g.Box(V(0,.736f,0), V(2.43f,.04f,2.43f), Finish.Cloth, Quaternion.identity, .08f);
            foreach (float edge in new[] { -1.28f, 1.28f })
            {
                g.Box(V(0,.756f,edge), V(2.64f,.03f,.035f), Finish.Brass, Quaternion.identity, .005f);
                g.Box(V(edge,.756f,0), V(.035f,.03f,2.64f), Finish.Brass, Quaternion.identity, .005f);
            }
            foreach (float x in new[] { -1.34f, 1.34f }) foreach (float z in new[] { -1.34f, 1.34f })
            {
                g.Box(V(x,.723f,z), V(.30f,.06f,.30f), Finish.Iron, Quaternion.identity, .025f);
                g.Bolt(V(x,.77f,z), .045f);
            }
            // Worn geometric markings carved into the slab, readable from overhead.
            foreach (float z in new[] { -1.45f, 1.45f }) for (int i = 0; i < 7; i++)
                g.Box(V(-.9f+i*.3f,.735f,z), V(.10f,.012f,.09f), Finish.Charcoal, Quaternion.Euler(0,45,0), .004f);
            SaveFurniture("low_stone_gambling_table_3d", g, V(0,.36f,0), V(3,.72f,3));
        }

        private static void BuildMat()
        {
            var g = new Geometry();
            g.Box(V(0,.06f,0), V(3.65f,.09f,1.35f), Finish.Straw, Quaternion.identity, .055f);
            g.Box(V(0,.11f,0), V(3.29f,.035f,1.03f), Finish.Cloth, Quaternion.identity, .03f);
            for (int i = 0; i < 27; i++)
            {
                float x = -1.67f+i*.128f;
                g.Box(V(x,.12f,0), V(.040f,.026f,1.20f), Finish.Straw, Quaternion.identity, .008f);
                foreach (float z in new[] { -.54f, .54f }) g.Box(V(x,.14f,z), V(.079f,.018f,.07f), Finish.Cork, Quaternion.identity, .003f);
            }
            foreach (float z in new[] { -.65f, .65f }) g.Beam(V(-1.82f,.135f,z), V(1.82f,.135f,z), .055f, Finish.Cork);
            for (int i = 0; i < 11; i++) foreach (float end in new[] { -1.86f, 1.86f })
                g.Beam(V(end,.09f,-.54f+i*.107f), V(end+Mathf.Sign(end)*.18f,.08f,-.53f+i*.107f), .023f, Finish.Straw);
            SaveFurniture("woven_gambling_mat_3d", g, Vector3.zero, Vector3.zero);
        }

        private static void Coin(Geometry g, Vector3 p, float radius = .12f, Finish finish = Finish.Gold)
        {
            g.Lathe(p, new[] { V(radius,0,0), V(radius,.037f,0) }, finish, 16);
            g.Ring(p+V(0,.038f,0), radius*.81f, radius*.64f, .009f, Finish.Brass, 16);
        }

        private static void Dice(Geometry g, Vector3 p, float yaw, int count)
        {
            Quaternion q = Quaternion.Euler(0,yaw,0);
            g.Box(p+V(0,.13f,0), V(.26f,.26f,.26f), Finish.Bone, q, .034f);
            Vector3[] pips = count == 3 ? new[] {V(-.065f,0,-.065f),Vector3.zero,V(.065f,0,.065f)}
                : new[] {V(-.065f,0,-.065f),V(.065f,0,-.065f),V(-.065f,0,.065f),V(.065f,0,.065f),Vector3.zero};
            foreach (Vector3 pip in pips) g.Lathe(p+q*pip+V(0,.261f,0), new[] {V(.022f,0,0),V(.022f,.008f,0)}, Finish.Charcoal, 8);
            g.Rock(p+q*V(0,.13f,-.132f), V(.047f,.047f,.012f), Finish.Charcoal, 0);
        }

        private static void BuildGamingPieces()
        {
            var dice = new Geometry();
            Dice(dice, V(-.20f,0,.10f), 17, 5);
            Dice(dice, V(.28f,0,-.18f), -21, 3);
            dice.Lathe(V(-.78f,0,-.3f), new[] {V(.20f,0,0),V(.24f,.30f,0),V(.22f,.40f,0)}, Finish.OakDark, 12);
            dice.Ring(V(-.78f,.37f,-.3f), .235f,.18f,.06f,Finish.Brass,16);
            SaveFurniture("bone_dice_and_cup_3d", dice, Vector3.zero, Vector3.zero);

            var cards = new Geometry();
            for (int i = 0; i < 5; i++)
            {
                Vector3 p = V(-.64f+i*.32f,.012f+i*.008f,0);
                Quaternion q = Quaternion.Euler(0,(i-2)*12,0);
                cards.Box(p, V(.44f,.025f,.65f), Finish.Parchment, q, .025f);
                cards.Box(p+q*V(0,.018f,0), V(.15f,.013f,.15f), i%2==0?Finish.Burgundy:Finish.Charcoal, q*Quaternion.Euler(0,45,0), .005f);
                cards.Box(p+q*V(-.13f,.018f,-.22f), V(.06f,.013f,.07f), Finish.Burgundy, q, .002f);
            }
            cards.Box(V(.62f,.09f,.60f), V(.45f,.18f,.65f), Finish.Parchment, Quaternion.identity, .02f);
            cards.Box(V(.62f,.187f,.60f), V(.38f,.015f,.58f), Finish.Burgundy, Quaternion.identity, .012f);
            SaveFurniture("marked_card_hand_3d", cards, Vector3.zero, Vector3.zero);

            var wheel = new Geometry();
            wheel.Lathe(V(0,0,0),new[] {V(.94f,0,0),V(1.0f,.06f,0),V(1.0f,.13f,0)},Finish.OakDark,48);
            wheel.Ring(V(0,.13f,0),1.02f,.91f,.06f,Finish.Brass,48);
            for (int i = 0; i < 12; i++)
            {
                float a=i*Mathf.PI/6;
                Vector3 p=V(Mathf.Cos(a)*.70f,.15f,Mathf.Sin(a)*.70f);
                wheel.Box(p,V(.34f,.035f,.28f),i%2==0?Finish.Burgundy:Finish.Cloth,Quaternion.Euler(0,-i*30,0),.02f);
                wheel.Box(p+V(0,.022f,0),V(.09f,.014f,.09f),Finish.Bone,Quaternion.Euler(0,45,0),.003f);
                wheel.Beam(V(Mathf.Cos(a)*.29f,.16f,Mathf.Sin(a)*.29f),V(Mathf.Cos(a)*.91f,.16f,Mathf.Sin(a)*.91f),.025f,Finish.Brass);
            }
            wheel.Lathe(V(0,.16f,0),new[] {V(.14f,0,0),V(.13f,.12f,0),V(.06f,.2f,0)},Finish.Brass,16);
            wheel.Beam(V(-.48f,.29f,.10f),V(.63f,.29f,-.14f),.055f,Finish.Gold);
            SaveFurniture("rune_luck_wheel_3d",wheel,Vector3.zero,Vector3.zero);

            var coins=new Geometry();
            for(int pile=0;pile<6;pile++)
            {
                Vector3 p=V((pile%3)*.31f-.3f,0,(pile/3)*.33f-.16f);
                for(int j=0;j<3+pile%4;j++) Coin(coins,p+V(.01f*Mathf.Sin(j),j*.041f,0),.12f,pile%3==0?Finish.RedChip:pile%3==1?Finish.BlueChip:Finish.Gold);
            }
            for(int i=0;i<7;i++)Coin(coins,V(-.48f+i*.14f,.0f,-.43f+Mathf.Sin(i)*.08f),.095f);
            SaveFurniture("coins_and_chip_stacks_3d",coins,Vector3.zero,Vector3.zero);
        }

        private static void BuildSafe()
        {
            var g=new Geometry();
            g.Box(V(0,.15f,0),V(3.65f,.3f,2.55f),Finish.StoneDark,Quaternion.identity,.13f);
            g.Box(V(0,1.78f,0),V(3.25f,3.26f,2.28f),Finish.Iron,Quaternion.identity,.17f);
            g.Box(V(0,1.78f,-1.16f),V(2.94f,2.93f,.15f),Finish.Charcoal,Quaternion.identity,.1f);
            g.Box(V(0,1.78f,-1.28f),V(2.65f,2.67f,.17f),Finish.Iron,Quaternion.identity,.13f);
            foreach(float x in new[]{-1.26f,1.26f})g.Box(V(x,1.78f,-1.4f),V(.13f,2.6f,.1f),Finish.Brass,Quaternion.identity,.02f);
            foreach(float y in new[]{.51f,3.04f})g.Box(V(0,y,-1.4f),V(2.66f,.13f,.1f),Finish.Brass,Quaternion.identity,.02f);
            foreach(float x in new[]{-1.18f,1.18f})foreach(float y in new[]{.72f,1.25f,2.31f,2.84f})g.Bolt(V(x,y,-1.49f),.075f);
            foreach(float y in new[]{.95f,2.55f})
            {
                g.Box(V(-1.53f,y,-1.27f),V(.39f,.40f,.36f),Finish.Iron,Quaternion.identity,.06f);
                g.Lathe(V(-1.52f,y-.25f,-1.4f),new[]{V(.10f,0,0),V(.10f,.50f,0)},Finish.Brass,16);
            }
            Quaternion face=Quaternion.Euler(90,0,0);
            g.Ring(V(.15f,1.6f,-1.72f),.50f,.43f,.07f,Finish.Brass,32,face);
            for(int i=0;i<5;i++)
            {
                float a=i*Mathf.PI*2/5;
                g.Beam(V(.15f,1.6f,-1.75f),V(.15f+Mathf.Cos(a)*.46f,1.6f+Mathf.Sin(a)*.46f,-1.75f),.065f,Finish.Iron);
            }
            g.Lathe(V(.15f,1.6f,-1.83f),new[]{V(.12f,0,0),V(.12f,.13f,0)},Finish.Brass,20,face);
            g.Lathe(V(.62f,2.37f,-1.55f),new[]{V(.19f,0,0),V(.19f,.10f,0)},Finish.Brass,24,face);
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/6;
                g.Box(V(.62f+Mathf.Cos(a)*.14f,2.37f+Mathf.Sin(a)*.14f,-1.67f),V(.025f,.035f,.012f),Finish.Bone,Quaternion.Euler(0,0,i*30),.002f);
            }
            g.Box(V(-.45f,2.64f,-1.51f),V(.62f,.3f,.03f),Finish.Brass,Quaternion.identity,.04f);
            g.Box(V(-.45f,2.64f,-1.535f),V(.23f,.018f,.016f),Finish.Charcoal,Quaternion.identity,.002f);
            SaveFurniture("riveted_vault_safe_3d",g,V(0,1.8f,0),V(3.65f,3.6f,2.8f));
        }

        private static void BuildGold()
        {
            var g=new Geometry();
            for(int row=0;row<3;row++)for(int x=0;x<3-row;x++)for(int z=0;z<3;z++)
            {
                Vector3 p=V((x-(2-row)*.5f)*.68f,.15f+row*.27f,(z-1)*.57f);
                g.Box(p,V(.62f,.26f,.48f),Finish.Gold,Quaternion.Euler(0,(row%2)*90,0),.065f);
                g.Box(p+V(0,.136f,0),V(.17f,.011f,.12f),Finish.Brass,Quaternion.identity,.025f);
            }
            for(int i=0;i<17;i++)Coin(g,V(Mathf.Sin(i*3.7f)*1.3f,0,Mathf.Cos(i*1.9f)*1.15f),.13f);
            SaveFurniture("stacked_gold_reserve_3d",g,Vector3.zero,Vector3.zero);
        }

        private static void BuildDesk()
        {
            var g=new Geometry();
            foreach(float x in new[]{-1.2f,1.2f})
            {
                g.Box(V(x,.63f,0),V(.70f,1.25f,1.35f),Finish.OakDark,Quaternion.identity,.06f);
                for(int i=0;i<3;i++)
                {
                    g.Box(V(x,.28f+i*.36f,-.7f),V(.60f,.31f,.08f),Finish.Oak,Quaternion.identity,.022f);
                    g.Ring(V(x,.3f+i*.36f,-.77f),.065f,.04f,.025f,Finish.Brass,12,Quaternion.Euler(90,0,0));
                }
            }
            for(int i=0;i<4;i++)g.Box(V(0,1.36f,-.60f+i*.4f),V(3.45f,.19f,.38f),i%2==0?Finish.Oak:Finish.OakLight,Quaternion.identity,.035f);
            g.Box(V(0,1.465f,0),V(1.45f,.018f,.85f),Finish.Burgundy,Quaternion.identity,.035f);
            g.Box(V(-.3f,1.53f,-.06f),V(.75f,.11f,.64f),Finish.OakDark,Quaternion.Euler(0,12,0),.02f);
            g.Box(V(-.3f,1.60f,-.06f),V(.69f,.023f,.60f),Finish.Parchment,Quaternion.Euler(0,12,0),.01f);
            for(int i=0;i<6;i++)g.Box(V(-.34f,1.616f,-.24f+i*.061f),V(.45f,.009f,.011f),Finish.Cork,Quaternion.Euler(0,12,0),.001f);
            g.Lathe(V(.67f,1.46f,-.06f),new[]{V(.11f,0,0),V(.12f,.13f,0),V(.05f,.18f,0)},Finish.Iron,12);
            g.Beam(V(.68f,1.6f,-.04f),V(.86f,2.06f,.04f),.025f,Finish.Bone);
            g.Box(V(.84f,1.97f,.025f),V(.075f,.3f,.025f),Finish.Bone,Quaternion.Euler(0,0,-18),.01f);
            SaveFurniture("boss_ledger_desk_3d",g,V(0,.73f,0),V(3.45f,1.46f,1.55f));
        }

        private static void BuildChair()
        {
            var g=new Geometry();
            foreach(float x in new[]{-.50f,.50f})foreach(float z in new[]{-.45f,.45f})g.Beam(V(x,0,z),V(x*.9f,.85f,z*.9f),.18f,Finish.OakDark);
            g.Box(V(0,.83f,0),V(1.25f,.19f,1.17f),Finish.Oak,Quaternion.identity,.06f);
            g.Box(V(0,.96f,0),V(1.10f,.16f,1.02f),Finish.Burgundy,Quaternion.identity,.09f);
            foreach(float x in new[]{-.59f,.59f})
            {
                g.Beam(V(x,.80f,.49f),V(x,2.35f,.55f),.17f,Finish.OakDark);
                g.Box(V(x,1.33f,0),V(.16f,.13f,1.2f),Finish.OakLight,Quaternion.identity,.03f);
                g.Beam(V(x,.92f,-.4f),V(x,1.32f,-.4f),.1f,Finish.Oak);
                g.Beam(V(x,2.26f,.55f),V(x*1.2f,2.7f,.55f),.13f,Finish.Bone);
            }
            g.Box(V(0,1.73f,.55f),V(1.06f,1.17f,.16f),Finish.Oak,Quaternion.identity,.05f);
            g.Box(V(0,1.77f,.44f),V(.85f,.9f,.10f),Finish.Burgundy,Quaternion.identity,.055f);
            g.Box(V(0,2.26f,.42f),V(.31f,.06f,.11f),Finish.Brass,Quaternion.identity,.02f);
            SaveFurniture("boss_bone_crest_chair_3d",g,V(0,1.1f,.05f),V(1.45f,2.2f,1.35f));
        }

        private static void BuildCashier()
        {
            var g=new Geometry();
            g.Box(V(0,.64f,0),V(3.2f,1.28f,1.3f),Finish.OakDark,Quaternion.identity,.055f);
            for(int i=0;i<8;i++)g.Box(V(-1.4f+i*.4f,.67f,-.68f),V(.37f,1.10f,.09f),Finish.Oak,Quaternion.identity,.02f);
            g.Box(V(0,1.36f,0),V(3.44f,.18f,1.65f),Finish.OakLight,Quaternion.identity,.06f);
            // Lower front opening lets coins change hands; cage remains visually tall at the sides.
            foreach(float x in new[]{-1.55f,1.55f})g.Beam(V(x,1.42f,-.25f),V(x,3.1f,-.25f),.10f,Finish.Iron);
            for(int i=0;i<11;i++)
            {
                float x=-1.4f+i*.28f;
                g.Beam(V(x,Mathf.Abs(x)<.6f?2.14f:1.43f,-.25f),V(x,3.07f,-.25f),.045f,Finish.Iron);
            }
            foreach(float y in new[]{2.14f,2.99f})g.Beam(V(-1.59f,y,-.25f),V(1.59f,y,-.25f),.075f,Finish.Brass);
            g.Box(V(0,3.12f,-.25f),V(3.4f,.19f,.21f),Finish.Oak,Quaternion.identity,.04f);
            g.Box(V(.92f,1.51f,-.42f),V(.64f,.12f,.47f),Finish.Iron,Quaternion.identity,.025f);
            for(int i=0;i<4;i++)Coin(g,V(.7f+i*.15f,1.585f,-.42f),.065f);
            SaveFurniture("caged_cashier_counter_3d",g,V(0,.72f,0),V(3.45f,1.44f,1.65f));
        }

        private static void BuildSign()
        {
            var g=new Geometry();
            g.Box(V(0,0,0),V(2.8f,1.05f,.18f),Finish.OakDark,Quaternion.identity,.11f);
            foreach(float x in new[]{-1.31f,1.31f})g.Box(V(x,0,-.10f),V(.11f,.96f,.06f),Finish.Brass,Quaternion.identity,.02f);
            foreach(float x in new[]{-.5f,.5f})
            {
                Quaternion q=Quaternion.Euler(0,0,x<0?-15:18);
                g.Box(V(x,0,-.17f),V(.56f,.56f,.10f),Finish.Bone,q,.055f);
                foreach(var p in new[]{V(-.12f,-.12f,0),Vector3.zero,V(.12f,.12f,0)})
                    g.Rock(V(x,0,-.23f)+q*p,V(.067f,.067f,.015f),Finish.Charcoal,0);
            }
            g.Beam(V(-.96f,-.32f,-.28f),V(.93f,.34f,-.28f),.046f,Finish.Gold);
            foreach(float x in new[]{-1.05f,1.05f})g.Beam(V(x,.43f,0),V(x,.94f,0),.035f,Finish.Iron);
            SaveFurniture("crossed_dice_casino_sign_3d",g,Vector3.zero,Vector3.zero);
        }

        private static void BuildSack()
        {
            var g=new Geometry();
            g.Lathe(Vector3.zero,new[]{V(.27f,.03f,0),V(.40f,.18f,0),V(.43f,.40f,0),V(.30f,.61f,0),V(.14f,.68f,0),V(.21f,.82f,0)},Finish.Straw,12);
            g.Ring(V(0,.66f,0),.16f,.125f,.08f,Finish.Cork,16);
            g.Beam(V(.1f,.7f,-.05f),V(.35f,.58f,-.12f),.035f,Finish.Cork);
            g.Beam(V(.1f,.7f,-.05f),V(.32f,.79f,-.11f),.035f,Finish.Cork);
            g.Box(V(0,.35f,-.408f),V(.2f,.2f,.025f),Finish.Burgundy,Quaternion.Euler(0,0,45),.02f);
            SaveFurniture("tied_coin_sack_3d",g,Vector3.zero,Vector3.zero);
        }
    }
}
