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

        public TextMeshProUGUI StatusHintText => statusHintText;

        [Header("Market Event HUD Badge")]
        [SerializeField] private GameObject marketEventBadgeObj;
        [SerializeField] private Button marketEventButton;
        [SerializeField] private Image marketEventIcon;
        [SerializeField] private Image marketEventIcon2;
        [SerializeField] private TextMeshProUGUI marketEventTrendText;
        [SerializeField] private TextMeshProUGUI marketEventDaysText;

        [Header("Market Event Modal Dialog")]
        [SerializeField] private GameObject marketEventModal;
        [SerializeField] private TextMeshProUGUI modalTitleText;
        [SerializeField] private TextMeshProUGUI modalDaysText;
        [SerializeField] private TextMeshProUGUI modalDescriptionText;
        [SerializeField] private TextMeshProUGUI modalImpactText;
        [SerializeField] private Button modalCloseButton;

        [Header("Drink Payout Indicator")]
        [Tooltip("Assign a Panel UI GameObject in the hierarchy (e.g. under HUD / Canvas) to control its position and visual style. The text will be auto-generated inside it if not present.")]
        [SerializeField] private GameObject payoutIndicatorPanel;
        [SerializeField] private TextMeshProUGUI payoutIndicatorText;

        [Header("Floating Cash Gain Delta")]
        [SerializeField] private TextMeshProUGUI cashGainDeltaText;

        [Header("Blitz Mode Duration Bar & Timer")]
        [Tooltip("Root panel for the Blitz Mode duration bar and timer (anchored at bottom center).")]
        [SerializeField] private GameObject blitzTimerPanel;
        [Tooltip("Filled Image component that depletes horizontally as the 30s countdown ticks down.")]
        [SerializeField] private Image blitzTimerFillImage;
        [Tooltip("Background Image track for the duration bar.")]
        [SerializeField] private Image blitzTimerBackgroundImage;
        [Tooltip("TextMeshProUGUI label displaying the countdown time.")]
        [SerializeField] private TextMeshProUGUI blitzTimerText;

        public GameObject PayoutIndicatorPanel
        {
            get => payoutIndicatorPanel;
            set => payoutIndicatorPanel = value;
        }

        public TextMeshProUGUI PayoutIndicatorText
        {
            get => payoutIndicatorText;
            set => payoutIndicatorText = value;
        }

        public TextMeshProUGUI CashGainDeltaText
        {
            get => cashGainDeltaText;
            set => cashGainDeltaText = value;
        }

        public GameObject BlitzTimerPanel
        {
            get => blitzTimerPanel;
            set => blitzTimerPanel = value;
        }

        public Image BlitzTimerFillImage
        {
            get => blitzTimerFillImage;
            set => blitzTimerFillImage = value;
        }

        public Image BlitzTimerBackgroundImage
        {
            get => blitzTimerBackgroundImage;
            set => blitzTimerBackgroundImage = value;
        }

        public TextMeshProUGUI BlitzTimerText
        {
            get => blitzTimerText;
            set => blitzTimerText = value;
        }

        [Header("Quit to Title UI")]
        [Tooltip("Quit button in the shopfront or global HUD.")]
        [SerializeField] private Button quitButton;
        [Tooltip("Quit button in the Night Phase panel (optional if separate).")]
        [SerializeField] private Button nightPhaseQuitButton;

        public Button QuitButton
        {
            get => quitButton;
            set => quitButton = value;
        }

        public Button NightPhaseQuitButton
        {
            get => nightPhaseQuitButton;
            set => nightPhaseQuitButton = value;
        }

        private Coroutine notificationRoutine;
        private Coroutine cashGainRoutine;
        private Vector2 cashGainOriginalPos = new Vector2(-280f, 0f);
        private bool hasCapturedCashGainPos = false;
        private Vector2 dayTextOriginalAnchoredPos = new Vector2(-700f, 0f);
        private bool hasCapturedDayTextPos = false;

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

            if (dayText != null && !hasCapturedDayTextPos)
            {
                dayTextOriginalAnchoredPos = dayText.rectTransform.anchoredPosition;
                hasCapturedDayTextPos = true;
            }
        }

        private void Start()
        {
            DisableRaycasts();
            BringToFront();
            if (dayText != null && !hasCapturedDayTextPos)
            {
                dayTextOriginalAnchoredPos = dayText.rectTransform.anchoredPosition;
                hasCapturedDayTextPos = true;
            }

            EnsureMarketEventUI();
            EnsureMarketEventModal();
            EnsurePayoutIndicatorUI();
            EnsureQuitButtonReferences();

            if (CustomerManager.Instance != null)
            {
                CustomerManager.Instance.OnCustomerArrived -= ShowOrderPayout;
                CustomerManager.Instance.OnCustomerArrived += ShowOrderPayout;
            }

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
                DayManager.Instance.OnDayCompleted += (day, sales, tips) =>
                {
                    RefreshHUDDisplay();
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

        private void EnsureMarketEventUI()
        {
            if (marketEventBadgeObj != null)
            {
                // Ensure correct updated positioning (-705f), transparent background, and 200% scale
                RectTransform existingRt = marketEventBadgeObj.GetComponent<RectTransform>();
                if (existingRt != null)
                {
                    existingRt.anchoredPosition = new Vector2(-705f, 0f);
                    existingRt.localScale = new Vector3(2f, 2f, 1f);
                }
                var existingBg = marketEventBadgeObj.GetComponent<Image>();
                if (existingBg != null) existingBg.color = Color.clear;
                return;
            }

            // Create interactive badge right beside day counter
            marketEventBadgeObj = new GameObject("MarketEventBadge", typeof(RectTransform), typeof(Image), typeof(Button));
            marketEventBadgeObj.transform.SetParent(transform, false);

            RectTransform rt = marketEventBadgeObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(65f, 32f);
            // Positioned at -715f and scaled by 200% (2x)
            rt.anchoredPosition = new Vector2(-715f, 0f);
            rt.localScale = new Vector3(2f, 2f, 1f);

            var bgImg = marketEventBadgeObj.GetComponent<Image>();
            // Transparent background so only the icon shows
            bgImg.color = Color.clear;

            marketEventButton = marketEventBadgeObj.GetComponent<Button>();
            marketEventButton.onClick.AddListener(OpenMarketEventModal);

            // Primary Icon Image
            GameObject iconObj = new GameObject("EventIcon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(marketEventBadgeObj.transform, false);
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.sizeDelta = new Vector2(26f, 26f);
            iconRt.anchoredPosition = new Vector2(0f, 0f);
            marketEventIcon = iconObj.GetComponent<Image>();
            marketEventIcon.raycastTarget = false;
            marketEventIcon.preserveAspect = true;

            // Secondary Icon Image (for dual-icon events like heatwave)
            GameObject icon2Obj = new GameObject("EventIcon2", typeof(RectTransform), typeof(Image));
            icon2Obj.transform.SetParent(marketEventBadgeObj.transform, false);
            RectTransform icon2Rt = icon2Obj.GetComponent<RectTransform>();
            icon2Rt.anchorMin = new Vector2(0f, 0.5f);
            icon2Rt.anchorMax = new Vector2(0f, 0.5f);
            icon2Rt.pivot = new Vector2(0f, 0.5f);
            icon2Rt.sizeDelta = new Vector2(26f, 26f);
            icon2Rt.anchoredPosition = new Vector2(27f, 0f);
            marketEventIcon2 = icon2Obj.GetComponent<Image>();
            marketEventIcon2.raycastTarget = false;
            marketEventIcon2.preserveAspect = true;
            marketEventIcon2.gameObject.SetActive(false);

            // Trend Indicator Text (Red/Green Triangle)
            GameObject trendObj = new GameObject("TrendText", typeof(RectTransform), typeof(TextMeshProUGUI));
            trendObj.transform.SetParent(marketEventBadgeObj.transform, false);
            RectTransform trendRt = trendObj.GetComponent<RectTransform>();
            trendRt.anchorMin = new Vector2(0f, 0.5f);
            trendRt.anchorMax = new Vector2(0f, 0.5f);
            trendRt.pivot = new Vector2(0f, 0.5f);
            trendRt.sizeDelta = new Vector2(18f, 26f);
            trendRt.anchoredPosition = new Vector2(27f, 0f);
            marketEventTrendText = trendObj.GetComponent<TextMeshProUGUI>();
            marketEventTrendText.fontSize = 17;
            marketEventTrendText.alignment = TextAlignmentOptions.Center;
            marketEventTrendText.raycastTarget = false;

            // Days Remaining Text
            GameObject daysObj = new GameObject("DaysText", typeof(RectTransform), typeof(TextMeshProUGUI));
            daysObj.transform.SetParent(marketEventBadgeObj.transform, false);
            RectTransform daysRt = daysObj.GetComponent<RectTransform>();
            daysRt.anchorMin = new Vector2(0f, 0.5f);
            daysRt.anchorMax = new Vector2(1f, 0.5f);
            daysRt.pivot = new Vector2(0f, 0.5f);
            daysRt.sizeDelta = new Vector2(0f, 26f);
            daysRt.anchoredPosition = new Vector2(46f, 0f);
            marketEventDaysText = daysObj.GetComponent<TextMeshProUGUI>();
            marketEventDaysText.fontSize = 12;
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
            cardRt.sizeDelta = new Vector2(900f, 600f);
            cardRt.anchoredPosition = Vector2.zero;

            var cardImg = cardObj.GetComponent<Image>();
            cardImg.color = new Color(0.12f, 0.11f, 0.16f, 0.98f);

            // Header Banner
            GameObject headerObj = new GameObject("Header", typeof(RectTransform), typeof(TextMeshProUGUI));
            headerObj.transform.SetParent(cardObj.transform, false);
            RectTransform headerRt = headerObj.GetComponent<RectTransform>();
            headerRt.sizeDelta = new Vector2(820f, 50f);
            headerRt.anchoredPosition = new Vector2(0f, 235f);
            var headerTmp = headerObj.GetComponent<TextMeshProUGUI>();
            headerTmp.text = "<color=#F1C40F><b>MARKET EVENT ACTIVE</b></color>";
            headerTmp.fontSize = 36;
            headerTmp.alignment = TextAlignmentOptions.Center;

            // Title
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(cardObj.transform, false);
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.sizeDelta = new Vector2(840f, 65f);
            titleRt.anchoredPosition = new Vector2(0f, 170f);
            modalTitleText = titleObj.GetComponent<TextMeshProUGUI>();
            modalTitleText.fontSize = 44;
            modalTitleText.fontStyle = FontStyles.Bold;
            modalTitleText.alignment = TextAlignmentOptions.Center;
            modalTitleText.color = Color.white;

            // Days Duration
            GameObject daysObj = new GameObject("Days", typeof(RectTransform), typeof(TextMeshProUGUI));
            daysObj.transform.SetParent(cardObj.transform, false);
            RectTransform daysRt = daysObj.GetComponent<RectTransform>();
            daysRt.sizeDelta = new Vector2(820f, 45f);
            daysRt.anchoredPosition = new Vector2(0f, 110f);
            modalDaysText = daysObj.GetComponent<TextMeshProUGUI>();
            modalDaysText.fontSize = 30;
            modalDaysText.fontStyle = FontStyles.Italic;
            modalDaysText.alignment = TextAlignmentOptions.Center;
            modalDaysText.color = new Color(1f, 0.75f, 0.3f, 1f);

            // Description
            GameObject descObj = new GameObject("Description", typeof(RectTransform), typeof(TextMeshProUGUI));
            descObj.transform.SetParent(cardObj.transform, false);
            RectTransform descRt = descObj.GetComponent<RectTransform>();
            descRt.sizeDelta = new Vector2(820f, 130f);
            descRt.anchoredPosition = new Vector2(0f, 15f);
            modalDescriptionText = descObj.GetComponent<TextMeshProUGUI>();
            modalDescriptionText.fontSize = 30;
            modalDescriptionText.alignment = TextAlignmentOptions.Center;
            modalDescriptionText.textWrappingMode = TextWrappingModes.Normal;
            modalDescriptionText.color = new Color(0.9f, 0.9f, 0.95f, 1f);

            // Impact Details Box
            GameObject impactObj = new GameObject("Impact", typeof(RectTransform), typeof(TextMeshProUGUI));
            impactObj.transform.SetParent(cardObj.transform, false);
            RectTransform impactRt = impactObj.GetComponent<RectTransform>();
            impactRt.sizeDelta = new Vector2(820f, 110f);
            impactRt.anchoredPosition = new Vector2(0f, -115f);
            modalImpactText = impactObj.GetComponent<TextMeshProUGUI>();
            modalImpactText.fontSize = 28;
            modalImpactText.fontStyle = FontStyles.Bold;
            modalImpactText.alignment = TextAlignmentOptions.Center;
            modalImpactText.color = new Color(0.5f, 0.85f, 1f, 1f);

            // Close Button
            GameObject closeObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeObj.transform.SetParent(cardObj.transform, false);
            RectTransform closeRt = closeObj.GetComponent<RectTransform>();
            closeRt.sizeDelta = new Vector2(260f, 65f);
            closeRt.anchoredPosition = new Vector2(0f, -230f);
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
            closeTxt.fontSize = 30;
            closeTxt.fontStyle = FontStyles.Bold;
            closeTxt.alignment = TextAlignmentOptions.Center;
            closeTxt.color = Color.white;

            marketEventModal.SetActive(false);
        }

        public void UpdateMarketEventDisplay()
        {
            EnsureMarketEventUI();
            EnsureMarketEventModal();

            if (dayText != null && !hasCapturedDayTextPos)
            {
                dayTextOriginalAnchoredPos = dayText.rectTransform.anchoredPosition;
                hasCapturedDayTextPos = true;
            }

            int currentDay = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
            bool isStorefrontOpen = GameManager.Instance != null &&
                                    (GameManager.Instance.CurrentState == GameState.ShopOpen ||
                                     GameManager.Instance.CurrentState == GameState.CustomerWaiting ||
                                     GameManager.Instance.CurrentState == GameState.ShopClosing);

            if (MarketEventManager.Instance == null || MarketEventManager.Instance.ActiveEvent == null ||
                string.IsNullOrEmpty(MarketEventManager.Instance.ActiveEvent.title) ||
                currentDay <= 3 || !isStorefrontOpen)
            {
                if (marketEventBadgeObj != null) marketEventBadgeObj.SetActive(false);
                if (marketEventModal != null) marketEventModal.SetActive(false);
                if (dayText != null) dayText.rectTransform.anchoredPosition = dayTextOriginalAnchoredPos;
                return;
            }

            var ev = MarketEventManager.Instance.ActiveEvent;

            // Configure Icon and Trend indicator based on event
            ConfigureEventBadge(ev);

            // Shift DayText to the left to make room for the badge:
            // Double-icon events shift left more (-85px), single-icon events shift left (-65px)
            if (dayText != null)
            {
                GetMarketEventSprites(ev, out _, out Sprite secSprite);
                float shiftX = (secSprite != null) ? -85f : -65f;
                dayText.rectTransform.anchoredPosition = new Vector2(dayTextOriginalAnchoredPos.x + shiftX, dayTextOriginalAnchoredPos.y);
            }

            // Dynamically dock the market badge right beside the shifted Day counter
            UpdateBadgePositionRelativeToDayText();

            if (marketEventBadgeObj != null)
            {
                bool shouldShowBadge = !isSubscreenActive && isStorefrontOpen;
                marketEventBadgeObj.SetActive(shouldShowBadge);
            }
        }

        private void UpdateBadgePositionRelativeToDayText()
        {
            if (dayText == null || marketEventBadgeObj == null) return;

            RectTransform badgeRt = marketEventBadgeObj.GetComponent<RectTransform>();
            RectTransform dayRt = dayText.rectTransform;

            if (marketEventBadgeObj.transform.parent != dayRt.parent)
            {
                marketEventBadgeObj.transform.SetParent(dayRt.parent, false);
            }

            badgeRt.anchorMin = dayRt.anchorMin;
            badgeRt.anchorMax = dayRt.anchorMax;
            badgeRt.pivot = new Vector2(0f, 0.5f);

            float textWidth = (dayText.preferredWidth > 10f) ? dayText.preferredWidth : 75f;
            float dayRightEdge = dayRt.anchoredPosition.x;

            if (dayRt.pivot.x == 0.5f)
            {
                dayRightEdge += (textWidth * 0.5f);
            }
            else if (dayRt.pivot.x == 0f)
            {
                dayRightEdge += textWidth;
            }

            badgeRt.anchoredPosition = new Vector2(dayRightEdge + 10f, dayRt.anchoredPosition.y);
            badgeRt.localScale = new Vector3(2f, 2f, 1f);
        }

        private Sprite GetIceSprite()
        {
            if (SpriteManager.Instance != null && SpriteManager.Instance.IceCubeSprite != null)
            {
                return SpriteManager.Instance.IceCubeSprite;
            }
            return SpriteManager.Instance?.GetSprite("Ice");
        }

        private Sprite GetBabyYippeeSprite()
        {
            if (SpriteManager.Instance != null && SpriteManager.Instance.BabyYippeeSprite != null)
            {
                return SpriteManager.Instance.BabyYippeeSprite;
            }
            return SpriteManager.Instance?.GetSprite("BabyYippee");
        }

        private Sprite GetIngredientSprite(string key)
        {
            if (SpriteManager.Instance != null)
            {
                var sp = SpriteManager.Instance.GetSprite(key);
                if (sp != null) return sp;
            }
            if (SupermarketViewController.Instance != null)
            {
                var sp = SupermarketViewController.Instance.GetIngredientIcon(key);
                if (sp != null) return sp;
            }
            if (CupStation.Instance != null)
            {
                var sp = key switch
                {
                    "Topping_TapiocaPearls" => CupStation.Instance.TapiocaSprite,
                    "Topping_PoppingBoba" => CupStation.Instance.PoppingBobaSprite,
                    "Topping_GrassJelly" => CupStation.Instance.GrassJellySprite,
                    "Topping_CoconutJelly" => CupStation.Instance.CoconutJellySprite,
                    "Topping_EggPudding" => CupStation.Instance.EggPuddingSprite,
                    "Topping_CheeseFoam" => CupStation.Instance.CheeseFoamSprite,
                    "Topping_GoldenHoneyPearls" => CupStation.Instance.GoldenHoneyPearlsSprite,
                    _ => null
                };
                if (sp != null) return sp;
            }
            return null;
        }

        private void GetMarketEventSprites(MarketEvent ev, out Sprite primary, out Sprite secondary)
        {
            primary = null;
            secondary = null;
            if (ev == null) return;

            switch (ev.eventId)
            {
                case "tapioca_delay":
                    primary = GetIngredientSprite("Topping_TapiocaPearls");
                    break;

                case "dairy_surplus":
                    primary = GetIngredientSprite("Milk_FreshMilk");
                    break;

                case "tropical_coconut":
                    primary = GetIngredientSprite("Milk_CoconutMilk");
                    secondary = GetIngredientSprite("Topping_CoconutJelly");
                    break;

                case "cream_shortage":
                    primary = GetIngredientSprite("Topping_CheeseFoam");
                    secondary = GetIngredientSprite("Topping_EggPudding");
                    break;

                case "plant_based_craze":
                    primary = GetIngredientSprite("Milk_OatMilk");
                    secondary = GetIngredientSprite("Milk_CoconutMilk");
                    break;

                case "wellness_trend":
                    primary = GetIngredientSprite("Topping_GrassJelly");
                    break;

                case "summer_heatwave":
                    primary = GetIceSprite();
                    break;

                case "chilly_rain":
                    primary = GetIceSprite();
                    break;

                case "golden_harvest":
                    primary = GetBabyYippeeSprite();
                    if (primary == null) primary = GetIngredientSprite("Topping_GoldenHoneyPearls");
                    secondary = SpriteManager.Instance != null ? SpriteManager.Instance.RawGoldenDewSprite : GetIngredientSprite("Raw_GoldenDew");
                    break;

                case "stock_clearance":
                    primary = GetIngredientSprite("Topping_TapiocaPearls");
                    secondary = GetIngredientSprite("Milk_FreshMilk");
                    break;
            }

            if (primary == null && !string.IsNullOrEmpty(ev.affectedKey))
            {
                primary = GetIngredientSprite(ev.affectedKey);
            }
        }

        private void ConfigureEventBadge(MarketEvent ev)
        {
            if (marketEventIcon == null || marketEventTrendText == null || marketEventDaysText == null) return;

            marketEventDaysText.text = $"{ev.daysRemaining}d";

            GetMarketEventSprites(ev, out Sprite primarySprite, out Sprite secondarySprite);

            marketEventIcon.sprite = primarySprite;
            marketEventIcon.enabled = (primarySprite != null);
            marketEventIcon.color = Color.white;

            RectTransform badgeRt = marketEventBadgeObj != null ? marketEventBadgeObj.GetComponent<RectTransform>() : null;

            if (secondarySprite != null && marketEventIcon2 != null)
            {
                marketEventIcon2.gameObject.SetActive(true);
                marketEventIcon2.sprite = secondarySprite;
                marketEventIcon2.enabled = true;
                marketEventIcon2.color = Color.white;
                marketEventIcon2.rectTransform.anchoredPosition = new Vector2(27f, 0f);

                marketEventTrendText.rectTransform.anchoredPosition = new Vector2(54f, 0f);
                marketEventDaysText.rectTransform.anchoredPosition = new Vector2(73f, 0f);
                if (badgeRt != null) badgeRt.sizeDelta = new Vector2(92f, 32f);
            }
            else
            {
                if (marketEventIcon2 != null) marketEventIcon2.gameObject.SetActive(false);
                marketEventTrendText.rectTransform.anchoredPosition = new Vector2(27f, 0f);
                marketEventDaysText.rectTransform.anchoredPosition = new Vector2(46f, 0f);
                if (badgeRt != null) badgeRt.sizeDelta = new Vector2(65f, 32f);
            }

            switch (ev.eventId)
            {
                case "chilly_rain":
                case "dairy_surplus":
                case "tropical_coconut":
                case "stock_clearance":
                    marketEventTrendText.text = "<color=#FF4D4D><b>▼</b></color>";
                    break;

                case "tapioca_delay":
                case "cream_shortage":
                case "plant_based_craze":
                case "wellness_trend":
                case "summer_heatwave":
                case "golden_harvest":
                default:
                    marketEventTrendText.text = "<color=#2ECC71><b>▲</b></color>";
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
                    "dairy_surplus" => "<color=#2ECC71>• Fresh & Oat Milk Wholesale: -30% (Discount!)</color>\n<color=#FF6666>• Customer Milk Drink Demand: -35% (Surplus Saturation)</color>",
                    "tropical_coconut" => "<color=#2ECC71>• Coconut Milk & Jelly Wholesale: -35% (Discount!)</color>\n<color=#FF6666>• Customer Coconut Demand: -40% (Harvest Saturation)</color>",
                    "cream_shortage" => "<color=#FF6666>• Cheese Foam & Egg Pudding Wholesale: +30%</color>\n<color=#F1C40F>• Customer Tips on Cream Drinks: +25%</color>",
                    "plant_based_craze" => "<color=#FF6666>• Oat & Coconut Milk Wholesale: +30%</color>\n<color=#3498DB>• Customer Plant Milk Orders: +60%</color>",
                    "wellness_trend" => "<color=#FF6666>• Grass Jelly Wholesale: +30%</color>\n<color=#3498DB>• Grass Jelly Orders: +50% (Low Sugar Preference)</color>",
                    "summer_heatwave" => "<color=#FFA500>• Customer 100% Full Ice Demand: +70%</color>\n<color=#3498DB>• Secret: Heatwave customers crave 100% Full Ice!</color>",
                    "chilly_rain" => "<color=#3498DB>• Customer Preference: 0% Ice (No Ice)</color>\n<color=#3498DB>• Secret: Freezing customers crave 0% No Ice!</color>",
                    "golden_harvest" => "<color=#F1C40F>• Foraging Expeditions Yield 2.0x DOUBLE HARVESTS!</color>",
                    "stock_clearance" => "<color=#2ECC71>• ALL Wholesale Market Stock: -70% MEGA CLEARANCE!</color>\n<color=#F1C40F>• Best time to stock up at the Wholesale Supermarket!</color>",
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
            if (GameManager.Instance != null && GameManager.Instance.IsCasualMode)
            {
                if (cashText != null) cashText.text = "<color=#2ECC71><b>Cash: ∞</b></color>";
                return;
            }
            if (cashText != null) cashText.text = $"${cash:F2}";
        }

        public void RefreshHUDDisplay()
        {
            if (DayManager.Instance != null)
            {
                int dayToShow = (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.NightPhase)
                    ? DayManager.Instance.LastCompletedDay
                    : DayManager.Instance.CurrentDay;
                UpdateDayDisplay(Mathf.Max(1, dayToShow));
                UpdateCustomerCountDisplay(DayManager.Instance.CurrentCustomerIndex, DayManager.Instance.TotalCustomersToday);
            }
            if (EconomyManager.Instance != null)
            {
                UpdateCashDisplay(EconomyManager.Instance.CurrentCash);
            }
            UpdateMarketEventDisplay();
        }

        public void UpdateDayDisplay(int day)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsCasualMode)
            {
                if (dayText != null) dayText.text = "<color=#00E5FF><b>Casual</b></color>";
                if (rentTimerText != null) rentTimerText.text = "<color=#F1C40F><b>No Rush 🌸</b></color>";
                return;
            }

            if (dayText != null) dayText.text = $"Day {day}";
            
            if (rentTimerText != null && EconomyManager.Instance != null)
            {
                int daysLeft = EconomyManager.Instance.GetDaysUntilRent(day);
                float rentAmount = EconomyManager.Instance.GetTotalRentDue(day);
                bool isEndless = EconomyManager.Instance.IsEndlessMode;
                string label = isEndless ? "Royalty" : "Rent";
                rentTimerText.text = daysLeft == 0 ? $"{label} Due: TONIGHT (${rentAmount:F0})" : $"{label} in: {daysLeft}d (${rentAmount:F0})";
            }
        }

        private void UpdateCustomerCountDisplay(int current, int total)
        {
            if (customerCountText != null)
            {
                if (GameManager.Instance != null && (GameManager.Instance.IsBlitzMode || GameManager.Instance.IsCasualMode))
                {
                    int served = DayManager.Instance != null ? DayManager.Instance.CustomersServedToday : 0;
                    customerCountText.text = $"Served: {served}";
                }
                else
                {
                    customerCountText.text = $"Customer: {current}/{total}";
                }
            }
        }

        private static Sprite whiteFillSprite = null;
        private static Sprite GetWhiteFillSprite()
        {
            if (whiteFillSprite == null)
            {
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                Color[] colors = new Color[] { Color.white, Color.white, Color.white, Color.white };
                tex.SetPixels(colors);
                tex.Apply();
                whiteFillSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
            }
            return whiteFillSprite;
        }

        public void EnsureBlitzTimerUI()
        {
            if (blitzTimerPanel != null && blitzTimerFillImage != null && blitzTimerText != null) return;

            Canvas rootCanvas = GetComponentInParent<Canvas>();
            Transform targetParent = rootCanvas != null ? rootCanvas.transform : transform;

            // 1. Root Panel at bottom of screen
            if (blitzTimerPanel == null)
            {
                Transform existing = targetParent.Find("BlitzTimerDurationBarPanel") ?? targetParent.Find("BlitzTimerPanel");
                if (existing != null)
                {
                    blitzTimerPanel = existing.gameObject;
                }
                else
                {
                    blitzTimerPanel = new GameObject("BlitzTimerDurationBarPanel", typeof(RectTransform), typeof(Image));
                    blitzTimerPanel.transform.SetParent(targetParent, false);

                    var panelRt = blitzTimerPanel.GetComponent<RectTransform>();
                    panelRt.anchorMin = new Vector2(0.5f, 0f);
                    panelRt.anchorMax = new Vector2(0.5f, 0f);
                    panelRt.pivot = new Vector2(0.5f, 0f);
                    panelRt.anchoredPosition = new Vector2(0f, 20f);
                    panelRt.sizeDelta = new Vector2(580f, 36f);

                    blitzTimerBackgroundImage = blitzTimerPanel.GetComponent<Image>();
                    blitzTimerBackgroundImage.color = new Color(0.06f, 0.06f, 0.10f, 0.92f);
                    blitzTimerBackgroundImage.raycastTarget = false;
                }
            }

            // 2. Depleting Fill Image
            if (blitzTimerFillImage == null && blitzTimerPanel != null)
            {
                Transform fillObj = blitzTimerPanel.transform.Find("FillImage");
                if (fillObj != null && fillObj.TryGetComponent<Image>(out var foundFill))
                {
                    blitzTimerFillImage = foundFill;
                }
                else
                {
                    GameObject fillGo = new GameObject("FillImage", typeof(RectTransform), typeof(Image));
                    fillGo.transform.SetParent(blitzTimerPanel.transform, false);

                    var fillRt = fillGo.GetComponent<RectTransform>();
                    fillRt.anchorMin = Vector2.zero;
                    fillRt.anchorMax = Vector2.one;
                    fillRt.offsetMin = new Vector2(4f, 4f);
                    fillRt.offsetMax = new Vector2(-4f, -4f);

                    blitzTimerFillImage = fillGo.GetComponent<Image>();
                    blitzTimerFillImage.sprite = GetWhiteFillSprite();
                    blitzTimerFillImage.type = Image.Type.Filled;
                    blitzTimerFillImage.fillMethod = Image.FillMethod.Horizontal;
                    blitzTimerFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                    blitzTimerFillImage.color = new Color(0.18f, 0.80f, 0.44f, 0.95f);
                    blitzTimerFillImage.raycastTarget = false;
                }
            }

            // 3. Countdown Text Label centered inside bar
            if (blitzTimerText == null && blitzTimerPanel != null)
            {
                Transform textObj = blitzTimerPanel.transform.Find("TimerText");
                if (textObj != null && textObj.TryGetComponent<TextMeshProUGUI>(out var foundText))
                {
                    blitzTimerText = foundText;
                }
                else
                {
                    GameObject textGo = new GameObject("TimerText", typeof(RectTransform), typeof(TextMeshProUGUI));
                    textGo.transform.SetParent(blitzTimerPanel.transform, false);

                    var textRt = textGo.GetComponent<RectTransform>();
                    textRt.anchorMin = Vector2.zero;
                    textRt.anchorMax = Vector2.one;
                    textRt.offsetMin = Vector2.zero;
                    textRt.offsetMax = Vector2.zero;

                    blitzTimerText = textGo.GetComponent<TextMeshProUGUI>();
                    blitzTimerText.fontSize = 20;
                    blitzTimerText.fontStyle = FontStyles.Bold;
                    blitzTimerText.alignment = TextAlignmentOptions.Center;
                    blitzTimerText.color = Color.white;
                    blitzTimerText.raycastTarget = false;
                }
            }

            if (blitzTimerPanel != null)
            {
                blitzTimerPanel.SetActive(false);
            }
        }

        public void UpdateBlitzTimer(float secondsRemaining)
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsBlitzMode)
            {
                if (blitzTimerPanel != null) blitzTimerPanel.SetActive(false);
                return;
            }

            EnsureBlitzTimerUI();
            if (blitzTimerPanel == null) return;

            bool isStorefrontOpen = GameManager.Instance.CurrentState == GameState.ShopOpen ||
                                    GameManager.Instance.CurrentState == GameState.CustomerWaiting ||
                                    GameManager.Instance.CurrentState == GameState.DrinkBrewing ||
                                    GameManager.Instance.CurrentState == GameState.CustomerReacting;

            blitzTimerPanel.SetActive(isStorefrontOpen && !isSubscreenActive);

            float totalDuration = GameManager.DefaultBlitzDayDuration;
            float fillRatio = Mathf.Clamp01(secondsRemaining / totalDuration);

            // 1. Update horizontal depletion fill
            if (blitzTimerFillImage != null)
            {
                blitzTimerFillImage.fillAmount = fillRatio;

                // Dynamic gradient color: Green (>25s) -> Gold (10-25s) -> Red (<=10s)
                if (secondsRemaining > 25f)
                {
                    float t = Mathf.Clamp01((secondsRemaining - 25f) / 35f);
                    blitzTimerFillImage.color = Color.Lerp(new Color(1f, 0.84f, 0f, 0.95f), new Color(0.18f, 0.80f, 0.44f, 0.95f), t);
                }
                else if (secondsRemaining > 10f)
                {
                    float t = Mathf.Clamp01((secondsRemaining - 10f) / 15f);
                    blitzTimerFillImage.color = Color.Lerp(new Color(1f, 0.28f, 0.28f, 0.95f), new Color(1f, 0.84f, 0f, 0.95f), t);
                }
                else
                {
                    // Urgency pulse when under 10 seconds (faster pulse under 5s)
                    float freq = (secondsRemaining <= 5f) ? 12f : 6f;
                    float pulse = 0.82f + 0.18f * Mathf.Sin(Time.unscaledTime * freq);
                    blitzTimerFillImage.color = new Color(1f, 0.20f, 0.20f, pulse);
                }
            }

            // 2. Update Countdown Text
            if (blitzTimerText != null)
            {
                int sec = Mathf.CeilToInt(secondsRemaining);
                if (secondsRemaining <= 0f)
                {
                    blitzTimerText.text = "<color=#FF4D4D><b>TIME'S UP!</b></color>";
                }
                else if (secondsRemaining <= 5f)
                {
                    blitzTimerText.text = $"<color=#FFFFFF><b>HURRY! {secondsRemaining:F1}s</b></color>";
                }
                else
                {
                    blitzTimerText.text = $"TIME REMAINING: {sec:D2}s";
                }
            }

            if (customerCountText != null)
            {
                int served = DayManager.Instance != null ? DayManager.Instance.CustomersServedToday : 0;
                customerCountText.text = $"Served: {served}";
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

                // If Title Screen is active, keep Title Screen on top of HUD
                if (TitleScreenController.Instance != null && TitleScreenController.Instance.IsTitleScreenActive)
                {
                    TitleScreenController.Instance.BringToFront();
                }
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
                bool isEndless = EconomyManager.Instance != null && EconomyManager.Instance.IsEndlessMode;
                statusHintText.text = isEndless ? "Chairwoman Chubi has arrived!" : "The Landlady has arrived!";
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
                bool isStorefrontOpen = GameManager.Instance != null &&
                                        (GameManager.Instance.CurrentState == GameState.ShopOpen ||
                                         GameManager.Instance.CurrentState == GameState.CustomerWaiting ||
                                         GameManager.Instance.CurrentState == GameState.ShopClosing);
                marketEventBadgeObj.SetActive(visible && hasActiveEvent && isStorefrontOpen && !isSubscreenActive);
            }
            if (!visible)
            {
                HideOrderPayout();
            }
        }

        public void SetStorefrontHUDVisible(bool visible) => SetHUDDetailsVisible(visible);

        public void EnsurePayoutIndicatorUI()
        {
            // 1. If user assigned a Panel in the inspector, use it directly
            if (payoutIndicatorPanel != null)
            {
                if (payoutIndicatorText == null)
                {
                    payoutIndicatorText = payoutIndicatorPanel.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (payoutIndicatorText == null)
                    {
                        GameObject textObj = new GameObject("PayoutText", typeof(RectTransform), typeof(TextMeshProUGUI));
                        textObj.transform.SetParent(payoutIndicatorPanel.transform, false);

                        var textRt = textObj.GetComponent<RectTransform>();
                        textRt.anchorMin = Vector2.zero;
                        textRt.anchorMax = Vector2.one;
                        textRt.offsetMin = new Vector2(16f, 0f);
                        textRt.offsetMax = new Vector2(-16f, 0f);

                        payoutIndicatorText = textObj.GetComponent<TextMeshProUGUI>();
                        payoutIndicatorText.fontSize = 36;
                        payoutIndicatorText.fontStyle = FontStyles.Bold;
                        payoutIndicatorText.alignment = TextAlignmentOptions.Center;
                        payoutIndicatorText.textWrappingMode = TextWrappingModes.NoWrap;
                        payoutIndicatorText.raycastTarget = false;
                    }
                }
                else
                {
                    payoutIndicatorText.fontSize = 36;
                }
                return;
            }

            // 2. Auto-discovery: Look for a child panel in HUD or on the Canvas root
            Transform existing = transform.Find("PayoutIndicatorPanel") ?? transform.Find("PayoutPanel");
            if (existing == null)
            {
                Canvas rootCanvas = GetComponentInParent<Canvas>();
                if (rootCanvas != null)
                {
                    existing = rootCanvas.transform.Find("PayoutIndicatorPanel") ?? rootCanvas.transform.Find("PayoutPanel");
                }
            }

            if (existing != null)
            {
                payoutIndicatorPanel = existing.gameObject;
                payoutIndicatorText = payoutIndicatorPanel.GetComponentInChildren<TextMeshProUGUI>(true);
                if (payoutIndicatorText == null)
                {
                    GameObject textObj = new GameObject("PayoutText", typeof(RectTransform), typeof(TextMeshProUGUI));
                    textObj.transform.SetParent(payoutIndicatorPanel.transform, false);

                    var textRt = textObj.GetComponent<RectTransform>();
                    textRt.anchorMin = Vector2.zero;
                    textRt.anchorMax = Vector2.one;
                    textRt.offsetMin = new Vector2(16f, 0f);
                    textRt.offsetMax = new Vector2(-16f, 0f);

                    payoutIndicatorText = textObj.GetComponent<TextMeshProUGUI>();
                    payoutIndicatorText.fontSize = 36;
                    payoutIndicatorText.fontStyle = FontStyles.Bold;
                    payoutIndicatorText.alignment = TextAlignmentOptions.Center;
                    payoutIndicatorText.textWrappingMode = TextWrappingModes.NoWrap;
                    payoutIndicatorText.raycastTarget = false;
                }
                else
                {
                    payoutIndicatorText.fontSize = 36;
                }
                return;
            }

            // 3. Fallback: Automatically generate a default panel under canvas
            Canvas canvas = GetComponentInParent<Canvas>();
            Transform targetParent = (canvas != null) ? canvas.transform : transform;

            payoutIndicatorPanel = new GameObject("PayoutIndicatorPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            payoutIndicatorPanel.transform.SetParent(targetParent, false);

            var rt = payoutIndicatorPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 15f);
            rt.sizeDelta = new Vector2(820f, 56f);

            var img = payoutIndicatorPanel.GetComponent<Image>();
            img.color = new Color(0.08f, 0.08f, 0.12f, 0.88f);
            img.raycastTarget = false;

            GameObject fallbackTextObj = new GameObject("PayoutText", typeof(RectTransform), typeof(TextMeshProUGUI));
            fallbackTextObj.transform.SetParent(payoutIndicatorPanel.transform, false);

            var fallbackTextRt = fallbackTextObj.GetComponent<RectTransform>();
            fallbackTextRt.anchorMin = Vector2.zero;
            fallbackTextRt.anchorMax = Vector2.one;
            fallbackTextRt.offsetMin = new Vector2(16f, 0f);
            fallbackTextRt.offsetMax = new Vector2(-16f, 0f);

            payoutIndicatorText = fallbackTextObj.GetComponent<TextMeshProUGUI>();
            payoutIndicatorText.fontSize = 36;
            payoutIndicatorText.fontStyle = FontStyles.Bold;
            payoutIndicatorText.alignment = TextAlignmentOptions.Center;
            payoutIndicatorText.textWrappingMode = TextWrappingModes.NoWrap;
            payoutIndicatorText.raycastTarget = false;

            payoutIndicatorPanel.SetActive(false);
        }

        public void EnsureCashGainUI()
        {
            if (cashGainDeltaText != null)
            {
                if (!hasCapturedCashGainPos)
                {
                    cashGainOriginalPos = cashGainDeltaText.rectTransform.anchoredPosition;
                    hasCapturedCashGainPos = true;
                }
                return;
            }

            Transform targetParent = (cashText != null) ? cashText.transform.parent : transform;
            Transform existing = targetParent.Find("CashGainDeltaText");
            if (existing != null)
            {
                cashGainDeltaText = existing.GetComponent<TextMeshProUGUI>();
                if (cashGainDeltaText != null)
                {
                    cashGainOriginalPos = cashGainDeltaText.rectTransform.anchoredPosition;
                    hasCapturedCashGainPos = true;
                    return;
                }
            }

            GameObject deltaObj = new GameObject("CashGainDeltaText", typeof(RectTransform), typeof(TextMeshProUGUI));
            deltaObj.transform.SetParent(targetParent, false);

            var rt = deltaObj.GetComponent<RectTransform>();
            rt.anchorMin = (cashText != null) ? cashText.rectTransform.anchorMin : new Vector2(0.5f, 0.5f);
            rt.anchorMax = (cashText != null) ? cashText.rectTransform.anchorMax : new Vector2(0.5f, 0.5f);
            rt.pivot = (cashText != null) ? cashText.rectTransform.pivot : new Vector2(0.5f, 0.5f);

            Vector2 basePos = (cashText != null) ? cashText.rectTransform.anchoredPosition + new Vector2(110f, 0f) : new Vector2(-280f, 0f);
            rt.anchoredPosition = basePos;
            rt.sizeDelta = new Vector2(160f, 40f);
            cashGainOriginalPos = basePos;
            hasCapturedCashGainPos = true;

            cashGainDeltaText = deltaObj.GetComponent<TextMeshProUGUI>();
            cashGainDeltaText.fontSize = 24;
            cashGainDeltaText.fontStyle = FontStyles.Bold;
            cashGainDeltaText.color = new Color(0.18f, 0.90f, 0.44f, 1f); // #2ECC71
            cashGainDeltaText.alignment = TextAlignmentOptions.Left;
            cashGainDeltaText.raycastTarget = false;

            deltaObj.SetActive(false);
        }

        public void ShowFloatingCashGain(float amount)
        {
            if (amount <= 0 || (GameManager.Instance != null && GameManager.Instance.IsCasualMode)) return;

            EnsureCashGainUI();
            if (cashGainDeltaText == null) return;

            if (cashGainRoutine != null)
            {
                StopCoroutine(cashGainRoutine);
            }
            cashGainRoutine = StartCoroutine(CashGainFloatRoutine(amount));
        }

        private IEnumerator CashGainFloatRoutine(float amount)
        {
            cashGainDeltaText.gameObject.SetActive(true);
            cashGainDeltaText.text = $"+${amount:F2}";
            cashGainDeltaText.color = new Color(0.18f, 0.90f, 0.44f, 1f);

            RectTransform rt = cashGainDeltaText.rectTransform;
            Vector2 startPos = cashGainOriginalPos;
            Vector2 targetPos = startPos + new Vector2(0f, 20f);

            float duration = 1.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Gentle upward float
                rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);

                // Pop scale in first 0.15s
                if (t < 0.15f)
                {
                    float pop = Mathf.Lerp(1.35f, 1.0f, t / 0.15f);
                    rt.localScale = new Vector3(pop, pop, 1f);
                }
                else
                {
                    rt.localScale = Vector3.one;
                }

                // Smooth fade out in last 0.5s
                if (t > 0.65f)
                {
                    float alpha = Mathf.Lerp(1f, 0f, (t - 0.65f) / 0.35f);
                    cashGainDeltaText.color = new Color(0.18f, 0.90f, 0.44f, alpha);
                }

                yield return null;
            }

            rt.anchoredPosition = startPos;
            rt.localScale = Vector3.one;
            cashGainDeltaText.gameObject.SetActive(false);
            cashGainRoutine = null;
        }

        private DrinkOrder activeDisplayedOrder = null;
        private bool isAwaitingQuitConfirmation = false;
        private float quitConfirmationTimer = 0f;
        private string defaultQuitText = "";
        private string defaultNightQuitText = "";

        private void Update()
        {
            if (activeDisplayedOrder != null)
            {
                UpdateOrderPayoutDisplay();
            }

            if (isAwaitingQuitConfirmation)
            {
                quitConfirmationTimer -= Time.unscaledDeltaTime;
                if (quitConfirmationTimer <= 0f)
                {
                    isAwaitingQuitConfirmation = false;
                    ResetQuitButtonLabels();
                }
            }

            UpdateQuitButtonsVisibility();
        }

        public void EnsureQuitButtonReferences()
        {
            // Auto-discover quit button in shopfront/HUD if unassigned
            if (quitButton == null)
            {
                quitButton = FindButtonInHierarchy("QuitButton", "QuitBtn", "BtnQuit", "QuitToTitleButton", "QuitToTitleBtn", "ExitButton", "ExitBtn", "Quit", "Quit Button", "Exit Button");
            }

            // Auto-discover night quit button if unassigned
            if (nightPhaseQuitButton == null && NightPhaseManager.Instance != null && NightPhaseManager.Instance.NightPanelRoot != null)
            {
                nightPhaseQuitButton = NightPhaseManager.Instance.QuitToTitleButton ??
                                       FindButtonInRoot(NightPhaseManager.Instance.NightPanelRoot.transform, "QuitButton", "QuitBtn", "BtnQuit", "NightQuitBtn", "QuitToTitleButton", "ExitButton", "ExitBtn", "Quit", "Quit Button");
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(OnQuitButtonClicked);
                quitButton.onClick.AddListener(OnQuitButtonClicked);

                var tmp = quitButton.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null && string.IsNullOrEmpty(defaultQuitText))
                {
                    defaultQuitText = tmp.text;
                }
            }

            if (nightPhaseQuitButton != null)
            {
                nightPhaseQuitButton.onClick.RemoveListener(OnQuitButtonClicked);
                nightPhaseQuitButton.onClick.AddListener(OnQuitButtonClicked);

                var tmp = nightPhaseQuitButton.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null && string.IsNullOrEmpty(defaultNightQuitText))
                {
                    defaultNightQuitText = tmp.text;
                }
            }
        }

        private bool IsTitleScreenElement(Component c)
        {
            if (c == null) return false;
            if (TitleScreenController.Instance != null && c.transform.IsChildOf(TitleScreenController.Instance.transform)) return true;
            if (c.name.StartsWith("TitleScreen_", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private Button FindButtonInHierarchy(params string[] names)
        {
            var allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            // 1. Exact match on provided names
            foreach (var b in allButtons)
            {
                if (IsTitleScreenElement(b)) continue;

                foreach (var name in names)
                {
                    if (b.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        return b;
                    }
                }
            }

            // 2. Fuzzy match on button GameObject name
            foreach (var b in allButtons)
            {
                if (IsTitleScreenElement(b)) continue;

                string cleaned = b.name.ToLower().Replace(" ", "").Replace("_", "").Replace("-", "");
                if (cleaned.Contains("quit") || cleaned.Contains("quittotitle") || cleaned.Contains("exittotitle"))
                {
                    return b;
                }
            }

            // 3. Match on child TextMeshProUGUI text label
            foreach (var b in allButtons)
            {
                if (IsTitleScreenElement(b)) continue;

                var tmp = b.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp != null && !string.IsNullOrEmpty(tmp.text))
                {
                    string t = tmp.text.Trim().ToLower();
                    if (t == "quit" || t == "exit" || t == "quit game" || t == "quit to title" || t == "back to title")
                    {
                        return b;
                    }
                }
            }

            return null;
        }

        private Button FindButtonInRoot(Transform root, params string[] names)
        {
            if (root == null) return null;
            var buttons = root.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                foreach (var name in names)
                {
                    if (b.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        return b;
                    }
                }
            }

            // Fuzzy fallback in root
            foreach (var b in buttons)
            {
                string cleaned = b.name.ToLower().Replace(" ", "").Replace("_", "").Replace("-", "");
                if (cleaned.Contains("quit") || cleaned.Contains("exit"))
                {
                    return b;
                }
            }
            return null;
        }

        public void OnQuitButtonClicked()
        {
            if (isAwaitingQuitConfirmation && quitConfirmationTimer > 0f)
            {
                // 2nd click: Confirm quit and return to title screen
                isAwaitingQuitConfirmation = false;
                quitConfirmationTimer = 0f;
                ResetQuitButtonLabels();
                GameManager.Instance?.ReturnToTitleScreen(reloadScene: true);
            }
            else
            {
                // 1st click: Prompt confirmation message
                isAwaitingQuitConfirmation = true;
                quitConfirmationTimer = 4.0f;

                SetStatusHint("Are you sure you want to quit? Click Quit again to return to Title Screen.");
                ShowNotification("Are you sure you want to quit? Click Quit again to return to Title Screen.", 3.5f);

                SetQuitButtonLabel("Confirm Quit?");
            }
        }

        private void SetQuitButtonLabel(string label)
        {
            if (quitButton != null)
            {
                var tmp = quitButton.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                {
                    if (string.IsNullOrEmpty(defaultQuitText)) defaultQuitText = tmp.text;
                    tmp.text = $"<color=#FF6B6B><b>{label}</b></color>";
                }
            }
            if (nightPhaseQuitButton != null)
            {
                var tmp = nightPhaseQuitButton.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                {
                    if (string.IsNullOrEmpty(defaultNightQuitText)) defaultNightQuitText = tmp.text;
                    tmp.text = $"<color=#FF6B6B><b>{label}</b></color>";
                }
            }
        }

        private void ResetQuitButtonLabels()
        {
            if (quitButton != null && !string.IsNullOrEmpty(defaultQuitText))
            {
                var tmp = quitButton.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = defaultQuitText;
            }
            if (nightPhaseQuitButton != null && !string.IsNullOrEmpty(defaultNightQuitText))
            {
                var tmp = nightPhaseQuitButton.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = defaultNightQuitText;
            }
        }

        public void UpdateQuitButtonsVisibility()
        {
            if (quitButton == null)
            {
                EnsureQuitButtonReferences();
            }

            bool isTitleActive = (TitleScreenController.Instance != null && TitleScreenController.Instance.IsTitleScreenActive);

            bool isServeButtonActive = false;
            if (CupStation.Instance != null && CupStation.Instance.ServeCupButton != null)
            {
                isServeButtonActive = CupStation.Instance.ServeCupButton.gameObject.activeInHierarchy;
            }
            else
            {
                // If serve button reference isn't hooked yet, check if storefront is running during the day
                isServeButtonActive = (GameManager.Instance != null && GameManager.Instance.IsGameStarted &&
                                       GameManager.Instance.CurrentState != GameState.NightPhase &&
                                       GameManager.Instance.CurrentState != GameState.GameOver &&
                                       GameManager.Instance.CurrentState != GameState.GameWon);
            }

            bool isNightActive = (NightPhaseManager.Instance != null &&
                                  NightPhaseManager.Instance.NightPanelRoot != null &&
                                  NightPhaseManager.Instance.NightPanelRoot.activeInHierarchy) ||
                                 (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.NightPhase);

            if (quitButton != null)
            {
                bool shouldShow = !isTitleActive && (isServeButtonActive || isNightActive);
                if (quitButton.gameObject.activeSelf != shouldShow)
                {
                    quitButton.gameObject.SetActive(shouldShow);
                }
            }

            if (nightPhaseQuitButton != null && nightPhaseQuitButton != quitButton)
            {
                bool shouldShow = !isTitleActive && isNightActive;
                if (nightPhaseQuitButton.gameObject.activeSelf != shouldShow)
                {
                    nightPhaseQuitButton.gameObject.SetActive(shouldShow);
                }
            }
        }

        public void ShowOrderPayout(DrinkOrder order)
        {
            if (order == null || isSubscreenActive)
            {
                HideOrderPayout();
                return;
            }

            activeDisplayedOrder = order;
            EnsurePayoutIndicatorUI();
            UpdateOrderPayoutDisplay();
        }

        private void UpdateOrderPayoutDisplay()
        {
            if (activeDisplayedOrder == null || isSubscreenActive)
            {
                if (payoutIndicatorPanel != null && payoutIndicatorPanel.activeSelf)
                {
                    payoutIndicatorPanel.SetActive(false);
                }
                return;
            }

            EnsurePayoutIndicatorUI();

            if (GameManager.Instance != null && GameManager.Instance.IsCasualMode)
            {
                if (payoutIndicatorText != null)
                {
                    BubbleTeaCup c = CupStation.Instance != null ? CupStation.Instance.CurrentCup : null;
                    if (c != null && c.hasCup)
                    {
                        var eval = c.Evaluate(activeDisplayedOrder, 1.0f);
                        string starStr = new string('★', eval.stars) + new string('☆', 5 - eval.stars);
                        string starCol = eval.stars >= 4 ? "2ECC71" : (eval.stars == 3 ? "FFD700" : "FF6B6B");
                        payoutIndicatorText.text = $"<color=#BDC3C7>Drink Preview:</color>  <color=#{starCol}><b>{starStr} ({eval.stars}/5)</b></color>  <color=#7F8C8D>•</color>  <color=#00E5FF>Casual Brewing</color>  <color=#7F8C8D>•</color>  <color=#2ECC71>Take Your Time</color>";
                    }
                    else
                    {
                        payoutIndicatorText.text = "<color=#BDC3C7>Order Preview:</color>  <color=#00E5FF>Ready to Brew</color>  <color=#7F8C8D>•</color>  <color=#2ECC71>No Time Limits</color>  <color=#7F8C8D>•</color>  <color=#F1C40F>Enjoy at Your Pace</color>";
                    }
                }

                if (payoutIndicatorPanel != null && !payoutIndicatorPanel.activeSelf)
                {
                    payoutIndicatorPanel.SetActive(true);
                    payoutIndicatorPanel.transform.SetAsLastSibling();
                }
                return;
            }

            float basePrice = (float)(Math.Round(activeDisplayedOrder.basePrice * 10.0, MidpointRounding.AwayFromZero) / 10.0);
            float minPrice = (float)(Math.Round((basePrice * 0.30f) * 10.0, MidpointRounding.AwayFromZero) / 10.0);

            bool hasLuckyCat = UpgradeManager.Instance != null && UpgradeManager.Instance.HasUpgrade(UpgradeType.LuckyCat);
            float marketTipMultiplier = 1.0f;
            if (MarketEventManager.Instance != null && MarketEventManager.Instance.ActiveEvent?.eventId == "cream_shortage")
            {
                marketTipMultiplier = 1.25f;
            }
            // Base tip 10% + Max speed tip 30% = 40% tip (multiplied by Lucky Cat and market event multipliers)
            float maxTipMultiplier = 0.40f * (hasLuckyCat ? 1.30f : 1.0f) * marketTipMultiplier;
            float maxTip = (float)(Math.Round((basePrice * maxTipMultiplier) * 10.0, MidpointRounding.AwayFromZero) / 10.0);
            float maxPrice = basePrice + maxTip;

            // Calculate current payout if served right at this second with the current cup ingredients and patience
            float currentPatience = 1f;
            if (CustomerManager.Instance != null && CustomerManager.Instance.CustomerController != null)
            {
                currentPatience = CustomerManager.Instance.CustomerController.PatiencePercent;
            }

            float currentPayout = minPrice;
            BubbleTeaCup cup = CupStation.Instance != null ? CupStation.Instance.CurrentCup : null;
            if (cup != null)
            {
                var eval = cup.Evaluate(activeDisplayedOrder, currentPatience);
                currentPayout = (float)(Math.Round((eval.earnedMoney + eval.tip) * 10.0, MidpointRounding.AwayFromZero) / 10.0);
            }

            if (payoutIndicatorText != null)
            {
                string currentColorHex = GetPayoutColorHex(currentPayout, minPrice, maxPrice);
                payoutIndicatorText.text = $"<color=#BDC3C7>Payout:</color>  <color=#FF6B6B>Min: ${minPrice:F2}</color>  <color=#7F8C8D>•</color>  <color=#{currentColorHex}>Current: ${currentPayout:F2}</color>  <color=#7F8C8D>•</color>  <color=#2ECC71>Max: ${maxPrice:F2}</color>";
            }

            if (payoutIndicatorPanel != null && !payoutIndicatorPanel.activeSelf)
            {
                payoutIndicatorPanel.SetActive(true);
                payoutIndicatorPanel.transform.SetAsLastSibling();
            }
        }

        private string GetPayoutColorHex(float current, float min, float max)
        {
            if (max <= min) return "2ECC71";
            float t = Mathf.Clamp01((current - min) / (max - min));

            // Multi-stop smooth gradient: Red (#FF6B6B) -> Amber/Gold (#FFD700) -> Vibrant Green (#2ECC71)
            Color minCol = new Color(1.0f, 0.42f, 0.42f);     // #FF6B6B
            Color midCol = new Color(1.0f, 0.84f, 0.0f);      // #FFD700
            Color maxCol = new Color(0.18f, 0.80f, 0.44f);    // #2ECC71

            Color result = (t < 0.5f) 
                ? Color.Lerp(minCol, midCol, t * 2f) 
                : Color.Lerp(midCol, maxCol, (t - 0.5f) * 2f);

            return ColorUtility.ToHtmlStringRGB(result);
        }

        public void HideOrderPayout()
        {
            activeDisplayedOrder = null;
            if (payoutIndicatorPanel != null)
            {
                payoutIndicatorPanel.SetActive(false);
            }
        }

        public void UpdateStateHint(GameState state)
        {
            if (isSubscreenActive)
            {
                SetHUDDetailsVisible(false);
                HideOrderPayout();
                if (statusHintText != null && !string.IsNullOrEmpty(defaultSubscreenHint) && notificationRoutine == null)
                {
                    statusHintText.text = defaultSubscreenHint;
                }
                return;
            }

            if (state != GameState.CustomerWaiting)
            {
                HideOrderPayout();
            }

            // HUD details are visible in storefront gameplay and night bedroom hub
            bool showHUD = (state != GameState.GameOver && state != GameState.GameWon);
            SetHUDDetailsVisible(showHUD);
            if (showHUD)
            {
                RefreshHUDDisplay();
            }

            if (statusHintText == null) return;

            if (CustomerManager.Instance != null && CustomerManager.Instance.CustomerController != null && CustomerManager.Instance.CustomerController.IsLandlordActive)
            {
                bool isEndless = EconomyManager.Instance != null && EconomyManager.Instance.IsEndlessMode;
                statusHintText.text = isEndless ? "Chairwoman Chubi has arrived!" : "The Landlady has arrived!";
                return;
            }

            if (GameManager.Instance != null && GameManager.Instance.IsCasualMode)
            {
                statusHintText.text = state switch
                {
                    GameState.MorningPrep => "Open the shutters to begin brewing!",
                    GameState.ShopOpen => "Ring the bell to call the next customer.",
                    GameState.CustomerWaiting => "Prepare the customer's order at your own pace!",
                    GameState.CustomerReacting => "Customer is enjoying their drink!",
                    _ => "Casual Mode: Endless brewing, zero pressure."
                };
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
