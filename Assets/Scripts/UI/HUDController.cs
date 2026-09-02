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

        private Coroutine notificationRoutine;
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
                    primary = GetIngredientSprite("Topping_PoppingBoba");
                    secondary = GetIceSprite();
                    break;

                case "chilly_rain":
                    primary = GetIngredientSprite("Milk_CondensedMilk");
                    break;

                case "golden_harvest":
                    primary = GetBabyYippeeSprite();
                    if (primary == null) primary = GetIngredientSprite("Topping_GoldenHoneyPearls");
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
                case "dairy_surplus":
                case "tropical_coconut":
                    marketEventTrendText.text = "<color=#FF4D4D><b>▼</b></color>";
                    break;

                case "tapioca_delay":
                case "cream_shortage":
                case "plant_based_craze":
                case "wellness_trend":
                case "summer_heatwave":
                case "chilly_rain":
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
                statusHintText.text = "The Landlady has arrived!";
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
                        textRt.offsetMin = new Vector2(10f, 0f);
                        textRt.offsetMax = new Vector2(-10f, 0f);

                        payoutIndicatorText = textObj.GetComponent<TextMeshProUGUI>();
                        payoutIndicatorText.fontSize = 18;
                        payoutIndicatorText.fontStyle = FontStyles.Bold;
                        payoutIndicatorText.alignment = TextAlignmentOptions.Center;
                        payoutIndicatorText.enableWordWrapping = false;
                        payoutIndicatorText.raycastTarget = false;
                    }
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
                    textRt.offsetMin = new Vector2(10f, 0f);
                    textRt.offsetMax = new Vector2(-10f, 0f);

                    payoutIndicatorText = textObj.GetComponent<TextMeshProUGUI>();
                    payoutIndicatorText.fontSize = 18;
                    payoutIndicatorText.fontStyle = FontStyles.Bold;
                    payoutIndicatorText.alignment = TextAlignmentOptions.Center;
                    payoutIndicatorText.enableWordWrapping = false;
                    payoutIndicatorText.raycastTarget = false;
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
            rt.sizeDelta = new Vector2(560f, 36f);

            var img = payoutIndicatorPanel.GetComponent<Image>();
            img.color = new Color(0.08f, 0.08f, 0.12f, 0.88f);
            img.raycastTarget = false;

            GameObject fallbackTextObj = new GameObject("PayoutText", typeof(RectTransform), typeof(TextMeshProUGUI));
            fallbackTextObj.transform.SetParent(payoutIndicatorPanel.transform, false);

            var fallbackTextRt = fallbackTextObj.GetComponent<RectTransform>();
            fallbackTextRt.anchorMin = Vector2.zero;
            fallbackTextRt.anchorMax = Vector2.one;
            fallbackTextRt.offsetMin = new Vector2(10f, 0f);
            fallbackTextRt.offsetMax = new Vector2(-10f, 0f);

            payoutIndicatorText = fallbackTextObj.GetComponent<TextMeshProUGUI>();
            payoutIndicatorText.fontSize = 18;
            payoutIndicatorText.fontStyle = FontStyles.Bold;
            payoutIndicatorText.alignment = TextAlignmentOptions.Center;
            payoutIndicatorText.enableWordWrapping = false;
            payoutIndicatorText.raycastTarget = false;

            payoutIndicatorPanel.SetActive(false);
        }

        private DrinkOrder activeDisplayedOrder = null;

        private void Update()
        {
            if (activeDisplayedOrder != null)
            {
                UpdateOrderPayoutDisplay();
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

            float basePrice = (float)(Math.Round(activeDisplayedOrder.basePrice * 10.0, MidpointRounding.AwayFromZero) / 10.0);
            float minPrice = (float)(Math.Round((basePrice * 0.30f) * 10.0, MidpointRounding.AwayFromZero) / 10.0);

            bool hasLuckyCat = UpgradeManager.Instance != null && UpgradeManager.Instance.HasUpgrade(UpgradeType.LuckyCat);
            // Base tip 10% + Max speed tip 30% = 40% tip (multiplied by 1.30 if Lucky Cat upgrade is active)
            float maxTipMultiplier = 0.40f * (hasLuckyCat ? 1.30f : 1.0f);
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
                payoutIndicatorText.text = $"<color=#BDC3C7>Payout:</color>  <color=#FF6B6B>Min: ${minPrice:F2}</color>  <color=#7F8C8D>•</color>  <color=#FFD700>Current: ${currentPayout:F2}</color>  <color=#7F8C8D>•</color>  <color=#2ECC71>Max: ${maxPrice:F2}</color>";
            }

            if (payoutIndicatorPanel != null && !payoutIndicatorPanel.activeSelf)
            {
                payoutIndicatorPanel.SetActive(true);
                payoutIndicatorPanel.transform.SetAsLastSibling();
            }
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
                statusHintText.text = "The Landlady has arrived!";
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
