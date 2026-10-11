using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Untitled.Editor
{
    public static class CaveTerrainBuilder
    {
        private const string Folder = "Assets/Art/Models/CaveHD2D";
        private const int ChunkSize = 16;

        public static void EnsureCurrent(CaveMap map, CaveTerrainStyle style)
        {
            CaveMapBuilder.Validate(map);
            if (map.GeometryRoot == null || map.GeometryHash != Hash(map, style) ||
                map.GeometrySceneGuid != AssetDatabase.AssetPathToGUID(map.gameObject.scene.path))
                Rebuild(map, style);
        }

        public static void Rebuild(CaveMap map, CaveTerrainStyle style)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before rebuilding terrain.");
            CaveMapBuilder.Validate(map);
            if (new[] { style.Soil, style.Settlement, style.WetGround, style.Hall, style.RockCap }.Any(m => m == null))
                throw new InvalidOperationException("Assign all cave terrain materials.");

            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Rebuild HD2D cave terrain");
            if (map.GeometryRoot != null) Undo.DestroyObjectImmediate(map.GeometryRoot.gameObject);
            Transform root = Create("Geometry", map.transform);
            string guid = AssetDatabase.AssetPathToGUID(map.gameObject.scene.path);
            var serialized = new SerializedObject(map);
            serialized.FindProperty("geometryRoot").objectReferenceValue = root;
            serialized.FindProperty("geometryAssetPrefix").stringValue = guid + "_terrain";
            serialized.FindProperty("geometrySceneGuid").stringValue = guid;
            serialized.ApplyModifiedProperties();

            var floors = CaveMap.ReadCells(map.FloorTilemap);
            var rivers = CaveMap.ReadCells(map.RiverTilemap);
            CaveElevation elevation = map.Elevation;
            float wallBase = elevation != null
                ? floors.Min(cell => { Vector4 h = elevation.Surface(cell).Heights; return Mathf.Min(h.x,h.y,h.z,h.w); }) - map.CellSize * 0.5f
                : -0.5f * map.CellSize;
            var chunks = new Dictionary<Vector2Int, TerrainChunk>();
            TerrainChunk Chunk(Vector2Int cell)
            {
                var key = new Vector2Int(Mathf.FloorToInt((float)cell.x / ChunkSize), Mathf.FloorToInt((float)cell.y / ChunkSize));
                if (!chunks.TryGetValue(key, out var result)) chunks.Add(key, result = new TerrainChunk());
                return result;
            }
            Vector3 Point(float x, float y, float height)
            {
                Vector3 origin = map.LayoutGrid.transform.localPosition;
                float dx = style.NaturalOutline ? Mathf.Sin(x * 1.87f + y * 2.34f) * 0.14f : 0;
                float dz = style.NaturalOutline ? Mathf.Sin(x * 2.31f - y * 1.53f) * 0.14f : 0;
                return origin + new Vector3((x + dx) * map.CellSize, height, -(y + dz) * map.CellSize);
            }
            Vector3 BoundaryPoint(Vector3 visible)
            {
                Vector3 origin = map.LayoutGrid.transform.localPosition;
                return new Vector3(origin.x + Mathf.Round((visible.x-origin.x)/map.CellSize)*map.CellSize,
                    visible.y, origin.z + Mathf.Round((visible.z-origin.z)/map.CellSize)*map.CellSize);
            }
            float Height(float x, float y)
            {
                // Shared corner heights keep adjoining chunks seamless. No runtime generation.
                float noise = Mathf.PerlinNoise((x + style.Seed) * 0.16f, (y - style.Seed) * 0.16f);
                float baseHeight = 0f;
                float nearestY = y;
                if (elevation != null)
                {
                    float nearest = float.PositiveInfinity;
                    var around = new Vector2Int(Mathf.FloorToInt(x), Mathf.FloorToInt(y));
                    for (int dy = -3; dy <= 3; dy++)
                        for (int dx = -3; dx <= 3; dx++)
                        {
                            Vector2Int cell = around + new Vector2Int(dx, dy);
                            if (!floors.Contains(cell)) continue;
                            float distance = (new Vector2(cell.x + 0.5f, cell.y + 0.5f) - new Vector2(x, y)).sqrMagnitude;
                            if (distance >= nearest) continue;
                            nearest = distance;
                            nearestY = cell.y;
                            baseHeight = elevation.Height(cell.x + 0.5f, cell.y + 0.5f);
                        }
                }
                float wallHeight = style.NaturalOutline && nearestY > 43 ? 0.65f : style.RockHeight;
                return baseHeight + Mathf.Max(0.5f,wallHeight + (noise - 0.5f) * 2f * style.RockVariation) * map.CellSize;
            }

            foreach (Vector2Int cell in floors.OrderBy(c => c.y).ThenBy(c => c.x))
            {
                var chunk = Chunk(cell);
                var surface = elevation != null ? elevation.Surface(cell) : default;
                int material = elevation != null ? surface.Material : GroundMaterial(cell);
                bool river = rivers.Contains(cell);
                Vector4 heights = elevation != null ? surface.Heights : Vector4.zero;
                Vector3 a = Point(cell.x, cell.y, heights.x), b = Point(cell.x + 1, cell.y, heights.y);
                Vector3 c = Point(cell.x + 1, cell.y + 1, heights.z), d = Point(cell.x, cell.y + 1, heights.w);
                if (elevation == null || !river || surface.Bridge) chunk.Collision.Quad(a, b, c, d, 0);
                float bed = river ? -0.42f * map.CellSize : 0f;
                chunk.Ground.Quad(a + Vector3.up * bed, b + Vector3.up * bed,
                    c + Vector3.up * bed, d + Vector3.up * bed, river ? 2 : material);
                if (elevation != null && !river)
                {
                    Edge(Vector2Int.up, d, c, 0, 1);
                    Edge(Vector2Int.right, c, b, 3, 0);
                    Edge(Vector2Int.down, b, a, 2, 3);
                    Edge(Vector2Int.left, a, d, 1, 2);
                }
                void Edge(Vector2Int offset, Vector3 start, Vector3 end, int otherStart, int otherEnd)
                {
                    Vector2Int otherCell = cell + offset;
                    if (!floors.Contains(otherCell))
                    {
                        // Keep the physical limit on the authored grid while the visible rock contour varies.
                        Vector3 boundaryStart = BoundaryPoint(start), boundaryEnd = BoundaryPoint(end);
                        Vector3 boundarySize = new Vector3(Mathf.Abs(boundaryEnd.x-boundaryStart.x)+0.15f,1.5f,
                            Mathf.Abs(boundaryEnd.z-boundaryStart.z)+0.15f);
                        chunk.Collision.Box((boundaryStart+boundaryEnd)*0.5f+Vector3.up*0.75f,boundarySize,0);
                        return;
                    }
                    Vector4 other = elevation.Surface(otherCell).Heights;
                    bool waterEdge = rivers.Contains(otherCell) && !elevation.Surface(otherCell).Bridge;
                    float lowStart = waterEdge ? start.y - 0.42f * map.CellSize : other[otherStart];
                    float lowEnd = waterEdge ? end.y - 0.42f * map.CellSize : other[otherEnd];
                    if (!waterEdge && Mathf.Max(start.y - lowStart, end.y - lowEnd) <= 0.2f) return;
                    Vector3 lowerStart = new Vector3(start.x, lowStart, start.z);
                    Vector3 lowerEnd = new Vector3(end.x, lowEnd, end.z);
                    chunk.Cliff.Quad(start, end, lowerEnd, lowerStart, 0, Color.red);
                    // A visible low rock rim prevents accidental drops into the river or off a terrace.
                    Vector3 size = new Vector3(Mathf.Abs(end.x - start.x) + 0.12f, 0.45f,
                        Mathf.Abs(end.z - start.z) + 0.12f);
                    Vector3 center = (start + end) * 0.5f + Vector3.up * 0.225f;
                    chunk.Cliff.Box(center, size, 0, Color.red);
                    Vector3 collisionCenter = (BoundaryPoint(start)+BoundaryPoint(end))*0.5f+Vector3.up*0.225f;
                    chunk.Collision.Box(collisionCenter,size,0);
                }
                if (!river) continue;
                Color banks = new Color(
                    rivers.Contains(cell + Vector2Int.left) ? 0 : 1,
                    rivers.Contains(cell + Vector2Int.right) ? 0 : 1,
                    rivers.Contains(cell + Vector2Int.up) ? 0 : 1,
                    rivers.Contains(cell + Vector2Int.down) ? 0 : 1);
                Vector3 waterOffset = Vector3.down * (0.10f * map.CellSize);
                chunk.Water.Quad(a + waterOffset, b + waterOffset, c + waterOffset, d + waterOffset, 0, banks);
                // Real river banks meet the recessed bed; walkability retains the saved river rules.
                var edges = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
                var starts = new[] { d, c, b, a };
                var ends = new[] { c, b, a, d };
                for (int i = 0; i < 4; i++)
                    if (!rivers.Contains(cell + edges[i]))
                        chunk.Ground.Quad(ends[i], starts[i], starts[i] + Vector3.up * bed,
                            ends[i] + Vector3.up * bed, 2);
            }

            int xMin = floors.Min(c => c.x) - style.RockMargin;
            int xMax = floors.Max(c => c.x) + style.RockMargin;
            int yMin = floors.Min(c => c.y) - style.RockMargin;
            int yMax = floors.Max(c => c.y) + style.RockMargin;
            var outline = new HashSet<Vector2Int>();
            if (style.NaturalOutline)
                foreach (var floor in floors)
                    for (int dy = -style.RockMargin; dy <= style.RockMargin; dy++)
                        for (int dx = -style.RockMargin; dx <= style.RockMargin; dx++)
                            if (dx * dx + dy * dy <= style.RockMargin * style.RockMargin) outline.Add(floor + new Vector2Int(dx, dy));
            for (int y = yMin; y <= yMax; y++)
                for (int x = xMin; x <= xMax; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (floors.Contains(cell)) continue;
                    if (style.NaturalOutline && !outline.Contains(cell)) continue;
                    var rock = Chunk(cell).Rock;
                    Vector3 a = Point(x, y, Height(x, y)), b = Point(x + 1, y, Height(x + 1, y));
                    Vector3 c = Point(x + 1, y + 1, Height(x + 1, y + 1)), d = Point(x, y + 1, Height(x, y + 1));
                    rock.Quad(a, b, c, d, 1, Color.red);
                    if (floors.Contains(cell + Vector2Int.down) || y == yMin || style.NaturalOutline && !outline.Contains(cell + Vector2Int.down)) Wall(a, b, Vector3.forward);
                    if (floors.Contains(cell + Vector2Int.right) || x == xMax || style.NaturalOutline && !outline.Contains(cell + Vector2Int.right)) Wall(b, c, Vector3.right);
                    if (floors.Contains(cell + Vector2Int.up) || y == yMax || style.NaturalOutline && !outline.Contains(cell + Vector2Int.up)) Wall(c, d, Vector3.back);
                    if (floors.Contains(cell + Vector2Int.left) || x == xMin || style.NaturalOutline && !outline.Contains(cell + Vector2Int.left)) Wall(d, a, Vector3.left);
                    void Wall(Vector3 start, Vector3 end, Vector3 inward)
                    {
                        // A broken, sloping rock profile gives thickness without occupying floor cells.
                        Vector3 lowA = new Vector3(start.x, wallBase, start.z);
                        Vector3 lowB = new Vector3(end.x, wallBase, end.z);
                        Vector3 midA = Vector3.Lerp(lowA, start, 0.45f) - inward * (0.16f * map.CellSize);
                        Vector3 midB = Vector3.Lerp(lowB, end, 0.45f) - inward * (0.16f * map.CellSize);
                        rock.Quad(lowA, lowB, midB, midA, 0, Color.red);
                        rock.Quad(midA, midB, end, start, 0, Color.red);
                    }
                }

            foreach (var pair in chunks.OrderBy(p => p.Key.y).ThenBy(p => p.Key.x))
            {
                string name = "Terrain_" + pair.Key.x + "_" + pair.Key.y;
                string path = Folder + "/" + guid + "_" + name + ".asset";
                Transform chunk = Create(name, root);
                var data = pair.Value;
                if (data.Ground.HasVertices)
                {
                    Mesh visible = Save(data.Ground.Build(name + "_Ground"), path, "Ground");
                    Render("Ground", chunk, visible, new[] { style.Soil, style.Settlement, style.WetGround, style.Hall });
                    Mesh collision = Save(data.Collision.Build(name + "_WalkSurface"), path, "WalkSurface");
                    chunk.gameObject.AddComponent<MeshCollider>().sharedMesh = collision;
                }
                if (data.Rock.HasVertices)
                {
                    Mesh mesh = Save(data.Rock.Build(name + "_Rock"), path, "Rock");
                    var renderer = Render("RockMass", chunk, mesh, new[] { map.WallMaterial, style.RockCap });
                    renderer.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
                }
                if (data.Cliff.HasVertices)
                {
                    Mesh mesh = Save(data.Cliff.Build(name + "_Cliffs"), path, "Cliffs");
                    var renderer = Render("TerraceCliffs", chunk, mesh, new[] { map.WallMaterial });
                    renderer.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
                }
                if (data.Water.HasVertices)
                {
                    Mesh mesh = Save(data.Water.Build(name + "_Water"), path, "Water");
                    var water = Render("Water", chunk, mesh, new[] { map.RiverMaterial });
                    water.shadowCastingMode = ShadowCastingMode.Off;
                }
            }
            serialized.Update();
            serialized.FindProperty("geometryHash").stringValue = Hash(map, style);
            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
            AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(group);
        }

        private static int GroundMaterial(Vector2Int cell)
        {
            if (cell.x <= 15 || cell.x >= 86 && cell.y <= 42) return 2;
            if (cell.x >= 46 && cell.x <= 82 && cell.y <= 42 || cell.x >= 92 && cell.y <= 12) return 3;
            if (cell.x >= 18 && cell.x <= 84 && cell.y >= 44 && cell.y <= 84) return 1;
            return 0;
        }

        private static string Hash(CaveMap map, CaveTerrainStyle style)
        {
            var text = new StringBuilder("hd2d-terrain-v5|");
            text.Append(style.NaturalOutline).Append('|');
            foreach (float value in new[] { map.CellSize, style.RockHeight, style.RockVariation, style.RockMargin, style.Seed })
                text.Append(value.ToString("R", CultureInfo.InvariantCulture)).Append('|');
            text.Append(map.LayoutGrid.transform.localPosition).Append('|');
            if (map.Elevation != null)
            {
                text.Append("elevation-v1|");
                foreach (var surface in map.Elevation.Surfaces.OrderBy(s => s.Cell.y).ThenBy(s => s.Cell.x))
                {
                    text.Append(surface.Cell.x).Append(',').Append(surface.Cell.y).Append(':');
                    for (int i = 0; i < 4; i++) text.Append(surface.Heights[i].ToString("R", CultureInfo.InvariantCulture)).Append(',');
                    text.Append(surface.Material).Append(',').Append(surface.Bridge).Append(';');
                }
            }
            foreach (var material in new[] { style.Soil, style.Settlement, style.WetGround, style.Hall, style.RockCap, map.WallMaterial, map.RiverMaterial })
                text.Append(AssetDatabase.GetAssetPath(material)).Append('|');
            foreach (var cells in new[] { CaveMap.ReadCells(map.FloorTilemap), CaveMap.ReadCells(map.RiverTilemap) })
            {
                foreach (var cell in cells.OrderBy(c => c.y).ThenBy(c => c.x)) text.Append(cell.x).Append(',').Append(cell.y).Append(';');
                text.Append('|');
            }
            return Hash128.Compute(text.ToString()).ToString();
        }

        private static Transform Create(string name, Transform parent)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create cave terrain");
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static MeshRenderer Render(string name, Transform parent, Mesh mesh, Material[] materials)
        {
            Transform go = Create(name, parent);
            go.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            return renderer;
        }

        private static Mesh Save(Mesh mesh, string path, string part)
        {
            mesh.name = part;
            string mainName = Path.GetFileNameWithoutExtension(path);
            int mainSubmeshes = part == "Ground" ? 4 : part == "Rock" ? 2 : 0;
            var saved = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().FirstOrDefault(m =>
                m.name == part || m.name == mainName && m.subMeshCount == mainSubmeshes);
            if (saved != null)
            {
                mesh.name = saved.name;
                Undo.RegisterCompleteObjectUndo(saved, "Update cave terrain mesh");
                // Update through Mesh APIs so the open Editor refreshes its render buffers as well as saved data.
                saved.Clear();
                saved.indexFormat = mesh.indexFormat;
                saved.SetVertices(mesh.vertices);
                saved.SetNormals(mesh.normals);
                saved.SetUVs(0, mesh.uv);
                saved.SetColors(mesh.colors);
                saved.subMeshCount = mesh.subMeshCount;
                for (int i = 0; i < mesh.subMeshCount; i++) saved.SetTriangles(mesh.GetTriangles(i), i);
                saved.RecalculateBounds();
                EditorUtility.SetDirty(saved);
                UnityEngine.Object.DestroyImmediate(mesh);
                return saved;
            }
            if (AssetDatabase.LoadMainAssetAtPath(path) == null)
            {
                mesh.name = mainName;
                AssetDatabase.CreateAsset(mesh, path);
            }
            else AssetDatabase.AddObjectToAsset(mesh, path);
            return mesh;
        }

        private sealed class TerrainChunk
        {
            public readonly Surface Ground = new Surface(4);
            public readonly Surface Collision = new Surface(1);
            public readonly Surface Rock = new Surface(2);
            public readonly Surface Water = new Surface(1);
            public readonly Surface Cliff = new Surface(1);
        }

        private sealed class Surface
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<Vector3> normals = new List<Vector3>();
            private readonly List<Vector2> uvs = new List<Vector2>();
            private readonly List<Color> colors = new List<Color>();
            private readonly List<int>[] triangles;
            public bool HasVertices => vertices.Count > 0;
            public Surface(int count) => triangles = Enumerable.Range(0, count).Select(_ => new List<int>()).ToArray();

            public void Box(Vector3 center, Vector3 size, int material, Color color = default)
            {
                Vector3 half = size * 0.5f;
                Vector3 V(float x, float y, float z) => center + Vector3.Scale(half, new Vector3(x, y, z));
                Quad(V(-1,1,-1), V(1,1,-1), V(1,1,1), V(-1,1,1), material, color);
                Quad(V(-1,-1,-1), V(-1,1,-1), V(1,1,-1), V(1,-1,-1), material, color);
                Quad(V(1,-1,1), V(1,1,1), V(-1,1,1), V(-1,-1,1), material, color);
                Quad(V(-1,-1,1), V(-1,1,1), V(-1,1,-1), V(-1,-1,-1), material, color);
                Quad(V(1,-1,-1), V(1,1,-1), V(1,1,1), V(1,-1,1), material, color);
            }

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int material, Color color = default)
            {
                int index = vertices.Count;
                Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                bool top = Mathf.Abs(normal.y) > 0.5f;
                bool reverse = top && normal.y < 0f;
                if (reverse) normal = -normal;
                vertices.AddRange(new[] { a, b, c, d });
                for (int i = 0; i < 4; i++) { normals.Add(normal); colors.Add(color); }
                // X and Z UV axes match the river shader's left/right/up/down shore mask.
                uvs.AddRange(top
                    ? new[] { Vector2.up, Vector2.one, Vector2.right, Vector2.zero }
                    : new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up });
                triangles[material].AddRange(reverse
                    ? new[] { index, index + 2, index + 1, index, index + 3, index + 2 }
                    : new[] { index, index + 1, index + 2, index, index + 2, index + 3 });
            }

            public Mesh Build(string name)
            {
                var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32, subMeshCount = triangles.Length };
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetUVs(0, uvs);
                mesh.SetColors(colors);
                for (int i = 0; i < triangles.Length; i++) mesh.SetTriangles(triangles[i], i);
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
