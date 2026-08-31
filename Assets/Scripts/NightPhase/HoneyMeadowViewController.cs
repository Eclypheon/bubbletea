using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    public class HoneyMeadowViewController : MonoBehaviour, IPointerClickHandler
    {
        public static HoneyMeadowViewController Instance { get; private set; }

        [Header("Root & Screen Panels")]
        [SerializeField] private GameObject honeyMeadowPanelRoot;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite honeyMeadowBackgroundSprite;
        [SerializeField] private Button returnToNightHubButton;

        [Header("UI Header & Basket")]
        [SerializeField] private TextMeshProUGUI harvestCounterText;

        [Header("Jelly Tree & Centerpiece")]
        [SerializeField] private Transform jellyTreeContainer;
        [SerializeField] private Image jellyTreeImage;
        [SerializeField] private Sprite jellyTreeSprite;
        [SerializeField] private Sprite rawJellyBlockSprite;

        [Header("Harvest Spawning Tuning (Editable)")]
        [Range(2, 8)]
        [SerializeField] private int minJellyBlocks = 3;
        [Range(2, 8)]
        [SerializeField] private int maxJellyBlocks = 5;

        [Header("Soil Absorption Speed (Editable)")]
        [Tooltip("Seconds before a fallen jelly block on the floor completely dissolves into the soil.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float soilAbsorptionSeconds = 2.0f;

        [Header("Audio SFX")]
        [SerializeField] private AudioClip treeKickSound;
        [SerializeField] private AudioClip harvestCollectSound;
        [SerializeField] private AudioClip completeSound;

        public event Action OnHoneyMeadowClosed;

        private int sessionCaughtCount = 0;
        private int totalSpawnedCount = 0;
        private int remainingActiveGroundBlocks = 0;
        private bool isMeadowOpen = false;
        private bool hasKickedTree = false;
        private List<Coroutine> activeDropRoutines = new List<Coroutine>();
        private Vector2 rootPanelBaseAnchoredPos = Vector2.zero;

        private const string IDLE_HINT = "Kick the tree hard to dislodge any loose jellies!";
        private const string DROPPED_HINT = "Quick! Pick up the jellies before they dissolve into the floor";
        private const string CLEARED_SUCCESS_HINT = "You have successfully collected all of the jelly blocks";
        private const string CLEARED_CONSUMED_HINT = "The jelly blocks have been consumed by the mysterious soil";

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

            if (honeyMeadowPanelRoot == null)
            {
                EnsureMeadowPanelHierarchy();
            }

            WireTreeAndButtonListeners();

            if (honeyMeadowPanelRoot != null)
            {
                var rt = honeyMeadowPanelRoot.GetComponent<RectTransform>();
                if (rt != null) rootPanelBaseAnchoredPos = rt.anchoredPosition;
                honeyMeadowPanelRoot.SetActive(false);
            }
        }

        private void Start()
        {
            WireTreeAndButtonListeners();
        }

        private void ResolveComponentReferences()
        {
            if (honeyMeadowPanelRoot == null) honeyMeadowPanelRoot = gameObject;
            if (backgroundImage == null && honeyMeadowPanelRoot != null)
            {
                backgroundImage = honeyMeadowPanelRoot.GetComponent<Image>();
            }
            if (jellyTreeContainer == null)
            {
                Transform t = transform.Find("JellyTree");
                if (t != null) jellyTreeContainer = t;
            }
            if (jellyTreeImage == null && jellyTreeContainer != null)
            {
                jellyTreeImage = jellyTreeContainer.GetComponent<Image>();
            }
            if (harvestCounterText == null && honeyMeadowPanelRoot != null)
            {
                harvestCounterText = honeyMeadowPanelRoot.GetComponentInChildren<TextMeshProUGUI>();
            }
            if (returnToNightHubButton == null && honeyMeadowPanelRoot != null)
            {
                var btns = honeyMeadowPanelRoot.GetComponentsInChildren<Button>(true);
                foreach (var b in btns)
                {
                    if (b.gameObject.name.ToLower().Contains("return") || b.gameObject.name.ToLower().Contains("hub"))
                    {
                        returnToNightHubButton = b;
                        break;
                    }
                }
            }
        }

        private void WireTreeAndButtonListeners()
        {
            ResolveComponentReferences();

            GameObject treeTarget = null;
            if (jellyTreeContainer != null) treeTarget = jellyTreeContainer.gameObject;
            else if (jellyTreeImage != null) treeTarget = jellyTreeImage.gameObject;

            if (treeTarget != null)
            {
                var img = treeTarget.GetComponent<Image>();
                if (img != null)
                {
                    img.raycastTarget = true;
                    if (jellyTreeSprite != null && img.sprite == null) img.sprite = jellyTreeSprite;
                }

                var btn = treeTarget.GetComponent<Button>();
                if (btn == null) btn = treeTarget.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.interactable = true;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnTreeClicked);
            }

            if (returnToNightHubButton != null)
            {
                returnToNightHubButton.onClick.RemoveAllListeners();
                returnToNightHubButton.onClick.AddListener(CloseHoneyMeadowView);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // If the player clicks anywhere in the panel before kicking the tree, trigger tree kick!
            if (isMeadowOpen && !hasKickedTree)
            {
                OnTreeClicked();
            }
        }

        private void EnsureFallbackAssets()
        {
#if UNITY_EDITOR
            if (honeyMeadowBackgroundSprite == null)
            {
                honeyMeadowBackgroundSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Sprites2/Sprites 3/Forage and prep/Meadows/meadows.jpg");
            }
            if (jellyTreeSprite == null)
            {
                jellyTreeSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Sprites2/Sprites 3/Forage and prep/Meadows/jellytree.png");
            }
            if (rawJellyBlockSprite == null)
            {
                var allRaw = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Sprites2/Sprites 3/Forage and prep/raw ingre.png");
                foreach (var a in allRaw)
                {
                    if (a is Sprite s && (s.name == "raw ingre_0" || s.name == "raw ingre_1"))
                    {
                        rawJellyBlockSprite = s;
                        break;
                    }
                }
            }
#endif
            if (honeyMeadowBackgroundSprite == null || jellyTreeSprite == null || rawJellyBlockSprite == null)
            {
                var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
                for (int i = 0; i < allSprites.Length; i++)
                {
                    var s = allSprites[i];
                    if (s == null) continue;
                    if (honeyMeadowBackgroundSprite == null && (s.name.ToLower().Contains("meadow") || s.name.ToLower().Contains("honey")))
                    {
                        honeyMeadowBackgroundSprite = s;
                    }
                    if (jellyTreeSprite == null && (s.name.ToLower().Contains("jellytree") || s.name.ToLower().Contains("tree")))
                    {
                        jellyTreeSprite = s;
                    }
                    if (rawJellyBlockSprite == null && (s.name == "raw ingre_0" || s.name == "raw ingre_1"))
                    {
                        rawJellyBlockSprite = s;
                    }
                }
            }
        }

        private void EnsureMeadowPanelHierarchy()
        {
            Transform parentCanvas = null;
            if (BambooGroveViewController.Instance != null && BambooGroveViewController.Instance.transform.parent != null)
            {
                parentCanvas = BambooGroveViewController.Instance.transform.parent;
            }
            else if (NightPhaseManager.Instance != null && NightPhaseManager.Instance.transform.parent != null)
            {
                parentCanvas = NightPhaseManager.Instance.transform.parent;
            }
            else
            {
                var canvas = FindObjectOfType<Canvas>();
                if (canvas != null) parentCanvas = canvas.transform;
            }

            if (parentCanvas == null) parentCanvas = transform;

            // Fullscreen panel root
            GameObject rootObj = new GameObject("HoneyMeadowViewPanel", typeof(RectTransform), typeof(Image));
            rootObj.transform.SetParent(parentCanvas, false);
            var rootRt = rootObj.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            backgroundImage = rootObj.GetComponent<Image>();
            if (honeyMeadowBackgroundSprite != null)
            {
                backgroundImage.sprite = honeyMeadowBackgroundSprite;
            }
            backgroundImage.color = Color.white;
            backgroundImage.raycastTarget = true;

            honeyMeadowPanelRoot = rootObj;
            rootPanelBaseAnchoredPos = rootRt.anchoredPosition;

            // Header Harvest Counter
            GameObject counterObj = new GameObject("HarvestCounterText", typeof(RectTransform), typeof(TextMeshProUGUI));
            counterObj.transform.SetParent(rootObj.transform, false);
            var countRt = counterObj.GetComponent<RectTransform>();
            countRt.anchorMin = new Vector2(0.5f, 1f);
            countRt.anchorMax = new Vector2(0.5f, 1f);
            countRt.pivot = new Vector2(0.5f, 1f);
            countRt.anchoredPosition = new Vector2(0f, -22f);
            countRt.sizeDelta = new Vector2(620f, 44f);

            harvestCounterText = counterObj.GetComponent<TextMeshProUGUI>();
            harvestCounterText.fontSize = 24;
            harvestCounterText.alignment = TextAlignmentOptions.Center;
            harvestCounterText.color = Color.white;

            // Return to Night Hub Button
            GameObject retBtnObj = new GameObject("ReturnToNightHubBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            retBtnObj.transform.SetParent(rootObj.transform, false);
            var retRt = retBtnObj.GetComponent<RectTransform>();
            retRt.anchorMin = new Vector2(1f, 1f);
            retRt.anchorMax = new Vector2(1f, 1f);
            retRt.pivot = new Vector2(1f, 1f);
            retRt.anchoredPosition = new Vector2(-25f, -20f);
            retRt.sizeDelta = new Vector2(230f, 48f);

            var retImg = retBtnObj.GetComponent<Image>();
            retImg.color = new Color(0.18f, 0.22f, 0.28f, 0.95f);

            returnToNightHubButton = retBtnObj.GetComponent<Button>();

            GameObject retTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            retTextObj.transform.SetParent(retBtnObj.transform, false);
            var retTxtRt = retTextObj.GetComponent<RectTransform>();
            retTxtRt.anchorMin = Vector2.zero;
            retTxtRt.anchorMax = Vector2.one;
            retTxtRt.offsetMin = Vector2.zero;
            retTxtRt.offsetMax = Vector2.zero;

            var retTmp = retTextObj.GetComponent<TextMeshProUGUI>();
            retTmp.text = "Return to Shop";
            retTmp.fontSize = 19;
            retTmp.alignment = TextAlignmentOptions.Center;
            retTmp.color = Color.white;

            // Jelly Tree Container & Image
            GameObject treeObj = new GameObject("JellyTree", typeof(RectTransform), typeof(Image), typeof(Button));
            treeObj.transform.SetParent(rootObj.transform, false);
            var treeRt = treeObj.GetComponent<RectTransform>();
            treeRt.anchorMin = new Vector2(0.5f, 0.5f);
            treeRt.anchorMax = new Vector2(0.5f, 0.5f);
            treeRt.pivot = new Vector2(0.5f, 0.5f);
            treeRt.anchoredPosition = new Vector2(0f, -20f);
            treeRt.sizeDelta = new Vector2(560f, 540f);

            jellyTreeImage = treeObj.GetComponent<Image>();
            if (jellyTreeSprite != null)
            {
                jellyTreeImage.sprite = jellyTreeSprite;
            }
            jellyTreeImage.preserveAspect = true;
            jellyTreeImage.raycastTarget = true;
            jellyTreeContainer = treeObj.transform;

            var treeBtn = treeObj.GetComponent<Button>();
            treeBtn.transition = Selectable.Transition.None;
            treeBtn.onClick.AddListener(OnTreeClicked);
        }

        public void OpenHoneyMeadowView(int dayNumber)
        {
            isMeadowOpen = true;
            sessionCaughtCount = 0;
            totalSpawnedCount = 0;
            remainingActiveGroundBlocks = 0;
            hasKickedTree = false;

            ResolveComponentReferences();
            EnsureFallbackAssets();

            if (honeyMeadowPanelRoot == null)
            {
                EnsureMeadowPanelHierarchy();
            }

            WireTreeAndButtonListeners();

            if (honeyMeadowPanelRoot != null)
            {
                honeyMeadowPanelRoot.SetActive(true);
                var rt = honeyMeadowPanelRoot.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = rootPanelBaseAnchoredPos;
            }

            if (returnToNightHubButton != null)
            {
                returnToNightHubButton.gameObject.SetActive(true);
                returnToNightHubButton.transform.SetAsLastSibling();
            }

            if (backgroundImage != null && honeyMeadowBackgroundSprite != null)
            {
                backgroundImage.sprite = honeyMeadowBackgroundSprite;
            }

            if (jellyTreeImage != null && jellyTreeSprite != null)
            {
                jellyTreeImage.sprite = jellyTreeSprite;
                jellyTreeImage.raycastTarget = true;
            }

            // Tree stays completely static at the start
            if (jellyTreeContainer != null)
            {
                jellyTreeContainer.localRotation = Quaternion.identity;
                jellyTreeContainer.localScale = Vector3.one;
            }

            ClearAllFallenBlocks();
            HUDController.Instance?.SetSubscreenMode(true, IDLE_HINT);
            UpdateHarvestCounterDisplay();
        }

        public void CloseHoneyMeadowView()
        {
            isMeadowOpen = false;

            StopAllDropRoutines();
            ClearAllFallenBlocks();

            if (honeyMeadowPanelRoot != null)
            {
                var rt = honeyMeadowPanelRoot.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = rootPanelBaseAnchoredPos;
                honeyMeadowPanelRoot.SetActive(false);
            }

            if (sessionCaughtCount > 0)
            {
                PlaySound(completeSound);
                HUDController.Instance?.ShowNotification($"Meadow expedition complete! Bagged <color=#2ECC71>+{sessionCaughtCount} Raw Jelly Blocks</color>!", 4.5f);
            }

            if (returnToNightHubButton != null)
            {
                returnToNightHubButton.gameObject.SetActive(false);
            }

            HUDController.Instance?.SetSubscreenMode(false);
            OnHoneyMeadowClosed?.Invoke();
        }

        private void UpdateHarvestCounterDisplay()
        {
            if (harvestCounterText != null)
            {
                harvestCounterText.text = $"Expedition Harvest: <color=#2ECC71>+{sessionCaughtCount}</color> Raw Jelly Blocks";
            }
        }

        // =========================================================================
        // KICK TREE & VIOLENT SCREEN SHAKE
        // =========================================================================
        public void OnTreeClicked()
        {
            if (hasKickedTree || !isMeadowOpen) return;

            hasKickedTree = true;
            PlaySound(treeKickSound);

            // Violent screen shake and tree impact
            StartCoroutine(ViolentScreenAndTreeShakeRoutine());

            // Update status hint
            HUDController.Instance?.SetStatusHint(DROPPED_HINT);

            // Spawn and dislodge all jelly blocks
            SpawnAndDropJellyBlocks();
        }

        private IEnumerator ViolentScreenAndTreeShakeRoutine()
        {
            RectTransform panelRt = (honeyMeadowPanelRoot != null) ? honeyMeadowPanelRoot.GetComponent<RectTransform>() : null;
            RectTransform treeRt = jellyTreeContainer as RectTransform;

            float elapsed = 0f;
            float duration = 0.55f;

            while (elapsed < duration && isMeadowOpen)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float intensity = 1f - progress; // Decays over time

                // Violent screen offset
                if (panelRt != null)
                {
                    float shakeX = UnityEngine.Random.Range(-22f, 22f) * intensity;
                    float shakeY = UnityEngine.Random.Range(-18f, 18f) * intensity;
                    panelRt.anchoredPosition = rootPanelBaseAnchoredPos + new Vector2(shakeX, shakeY);
                }

                // Tree violent tilt and recoil
                if (treeRt != null)
                {
                    float angle = Mathf.Sin(elapsed * 55f) * (12f * intensity);
                    float scaleMod = 1f + (Mathf.Sin(elapsed * 60f) * (0.07f * intensity));
                    treeRt.localRotation = Quaternion.Euler(0, 0, angle);
                    treeRt.localScale = new Vector3(scaleMod, scaleMod, 1f);
                }

                yield return null;
            }

            // Restore clean transform positions
            if (panelRt != null) panelRt.anchoredPosition = rootPanelBaseAnchoredPos;
            if (treeRt != null)
            {
                treeRt.localRotation = Quaternion.identity;
                treeRt.localScale = Vector3.one;
            }
        }

        // =========================================================================
        // JELLY BLOCK DROP & SOIL ABSORPTION
        // =========================================================================
        private void SpawnAndDropJellyBlocks()
        {
            StopAllDropRoutines();
            ClearAllFallenBlocks();

            Transform container = (honeyMeadowPanelRoot != null) ? honeyMeadowPanelRoot.transform : transform;

            // Branch origin anchor points
            List<Vector2> branchAnchors = new List<Vector2>
            {
                new Vector2(-155f, 95f),
                new Vector2(-75f, 165f),
                new Vector2(65f, 145f),
                new Vector2(165f, 85f),
                new Vector2(0f, 115f),
                new Vector2(-115f, 30f),
                new Vector2(115f, 35f)
            };

            // Shuffle
            for (int i = 0; i < branchAnchors.Count; i++)
            {
                int rnd = UnityEngine.Random.Range(i, branchAnchors.Count);
                var temp = branchAnchors[i];
                branchAnchors[i] = branchAnchors[rnd];
                branchAnchors[rnd] = temp;
            }

            int spawnCount = Mathf.Clamp(UnityEngine.Random.Range(minJellyBlocks, maxJellyBlocks + 1), 2, branchAnchors.Count);
            totalSpawnedCount = spawnCount;
            remainingActiveGroundBlocks = spawnCount;

            for (int i = 0; i < spawnCount; i++)
            {
                Vector2 branchPos = branchAnchors[i];

                GameObject blockObj = new GameObject($"FallenJelly_{i + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
                blockObj.transform.SetParent(container, false);
                var rt = blockObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(66f, 66f);
                rt.anchoredPosition = branchPos;

                var img = blockObj.GetComponent<Image>();
                if (rawJellyBlockSprite != null)
                {
                    img.sprite = rawJellyBlockSprite;
                }
                img.preserveAspect = true;
                img.raycastTarget = true;

                var btn = blockObj.GetComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => OnJellyBlockCollected(blockObj));

                float staggerDelay = i * 0.06f;
                var coroutine = StartCoroutine(JellyBlockDropAndAbsorbRoutine(rt, blockObj, branchPos, staggerDelay));
                activeDropRoutines.Add(coroutine);
            }
        }

        private IEnumerator JellyBlockDropAndAbsorbRoutine(RectTransform rt, GameObject obj, Vector2 branchStartPos, float startDelay)
        {
            if (rt == null) yield break;

            if (startDelay > 0f)
            {
                rt.localScale = Vector3.zero;
                yield return new WaitForSeconds(startDelay);
                if (rt == null) yield break;
                rt.localScale = Vector3.one;
            }

            // 1. Initial Parabolic Fall Arc
            float targetGroundY = UnityEngine.Random.Range(-210f, -260f);
            float targetGroundX = branchStartPos.x + UnityEngine.Random.Range(-70f, 70f);
            targetGroundX = Mathf.Clamp(targetGroundX, -380f, 380f);

            float dropDuration = 0.45f;
            float elapsed = 0f;
            float spinSpeed = UnityEngine.Random.Range(-240f, 240f);

            while (elapsed < dropDuration && rt != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dropDuration;
                float curX = Mathf.Lerp(branchStartPos.x, targetGroundX, t);
                // EaseIn quad for accelerating gravity
                float curY = Mathf.Lerp(branchStartPos.y, targetGroundY, t * t);

                rt.anchoredPosition = new Vector2(curX, curY);
                rt.localRotation = Quaternion.Euler(0, 0, t * spinSpeed);
                yield return null;
            }

            // 2. Bounce on Meadow Floor
            if (rt != null)
            {
                float bounceHeight = 28f;
                float bounceDuration = 0.20f;
                elapsed = 0f;
                Vector2 groundPos = new Vector2(targetGroundX, targetGroundY);

                while (elapsed < bounceDuration && rt != null)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / bounceDuration;
                    float arcY = Mathf.Sin(t * Mathf.PI) * bounceHeight;
                    rt.anchoredPosition = groundPos + new Vector2(t * 10f, arcY);
                    yield return null;
                }
            }

            // 3. Ground Soil Absorption (Shrinking to zero over soilAbsorptionSeconds)
            if (rt != null)
            {
                rt.localRotation = Quaternion.identity;
                float absorbDuration = Mathf.Max(0.2f, soilAbsorptionSeconds);
                elapsed = 0f;

                while (elapsed < absorbDuration && rt != null && rt.gameObject.activeInHierarchy)
                {
                    elapsed += Time.deltaTime;
                    float shrinkProgress = elapsed / absorbDuration;
                    float currentScale = Mathf.Clamp01(1f - shrinkProgress);

                    rt.localScale = new Vector3(currentScale, currentScale, 1f);
                    yield return null;
                }

                // Absorbed into the soil
                if (rt != null)
                {
                    Destroy(rt.gameObject);
                    remainingActiveGroundBlocks = Mathf.Max(0, remainingActiveGroundBlocks - 1);
                    CheckAllBlocksResolved();
                }
            }
        }

        // =========================================================================
        // COLLECT JELLY BLOCK INTERACTION
        // =========================================================================
        private void OnJellyBlockCollected(GameObject blockObj)
        {
            if (blockObj == null) return;

            PlaySound(harvestCollectSound);

            sessionCaughtCount++;
            remainingActiveGroundBlocks = Mathf.Max(0, remainingActiveGroundBlocks - 1);
            InventoryManager.Instance?.AddRawStock(RawIngredientType.JellyBlocks, 1);

            UpdateHarvestCounterDisplay();
            SpawnCollectPopText(blockObj.transform.position, "+1 Raw Jelly Block!");

            // 15% rare bonus loot roll (Wild Honeycomb / Golden Dew)
            if (UnityEngine.Random.value < 0.15f)
            {
                InventoryManager.Instance?.AddToppingStock(ToppingType.GoldenHoneyPearls, 1);
                EconomyManager.Instance?.AddCash(5.00f, "Wild Honey Meadow Discovery");
                SpawnCollectPopText(blockObj.transform.position + new Vector3(0, 30f, 0), "<color=#F1C40F>+Wild Honey Pearls (+$5.00)</color>");
            }

            Destroy(blockObj);
            CheckAllBlocksResolved();
        }

        private void CheckAllBlocksResolved()
        {
            if (remainingActiveGroundBlocks <= 0 && hasKickedTree)
            {
                if (sessionCaughtCount == totalSpawnedCount)
                {
                    PlaySound(completeSound);
                    HUDController.Instance?.SetStatusHint(CLEARED_SUCCESS_HINT);
                    HUDController.Instance?.ShowNotification($"You have successfully collected all of the jelly blocks! (Harvested: <color=#2ECC71>+{sessionCaughtCount}</color>)", 4f);
                }
                else
                {
                    HUDController.Instance?.SetStatusHint(CLEARED_CONSUMED_HINT);
                    HUDController.Instance?.ShowNotification("The jelly blocks have been consumed by the mysterious soil.", 4f);
                }
            }
        }

        private void SpawnCollectPopText(Vector3 worldPos, string text)
        {
            GameObject popObj = new GameObject("CollectPopText", typeof(RectTransform), typeof(TextMeshProUGUI));
            popObj.transform.SetParent(honeyMeadowPanelRoot.transform, false);
            popObj.transform.position = worldPos;

            var tmp = popObj.GetComponent<TextMeshProUGUI>();
            tmp.text = $"<color=#2ECC71><b>{text}</b></color>";
            tmp.fontSize = 22;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            StartCoroutine(FloatAndFadeRoutine(popObj));
        }

        private IEnumerator FloatAndFadeRoutine(GameObject popObj)
        {
            if (popObj == null) yield break;
            var rt = popObj.GetComponent<RectTransform>();
            var tmp = popObj.GetComponent<TextMeshProUGUI>();
            float elapsed = 0f;
            float duration = 0.85f;
            Vector3 startPos = rt.anchoredPosition;

            while (elapsed < duration && popObj != null)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                rt.anchoredPosition = startPos + new Vector3(0, progress * 45f, 0);
                if (tmp != null) tmp.alpha = 1f - progress;
                yield return null;
            }

            if (popObj != null) Destroy(popObj);
        }

        private void StopAllDropRoutines()
        {
            foreach (var routine in activeDropRoutines)
            {
                if (routine != null) StopCoroutine(routine);
            }
            activeDropRoutines.Clear();
        }

        private void ClearAllFallenBlocks()
        {
            Transform container = (honeyMeadowPanelRoot != null) ? honeyMeadowPanelRoot.transform : transform;
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Transform child = container.GetChild(i);
                if (child.name.StartsWith("FallenJelly_") || child.name.StartsWith("CollectPopText"))
                {
                    Destroy(child.gameObject);
                }
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
