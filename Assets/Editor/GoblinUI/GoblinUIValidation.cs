using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Untitled.UI;

namespace Untitled.Editor
{
    public static class GoblinUIValidation
    {
        [MenuItem("Untitled/UI/Validate First Round Assets")]
        public static void Validate()
        {
            foreach (var name in new[] { "DayTheme", "NightTheme" })
            {
                var theme = AssetDatabase.LoadAssetAtPath<UIThemeDefinition>(GoblinUIBuilder.Themes + "/" + name + ".asset");
                Require(theme != null && theme.BodyFont != null && theme.PanelSprite != null && theme.PrimaryButtonSprite != null, "Missing theme dependencies: " + name);
                Require(theme.PanelSprite.border.x > 0, "Panel must be nine-sliced.");
                Require(theme.BodyFont.sourceFontFile != null && theme.BodyFont.isMultiAtlasTexturesEnabled, "Dynamic Chinese font source must be shipped.");
                // Chinese basic block through U+9FEF + Latin/digits/common punctuation, checked against the actual source font.
                var characters = new string(Enumerable.Range(0x4e00, 0x9fef - 0x4e00 + 1).Select(c => (char)c).ToArray());
                characters += "中文简体，。！？：；（）【】《》、‘’“”￥…—0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ+-×/%&@#=<>";
                // Validate source glyph coverage without allocating thousands of SDF pages.
                UnityEngine.TextCore.LowLevel.FontEngine.LoadFontFace(theme.BodyFont.sourceFontFile, 36);
                var missing = characters.Distinct().Where(c => !UnityEngine.TextCore.LowLevel.FontEngine.TryGetGlyphWithUnicodeValue(c, UnityEngine.TextCore.LowLevel.GlyphLoadFlags.LOAD_NO_BITMAP, out var glyph)).ToArray();
                Require(missing.Length == 0, "Source font misses glyphs: " + new string(missing));
            }
            foreach (var name in new[] { "UIButton_Primary", "UIButton_Secondary", "UIButton_Danger", "UIPanel", "UIOrderCard", "UIQuestRow", "UIQuestTracker" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GoblinUIBuilder.Prefabs + "/" + name + ".prefab");
                Require(prefab != null, "Missing prefab " + name);
                foreach (var transform in prefab.GetComponentsInChildren<Transform>(true))
                    Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "Missing script in " + name);
                foreach (var t in prefab.GetComponentsInChildren<TMP_Text>(true)) Require(t.font != null, "Missing text font: " + t.name);
            }
            Debug.Log("GOBLIN_UI_ASSET_VALIDATION_OK: themes, prefab dependencies, sliced borders, source glyph coverage U+4E00–U+9FEF and common symbols. Play Mode not implied.");
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
