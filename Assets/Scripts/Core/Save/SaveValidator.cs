using System;
using System.Collections.Generic;

namespace Untitled
{
    public static class SaveValidator
    {
        public static SaveResult Validate(SaveData data)
        {
            if (data == null) return Invalid("Missing save data.");
            if (data.SchemaVersion != SaveData.CurrentSchemaVersion)
                return SaveResult.Fail(SaveError.UnsupportedVersion, "Unsupported schemaVersion: " + data.SchemaVersion);
            if (data.Progress == null || data.Progress.Day < 1 || data.Progress.Day > 7
                || !Enum.IsDefined(typeof(GamePhase), data.Progress.Phase)) return Invalid("Invalid day or phase.");
            if (data.Economy == null || data.Economy.Coins < 0) return Invalid("Invalid coins.");
            if (data.Inventory?.Items == null || data.Inventory.Items.Count > 10000) return Invalid("Invalid inventory.");
            var ids = new HashSet<int>();
            foreach (var item in data.Inventory.Items)
                if (item == null || item.Id <= 0 || item.Quantity <= 0 || !ids.Add(item.Id)) return Invalid("Invalid or duplicate item.");
            if (!ValidOrder(data.Order)) return Invalid("Invalid order.");
            if (!ValidIds(data.Story?.Flags) || !ValidIds(data.Unlocks?.AbilityIds) || !ValidIds(data.Underground?.Flags))
                return Invalid("Invalid module IDs.");
            // Added fields are optional when reading the original schema-v1 slot.
            if ((data.AcceptedOrders != null && !ValidIds(data.AcceptedOrders))
                || (data.CompletedOrders != null && !ValidIds(data.CompletedOrders))) return Invalid("Invalid order IDs.");
            if (data.Package != null)
            {
                if (data.Package.Count > 10000) return Invalid("Invalid package.");
                ids.Clear();
                foreach (var item in data.Package)
                    if (item == null || item.ItemId <= 0 || item.Count <= 0 || !ids.Add(item.ItemId)) return Invalid("Invalid package item.");
            }
            return SaveResult.Ok();
        }

        internal static bool ValidOrder(OrderSaveData order) => order != null && order.Id != null && order.Id.Length <= 128
            && !float.IsNaN(order.Progress) && !float.IsInfinity(order.Progress) && order.Progress >= 0 && order.Progress <= 1
            && (!order.Completed || order.Progress == 1)
            && (order.Id.Length != 0 || (order.Progress == 0 && !order.Completed));

        private static bool ValidIds(List<string> values)
        {
            if (values == null || values.Count > 10000) return false;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (string value in values)
                if (string.IsNullOrWhiteSpace(value) || value.Length > 128 || !ids.Add(value)) return false;
            return true;
        }

        private static SaveResult Invalid(string message) => SaveResult.Fail(SaveError.InvalidData, message);
    }
}
