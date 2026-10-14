using System.Collections.Generic;
using UnityEngine;

namespace Untitled.UI
{
    public sealed class UIQuestTracker : UIThemedView
    {
        [SerializeField] private RectTransform rowsRoot;
        [SerializeField] private UIQuestRow rowPrefab;
        private readonly List<UIQuestRow> rows = new List<UIQuestRow>();
        public void Configure(RectTransform root, UIQuestRow prefab) { rowsRoot = root; rowPrefab = prefab; }
        public void Bind(IReadOnlyList<UIQuestData> quests)
        {
            int count = quests?.Count ?? 0;
            if (rowsRoot == null || rowPrefab == null) return;
            while (rows.Count < count) rows.Add(Instantiate(rowPrefab, rowsRoot));
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].SetVisible(i < count);
                if (i < count) rows[i].Bind(quests[i]);
            }
        }
        public override void ApplyTheme(UIThemeDefinition theme) { }
    }
}
