using System.Collections.Generic;
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
        public int targetIcePercent;       // 0, 30, 50, 100
        public List<ToppingType> targetToppings = new List<ToppingType>();
        
        public string dialogueText;
        public float basePrice = 6.50f;

        public string GetFormattedSummary()
        {
            string milkStr = targetMilk != MilkType.None ? $" w/ {targetMilk}" : "";
            string toppingsStr = targetToppings.Count > 0 ? string.Join(", ", targetToppings) : "No Toppings";
            return $"{targetTea}{milkStr}\n• Sugar: {targetSweetnessPercent}% | Ice: {targetIcePercent}%\n• Toppings: {toppingsStr}";
        }
    }
}
