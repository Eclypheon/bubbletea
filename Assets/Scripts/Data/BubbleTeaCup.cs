using System;
using System.Collections.Generic;
using UnityEngine;

namespace BubbleTeaShop
{
    [System.Serializable]
    public class EvaluationResult
    {
        public float accuracy; // 0.0 to 1.0
        public int stars;      // 1 to 5
        public float earnedMoney;
        public float tip;
        public List<string> feedbackNotes = new List<string>();
        public bool isPerfect => accuracy >= 0.95f;
    }

    [System.Serializable]
    public class BubbleTeaCup
    {
        public bool hasCup = false;
        public TeaBase tea = TeaBase.None;
        public MilkType milk = MilkType.None;
        public int sweetnessPercent = 0;
        public int icePercent = 0;
        public List<ToppingType> toppings = new List<ToppingType>();
        public bool isSealed = false;

        public bool IsEmpty => !hasCup || (tea == TeaBase.None && toppings.Count == 0);

        public void Reset()
        {
            hasCup = true;
            tea = TeaBase.None;
            milk = MilkType.None;
            sweetnessPercent = 0;
            icePercent = 0;
            toppings.Clear();
            isSealed = false;
        }

        public EvaluationResult Evaluate(DrinkOrder order, float patienceRemainingPercent)
        {
            var result = new EvaluationResult();
            float score = 0f;
            float maxScore = 5f;

            // 1. Tea Base Check (Weight: 1.5)
            if (tea == order.targetTea)
            {
                score += 1.5f;
            }
            else
            {
                result.feedbackNotes.Add($"Wrong tea base! Wanted {order.targetTea}, got {tea}.");
            }

            // 2. Milk Check (Weight: 0.8)
            if (milk == order.targetMilk)
            {
                score += 0.8f;
            }
            else
            {
                result.feedbackNotes.Add($"Milk mismatch! Expected {order.targetMilk}.");
            }

            // 3. Sweetness Check (Weight: 0.9)
            int sweetDiff = Mathf.Abs(sweetnessPercent - order.targetSweetnessPercent);
            if (sweetDiff == 0)
            {
                score += 0.9f;
            }
            else if (sweetDiff <= 25)
            {
                score += 0.5f;
                result.feedbackNotes.Add("Sweetness was slightly off.");
            }
            else
            {
                result.feedbackNotes.Add($"Sweetness level was wrong ({sweetnessPercent}% vs {order.targetSweetnessPercent}%).");
            }

            // 4. Ice Check (Weight: 0.6)
            int iceDiff = Mathf.Abs(icePercent - order.targetIcePercent);
            if (iceDiff == 0)
            {
                score += 0.6f;
            }
            else if (iceDiff <= 30)
            {
                score += 0.3f;
                result.feedbackNotes.Add("Ice amount was slightly off.");
            }
            else
            {
                result.feedbackNotes.Add("Ice level was wrong.");
            }

            // 5. Toppings Check (Weight: 0.8)
            int matchedToppings = 0;
            foreach (var t in order.targetToppings)
            {
                if (toppings.Contains(t)) matchedToppings++;
            }
            
            if (order.targetToppings.Count == 0 && toppings.Count == 0)
            {
                score += 0.8f;
            }
            else if (order.targetToppings.Count > 0)
            {
                float toppingRatio = (float)matchedToppings / order.targetToppings.Count;
                score += toppingRatio * 0.8f;
                if (matchedToppings < order.targetToppings.Count)
                {
                    result.feedbackNotes.Add("Missing requested toppings!");
                }
            }

            // 6. Seal Check (Penalty if not sealed)
            if (!isSealed)
            {
                score *= 0.5f;
                result.feedbackNotes.Add("Forgot to seal the cup with a lid/film!");
            }

            result.accuracy = Mathf.Clamp01(score / maxScore);
            result.stars = Mathf.Clamp(Mathf.RoundToInt(result.accuracy * 5f), 1, 5);

            if (result.accuracy >= 0.5f)
            {
                result.earnedMoney = order.basePrice;
                // Tip is scaled by accuracy and remaining patience speed bonus
                float speedBonus = Mathf.Max(0f, patienceRemainingPercent * 0.5f);
                result.tip = (result.accuracy >= 0.85f) ? (order.basePrice * (0.2f + speedBonus)) : 0f;
                if (result.tip > 0f && UpgradeManager.Instance != null && UpgradeManager.Instance.HasUpgrade(UpgradeType.LuckyCat))
                {
                    result.tip *= 1.30f;
                }
                result.tip = (float)Math.Round(result.tip, 2);
            }
            else
            {
                result.earnedMoney = order.basePrice * 0.3f; // unhappy partial payout
                result.tip = 0f;
                result.feedbackNotes.Add("Customer was unhappy with the order.");
            }

            return result;
        }
    }
}
