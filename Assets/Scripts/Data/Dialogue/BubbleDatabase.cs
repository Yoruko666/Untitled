using System.Collections.Generic;
using Untitled.Config;

namespace Untitled
{
    public sealed class BubbleDatabase : DatabaseBase<Bubble>
    {
        protected override int KeyOf(Bubble row)
        {
            return row.Id;
        }

        protected override IEnumerable<Bubble> Parse(byte[] bytes)
        {
            return BubbleTable.Parser.ParseFrom(bytes).Bubbles;
        }
    }
}
