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
        [SerializeField] private bool hasCompletedDay5Briefing = false;
        [SerializeField] private bool hasCompletedDay8Briefing = false;
        [SerializeField] private bool hasCompletedDay11Briefing = false;
        [SerializeField] private bool hasCompletedDay18Briefing = false;

        public bool HasCompletedDay1Briefing => hasCompletedDay1Briefing;
        public bool HasCompletedDay2Briefing => hasCompletedDay2Briefing;
        public bool HasCompletedDay5Briefing => hasCompletedDay5Briefing;
        public bool HasCompletedDay8Briefing => hasCompletedDay8Briefing;
        public bool HasCompletedDay11Briefing => hasCompletedDay11Briefing;
        public bool HasCompletedDay18Briefing => hasCompletedDay18Briefing;

        public event Action OnDay1BriefingCompleted;
        public event Action OnDay2BriefingCompleted;
        public event Action OnDay5BriefingCompleted;
        public event Action OnDay8BriefingCompleted;
        public event Action OnDay11BriefingCompleted;
        public event Action OnDay18BriefingCompleted;

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
                "Thanks for bringing <i>Yippee Tea</i> to <i>L-PAX</i>",
                "Rent here is pretty steep but you can buy out this entire store location for $1,500!",
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
                "From tonight onwards, you can head to the Wholesale Market to purchase fresh ingredients and toppings.",
                "Keep in mind: embarking on night activities like the market means staying up late and opening late tomorrow, costing you 1 customer the next day!",
                "You can only do 1 night activity per night, so choose wisely.",
                "I'm also passing you this Premium Milk Dispenser unit! It lets you dispense Oat Milk, Coconut Milk, and Condensed Milk.",
                "I've loaded it with 1 starter serving of each milk. Close the shutter whenever you're ready to start the night phase!"
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

        public void TriggerDay5NightBriefing(CustomerController customerController, Action onFinished = null)
        {
            if (hasCompletedDay5Briefing)
            {
                onFinished?.Invoke();
                return;
            }

            hasCompletedDay5Briefing = true;

            string[] briefingLines = new string[]
            {
                "You've made remarkable progress managing the store through your first 5 days!",
                "Starting tonight, you can embark on Foraging Expeditions to find rare wild ingredients and toppings.",
                "I'm also passing you a Blender, Sieve, and a Bucket so you can process the raw ingredients found while foraging in the Kitchen Prep Area!",
                "Remember: foraging is also a late-night activity that will cause you to open late and lose 1 customer tomorrow.",
                "Also, you can only do ONE night activity per night: either visit the Market OR go Foraging, but never both in the same night.",
                "Close the shutter whenever you're ready to head into the night!"
            };

            if (customerController != null)
            {
                customerController.SpawnMentorSequence(
                    briefingLines,
                    3.5f,
                    mentorSprite,
                    () =>
                    {
                        OnDay5BriefingCompleted?.Invoke();
                        onFinished?.Invoke();
                    }
                );
            }
            else
            {
                OnDay5BriefingCompleted?.Invoke();
                onFinished?.Invoke();
            }
        }

        public void TriggerDay8NightBriefing(CustomerController customerController, Action onFinished = null)
        {
            if (hasCompletedDay8Briefing)
            {
                onFinished?.Invoke();
                return;
            }

            hasCompletedDay8Briefing = true;

            string[] briefingLines = new string[]
            {
                "Ahh I see you've met our landlady Chubi, quite a character isn't she?",
                "...Anyways, if you've made enough cash, you can actually start to purchase some upgrades for your shop!",
                "You can inspect available upgrades to attract more customers, enhance customer patience, or boost your earnings.",
                "Keep up the great work, I'm sure you'll be able to buy over this shop some day!",
                "Close the shutter whenever you're ready to start the night phase!"
            };

            if (customerController != null)
            {
                customerController.SpawnMentorSequence(
                    briefingLines,
                    3.5f,
                    mentorSprite,
                    () =>
                    {
                        OnDay8BriefingCompleted?.Invoke();
                        onFinished?.Invoke();
                    }
                );
            }
            else
            {
                OnDay8BriefingCompleted?.Invoke();
                onFinished?.Invoke();
            }
        }

        public void TriggerDay11NightBriefing(CustomerController customerController, Action onFinished = null)
        {
            if (hasCompletedDay11Briefing)
            {
                onFinished?.Invoke();
                return;
            }

            hasCompletedDay11Briefing = true;

            string[] briefingLines = new string[]
            {
                "You're doing fantastic! Store sales and reputation are climbing steadily.",
                "Tonight, I'm passing you a Chopping Board and Knife for your Kitchen Prep Area!",
                "I found an interesting tree in the nearby Honey Meadows, here's the location, take a look if you have time and you may find something worthwhile!",
                "Prepping a diverse topping selection helps satisfy all your customers' unique orders!",
                "Close the shutter whenever you're ready to head into the night!"
            };

            if (customerController != null)
            {
                customerController.SpawnMentorSequence(
                    briefingLines,
                    3.5f,
                    mentorSprite,
                    () =>
                    {
                        OnDay11BriefingCompleted?.Invoke();
                        onFinished?.Invoke();
                    }
                );
            }
            else
            {
                OnDay11BriefingCompleted?.Invoke();
                onFinished?.Invoke();
            }
        }

        public void TriggerDay18NightBriefing(CustomerController customerController, Action onFinished = null)
        {
            if (hasCompletedDay18Briefing)
            {
                onFinished?.Invoke();
                return;
            }

            hasCompletedDay18Briefing = true;

            string[] briefingLines = new string[]
            {
                "Incredible work! You've really mastered the rhythm of running this bubble tea shop.",
                "Tonight, I'm delivering our highest-grade preparation equipment: a high-speed Centrifuge!",
                "They recently opened up the path to the Misty Mountains again for merchants, there might be some special ingredients you can use to really elevate your tea!",
                "We're getting closer to buying out the shop for good! Close the shutter whenever you're ready to start the night!"
            };

            if (customerController != null)
            {
                customerController.SpawnMentorSequence(
                    briefingLines,
                    3.5f,
                    mentorSprite,
                    () =>
                    {
                        OnDay18BriefingCompleted?.Invoke();
                        onFinished?.Invoke();
                    }
                );
            }
            else
            {
                OnDay18BriefingCompleted?.Invoke();
                onFinished?.Invoke();
            }
        }

        private string GetMarketEventMentorHint(string eventId)
        {
            return eventId switch
            {
                "stock_clearance" => "I hear the wholesale market is running a massive clearance sale right now! All ingredients and milks are 70% cheaper, so you should definitely stock up while prices are low this period!",
                "summer_heatwave" => "Phew, the weather is scorching hot right now! Here's a pro tip: in this intense heatwave, I bet customers would love 100% Full Ice to cool off, even if they didn't ask for it this period!",
                "chilly_rain" => "Brrr, this freezing rainy cold front is brutal! I bet shivering customers would really appreciate 0% No Ice to stay warm this period, even if they forgot to ask for no ice!",
                "golden_harvest" => "The wilderness is flourishing right now! If you go foraging during this period, you'll bag 2.0x double harvests across all expedition zones!",
                "tapioca_delay" => "Harbor shipping delays mean tapioca wholesale prices are up, but customer boba cravings are surging this period! Make sure you have enough pearls ready!",
                "dairy_surplus" => "Local pastures produced a massive surplus of milk! Fresh Milk and Oat Milk are discounted by 30% at the wholesale market, but customers are somewhat saturated and ordering less milk tea this period!",
                "tropical_coconut" => "A massive tropical coconut harvest flooded the market! Coconut Milk and Coconut Jelly are 35% off, but customer coconut orders will be lower this period!",
                "cream_shortage" => "Gourmet cream costs are up, but wealthy customers are tipping an extra +25% on rich drinks with Cheese Foam and Egg Pudding! Take advantage of those generous tips this period!",
                "plant_based_craze" => "A viral wellness article is sweeping town! Expect a huge surge of customers asking for Barista Oat Milk and Organic Coconut Milk this period!",
                "wellness_trend" => "Health and wellness are trending! Customers are favoring refreshing Herbal Grass Jelly and low or zero sweetness levels this period!",
                _ => "Keep a close eye on the market badge on your counter to stay ahead of customer trends this period!"
            };
        }

        public void TriggerMarketEventBriefing(CustomerController customerController, MarketEvent marketEvent, Action onFinished = null)
        {
            if (marketEvent == null || customerController == null)
            {
                onFinished?.Invoke();
                return;
            }

            string title = !string.IsNullOrEmpty(marketEvent.title) ? marketEvent.title : "Special Market Conditions";
            string desc = !string.IsNullOrEmpty(marketEvent.description) ? marketEvent.description : "Special market supply and demand conditions are now active!";
            string mentorHint = GetMarketEventMentorHint(marketEvent.eventId);

            string[] briefingLines = new string[]
            {
                $"Good morning! There's breaking market news today: {title}!",
                desc,
                mentorHint,
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
