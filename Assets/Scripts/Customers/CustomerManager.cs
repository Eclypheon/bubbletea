using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BubbleTeaShop
{
    public class CustomerManager : MonoBehaviour
    {
        public static CustomerManager Instance { get; private set; }

        [Header("Customer Controller")]
        [SerializeField] private CustomerController customerController;

        [Header("Archetype Patience Durations (Seconds)")]
        [Tooltip("Patience duration in seconds for ADHD customer")]
        [SerializeField] private float adhdPatience = 30f;

        [Tooltip("Patience duration in seconds for Autism customer")]
        [SerializeField] private float autismPatience = 45f;

        [Tooltip("Patience duration in seconds for Anxiety customer")]
        [SerializeField] private float anxietyPatience = 55f;

        [Tooltip("Patience duration in seconds for Tourettes customer")]
        [SerializeField] private float tourettesPatience = 35f;

        [Tooltip("Patience duration in seconds for Dyscalculia customer")]
        [SerializeField] private float dyscalculiaPatience = 60f;

        [Tooltip("Patience duration in seconds for Dyslexia customer")]
        [SerializeField] private float dyslexiaPatience = 50f;

        [Header("Customer Dismissal Safety Settings")]
        [Tooltip("When enabled, ringing the bell while a waiting customer has not yet been served requires a second confirmation ring before dismissing them.")]
        [SerializeField] private bool confirmDismissIfCustomerWaiting = true;

        public bool ConfirmDismissIfCustomerWaiting
        {
            get => confirmDismissIfCustomerWaiting;
            set => confirmDismissIfCustomerWaiting = value;
        }

        // Backward compatibility alias
        public bool ConfirmDismissIfCupNotEmpty
        {
            get => confirmDismissIfCustomerWaiting;
            set => confirmDismissIfCustomerWaiting = value;
        }

        private bool awaitingDismissalConfirmation = false;

        public void ResetDismissalConfirmation()
        {
            awaitingDismissalConfirmation = false;
        }

        private Queue<DrinkOrder> dailyCustomerQueue = new Queue<DrinkOrder>();
        private bool rentEncounterTriggeredToday = false;
        public bool RentEncounterTriggeredToday => rentEncounterTriggeredToday;
        public bool HasCustomerAtWindow => customerController != null && customerController.IsWaitingDrink;

        public event Action<DrinkOrder> OnCustomerArrived;
        public event Action OnAllDailyCustomersFinished;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (customerController != null)
            {
                customerController.OnCustomerServed += HandleCustomerFinished;
                customerController.OnCustomerLeftAngry += HandleCustomerFinishedAngry;
                customerController.DismissCustomer();
            }

            if (DayManager.Instance != null)
            {
                DayManager.Instance.OnDayStarted += GenerateDailyQueue;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += HandleStateChanged;
            }
        }

        private bool hasTriggeredDay4EventBriefing = false;
        [SerializeField] private bool hasTriggeredEndlessLandlordGreeting = false;
        public bool HasTriggeredEndlessLandlordGreeting => hasTriggeredEndlessLandlordGreeting;

        private void HandleStateChanged(GameState state)
        {
            if (state == GameState.ShopOpen)
            {
                // In Blitz mode, skip mentor briefings and landlady cutscenes
                if (GameManager.Instance != null && GameManager.Instance.IsBlitzMode)
                {
                    HUDController.Instance?.SetStatusHint("BLITZ MODE: Serve as many drinks as possible before time runs out!");
                    return;
                }

                int day = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
                bool isEndless = EconomyManager.Instance != null && EconomyManager.Instance.IsEndlessMode;

                if (isEndless && !hasTriggeredEndlessLandlordGreeting && customerController != null)
                {
                    hasTriggeredEndlessLandlordGreeting = true;
                    customerController.SpawnLandlordEndlessGreeting(() =>
                    {
                        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.ShopOpen)
                        {
                            HUDController.Instance?.SetStatusHint("Ring the bell to call the next customer.");
                        }
                    });
                }
                else if (day == 1 && MentorController.Instance != null && !MentorController.Instance.HasCompletedDay1Briefing)
                {
                    MentorController.Instance.TriggerDay1MorningBriefing(customerController);
                }
                else if (MarketEventManager.Instance != null && MentorController.Instance != null && customerController != null)
                {
                    var ev = MarketEventManager.Instance.ActiveEvent;
                    if (ev != null && !string.IsNullOrEmpty(ev.title) && (MarketEventManager.Instance.HasNewEventToday || (day == 4 && !hasTriggeredDay4EventBriefing)))
                    {
                        hasTriggeredDay4EventBriefing = true;
                        MarketEventManager.Instance.ConsumeNewEventFlag();
                        MentorController.Instance.TriggerMarketEventBriefing(customerController, ev);
                    }
                }
            }
        }

        public void GenerateDailyQueue(int dayNumber)
        {
            dailyCustomerQueue.Clear();
            awaitingDismissalConfirmation = false;
            rentEncounterTriggeredToday = false;
            hasTriggeredDay4EventBriefing = false;

            int count = (GameManager.Instance != null && GameManager.Instance.IsBlitzMode) ? 100 : (DayManager.Instance != null ? DayManager.Instance.TotalCustomersToday : 5);

            for (int i = 0; i < count; i++)
            {
                dailyCustomerQueue.Enqueue(GenerateRandomOrder());
            }
            Debug.Log($"[CustomerManager] Generated {count} customers for Day {dayNumber}.");
        }

        public bool TryCallNextCustomer()
        {
            if (customerController != null && customerController.IsLandlordActive)
            {
                if (customerController.IsEndlessGreetingActive)
                {
                    HUDController.Instance?.ShowNotification("Chubi is waiting for her drink! Serve her first.");
                }
                else
                {
                    bool isEndless = EconomyManager.Instance != null && EconomyManager.Instance.IsEndlessMode;
                    HUDController.Instance?.ShowNotification(isEndless ? "Chairwoman Chubi is waiting! Settle her royalties first!" : "The Landlady is waiting! Deal with her first!");
                }
                return false;
            }

            if (customerController != null && customerController.IsMentorTalking)
            {
                HUDController.Instance?.ShowNotification("Listen to your Mentor's advice first!");
                return false;
            }

            if (customerController != null && customerController.IsMentorActive)
            {
                awaitingDismissalConfirmation = false;
                customerController.DismissMentor();
                return SpawnNextInQueue();
            }

            if (customerController != null && customerController.IsPresent)
            {
                if (customerController.IsWaitingDrink)
                {
                    // Safety check: require a second ring if the customer has not yet been served
                    if (confirmDismissIfCustomerWaiting && !awaitingDismissalConfirmation)
                    {
                        awaitingDismissalConfirmation = true;
                        HUDController.Instance?.SetStatusHint("Are you sure you want to dismiss this customer? Ring again to dismiss them.");
                        HUDController.Instance?.ShowNotification("Customer is still waiting! Ring bell again to confirm skipping.", 3.5f);
                        return false;
                    }

                    awaitingDismissalConfirmation = false;
                    // Customer was waiting and unserved -> trigger angry skip reaction, then spawn next
                    customerController.ForceSkipCustomer(() => SpawnNextInQueue());
                    return true;
                }
                else
                {
                    awaitingDismissalConfirmation = false;
                    // Customer was already in departure animation -> dismiss immediately and bring next
                    customerController.DismissCustomer();
                }
            }

            awaitingDismissalConfirmation = false;
            return SpawnNextInQueue();
        }

        private bool SpawnNextInQueue()
        {
            awaitingDismissalConfirmation = false;
            if (dailyCustomerQueue.Count == 0)
            {
                // In Blitz mode, replenish queue continuously so customers never run out
                if (GameManager.Instance != null && GameManager.Instance.IsBlitzMode)
                {
                    for (int i = 0; i < 20; i++)
                    {
                        dailyCustomerQueue.Enqueue(GenerateRandomOrder());
                    }
                }
                else
                {
                    Debug.Log("No more customers in line today!");
                    CheckRemainingCustomers();
                    return false;
                }
            }

            DrinkOrder nextOrder = dailyCustomerQueue.Dequeue();
            float patience = GetPatienceForArchetype(nextOrder.archetype);
            DayManager.Instance?.AdvanceCustomerIndex();
            GameManager.Instance?.SetState(GameState.CustomerWaiting);
            customerController.SpawnCustomer(nextOrder, patience);
            OnCustomerArrived?.Invoke(nextOrder);
            return true;
        }

        private Coroutine rentArrivalRoutine;

        public void ServeCurrentCustomer(BubbleTeaCup cup)
        {
            awaitingDismissalConfirmation = false;
            if (HasCustomerAtWindow)
            {
                customerController.ReceiveDrink(cup);

                if (GameManager.Instance != null && GameManager.Instance.IsBlitzMode)
                {
                    // Blitz mode customer transition is handled immediately inside HandleCustomerFinished / ReceiveDrink
                    return;
                }

                // If more customers remain in queue, allow ringing bell while current customer leaves
                if (dailyCustomerQueue.Count > 0)
                {
                    GameManager.Instance?.SetState(GameState.ShopOpen);
                }
            }
        }

        private void HandleCustomerFinished(CustomerController customer, EvaluationResult result)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsBlitzMode && GameManager.Instance.BlitzTimeRemaining > 0f)
            {
                customerController?.DismissCustomer();
                SpawnNextInQueue();
                return;
            }

            CheckRemainingCustomers();
        }

        private void HandleCustomerFinishedAngry(CustomerController customer)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsBlitzMode && GameManager.Instance.BlitzTimeRemaining > 0f)
            {
                customerController?.DismissCustomer();
                SpawnNextInQueue();
                return;
            }

            CheckRemainingCustomers();
        }

        public CustomerController CustomerController => customerController;

        private Coroutine mentorArrivalRoutine;

        public void CheckRemainingCustomers()
        {
            // Only check end of day after the current customer has completely departed
            bool customerStillPresent = customerController != null && customerController.IsPresent;

            if (dailyCustomerQueue.Count == 0 && !customerStillPresent)
            {
                int currentDay = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
                bool isRentDay = (currentDay % 7 == 0);

                if (currentDay == 2 && MentorController.Instance != null && !MentorController.Instance.HasCompletedDay2Briefing && customerController != null)
                {
                    if (mentorArrivalRoutine != null) StopCoroutine(mentorArrivalRoutine);
                    mentorArrivalRoutine = StartCoroutine(DelayedDay2MentorArrivalRoutine(0.6f));
                }
                else if (currentDay == 5 && MentorController.Instance != null && !MentorController.Instance.HasCompletedDay5Briefing && customerController != null)
                {
                    if (mentorArrivalRoutine != null) StopCoroutine(mentorArrivalRoutine);
                    mentorArrivalRoutine = StartCoroutine(DelayedDay5MentorArrivalRoutine(0.6f));
                }
                else if (currentDay == 8 && MentorController.Instance != null && !MentorController.Instance.HasCompletedDay8Briefing && customerController != null)
                {
                    if (mentorArrivalRoutine != null) StopCoroutine(mentorArrivalRoutine);
                    mentorArrivalRoutine = StartCoroutine(DelayedDay8MentorArrivalRoutine(0.6f));
                }
                else if (currentDay == 11 && MentorController.Instance != null && !MentorController.Instance.HasCompletedDay11Briefing && customerController != null)
                {
                    if (mentorArrivalRoutine != null) StopCoroutine(mentorArrivalRoutine);
                    mentorArrivalRoutine = StartCoroutine(DelayedDay11MentorArrivalRoutine(0.6f));
                }
                else if (currentDay == 18 && MentorController.Instance != null && !MentorController.Instance.HasCompletedDay18Briefing && customerController != null)
                {
                    if (mentorArrivalRoutine != null) StopCoroutine(mentorArrivalRoutine);
                    mentorArrivalRoutine = StartCoroutine(DelayedDay18MentorArrivalRoutine(0.6f));
                }
                else if (isRentDay && !rentEncounterTriggeredToday && customerController != null)
                {
                    rentEncounterTriggeredToday = true;
                    if (rentArrivalRoutine != null) StopCoroutine(rentArrivalRoutine);
                    rentArrivalRoutine = StartCoroutine(DelayedRentArrivalRoutine(0.6f, currentDay));
                }
                else if (!isRentDay || rentEncounterTriggeredToday)
                {
                    GameManager.Instance?.SetState(GameState.ShopClosing);
                    OnAllDailyCustomersFinished?.Invoke();
                }
            }
            else if (!HasCustomerAtWindow && GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.ShopClosing && (customerController == null || (!customerController.IsLandlordActive && !customerController.IsMentorActive)))
            {
                GameManager.Instance?.SetState(GameState.ShopOpen);
            }
        }

        private IEnumerator DelayedDay2MentorArrivalRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            MentorController.Instance?.TriggerDay2NightBriefing(customerController, () =>
            {
                GameManager.Instance?.SetState(GameState.ShopClosing);
                OnAllDailyCustomersFinished?.Invoke();
            });
        }

        private IEnumerator DelayedDay5MentorArrivalRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            MentorController.Instance?.TriggerDay5NightBriefing(customerController, () =>
            {
                GameManager.Instance?.SetState(GameState.ShopClosing);
                OnAllDailyCustomersFinished?.Invoke();
            });
        }

        private IEnumerator DelayedDay8MentorArrivalRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            MentorController.Instance?.TriggerDay8NightBriefing(customerController, () =>
            {
                GameManager.Instance?.SetState(GameState.ShopClosing);
                OnAllDailyCustomersFinished?.Invoke();
            });
        }

        private IEnumerator DelayedDay11MentorArrivalRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            MentorController.Instance?.TriggerDay11NightBriefing(customerController, () =>
            {
                GameManager.Instance?.SetState(GameState.ShopClosing);
                OnAllDailyCustomersFinished?.Invoke();
            });
        }

        private IEnumerator DelayedDay18MentorArrivalRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            MentorController.Instance?.TriggerDay18NightBriefing(customerController, () =>
            {
                GameManager.Instance?.SetState(GameState.ShopClosing);
                OnAllDailyCustomersFinished?.Invoke();
            });
        }

        private IEnumerator DelayedRentArrivalRoutine(float delay, int dayNumber)
        {
            yield return new WaitForSeconds(delay);
            customerController?.SpawnLandlord(dayNumber, () =>
            {
                GameManager.Instance?.SetState(GameState.ShopClosing);
                OnAllDailyCustomersFinished?.Invoke();
            });
        }

        public float GetPatienceForArchetype(CustomerArchetype archetype)
        {
            return archetype switch
            {
                CustomerArchetype.Adhd => adhdPatience,
                CustomerArchetype.Autism => autismPatience,
                CustomerArchetype.Anxiety => anxietyPatience,
                CustomerArchetype.Tourettes => tourettesPatience,
                CustomerArchetype.Dyscalculia => dyscalculiaPatience,
                CustomerArchetype.Dyslexia => dyslexiaPatience,
                _ => 40f
            };
        }

        private int GetArtisanalToppingWeight(ToppingType topping)
        {
            return topping switch
            {
                ToppingType.TapiocaPearls => 1,
                ToppingType.PoppingBoba => 2,
                ToppingType.GrassJelly => 3,
                ToppingType.CoconutJelly => 4,
                ToppingType.EggPudding => 5,
                ToppingType.CheeseFoam => 6,
                ToppingType.GoldenHoneyPearls => 7,
                _ => 1
            };
        }

        private int GetArtisanalMilkWeight(MilkType milk)
        {
            return milk switch
            {
                MilkType.FreshMilk => 1,
                MilkType.OatMilk => 2,
                MilkType.CondensedMilk => 2,
                MilkType.CoconutMilk => 3,
                _ => 1
            };
        }

        private DrinkOrder GenerateRandomOrder()
        {
            var order = new DrinkOrder();
            CustomerArchetype archetype = (CustomerArchetype)UnityEngine.Random.Range(0, 6);
            order.archetype = archetype;

            int currentDay = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
            bool isBlitz = GameManager.Instance != null && GameManager.Instance.IsBlitzMode;
            if (isBlitz)
            {
                currentDay = 22; // Always follow Week 4 gourmet order distribution
            }

            // 1. Day Progression: Day 1 & Day 2 Funnel vs Progressive Day 3+ Difficulty
            if (currentDay == 1)
            {
                // Day 1: Any type of tea, optional fresh milk, with or without Tapioca Pearls, and polarized 0% or 100% sugar and ice
                List<TeaBase> day1Teas = new List<TeaBase> { TeaBase.BlackTea, TeaBase.GreenTea, TeaBase.OolongTea, TeaBase.ThaiTea, TeaBase.TaroTea };
                order.targetTea = day1Teas[UnityEngine.Random.Range(0, day1Teas.Count)];
                order.targetMilk = (UnityEngine.Random.value > 0.4f) ? MilkType.FreshMilk : MilkType.None;
                order.targetSweetnessPercent = (UnityEngine.Random.value > 0.5f) ? 100 : 0;
                order.targetIcePercent = (UnityEngine.Random.value > 0.5f) ? 100 : 0;
                order.targetToppings = (UnityEngine.Random.value > 0.4f)
                    ? new List<ToppingType> { ToppingType.TapiocaPearls }
                    : new List<ToppingType>();
            }
            else if (currentDay == 2)
            {
                // Day 2: Introduces subtle sugar (25%, 50%, 75%) and ice (50%) customizations with tea, fresh milk, and tapioca
                List<TeaBase> day2Teas = new List<TeaBase> { TeaBase.BlackTea, TeaBase.GreenTea, TeaBase.OolongTea, TeaBase.ThaiTea, TeaBase.TaroTea };
                order.targetTea = day2Teas[UnityEngine.Random.Range(0, day2Teas.Count)];
                order.targetMilk = (UnityEngine.Random.value > 0.4f) ? MilkType.FreshMilk : MilkType.None;
                List<int> day2Sweetness = new List<int> { 0, 25, 50, 75, 100 };
                List<int> day2Ice = new List<int> { 0, 50, 100 };
                order.targetSweetnessPercent = day2Sweetness[UnityEngine.Random.Range(0, day2Sweetness.Count)];
                order.targetIcePercent = day2Ice[UnityEngine.Random.Range(0, day2Ice.Count)];
                order.targetToppings = (UnityEngine.Random.value > 0.4f)
                    ? new List<ToppingType> { ToppingType.TapiocaPearls }
                    : new List<ToppingType>();
            }
            else
            {
                // Day 3+: Progressive ingredient availability based on progression
                List<TeaBase> availableTeas = new List<TeaBase> { TeaBase.BlackTea, TeaBase.GreenTea, TeaBase.OolongTea, TeaBase.ThaiTea, TeaBase.TaroTea };
                List<MilkType> availableMilks = new List<MilkType> { MilkType.None, MilkType.FreshMilk, MilkType.OatMilk };
                List<int> availableSweetness = new List<int> { 0, 25, 50, 75, 100 };
                List<int> availableIce = new List<int> { 0, 50, 100 };
                List<ToppingType> availableToppings = new List<ToppingType> { ToppingType.TapiocaPearls, ToppingType.PoppingBoba, ToppingType.GrassJelly };
                int maxToppings = 1;

                bool hasArtisanalMenu = UpgradeManager.Instance != null && UpgradeManager.Instance.HasUpgrade(UpgradeType.ArtisanalTeaMenu);

                if (currentDay >= 8 || hasArtisanalMenu) // Week 2+ or Artisanal Menu
                {
                    if (!availableMilks.Contains(MilkType.CoconutMilk)) availableMilks.Add(MilkType.CoconutMilk);
                    if (!availableToppings.Contains(ToppingType.EggPudding)) availableToppings.Add(ToppingType.EggPudding);
                    if (!availableToppings.Contains(ToppingType.CoconutJelly)) availableToppings.Add(ToppingType.CoconutJelly);
                    maxToppings = 2;
                }

                if (currentDay >= 15 || hasArtisanalMenu) // Week 3+ (Rare & Foraged)
                {
                    if (!availableMilks.Contains(MilkType.CondensedMilk)) availableMilks.Add(MilkType.CondensedMilk);
                    if (!availableToppings.Contains(ToppingType.CheeseFoam)) availableToppings.Add(ToppingType.CheeseFoam);
                    if (!availableToppings.Contains(ToppingType.GoldenHoneyPearls)) availableToppings.Add(ToppingType.GoldenHoneyPearls);
                }

                // Randomly construct the drink from available ingredients
                order.targetTea = availableTeas[UnityEngine.Random.Range(0, availableTeas.Count)];

                // Milk Selection (Artisanal Menu gives higher weighting to premium milks)
                float milkChance = hasArtisanalMenu ? 0.85f : 0.65f;
                if (UnityEngine.Random.value < milkChance)
                {
                    List<MilkType> milkPool = new List<MilkType>(availableMilks);
                    milkPool.Remove(MilkType.None);
                    if (hasArtisanalMenu)
                    {
                        List<MilkType> weightedMilks = new List<MilkType>();
                        foreach (var m in milkPool)
                        {
                            int w = GetArtisanalMilkWeight(m);
                            for (int k = 0; k < w; k++) weightedMilks.Add(m);
                        }
                        milkPool = weightedMilks;
                    }
                    order.targetMilk = milkPool[UnityEngine.Random.Range(0, milkPool.Count)];
                }
                else
                {
                    order.targetMilk = MilkType.None;
                }

                order.targetSweetnessPercent = availableSweetness[UnityEngine.Random.Range(0, availableSweetness.Count)];
                order.targetIcePercent = availableIce[UnityEngine.Random.Range(0, availableIce.Count)];

                // Toppings: Base chance scales with week (65% in W1/W2, 70% in W3, 80% in W4; 85-90% with Artisanal Menu)
                float toppingChance = hasArtisanalMenu ? (currentDay >= 22 ? 0.90f : 0.85f) : (currentDay >= 22 ? 0.80f : (currentDay >= 15 ? 0.70f : 0.65f));
                if (UnityEngine.Random.value < toppingChance && availableToppings.Count > 0)
                {
                    bool isWeek4 = (currentDay >= 22);

                    if (isWeek4)
                    {
                        // Week 4: Up to 2 bottom toppings PLUS optional Cheese Foam cap (up to 3 toppings total)
                        List<ToppingType> bottomPool = new List<ToppingType>(availableToppings);
                        bottomPool.Remove(ToppingType.CheeseFoam);

                        // Roll 1 or 2 bottom toppings (40% for 1, 60% for 2 without Artisanal; 30% for 1, 70% for 2 with Artisanal)
                        float rollTwoBottomChance = hasArtisanalMenu ? 0.70f : 0.60f;
                        int bottomCount = (UnityEngine.Random.value < rollTwoBottomChance) ? 2 : 1;

                        List<ToppingType> weightedBottomPool = new List<ToppingType>();
                        foreach (var t in bottomPool)
                        {
                            int w = hasArtisanalMenu ? GetArtisanalToppingWeight(t) : 1;
                            for (int k = 0; k < w; k++) weightedBottomPool.Add(t);
                        }

                        for (int i = 0; i < bottomCount && weightedBottomPool.Count > 0; i++)
                        {
                            int randIdx = UnityEngine.Random.Range(0, weightedBottomPool.Count);
                            ToppingType selected = weightedBottomPool[randIdx];
                            order.targetToppings.Add(selected);
                            weightedBottomPool.RemoveAll(x => x == selected);
                        }

                        // Dedicated roll for Cheese Foam on top (50% base, 70% with Artisanal Menu)
                        float cheeseFoamChance = hasArtisanalMenu ? 0.70f : 0.50f;
                        if (UnityEngine.Random.value < cheeseFoamChance)
                        {
                            order.targetToppings.Add(ToppingType.CheeseFoam);
                        }
                    }
                    else
                    {
                        // Weeks 1 - 3: standard 1 to maxToppings
                        int toppingsCount = 1;
                        if (maxToppings > 1)
                        {
                            float rollTwoChance = hasArtisanalMenu ? 0.70f : 0.50f;
                            toppingsCount = (UnityEngine.Random.value < rollTwoChance) ? 2 : 1;
                        }

                        List<ToppingType> weightedPool = new List<ToppingType>();
                        foreach (var t in availableToppings)
                        {
                            int w = hasArtisanalMenu ? GetArtisanalToppingWeight(t) : 1;
                            for (int k = 0; k < w; k++) weightedPool.Add(t);
                        }

                        for (int i = 0; i < toppingsCount && weightedPool.Count > 0; i++)
                        {
                            int randIdx = UnityEngine.Random.Range(0, weightedPool.Count);
                            ToppingType selected = weightedPool[randIdx];
                            order.targetToppings.Add(selected);
                            weightedPool.RemoveAll(x => x == selected);
                        }
                    }
                }
            }

            // Base price scales dynamically with ingredient cost + 15% profit markup
            if (MarketPriceManager.Instance != null)
            {
                order.basePrice = MarketPriceManager.Instance.CalculateDrinkSellPrice(order);
            }
            else
            {
                order.basePrice = 5.00f + (order.targetMilk != MilkType.None ? 0.75f : 0f) + (order.targetToppings.Count * 0.75f);
            }

            // 3. Generate personality-rich dialogue referencing their exact order
            string teaName = order.GetFormattedTea();
            string milkName = order.GetFormattedMilk();
            string milkDesc = !string.IsNullOrEmpty(milkName) ? $" with {milkName}" : "";
            string toppingDesc = order.GetFormattedToppings();

            switch (archetype)
            {
                case CustomerArchetype.Adhd:
                    order.customerName = "ADHD Creature";
                    order.dialogueText = $"Quick, quick! Can I get a {teaName}{milkDesc} with {toppingDesc}? {order.targetSweetnessPercent}% sugar, {order.targetIcePercent}% ice, thanks!";
                    break;

                case CustomerArchetype.Autism:
                    order.customerName = "Autism Creature";
                    order.dialogueText = $"Hello. I would like a {teaName}{milkDesc} with {toppingDesc}. Exactly {order.targetSweetnessPercent}% sweetness and {order.targetIcePercent}% ice, please.";
                    break;

                case CustomerArchetype.Anxiety:
                    order.customerName = "Anxiety Creature";
                    order.dialogueText = $"U-um... hello! Could I please have a {teaName}{milkDesc} with {toppingDesc}? {order.targetSweetnessPercent}% sugar and {order.targetIcePercent}% ice if that's okay...";
                    break;

                case CustomerArchetype.Tourettes:
                    order.customerName = "Tourettes Creature";
                    order.dialogueText = $"GIVE ME A {teaName.ToUpper()}{milkDesc.ToUpper()} WITH {toppingDesc.ToUpper()}!! {order.targetSweetnessPercent}% SWEET, {order.targetIcePercent}% ICE, LET'S GO!";
                    break;

                case CustomerArchetype.Dyscalculia:
                    order.customerName = "Dyscalculia Creature";
                    order.dialogueText = $"I counted my coins! I want a {teaName}{milkDesc} with {toppingDesc}! {order.targetSweetnessPercent}% sweetness and {order.targetIcePercent}% ice, please!";
                    break;

                case CustomerArchetype.Dyslexia:
                    order.customerName = "Dyslexia Creature";
                    order.dialogueText = $"Hi! I finally read the menu! Can I get a {teaName}{milkDesc} with {toppingDesc}? {order.targetSweetnessPercent}% sugar, {order.targetIcePercent}% ice please!";
                    break;
            }

            return order;
        }
    }
}
