using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    public class HoneyMeadowViewController : MonoBehaviour
    {
        public static HoneyMeadowViewController Instance { get; private set; }

        [Header("Root & Screen Panels")]
        [SerializeField] private GameObject honeyMeadowPanelRoot;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite honeyMeadowBackgroundSprite;
        [SerializeField] private Button returnToNightHubButton;

        [Header("UI Header & Basket")]
        [SerializeField] private TextMeshProUGUI harvestCounterText;

        [Header("Jelly Tree & Branches (Editable)")]
        [SerializeField] private Transform jellyTreeContainer;
        [SerializeField] private Image jellyTreeImage;
        [SerializeField] private Sprite jellyTreeSprite;
        [SerializeField] private Sprite rawJellyBlockSprite;

        [Header("Harvest Spawning Tuning (Editable)")]
        [Range(2, 8)]
        [SerializeField] private int minJellyBlocks = 3;
        [Range(2, 8)]
        [SerializeField] private int maxJellyBlocks = 5;

        [Header("Audio SFX")]
        [SerializeField] private AudioClip treeRustleSound;
        [SerializeField] private AudioClip harvestCollectSound;
        [SerializeField] private AudioClip completeSound;

        public event Action OnHoneyMeadowClosed;

        private int sessionCaughtCount = 0;
        private int remainingHangingBlocks = 0;
        private int remainingActiveGroundBlocks = 0;
        private bool isMeadowOpen = false;
        private Coroutine treeIdleSwayCoroutine;
        private List<Coroutine> activePendulumRoutines = new List<Coroutine>();
        private List<HangingJellyBlock> hangingBlocks = new List<HangingJellyBlock>();

        private const string IDLE_HINT = "Honey Meadows: Shake the Jelly Tree to drop ripe Jelly Blocks, then collect them!";
        private const string DROPPED_HINT = "Ripe Jelly Blocks have fallen onto the meadow! Tap them to collect!";
        private const string CLEARED_HINT = "You have harvested all the ripe Jelly Blocks! Time to head back.";

        private class HangingJellyBlock
        {
            public GameObject gameObject;
            public RectTransform rectTransform;
            public Vector2 branchLocalPos;
            public bool isDislodged;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureFallbackAssets();

            if (honeyMeadowPanelRoot == null)
            {
                EnsureMeadowPanelHierarchy();
            }

            if (returnToNightHubButton != null)
            {
                returnToNightHubButton.onClick.AddListener(CloseHoneyMeadowView);
                returnToNightHubButton.gameObject.SetActive(false);
            }

            if (honeyMeadowPanelRoot != null)
            {
                honeyMeadowPanelRoot.SetActive(false);
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

            honeyMeadowPanelRoot = rootObj;

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
            jellyTreeContainer = treeObj.transform;

            var treeBtn = treeObj.GetComponent<Button>();
            treeBtn.transition = Selectable.Transition.None;
            treeBtn.onClick.AddListener(OnTreeClicked);
        }

        public void OpenHoneyMeadowView(int dayNumber)
        {
            isMeadowOpen = true;
            sessionCaughtCount = 0;
            remainingHangingBlocks = 0;
            remainingActiveGroundBlocks = 0;

            EnsureFallbackAssets();

            if (honeyMeadowPanelRoot == null)
            {
                EnsureMeadowPanelHierarchy();
            }

            if (honeyMeadowPanelRoot != null)
            {
                honeyMeadowPanelRoot.SetActive(true);
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
            }

            HUDController.Instance?.SetSubscreenMode(true, IDLE_HINT);

            UpdateHarvestCounterDisplay();
            SetupTreeAndHangingBlocks();
            StartTreeIdleSway();
        }

        public void CloseHoneyMeadowView()
        {
            isMeadowOpen = false;

            StopTreeIdleSway();
            StopAllPendulumRoutines();
            ClearAllHangingAndGroundBlocks();

            if (sessionCaughtCount > 0)
            {
                PlaySound(completeSound);
                HUDController.Instance?.ShowNotification($"Meadow expedition complete! Bagged <color=#2ECC71>+{sessionCaughtCount} Raw Jelly Blocks</color>!", 4.5f);
            }

            if (returnToNightHubButton != null)
            {
                returnToNightHubButton.gameObject.SetActive(false);
            }

            if (honeyMeadowPanelRoot != null)
            {
                honeyMeadowPanelRoot.SetActive(false);
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
        // JELLY TREE SETUP & IDLE SWAY
        // =========================================================================
        private void SetupTreeAndHangingBlocks()
        {
            StopAllPendulumRoutines();
            ClearAllHangingAndGroundBlocks();

            if (jellyTreeContainer == null) return;

            // Ripe branch anchor positions relative to tree center
            List<Vector2> branchAnchors = new List<Vector2>
            {
                new Vector2(-155f, 105f),
                new Vector2(-75f, 175f),
                new Vector2(65f, 155f),
                new Vector2(165f, 95f),
                new Vector2(0f, 125f),
                new Vector2(-115f, 40f),
                new Vector2(115f, 45f)
            };

            // Shuffle branch positions
            for (int i = 0; i < branchAnchors.Count; i++)
            {
                int rnd = UnityEngine.Random.Range(i, branchAnchors.Count);
                var temp = branchAnchors[i];
                branchAnchors[i] = branchAnchors[rnd];
                branchAnchors[rnd] = temp;
            }

            int spawnCount = Mathf.Clamp(UnityEngine.Random.Range(minJellyBlocks, maxJellyBlocks + 1), 2, branchAnchors.Count);
            remainingHangingBlocks = spawnCount;

            for (int i = 0; i < spawnCount; i++)
            {
                Vector2 anchor = branchAnchors[i];

                GameObject blockObj = new GameObject($"HangingJelly_{i + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
                blockObj.transform.SetParent(jellyTreeContainer, false);
                var rt = blockObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.9f); // pivot at stem top for pendulum swing
                rt.sizeDelta = new Vector2(62f, 62f);
                rt.anchoredPosition = anchor;

                var img = blockObj.GetComponent<Image>();
                if (rawJellyBlockSprite != null)
                {
                    img.sprite = rawJellyBlockSprite;
                }
                img.preserveAspect = true;

                HangingJellyBlock hb = new HangingJellyBlock
                {
                    gameObject = blockObj,
                    rectTransform = rt,
                    branchLocalPos = anchor,
                    isDislodged = false
                };
                hangingBlocks.Add(hb);

                var btn = blockObj.GetComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => DislodgeHangingBlock(hb));

                var swingCoroutine = StartCoroutine(PendulumSwingRoutine(rt, 2.2f + (i * 0.4f), i * 1.5f));
                activePendulumRoutines.Add(swingCoroutine);
            }
        }

        private void StartTreeIdleSway()
        {
            StopTreeIdleSway();
            if (jellyTreeContainer != null)
            {
                treeIdleSwayCoroutine = StartCoroutine(TreeIdleSwayRoutine(jellyTreeContainer as RectTransform));
            }
        }

        private void StopTreeIdleSway()
        {
            if (treeIdleSwayCoroutine != null)
            {
                StopCoroutine(treeIdleSwayCoroutine);
                treeIdleSwayCoroutine = null;
            }
        }

        private IEnumerator TreeIdleSwayRoutine(RectTransform treeRt)
        {
            if (treeRt == null) yield break;
            Vector3 baseScale = Vector3.one;

            while (isMeadowOpen && treeRt != null)
            {
                float t = Time.time * 1.4f;
                float angle = Mathf.Sin(t) * 1.6f;
                float scalePulse = 1f + (Mathf.Sin(t * 2f) * 0.015f);

                treeRt.localRotation = Quaternion.Euler(0, 0, angle);
                treeRt.localScale = new Vector3(baseScale.x * scalePulse, baseScale.y * scalePulse, 1f);
                yield return null;
            }
        }

        private IEnumerator PendulumSwingRoutine(RectTransform rt, float speed, float phaseOffset)
        {
            if (rt == null) yield break;

            while (isMeadowOpen && rt != null && rt.gameObject.activeInHierarchy)
            {
                float t = (Time.time * speed) + phaseOffset;
                float angle = Mathf.Sin(t) * 10f;
                rt.localRotation = Quaternion.Euler(0, 0, angle);
                yield return null;
            }
        }

        private void StopAllPendulumRoutines()
        {
            foreach (var routine in activePendulumRoutines)
            {
                if (routine != null) StopCoroutine(routine);
            }
            activePendulumRoutines.Clear();
        }

        // =========================================================================
        // TREE CLICK & SHAKE INTERACTION
        // =========================================================================
        private bool isTreeShaking = false;

        private void OnTreeClicked()
        {
            if (isTreeShaking || !isMeadowOpen) return;

            PlaySound(treeRustleSound);
            StartCoroutine(VigorousTreeShakeRoutine());
        }

        private IEnumerator VigorousTreeShakeRoutine()
        {
            isTreeShaking = true;
            RectTransform treeRt = jellyTreeContainer as RectTransform;

            if (treeRt != null)
            {
                float elapsed = 0f;
                float duration = 0.45f;

                while (elapsed < duration && treeRt != null)
                {
                    elapsed += Time.deltaTime;
                    float angle = Mathf.Sin(elapsed * 45f) * 6.5f;
                    float scale = 1f + (Mathf.Sin(elapsed * 50f) * 0.04f);
                    treeRt.localRotation = Quaternion.Euler(0, 0, angle);
                    treeRt.localScale = new Vector3(scale, scale, 1f);
                    yield return null;
                }

                if (treeRt != null)
                {
                    treeRt.localRotation = Quaternion.identity;
                    treeRt.localScale = Vector3.one;
                }
            }

            // Dislodge 1 to 2 undisplaced hanging blocks
            List<HangingJellyBlock> available = hangingBlocks.FindAll(b => !b.isDislodged && b.gameObject != null);
            if (available.Count > 0)
            {
                int countToDrop = Mathf.Clamp(UnityEngine.Random.Range(1, 3), 1, available.Count);
                for (int i = 0; i < countToDrop; i++)
                {
                    DislodgeHangingBlock(available[i]);
                }
            }

            isTreeShaking = false;
        }

        // =========================================================================
        // DISLODGE & BOUNCE PHYSICS
        // =========================================================================
        private void DislodgeHangingBlock(HangingJellyBlock hb)
        {
            if (hb == null || hb.isDislodged || hb.gameObject == null) return;

            hb.isDislodged = true;
            remainingHangingBlocks = Mathf.Max(0, remainingHangingBlocks - 1);
            remainingActiveGroundBlocks++;

            HUDController.Instance?.SetStatusHint(DROPPED_HINT);

            // Re-parent to meadow panel root so it falls naturally across meadow space
            Vector3 worldPos = hb.rectTransform.position;
            hb.gameObject.transform.SetParent(honeyMeadowPanelRoot.transform, true);
            hb.rectTransform.position = worldPos;
            hb.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            var btn = hb.gameObject.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnJellyBlockCollected(hb.gameObject));
            }

            StartCoroutine(JellyBlockDropAndBounceRoutine(hb.rectTransform, hb.gameObject));
        }

        private IEnumerator JellyBlockDropAndBounceRoutine(RectTransform rt, GameObject obj)
        {
            if (rt == null) yield break;

            Vector2 startPos = rt.anchoredPosition;
            float targetGroundY = UnityEngine.Random.Range(-210f, -260f);
            float targetGroundX = startPos.x + UnityEngine.Random.Range(-80f, 80f);
            targetGroundX = Mathf.Clamp(targetGroundX, -380f, 380f);

            float dropDuration = 0.55f;
            float elapsed = 0f;
            float spinSpeed = UnityEngine.Random.Range(-180f, 180f);

            // 1. Initial fall arc
            while (elapsed < dropDuration && rt != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dropDuration;
                float curX = Mathf.Lerp(startPos.x, targetGroundX, t);
                // EaseIn quad for realistic gravity drop
                float curY = Mathf.Lerp(startPos.y, targetGroundY, t * t);

                rt.anchoredPosition = new Vector2(curX, curY);
                rt.localRotation = Quaternion.Euler(0, 0, t * spinSpeed);
                yield return null;
            }

            // 2. Bounce 1
            if (rt != null)
            {
                float bounceHeight = 35f;
                float bounceDuration = 0.25f;
                elapsed = 0f;
                Vector2 groundPos = new Vector2(targetGroundX, targetGroundY);

                while (elapsed < bounceDuration && rt != null)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / bounceDuration;
                    float arcY = Mathf.Sin(t * Mathf.PI) * bounceHeight;
                    rt.anchoredPosition = groundPos + new Vector2(t * 15f, arcY);
                    yield return null;
                }
            }

            // 3. Bounce 2 (small settle)
            if (rt != null)
            {
                float bounceHeight = 12f;
                float bounceDuration = 0.18f;
                elapsed = 0f;
                Vector2 groundPos = rt.anchoredPosition;

                while (elapsed < bounceDuration && rt != null)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / bounceDuration;
                    float arcY = Mathf.Sin(t * Mathf.PI) * bounceHeight;
                    rt.anchoredPosition = groundPos + new Vector2(t * 6f, arcY);
                    yield return null;
                }
            }

            // 4. Settled pulse on ground until tapped
            if (rt != null)
            {
                rt.localRotation = Quaternion.identity;
                while (isMeadowOpen && rt != null && rt.gameObject.activeInHierarchy)
                {
                    float pulse = 1f + (Mathf.Sin(Time.time * 5f) * 0.08f);
                    rt.localScale = new Vector3(pulse, pulse, 1f);
                    yield return null;
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

            if (remainingHangingBlocks == 0 && remainingActiveGroundBlocks == 0)
            {
                PlaySound(completeSound);
                HUDController.Instance?.SetStatusHint(CLEARED_HINT);
                HUDController.Instance?.ShowNotification($"All Jelly Blocks harvested from Honey Meadows! Total: <color=#2ECC71>{sessionCaughtCount} Jelly Blocks</color>.", 4f);
            }
            else if (remainingActiveGroundBlocks == 0)
            {
                HUDController.Instance?.SetStatusHint(IDLE_HINT);
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

        private void ClearAllHangingAndGroundBlocks()
        {
            hangingBlocks.Clear();

            if (jellyTreeContainer != null)
            {
                for (int i = jellyTreeContainer.childCount - 1; i >= 0; i--)
                {
                    Transform child = jellyTreeContainer.GetChild(i);
                    if (child.name.StartsWith("HangingJelly_"))
                    {
                        Destroy(child.gameObject);
                    }
                }
            }

            if (honeyMeadowPanelRoot != null)
            {
                for (int i = honeyMeadowPanelRoot.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = honeyMeadowPanelRoot.transform.GetChild(i);
                    if (child.name.StartsWith("HangingJelly_") || child.name.StartsWith("CollectPopText"))
                    {
                        Destroy(child.gameObject);
                    }
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
