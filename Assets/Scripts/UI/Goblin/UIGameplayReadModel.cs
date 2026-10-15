using System;
using UnityEngine;

namespace Untitled.UI
{
    public sealed class UIGameplayReadModel : MonoBehaviour, IUIHUDReadModel
    {
        [SerializeField] private bool night;
        [SerializeField] private ShopRaycastInteractor shopInteractor;
        [SerializeField] private ShopSceneBindings shop;
        [SerializeField] private UIInventoryDataSource inventory;
        [SerializeField] private Sprite itemIcon;
        private GameData game;
        private GameSession session;
        private InteractManager interactions;
        private UIHUDData current;
        // Presentation metadata only; persistent gameplay state lives in GameData.
        private static UIHUDData sessionDisplay;
        private static string displayedSessionOrderId;
        public UIHUDData Current => current;
        public event Action<UIHUDData> Changed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetDisplay() { sessionDisplay = null; displayedSessionOrderId = null; }
        public void Configure(bool isNight, UIInventoryDataSource inventorySource, ShopSceneBindings bindings = null, ShopRaycastInteractor interactor = null, Sprite fallback = null)
        { night = isNight; inventory = inventorySource; shop = bindings; shopInteractor = interactor; itemIcon = fallback; }
        private void Start()
        {
            if (!GameManager.TryGet(out var manager)) return;
            game = manager.GameData; game.MoneyChanged += OnMoney;
            session = manager.Session; session.Changed += RefreshSession;
            RefreshSession();
            if (shopInteractor != null)
            {
                shopInteractor.SetExternalHUD(true);
                shopInteractor.TargetChanged += OnShopTarget;
            }
            else if (InteractManager.TryGet(out interactions)) interactions.CurrentChanged += OnTarget;
            if (shop != null) shop.StationRequested += OnStation;
        }
        private void OnDestroy()
        {
            if (game != null) game.MoneyChanged -= OnMoney;
            if (session != null) session.Changed -= RefreshSession;
            if (shopInteractor != null) { shopInteractor.TargetChanged -= OnShopTarget; shopInteractor.SetExternalHUD(false); }
            if (interactions != null) interactions.CurrentChanged -= OnTarget;
            if (shop != null) shop.StationRequested -= OnStation;
        }
        public void Publish(UIHUDData display)
        {
            if (display == null) return;
            if (session != null && !session.IsReady) return;
            current = new UIHUDData(session?.Day ?? display.Day, game != null ? game.Money : display.Coins, night, session != null ? SessionOrder() ?? display.Order : display.Order, display.Quests,
                display.Health, display.Resource, display.Quickbar, display.Prompt, display.Alert);
            sessionDisplay = current;
            displayedSessionOrderId = session?.Order.Id;
            Changed?.Invoke(current);
        }
        private UIOrderData SessionOrder()
        {
            var order = session.Order;
            if (string.IsNullOrEmpty(order.Id)) return null;
            return new UIOrderData(order.Id, "订单 " + order.Id, "", "", order.Completed ? "已完成" : "进行中",
                order.Progress, !order.Completed);
        }
        private void RefreshSession()
        {
            if (session == null || !session.IsReady) return;
            if (inventory != null && DatabaseManager.TryGet(out var db))
            {
                inventory.Initialize(game.InventoryData, db.ItemDatabase, address => itemIcon, "仓库");
                inventory.Publish();
                if (UIManager.TryGet(out var ui) && ui.IsOpen("UI_Inventory")) ui.Open("UI_Inventory", inventory.Current);
            }
            var cached = current ?? sessionDisplay;
            var cachedOrder = displayedSessionOrderId == session.Order.Id ? cached?.Order : null;
            Publish(new UIHUDData(session.Day, game.Money, night, SessionOrder() ?? cachedOrder, cached?.Quests,
                cached?.Health, cached?.Resource, cached?.Quickbar, cached?.Prompt, cached?.Alert ?? false));
            if (UIManager.TryGet(out var panels) && panels.IsOpen("UI_Workstation")) panels.Open("UI_Workstation", current.Order);
        }
        private void OnMoney(int amount)
        {
            if (session != null && !session.IsReady) { current = null; sessionDisplay = null; return; }
            if (current != null) Publish(current);
        }
        private void SetPrompt(string text)
        { if (current != null) Publish(new UIHUDData(current.Day, current.Coins, night, current.Order, current.Quests, current.Health, current.Resource, current.Quickbar, text, current.Alert)); }
        private void OnShopTarget(RaycastInteractable target) => SetPrompt(target != null ? target.Prompt : null);
        private void OnTarget(InteractableBase target) => SetPrompt(target == null ? null : target is NpcDialog ? "交谈" : target is Collection ? "收集材料" : "交互");
        private void OnStation(ShopStation station)
        {
            if (station.Kind == ShopStationKind.Storage)
            { inventory.Publish(); UIManager.Instance.Open("UI_Inventory", inventory.Current); }
            else if (station.Kind == ShopStationKind.OrderCounter || station.Kind == ShopStationKind.OrderBoard || station.Kind == ShopStationKind.Preparation)
                UIManager.Instance.Open("UI_Workstation", current?.Order);
        }
    }
}
