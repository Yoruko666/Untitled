using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Untitled.EditorTools
{
    public static class AdventureCaveImporter
    {
        private const string ScenePath = "Assets/Scenes/Gameplay/Adventure.unity";
        private const string ModelPath = "Assets/Art/Models/GoblinCave/goblin_cave_natural.fbx";
        private const string LayoutPath = "Assets/Art/Models/GoblinCave/layout_manifest.json";
        private const string MaterialsFolder = "Assets/Art/Materials/GoblinCave";
        private const string ReportFolder = "output/adventure-cave";

        [MenuItem("Untitled/Adventure/Import Natural Goblin Cave")]
        public static void Import()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            Scene current = SceneManager.GetActiveScene();
            if (current.isDirty) throw new InvalidOperationException("Save the current scene before changing Adventure.");
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = true;
            importer.preserveHierarchy = true;
            importer.isReadable = true;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importNormals = ModelImporterNormals.Import;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
            Directory.CreateDirectory(MaterialsFolder);
            AssetDatabase.Refresh();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader is missing.");
            foreach (Material source in AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Material>())
            {
                string path = MaterialsFolder + "/" + source.name.ToLowerInvariant() + ".mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader) { name = source.name.ToLowerInvariant() };
                    AssetDatabase.CreateAsset(material, path);
                }
                Color color = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : source.color;
                material.SetColor("_BaseColor", color);
                material.SetFloat("_Smoothness", source.name.Contains("Water") ? 0.05f : 0.12f);
                material.SetFloat("_Metallic", 0f);
                EditorUtility.SetDirty(material);
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), source.name), material);
            }
            AssetDatabase.SaveAssets();
            importer.SaveAndReimport();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            // A repeat import replaces only the subtrees owned by this operation.
            foreach (GameObject old in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).Where(go => go.name == "Adventure_GoblinCave" || go.name == "Adventure_CaveRuntime").ToArray())
                Undo.DestroyObjectImmediate(old);
            var level = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
            level.name = "Adventure_GoblinCave";
            Undo.SetTransformParent(level.transform, SceneGroup(scene, "World"), "Organize adventure environment");
            Undo.RegisterCreatedObjectUndo(level, "Import adventure cave");
            foreach (MeshFilter filter in level.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null || filter.name.StartsWith("Water_", StringComparison.Ordinal)) continue;
                var collider = Undo.AddComponent<MeshCollider>(filter.gameObject);
                collider.sharedMesh = filter.sharedMesh;
                collider.convex = false;
                GameObjectUtility.SetStaticEditorFlags(filter.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic);
            }
            Transform[] markers = level.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.StartsWith("ZoneMarker_", StringComparison.Ordinal)).OrderBy(t => t.name).ToArray();
            if (markers.Length != 15) throw new InvalidOperationException("FBX must contain 15 zone markers.");
            if (Mathf.Abs(markers[8].position.y - markers[0].position.y - 22f) > 0.01f)
                throw new InvalidOperationException("FBX units or up-axis are incorrect.");
            var runtime = new GameObject("Adventure_CaveRuntime");
            Undo.RegisterCreatedObjectUndo(runtime, "Create cave scene bindings");
            Undo.SetTransformParent(runtime.transform, SceneGroup(scene, "Systems"), "Organize adventure runtime");
            if (!scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Bootstrap>(true)).Any()) runtime.AddComponent<Bootstrap>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/FirstRoom/GoblinPlayer.prefab");
            var playerObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, runtime.transform);
            playerObject.name = "Player";
            var player = playerObject.GetComponent<PlayerEntity>();
            player.Speed = 5f;
            var playerData = new SerializedObject(player);
            playerData.FindProperty("useGravity").boolValue = true;
            playerData.ApplyModifiedProperties();
            CharacterController controller = player.Controller;
            controller.height = 1.8f; controller.radius = 0.35f; controller.center = Vector3.up * 0.9f;
            controller.stepOffset = 0.28f; controller.slopeLimit = 45f; controller.skinWidth = 0.04f;
            controller.minMoveDistance = 0f;
            playerObject.transform.position = markers[0].position + Vector3.up * 0.15f;
            Camera camera = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>(true)).SingleOrDefault(c => c.CompareTag("MainCamera"));
            if (camera == null) throw new InvalidOperationException("Adventure needs its existing Main Camera.");
            var follow = camera.GetComponent<CameraController>() ?? Undo.AddComponent<CameraController>(camera.gameObject);
            Vector3 north = markers[8].position - markers[0].position;
            north.y = 0f;
            float yaw = Mathf.Atan2(north.x, north.z) * Mathf.Rad2Deg + 30f;
            follow.SetTarget(player.transform); follow.SetViewAngle(55f); follow.SetYawAngle(yaw);
            follow.SetDistance(22f); follow.SetFocusOffset(Vector3.up * 0.9f);
            player.SetMovementCamera(camera.transform);
            camera.orthographic = true; camera.orthographicSize = 10f;
            camera.nearClipPlane = 0.1f; camera.farClipPlane = 300f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.075f,.09f,.105f);
            Vector3 cameraOffset = follow.FocusOffset + Quaternion.Euler(55f,yaw,0f) * Vector3.back * 22f;
            camera.transform.SetPositionAndRotation(player.transform.position + cameraOffset, Quaternion.Euler(55f,yaw,0f));
            SpriteBillboard billboard = player.GetComponentInChildren<SpriteBillboard>();
            var visualData = new SerializedObject(billboard);
            visualData.FindProperty("targetCamera").objectReferenceValue = camera;
            visualData.FindProperty("keepUpright").boolValue = true;
            visualData.ApplyModifiedProperties();
            billboard.transform.localPosition = new Vector3(0f,0.025f,0f);
            var session = runtime.AddComponent<AdventureCaveSession>();
            var sessionData = new SerializedObject(session);
            sessionData.FindProperty("player").objectReferenceValue = player;
            sessionData.FindProperty("followCamera").objectReferenceValue = follow;
            JArray zones = (JArray)JObject.Parse(File.ReadAllText(LayoutPath))["zones"];
            SerializedProperty zoneRefs = sessionData.FindProperty("zoneMarkers"); zoneRefs.arraySize = 15;
            SerializedProperty zoneNames = sessionData.FindProperty("zoneNames"); zoneNames.arraySize = 15;
            for (int i = 0; i < 15; i++)
            {
                zoneRefs.GetArrayElementAtIndex(i).objectReferenceValue = markers[i];
                zoneNames.GetArrayElementAtIndex(i).stringValue = (string)zones[i]["name"];
            }
            sessionData.ApplyModifiedProperties();
            Light sun = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Light>(true)).FirstOrDefault();
            if (sun != null) { sun.intensity = 1.1f; sun.transform.rotation = Quaternion.Euler(50,-30,0); sun.shadows = LightShadows.Soft; }
            RenderSettings.skybox = null; RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.42f,.46f,.49f);
            RenderSettings.ambientEquatorColor = new Color(.27f,.29f,.3f);
            RenderSettings.ambientGroundColor = new Color(.15f,.16f,.18f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(player);
            PrefabUtility.RecordPrefabInstancePropertyModifications(controller);
            PrefabUtility.RecordPrefabInstancePropertyModifications(billboard);
            PrefabUtility.RecordPrefabInstancePropertyModifications(billboard.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Validate();
            Selection.activeGameObject = playerObject;
            Debug.Log("[AdventureCave] Imported, Player and camera connected, scene saved.");
        }

        private static Transform SceneGroup(Scene scene, string name)
        {
            var root = scene.GetRootGameObjects().SingleOrDefault(g => g.name == "SceneRoot");
            if (root == null)
            {
                root = new GameObject("SceneRoot");
                Undo.RegisterCreatedObjectUndo(root, "Create scene root");
            }
            var group = root.transform.Cast<Transform>().SingleOrDefault(t => t.name == name);
            if (group == null)
            {
                var go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, "Create scene group");
                go.transform.SetParent(root.transform, false);
                group = go.transform;
            }
            var order = new[] { "Systems", "Cameras", "World", "Lighting", "UI" };
            int index = 0;
            foreach (string entry in order)
            {
                var child = root.transform.Cast<Transform>().SingleOrDefault(t => t.name == entry);
                if (child != null) child.SetSiblingIndex(index++);
            }
            return group;
        }

        [MenuItem("Untitled/Adventure/Validate Cave Integration")]
        public static void Validate()
        {
            var scene = SceneManager.GetActiveScene();
            var session = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<AdventureCaveSession>(true)).SingleOrDefault();
            if (session == null || session.ZoneMarkers.Length != 15) throw new InvalidOperationException("Missing cave bindings.");
            Transform[] markers = session.ZoneMarkers;
            Vector3 yAxis = (markers[1].position - markers[0].position) / 16f;
            Vector3 xAxis = (markers[12].position - markers[0].position - yAxis * 12f) / 35f;
            Vector3 zAxis = (markers[13].position - markers[0].position - xAxis * 6f - yAxis * 57f) / 9f;
            Vector3 origin = markers[0].position + yAxis * 54f;
            Physics.SyncTransforms();
            var missing = new List<string>();
            int samples = 0;
            JObject layout = JObject.Parse(File.ReadAllText(LayoutPath));
            foreach (JObject segment in (JArray)layout["segments"])
            {
                Vector3 Convert(JToken p) => origin + xAxis * (float)p[0] + yAxis * (float)p[1] + zAxis * (float)p[2];
                Vector3 a = Convert(segment["a"]), b = Convert(segment["b"]);
                float length = Vector2.Distance(new Vector2(a.x,a.z),new Vector2(b.x,b.z));
                int count = Mathf.Max(1,Mathf.CeilToInt(length / .35f));
                for (int i = 0; i <= count; i++)
                {
                    Vector3 p = Vector3.Lerp(a,b,i/(float)count);
                    samples++;
                    if (!Physics.Raycast(p + Vector3.up*.65f,Vector3.down,out RaycastHit hit,1.3f,~0,QueryTriggerInteraction.Ignore)
                        || Mathf.Abs(hit.point.y-p.y)>.3f) missing.Add((string)segment["path"] + " " + p);
                }
            }
            bool target = session.FollowCamera.Target == session.Player.transform;
            if (!target || missing.Count != 0) throw new InvalidOperationException("Cave integration check failed: " + string.Join("; ", missing.Take(8)));
            Directory.CreateDirectory(ReportFolder);
            File.WriteAllText(ReportFolder+"/integration.json", JObject.FromObject(new {
                passed = true, zones = markers.Length, colliderGroundSamples = samples,
                meshColliders = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).Single(go => go.name == "Adventure_GoblinCave").GetComponentsInChildren<MeshCollider>(true).Length,
                cameraTargetLinked = target, sourceXAxis = new[] { xAxis.x,xAxis.y,xAxis.z },
                sourceYAxis = new[] { yAxis.x,yAxis.y,yAxis.z }, sourceZAxis = new[] { zAxis.x,zAxis.y,zAxis.z },
                spawn = new[] { markers[0].position.x,markers[0].position.y,markers[0].position.z },
                playerHeight = session.Player.Controller.height
            }).ToString());
            Debug.Log($"[AdventureCave] Validation passed: 15 zones, {samples} collider ground samples.");
        }
    }
}
