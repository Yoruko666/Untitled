using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Untitled.UI;
using Panel = Untitled.UI.UIPanel;

namespace Untitled.Editor
{
    public static partial class GoblinUIBuilder
    {
        private static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
            return rect;
        }
        private static void Stretch(RectTransform rect, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(-right, -top);
        }
        private static TMP_Text Text(Transform parent, string name, string value, float x, float y, float width, float height,
            int size = 24, UIVisualRole role = UIVisualRole.Body)
        {
            var rect = Rect(name, parent, x, y, width, height);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = size; text.text = value;
            text.enableWordWrapping = true; text.overflowMode = TextOverflowModes.Ellipsis;
            text.richText = false; text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.margin = Vector4.zero;
            text.color = day.ColorFor(role);
            text.enableAutoSizing = true; text.fontSizeMax = size; text.fontSizeMin = Mathf.Max(18, size - 4);
            var themed = rect.gameObject.AddComponent<UIThemeGraphic>(); themed.Role = role;
            return text;
        }
        private static Image Image(Transform parent, string name, Sprite sprite, float x, float y, float width, float height)
        {
            var rect = Rect(name, parent, x, y, width, height);
            var image = rect.gameObject.AddComponent<Image>(); image.sprite = sprite;
            image.raycastTarget = false; image.preserveAspect = sprite != null && sprite.border == Vector4.zero;
            if (sprite != null && sprite.border != Vector4.zero) image.type = UnityEngine.UI.Image.Type.Sliced;
            return image;
        }
        private static Image ColorImage(Transform parent, string name, UIVisualRole role, float x, float y, float width, float height)
        {
            var image = Image(parent, name, null, x, y, width, height);
            image.color = day.ColorFor(role);
            var themed = image.gameObject.AddComponent<UIThemeGraphic>(); themed.Role = role;
            return image;
        }
        private static UIButton Button(Transform parent, string name, string label, UIButtonStyle style, float x, float y, float width = 264, float height = 60)
        {
            var image = Image(parent, name, day.PrimaryButtonSprite, x, y, width, height);
            image.raycastTarget = true;
            var control = image.gameObject.AddComponent<UnityEngine.UI.Button>(); control.targetGraphic = image;
            var icon = Image(image.transform, "Icon", null, 18, 16, 28, 28); icon.gameObject.SetActive(false);
            var text = Text(image.transform, "Label", label, 20, 0, width - 40, height, 24);
            Stretch(text.rectTransform, 20, 4, 20, 6); text.alignment = TextAlignmentOptions.Center;
            UnityEngine.Object.DestroyImmediate(text.GetComponent<UIThemeGraphic>());
            var button = image.gameObject.AddComponent<UIButton>(); button.Configure(style, text, icon); button.ApplyTheme(day);
            return button;
        }
        private static Panel Panel(Transform parent, string name, string title, Sprite icon, float x, float y, float width, float height)
        {
            var bg = Image(parent, name, day.PanelSprite, x, y, width, height);
            bg.raycastTarget = true; bg.gameObject.AddComponent<CanvasGroup>();
            var header = ColorImage(bg.transform, "HeaderBand", UIVisualRole.Inset, 14, 14, width - 28, 64);
            var headerRect = header.rectTransform; headerRect.anchorMax = new Vector2(1, 1); headerRect.sizeDelta = new Vector2(-28, 64);
            var emblem = Image(bg.transform, "TitleIcon", icon, 30, 29, 34, 34);
            var heading = Text(bg.transform, "Title", title, 80, 27, width - 112, 44, 28, UIVisualRole.Heading);
            heading.rectTransform.anchorMax = new Vector2(1, 1); heading.rectTransform.sizeDelta = new Vector2(-112, 44);
            var content = Rect("Content", bg.transform, 0, 0, width, height); Stretch(content, 26, 94, 26, 26);
            var panel = bg.gameObject.AddComponent<Panel>(); panel.Configure(bg, heading, emblem, content); panel.ApplyTheme(day);
            return panel;
        }
        private static void Save(GameObject value, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(value, Prefabs + "/" + name + ".prefab");
            UnityEngine.Object.DestroyImmediate(value);
        }
        private static void PreparePrefabs()
        {
            foreach (UIButtonStyle style in System.Enum.GetValues(typeof(UIButtonStyle)))
            {
                string label = style == UIButtonStyle.Primary ? "主要按钮" : style == UIButtonStyle.Secondary ? "次要按钮" : "危险按钮";
                var button = Button(null, "UIButton_" + style, label, style, 0, 0);
                Save(button.gameObject, "UIButton_" + style);
            }
            var generic = Panel(null, "UIPanel", "通用面板", day.QuestIcon, 0, 0, 560, 612);
            Save(generic.gameObject, "UIPanel");
            MakeOrder(); MakeQuestRow(); MakeQuestTracker();
        }
        private static void MakeOrder()
        {
            var panel = Panel(null, "UIOrderCard", "柜台订单", day.MilkTeaIcon, 0, 0, 560, 612);
            var root = panel.transform;
            var id = Text(root, "OrderId", "订单 / 003", 36, 106, 488, 32, 22, UIVisualRole.Muted);
            var iconFrame = ColorImage(root, "ItemInset", UIVisualRole.Inset, 36, 156, 110, 110);
            var item = Image(root, "ItemIcon", day.MilkTeaIcon, 48, 161, 86, 96);
            var title = Text(root, "DrinkTitle", "红糖珍珠奶茶", 166, 161, 356, 48, 32, UIVisualRole.Heading);
            var customer = Text(root, "Customer", "绿耳朵 · 少冰 / 加珍珠", 166, 216, 356, 54, 22, UIVisualRole.Muted);
            ColorImage(root, "Divider", UIVisualRole.Border, 36, 290, 488, 2);
            Text(root, "RecipeHeading", "配方清单", 36, 318, 488, 40, 26, UIVisualRole.Heading);
            var recipe = Text(root, "Recipe", "红茶 ×1     牛奶 ×1\n红糖 ×1     珍珠 ×2", 36, 370, 488, 74, 24);
            var bar = ColorImage(root, "ProgressTrack", UIVisualRole.Inset, 36, 466, 488, 14);
            var progress = ColorImage(bar.transform, "Fill", UIVisualRole.Progress, 0, 0, 488, 14);
            Stretch(progress.rectTransform, 2, 2, 2, 2);
            progress.sprite = fillSprite;
            progress.type = UnityEngine.UI.Image.Type.Filled; progress.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal; progress.fillAmount = .5f;
            var status = Text(root, "Status", "正在备料 · 2/4", 36, 490, 488, 32, 22, UIVisualRole.Muted);
            var action = Button(root, "Action", "查看制作台", UIButtonStyle.Primary, 36, 538, 488, 50);
            var card = root.gameObject.AddComponent<UIOrderCard>(); card.Configure(id, title, customer, recipe, status, item, progress, action);
            Save(root.gameObject, "UIOrderCard");
        }
        private static void MakeQuestRow()
        {
            var root = Rect("UIQuestRow", null, 0, 0, 508, 116);
            var layout = root.gameObject.AddComponent<LayoutElement>(); layout.preferredHeight = 116; layout.minHeight = 116;
            var icon = Image(root, "Icon", day.QuestIcon, 0, 5, 32, 32);
            var title = Text(root, "Title", "收集发光水晶", 46, 4, 350, 36, 24, UIVisualRole.Heading);
            title.rectTransform.anchorMax = new Vector2(1, 1); title.rectTransform.sizeDelta = new Vector2(-152, 36);
            var detail = Text(root, "Detail", "给店里的招牌换一颗新灯泡。", 46, 47, 460, 54, 21);
            detail.rectTransform.anchorMax = new Vector2(1, 1); detail.rectTransform.sizeDelta = new Vector2(-46, 54);
            var counter = Text(root, "Counter", "3 / 5", 406, 6, 100, 34, 22, UIVisualRole.Muted);
            counter.rectTransform.anchorMin = counter.rectTransform.anchorMax = new Vector2(1, 1);
            counter.rectTransform.anchoredPosition = new Vector2(-100, -6);
            counter.alignment = TextAlignmentOptions.TopRight;
            UnityEngine.Object.DestroyImmediate(counter.GetComponent<UIThemeGraphic>()); // Row controls complete state color.
            var divider = ColorImage(root, "Divider", UIVisualRole.Border, 46, 114, 460, 1);
            divider.rectTransform.anchorMax = new Vector2(1, 1); divider.rectTransform.sizeDelta = new Vector2(-46, 1);
            var row = root.gameObject.AddComponent<UIQuestRow>(); row.Configure(title, detail, counter, icon);
            Save(root.gameObject, "UIQuestRow");
        }
        private static void MakeQuestTracker()
        {
            var panel = Panel(null, "UIQuestTracker", "任务追踪", day.QuestIcon, 0, 0, 560, 480);
            var viewport = Rect("Viewport", panel.transform, 26, 96, 508, 344);
            viewport.anchorMax = new Vector2(1, 1); viewport.sizeDelta = new Vector2(-52, 344);
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>(); hit.color = Color.clear; hit.raycastTarget = true;
            var content = Rect("Rows", viewport, 0, 0, 508, 0);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.sizeDelta = new Vector2(0, 0);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true; layout.childControlHeight = true; layout.childForceExpandWidth = true; layout.childForceExpandHeight = false; layout.spacing = 14;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = panel.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = content;
            scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 24;
            Text(panel.transform, "ScrollHint", "滚轮 / PgUp PgDn 查看任务", 26, 440, 508, 32, 18, UIVisualRole.Muted);
            var tracker = panel.gameObject.AddComponent<UIQuestTracker>();
            tracker.Configure(content, AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/UIQuestRow.prefab").GetComponent<UIQuestRow>());
            Save(panel.gameObject, "UIQuestTracker");
        }
    }
}
