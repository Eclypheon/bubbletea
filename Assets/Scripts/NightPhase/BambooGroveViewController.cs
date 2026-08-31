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
        public static BambooGroveViewController Instance { get; private set; }

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
        [SerializeField] private Sprite grassPileSprite;
        [SerializeField] private Transform grassPatchesContainer;
        [SerializeField] private List<Button> grassPatchButtons = new List<Button>();

        [Header("Critter Spawning Tuning (Editable)")]
        [Range(1, 10)]
        [SerializeField] private int minYippeesPerPatch = 1;
        [Range(1, 10)]
        [SerializeField] private int maxYippeesPerPatch = 3;
        [SerializeField] private Transform crittersContainer;
        [SerializeField] private Sprite[] babyYippeeRunSprites;
        [SerializeField] private Sprite babyYippeeStaticSprite;

        [Header("Audio SFX")]
        [SerializeField] private AudioClip grassRustleSound;
        [SerializeField] private AudioClip catchCritterSound;
        [SerializeField] private AudioClip completeSound;

        public event Action OnBambooGroveClosed;

        private int sessionCaughtCount = 0;
        private int remainingActiveCritters = 0;
        private bool isGroveOpen = false;
        private List<Coroutine> activeWobbleRoutines = new List<Coroutine>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (bambooGrovePanelRoot == null)
            {
                bambooGrovePanelRoot = gameObject;
            }

            if (returnToNightHubButton != null)
            {
                returnToNightHubButton.onClick.AddListener(CloseBambooGroveView);
                returnToNightHubButton.gameObject.SetActive(false);
            }

            if (bambooGrovePanelRoot != null)
            {
                bambooGrovePanelRoot.SetActive(false);
            }
        }

        public void OpenBambooGroveView(int dayNumber)
        {
            isGroveOpen = true;
            sessionCaughtCount = 0;
            remainingActiveCritters = 0;

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

            HUDController.Instance?.SetSubscreenMode(true, "🎋 Bamboo Grove: Tap the rustling grass to flush out wild Baby Yippees, then catch them!");

            UpdateHarvestCounterDisplay();
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

            if (sessionCaughtCount > 0)
            {
                PlaySound(completeSound);
                HUDController.Instance?.ShowNotification($"🎉 Foraging completed! Bagged <color=#2ECC71>+{sessionCaughtCount} Baby Yippees</color> in your expedition!", 4.5f);
            }

            if (returnToNightHubButton != null)
            {
                returnToNightHubButton.gameObject.SetActive(false);
            }

            if (bambooGrovePanelRoot != null)
            {
                bambooGrovePanelRoot.SetActive(false);
            }

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
                if (grassPileSprite != null)
                {
                    img.sprite = grassPileSprite;
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
            int countToSpawn = UnityEngine.Random.Range(minYippeesPerPatch, maxYippeesPerPatch + 1); // 1-3 critters per patch

            HUDController.Instance?.ShowNotification($"💨 Wild Baby Yippees scattered into the bamboo! Tap them quickly!", 2.5f);

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
                Sprite critterSprite = (babyYippeeStaticSprite != null)
                    ? babyYippeeStaticSprite
                    : ((babyYippeeRunSprites != null && babyYippeeRunSprites.Length > 0) ? babyYippeeRunSprites[0] : null);

                img.sprite = critterSprite;
                img.preserveAspect = true;

                var btn = critterObj.GetComponent<Button>();
                var capturedObj = critterObj;
                btn.onClick.AddListener(() => OnCritterCaught(capturedObj));

                StartCoroutine(CritterScurryRoutine(rt, img));
            }
        }

        private IEnumerator CritterScurryRoutine(RectTransform rt, Image img)
        {
            if (rt == null) yield break;

            Vector2 moveDir = new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-0.8f, 0.8f)).normalized;
            float speed = UnityEngine.Random.Range(150f, 240f);
            float lifetime = 14f; // Stay active for 14 seconds before burrowing away
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

                // Flip sprite depending on horizontal movement
                float dirScaleX = (moveDir.x > 0.05f) ? -1f : 1f;
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
                HUDController.Instance?.ShowNotification($"🌟 All critters caught from this patch! Total: <color=#2ECC71>{sessionCaughtCount} Baby Yippees</color>.", 3f);
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
