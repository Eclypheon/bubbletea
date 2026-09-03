using System;
using System.Collections.Generic;
using UnityEngine;

namespace BubbleTeaShop
{
    public class ForagingManager : MonoBehaviour
    {
        public static ForagingManager Instance { get; private set; }

        private bool hasForagedTonight = false;
        public bool HasForagedTonight => hasForagedTonight;

        public event Action<string> OnForagingResult;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void ResetNightForaging()
        {
            hasForagedTonight = false;
        }

        public void SetForagedTonight()
        {
            hasForagedTonight = true;
        }

        public bool IsZoneUnlocked(string zone, int day)
        {
            return zone switch
            {
                "BambooGrove" => day >= 5,
                "HoneyMeadow" => day >= 11,
                "MistMountain" => day >= 18,
                _ => false
            };
        }

        public bool GoForaging(string zone)
        {
            if (NightPhaseManager.Instance != null && NightPhaseManager.Instance.PerformedActivityTonight == NightPhaseManager.NightActivityType.Market)
            {
                OnForagingResult?.Invoke("You are too exhausted from visiting the Supermarket! Only 1 night activity allowed per night.");
                HUDController.Instance?.ShowNotification("You've already visited the Market tonight! Only 1 night activity allowed per night (late opening penalty tomorrow).", 4.5f);
                return false;
            }

            if (hasForagedTonight)
            {
                OnForagingResult?.Invoke("You are too exhausted to forage again tonight! Rest up for tomorrow.");
                return false;
            }

            hasForagedTonight = true;
            NightPhaseManager.Instance?.RecordActivity(NightPhaseManager.NightActivityType.Foraging, zone);
            string resultMessage = "";

            bool isGoldenHarvest = MarketEventManager.Instance != null && MarketEventManager.Instance.ActiveEvent?.eventId == "golden_harvest";
            int mult = isGoldenHarvest ? 2 : 1;

            if (zone == "BambooGrove")
            {
                InventoryManager.Instance.AddTeaStock(TeaBase.GreenTea, 6 * mult);
                InventoryManager.Instance.AddToppingStock(ToppingType.GrassJelly, 4 * mult);
                resultMessage = isGoldenHarvest
                    ? "Bamboo Grove (2x Harvest): Foraged 12x Fresh Jasmine Green Leaves and 8x Herbal Grass Jelly herbs!"
                    : "Bamboo Grove: Foraged 6x Fresh Jasmine Green Leaves and 4x Herbal Grass Jelly herbs!";
            }
            else if (zone == "HoneyMeadow")
            {
                InventoryManager.Instance.AddToppingStock(ToppingType.GoldenHoneyPearls, 6 * mult);
                EconomyManager.Instance.AddCash(15f * mult, "Foraged Wild Honey Sale");
                resultMessage = isGoldenHarvest
                    ? "Honey Meadows (2x Harvest): Discovered 12x Rare Golden Honey Pearls and sold extra wild honeycomb for +$30.00!"
                    : "Honey Meadows: Discovered 6x Rare Golden Honey Pearls and sold extra wild honeycomb for +$15.00!";
            }
            else if (zone == "MistMountain")
            {
                InventoryManager.Instance.AddRawStock(RawIngredientType.GoldenDew, 5); // AddRawStock will multiply by 2x automatically
                resultMessage = isGoldenHarvest
                    ? "Misty Mountains (2x Harvest): Scaled the peak and collected 10x Raw Golden Dew!"
                    : "Misty Mountains: Scaled the peak and collected 5x Raw Golden Dew!";
            }

            OnForagingResult?.Invoke(resultMessage);
            SaveManager.Instance?.SaveGame();
            return true;
        }
    }
}
