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
        }

        public void GenerateDailyQueue(int dayNumber)
        {
            dailyCustomerQueue.Clear();
            rentEncounterTriggeredToday = false;
            int count = DayManager.Instance.TotalCustomersToday;

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
                HUDController.Instance?.ShowNotification("The Landlord is waiting! Settle your rent first.");
                return false;
            }

            if (customerController != null && customerController.IsPresent)
            {
                if (customerController.IsWaitingDrink)
                {
                    // Customer was waiting and unserved -> trigger angry skip reaction, then spawn next
                    customerController.ForceSkipCustomer(() => SpawnNextInQueue());
                    return true;
                }
                else
                {
                    // Customer was already in departure animation -> dismiss immediately and bring next
                    customerController.DismissCustomer();
                }
            }

            return SpawnNextInQueue();
        }

        private bool SpawnNextInQueue()
        {
            if (dailyCustomerQueue.Count == 0)
            {
                Debug.Log("No more customers in line today!");
                CheckRemainingCustomers();
                return false;
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
            if (HasCustomerAtWindow)
            {
                customerController.ReceiveDrink(cup);
                // If more customers remain in queue, allow ringing bell while current customer leaves
                if (dailyCustomerQueue.Count > 0)
                {
                    GameManager.Instance?.SetState(GameState.ShopOpen);
                }
            }
        }

        private void HandleCustomerFinished(CustomerController customer, EvaluationResult result)
        {
            CheckRemainingCustomers();
        }

        private void HandleCustomerFinishedAngry(CustomerController customer)
        {
            CheckRemainingCustomers();
        }

        public CustomerController CustomerController => customerController;

        public void CheckRemainingCustomers()
        {
            // Only check end of day after the current customer has completely departed
            bool customerStillPresent = customerController != null && customerController.IsPresent;

            if (dailyCustomerQueue.Count == 0 && !customerStillPresent)
            {
                int currentDay = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
                bool isRentDay = (currentDay % 7 == 0);

                if (isRentDay && !rentEncounterTriggeredToday && customerController != null)
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
            else if (!HasCustomerAtWindow && GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.ShopClosing && (customerController == null || !customerController.IsLandlordActive))
            {
                GameManager.Instance?.SetState(GameState.ShopOpen);
            }
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

        private DrinkOrder GenerateRandomOrder()
        {
            var order = new DrinkOrder();
            CustomerArchetype archetype = (CustomerArchetype)UnityEngine.Random.Range(0, 6);
            order.archetype = archetype;

            int currentDay = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;

            // 1. Day 1 Dopamine Funnel vs Progressive Day 2+ Difficulty
            if (currentDay == 1)
            {
                // Day 1: Simple, highly satisfying classic milk teas with Tapioca Pearls and polarized 0%/100% options
                order.targetTea = (UnityEngine.Random.value > 0.5f) ? TeaBase.BlackTea : TeaBase.GreenTea;
                order.targetMilk = MilkType.FreshMilk;
                order.targetSweetnessPercent = (UnityEngine.Random.value > 0.5f) ? 100 : 0;
                order.targetIcePercent = (UnityEngine.Random.value > 0.5f) ? 100 : 0;
                order.targetToppings = new List<ToppingType> { ToppingType.TapiocaPearls };
            }
            else
            {
                // Day 2+: Progressive ingredient availability based on progression
                List<TeaBase> availableTeas = new List<TeaBase> { TeaBase.BlackTea, TeaBase.GreenTea };
                List<MilkType> availableMilks = new List<MilkType> { MilkType.None, MilkType.FreshMilk };
                List<int> availableSweetness = new List<int> { 0, 50, 100 };
                List<int> availableIce = new List<int> { 0, 50, 100 };
                List<ToppingType> availableToppings = new List<ToppingType> { ToppingType.TapiocaPearls, ToppingType.PoppingBoba, ToppingType.GrassJelly };
                int maxToppings = 1;

                if (currentDay >= 4) // Mid-Week 1
                {
                    availableTeas.Add(TeaBase.OolongTea);
                    availableMilks.Add(MilkType.OatMilk);
                    availableSweetness = new List<int> { 0, 25, 50, 75, 100 };
                    availableToppings.Add(ToppingType.CoconutJelly);
                }

                if (currentDay >= 8) // Week 2+
                {
                    availableTeas.Add(TeaBase.ThaiTea);
                    availableTeas.Add(TeaBase.TaroTea);
                    availableMilks.Add(MilkType.CondensedMilk);
                    availableToppings.Add(ToppingType.EggPudding);
                    availableToppings.Add(ToppingType.CheeseFoam);
                    maxToppings = 2;
                }

                if (currentDay >= 15) // Week 3+ (Rare & Foraged)
                {
                    availableTeas.Add(TeaBase.WildMountainTea);
                    availableMilks.Add(MilkType.CoconutMilk);
                    availableToppings.Add(ToppingType.GoldenHoneyPearls);
                }

                // Randomly construct the drink from available ingredients
                order.targetTea = availableTeas[UnityEngine.Random.Range(0, availableTeas.Count)];
                order.targetMilk = (UnityEngine.Random.value > 0.35f)
                    ? availableMilks[UnityEngine.Random.Range(1, availableMilks.Count)]
                    : MilkType.None;

                order.targetSweetnessPercent = availableSweetness[UnityEngine.Random.Range(0, availableSweetness.Count)];
                order.targetIcePercent = availableIce[UnityEngine.Random.Range(0, availableIce.Count)];

                // Toppings: 65% chance of topping(s), 35% chance of no toppings
                if (UnityEngine.Random.value < 0.65f && availableToppings.Count > 0)
                {
                    int toppingsCount = UnityEngine.Random.Range(1, maxToppings + 1);
                    List<ToppingType> toppingPool = new List<ToppingType>(availableToppings);

                    for (int i = 0; i < toppingsCount && toppingPool.Count > 0; i++)
                    {
                        int randIdx = UnityEngine.Random.Range(0, toppingPool.Count);
                        order.targetToppings.Add(toppingPool[randIdx]);
                        toppingPool.RemoveAt(randIdx);
                    }
                }
            }

            // Base price scales slightly with complexity
            order.basePrice = 5.00f + (order.targetMilk != MilkType.None ? 0.75f : 0f) + (order.targetToppings.Count * 0.75f);

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
