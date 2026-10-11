using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Untitled.Editor
{
    public static partial class CaveHD2DSceneBuilder
    {
        // Explicit replacement only. The ordinary Build operation preserves saved edits.
        [MenuItem("Untitled/Cave/Replace With Terraced Settlement Layout")]
        public static void BuildTerracedSettlement()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before replacing the cave layout.");
            var active = EditorSceneManager.GetActiveScene();
            if (active.isDirty) throw new InvalidOperationException("Save the current scene before applying a new layout.");
            var scene = active.path == ScenePath ? active : EditorSceneManager.OpenScene(ScenePath);
            map = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CaveMap>()).Single();
            if (!map.HasEditableLayout || !map.TryGetComponent<CaveTerrainStyle>(out var style))
                throw new InvalidOperationException("The existing CaveHD2D scene and terrain materials are required.");
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Build illustrated goblin settlement");
            ReplaceSettlementLayout();
            Undo.RecordObject(style, "Sculpt cave boundary");
            style.RockHeight = 2.8f;
            style.RockVariation = 0.7f;
            style.RockMargin = 3;
            style.NaturalOutline = true;
            EditorUtility.SetDirty(style);
            PrepareSettlementMaterials(style);
            CaveTerrainBuilder.Rebuild(map, style);
            Transform root = map.transform.parent;
            // Keep the already placed dressing as an inactive draft while reviewing terrain.
            foreach (Transform draft in new[] { map.transform.Find("Architecture"), root.Find("PlacedProps") })
                if (draft != null)
                {
                    Undo.RecordObject(draft.gameObject,"Review terrain without dressing");
                    draft.gameObject.SetActive(false);
                }
            Transform oldLandform = map.transform.Find("Landform");
            if (oldLandform != null) Undo.DestroyObjectImmediate(oldLandform.gameObject);
            Transform landform = Group("Landform", map.transform);
            cube = PrimitiveMesh(PrimitiveType.Cube, "StoneBlock");
            column = PrimitiveMesh(PrimitiveType.Cylinder, "StoneColumn");
            PrepareSettlementModules();
            SettlementRockwork(landform);
            SettlementBridgeBlockout(landform);
            SetPresentation(scene);
            ApplySceneLighting();
            SetSettlementLighting(scene);
            var player = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PlayerEntity>()).Single();
            var playerSettings = new SerializedObject(player);
            playerSettings.FindProperty("useGravity").boolValue = true;
            playerSettings.ApplyModifiedProperties();
            Undo.RecordObject(player.transform, "Position player at settlement entrance");
            player.transform.position = map.SpawnPosition;
            PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
            var controller = player.GetComponent<CharacterController>();
            Undo.RecordObject(controller, "Support short terrace steps");
            controller.stepOffset = 0.18f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(controller);
            var follow = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CameraController>()).Single();
            Quaternion rotation = Quaternion.Euler(follow.ViewAngle, 0, 0);
            follow.transform.SetPositionAndRotation(player.transform.position + new Vector3(0, 1.8f, 4)
                - rotation * Vector3.forward * follow.Distance, rotation);
            Selection.activeGameObject = null;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.sceneLighting = true;
                SceneView.lastActiveSceneView.LookAt(Position(32, 28, 2), Quaternion.Euler(34, -6, 0), 80f);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            Undo.CollapseUndoOperations(group);
            Debug.Log($"Terrain V2 saved: {CaveMap.ReadCells(map.FloorTilemap).Count} cells; "
                + "river -4 / plaza 0 / homes +2.4,+4.8 / chieftain +10.4. Dressing draft hidden for terrain review.");
        }

        private static void ReplaceSettlementLayout()
        {
            var surfaces = new Dictionary<Vector2Int, CaveElevation.CellSurface>();
            void Flat(int x, int y, float height, int material)
            {
                var cell = new Vector2Int(x, y);
                surfaces[cell] = new CaveElevation.CellSurface { Cell = cell, Heights = Vector4.one * height, Material = material };
            }
            void Polygon(float height, int material, params Vector2[] points)
            {
                for (int y = 4; y < 55; y++)
                    for (int x = 1; x < 66; x++)
                    {
                        var p = new Vector2(x + 0.5f, y + 0.5f);
                        bool inside = false;
                        for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
                            if ((points[i].y > p.y) != (points[j].y > p.y)
                                && p.x < (points[j].x - points[i].x) * (p.y - points[i].y)
                                / (points[j].y - points[i].y) + points[i].x) inside = !inside;
                        if (inside) Flat(x, y, height, material);
                    }
            }
            void Rectangle(int xMin, int yMin, int xMax, int yMax, Func<float, float, float> height)
            {
                for (int y = yMin; y < yMax; y++)
                    for (int x = xMin; x < xMax; x++)
                    {
                        var cell = new Vector2Int(x, y);
                        surfaces[cell] = new CaveElevation.CellSurface
                        {
                            Cell = cell, Material = 0,
                            Heights = new Vector4(height(x, y), height(x + 1, y), height(x + 1, y + 1), height(x, y + 1))
                        };
                    }
            }
            void Ribbon(int yMin, int yMax, Func<float,float> center, Func<float,float> height)
            {
                for (int y = yMin; y < yMax; y++)
                    for (int x = 53; x < 66; x++)
                        if (Mathf.Abs(x+0.5f-center(y+0.5f)) <= 2.2f)
                            Rectangle(x,y,x+1,y+1,(xx,yy)=>height(yy));
            }
            Vector2 P(float x, float y) => new Vector2(x, y);
            Polygon(0, 1, P(23,21), P(31,20), P(39,21), P(44,20), P(52,22), P(58,27),
                P(59,36), P(62,41), P(61,48), P(56,51), P(46,50), P(43,54), P(37,54),
                P(33,49), P(28,48), P(25,43), P(21,39), P(22,32), P(20,28));
            Polygon(-2, 2, P(4,13), P(9,10), P(17,12), P(22,17), P(23,25), P(20,29),
                P(23,35), P(23,44), P(21,49), P(14,52), P(7,49), P(3,44), P(2,37), P(4,31), P(2,23));
            Polygon(5.2f, 3, P(26,7), P(31,4), P(43,5), P(52,6), P(56,11), P(55,16),
                P(51,19), P(44,18), P(40,20), P(30,19), P(25,16), P(24,11));
            Polygon(1.2f, 1, P(44,27), P(50,25), P(56,27), P(56,37), P(51,39), P(45,37), P(43,32));
            Polygon(2.4f, 1, P(43,22), P(48,20), P(54,21), P(55,27), P(51,29), P(44,28), P(42,25));
            Rectangle(17, 33, 27, 37, (x,y) => Mathf.Lerp(-2, 0, (x - 17) / 10));
            Rectangle(17, 44, 29, 48, (x,y) => Mathf.Lerp(-2, 0, (x - 17) / 12));
            Rectangle(43, 35, 47, 41, (x,y) => (41 - y) * 0.2f);
            Rectangle(44, 27, 48, 33, (x,y) => 1.2f + (33 - y) * 0.2f);
            Ribbon(21,35,y => y > 30 ? Mathf.Lerp(59.5f,57.5f,(y-30)/5)
                : y > 25 ? Mathf.Lerp(60.5f,59.5f,(y-25)/5) : Mathf.Lerp(58.5f,60.5f,(y-21)/4),
                y => (35-y)*(2.6f/14));
            Rectangle(56, 18, 65, 21, (x,y) => 2.6f);
            Ribbon(8,18,y => y > 14 ? 63 : Mathf.Lerp(60.5f,63,(y-8)/6), y => 2.6f+(18-y)*0.26f);
            Rectangle(52, 6, 64, 8, (x,y) => 5.2f);
            var rivers = new HashSet<Vector2Int>();
            foreach (Vector2Int cell in surfaces.Keys.ToArray())
            {
                float center = 12.5f + Mathf.Sin((cell.y - 14) * 0.20f) * 2.6f;
                bool river = cell.y >= 13 && cell.y < 51 && Mathf.Abs(cell.x + 0.5f - center) < 2.0f
                    && surfaces[cell].Heights.x <= -1.99f;
                if (river) rivers.Add(cell);
                if (river && cell.y >= 42 && cell.y < 45)
                {
                    var surface = surfaces[cell]; surface.Bridge = true; surfaces[cell] = surface;
                }
            }
            Vector2[] route = { P(40,51), P(39,44), P(38,38), P(32,34), P(28,28), P(24,24) };
            foreach (var cell in surfaces.Keys.ToArray())
            {
                var p = (Vector2)cell + Vector2.one * 0.5f;
                bool path = false;
                for (int i = 1; i < route.Length; i++)
                {
                    Vector2 line = route[i] - route[i-1];
                    Vector2 nearest = route[i-1] + line * Mathf.Clamp01(Vector2.Dot(p-route[i-1],line)/line.sqrMagnitude);
                    if (Vector2.Distance(p, nearest) < 1.15f) path = true;
                }
                if (Mathf.Abs(Vector2.Distance(p, P(38,38))-4.5f) < 0.8f) path = true;
                if (path && surfaces[cell].Heights.x == 0)
                {
                    var surface = surfaces[cell]; surface.Material = 0; surfaces[cell] = surface;
                }
            }
            var elevation = map.Elevation != null ? map.Elevation : Undo.AddComponent<CaveElevation>(map.gameObject);
            // Double the world footprint, keeping an editable grid and matching vertical proportions.
            foreach (var cell in surfaces.Keys.ToArray())
            {
                var surface = surfaces[cell]; surface.Heights *= 2; surfaces[cell] = surface;
            }
            Undo.RecordObject(elevation, "Author settlement terraces");
            elevation.Surfaces = surfaces.Values.OrderBy(s => s.Cell.y).ThenBy(s => s.Cell.x).ToList();
            elevation.Invalidate();
            EditorUtility.SetDirty(elevation);
            var settings = new SerializedObject(map);
            settings.FindProperty("elevation").objectReferenceValue = elevation;
            settings.ApplyModifiedProperties();
            Undo.RecordObject(map.LayoutGrid.transform, "Center settlement layout");
            Undo.RecordObject(map.LayoutGrid, "Expand settlement world scale");
            map.LayoutGrid.cellSize = new Vector3(2, 2, 0);
            map.LayoutGrid.transform.localPosition = new Vector3(-64, 0, 54);
            Fill(map.FloorTilemap, surfaces.Keys, AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Configs/Cave/Tiles/CaveFloorTile.asset"));
            Fill(map.RiverTilemap, rivers, AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Configs/Cave/Tiles/CaveRiverTile.asset"));
            Undo.RecordObject(map.SpawnPoint, "Place settlement spawn");
            map.SpawnPoint.position = Position(40, 50, 0.06f);
            void Fill(Tilemap tilemap, IEnumerable<Vector2Int> cells, TileBase tile)
            {
                Undo.RegisterCompleteObjectUndo(tilemap, "Replace settlement footprint");
                tilemap.ClearAllTiles();
                Vector3Int[] positions = cells.Select(c => new Vector3Int(c.x, c.y, 0)).ToArray();
                tilemap.SetTiles(positions, positions.Select(_ => tile).ToArray());
                tilemap.CompressBounds();
                EditorUtility.SetDirty(tilemap);
            }
        }
    }
}
