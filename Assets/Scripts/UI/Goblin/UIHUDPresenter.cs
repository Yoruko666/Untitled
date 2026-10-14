using TMPro;
using UnityEngine;

namespace Untitled.UI
{
    public sealed class UIHUDPresenter : MonoBehaviour
    {
        [Tooltip("Must implement IUIReadModel. Leave empty and inject with SetSource for production.")]
        [SerializeField] private MonoBehaviour source;
        [SerializeField] private UIOrderCard order;
        [SerializeField] private UIQuestTracker quests;
        [SerializeField] private TMP_Text day, coins;
        private IUIReadModel model;
        private bool listening;
        public void Configure(MonoBehaviour provider, UIOrderCard card, UIQuestTracker tracker, TMP_Text dayText, TMP_Text coinText)
        { source = provider; order = card; quests = tracker; day = dayText; coins = coinText; }
        public void SetSource(IUIReadModel value)
        {
            Unsubscribe();
            model = value;
            if (isActiveAndEnabled) Subscribe();
        }
        private void OnEnable()
        {
            if (model == null) model = source as IUIReadModel;
            if (source != null && model == null) Debug.LogError("UI source must implement IUIReadModel.", this);
            Subscribe();
        }
        private void OnDisable() => Unsubscribe();
        private void Subscribe()
        {
            if (model == null || listening) return;
            model.Changed += Render; listening = true; Render(model.Current);
        }
        private void Unsubscribe()
        {
            if (model != null && listening) model.Changed -= Render;
            listening = false;
        }
        private void Render(UISnapshot value)
        {
            if (value == null) return;
            if (day != null) day.text = "第 " + value.Day + " 天 / 七日物语";
            if (coins != null) coins.text = value.Coins.ToString("N0");
            if (order != null) order.Bind(value.Order);
            if (quests != null) quests.Bind(value.Quests);
        }
    }
}
