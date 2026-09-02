using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    public class EndGameModal : MonoBehaviour
    {
        [SerializeField] private GameObject modalRoot;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button endlessModeButton;

        [Header("Victory Sound & FX")]
        [SerializeField] private AudioClip victorySound;

        private Transform confettiContainer;
        private Coroutine confettiRoutine;

        private void Awake()
        {
            EnsureUIReferences();
            EnsureFallbackAssets();
        }

        private void Start()
        {
            EnsureUIReferences();
            EnsureFallbackAssets();

            if (modalRoot != null) modalRoot.SetActive(false);
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(RestartGame);
                restartButton.onClick.AddListener(RestartGame);
            }
            if (endlessModeButton != null)
            {
                endlessModeButton.onClick.RemoveListener(ContinueInEndlessMode);
                endlessModeButton.onClick.AddListener(ContinueInEndlessMode);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
                GameManager.Instance.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
            }
        }

        private void EnsureFallbackAssets()
        {
#if UNITY_EDITOR
            if (victorySound == null)
            {
                victorySound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/prep and forage/YIPPEEEEEEEEEEEEEE - QuickSounds.com.mp3");
            }
#endif
            if (victorySound == null)
            {
                var allAudio = Resources.FindObjectsOfTypeAll<AudioClip>();
                for (int i = 0; i < allAudio.Length; i++)
                {
                    var a = allAudio[i];
                    if (a != null && (a.name.ToLower().Contains("yippee") || a.name.ToLower().Contains("victory") || a.name.ToLower().Contains("complete")))
                    {
                        victorySound = a;
                        break;
                    }
                }
            }
        }

        private void EnsureUIReferences()
        {
            if (modalRoot == null) modalRoot = gameObject;

            if (restartButton == null)
            {
                var btns = GetComponentsInChildren<Button>(true);
                foreach (var b in btns)
                {
                    if (b.name.ToLower().Contains("restart") || b.name.ToLower().Contains("play") || b.name.ToLower().Contains("again"))
                    {
                        restartButton = b;
                        break;
                    }
                }
                if (restartButton == null && btns.Length > 0)
                {
                    restartButton = btns[0];
                }
            }

            if (endlessModeButton == null && restartButton != null)
            {
                // Look for existing button named Endless
                var btns = GetComponentsInChildren<Button>(true);
                foreach (var b in btns)
                {
                    if (b.name.ToLower().Contains("endless") || b.name.ToLower().Contains("continue"))
                    {
                        endlessModeButton = b;
                        break;
                    }
                }

                // If not found, dynamically clone from restartButton
                if (endlessModeButton == null && restartButton.transform.parent != null)
                {
                    GameObject endlessObj = Instantiate(restartButton.gameObject, restartButton.transform.parent);
                    endlessObj.name = "EndlessModeBtn";
                    endlessModeButton = endlessObj.GetComponent<Button>();
                    endlessModeButton.onClick.RemoveAllListeners();
                    endlessModeButton.onClick.AddListener(ContinueInEndlessMode);
                    var txt = endlessObj.GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null)
                    {
                        txt.text = "Endless Mode";
                    }
                }
            }

            if (titleText == null || messageText == null)
            {
                var tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in tmps)
                {
                    if (t.name.ToLower().Contains("title") && titleText == null) titleText = t;
                    else if ((t.name.ToLower().Contains("msg") || t.name.ToLower().Contains("message")) && messageText == null) messageText = t;
                }
            }
        }

        private void HandleStateChanged(GameState state)
        {
            EnsureUIReferences();

            if (state == GameState.GameOver)
            {
                ClearConfetti();
                if (modalRoot != null) modalRoot.SetActive(true);
                if (titleText != null) titleText.text = "<color=#FF4444>EVICTION NOTICE</color>";
                if (messageText != null) messageText.text = "You were unable to afford weekly rent. The landlord has locked the shutters and taken over the shop.";

                if (restartButton != null)
                {
                    restartButton.gameObject.SetActive(true);
                    var rt = restartButton.GetComponent<RectTransform>();
                    if (rt != null) rt.anchoredPosition = new Vector2(0f, -120f);
                    var txt = restartButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null) txt.text = "Try Again";
                }

                if (endlessModeButton != null)
                {
                    endlessModeButton.gameObject.SetActive(false);
                }
            }
            else if (state == GameState.GameWon)
            {
                if (modalRoot != null) modalRoot.SetActive(true);
                if (titleText != null) titleText.text = "<color=#FFD700>LOCATION BOUGHT OVER!</color>";
                if (messageText != null) messageText.text = "Congratulations! You earned enough through brewing and selling bubble tea to buy the deed to the building! The shop is forever yours!\n\n<size=80%><color=#A8E6CF>You can now continue in <b>Endless Mode</b> with weekly exponential rent escalation!</color></size>";

                if (restartButton != null)
                {
                    restartButton.gameObject.SetActive(true);
                    var rt = restartButton.GetComponent<RectTransform>();
                    if (rt != null) rt.anchoredPosition = new Vector2(-250f, -250f);
                    var txt = restartButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null) txt.text = "Play Again";
                }

                if (endlessModeButton != null)
                {
                    endlessModeButton.gameObject.SetActive(true);
                    var rt = endlessModeButton.GetComponent<RectTransform>();
                    if (rt != null) rt.anchoredPosition = new Vector2(200f, -250f);
                    var txt = endlessModeButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null) txt.text = "Endless Mode";
                }

                PlaySound(victorySound);
                TriggerConfettiExplosion();
            }
        }

        // =========================================================================
        // CELEBRATION FX & CONFETTI EXPLOSION
        // =========================================================================
        private void TriggerConfettiExplosion()
        {
            if (confettiRoutine != null) StopCoroutine(confettiRoutine);
            confettiRoutine = StartCoroutine(ConfettiExplosionRoutine());
        }

        private IEnumerator ConfettiExplosionRoutine()
        {
            EnsureConfettiContainer();
            if (confettiContainer == null) yield break;

            ClearConfetti();

            Color[] confettiColors = new Color[]
            {
                new Color(1.0f, 0.84f, 0.0f, 1f),   // Gold
                new Color(0.18f, 0.80f, 0.44f, 1f), // Emerald Green
                new Color(0.00f, 0.82f, 0.83f, 1f), // Bright Cyan
                new Color(1.0f, 0.42f, 0.51f, 1f),  // Coral Pink
                new Color(0.65f, 0.37f, 0.92f, 1f), // Vibrant Purple
                new Color(1.0f, 0.65f, 0.01f, 1f),  // Warm Orange
                new Color(0.98f, 0.95f, 0.46f, 1f), // Pastel Lemon
                new Color(0.95f, 0.28f, 0.34f, 1f)  // Strawberry Red
            };

            int wave1 = 65;
            int wave2 = 35;

            // Wave 1 - Initial Main Blast
            SpawnConfettiWave(wave1, confettiColors);

            yield return new WaitForSeconds(0.28f);

            // Wave 2 - Follow-up Burst
            SpawnConfettiWave(wave2, confettiColors);
        }

        private void EnsureConfettiContainer()
        {
            if (confettiContainer == null && modalRoot != null)
            {
                Transform existing = modalRoot.transform.Find("ConfettiContainer");
                if (existing != null)
                {
                    confettiContainer = existing;
                }
                else
                {
                    GameObject cObj = new GameObject("ConfettiContainer", typeof(RectTransform));
                    cObj.transform.SetParent(modalRoot.transform, false);
                    cObj.transform.SetAsFirstSibling(); // Behind text/buttons but in front of background
                    var rt = cObj.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    confettiContainer = cObj.transform;
                }
            }
        }

        private void ClearConfetti()
        {
            if (confettiRoutine != null)
            {
                StopCoroutine(confettiRoutine);
                confettiRoutine = null;
            }

            if (confettiContainer != null)
            {
                for (int i = confettiContainer.childCount - 1; i >= 0; i--)
                {
                    Destroy(confettiContainer.GetChild(i).gameObject);
                }
            }
        }

        private void SpawnConfettiWave(int count, Color[] colors)
        {
            if (confettiContainer == null) return;

            for (int i = 0; i < count; i++)
            {
                GameObject pObj = new GameObject($"Confetti_{i}", typeof(RectTransform), typeof(Image));
                pObj.transform.SetParent(confettiContainer, false);

                var rt = pObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);

                // Start near center
                float startX = UnityEngine.Random.Range(-180f, 180f);
                float startY = UnityEngine.Random.Range(-120f, -20f);
                rt.anchoredPosition = new Vector2(startX, startY);

                // Varied shapes: square, tall strip, ribbon rectangle
                float w = UnityEngine.Random.Range(10f, 22f);
                float h = UnityEngine.Random.Range(12f, 26f);
                rt.sizeDelta = new Vector2(w, h);

                var img = pObj.GetComponent<Image>();
                Color baseColor = colors[UnityEngine.Random.Range(0, colors.Length)];
                img.color = baseColor;
                img.raycastTarget = false;

                // Explosive fountain burst trajectory
                float angleDeg = UnityEngine.Random.Range(35f, 145f); // Upward celebratory fountain
                float angleRad = angleDeg * Mathf.Deg2Rad;
                float speed = UnityEngine.Random.Range(450f, 950f);
                Vector2 initialVelocity = new Vector2(Mathf.Cos(angleRad) * speed, Mathf.Sin(angleRad) * speed);

                float rotationSpeed = UnityEngine.Random.Range(-360f, 360f);
                float flipSpeed = UnityEngine.Random.Range(2.5f, 9f);
                float flutterFreq = UnityEngine.Random.Range(3f, 7f);
                float flutterAmp = UnityEngine.Random.Range(15f, 40f);
                float gravity = UnityEngine.Random.Range(550f, 780f);
                float lifetime = UnityEngine.Random.Range(3.2f, 4.8f);

                StartCoroutine(AnimateConfettiPiece(rt, img, initialVelocity, rotationSpeed, flipSpeed, flutterFreq, flutterAmp, gravity, lifetime));
            }
        }

        private IEnumerator AnimateConfettiPiece(RectTransform rt, Image img, Vector2 velocity, float rotSpeed, float flipSpeed, float flutterFreq, float flutterAmp, float gravity, float lifetime)
        {
            float elapsed = 0f;
            Vector2 currentPos = rt.anchoredPosition;
            Color baseColor = img.color;

            while (elapsed < lifetime && rt != null && img != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / lifetime;

                // Physics simulation
                velocity.y -= gravity * Time.deltaTime;
                velocity.x = Mathf.Lerp(velocity.x, 0f, Time.deltaTime * 1.5f); // Air drag

                float flutterOffset = Mathf.Sin(elapsed * flutterFreq) * flutterAmp * Time.deltaTime * 60f;
                currentPos += new Vector2((velocity.x + flutterOffset) * Time.deltaTime, velocity.y * Time.deltaTime);
                rt.anchoredPosition = currentPos;

                // 3D paper tumble & rotation
                float zRot = elapsed * rotSpeed;
                float xFlip = Mathf.Cos(elapsed * flipSpeed);
                rt.localRotation = Quaternion.Euler(0f, 0f, zRot);
                rt.localScale = new Vector3(xFlip, 1f, 1f);

                // Gentle alpha fade out in the final stretch
                if (t > 0.65f)
                {
                    float fadeAlpha = Mathf.InverseLerp(1f, 0.65f, t);
                    img.color = new Color(baseColor.r, baseColor.g, baseColor.b, fadeAlpha);
                }

                yield return null;
            }

            if (rt != null)
            {
                Destroy(rt.gameObject);
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(clip);
            }
        }

        public void ContinueInEndlessMode()
        {
            ClearConfetti();

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.IsEndlessMode = true;
            }

            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.NightPhase);
            }

            HUDController.Instance?.RefreshHUDDisplay();
            HUDController.Instance?.ShowNotification("Endless Mode Activated! Weekly rent will now escalate exponentially!", 5f);
        }

        public void RestartGame()
        {
            ClearConfetti();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
