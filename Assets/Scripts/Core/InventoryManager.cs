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
        public bool HasPremiumMilkDispenser => (GameManager.Instance != null && GameManager.Instance.IsBlitzMode) || hasPremiumMilkDispenser;

        [Header("Live Stock (Editable in Inspector at Runtime)")]
        [Header("Raw Foraged Ingredients")]
        [SerializeField] private int rawBabyYippees = 0;
        [SerializeField] private int rawJellyBlocks = 0;
        [SerializeField] private int rawGoldenDew = 0;

        [Header("Toppings")]
        [SerializeField] private int toppingTapiocaPearls = 0;
        [SerializeField] private int toppingPoppingBoba = 0;
        [SerializeField] private int toppingGrassJelly = 0;
        [SerializeField] private int toppingEggPudding = 0;
        [SerializeField] private int toppingCoconutJelly = 0;
        [SerializeField] private int toppingCheeseFoam = 0;
        [SerializeField] private int toppingGoldenHoneyPearls = 0;

        [Header("Milks")]
        [SerializeField] private int milkFreshMilk = 0;
        [SerializeField] private int milkOatMilk = 0;
        [SerializeField] private int milkCoconutMilk = 0;
        [SerializeField] private int milkCondensedMilk = 0;

        [Header("Base Supplies & Teas")]
        [SerializeField] private int cups = 0;
        [SerializeField] private int sugar = 0;
        [SerializeField] private int ice = 0;
        [SerializeField] private int teaBlack = 0;
        [SerializeField] private int teaGreen = 0;
        [SerializeField] private int teaOolong = 0;
        [SerializeField] private int teaThai = 0;
        [SerializeField] private int teaTaro = 0;

        private Dictionary<string, int> stock = new Dictionary<string, int>();
        private HashSet<string> discoveredKeys = new HashSet<string>();

        public event Action OnInventoryUpdated;

        public bool HasEverHadStock(string key)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsBlitzMode) return true;
            if (string.IsNullOrEmpty(key)) return false;
            return (discoveredKeys != null && discoveredKeys.Contains(key)) || GetStock(key) > 0;
        }

        private void MarkDiscovered(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                if (discoveredKeys == null) discoveredKeys = new HashSet<string>();
                discoveredKeys.Add(key);
            }
        }

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

        private void Update()
        {
            CheckInspectorModifications();
        }

        private void OnValidate()
        {
            if (Application.isPlaying && stock.Count > 0)
            {
                CheckInspectorModifications();
            }
        }

        private void SyncDictionaryToInspector()
        {
            rawBabyYippees = GetStock($"Raw_{RawIngredientType.BabyYippees}");
            rawJellyBlocks = GetStock($"Raw_{RawIngredientType.JellyBlocks}");
            rawGoldenDew = GetStock($"Raw_{RawIngredientType.GoldenDew}");

            toppingTapiocaPearls = GetStock($"Topping_{ToppingType.TapiocaPearls}");
            toppingPoppingBoba = GetStock($"Topping_{ToppingType.PoppingBoba}");
            toppingGrassJelly = GetStock($"Topping_{ToppingType.GrassJelly}");
            toppingEggPudding = GetStock($"Topping_{ToppingType.EggPudding}");
            toppingCoconutJelly = GetStock($"Topping_{ToppingType.CoconutJelly}");
            toppingCheeseFoam = GetStock($"Topping_{ToppingType.CheeseFoam}");
            toppingGoldenHoneyPearls = GetStock($"Topping_{ToppingType.GoldenHoneyPearls}");

            milkFreshMilk = GetStock($"Milk_{MilkType.FreshMilk}");
            milkOatMilk = GetStock($"Milk_{MilkType.OatMilk}");
            milkCoconutMilk = GetStock($"Milk_{MilkType.CoconutMilk}");
            milkCondensedMilk = GetStock($"Milk_{MilkType.CondensedMilk}");

            cups = GetStock("Cup");
            sugar = GetStock("Sugar");
            ice = GetStock("Ice");

            teaBlack = GetStock($"Tea_{TeaBase.BlackTea}");
            teaGreen = GetStock($"Tea_{TeaBase.GreenTea}");
            teaOolong = GetStock($"Tea_{TeaBase.OolongTea}");
            teaThai = GetStock($"Tea_{TeaBase.ThaiTea}");
            teaTaro = GetStock($"Tea_{TeaBase.TaroTea}");
        }

        private void CheckInspectorModifications()
        {
            if (stock.Count == 0) return;

            bool changed = false;

            changed |= UpdateKeyIfDifferent($"Raw_{RawIngredientType.BabyYippees}", rawBabyYippees);
            changed |= UpdateKeyIfDifferent($"Raw_{RawIngredientType.JellyBlocks}", rawJellyBlocks);
            changed |= UpdateKeyIfDifferent($"Raw_{RawIngredientType.GoldenDew}", rawGoldenDew);

            changed |= UpdateKeyIfDifferent($"Topping_{ToppingType.TapiocaPearls}", toppingTapiocaPearls);
            changed |= UpdateKeyIfDifferent($"Topping_{ToppingType.PoppingBoba}", toppingPoppingBoba);
            changed |= UpdateKeyIfDifferent($"Topping_{ToppingType.GrassJelly}", toppingGrassJelly);
            changed |= UpdateKeyIfDifferent($"Topping_{ToppingType.EggPudding}", toppingEggPudding);
            changed |= UpdateKeyIfDifferent($"Topping_{ToppingType.CoconutJelly}", toppingCoconutJelly);
            changed |= UpdateKeyIfDifferent($"Topping_{ToppingType.CheeseFoam}", toppingCheeseFoam);
            changed |= UpdateKeyIfDifferent($"Topping_{ToppingType.GoldenHoneyPearls}", toppingGoldenHoneyPearls);

            changed |= UpdateKeyIfDifferent($"Milk_{MilkType.FreshMilk}", milkFreshMilk);
            changed |= UpdateKeyIfDifferent($"Milk_{MilkType.OatMilk}", milkOatMilk);
            changed |= UpdateKeyIfDifferent($"Milk_{MilkType.CoconutMilk}", milkCoconutMilk);
            changed |= UpdateKeyIfDifferent($"Milk_{MilkType.CondensedMilk}", milkCondensedMilk);

            changed |= UpdateKeyIfDifferent("Cup", cups);
            changed |= UpdateKeyIfDifferent("Sugar", sugar);
            changed |= UpdateKeyIfDifferent("Ice", ice);

            changed |= UpdateKeyIfDifferent($"Tea_{TeaBase.BlackTea}", teaBlack);
            changed |= UpdateKeyIfDifferent($"Tea_{TeaBase.GreenTea}", teaGreen);
            changed |= UpdateKeyIfDifferent($"Tea_{TeaBase.OolongTea}", teaOolong);
            changed |= UpdateKeyIfDifferent($"Tea_{TeaBase.ThaiTea}", teaThai);
            changed |= UpdateKeyIfDifferent($"Tea_{TeaBase.TaroTea}", teaTaro);

            if (changed)
            {
                OnInventoryUpdated?.Invoke();
                PrepAreaViewController.Instance?.UpdateUnlocksAndDisplay();
                CashRegisterInventoryUI.Instance?.UpdateInventoryDisplay();
            }
        }

        private bool UpdateKeyIfDifferent(string key, int inspectorValue)
        {
            int current = GetStock(key);
            if (current != inspectorValue)
            {
                stock[key] = Mathf.Max(0, inspectorValue);
                return true;
            }
            return false;
        }

        public void SetupDay1StarterStock()
        {
            stock.Clear();
            if (discoveredKeys == null) discoveredKeys = new HashSet<string>();
            discoveredKeys.Clear();

            stock["Cup"] = startingCups;
            stock["Sugar"] = startingSugarServings;
            stock["Ice"] = startingIceServings;

            // Day 1 Teas
            stock[$"Tea_{TeaBase.BlackTea}"] = startingTeaServings;
            stock[$"Tea_{TeaBase.GreenTea}"] = startingTeaServings;
            stock[$"Tea_{TeaBase.OolongTea}"] = 10;
            stock[$"Tea_{TeaBase.ThaiTea}"] = 10;
            stock[$"Tea_{TeaBase.TaroTea}"] = 10;

            // Day 1 Milk: Fresh Milk
            stock[$"Milk_{MilkType.FreshMilk}"] = startingMilkServings;
            stock[$"Milk_{MilkType.OatMilk}"] = 0;
            stock[$"Milk_{MilkType.CoconutMilk}"] = 0;
            stock[$"Milk_{MilkType.CondensedMilk}"] = 0;

            // Day 1 Toppings: Tapioca Pearls
            stock[$"Topping_{ToppingType.TapiocaPearls}"] = startingToppings;
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

            // Mark initial items as discovered
            foreach (var kvp in stock)
            {
                if (kvp.Value > 0)
                {
                    MarkDiscovered(kvp.Key);
                }
            }

            hasPremiumMilkDispenser = false;
            SyncDictionaryToInspector();
            OnInventoryUpdated?.Invoke();
        }

        public void UnlockPremiumMilkDispenser()
        {
            hasPremiumMilkDispenser = true;
            AddMilkStock(MilkType.OatMilk, 1);
            AddMilkStock(MilkType.CoconutMilk, 1);
            AddMilkStock(MilkType.CondensedMilk, 1);
            HUDController.Instance?.ShowNotification("Premium Milk Dispenser Unlocked (+1 sample of Oat, Coconut, Condensed Milk)!", 4f);
            OnInventoryUpdated?.Invoke();
        }

        private void InitializeStock()
        {
            SetupDay1StarterStock();
        }

        public int GetStock(string key)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsBlitzMode)
            {
                return 99;
            }
            return stock.TryGetValue(key, out int count) ? count : 0;
        }

        public int GetTeaStock(TeaBase tea) => GetStock($"Tea_{tea}");
        public int GetMilkStock(MilkType milk) => GetStock($"Milk_{milk}");
        public int GetToppingStock(ToppingType topping) => GetStock($"Topping_{topping}");
        public int GetCupStock() => GetStock("Cup");

        public bool HasStock(string key, int quantity = 1)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsBlitzMode)
            {
                return true;
            }
            return GetStock(key) >= quantity;
        }

        public bool ConsumeStock(string key, int quantity = 1)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsBlitzMode)
            {
                return true;
            }

            int current = GetStock(key);
            if (current >= quantity)
            {
                stock[key] = current - quantity;
                SyncDictionaryToInspector();
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
            MarkDiscovered(key);
            SyncDictionaryToInspector();
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
