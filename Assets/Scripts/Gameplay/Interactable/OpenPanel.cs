using UnityEngine;

namespace Untitled
{
    public class OpenPanel : InteractableBase
    {
        [SerializeField] private string panelName;

        public override void Interact()
        {
            UIManager.Instance.Open(panelName);
        }
    }
}
