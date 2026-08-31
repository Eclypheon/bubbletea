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
            if (patienceFillImage != null && patienceFillImage.sprite == null)
            {
                Texture2D whiteTex = Texture2D.whiteTexture;
                patienceFillImage.sprite = Sprite.Create(whiteTex, new Rect(0, 0, whiteTex.width, whiteTex.height), new Vector2(0.5f, 0.5f));
            }

            if (payRentButton != null)
            {
                payRentButton.onClick.RemoveAllListeners();
                payRentButton.onClick.AddListener(HandlePayRent);
            }
            if (skipRentButton != null)
            {
                skipRentButton.onClick.RemoveAllListeners();
                skipRentButton.onClick.AddListener(HandleSkipRent);
            }
            if (rentChoicePanel != null) rentChoicePanel.SetActive(false);
            if (payRentButton != null) payRentButton.gameObject.SetActive(false);
            if (skipRentButton != null) skipRentButton.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (isMentorTalking)
            {
                UpdateMentorNavPosition();
            }

            if (!isWaiting) return;

            // Continually refresh hierarchy positioning and visibility of patience bar during day phase
            if (patienceFillImage != null)
            {
                if (patienceFillImage.transform.parent != null)
                {
                    if (!patienceFillImage.transform.parent.gameObject.activeSelf)
                    {
                        patienceFillImage.transform.parent.gameObject.SetActive(true);
                    }
                    patienceFillImage.transform.parent.SetAsLastSibling();
                }

                if (!patienceFillImage.gameObject.activeSelf)
                {
                    patienceFillImage.gameObject.SetActive(true);
                }

                patienceFillImage.fillAmount = PatiencePercent;
                // Tint patience bar from Green -> Yellow -> Red
                patienceFillImage.color = Color.Lerp(Color.red, Color.green, PatiencePercent);
            }

            currentPatience -= Time.deltaTime;

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

            transform.SetAsLastSibling();

            if (patienceFillImage != null)
            {
                if (patienceFillImage.sprite == null)
                {
                    Texture2D whiteTex = Texture2D.whiteTexture;
                    patienceFillImage.sprite = Sprite.Create(whiteTex, new Rect(0, 0, whiteTex.width, whiteTex.height), new Vector2(0.5f, 0.5f));
                }
                if (patienceFillImage.transform.parent != null)
                {
                    patienceFillImage.transform.parent.gameObject.SetActive(true);
                    patienceFillImage.transform.parent.SetAsLastSibling();
                }
                patienceFillImage.gameObject.SetActive(true);
                patienceFillImage.fillAmount = 1f;
                patienceFillImage.color = Color.green;
            }

            UpdateCustomerSprite(order.archetype);
            gameObject.SetActive(true);

            if (speechBubble != null)
            {
                speechBubble.ShowOrder(order);
            }

            if (patienceFillImage != null && patienceFillImage.transform.parent != null)
            {
                patienceFillImage.transform.parent.SetAsLastSibling();
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
            isMentorTalking = false;
            if (mentorNavPanel != null) mentorNavPanel.SetActive(false);
            OrderTicketUI.Instance?.HideTicket();
            if (speechBubble != null) speechBubble.HideBubbleInstant();
            if (rentChoicePanel != null) rentChoicePanel.SetActive(false);
            if (payRentButton != null) payRentButton.gameObject.SetActive(false);
            if (skipRentButton != null) skipRentButton.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        private bool isMentorActive = false;
        public bool IsMentorActive => isMentorActive;
        private bool isMentorTalking = false;
        public bool IsMentorTalking => isMentorTalking;

        private GameObject mentorNavPanel;
        private Button mentorNextButton;
        private TMPro.TextMeshProUGUI mentorNextButtonText;
        private Button mentorSkipButton;
        private TMPro.TextMeshProUGUI mentorSkipButtonText;
        private string[] activeMentorLines;
        private int currentMentorLineIndex = 0;
        private Action onActiveMentorCompleted;

        private void EnsureMentorNavUI()
        {
            Canvas rootCanvas = GetComponentInParent<Canvas>();
            Transform targetParent = (rootCanvas != null) ? rootCanvas.transform : transform.root;

            if (mentorNavPanel == null)
            {
                mentorNavPanel = new GameObject("MentorNavPanel", typeof(RectTransform));
                mentorNavPanel.transform.SetParent(targetParent, false);

                var navRt = mentorNavPanel.GetComponent<RectTransform>();
                navRt.anchorMin = new Vector2(0.5f, 0.5f);
                navRt.anchorMax = new Vector2(0.5f, 0.5f);
                navRt.pivot = new Vector2(0.5f, 0.5f);
                navRt.sizeDelta = new Vector2(400f, 48f);

                Texture2D whiteTex = Texture2D.whiteTexture;
                Sprite whiteSp = Sprite.Create(whiteTex, new Rect(0, 0, whiteTex.width, whiteTex.height), new Vector2(0.5f, 0.5f));

                // 1. Skip Button (Left)
                GameObject skipObj = new GameObject("SkipButton", typeof(RectTransform), typeof(Image), typeof(Button));
                skipObj.transform.SetParent(mentorNavPanel.transform, false);
                var skipRt = skipObj.GetComponent<RectTransform>();
                skipRt.anchorMin = new Vector2(0f, 0.5f);
                skipRt.anchorMax = new Vector2(0f, 0.5f);
                skipRt.pivot = new Vector2(0f, 0.5f);
                skipRt.anchoredPosition = new Vector2(10f, 0f);
                skipRt.sizeDelta = new Vector2(140f, 44f);

                var skipImg = skipObj.GetComponent<Image>();
                skipImg.sprite = whiteSp;
                skipImg.color = new Color(0.32f, 0.35f, 0.40f, 1f);

                GameObject skipTextObj = new GameObject("Text", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
                skipTextObj.transform.SetParent(skipObj.transform, false);
                var stRt = skipTextObj.GetComponent<RectTransform>();
                stRt.anchorMin = Vector2.zero;
                stRt.anchorMax = Vector2.one;
                stRt.offsetMin = Vector2.zero;
                stRt.offsetMax = Vector2.zero;

                mentorSkipButtonText = skipTextObj.GetComponent<TMPro.TextMeshProUGUI>();
                mentorSkipButtonText.text = "Skip >>";
                mentorSkipButtonText.fontSize = 18;
                mentorSkipButtonText.fontStyle = TMPro.FontStyles.Bold;
                mentorSkipButtonText.alignment = TMPro.TextAlignmentOptions.Center;
                mentorSkipButtonText.color = Color.white;
                mentorSkipButtonText.raycastTarget = false;

                mentorSkipButton = skipObj.GetComponent<Button>();
                mentorSkipButton.onClick.AddListener(OnMentorSkipClicked);

                // 2. Next Button (Right)
                GameObject nextObj = new GameObject("NextButton", typeof(RectTransform), typeof(Image), typeof(Button));
                nextObj.transform.SetParent(mentorNavPanel.transform, false);
                var nextRt = nextObj.GetComponent<RectTransform>();
                nextRt.anchorMin = new Vector2(1f, 0.5f);
                nextRt.anchorMax = new Vector2(1f, 0.5f);
                nextRt.pivot = new Vector2(1f, 0.5f);
                nextRt.anchoredPosition = new Vector2(-10f, 0f);
                nextRt.sizeDelta = new Vector2(180f, 44f);

                var nextImg = nextObj.GetComponent<Image>();
                nextImg.sprite = whiteSp;
                nextImg.color = new Color(0.16f, 0.68f, 0.32f, 1f);

                GameObject nextTextObj = new GameObject("Text", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
                nextTextObj.transform.SetParent(nextObj.transform, false);
                var ntRt = nextTextObj.GetComponent<RectTransform>();
                ntRt.anchorMin = Vector2.zero;
                ntRt.anchorMax = Vector2.one;
                ntRt.offsetMin = Vector2.zero;
                ntRt.offsetMax = Vector2.zero;

                mentorNextButtonText = nextTextObj.GetComponent<TMPro.TextMeshProUGUI>();
                mentorNextButtonText.text = "Next >";
                mentorNextButtonText.fontSize = 18;
                mentorNextButtonText.fontStyle = TMPro.FontStyles.Bold;
                mentorNextButtonText.alignment = TMPro.TextAlignmentOptions.Center;
                mentorNextButtonText.color = Color.white;
                mentorNextButtonText.raycastTarget = false;

                mentorNextButton = nextObj.GetComponent<Button>();
                mentorNextButton.onClick.AddListener(OnMentorNextClicked);
            }
            else
            {
                if (mentorNavPanel.transform.parent != targetParent)
                {
                    mentorNavPanel.transform.SetParent(targetParent, false);
                }
            }

            mentorNavPanel.transform.SetAsLastSibling();
            UpdateMentorNavPosition();
        }

        private void UpdateMentorNavPosition()
        {
            if (mentorNavPanel == null) return;
            var navRt = mentorNavPanel.GetComponent<RectTransform>();
            if (navRt == null) return;

            if (speechBubble != null)
            {
                var sbRt = speechBubble.GetComponent<RectTransform>();
                if (sbRt != null)
                {
                    Vector3[] corners = new Vector3[4];
                    sbRt.GetWorldCorners(corners);
                    // corners[0] is bottom-left, corners[3] is bottom-right
                    Vector3 bottomCenterWorld = (corners[0] + corners[3]) * 0.5f;

                    Canvas rootCanvas = GetComponentInParent<Canvas>();
                    float scaleFactor = (rootCanvas != null) ? rootCanvas.scaleFactor : 1f;
                    navRt.position = bottomCenterWorld + new Vector3(0f, -28f * scaleFactor, 0f);
                    return;
                }
            }

            navRt.anchoredPosition = new Vector2(260f, 95f);
        }

        public void SpawnMentorSequence(string[] lines, float delayPerLine, Sprite mentorSpriteParam, Action onCompletedSequence = null)
        {
            if (leaveRoutine != null)
            {
                StopCoroutine(leaveRoutine);
                leaveRoutine = null;
            }

            isMentorActive = true;
            isMentorTalking = true;
            isWaiting = false;
            activeOrder = null;
            activeMentorLines = lines;
            currentMentorLineIndex = 0;
            onActiveMentorCompleted = onCompletedSequence;

            if (patienceFillImage != null) patienceFillImage.gameObject.SetActive(false);

            if (customerImage != null)
            {
                Sprite s = mentorSpriteParam != null ? mentorSpriteParam : dyscalculiaSprite;
                if (s != null) customerImage.sprite = s;
            }

            gameObject.SetActive(true);
            OrderTicketUI.Instance?.HideTicket();
            HUDController.Instance?.SetStatusHint("Listen to your Mentor's advice...");

            EnsureMentorNavUI();
            if (mentorNavPanel != null)
            {
                mentorNavPanel.SetActive(true);
                mentorNavPanel.transform.SetAsLastSibling();
            }

            DisplayCurrentMentorLine();
        }

        private void DisplayCurrentMentorLine()
        {
            if (activeMentorLines == null || activeMentorLines.Length == 0)
            {
                FinishMentorDialogue();
                return;
            }

            if (mentorNavPanel != null)
            {
                mentorNavPanel.SetActive(true);
                mentorNavPanel.transform.SetAsLastSibling();
                UpdateMentorNavPosition();
            }

            string line = activeMentorLines[currentMentorLineIndex];
            if (speechBubble != null)
            {
                speechBubble.ShowMessage(line);
            }

            bool isLastLine = (currentMentorLineIndex == activeMentorLines.Length - 1);
            if (mentorNextButtonText != null)
            {
                mentorNextButtonText.text = isLastLine ? "Got it! >" : "Next >";
            }

            if (line.Contains("Cash Register"))
            {
                CashRegisterInventoryUI.Instance?.TriggerAttentionPulse(3.0f);
            }
            if (line.Contains("desk bell") || line.Contains("Desk Bell"))
            {
                DeskBell.Instance?.StartAttentionWiggle();
            }
        }

        private void OnMentorNextClicked()
        {
            if (!isMentorTalking || activeMentorLines == null) return;

            currentMentorLineIndex++;
            if (currentMentorLineIndex < activeMentorLines.Length)
            {
                DisplayCurrentMentorLine();
            }
            else
            {
                FinishMentorDialogue();
            }
        }

        private void OnMentorSkipClicked()
        {
            if (!isMentorTalking) return;
            FinishMentorDialogue();
        }

        private void FinishMentorDialogue()
        {
            isMentorTalking = false;

            if (mentorNavPanel != null)
            {
                mentorNavPanel.SetActive(false);
            }

            bool isBellPrompt = activeMentorLines != null && activeMentorLines.Length > 0 &&
                (activeMentorLines[activeMentorLines.Length - 1].Contains("desk bell") || activeMentorLines[activeMentorLines.Length - 1].Contains("Desk Bell"));

            if (isBellPrompt)
            {
                HUDController.Instance?.SetStatusHint("Ring the desk bell to call your first customer!");
                DeskBell.Instance?.StartAttentionWiggle();
                // Keeps isMentorActive = true so ringing the bell calls customer and dismisses mentor
            }
            else
            {
                // Day 2 closing / non-bell briefing is finished: release isMentorActive so shutters can be closed
                isMentorActive = false;
                HUDController.Instance?.SetStatusHint("Close the shutter to start the Night Phase!");
            }

            var callback = onActiveMentorCompleted;
            onActiveMentorCompleted = null;
            callback?.Invoke();
        }

        public void DismissMentor()
        {
            isMentorActive = false;
            isMentorTalking = false;
            if (mentorNavPanel != null) mentorNavPanel.SetActive(false);
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
                customerImage.gameObject.SetActive(true);
                customerImage.color = Color.white;
                Sprite s = landlordSprite != null ? landlordSprite : anxietySprite;
                if (s != null) customerImage.sprite = s;
            }
            if (customerRoot != null) customerRoot.gameObject.SetActive(true);

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
                var cg = payRentButton.GetComponentInParent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
                if (payRentButtonText != null)
                {
                    payRentButtonText.text = canAfford ? $"Pay Rent (${totalRent:F2})" : $"Can't Afford (${totalRent:F2})";
                }
            }

            if (skipRentButton != null)
            {
                skipRentButton.gameObject.SetActive(true);
                skipRentButton.interactable = true;
                var cg = skipRentButton.GetComponentInParent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
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
                    HUDController.Instance?.ShowNotification("Landlord loved her favorite drink! Rent reduced by 10%!", 4f);
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
