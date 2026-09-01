using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    public class PrepAreaViewController : MonoBehaviour
    {
        private static PrepAreaViewController instance;
        public static PrepAreaViewController Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<PrepAreaViewController>(FindObjectsInactive.Include);
                }
                return instance;
            }
            private set => instance = value;
        }

        public enum PrepStationState
        {
            Empty,
            Loaded,
            Processing,
            ReadyToCollect
        }

        [Header("Screen Root & Navigation")]
        [SerializeField] private GameObject prepAreaPanelRoot;
        [SerializeField] private Image prepAreaBackgroundImage;
        [SerializeField] private Sprite prepAreaInteriorSprite;
        [SerializeField] private Button returnToNightHubButton;

        [Header("Top Raw Cards Container (Runtime Generated)")]
        [SerializeField] private Transform topRawCardsContainer;

        [Header("Station 1: Blender & Sieve (Day 5+)")]
        [SerializeField] private GameObject stationBlenderRoot;
        [SerializeField] private Button stationBlenderButton;
        [SerializeField] private Image stationBlenderImage;
        [SerializeField] private Image stationSieveImage;
        [SerializeField] private Transform blenderContentsContainer;
        [SerializeField] private Sprite blenderEmptySprite;
        [SerializeField] private Sprite blenderBlendedSprite;

        [Header("Station 2: Chopping Board & Knife (Day 11+)")]
        [SerializeField] private GameObject stationChoppingRoot;
        [SerializeField] private Button stationChoppingButton;
        [SerializeField] private Image stationChoppingImage;
        [SerializeField] private Image stationKnifeImage;
        [SerializeField] private Transform choppingContentsContainer;
        [SerializeField] private Sprite choppingEmptySprite;

        [Header("Station 3: Bucket & Centrifuge (Day 18+)")]
        [SerializeField] private GameObject stationCentrifugeRoot;
        [SerializeField] private Button stationCentrifugeButton;
        [SerializeField] private Image stationBucketImage;
        [SerializeField] private Image stationCentrifugeImage;
        [SerializeField] private Sprite bucketEmptySprite;
        [SerializeField] private Sprite bucketLoadedSprite;
        [SerializeField] private Sprite centrifugeRefinedSprite;

        [Header("Audio Feedback (Optional)")]
        [SerializeField] private AudioClip loadIngredientSound;
        [SerializeField] private AudioClip blendSound;
        [SerializeField] private AudioClip chopSound;
        [SerializeField] private AudioClip centrifugeSound;
        [SerializeField] private AudioClip collectRewardSound;

        // Runtime Station Tracking
        [Header("Runtime State")]
        [SerializeField] private PrepStationState blenderState = PrepStationState.Empty;
        [SerializeField] private int blenderLoadedCount = 0;

        [SerializeField] private PrepStationState choppingState = PrepStationState.Empty;
        [SerializeField] private int choppingLoadedCount = 0;

        [SerializeField] private PrepStationState centrifugeState = PrepStationState.Empty;
        [SerializeField] private int centrifugeLoadedCount = 0;

        [SerializeField] private int rolledGrassJellyYield = 0;
        [SerializeField] private int rolledCoconutJellyYield = 0;

        private const int MAX_BLENDER_CAPACITY = 9;
        private const int MAX_CHOPPING_CAPACITY = 3;
        private const int MAX_CENTRIFUGE_CAPACITY = 9;
        private int currentDayNumber = 1;
        private bool isPrepAreaOpen = false;
        public event Action OnPrepAreaClosed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            ResolveComponentReferences();
            EnsureFallbackAssets();
            EnsurePrepAreaPanelHierarchy();

            if (returnToNightHubButton != null)
            {
                returnToNightHubButton.onClick.RemoveListener(ClosePrepAreaView);
                returnToNightHubButton.onClick.AddListener(ClosePrepAreaView);
                returnToNightHubButton.gameObject.SetActive(false);
            }

            if (prepAreaPanelRoot != null)
            {
                prepAreaPanelRoot.SetActive(false);
            }
            gameObject.SetActive(false);
        }

        private void Start()
        {
            if (returnToNightHubButton != null)
            {
                returnToNightHubButton.onClick.RemoveListener(ClosePrepAreaView);
                returnToNightHubButton.onClick.AddListener(ClosePrepAreaView);
                if (!isPrepAreaOpen) returnToNightHubButton.gameObject.SetActive(false);
            }

            if (prepAreaBackgroundImage != null && prepAreaInteriorSprite != null)
            {
                prepAreaBackgroundImage.sprite = prepAreaInteriorSprite;
            }

            // Station Clicks (Start processing or Collect rewards)
            if (stationBlenderButton != null)
            {
                stationBlenderButton.onClick.RemoveListener(OnBlenderClicked);
                stationBlenderButton.onClick.AddListener(OnBlenderClicked);
            }
            if (stationChoppingButton != null)
            {
                stationChoppingButton.onClick.RemoveListener(OnChoppingClicked);
                stationChoppingButton.onClick.AddListener(OnChoppingClicked);
            }
            if (stationCentrifugeButton != null)
            {
                stationCentrifugeButton.onClick.RemoveListener(OnCentrifugeClicked);
                stationCentrifugeButton.onClick.AddListener(OnCentrifugeClicked);
            }

            if (!isPrepAreaOpen)
            {
                if (prepAreaPanelRoot != null)
                {
                    prepAreaPanelRoot.SetActive(false);
                }
                gameObject.SetActive(false);
            }
        }

        private void ResolveComponentReferences()
        {
            if (prepAreaPanelRoot == null) prepAreaPanelRoot = gameObject;
            if (prepAreaBackgroundImage == null && prepAreaPanelRoot != null)
            {
                prepAreaBackgroundImage = prepAreaPanelRoot.GetComponent<Image>();
                if (prepAreaBackgroundImage == null)
                {
                    var bgChild = prepAreaPanelRoot.transform.Find("PrepAreaBG") ?? prepAreaPanelRoot.transform.Find("Background");
                    if (bgChild != null) prepAreaBackgroundImage = bgChild.GetComponent<Image>();
                }
            }
            if (topRawCardsContainer == null && prepAreaPanelRoot != null)
            {
                var t = prepAreaPanelRoot.transform.Find("TopRawPanel") ?? prepAreaPanelRoot.transform.Find("TopRawCardsContainer");
                if (t != null) topRawCardsContainer = t;
            }
            if (returnToNightHubButton == null && prepAreaPanelRoot != null)
            {
                var btns = prepAreaPanelRoot.GetComponentsInChildren<Button>(true);
                foreach (var b in btns)
                {
                    if (b.gameObject.name.ToLower().Contains("return") || b.gameObject.name.ToLower().Contains("hub") || b.gameObject.name.ToLower().Contains("shop") || b.gameObject.name.ToLower().Contains("exit"))
                    {
                        returnToNightHubButton = b;
                        break;
                    }
                }
            }
        }

        private void EnsureFallbackAssets()
        {
#if UNITY_EDITOR
            if (prepAreaInteriorSprite == null)
            {
                prepAreaInteriorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Sprites2/Sprites 3/Forage and prep/Preparea/filledeqm.png");
                if (prepAreaInteriorSprite == null)
                {
                    prepAreaInteriorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Sprites2/Sprites 3/Forage and prep/Preparea/prepEquipment.png");
                }
            }
#endif
            if (prepAreaInteriorSprite == null)
            {
                var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
                for (int i = 0; i < allSprites.Length; i++)
                {
                    var s = allSprites[i];
                    if (s == null) continue;
                    if (prepAreaInteriorSprite == null && (s.name.ToLower().Contains("prep") || s.name.ToLower().Contains("filledeqm") || s.name.ToLower().Contains("equipment")))
                    {
                        prepAreaInteriorSprite = s;
                    }
                }
            }
        }

        private void EnsurePrepAreaPanelHierarchy()
        {
            ResolveComponentReferences();

            if (prepAreaPanelRoot == null)
            {
                prepAreaPanelRoot = gameObject;
            }

            if (topRawCardsContainer == null)
            {
                var topPanel = new GameObject("TopRawPanel", typeof(RectTransform));
                topPanel.transform.SetParent(prepAreaPanelRoot.transform, false);
                var topRt = topPanel.GetComponent<RectTransform>();
                topRt.anchorMin = new Vector2(0.5f, 1f);
                topRt.anchorMax = new Vector2(0.5f, 1f);
                topRt.pivot = new Vector2(0.5f, 1f);
                topRt.anchoredPosition = new Vector2(0f, -65f);
            }
        }

        public void OpenPrepAreaView(int dayNumber)
        {
            EnsureFallbackAssets();
            EnsurePrepAreaPanelHierarchy();

            currentDayNumber = dayNumber;
            isPrepAreaOpen = true;

            gameObject.SetActive(true);
            if (prepAreaPanelRoot != null)
            {
                prepAreaPanelRoot.SetActive(true);
            }

            HUDController.Instance?.SetSubscreenMode(true, "Kitchen Prep Area: Click raw cards above to load stations, then tap to process!");

            if (returnToNightHubButton != null)
            {
                returnToNightHubButton.gameObject.SetActive(true);
            }

            if (prepAreaBackgroundImage != null && prepAreaInteriorSprite != null)
            {
                prepAreaBackgroundImage.sprite = prepAreaInteriorSprite;
            }

            UpdateUnlocksAndDisplay();
        }

        public void ClosePrepAreaView()
        {
            isPrepAreaOpen = false;

            if (prepAreaPanelRoot != null)
            {
                prepAreaPanelRoot.SetActive(false);
            }

            if (returnToNightHubButton != null)
            {
                returnToNightHubButton.gameObject.SetActive(false);
            }
            gameObject.SetActive(false);

            HUDController.Instance?.SetSubscreenMode(false);
            OnPrepAreaClosed?.Invoke();
        }

        public void UpdateUnlocksAndDisplay()
        {
            int day = currentDayNumber;

            // 1. Populate Dynamic Raw Item Cards at Top
            PopulateTopRawCards(day);

            // 2. Unlocks for Stations on the Desk
            bool yippeesUnlocked = (day >= 5);
            bool jelliesUnlocked = (day >= 11);
            bool goldenDewUnlocked = (day >= 18);

            if (stationBlenderRoot != null) stationBlenderRoot.SetActive(yippeesUnlocked);
            if (stationChoppingRoot != null) stationChoppingRoot.SetActive(jelliesUnlocked);
            if (stationCentrifugeRoot != null) stationCentrifugeRoot.SetActive(goldenDewUnlocked);

            // Update Station UI visuals
            UpdateBlenderUI();
            UpdateChoppingUI();
            UpdateCentrifugeUI();
        }

        // =========================================================================
        // RUNTIME TOP RAW CARDS GENERATOR
        // =========================================================================
        private void PopulateTopRawCards(int day)
        {
            if (topRawCardsContainer == null || InventoryManager.Instance == null) return;

            topRawCardsContainer.SetAsLastSibling();

            // Clear existing cards
            for (int i = topRawCardsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(topRawCardsContainer.GetChild(i).gameObject);
            }

            var activeRawItems = new List<(RawIngredientType type, string name, Sprite icon, string actionLabel)>();

            Sprite babyIcon = SpriteManager.Instance != null ? SpriteManager.Instance.BabyYippeeSprite : null;
            Sprite jellyIcon = SpriteManager.Instance != null ? SpriteManager.Instance.RawJellyBlocksSprite : null;
            Sprite dewIcon = SpriteManager.Instance != null ? SpriteManager.Instance.RawGoldenDewSprite : null;

            if (day >= 5)
            {
                activeRawItems.Add((RawIngredientType.BabyYippees, "Baby Yippees", babyIcon, "Load Blender"));
            }
            if (day >= 11)
            {
                activeRawItems.Add((RawIngredientType.JellyBlocks, "Jelly Blocks", jellyIcon, "Load Board"));
            }
            if (day >= 18)
            {
                activeRawItems.Add((RawIngredientType.GoldenDew, "Golden Dew", dewIcon, "Pour Bucket"));
            }

            if (activeRawItems.Count == 0) return;

            RectTransform containerRt = topRawCardsContainer as RectTransform;
            float totalWidth = containerRt != null && containerRt.rect.width > 200 ? containerRt.rect.width : 1000f;
            float totalHeight = containerRt != null && containerRt.rect.height > 40 ? containerRt.rect.height : 90f;

            int count = activeRawItems.Count;
            float spacingX = 24f;
            float cardWidth = Mathf.Min(310f, (totalWidth - (spacingX * (count - 1))) / count);
            float cardHeight = Mathf.Min(84f, totalHeight);

            float totalBlockWidth = (cardWidth * count) + (spacingX * (count - 1));
            float startX = -totalBlockWidth * 0.5f + (cardWidth * 0.5f);

            for (int i = 0; i < count; i++)
            {
                var item = activeRawItems[i];
                int stockCount = InventoryManager.Instance.GetRawStock(item.type);
                Vector2 pos = new Vector2(startX + i * (cardWidth + spacingX), 0);

                // Card Root Object with Button for easy tapping
                GameObject cardObj = new GameObject($"RawCard_{item.type}", typeof(RectTransform), typeof(Image), typeof(Button));
                cardObj.transform.SetParent(topRawCardsContainer, false);
                var rt = cardObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(cardWidth, cardHeight);
                rt.anchoredPosition = pos;

                var cardImg = cardObj.GetComponent<Image>();
                cardImg.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);
                cardImg.raycastTarget = true;

                // Left: Raw Ingredient Icon
                float leftOffset = 12f;
                if (item.icon != null)
                {
                    GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                    iconObj.transform.SetParent(cardObj.transform, false);
                    var iconRt = iconObj.GetComponent<RectTransform>();
                    iconRt.anchorMin = new Vector2(0, 0.5f);
                    iconRt.anchorMax = new Vector2(0, 0.5f);
                    iconRt.pivot = new Vector2(0, 0.5f);
                    float iconSize = Mathf.Min(cardHeight - 16f, 56f);
                    iconRt.sizeDelta = new Vector2(iconSize, iconSize);
                    iconRt.anchoredPosition = new Vector2(10, 0);

                    var img = iconObj.GetComponent<Image>();
                    img.sprite = item.icon;
                    img.preserveAspect = true;
                    img.raycastTarget = false;
                    leftOffset = iconSize + 20f;
                }

                // Right: Count Pill Badge
                GameObject pillObj = new GameObject("CountPill", typeof(RectTransform), typeof(Image));
                pillObj.transform.SetParent(cardObj.transform, false);
                var pillRt = pillObj.GetComponent<RectTransform>();
                pillRt.anchorMin = new Vector2(1, 0.5f);
                pillRt.anchorMax = new Vector2(1, 0.5f);
                pillRt.pivot = new Vector2(1, 0.5f);
                pillRt.sizeDelta = new Vector2(80, 36);
                pillRt.anchoredPosition = new Vector2(-10, 0);

                var pillImg = pillObj.GetComponent<Image>();
                pillImg.color = new Color(0.18f, 0.24f, 0.36f, 0.90f);
                pillImg.raycastTarget = false;

                GameObject countTextObj = new GameObject("CountText", typeof(RectTransform), typeof(TextMeshProUGUI));
                countTextObj.transform.SetParent(pillObj.transform, false);
                var countTextRt = countTextObj.GetComponent<RectTransform>();
                countTextRt.anchorMin = Vector2.zero;
                countTextRt.anchorMax = Vector2.one;
                countTextRt.offsetMin = Vector2.zero;
                countTextRt.offsetMax = Vector2.zero;

                var countTmp = countTextObj.GetComponent<TextMeshProUGUI>();
                countTmp.text = FormatCount(stockCount);
                countTmp.fontSize = 18;
                countTmp.alignment = TextAlignmentOptions.Center;
                countTmp.textWrappingMode = TextWrappingModes.NoWrap;
                countTmp.raycastTarget = false;

                // Middle: Item Name and Action Subtitle
                GameObject textObj = new GameObject("TextInfo", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(cardObj.transform, false);
                var textRt = textObj.GetComponent<RectTransform>();
                textRt.anchorMin = new Vector2(0, 0);
                textRt.anchorMax = new Vector2(1, 1);
                textRt.offsetMin = new Vector2(leftOffset, 4);
                textRt.offsetMax = new Vector2(-95, -4);

                var tmp = textObj.GetComponent<TextMeshProUGUI>();
                tmp.text = $"<b>{item.name}</b>\n<size=13><color=#88AACC>{item.actionLabel}</color></size>";
                tmp.fontSize = 17;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.color = Color.white;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.raycastTarget = false;

                // Button Click -> Deposit Raw Ingredient
                var btn = cardObj.GetComponent<Button>();
                var capturedType = item.type;
                btn.onClick.AddListener(() => DepositRawIngredient(capturedType));
            }
        }

        private string FormatCount(int count)
        {
            string col = count == 0 ? "#FF4444" : (count <= 3 ? "#F1C40F" : "#2ECC71");
            return $"<color={col}>x {count:D2}</color>";
        }

        // =========================================================================
        // DEPOSIT RAW INGREDIENTS
        // =========================================================================
        public void DepositRawIngredient(RawIngredientType type)
        {
            if (InventoryManager.Instance == null) return;

            int currentStock = InventoryManager.Instance.GetRawStock(type);
            if (currentStock <= 0)
            {
                string itemName = type switch
                {
                    RawIngredientType.BabyYippees => "Baby Yippees",
                    RawIngredientType.JellyBlocks => "Jelly Blocks",
                    RawIngredientType.GoldenDew => "Golden Dew",
                    _ => "raw ingredient"
                };
                HUDController.Instance?.ShowNotification($"No {itemName} in inventory! Go foraging at night to gather more.", 3.5f);
                return;
            }

            switch (type)
            {
                case RawIngredientType.BabyYippees:
                    if (blenderState == PrepStationState.Processing || blenderState == PrepStationState.ReadyToCollect)
                    {
                        HUDController.Instance?.ShowNotification("Please collect your finished Boba before loading a new batch!", 3f);
                        return;
                    }
                    if (blenderLoadedCount >= MAX_BLENDER_CAPACITY)
                    {
                        HUDController.Instance?.ShowNotification($"The blender is full! (Max {MAX_BLENDER_CAPACITY} Baby Yippees). Click blender to process.", 3f);
                        return;
                    }
                    if (InventoryManager.Instance.ConsumeRawStock(RawIngredientType.BabyYippees, 1))
                    {
                        blenderLoadedCount++;
                        blenderState = PrepStationState.Loaded;
                        PlaySound(loadIngredientSound);
                        SpawnYippeeInBlender();
                        UpdateUnlocksAndDisplay();
                        HUDController.Instance?.ShowNotification($"Loaded a Baby Yippee into the Blender! ({blenderLoadedCount}/{MAX_BLENDER_CAPACITY})", 2f);
                    }
                    break;

                case RawIngredientType.JellyBlocks:
                    if (choppingState == PrepStationState.Processing || choppingState == PrepStationState.ReadyToCollect)
                    {
                        HUDController.Instance?.ShowNotification("Please collect your chopped Jelly cubes before loading a new batch!", 3f);
                        return;
                    }
                    if (choppingLoadedCount >= MAX_CHOPPING_CAPACITY)
                    {
                        HUDController.Instance?.ShowNotification($"The chopping board is full! (Max {MAX_CHOPPING_CAPACITY} Jelly Blocks). Click board to chop.", 3f);
                        return;
                    }
                    if (InventoryManager.Instance.ConsumeRawStock(RawIngredientType.JellyBlocks, 1))
                    {
                        choppingLoadedCount++;
                        choppingState = PrepStationState.Loaded;
                        PlaySound(loadIngredientSound);
                        SpawnJellyBlockOnBoard();
                        UpdateUnlocksAndDisplay();
                        HUDController.Instance?.ShowNotification($"Placed a Jelly Block on the Chopping Board! ({choppingLoadedCount}/{MAX_CHOPPING_CAPACITY})", 2f);
                    }
                    break;

                case RawIngredientType.GoldenDew:
                    if (centrifugeState == PrepStationState.Processing || centrifugeState == PrepStationState.ReadyToCollect)
                    {
                        HUDController.Instance?.ShowNotification("Please collect your refined gourmet toppings before spinning a new batch!", 3f);
                        return;
                    }
                    if (centrifugeLoadedCount >= MAX_CENTRIFUGE_CAPACITY)
                    {
                        HUDController.Instance?.ShowNotification($"The centrifuge bucket is full! (Max {MAX_CENTRIFUGE_CAPACITY} Golden Dew). Click station to process.", 3f);
                        return;
                    }
                    if (InventoryManager.Instance.ConsumeRawStock(RawIngredientType.GoldenDew, 1))
                    {
                        centrifugeLoadedCount++;
                        centrifugeState = PrepStationState.Loaded;
                        PlaySound(loadIngredientSound);
                        UpdateUnlocksAndDisplay();
                        HUDController.Instance?.ShowNotification($"Poured Golden Dew into the Bucket! ({centrifugeLoadedCount}/{MAX_CENTRIFUGE_CAPACITY})", 2f);
                    }
                    break;
            }
        }

        // =========================================================================
        // STATION 1: BLENDER & SIEVE (TAPIOCA PEARLS & POPPING BOBA)
        // =========================================================================
        private void OnBlenderClicked()
        {
            if (blenderState == PrepStationState.Empty)
            {
                HUDController.Instance?.ShowNotification("Click the Baby Yippees card above to place them in the Blender first.", 3f);
            }
            else if (blenderState == PrepStationState.Loaded)
            {
                StartCoroutine(ProcessBlenderRoutine());
            }
            else if (blenderState == PrepStationState.ReadyToCollect)
            {
                CollectBlenderYield();
            }
        }

        private void SpawnYippeeInBlender()
        {
            if (blenderContentsContainer == null) return;
            Sprite yippeeSp = SpriteManager.Instance != null ? SpriteManager.Instance.BabyYippeeSprite : null;
            if (yippeeSp == null) return;

            GameObject yippeeObj = new GameObject($"Yippee_{blenderContentsContainer.childCount + 1}", typeof(RectTransform), typeof(Image));
            yippeeObj.transform.SetParent(blenderContentsContainer, false);
            var rt = yippeeObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            float size = 48f;
            rt.sizeDelta = new Vector2(size, size);

            RectTransform contRt = blenderContentsContainer as RectTransform;
            float halfW = contRt != null && contRt.rect.width > 20 ? contRt.rect.width * 0.35f : 35f;
            float halfH = contRt != null && contRt.rect.height > 20 ? contRt.rect.height * 0.35f : 35f;

            float randX = UnityEngine.Random.Range(-halfW, halfW);
            float randY = UnityEngine.Random.Range(-halfH, halfH);
            rt.anchoredPosition = new Vector2(randX, randY);
            rt.localRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-45f, 45f));

            var img = yippeeObj.GetComponent<Image>();
            img.sprite = yippeeSp;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }

        private void ClearBlenderContents()
        {
            if (blenderContentsContainer == null) return;
            for (int i = blenderContentsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(blenderContentsContainer.GetChild(i).gameObject);
            }
        }

        private IEnumerator ProcessBlenderRoutine()
        {
            blenderState = PrepStationState.Processing;
            UpdateBlenderUI();
            PlaySound(blendSound);
            HUDController.Instance?.ShowNotification("Blending and sieving Baby Yippees...", 2.0f);

            Vector3 origBlenderPos = stationBlenderImage != null ? stationBlenderImage.rectTransform.localPosition : Vector3.zero;
            Vector3 origContentsPos = blenderContentsContainer != null ? blenderContentsContainer.localPosition : Vector3.zero;

            float elapsed = 0f;
            float duration = 2.0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float offsetX = UnityEngine.Random.Range(-5f, 5f);
                float offsetY = UnityEngine.Random.Range(-5f, 5f);

                if (stationBlenderImage != null) stationBlenderImage.rectTransform.localPosition = origBlenderPos + new Vector3(offsetX, offsetY, 0);
                if (blenderContentsContainer != null) blenderContentsContainer.localPosition = origContentsPos + new Vector3(offsetX, offsetY, 0);

                yield return null;
            }

            if (stationBlenderImage != null) stationBlenderImage.rectTransform.localPosition = origBlenderPos;
            if (blenderContentsContainer != null) blenderContentsContainer.localPosition = origContentsPos;

            ClearBlenderContents();
            blenderState = PrepStationState.ReadyToCollect;
            UpdateBlenderUI();
            HUDController.Instance?.ShowNotification("Blending and sieving complete! Click the blender to collect fresh Boba!", 3.5f);
        }

        private void CollectBlenderYield()
        {
            if (blenderLoadedCount <= 0) return;

            int totalItems = blenderLoadedCount * 2;
            int poppingBobaYield = 0;
            int tapiocaYield = 0;

            float poppingChance = 0.40f;
            if (UpgradeManager.Instance != null && UpgradeManager.Instance.HasUpgrade(UpgradeType.LuckyPoppingBobaBracelet))
            {
                poppingChance = 0.75f;
            }

            for (int i = 0; i < totalItems; i++)
            {
                if (UnityEngine.Random.value < poppingChance)
                {
                    poppingBobaYield++;
                }
                else
                {
                    tapiocaYield++;
                }
            }

            if (tapiocaYield > 0) InventoryManager.Instance?.AddToppingStock(ToppingType.TapiocaPearls, tapiocaYield);
            if (poppingBobaYield > 0) InventoryManager.Instance?.AddToppingStock(ToppingType.PoppingBoba, poppingBobaYield);

            PlaySound(collectRewardSound);

            List<string> parts = new List<string>();
            if (tapiocaYield > 0) parts.Add($"<color=#2ECC71>+{tapiocaYield} Tapioca Pearls</color>");
            if (poppingBobaYield > 0) parts.Add($"<color=#2ECC71>+{poppingBobaYield} Popping Boba</color>");
            string summary = string.Join(" and ", parts);

            HUDController.Instance?.ShowNotification($"Collected {summary}!", 4f);

            ClearBlenderContents();
            blenderLoadedCount = 0;
            blenderState = PrepStationState.Empty;
            UpdateUnlocksAndDisplay();
        }

        private void UpdateBlenderUI()
        {
            if (stationBlenderImage != null)
            {
                Sprite target = blenderState switch
                {
                    PrepStationState.Empty => blenderEmptySprite,
                    PrepStationState.Loaded => blenderEmptySprite,
                    PrepStationState.Processing => blenderEmptySprite,
                    PrepStationState.ReadyToCollect => (blenderBlendedSprite != null ? blenderBlendedSprite : blenderEmptySprite),
                    _ => blenderEmptySprite
                };
                if (target != null) stationBlenderImage.sprite = target;
            }
        }

        // =========================================================================
        // STATION 2: CHOPPING BOARD & KNIFE (GRASS JELLY & COCONUT JELLY)
        // =========================================================================
        private Transform GetChoppingContainer()
        {
            if (choppingContentsContainer != null) return choppingContentsContainer;
            if (stationChoppingImage != null) return stationChoppingImage.transform;
            return stationChoppingRoot != null ? stationChoppingRoot.transform : null;
        }

        private Sprite GetGrassJellySprite()
        {
            if (SpriteManager.Instance != null && SpriteManager.Instance.GrassJellySprite != null)
            {
                return SpriteManager.Instance.GrassJellySprite;
            }
            if (SupermarketViewController.Instance != null)
            {
                var sp = SupermarketViewController.Instance.GetIngredientIcon("Topping_GrassJelly");
                if (sp != null) return sp;
            }
            if (CupStation.Instance != null)
            {
                var sp = CupStation.Instance.GrassJellySprite;
                if (sp != null) return sp;
            }
            return null;
        }

        private Sprite GetCoconutJellySprite()
        {
            if (SpriteManager.Instance != null && SpriteManager.Instance.CoconutJellySprite != null)
            {
                return SpriteManager.Instance.CoconutJellySprite;
            }
            if (SupermarketViewController.Instance != null)
            {
                var sp = SupermarketViewController.Instance.GetIngredientIcon("Topping_CoconutJelly");
                if (sp != null) return sp;
            }
            if (CupStation.Instance != null)
            {
                var sp = CupStation.Instance.CoconutJellySprite;
                if (sp != null) return sp;
            }
            return null;
        }
        // STATION 2: CHOPPING BOARD & KNIFE (DAY 11+)
        // =========================================================================
        private void EnsureChoppingStationHierarchy()
        {
            if (stationChoppingRoot == null)
            {
                stationChoppingRoot = transform.Find("Station2_ChoppingBoard")?.gameObject ??
                                      transform.Find("StationChopping")?.gameObject;
            }

            if (stationChoppingRoot != null)
            {
                if (stationChoppingImage == null) stationChoppingImage = stationChoppingRoot.GetComponent<Image>();
                if (stationChoppingButton == null) stationChoppingButton = stationChoppingRoot.GetComponent<Button>();
                if (stationKnifeImage == null)
                {
                    var knifeChild = stationChoppingRoot.transform.Find("KnifeImage");
                    if (knifeChild != null) stationKnifeImage = knifeChild.GetComponent<Image>();
                }
                if (choppingContentsContainer == null)
                {
                    var contChild = stationChoppingRoot.transform.Find("ContentsContainer");
                    if (contChild != null) choppingContentsContainer = contChild;
                    else choppingContentsContainer = stationChoppingRoot.transform;
                }
            }
        }

        private void OnChoppingClicked()
        {
            if (choppingState == PrepStationState.Empty)
            {
                HUDController.Instance?.ShowNotification("Click the Jelly Blocks card above to place them on the Chopping Board.", 3f);
            }
            else if (choppingState == PrepStationState.Loaded)
            {
                StartCoroutine(ProcessChoppingRoutine());
            }
            else if (choppingState == PrepStationState.Processing)
            {
                HUDController.Instance?.ShowNotification("Jelly Blocks are being sliced...", 1.5f);
            }
            else if (choppingState == PrepStationState.ReadyToCollect)
            {
                CollectChoppingYield();
            }
        }

        private void SpawnJellyBlockOnBoard()
        {
            Transform parent = GetChoppingContainer();
            if (parent == null) return;
            Sprite jellySp = SpriteManager.Instance != null ? SpriteManager.Instance.RawJellyBlocksSprite : null;
            if (jellySp == null) return;

            int index = choppingLoadedCount - 1;
            GameObject blockObj = new GameObject($"JellyBlock_{choppingLoadedCount}", typeof(RectTransform), typeof(Image));
            blockObj.transform.SetParent(parent, false);
            var rt = blockObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            float blockSize = 68f;
            rt.sizeDelta = new Vector2(blockSize, blockSize);

            RectTransform boardRt = (stationChoppingImage != null ? stationChoppingImage.rectTransform : parent as RectTransform);
            float boardW = boardRt != null && boardRt.rect.width > 20 ? boardRt.rect.width : 220f;
            float boardH = boardRt != null && boardRt.rect.height > 20 ? boardRt.rect.height : 140f;

            float[] posXFactors = { -0.28f, 0.0f, 0.28f };
            float[] posYFactors = { -0.05f, 0.10f, -0.05f };

            float factorX = index >= 0 && index < posXFactors.Length ? posXFactors[index] : 0f;
            float factorY = index >= 0 && index < posYFactors.Length ? posYFactors[index] : 0f;

            float randJitterX = UnityEngine.Random.Range(-4f, 4f);
            float randJitterY = UnityEngine.Random.Range(-4f, 4f);

            rt.anchoredPosition = new Vector2((boardW * factorX) + randJitterX, (boardH * factorY) + randJitterY);
            rt.localRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-10f, 10f));

            var img = blockObj.GetComponent<Image>();
            img.sprite = jellySp;
            img.preserveAspect = true;
            img.raycastTarget = false;

            if (stationKnifeImage != null)
            {
                stationKnifeImage.transform.SetAsLastSibling();
            }
        }

        private void SpawnChoppedJelliesOnBoard()
        {
            ClearChoppingContents();
            Transform parent = GetChoppingContainer();
            if (parent == null) return;

            Sprite grassSp = GetGrassJellySprite();
            Sprite cocoSp = GetCoconutJellySprite();

            RectTransform boardRt = (stationChoppingImage != null ? stationChoppingImage.rectTransform : parent as RectTransform);
            float boardW = boardRt != null && boardRt.rect.width > 20 ? boardRt.rect.width : 220f;
            float boardH = boardRt != null && boardRt.rect.height > 20 ? boardRt.rect.height : 140f;

            float cubeSize = 44f;

            // Spawn Grass Jelly cubes on the left half of the board
            int grassToSpawn = Mathf.Min(rolledGrassJellyYield, 12);
            for (int i = 0; i < grassToSpawn; i++)
            {
                if (grassSp == null) break;
                GameObject cubeObj = new GameObject($"GrassJelly_{i + 1}", typeof(RectTransform), typeof(Image));
                cubeObj.transform.SetParent(parent, false);
                var rt = cubeObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(cubeSize, cubeSize);

                int col = i % 4;
                int row = i / 4;
                float posX = -boardW * 0.35f + (col * 22f) + UnityEngine.Random.Range(-3f, 3f);
                float posY = (boardH * 0.15f) - (row * 20f) + UnityEngine.Random.Range(-3f, 3f);
                rt.anchoredPosition = new Vector2(posX, posY);
                rt.localRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-15f, 15f));

                var img = cubeObj.GetComponent<Image>();
                img.sprite = grassSp;
                img.preserveAspect = true;
                img.raycastTarget = false;
            }

            // Spawn Coconut Jelly cubes on the right half of the board
            int cocoToSpawn = Mathf.Min(rolledCoconutJellyYield, 12);
            for (int i = 0; i < cocoToSpawn; i++)
            {
                if (cocoSp == null) break;
                GameObject cubeObj = new GameObject($"CoconutJelly_{i + 1}", typeof(RectTransform), typeof(Image));
                cubeObj.transform.SetParent(parent, false);
                var rt = cubeObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(cubeSize, cubeSize);

                int col = i % 4;
                int row = i / 4;
                float posX = boardW * 0.05f + (col * 22f) + UnityEngine.Random.Range(-3f, 3f);
                float posY = (boardH * 0.15f) - (row * 20f) + UnityEngine.Random.Range(-3f, 3f);
                rt.anchoredPosition = new Vector2(posX, posY);
                rt.localRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-15f, 15f));

                var img = cubeObj.GetComponent<Image>();
                img.sprite = cocoSp;
                img.preserveAspect = true;
                img.raycastTarget = false;
            }

            if (stationKnifeImage != null)
            {
                stationKnifeImage.transform.SetAsLastSibling();
            }
        }

        private void ClearChoppingContents()
        {
            Transform parent = GetChoppingContainer();
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (stationKnifeImage != null && child == stationKnifeImage.transform) continue;
                Destroy(child.gameObject);
            }
        }

        private IEnumerator ProcessChoppingRoutine()
        {
            choppingState = PrepStationState.Processing;
            UpdateChoppingUI();
            PlaySound(chopSound);
            HUDController.Instance?.ShowNotification("Chopping Jelly Blocks into cubes...", 1.8f);

            if (stationKnifeImage != null)
            {
                stationKnifeImage.transform.SetAsLastSibling();
                Vector3 origPos = stationKnifeImage.rectTransform.localPosition;
                Quaternion origRot = stationKnifeImage.rectTransform.localRotation;
                float elapsed = 0f;
                float duration = 1.8f;
                float sweepDistance = 350f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / duration;

                    float currentX;
                    float chopY;
                    float tiltZ;

                    if (progress < 0.75f)
                    {
                        // Active chopping sweep: right to left across the cutting board
                        float cutProgress = progress / 0.75f;
                        currentX = origPos.x - (cutProgress * sweepDistance);
                        chopY = Mathf.Abs(Mathf.Sin(cutProgress * Mathf.PI * 8f)) * 28f;
                        tiltZ = Mathf.Sin(cutProgress * Mathf.PI * 8f) * 14f;
                    }
                    else
                    {
                        // Return smoothly back to the right
                        float returnProgress = (progress - 0.75f) / 0.25f;
                        currentX = Mathf.Lerp(origPos.x - sweepDistance, origPos.x, returnProgress);
                        chopY = Mathf.Lerp(20f, 0f, returnProgress);
                        tiltZ = Mathf.Lerp(10f, 0f, returnProgress);
                    }

                    stationKnifeImage.rectTransform.localPosition = new Vector3(currentX, origPos.y + chopY, origPos.z);
                    stationKnifeImage.rectTransform.localRotation = Quaternion.Euler(0, 0, tiltZ);
                    yield return null;
                }

                stationKnifeImage.rectTransform.localPosition = origPos;
                stationKnifeImage.rectTransform.localRotation = origRot;
            }
            else
            {
                yield return new WaitForSeconds(1.8f);
            }

            // Roll yields for the batch: 6 items per block (60% Grass, 40% Coconut)
            int totalItems = choppingLoadedCount * 6;
            rolledGrassJellyYield = 0;
            rolledCoconutJellyYield = 0;
            for (int i = 0; i < totalItems; i++)
            {
                if (UnityEngine.Random.value < 0.60f)
                {
                    rolledGrassJellyYield++;
                }
                else
                {
                    rolledCoconutJellyYield++;
                }
            }

            bool doubledYield = false;
            if (UpgradeManager.Instance != null && UpgradeManager.Instance.HasUpgrade(UpgradeType.ChefsHoningSteel))
            {
                if (UnityEngine.Random.value < 0.25f)
                {
                    rolledGrassJellyYield *= 2;
                    rolledCoconutJellyYield *= 2;
                    doubledYield = true;
                }
            }

            SpawnChoppedJelliesOnBoard();
            choppingState = PrepStationState.ReadyToCollect;
            UpdateChoppingUI();
            if (doubledYield)
            {
                HUDController.Instance?.ShowNotification("Razor-sharp precision! Chef's Honing Steel doubled the topping yield!", 4f);
            }
            else
            {
                HUDController.Instance?.ShowNotification("Chopping complete! Click the board to collect diced jellies!", 3.5f);
            }
        }

        private void CollectChoppingYield()
        {
            if (choppingLoadedCount <= 0) return;

            int grassJellyYield = rolledGrassJellyYield;
            int coconutJellyYield = rolledCoconutJellyYield;

            if (grassJellyYield > 0) InventoryManager.Instance?.AddToppingStock(ToppingType.GrassJelly, grassJellyYield);
            if (coconutJellyYield > 0) InventoryManager.Instance?.AddToppingStock(ToppingType.CoconutJelly, coconutJellyYield);

            PlaySound(collectRewardSound);

            List<string> parts = new List<string>();
            if (grassJellyYield > 0) parts.Add($"<color=#2ECC71>+{grassJellyYield} Grass Jelly</color>");
            if (coconutJellyYield > 0) parts.Add($"<color=#2ECC71>+{coconutJellyYield} Coconut Jelly</color>");
            string summary = string.Join(" and ", parts);

            HUDController.Instance?.ShowNotification($"Collected {summary}!", 4f);

            ClearChoppingContents();
            choppingLoadedCount = 0;
            rolledGrassJellyYield = 0;
            rolledCoconutJellyYield = 0;
            choppingState = PrepStationState.Empty;
            UpdateUnlocksAndDisplay();
        }

        private void UpdateChoppingUI()
        {
            if (stationChoppingImage != null && choppingEmptySprite != null)
            {
                stationChoppingImage.sprite = choppingEmptySprite;
            }
        }

        // =========================================================================
        // STATION 3: BUCKET & CENTRIFUGE (EGG PUDDING, CHEESE FOAM, HONEY PEARLS)
        // =========================================================================
        private void OnCentrifugeClicked()
        {
            if (centrifugeState == PrepStationState.Empty)
            {
                HUDController.Instance?.ShowNotification("Click the Golden Dew card above to pour it into the Centrifuge Bucket.", 3f);
            }
            else if (centrifugeState == PrepStationState.Loaded)
            {
                StartCoroutine(ProcessCentrifugeRoutine());
            }
            else if (centrifugeState == PrepStationState.ReadyToCollect)
            {
                CollectCentrifugeYield();
            }
        }

        private IEnumerator ProcessCentrifugeRoutine()
        {
            centrifugeState = PrepStationState.Processing;
            UpdateCentrifugeUI();
            PlaySound(centrifugeSound);
            HUDController.Instance?.ShowNotification("Spinning centrifuge to extract gourmet toppings...", 2.2f);

            if (stationCentrifugeImage != null)
            {
                float elapsed = 0f;
                float duration = 2.2f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    stationCentrifugeImage.rectTransform.Rotate(0, 1080f * Time.deltaTime, 0, Space.Self);
                    yield return null;
                }
                stationCentrifugeImage.rectTransform.localRotation = Quaternion.identity;
            }
            else
            {
                yield return new WaitForSeconds(2.2f);
            }

            centrifugeState = PrepStationState.ReadyToCollect;
            UpdateCentrifugeUI();
            HUDController.Instance?.ShowNotification("Centrifuge separation complete! Click to collect gourmet toppings!", 3.5f);
        }

        private void CollectCentrifugeYield()
        {
            if (centrifugeLoadedCount <= 0) return;

            int totalItems = centrifugeLoadedCount * 2;
            int puddingYield = 0;
            int foamYield = 0;
            int honeyPearlsYield = 0;

            bool hasDowsingRods = UpgradeManager.Instance != null && UpgradeManager.Instance.HasUpgrade(UpgradeType.DowsingRods);

            for (int i = 0; i < totalItems; i++)
            {
                float roll = UnityEngine.Random.value;
                if (hasDowsingRods)
                {
                    // Dowsing rods attune to Golden Dew: 35% chance of rare Honey Pearls (vs 10% base)
                    if (roll < 0.35f)
                    {
                        puddingYield++;
                    }
                    else if (roll < 0.65f)
                    {
                        foamYield++;
                    }
                    else
                    {
                        honeyPearlsYield++;
                    }
                }
                else
                {
                    if (roll < 0.50f)
                    {
                        puddingYield++;
                    }
                    else if (roll < 0.90f)
                    {
                        foamYield++;
                    }
                    else
                    {
                        honeyPearlsYield++;
                    }
                }
            }

            if (puddingYield > 0) InventoryManager.Instance?.AddToppingStock(ToppingType.EggPudding, puddingYield);
            if (foamYield > 0) InventoryManager.Instance?.AddToppingStock(ToppingType.CheeseFoam, foamYield);
            if (honeyPearlsYield > 0) InventoryManager.Instance?.AddToppingStock(ToppingType.GoldenHoneyPearls, honeyPearlsYield);

            PlaySound(collectRewardSound);

            List<string> parts = new List<string>();
            if (puddingYield > 0) parts.Add($"<color=#2ECC71>+{puddingYield} Egg Custard</color>");
            if (foamYield > 0) parts.Add($"<color=#2ECC71>+{foamYield} Cheese Foam</color>");
            if (honeyPearlsYield > 0) parts.Add($"<color=#2ECC71>+{honeyPearlsYield} Honey Pearls</color>");
            string summary = string.Join(", ", parts);

            HUDController.Instance?.ShowNotification($"Collected {summary}!", 4.5f);

            centrifugeLoadedCount = 0;
            centrifugeState = PrepStationState.Empty;
            UpdateUnlocksAndDisplay();
        }

        private void UpdateCentrifugeUI()
        {
            if (stationBucketImage != null)
            {
                Sprite target = centrifugeState switch
                {
                    PrepStationState.Empty => bucketEmptySprite,
                    PrepStationState.Loaded => (bucketLoadedSprite != null ? bucketLoadedSprite : bucketEmptySprite),
                    _ => bucketEmptySprite
                };
                if (target != null) stationBucketImage.sprite = target;
            }

            if (stationCentrifugeImage != null && centrifugeRefinedSprite != null && centrifugeState == PrepStationState.ReadyToCollect)
            {
                stationCentrifugeImage.sprite = centrifugeRefinedSprite;
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(clip);
            }
        }
    }
}
