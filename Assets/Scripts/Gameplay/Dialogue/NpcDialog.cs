using UnityEngine;

namespace Untitled
{
    public class NpcDialog : InteractableBase
    {
        [SerializeField] private int startId = 1;

        public override void Interact()
        {
            UIManager.Instance.Open(UIPanel.Dialogue, new DialogRequest(startId));
        }
    }
}
