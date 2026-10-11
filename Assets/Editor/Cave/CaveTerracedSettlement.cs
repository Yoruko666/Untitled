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
        // Explicit initial layout operation. The ordinary Build menu continues to preserve saved edits.
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
            Undo.SetCurrentGroupName("Lay out terraced goblin settlement");
            ReplaceSettlementLayout();
            Undo.RecordObject(style, "Set settlement rock scale");
            style.RockHeight = 1.8f;
            style.RockVariation = 0.45f;
            style.RockMargin = 3;
            EditorUtility.SetDirty(style);
            CaveTerrainBuilder.Rebuild(map, style);

            Transform root = map.transform.parent;
            foreach (Transform old in new[] { map.transform.Find("Architecture"), root.Find("PlacedProps") })
                if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            Transform architecture = Group("Architecture", map.transform);
            Transform props = Group("PlacedProps", root);
            stone = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/CarvedStone.mat");
            wood = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Timber.mat");
            soil = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/GardenSoil.mat");
            cube = PrimitiveMesh(PrimitiveType.Cube, "StoneBlock");
            column = PrimitiveMesh(PrimitiveType.Cylinder, "StoneColumn");
            propCount = 0;
            SettlementEntrance(props, architecture);
            SettlementHomes(props, architecture);
            SettlementFood(props, architecture);
            SettlementRiver(props, architecture);
            SettlementHighTerrace(props, architecture);
            SetPresentation(scene);
            ApplySceneLighting();
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
            Camera camera = follow.GetComponent<Camera>();
            Quaternion rotation = Quaternion.Euler(follow.ViewAngle, 0, 0);
            camera.transform.SetPositionAndRotation(player.transform.position + Vector3.up * 1.2f
                - rotation * Vector3.forward * follow.Distance, rotation);
            Selection.activeGameObject = map.gameObject;
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.LookAt(Position(36, 34), Quaternion.Euler(48, 0, 0), 43f);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            Undo.CollapseUndoOperations(group);
            Debug.Log($"Terraced settlement saved: {CaveMap.ReadCells(map.FloorTilemap).Count} cells, "
                + $"{propCount} props, riverbank -1.2 / main 0 / upper terrace +2.4. Two river ramps, a bridge and a switchback ascent.");
        }

        private static void ReplaceSettlementLayout()
        {
            var surfaces = new Dictionary<Vector2Int, CaveElevation.CellSurface>();
            void Polygon(float height, int material, params Vector2[] points)
            {
                for (int y = 5; y < 67; y++)
                    for (int x = 2; x < 72; x++)
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
            void Flat(int x, int y, float height, int material)
            {
                var cell = new Vector2Int(x, y);
                surfaces[cell] = new CaveElevation.CellSurface
                {
                    Cell = cell, Heights = Vector4.one * height, Material = material
                };
            }
            void Rectangle(int xMin, int yMin, int xMax, int yMax, Func<float, float, float> height)
            {
                for (int y = yMin; y < yMax; y++)
                    for (int x = xMin; x < xMax; x++)
                    {
                        var cell = new Vector2Int(x, y);
                        surfaces[cell] = new CaveElevation.CellSurface
                        {
                            Cell = cell, Material = 1,
                            Heights = new Vector4(height(x, y), height(x + 1, y), height(x + 1, y + 1), height(x, y + 1))
                        };
                    }
            }
            Polygon(0, 1, new Vector2(26,25), new Vector2(33,23), new Vector2(43,25), new Vector2(51,25),
                new Vector2(56,29), new Vector2(65,30), new Vector2(70,36), new Vector2(69,44),
                new Vector2(66,48), new Vector2(68,57), new Vector2(63,61), new Vector2(52,60),
                new Vector2(47,56), new Vector2(44,62), new Vector2(40,65), new Vector2(34,64),
                new Vector2(31,58), new Vector2(26,55), new Vector2(26,47), new Vector2(24,44),
                new Vector2(25,37), new Vector2(23,34), new Vector2(25,28));
            Polygon(-1.2f, 2, new Vector2(5,23), new Vector2(12,19), new Vector2(20,20),
                new Vector2(26,24), new Vector2(26,32), new Vector2(23,36), new Vector2(24,42),
                new Vector2(26,48), new Vector2(26,54), new Vector2(22,58), new Vector2(14,57),
                new Vector2(8,54), new Vector2(5,48), new Vector2(7,39), new Vector2(3,33));
            Polygon(2.4f, 3, new Vector2(28,10), new Vector2(38,6), new Vector2(49,7),
                new Vector2(61,10), new Vector2(63,18), new Vector2(60,24), new Vector2(51,25),
                new Vector2(46,23), new Vector2(34,24), new Vector2(27,20));
            Rectangle(18, 37, 32, 41, (x, y) => Mathf.Lerp(-1.2f, 0, (x - 18) / 14f));
            Rectangle(18, 50, 32, 54, (x, y) => Mathf.Lerp(-1.2f, 0, (x - 18) / 14f));
            Rectangle(60, 30, 65, 40, (x, y) => (40 - y) * 0.12f);
            Rectangle(60, 27, 69, 30, (x, y) => 1.2f);
            Rectangle(65, 18, 69, 27, (x, y) => 1.2f + (27 - y) * (1.2f / 9f));
            Rectangle(60, 15, 69, 18, (x, y) => 2.4f);
            var rivers = new HashSet<Vector2Int>();
            foreach (Vector2Int cell in surfaces.Keys.ToArray())
            {
                float center = 13f + Mathf.Sin((cell.y - 21f) * 0.21f) * 2.8f;
                bool river = cell.y >= 22 && cell.y < 56 && Mathf.Abs(cell.x + 0.5f - center) < 1.7f
                    && surfaces[cell].Heights.x <= -1.19f;
                if (river) rivers.Add(cell);
                if (river && cell.y >= 48 && cell.y < 51)
                {
                    var surface = surfaces[cell];
                    surface.Bridge = true;
                    surfaces[cell] = surface;
                }
            }
            var elevation = map.Elevation != null ? map.Elevation : Undo.AddComponent<CaveElevation>(map.gameObject);
            Undo.RecordObject(elevation, "Author terrain elevation");
            elevation.Surfaces = surfaces.Values.OrderBy(s => s.Cell.y).ThenBy(s => s.Cell.x).ToList();
            elevation.Invalidate();
            EditorUtility.SetDirty(elevation);
            var settings = new SerializedObject(map);
            settings.FindProperty("elevation").objectReferenceValue = elevation;
            settings.ApplyModifiedProperties();
            Undo.RecordObject(map.LayoutGrid.transform, "Center settlement layout");
            map.LayoutGrid.transform.localPosition = new Vector3(-36, 0, 32);
            TileBase floor = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Configs/Cave/Tiles/CaveFloorTile.asset");
            TileBase riverTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Configs/Cave/Tiles/CaveRiverTile.asset");
            Fill(map.FloorTilemap, surfaces.Keys, floor);
            Fill(map.RiverTilemap, rivers, riverTile);
            Undo.RecordObject(map.SpawnPoint, "Place settlement spawn");
            map.SpawnPoint.position = Position(38.5f, 61.5f, 0.06f);

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

        private static void SettlementEntrance(Transform props, Transform architecture)
        {
            Transform entrance = Group("01_EntranceAndFirepit", props);
            Prop(entrance, "Camp/spike_barrier_hd2d", 34, 61, 1.2f);
            Prop(entrance, "Camp/spike_barrier_hd2d", 43, 60, 1.2f);
            Prop(entrance, "TotemsBones/rune_banner_hd2d", 33, 59, 1.2f);
            Fire(entrance, "Lighting/wood_tripod_lit_hd2d", 42, 59, 1.2f, 8);
            Fire(entrance, "Lighting/campfire_hd2d", 40, 45, 1.5f, 13);
            Prop(entrance, "TotemsBones/beast_totem_hd2d", 45, 41, 1.1f);
            Spot(entrance, "EntranceDaylight", 38, 63, 5, new Color(0.63f,0.70f,0.72f), 2, 18);
            Transform warehouse = Group("02_DryStorage", props);
            foreach (Vector2 p in new[] { new Vector2(56,54), new Vector2(61,53), new Vector2(63,57) })
            {
                Pad(architecture, "DryStoragePlinth", p.x, p.y, 3, 2.5f, 0.12f, wood);
                Prop(warehouse, "Kitchen/firewood_stack_hd2d", p.x, p.y, 1.8f, 0.12f);
                Prop(warehouse, "Kitchen/produce_garden_hd2d", p.x - 1.1f, p.y + 1, 1, 0.12f);
            }
            Fire(warehouse, "Lighting/timber_brazier_hd2d", 54, 56, 1, 8);
            Transform training = Group("03_EntranceTraining", props);
            foreach (Vector2 p in new[] { new Vector2(54,59), new Vector2(59,59) })
                Prop(training, "Camp/training_dummy_hd2d", p.x, p.y, 1.4f);
            Prop(training, "TotemsBones/ring_training_rack_hd2d", 64, 58, 1.4f);
        }

        private static void SettlementHomes(Transform props, Transform architecture)
        {
            Transform homes = Group("04_RocksideHomes", props);
            foreach (Vector2 p in new[] { new Vector2(55,33), new Vector2(56,37), new Vector2(66,39),
                new Vector2(64,44), new Vector2(56,47), new Vector2(62,48) })
            {
                Pad(architecture, "SleepingDeck", p.x, p.y, 3.5f, 3, 0.12f, wood);
                Prop(homes, "Camp/hide_tent_hd2d", p.x, p.y - 0.5f, 1.7f, 0.12f);
                Prop(homes, "Camp/straw_bed_hd2d", p.x + 0.9f, p.y + 0.8f, 1.05f, 0.12f, 90);
            }
            foreach (Vector2 p in new[] { new Vector2(53,38), new Vector2(67,43), new Vector2(58,49) })
                Fire(homes, "Lighting/campfire_hd2d", p.x, p.y, 0.75f, 7);
            Prop(homes, "Kitchen/firewood_bundle_hd2d", 53, 33, 1.1f);
        }

        private static void SettlementFood(Transform props, Transform architecture)
        {
            Transform kitchen = Group("05_KitchenAndFeast", props);
            Prop(kitchen, "Kitchen/cooking_stove_hd2d", 28, 28, 1.8f);
            Prop(kitchen, "Kitchen/cauldron_a_3d", 31, 28, 1.2f);
            Prop(kitchen, "Kitchen/meat_rack_hd2d", 28, 32, 1.3f);
            Prop(kitchen, "Kitchen/chopping_table_hd2d", 33, 31, 1.6f);
            Prop(kitchen, "Kitchen/firewood_stack_hd2d", 35, 28, 1.3f);
            Fire(kitchen, "Lighting/iron_bowl_fire_hd2d", 30, 28, 0.9f, 9);
            foreach (Vector2 p in new[] { new Vector2(37,33), new Vector2(43,30), new Vector2(45,35) })
            {
                Pad(architecture, "FeastWoodFloor", p.x, p.y, 4, 3, 0.06f, wood);
                Prop(kitchen, "Kitchen/chopping_table_hd2d", p.x, p.y, 1.9f, 0.06f, 90);
                Prop(kitchen, "Camp/straw_bed_hd2d", p.x + 1.4f, p.y + 0.4f, 0.7f, 0.06f, 90);
            }
            Fire(kitchen, "Lighting/timber_brazier_hd2d", 39, 29, 1, 9);
            Transform tavern = Group("06_PlazaTavern", props);
            Pad(architecture, "TavernDeck", 32, 45, 7, 6, 0.08f, wood);
            Prop(tavern, "Camp/hide_tent_hd2d", 30, 44, 1.9f, 0.08f);
            Prop(tavern, "Kitchen/chopping_table_hd2d", 34, 45, 1.5f, 0.08f);
            Prop(tavern, "Kitchen/stone_cistern_standard_3d", 30, 47, 1.15f, 0.08f);
            Fire(tavern, "Lighting/iron_bowl_fire_hd2d", 34, 43, 0.8f, 8);
        }

        private static void SettlementRiver(Transform props, Transform architecture)
        {
            Transform bank = Group("07_LowerRiverAndGarden", props);
            foreach (Vector2 p in new[] { new Vector2(8,25), new Vector2(9,29), new Vector2(7,33) })
            {
                Pad(architecture, "RiversideSoilBed", p.x, p.y, 2.5f, 2, 0.12f, soil);
                Prop(bank, "Kitchen/produce_leaves_hd2d", p.x, p.y, 1.2f, 0.12f);
                Prop(bank, "Kitchen/produce_roots_hd2d", p.x + 0.7f, p.y + 0.4f, 1, 0.12f);
            }
            Prop(bank, "Kitchen/stone_cistern_wide_3d", 20, 33, 1.4f);
            Prop(bank, "Kitchen/chopping_table_hd2d", 21, 45, 1.6f);
            Prop(bank, "Kitchen/firewood_bundle_hd2d", 22, 47, 1.3f);
            Fire(bank, "Lighting/iron_cage_fire_hd2d", 22, 43, 0.85f, 7);
            Spot(bank, "GardenSkylight", 12, 23, 7, new Color(0.65f,0.78f,0.75f), 2.5f, 16);
            foreach (float y in new[] { 31f, 43f, 54f })
                Spot(bank, "RiverCoolLight", 13, y, 3, Cool, 1.3f, 10);
            for (int i = 0; i < 22; i++)
                Block(architecture, "BridgePlank", 8.25f + i * 0.5f, 49.5f, 0.46f, 0.08f, 3, wood);
            foreach (float y in new[] { 48.05f, 50.95f })
            {
                Block(architecture, "BridgeHandrail", 13.5f, y, 11, 0.6f, 0.1f, wood);
                for (int i = 0; i < 6; i++)
                    Block(architecture, "BridgePost", 8.5f + i * 2, y, 0.14f, 0.9f, 0.14f, wood);
            }
            foreach (Vector2 p in new[] { new Vector2(32,39), new Vector2(32,52) })
                Fire(bank, "Lighting/wood_tripod_lit_hd2d", p.x + 1, p.y - 2.3f, 0.9f, 8);
        }

        private static void SettlementHighTerrace(Transform props, Transform architecture)
        {
            Transform upper = Group("08_ChieftainAndElder", props);
            Pad(architecture, "ChieftainStoneDais", 40, 12, 7, 5, 0.36f, stone);
            for (int i = 0; i < 3; i++)
                Block(architecture, "DaisStep", 40, 15.75f - i * 0.5f, 4, 0.12f * (i + 1), 0.5f, stone);
            Prop(upper, "TotemsBones/beast_totem_hd2d", 40, 11, 2.2f, 0.36f);
            Prop(upper, "TotemsBones/rune_banner_hd2d", 36, 13, 1.5f);
            Prop(upper, "TotemsBones/rune_banner_hd2d", 44, 13, 1.5f);
            foreach (float x in new[] { 34f, 46f })
            {
                Column(architecture, "HallRockPillar", x, 11, 0.8f, 2.5f);
                Fire(upper, "Lighting/bone_fire_stand_hd2d", x, 16, 1.1f, 10);
            }
            Pad(architecture, "ElderDryDeck", 55, 13, 5, 4, 0.12f, wood);
            Prop(upper, "Camp/hide_tent_hd2d", 55, 12, 1.9f, 0.12f);
            Prop(upper, "Camp/wooden_bed_hd2d", 57, 13, 1.1f, 0.12f);
            Prop(upper, "Kitchen/chopping_table_hd2d", 53, 15, 1.2f);
            Fire(upper, "Lighting/timber_brazier_hd2d", 58, 15, 0.9f, 9);
            Spot(upper, "HighTerraceSkylight", 42, 12, 7, new Color(0.75f,0.76f,0.63f), 2, 18);
            foreach (Vector2 p in new[] { new Vector2(61,39), new Vector2(68,28), new Vector2(67,19) })
                Fire(upper, "Lighting/wood_tripod_lit_hd2d", p.x, p.y, 0.75f, 8);
        }
    }
}
