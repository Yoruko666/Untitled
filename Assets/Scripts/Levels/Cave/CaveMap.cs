using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Untitled
{
    public sealed class CaveMap : MonoBehaviour
    {
        [SerializeField, HideInInspector] private CaveMapLayout layout;
        [SerializeField] private Grid layoutGrid;
        [SerializeField] private Tilemap floorTilemap;
        [SerializeField] private Tilemap riverTilemap;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private CaveElevation elevation;
        [SerializeField] private Material groundMaterial;
        [SerializeField] private Material wallMaterial;
        [SerializeField] private Material riverMaterial;
        [SerializeField, Min(0.1f)] private float wallHeight = 1.5f;
        [SerializeField] private Transform geometryRoot;
        [SerializeField, HideInInspector] private string geometryAssetPrefix;
        [SerializeField, HideInInspector] private string geometrySceneGuid;
        [SerializeField, HideInInspector] private string geometryHash;

        // This asset is an explicit import baseline, never the runtime layout.
        public CaveMapLayout ImportBaseline => layout;
        public Grid LayoutGrid => layoutGrid;
        public Tilemap FloorTilemap => floorTilemap;
        public Tilemap RiverTilemap => riverTilemap;
        public Transform SpawnPoint => spawnPoint;
        public CaveElevation Elevation => elevation;
        public bool HasEditableLayout => layoutGrid != null && floorTilemap != null && riverTilemap != null && spawnPoint != null;
        public float CellSize => layoutGrid.cellSize.x;
        public string GeometryAssetPrefix => geometryAssetPrefix;
        public string GeometrySceneGuid => geometrySceneGuid;
        public string GeometryHash => geometryHash;
        public Material GroundMaterial => groundMaterial;
        public Material WallMaterial => wallMaterial;
        public Material RiverMaterial => riverMaterial;
        public float WallHeight => wallHeight;
        public Transform GeometryRoot => geometryRoot;
        public Vector3 SpawnPosition => spawnPoint.position;

        private void Awake()
        {
            // Tilemap sprites are editing guides; 3D meshes provide the game render.
            foreach (var tilemap in new[] { floorTilemap, riverTilemap })
                if (tilemap != null && tilemap.TryGetComponent<TilemapRenderer>(out var renderer))
                    renderer.forceRenderingOff = true;
        }

        public Vector3 CellToWorld(Vector2Int cell)
        {
            Vector3 position = CaveCoordinates.CellCenter(cell, layoutGrid);
            if (elevation != null) position += transform.TransformVector(Vector3.up * elevation.Height(cell.x + 0.5f, cell.y + 0.5f));
            return position;
        }
        public Vector2Int WorldToCell(Vector3 position) => CaveCoordinates.WorldToCell(position, layoutGrid);

        public bool IsWalkable(Vector3 position)
        {
            if (!HasEditableLayout) return false;
            Vector2Int cell = WorldToCell(position);
            var tile = new Vector3Int(cell.x, cell.y, 0);
            return floorTilemap.HasTile(tile) && (elevation == null || !riverTilemap.HasTile(tile) || elevation.Surface(cell).Bridge);
        }

        public static HashSet<Vector2Int> ReadCells(Tilemap tilemap)
        {
            var cells = new HashSet<Vector2Int>();
            foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
                if (cell.z == 0 && tilemap.HasTile(cell)) cells.Add(new Vector2Int(cell.x, cell.y));
            return cells;
        }
    }
}
