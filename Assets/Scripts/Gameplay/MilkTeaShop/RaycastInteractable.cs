using UnityEngine;

namespace Untitled
{
    // The raycaster knows this contract, not the individual shop stations.
    public abstract class RaycastInteractable : MonoBehaviour
    {
        public abstract string Prompt { get; }
        public abstract string Interact();
    }
}
