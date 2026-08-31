using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    public class PrepAreaViewController : MonoBehaviour
    {
        public static PrepAreaViewController Instance { get; private set; }

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

        [Header("Top Raw Cards Panel")]
        [SerializeField] private GameObject topRawPanelRoot;
        
        [Header("Raw Card 1 - Baby Yippees (Day 5+)")]
        [SerializeField] private GameObject rawCardBabyYippees;
        [SerializeField] private Button rawCardBabyYippeesBtn;
        [SerializeField] private TextMeshProUGUI rawCountBabyYippeesText;
        [SerializeField] private Image rawIconBabyYippees;

        [Header("Raw Card 2 - Jelly Blocks (Day 11+)")]
        [SerializeField] private GameObject rawCardJellyBlocks;
        [SerializeField] private Button rawCardJellyBlocksBtn;
        [SerializeField] private TextMeshProUGUI rawCountJellyBlocksText;
        [SerializeField] private Image rawIconJellyBlocks;

        [Header("Raw Card 3 - Golden Dew (Day 18+)")]
        [SerializeField] private GameObject rawCardGoldenDew;
        [SerializeField] private Button rawCardGoldenDewBtn;
        [SerializeField] private TextMeshProUGUI rawCountGoldenDewText;
        [SerializeField] private Image rawIconGoldenDew;

        [Header("Station 1: Blender & Sieve (Day 5+)")]
        [SerializeField] private GameObject stationBlenderRoot;
        [SerializeField] private Button stationBlenderButton;
        [SerializeField] private Image stationBlenderImage;
        [SerializeField] private Image stationSieveImage;
        [SerializeField] private TextMeshProUGUI stationBlenderStatusText;
        [SerializeField] private Sprite blenderEmptySprite;
        [SerializeField] private Sprite blenderLoadedSprite;
        [SerializeField] private Sprite blenderBlendedSprite;

        [Header("Station 2: Chopping Board & Knife (Day 11+)")]
        [SerializeField] private GameObject stationChoppingRoot;
        [SerializeField] private Button stationChoppingButton;
        [SerializeField] private Image stationChoppingImage;
        [SerializeField] private Image stationKnifeImage;
        [SerializeField] private TextMeshProUGUI stationChoppingStatusText;
        [SerializeField] private Sprite choppingEmptySprite;
        [SerializeField] private Sprite choppingLoadedSprite;
        [SerializeField] private Sprite choppingChoppedSprite;

        [Header("Station 3: Bucket & Centrifuge (Day 18+)")]
        [SerializeField] private GameObject stationCentrifugeRoot;
        [SerializeField] private Button stationCentrifugeButton;
        [SerializeField] private Image stationBucketImage;
        [SerializeField] private Image stationCentrifugeImage;
        [SerializeField] private TextMeshProUGUI stationCentrifugeStatusText;
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

        private int currentDayNumber = 1;
        public event Action OnPrepAreaClosed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (returnToNightHubButton != null)
            {
                returnToNightHubButton.onClick.AddListener(ClosePrepAreaView);
            }

            if (prepAreaBackgroundImage != null && prepAreaInteriorSprite != null)
            {
                prepAreaBackgroundImage.sprite = prepAreaInteriorSprite;
            }

            // Raw Card Clicks (Deposit raw ingredient into matching station)
            if (rawCardBabyYippeesBtn != null) rawCardBabyYippeesBtn.onClick.AddListener(() => DepositRawIngredient(RawIngredientType.BabyYippees));
            if (rawCardJellyBlocksBtn != null) rawCardJellyBlocksBtn.onClick.AddListener(() => DepositRawIngredient(RawIngredientType.JellyBlocks));
            if (rawCardGoldenDewBtn != null) rawCardGoldenDewBtn.onClick.AddListener(() => DepositRawIngredient(RawIngredientType.GoldenDew));

            // Station Clicks (Start processing or Collect rewards)
            if (stationBlenderButton != null) stationBlenderButton.onClick.AddListener(OnBlenderClicked);
            if (stationChoppingButton != null) stationChoppingButton.onClick.AddListener(OnChoppingClicked);
            if (stationCentrifugeButton != null) stationCentrifugeButton.onClick.AddListener(OnCentrifugeClicked);

            if (prepAreaPanelRoot != null)
            {
                prepAreaPanelRoot.SetActive(false);
            }
        }

        public void OpenPrepAreaView(int dayNumber)
        {
            currentDayNumber = dayNumber;

            if (prepAreaPanelRoot != null)
            {
                prepAreaPanelRoot.SetActive(true);
            }

            if (prepAreaBackgroundImage != null && prepAreaInteriorSprite != null)
            {
                prepAreaBackgroundImage.sprite = prepAreaInteriorSprite;
            }

            UpdateUnlocksAndDisplay();
        }

        public void ClosePrepAreaView()
        {
            if (prepAreaPanelRoot != null)
            {
                prepAreaPanelRoot.SetActive(false);
            }

            OnPrepAreaClosed?.Invoke();
        }

        public void UpdateUnlocksAndDisplay()
        {
            int day = currentDayNumber;

            // 1. Unlocks for Top Raw Cards
            bool yippeesUnlocked = (day >= 5);
            bool jelliesUnlocked = (day >= 11);
            bool goldenDewUnlocked = (day >= 18);

            if (rawCardBabyYippees != null) rawCardBabyYippees.SetActive(yippeesUnlocked);
            if (rawCardJellyBlocks != null) rawCardJellyBlocks.SetActive(jelliesUnlocked);
            if (rawCardGoldenDew != null) rawCardGoldenDew.SetActive(goldenDewUnlocked);

            // Update raw stock counts
            if (InventoryManager.Instance != null)
            {
                if (rawCountBabyYippeesText != null)
                {
                    int c = InventoryManager.Instance.GetRawStock(RawIngredientType.BabyYippees);
                    rawCountBabyYippeesText.text = FormatCount(c);
                }
                if (rawCountJellyBlocksText != null)
                {
                    int c = InventoryManager.Instance.GetRawStock(RawIngredientType.JellyBlocks);
                    rawCountJellyBlocksText.text = FormatCount(c);
                }
                if (rawCountGoldenDewText != null)
                {
                    int c = InventoryManager.Instance.GetRawStock(RawIngredientType.GoldenDew);
                    rawCountGoldenDewText.text = FormatCount(c);
                }
            }

            // 2. Unlocks for Stations on the Desk
            if (stationBlenderRoot != null) stationBlenderRoot.SetActive(yippeesUnlocked);
            if (stationChoppingRoot != null) stationChoppingRoot.SetActive(jelliesUnlocked);
            if (stationCentrifugeRoot != null) stationCentrifugeRoot.SetActive(goldenDewUnlocked);

            // Update Station UI visuals and status texts
            UpdateBlenderUI();
            UpdateChoppingUI();
            UpdateCentrifugeUI();
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
                    if (InventoryManager.Instance.ConsumeRawStock(RawIngredientType.BabyYippees, 1))
                    {
                        blenderLoadedCount++;
                        blenderState = PrepStationState.Loaded;
                        PlaySound(loadIngredientSound);
                        UpdateUnlocksAndDisplay();
                        HUDController.Instance?.ShowNotification($"Loaded 1 Baby Yippee into the Blender! (Total loaded: {blenderLoadedCount})", 2.5f);
                    }
                    break;

                case RawIngredientType.JellyBlocks:
                    if (choppingState == PrepStationState.Processing || choppingState == PrepStationState.ReadyToCollect)
                    {
                        HUDController.Instance?.ShowNotification("Please collect your chopped Jelly cubes before loading a new batch!", 3f);
                        return;
                    }
                    if (InventoryManager.Instance.ConsumeRawStock(RawIngredientType.JellyBlocks, 1))
                    {
                        choppingLoadedCount++;
                        choppingState = PrepStationState.Loaded;
                        PlaySound(loadIngredientSound);
                        UpdateUnlocksAndDisplay();
                        HUDController.Instance?.ShowNotification($"Placed 1 Jelly Block on the Chopping Board! (Total loaded: {choppingLoadedCount})", 2.5f);
                    }
                    break;

                case RawIngredientType.GoldenDew:
                    if (centrifugeState == PrepStationState.Processing || centrifugeState == PrepStationState.ReadyToCollect)
                    {
                        HUDController.Instance?.ShowNotification("Please collect your refined gourmet toppings before spinning a new batch!", 3f);
                        return;
                    }
                    if (InventoryManager.Instance.ConsumeRawStock(RawIngredientType.GoldenDew, 1))
                    {
                        centrifugeLoadedCount++;
                        centrifugeState = PrepStationState.Loaded;
                        PlaySound(loadIngredientSound);
                        UpdateUnlocksAndDisplay();
                        HUDController.Instance?.ShowNotification($"Poured Golden Dew into the Bucket! (Total loaded: {centrifugeLoadedCount})", 2.5f);
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

        private IEnumerator ProcessBlenderRoutine()
        {
            blenderState = PrepStationState.Processing;
            UpdateBlenderUI();
            PlaySound(blendSound);

            // Shaking animation
            if (stationBlenderImage != null)
            {
                Vector3 origPos = stationBlenderImage.rectTransform.localPosition;
                float elapsed = 0f;
                float duration = 2.0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float offsetX = UnityEngine.Random.Range(-4f, 4f);
                    float offsetY = UnityEngine.Random.Range(-4f, 4f);
                    stationBlenderImage.rectTransform.localPosition = origPos + new Vector3(offsetX, offsetY, 0);
                    yield return null;
                }
                stationBlenderImage.rectTransform.localPosition = origPos;
            }
            else
            {
                yield return new WaitForSeconds(2.0f);
            }

            blenderState = PrepStationState.ReadyToCollect;
            UpdateBlenderUI();
            HUDController.Instance?.ShowNotification("✨ Blending & sieving complete! Click the station to collect fresh Boba!", 3.5f);
        }

        private void CollectBlenderYield()
        {
            if (blenderLoadedCount <= 0) return;

            int tapiocaYield = blenderLoadedCount * 1;
            int poppingBobaYield = blenderLoadedCount * 1;

            InventoryManager.Instance?.AddToppingStock(ToppingType.TapiocaPearls, tapiocaYield);
            InventoryManager.Instance?.AddToppingStock(ToppingType.PoppingBoba, poppingBobaYield);

            PlaySound(collectRewardSound);
            HUDController.Instance?.ShowNotification($"🎉 Collected <color=#2ECC71>+{tapiocaYield} Tapioca Pearls</color> and <color=#2ECC71>+{poppingBobaYield} Popping Boba</color>!", 4f);

            blenderLoadedCount = 0;
            blenderState = PrepStationState.Empty;
            UpdateUnlocksAndDisplay();
        }

        private void UpdateBlenderUI()
        {
            if (stationBlenderStatusText != null)
            {
                stationBlenderStatusText.text = blenderState switch
                {
                    PrepStationState.Empty => "<color=#8899AA>Empty (Load Yippees)</color>",
                    PrepStationState.Loaded => $"<color=#F1C40F>Ready ({blenderLoadedCount} Yippees)\nClick to Blend & Sieve!</color>",
                    PrepStationState.Processing => "<color=#3498DB>Blending & Sieving...</color>",
                    PrepStationState.ReadyToCollect => "<color=#2ECC71><b>✨ Click to Collect Boba!</b></color>",
                    _ => ""
                };
            }

            if (stationBlenderImage != null)
            {
                Sprite target = blenderState switch
                {
                    PrepStationState.Empty => blenderEmptySprite,
                    PrepStationState.Loaded => (blenderLoadedSprite != null ? blenderLoadedSprite : blenderEmptySprite),
                    PrepStationState.Processing => (blenderBlendedSprite != null ? blenderBlendedSprite : blenderEmptySprite),
                    PrepStationState.ReadyToCollect => (blenderBlendedSprite != null ? blenderBlendedSprite : blenderEmptySprite),
                    _ => blenderEmptySprite
                };
                if (target != null) stationBlenderImage.sprite = target;
            }
        }

        // =========================================================================
        // STATION 2: CHOPPING BOARD & KNIFE (GRASS JELLY & COCONUT JELLY)
        // =========================================================================
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
            else if (choppingState == PrepStationState.ReadyToCollect)
            {
                CollectChoppingYield();
            }
        }

        private IEnumerator ProcessChoppingRoutine()
        {
            choppingState = PrepStationState.Processing;
            UpdateChoppingUI();
            PlaySound(chopSound);

            if (stationKnifeImage != null)
            {
                Vector3 origPos = stationKnifeImage.rectTransform.localPosition;
                float elapsed = 0f;
                float duration = 1.8f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float chopOffset = Mathf.Sin(elapsed * 18f) * 12f;
                    stationKnifeImage.rectTransform.localPosition = origPos + new Vector3(0, chopOffset, 0);
                    yield return null;
                }
                stationKnifeImage.rectTransform.localPosition = origPos;
            }
            else
            {
                yield return new WaitForSeconds(1.8f);
            }

            choppingState = PrepStationState.ReadyToCollect;
            UpdateChoppingUI();
            HUDController.Instance?.ShowNotification("✨ Chopping complete! Click the board to collect diced jellies!", 3.5f);
        }

        private void CollectChoppingYield()
        {
            if (choppingLoadedCount <= 0) return;

            int grassJellyYield = choppingLoadedCount * 1;
            int coconutJellyYield = choppingLoadedCount * 1;

            InventoryManager.Instance?.AddToppingStock(ToppingType.GrassJelly, grassJellyYield);
            InventoryManager.Instance?.AddToppingStock(ToppingType.CoconutJelly, coconutJellyYield);

            PlaySound(collectRewardSound);
            HUDController.Instance?.ShowNotification($"🎉 Collected <color=#2ECC71>+{grassJellyYield} Grass Jelly</color> and <color=#2ECC71>+{coconutJellyYield} Coconut Jelly</color>!", 4f);

            choppingLoadedCount = 0;
            choppingState = PrepStationState.Empty;
            UpdateUnlocksAndDisplay();
        }

        private void UpdateChoppingUI()
        {
            if (stationChoppingStatusText != null)
            {
                stationChoppingStatusText.text = choppingState switch
                {
                    PrepStationState.Empty => "<color=#8899AA>Empty (Load Jelly Blocks)</color>",
                    PrepStationState.Loaded => $"<color=#F1C40F>Ready ({choppingLoadedCount} Blocks)\nClick to Chop!</color>",
                    PrepStationState.Processing => "<color=#3498DB>Chopping Jellies...</color>",
                    PrepStationState.ReadyToCollect => "<color=#2ECC71><b>✨ Click to Collect Jellies!</b></color>",
                    _ => ""
                };
            }

            if (stationChoppingImage != null)
            {
                Sprite target = choppingState switch
                {
                    PrepStationState.Empty => choppingEmptySprite,
                    PrepStationState.Loaded => (choppingLoadedSprite != null ? choppingLoadedSprite : choppingEmptySprite),
                    PrepStationState.Processing => (choppingChoppedSprite != null ? choppingChoppedSprite : choppingEmptySprite),
                    PrepStationState.ReadyToCollect => (choppingChoppedSprite != null ? choppingChoppedSprite : choppingEmptySprite),
                    _ => choppingEmptySprite
                };
                if (target != null) stationChoppingImage.sprite = target;
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

            if (stationCentrifugeImage != null)
            {
                float elapsed = 0f;
                float duration = 2.2f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    stationCentrifugeImage.rectTransform.Rotate(0, 0, -720f * Time.deltaTime);
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
            HUDController.Instance?.ShowNotification("✨ Centrifuge separation complete! Click to collect gourmet toppings!", 3.5f);
        }

        private void CollectCentrifugeYield()
        {
            if (centrifugeLoadedCount <= 0) return;

            int puddingYield = centrifugeLoadedCount * 1;
            int foamYield = centrifugeLoadedCount * 1;
            int honeyPearlsYield = centrifugeLoadedCount * 1;

            InventoryManager.Instance?.AddToppingStock(ToppingType.EggPudding, puddingYield);
            InventoryManager.Instance?.AddToppingStock(ToppingType.CheeseFoam, foamYield);
            InventoryManager.Instance?.AddToppingStock(ToppingType.GoldenHoneyPearls, honeyPearlsYield);

            PlaySound(collectRewardSound);
            HUDController.Instance?.ShowNotification($"🎉 Collected <color=#2ECC71>+{puddingYield} Egg Custard</color>, <color=#2ECC71>+{foamYield} Cheese Foam</color>, and <color=#2ECC71>+{honeyPearlsYield} Honey Pearls</color>!", 4.5f);

            centrifugeLoadedCount = 0;
            centrifugeState = PrepStationState.Empty;
            UpdateUnlocksAndDisplay();
        }

        private void UpdateCentrifugeUI()
        {
            if (stationCentrifugeStatusText != null)
            {
                stationCentrifugeStatusText.text = centrifugeState switch
                {
                    PrepStationState.Empty => "<color=#8899AA>Empty (Pour Golden Dew)</color>",
                    PrepStationState.Loaded => $"<color=#F1C40F>Ready ({centrifugeLoadedCount} Dew)\nClick to Spin Centrifuge!</color>",
                    PrepStationState.Processing => "<color=#3498DB>Spinning & Separating...</color>",
                    PrepStationState.ReadyToCollect => "<color=#2ECC71><b>✨ Click to Collect Toppings!</b></color>",
                    _ => ""
                };
            }

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
