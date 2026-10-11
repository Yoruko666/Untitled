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
            Debug.Log($"Terrain V3 saved: {CaveMap.ReadCells(map.FloorTilemap).Count} cells; "
                + "wide west bank, cliff-side river, diagonal low crossing, residential terraces and separate elder platform.");
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
            void Ribbon(int yMin, int yMax, Func<float, float> center, Func<float, float> height, float halfWidth)
            {
                for (int y = yMin; y < yMax; y++)
                    for (int x = 38; x < 65; x++)
                        if (Mathf.Abs(x + 0.5f - center(y + 0.5f)) <= halfWidth)
                            Rectangle(x, y, x + 1, y + 1, (xx, yy) => height(yy));
            }
            Vector2 P(float x, float y) => new Vector2(x, y);
            // Coordinates are editable grid cells; heights below are already world units.
            Polygon(0, 1, P(4,14), P(10,11), P(20,13), P(24,15), P(30,14), P(32,6),
                P(38,4), P(48,4), P(53,7), P(57,5), P(62,6), P(65,12), P(64,39),
                P(62,49), P(55,51), P(46,50), P(43,54), P(37,54), P(33,50), P(23,51),
                P(13,53), P(6,50), P(3,44), P(2,36), P(4,29), P(2,22));

            float RiverCenter(float y)
            {
                Vector2[] bends = { P(11,17), P(14,18.5f), P(22,20.5f), P(30,19.5f),
                    P(38,21.5f), P(44,20), P(51,16), P(54,17) };
                for (int i = 1; i < bends.Length; i++)
                    if (y <= bends[i].x)
                        return Mathf.Lerp(bends[i - 1].y, bends[i].y,
                            Mathf.InverseLerp(bends[i - 1].x, bends[i].x, y));
                return bends[bends.Length - 1].y;
            }
            var rivers = new HashSet<Vector2Int>();
            foreach (Vector2Int cell in surfaces.Keys.ToArray())
            {
                float center = RiverCenter(cell.y + 0.5f);
                if (cell.x + 0.5f < center + 2) Flat(cell.x, cell.y, -4, 2);
                if (cell.y >= 14 && Mathf.Abs(cell.x + 0.5f - center) < 2)
                    rivers.Add(cell);
            }

            // Kitchen/feast stays at zero between the river and the recessed hall.
            Polygon(0, 0, P(25,20), P(28,19), P(37,19), P(38,23), P(37,29), P(25,29));
            Polygon(10.4f, 3, P(32,7), P(35,5), P(49,5), P(52,7), P(52,15),
                P(49,17), P(35,17), P(32,15));
            Polygon(10.4f, 3, P(55,9), P(57,7), P(62,7), P(64,9), P(64,13),
                P(62,15), P(55,15));
            // A rock cleft separates the elder platform; only the four-unit neck crosses it.
            foreach (var cell in surfaces.Keys.Where(c => c.x >= 52 && c.x < 55 && c.y >= 8 && c.y < 17).ToArray())
                surfaces.Remove(cell);
            Rectangle(51, 10, 57, 12, (x, y) => 10.4f);

            Polygon(2.4f, 1, P(43,29), P(54,29), P(57,32), P(57,37), P(54,39), P(43,39));
            Polygon(4.8f, 1, P(44,21), P(54,21), P(56,23), P(56,29), P(42,29), P(42,24));
            Rectangle(40, 35, 43, 41, (x, y) => (41 - y) * 0.4f);
            Rectangle(40, 33, 45, 35, (x, y) => 2.4f);
            Rectangle(39, 25, 42, 33, (x, y) => 2.4f + (33 - y) * 0.3f);
            Rectangle(39, 23, 45, 25, (x, y) => 4.8f);
            // The ascent starts from the upper residence, rather than bypassing both homes.
            Rectangle(54, 27, 58, 29, (x, y) => 4.8f);
            Ribbon(18, 27, y => y > 23 ? Mathf.Lerp(61, 55.5f, (y - 23) / 4)
                : Mathf.Lerp(58.5f, 61, (y - 18) / 5), y => 4.8f + (27 - y) * (2.8f / 9), 1.5f);
            Rectangle(57, 16, 65, 18, (x, y) => 7.6f);
            Ribbon(12, 16, y => 63, y => 7.6f + (16 - y) * 0.7f, 1);
            Rectangle(60, 10, 64, 12, (x, y) => 10.4f);

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
