using System;
using System.Collections.Generic;
using UnityEngine;

namespace BubbleTeaShop
{
    [System.Serializable]
    public class InventoryItemEntry
    {
        public string key;
        public int quantity;

        public InventoryItemEntry() { }
        public InventoryItemEntry(string k, int q)
        {
            key = k;
            quantity = q;
        }
    }

    [System.Serializable]
    public class GameSaveData
    {
        public int saveVersion = 1;
        public string saveTimestamp = "";

        // Game Configuration
        public int gameMode = 0; // 0 = Normal, 1 = Blitz
        public int difficulty = 1; // 0 = Easy, 1 = Normal, 2 = Hard

        // Day Progression
        public int currentDay = 1;
        public int lastCompletedDay = 1;
        public bool hadNightActivityLastNight = false;

        // Economy
        public float currentCash = 50.00f;
        public float accumulatedRentOwed = 0f;
        public int rentSkipsUsed = 0;
        public bool isEndlessMode = false;

        // Inventory
        public bool hasPremiumMilkDispenser = false;
        public List<InventoryItemEntry> stockList = new List<InventoryItemEntry>();
        public List<string> discoveredKeys = new List<string>();

        // Upgrades
        public List<int> purchasedUpgrades = new List<int>();
    }

    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<SaveManager>();
                    if (instance == null)
                    {
                        var go = new GameObject("SaveManager");
                        instance = go.AddComponent<SaveManager>();
                    }
                }
                return instance;
            }
            private set => instance = value;
        }
        private static SaveManager instance;

        private const string SAVE_KEY = "BubbleTeaShop_SaveData";
        private const string HAS_SAVE_KEY = "BubbleTeaShop_HasSave";

        public event Action OnSaveCompleted;
        public event Action OnSaveLoaded;
        public event Action OnSaveCleared;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        public bool HasSave()
        {
            return PlayerPrefs.GetInt(HAS_SAVE_KEY, 0) == 1 && PlayerPrefs.HasKey(SAVE_KEY);
        }

        public GameSaveData GetCurrentSaveData()
        {
            if (!HasSave()) return null;

            try
            {
                string json = PlayerPrefs.GetString(SAVE_KEY, "");
                if (string.IsNullOrEmpty(json)) return null;
                return JsonUtility.FromJson<GameSaveData>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveManager] Error reading save data: {ex.Message}");
                return null;
            }
        }

        public void SaveGame()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsCasualMode)
            {
                // Casual mode is an endless no-stakes session; do not overwrite persistent story save
                return;
            }

            try
            {
                GameSaveData data = new GameSaveData
                {
                    saveVersion = 1,
                    saveTimestamp = DateTime.UtcNow.ToString("o"),
                    gameMode = GameManager.Instance != null ? (int)GameManager.Instance.CurrentGameMode : 0,
                    difficulty = GameManager.Instance != null ? (int)GameManager.Instance.CurrentDifficulty : 1
                };

                // Day Progression
                if (DayManager.Instance != null)
                {
                    data.currentDay = DayManager.Instance.CurrentDay;
                    data.lastCompletedDay = DayManager.Instance.LastCompletedDay;
                    data.hadNightActivityLastNight = DayManager.Instance.HadNightActivityLastNight;
                }

                // Economy
                if (EconomyManager.Instance != null)
                {
                    data.currentCash = EconomyManager.Instance.CurrentCash;
                    data.accumulatedRentOwed = EconomyManager.Instance.AccumulatedRentOwed;
                    data.rentSkipsUsed = EconomyManager.Instance.RentSkipsUsed;
                    data.isEndlessMode = EconomyManager.Instance.IsEndlessMode;
                }

                // Inventory
                if (InventoryManager.Instance != null)
                {
                    data.hasPremiumMilkDispenser = InventoryManager.Instance.HasPremiumMilkDispenser;
                    var stock = InventoryManager.Instance.GetAllStock();
                    data.stockList.Clear();
                    foreach (var kvp in stock)
                    {
                        data.stockList.Add(new InventoryItemEntry(kvp.Key, kvp.Value));
                    }

                    var discovered = InventoryManager.Instance.GetDiscoveredKeys();
                    data.discoveredKeys.Clear();
                    data.discoveredKeys.AddRange(discovered);
                }

                // Upgrades
                if (UpgradeManager.Instance != null)
                {
                    data.purchasedUpgrades.Clear();
                    var list = UpgradeManager.Instance.GetPurchasedUpgrades();
                    foreach (var u in list)
                    {
                        data.purchasedUpgrades.Add((int)u);
                    }
                }

                string json = JsonUtility.ToJson(data, false);
                PlayerPrefs.SetString(SAVE_KEY, json);
                PlayerPrefs.SetInt(HAS_SAVE_KEY, 1);
                PlayerPrefs.Save();

                Debug.Log($"[SaveManager] Auto-saved successfully at Day {data.currentDay}, Cash ${data.currentCash:F2}, Upgrades: {data.purchasedUpgrades.Count}.");
                OnSaveCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to auto-save game: {ex.Message}");
            }
        }

        public bool LoadGame()
        {
            GameSaveData data = GetCurrentSaveData();
            if (data == null)
            {
                Debug.LogWarning("[SaveManager] No save data found to load!");
                return false;
            }

            try
            {
                // Restore Game Mode & Difficulty
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.RestoreGameSettings((GameMode)data.gameMode, (GameDifficulty)data.difficulty);
                }

                // Restore Day Progression
                if (DayManager.Instance != null)
                {
                    DayManager.Instance.RestoreDay(data.currentDay, data.lastCompletedDay, data.hadNightActivityLastNight);
                }

                // Restore Economy
                if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.RestoreEconomy(data.currentCash, data.accumulatedRentOwed, data.rentSkipsUsed, data.isEndlessMode);
                }

                // Restore Inventory
                if (InventoryManager.Instance != null)
                {
                    var stockDict = new Dictionary<string, int>();
                    if (data.stockList != null)
                    {
                        foreach (var item in data.stockList)
                        {
                            stockDict[item.key] = item.quantity;
                        }
                    }
                    InventoryManager.Instance.RestoreStock(stockDict, data.discoveredKeys, data.hasPremiumMilkDispenser);
                }

                // Restore Upgrades
                if (UpgradeManager.Instance != null)
                {
                    var upgrades = new List<UpgradeType>();
                    if (data.purchasedUpgrades != null)
                    {
                        foreach (var id in data.purchasedUpgrades)
                        {
                            upgrades.Add((UpgradeType)id);
                        }
                    }
                    UpgradeManager.Instance.RestorePurchasedUpgrades(upgrades);
                }

                // Refresh HUD
                if (HUDController.Instance != null)
                {
                    HUDController.Instance.RefreshHUDDisplay();
                    HUDController.Instance.UpdateDayDisplay(data.currentDay);
                    HUDController.Instance.UpdateMarketEventDisplay();
                }

                Debug.Log($"[SaveManager] Save loaded successfully! Resuming Day {data.currentDay}, Cash ${data.currentCash:F2}.");
                OnSaveLoaded?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to load save data: {ex.Message}");
                return false;
            }
        }

        public void ClearSave()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.DeleteKey(HAS_SAVE_KEY);
            PlayerPrefs.DeleteKey("BubbleTea_HasSave");
            PlayerPrefs.Save();

            Debug.Log("[SaveManager] Save data cleared.");
            OnSaveCleared?.Invoke();
        }
    }
}
