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
                TeaBase.WildMountainTea => "Wild Mountain Tea",
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
            string teaName = GetFormattedTea();
            string milkName = GetFormattedMilk();
            string milkStr = !string.IsNullOrEmpty(milkName) ? $" w/ {milkName}" : "";
            string toppingsStr = targetToppings.Count > 0 ? string.Join(", ", targetToppings.Select(FormatToppingName)) : "No Toppings";
            return $"{teaName}{milkStr}\n• Sugar: {targetSweetnessPercent}% | Ice: {targetIcePercent}%\n• Toppings: {toppingsStr}";
        }
    }
}
