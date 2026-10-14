using UnityEngine;

namespace Untitled
{
    // Extends the existing Item table. Names, descriptions and valid IDs remain table-owned.
    [CreateAssetMenu(menuName = "Untitled/Inventory/Item Stack Definition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField, Min(1)] private int itemId = 1;
        [SerializeField, Min(1)] private int maxStack = 99;

        public int ItemId => itemId;
        public int MaxStack => Mathf.Max(1, maxStack);
    }
}
