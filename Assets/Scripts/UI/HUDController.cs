using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    public class HUDController : MonoBehaviour
    {
        public static HUDController Instance { get; private set; }

        [Header("Top Bar Elements")]
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private TextMeshProUGUI cashText;
        [SerializeField] private TextMeshProUGUI rentTimerText;
        [SerializeField] private TextMeshProUGUI customerCountText;
        [SerializeField] private TextMeshProUGUI statusHintText;

        [Header("Market Event HUD Badge")]
        [SerializeField] private GameObject marketEventBadgeObj;
        [SerializeField] private Button marketEventButton;
        [SerializeField] private Image marketEventIcon;
        [SerializeField] private TextMeshProUGUI marketEventTrendText;
        [SerializeField] private TextMeshProUGUI marketEventDaysText;

        [Header("Market Event Modal Dialog")]
        [SerializeField] private GameObject marketEventModal;
        [SerializeField] private TextMeshProUGUI modalTitleText;
        [SerializeField] private TextMeshProUGUI modalDaysText;
        [SerializeField] private TextMeshProUGUI modalDescriptionText;
        [SerializeField] private TextMeshProUGUI modalImpactText;
        [SerializeField] private Button modalCloseButton;

        private Coroutine notificationRoutine;
        private Sprite bobaSprite;
        private Sprite iceSprite;
        private Sprite milkSprite;
        private Sprite jellySprite;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DisableRaycasts();
            BringToFront();
        }

        private void Start()
        {
            DisableRaycasts();
            BringToFront();
            LoadFallbackSprites();
            EnsureMarketEventUI();
            EnsureMarketEventModal();

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnCashChanged += UpdateCashDisplay;
                UpdateCashDisplay(EconomyManager.Instance.CurrentCash);
            }

            if (DayManager.Instance != null)
            {
                DayManager.Instance.OnDayStarted += (day) =>
                {
                    UpdateDayDisplay(day);
                    UpdateMarketEventDisplay();
                };
                DayManager.Instance.OnCustomerProgressUpdated += UpdateCustomerCountDisplay;
                UpdateDayDisplay(DayManager.Instance.CurrentDay);
            }

            if (MarketEventManager.Instance != null)
            {
                MarketEventManager.Instance.OnMarketEventTriggered += (ev) => UpdateMarketEventDisplay();
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += (state) =>
                {
                    UpdateStateHint(state);
                    UpdateMarketEventDisplay();
                };
                UpdateStateHint(GameManager.Instance.CurrentState);
            }
            else
            {
                UpdateStateHint(GameState.MorningPrep);
            }

            UpdateMarketEventDisplay();
        }

        private void LoadFallbackSprites()
        {
#if UNITY_EDITOR
            bobaSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Topping_Boba.png");
            iceSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ice_Cubes.png");
#endif
            if (bobaSprite == null || iceSprite == null)
            {
                var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
                foreach (var s in allSprites)
                {
                    if (s == null) continue;
                    if (bobaSprite == null && (s.name.Contains("Boba") || s.name.Contains("boba"))) bobaSprite = s;
                    if (iceSprite == null && (s.name.Contains("Ice") || s.name.Contains("ice"))) iceSprite = s;
                    if (milkSprite == null && (s.name.Contains("milk") || s.name.Contains("Milk"))) milkSprite = s;
                    if (jellySprite == null && (s.name.Contains("jelly") || s.name.Contains("Jelly"))) jellySprite = s;
                }
            }
        }

        private void EnsureMarketEventUI()
        {
            if (marketEventBadgeObj != null) return;

            // Create interactive badge right beside day counter
            marketEventBadgeObj = new GameObject("MarketEventBadge", typeof(RectTransform), typeof(Image), typeof(Button));
            marketEventBadgeObj.transform.SetParent(transform, false);

            RectTransform rt = marketEventBadgeObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(125f, 42f);
            rt.anchoredPosition = new Vector2(-565f, 0f);

            var bgImg = marketEventBadgeObj.GetComponent<Image>();
            bgImg.color = new Color(0.18f, 0.15f, 0.25f, 0.95f);

            marketEventButton = marketEventBadgeObj.GetComponent<Button>();
            marketEventButton.onClick.AddListener(OpenMarketEventModal);

            // Icon Image
            GameObject iconObj = new GameObject("EventIcon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(marketEventBadgeObj.transform, false);
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.sizeDelta = new Vector2(30f, 30f);
            iconRt.anchoredPosition = new Vector2(8f, 0f);
            marketEventIcon = iconObj.GetComponent<Image>();
            marketEventIcon.raycastTarget = false;
            marketEventIcon.preserveAspect = true;

            // Trend Indicator Text (Red/Green Triangle)
            GameObject trendObj = new GameObject("TrendText", typeof(RectTransform), typeof(TextMeshProUGUI));
            trendObj.transform.SetParent(marketEventBadgeObj.transform, false);
            RectTransform trendRt = trendObj.GetComponent<RectTransform>();
            trendRt.anchorMin = new Vector2(0f, 0.5f);
            trendRt.anchorMax = new Vector2(0f, 0.5f);
            trendRt.pivot = new Vector2(0f, 0.5f);
            trendRt.sizeDelta = new Vector2(24f, 30f);
            trendRt.anchoredPosition = new Vector2(44f, 0f);
            marketEventTrendText = trendObj.GetComponent<TextMeshProUGUI>();
            marketEventTrendText.fontSize = 20;
            marketEventTrendText.alignment = TextAlignmentOptions.Center;
            marketEventTrendText.raycastTarget = false;

            // Days Remaining Text
            GameObject daysObj = new GameObject("DaysText", typeof(RectTransform), typeof(TextMeshProUGUI));
            daysObj.transform.SetParent(marketEventBadgeObj.transform, false);
            RectTransform daysRt = daysObj.GetComponent<RectTransform>();
            daysRt.anchorMin = new Vector2(0f, 0.5f);
            daysRt.anchorMax = new Vector2(1f, 0.5f);
            daysRt.pivot = new Vector2(0.5f, 0.5f);
            daysRt.sizeDelta = new Vector2(0f, 30f);
            daysRt.anchoredPosition = new Vector2(34f, 0f);
            marketEventDaysText = daysObj.GetComponent<TextMeshProUGUI>();
            marketEventDaysText.fontSize = 16;
            marketEventDaysText.fontStyle = FontStyles.Bold;
            marketEventDaysText.color = new Color(1f, 0.85f, 0.4f, 1f);
            marketEventDaysText.alignment = TextAlignmentOptions.Left;
            marketEventDaysText.raycastTarget = false;

            marketEventBadgeObj.SetActive(false);
        }

        private void EnsureMarketEventModal()
        {
            if (marketEventModal != null) return;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            marketEventModal = new GameObject("MarketEventModal_Root", typeof(RectTransform), typeof(Image));
            marketEventModal.transform.SetParent(canvas.transform, false);
            marketEventModal.transform.SetAsLastSibling();

            RectTransform rootRt = marketEventModal.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            var bgOverlay = marketEventModal.GetComponent<Image>();
            bgOverlay.color = new Color(0f, 0f, 0f, 0.70f);

            var overlayBtn = marketEventModal.AddComponent<Button>();
            overlayBtn.onClick.AddListener(CloseMarketEventModal);

            // Dialog Card
            GameObject cardObj = new GameObject("Card", typeof(RectTransform), typeof(Image));
            cardObj.transform.SetParent(marketEventModal.transform, false);
            RectTransform cardRt = cardObj.GetComponent<RectTransform>();
            cardRt.sizeDelta = new Vector2(540f, 360f);
            cardRt.anchoredPosition = Vector2.zero;

            var cardImg = cardObj.GetComponent<Image>();
            cardImg.color = new Color(0.12f, 0.11f, 0.16f, 0.98f);

            // Header Banner
            GameObject headerObj = new GameObject("Header", typeof(RectTransform), typeof(TextMeshProUGUI));
            headerObj.transform.SetParent(cardObj.transform, false);
            RectTransform headerRt = headerObj.GetComponent<RectTransform>();
            headerRt.sizeDelta = new Vector2(500f, 40f);
            headerRt.anchoredPosition = new Vector2(0f, 140f);
            var headerTmp = headerObj.GetComponent<TextMeshProUGUI>();
            headerTmp.text = "<color=#F1C40F><b>MARKET EVENT ACTIVE</b></color>";
            headerTmp.fontSize = 20;
            headerTmp.alignment = TextAlignmentOptions.Center;

            // Title
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(cardObj.transform, false);
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.sizeDelta = new Vector2(500f, 40f);
            titleRt.anchoredPosition = new Vector2(0f, 100f);
            modalTitleText = titleObj.GetComponent<TextMeshProUGUI>();
            modalTitleText.fontSize = 24;
            modalTitleText.fontStyle = FontStyles.Bold;
            modalTitleText.alignment = TextAlignmentOptions.Center;
            modalTitleText.color = Color.white;

            // Days Duration
            GameObject daysObj = new GameObject("Days", typeof(RectTransform), typeof(TextMeshProUGUI));
            daysObj.transform.SetParent(cardObj.transform, false);
            RectTransform daysRt = daysObj.GetComponent<RectTransform>();
            daysRt.sizeDelta = new Vector2(500f, 30f);
            daysRt.anchoredPosition = new Vector2(0f, 65f);
            modalDaysText = daysObj.GetComponent<TextMeshProUGUI>();
            modalDaysText.fontSize = 16;
            modalDaysText.fontStyle = FontStyles.Italic;
            modalDaysText.alignment = TextAlignmentOptions.Center;
            modalDaysText.color = new Color(1f, 0.75f, 0.3f, 1f);

            // Description
            GameObject descObj = new GameObject("Description", typeof(RectTransform), typeof(TextMeshProUGUI));
            descObj.transform.SetParent(cardObj.transform, false);
            RectTransform descRt = descObj.GetComponent<RectTransform>();
            descRt.sizeDelta = new Vector2(480f, 85f);
            descRt.anchoredPosition = new Vector2(0f, 5f);
            modalDescriptionText = descObj.GetComponent<TextMeshProUGUI>();
            modalDescriptionText.fontSize = 16;
            modalDescriptionText.alignment = TextAlignmentOptions.Center;
            modalDescriptionText.enableWordWrapping = true;
            modalDescriptionText.color = new Color(0.9f, 0.9f, 0.95f, 1f);

            // Impact Details Box
            GameObject impactObj = new GameObject("Impact", typeof(RectTransform), typeof(TextMeshProUGUI));
            impactObj.transform.SetParent(cardObj.transform, false);
            RectTransform impactRt = impactObj.GetComponent<RectTransform>();
            impactRt.sizeDelta = new Vector2(480f, 65f);
            impactRt.anchoredPosition = new Vector2(0f, -65f);
            modalImpactText = impactObj.GetComponent<TextMeshProUGUI>();
            modalImpactText.fontSize = 15;
            modalImpactText.fontStyle = FontStyles.Bold;
            modalImpactText.alignment = TextAlignmentOptions.Center;
            modalImpactText.color = new Color(0.5f, 0.85f, 1f, 1f);

            // Close Button
            GameObject closeObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeObj.transform.SetParent(cardObj.transform, false);
            RectTransform closeRt = closeObj.GetComponent<RectTransform>();
            closeRt.sizeDelta = new Vector2(160f, 40f);
            closeRt.anchoredPosition = new Vector2(0f, -135f);
            var closeImg = closeObj.GetComponent<Image>();
            closeImg.color = new Color(0.25f, 0.45f, 0.85f, 1f);

            modalCloseButton = closeObj.GetComponent<Button>();
            modalCloseButton.onClick.AddListener(CloseMarketEventModal);

            GameObject closeTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            closeTxtObj.transform.SetParent(closeObj.transform, false);
            RectTransform closeTxtRt = closeTxtObj.GetComponent<RectTransform>();
            closeTxtRt.sizeDelta = closeRt.sizeDelta;
            closeTxtRt.anchoredPosition = Vector2.zero;
            var closeTxt = closeTxtObj.GetComponent<TextMeshProUGUI>();
            closeTxt.text = "Got It / Close";
            closeTxt.fontSize = 16;
            closeTxt.fontStyle = FontStyles.Bold;
            closeTxt.alignment = TextAlignmentOptions.Center;
            closeTxt.color = Color.white;

            marketEventModal.SetActive(false);
        }

        public void UpdateMarketEventDisplay()
        {
            EnsureMarketEventUI();
            EnsureMarketEventModal();

            if (MarketEventManager.Instance == null || MarketEventManager.Instance.ActiveEvent == null)
            {
                if (marketEventBadgeObj != null) marketEventBadgeObj.SetActive(false);
                if (marketEventModal != null) marketEventModal.SetActive(false);
                return;
            }

            var ev = MarketEventManager.Instance.ActiveEvent;
            if (string.IsNullOrEmpty(ev.title))
            {
                if (marketEventBadgeObj != null) marketEventBadgeObj.SetActive(false);
                return;
            }

            if (marketEventBadgeObj != null)
            {
                bool shouldShowBadge = !isSubscreenActive && GameManager.Instance != null &&
                                       GameManager.Instance.CurrentState != GameState.GameOver &&
                                       GameManager.Instance.CurrentState != GameState.GameWon;
                marketEventBadgeObj.SetActive(shouldShowBadge);
            }

            // Configure Icon and Trend indicator based on event
            ConfigureEventBadge(ev);
        }

        private void ConfigureEventBadge(MarketEvent ev)
        {
            if (marketEventIcon == null || marketEventTrendText == null || marketEventDaysText == null) return;

            marketEventDaysText.text = $"{ev.daysRemaining}d";

            switch (ev.eventId)
            {
                case "tapioca_delay":
                    // Tapioca Pearl Shortage: Tapioca Sprite + Red Down Triangle (shortage / high wholesale price)
                    marketEventIcon.sprite = bobaSprite;
                    marketEventIcon.color = new Color(0.2f, 0.15f, 0.12f, 1f);
                    marketEventTrendText.text = "<color=#FF4D4D><b>▼</b></color>";
                    break;

                case "dairy_surplus":
                    // Local Dairy Surplus: Milk Sprite + Green Down Triangle (discount!)
                    marketEventIcon.sprite = milkSprite != null ? milkSprite : bobaSprite;
                    marketEventIcon.color = new Color(0.85f, 0.95f, 1f, 1f);
                    marketEventTrendText.text = "<color=#2ECC71><b>▼</b></color>";
                    break;

                case "tropical_coconut":
                    // Tropical Coconut Harvest: Jelly Sprite + Green Down Triangle (discount!)
                    marketEventIcon.sprite = jellySprite != null ? jellySprite : bobaSprite;
                    marketEventIcon.color = new Color(0.92f, 0.98f, 1f, 1f);
                    marketEventTrendText.text = "<color=#2ECC71><b>▼</b></color>";
                    break;

                case "cream_shortage":
                    // Gourmet Cream Shortage: Cream Sprite + Red Down Triangle (shortage / tip bonus)
                    marketEventIcon.sprite = milkSprite != null ? milkSprite : bobaSprite;
                    marketEventIcon.color = new Color(1f, 0.92f, 0.7f, 1f);
                    marketEventTrendText.text = "<color=#FF4D4D><b>▼</b></color>";
                    break;

                case "plant_based_craze":
                    // Plant-Based Milk Craze: Oat Milk + Green Up Arrow (demand surge)
                    marketEventIcon.sprite = milkSprite != null ? milkSprite : bobaSprite;
                    marketEventIcon.color = new Color(0.9f, 0.82f, 0.65f, 1f);
                    marketEventTrendText.text = "<color=#2ECC71><b>▲</b></color>";
                    break;

                case "wellness_trend":
                    // Herbal Wellness Trend: Grass Jelly + Green Up Arrow (demand & discount)
                    marketEventIcon.sprite = jellySprite != null ? jellySprite : bobaSprite;
                    marketEventIcon.color = new Color(0.12f, 0.25f, 0.15f, 1f);
                    marketEventTrendText.text = "<color=#2ECC71><b>▲</b></color>";
                    break;

                case "summer_heatwave":
                    // Summer Heatwave: Ice Cube + Orange Up Arrow (ice demand surge)
                    marketEventIcon.sprite = iceSprite;
                    marketEventIcon.color = new Color(0.75f, 0.92f, 1f, 1f);
                    marketEventTrendText.text = "<color=#FFA500><b>▲</b></color>";
                    break;

                case "chilly_rain":
                    // Chilly Monsoon Rain: Ice/Rain + Blue Down Triangle (no ice / hot comfort drinks)
                    marketEventIcon.sprite = iceSprite;
                    marketEventIcon.color = new Color(0.5f, 0.75f, 1f, 1f);
                    marketEventTrendText.text = "<color=#3498DB><b>▼</b></color>";
                    break;

                case "golden_harvest":
                    // Bountiful Foraging Season: Golden Harvest Star
                    marketEventIcon.sprite = bobaSprite;
                    marketEventIcon.color = new Color(1f, 0.85f, 0.2f, 1f);
                    marketEventTrendText.text = "<color=#F1C40F><b>2x★</b></color>";
                    break;

                default:
                    marketEventIcon.sprite = bobaSprite;
                    marketEventIcon.color = Color.white;
                    marketEventTrendText.text = "<color=#FFA500><b>!</b></color>";
                    break;
            }
        }

        public void OpenMarketEventModal()
        {
            if (MarketEventManager.Instance == null || MarketEventManager.Instance.ActiveEvent == null) return;
            EnsureMarketEventModal();

            var ev = MarketEventManager.Instance.ActiveEvent;
            if (modalTitleText != null) modalTitleText.text = ev.title;
            if (modalDaysText != null) modalDaysText.text = $"Duration: {ev.daysRemaining} of {ev.totalDurationDays} days remaining";
            if (modalDescriptionText != null) modalDescriptionText.text = ev.description;

            if (modalImpactText != null)
            {
                modalImpactText.text = ev.eventId switch
                {
                    "tapioca_delay" => "<color=#FF6666>• Wholesale Tapioca Cost: +40%</color>\n<color=#3498DB>• Customer Boba Orders: +50%</color>",
                    "dairy_surplus" => "<color=#2ECC71>• Fresh & Oat Milk Wholesale: -30% (Discount!)</color>\n<color=#3498DB>• Customer Milk Drink Orders: +30%</color>",
                    "tropical_coconut" => "<color=#2ECC71>• Coconut Milk & Jelly Wholesale: -35% (Discount!)</color>\n<color=#3498DB>• Customer Coconut Orders: +40%</color>",
                    "cream_shortage" => "<color=#FF6666>• Cheese Foam & Egg Pudding Wholesale: +30%</color>\n<color=#F1C40F>• Customer Tips on Cream Drinks: +25%</color>",
                    "plant_based_craze" => "<color=#3498DB>• Customer Oat & Plant Milk Orders: +60%</color>",
                    "wellness_trend" => "<color=#2ECC71>• Grass Jelly Wholesale: -15% (Discount!)</color>\n<color=#3498DB>• Grass Jelly Orders: +50% (Low Sugar Preference)</color>",
                    "summer_heatwave" => "<color=#FFA500>• Customer 100% Full Ice Orders: +70%</color>\n<color=#3498DB>• Increased Demand for Popping Boba</color>",
                    "chilly_rain" => "<color=#3498DB>• Customer Preference: 0% Ice (No Ice)</color>\n<color=#3498DB>• Condensed Milk / Hot Comfort Drinks: +40%</color>",
                    "golden_harvest" => "<color=#F1C40F>• Foraging Expeditions Yield 2.0x DOUBLE HARVESTS!</color>",
                    _ => "<color=#80D8FF>• Special Market Conditions Active</color>"
                };
            }

            if (marketEventModal != null)
            {
                marketEventModal.SetActive(true);
                marketEventModal.transform.SetAsLastSibling();
            }
        }

        public void CloseMarketEventModal()
        {
            if (marketEventModal != null)
            {
                marketEventModal.SetActive(false);
            }
        }

        private void UpdateCashDisplay(float cash)
        {
            if (cashText != null) cashText.text = $"${cash:F2}";
        }

        private void UpdateDayDisplay(int day)
        {
            if (dayText != null) dayText.text = $"Day {day}";
            
            if (rentTimerText != null && EconomyManager.Instance != null)
            {
                int daysLeft = EconomyManager.Instance.GetDaysUntilRent(day);
                float rentAmount = EconomyManager.Instance.GetTotalRentDue(day);
                rentTimerText.text = daysLeft == 0 ? $"Rent Due: TONIGHT (${rentAmount:F0})" : $"Rent in: {daysLeft}d (${rentAmount:F0})";
            }
        }

        private void UpdateCustomerCountDisplay(int current, int total)
        {
            if (customerCountText != null)
            {
                customerCountText.text = $"Customer: {current}/{total}";
            }
        }

        public void SetStatusHint(string text)
        {
            if (isSubscreenActive)
            {
                defaultSubscreenHint = text;
            }
            if (statusHintText == null) return;
            if (notificationRoutine != null)
            {
                StopCoroutine(notificationRoutine);
                notificationRoutine = null;
            }
            statusHintText.text = text;
        }

        private bool isSubscreenActive = false;
        private string defaultSubscreenHint = "";

        public void DisableRaycasts()
        {
            var img = GetComponent<Image>();
            if (img != null) img.raycastTarget = false;
            if (statusHintText != null) statusHintText.raycastTarget = false;
            if (dayText != null) dayText.raycastTarget = false;
            if (cashText != null) cashText.raycastTarget = false;
            if (rentTimerText != null) rentTimerText.raycastTarget = false;
            if (customerCountText != null) customerCountText.raycastTarget = false;
        }

        public void BringToFront()
        {
            if (transform.parent != null)
            {
                transform.SetAsLastSibling();
            }
        }

        public void SetSubscreenMode(bool inSubscreen, string persistentHint = "")
        {
            isSubscreenActive = inSubscreen;
            defaultSubscreenHint = persistentHint;

            if (inSubscreen)
            {
                SetHUDDetailsVisible(false);
                if (!string.IsNullOrEmpty(persistentHint) && notificationRoutine == null && statusHintText != null)
                {
                    statusHintText.text = persistentHint;
                }
            }
            else
            {
                if (GameManager.Instance != null)
                {
                    UpdateStateHint(GameManager.Instance.CurrentState);
                }
                UpdateMarketEventDisplay();
            }
        }

        public void ShowNotification(string message, float duration = 2.5f)
        {
            BringToFront();
            if (statusHintText == null) return;
            if (notificationRoutine != null) StopCoroutine(notificationRoutine);
            notificationRoutine = StartCoroutine(NotificationRoutine(message, duration));
        }

        private System.Collections.IEnumerator NotificationRoutine(string message, float duration)
        {
            if (message.Contains("<color"))
            {
                statusHintText.text = $"<b>{message}</b>";
            }
            else
            {
                statusHintText.text = $"<color=#FFAA00><b>{message}</b></color>";
            }

            yield return new WaitForSeconds(duration);
            notificationRoutine = null;

            if (isSubscreenActive)
            {
                SetHUDDetailsVisible(false);
                if (statusHintText != null && !string.IsNullOrEmpty(defaultSubscreenHint))
                {
                    statusHintText.text = defaultSubscreenHint;
                }
                yield break;
            }

            if (CustomerManager.Instance != null && CustomerManager.Instance.CustomerController != null && CustomerManager.Instance.CustomerController.IsLandlordActive)
            {
                statusHintText.text = "The Landlord has arrived to collect weekly rent!";
            }
            else if (GameManager.Instance != null)
            {
                UpdateStateHint(GameManager.Instance.CurrentState);
            }
        }

        public void SetHUDDetailsVisible(bool visible)
        {
            if (dayText != null) dayText.gameObject.SetActive(visible);
            if (cashText != null) cashText.gameObject.SetActive(visible);
            if (rentTimerText != null) rentTimerText.gameObject.SetActive(visible);
            if (customerCountText != null) customerCountText.gameObject.SetActive(visible);
            if (marketEventBadgeObj != null)
            {
                bool hasActiveEvent = MarketEventManager.Instance != null && MarketEventManager.Instance.ActiveEvent != null;
                marketEventBadgeObj.SetActive(visible && hasActiveEvent);
            }
        }

        public void SetStorefrontHUDVisible(bool visible) => SetHUDDetailsVisible(visible);

        public void UpdateStateHint(GameState state)
        {
            if (isSubscreenActive)
            {
                SetHUDDetailsVisible(false);
                if (statusHintText != null && !string.IsNullOrEmpty(defaultSubscreenHint) && notificationRoutine == null)
                {
                    statusHintText.text = defaultSubscreenHint;
                }
                return;
            }

            // HUD details are visible in storefront gameplay and night bedroom hub
            SetHUDDetailsVisible(state != GameState.GameOver && state != GameState.GameWon);

            if (statusHintText == null) return;

            if (CustomerManager.Instance != null && CustomerManager.Instance.CustomerController != null && CustomerManager.Instance.CustomerController.IsLandlordActive)
            {
                statusHintText.text = "The Landlord has arrived to collect weekly rent!";
                return;
            }

            statusHintText.text = state switch
            {
                GameState.MorningPrep => "Open the shutters to begin the day!",
                GameState.ShopOpen => "Ring the bell to call the next customer.",
                GameState.CustomerWaiting => "Prepare the customer's order!",
                GameState.ShopClosing => "All customers served! Pull down the shutter to close.",
                GameState.NightPhase => "Night Phase: Buy stock, forage, and upgrade.",
                GameState.GameOver => "Game Over! You lost the shop.",
                GameState.GameWon => "Victory! You own the shop permanently!",
                _ => ""
            };
        }
    }
}
