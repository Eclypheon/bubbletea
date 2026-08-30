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
        private Coroutine leaveRoutine;

        public DrinkOrder ActiveOrder => activeOrder;
        public float PatiencePercent => Mathf.Clamp01(currentPatience / maxPatience);
        public bool IsActive => isWaiting;
        public bool IsPresent => gameObject.activeSelf;

        public event Action<CustomerController, EvaluationResult> OnCustomerServed;
        public event Action<CustomerController> OnCustomerLeftAngry;

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
                    "HEY! WOW! You didn't even make my drink! Worst boba shop in town, I'M OUTTA HERE!",
                    "DON'T RING THAT BELL AT ME! Zero stars, never stepping foot in this joint again!",
                    "Rude!! You just lost your best customer! Keep your tea!"
                },
                CustomerArchetype.Dyscalculia => new string[]
                {
                    "I spent 10 minutes counting my coins for you to just kick me out?! I'm never returning!",
                    "Ringing the bell before I can even pay?! The math on your customer service is ZERO!",
                    "Forget it! I'm taking my money to the coffee shop next door!"
                },
                CustomerArchetype.Dyslexia => new string[]
                {
                    "I was just trying to read your menu board! You didn't have to kick me out, I'm never coming back!",
                    "Rude! Not everyone can read your fancy tea names in 2 seconds! Never returning!",
                    "Worst service ever! I'll tell everyone to avoid this place!"
                },
                _ => new string[]
                {
                    "Hey! You're skipping me?! Never coming back to this shop!",
                    "Rude! I'm taking my business elsewhere!"
                }
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
            if (speechBubble != null) speechBubble.HideBubbleInstant();
            gameObject.SetActive(false);
        }
    }
}
