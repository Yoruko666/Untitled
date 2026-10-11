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
        private static readonly Vector2 SettlementBridgeStart = new Vector2(14, 41);
        private static readonly Vector2 SettlementBridgeEnd = new Vector2(24.3923f, 47);
        private const string SettlementScenePath = "Assets/Scenes/CaveHD2DLocal.unity";

        // Explicit replacement only. The ordinary Build operation preserves saved edits.
        [MenuItem("Untitled/Cave/Replace With Terraced Settlement Layout")]
        public static void BuildTerracedSettlement()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before replacing the cave layout.");
            var active = EditorSceneManager.GetActiveScene();
            if (active.isDirty) throw new InvalidOperationException("Save the current scene before applying a new layout.");
            var scene = active.path == SettlementScenePath ? active : EditorSceneManager.OpenScene(SettlementScenePath);
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
            SettlementRiverExit(landform);
            CaveTerrainBuilder.Rebuild(map, style);
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
            Debug.Log($"Terrain V5 saved: {CaveMap.ReadCells(map.FloorTilemap).Count} cells; "
                + "winding hall ascent, eastern elder branch and continuous river barriers.");
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
                    for (int x = 1; x < 72; x++)
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
            Vector2 P(float x, float y) => new Vector2(x, y);
            // Coordinates are editable grid cells; heights below are already world units.
            Polygon(0, 1, P(4,14), P(10,11), P(20,13), P(24,15), P(30,14), P(32,6),
                P(38,4), P(48,4), P(53,7), P(61,5), P(67,6), P(70,12), P(68,20), P(66,28), P(63,36),
                P(63,40), P(61,45), P(55,48), P(47,49), P(43,54), P(37,54), P(33,50), P(23,51),
                P(13,53), P(6,50), P(3,44), P(2,36), P(4,29), P(2,22));

            Vector2[] riverCourse = { P(15,0), P(16,7), P(18.5f,14), P(19,20), P(17.5f,27),
                P(21,34), P(22,39), P(20,44), P(16,51), P(12,57), P(9,62) };
            float RiverCenter(float y)
            {
                for (int i = 1; i < riverCourse.Length; i++)
                    if (y <= riverCourse[i].y)
                        return Mathf.Lerp(riverCourse[i - 1].x, riverCourse[i].x,
                            Mathf.InverseLerp(riverCourse[i - 1].y, riverCourse[i].y, y));
                return riverCourse[riverCourse.Length - 1].x;
            }
            var rivers = new HashSet<Vector2Int>();
            foreach (Vector2Int cell in surfaces.Keys.ToArray())
            {
                float center = RiverCenter(cell.y + 0.5f);
                if (cell.x + 0.5f < center + 2) Flat(cell.x, cell.y, -4, 2);
            }
            void RiverStroke(float waterHalfWidth, params Vector2[] course)
            {
                for (int i = 1; i < course.Length; i++)
                {
                    Vector2 a = course[i - 1], b = course[i], line = b - a;
                    for (int y = Mathf.FloorToInt(a.y); y < Mathf.CeilToInt(b.y); y++)
                        for (int x = Mathf.FloorToInt(Mathf.Min(a.x, b.x) - 4); x < Mathf.CeilToInt(Mathf.Max(a.x, b.x) + 4); x++)
                        {
                            var cell = new Vector2Int(x, y);
                            Vector2 p = (Vector2)cell + Vector2.one * 0.5f;
                            Vector2 nearest = a + line * Mathf.Clamp01(Vector2.Dot(p - a, line) / line.sqrMagnitude);
                            float distance = Vector2.Distance(p, nearest);
                            if (distance > 4) continue;
                            if (!surfaces.ContainsKey(cell)) Flat(x, y, -4, 2);
                            if (distance < waterHalfWidth) { Flat(x, y, -4, 2); rivers.Add(cell); }
                        }
                }
            }
            RiverStroke(2, riverCourse);
            RiverStroke(1.7f, P(18.7f,46.5f), P(22,50), P(28,52), P(32,57), P(33,62));

            // Kitchen/feast stays at zero between the river and the recessed hall.
            Polygon(0, 0, P(25,20), P(28,19), P(37,19), P(38,23), P(37,29), P(25,29));
            Polygon(10.4f, 3, P(32,7), P(35,5), P(49,5), P(52,7), P(52,15),
                P(49,17), P(35,17), P(32,15));
            Polygon(10.4f, 3, P(59,9), P(61,7), P(66,7), P(68,9), P(68,13),
                P(66,15), P(59,15));
            // The elder remains a separate destination, reached by a branch from the ascent.
            foreach (var cell in surfaces.Keys.Where(c => c.x >= 52 && c.x < 59 && c.y >= 8 && c.y < 17).ToArray())
                surfaces.Remove(cell);
            Polygon(2.4f, 1, P(48,32), P(59,32), P(62,35), P(61,40), P(58,42), P(48,42), P(47,38));
            Polygon(4.8f, 1, P(45,23), P(49,22), P(55,23), P(59,25), P(59,28), P(56,29), P(46,29), P(44,26));

            // Smooth road centre lines author shared corner heights into the saved grid.
            // The broad eastern bend wraps the rear homes, then enters the hall from its front edge.
            var roadSegments = new List<(Vector3 Start, Vector3 End, float Width)>();
            void Road(float width, params Vector3[] points)
            {
                for (int i = 0; i < points.Length - 1; i++)
                {
                    Vector3 a = points[Mathf.Max(0, i - 1)], b = points[i];
                    Vector3 c = points[i + 1], d = points[Mathf.Min(points.Length - 1, i + 2)];
                    Vector3 previous = b;
                    for (int step = 1; step <= 12; step++)
                    {
                        float t = step / 12f;
                        Vector3 next = 0.5f * (2 * b + (-a + c) * t
                            + (2 * a - 5 * b + 4 * c - d) * t * t
                            + (-a + 3 * b - 3 * c + d) * t * t * t);
                        next.z = Mathf.Clamp(next.z, Mathf.Min(b.z, c.z), Mathf.Max(b.z, c.z));
                        roadSegments.Add((previous, next, width));
                        previous = next;
                    }
                }
            }
            Vector3 R(float x, float y, float height) => new Vector3(x, y, height);
            Road(1.6f, R(41,42,0), R(41.5f,39,0.8f), R(42.5f,35,2.8f), R(44,32,4.8f), R(47,31,4.8f));
            Road(1.6f, R(45,31,4.8f), R(50,30.5f,4.8f), R(56,31,4.8f), R(61,30.5f,4.8f),
                R(64,28,5.8f), R(64,25,6.8f), R(63.5f,23.5f,7.6f), R(62,22.5f,7.6f), R(60,22,7.6f),
                R(57,20.5f,8.6f), R(53,19,9.5f), R(49,17,10.4f), R(47,15.5f,10.4f));
            Road(1.1f, R(62.5f,23,7.6f), R(65,22.5f,7.6f), R(68,20,8.4f), R(68,17.5f,9.2f),
                R(66,15,10.4f), R(64,13,10.4f));
            (float Distance, float Height, float Width) NearestRoad(Vector2 point)
            {
                var nearest = (Distance: float.PositiveInfinity, Height: 0f, Width: 0f);
                foreach (var segment in roadSegments)
                {
                    Vector2 start = segment.Start, line = (Vector2)(segment.End - segment.Start);
                    float t = Mathf.Clamp01(Vector2.Dot(point - start, line) / line.sqrMagnitude);
                    float distance = Vector2.Distance(point, start + line * t);
                    if (distance >= nearest.Distance) continue;
                    nearest = (distance, Mathf.Lerp(segment.Start.z, segment.End.z, t), segment.Width);
                }
                return nearest;
            }
            for (int y = 12; y < 45; y++)
                for (int x = 39; x < 71; x++)
                {
                    var nearest = NearestRoad(P(x + 0.5f, y + 0.5f));
                    if (nearest.Distance > nearest.Width) continue;
                    var cell = new Vector2Int(x, y);
                    surfaces[cell] = new CaveElevation.CellSurface
                    {
                        Cell = cell, Material = 0,
                        Heights = new Vector4(NearestRoad(P(x,y)).Height, NearestRoad(P(x+1,y)).Height,
                            NearestRoad(P(x+1,y+1)).Height, NearestRoad(P(x,y+1)).Height)
                    };
                }
            Rectangle(50, 31, 53, 35, (x, y) => 4.8f - (y - 31) * 0.6f);
            Rectangle(48, 40, 51, 44, (x, y) => (44 - y) * 0.6f);

            Rectangle(23, 45, 29, 50, (x, y) => -4);
            Rectangle(29, 46, 35, 50, (x, y) => -4 + (x - 29) * (4f / 6));
            // Widen the landing around the eastern bridge mouth without extending it upriver.
            Polygon(-4, 2, P(21,44), P(25,44), P(25,46), P(23,48), P(21,47));
            Vector2 bridgeLine = SettlementBridgeEnd - SettlementBridgeStart;
            foreach (var cell in rivers.ToArray())
            {
                Vector2 p = (Vector2)cell + Vector2.one * 0.5f;
                float t = Mathf.Clamp01(Vector2.Dot(p - SettlementBridgeStart, bridgeLine) / bridgeLine.sqrMagnitude);
                if (Vector2.Distance(p, SettlementBridgeStart + bridgeLine * t) > 1.7f) continue;
                var surface = surfaces[cell];
                surface.Bridge = true;
                surfaces[cell] = surface;
            }

            Vector2[] route = { P(40,51), P(39,44), P(37,38), P(32,34), P(29,28), P(29,23) };
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
            Undo.RecordObject(elevation, "Author settlement terraces");
            elevation.RiverSurfaceDrop = 2.2f;
            elevation.RiverBedDrop = 3.4f;
            elevation.Surfaces = surfaces.Values.OrderBy(s => s.Cell.y).ThenBy(s => s.Cell.x).ToList();
            elevation.Invalidate();
            EditorUtility.SetDirty(elevation);
            var settings = new SerializedObject(map);
            settings.FindProperty("elevation").objectReferenceValue = elevation;
            settings.ApplyModifiedProperties();
            Undo.RecordObject(map.LayoutGrid.transform, "Center settlement layout");
            Undo.RecordObject(map.LayoutGrid, "Preserve expanded settlement scale");
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
