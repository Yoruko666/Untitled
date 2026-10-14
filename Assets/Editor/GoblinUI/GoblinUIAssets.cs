using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.U2D;
using Untitled.UI;

namespace Untitled.Editor
{
    public static partial class GoblinUIBuilder
    {
        public const string ScenePath = "Assets/Scenes/UI/UIShowcase.unity";
        public const string Prefabs = "Assets/Prefabs/UI/Goblin";
        public const string Themes = "Assets/Configs/UI/Goblin";
        public const string Art = "Assets/Art/UI/Goblin";
        private static UIThemeDefinition day, night;
        private static TMP_FontAsset font;
        private static Sprite fillSprite;
        private static Color C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var color); return color; }

        private static void PrepareAssets()
        {
            foreach (var folder in new[] { Prefabs, Themes, Art + "/Sprites", "Assets/Scenes/UI" }) Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Art + "/Fonts/noto_sans_sc_sdf.asset");
            if (font == null)
            {
                var source = AssetDatabase.LoadAssetAtPath<Font>(Art + "/Fonts/NotoSansCJKsc-Regular.otf");
                if (source == null) throw new InvalidOperationException("Missing licensed Noto CJK font.");
                font = TMP_FontAsset.CreateFontAsset(source, 36, 5, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
                font.name = "Noto Sans SC SDF";
                font.isMultiAtlasTexturesEnabled = true;
                AssetDatabase.CreateAsset(font, Art + "/Fonts/noto_sans_sc_sdf.asset");
                font.material.name = "Noto Sans SC Material";
                AssetDatabase.AddObjectToAsset(font.material, font);
                foreach (var texture in font.atlasTextures) { texture.name = "Noto Sans SC Atlas"; AssetDatabase.AddObjectToAsset(texture, font); }
                // Seed the authored UI, while retaining full dynamic CJK support from the shipped OTF.
                var characters = "";
                foreach (var file in Directory.GetFiles("Assets/Scripts/UI/Goblin", "*.cs")) characters += File.ReadAllText(file);
                foreach (var file in Directory.GetFiles("Assets/Editor/GoblinUI", "*.cs")) characters += File.ReadAllText(file);
                font.TryAddCharacters(characters, out string missing);
                EditorUtility.SetDirty(font);
            }
            var fillTexture = NewTexture(4, 4); Fill(fillTexture, 0, 0, 4, 4, Color.white);
            fillSprite = SaveSprite("solid_placeholder", fillTexture, Vector4.zero);
            var tea = Icon("milk_tea_placeholder", "tea");
            var quest = Icon("quest_placeholder", "quest");
            var coin = Icon("coin_placeholder", "coin");
            day = MakeTheme(false, tea, quest, coin);
            night = MakeTheme(true, tea, quest, coin);
            var atlasPath = Art + "/goblin_ui.spriteatlas";
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            if (atlas == null)
            {
                atlas = new SpriteAtlas();
                var packing = atlas.GetPackingSettings(); packing.enableRotation = false; packing.enableTightPacking = false; packing.padding = 4;
                atlas.SetPackingSettings(packing);
                var textureSettings = atlas.GetTextureSettings(); textureSettings.filterMode = FilterMode.Point; textureSettings.generateMipMaps = false;
                atlas.SetTextureSettings(textureSettings);
                var platform = atlas.GetPlatformSettings("DefaultTexturePlatform"); platform.maxTextureSize = 1024; platform.textureCompression = TextureImporterCompression.Uncompressed;
                atlas.SetPlatformSettings(platform);
                atlas.Add(new UnityEngine.Object[] { AssetDatabase.LoadAssetAtPath<DefaultAsset>(Art + "/Sprites") });
                AssetDatabase.CreateAsset(atlas, atlasPath);
            }
            AssetDatabase.SaveAssets();
        }

        private static UIThemeDefinition MakeTheme(bool dark, Sprite tea, Sprite quest, Sprite coin)
        {
            string name = dark ? "NightTheme" : "DayTheme";
            var value = AssetDatabase.LoadAssetAtPath<UIThemeDefinition>(Themes + "/" + name + ".asset");
            if (value != null) return value; // Preserve artist edits; generation is not a runtime dependency.
            value = ScriptableObject.CreateInstance<UIThemeDefinition>();
            value.DisplayName = dark ? "夜晚 · 地下探索" : "白天 · 奶茶店";
            value.Background = C(dark ? "101B29" : "EFE3C9");
            value.Surface = C(dark ? "22303E" : "FFF1D4");
            value.Inset = C(dark ? "16212D" : "E6D1AA");
            value.Border = C(dark ? "9B7448" : "704528");
            value.Text = C(dark ? "F6EACF" : "38271D");
            value.Muted = C(dark ? "B9C4C8" : "715A45");
            value.Heading = C(dark ? "FFD777" : "493022");
            value.Accent = C(dark ? "F1C465" : "397451");
            value.Danger = C(dark ? "D96C5F" : "A13E35");
            value.PrimaryText = C(dark ? "302114" : "FFF3D8");
            value.SecondaryText = C(dark ? "F6EACF" : "FFF3D8");
            value.DangerText = C("FFF1DA");
            value.BodyFont = value.TitleFont = font;
            value.PanelSprite = PanelSprite(dark);
            value.BackgroundSprite = value.PanelSprite;
            value.BorderSprite = PanelSprite(dark, true);
            value.PrimaryButtonSprite = ButtonSprite(name.ToLowerInvariant() + "_primary_placeholder", value.Accent);
            value.SecondaryButtonSprite = ButtonSprite(name.ToLowerInvariant() + "_secondary_placeholder", C(dark ? "465765" : "815533"));
            value.DangerButtonSprite = ButtonSprite(name.ToLowerInvariant() + "_danger_placeholder", C(dark ? "813C3A" : "A34638"));
            value.MilkTeaIcon = tea; value.QuestIcon = quest; value.CoinIcon = coin;
            value.PhaseIcon = Icon(dark ? "moon_placeholder" : "sun_placeholder", dark ? "moon" : "sun");
            AssetDatabase.CreateAsset(value, Themes + "/" + name + ".asset");
            return value;
        }

        private static Sprite PanelSprite(bool dark, bool borderOnly = false)
        {
            const int size = 64;
            var texture = NewTexture(size, size);
            var outer = C(dark ? "0B111B" : "372419");
            var wood = C(dark ? "493F39" : "8A5735");
            var light = C(dark ? "AF8750" : "D89A57");
            var inner = C(dark ? "22303E" : "FFF1D4");
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                int edge = Math.Min(Math.Min(x, y), Math.Min(size - 1 - x, size - 1 - y));
                Color color = edge < 2 ? outer : edge < 4 ? light : edge < 9 ? wood : edge < 11 ? outer : edge < 12 ? light : inner;
                if (edge >= 4 && edge < 9 && (x + y * 3) % 13 == 0) color *= .8f;
                if (edge > 11 && borderOnly) color = Color.clear;
                texture.SetPixel(x, y, color);
            }
            // Square brass nails and cut pixel corners belong to the fixed nine-slice border.
            foreach (int y in new[] { 5, 55 }) foreach (int x in new[] { 5, 55 })
            { Fill(texture, x, y, 4, 4, outer); Fill(texture, x, y + 1, 3, 3, C("E7BA6A")); Fill(texture, x, y + 2, 1, 1, C("FFF0AE")); }
            foreach (int y in new[] { 0, 62 }) foreach (int x in new[] { 0, 62 }) Fill(texture, x, y, 2, 2, Color.clear);
            return SaveSprite((dark ? "night" : "day") + (borderOnly ? "_border_placeholder" : "_panel_placeholder"), texture, new Vector4(14, 14, 14, 14));
        }

        private static Sprite ButtonSprite(string name, Color face)
        {
            var texture = NewTexture(64, 32);
            for (int y = 0; y < 32; y++) for (int x = 0; x < 64; x++)
            {
                int edge = Math.Min(Math.Min(x, y), Math.Min(63 - x, 31 - y));
                bool clipped = (x < 4 || x > 59) && (y < 4 || y > 27);
                var color = edge < 2 ? C("32281E") : edge < 3 ? C("D9AE64") : edge < 5 ? face * .6f : face;
                if (y >= 25 && edge >= 5) color = Color.Lerp(face, Color.white, .22f);
                if (y < 7 && edge >= 3) color = face * .55f;
                texture.SetPixel(x, y, clipped ? Color.clear : color);
            }
            foreach (int x in new[] { 6, 55 }) { Fill(texture, x, 21, 3, 2, C("E7D39A")); Fill(texture, x + 1, 19, 1, 6, C("E7D39A")); }
            return SaveSprite(name, texture, new Vector4(12, 10, 12, 10));
        }

        private static Sprite Icon(string name, string kind)
        {
            var t = NewTexture(32, 32);
            var ink = C("2D261E");
            if (kind == "tea")
            {
                Fill(t, 9, 4, 15, 22, ink); Fill(t, 10, 6, 13, 18, C("F7D7A5"));
                Fill(t, 12, 7, 9, 14, C("D98542")); Fill(t, 8, 24, 17, 4, ink); Fill(t, 9, 25, 15, 2, C("FFF0C9"));
                Fill(t, 18, 26, 3, 6, ink); Fill(t, 19, 27, 1, 5, C("DC6F59"));
                foreach (var p in new[] { new Vector2Int(12, 8), new Vector2Int(18, 9), new Vector2Int(15, 12), new Vector2Int(20, 14), new Vector2Int(11, 15) }) Fill(t, p.x, p.y, 2, 2, ink);
                Fill(t, 11, 19, 2, 4, C("FFF7E6"));
            }
            else if (kind == "coin" || kind == "sun")
            {
                for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                    if (d < 11) t.SetPixel(x, y, d > 9 ? ink : d > 7 ? C("C78930") : C("FFD56A"));
                }
                if (kind == "coin") { Fill(t, 14, 10, 3, 12, C("A76226")); Fill(t, 11, 18, 9, 2, C("A76226")); Fill(t, 11, 12, 9, 2, C("A76226")); }
                else { Fill(t, 14, 0, 3, 4, C("FFD56A")); Fill(t, 14, 28, 3, 4, C("FFD56A")); Fill(t, 0, 14, 4, 3, C("FFD56A")); Fill(t, 28, 14, 4, 3, C("FFD56A")); }
                Fill(t, 11, 21, 6, 2, C("FFF3BC"));
            }
            else if (kind == "moon")
            {
                for (int y = 3; y < 29; y++) for (int x = 3; x < 29; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(16, 16));
                    float cut = Vector2.Distance(new Vector2(x, y), new Vector2(22, 20));
                    if (d < 12 && cut > 11) t.SetPixel(x, y, d > 10 ? C("778BAA") : C("DCE7F2"));
                }
                Fill(t, 26, 7, 2, 4, C("FFD56A")); Fill(t, 25, 8, 4, 2, C("FFD56A"));
            }
            else
            {
                Fill(t, 7, 4, 19, 24, ink); Fill(t, 9, 6, 14, 20, C("F9DEA6"));
                Fill(t, 5, 25, 21, 3, C("98633C")); Fill(t, 8, 4, 20, 3, C("98633C"));
                Fill(t, 12, 20, 8, 2, C("6B482F")); Fill(t, 12, 15, 7, 2, C("6B482F")); Fill(t, 12, 10, 4, 2, C("6B482F"));
            }
            return SaveSprite(name, t, Vector4.zero);
        }
        private static Texture2D NewTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels(new Color[width * height]); return texture;
        }
        private static void Fill(Texture2D t, int x, int y, int width, int height, Color color)
        { for (int j = y; j < y + height; j++) for (int i = x; i < x + width; i++) t.SetPixel(i, j, color); }
        private static Sprite SaveSprite(string name, Texture2D texture, Vector4 border)
        {
            string path = Art + "/Sprites/" + name + ".png";
            if (!File.Exists(path)) { texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); }
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.spriteBorder = border;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
