using System;
using System.Collections.Generic;
using UnityEngine;

namespace BubbleTeaShop
{
    [System.Serializable]
    public class MarketEvent
    {
        public string eventId;
        public string title;
        public string description;
        public string affectedKey;
        public float priceMultiplier = 1.0f;
        public float demandMultiplier = 1.0f;
        public int totalDurationDays = 3;
        public int daysRemaining = 3;
    }

    public class MarketEventManager : MonoBehaviour
    {
        public static MarketEventManager Instance { get; private set; }

        [Header("Runtime State")]
        [SerializeField] private MarketEvent activeEvent = null;
        [SerializeField] private bool hasNewEventToday = false;
        [SerializeField] private int lastEventEndDay = 0;

        [Header("Debug / Inspector Event Selector")]
        [Tooltip("Select any market event from this dropdown to preview its icon and test its mechanics in-game.")]
        [SerializeField] private MarketEventType testEventSelection = MarketEventType.None;

        public MarketEvent ActiveEvent => (activeEvent != null && !string.IsNullOrEmpty(activeEvent.eventId) && !string.IsNullOrEmpty(activeEvent.title)) ? activeEvent : null;
        public bool HasNewEventToday => hasNewEventToday && ActiveEvent != null;
        public MarketEventType TestEventSelection => testEventSelection;

        public event Action<MarketEvent> OnMarketEventTriggered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (activeEvent != null && (string.IsNullOrEmpty(activeEvent.eventId) || string.IsNullOrEmpty(activeEvent.title)))
            {
                activeEvent = null;
            }
            hasNewEventToday = false;
        }

        private void Start()
        {
            if (activeEvent != null && (string.IsNullOrEmpty(activeEvent.eventId) || string.IsNullOrEmpty(activeEvent.title)))
            {
                activeEvent = null;
            }

            if (DayManager.Instance != null)
            {
                DayManager.Instance.OnDayStarted -= EvaluateDailyEvent;
                DayManager.Instance.OnDayStarted += EvaluateDailyEvent;
            }
        }

        private void OnValidate()
        {
            if (testEventSelection != MarketEventType.None)
            {
                activeEvent = CreateEvent(testEventSelection);
                if (activeEvent != null)
                {
                    hasNewEventToday = true;
                }
            }
        }

        public void SetEventByType(MarketEventType type)
        {
            testEventSelection = type;
            if (type == MarketEventType.None)
            {
                activeEvent = null;
            }
            else
            {
                activeEvent = CreateEvent(type);
                if (activeEvent != null)
                {
                    hasNewEventToday = true;
                    OnMarketEventTriggered?.Invoke(activeEvent);
                }
            }
            HUDController.Instance?.UpdateMarketEventDisplay();
        }

        public void ClearActiveEvent()
        {
            testEventSelection = MarketEventType.None;
            activeEvent = null;
            hasNewEventToday = false;
            HUDController.Instance?.UpdateMarketEventDisplay();
        }

        private void OnDestroy()
        {
            if (DayManager.Instance != null)
            {
                DayManager.Instance.OnDayStarted -= EvaluateDailyEvent;
            }
        }

        public void ConsumeNewEventFlag()
        {
            hasNewEventToday = false;
        }

        public void EvaluateDailyEvent(int dayNumber)
        {
            hasNewEventToday = false;

            if (activeEvent != null && (string.IsNullOrEmpty(activeEvent.eventId) || string.IsNullOrEmpty(activeEvent.title)))
            {
                activeEvent = null;
            }

            // Days 1, 2, and 3: 0% probability of market events
            if (dayNumber <= 3)
            {
                activeEvent = null;
                return;
            }

            // If an event is already active, decrement its remaining days
            if (activeEvent != null)
            {
                activeEvent.daysRemaining--;
                if (activeEvent.daysRemaining <= 0)
                {
                    Debug.Log($"[MarketEventManager] Event '{activeEvent.title}' has concluded.");
                    activeEvent = null;
                    lastEventEndDay = dayNumber;
                }
                else
                {
                    Debug.Log($"[MarketEventManager] Event '{activeEvent.title}' continuing ({activeEvent.daysRemaining} days remaining).");
                    OnMarketEventTriggered?.Invoke(activeEvent);
                    return;
                }
            }

            // Roll for a new 3-day event if no event is active
            if (activeEvent == null)
            {
                // Day 4 is guaranteed 100% probability. Subsequently, 55% chance with at least 1-day breather.
                bool shouldTrigger = (dayNumber == 4) || (dayNumber > lastEventEndDay && UnityEngine.Random.value < 0.55f);

                if (shouldTrigger)
                {
                    activeEvent = GenerateRandomEvent(dayNumber);
                    if (activeEvent != null)
                    {
                        activeEvent.totalDurationDays = 3;
                        activeEvent.daysRemaining = 3;
                        hasNewEventToday = true;
                        Debug.Log($"[MarketEventManager] New 3-day event triggered on Day {dayNumber}: {activeEvent.title}");
                        OnMarketEventTriggered?.Invoke(activeEvent);
                    }
                }
            }
        }

        public static MarketEvent CreateEvent(MarketEventType type)
        {
            return type switch
            {
                MarketEventType.TapiocaPearlShortage => new MarketEvent
                {
                    eventId = "tapioca_delay",
                    title = "Tapioca Pearl Shortage",
                    description = "Harbor shipping delays cause pearl wholesale prices to rise (+40%), and eager customers crave classic Boba (+50% demand)!",
                    affectedKey = "Topping_TapiocaPearls",
                    priceMultiplier = 1.40f,
                    demandMultiplier = 1.50f,
                    totalDurationDays = 3,
                    daysRemaining = 3
                },
                MarketEventType.LocalDairySurplus => new MarketEvent
                {
                    eventId = "dairy_surplus",
                    title = "Local Dairy Surplus",
                    description = "Local pastures produced an abundance of milk! Fresh Milk & Oat Milk wholesale packs are discounted by 30%!",
                    affectedKey = "Milk_FreshMilk",
                    priceMultiplier = 0.70f,
                    demandMultiplier = 1.30f,
                    totalDurationDays = 3,
                    daysRemaining = 3
                },
                MarketEventType.TropicalCoconutHarvest => new MarketEvent
                {
                    eventId = "tropical_coconut",
                    title = "Tropical Coconut Harvest",
                    description = "A massive harvest of tropical coconuts has arrived! Coconut Milk & Coconut Jelly wholesale prices drop by 35%!",
                    affectedKey = "Milk_CoconutMilk",
                    priceMultiplier = 0.65f,
                    demandMultiplier = 1.40f,
                    totalDurationDays = 3,
                    daysRemaining = 3
                },
                MarketEventType.GourmetCreamShortage => new MarketEvent
                {
                    eventId = "cream_shortage",
                    title = "Gourmet Cream Shortage",
                    description = "Egg Custard and Cheese Foam wholesale prices rise (+30%), but customers are tipping generously (+25% tips) on rich drinks!",
                    affectedKey = "Topping_CheeseFoam",
                    priceMultiplier = 1.30f,
                    demandMultiplier = 1.25f,
                    totalDurationDays = 3,
                    daysRemaining = 3
                },
                MarketEventType.PlantBasedMilkCraze => new MarketEvent
                {
                    eventId = "plant_based_craze",
                    title = "Plant-Based Milk Craze",
                    description = "A viral wellness article surges customer demand for Barista Oat Milk and Organic Coconut Milk (+60% orders)!",
                    affectedKey = "Milk_OatMilk",
                    priceMultiplier = 1.0f,
                    demandMultiplier = 1.60f,
                    totalDurationDays = 3,
                    daysRemaining = 3
                },
                MarketEventType.HerbalWellnessTrend => new MarketEvent
                {
                    eventId = "wellness_trend",
                    title = "Herbal Wellness Trend",
                    description = "Customers favor low/zero sweetness and refreshing Herbal Grass Jelly toppings (+50% Grass Jelly orders)!",
                    affectedKey = "Topping_GrassJelly",
                    priceMultiplier = 0.85f,
                    demandMultiplier = 1.50f,
                    totalDurationDays = 3,
                    daysRemaining = 3
                },
                MarketEventType.SummerHeatwave => new MarketEvent
                {
                    eventId = "summer_heatwave",
                    title = "Summer Heatwave",
                    description = "Scorching sunny weather hits town! Customers heavily prefer 100% Full Ice (+70% ice demand) and fruity Popping Boba!",
                    affectedKey = "Ice",
                    priceMultiplier = 1.0f,
                    demandMultiplier = 1.70f,
                    totalDurationDays = 3,
                    daysRemaining = 3
                },
                MarketEventType.ChillyMonsoonRain => new MarketEvent
                {
                    eventId = "chilly_rain",
                    title = "Chilly Monsoon Rain",
                    description = "A rainy cold front sweeps across the city! Customers prefer 0% Ice (No Ice) and rich, creamy comfort milks!",
                    affectedKey = "Milk_CondensedMilk",
                    priceMultiplier = 1.0f,
                    demandMultiplier = 1.40f,
                    totalDurationDays = 3,
                    daysRemaining = 3
                },
                MarketEventType.BountifulForagingSeason => new MarketEvent
                {
                    eventId = "golden_harvest",
                    title = "Bountiful Foraging Season",
                    description = "Wild groves and honey meadows are flourishing! Foraging expeditions yield double harvests for the next 3 days!",
                    affectedKey = "Foraging",
                    priceMultiplier = 1.0f,
                    demandMultiplier = 2.0f,
                    totalDurationDays = 3,
                    daysRemaining = 3
                },
                _ => null
            };
        }

        private MarketEvent GenerateRandomEvent(int dayNumber)
        {
            List<MarketEventType> pool = new List<MarketEventType>
            {
                MarketEventType.TapiocaPearlShortage,
                MarketEventType.LocalDairySurplus,
                MarketEventType.TropicalCoconutHarvest,
                MarketEventType.GourmetCreamShortage,
                MarketEventType.PlantBasedMilkCraze,
                MarketEventType.HerbalWellnessTrend,
                MarketEventType.SummerHeatwave,
                MarketEventType.ChillyMonsoonRain
            };

            if (dayNumber >= 5)
            {
                pool.Add(MarketEventType.BountifulForagingSeason);
            }

            MarketEventType selected = pool[UnityEngine.Random.Range(0, pool.Count)];
            return CreateEvent(selected);
        }

        public float GetPriceMultiplier(string stockKey)
        {
            if (activeEvent == null) return 1.0f;

            if (activeEvent.affectedKey == stockKey)
            {
                return activeEvent.priceMultiplier;
            }

            // Multi-key group handlers
            if (activeEvent.eventId == "dairy_surplus" && (stockKey == "Milk_FreshMilk" || stockKey == "Milk_OatMilk"))
            {
                return 0.70f;
            }
            if (activeEvent.eventId == "tropical_coconut" && (stockKey == "Milk_CoconutMilk" || stockKey == "Topping_CoconutJelly"))
            {
                return 0.65f;
            }
            if (activeEvent.eventId == "cream_shortage" && (stockKey == "Topping_CheeseFoam" || stockKey == "Topping_EggPudding"))
            {
                return 1.30f;
            }

            return 1.0f;
        }

        public float GetDemandMultiplier(string key)
        {
            if (activeEvent != null && activeEvent.affectedKey == key)
            {
                return activeEvent.demandMultiplier;
            }
            return 1.0f;
        }
    }
}
