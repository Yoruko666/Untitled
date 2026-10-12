using System.Collections.Generic;
using Untitled.Config;

namespace Untitled
{
    public sealed class DialogDatabase : DatabaseBase<Dialog>
    {
        protected override int KeyOf(Dialog row)
        {
            return row.Id;
        }

        protected override IEnumerable<Dialog> Parse(byte[] bytes)
        {
            return DialogTable.Parser.ParseFrom(bytes).Dialogs;
        }
    }
}
