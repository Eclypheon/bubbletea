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

        // Market Weather Ice Outcomes
        public bool isHeatwaveIceSecretSatisfied = false;
        public bool isHeatwaveIcePenaltyIncurred = false;
        public bool isChillyRainIceSecretSatisfied = false;
        public bool isChillyRainIcePenaltyIncurred = false;
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
            int mistakeCount = 0;

            // 1. Tea Base Check (1 mistake if wrong)
            if (tea != order.targetTea)
            {
                mistakeCount++;
                result.feedbackNotes.Add($"Wrong tea base! Wanted {order.targetTea}, got {tea}.");
            }

            // 2. Milk Check (1 mistake if wrong)
            if (milk != order.targetMilk)
            {
                mistakeCount++;
                result.feedbackNotes.Add($"Milk mismatch! Expected {order.targetMilk}.");
            }

            // 3. Sweetness Check (1 mistake if wrong)
            if (sweetnessPercent != order.targetSweetnessPercent)
            {
                mistakeCount++;
                result.feedbackNotes.Add($"Sweetness level was wrong ({sweetnessPercent}% vs {order.targetSweetnessPercent}%).");
            }

            // 4. Ice Check (with Market Event secret weather preferences)
            var activeMarketEv = MarketEventManager.Instance != null ? MarketEventManager.Instance.ActiveEvent : null;
            if (activeMarketEv != null && activeMarketEv.eventId == "summer_heatwave")
            {
                // Summer Heatwave secret: Customers secretly crave 100% Full Ice regardless of ticket
                if (icePercent == 100)
                {
                    result.isHeatwaveIceSecretSatisfied = true;
                    result.feedbackNotes.Add("Satisfied Summer Heatwave craving with 100% Full Ice!");
                }
                else
                {
                    mistakeCount++;
                    result.isHeatwaveIcePenaltyIncurred = true;
                    result.feedbackNotes.Add("Customer secretly craved 100% Full Ice due to the scorching Summer Heatwave!");
                }
            }
            else if (activeMarketEv != null && activeMarketEv.eventId == "chilly_rain")
            {
                // Chilly Monsoon Rain secret: Customers secretly crave 0% No Ice to stay warm regardless of ticket
                if (icePercent == 0)
                {
                    result.isChillyRainIceSecretSatisfied = true;
                    result.feedbackNotes.Add("Satisfied Chilly Monsoon craving with 0% No Ice!");
                }
                else
                {
                    mistakeCount++;
                    result.isChillyRainIcePenaltyIncurred = true;
                    result.feedbackNotes.Add("Customer secretly craved 0% No Ice due to the freezing Chilly Monsoon Rain!");
                }
            }
            else
            {
                if (icePercent != order.targetIcePercent)
                {
                    mistakeCount++;
                    result.feedbackNotes.Add($"Ice level was wrong ({icePercent}% vs {order.targetIcePercent}%).");
                }
            }

            // 5. Toppings Check (1 mistake per missing or extra topping)
            int missingToppings = 0;
            if (order.targetToppings != null)
            {
                foreach (var t in order.targetToppings)
                {
                    if (toppings == null || !toppings.Contains(t)) missingToppings++;
                }
            }
            if (missingToppings > 0)
            {
                mistakeCount += missingToppings;
                result.feedbackNotes.Add($"Missing {missingToppings} requested topping(s)!");
            }

            int extraToppings = 0;
            if (toppings != null)
            {
                foreach (var t in toppings)
                {
                    if (order.targetToppings == null || !order.targetToppings.Contains(t)) extraToppings++;
                }
            }
            if (extraToppings > 0)
            {
                mistakeCount += extraToppings;
                result.feedbackNotes.Add($"Added {extraToppings} extra unwanted topping(s)!");
            }

            // 6. Slowness / Patience Penalty:
            // Below 20% patience, deduct 1 star for every 5%
            // (15% -> 1 star deducted, 10% -> 2 stars deducted, 5% -> 3 stars deducted, 0% -> 4 stars deducted)
            int slownessPenalty = 0;
            if (patienceRemainingPercent < 0.20f)
            {
                slownessPenalty = Mathf.CeilToInt((0.20f - patienceRemainingPercent) / 0.05f);
                slownessPenalty = Mathf.Clamp(slownessPenalty, 0, 4);
                if (slownessPenalty > 0)
                {
                    result.feedbackNotes.Add($"Took too long to serve! (-{slownessPenalty} star{(slownessPenalty > 1 ? "s" : "")})");
                }
            }

            // Total deductions capped at 4 stars (meaning the lowest possible rating is 1 star)
            int totalDeductions = Mathf.Clamp(mistakeCount + slownessPenalty, 0, 4);
            result.stars = 5 - totalDeductions;
            result.accuracy = result.stars / 5.0f;

            // Earnings & Tips:
            // 3+ stars earns full base price, 1-2 stars gives 30% unhappy partial payout
            if (result.stars >= 3)
            {
                result.earnedMoney = (float)(Math.Round(order.basePrice * 10.0, MidpointRounding.AwayFromZero) / 10.0);

                // Base tip is 10% of drink price.
                // Speed bonus is up to +30%: Full +30% tip when patience >= 90%, scaling down linearly below 90%
                float speedFactor = (patienceRemainingPercent >= 0.90f) ? 1.0f : Mathf.Clamp01(patienceRemainingPercent / 0.90f);
                float speedBonus = speedFactor * 0.30f;

                // Tips rewarded on well-made drinks (4 or 5 stars)
                if (result.stars >= 4)
                {
                    result.tip = order.basePrice * (0.10f + speedBonus);
                    if (UpgradeManager.Instance != null && UpgradeManager.Instance.HasUpgrade(UpgradeType.LuckyCat))
                    {
                        result.tip *= 1.30f;
                    }
                    if (activeMarketEv != null && activeMarketEv.eventId == "cream_shortage")
                    {
                        result.tip *= 1.25f;
                    }
                    result.tip = (float)(Math.Round(result.tip * 10.0, MidpointRounding.AwayFromZero) / 10.0);
                }
                else
                {
                    result.tip = 0f;
                }
            }
            else
            {
                result.earnedMoney = (float)(Math.Round((order.basePrice * 0.30f) * 10.0, MidpointRounding.AwayFromZero) / 10.0); // unhappy partial payout
                result.tip = 0f;
                result.feedbackNotes.Add("Customer was unhappy with the order.");
            }

            return result;
        }
    }
}
