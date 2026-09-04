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

        public MarketEvent ActiveEvent => (GameManager.Instance != null && GameManager.Instance.IsCasualMode) ? null : ((activeEvent != null && !string.IsNullOrEmpty(activeEvent.eventId) && !string.IsNullOrEmpty(activeEvent.title)) ? activeEvent : null);
        public bool HasNewEventToday => (GameManager.Instance != null && GameManager.Instance.IsCasualMode) ? false : (hasNewEventToday && ActiveEvent != null);
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

            if (GameManager.Instance != null && GameManager.Instance.IsCasualMode)
            {
                activeEvent = null;
                return;
            }

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
                // Day 4 is guaranteed 100% probability. Subsequently, 75% chance with at least 1-day breather.
                bool shouldTrigger = (dayNumber == 4) || (dayNumber > lastEventEndDay && UnityEngine.Random.value < 0.75f);

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
                    description = "Shipping delays at the harbor! Tapioca wholesale prices +40%, customer boba demand +50%.",
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
                    description = "Local pastures produced a massive surplus of milk! Fresh Milk & Oat Milk wholesale prices -30%, customer milk demand -35%.",
                    affectedKey = "Milk_FreshMilk",
                    priceMultiplier = 0.70f,
                    demandMultiplier = 0.65f,
                    totalDurationDays = 3,
                    daysRemaining = 3
                },
                MarketEventType.TropicalCoconutHarvest => new MarketEvent
                {
                    eventId = "tropical_coconut",
                    title = "Tropical Coconut Harvest",
                    description = "Massive harvest flooded the market! Coconut Milk & Jelly wholesale prices -35%, customer coconut demand -40%.",
                    affectedKey = "Milk_CoconutMilk",
                    priceMultiplier = 0.65f,
                    demandMultiplier = 0.60f,
                    totalDurationDays = 3,
                    daysRemaining = 3
                },
                MarketEventType.GourmetCreamShortage => new MarketEvent
                {
                    eventId = "cream_shortage",
                    title = "Gourmet Cream Shortage",
                    description = "Gourmet cream supply is tight! Cheese Foam & Egg Pudding wholesale prices +30%, customer tips on rich drinks +25%.",
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
                    description = "A viral wellness article is trending! Oat Milk & Coconut Milk wholesale prices +30%, demand +60%.",
                    affectedKey = "Milk_OatMilk",
                    priceMultiplier = 1.30f,
                    demandMultiplier = 1.60f,
                    totalDurationDays = 3,
                    daysRemaining = 3
                },
                MarketEventType.HerbalWellnessTrend => new MarketEvent
                {
                    eventId = "wellness_trend",
                    title = "Herbal Wellness Trend",
                    description = "Herbal wellness is in style! Grass Jelly wholesale prices +30%, Grass Jelly demand +50%, low/zero sugar preferred.",
                    affectedKey = "Topping_GrassJelly",
                    priceMultiplier = 1.30f,
                    demandMultiplier = 1.50f,
                    totalDurationDays = 3,
                    daysRemaining = 3
                },
                MarketEventType.SummerHeatwave => new MarketEvent
                {
                    eventId = "summer_heatwave",
                    title = "Summer Heatwave",
                    description = "Scorching heat hits town! Customers crave 100% Full Ice (+70% ice demand).",
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
                    description = "Freezing rainy cold front sweeps through! Customers crave 0% No Ice (hot comfort drinks).",
                    affectedKey = "Ice",
                    priceMultiplier = 1.0f,
                    demandMultiplier = 1.40f,
                    totalDurationDays = 3,
                    daysRemaining = 3
                },
                MarketEventType.BountifulForagingSeason => new MarketEvent
                {
                    eventId = "golden_harvest",
                    title = "Bountiful Foraging Season",
                    description = "Wild foraging regions are flourishing! Foraging expeditions yield 2.0x double harvests across all zones.",
                    affectedKey = "Foraging",
                    priceMultiplier = 1.0f,
                    demandMultiplier = 2.0f,
                    totalDurationDays = 3,
                    daysRemaining = 3
                },
                MarketEventType.WholesaleStockClearance => new MarketEvent
                {
                    eventId = "stock_clearance",
                    title = "Wholesale Stock Clearance",
                    description = "Flash clearance at the wholesale market! ALL ingredients, milks, and toppings are 70% cheaper (-70% off).",
                    affectedKey = "All_Stock",
                    priceMultiplier = 0.30f,
                    demandMultiplier = 1.0f,
                    totalDurationDays = 3,
                    daysRemaining = 3
                },
                _ => null
            };
        }

        private MarketEvent GenerateRandomEvent(int dayNumber)
        {
            // Week 1 Pool (Basic ingredients available on Days 1–4, Day 4 3-day duration reaches Day 5/6 foraging, plus Wholesale clearance & Weather)
            List<MarketEventType> pool = new List<MarketEventType>
            {
                MarketEventType.TapiocaPearlShortage,
                MarketEventType.LocalDairySurplus,
                MarketEventType.SummerHeatwave,
                MarketEventType.HerbalWellnessTrend,
                MarketEventType.BountifulForagingSeason,
                MarketEventType.WholesaleStockClearance,
                MarketEventType.ChillyMonsoonRain
            };

            // Week 2+ (Day 8+): Coconut Milk & Coconut Jelly unlocked at Wholesale Market
            if (dayNumber >= 8)
            {
                pool.Add(MarketEventType.TropicalCoconutHarvest);
                pool.Add(MarketEventType.PlantBasedMilkCraze);
            }

            // Week 3+ (Day 15+): Cheese Foam & Egg Pudding unlocked at Wholesale Market
            if (dayNumber >= 15)
            {
                pool.Add(MarketEventType.GourmetCreamShortage);
            }

            MarketEventType selected = pool[UnityEngine.Random.Range(0, pool.Count)];
            return CreateEvent(selected);
        }

        public float GetPriceMultiplier(string stockKey)
        {
            if (activeEvent == null) return 1.0f;

            // Global stock clearance discount (70% off everything)
            if (activeEvent.eventId == "stock_clearance" || activeEvent.affectedKey == "All_Stock")
            {
                return 0.30f;
            }

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
            if (activeEvent.eventId == "plant_based_craze" && (stockKey == "Milk_OatMilk" || stockKey == "Milk_CoconutMilk"))
            {
                return 1.30f;
            }
            if (activeEvent.eventId == "wellness_trend" && stockKey == "Topping_GrassJelly")
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
