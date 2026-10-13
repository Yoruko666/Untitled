using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Untitled.Editor
{
    public static class GoblinCaveSceneBuilder
    {
        private const string ModelPath = "Assets/Art/Models/GoblinCave/goblin_cave_200x200.fbx";
        private const string ScenePath = "Assets/Scenes/GoblinCave200.unity";
        private const string MaterialFolder = "Assets/Art/Materials/GoblinCave";

        [MenuItem("Untitled/Cave/Create Imported Goblin Cave Scene")]
        public static void Create()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (File.Exists(ScenePath)) throw new InvalidOperationException("GoblinCave200 already exists; edit the saved scene directly.");
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/FirstRoom/GoblinPlayer.prefab");
            if (model == null || playerPrefab == null) throw new InvalidOperationException("Cave model or player prefab is missing.");

            // Additive creation leaves the user's current scene and unsaved edits intact.
            var previousScene = EditorSceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            EditorSceneManager.SetActiveScene(scene);
            Directory.CreateDirectory(MaterialFolder);
            AssetDatabase.Refresh();
            var clay = MakeMaterial("white_clay", new Color(0.82f, 0.82f, 0.82f));
            var water = MakeMaterial("white_water", new Color(0.82f, 0.82f, 0.82f));
            water.SetFloat("_Smoothness", 0.35f);
            EditorUtility.SetDirty(water);
            var cave = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
            cave.name = "GoblinCave_200x200";
            // FBX's forward-axis conversion reverses the plan axes; restore Blender X/Y as Unity X/Z.
            cave.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(cave.transform);
            foreach (var renderer in cave.GetComponentsInChildren<MeshRenderer>())
            {
                bool isWater = renderer.name.Contains("River") || renderer.name.Contains("Waterfall");
                renderer.sharedMaterials = Enumerable.Repeat(isWater ? water : clay, renderer.sharedMaterials.Length).ToArray();
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                if (!isWater)
                {
                    var collider = renderer.gameObject.AddComponent<MeshCollider>();
                    collider.sharedMesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                }
            }

            var spawn = cave.GetComponentsInChildren<Transform>().Single(t => t.name == "PlayerSpawn_Entry");
            Physics.SyncTransforms();
            if (!Physics.Raycast(spawn.position + Vector3.up * 3f, Vector3.down, out var hit, 5f))
                throw new InvalidOperationException("The imported entry marker has no ground beneath it.");
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.name = "GoblinPlayer";
            player.transform.position = hit.point + Vector3.up * 0.05f;
            // The scene sample owns input; the gameplay prefab and its animation remain reusable.
            var entity = player.GetComponent<PlayerEntity>();
            entity.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(entity);
            var controller = player.GetComponent<CharacterController>();
            controller.height = 1.8f;
            controller.center = Vector3.up * 0.9f;
            controller.radius = 0.3f;
            controller.stepOffset = 0.3f;
            controller.slopeLimit = 50f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(controller);
            var movement = player.AddComponent<CaveBlockoutPlayer>();
            var movementData = new SerializedObject(movement);
            movementData.FindProperty("moveSpeed").floatValue = 7.625f;
            movementData.ApplyModifiedPropertiesWithoutUndo();
            var visual = player.GetComponentInChildren<PlayerRunAnimation>().transform;
            visual.localScale = Vector3.one * 2.1f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(visual);

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CameraController));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.fieldOfView = 45f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 400f;
            camera.backgroundColor = new Color(0.16f, 0.18f, 0.20f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            var follow = cameraObject.GetComponent<CameraController>();
            follow.SetTarget(player.transform);
            follow.SetViewAngle(38f);
            follow.SetDistance(20f);
            follow.SetFocusOffset(Vector3.up);
            cameraObject.transform.SetPositionAndRotation(player.transform.position + Vector3.up
                + Quaternion.Euler(38f, 0f, 0f) * Vector3.back * 20f, Quaternion.Euler(38f, 0f, 0f));
            var billboard = player.GetComponentInChildren<SpriteBillboard>();
            var billboardData = new SerializedObject(billboard);
            billboardData.FindProperty("targetCamera").objectReferenceValue = camera;
            billboardData.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(billboard);

            var lighting = new GameObject("Directional Light", typeof(Light));
            lighting.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lighting.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            RenderSettings.sun = light;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.48f, 0.48f, 0.48f);
            RenderSettings.fog = false;
            RenderSettings.skybox = null;
            var boundaries = new GameObject("Map Boundaries");
            AddBoundary(boundaries.transform, "West", new Vector3(-100f, 15f, 0f), new Vector3(1f, 60f, 200f));
            AddBoundary(boundaries.transform, "East", new Vector3(100f, 15f, 0f), new Vector3(1f, 60f, 200f));
            AddBoundary(boundaries.transform, "South", new Vector3(0f, 15f, -100f), new Vector3(200f, 60f, 1f));
            AddBoundary(boundaries.transform, "North", new Vector3(0f, 15f, 100f), new Vector3(200f, 60f, 1f));
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            // Remove the previous scene from the live setup, retaining its dirty state in the Editor.
            EditorSceneManager.CloseScene(previousScene, false);
            Selection.activeGameObject = player;
            SceneView.lastActiveSceneView?.LookAt(player.transform.position, Quaternion.Euler(38f, 0f, 0f), 25f);
            Debug.Log($"GoblinCave200 saved: {cave.GetComponentsInChildren<MeshFilter>().Length} meshes, spawn {player.transform.position}, player movement and camera connected.");
        }

        private static Material MakeMaterial(string name, Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.05f);
            AssetDatabase.CreateAsset(material, MaterialFolder + "/" + name + ".mat");
            return material;
        }

        private static void AddBoundary(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var boundary = new GameObject(name);
            boundary.transform.SetParent(parent, false);
            boundary.transform.position = position;
            boundary.AddComponent<BoxCollider>().size = size;
        }
    }
}
