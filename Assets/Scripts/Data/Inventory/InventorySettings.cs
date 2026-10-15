using System;
using UnityEngine;

namespace Untitled
{
    [CreateAssetMenu(menuName = "Untitled/Inventory/Inventory Settings")]
    public sealed class InventorySettings : ScriptableObject
    {
        [SerializeField] private bool limitCapacity;
        [SerializeField, Min(1)] private int capacity = 24;
        [SerializeField] private ItemDefinition[] items = Array.Empty<ItemDefinition>();

        // Zero disables the slot limit. Settings are copied at session creation.
        public int Capacity => limitCapacity ? Mathf.Max(1, capacity) : 0;
        public System.Collections.Generic.IReadOnlyList<ItemDefinition> Items => Array.AsReadOnly(items ?? Array.Empty<ItemDefinition>());
    }
}
