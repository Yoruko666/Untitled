using System.Collections.Generic;
using UnityEngine;

namespace Untitled
{
    public class InteractManager : ManagerBase<InteractManager>
    {
        private readonly List<InteractableBase> candidates = new();
        private int index;

        public InteractableBase Current
        {
            get
            {
                if (candidates.Count == 0) return null;
                return candidates[Mathf.Clamp(index, 0, candidates.Count - 1)];
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            EventCenter.Subscribe(GameEventType.Interact, OnInteract);
            EventCenter.Subscribe<int>(GameEventType.ScrollSelect, OnScrollSelect);
        }

        public void Enter(InteractableBase interactable)
        {
            if (interactable == null || candidates.Contains(interactable)) return;
            candidates.Add(interactable);
        }

        public void Exit(InteractableBase interactable)
        {
            if (interactable == null || !candidates.Remove(interactable)) return;

            if (candidates.Count == 0) index = 0;
            else index = Mathf.Clamp(index, 0, candidates.Count - 1);
        }

        private void OnInteract()
        {
            Current?.Interact();
        }

        private void OnScrollSelect(int direction)
        {
            if (candidates.Count == 0) return;

            index = (index + direction) % candidates.Count;
            if (index < 0) index += candidates.Count;
        }
    }
}
