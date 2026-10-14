using System;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using Untitled.UI;

namespace Untitled.Editor
{
    public static partial class GoblinUIBuilder
    {
        public const string ExtendedScenePath = "Assets/Scenes/UI/UIShowcaseExtended.unity";
        private static Sprite milkIcon, leafIcon, woodIcon, crystalIcon, pearlsIcon, bagIcon, goblinIcon;
        private static void PrepareExtendedAssets()
        {
            PrepareAssets();
            milkIcon = ExtendedIcon("milk"); leafIcon = ExtendedIcon("leaf"); woodIcon = ExtendedIcon("wood");
            crystalIcon = ExtendedIcon("crystal"); pearlsIcon = ExtendedIcon("pearls"); bagIcon = ExtendedIcon("bag"); goblinIcon = ExtendedIcon("goblin");
            foreach (var theme in new[] { day, night })
            {
                if (theme.ItemFallbackIcon == null) theme.ItemFallbackIcon = bagIcon;
                if (theme.NPCPortrait == null) theme.NPCPortrait = goblinIcon;
                EditorUtility.SetDirty(theme);
            }
            string characters = "";
            foreach (var folder in new[] { "Assets/Scripts/UI/Goblin", "Assets/Editor/GoblinUI" })
                foreach (var path in Directory.GetFiles(folder, "*.cs")) characters += File.ReadAllText(path);
            font.TryAddCharacters(characters, out string missing); EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
        }
        private static Sprite ExtendedIcon(string kind)
        {
            var t = NewTexture(32, 32); var ink = C("2D261E");
            if (kind == "crystal")
            {
                for (int y = 3; y < 30; y++) for (int x = 4; x < 29; x++)
                    if (Mathf.Abs(x - 16) < (y < 18 ? (y - 1) * .45f : (31 - y) * .55f))
                        t.SetPixel(x, y, x < 15 ? C("25AFCB") : C("7DECF2"));
                Fill(t, 13, 8, 2, 18, C("E5FFFF")); Fill(t, 19, 11, 2, 10, C("16849C"));
            }
            else if (kind == "leaf")
            {
                for (int y = 7; y < 26; y++) for (int x = 5; x < 27; x++)
                    if (Mathf.Pow((x - 16) / 10f, 2) + Mathf.Pow((y - 16) / 10f, 2) < 1) t.SetPixel(x, y, x + y < 33 ? C("416840") : C("8AB35F"));
                for (int i = 4; i < 24; i++) Fill(t, i, i - 1, 2, 2, C("D1CF85"));
            }
            else if (kind == "wood")
            {
                Fill(t, 5, 5, 22, 22, ink); Fill(t, 7, 7, 18, 18, C("AD7342"));
                Fill(t, 8, 22, 16, 3, C("DAA66B")); Fill(t, 12, 7, 2, 15, C("74492E")); Fill(t, 19, 7, 2, 15, C("74492E"));
            }
            else if (kind == "milk")
            {
                Fill(t, 8, 4, 17, 23, ink); Fill(t, 10, 6, 13, 18, C("F4EACC"));
                Fill(t, 10, 7, 13, 9, C("7095AB")); Fill(t, 12, 19, 8, 3, Color.white);
                Fill(t, 10, 27, 13, 2, C("D1C5A4")); Fill(t, 13, 29, 9, 2, ink);
            }
            else if (kind == "pearls")
            {
                foreach (var p in new[] { new Vector2Int(6, 6), new Vector2Int(17, 7), new Vector2Int(10, 16), new Vector2Int(22, 17) })
                { Fill(t, p.x, p.y, 7, 7, C("4C302A")); Fill(t, p.x + 1, p.y + 4, 3, 2, C("AF8060")); }
            }
            else if (kind == "bag")
            {
                Fill(t, 9, 4, 15, 18, ink); Fill(t, 7, 8, 19, 12, ink); Fill(t, 9, 9, 15, 11, C("A77A45"));
                Fill(t, 10, 5, 13, 5, C("845733")); Fill(t, 12, 21, 9, 5, C("C29656")); Fill(t, 10, 21, 13, 2, C("F0CD7F"));
            }
            else
            {
                Fill(t, 7, 5, 19, 21, ink); Fill(t, 8, 7, 17, 17, C("77A04E"));
                Fill(t, 2, 16, 8, 6, ink); Fill(t, 25, 16, 6, 6, ink); Fill(t, 3, 18, 7, 3, C("9ABE6D")); Fill(t, 25, 18, 5, 3, C("9ABE6D"));
                Fill(t, 8, 25, 18, 4, C("BC5048")); Fill(t, 11, 28, 13, 3, C("EEE0C0"));
                Fill(t, 11, 17, 3, 2, ink); Fill(t, 20, 17, 3, 2, ink); Fill(t, 13, 11, 9, 2, ink); Fill(t, 19, 11, 2, 2, Color.white);
            }
            return SaveSprite(kind + "_extended_placeholder", t, Vector4.zero);
        }
        private static void RegisterDialogue()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) throw new InvalidOperationException("Existing Addressables settings are missing.");
            var group = settings.FindGroup("Panels");
            if (group == null) throw new InvalidOperationException("Existing Panels group is missing.");
            var guid = AssetDatabase.AssetPathToGUID(Prefabs + "/UI_Dialogue.prefab");
            foreach (var existing in group.entries)
                if (existing.address == Untitled.UIPanel.Dialogue && existing.guid != guid)
                    throw new InvalidOperationException("UI_Dialogue address is already assigned; preserve the existing panel.");
            var entry = settings.CreateOrMoveEntry(guid, group); entry.address = Untitled.UIPanel.Dialogue;
            EditorUtility.SetDirty(group); EditorUtility.SetDirty(settings);
        }
    }
}
