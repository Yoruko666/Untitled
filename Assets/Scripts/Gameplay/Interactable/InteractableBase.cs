using UnityEngine;

namespace Untitled
{
    public abstract class InteractableBase : MonoBehaviour
    {
        [SerializeField] protected float range = 2f;

        private bool inRange;

        private void Start()
        {
            SphereCollider rangeCollider = GetComponent<SphereCollider>();
            if (rangeCollider == null) rangeCollider = gameObject.AddComponent<SphereCollider>();
            rangeCollider.isTrigger = true;
            rangeCollider.radius = range;
        }

        private void OnDestroy()
        {
            if (inRange && InteractManager.TryGet(out var manager)) manager.Exit(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (inRange || other.GetComponent<PlayerEntity>() == null) return;

            inRange = true;
            InteractManager.Instance.Enter(this);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!inRange || other.GetComponent<PlayerEntity>() == null) return;

            inRange = false;
            InteractManager.Instance.Exit(this);
        }

        public abstract void Interact();
    }
}
