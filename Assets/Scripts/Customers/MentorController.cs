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
                "Here are some basic ingredients to get you started: 15 servings of Tapioca Pearls, 15 Fresh Milk, and today's sugar & ice.",
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

        public void TriggerDay2NightBriefing(Action onFinished)
        {
            if (hasCompletedDay2Briefing)
            {
                onFinished?.Invoke();
                return;
            }

            hasCompletedDay2Briefing = true;

            // Unlock Premium Milk Dispenser and grant 1 sample of Oat, Coconut, Condensed milk
            InventoryManager.Instance?.UnlockPremiumMilkDispenser();

            OnDay2BriefingCompleted?.Invoke();
            onFinished?.Invoke();
        }
    }
}
