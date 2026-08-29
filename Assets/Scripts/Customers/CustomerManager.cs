using System;
using System.Collections.Generic;
using UnityEngine;

namespace BubbleTeaShop
{
    public class CustomerManager : MonoBehaviour
    {
        public static CustomerManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private CustomerController customerController;

        private Queue<DrinkOrder> dailyCustomerQueue = new Queue<DrinkOrder>();
        public bool HasCustomerAtWindow => customerController != null && customerController.IsActive;

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
            int count = DayManager.Instance.TotalCustomersToday;

            for (int i = 0; i < count; i++)
            {
                dailyCustomerQueue.Enqueue(GenerateRandomOrder());
            }
            Debug.Log($"[CustomerManager] Generated {count} customers for Day {dayNumber}.");
        }

        public bool TryCallNextCustomer()
        {
            if (customerController != null && customerController.IsPresent)
            {
                if (customerController.IsActive)
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
            customerController.SpawnCustomer(nextOrder, patience);
            OnCustomerArrived?.Invoke(nextOrder);
            return true;
        }

        public void ServeCurrentCustomer(BubbleTeaCup cup)
        {
            if (HasCustomerAtWindow)
            {
                customerController.ReceiveDrink(cup);
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

        private void CheckRemainingCustomers()
        {
            if (dailyCustomerQueue.Count == 0 && !HasCustomerAtWindow)
            {
                GameManager.Instance?.SetState(GameState.ShopClosing);
                OnAllDailyCustomersFinished?.Invoke();
            }
        }

        private float GetPatienceForArchetype(CustomerArchetype archetype)
        {
            return archetype switch
            {
                CustomerArchetype.Adhd => 30f,
                CustomerArchetype.Autism => 45f,
                CustomerArchetype.Anxiety => 55f,
                CustomerArchetype.Tourettes => 35f,
                CustomerArchetype.Dyscalculia => 60f,
                CustomerArchetype.Dyslexia => 50f,
                _ => 40f
            };
        }

        private DrinkOrder GenerateRandomOrder()
        {
            var order = new DrinkOrder();
            CustomerArchetype archetype = (CustomerArchetype)UnityEngine.Random.Range(0, 6);
            order.archetype = archetype;

            int currentDay = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;

            // 1. Determine ingredient availability based on day / week progression
            List<TeaBase> availableTeas = new List<TeaBase> { TeaBase.BlackTea, TeaBase.GreenTea };
            List<MilkType> availableMilks = new List<MilkType> { MilkType.None, MilkType.FreshMilk };
            List<int> availableSweetness = new List<int> { 0, 50, 100 };
            List<int> availableIce = new List<int> { 0, 50, 100 };
            List<ToppingType> availableToppings = new List<ToppingType> { ToppingType.TapiocaPearls };
            int maxToppings = 1;

            if (currentDay >= 4) // Mid-Week 1
            {
                availableTeas.Add(TeaBase.OolongTea);
                availableMilks.Add(MilkType.OatMilk);
                availableSweetness = new List<int> { 0, 25, 50, 75, 100 };
                availableToppings.Add(ToppingType.GrassJelly);
                availableToppings.Add(ToppingType.CoconutJelly);
            }

            if (currentDay >= 8) // Week 2+
            {
                availableTeas.Add(TeaBase.ThaiTea);
                availableTeas.Add(TeaBase.TaroTea);
                availableMilks.Add(MilkType.CondensedMilk);
                availableToppings.Add(ToppingType.PoppingBoba);
                availableToppings.Add(ToppingType.EggPudding);
                availableToppings.Add(ToppingType.CheeseFoam);
                maxToppings = 2;
            }

            if (currentDay >= 15) // Week 3+ (Rare & Foraged)
            {
                availableTeas.Add(TeaBase.MatchaTea);
                availableTeas.Add(TeaBase.WildMountainTea);
                availableMilks.Add(MilkType.CoconutMilk);
                availableToppings.Add(ToppingType.GoldenHoneyPearls);
            }

            // 2. Randomly construct the drink from available ingredients
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

            // Base price scales slightly with complexity
            order.basePrice = 5.00f + (order.targetMilk != MilkType.None ? 0.75f : 0f) + (order.targetToppings.Count * 0.75f);

            // 3. Generate personality-rich dialogue referencing their exact order
            string milkDesc = order.targetMilk != MilkType.None ? $" w/ {order.targetMilk}" : "";
            string toppingDesc = order.targetToppings.Count > 0 ? string.Join(" & ", order.targetToppings) : "no toppings";

            switch (archetype)
            {
                case CustomerArchetype.Adhd:
                    order.customerName = "ADHD Creature";
                    order.dialogueText = $"Quick, quick! Can I get a {order.targetTea}{milkDesc} with {toppingDesc}? {order.targetSweetnessPercent}% sugar, {order.targetIcePercent}% ice, thanks!";
                    break;

                case CustomerArchetype.Autism:
                    order.customerName = "Autism Creature";
                    order.dialogueText = $"Hello. I would like a {order.targetTea}{milkDesc} with {toppingDesc}. Exactly {order.targetSweetnessPercent}% sweetness and {order.targetIcePercent}% ice, please.";
                    break;

                case CustomerArchetype.Anxiety:
                    order.customerName = "Anxiety Creature";
                    order.dialogueText = $"U-um... hello! Could I please have a {order.targetTea}{milkDesc} with {toppingDesc}? {order.targetSweetnessPercent}% sugar and {order.targetIcePercent}% ice if that's okay...";
                    break;

                case CustomerArchetype.Tourettes:
                    order.customerName = "Tourettes Creature";
                    order.dialogueText = $"GIVE ME A {order.targetTea}{milkDesc} WITH {toppingDesc}!! {order.targetSweetnessPercent}% SWEET, {order.targetIcePercent}% ICE, LET'S GO!";
                    break;

                case CustomerArchetype.Dyscalculia:
                    order.customerName = "Dyscalculia Creature";
                    order.dialogueText = $"I counted my coins! I want a {order.targetTea}{milkDesc} with {toppingDesc}! {order.targetSweetnessPercent}% sweetness and {order.targetIcePercent}% ice, please!";
                    break;

                case CustomerArchetype.Dyslexia:
                    order.customerName = "Dyslexia Creature";
                    order.dialogueText = $"Hi! I finally read the menu! Can I get a {order.targetTea}{milkDesc} with {toppingDesc}? {order.targetSweetnessPercent}% sugar, {order.targetIcePercent}% ice please!";
                    break;
            }

            return order;
        }
    }
}
