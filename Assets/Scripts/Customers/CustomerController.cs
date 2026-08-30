using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    public class CustomerController : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] private Image customerImage;
        [SerializeField] private Image patienceFillImage;
        [SerializeField] private SpeechBubbleUI speechBubble;
        [SerializeField] private RectTransform customerRoot;

        [FormerlySerializedAs("officeWorkerSprite")]
        [Header("Archetype Sprites (Assign in Inspector)")]
        [SerializeField] private Sprite adhdSprite;
        [SerializeField] private Sprite autismSprite;
        [SerializeField] private Sprite anxietySprite;
        [SerializeField] private Sprite tourettesSprite;
        [SerializeField] private Sprite dyscalculiaSprite;
        [SerializeField] private Sprite dyslexiaSprite;

        [Header("Landlord & Rent Settings")]
        [SerializeField] private Sprite landlordSprite;
        [SerializeField] private GameObject rentChoicePanel;
        [SerializeField] private Button payRentButton;
        [SerializeField] private TMPro.TextMeshProUGUI payRentButtonText;
        [SerializeField] private Button skipRentButton;
        [SerializeField] private TMPro.TextMeshProUGUI skipRentButtonText;

        [Header("Departure Timers (Seconds)")]
        [Tooltip("How long a customer stays after receiving their drink")]
        [SerializeField] private float servedReactionDuration = 4f;

        [Tooltip("How long an unserved customer speaks their angry line before leaving when skipped by the bell")]
        [SerializeField] private float skippedReactionDuration = 4f;

        [Tooltip("How long an unserved customer speaks their angry line before leaving when their patience runs out")]
        [SerializeField] private float timeoutReactionDuration = 4f;
        
        private DrinkOrder activeOrder;
        private float maxPatience = 45f;
        private float currentPatience = 45f;
        private bool isWaiting = false;
        private bool isLandlordActive = false;
        private Action onLandlordFinished;
        private int currentRentDay;
        private Coroutine leaveRoutine;

        public DrinkOrder ActiveOrder => activeOrder;
        public float PatiencePercent => Mathf.Clamp01(currentPatience / maxPatience);
        public bool IsActive => isWaiting || isLandlordActive;
        public bool IsWaitingDrink => isWaiting;
        public bool IsLandlordActive => isLandlordActive;
        public bool IsPresent => gameObject.activeSelf;

        public event Action<CustomerController, EvaluationResult> OnCustomerServed;
        public event Action<CustomerController> OnCustomerLeftAngry;

        private void Start()
        {
            if (payRentButton != null) payRentButton.onClick.AddListener(HandlePayRent);
            if (skipRentButton != null) skipRentButton.onClick.AddListener(HandleSkipRent);
            if (rentChoicePanel != null) rentChoicePanel.SetActive(false);
            if (payRentButton != null) payRentButton.gameObject.SetActive(false);
            if (skipRentButton != null) skipRentButton.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!isWaiting) return;

            currentPatience -= Time.deltaTime;
            if (patienceFillImage != null)
            {
                patienceFillImage.fillAmount = PatiencePercent;
                // Tint patience bar from Green -> Yellow -> Red
                patienceFillImage.color = Color.Lerp(Color.red, Color.green, PatiencePercent);
            }

            if (currentPatience <= 0f)
            {
                HandleCustomerTimeout();
            }
        }

        public void SpawnCustomer(DrinkOrder order, float patience)
        {
            // Cancel any ongoing departure routine from a previous customer
            if (leaveRoutine != null)
            {
                StopCoroutine(leaveRoutine);
                leaveRoutine = null;
            }

            activeOrder = order;
            maxPatience = patience;
            
            // If Cozy Decor upgrade is active, grant +20% extra patience
            if (UpgradeManager.Instance != null && UpgradeManager.Instance.HasUpgrade(UpgradeType.CozyDecor))
            {
                maxPatience *= 1.20f;
            }

            currentPatience = maxPatience;
            isWaiting = true;

            UpdateCustomerSprite(order.archetype);
            gameObject.SetActive(true);

            if (speechBubble != null)
            {
                speechBubble.ShowOrder(order);
            }

            OrderTicketUI.Instance?.ShowTicket(order);
        }

        private void UpdateCustomerSprite(CustomerArchetype archetype)
        {
            if (customerImage == null) return;

            Sprite s = archetype switch
            {
                CustomerArchetype.Adhd => adhdSprite,
                CustomerArchetype.Autism => autismSprite,
                CustomerArchetype.Anxiety => anxietySprite,
                CustomerArchetype.Tourettes => tourettesSprite,
                CustomerArchetype.Dyscalculia => dyscalculiaSprite,
                CustomerArchetype.Dyslexia => dyslexiaSprite,
                _ => adhdSprite
            };

            if (s != null) customerImage.sprite = s;
        }

        public void ReceiveDrink(BubbleTeaCup cup)
        {
            if (!isWaiting) return;
            isWaiting = false;
            OrderTicketUI.Instance?.HideTicket();

            if (leaveRoutine != null) StopCoroutine(leaveRoutine);

            EvaluationResult evaluation = cup.Evaluate(activeOrder, PatiencePercent);
            EconomyManager.Instance?.AddCash(evaluation.earnedMoney, $"Drink Sale ({activeOrder.archetype})");
            
            if (evaluation.tip > 0)
            {
                EconomyManager.Instance?.AddCash(evaluation.tip, "Customer Tip");
            }

            DayManager.Instance?.RecordCustomerServed(evaluation.earnedMoney, evaluation.tip);

            string reactionLine = GetReactionLine(evaluation.stars);
            if (speechBubble != null)
            {
                speechBubble.ShowReaction(reactionLine, evaluation.stars);
            }

            leaveRoutine = StartCoroutine(LeaveAfterDelay(evaluation, servedReactionDuration));
        }

        public void ForceSkipCustomer(Action onFinished = null)
        {
            if (leaveRoutine != null)
            {
                StopCoroutine(leaveRoutine);
                leaveRoutine = null;
            }

            if (isWaiting)
            {
                isWaiting = false;
                OrderTicketUI.Instance?.HideTicket();
                DayManager.Instance?.RecordCustomerSkipped();
                string angryLine = GetAngrySkipLine(activeOrder != null ? activeOrder.archetype : CustomerArchetype.Adhd);
                if (speechBubble != null)
                {
                    speechBubble.ShowReaction(angryLine, 1);
                }

                leaveRoutine = StartCoroutine(AngrySkipRoutine(onFinished));
            }
            else
            {
                DismissCustomer();
                onFinished?.Invoke();
            }
        }

        private IEnumerator AngrySkipRoutine(Action onFinished)
        {
            yield return new WaitForSeconds(skippedReactionDuration);
            DismissCustomer();
            OnCustomerLeftAngry?.Invoke(this);
            onFinished?.Invoke();
        }

        private string GetAngrySkipLine(CustomerArchetype archetype)
        {
            string[] lines = archetype switch
            {
                CustomerArchetype.Adhd => new string[]
                {
                    "Hey! You're ringing the bell on me?! I was literally about to pay! Never coming back here!",
                    "Wait, did you just skip my turn?! Unbelievable, 1-star review on Yelp!",
                    "I waited in line for 20 minutes for THIS?! I'm taking my business elsewhere!"
                },
                CustomerArchetype.Autism => new string[]
                {
                    "Skipping someone mid-order violates basic queuing etiquette. I will never visit this establishment again.",
                    "This is completely unacceptable service. My routine is ruined forever!",
                    "You didn't even attempt my order. I am boycotting this shop!"
                },
                CustomerArchetype.Anxiety => new string[]
                {
                    "I-is it because I took too long to speak?! I knew this was a mistake... I'm never coming back!",
                    "Oh no... did I do something wrong?! Fine, I'll just leave and never show my face here again!",
                    "I can't believe you just dismissed me like that... my social anxiety was right about this place!"
                },
                CustomerArchetype.Tourettes => new string[]
                {
                    "Ringing the bell in my face?! *tic* Rude! I'm leaving right now!",
                    "You couldn't wait two seconds?! Wow, worst customer service ever!",
                    "I had my money ready! Unbelievable disrespect!"
                },
                CustomerArchetype.Dyscalculia => new string[]
                {
                    "I was just counting my change! Why are you rushing me out the door?!",
                    "Numbers take me a moment to count, you didn't have to be so rude about it!",
                    "Skipped just for trying to pay?! That's terrible!"
                },
                CustomerArchetype.Dyslexia => new string[]
                {
                    "I was still trying to read the menu board! You didn't give me a chance!",
                    "I just needed a few seconds to understand the drink list! Goodbye!",
                    "So impatient... never returning here again!"
                },
                _ => new string[] { "I'm leaving!" }
            };

            return lines[UnityEngine.Random.Range(0, lines.Length)];
        }

        private string GetReactionLine(int stars)
        {
            if (stars >= 5) return "Absolutely sublime! Exactly what I needed!";
            if (stars >= 4) return "Mmm, delicious! Great job!";
            if (stars >= 3) return "Pretty good, thanks!";
            if (stars >= 2) return "Hmm, tastes a bit off from what I ordered...";
            return "Ugh, this isn't what I ordered at all!";
        }

        private void HandleCustomerTimeout()
        {
            isWaiting = false;
            OrderTicketUI.Instance?.HideTicket();
            DayManager.Instance?.RecordCustomerSkipped();
            if (speechBubble != null)
            {
                speechBubble.ShowReaction("Took too long! I'm leaving!", 1);
            }
            CustomerManager.Instance?.CheckRemainingCustomers();

            if (leaveRoutine != null) StopCoroutine(leaveRoutine);
            leaveRoutine = StartCoroutine(LeaveAngryRoutine());
        }

        private IEnumerator LeaveAngryRoutine()
        {
            yield return new WaitForSeconds(timeoutReactionDuration);
            DismissCustomer();
            OnCustomerLeftAngry?.Invoke(this);
        }

        private IEnumerator LeaveAfterDelay(EvaluationResult evaluation, float delay)
        {
            yield return new WaitForSeconds(delay);
            DismissCustomer();
            OnCustomerServed?.Invoke(this, evaluation);
        }

        public void DismissCustomer()
        {
            if (leaveRoutine != null)
            {
                StopCoroutine(leaveRoutine);
                leaveRoutine = null;
            }

            isWaiting = false;
            OrderTicketUI.Instance?.HideTicket();
            if (speechBubble != null) speechBubble.HideBubbleInstant();
            if (rentChoicePanel != null) rentChoicePanel.SetActive(false);
            if (payRentButton != null) payRentButton.gameObject.SetActive(false);
            if (skipRentButton != null) skipRentButton.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        private bool isMentorActive = false;
        public bool IsMentorActive => isMentorActive;
        private Coroutine mentorRoutine;

        public void SpawnMentorSequence(string[] lines, float delayPerLine, Sprite mentorSpriteParam, Action onCompletedSequence = null)
        {
            if (leaveRoutine != null)
            {
                StopCoroutine(leaveRoutine);
                leaveRoutine = null;
            }

            if (mentorRoutine != null)
            {
                StopCoroutine(mentorRoutine);
                mentorRoutine = null;
            }

            isMentorActive = true;
            isWaiting = false;
            activeOrder = null;

            if (patienceFillImage != null) patienceFillImage.gameObject.SetActive(false);

            if (customerImage != null)
            {
                Sprite s = mentorSpriteParam != null ? mentorSpriteParam : dyscalculiaSprite;
                if (s != null) customerImage.sprite = s;
            }

            gameObject.SetActive(true);
            mentorRoutine = StartCoroutine(MentorDialogueSequenceRoutine(lines, delayPerLine, onCompletedSequence));
        }

        private IEnumerator MentorDialogueSequenceRoutine(string[] lines, float delayPerLine, Action onCompletedSequence)
        {
            if (lines == null || lines.Length == 0) yield break;

            for (int i = 0; i < lines.Length; i++)
            {
                if (speechBubble != null)
                {
                    speechBubble.ShowMessage(lines[i]);
                }

                if (i < lines.Length - 1)
                {
                    yield return new WaitForSeconds(delayPerLine);
                }
            }

            HUDController.Instance?.SetStatusHint("Ring the desk bell to call your first customer!");
            onCompletedSequence?.Invoke();
        }

        public void DismissMentor()
        {
            if (mentorRoutine != null)
            {
                StopCoroutine(mentorRoutine);
                mentorRoutine = null;
            }

            isMentorActive = false;
            if (patienceFillImage != null) patienceFillImage.gameObject.SetActive(true);
            DismissCustomer();
            if (GameManager.Instance != null)
            {
                HUDController.Instance?.UpdateStateHint(GameManager.Instance.CurrentState);
            }
        }

        private bool rentDiscountedByDrink = false;
        private float discountedRentAmount = 0f;

        public void SpawnLandlord(int dayNumber, Action onFinished)
        {
            if (leaveRoutine != null)
            {
                StopCoroutine(leaveRoutine);
                leaveRoutine = null;
            }

            isLandlordActive = true;
            isWaiting = false;
            activeOrder = null;
            currentRentDay = dayNumber;
            onLandlordFinished = onFinished;
            rentDiscountedByDrink = false;
            discountedRentAmount = 0f;

            if (patienceFillImage != null) patienceFillImage.gameObject.SetActive(false);

            // Use assigned Landlord sprite, fallback to anxiety/connoisseur
            if (customerImage != null)
            {
                Sprite s = landlordSprite != null ? landlordSprite : anxietySprite;
                if (s != null) customerImage.sprite = s;
            }

            gameObject.SetActive(true);

            float totalRent = EconomyManager.Instance.GetTotalRentDue(dayNumber);
            bool canAfford = EconomyManager.Instance.CanAfford(totalRent);
            bool canSkip = EconomyManager.Instance.CanSkipRent();
            int skipsUsed = EconomyManager.Instance.RentSkipsUsed;

            if (speechBubble != null)
            {
                if (skipsUsed > 0)
                {
                    speechBubble.ShowMessage($"B-Baka! You still owe me last week's rent PLUS this week's (${totalRent:F2})! Pay up right now or you're totally kicked out, hmph!");
                }
                else
                {
                    int week = Mathf.CeilToInt((float)dayNumber / EconomyManager.Instance.RentCycleDays);
                    speechBubble.ShowMessage($"H-hey! Don't look at me like that! Week {week} is over, so hand over the rent (${totalRent:F2}) already! It's not like I came here just to see you or anything...");
                }
            }

            if (rentChoicePanel != null) rentChoicePanel.SetActive(true);

            if (payRentButton != null)
            {
                payRentButton.gameObject.SetActive(true);
                payRentButton.interactable = canAfford;
                if (payRentButtonText != null)
                {
                    payRentButtonText.text = canAfford ? $"Pay Rent (${totalRent:F2})" : $"Can't Afford (${totalRent:F2})";
                }
            }

            if (skipRentButton != null)
            {
                skipRentButton.gameObject.SetActive(true);
                skipRentButton.interactable = true;
                if (skipRentButtonText != null)
                {
                    skipRentButtonText.text = canSkip ? "Ask for Extension (1 left)" : "Can't Pay (Face Eviction)";
                }
            }

            HUDController.Instance?.SetStatusHint("The Landlord has arrived to collect weekly rent!");
        }

        public void ReceiveLandlordDrink(BubbleTeaCup cup)
        {
            if (!isLandlordActive) return;

            // Check favorite recipe: Oolong Tea + Fresh Milk + 100% Sugar + 50% Ice + Tapioca Pearls
            bool isFavorite = (cup.tea == TeaBase.OolongTea &&
                               cup.milk == MilkType.FreshMilk &&
                               cup.sweetnessPercent == 100 &&
                               cup.icePercent == 50 &&
                               cup.toppings != null &&
                               cup.toppings.Contains(ToppingType.TapiocaPearls));

            if (isFavorite)
            {
                if (rentDiscountedByDrink)
                {
                    if (speechBubble != null)
                    {
                        speechBubble.ShowMessage("H-hey! I already gave you a 10% discount for that drink! You can't get double discounts, baka! Just pay the rent!");
                    }
                }
                else
                {
                    rentDiscountedByDrink = true;
                    float baseRentDue = EconomyManager.Instance.GetTotalRentDue(currentRentDay);
                    discountedRentAmount = (float)Math.Round(baseRentDue * 0.90f, 2);

                    if (payRentButtonText != null)
                    {
                        payRentButtonText.text = $"Pay Rent (${discountedRentAmount:F2})";
                    }
                    if (payRentButton != null)
                    {
                        payRentButton.interactable = EconomyManager.Instance.CanAfford(discountedRentAmount);
                    }

                    if (speechBubble != null)
                    {
                        speechBubble.ShowMessage("W-wait... Is this Oolong Milk Tea with tapioca pearls, 100% sugar and 50% ice?! ...O-okay, fine! Just for this once, I'll lower your rent by 10%! But don't get the wrong idea, baka!");
                    }
                    HUDController.Instance?.ShowNotification("💖 Landlord loved her favorite drink! Rent reduced by 10%!", 4f);
                }
            }
            else
            {
                if (speechBubble != null)
                {
                    speechBubble.ShowMessage("D-DON'T TRY TO BRIBE ME WITH A DRINK >:(! Hand over the rent already!");
                }
                HUDController.Instance?.ShowNotification("The Landlord rejected the bribe!", 3f);
            }
        }

        private void HandlePayRent()
        {
            if (payRentButton != null) payRentButton.interactable = false;
            if (skipRentButton != null) skipRentButton.interactable = false;

            float amountToPay = rentDiscountedByDrink ? discountedRentAmount : EconomyManager.Instance.GetTotalRentDue(currentRentDay);
            bool success = EconomyManager.Instance.PaySpecificRent(amountToPay, currentRentDay);
            if (success)
            {
                if (rentChoicePanel != null) rentChoicePanel.SetActive(false);
                if (payRentButton != null) payRentButton.gameObject.SetActive(false);
                if (skipRentButton != null) skipRentButton.gameObject.SetActive(false);

                if (speechBubble != null)
                {
                    speechBubble.ShowMessage("Hmph, fine! I guess your payment is good. Don't go slacking off next week, dummy! ...S-see you next time.");
                }
                HUDController.Instance?.ShowNotification("Rent paid successfully!", 3f);
                StartCoroutine(DismissLandlordAfterDelay(3.5f));
            }
            else
            {
                if (speechBubble != null)
                {
                    speechBubble.ShowMessage("You don't even have enough money, dummy! Stop teasing me and pay up!");
                }
                if (payRentButton != null) payRentButton.interactable = false;
                if (skipRentButton != null) skipRentButton.interactable = true;
            }
        }

        private void HandleSkipRent()
        {
            if (payRentButton != null) payRentButton.interactable = false;
            if (skipRentButton != null) skipRentButton.interactable = false;

            if (EconomyManager.Instance.CanSkipRent())
            {
                EconomyManager.Instance.SkipRent(currentRentDay);
                if (rentChoicePanel != null) rentChoicePanel.SetActive(false);
                if (payRentButton != null) payRentButton.gameObject.SetActive(false);
                if (skipRentButton != null) skipRentButton.gameObject.SetActive(false);

                if (speechBubble != null)
                {
                    speechBubble.ShowMessage("W-WHAT?! You can't pay?! ...F-fine, I'll give you ONE extension, but ONLY because I'm nice! Next week you better pay double or you're out on the street! Hmph!");
                }
                HUDController.Instance?.ShowNotification("Rent skipped! 1 extension used.", 3.5f);
                StartCoroutine(DismissLandlordAfterDelay(3.5f));
            }
            else
            {
                if (speechBubble != null)
                {
                    speechBubble.ShowMessage("I already gave you an extension, idiot! That's it, you're officially EVICTED! Hmph!");
                }
                StartCoroutine(TriggerEvictionGameOver(2.5f));
            }
        }

        private IEnumerator DismissLandlordAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            isLandlordActive = false;
            if (patienceFillImage != null) patienceFillImage.gameObject.SetActive(true);
            DismissCustomer();
            onLandlordFinished?.Invoke();
            if (GameManager.Instance != null)
            {
                HUDController.Instance?.UpdateStateHint(GameManager.Instance.CurrentState);
            }
        }

        private IEnumerator TriggerEvictionGameOver(float delay)
        {
            yield return new WaitForSeconds(delay);
            GameManager.Instance?.TriggerGameOver("Evicted: Failed to pay overdue rent to the landlord.");
        }
    }
}
