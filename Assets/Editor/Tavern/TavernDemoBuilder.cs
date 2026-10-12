using System;
using System.Collections.Generic;
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
    // Offline authoring only. All geometry is saved as editable scene/prefab assets.
    public static partial class TavernDemoBuilder
    {
        public const string ScenePath = "Assets/Scenes/Props/GoblinTavernDemo.unity";
        private const string Models = "Assets/Art/Models/Props/Tavern";
        private const string Materials = "Assets/Art/Materials/Props/Tavern";
        private const string Textures = "Assets/Art/Textures/Props/Tavern";
        private const string Prefabs = "Assets/Prefabs/Props/Tavern";
        private static Material[] palette;
        private static readonly Dictionary<string, GameObject> furniture = new Dictionary<string, GameObject>();
        private static readonly Color warm = new Color(1f, 0.55f, 0.22f);
        private enum Finish { Oak, OakLight, OakDark, Iron, Brass, Stone, StoneLight, StoneDark, Cloth, Bone, Flame, Ember, Bottle, Cork, Moss }
        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        [MenuItem("Untitled/Tavern/Open Scene Demo")]
        public static void OpenDemo()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath)) EditorSceneManager.OpenScene(ScenePath);
            else Build();
        }

        // Intentionally has no automatic load callback and never edits CaveHD2DLocal.
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Demo already exists. Open and edit it directly.");
            foreach (string folder in new[] { Models, Materials, Textures, Prefabs, "Assets/Scenes/Props" })
                Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            PrepareMaterials();
            PrepareFurniture();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Transform room = Group("GoblinTavern_Module_32x32", null);
            BuildRoom(room);
            BuildLayout(room);
            PrefabUtility.SaveAsPrefabAsset(room.gameObject, Prefabs + "/goblin_tavern_room.prefab");
            Object.DestroyImmediate(room.gameObject);
            room = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/goblin_tavern_room.prefab"))).transform;
            BuildPresentation();
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            int triangles = room.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh.triangles.Length / 3);
            Debug.Log($"TAVERN_BUILD_OK: {furniture.Count} reusable furniture prefabs; {triangles} placed triangles; scene saved at {ScenePath}");
            Capture();
        }

        private static void PrepareMaterials()
        {
            var wood = CreateWoodTexture();
            palette = new Material[15];
            Color[] colors = {
                new Color(0.47f,0.27f,0.12f), new Color(0.65f,0.40f,0.19f), new Color(0.24f,0.13f,0.075f),
                new Color(0.16f,0.19f,0.19f), new Color(0.51f,0.34f,0.12f),
                new Color(0.36f,0.38f,0.35f), new Color(0.40f,0.41f,0.37f), new Color(0.29f,0.32f,0.30f),
                new Color(0.16f,0.28f,0.21f), new Color(0.75f,0.68f,0.48f),
                new Color(1f,0.69f,0.25f), new Color(0.83f,0.16f,0.025f),
                new Color(0.12f,0.28f,0.16f), new Color(0.44f,0.31f,0.16f), new Color(0.12f,0.20f,0.12f)
            };
            for (int i = 0; i < palette.Length; i++)
            {
                string name = ((Finish)i).ToString().ToLowerInvariant();
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "tavern_" + name };
                mat.SetColor("_BaseColor", colors[i]);
                mat.SetFloat("_Smoothness", i == 12 ? 0.55f : i == 3 || i == 4 ? 0.28f : 0.09f);
                mat.SetFloat("_Metallic", i == 3 || i == 4 ? 0.68f : 0);
                if (i < 3) mat.SetTexture("_BaseMap", wood);
                if (i == 10 || i == 11)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", colors[i] * (i == 10 ? 3.5f : 1.7f));
                }
                string path = Materials + "/tavern_" + name + ".mat";
                palette[i] = SaveAsset(mat, path);
            }
        }

        private static Texture2D CreateWoodTexture()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGB24, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float wave = x + 2.2f * Mathf.Sin(y * 0.085f) + 1.1f * Mathf.Sin(y * 0.18f + x * 0.06f);
                    float grain = Mathf.Pow(Mathf.Abs(Mathf.Sin(wave * 1.15f)), 12);
                    float knot = Mathf.Sin(Mathf.Sqrt((x-39)*(x-39)*0.7f+(y-64)*(y-64)*0.09f) * 1.4f);
                    float value = 0.78f + 0.17f * Mathf.PerlinNoise(x * 0.11f, y * 0.018f) - grain * 0.19f + knot * 0.035f;
                    texture.SetPixel(x, y, new Color(value, value * 0.96f, value * 0.89f));
                }
            texture.Apply();
            string path = Textures + "/tavern_oak_grain.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static T SaveAsset<T>(T asset, string path) where T : Object
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) { EditorUtility.CopySerialized(asset, existing); Object.DestroyImmediate(asset); EditorUtility.SetDirty(existing); return existing; }
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static Transform Group(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static GameObject MeshObject(string name, Geometry geometry, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Mesh mesh = SaveAsset(geometry.ToMesh(name), Models + "/" + name.ToLowerInvariant() + ".asset");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = geometry.UsedMaterials.Select(i => palette[i]).ToArray();
            return go;
        }

        private static GameObject SaveFurniture(string name, Geometry g, Vector3 colliderCenter, Vector3 colliderSize)
        {
            GameObject go = MeshObject(name, g, null);
            if (colliderSize != Vector3.zero)
            {
                BoxCollider collider = go.AddComponent<BoxCollider>();
                collider.center = colliderCenter; collider.size = colliderSize;
            }
            string path = Prefabs + "/" + name + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            furniture[name] = prefab;
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static Transform Place(Transform parent, string name, Vector3 position, float yaw = 0, float scale = 1)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(furniture[name], parent);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale = Vector3.one * scale;
            return go.transform;
        }

        private static void BoxCollision(Transform parent, Vector3 center, Vector3 size)
        {
            var c = parent.gameObject.AddComponent<BoxCollider>(); c.center = center; c.size = size;
        }

        private static void PrepareFurniture()
        {
            furniture.Clear();
            BuildTable(); BuildStool(); BuildBar(); BuildShelf(); BuildBarrel(); BuildCrate(); BuildBench(); BuildGate(); BuildLantern(); BuildTorch(); BuildChandelier(); BuildSign();
        }

        private static void BuildTable()
        {
            var g = new Geometry();
            g.RoundPlanks(V(0,1.46f,0), 2f, 0.21f, 12);
            g.Lathe(V(0,1.29f,0), new[] { V(1.86f,0,0),V(1.95f,0.04f,0),V(1.95f,0.11f,0),V(1.88f,0.14f,0) }, Finish.OakDark, 48);
            g.Ring(V(0,1.47f,0), 2.01f, 1.965f, 0.085f, Finish.Iron, 64);
            for (int i=0;i<20;i++)
            {
                float a = i*Mathf.PI*0.1f;
                g.Bolt(V(1.998f*Mathf.Cos(a),1.5f,1.998f*Mathf.Sin(a)),0.035f);
            }
            g.Lathe(V(0,0,0), new[] { V(0.65f,0.10f,0),V(0.72f,0.18f,0),V(0.52f,0.32f,0),V(0.35f,0.40f,0),V(0.32f,1.18f,0),V(0.58f,1.34f,0) }, Finish.OakDark, 12);
            for (int i=0;i<4;i++)
            {
                Quaternion rot = Quaternion.Euler(0,i*90,0);
                g.Box(rot*V(0.65f,0.16f,0),V(1.65f,0.21f,0.32f),Finish.Oak,rot,0.05f);
                g.Beam(rot*V(0.20f,0.38f,0),rot*V(1.38f,1.3f,0),0.19f,Finish.Oak);
            }
            GameObject table = SaveFurniture("round_table_3d",g,Vector3.zero,Vector3.zero);
            GameObject editable = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(table));
            var collision = new Geometry(); collision.Lathe(V(0,1.3f,0),new[] {V(2f,0,0),V(2f,0.3f,0)},Finish.Oak,24);
            var top = editable.AddComponent<MeshCollider>(); top.sharedMesh=SaveAsset(collision.ToMesh("round_table_collision"),Models+"/round_table_collision.asset"); top.convex=true;
            var foot = editable.AddComponent<BoxCollider>(); foot.center=V(0,0.62f,0); foot.size=V(2.6f,1.24f,2.6f);
            PrefabUtility.SaveAsPrefabAsset(editable,AssetDatabase.GetAssetPath(table));
            PrefabUtility.UnloadPrefabContents(editable);
        }

        private static void BuildStool()
        {
            var g = new Geometry(); g.RoundPlanks(V(0,0.80f,0),0.48f,0.14f,5);
            g.Ring(V(0,0.78f,0),0.49f,0.455f,0.085f,Finish.Iron,24);
            for(int i=0;i<4;i++)
            {
                float a=Mathf.PI*0.5f*i+Mathf.PI*0.25f;
                Vector3 bottom=V(Mathf.Cos(a)*0.4f,0.07f,Mathf.Sin(a)*0.4f);
                Vector3 top=V(Mathf.Cos(a)*0.28f,0.77f,Mathf.Sin(a)*0.28f);
                g.Beam(bottom,top,0.125f,Finish.OakDark);
                g.Bolt(V(Mathf.Cos(a)*0.29f,0.88f,Mathf.Sin(a)*0.29f),0.034f);
            }
            g.Ring(V(0,0.27f,0),0.37f,0.32f,0.065f,Finish.OakDark,16);
            SaveFurniture("wooden_stool_3d",g,V(0,0.4f,0),V(0.85f,0.85f,0.85f));
        }

        private static void BuildBar()
        {
            var g = new Geometry();
            g.Box(V(0,0.8f,0),V(4,1.6f,1.22f),Finish.OakDark,Quaternion.identity,0.045f);
            for(int i=0;i<8;i++)
                g.Box(V(-1.75f+i*0.5f,0.83f,-0.63f),V(0.455f,1.41f,0.09f),i%3==0?Finish.OakLight:Finish.Oak,Quaternion.identity,0.016f);
            for(int i=0;i<4;i++)
                g.Box(V(0,1.76f,-0.67f+i*0.445f),V(4.16f,0.22f,0.425f),i%2==0?Finish.OakLight:Finish.Oak,Quaternion.identity,0.045f);
            foreach(float x in new[] {-1.96f,0f,1.96f})
            {
                g.Box(V(x,0.85f,-0.7f),V(0.15f,1.55f,0.14f),Finish.Iron,Quaternion.identity,0.025f);
                foreach(float y in new[] {0.25f,1.38f}) g.Bolt(V(x,y,-0.79f),0.043f);
            }
            g.Beam(V(-2.04f,0.1f,-0.72f),V(2.04f,0.1f,-0.72f),0.13f,Finish.OakLight);
            g.Beam(V(-1.8f,0.33f,-1.0f),V(1.8f,0.33f,-1.0f),0.075f,Finish.Brass);
            foreach(float x in new[] {-1.65f,1.65f}) g.Beam(V(x,0.25f,-0.67f),V(x,0.33f,-1f),0.055f,Finish.Iron);
            SaveFurniture("bar_counter_module_3d",g,V(0,0.87f,0),V(4.15f,1.87f,1.62f));
        }

        private static void BuildShelf()
        {
            var g = new Geometry();
            for(int i=0;i<9;i++) g.Box(V(-1.8f+i*0.45f,1.9f,0.42f),V(0.425f,3.8f,0.12f),Finish.OakDark,Quaternion.identity,0.02f);
            foreach(float x in new[] {-2f,0,2f}) g.Box(V(x,1.95f,0),V(0.18f,3.9f,1f),Finish.Oak,Quaternion.identity,0.04f);
            foreach(float y in new[] {0.2f,1.15f,2.15f,3.25f,3.85f})
            {
                g.Box(V(0,y,0),V(4.3f,0.16f,1.16f),Finish.OakLight,Quaternion.identity,0.025f);
                g.Box(V(0,y,-0.56f),V(4.15f,0.085f,0.05f),Finish.Brass,Quaternion.identity,0.008f);
            }
            // Shelf stock is part of the wine rack, while dining tables remain empty.
            for(int row=0;row<3;row++)
                for(int i=0;i<8;i++)
                {
                    float x=-1.66f+i*0.46f;
                    float y=new[] {0.28f,1.23f,2.23f}[row];
                    float h=0.50f+0.10f*((i+row)%3);
                    g.Lathe(V(x,y,-0.06f),new[] {V(0.14f,0,0),V(0.16f,0.06f,0),V(0.16f,h*0.65f,0),V(0.075f,h*0.78f,0),V(0.06f,h,0)},Finish.Bottle,12);
                    g.Box(V(x,y+h*0.4f,-0.2f),V(0.13f,0.15f,0.015f),Finish.Bone,Quaternion.identity,0.005f);
                    g.Lathe(V(x,y+h,-0.06f),new[] {V(0.058f,0,0),V(0.058f,0.065f,0)},Finish.Cork,8);
                }
            SaveFurniture("wine_rack_3d",g,V(0,1.94f,0.1f),V(4.3f,3.9f,1.1f));
        }

        private static void BuildBarrel()
        {
            var g = new Geometry();
            float[] heights={0.05f,0.16f,0.45f,0.88f,1.3f,1.66f,1.78f};
            float[] radii={0.59f,0.64f,0.73f,0.77f,0.73f,0.64f,0.59f};
            for(int stave=0;stave<18;stave++)
            {
                float a=stave*360f/18+0.22f, b=(stave+1)*360f/18-0.22f;
                for(int j=0;j<heights.Length-1;j++)
                    g.SectorSide(a,b,radii[j],radii[j+1],heights[j],heights[j+1],stave%4==0?Finish.OakLight:Finish.Oak);
            }
            foreach(float y in new[] {0.19f,0.49f,1.27f,1.63f})
            {
                float radius=y<0.3f||y>1.5f?0.666f:0.762f;
                g.Ring(V(0,y,0),radius,radius-0.06f,0.10f,Finish.Iron,36);
                for(int i=0;i<9;i++) {float a=i*Mathf.PI*2/9;g.Bolt(V(radius*Mathf.Cos(a),y+0.05f,radius*Mathf.Sin(a)),0.029f);}
            }
            g.RoundPlanks(V(0,1.77f,0),0.59f,0.09f,6);
            g.Lathe(V(0,1.84f,0),new[] {V(0.09f,0,0),V(0.08f,0.065f,0)},Finish.Cork,12);
            SaveFurniture("ale_barrel_3d",g,V(0,0.9f,0),V(1.45f,1.85f,1.45f));
        }

        private static void BuildCrate()
        {
            var g=new Geometry();
            for(int i=0;i<5;i++)
            {
                float p=-0.6f+i*0.3f;
                g.Box(V(p,0.70f,0),V(0.275f,0.12f,1.35f),Finish.Oak,Quaternion.identity,0.022f);
                foreach(float z in new[] {-0.67f,0.67f}) g.Box(V(p,0.36f,z),V(0.28f,0.64f,0.12f),Finish.Oak,Quaternion.identity,0.018f);
            }
            foreach(float x in new[] {-0.68f,0.68f})
            {
                for(int j=0;j<3;j++) g.Box(V(x,0.16f+j*0.23f,0),V(0.12f,0.20f,1.3f),Finish.OakDark,Quaternion.identity,0.018f);
                foreach(float z in new[] {-0.67f,0.67f}) {g.Box(V(x,0.36f,z),V(0.17f,0.74f,0.17f),Finish.OakLight,Quaternion.identity,0.02f);g.Bolt(V(x,0.65f,z-0.095f),0.035f);}
            }
            foreach(float z in new[] {-0.2f,0.2f}) g.Box(V(0,0.78f,z),V(1.45f,0.045f,0.095f),Finish.Iron,Quaternion.identity,0.01f);
            g.Beam(V(-0.57f,0.15f,-0.74f),V(0.57f,0.65f,-0.74f),0.11f,Finish.OakLight);
            SaveFurniture("supply_crate_3d",g,V(0,0.4f,0),V(1.52f,0.85f,1.55f));
        }

        private static void BuildBench()
        {
            var g=new Geometry();
            for(int i=0;i<3;i++)g.Box(V(0,0.77f,-0.34f+i*0.34f),V(3.7f,0.15f,0.32f),i==1?Finish.OakLight:Finish.Oak,Quaternion.identity,0.025f);
            foreach(float x in new[] {-1.35f,1.35f})
            {
                foreach(float z in new[] {-0.32f,0.32f})g.Beam(V(x,0.05f,z*1.3f),V(x,0.76f,z),0.19f,Finish.OakDark);
                g.Beam(V(x,0.22f,-0.45f),V(x,0.22f,0.45f),0.12f,Finish.Oak);
            }
            g.Beam(V(-1.4f,0.32f,0),V(1.4f,0.32f,0),0.16f,Finish.OakDark);
            SaveFurniture("wall_bench_3d",g,V(0,0.43f,0),V(3.75f,0.9f,1.05f));
        }

        private static void BuildGate()
        {
            var g=new Geometry();
            for(int i=0;i<6;i++)g.Box(V(-0.64f+i*0.255f,0.55f,0),V(0.235f,1.06f,0.14f),i%2==0?Finish.Oak:Finish.OakLight,Quaternion.identity,0.02f);
            g.Box(V(0,1.1f,0),V(1.56f,0.12f,0.2f),Finish.OakDark,Quaternion.identity,0.03f);
            foreach(float y in new[] {0.25f,0.87f}) g.Box(V(0,y,-0.1f),V(1.47f,0.105f,0.055f),Finish.Iron,Quaternion.identity,0.01f);
            g.Beam(V(-0.63f,0.21f,-0.14f),V(0.64f,0.9f,-0.14f),0.085f,Finish.Iron);
            g.Ring(V(0.52f,0.77f,-0.18f),0.11f,0.065f,0.055f,Finish.Brass,16,Quaternion.Euler(90,0,0));
            SaveFurniture("saloon_half_gate_3d",g,V(0,0.56f,0),V(1.58f,1.18f,0.22f));
        }

        private static void BuildLantern()
        {
            var g=new Geometry();
            g.Lathe(V(0,0,0),new[] {V(0.23f,0.03f,0),V(0.3f,0.12f,0),V(0.26f,0.2f,0),V(0.13f,0.28f,0)},Finish.Brass,16);
            g.Lathe(V(0,0.23f,0),new[] {V(0.05f,0,0),V(0.06f,0.22f,0),V(0.018f,0.44f,0)},Finish.Flame,12);
            foreach(float x in new[] {-0.25f,0.25f})foreach(float z in new[] {-0.25f,0.25f})g.Beam(V(x,0.12f,z),V(x,0.75f,z),0.035f,Finish.Iron);
            g.Box(V(0,0.76f,0),V(0.65f,0.09f,0.65f),Finish.Iron,Quaternion.identity,0.05f);
            g.Lathe(V(0,0.8f,0),new[] {V(0.3f,0,0),V(0.15f,0.17f,0),V(0.11f,0.22f,0)},Finish.Iron,8);
            g.Ring(V(0,1.12f,0),0.14f,0.105f,0.04f,Finish.Iron,20,Quaternion.Euler(90,0,0));
            var lantern=SaveFurniture("oil_lantern_3d",g,Vector3.zero,Vector3.zero);
            AddLight(lantern,V(0,0.46f,0),2.5f,9f,false);
        }

        private static void BuildTorch()
        {
            var g=new Geometry();
            g.Box(V(0,0.35f,0.14f),V(0.32f,0.72f,0.09f),Finish.Iron,Quaternion.identity,0.025f);
            g.Beam(V(0,0.1f,0.1f),V(0,0.5f,-0.35f),0.12f,Finish.Iron);
            g.Lathe(V(0,0.3f,-0.34f),new[] {V(0.15f,0,0),V(0.24f,0.25f,0),V(0.26f,0.37f,0)},Finish.Iron,12);
            for(int i=0;i<5;i++)
            {
                float a=i*Mathf.PI*2/5;
                g.Beam(V(Mathf.Cos(a)*0.13f,0.50f,-0.34f+Mathf.Sin(a)*0.13f),V(Mathf.Cos(a)*0.08f,0.82f,-0.34f+Mathf.Sin(a)*0.08f),0.09f,Finish.OakDark);
            }
            g.Lathe(V(0,0.63f,-0.34f),new[] {V(0.17f,0,0),V(0.20f,0.17f,0),V(0.10f,0.38f,0),V(0.01f,0.62f,0)},Finish.Ember,9);
            g.Lathe(V(0,0.65f,-0.39f),new[] {V(0.10f,0,0),V(0.12f,0.14f,0),V(0.005f,0.41f,0)},Finish.Flame,8);
            var torch=SaveFurniture("wall_brazier_3d",g,Vector3.zero,Vector3.zero);
            AddLight(torch,V(0,0.93f,-0.45f),4.5f,11.5f,false);
        }

        private static void AddLight(GameObject prefab,Vector3 point,float intensity,float range,bool shadows)
        {
            string path=AssetDatabase.GetAssetPath(prefab);
            GameObject contents=PrefabUtility.LoadPrefabContents(path);
            var go=new GameObject("WarmLight"); go.transform.SetParent(contents.transform,false); go.transform.localPosition=point;
            var light=go.AddComponent<Light>();light.type=LightType.Point;light.color=warm;light.intensity=intensity;light.range=range;
            light.shadows=shadows?LightShadows.Soft:LightShadows.None;light.shadowBias=0.035f;light.shadowNormalBias=0.15f;
            var flicker=go.AddComponent<HybridFireLight>();var settings=new SerializedObject(flicker);
            settings.FindProperty("baseIntensity").floatValue=intensity;settings.FindProperty("baseRange").floatValue=range;settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(contents,path);PrefabUtility.UnloadPrefabContents(contents);
        }

        private static void BuildChandelier()
        {
            var g=new Geometry();g.Ring(V(0,0,0),1.55f,1.43f,0.13f,Finish.Iron,48);
            for(int i=0;i<6;i++)
            {
                float a=i*Mathf.PI/3;Vector3 p=V(Mathf.Cos(a)*1.5f,0,Mathf.Sin(a)*1.5f);
                g.Beam(p,V(0,1.8f,0),0.035f,Finish.Iron);
                g.Lathe(p,new[] {V(0.13f,0,0),V(0.18f,0.14f,0),V(0.05f,0.22f,0)},Finish.Brass,12);
                g.Lathe(p+Vector3.up*0.22f,new[] {V(0.09f,0,0),V(0.05f,0.20f,0),V(0.002f,0.37f,0)},Finish.Flame,8);
            }
            g.Beam(V(0,1.8f,0),V(0,2.6f,0),0.045f,Finish.Iron);
            var chandelier=SaveFurniture("iron_chandelier_3d",g,Vector3.zero,Vector3.zero);
            AddLight(chandelier,V(0,-0.2f,0),12f,18f,true);
        }

        private static void BuildSign()
        {
            var g=new Geometry();
            g.Box(V(0,0,0),V(2.1f,1.05f,0.18f),Finish.OakDark,Quaternion.identity,0.12f);
            g.Box(V(0,-0.08f,-0.13f),V(0.65f,0.58f,0.1f),Finish.Brass,Quaternion.Euler(0,0,-10),0.06f);
            g.Ring(V(0.42f,-0.04f,-0.16f),0.25f,0.16f,0.05f,Finish.Brass,16,Quaternion.Euler(90,0,0));
            g.Box(V(0,0.24f,-0.16f),V(0.76f,0.15f,0.08f),Finish.Bone,Quaternion.Euler(0,0,-10),0.06f);
            foreach(float x in new[] {-0.82f,0.82f}){g.Beam(V(x,0.48f,0),V(x,1.01f,0),0.035f,Finish.Iron);g.Bolt(V(x,0.32f,-0.11f),0.048f);}
            SaveFurniture("tavern_ale_sign_3d",g,Vector3.zero,Vector3.zero);
        }
    }
}
