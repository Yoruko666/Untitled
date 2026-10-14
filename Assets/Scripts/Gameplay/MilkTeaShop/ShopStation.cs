using System;
using UnityEngine;
using UnityEngine.Events;

namespace Untitled
{
    public enum ShopStationKind
    {
        OrderCounter, OrderBoard, Preparation, Sealing, Delivery, Storage, Water, DungeonEntrance
    }

    public sealed class ShopStation : RaycastInteractable
    {
        [SerializeField] private string stationId;
        [SerializeField] private ShopStationKind kind;
        [SerializeField] private string prompt;
        [SerializeField, TextArea] private string feedback;
        [SerializeField] private Transform approachPoint;
        [SerializeField] private UnityEvent onRequested = new UnityEvent();

        public string StationId => stationId;
        public ShopStationKind Kind => kind;
        public Transform ApproachPoint => approachPoint;
        public override string Prompt => prompt;
        public event Action<ShopStation> Requested;

        public override string Interact()
        {
            Requested?.Invoke(this);
            onRequested.Invoke();
            return feedback;
        }
    }
}
