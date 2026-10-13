using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Untitled.Editor
{
    public static partial class RitualHallDemoBuilder
    {
        // Wrap existing source images with the same HybridProp/point-sampled shader as the library.
        // Original PNGs and existing shared library prefabs are never modified here.
        private static void PrepareSupplementalPixelProps()
        {
            CreatePixelProp("supplies_crate_hd2d", "Ritual_Wooden_Crates", "像素供品箱垛", "Storage/wooden_crate_8view_sheet.png", 8, 1.15f, .75f, false);
            CreatePixelProp("offering_barrel_hd2d", "Ritual_Wooden_Barrel", "像素供品木桶", "Tavern/wooden_barrel_8view_sheet.png", 8, 1.40f, .85f, false);
            CreatePixelProp("iron_oil_lantern_hd2d", "Ritual_Iron_Lantern", "像素铁提灯", "Lighting/iron_lantern.png", 1, .75f, .25f, true);
            CreatePixelProp("tribute_treasure_hoard_hd2d", "Ritual_Treasure_Offering", "像素金币宝石贡品", "Treasure/gold_gems_pile.png", 1, .62f, .40f, false);
        }

        private static void CreatePixelProp(string name, string id, string label, string source, int count, float height, float depth, bool lit)
        {
            string texturePath = "Assets/Art/Sprites/Props/" + source;
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null) throw new InvalidOperationException("Missing pixel source: " + texturePath);
            var scan = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!scan.LoadImage(File.ReadAllBytes(texturePath))) throw new InvalidOperationException("Cannot read pixel source: " + texturePath);
            var rects = new RectInt[count];
            int maxWidth = 0, maxHeight = 0, cellWidth = scan.width / count;
            Color32[] pixels = scan.GetPixels32();
            for (int frame = 0; frame < count; frame++)
            {
                int left = frame * cellWidth, right = left + cellWidth;
                int minX = right, minY = scan.height, maxX = left - 1, maxY = -1;
                for (int y = 0; y < scan.height; y++) for (int x = left; x < right; x++)
                {
                    if (pixels[y * scan.width + x].a < 128) continue;
                    minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                }
                if (maxX < minX) throw new InvalidOperationException("Empty pixel frame: " + name);
                rects[frame] = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
                maxWidth = Mathf.Max(maxWidth, rects[frame].width); maxHeight = Mathf.Max(maxHeight, rects[frame].height);
            }
            float width = maxWidth * height / maxHeight;
            var mesh = new Mesh { name = name };
            mesh.vertices = new[] { V(-width / 2, 0, 0), V(width / 2, 0, 0), V(width / 2, height, 0), V(-width / 2, height, 0) };
            mesh.normals = Enumerable.Repeat(Vector3.back, 4).ToArray();
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 }; mesh.RecalculateBounds();
            mesh = SaveAsset(mesh, Models + "/" + name + ".asset");
            var material = new Material(Shader.Find("Untitled/HD2D Pixel Prop")) { name = name };
            material.SetTexture("_BaseMap", texture); material.SetFloat("_Cutoff", .45f); material.SetFloat("_Lighting", .65f);
            material.SetFloat("_WhiteKey", 0); material.SetFloat("_Emission", lit ? 1.1f : 0);
            material.SetFloat("_FlameMin", .22f); material.SetFloat("_FlameMax", .64f);
            RectInt first = rects[0];
            material.SetVector("_FrameRect", new Vector4((float)first.x / scan.width, (float)first.y / scan.height, (float)first.width / scan.width, (float)first.height / scan.height));
            material = SaveAsset(material, Materials + "/" + name + ".mat");
            Transform root = Group(name, null), visual = Group("Visual", root);
            visual.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = visual.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.TwoSided; renderer.receiveShadows = true;
            BoxCollision(root, V(0, height / 2, 0), V(width, height, depth));
            HybridProp prop = root.gameObject.AddComponent<HybridProp>();
            var serialized = new SerializedObject(prop);
            serialized.FindProperty("assetId").stringValue = id;
            serialized.FindProperty("displayName").stringValue = label;
            serialized.FindProperty("category").stringValue = lit ? "照明" : "仪式陈设";
            serialized.FindProperty("implementation").stringValue = "复用项目原始像素图；透明裁切、点采样与 3D 受光；固定摆放朝向和方向帧。";
            serialized.FindProperty("sourceFiles").stringValue = texturePath;
            serialized.FindProperty("defaultSize").vector3Value = V(width, height, depth);
            serialized.FindProperty("visual").objectReferenceValue = visual;
            serialized.FindProperty("directionFrame").intValue = 0;
            SerializedProperty references = serialized.FindProperty("referenceImages"); references.arraySize = 1;
            references.GetArrayElementAtIndex(0).objectReferenceValue = texture;
            SerializedProperty frames = serialized.FindProperty("frames"); frames.arraySize = count;
            for (int i = 0; i < count; i++)
            {
                RectInt r = rects[i]; SerializedProperty f = frames.GetArrayElementAtIndex(i);
                f.FindPropertyRelative("x").floatValue = (float)r.x / scan.width;
                f.FindPropertyRelative("y").floatValue = (float)r.y / scan.height;
                f.FindPropertyRelative("w").floatValue = (float)r.width / scan.width;
                f.FindPropertyRelative("h").floatValue = (float)r.height / scan.height;
                f.FindPropertyRelative("sx").floatValue = (float)r.width / maxWidth;
                f.FindPropertyRelative("sy").floatValue = (float)r.height / maxHeight;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo(); prop.ApplyVisual();
            if (lit) Fire(root, "OilFlame", V(0, height * .46f, -.08f), .65f, 2.2f, new Color(1, .49f, .14f));
            PrefabUtility.SaveAsPrefabAsset(root.gameObject, Prefabs + "/" + name + ".prefab");
            Object.DestroyImmediate(root.gameObject); Object.DestroyImmediate(scan);
        }

        private static void SetPixelLamp(Transform root, float intensity, float range)
        {
            Light light = root.GetComponentInChildren<Light>();
            if (light == null) throw new InvalidOperationException("Missing pixel light: " + root.name);
            light.intensity = intensity; light.range = range; light.shadows = LightShadows.None;
            HybridFireLight flicker = light.GetComponent<HybridFireLight>();
            if (flicker != null)
            {
                var settings = new SerializedObject(flicker);
                settings.FindProperty("baseIntensity").floatValue = intensity;
                settings.FindProperty("baseRange").floatValue = range / Mathf.Max(.01f, light.transform.lossyScale.x);
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static Transform WitnessStrawMat(Transform parent, Vector3 center)
        {
            const float scale = 1.45f;
            Transform mat = Reuse(parent, "Camp/straw_bed_hd2d", center + V(0, .025f * scale, -.75f * scale / 2), scale);
            mat.localRotation = Quaternion.Euler(90, 0, 0);
            return mat;
        }

        public static void ApplyHD2DLibrary()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            PrepareSupplementalPixelProps();
            string roomPath = Prefabs + "/goblin_ritual_hall_room.prefab";
            GameObject room = PrefabUtility.LoadPrefabContents(roomPath);
            int changed = 0;
            try
            {
                Transform[] roots = room.GetComponentsInChildren<Transform>().Where(t => PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)).ToArray();
                foreach (Transform old in roots)
                {
                    string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(old.gameObject);
                    string replacement = null;
                    Vector3 position = old.localPosition;
                    float scale = old.localScale.x, yaw = 0;
                    bool straw = false;
                    switch (path)
                    {
                        case Prefabs + "/bone_bound_procession_torch_3d.prefab": replacement = "Lighting/bone_fire_stand_hd2d"; scale = 1.30f; break;
                        case Prefabs + "/ancestor_sigil_hide_banner_3d.prefab": replacement = "TotemsBones/rune_banner_hd2d"; position.y = 1.47f; position.z = 12.51f; scale = 1.45f; break;
                        case Prefabs + "/horned_ancestor_goblin_idol_3d.prefab": replacement = "TotemsBones/beast_totem_hd2d"; position = V(0, 1.0f, 12.50f); scale = 1.85f; break;
                        case Prefabs + "/hanging_ancestor_bone_chimes_3d.prefab": replacement = "TotemsBones/ring_training_rack_hd2d"; scale = 1.45f; break;
                        case Prefabs + "/mushroom_and_roots_offering_3d.prefab": replacement = "Kitchen/produce_pixel_hd2d"; scale = .80f; break;
                        case Prefabs + "/skull_and_candles_offering_3d.prefab": replacement = "TotemsBones/beast_totem_hd2d"; scale = old.parent.name.StartsWith("10_") ? .30f : .34f; break;
                        case Prefabs + "/jade_and_coins_offering_3d.prefab": replacement = "RitualHall/tribute_treasure_hoard_hd2d"; scale = .90f; break;
                        case Prefabs + "/elder_grey_woven_kneeling_mat_3d.prefab": straw = old.parent.name.StartsWith("08_"); break;
                        case "Assets/Prefabs/Props/Kitchen/roots_and_mushrooms_tub_3d.prefab": replacement = "Kitchen/produce_pixel_hd2d"; scale = 1.25f; break;
                        case "Assets/Prefabs/Props/Tavern/supply_crate_3d.prefab": replacement = "RitualHall/supplies_crate_hd2d"; break;
                        case "Assets/Prefabs/Props/Tavern/ale_barrel_3d.prefab": replacement = "RitualHall/offering_barrel_hd2d"; break;
                        case "Assets/Prefabs/Props/Tavern/oil_lantern_3d.prefab": replacement = "RitualHall/iron_oil_lantern_hd2d"; break;
                        case "Assets/Prefabs/Props/Tavern/wall_brazier_3d.prefab": replacement = "Lighting/wall_torch_stone_hd2d"; yaw = -90; break;
                    }
                    if (replacement == null && !straw) continue;
                    Light oldLight = old.GetComponentInChildren<Light>();
                    Transform next = straw ? WitnessStrawMat(old.parent, position) : Reuse(old.parent, replacement, position, scale, yaw);
                    next.name = old.name;
                    if (oldLight != null) SetPixelLamp(next, oldLight.intensity, oldLight.range);
                    if (replacement == "Lighting/bone_fire_stand_hd2d")
                    {
                        BoxCollider c = next.GetComponent<BoxCollider>(); c.size = V(.64f, 2, .50f);
                    }
                    if (replacement == "TotemsBones/rune_banner_hd2d" || (replacement == "TotemsBones/beast_totem_hd2d" && position.z > 12))
                    {
                        BoxCollider c = next.GetComponent<BoxCollider>(); c.size = V(c.size.x, c.size.y, .05f);
                    }
                    Object.DestroyImmediate(old.gameObject); changed++;
                }
                PrefabUtility.SaveAsPrefabAsset(room, roomPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(room); }
            AssetDatabase.SaveAssets(); EditorSceneManager.OpenScene(ScenePath);
            Capture();
            Debug.Log("RITUAL_HD2D_REPLACEMENT_OK: " + changed + " replaced instances; original PNGs and shared prefabs preserved.");
        }
    }
}
