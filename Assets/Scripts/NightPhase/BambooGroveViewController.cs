using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    public class BambooGroveViewController : MonoBehaviour
    {
        private static BambooGroveViewController instance;
        public static BambooGroveViewController Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<BambooGroveViewController>(FindObjectsInactive.Include);
                }
                return instance;
            }
            private set => instance = value;
        }

        [Header("Root & Screen Panels")]
        [SerializeField] private GameObject bambooGrovePanelRoot;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite bambooGroveBackgroundSprite;
        [SerializeField] private Button returnToNightHubButton;

        [Header("UI Header & Basket")]
        [SerializeField] private TextMeshProUGUI harvestCounterText;

        [Header("Signpost & Lore")]
        [SerializeField] private Signpost signpost;

        [Header("Grass Spawning Tuning (Editable)")]
        [Range(1, 6)]
        [SerializeField] private int minGrassPatches = 2;
        [Range(1, 6)]
        [SerializeField] private int maxGrassPatches = 4;
        [SerializeField] private Transform grassPatchesContainer;
        [SerializeField] private List<Button> grassPatchButtons = new List<Button>();

        [Header("Critter Spawning Tuning (Editable)")]
        [Range(1, 10)]
        [SerializeField] private int minYippeesPerPatch = 1;
        [Range(1, 10)]
        [SerializeField] private int maxYippeesPerPatch = 3;
        [SerializeField] private Transform crittersContainer;
        [SerializeField] private Sprite[] babyYippeeRunSprites;

        [Header("Critter Scurry Duration & Timer HUD (Editable)")]
        [Tooltip("Seconds the Baby Yippees will scurry on screen before escaping.")]
        [Range(3f, 30f)]
        [SerializeField] private float scurryDurationSeconds = 8f;
        [SerializeField] private GameObject timerBarRoot;
        [SerializeField] private Image timerBarFill;
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Audio SFX")]
        [SerializeField] private AudioClip grassRustleSound;
        [SerializeField] private AudioClip catchCritterSound;
        [SerializeField] private AudioClip completeSound;

        public event Action OnBambooGroveClosed;

        private int sessionCaughtCount = 0;
        private int remainingActiveCritters = 0;
        private int remainingGrassPatches = 0;
        private bool isGroveOpen = false;
        private List<Coroutine> activeWobbleRoutines = new List<Coroutine>();
        private Coroutine timerCoroutine;

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
            EnsureGrovePanelHierarchy();

            if (returnToNightHubButton != null)
            {
                returnToNightHubButton.onClick.RemoveListener(CloseBambooGroveView);
                returnToNightHubButton.onClick.AddListener(CloseBambooGroveView);
                returnToNightHubButton.gameObject.SetActive(false);
            }

            if (bambooGrovePanelRoot != null)
            {
                bambooGrovePanelRoot.SetActive(false);
            }
            gameObject.SetActive(false);
        }

        private void Start()
        {
            if (!isGroveOpen)
            {
                if (bambooGrovePanelRoot != null)
                {
                    bambooGrovePanelRoot.SetActive(false);
                }
                gameObject.SetActive(false);
            }
        }

        private void ResolveComponentReferences()
        {
            if (bambooGrovePanelRoot == null) bambooGrovePanelRoot = gameObject;
            if (backgroundImage == null && bambooGrovePanelRoot != null)
            {
                backgroundImage = bambooGrovePanelRoot.GetComponent<Image>();
                if (backgroundImage == null)
                {
                    var bgChild = bambooGrovePanelRoot.transform.Find("BambooGroveBG") ?? bambooGrovePanelRoot.transform.Find("Background");
                    if (bgChild != null) backgroundImage = bgChild.GetComponent<Image>();
                }
            }
            if (grassPatchesContainer == null && bambooGrovePanelRoot != null)
            {
                var t = bambooGrovePanelRoot.transform.Find("GrassPatchesContainer") ?? bambooGrovePanelRoot.transform.Find("GrassPatches");
                if (t != null) grassPatchesContainer = t;
            }
            if (crittersContainer == null && bambooGrovePanelRoot != null)
            {
                var t = bambooGrovePanelRoot.transform.Find("CrittersContainer") ?? bambooGrovePanelRoot.transform.Find("Critters");
                if (t != null) crittersContainer = t;
            }
            if (harvestCounterText == null && bambooGrovePanelRoot != null)
            {
                var countChild = bambooGrovePanelRoot.transform.Find("HarvestCounter") ?? bambooGrovePanelRoot.transform.Find("HarvestCounterText");
                if (countChild != null) harvestCounterText = countChild.GetComponent<TextMeshProUGUI>();
                else harvestCounterText = bambooGrovePanelRoot.GetComponentInChildren<TextMeshProUGUI>();
            }
            if (signpost == null && bambooGrovePanelRoot != null)
            {
                signpost = bambooGrovePanelRoot.GetComponentInChildren<Signpost>(true);
            }
            if (returnToNightHubButton == null && bambooGrovePanelRoot != null)
            {
                var btns = bambooGrovePanelRoot.GetComponentsInChildren<Button>(true);
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
            if (bambooGroveBackgroundSprite == null)
            {
                bambooGroveBackgroundSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Sprites2/Sprites 3/Forage and prep/Bamboo/bamboogrove.jpg");
            }
            if (babyYippeeRunSprites == null || babyYippeeRunSprites.Length == 0)
            {
                var allAlien = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Sprites2/Sprites 3/Forage and prep/Bamboo/bbyalienrun.png");
                List<Sprite> sprites = new List<Sprite>();
                foreach (var a in allAlien)
                {
                    if (a is Sprite s) sprites.Add(s);
                }
                if (sprites.Count > 0) babyYippeeRunSprites = sprites.ToArray();
            }
#endif
            if (bambooGroveBackgroundSprite == null)
            {
                var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
                for (int i = 0; i < allSprites.Length; i++)
                {
                    var s = allSprites[i];
                    if (s == null) continue;
                    if (bambooGroveBackgroundSprite == null && (s.name.ToLower().Contains("bamboo") || s.name.ToLower().Contains("grove")))
                    {
                        bambooGroveBackgroundSprite = s;
                        break;
                    }
                }
            }
        }

        private void EnsureGrovePanelHierarchy()
        {
            ResolveComponentReferences();

            if (bambooGrovePanelRoot == null)
            {
                bambooGrovePanelRoot = gameObject;
            }

            if (grassPatchesContainer == null)
            {
                var grassCont = new GameObject("GrassPatchesContainer", typeof(RectTransform));
                grassCont.transform.SetParent(bambooGrovePanelRoot.transform, false);
                var grassRt = grassCont.GetComponent<RectTransform>();
                grassRt.anchorMin = Vector2.zero;
                grassRt.anchorMax = Vector2.one;
                grassRt.offsetMin = Vector2.zero;
                grassRt.offsetMax = Vector2.zero;
                grassPatchesContainer = grassCont.transform;
            }

            if (crittersContainer == null)
            {
                var critCont = new GameObject("CrittersContainer", typeof(RectTransform));
                critCont.transform.SetParent(bambooGrovePanelRoot.transform, false);
                var critRt = critCont.GetComponent<RectTransform>();
                critRt.anchorMin = Vector2.zero;
                critRt.anchorMax = Vector2.one;
                critRt.offsetMin = Vector2.zero;
                critRt.offsetMax = Vector2.zero;
                crittersContainer = critCont.transform;
            }
        }

        private const string IDLE_HINT = "Bamboo Grove: Tap the rustling grass to flush out wild Baby Yippees, then catch them!";
        private const string SCATTERED_HINT = "Wild Baby Yippees have scattered into the bamboo! Tap them quickly!";
        private const string CLEARED_HINT = "You have cleared all the nests, time to head back.";

        public void OpenBambooGroveView(int dayNumber)
        {
            EnsureFallbackAssets();
            EnsureGrovePanelHierarchy();

            isGroveOpen = true;
            sessionCaughtCount = 0;
            remainingActiveCritters = 0;

            gameObject.SetActive(true);
            if (bambooGrovePanelRoot != null)
            {
                bambooGrovePanelRoot.SetActive(true);
            }

            if (returnToNightHubButton != null)
            {
                returnToNightHubButton.gameObject.SetActive(true);
                returnToNightHubButton.transform.SetAsLastSibling();
            }

            if (backgroundImage != null && bambooGroveBackgroundSprite != null)
            {
                backgroundImage.sprite = bambooGroveBackgroundSprite;
            }

            HUDController.Instance?.SetSubscreenMode(true, IDLE_HINT);

            UpdateHarvestCounterDisplay();
            EnsureTimerBarUI();
            SetupGrassPatches();
        }

        public void CloseBambooGroveView()
        {
            isGroveOpen = false;
            if (signpost != null)
            {
                signpost.CloseSignInspection();
            }

            StopAllWobbles();
            ClearAllCritters();

            if (timerCoroutine != null)
            {
                StopCoroutine(timerCoroutine);
                timerCoroutine = null;
            }
            if (timerBarRoot != null)
            {
                timerBarRoot.SetActive(false);
            }

            if (sessionCaughtCount > 0)
            {
                PlaySound(completeSound);
                HUDController.Instance?.ShowNotification($"Foraging completed! Bagged <color=#2ECC71>+{sessionCaughtCount} Baby Yippees</color> in your expedition!", 4.5f);
            }

            if (returnToNightHubButton != null)
            {
                returnToNightHubButton.gameObject.SetActive(false);
            }

            if (bambooGrovePanelRoot != null)
            {
                bambooGrovePanelRoot.SetActive(false);
            }
            gameObject.SetActive(false);

            HUDController.Instance?.SetSubscreenMode(false);
            OnBambooGroveClosed?.Invoke();
        }

        private void UpdateHarvestCounterDisplay()
        {
            if (harvestCounterText != null)
            {
                harvestCounterText.text = $"Expedition Harvest: <color=#2ECC71>+{sessionCaughtCount}</color> Baby Yippees";
            }
        }

        // =========================================================================
        // GRASS PATCH SETUP & RUSTLE WOBBLE
        // =========================================================================
        private void SetupGrassPatches()
        {
            StopAllWobbles();

            // If user assigned explicit buttons in Inspector, wire them
            if (grassPatchButtons != null && grassPatchButtons.Count > 0)
            {
                remainingGrassPatches = grassPatchButtons.Count;
                for (int i = 0; i < grassPatchButtons.Count; i++)
                {
                    int index = i;
                    var btn = grassPatchButtons[i];
                    if (btn == null) continue;

                    btn.gameObject.SetActive(true);
                    btn.interactable = true;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnGrassPatchClicked(btn, index));

                    var coroutine = StartCoroutine(GrassWobbleRoutine(btn.transform as RectTransform, 1.2f + (i * 0.3f)));
                    activeWobbleRoutines.Add(coroutine);
                }
                return;
            }

            // Auto-generate random 2, 3, or 4 grass patches
            Transform container = grassPatchesContainer != null ? grassPatchesContainer : bambooGrovePanelRoot.transform;
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Transform child = container.GetChild(i);
                if (child.name.StartsWith("AutoGrass_"))
                {
                    Destroy(child.gameObject);
                }
            }

            List<Vector2> candidatePositions = new List<Vector2>
            {
                new Vector2(-330f, -160f),
                new Vector2(-120f, -220f),
                new Vector2(110f, -190f),
                new Vector2(320f, -150f),
                new Vector2(-210f, -130f),
                new Vector2(210f, -230f)
            };

            // Shuffle positions
            for (int i = 0; i < candidatePositions.Count; i++)
            {
                int rnd = UnityEngine.Random.Range(i, candidatePositions.Count);
                var temp = candidatePositions[i];
                candidatePositions[i] = candidatePositions[rnd];
                candidatePositions[rnd] = temp;
            }

            int spawnCount = Mathf.Clamp(UnityEngine.Random.Range(minGrassPatches, maxGrassPatches + 1), 1, candidatePositions.Count);
            remainingGrassPatches = spawnCount;

            for (int i = 0; i < spawnCount; i++)
            {
                int index = i;
                GameObject patchObj = new GameObject($"AutoGrass_{i + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
                patchObj.transform.SetParent(container, false);
                var rt = patchObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(170f, 140f);
                rt.anchoredPosition = candidatePositions[i];

                var img = patchObj.GetComponent<Image>();
                Sprite gSp = SpriteManager.Instance != null ? SpriteManager.Instance.GrassPileSprite : null;
                if (gSp != null)
                {
                    img.sprite = gSp;
                }
                img.preserveAspect = true;

                var btn = patchObj.GetComponent<Button>();
                btn.onClick.AddListener(() => OnGrassPatchClicked(btn, index));

                var coroutine = StartCoroutine(GrassWobbleRoutine(rt, 1.2f + (i * 0.35f)));
                activeWobbleRoutines.Add(coroutine);
            }
        }

        private IEnumerator GrassWobbleRoutine(RectTransform rt, float speed)
        {
            if (rt == null) yield break;
            Vector3 baseScale = rt.localScale;

            while (isGroveOpen && rt != null && rt.gameObject.activeInHierarchy)
            {
                float t = Time.time * speed;
                float angle = Mathf.Sin(t * 3f) * 6f;
                float scaleMod = 1f + (Mathf.Sin(t * 5f) * 0.04f);

                rt.localRotation = Quaternion.Euler(0, 0, angle);
                rt.localScale = new Vector3(baseScale.x * scaleMod, baseScale.y, baseScale.z);
                yield return null;
            }
        }

        private void StopAllWobbles()
        {
            foreach (var routine in activeWobbleRoutines)
            {
                if (routine != null) StopCoroutine(routine);
            }
            activeWobbleRoutines.Clear();
        }

        // =========================================================================
        // FLUSHING OUT CRITTERS
        // =========================================================================
        private void OnGrassPatchClicked(Button patchBtn, int patchIndex)
        {
            if (patchBtn == null) return;
            patchBtn.interactable = false;
            remainingGrassPatches = Mathf.Max(0, remainingGrassPatches - 1);

            PlaySound(grassRustleSound);
            StartCoroutine(VigorousRustleRoutine(patchBtn.transform as RectTransform, () =>
            {
                SpawnCrittersFromPatch(patchBtn.transform.position);
            }));
        }

        private IEnumerator VigorousRustleRoutine(RectTransform rt, Action onFinished)
        {
            if (rt != null)
            {
                Vector3 origScale = rt.localScale;
                float elapsed = 0f;
                float duration = 0.4f;

                while (elapsed < duration && rt != null)
                {
                    elapsed += Time.deltaTime;
                    float angle = Mathf.Sin(elapsed * 45f) * 14f;
                    rt.localRotation = Quaternion.Euler(0, 0, angle);
                    yield return null;
                }

                if (rt != null)
                {
                    rt.localRotation = Quaternion.identity;
                    rt.localScale = origScale * 0.85f; // visually compressed once flushed
                }
            }

            onFinished?.Invoke();
        }

        private void SpawnCrittersFromPatch(Vector3 spawnWorldPos)
        {
            Transform container = crittersContainer != null ? crittersContainer : bambooGrovePanelRoot.transform;
            int minSpawn = minYippeesPerPatch;
            int maxSpawn = maxYippeesPerPatch;
            if (UpgradeManager.Instance != null && UpgradeManager.Instance.HasUpgrade(UpgradeType.YippeePheromones))
            {
                minSpawn += 1;
                maxSpawn += 1;
            }
            int countToSpawn = UnityEngine.Random.Range(minSpawn, maxSpawn + 1);

            HUDController.Instance?.SetStatusHint(SCATTERED_HINT);

            EnsureTimerBarUI(container);

            for (int i = 0; i < countToSpawn; i++)
            {
                remainingActiveCritters++;
                GameObject critterObj = new GameObject($"BabyYippee_Wild_{remainingActiveCritters}", typeof(RectTransform), typeof(Image), typeof(Button));
                critterObj.transform.SetParent(container, false);
                critterObj.transform.position = spawnWorldPos;

                var rt = critterObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(72f, 72f);
                rt.pivot = new Vector2(0.5f, 0.5f);

                var img = critterObj.GetComponent<Image>();
                Sprite critterSprite = SpriteManager.Instance != null && SpriteManager.Instance.BabyYippeeSprite != null
                    ? SpriteManager.Instance.BabyYippeeSprite
                    : ((babyYippeeRunSprites != null && babyYippeeRunSprites.Length > 0) ? babyYippeeRunSprites[0] : null);

                img.sprite = critterSprite;
                img.preserveAspect = true;

                var btn = critterObj.GetComponent<Button>();
                var capturedObj = critterObj;
                btn.onClick.AddListener(() => OnCritterCaught(capturedObj));

                StartCoroutine(CritterScurryRoutine(rt, img));
            }

            if (timerCoroutine != null) StopCoroutine(timerCoroutine);
            timerCoroutine = StartCoroutine(ScurryTimerBarRoutine(scurryDurationSeconds));
        }

        private void EnsureTimerBarUI(Transform containerParent = null)
        {
            Transform targetParent = (containerParent != null)
                ? containerParent
                : ((crittersContainer != null) ? crittersContainer : (bambooGrovePanelRoot != null ? bambooGrovePanelRoot.transform : transform));

            if (targetParent == null) return;

            if (timerBarRoot != null)
            {
                if (timerBarRoot.transform.parent != targetParent)
                {
                    timerBarRoot.transform.SetParent(targetParent, false);
                }
                var existingRt = timerBarRoot.GetComponent<RectTransform>();
                if (existingRt != null) existingRt.anchoredPosition = new Vector2(0f, 370f);
                return;
            }

            // Spawn timer bar directly under the same container as Baby Yippees
            timerBarRoot = new GameObject("ScurryTimerBar", typeof(RectTransform), typeof(Image));
            timerBarRoot.transform.SetParent(targetParent, false);

            var rootRt = timerBarRoot.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.anchoredPosition = new Vector2(0f, 370f); // Shifted up by 150px to sit cleanly at top
            rootRt.sizeDelta = new Vector2(360f, 28f);

            var bgImg = timerBarRoot.GetComponent<Image>();
            bgImg.color = new Color(0.10f, 0.08f, 0.06f, 0.95f);
            bgImg.raycastTarget = false;

            // Golden Border
            GameObject borderObj = new GameObject("Border", typeof(RectTransform), typeof(Image));
            borderObj.transform.SetParent(timerBarRoot.transform, false);
            var borderRt = borderObj.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(-2, -2);
            borderRt.offsetMax = new Vector2(2, 2);
            var borderImg = borderObj.GetComponent<Image>();
            borderImg.color = new Color(0.95f, 0.82f, 0.35f, 1f);
            borderImg.raycastTarget = false;
            borderObj.transform.SetAsFirstSibling();

            // Fill Bar
            GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(timerBarRoot.transform, false);
            var fillRt = fillObj.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(3, 3);
            fillRt.offsetMax = new Vector2(-3, -3);

            timerBarFill = fillObj.GetComponent<Image>();
            // Assign solid white sprite so Image.Type.Filled shrinks properly from right to left in Unity UI
            Texture2D whiteTex = Texture2D.whiteTexture;
            Sprite whiteSprite = Sprite.Create(whiteTex, new Rect(0, 0, whiteTex.width, whiteTex.height), new Vector2(0.5f, 0.5f));
            timerBarFill.sprite = whiteSprite;
            timerBarFill.type = Image.Type.Filled;
            timerBarFill.fillMethod = Image.FillMethod.Horizontal;
            timerBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            timerBarFill.color = new Color(0.18f, 0.85f, 0.45f, 1f);
            timerBarFill.raycastTarget = false;

            // Label Text
            GameObject labelObj = new GameObject("TimerLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObj.transform.SetParent(timerBarRoot.transform, false);
            var labelRt = labelObj.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            timerText = labelObj.GetComponent<TextMeshProUGUI>();
            timerText.text = $"Escape in: {scurryDurationSeconds:F1}s";
            timerText.fontSize = 15;
            timerText.fontStyle = FontStyles.Bold;
            timerText.alignment = TextAlignmentOptions.Center;
            timerText.color = Color.white;
            timerText.raycastTarget = false;

            timerBarRoot.SetActive(false);
        }

        private IEnumerator ScurryTimerBarRoutine(float duration)
        {
            EnsureTimerBarUI();
            if (timerBarRoot != null)
            {
                timerBarRoot.SetActive(true);
                timerBarRoot.transform.SetAsLastSibling();
            }

            float elapsed = 0f;
            while (elapsed < duration && remainingActiveCritters > 0)
            {
                elapsed += Time.deltaTime;
                float pct = Mathf.Clamp01(1f - (elapsed / duration));
                float remaining = Mathf.Max(0f, duration - elapsed);

                if (timerBarFill != null)
                {
                    timerBarFill.fillAmount = pct;
                    timerBarFill.color = Color.Lerp(new Color(0.95f, 0.25f, 0.25f), new Color(0.18f, 0.80f, 0.44f), pct);
                }

                if (timerText != null)
                {
                    timerText.text = $"Escape in: {remaining:F1}s";
                }

                yield return null;
            }

            if (timerBarRoot != null)
            {
                timerBarRoot.SetActive(false);
            }
            timerCoroutine = null;
        }

        private IEnumerator CritterScurryRoutine(RectTransform rt, Image img)
        {
            if (rt == null) yield break;

            Vector2 moveDir = new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-0.8f, 0.8f)).normalized;
            float speed = UnityEngine.Random.Range(150f, 240f);
            float lifetime = scurryDurationSeconds;
            float elapsed = 0f;
            float animFps = 10f; // 10 frames per second run animation

            while (elapsed < lifetime && rt != null && rt.gameObject.activeInHierarchy)
            {
                elapsed += Time.deltaTime;

                // Move
                rt.anchoredPosition += moveDir * speed * Time.deltaTime;

                // Frame-by-frame sprite flipbook animation for running
                if (img != null && babyYippeeRunSprites != null && babyYippeeRunSprites.Length > 0)
                {
                    int frameIndex = (int)(elapsed * animFps) % babyYippeeRunSprites.Length;
                    if (babyYippeeRunSprites[frameIndex] != null)
                    {
                        img.sprite = babyYippeeRunSprites[frameIndex];
                    }
                }

                // Running footstep tilt & vertical hop
                float hopY = Mathf.Abs(Mathf.Sin(elapsed * 16f)) * 6f;
                float tiltAngle = Mathf.Sin(elapsed * 18f) * 7f;
                rt.localRotation = Quaternion.Euler(0, 0, tiltAngle);

                // Flip sprite depending on horizontal movement (facing forward in direction of motion)
                float dirScaleX = (moveDir.x > 0f) ? 1f : -1f;
                rt.localScale = new Vector3(dirScaleX, 1f, 1f);

                // Screen boundaries bounce check
                if (Mathf.Abs(rt.anchoredPosition.x) > 420f)
                {
                    moveDir.x = -moveDir.x;
                }
                if (Mathf.Abs(rt.anchoredPosition.y) > 240f)
                {
                    moveDir.y = -moveDir.y;
                }

                yield return null;
            }

            // If time ran out and player did not catch, critter burrows away
            if (rt != null)
            {
                remainingActiveCritters = Mathf.Max(0, remainingActiveCritters - 1);
                Destroy(rt.gameObject);
                HUDController.Instance?.ShowNotification("The baby yippees have escaped successfully!", 3f);

                if (remainingActiveCritters == 0)
                {
                    if (remainingGrassPatches == 0)
                    {
                        HUDController.Instance?.SetStatusHint(CLEARED_HINT);
                    }
                    else
                    {
                        HUDController.Instance?.SetStatusHint(IDLE_HINT);
                    }
                }
            }
        }

        // =========================================================================
        // CATCH CRITTER INTERACTION
        // =========================================================================
        private void OnCritterCaught(GameObject critterObj)
        {
            if (critterObj == null) return;

            PlaySound(catchCritterSound);

            sessionCaughtCount++;
            remainingActiveCritters = Mathf.Max(0, remainingActiveCritters - 1);
            InventoryManager.Instance?.AddRawStock(RawIngredientType.BabyYippees, 1);

            UpdateHarvestCounterDisplay();
            SpawnCatchPopText(critterObj.transform.position);

            Destroy(critterObj);

            if (remainingActiveCritters == 0)
            {
                if (timerCoroutine != null)
                {
                    StopCoroutine(timerCoroutine);
                    timerCoroutine = null;
                }
                if (timerBarRoot != null)
                {
                    timerBarRoot.SetActive(false);
                }
                HUDController.Instance?.ShowNotification($"All critters caught from this patch! Total: <color=#2ECC71>{sessionCaughtCount} Baby Yippees</color>.", 3f);

                if (remainingGrassPatches == 0)
                {
                    HUDController.Instance?.SetStatusHint(CLEARED_HINT);
                }
                else
                {
                    HUDController.Instance?.SetStatusHint(IDLE_HINT);
                }
            }
        }

        private void SpawnCatchPopText(Vector3 worldPos)
        {
            GameObject popObj = new GameObject("CatchPopText", typeof(RectTransform), typeof(TextMeshProUGUI));
            popObj.transform.SetParent(bambooGrovePanelRoot.transform, false);
            popObj.transform.position = worldPos;

            var tmp = popObj.GetComponent<TextMeshProUGUI>();
            tmp.text = "<color=#2ECC71><b>+1 Baby Yippee!</b></color>";
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
            float duration = 0.8f;
            Vector3 startPos = rt.anchoredPosition;

            while (elapsed < duration && popObj != null)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                rt.anchoredPosition = startPos + new Vector3(0, progress * 40f, 0);
                if (tmp != null) tmp.alpha = 1f - progress;
                yield return null;
            }

            if (popObj != null) Destroy(popObj);
        }

        private void ClearAllCritters()
        {
            Transform container = crittersContainer != null ? crittersContainer : bambooGrovePanelRoot.transform;
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Transform child = container.GetChild(i);
                if (child.name.StartsWith("BabyYippee_Wild_") || child.name.StartsWith("CatchPopText"))
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
