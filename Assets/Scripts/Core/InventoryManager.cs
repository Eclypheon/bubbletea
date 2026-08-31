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
        [Header("Dispensers Unlocked")]
        [SerializeField] private bool hasPremiumMilkDispenser = false;
        public bool HasPremiumMilkDispenser => hasPremiumMilkDispenser;

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

        public void SetupDay1StarterStock()
        {
            stock.Clear();
            stock["Cup"] = 25;
            stock["Sugar"] = 100;
            stock["Ice"] = 100;

            // Day 1 Teas
            stock[$"Tea_{TeaBase.BlackTea}"] = 15;
            stock[$"Tea_{TeaBase.GreenTea}"] = 15;
            stock[$"Tea_{TeaBase.OolongTea}"] = 10;
            stock[$"Tea_{TeaBase.ThaiTea}"] = 10;
            stock[$"Tea_{TeaBase.TaroTea}"] = 10;
            stock[$"Tea_{TeaBase.WildMountainTea}"] = 0;

            // Day 1 Milk: 15 Fresh Milk
            stock[$"Milk_{MilkType.FreshMilk}"] = 15;
            stock[$"Milk_{MilkType.OatMilk}"] = 0;
            stock[$"Milk_{MilkType.CoconutMilk}"] = 0;
            stock[$"Milk_{MilkType.CondensedMilk}"] = 0;

            // Day 1 Toppings: 15 Tapioca Pearls
            stock[$"Topping_{ToppingType.TapiocaPearls}"] = 15;
            stock[$"Topping_{ToppingType.PoppingBoba}"] = 0;
            stock[$"Topping_{ToppingType.GrassJelly}"] = 0;
            stock[$"Topping_{ToppingType.EggPudding}"] = 0;
            stock[$"Topping_{ToppingType.CoconutJelly}"] = 0;
            stock[$"Topping_{ToppingType.CheeseFoam}"] = 0;
            stock[$"Topping_{ToppingType.GoldenHoneyPearls}"] = 0;

            // Raw Foraged Ingredients
            stock[$"Raw_{RawIngredientType.BabyYippees}"] = 0;
            stock[$"Raw_{RawIngredientType.JellyBlocks}"] = 0;
            stock[$"Raw_{RawIngredientType.GoldenDew}"] = 0;

            hasPremiumMilkDispenser = false;
            OnInventoryUpdated?.Invoke();
        }

        public void UnlockPremiumMilkDispenser()
        {
            hasPremiumMilkDispenser = true;
            AddMilkStock(MilkType.OatMilk, 1);
            AddMilkStock(MilkType.CoconutMilk, 1);
            AddMilkStock(MilkType.CondensedMilk, 1);
            HUDController.Instance?.ShowNotification("🌟 Premium Milk Dispenser Unlocked (+1 sample of Oat, Coconut, Condensed Milk)!", 4f);
            OnInventoryUpdated?.Invoke();
        }

        private void InitializeStock()
        {
            SetupDay1StarterStock();
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

        // Raw Foraged Ingredients API
        public int GetRawStock(RawIngredientType type) => GetStock($"Raw_{type}");
        public void AddRawStock(RawIngredientType type, int qty) => AddStock($"Raw_{type}", qty);
        public bool ConsumeRawStock(RawIngredientType type, int qty = 1) => ConsumeStock($"Raw_{type}", qty);
    }

    public enum RawIngredientType
    {
        BabyYippees,
        JellyBlocks,
        GoldenDew
    }
}
