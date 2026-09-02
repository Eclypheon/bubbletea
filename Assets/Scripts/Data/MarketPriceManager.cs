using System;
using System.Collections.Generic;
using UnityEngine;

namespace BubbleTeaShop
{
    public class MarketPriceManager : MonoBehaviour
    {
        public static MarketPriceManager Instance { get; private set; }

        [Header("Profit Markup")]
        [Tooltip("Percentage markup added to the ingredient cost to determine customer drink price")]
        [SerializeField] private float profitMarkupPercent = 15f; // 15% markup

        [Header("Single Serving Costs ($)")]
        [SerializeField] private float cupCost = 0.25f;
        [SerializeField] private float dailySugarIceCost = 10.00f;

        [Header("Tea Base Costs per Serving ($)")]
        [SerializeField] private float blackTeaCost = 0.60f;
        [SerializeField] private float greenTeaCost = 0.60f;
        [SerializeField] private float oolongTeaCost = 0.80f;
        [SerializeField] private float thaiTeaCost = 0.80f;
        [SerializeField] private float taroTeaCost = 0.80f;

        [Header("Milk Type Costs per Serving ($)")]
        [SerializeField] private float freshMilkCost = 0.50f;
        [SerializeField] private float oatMilkCost = 0.75f;
        [SerializeField] private float coconutMilkCost = 0.85f;
        [SerializeField] private float condensedMilkCost = 0.75f;

        [Header("Topping Costs per Serving ($)")]
        [SerializeField] private float tapiocaPearlsCost = 0.35f;
        [SerializeField] private float poppingBobaCost = 0.55f;
        [SerializeField] private float grassJellyCost = 0.70f;
        [SerializeField] private float coconutJellyCost = 0.90f;
        [SerializeField] private float eggPuddingCost = 1.15f;
        [SerializeField] private float cheeseFoamCost = 1.45f;
        [SerializeField] private float goldenHoneyPearlsCost = 1.85f;

        [Header("Market Pack Sizes")]
        [SerializeField] private int cupPackSize = 10;
        [SerializeField] private int ingredientPackSize = 8;
        [SerializeField] private int tapiocaPackSize = 10;

        public float DailySugarIceCost => dailySugarIceCost;
        public float ProfitMarkupPercent => profitMarkupPercent;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public float GetTeaCost(TeaBase tea)
        {
            float baseCost = tea switch
            {
                TeaBase.BlackTea => blackTeaCost,
                TeaBase.GreenTea => greenTeaCost,
                TeaBase.OolongTea => oolongTeaCost,
                TeaBase.ThaiTea => thaiTeaCost,
                TeaBase.TaroTea => taroTeaCost,
                _ => 0f
            };

            float multiplier = MarketEventManager.Instance != null ? MarketEventManager.Instance.GetPriceMultiplier($"Tea_{tea}") : 1f;
            return baseCost * multiplier;
        }

        public float GetMilkCost(MilkType milk)
        {
            float baseCost = milk switch
            {
                MilkType.FreshMilk => freshMilkCost,
                MilkType.OatMilk => oatMilkCost,
                MilkType.CoconutMilk => coconutMilkCost,
                MilkType.CondensedMilk => condensedMilkCost,
                _ => 0f
            };

            float multiplier = MarketEventManager.Instance != null ? MarketEventManager.Instance.GetPriceMultiplier($"Milk_{milk}") : 1f;
            return baseCost * multiplier;
        }

        public float GetToppingCost(ToppingType topping)
        {
            float baseCost = topping switch
            {
                ToppingType.TapiocaPearls => tapiocaPearlsCost,
                ToppingType.PoppingBoba => poppingBobaCost,
                ToppingType.GrassJelly => grassJellyCost,
                ToppingType.EggPudding => eggPuddingCost,
                ToppingType.CoconutJelly => coconutJellyCost,
                ToppingType.CheeseFoam => cheeseFoamCost,
                ToppingType.GoldenHoneyPearls => goldenHoneyPearlsCost,
                _ => 0.50f
            };

            float multiplier = MarketEventManager.Instance != null ? MarketEventManager.Instance.GetPriceMultiplier($"Topping_{topping}") : 1f;
            return baseCost * multiplier;
        }

        public float CalculateDrinkCost(DrinkOrder order)
        {
            float cost = cupCost;
            cost += GetTeaCost(order.targetTea);
            cost += GetMilkCost(order.targetMilk);

            if (order.targetToppings != null)
            {
                foreach (var topping in order.targetToppings)
                {
                    cost += GetToppingCost(topping);
                }
            }

            return cost;
        }

        public float CalculateDrinkSellPrice(DrinkOrder order)
        {
            float basePrice = 3.25f; // Base drink preparation fee
            basePrice += GetTeaCost(order.targetTea) * 1.8f;
            if (order.targetMilk != MilkType.None)
            {
                basePrice += GetMilkCost(order.targetMilk) * 1.8f;
            }

            if (order.targetToppings != null)
            {
                bool hasJelly = false;
                foreach (var topping in order.targetToppings)
                {
                    // Toppings scale customer drink price according to rarity
                    basePrice += GetToppingCost(topping) * 2.2f;
                    if (topping == ToppingType.GrassJelly || topping == ToppingType.CoconutJelly)
                    {
                        hasJelly = true;
                    }
                }

                if (hasJelly && UpgradeManager.Instance != null && UpgradeManager.Instance.HasUpgrade(UpgradeType.MarketingIntern))
                {
                    basePrice *= 1.10f;
                }
            }

            return (float)(Math.Round(Mathf.Max(3.50f, basePrice) * 10.0, MidpointRounding.AwayFromZero) / 10.0);
        }

        public float GetMarketPackPrice(string stockKey)
        {
            if (stockKey == "Cup") return (float)(Math.Round((cupCost * cupPackSize * 1.5f) * 10.0, MidpointRounding.AwayFromZero) / 10.0);

            if (stockKey.StartsWith("Tea_") && Enum.TryParse(stockKey.Substring(4), out TeaBase tea))
            {
                return (float)(Math.Round((GetTeaCost(tea) * ingredientPackSize * 1.6f) * 10.0, MidpointRounding.AwayFromZero) / 10.0);
            }

            if (stockKey.StartsWith("Milk_") && Enum.TryParse(stockKey.Substring(5), out MilkType milk))
            {
                return (float)(Math.Round((GetMilkCost(milk) * ingredientPackSize * 1.5f) * 10.0, MidpointRounding.AwayFromZero) / 10.0);
            }

            if (stockKey.StartsWith("Topping_") && Enum.TryParse(stockKey.Substring(8), out ToppingType topping))
            {
                int packSize = (topping == ToppingType.TapiocaPearls) ? tapiocaPackSize : ingredientPackSize;
                return (float)(Math.Round((GetToppingCost(topping) * packSize * 1.6f) * 10.0, MidpointRounding.AwayFromZero) / 10.0);
            }

            return 8.00f;
        }

        public int GetMarketPackQuantity(string stockKey)
        {
            if (stockKey == "Cup") return cupPackSize;
            if (stockKey == "Topping_TapiocaPearls") return tapiocaPackSize;
            return ingredientPackSize;
        }
    }
}
