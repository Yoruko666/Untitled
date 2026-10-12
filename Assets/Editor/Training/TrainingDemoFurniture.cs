using UnityEditor;
using UnityEngine;

namespace Untitled.Editor
{
    public static partial class TrainingDemoBuilder
    {
        private static void PrepareFurniture()
        {
            furniture.Clear();
            BuildCage();
            BuildStrategyTable();
            BuildWeaponRack();
            BuildArcheryTarget();
            BuildPracticeMat();
            BuildSandbags();
            BuildWarBoard();
            BuildEquipmentBench();
            BuildTrainingSign();
        }

        // Closed rings with a round cross section, rather than flat chain silhouettes.
        private static void Link(Geometry g, Vector3 center, float radius, Quaternion q, Finish finish = Finish.Iron)
        {
            const int sides = 16;
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides;
                float b = (i + 1) * Mathf.PI * 2 / sides;
                g.Beam(center + q * V(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0),
                    center + q * V(Mathf.Cos(b) * radius, Mathf.Sin(b) * radius, 0), .034f, finish);
            }
        }

        private static void BuildCage()
        {
            var g = new Geometry();
            g.Box(V(0,.16f,0), V(4.7f,.30f,4.25f), Finish.Iron, Quaternion.identity, .09f);
            for (int i = 0; i < 10; i++)
                g.Box(V(-2.05f + i * .456f,.34f,0), V(.43f,.09f,3.85f), i % 3 == 0 ? Finish.OakDark : Finish.Oak, Quaternion.identity, .022f);
            foreach (float x in new[] {-2.2f,2.2f}) foreach (float z in new[] {-1.95f,1.95f})
            {
                g.Box(V(x,1.85f,z),V(.18f,3.3f,.18f),Finish.Iron,Quaternion.identity,.028f);
                g.Box(V(x,3.58f,z),V(.33f,.19f,.33f),Finish.Brass,Quaternion.identity,.035f);
                g.Lathe(V(x,3.68f,z),new[]{V(.14f,0,0),V(.11f,.17f,0),V(.012f,.32f,0)},Finish.Iron,10);
            }
            foreach (float y in new[] {.47f,1.22f,2.63f,3.37f})
            {
                foreach (float z in new[] {-1.95f,1.95f})
                    g.Box(V(0,y,z),V(4.5f,.13f,.14f),Finish.Iron,Quaternion.identity,.025f);
                foreach (float x in new[] {-2.2f,2.2f})
                    g.Box(V(x,y,0),V(.14f,.13f,3.95f),Finish.Iron,Quaternion.identity,.025f);
            }
            for (int i = 0; i < 13; i++)
            {
                float x = -2.06f + i * .343f;
                g.Lathe(V(x,.5f,1.95f),new[]{V(.045f,0,0),V(.045f,2.8f,0)},Finish.Iron,8);
                // A distinct, inset door occupies the middle of the front face.
                if (Mathf.Abs(x) > .86f) g.Lathe(V(x,.5f,-1.95f),new[]{V(.045f,0,0),V(.045f,2.8f,0)},Finish.Iron,8);
                g.Beam(V(x,3.39f,-1.93f),V(x,3.39f,1.93f),.075f,Finish.Iron);
            }
            for (int i = 0; i < 11; i++) foreach (float x in new[] {-2.2f,2.2f})
                g.Lathe(V(x,.5f,-1.8f + i * .36f),new[]{V(.045f,0,0),V(.045f,2.8f,0)},Finish.Iron,8);
            foreach (float x in new[] {-.87f,.87f}) g.Box(V(x,1.83f,-2.02f),V(.15f,2.68f,.15f),Finish.Rust,Quaternion.identity,.018f);
            foreach (float y in new[] {.52f,3.13f}) g.Box(V(0,y,-2.02f),V(1.88f,.15f,.15f),Finish.Iron,Quaternion.identity,.025f);
            for (int i = 0; i < 6; i++) g.Beam(V(-.72f + i * .287f,.61f,-2.02f),V(-.72f + i * .287f,3.06f,-2.02f),.068f,Finish.Iron);
            foreach (float y in new[] {.9f,2.68f})
            {
                g.Box(V(-.94f,y,-2.08f),V(.31f,.26f,.22f),Finish.Brass,Quaternion.identity,.035f);
                g.Lathe(V(-.93f,y-.17f,-2.15f),new[]{V(.075f,0,0),V(.075f,.34f,0)},Finish.Iron,12);
            }
            g.Box(V(.65f,1.79f,-2.14f),V(.31f,.46f,.16f),Finish.Brass,Quaternion.identity,.05f);
            Link(g,V(.65f,2.01f,-2.13f),.13f,Quaternion.identity);
            g.Lathe(V(.65f,1.84f,-2.245f),new[]{V(.035f,0,0),V(.035f,.02f,0)},Finish.Charcoal,10,Quaternion.Euler(90,0,0));
            g.Box(V(.65f,1.77f,-2.25f),V(.025f,.09f,.012f),Finish.Charcoal,Quaternion.identity,.001f);
            foreach (float z in new[] {-2.03f,2.03f}) foreach (float y in new[] {.47f,1.22f,2.63f,3.37f})
                for (int i = 0; i < 8; i++) g.Bolt(V(-2.0f + i * .57f,y,z),.049f);
            for (int i = 0; i < 10; i++) Link(g,V(1.55f,2.55f-i*.12f,1.81f),.093f,Quaternion.Euler(0,i%2*90,0));
            Link(g,V(1.55f,1.28f,1.81f),.19f,Quaternion.identity);
            for (int i = 0; i < 24; i++)
            {
                Vector3 p=V(-1.75f+(i%6)*.22f,.409f,.48f+(i/6)*.21f);
                g.Beam(p,p+V(.26f,.003f,.07f*Mathf.Sin(i)),.019f,Finish.Straw);
            }
            g.Lathe(V(-1.3f,.39f,-1.15f),new[]{V(.25f,0,0),V(.36f,.12f,0),V(.37f,.17f,0)},Finish.Charcoal,16);
            g.Ring(V(-1.3f,.56f,-1.15f),.375f,.29f,.03f,Finish.Iron,16);
            SaveFurniture("riveted_iron_cage_3d",g,V(0,1.85f,0),V(4.7f,3.7f,4.25f));
        }

        private static void BuildStrategyTable()
        {
            var g = new Geometry();
            foreach (float x in new[] {-1.48f,1.48f}) foreach (float z in new[] {-.95f,.95f})
            {
                g.Box(V(x,.70f,z),V(.30f,1.35f,.32f),Finish.OakDark,Quaternion.identity,.04f);
                foreach(float y in new[]{.18f,1.10f})g.Box(V(x,y,z),V(.34f,.13f,.36f),Finish.Iron,Quaternion.identity,.022f);
            }
            g.Beam(V(-1.48f,.45f,0),V(1.48f,.45f,0),.15f,Finish.Oak);
            g.Beam(V(-1.4f,.47f,0),V(1.4f,1.2f,0),.10f,Finish.OakDark);
            for(int i=0;i<9;i++)g.Box(V(-1.78f+i*.445f,1.42f,0),V(.43f,.19f,2.7f),i%4==0?Finish.OakLight:Finish.Oak,Quaternion.identity,.03f);
            foreach(float z in new[]{-1.3f,1.3f})
            {
                g.Box(V(0,1.34f,z),V(4.05f,.25f,.1f),Finish.Iron,Quaternion.identity,.022f);
                for(int i=0;i<8;i++)g.Bolt(V(-1.68f+i*.48f,1.35f,z+Mathf.Sign(z)*.06f),.045f);
            }
            g.Box(V(-.16f,1.526f,0),V(2.65f,.018f,1.95f),Finish.Parchment,Quaternion.Euler(0,-7,0),.035f);
            var mapMat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="training_strategy_map"};
            mapMat.SetTexture("_BaseMap",CreateMapTexture());mapMat.SetColor("_BaseColor",Color.white);mapMat.SetFloat("_Smoothness",.05f);
            SaveAsset(mapMat,Materials+"/training_strategy_map.mat");
            // Planar inset with explicit 0..1 UVs prevents the map from repeating across the table.
            GameObject table=SaveFurniture("war_strategy_table_3d",g,V(0,.74f,0),V(4.05f,1.48f,2.7f));
            GameObject contents=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(table));
            var inset=new GameObject("HandDrawnCaveMap");inset.transform.SetParent(contents.transform,false);inset.transform.localPosition=V(-.16f,1.539f,0);inset.transform.localRotation=Quaternion.Euler(0,-7,0);
            var mesh=new Mesh{name="training_strategy_map_surface"};mesh.vertices=new[]{V(-1.30f,0,-.95f),V(-1.30f,0,.95f),V(1.30f,0,.95f),V(1.30f,0,-.95f)};
            mesh.triangles=new[]{0,1,2,0,2,3};mesh.uv=new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)};mesh.RecalculateNormals();mesh.RecalculateBounds();
            inset.AddComponent<MeshFilter>().sharedMesh=SaveAsset(mesh,Models+"/training_strategy_map_surface.asset");inset.AddComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Materials+"/training_strategy_map.mat");
            var pieces=new Geometry();
            for(int i=0;i<7;i++)
            {
                Vector3 p=V(-1.04f+(i%4)*.52f,1.558f,-.56f+(i/4)*.9f);
                pieces.Lathe(p,new[]{V(.075f,0,0),V(.06f,.065f,0),V(.035f,.16f,0),V(.075f,.21f,0)},i%2==0?Finish.Burgundy:Finish.Cloth,10);
                pieces.Beam(p+V(0,.20f,0),p+V(0,.42f,0),.022f,Finish.Iron);
                pieces.Box(p+V(.075f,.36f,0),V(.15f,.11f,.018f),i%2==0?Finish.Burgundy:Finish.Cloth,Quaternion.identity,.002f);
            }
            foreach(float z in new[]{-.64f,.40f})pieces.Lathe(V(1.51f,1.56f,z),new[]{V(.13f,0,0),V(.13f,.55f,0)},Finish.Parchment,16,Quaternion.Euler(0,0,90));
            pieces.Box(V(1.38f,1.57f,-.07f),V(.56f,.11f,.58f),Finish.OakDark,Quaternion.Euler(0,9,0),.02f);
            pieces.Box(V(1.38f,1.63f,-.07f),V(.51f,.022f,.54f),Finish.Parchment,Quaternion.Euler(0,9,0),.007f);
            pieces.Lathe(V(-1.70f,1.54f,-.78f),new[]{V(.10f,0,0),V(.10f,.14f,0),V(.056f,.19f,0)},Finish.Charcoal,12);
            pieces.Beam(V(-1.61f,1.61f,-.62f),V(-1.15f,1.58f,-.84f),.023f,Finish.Bone);
            MeshObject("training_strategy_tokens_and_scrolls",pieces,contents.transform);
            furniture["war_strategy_table_3d"]=PrefabUtility.SaveAsPrefabAsset(contents,AssetDatabase.GetAssetPath(table));PrefabUtility.UnloadPrefabContents(contents);
        }

        private static Texture2D CreateMapTexture()
        {
            const int w=512,h=384;
            var t=new Texture2D(w,h,TextureFormat.RGB24,false);
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                float n=Mathf.PerlinNoise(x*.10f,y*.10f)*.12f;
                Color c=new Color(.68f+n,.55f+n,.34f+n);
                float river=256+38*Mathf.Sin(y*.026f)+16*Mathf.Sin(y*.071f);
                if(Mathf.Abs(x-river)<9)c=new Color(.21f,.35f,.32f);
                int[] roomsX={50,126,330,405,95,340};int[] roomsY={58,112,55,140,265,275};
                for(int i=0;i<roomsX.Length;i++)
                {
                    int dx=x-roomsX[i],dy=y-roomsY[i];
                    if(dx>=0&&dx<65&&dy>=0&&dy<47)
                        if(dx<3||dx>61||dy<3||dy>43)c=new Color(.24f,.21f,.14f);
                    if(Mathf.Abs(dx-32)<2&&dy>46&&dy<90)c=new Color(.32f,.24f,.14f);
                }
                if((y==211||y==213)&&x>90&&x<420)c=new Color(.42f,.17f,.08f);
                if((x%32==0||y%32==0)&&x>18&&x<w-18&&y>18&&y<h-18)c=Color.Lerp(c,new Color(.35f,.29f,.20f),.15f);
                if(x<7||x>w-8||y<7||y>h-8)c=new Color(.38f,.28f,.16f);
                t.SetPixel(x,y,c);
            }
            t.Apply();string path=Textures+"/training_hand_drawn_cave_map.png";System.IO.File.WriteAllBytes(path,t.EncodeToPNG());Object.DestroyImmediate(t);AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.filterMode=FilterMode.Point;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void Spear(Geometry g,Vector3 p,float lean)
        {
            Vector3 top=p+V(lean,2.7f,0);
            g.Beam(p,top,.067f,Finish.OakLight);
            g.Lathe(top,new[]{V(.03f,0,0),V(.18f,.17f,0),V(.013f,.62f,0)},Finish.Iron,4);
            for(int i=0;i<6;i++)g.Ring(p+V(lean*.86f,2.32f+i*.047f,0),.054f,.033f,.025f,Finish.Leather,8);
        }

        private static void BuildWeaponRack()
        {
            var g=new Geometry();
            foreach(float x in new[]{-1.8f,1.8f})
            {
                g.Box(V(x,1.35f,.17f),V(.22f,2.65f,.23f),Finish.OakDark,Quaternion.identity,.03f);
                g.Beam(V(x,.14f,-.7f),V(x,1.13f,.23f),.17f,Finish.Oak);
                g.Box(V(x,.15f,0),V(.6f,.18f,1.6f),Finish.OakDark,Quaternion.identity,.04f);
            }
            foreach(float y in new[]{.55f,2.35f})g.Box(V(0,y,.18f),V(3.9f,.21f,.24f),Finish.Oak,Quaternion.identity,.03f);
            for(int i=0;i<6;i++)Spear(g,V(-1.50f+i*.46f,.12f,-.05f),.13f*Mathf.Sin(i));
            foreach(float x in new[]{-1.85f,1.85f})foreach(float y in new[]{.55f,2.35f})g.Bolt(V(x,y,.025f),.06f);
            for(int i=0;i<2;i++)
            {
                Vector3 p=V(-.6f+i*1.25f,.62f,-.39f);
                Quaternion q=Quaternion.Euler(90,0,0);
                g.Lathe(p,new[]{V(.46f,0,0),V(.48f,.08f,0)},Finish.OakDark,20,q);
                g.Ring(p+V(0,0,-.08f),.49f,.425f,.035f,Finish.Iron,20,q);
                g.Lathe(p+V(0,0,-.12f),new[]{V(.14f,0,0),V(.09f,.11f,0)},Finish.Iron,12,q);
            }
            SaveFurniture("spears_and_shields_rack_3d",g,V(0,1.4f,0),V(4.0f,2.8f,1.65f));
        }

        private static void BuildArcheryTarget()
        {
            var g=new Geometry();
            foreach(float x in new[]{-.67f,.67f})g.Beam(V(x,.1f,-.43f),V(x*.4f,1.7f,.16f),.14f,Finish.OakDark);
            g.Beam(V(0,.1f,.77f),V(0,1.78f,.2f),.15f,Finish.Oak);
            Quaternion q=Quaternion.Euler(90,0,0);Vector3 p=V(0,1.66f,-.18f);
            g.Lathe(p,new[]{V(.79f,0,0),V(.79f,.15f,0)},Finish.Straw,32,q);
            g.Ring(p+V(0,0,-.16f),.78f,.63f,.025f,Finish.OakDark,32,q);
            g.Ring(p+V(0,0,-.19f),.52f,.43f,.014f,Finish.Burgundy,32,q);
            g.Ring(p+V(0,0,-.21f),.30f,.24f,.014f,Finish.Burgundy,32,q);
            g.Lathe(p+V(0,0,-.235f),new[]{V(.12f,0,0),V(.12f,.02f,0)},Finish.Burgundy,20,q);
            for(int i=0;i<4;i++)
            {
                Vector3 a=p+V(-.22f+i*.13f,.18f*Mathf.Sin(i),-.27f);
                g.Beam(a,a+V(.13f,-.05f,-.7f),.024f,Finish.OakLight);
                g.Box(a+V(.13f,-.05f,-.64f),V(.12f,.07f,.19f),Finish.Bone,Quaternion.Euler(0,-10,0),.004f);
            }
            SaveFurniture("straw_archery_target_3d",g,V(0,1.35f,0),V(1.7f,2.7f,1.1f));
        }

        private static void BuildPracticeMat()
        {
            var g=new Geometry();
            g.Lathe(Vector3.zero,new[]{V(1.10f,0,0),V(1.16f,.05f,0)},Finish.Sand,40);
            g.Ring(V(0,.051f,0),1.12f,1.09f,.008f,Finish.Straw,40);
            for(int i=0;i<10;i++)g.Beam(V(-.68f+i*.14f,.058f,-.70f),V(-.58f+i*.14f,.058f,-.40f),.012f,Finish.OakDark);
            SaveFurniture("worn_dummy_training_pad_3d",g,Vector3.zero,Vector3.zero);
        }

        private static void BuildSandbags()
        {
            var g=new Geometry();
            for(int i=0;i<6;i++)
            {
                Vector3 p=V((i%3-1)*.58f,.30f+(i/3)*.37f,(i/3)*.17f);
                g.Rock(p,V(.86f,.48f,.62f),Finish.Straw,i*.87f);
                g.Beam(p+V(-.3f,.16f,-.21f),p+V(.29f,.16f,-.21f),.017f,Finish.Leather);
                for(int j=0;j<6;j++)g.Beam(p+V(-.24f+j*.085f,.17f,-.23f),p+V(-.22f+j*.085f,.17f,-.18f),.011f,Finish.OakDark);
            }
            SaveFurniture("stitched_sandbag_stack_3d",g,V(0,.55f,.1f),V(2.1f,1.1f,1.1f));
        }

        private static void BuildWarBoard()
        {
            var g=new Geometry();
            g.Box(V(0,0,0),V(2.85f,1.85f,.15f),Finish.OakDark,Quaternion.identity,.05f);
            g.Box(V(0,0,-.09f),V(2.55f,1.55f,.025f),Finish.Charcoal,Quaternion.identity,.01f);
            foreach(float x in new[]{-1.32f,1.32f})g.Box(V(x,0,-.13f),V(.12f,1.77f,.12f),Finish.Oak,Quaternion.identity,.02f);
            foreach(float y in new[]{-.82f,.82f})g.Box(V(0,y,-.13f),V(2.7f,.12f,.12f),Finish.Oak,Quaternion.identity,.02f);
            for(int i=0;i<5;i++)for(int j=0;j<4;j++)
            {
                Vector3 p=V(-1.02f+i*.47f,-.55f+j*.34f,-.119f);
                g.Beam(p,p+V(.24f,.13f,0),.015f,Finish.Bone);
                if(i%2==0)g.Beam(p+V(.02f,.13f,0),p+V(.24f,.01f,0),.014f,Finish.Bone);
            }
            g.Box(V(0,-.90f,-.23f),V(2.65f,.1f,.43f),Finish.OakDark,Quaternion.identity,.02f);
            for(int i=0;i<4;i++)g.Box(V(-.7f+i*.13f,-.84f,-.27f),V(.10f,.035f,.025f),Finish.Bone,Quaternion.Euler(0,i*5,0),.004f);
            SaveFurniture("chalk_tactics_board_3d",g,Vector3.zero,Vector3.zero);
        }

        private static void BuildEquipmentBench()
        {
            var g=new Geometry();
            foreach(float x in new[]{-1.3f,1.3f})foreach(float z in new[]{-.35f,.35f})g.Box(V(x,.5f,z),V(.16f,1,.17f),Finish.OakDark,Quaternion.identity,.022f);
            for(int i=0;i<5;i++)g.Box(V(0,1.04f,-.46f+i*.23f),V(3.35f,.14f,.215f),Finish.Oak,Quaternion.identity,.02f);
            g.Lathe(V(-1.05f,1.12f,0),new[]{V(.29f,0,0),V(.28f,.18f,0),V(.23f,.36f,0),V(.05f,.47f,0)},Finish.Iron,16);
            g.Box(V(-1.05f,1.32f,-.25f),V(.46f,.05f,.05f),Finish.Brass,Quaternion.identity,.008f);
            for(int i=0;i<2;i++)
            {
                Vector3 p=V(.05f+i*.58f,1.13f,-.1f);
                g.Beam(p,p+V(.09f,.06f,.65f),.06f,Finish.OakLight);
                g.Box(p+V(.09f,.08f,.66f),V(.38f,.09f,.25f),Finish.Iron,Quaternion.Euler(0,-12,0),.02f);
                g.Beam(p+V(.09f,.09f,.52f),p+V(.09f,.09f,.78f),.09f,Finish.Iron);
            }
            g.Box(V(.45f,.28f,.1f),V(1.15f,.3f,.65f),Finish.Leather,Quaternion.identity,.09f);
            SaveFurniture("armour_and_axe_workbench_3d",g,V(0,.56f,0),V(3.4f,1.12f,1.15f));
        }

        private static void BuildTrainingSign()
        {
            var g=new Geometry();
            g.Box(Vector3.zero,V(2.7f,.85f,.16f),Finish.OakDark,Quaternion.identity,.07f);
            for(int i=0;i<2;i++)
            {
                Quaternion q=Quaternion.Euler(0,0,i==0?-38:38);
                g.Beam(q*V(0,-.39f,-.13f),q*V(0,.32f,-.13f),.055f,Finish.Bone);
                g.Box(q*V(0,.18f,-.14f),V(.45f,.24f,.07f),Finish.Iron,q,.015f);
            }
            foreach(float x in new[]{-1.12f,1.12f})foreach(float y in new[]{-.25f,.25f})g.Bolt(V(x,y,-.1f),.04f);
            SaveFurniture("crossed_axes_training_sign_3d",g,Vector3.zero,Vector3.zero);
        }
    }
}
