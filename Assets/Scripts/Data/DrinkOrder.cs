using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BubbleTeaShop
{
    [System.Serializable]
    public class DrinkOrder
    {
        public CustomerArchetype archetype;
        public string customerName;
        public TeaBase targetTea;
        public MilkType targetMilk;
        public int targetSweetnessPercent; // 0, 25, 50, 75, 100
        public int targetIcePercent;       // 0, 50, 100
        public List<ToppingType> targetToppings = new List<ToppingType>();
        
        public string dialogueText;
        public float basePrice = 6.50f;

        public static string FormatTeaName(TeaBase tea)
        {
            return tea switch
            {
                TeaBase.BlackTea => "Black Tea",
                TeaBase.GreenTea => "Green Tea",
                TeaBase.OolongTea => "Oolong Tea",
                TeaBase.ThaiTea => "Thai Tea",
                TeaBase.TaroTea => "Taro Tea",
                _ => "Tea"
            };
        }

        public static string FormatMilkName(MilkType milk)
        {
            return milk switch
            {
                MilkType.FreshMilk => "Fresh Milk",
                MilkType.OatMilk => "Oat Milk",
                MilkType.CoconutMilk => "Coconut Milk",
                MilkType.CondensedMilk => "Condensed Milk",
                _ => ""
            };
        }

        public static string FormatToppingName(ToppingType topping)
        {
            return topping switch
            {
                ToppingType.TapiocaPearls => "Tapioca Pearls",
                ToppingType.PoppingBoba => "Popping Boba",
                ToppingType.GrassJelly => "Grass Jelly",
                ToppingType.CoconutJelly => "Coconut Jelly",
                ToppingType.EggPudding => "Egg Pudding",
                ToppingType.GoldenHoneyPearls => "Golden Honey Pearls",
                ToppingType.CheeseFoam => "Cheese Foam",
                _ => topping.ToString()
            };
        }

        public string GetFormattedTea() => FormatTeaName(targetTea);
        public string GetFormattedMilk() => FormatMilkName(targetMilk);

        public string GetFormattedToppings()
        {
            if (targetToppings == null || targetToppings.Count == 0)
                return "no toppings";
            
            return string.Join(" and ", targetToppings.Select(FormatToppingName));
        }

        public string GetFormattedSummary()
        {
            List<string> lines = new List<string>();

            // 1. Tea Base
            string teaName = GetFormattedTea();
            if (!string.IsNullOrEmpty(teaName))
            {
                lines.Add(teaName);
            }

            // 2. Milk Type (if any)
            string milkName = GetFormattedMilk();
            if (!string.IsNullOrEmpty(milkName))
            {
                lines.Add(milkName);
            }

            // 3. Sugar Percentage
            lines.Add($"{targetSweetnessPercent}% Sugar");

            // 4. Ice Percentage
            lines.Add($"{targetIcePercent}% Ice");

            // 5. Toppings (each topping on a new line)
            if (targetToppings != null && targetToppings.Count > 0)
            {
                foreach (var topping in targetToppings)
                {
                    lines.Add(FormatToppingName(topping));
                }
            }

            return string.Join("\n", lines);
        }
    }
}
