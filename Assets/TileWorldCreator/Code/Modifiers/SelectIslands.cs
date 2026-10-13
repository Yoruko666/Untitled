/*

  _____ _ _    __        __         _     _  ____                _
 |_   _(_) | __\ \      / /__  _ __| | __| |/ ___|_ __ ___  __ _| |_ ___  _ __
   | | | | |/ _ \ \ /\ / / _ \| '__| |/ _` | |   | '__/ _ \/ _` | __/ _ \| '__|
   | | | | |  __/\ V  V / (_) | |  | | (_| | |___| | |  __/ (_| | || (_) | |
   |_| |_|_|\___| \_/\_/ \___/|_|  |_|\__,_|\____|_|  \___|\__,_|\__\___/|_|

    TileWorldCreator (c) by Giant Grey
    Author: Marc Egli

    www.giantgrey.com

*/
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif
using UnityEngine;
using UnityEngine.UIElements;
using GiantGrey.TileWorldCreator.Attributes;

namespace GiantGrey.TileWorldCreator
{
    [Modifier(ModifierAttribute.Category.Modifiers, "Select Islands", "")]
    public class SelectIslands : BlueprintModifier
    {
        public enum FilterMode
        {
            SmallerThan,
            LargerThan,
            EqualTo
        }

        [HideInInspector]
        public FilterMode filterMode = FilterMode.SmallerThan;

        [HideInInspector]
        public int thresholdSize = 10;

        BlueprintLayer layer;

        public override HashSet<Vector2> Execute(HashSet<Vector2> _positions, BlueprintLayer _layer)
        {
            layer = _layer;
            return SelectIslandsBySize(_positions);
        }

        HashSet<Vector2> SelectIslandsBySize(HashSet<Vector2> _positions)
        {
            HashSet<Vector2> result = new HashSet<Vector2>();
            HashSet<Vector2> visited = new HashSet<Vector2>();

            foreach (var pos in _positions)
            {
                if (visited.Contains(pos))
                    continue;

                // Find all connected cells in this island
                HashSet<Vector2> island = FloodFill(pos, _positions, visited);

                // Check if island meets the threshold criteria
                bool meetsThreshold = false;
                switch (filterMode)
                {
                    case FilterMode.SmallerThan:
                        meetsThreshold = island.Count < thresholdSize;
                        break;
                    case FilterMode.LargerThan:
                        meetsThreshold = island.Count > thresholdSize;
                        break;
                    case FilterMode.EqualTo:
                        meetsThreshold = island.Count == thresholdSize;
                        break;
                }

                // Add island to result if it meets criteria
                if (meetsThreshold)
                {
                    foreach (var cell in island)
                    {
                        result.Add(cell);
                    }
                }
            }

            return result;
        }

        HashSet<Vector2> FloodFill(Vector2 start, HashSet<Vector2> allPositions, HashSet<Vector2> visited)
        {
            HashSet<Vector2> island = new HashSet<Vector2>();
            Queue<Vector2> queue = new Queue<Vector2>();

            queue.Enqueue(start);
            visited.Add(start);
            island.Add(start);

            // Four directional neighbors
            Vector2[] directions = new Vector2[]
            {
                new Vector2(1, 0),   // Right
                new Vector2(-1, 0),  // Left
                new Vector2(0, 1),   // Up
                new Vector2(0, -1)   // Down
            };

            while (queue.Count > 0)
            {
                Vector2 current = queue.Dequeue();

                foreach (var dir in directions)
                {
                    Vector2 neighbor = current + dir;

                    if (allPositions.Contains(neighbor) && !visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        island.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return island;
        }

#if UNITY_EDITOR
        public override VisualElement BuildInspector(Configuration _asset)
        {
            var root = new VisualElement();
            var so = new SerializedObject(this);

            var dropdown = new DropdownField();
            dropdown.BindProperty(so.FindProperty(nameof(filterMode)));

            var threshold = new PropertyField();
            threshold.BindProperty(so.FindProperty(nameof(thresholdSize)));

            root.Add(dropdown);
            root.Add(threshold);

            return root;
        }
#endif
    }
}
