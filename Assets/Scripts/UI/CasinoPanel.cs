using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Untitled
{
    public class CasinoPanel : PanelBase
    {
        private enum BetTarget
        {
            Big,
            Small,
        }

        private const int DiceCount = 3;
        private const int DiceMax = 6;
        private const int MinBet = 10;
        private const int MaxBet = 500;
        private const int BigThreshold = 11;

        [SerializeField] private TMP_Text moneyText;
        [SerializeField] private TMP_Text betText;
        [SerializeField] private TMP_Text diceText;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private Button betBigButton;
        [SerializeField] private Button betSmallButton;
        [SerializeField] private Button openButton;
        [SerializeField] private Button betUpButton;
        [SerializeField] private Button betDownButton;
        [SerializeField] private Button leaveButton;

        [SerializeField] private int betStep = 10;

        private readonly List<int> dice = new();
        private int betAmount = MinBet;
        private BetTarget? betTarget;
        private int total;
        private bool? lastWin;
        private int lastBetAmount;

        protected override void OnInit()
        {
            Bind(betUpButton, OnBetUp);
            Bind(betDownButton, OnBetDown);
            Bind(betBigButton, OnBetBig);
            Bind(betSmallButton, OnBetSmall);
            Bind(openButton, OnOpen);
            Bind(leaveButton, OnLeave);
        }

        protected override void OnShow(object data)
        {
            Refresh();
        }

        protected override void OnHide()
        {
            betTarget = null;
        }

        private void OnBetUp()
        {
            betAmount = Mathf.Min(betAmount + betStep, MaxBet);
            Refresh();
        }

        private void OnBetDown()
        {
            betAmount = Mathf.Max(betAmount - betStep, MinBet);
            Refresh();
        }

        private void OnBetBig()
        {
            PlaceBet(BetTarget.Big);
        }

        private void OnBetSmall()
        {
            PlaceBet(BetTarget.Small);
        }

        private void PlaceBet(BetTarget target)
        {
            if (betTarget != null)
            {
                SetText(resultText, "已经下注了，先开盘");
                return;
            }

            if (betAmount > GameManager.Instance.GameData.Money)
            {
                SetText(resultText, $"余额不足，这一注需要 {betAmount}");
                return;
            }

            betTarget = target;
            Refresh();
        }

        private void OnOpen()
        {
            if (betTarget == null)
            {
                SetText(resultText, "先押大或押小");
                return;
            }

            if (!GameManager.Instance.GameData.TrySpendMoney(betAmount))
            {
                betTarget = null;
                SetText(resultText, $"余额不足，押注已取消");
                return;
            }

            lastBetAmount = betAmount;
            Roll();

            bool win = IsWin(betTarget.Value);
            lastWin = win;
            betTarget = null;

            if (win) GameManager.Instance.GameData.AddMoney(lastBetAmount * 2);

            Refresh();
        }

        private void OnLeave()
        {
            UIManager.Instance.Close(this);
        }

        private void Refresh()
        {
            SetText(moneyText, $"余额：{GameManager.Instance.GameData.Money}");
            SetText(betText, $"下注：{betAmount}");

            if (betTarget != null)
            {
                string target = betTarget == BetTarget.Big ? "大" : "小";
                SetText(resultText, $"已押{target} {betAmount}，开盘时扣除");
                return;
            }

            if (lastWin == null)
            {
                SetText(diceText, string.Empty);
                SetText(resultText, "押大或押小开始这一局");
                return;
            }

            string values = string.Join(" ", dice);
            string label = IsTriple() ? "豹子" : IsBig(total) ? "大" : "小";
            SetText(diceText, $"{values}（{total} {label}）");
            SetText(resultText, lastWin.Value ? $"赢 +{lastBetAmount}" : $"输 -{lastBetAmount}");
        }

        private void Roll()
        {
            dice.Clear();
            total = 0;

            for (int i = 0; i < DiceCount; i++)
            {
                int value = Random.Range(1, DiceMax + 1);
                dice.Add(value);
                total += value;
            }
        }

        private bool IsWin(BetTarget target)
        {
            if (IsTriple()) return false;

            return IsBig(total) == (target == BetTarget.Big);
        }

        private bool IsTriple()
        {
            for (int i = 1; i < dice.Count; i++)
            {
                if (dice[i] != dice[0]) return false;
            }

            return true;
        }

        private static bool IsBig(int value)
        {
            return value >= BigThreshold;
        }

        private void Bind(Button button, UnityAction action)
        {
            if (button == null)
            {
                Debug.LogError($"[CasinoPanel] {action.Method.Name} 的按钮没有指定。");
                return;
            }

            button.onClick.AddListener(action);
        }

        private void SetText(TMP_Text text, string value)
        {
            if (text == null) return;

            text.text = value;
        }
    }
}
