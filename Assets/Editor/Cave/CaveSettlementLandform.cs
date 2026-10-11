using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Untitled.Editor
{
    public static partial class CaveHD2DSceneBuilder
    {
        private const string SettlementMeshPath = "Assets/Art/Models/CaveHD2D/settlement_modules.asset";
        private static Mesh boulder;
        private static Material rock, rockLight, rope;

        private static void PrepareSettlementMaterials(CaveTerrainStyle style)
        {
            void Terrain(Material material, Color tint, float strength, float frequency)
            {
                Undo.RecordObject(material, "Unify cave material scale");
                material.SetColor("_BaseColor", tint);
                material.SetFloat("_TextureStrength", strength);
                material.SetFloat("_TileScale", frequency);
                material.SetFloat("_Smoothness", 0.03f);
                EditorUtility.SetDirty(material);
            }
            Terrain(style.Soil, new Color(0.55f,0.41f,0.25f), 0.26f, 0.35f);
            Terrain(style.Settlement, new Color(0.41f,0.36f,0.28f), 0.30f, 0.45f);
            Terrain(style.WetGround, new Color(0.24f,0.36f,0.33f), 0.26f, 0.45f);
            Terrain(style.Hall, new Color(0.51f,0.43f,0.34f), 0.30f, 0.45f);
            Terrain(style.RockCap, new Color(0.28f,0.31f,0.30f), 0.16f, 0.3f);
            Terrain(map.WallMaterial, new Color(0.33f,0.34f,0.32f), 0.08f, 0.3f);
            stone = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/CarvedStone.mat");
            wood = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Timber.mat");
            soil = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/GardenSoil.mat");
            Terrain(stone, new Color(0.43f,0.40f,0.34f), 0.10f, 0.45f);
            Terrain(wood, new Color(0.31f,0.19f,0.095f), 0.10f, 0.6f);
            Terrain(soil, new Color(0.20f,0.20f,0.12f), 0.10f, 0.5f);
            rock = Solid("settlement_rock", new Color(0.31f,0.34f,0.34f));
            rockLight = Solid("settlement_rock_light", new Color(0.42f,0.42f,0.37f));
            rope = Solid("settlement_rope", new Color(0.53f,0.43f,0.28f));
            Undo.RecordObject(map.RiverMaterial, "Color cave river");
            map.RiverMaterial.SetColor("_DeepColor", new Color(0.035f,0.22f,0.24f));
            map.RiverMaterial.SetColor("_ShallowColor", new Color(0.12f,0.46f,0.42f));
            map.RiverMaterial.SetColor("_FoamColor", new Color(0.46f,0.68f,0.59f));
            map.RiverMaterial.SetFloat("_PixelsPerUnit", 16);
            map.RiverMaterial.SetFloat("_CellSize", map.CellSize);
            EditorUtility.SetDirty(map.RiverMaterial);
        }

        private static Material Solid(string name, Color color, bool twoSided = false)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
                material.SetColor("_BaseColor", color);
                material.SetFloat("_Smoothness", 0.08f);
                material.SetFloat("_Cull", twoSided ? 0 : 2);
                AssetDatabase.CreateAsset(material, path);
            }
            return material;
        }

        private static void PrepareSettlementModules()
        {
            boulder = Module("FacetedRock", () =>
            {
                var vertices = new List<Vector3>();
                void Tri(Vector3 a, Vector3 b, Vector3 c) => vertices.AddRange(new[] { a,b,c });
                Vector3 Ring(int i, float height, float radius)
                {
                    float angle = i * Mathf.PI / 4;
                    float irregular = 1 + 0.12f * Mathf.Sin(i * 2.3f);
                    return new Vector3(Mathf.Cos(angle)*radius*irregular, height, Mathf.Sin(angle)*radius*irregular);
                }
                for (int i = 0; i < 8; i++)
                {
                    var a = Ring(i,0,0.78f); var b = Ring(i+1,0,0.78f);
                    var c = Ring(i+1,0.50f,1); var d = Ring(i,0.50f,1);
                    var e = Ring(i+1,0.91f,0.62f); var f = Ring(i,0.91f,0.62f);
                    Tri(a,d,c); Tri(a,c,b); Tri(d,f,e); Tri(d,e,c);
                    Tri(f,new Vector3(0.07f,1.05f,-0.08f),e);
                }
                return MeshFrom("FacetedRock", vertices);
            });
        }

        private static Mesh MeshFrom(string name, List<Vector3> vertices)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(Enumerable.Range(0,vertices.Count).ToArray(),0);
            mesh.SetUVs(0, vertices.Select(v => new Vector2(v.x,v.z)).ToList());
            mesh.SetColors(Enumerable.Repeat(Color.red,vertices.Count).ToList());
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh Module(string name, Func<Mesh> create)
        {
            var existing = AssetDatabase.LoadAllAssetsAtPath(SettlementMeshPath).OfType<Mesh>().FirstOrDefault(m => m.name == name);
            var mesh = create();
            if (existing != null)
            {
                Undo.RegisterCompleteObjectUndo(existing,"Update settlement module");
                existing.Clear();
                existing.vertices = mesh.vertices;
                existing.normals = mesh.normals;
                existing.uv = mesh.uv;
                existing.colors = mesh.colors;
                existing.triangles = mesh.triangles;
                existing.RecalculateBounds();
                EditorUtility.SetDirty(existing);
                UnityEngine.Object.DestroyImmediate(mesh);
                return existing;
            }
            if (AssetDatabase.LoadMainAssetAtPath(SettlementMeshPath) == null) AssetDatabase.CreateAsset(mesh, SettlementMeshPath);
            else AssetDatabase.AddObjectToAsset(mesh, SettlementMeshPath);
            return mesh;
        }

        private static Transform Piece(Transform parent, string name, Mesh mesh, Material material,
            Vector3 position, Vector3 size, bool collision = false)
        {
            var go = Group(name, parent);
            go.position = position;
            go.localScale = size;
            go.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
            if (collision) go.gameObject.AddComponent<BoxCollider>();
            return go;
        }

        private static Transform Detail(Transform parent, string name, float x, float y, float height,
            Vector3 size, Material material, Mesh mesh = null, bool collision = false)
            => Piece(parent,name,mesh != null ? mesh : cube,material,Position(x,y,height),size*map.CellSize,collision);

        private static void Beam(Transform parent, string name, Vector3 start, Vector3 end, float width, Material material, bool collision = false)
        {
            var beam = Piece(parent,name,cube,material,(start+end)*0.5f,
                new Vector3(width*map.CellSize,(end-start).magnitude,width*map.CellSize),collision);
            beam.rotation = Quaternion.FromToRotation(Vector3.up,end-start);
        }

        private static void Rock(Transform parent, string name, float x, float y, float height, float width, float tall, float depth, bool collision = false)
        {
            var go = Detail(parent,name,x,y,height,new Vector3(width,tall,depth),rock,boulder,collision);
            go.rotation = Quaternion.Euler(0,(x*37+y*19)%360,0);
        }

        private static void Rail(Transform parent, float x1,float y1,float x2,float y2,float height = 0.12f)
        {
            Vector3 a = Position(x1,y1,height), b = Position(x2,y2,height);
            int count = Mathf.CeilToInt(Vector2.Distance(new Vector2(x1,y1),new Vector2(x2,y2))/1.5f);
            for (int i = 0; i <= count; i++)
            {
                Vector3 p = Vector3.Lerp(a,b,(float)i/count);
                Beam(parent,"HandrailPost",p,p+Vector3.up*map.CellSize*0.9f,0.10f,wood,true);
            }
            foreach (float h in new[] { 0.4f,0.78f })
                Beam(parent,"RopeRail",a+Vector3.up*h*map.CellSize,b+Vector3.up*h*map.CellSize,0.035f,rope);
        }

        private static void SettlementRockwork(Transform architecture)
        {
            var group = Group("00_SculptedCaveRocks",architecture);
            var floors = CaveMap.ReadCells(map.FloorTilemap);
            int index = 0;
            foreach (var cell in floors.OrderBy(c=>c.y).ThenBy(c=>c.x))
            {
                foreach (Vector2Int direction in new[] { Vector2Int.left,Vector2Int.up,Vector2Int.right,Vector2Int.down })
                {
                    if (floors.Contains(cell+direction)) continue;
                    if (++index%3 != 0) continue;
                    float h = map.Elevation.Height(cell.x+0.5f,cell.y+0.5f);
                    float x = cell.x+0.5f+direction.x*0.85f, y = cell.y+0.5f+direction.y*0.85f;
                    float tall = cell.y > 43 ? 0.65f+0.25f*Mathf.Sin(index) : 2.3f+0.6f*Mathf.Sin(index*2.1f);
                    // Position against the nearest authored edge instead of the nonexistent outside cell's height.
                    var go = Piece(group,"BrokenRockShoulder",boulder,rock,
                        map.transform.TransformPoint(map.LayoutGrid.transform.localPosition+new Vector3(x*map.CellSize,h,-y*map.CellSize)),
                        new Vector3(1.45f,tall,1.2f)*map.CellSize);
                    go.rotation = Quaternion.Euler(0,index*47%360,0);
                }
            }
            // Large leaning cliff ribs give the back terrace a continuous, non-tiled silhouette.
            for (int i = 0; i < 12; i++)
            {
                float x = 25.5f+i*2.1f, y = 19.5f+0.7f*Mathf.Sin(i*0.8f);
                Piece(group,"ChieftainCliffButtress",boulder,i%3==0?rockLight:rock,
                    Position(x,y)-Vector3.up*map.Elevation.Height(x,y),
                    new Vector3(1.7f,4.0f+0.35f*Mathf.Sin(i),1.1f)*map.CellSize);
            }
            foreach (Vector2 p in new[] { new Vector2(43,29),new Vector2(48,38),new Vector2(53,29),new Vector2(55,38) })
                Rock(group,"ResidenceRockSpur",p.x,p.y,-0.25f,0.7f,1.2f,0.8f);
        }

        private static void SettlementBridgeBlockout(Transform landform)
        {
            var bridge = Group("RiverCrossing",landform);
            Detail(bridge,"BridgeDeck",13.7f,43.5f,0.025f,new Vector3(11,0.05f,2.8f),wood,null,true);
            Rail(bridge,8.3f,42.1f,19.1f,42.1f,0.03f);
            Rail(bridge,8.3f,44.9f,19.1f,44.9f,0.03f);
        }

        private static void SetSettlementLighting(UnityEngine.SceneManagement.Scene scene)
        {
            var lights = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>()).ToArray();
            foreach (var light in lights.Where(l=>l.type==LightType.Directional))
            {
                Undo.RecordObject(light,"Balance cave key and fill");
                light.color = light.name=="FrontFill" ? new Color(0.72f,0.79f,0.78f) : new Color(0.87f,0.83f,0.72f);
                light.intensity = light.name=="FrontFill" ? 0.55f : 1.05f;
                if (light.name!="FrontFill") light.transform.rotation = Quaternion.Euler(47,-28,0);
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.45f,0.50f,0.48f);
            RenderSettings.ambientEquatorColor = new Color(0.31f,0.35f,0.33f);
            RenderSettings.ambientGroundColor = new Color(0.19f,0.17f,0.13f);
            RenderSettings.fogColor = new Color(0.10f,0.13f,0.13f);
            RenderSettings.fog = false;
            RenderSettings.fogStartDistance = 42;
            RenderSettings.fogEndDistance = 100;
            foreach (var lamp in lights.Where(l=>l.type==LightType.Point))
            {
                Undo.RecordObject(lamp,"Give fires a visible warm pool");
                lamp.intensity = 7;
                PrefabUtility.RecordPrefabInstancePropertyModifications(lamp);
                var flicker = lamp.GetComponent<HybridFireLight>();
                if (flicker == null) continue;
                var settings = new SerializedObject(flicker);
                settings.FindProperty("baseIntensity").floatValue = 7;
                settings.ApplyModifiedProperties();
            }
            var volume = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Volume>()).Single();
            var follow = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CameraController>()).Single();
            Undo.RecordObject(follow,"Frame enlarged cave routes");
            var cameraSettings = new SerializedObject(follow);
            cameraSettings.FindProperty("distance").floatValue = 28;
            cameraSettings.FindProperty("viewAngle").floatValue = 35;
            cameraSettings.FindProperty("focusOffset").vector3Value = new Vector3(0,1.8f,4);
            cameraSettings.ApplyModifiedProperties();
            Undo.RecordObject(follow.GetComponent<Camera>(),"Frame enlarged cave routes");
            follow.GetComponent<Camera>().fieldOfView = 40;
            Undo.RecordObject(volume.sharedProfile,"Keep play area crisp");
            if (volume.sharedProfile.TryGet<DepthOfField>(out var depth))
            {
                depth.aperture.Override(9);
                depth.focalLength.Override(40);
                depth.focusDistance.Override(29);
            }
            if (volume.sharedProfile.TryGet<Bloom>(out var bloom)) bloom.intensity.Override(0.22f);
            if (volume.sharedProfile.TryGet<ColorAdjustments>(out var color))
            {
                color.postExposure.Override(0.35f);
                color.contrast.Override(8);
                color.saturation.Override(-4);
            }
            EditorUtility.SetDirty(volume.sharedProfile);
            DynamicGI.UpdateEnvironment();
        }
    }
}
