using System;
using System.Collections.Generic;
using UnityEngine;

namespace Untitled
{
    // Scene-authored heights accompany the Floor/River Tilemaps. Rebuilding reads them, never replaces them.
    [DisallowMultipleComponent]
    public sealed class CaveElevation : MonoBehaviour
    {
        [Serializable]
        public struct CellSurface
        {
            public Vector2Int Cell;
            // Corners: (x,y), (x+1,y), (x+1,y+1), (x,y+1), in world units.
            public Vector4 Heights;
            [Range(0, 3)] public int Material;
            public bool Bridge;
        }

        public List<CellSurface> Surfaces = new();
        private Dictionary<Vector2Int, CellSurface> lookup;

        public void Invalidate() => lookup = null;
        private void OnValidate() => Invalidate();

        public CellSurface Surface(Vector2Int cell)
        {
            if (lookup == null)
            {
                lookup = new Dictionary<Vector2Int, CellSurface>(Surfaces.Count);
                foreach (CellSurface surface in Surfaces) lookup[surface.Cell] = surface;
            }
            return lookup.TryGetValue(cell, out CellSurface result)
                ? result : new CellSurface { Cell = cell, Material = 1 };
        }

        public float Height(float x, float y)
        {
            var cell = new Vector2Int(Mathf.FloorToInt(x), Mathf.FloorToInt(y));
            Vector4 h = Surface(cell).Heights;
            float u = x - cell.x, v = y - cell.y;
            // Match the terrain's a-b-c / a-c-d triangles, including edited non-planar quads.
            return u >= v
                ? h.x + u * (h.y - h.x) + v * (h.z - h.y)
                : h.x + v * (h.w - h.x) + u * (h.z - h.w);
        }
    }
}
