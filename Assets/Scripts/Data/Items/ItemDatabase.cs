using System.Collections.Generic;
using Untitled.Config;

namespace Untitled
{
    public sealed class ItemDatabase : DatabaseBase<Item>
    {
        protected override int KeyOf(Item row)
        {
            return row.Id;
        }

        protected override IEnumerable<Item> Parse(byte[] bytes)
        {
            return ItemTable.Parser.ParseFrom(bytes).Items;
        }
    }
}
