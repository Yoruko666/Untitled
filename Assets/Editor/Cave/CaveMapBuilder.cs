using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Untitled.Editor
{
    public static class CaveMapBuilder
    {
        private const int ChunkSize = 16;
        private const string MeshFolder = "Assets/Art/Models/Cave";

        public static void Rebuild(CaveMap map)
        {
            if (map.TryGetComponent<CaveTerrainStyle>(out var terrain))
            {
                CaveTerrainBuilder.Rebuild(map, terrain);
                return;
            }
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before rebuilding.");
            Validate(map);
            var floors = CaveMap.ReadCells(map.FloorTilemap);
            var rivers = CaveMap.ReadCells(map.RiverTilemap);
            string hash = ComputeHash(map, floors, rivers);
            Directory.CreateDirectory(MeshFolder);
            AssetDatabase.Refresh();
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Rebuild cave geometry");
            if (map.GeometryRoot != null) Undo.DestroyObjectImmediate(map.GeometryRoot.gameObject);
            var root = new GameObject("Geometry");
            Undo.RegisterCreatedObjectUndo(root, "Create cave geometry");
            root.transform.SetParent(map.transform, false);
            var serialized = new SerializedObject(map);
            serialized.FindProperty("geometryRoot").objectReferenceValue = root.transform;
            string sceneGuid = AssetDatabase.AssetPathToGUID(map.gameObject.scene.path);
            if (string.IsNullOrEmpty(map.GeometryAssetPrefix) || map.GeometrySceneGuid != sceneGuid)
            {
                // A copied scene gets its own generated meshes instead of modifying the source scene.
                string prefix = sceneGuid + "_" + GlobalObjectId.GetGlobalObjectIdSlow(map).targetObjectId;
                serialized.FindProperty("geometryAssetPrefix").stringValue = prefix;
                serialized.FindProperty("geometrySceneGuid").stringValue = sceneGuid;
            }
            serialized.ApplyModifiedProperties();

            var chunks = new Dictionary<Vector2Int, Chunk>();
            var collisionChunks = new Dictionary<Vector2Int, Chunk>();
            var waterChunks = new Dictionary<Vector2Int, Chunk>();
            Vector2Int Key(Vector2Int cell) => new(Mathf.FloorToInt((float)cell.x / ChunkSize), Mathf.FloorToInt((float)cell.y / ChunkSize));
            Chunk GetChunk(Vector2Int cell)
            {
                var key = Key(cell);
                if (!chunks.TryGetValue(key, out var value)) { value = new Chunk(); chunks.Add(key, value); }
                return value;
            }
            foreach (Vector2Int cell in floors.OrderBy(c => c.y).ThenBy(c => c.x))
            {
                Vector3 center = map.transform.InverseTransformPoint(map.CellToWorld(cell));
                Vector3 size = new(map.CellSize, 0.3f, map.CellSize);
                Vector2Int key = Key(cell);
                if (!collisionChunks.TryGetValue(key, out var collision))
                    collisionChunks.Add(key, collision = new Chunk());
                collision.Box(center + Vector3.down * 0.15f, size, 0);
                GetChunk(cell).Box(center + Vector3.down * (rivers.Contains(cell) ? 0.31f : 0.15f), size, 0);
                if (rivers.Contains(cell))
                {
                    if (!waterChunks.TryGetValue(key, out var water))
                        waterChunks.Add(key, water = new Chunk());
                    // Only exposed river edges receive foam; shared cell/chunk edges stay seamless.
                    var banks = new Color(
                        rivers.Contains(cell + Vector2Int.left) ? 0 : 1,
                        rivers.Contains(cell + Vector2Int.right) ? 0 : 1,
                        rivers.Contains(cell + Vector2Int.up) ? 0 : 1,
                        rivers.Contains(cell + Vector2Int.down) ? 0 : 1);
                    water.Top(center + Vector3.down * 0.055f, map.CellSize, 0, banks);
                }
            }
            var walls = new HashSet<Vector2Int>();
            foreach (Vector2Int cell in floors)
                for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++)
                {
                    var candidate = cell + new Vector2Int(x, y);
                    if (!floors.Contains(candidate)) walls.Add(candidate);
                }
            foreach (Vector2Int cell in walls.OrderBy(c => c.y).ThenBy(c => c.x))
            {
                Vector3 center = map.transform.InverseTransformPoint(map.CellToWorld(cell));
                GetChunk(cell).Box(center + Vector3.up * map.WallHeight * 0.5f,
                    new Vector3(map.CellSize, map.WallHeight, map.CellSize), 1, true);
                Vector2Int key = Key(cell);
                if (!collisionChunks.TryGetValue(key, out var collision))
                    collisionChunks.Add(key, collision = new Chunk());
                collision.Box(center + Vector3.up * map.WallHeight * 0.5f,
                    new Vector3(map.CellSize, map.WallHeight, map.CellSize), 1);
            }
            string assetPrefix = map.GeometryAssetPrefix;
            foreach (var pair in chunks.OrderBy(c => c.Key.y).ThenBy(c => c.Key.x))
            {
                string name = "Chunk_" + pair.Key.x + "_" + pair.Key.y;
                string path = MeshFolder + "/" + assetPrefix + "_" + name + ".asset";
                var saved = SaveMesh(pair.Value.Build(name), path);
                var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
                Undo.RegisterCreatedObjectUndo(go, "Create cave chunk");
                go.transform.SetParent(root.transform, false);
                go.GetComponent<MeshFilter>().sharedMesh = saved;
                go.GetComponent<MeshRenderer>().sharedMaterials = new[] { map.GroundMaterial, map.WallMaterial, map.RiverMaterial };
                // Collisions preserve full wall height and the original flat walk surface.
                go.GetComponent<MeshCollider>().sharedMesh = waterChunks.ContainsKey(pair.Key)
                    ? SaveMesh(collisionChunks[pair.Key].Build(name + "_Collision"), path.Replace(".asset", "_Collision.asset"))
                    : saved;
                if (waterChunks.TryGetValue(pair.Key, out var water))
                {
                    var surface = new GameObject("Water", typeof(MeshFilter), typeof(MeshRenderer));
                    Undo.RegisterCreatedObjectUndo(surface, "Create river surface");
                    surface.transform.SetParent(go.transform, false);
                    surface.GetComponent<MeshFilter>().sharedMesh = SaveMesh(water.Build(name + "_Water", true), path.Replace(".asset", "_Water.asset"));
                    var renderer = surface.GetComponent<MeshRenderer>();
                    renderer.sharedMaterial = map.RiverMaterial;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                }
            }
            serialized.Update();
            serialized.FindProperty("geometryHash").stringValue = hash;
            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
            AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(undoGroup);
        }

        public static void EnsureCurrent(CaveMap map)
        {
            if (map.TryGetComponent<CaveTerrainStyle>(out var terrain))
            {
                CaveTerrainBuilder.EnsureCurrent(map, terrain);
                return;
            }
            Validate(map);
            string hash = ComputeHash(map, CaveMap.ReadCells(map.FloorTilemap), CaveMap.ReadCells(map.RiverTilemap));
            if (map.GeometryRoot == null || map.GeometryHash != hash ||
                map.GeometrySceneGuid != AssetDatabase.AssetPathToGUID(map.gameObject.scene.path)) Rebuild(map);
        }

        public static void Validate(CaveMap map)
        {
            if (!map.HasEditableLayout || map.GroundMaterial == null || map.WallMaterial == null || map.RiverMaterial == null)
                throw new InvalidOperationException("Assign the scene Grid, two Tilemaps, spawn point and materials.");
            Grid grid = map.LayoutGrid;
            if (string.IsNullOrEmpty(map.gameObject.scene.path))
                throw new InvalidOperationException("Save the scene before generating meshes.");
            if (grid.transform.parent != map.transform || grid.cellLayout != GridLayout.CellLayout.Rectangle ||
                grid.cellSwizzle != GridLayout.CellSwizzle.XYZ || grid.cellGap != Vector3.zero ||
                grid.cellSize.x <= 0f || !Mathf.Approximately(grid.cellSize.x, grid.cellSize.y) ||
                !Mathf.Approximately(grid.cellSize.z, 0f) || grid.transform.localScale != Vector3.one ||
                Quaternion.Angle(grid.transform.localRotation, Quaternion.Euler(-90f, 0f, 0f)) > 0.01f)
                throw new InvalidOperationException("Use a square XY Grid rotated -90 degrees around X, with no gap or scale.");
            foreach (var tilemap in new[] { map.FloorTilemap, map.RiverTilemap })
            {
                if (tilemap.layoutGrid != grid || tilemap.transform.localPosition.x != 0f ||
                    tilemap.transform.localPosition.y != 0f || tilemap.transform.localRotation != Quaternion.identity ||
                    tilemap.transform.localScale != Vector3.one)
                    throw new InvalidOperationException("Both Tilemaps must align with the layout Grid.");
                foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
                    if (cell.z != 0 && tilemap.HasTile(cell))
                        throw new InvalidOperationException("Paint tiles on cell Z = 0 only.");
            }
            var floors = CaveMap.ReadCells(map.FloorTilemap);
            var rivers = CaveMap.ReadCells(map.RiverTilemap);
            if (floors.Count == 0 || !map.IsWalkable(map.SpawnPosition) || !rivers.IsSubsetOf(floors))
                throw new InvalidOperationException("Floor cannot be empty; spawn and every river cell must be on floor.");
            if (map.Elevation != null)
            {
                var seen = new HashSet<Vector2Int>();
                foreach (var surface in map.Elevation.Surfaces)
                {
                    if (!seen.Add(surface.Cell) || surface.Material < 0 || surface.Material > 3)
                        throw new InvalidOperationException("Elevation cells must be unique and use a valid ground material.");
                    for (int i = 0; i < 4; i++)
                        if (float.IsNaN(surface.Heights[i]) || float.IsInfinity(surface.Heights[i]))
                            throw new InvalidOperationException("Elevation corner heights must be finite.");
                }
            }
        }

        private static string ComputeHash(CaveMap map, HashSet<Vector2Int> floors, HashSet<Vector2Int> rivers)
        {
            var data = new StringBuilder("scene-tilemap-v3-wall-reveal|");
            data.Append(map.CellSize.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append('|');
            data.Append(map.WallHeight.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append('|');
            Vector3 origin = map.LayoutGrid.transform.localPosition;
            foreach (float value in new[] { origin.x, origin.y, origin.z })
                data.Append(value.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append('|');
            foreach (var material in new[] { map.GroundMaterial, map.WallMaterial, map.RiverMaterial })
                data.Append(AssetDatabase.GetAssetPath(material)).Append('|');
            foreach (var cells in new[] { floors, rivers })
            {
                foreach (Vector2Int cell in cells.OrderBy(c => c.y).ThenBy(c => c.x))
                    data.Append(cell.x).Append(',').Append(cell.y).Append(';');
                data.Append('|');
            }
            return Hash128.Compute(data.ToString()).ToString();
        }

        private static Mesh SaveMesh(Mesh mesh, string path)
        {
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            mesh.name = saved != null ? saved.name : Path.GetFileNameWithoutExtension(path);
            if (saved == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
            Undo.RegisterCompleteObjectUndo(saved, "Update cave mesh");
            EditorUtility.CopySerialized(mesh, saved);
            EditorUtility.SetDirty(saved);
            UnityEngine.Object.DestroyImmediate(mesh);
            return saved;
        }

        private sealed class Chunk
        {
            private readonly List<Vector3> vertices = new();
            private readonly List<Vector3> normals = new();
            private readonly List<Vector2> uv = new();
            private readonly List<Color> colors = new();
            private readonly List<int>[] triangles = { new(), new(), new() };

            private void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, int material, Color banks = default)
            {
                int index = vertices.Count;
                vertices.AddRange(new[] { a, b, c, d });
                for (int i = 0; i < 4; i++) normals.Add(normal);
                for (int i = 0; i < 4; i++) colors.Add(banks);
                uv.AddRange(new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up });
                bool forward = Vector3.Dot(Vector3.Cross(b - a, c - a), normal) > 0f;
                triangles[material].AddRange(forward
                    ? new[] { index, index + 1, index + 2, index, index + 2, index + 3 }
                    : new[] { index, index + 2, index + 1, index, index + 3, index + 2 });
            }

            public void Top(Vector3 center, float size, int material, Color banks = default)
            {
                float half = size * 0.5f;
                Quad(center + new Vector3(-half, 0, -half), center + new Vector3(half, 0, -half),
                    center + new Vector3(half, 0, half), center + new Vector3(-half, 0, half), Vector3.up, material, banks);
            }

            public void Box(Vector3 center, Vector3 size, int sideMaterial, bool wall = false)
            {
                float x = size.x * 0.5f, y = size.y * 0.5f, z = size.z * 0.5f;
                Vector3 V(float px, float py, float pz) => center + new Vector3(px, py, pz);
                // Red marks wall geometry, including caps that share the floor material.
                var mask = new Color(wall ? 1f : 0f, 0f, 0f, 1f);
                Top(center + Vector3.up * y, size.x, 0, mask);
                Quad(V(-x,-y,-z), V(x,-y,-z), V(x,y,-z), V(-x,y,-z), Vector3.back, sideMaterial, mask);
                Quad(V(x,-y,z), V(-x,-y,z), V(-x,y,z), V(x,y,z), Vector3.forward, sideMaterial, mask);
                Quad(V(-x,-y,z), V(-x,-y,-z), V(-x,y,-z), V(-x,y,z), Vector3.left, sideMaterial, mask);
                Quad(V(x,-y,-z), V(x,-y,z), V(x,y,z), V(x,y,-z), Vector3.right, sideMaterial, mask);
                Quad(V(-x,-y,z), V(x,-y,z), V(x,-y,-z), V(-x,-y,-z), Vector3.down, sideMaterial, mask);
            }

            public Mesh Build(string name, bool water = false)
            {
                var mesh = new Mesh { name = name, subMeshCount = water ? 1 : 3 };
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetUVs(0, uv);
                mesh.SetColors(colors);
                for (int i = 0; i < mesh.subMeshCount; i++) mesh.SetTriangles(triangles[i], i);
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
