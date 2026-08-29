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
        
        private DrinkOrder activeOrder;
        private float maxPatience = 45f;
        private float currentPatience = 45f;
        private bool isWaiting = false;

        public DrinkOrder ActiveOrder => activeOrder;
        public float PatiencePercent => Mathf.Clamp01(currentPatience / maxPatience);
        public bool IsActive => isWaiting;

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

            StartCoroutine(LeaveAfterDelay(evaluation, 2.5f));
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
            if (speechBubble != null)
            {
                speechBubble.ShowReaction("Took too long! I'm leaving!", 1);
            }
            StartCoroutine(LeaveAngryRoutine());
        }

        private IEnumerator LeaveAngryRoutine()
        {
            yield return new WaitForSeconds(2.0f);
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
            isWaiting = false;
            if (speechBubble != null) speechBubble.HideBubbleInstant();
            gameObject.SetActive(false);
        }
    }
}
