using UnityEngine;

namespace Untitled
{
    public class Collection : InteractableBase
    {
        public int ItemId;
        public int Num;

        public override void Interact()
        {
            GameManager.Instance.GameData.PackageData.AddItem(ItemId, Num);
            EventCenter.Invoke(GameEventType.CollectItem, ItemId, Num);
            Destroy(gameObject);
        }
    }
}
