namespace Untitled
{
    // Exploration staging remains a separate container, using the shared inventory rules.
    public class PackageData : InventoryData
    {
        public PackageData(IItemCatalog catalog = null) : base(catalog) { }
    }
}
