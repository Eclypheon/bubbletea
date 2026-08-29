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
                    // Customer was waiting and unserved -> force skip them with 0 sales/tips
                    customerController.ForceSkipCustomer();
                }
                else
                {
                    // Customer was already served and in departure animation -> dismiss immediately
                    customerController.DismissCustomer();
                }
            }

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

            switch (archetype)
            {
                case CustomerArchetype.Adhd:
                    order.customerName = "ADHD Creature";
                    order.targetTea = UnityEngine.Random.value > 0.5f ? TeaBase.BlackTea : TeaBase.GreenTea;
                    order.targetMilk = MilkType.FreshMilk;
                    order.targetSweetnessPercent = 50;
                    order.targetIcePercent = 50;
                    order.targetToppings.Add(ToppingType.TapiocaPearls);
                    order.dialogueText = "Need a quick pick-me-up before my 2 PM meeting! Classic milk tea, please!";
                    break;

                case CustomerArchetype.Autism:
                    order.customerName = "Autism Creature";
                    order.targetTea = UnityEngine.Random.value > 0.5f ? TeaBase.TaroTea : TeaBase.MatchaTea;
                    order.targetMilk = MilkType.OatMilk;
                    order.targetSweetnessPercent = 75;
                    order.targetIcePercent = 50;
                    order.targetToppings.Add(ToppingType.PoppingBoba);
                    order.targetToppings.Add(ToppingType.CoconutJelly);
                    order.dialogueText = "Hi! Can I get something super cute for my story? Taro or Matcha with popping boba!";
                    break;

                case CustomerArchetype.Anxiety:
                    order.customerName = "Anxiety Creature";
                    order.targetTea = TeaBase.OolongTea;
                    order.targetMilk = MilkType.None;
                    order.targetSweetnessPercent = 25;
                    order.targetIcePercent = 0;
                    order.targetToppings.Add(ToppingType.GrassJelly);
                    order.dialogueText = "Greetings. Brew me a pure Oolong with grass jelly, minimal sugar. Keep it authentic.";
                    break;

                case CustomerArchetype.Tourettes:
                    order.customerName = "Tourettes Creature";
                    order.targetTea = TeaBase.ThaiTea;
                    order.targetMilk = MilkType.CondensedMilk;
                    order.targetSweetnessPercent = 100;
                    order.targetIcePercent = 50;
                    order.targetToppings.Add(ToppingType.TapiocaPearls);
                    order.targetToppings.Add(ToppingType.EggPudding);
                    order.dialogueText = "Make it super sweet and loaded with extra boba and egg pudding!!";
                    break;

                case CustomerArchetype.Dyscalculia:
                    order.customerName = "Dyscalculia Creature";
                    order.targetTea = UnityEngine.Random.value > 0.5f ? TeaBase.WildMountainTea : TeaBase.MatchaTea;
                    order.targetMilk = MilkType.CoconutMilk;
                    order.targetSweetnessPercent = 50;
                    order.targetIcePercent = 30;
                    order.targetToppings.Add(ToppingType.GoldenHoneyPearls);
                    order.dialogueText = "I travel with the wind... give me something imbued with the serenity of wild mountains.";
                    break;
                
                case CustomerArchetype.Dyslexia:
                    order.customerName = "Dyslexia Creature";
                    order.targetTea = UnityEngine.Random.value > 0.5f ? TeaBase.WildMountainTea : TeaBase.MatchaTea;
                    order.targetMilk = MilkType.CoconutMilk;
                    order.targetSweetnessPercent = 50;
                    order.targetIcePercent = 30;
                    order.targetToppings.Add(ToppingType.GoldenHoneyPearls);
                    order.dialogueText = "I travel with the wind... give me something imbued with the serenity of wild mountains.";
                    break;
            }

            return order;
        }
    }
}
