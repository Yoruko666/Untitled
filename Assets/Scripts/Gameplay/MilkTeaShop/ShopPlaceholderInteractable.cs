using UnityEngine;
using UnityEngine.Events;

namespace Untitled
{
    public sealed class ShopPlaceholderInteractable : RaycastInteractable
    {
        [SerializeField] private string prompt = "交互";
        [SerializeField, TextArea] private string feedback = "交互成功（占位反馈）";
        [SerializeField] private UnityEvent onInteract = new UnityEvent();

        public override string Prompt => prompt;
        public override string Interact()
        {
            onInteract.Invoke();
            return feedback;
        }
    }
}
