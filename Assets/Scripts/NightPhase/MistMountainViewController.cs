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
        public static MistMountainViewController Instance { get; private set; }

        public enum MistMountainStage
        {
            PanoramaApproach, // Viewing wide mountain and clicking rock shelves
            RockWallCatching  // Close-up rock wall and bucket catching minigame
        }

        [Header("Root & Screen Panels")]
        [SerializeField] private GameObject mistMountainPanelRoot;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite mountainPanoramaSprite;
        [SerializeField] private Sprite rockWallSprite;
        [SerializeField] private Sprite rockShelfSprite;
        [SerializeField] private Sprite rawGoldenDewSprite;
        [SerializeField] private Sprite bucketSprite;
        [SerializeField] private Button returnToNightHubButton;

        [Header("UI Header & Harvest Counter")]
        [SerializeField] private TextMeshProUGUI harvestCounterText;

        [Header("Stage 1: Panorama Rock Shelves")]
        [SerializeField] private Transform rockShelvesContainer;
        [SerializeField] private List<Button> rockShelfButtons = new List<Button>();

        [Header("Stage 2: Close-up Rock Wall & Bucket Catching")]
        [SerializeField] private Transform rockWallContainer;
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
        private List<Coroutine> activeDewDropRoutines = new List<Coroutine>();
        private List<GameObject> activeDewDropObjects = new List<GameObject>();
        private Vector2 rootPanelBaseAnchoredPos = Vector2.zero;

        private const string PANORAMA_HINT = "Misty Mountains: Select a mineral-rich rock shelf to approach the cliffside!";
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
        }

        private void Start()
        {
            WireButtonsAndListeners();
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

            if (rockShelvesContainer == null && mistMountainPanelRoot != null)
            {
                Transform t = mistMountainPanelRoot.transform.Find("RockShelvesContainer");
                if (t != null) rockShelvesContainer = t;
            }

            if (rockWallContainer == null && mistMountainPanelRoot != null)
            {
                Transform t = mistMountainPanelRoot.transform.Find("RockWallContainer");
                if (t != null) rockWallContainer = t;
            }

            if (bucketRectTransform == null && mistMountainPanelRoot != null)
            {
                Transform b = mistMountainPanelRoot.transform.Find("CatchBucket");
                if (b == null && rockWallContainer != null) b = rockWallContainer.Find("CatchBucket");
                if (b != null)
                {
                    bucketRectTransform = b.GetComponent<RectTransform>();
                    bucketImage = b.GetComponent<Image>();
                }
            }
        }

        private void EnsureFallbackAssets()
        {
#if UNITY_EDITOR
            if (mountainPanoramaSprite == null)
            {
                mountainPanoramaSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Sprites2/Sprites 3/Forage and prep/mountains/mistmountain.jpg");
            }
            if (rockWallSprite == null)
            {
                rockWallSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Sprites2/Sprites 3/Forage and prep/mountains/RockWall.jpg");
            }
            if (rockShelfSprite == null)
            {
                rockShelfSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Sprites2/Sprites 3/Forage and prep/mountains/Rockshelf.png");
            }
            if (rawGoldenDewSprite == null)
            {
                var allRaw = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Sprites2/Sprites 3/Forage and prep/raw ingre.png");
                foreach (var a in allRaw)
                {
                    if (a is Sprite s && (s.name == "raw ingre_1" || s.name.Contains("1")))
                    {
                        rawGoldenDewSprite = s;
                        break;
                    }
                }
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
            if (mountainPanoramaSprite == null || rockWallSprite == null || rockShelfSprite == null || rawGoldenDewSprite == null || bucketSprite == null)
            {
                var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
                for (int i = 0; i < allSprites.Length; i++)
                {
                    var s = allSprites[i];
                    if (s == null) continue;
                    if (mountainPanoramaSprite == null && s.name.ToLower().Contains("mistmountain")) mountainPanoramaSprite = s;
                    if (rockWallSprite == null && s.name.ToLower().Contains("rockwall")) rockWallSprite = s;
                    if (rockShelfSprite == null && s.name.ToLower().Contains("rockshelf")) rockShelfSprite = s;
                    if (rawGoldenDewSprite == null && (s.name == "raw ingre_1" || s.name.ToLower().Contains("dew"))) rawGoldenDewSprite = s;
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
                var canvas = FindObjectOfType<Canvas>();
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

            // 4. Stage 1: Rock Shelves Container
            GameObject shelvesObj = new GameObject("RockShelvesContainer", typeof(RectTransform));
            shelvesObj.transform.SetParent(rootObj.transform, false);
            var shelvesRt = shelvesObj.GetComponent<RectTransform>();
            shelvesRt.anchorMin = Vector2.zero;
            shelvesRt.anchorMax = Vector2.one;
            shelvesRt.offsetMin = Vector2.zero;
            shelvesRt.offsetMax = Vector2.zero;
            rockShelvesContainer = shelvesObj.transform;

            // 5. Stage 2: Rock Wall Container
            GameObject wallObj = new GameObject("RockWallContainer", typeof(RectTransform), typeof(Image), typeof(Button));
            wallObj.transform.SetParent(rootObj.transform, false);
            var wallRt = wallObj.GetComponent<RectTransform>();
            wallRt.anchorMin = Vector2.zero;
            wallRt.anchorMax = Vector2.one;
            wallRt.offsetMin = Vector2.zero;
            wallRt.offsetMax = Vector2.zero;

            rockWallImage = wallObj.GetComponent<Image>();
            if (rockWallSprite != null) rockWallImage.sprite = rockWallSprite;
            rockWallImage.color = Color.white;
            rockWallImage.raycastTarget = true;
            rockWallContainer = wallObj.transform;

            var wallBtn = wallObj.GetComponent<Button>();
            wallBtn.transition = Selectable.Transition.None;
            wallBtn.onClick.AddListener(OnRockWallClicked);

            // 6. Catch Bucket inside RockWallContainer
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

            if (rockWallContainer != null)
            {
                var btn = rockWallContainer.GetComponent<Button>();
                if (btn == null) btn = rockWallContainer.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.interactable = true;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnRockWallClicked);
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

            StopAllDewRoutines();
            ClearAllDewDrops();

            if (mistMountainPanelRoot != null)
            {
                var rt = mistMountainPanelRoot.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = rootPanelBaseAnchoredPos;
                mistMountainPanelRoot.SetActive(false);
            }

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
        // STAGE 1: PANORAMA ROCK SHELVES
        // =========================================================================
        private void SetupPanoramaApproachStage()
        {
            currentStage = MistMountainStage.PanoramaApproach;

            if (backgroundImage != null && mountainPanoramaSprite != null)
            {
                backgroundImage.sprite = mountainPanoramaSprite;
            }

            if (rockWallContainer != null) rockWallContainer.gameObject.SetActive(false);
            if (rockShelvesContainer != null)
            {
                rockShelvesContainer.gameObject.SetActive(true);
                BuildInteractiveRockShelves();
            }

            HUDController.Instance?.SetSubscreenMode(true, PANORAMA_HINT);
        }

        private void BuildInteractiveRockShelves()
        {
            if (rockShelvesContainer == null) return;

            // Clear old children
            for (int i = rockShelvesContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(rockShelvesContainer.GetChild(i).gameObject);
            }
            rockShelfButtons.Clear();

            // 3 rock shelf locations on the mountain path
            Vector2[] shelfPositions = new Vector2[]
            {
                new Vector2(-260f, -80f),
                new Vector2(40f, 20f),
                new Vector2(280f, 130f)
            };

            for (int i = 0; i < shelfPositions.Length; i++)
            {
                int shelfIndex = i + 1;
                Vector2 pos = shelfPositions[i];

                GameObject shelfObj = new GameObject($"RockShelf_{shelfIndex}", typeof(RectTransform), typeof(Image), typeof(Button));
                shelfObj.transform.SetParent(rockShelvesContainer, false);
                var rt = shelfObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = pos;
                rt.sizeDelta = new Vector2(210f, 110f);

                var img = shelfObj.GetComponent<Image>();
                if (rockShelfSprite != null) img.sprite = rockShelfSprite;
                img.preserveAspect = true;
                img.raycastTarget = true;

                var btn = shelfObj.GetComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => OnRockShelfSelected(shelfIndex));
                rockShelfButtons.Add(btn);

                // Subtle floating breathing animation
                StartCoroutine(ShelfHoverRoutine(rt, 1.8f + (i * 0.3f), i * 1.2f));
            }
        }

        private IEnumerator ShelfHoverRoutine(RectTransform rt, float speed, float phaseOffset)
        {
            if (rt == null) yield break;
            Vector2 basePos = rt.anchoredPosition;

            while (isMountainOpen && currentStage == MistMountainStage.PanoramaApproach && rt != null)
            {
                float offset = Mathf.Sin((Time.time * speed) + phaseOffset) * 6f;
                rt.anchoredPosition = basePos + new Vector2(0, offset);
                yield return null;
            }
        }

        private void OnRockShelfSelected(int shelfIndex)
        {
            if (currentStage != MistMountainStage.PanoramaApproach) return;
            PlaySound(rockKickSound);
            TransitionToRockWallStage();
        }

        // =========================================================================
        // STAGE 2: CLOSE-UP ROCK WALL & BUCKET CATCHING
        // =========================================================================
        private void TransitionToRockWallStage()
        {
            currentStage = MistMountainStage.RockWallCatching;
            hasKickedWall = false;

            if (rockShelvesContainer != null) rockShelvesContainer.gameObject.SetActive(false);

            if (rockWallContainer != null)
            {
                rockWallContainer.gameObject.SetActive(true);
                if (rockWallImage != null && rockWallSprite != null)
                {
                    rockWallImage.sprite = rockWallSprite;
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
            RectTransform wallRt = rockWallContainer as RectTransform;

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

            Transform container = (rockWallContainer != null) ? rockWallContainer : mistMountainPanelRoot.transform;

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
                if (rawGoldenDewSprite != null) img.sprite = rawGoldenDewSprite;
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
                    float bucketCatchRadiusX = 65f; // Catch zone horizontal tolerance

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
            if (currentStage == MistMountainStage.RockWallCatching && bucketRectTransform != null)
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
            bucketRectTransform.anchoredPosition = new Vector2(clampedX, -220f);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (currentStage == MistMountainStage.RockWallCatching && !hasKickedWall)
            {
                OnRockWallClicked();
            }
            else if (currentStage == MistMountainStage.PanoramaApproach)
            {
                // If clicked somewhere near a shelf or background, trigger shelf selection
                if (rockShelfButtons.Count > 0 && rockShelfButtons[0] != null)
                {
                    OnRockShelfSelected(1);
                }
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

            Transform container = (rockWallContainer != null) ? rockWallContainer : mistMountainPanelRoot.transform;
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
