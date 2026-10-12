using System.Collections.Generic;
using UnityEngine;

namespace Untitled
{
    public enum DiceBetTarget
    {
        Big,
        Small,
    }

    public class Casino
    {
        public const int DiceCount = 3;
        public const int DiceMax = 6;
        public const int MinBet = 10;
        public const int MaxBet = 500;

        private readonly List<int> dice = new();

        public IReadOnlyList<int> Dice => dice;

        public int Total { get; private set; }

        public bool HasBet { get; private set; }

        public int BetAmount { get; private set; }

        public DiceBetTarget BetTarget { get; private set; }

        public bool? LastWin { get; private set; }

        public int LastPayout { get; private set; }

        public bool PlaceBet(int amount, DiceBetTarget target)
        {
            if (HasBet)
            {
                Debug.LogError("[Casino] 上一局还没有开盘，不能重复下注。");
                return false;
            }

            if (amount < MinBet || amount > MaxBet)
            {
                Debug.LogError($"[Casino] 下注额 {amount} 超出限红 {MinBet}~{MaxBet}。");
                return false;
            }

            if (!GameManager.Instance.GameData.TrySpendMoney(amount)) return false;

            BetAmount = amount;
            BetTarget = target;
            HasBet = true;
            return true;
        }

        public bool Open()
        {
            if (!HasBet)
            {
                Debug.LogError("[Casino] 还没有下注，不能开盘。");
                return false;
            }

            Roll();

            bool win = IsWin();
            int payout = win ? BetAmount * 2 : 0;
            if (payout > 0) GameManager.Instance.GameData.AddMoney(payout);

            LastWin = win;
            LastPayout = payout;
            HasBet = false;
            return win;
        }

        private void Roll()
        {
            dice.Clear();
            Total = 0;

            for (int i = 0; i < DiceCount; i++)
            {
                int value = Random.Range(1, DiceMax + 1);
                dice.Add(value);
                Total += value;
            }
        }

        private bool IsWin()
        {
            if (IsTriple()) return false;

            return (Total >= 11) == (BetTarget == DiceBetTarget.Big);
        }

        private bool IsTriple()
        {
            for (int i = 1; i < dice.Count; i++)
            {
                if (dice[i] != dice[0]) return false;
            }

            return true;
        }
    }
}
