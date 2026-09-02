using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    public class MistMountainViewController : MonoBehaviour, IPointerClickHandler, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        private static MistMountainViewController instance;
        public static MistMountainViewController Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<MistMountainViewController>(FindObjectsInactive.Include);
                }
                return instance;
            }
            private set => instance = value;
        }

        public enum MistMountainStage
        {
            PanoramaApproach, // Wide mountain view with pulsing rock shelf
            RockWallCatching  // Full-screen close-up rock wall with bucket minigame
        }

        [Header("Root & Screen Panels")]
        [SerializeField] private GameObject mistMountainPanelRoot;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite mountainPanoramaSprite;
        [SerializeField] private Sprite bucketSprite;
        [SerializeField] private Button returnToNightHubButton;

        [Header("UI Header & Harvest Counter")]
        [SerializeField] private TextMeshProUGUI harvestCounterText;

        [Header("Stage 1: Panorama Rock Shelf")]
        [SerializeField] private GameObject rockShelfObject;
        [SerializeField] private Button rockShelfButton;
        [SerializeField] private RectTransform rockShelfRectTransform;

        [Header("Stage 2: Close-up Rock Wall & Minigame")]
        [SerializeField] private GameObject rockWallObject;
        [SerializeField] private Image rockWallImage;
        [SerializeField] private RectTransform bucketRectTransform;
        [SerializeField] private Image bucketImage;

        [Header("Dew Drops Spawning Tuning (Editable)")]
        [Range(3, 12)]
        [SerializeField] private int minDewDrops = 4;
        [Range(3, 12)]
        [SerializeField] private int maxDewDrops = 7;

        [Header("Audio SFX")]
        [SerializeField] private AudioClip rockKickSound;
        [SerializeField] private AudioClip dewCatchSound;
        [SerializeField] private AudioClip completeSound;

        public event Action OnMistMountainClosed;

        private MistMountainStage currentStage = MistMountainStage.PanoramaApproach;
        private int sessionCaughtCount = 0;
        private int totalSpawnedCount = 0;
        private int remainingActiveDewDrops = 0;
        private bool isMountainOpen = false;
        private bool hasKickedWall = false;
        private bool isDraggingBucket = false;
        private Coroutine pulsingShelfCoroutine;
        private List<Coroutine> activeDewDropRoutines = new List<Coroutine>();
        private List<GameObject> activeDewDropObjects = new List<GameObject>();
        private Vector2 rootPanelBaseAnchoredPos = Vector2.zero;

        private const string PANORAMA_HINT = "Tap the rock shelf to approach the cliff face!";
        private const string WALL_IDLE_HINT = "Kick the rock wall hard to dislodge the glistening Golden Dew!";
        private const string WALL_CATCHING_HINT = "Quick! Drag your bucket to catch the falling Golden Dew!";
        private const string CLEARED_HINT = "The remaining dew has settled into the mountain stone, its time to return";

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

            if (mistMountainPanelRoot == null)
            {
                EnsureMountainPanelHierarchy();
            }

            WireButtonsAndListeners();

            if (mistMountainPanelRoot != null)
            {
                var rt = mistMountainPanelRoot.GetComponent<RectTransform>();
                if (rt != null) rootPanelBaseAnchoredPos = rt.anchoredPosition;
                mistMountainPanelRoot.SetActive(false);
            }
            gameObject.SetActive(false);
        }

        private void Start()
        {
            WireButtonsAndListeners();
            if (!isMountainOpen)
            {
                if (mistMountainPanelRoot != null)
                {
                    mistMountainPanelRoot.SetActive(false);
                }
                gameObject.SetActive(false);
            }
        }

        private void ResolveComponentReferences()
        {
            if (mistMountainPanelRoot == null) mistMountainPanelRoot = gameObject;
            if (backgroundImage == null && mistMountainPanelRoot != null)
            {
                backgroundImage = mistMountainPanelRoot.GetComponent<Image>();
            }

            if (harvestCounterText == null && mistMountainPanelRoot != null)
            {
                harvestCounterText = mistMountainPanelRoot.GetComponentInChildren<TextMeshProUGUI>();
            }

            if (returnToNightHubButton == null && mistMountainPanelRoot != null)
            {
                var btns = mistMountainPanelRoot.GetComponentsInChildren<Button>(true);
                foreach (var b in btns)
                {
                    if (b.gameObject.name.ToLower().Contains("return") || b.gameObject.name.ToLower().Contains("hub"))
                    {
                        returnToNightHubButton = b;
                        break;
                    }
                }
            }

            if (rockShelfObject == null && mistMountainPanelRoot != null)
            {
                Transform t = mistMountainPanelRoot.transform.Find("RockShelf");
                if (t == null) t = mistMountainPanelRoot.transform.Find("PulsingRockShelf");
                if (t != null)
                {
                    rockShelfObject = t.gameObject;
                    rockShelfButton = t.GetComponent<Button>();
                    rockShelfRectTransform = t.GetComponent<RectTransform>();
                }
            }
            else if (rockShelfObject != null)
            {
                if (rockShelfButton == null) rockShelfButton = rockShelfObject.GetComponent<Button>();
                if (rockShelfRectTransform == null) rockShelfRectTransform = rockShelfObject.GetComponent<RectTransform>();
            }

            if (rockWallObject == null && mistMountainPanelRoot != null)
            {
                Transform t = mistMountainPanelRoot.transform.Find("RockWall");
                if (t == null) t = mistMountainPanelRoot.transform.Find("RockWallContainer");
                if (t != null)
                {
                    rockWallObject = t.gameObject;
                    rockWallImage = t.GetComponent<Image>();
                }
            }
            else if (rockWallObject != null)
            {
                if (rockWallImage == null) rockWallImage = rockWallObject.GetComponent<Image>();
            }

            if (bucketRectTransform == null && mistMountainPanelRoot != null)
            {
                Transform b = mistMountainPanelRoot.transform.Find("CatchBucket");
                if (b == null && rockWallObject != null) b = rockWallObject.transform.Find("CatchBucket");
                if (b != null)
                {
                    bucketRectTransform = b.GetComponent<RectTransform>();
                    bucketImage = b.GetComponent<Image>();
                }
            }
            else if (bucketRectTransform != null && bucketImage == null)
            {
                bucketImage = bucketRectTransform.GetComponent<Image>();
            }
        }

        private void EnsureFallbackAssets()
        {
#if UNITY_EDITOR
            if (mountainPanoramaSprite == null)
            {
                mountainPanoramaSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Sprites2/Sprites 3/Forage and prep/mountains/mistmountain.jpg");
            }
            if (bucketSprite == null)
            {
                var allEqm = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Sprites2/Sprites 3/Forage and prep/Preparea/prepEquipment.png");
                foreach (var a in allEqm)
                {
                    if (a is Sprite s && (s.name == "prepEquipment_4" || s.name.Contains("4")))
                    {
                        bucketSprite = s;
                        break;
                    }
                }
            }
#endif
            if (mountainPanoramaSprite == null || bucketSprite == null)
            {
                var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
                for (int i = 0; i < allSprites.Length; i++)
                {
                    var s = allSprites[i];
                    if (s == null) continue;
                    if (mountainPanoramaSprite == null && s.name.ToLower().Contains("mistmountain")) mountainPanoramaSprite = s;
                    if (bucketSprite == null && (s.name == "prepEquipment_4" || s.name.ToLower().Contains("bucket"))) bucketSprite = s;
                }
            }
        }

        private void EnsureMountainPanelHierarchy()
        {
            Transform parentCanvas = null;
            if (HoneyMeadowViewController.Instance != null && HoneyMeadowViewController.Instance.transform.parent != null)
            {
                parentCanvas = HoneyMeadowViewController.Instance.transform.parent;
            }
            else if (BambooGroveViewController.Instance != null && BambooGroveViewController.Instance.transform.parent != null)
            {
                parentCanvas = BambooGroveViewController.Instance.transform.parent;
            }
            else if (NightPhaseManager.Instance != null && NightPhaseManager.Instance.transform.parent != null)
            {
                parentCanvas = NightPhaseManager.Instance.transform.parent;
            }
            else
            {
                var canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null) parentCanvas = canvas.transform;
            }

            if (parentCanvas == null) parentCanvas = transform;

            // 1. Root Panel
            GameObject rootObj = new GameObject("MistMountainViewPanel", typeof(RectTransform), typeof(Image));
            rootObj.transform.SetParent(parentCanvas, false);
            var rootRt = rootObj.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            backgroundImage = rootObj.GetComponent<Image>();
            if (mountainPanoramaSprite != null) backgroundImage.sprite = mountainPanoramaSprite;
            backgroundImage.color = Color.white;
            backgroundImage.raycastTarget = true;

            mistMountainPanelRoot = rootObj;
            rootPanelBaseAnchoredPos = rootRt.anchoredPosition;

            // 2. Harvest Counter Header
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

            // 3. Return to Night Hub Button
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

            // 4. Stage 1: Pulsing Rock Shelf
            GameObject shelfObj = new GameObject("RockShelf", typeof(RectTransform), typeof(Image), typeof(Button));
            shelfObj.transform.SetParent(rootObj.transform, false);
            rockShelfRectTransform = shelfObj.GetComponent<RectTransform>();
            rockShelfRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rockShelfRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rockShelfRectTransform.pivot = new Vector2(0.5f, 0.5f);
            rockShelfRectTransform.anchoredPosition = new Vector2(0f, -40f);
            rockShelfRectTransform.sizeDelta = new Vector2(270f, 140f);

            var shelfImg = shelfObj.GetComponent<Image>();
            Sprite shelfSp = SpriteManager.Instance != null ? SpriteManager.Instance.RockShelfSprite : null;
            if (shelfSp != null) shelfImg.sprite = shelfSp;
            shelfImg.preserveAspect = true;
            shelfImg.raycastTarget = true;

            rockShelfButton = shelfObj.GetComponent<Button>();
            rockShelfButton.transition = Selectable.Transition.None;
            rockShelfButton.onClick.AddListener(OnRockShelfClicked);
            rockShelfObject = shelfObj;

            // 5. Stage 2: Rock Wall Container
            GameObject wallObj = new GameObject("RockWall", typeof(RectTransform), typeof(Image), typeof(Button));
            wallObj.transform.SetParent(rootObj.transform, false);
            var wallRt = wallObj.GetComponent<RectTransform>();
            wallRt.anchorMin = Vector2.zero;
            wallRt.anchorMax = Vector2.one;
            wallRt.offsetMin = Vector2.zero;
            wallRt.offsetMax = Vector2.zero;

            rockWallImage = wallObj.GetComponent<Image>();
            Sprite wallSp = SpriteManager.Instance != null ? SpriteManager.Instance.RockWallSprite : null;
            if (wallSp != null) rockWallImage.sprite = wallSp;
            rockWallImage.color = Color.white;
            rockWallImage.raycastTarget = true;
            rockWallObject = wallObj;

            var wallBtn = wallObj.GetComponent<Button>();
            wallBtn.transition = Selectable.Transition.None;
            wallBtn.onClick.AddListener(OnRockWallClicked);

            // 6. Catch Bucket inside RockWall
            GameObject bucketObj = new GameObject("CatchBucket", typeof(RectTransform), typeof(Image));
            bucketObj.transform.SetParent(wallObj.transform, false);
            bucketRectTransform = bucketObj.GetComponent<RectTransform>();
            bucketRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            bucketRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            bucketRectTransform.pivot = new Vector2(0.5f, 0.5f);
            bucketRectTransform.anchoredPosition = new Vector2(0f, -220f);
            bucketRectTransform.sizeDelta = new Vector2(130f, 130f);

            bucketImage = bucketObj.GetComponent<Image>();
            if (bucketSprite != null) bucketImage.sprite = bucketSprite;
            bucketImage.preserveAspect = true;
            bucketImage.raycastTarget = true;
        }

        private void WireButtonsAndListeners()
        {
            ResolveComponentReferences();

            if (returnToNightHubButton != null)
            {
                returnToNightHubButton.onClick.RemoveAllListeners();
                returnToNightHubButton.onClick.AddListener(CloseMistMountainView);
            }

            if (rockShelfObject != null)
            {
                var btn = rockShelfObject.GetComponent<Button>();
                if (btn == null) btn = rockShelfObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.interactable = true;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnRockShelfClicked);
                rockShelfButton = btn;

                var img = rockShelfObject.GetComponent<Image>();
                if (img != null) img.raycastTarget = true;
            }

            if (rockWallObject != null)
            {
                var btn = rockWallObject.GetComponent<Button>();
                if (btn == null) btn = rockWallObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.interactable = true;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnRockWallClicked);

                var img = rockWallObject.GetComponent<Image>();
                if (img != null) img.raycastTarget = true;
            }
        }

        public void OpenMistMountainView(int dayNumber)
        {
            isMountainOpen = true;
            sessionCaughtCount = 0;
            totalSpawnedCount = 0;
            remainingActiveDewDrops = 0;
            hasKickedWall = false;
            isDraggingBucket = false;

            ResolveComponentReferences();
            EnsureFallbackAssets();

            if (mistMountainPanelRoot == null)
            {
                EnsureMountainPanelHierarchy();
            }

            WireButtonsAndListeners();

            gameObject.SetActive(true);
            if (mistMountainPanelRoot != null)
            {
                mistMountainPanelRoot.SetActive(true);
                var rt = mistMountainPanelRoot.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = rootPanelBaseAnchoredPos;
            }

            if (returnToNightHubButton != null)
            {
                returnToNightHubButton.gameObject.SetActive(true);
                returnToNightHubButton.transform.SetAsLastSibling();
            }

            UpdateHarvestCounterDisplay();
            SetupPanoramaApproachStage();
        }

        public void CloseMistMountainView()
        {
            isMountainOpen = false;

            StopPulsingShelf();
            StopAllDewRoutines();
            ClearAllDewDrops();

            if (mistMountainPanelRoot != null)
            {
                var rt = mistMountainPanelRoot.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = rootPanelBaseAnchoredPos;
                mistMountainPanelRoot.SetActive(false);
            }
            gameObject.SetActive(false);

            if (sessionCaughtCount > 0)
            {
                PlaySound(completeSound);
                HUDController.Instance?.ShowNotification($"Mountain expedition complete! Bagged <color=#2ECC71>+{sessionCaughtCount} Raw Golden Dew</color>!", 4.5f);
            }

            if (returnToNightHubButton != null)
            {
                returnToNightHubButton.gameObject.SetActive(false);
            }

            HUDController.Instance?.SetSubscreenMode(false);
            OnMistMountainClosed?.Invoke();
        }

        private void UpdateHarvestCounterDisplay()
        {
            if (harvestCounterText != null)
            {
                harvestCounterText.text = $"Expedition Harvest: <color=#2ECC71>+{sessionCaughtCount}</color> Raw Golden Dew";
            }
        }

        // =========================================================================
        // STAGE 1: PANORAMA APPROACH & PULSING ROCK SHELF
        // =========================================================================
        private void SetupPanoramaApproachStage()
        {
            currentStage = MistMountainStage.PanoramaApproach;
            StopPulsingShelf();

            if (backgroundImage != null && mountainPanoramaSprite != null)
            {
                backgroundImage.sprite = mountainPanoramaSprite;
            }

            if (rockWallObject != null) rockWallObject.SetActive(false);

            if (rockShelfObject != null)
            {
                rockShelfObject.SetActive(true);
                var rt = rockShelfObject.GetComponent<RectTransform>();
                if (rt != null)
                {
                    pulsingShelfCoroutine = StartCoroutine(PulsingShelfRoutine(rt));
                }
            }

            HUDController.Instance?.SetSubscreenMode(true, PANORAMA_HINT);
        }

        private IEnumerator PulsingShelfRoutine(RectTransform rt)
        {
            if (rt == null) yield break;
            Vector2 basePos = rt.anchoredPosition;

            while (isMountainOpen && currentStage == MistMountainStage.PanoramaApproach && rt != null)
            {
                float scalePulse = 1f + (Mathf.Sin(Time.time * 3.8f) * 0.08f);
                float bobbing = Mathf.Sin(Time.time * 2.2f) * 5f;

                rt.localScale = new Vector3(scalePulse, scalePulse, 1f);
                rt.anchoredPosition = basePos + new Vector2(0f, bobbing);
                yield return null;
            }

            if (rt != null) rt.localScale = Vector3.one;
        }

        private void StopPulsingShelf()
        {
            if (pulsingShelfCoroutine != null)
            {
                StopCoroutine(pulsingShelfCoroutine);
                pulsingShelfCoroutine = null;
            }
        }

        public void OnRockShelfClicked()
        {
            if (currentStage != MistMountainStage.PanoramaApproach) return;
            StopPulsingShelf();
            TransitionToRockWallStage();
        }

        // =========================================================================
        // STAGE 2: CLOSE-UP ROCK WALL & BUCKET CATCHING
        // =========================================================================
        private void TransitionToRockWallStage()
        {
            currentStage = MistMountainStage.RockWallCatching;
            hasKickedWall = false;

            if (rockShelfObject != null) rockShelfObject.SetActive(false);

            if (rockWallObject != null)
            {
                rockWallObject.SetActive(true);
                if (rockWallImage != null)
                {
                    Sprite wallSp = SpriteManager.Instance != null ? SpriteManager.Instance.RockWallSprite : null;
                    if (wallSp != null) rockWallImage.sprite = wallSp;
                }
            }

            if (bucketRectTransform != null)
            {
                bucketRectTransform.gameObject.SetActive(true);
                bucketRectTransform.anchoredPosition = new Vector2(0f, -220f);
                if (bucketImage != null && bucketSprite != null) bucketImage.sprite = bucketSprite;
            }

            ClearAllDewDrops();
            HUDController.Instance?.SetStatusHint(WALL_IDLE_HINT);
        }

        public void OnRockWallClicked()
        {
            if (currentStage != MistMountainStage.RockWallCatching || hasKickedWall || !isMountainOpen) return;

            hasKickedWall = true;
            PlaySound(rockKickSound);

            StartCoroutine(ViolentRockWallShakeRoutine());
            HUDController.Instance?.SetStatusHint(WALL_CATCHING_HINT);
            SpawnAndDropGoldenDew();
        }

        private IEnumerator ViolentRockWallShakeRoutine()
        {
            RectTransform panelRt = (mistMountainPanelRoot != null) ? mistMountainPanelRoot.GetComponent<RectTransform>() : null;
            RectTransform wallRt = (rockWallObject != null) ? rockWallObject.GetComponent<RectTransform>() : null;

            float elapsed = 0f;
            float duration = 0.55f;

            while (elapsed < duration && isMountainOpen)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float intensity = 1f - progress;

                if (panelRt != null)
                {
                    float shakeX = UnityEngine.Random.Range(-24f, 24f) * intensity;
                    float shakeY = UnityEngine.Random.Range(-20f, 20f) * intensity;
                    panelRt.anchoredPosition = rootPanelBaseAnchoredPos + new Vector2(shakeX, shakeY);
                }

                if (wallRt != null)
                {
                    float angle = Mathf.Sin(elapsed * 50f) * (8f * intensity);
                    wallRt.localRotation = Quaternion.Euler(0, 0, angle);
                }

                yield return null;
            }

            if (panelRt != null) panelRt.anchoredPosition = rootPanelBaseAnchoredPos;
            if (wallRt != null) wallRt.localRotation = Quaternion.identity;
        }

        // =========================================================================
        // DEW DROPS SPAWNING & BUCKET COLLISION
        // =========================================================================
        private void SpawnAndDropGoldenDew()
        {
            StopAllDewRoutines();
            ClearAllDewDrops();

            Transform container = (rockWallObject != null) ? rockWallObject.transform : mistMountainPanelRoot.transform;

            int spawnCount = Mathf.Clamp(UnityEngine.Random.Range(minDewDrops, maxDewDrops + 1), 3, 10);
            totalSpawnedCount = spawnCount;
            remainingActiveDewDrops = spawnCount;

            for (int i = 0; i < spawnCount; i++)
            {
                float spawnX = UnityEngine.Random.Range(-320f, 320f);
                float spawnY = UnityEngine.Random.Range(240f, 280f);
                float fallSpeed = UnityEngine.Random.Range(260f, 360f);
                float startDelay = i * UnityEngine.Random.Range(0.25f, 0.6f);

                GameObject dropObj = new GameObject($"GoldenDewDrop_{i + 1}", typeof(RectTransform), typeof(Image));
                dropObj.transform.SetParent(container, false);
                var rt = dropObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(58f, 58f);
                rt.anchoredPosition = new Vector2(spawnX, spawnY);

                var img = dropObj.GetComponent<Image>();
                Sprite dewSp = SpriteManager.Instance != null ? SpriteManager.Instance.RawGoldenDewSprite : null;
                if (dewSp != null) img.sprite = dewSp;
                img.preserveAspect = true;
                img.raycastTarget = false;

                activeDewDropObjects.Add(dropObj);

                var coroutine = StartCoroutine(DewDropFallAndCatchRoutine(rt, dropObj, spawnX, spawnY, fallSpeed, startDelay));
                activeDewDropRoutines.Add(coroutine);
            }
        }

        private IEnumerator DewDropFallAndCatchRoutine(RectTransform dropRt, GameObject dropObj, float startX, float startY, float speed, float delay)
        {
            if (dropRt == null) yield break;

            if (delay > 0f)
            {
                dropRt.localScale = Vector3.zero;
                yield return new WaitForSeconds(delay);
                if (dropRt == null) yield break;
                dropRt.localScale = Vector3.one;
            }

            float currentY = startY;
            float currentX = startX;
            float driftSpeed = UnityEngine.Random.Range(-18f, 18f);

            bool isCaught = false;

            while (isMountainOpen && dropRt != null && currentY > -310f)
            {
                currentY -= speed * Time.deltaTime;
                currentX += driftSpeed * Time.deltaTime;
                currentX = Mathf.Clamp(currentX, -360f, 360f);

                dropRt.anchoredPosition = new Vector2(currentX, currentY);

                // Check collision with dragged bucket
                if (bucketRectTransform != null && !isCaught)
                {
                    Vector2 bucketPos = bucketRectTransform.anchoredPosition;
                    float bucketCatchTopY = bucketPos.y + 45f;
                    float bucketCatchBottomY = bucketPos.y - 35f;
                    float bucketCatchRadiusX = 65f;

                    if (currentY <= bucketCatchTopY && currentY >= bucketCatchBottomY)
                    {
                        if (Mathf.Abs(currentX - bucketPos.x) <= bucketCatchRadiusX)
                        {
                            isCaught = true;
                            OnDewDropCaught(dropObj);
                            yield break;
                        }
                    }
                }

                yield return null;
            }

            // Missed and fallen into mountain abyss
            if (!isCaught && dropObj != null)
            {
                activeDewDropObjects.Remove(dropObj);
                Destroy(dropObj);
                remainingActiveDewDrops = Mathf.Max(0, remainingActiveDewDrops - 1);
                CheckAllDewResolved();
            }
        }

        private void OnDewDropCaught(GameObject dropObj)
        {
            if (dropObj == null) return;

            PlaySound(dewCatchSound);

            sessionCaughtCount++;
            remainingActiveDewDrops = Mathf.Max(0, remainingActiveDewDrops - 1);
            activeDewDropObjects.Remove(dropObj);
            InventoryManager.Instance?.AddRawStock(RawIngredientType.GoldenDew, 1);

            UpdateHarvestCounterDisplay();
            SpawnCollectPopText(dropObj.transform.position, "+1 Raw Golden Dew!");

            // 15% rare mountain cash discovery roll
            if (UnityEngine.Random.value < 0.15f)
            {
                EconomyManager.Instance?.AddCash(50.00f, "Misty Mountain Mineral Discovery");
                SpawnCollectPopText(dropObj.transform.position + new Vector3(0, 30f, 0), "<color=#F1C40F>+Mountain Discovery (+$50.00)</color>");
            }

            // Bucket catch bounce animation
            if (bucketRectTransform != null)
            {
                StartCoroutine(BucketCatchWobbleRoutine());
            }

            Destroy(dropObj);
            CheckAllDewResolved();
        }

        private IEnumerator BucketCatchWobbleRoutine()
        {
            if (bucketRectTransform == null) yield break;
            Vector3 baseScale = Vector3.one;
            float elapsed = 0f;
            float duration = 0.22f;

            while (elapsed < duration && bucketRectTransform != null)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float squash = 1f + (Mathf.Sin(progress * Mathf.PI) * 0.18f);
                float stretch = 1f - (Mathf.Sin(progress * Mathf.PI) * 0.12f);
                bucketRectTransform.localScale = new Vector3(baseScale.x * squash, baseScale.y * stretch, 1f);
                yield return null;
            }

            if (bucketRectTransform != null) bucketRectTransform.localScale = Vector3.one;
        }

        private void CheckAllDewResolved()
        {
            if (remainingActiveDewDrops <= 0 && hasKickedWall)
            {
                HUDController.Instance?.SetStatusHint(CLEARED_HINT);

                if (sessionCaughtCount == totalSpawnedCount)
                {
                    PlaySound(completeSound);
                    HUDController.Instance?.ShowNotification($"Flawless catch! Collected all Golden Dew! (Harvested: <color=#2ECC71>+{sessionCaughtCount}</color>)", 4f);
                }
                else
                {
                    HUDController.Instance?.ShowNotification($"Expedition finished! Caught <color=#2ECC71>+{sessionCaughtCount} Golden Dew</color>.", 4f);
                }
            }
        }

        // =========================================================================
        // DRAG BUCKET INTERACTION
        // =========================================================================
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (currentStage == MistMountainStage.RockWallCatching && bucketRectTransform != null)
            {
                isDraggingBucket = true;
                UpdateBucketPositionFromPointer(eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isDraggingBucket && currentStage == MistMountainStage.RockWallCatching && bucketRectTransform != null)
            {
                UpdateBucketPositionFromPointer(eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDraggingBucket = false;
        }

        private void UpdateBucketPositionFromPointer(PointerEventData eventData)
        {
            if (bucketRectTransform == null || mistMountainPanelRoot == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mistMountainPanelRoot.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint
            );

            float clampedX = Mathf.Clamp(localPoint.x, -360f, 360f);
            float clampedY = Mathf.Clamp(localPoint.y, -270f, -120f);
            bucketRectTransform.anchoredPosition = new Vector2(clampedX, clampedY);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (currentStage == MistMountainStage.RockWallCatching && !hasKickedWall)
            {
                OnRockWallClicked();
            }
            else if (currentStage == MistMountainStage.PanoramaApproach)
            {
                OnRockShelfClicked();
            }
        }

        private void SpawnCollectPopText(Vector3 worldPos, string text)
        {
            GameObject popObj = new GameObject("CollectPopText", typeof(RectTransform), typeof(TextMeshProUGUI));
            popObj.transform.SetParent(mistMountainPanelRoot.transform, false);
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

        private void StopAllDewRoutines()
        {
            foreach (var routine in activeDewDropRoutines)
            {
                if (routine != null) StopCoroutine(routine);
            }
            activeDewDropRoutines.Clear();
        }

        private void ClearAllDewDrops()
        {
            foreach (var drop in activeDewDropObjects)
            {
                if (drop != null) Destroy(drop);
            }
            activeDewDropObjects.Clear();

            Transform container = (rockWallObject != null) ? rockWallObject.transform : mistMountainPanelRoot.transform;
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Transform child = container.GetChild(i);
                if (child.name.StartsWith("GoldenDewDrop_") || child.name.StartsWith("CollectPopText"))
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
