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
        private InteractManager interactions;
        private UIHUDData current;
        // Display cache only. Gameplay remains authoritative; this cache is not a save system.
        private static UIHUDData sessionDisplay;
        public UIHUDData Current => current;
        public event Action<UIHUDData> Changed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetDisplay() => sessionDisplay = null;
        public void Configure(bool isNight, UIInventoryDataSource inventorySource, ShopSceneBindings bindings = null, ShopRaycastInteractor interactor = null, Sprite fallback = null)
        { night = isNight; inventory = inventorySource; shop = bindings; shopInteractor = interactor; itemIcon = fallback; }
        private void Start()
        {
            if (!GameManager.TryGet(out var manager)) return;
            game = manager.GameData; game.MoneyChanged += OnMoney;
            if (DatabaseManager.TryGet(out var db)) inventory.Initialize(game.InventoryData.Items, db.ItemDatabase, address => itemIcon, "仓库");
            var cached = sessionDisplay;
            Publish(new UIHUDData(cached?.Day ?? 1, game.Money, night, cached?.Order, cached?.Quests, cached?.Health, cached?.Resource, cached?.Quickbar));
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
            if (shopInteractor != null) { shopInteractor.TargetChanged -= OnShopTarget; shopInteractor.SetExternalHUD(false); }
            if (interactions != null) interactions.CurrentChanged -= OnTarget;
            if (shop != null) shop.StationRequested -= OnStation;
        }
        public void Publish(UIHUDData display)
        {
            if (display == null) return;
            current = new UIHUDData(display.Day, game != null ? game.Money : display.Coins, night, display.Order, display.Quests,
                display.Health, display.Resource, display.Quickbar, display.Prompt, display.Alert);
            sessionDisplay = current; Changed?.Invoke(current);
        }
        private void OnMoney(int amount) { if (current != null) Publish(current); }
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
