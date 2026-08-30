using System;
using System.Collections;
using UnityEngine;

namespace BubbleTeaShop
{
    public class MentorController : MonoBehaviour
    {
        public static MentorController Instance { get; private set; }

        [Header("Mentor Settings")]
        [SerializeField] private Sprite mentorSprite;
        [SerializeField] private bool hasCompletedDay1Briefing = false;
        [SerializeField] private bool hasCompletedDay2Briefing = false;

        public bool HasCompletedDay1Briefing => hasCompletedDay1Briefing;
        public bool HasCompletedDay2Briefing => hasCompletedDay2Briefing;

        public event Action OnDay1BriefingCompleted;
        public event Action OnDay2BriefingCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void TriggerDay1MorningBriefing(CustomerController customerController, Action onFinished = null)
        {
            if (hasCompletedDay1Briefing)
            {
                onFinished?.Invoke();
                return;
            }

            hasCompletedDay1Briefing = true;

            // Grant starter stock: 15 Tapioca Pearls, 15 Fresh Milk, daily sugar & ice
            InventoryManager.Instance?.SetupDay1StarterStock();

            string[] briefingLines = new string[]
            {
                "Glad to see you starting up your own bubble tea store here!",
                "Your ultimate goal is to buy out this store location for $5,000 to win the game.",
                "Be careful with expenses! If you miss two weekly rent payments to the landlord, you will lose the shop.",
                "Here are some basic ingredients to get you started: 15 servings of Tapioca Pearls and Fresh Milk.",
                "Your daily supply of sugar, ice, cups, straws, and tea blends costs $10 to automatically restock each day.",
                "You can inspect your stock anytime by checking the Cash Register on the counter.",
                "Ring the desk bell whenever you're ready to serve your first customer!"
            };

            if (customerController != null)
            {
                customerController.SpawnMentorSequence(
                    briefingLines,
                    3.5f,
                    mentorSprite,
                    () =>
                    {
                        OnDay1BriefingCompleted?.Invoke();
                        onFinished?.Invoke();
                    }
                );
            }
            else
            {
                OnDay1BriefingCompleted?.Invoke();
                onFinished?.Invoke();
            }
        }

        public void TriggerDay2NightBriefing(CustomerController customerController, Action onFinished = null)
        {
            if (hasCompletedDay2Briefing)
            {
                onFinished?.Invoke();
                return;
            }

            hasCompletedDay2Briefing = true;

            // Unlock Premium Milk Dispenser and grant 1 sample of Oat, Coconut, Condensed milk
            InventoryManager.Instance?.UnlockPremiumMilkDispenser();

            string[] briefingLines = new string[]
            {
                "Great job surviving your first two days of business!",
                "From tonight onwards, you can head to the Wholesale Market tab to purchase fresh ingredients and toppings.",
                "I'm also passing you this Premium Milk Dispenser unit! It lets you dispense Oat Milk, Coconut Milk, and Condensed Milk.",
                "I've loaded it with 1 starter serving of each milk. Close the shutter whenever you're ready to head to the market!"
            };

            if (customerController != null)
            {
                customerController.SpawnMentorSequence(
                    briefingLines,
                    3.5f,
                    mentorSprite,
                    () =>
                    {
                        OnDay2BriefingCompleted?.Invoke();
                        onFinished?.Invoke();
                    }
                );
            }
            else
            {
                OnDay2BriefingCompleted?.Invoke();
                onFinished?.Invoke();
            }
        }

        public void TriggerMarketEventBriefing(CustomerController customerController, MarketEvent marketEvent, Action onFinished = null)
        {
            if (marketEvent == null || customerController == null)
            {
                onFinished?.Invoke();
                return;
            }

            string[] briefingLines = new string[]
            {
                $"Good morning! There's breaking market news today: {marketEvent.title}!",
                marketEvent.description,
                "This market condition will last for the next 3 days, so plan your stock and preparations accordingly!",
                "Ring the desk bell whenever you're ready to serve your first customer!"
            };

            customerController.SpawnMentorSequence(
                briefingLines,
                3.5f,
                mentorSprite,
                () =>
                {
                    onFinished?.Invoke();
                }
            );
        }
    }
}
