using UnityEngine;

namespace Untitled
{
    public class GameData
    {
        public int Money;
        public InventoryData InventoryData;  
        public PackageData PackageData; 

        public GameData()
        {
            Money = 0;
            InventoryData = new InventoryData();
            PackageData = new PackageData();
        }

        public void AddMoney(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogError($"[GameData] 增加的金钱必须为正数，收到 {amount}。");
                return;
            }

            Money += amount;
        }

        public bool TrySpendMoney(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogError($"[GameData] 扣除的金钱必须为正数，收到 {amount}。");
                return false;
            }

            if (Money < amount) return false;

            Money -= amount;
            return true;
        }
    }
}
