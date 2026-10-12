using System.Collections.Generic;
using Untitled.Config;

namespace Untitled
{
    public sealed class DialogOptionDatabase : DatabaseBase<DialogOption>
    {
        private Dictionary<int, List<DialogOption>> optionsByDialog;

        protected override int KeyOf(DialogOption row)
        {
            return row.Id;
        }

        protected override IEnumerable<DialogOption> Parse(byte[] bytes)
        {
            return DialogOptionTable.Parser.ParseFrom(bytes).DialogOptions;
        }

        public override void Load(string address)
        {
            base.Load(address);
            optionsByDialog = null;
        }

        public List<DialogOption> GetOptions(int dialogId)
        {
            if (optionsByDialog == null) BuildIndex();

            return optionsByDialog.TryGetValue(dialogId, out List<DialogOption> options) ? options : null;
        }

        private void BuildIndex()
        {
            optionsByDialog = new Dictionary<int, List<DialogOption>>();

            foreach (DialogOption option in All.Values)
            {
                if (!optionsByDialog.TryGetValue(option.DialogId, out List<DialogOption> list))
                {
                    list = new List<DialogOption>();
                    optionsByDialog.Add(option.DialogId, list);
                }

                list.Add(option);
            }
        }
    }
}
