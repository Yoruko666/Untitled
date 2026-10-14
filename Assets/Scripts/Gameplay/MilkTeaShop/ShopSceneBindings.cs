using System;
using System.Collections.Generic;
using UnityEngine;

namespace Untitled
{
    // Scene entry references for a future order/inventory/day-flow service.
    public sealed class ShopSceneBindings : MonoBehaviour
    {
        [SerializeField] private ShopFirstPersonController player;
        [SerializeField] private Transform playerSpawn;
        [SerializeField] private Transform customerSpawn;
        [SerializeField] private ShopStation[] stations = Array.Empty<ShopStation>();

        public ShopFirstPersonController Player => player;
        public Transform PlayerSpawn => playerSpawn;
        public Transform CustomerSpawn => customerSpawn;
        public IReadOnlyList<ShopStation> Stations => stations;
        public event Action<ShopStation> StationRequested;

        private void OnEnable()
        {
            foreach (var station in stations)
                if (station != null) station.Requested += OnStationRequested;
        }

        private void OnDisable()
        {
            foreach (var station in stations)
                if (station != null) station.Requested -= OnStationRequested;
        }

        public ShopStation GetStation(string stationId)
        {
            foreach (var station in stations)
                if (station != null && station.StationId == stationId) return station;
            return null;
        }

        private void OnStationRequested(ShopStation station) => StationRequested?.Invoke(station);
    }
}
