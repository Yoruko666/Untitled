using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Untitled.Editor
{
    public static partial class CaveHD2DSceneBuilder
    {
        private static void SettlementStealthBlockout(Transform landform)
        {
            Transform root = Group("StealthBlockout", landform);
            Mesh block = Module("StealthCoverBlock", () =>
            {
                Mesh mesh = Object.Instantiate(cube);
                mesh.name = "StealthCoverBlock";
                mesh.colors = Enumerable.Repeat(Color.red, mesh.vertexCount).ToArray();
                return mesh;
            });
            Mesh roof = Module("StealthShelterRoof", () =>
            {
                var vertices = new System.Collections.Generic.List<Vector3>();
                void Tri(Vector3 a, Vector3 b, Vector3 c) => vertices.AddRange(new[] { a,b,c });
                Vector3 a = new Vector3(-0.5f,0,-0.5f), b = new Vector3(0.5f,0,-0.5f);
                Vector3 c = new Vector3(0.5f,0,0.5f), d = new Vector3(-0.5f,0,0.5f);
                Vector3 e = new Vector3(0,1,-0.5f), f = new Vector3(0,1,0.5f);
                Tri(a,f,e); Tri(a,d,f); Tri(e,c,b); Tri(e,f,c); Tri(a,e,b); Tri(d,c,f);
                return MeshFrom("StealthShelterRoof", vertices);
            });
            void Cover(Transform area, string name, float x, float y, Vector3 size, Material material, bool shelter = false)
            {
                Transform cover = Piece(area, name, block, material, Position(x,y)+Vector3.up*size.y*0.5f, size, true);
                if (shelter) Piece(area, name+"Roof", roof, material, cover.position+Vector3.up*size.y*0.5f,
                    new Vector3(size.x+0.5f,0.9f,size.z+0.5f));
            }
            Transform bridge = Group("01_BridgeObservation", root);
            Cover(bridge,"WestBankObservationRock",12.5f,39,new Vector3(3,2.6f,3),stone);
            Cover(bridge,"EastBankObservationCargo",26,45.7f,new Vector3(3,2.6f,2.5f),wood);
            Transform supply = Group("02_WorkshopAndKitchen", root);
            Cover(supply,"WorkshopWall",7.5f,34,new Vector3(4,2.8f,3),wood,true);
            Cover(supply,"WorkshopCargo",12.8f,33,new Vector3(4,2.6f,3),wood);
            Cover(supply,"WorkshopRock",8.5f,29,new Vector3(3.5f,2.8f,3),stone);
            Cover(supply,"SupplyBridgeObservation",13.5f,23.3f,new Vector3(3,2.8f,2.8f),wood);
            Cover(supply,"KitchenFurnace",30.5f,25.5f,new Vector3(5,3.4f,4),stone);
            Cover(supply,"KitchenRearCargo",33,22.5f,new Vector3(4.5f,2.8f,3),wood);
            Cover(supply,"KitchenSideCargo",35.5f,27,new Vector3(4,2.6f,3),wood);
            Transform plaza = Group("03_PublicLifeAndStorage",root);
            Cover(plaza,"PlazaSouthRock",35,42,new Vector3(3.5f,2.6f,2.8f),stone);
            Cover(plaza,"PlazaWestCargo",34,35,new Vector3(3.5f,2.6f,2.5f),wood);
            Cover(plaza,"PlazaNorthRock",40,34,new Vector3(3,2.8f,2.5f),stone);
            Cover(plaza,"StoragePerimeterCargo",47,46,new Vector3(4,3,4),wood);
            Cover(plaza,"StorageRearCargo",53,44,new Vector3(4,2.8f,3),wood);
            Transform homes = Group("04_HomeCourtsAndAlleys",root);
            Cover(homes,"FrontHomeWest",53.5f,38,new Vector3(4.5f,2.8f,4),wood,true);
            Cover(homes,"FrontHomeEast",57,38,new Vector3(3,2.8f,4),wood,true);
            Cover(homes,"FrontHomeNorth",54,33.5f,new Vector3(5,2.8f,2.2f),wood,true);
            Cover(homes,"RearHomeWest",48.5f,27,new Vector3(4.2f,2.8f,3.2f),wood,true);
            Cover(homes,"RearHomeEast",53,27.5f,new Vector3(4,2.8f,3),wood,true);
            Transform hall = Group("05_HallAndElder",root);
            Cover(hall,"HallSupplyEntryCover",36,12.5f,new Vector3(3,2.8f,3),stone);
            Cover(hall,"HallFrontEntryCover",48.5f,13,new Vector3(3,2.6f,3),stone);
            Cover(hall,"HallCourtDivider",44,12.5f,new Vector3(4,3,4),wood);
            Cover(hall,"ElderObservationCover",61,11,new Vector3(2.6f,2.6f,2.6f),stone);
        }

        private static void ConfigureStealthPresentation(Scene scene, CameraController follow, PlayerEntity player)
        {
            Undo.RecordObject(follow,"Frame stealth routes");
            var settings = new SerializedObject(follow);
            settings.FindProperty("viewAngle").floatValue = 42;
            settings.FindProperty("distance").floatValue = 32;
            settings.FindProperty("focusOffset").vector3Value = new Vector3(0,1.6f,2);
            settings.ApplyModifiedProperties();
            var camera = follow.GetComponent<Camera>();
            Undo.RecordObject(camera,"Keep terrain and cover readable");
            camera.orthographic = false;
            camera.fieldOfView = 34;
            var reveal = map.GetComponent<CaveWallReveal>();
            if (reveal == null) reveal = Undo.AddComponent<CaveWallReveal>(map.gameObject);
            var visibility = new SerializedObject(reveal);
            visibility.FindProperty("viewCamera").objectReferenceValue = camera;
            visibility.FindProperty("targetVisual").objectReferenceValue = player.GetComponentsInChildren<Renderer>()
                .First(r => r is MeshRenderer || r is SpriteRenderer);
            visibility.FindProperty("revealRadius").floatValue = 1.3f;
            visibility.ApplyModifiedProperties();
            var volume = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UnityEngine.Rendering.Volume>()).Single();
            const string profilePath = "Assets/Settings/CaveHD2DLocalPresentation.asset";
            var profile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(profilePath);
            if (profile == null)
            {
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(volume.sharedProfile),profilePath);
                profile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(profilePath);
            }
            Undo.RecordObject(volume,"Save local cave presentation");
            volume.sharedProfile = profile;
            if (profile.TryGet<DepthOfField>(out var depth))
            {
                Undo.RecordObject(depth,"Keep active stealth space in focus");
                depth.mode.Override(DepthOfFieldMode.Gaussian);
                depth.gaussianStart.Override(45);
                depth.gaussianEnd.Override(70);
                depth.gaussianMaxRadius.Override(0.5f);
                EditorUtility.SetDirty(depth);
            }
            EditorUtility.SetDirty(profile);
        }
    }
}
