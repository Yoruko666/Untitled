using System;
using System.Collections.Generic;
using UnityEngine;

namespace Untitled.UI
{
    // Economy has no change event yet. The gameplay host injects GameData and calls Publish
    // after its own mutations. This adapter neither polls nor changes game rules or money.
    public sealed class UIGameDataSource : MonoBehaviour, IUIReadModel
    {
        public UISnapshot Current { get; private set; }
        public event Action<UISnapshot> Changed;
        private GameData gameData;
        public void Initialize(GameData value) => gameData = value;
        public void Publish(int day, UIOrderData order, IReadOnlyList<UIQuestData> quests)
        {
            if (gameData == null) throw new InvalidOperationException("Inject GameData before publishing UI data.");
            Current = new UISnapshot(day, gameData.Money, order, quests);
            Changed?.Invoke(Current);
        }
    }
}
