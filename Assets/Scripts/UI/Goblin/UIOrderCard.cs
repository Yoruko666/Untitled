using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Untitled.UI
{
    public sealed class UIOrderCard : UIThemedView
    {
        [SerializeField] private TMP_Text orderId, title, customer, recipe, status;
        [SerializeField] private Image icon, progress;
        [SerializeField] private UIButton action;
        public UnityEvent<string> ActionRequested = new UnityEvent<string>();
        private UIOrderData data;
        private UIThemeDefinition theme;
        public UIOrderData Data => data;
        public void Configure(TMP_Text idText, TMP_Text titleText, TMP_Text customerText, TMP_Text recipeText,
            TMP_Text statusText, Image itemIcon, Image fill, UIButton actionButton)
        {
            orderId = idText; title = titleText; customer = customerText; recipe = recipeText;
            status = statusText; icon = itemIcon; progress = fill; action = actionButton;
        }
        protected override void OnEnable()
        {
            base.OnEnable();
            if (action != null) action.Clicked.AddListener(Request);
            Render();
        }
        protected override void OnDisable()
        {
            if (action != null) action.Clicked.RemoveListener(Request);
            base.OnDisable();
        }
        public void Bind(UIOrderData value) { data = value; Render(); }
        private void Request() { if (data != null && data.CanRequest) ActionRequested.Invoke(data.Id); }
        private void Render()
        {
            if (orderId != null) orderId.text = data == null ? "暂无订单" : "订单 / " + data.Id;
            if (title != null) title.text = data?.Title ?? "等待下一位客人";
            if (customer != null) customer.text = data?.Customer ?? "";
            if (recipe != null) recipe.text = data?.Recipe ?? "";
            if (status != null) status.text = data?.Status ?? "柜台暂时很安静。";
            if (progress != null) progress.fillAmount = data?.Progress ?? 0f;
            if (icon != null)
            {
                icon.sprite = data?.Icon != null ? data.Icon : theme?.MilkTeaIcon;
                icon.enabled = icon.sprite != null && data != null;
            }
            if (action != null) { action.SetInteractable(data != null && data.CanRequest); action.SetLabel("查看制作台"); }
        }
        public override void ApplyTheme(UIThemeDefinition value) { theme = value; Render(); }
    }
}
