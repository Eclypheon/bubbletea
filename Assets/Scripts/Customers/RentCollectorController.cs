using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    public class RentCollectorController : MonoBehaviour
    {
        private static RentCollectorController _instance;
        public static RentCollectorController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<RentCollectorController>(FindObjectsInactive.Include);
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("UI Root & Container")]
        [SerializeField] private GameObject collectorRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private GameObject rentBubbleObject;
        [SerializeField] private Image landlordImage;
        [SerializeField] private Sprite landlordSprite;

        [Header("Dialogue & Text")]
        [SerializeField] private TextMeshProUGUI collectorNameText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private TextMeshProUGUI rentAmountText;

        [Header("Choice Buttons")]
        [SerializeField] private Button payRentButton;
        [SerializeField] private TextMeshProUGUI payRentButtonText;
        [SerializeField] private Button skipRentButton;
        [SerializeField] private TextMeshProUGUI skipRentButtonText;

        [Header("Audio (Optional)")]
        [SerializeField] private AudioClip arriveSound;
        [SerializeField] private AudioClip paySound;
        [SerializeField] private AudioClip angerSound;

        private Action onEncounterFinished;
        private int currentDayNumber;
        private Coroutine fadeRoutine;
        private bool isEncounterActive = false;

        public bool IsEncounterActive => isEncounterActive;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            AutoDiscoverReferences();
            HideInstant();
        }

        private void Start()
        {
            AutoDiscoverReferences();
        }

        private void AutoDiscoverReferences()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>() ?? GetComponentInChildren<CanvasGroup>(true);
            if (collectorRoot == null) collectorRoot = gameObject;

            if (rentBubbleObject == null)
            {
                Transform bubbleTr = transform.Find("RentBubble") ?? transform.Find("Bubble") ?? transform.Find("SpeechBubble");
                if (bubbleTr != null) rentBubbleObject = bubbleTr.gameObject;
            }

            if (landlordImage == null)
            {
                var imgs = GetComponentsInChildren<Image>(true);
                foreach (var img in imgs)
                {
                    if (img.gameObject.name.ToLower().Contains("avatar") || img.gameObject.name.ToLower().Contains("landlord"))
                    {
                        landlordImage = img;
                        break;
                    }
                }
            }

            if (collectorNameText == null || dialogueText == null || rentAmountText == null || payRentButtonText == null || skipRentButtonText == null)
            {
                var tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var tmp in tmps)
                {
                    string n = tmp.gameObject.name.ToLower();
                    if (n.Contains("name") && collectorNameText == null) collectorNameText = tmp;
                    else if (n.Contains("dialog") && dialogueText == null) dialogueText = tmp;
                    else if (n.Contains("amount") && rentAmountText == null) rentAmountText = tmp;
                    else if (n.Contains("pay") && payRentButtonText == null) payRentButtonText = tmp;
                    else if ((n.Contains("skip") || n.Contains("exten")) && skipRentButtonText == null) skipRentButtonText = tmp;
                }
            }

            if (payRentButton == null || skipRentButton == null)
            {
                var btns = GetComponentsInChildren<Button>(true);
                foreach (var btn in btns)
                {
                    string n = btn.gameObject.name.ToLower();
                    if (n.Contains("pay") && payRentButton == null) payRentButton = btn;
                    else if ((n.Contains("skip") || n.Contains("exten")) && skipRentButton == null) skipRentButton = btn;
                }
            }
        }

        public void HideInstant()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            if (collectorRoot != null && collectorRoot != gameObject)
            {
                collectorRoot.SetActive(false);
            }
        }

        public void TriggerRentEncounter(int dayNumber, Action onComplete)
        {
            currentDayNumber = dayNumber;
            onEncounterFinished = onComplete;
            isEncounterActive = true;

            AutoDiscoverReferences();

            gameObject.SetActive(true);
            transform.SetAsLastSibling(); // Ensure Landlord renders in front of counter and shutters

            if (collectorRoot != null) collectorRoot.SetActive(true);
            if (rentBubbleObject != null)
            {
                rentBubbleObject.SetActive(true);
                rentBubbleObject.transform.SetAsLastSibling();
            }

            if (landlordImage != null)
            {
                landlordImage.gameObject.SetActive(true);
                if (landlordSprite != null) landlordImage.sprite = landlordSprite;
            }

            if (collectorNameText != null)
            {
                collectorNameText.gameObject.SetActive(true);
                collectorNameText.text = "Landlady Chubi";
            }

            float totalRent = EconomyManager.Instance.GetTotalRentDue(dayNumber);
            bool canAfford = EconomyManager.Instance.CanAfford(totalRent);
            bool canSkip = EconomyManager.Instance.CanSkipRent();
            int skipsUsed = EconomyManager.Instance.RentSkipsUsed;

            if (rentAmountText != null)
            {
                rentAmountText.gameObject.SetActive(true);
                rentAmountText.text = $"Total Rent Due: <color=#FFD700>${totalRent:F2}</color>";
            }

            if (dialogueText != null)
            {
                dialogueText.gameObject.SetActive(true);
                if (skipsUsed > 0)
                {
                    dialogueText.text = $"\"You're on thin ice! You owe last week's rent PLUS this week's: ${totalRent:F2}. Pay up now or you're evicted!\"";
                }
                else
                {
                    int week = Mathf.CeilToInt((float)dayNumber / EconomyManager.Instance.RentCycleDays);
                    dialogueText.text = $"\"Greetings. Week {week} has ended. Your rent of ${totalRent:F2} is due right now before you close up.\"";
                }
            }

            // Pay Button
            if (payRentButton != null)
            {
                payRentButton.gameObject.SetActive(true);
                payRentButton.interactable = canAfford;
                if (payRentButtonText != null)
                {
                    payRentButtonText.text = canAfford ? $"Pay Rent (${totalRent:F2})" : $"Can't Afford (${totalRent:F2})";
                }
            }

            // Skip Button
            if (skipRentButton != null)
            {
                skipRentButton.gameObject.SetActive(true);
                skipRentButton.interactable = true;
                if (skipRentButtonText != null)
                {
                    skipRentButtonText.text = canSkip ? "Ask for Extension (1 left)" : "Can't Pay (Face Eviction)";
                }
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            HUDController.Instance?.ShowNotification("The Landlady has arrived!", 3.5f);
            if (arriveSound != null) AudioManager.Instance?.PlaySFX(arriveSound);

            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeCanvasGroup(0f, 1f, 0.25f));
        }

        private void OnPayRentClicked()
        {
            if (payRentButton != null) payRentButton.interactable = false;
            if (skipRentButton != null) skipRentButton.interactable = false;

            bool success = EconomyManager.Instance.PayTotalRent(currentDayNumber);
            if (success)
            {
                if (paySound != null) AudioManager.Instance?.PlaySFX(paySound);
                if (dialogueText != null)
                {
                    dialogueText.text = "\"Payment accepted in full. Keep the shop running well, and I will see you next week.\"";
                }
                HUDController.Instance?.ShowNotification("Rent paid successfully!", 2.5f);
                StartCoroutine(DismissCollectorAfterDelay(2.5f));
            }
            else
            {
                if (dialogueText != null)
                {
                    dialogueText.text = "\"You don't have enough money! Don't play games with me!\"";
                }
                if (payRentButton != null) payRentButton.interactable = false;
                if (skipRentButton != null) skipRentButton.interactable = true;
            }
        }

        private void OnSkipRentClicked()
        {
            if (payRentButton != null) payRentButton.interactable = false;
            if (skipRentButton != null) skipRentButton.interactable = false;

            if (EconomyManager.Instance.CanSkipRent())
            {
                // Use the 1 skip grace
                EconomyManager.Instance.SkipRent(currentDayNumber);
                if (angerSound != null) AudioManager.Instance?.PlaySFX(angerSound);
                if (dialogueText != null)
                {
                    dialogueText.text = "\"Hmph! I'll give you ONE extension. Next week you MUST pay the accumulated amount or get evicted on the spot!\"";
                }
                HUDController.Instance?.ShowNotification("Rent skipped! 1 extension used.", 3.5f);
                StartCoroutine(DismissCollectorAfterDelay(3.0f));
            }
            else
            {
                // No skips left -> Game Over!
                if (angerSound != null) AudioManager.Instance?.PlaySFX(angerSound);
                if (dialogueText != null)
                {
                    dialogueText.text = "\"You already used your ONE extension! Pack your things, you are EVICTED!\"";
                }
                StartCoroutine(TriggerEvictionGameOver(2.5f));
            }
        }

        private IEnumerator DismissCollectorAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            yield return StartCoroutine(FadeCanvasGroup(canvasGroup != null ? canvasGroup.alpha : 1f, 0f, 0.3f));
            HideInstant();
            isEncounterActive = false;
            onEncounterFinished?.Invoke();
        }

        private IEnumerator TriggerEvictionGameOver(float delay)
        {
            yield return new WaitForSeconds(delay);
            GameManager.Instance?.TriggerGameOver("Evicted: Failed to pay overdue rent to the Landlady.");
        }

        private IEnumerator FadeCanvasGroup(float start, float target, float duration)
        {
            if (canvasGroup == null) yield break;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, target, elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = target;
            canvasGroup.blocksRaycasts = target > 0.5f;
            canvasGroup.interactable = target > 0.5f;
        }
    }
}
