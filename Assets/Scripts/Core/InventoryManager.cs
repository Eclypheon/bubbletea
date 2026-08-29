using System;
using System.Collections.Generic;
using UnityEngine;

namespace BubbleTeaShop
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [Header("Starting Quantities")]
        [SerializeField] private int startingCups = 25;
        [SerializeField] private int startingTeaServings = 15;
        [SerializeField] private int startingMilkServings = 15;
        [SerializeField] private int startingSugarServings = 40;
        [SerializeField] private int startingIceServings = 40;
        [SerializeField] private int startingToppings = 15;

        private Dictionary<string, int> stock = new Dictionary<string, int>();

        public event Action OnInventoryUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeStock();
        }

        private void InitializeStock()
        {
            stock["Cup"] = startingCups;
            stock["Sugar"] = startingSugarServings;
            stock["Ice"] = startingIceServings;
            
            // Teas
            foreach (TeaBase tea in Enum.GetValues(typeof(TeaBase)))
            {
                if (tea == TeaBase.None) continue;
                stock[$"Tea_{tea}"] = (tea == TeaBase.BlackTea || tea == TeaBase.GreenTea) ? startingTeaServings : 5;
            }

            // Milk
            foreach (MilkType milk in Enum.GetValues(typeof(MilkType)))
            {
                if (milk == MilkType.None) continue;
                stock[$"Milk_{milk}"] = (milk == MilkType.FreshMilk) ? startingMilkServings : 5;
            }

            // Toppings
            foreach (ToppingType topping in Enum.GetValues(typeof(ToppingType)))
            {
                stock[$"Topping_{topping}"] = (topping == ToppingType.TapiocaPearls) ? startingToppings : 5;
            }
        }

        public int GetStock(string key)
        {
            return stock.TryGetValue(key, out int count) ? count : 0;
        }

        public int GetTeaStock(TeaBase tea) => GetStock($"Tea_{tea}");
        public int GetMilkStock(MilkType milk) => GetStock($"Milk_{milk}");
        public int GetToppingStock(ToppingType topping) => GetStock($"Topping_{topping}");
        public int GetCupStock() => GetStock("Cup");

        public bool HasStock(string key, int quantity = 1)
        {
            return GetStock(key) >= quantity;
        }

        public bool ConsumeStock(string key, int quantity = 1)
        {
            int current = GetStock(key);
            if (current >= quantity)
            {
                stock[key] = current - quantity;
                OnInventoryUpdated?.Invoke();
                return true;
            }
            return false;
        }

        public void AddStock(string key, int quantity)
        {
            if (quantity <= 0) return;
            if (!stock.ContainsKey(key)) stock[key] = 0;
            stock[key] += quantity;
            OnInventoryUpdated?.Invoke();
        }

        public void AddTeaStock(TeaBase tea, int qty) => AddStock($"Tea_{tea}", qty);
        public void AddMilkStock(MilkType milk, int qty) => AddStock($"Milk_{milk}", qty);
        public void AddToppingStock(ToppingType topping, int qty) => AddStock($"Topping_{topping}", qty);
        public void AddCups(int qty) => AddStock("Cup", qty);
    }
}
