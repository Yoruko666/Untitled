using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Untitled.Editor
{
    [CustomEditor(typeof(HybridProp))]
    public sealed class HybridPropInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            HybridProp prop = (HybridProp)target;
            EditorGUILayout.LabelField(prop.DisplayName, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(prop.Implementation, MessageType.Info);
            EditorGUI.BeginChangeCheck();
            float size = EditorGUILayout.Slider("整体大小", prop.transform.localScale.x, 0.1f, 3f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(prop.transform, "Scale prop");
                prop.transform.localScale = Vector3.one * size;
                PrefabUtility.RecordPrefabInstancePropertyModifications(prop.transform);
            }
            if (GUILayout.Button("恢复 1 倍大小"))
            {
                Undo.RecordObject(prop.transform, "Reset prop scale");
                prop.ResetScale();
                PrefabUtility.RecordPrefabInstancePropertyModifications(prop.transform);
            }
            EditorGUILayout.LabelField("摆放", prop.WallMounted ? "贴墙放置，手动设置高度" : "底部中心对齐地面");
            EditorGUILayout.Space();
            DrawDefaultInspector();
        }
    }

    public sealed class HybridPropWindow : EditorWindow
    {
        private const string PrefabFolder = "Assets/Prefabs/Props";
        private const string GalleryPath = "Assets/Scenes/Props/HybridPropGallery.unity";
        private const string ComparisonPath = "Assets/Scenes/Props/ContainerComparison.unity";
        private readonly string[] categories = { "全部", "3D 容器", "HD-2D 容器", "厨房", "营地", "图腾骨饰", "照明" };
        private readonly List<HybridProp> prefabs = new List<HybridProp>();
        private Vector2 scroll;
        private string search = "";
        private int selected, category, previewIndex = -1;
        private PreviewRenderUtility preview;
        private GameObject previewObject;
        private float yaw, pitch = 25f, zoom = 1f, scale = 1f;
        private bool rotate;
        private double previousTime;

        [MenuItem("Untitled/Props/素材浏览器")]
        public static void Open() => GetWindow<HybridPropWindow>("可放置素材").Show();

        [MenuItem("Untitled/Props/完整预制体展厅")]
        public static void OpenGallery() => OpenScene(GalleryPath);

        [MenuItem("Untitled/Props/水缸和锅·3D 与 HD-2D 对照")]
        public static void OpenContainerComparison() => OpenScene(ComparisonPath);

        private static void OpenScene(string path)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(path);
            Open();
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.LookAt(Vector3.zero, Quaternion.Euler(38f, 12f, 0f), path == ComparisonPath ? 13f : 20f);
        }

        private void OnEnable()
        {
            minSize = new Vector2(950f, 640f);
            Load();
            EditorApplication.update += Tick;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Tick;
            Dispose();
        }

        private void Load()
        {
            prefabs.Clear();
            foreach (string asset in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(asset));
                if (prefab != null && prefab.TryGetComponent(out HybridProp prop)) prefabs.Add(prop);
            }
            prefabs.Sort((a, b) => string.CompareOrdinal(AssetDatabase.GetAssetPath(a), AssetDatabase.GetAssetPath(b)));
            selected = Mathf.Clamp(selected, 0, Mathf.Max(0, prefabs.Count - 1));
        }

        private void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            if (rotate)
            {
                yaw += (float)(now - previousTime) * 25f;
                Repaint();
            }
            previousTime = now;
        }

        private void Dispose()
        {
            if (previewObject != null) Object.DestroyImmediate(previewObject);
            previewObject = null;
            preview?.Cleanup();
            preview = null;
            previewIndex = -1;
        }

        private bool InCategory(HybridProp prop)
        {
            bool container = prop.AssetId.StartsWith("Cauldron") || prop.AssetId.StartsWith("StoneCistern");
            if (category == 0) return true;
            if (category == 1) return container && !prop.IsPixel;
            if (category == 2) return container && prop.IsPixel;
            string folder = new[] { "Kitchen", "Camp", "TotemsBones", "Lighting" }[category - 3];
            return AssetDatabase.GetAssetPath(prop).Contains("/" + folder + "/");
        }

        private void OnGUI()
        {
            if (prefabs.Count == 0)
            {
                EditorGUILayout.HelpBox("未找到可放置素材。", MessageType.Info);
                if (GUILayout.Button("刷新")) Load();
                return;
            }
            EditorGUILayout.LabelField("可放置素材 / " + prefabs.Count + " 个预制体", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                search = EditorGUILayout.TextField("搜索", search);
                if (GUILayout.Button("容器对照", GUILayout.Width(95f))) OpenContainerComparison();
                if (GUILayout.Button("完整展厅", GUILayout.Width(95f))) OpenGallery();
                if (GUILayout.Button("刷新", GUILayout.Width(60f))) { Load(); Dispose(); }
            }
            category = GUILayout.Toolbar(category, categories);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(230f)))
                {
                    scroll = EditorGUILayout.BeginScrollView(scroll);
                    for (int index = 0; index < prefabs.Count; index++)
                    {
                        HybridProp prop = prefabs[index];
                        if (!InCategory(prop) || (!string.IsNullOrEmpty(search) && !prop.DisplayName.Contains(search)
                            && prop.AssetId.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)) continue;
                        GUI.backgroundColor = selected == index ? new Color(0.54f, 0.8f, 0.78f) : Color.white;
                        if (GUILayout.Button(prop.DisplayName + "\n" + Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(prop)), GUILayout.Height(46f)))
                        {
                            selected = index;
                            yaw = 0f;
                            pitch = prop.IsPixel ? 25f : 35f;
                            scale = zoom = 1f;
                        }
                    }
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.EndScrollView();
                }
                using (new EditorGUILayout.VerticalScope())
                {
                    HybridProp prop = prefabs[selected];
                    EditorGUILayout.LabelField(prop.DisplayName, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(prop.IsPixel
                        ? "受光像素面片 / " + (prop.VerticalOnly ? "仅上下跟随相机" : "沿竖直轴朝向相机")
                            + (prop.HasEightDirections ? " / 固定方向帧 " + prop.DirectionFrame : "")
                        : "原图贴图 / 连续封闭 3D 网格");
                    Rect rect = GUILayoutUtility.GetRect(200f, 300f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                    DrawPreview(rect, prop);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        rotate = GUILayout.Toggle(rotate, "自动转台", GUILayout.Width(95f));
                        foreach (int angle in new[] { 0, 45, 90, 180 })
                            if (GUILayout.Button(angle + "°", GUILayout.Width(48f))) { yaw = angle; Repaint(); }
                        if (GUILayout.Button("复原", GUILayout.Width(65f))) { yaw = 0f; pitch = 25f; zoom = scale = 1f; }
                    }
                    scale = EditorGUILayout.Slider("预览与放置大小", scale, 0.25f, 2.5f);
                    EditorGUILayout.LabelField("左键拖动换角度，滚轮缩放；自动转台可检查方向切换。");
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        DrawReference(prop);
                        using (new EditorGUILayout.VerticalScope(GUILayout.Width(250f)))
                        {
                            EditorGUILayout.LabelField("原图 / 当前方向", EditorStyles.boldLabel);
                            EditorGUILayout.LabelField("默认高度 " + prop.DefaultSize.y.ToString("0.00") + " 米");
                            EditorGUILayout.LabelField(prop.WallMounted ? "贴墙放置，自由调高度" : "底部中心为放置支点");
                            if (prop.IsPixel && !prop.HasEightDirections)
                                EditorGUILayout.HelpBox("原图只有一面；转动时保持同一完整轮廓。", MessageType.None);
                            if (GUILayout.Button("定位 Prefab")) { Selection.activeObject = prop.gameObject; EditorGUIUtility.PingObject(prop.gameObject); }
                            if (GUILayout.Button("放入当前场景")) Place(prop);
                            if (GUILayout.Button("打开 Prefab")) AssetDatabase.OpenAsset(prop.gameObject);
                        }
                    }
                }
            }
        }

        private void DrawReference(HybridProp prop)
        {
            MeshRenderer renderer = prop.Visual != null ? prop.Visual.GetComponent<MeshRenderer>() : null;
            Texture2D source = renderer != null ? renderer.sharedMaterial.GetTexture("_BaseMap") as Texture2D : null;
            Rect rect = GUILayoutUtility.GetRect(180f, 160f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.12f, 0.15f, 0.18f));
            if (source == null) return;
            if (!prop.IsPixel) { GUI.DrawTexture(rect, source, ScaleMode.ScaleToFit, true); return; }
            HybridProp instance = previewObject != null ? previewObject.GetComponent<HybridProp>() : prop;
            Vector4 uv = prop.Frames[instance.CurrentFrame].UV;
            float ratio = source.width * uv.z / (source.height * uv.w);
            float width = Mathf.Min(rect.width - 12f, (rect.height - 12f) * ratio), height = width / ratio;
            GUI.DrawTextureWithTexCoords(new Rect(rect.center.x - width / 2f, rect.center.y - height / 2f, width, height), source, new Rect(uv.x, uv.y, uv.z, uv.w), true);
        }

        private void DrawPreview(Rect rect, HybridProp prop)
        {
            if (previewIndex != selected || previewObject == null)
            {
                Dispose();
                preview = new PreviewRenderUtility();
                previewObject = Object.Instantiate(prop.gameObject);
                preview.AddSingleGO(previewObject);
                previewIndex = selected;
            }
            Event current = Event.current;
            if (rect.Contains(current.mousePosition))
            {
                if (current.type == EventType.MouseDrag && current.button == 0)
                {
                    yaw += current.delta.x * 0.7f;
                    pitch = Mathf.Clamp(pitch + current.delta.y * 0.4f, 0f, 75f);
                    current.Use();
                    Repaint();
                }
                if (current.type == EventType.ScrollWheel)
                {
                    zoom = Mathf.Clamp(zoom + current.delta.y * 0.05f, 0.5f, 2f);
                    current.Use();
                    Repaint();
                }
            }
            previewObject.transform.localScale = Vector3.one * scale;
            if (Event.current.type != EventType.Repaint) return;
            Camera camera = preview.camera;
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(0.5f, prop.DefaultSize.y * 0.68f, prop.DefaultSize.x * 0.68f)
                * zoom * Mathf.Max(1f, rect.height / rect.width);
            camera.aspect = rect.width / rect.height;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.16f, 0.19f);
            camera.allowHDR = false;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            camera.transform.SetPositionAndRotation(Vector3.up * prop.DefaultSize.y * 0.5f + rotation * Vector3.back * 8f, rotation);
            previewObject.GetComponent<HybridProp>().ApplyCamera(camera);
            preview.lights[0].intensity = 1.1f;
            preview.lights[0].transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            preview.lights[1].intensity = 0.35f;
            preview.lights[1].color = new Color(0.68f, 0.8f, 1f);
            preview.lights[1].transform.rotation = Quaternion.Euler(30f, 135f, 0f);
            preview.ambientColor = new Color(0.4f, 0.44f, 0.48f);
            preview.BeginPreview(rect, GUIStyle.none);
            preview.Render(true);
            GUI.DrawTexture(rect, preview.EndPreview(), ScaleMode.StretchToFill, false);
        }

        private void Place(HybridProp prop)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prop.gameObject);
            Undo.RegisterCreatedObjectUndo(instance, "Place prop");
            Vector3 point = Vector3.zero;
            if (SceneView.lastActiveSceneView != null)
            {
                Ray ray = SceneView.lastActiveSceneView.camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
                {
                    point = hit.point;
                    if (prop.WallMounted && Mathf.Abs(hit.normal.y) < 0.5f)
                    {
                        Vector3 normal = hit.normal; normal.y = 0f;
                        instance.transform.rotation = Quaternion.LookRotation(-normal.normalized, Vector3.up);
                    }
                }
                else { point = SceneView.lastActiveSceneView.pivot; point.y = 0f; }
            }
            instance.transform.position = point;
            instance.transform.localScale = Vector3.one * scale;
            Selection.activeGameObject = instance;
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
        }

        [MenuItem("Untitled/Props/检查预制体引用")]
        public static void CheckReferences()
        {
            var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder });
            if (prefabs.Length != 41) throw new InvalidOperationException("Expected 41 imported prop prefabs.");
            int meshes = 0, pixels = 0;
            foreach (string guid in prefabs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                CheckMissingScripts(prefab);
                HybridProp prop = prefab.GetComponent<HybridProp>();
                if (prop == null || prop.Visual == null || prop.AssetId.Length == 0 || prop.ReferenceImages.Any(image => image == null)
                    || prefab.GetComponent<BoxCollider>() == null) throw new InvalidOperationException("Missing reference: " + path + "; prop=" + (prop != null) + "; id=" + (prop != null ? prop.AssetId : "null") + "; visual=" + (prop != null && prop.Visual != null) + "; collider=" + (prefab.GetComponent<BoxCollider>() != null) + "; images=" + (prop != null ? string.Join(",", prop.ReferenceImages.Select(image => image != null ? image.name : "NULL")) : "null"));
                MeshFilter filter = prop.Visual.GetComponent<MeshFilter>();
                MeshRenderer renderer = prop.Visual.GetComponent<MeshRenderer>();
                if (filter == null || filter.sharedMesh == null || renderer == null || renderer.sharedMaterial == null
                    || renderer.sharedMaterial.shader == null || renderer.sharedMaterial.GetTexture("_BaseMap") == null)
                    throw new InvalidOperationException("Missing mesh/material/texture: " + path);
                if (prop.IsPixel) pixels++; else meshes++;
                foreach (PixelFrame frame in prop.Frames)
                {
                    Vector4 uv = frame.UV;
                    if (uv.z <= 0f || uv.w <= 0f || uv.x + uv.z > 1.0001f || uv.y + uv.w > 1.0001f)
                        throw new InvalidOperationException("Invalid saved directional frame: " + path);
                }
            }
            foreach (string path in new[] { GalleryPath, ComparisonPath })
            {
                var scene = EditorSceneManager.OpenScene(path);
                foreach (GameObject root in scene.GetRootGameObjects()) CheckMissingScripts(root);
                HybridGalleryViewer viewer = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<HybridGalleryViewer>()).Single();
                int expected = path == GalleryPath ? 41 : 10;
                if (viewer.ViewCamera == null || viewer.Props.Length != expected || viewer.Props.Any(prop => prop == null))
                    throw new InvalidOperationException("Invalid preview scene references: " + path);
            }
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/hd2d_pixel_prop.shader");
            if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Pixel shader import failed.");
            string result = "PASS: 41 prefabs; " + meshes + " continuous 3D meshes; " + pixels + " HD-2D cards; saved directional frames; valid mesh/material/texture/script references; both preview scenes imported.\n";
            Directory.CreateDirectory("Temp");
            File.WriteAllText("Temp/props_import_check.txt", result);
            Debug.Log(result);
        }

        private static void CheckMissingScripts(GameObject root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0)
                    throw new InvalidOperationException("Missing script on " + child.name);
        }
    }
}
