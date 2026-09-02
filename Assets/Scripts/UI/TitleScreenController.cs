using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    [AddComponentMenu("BubbleTea/Title Screen Controller")]
    public class TitleScreenController : MonoBehaviour
    {
        public static TitleScreenController Instance { get; private set; }

        [Header("Title Screen Root")]
        [Tooltip("The main container GameObject for the entire Title Screen (defaults to this GameObject if unassigned).")]
        [SerializeField] private GameObject titleScreenRoot;

        [Header("Logo & Decorative Sprites")]
        [Tooltip("UI Image component displaying the game logo.")]
        [SerializeField] private Image gameLogoImage;

        [Tooltip("Any decorative images/critters placed around the title screen.")]
        [SerializeField] private Image[] decorativeImages;

        [Tooltip("Gentle floating bobbing animation for logo and decorations.")]
        [SerializeField] private bool enableBobbingAnimation = true;

        [Header("Main Menu Buttons")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueGameButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button creditsButton;

        [Header("Game Mode Selection Buttons")]
        [SerializeField] private Button normalModeButton;
        [SerializeField] private Button blitzModeButton;
        [SerializeField] private Button backButton;

        [Header("Options / Audio Settings UI")]
        [Tooltip("Container GameObject for Options sliders & labels. Auto-created if unassigned.")]
        [SerializeField] private GameObject optionsContainer;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TextMeshProUGUI musicVolumeValueText;
        [SerializeField] private TextMeshProUGUI sfxVolumeValueText;
        [SerializeField] private Button testSFXButton;
        [SerializeField] private Button fullscreenToggleButton;
        [SerializeField] private TextMeshProUGUI fullscreenToggleText;
        [SerializeField] private Button difficultyToggleButton;
        [SerializeField] private TextMeshProUGUI difficultyToggleText;

        [Header("Credits UI")]
        [Tooltip("Container GameObject for Credits text. Auto-created if unassigned.")]
        [SerializeField] private GameObject creditsContainer;
        [SerializeField] private TextMeshProUGUI creditsText;

        [Header("Audio SFX (Optional)")]
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip startChimeSound;
        [SerializeField] private AudioClip testSFXClip;

        public GameObject OptionsContainer => optionsContainer;
        public GameObject CreditsContainer => creditsContainer;
        public Slider MusicVolumeSlider => musicVolumeSlider;
        public Slider SFXVolumeSlider => sfxVolumeSlider;
        public Button TestSFXButton => testSFXButton;
        public Button FullscreenToggleButton => fullscreenToggleButton;
        public Button DifficultyToggleButton => difficultyToggleButton;

        public bool IsTitleScreenActive => titleScreenRoot != null ? titleScreenRoot.activeSelf : gameObject.activeSelf;

        public event Action<GameMode> OnGameStarted;

        private struct BobbingItem
        {
            public RectTransform transform;
            public Vector2 basePos;
            public float speed;
            public float amplitude;
            public float phase;
        }

        private List<BobbingItem> bobbingItems = new List<BobbingItem>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureReferences();
            InitializeBobbingItems();
            BringToFront();

            // Automatically place buttons in main menu state on initialization
            ShowMainMenu(playAudio: false);
        }

        private void Start()
        {
            WireButtonListeners();
            UpdateContinueButtonState();
            BringToFront();
            ShowMainMenu(playAudio: false);
        }

        private void Update()
        {
            if (!enableBobbingAnimation || bobbingItems.Count == 0 || !IsTitleScreenActive) return;

            float time = Time.unscaledTime;
            for (int i = 0; i < bobbingItems.Count; i++)
            {
                var item = bobbingItems[i];
                if (item.transform != null && item.transform.gameObject.activeInHierarchy)
                {
                    float offsetY = Mathf.Sin(time * item.speed + item.phase) * item.amplitude;
                    item.transform.anchoredPosition = new Vector2(item.basePos.x, item.basePos.y + offsetY);
                }
            }
        }

        private void InitializeBobbingItems()
        {
            bobbingItems.Clear();

            if (gameLogoImage != null)
            {
                var rt = gameLogoImage.rectTransform;
                bobbingItems.Add(new BobbingItem
                {
                    transform = rt,
                    basePos = rt.anchoredPosition,
                    speed = 1.6f,
                    amplitude = 6f,
                    phase = 0f
                });
            }

            if (decorativeImages != null)
            {
                for (int i = 0; i < decorativeImages.Length; i++)
                {
                    var img = decorativeImages[i];
                    if (img != null)
                    {
                        var rt = img.rectTransform;
                        bobbingItems.Add(new BobbingItem
                        {
                            transform = rt,
                            basePos = rt.anchoredPosition,
                            speed = 1.8f + (i * 0.35f),
                            amplitude = 8f + (i % 2 * 3f),
                            phase = i * 1.3f
                        });
                    }
                }
            }
        }

        private void EnsureReferences()
        {
            if (titleScreenRoot == null)
            {
                titleScreenRoot = gameObject;
            }

            // Ensure title screen renders on top of the storefront and blocks underlying clicks
            titleScreenRoot.transform.SetAsLastSibling();
            if (titleScreenRoot.TryGetComponent<Image>(out var bgImage))
            {
                bgImage.raycastTarget = true;
            }

            // Auto-discover buttons if unassigned
            if (newGameButton == null) newGameButton = FindButtonByName("NewGameButton", "NewGameBtn", "BtnNewGame", "NewGame");
            if (continueGameButton == null) continueGameButton = FindButtonByName("ContinueGameButton", "ContinueBtn", "BtnContinue", "Continue");
            if (optionsButton == null) optionsButton = FindButtonByName("OptionsButton", "OptionsBtn", "BtnOptions", "Options", "SettingsButton");
            if (creditsButton == null) creditsButton = FindButtonByName("CreditsButton", "CreditsBtn", "BtnCredits", "Credits");

            if (normalModeButton == null) normalModeButton = FindButtonByName("NormalModeButton", "NormalBtn", "BtnNormal", "Normal");
            if (blitzModeButton == null) blitzModeButton = FindButtonByName("BlitzModeButton", "BlitzBtn", "BtnBlitz", "Blitz");
            if (backButton == null) backButton = FindButtonByName("BackButton", "BackBtn", "ModeSelectBackButton", "ModeBackButton", "BackToMenuBtn");

            // Auto-discover logo if unassigned
            if (gameLogoImage == null)
            {
                gameLogoImage = transform.Find("GameLogo")?.GetComponent<Image>() ??
                                transform.Find("Logo")?.GetComponent<Image>() ??
                                transform.Find("LogoImage")?.GetComponent<Image>() ??
                                transform.Find("TitleLogo")?.GetComponent<Image>();
            }

            // Auto-discover decorative sprites if unassigned
            if (decorativeImages == null || decorativeImages.Length == 0)
            {
                List<Image> discovered = new List<Image>();
                for (int i = 1; i <= 8; i++)
                {
                    Transform decor = transform.Find($"Decorative_{i}") ??
                                      transform.Find($"Decor_{i}") ??
                                      transform.Find($"Sprite_{i}") ??
                                      transform.Find($"Critter_{i}") ??
                                      transform.Find($"Decoration_{i}");
                    if (decor != null && decor.TryGetComponent<Image>(out var img))
                    {
                        discovered.Add(img);
                    }
                }
                if (discovered.Count > 0)
                {
                    decorativeImages = discovered.ToArray();
                }
            }
        }

        private Button FindButtonByName(params string[] candidateNames)
        {
            foreach (var name in candidateNames)
            {
                Transform found = transform.Find(name);
                if (found != null && found.TryGetComponent<Button>(out var btn))
                {
                    return btn;
                }
            }

            var allButtons = GetComponentsInChildren<Button>(true);
            foreach (var btn in allButtons)
            {
                foreach (var name in candidateNames)
                {
                    if (btn.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        return btn;
                    }
                }
            }
            return null;
        }

        private void WireButtonListeners()
        {
            if (newGameButton != null)
            {
                newGameButton.onClick.RemoveListener(ShowModeSelect);
                newGameButton.onClick.AddListener(ShowModeSelect);
            }

            if (continueGameButton != null)
            {
                continueGameButton.onClick.RemoveListener(ContinueGame);
                continueGameButton.onClick.AddListener(ContinueGame);
            }

            if (optionsButton != null)
            {
                optionsButton.onClick.RemoveListener(ShowOptions);
                optionsButton.onClick.AddListener(ShowOptions);
            }

            if (creditsButton != null)
            {
                creditsButton.onClick.RemoveListener(ShowCredits);
                creditsButton.onClick.AddListener(ShowCredits);
            }

            if (normalModeButton != null)
            {
                normalModeButton.onClick.RemoveListener(StartNormalGame);
                normalModeButton.onClick.AddListener(StartNormalGame);
            }

            if (blitzModeButton != null)
            {
                blitzModeButton.onClick.RemoveListener(StartBlitzGame);
                blitzModeButton.onClick.AddListener(StartBlitzGame);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(ShowMainMenuWithAudio);
                backButton.onClick.AddListener(ShowMainMenuWithAudio);
            }
        }

        public void UpdateContinueButtonState()
        {
            if (continueGameButton != null)
            {
                // Greyed out for now (upcoming feature)
                continueGameButton.interactable = false;

                var txt = continueGameButton.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                {
                    txt.color = new Color(0.6f, 0.6f, 0.6f, 0.45f);
                }

                if (continueGameButton.TryGetComponent<CanvasGroup>(out var cg))
                {
                    cg.alpha = 0.45f;
                }
            }
        }

        public void EnsureOptionsUI()
        {
            if (optionsContainer != null && musicVolumeSlider != null && sfxVolumeSlider != null && fullscreenToggleButton != null) return;

            Transform rootTr = (titleScreenRoot != null) ? titleScreenRoot.transform : transform;

            // 1. Discover or create optionsContainer centered relative to optionsButton (shifted up 200px)
            if (optionsContainer == null)
            {
                Transform found = rootTr.Find("OptionsContainer") ?? rootTr.Find("AudioSettingsPanel") ?? rootTr.Find("SettingsPanel");
                if (found != null)
                {
                    optionsContainer = found.gameObject;
                }
                else
                {
                    optionsContainer = new GameObject("OptionsContainer", typeof(RectTransform));
                    optionsContainer.transform.SetParent(rootTr, false);

                    var containerRt = optionsContainer.GetComponent<RectTransform>();
                    containerRt.anchorMin = new Vector2(0.5f, 0.5f);
                    containerRt.anchorMax = new Vector2(0.5f, 0.5f);
                    containerRt.pivot = new Vector2(0.5f, 0.5f);

                    float targetX = 0f;
                    float targetY = 180f;
                    if (optionsButton != null && optionsButton.TryGetComponent<RectTransform>(out var optRt))
                    {
                        targetX = optRt.anchoredPosition.x;
                        targetY = optRt.anchoredPosition.y + 200f;
                    }
                    containerRt.anchoredPosition = new Vector2(targetX, targetY);
                    containerRt.sizeDelta = new Vector2(560f, 320f);
                }
            }

            // 2. Discover or create Music Volume Slider (BGM)
            if (musicVolumeSlider == null && optionsContainer != null)
            {
                Transform found = optionsContainer.transform.Find("MusicSliderContainer") ?? optionsContainer.transform.Find("MusicVolumeSlider");
                if (found != null && found.TryGetComponent<Slider>(out var s))
                {
                    musicVolumeSlider = s;
                    musicVolumeValueText = found.Find("ValueText")?.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    musicVolumeSlider = CreateSliderElement("MusicSliderContainer", optionsContainer.transform, new Vector2(0f, 90f), "BGM Volume", out musicVolumeValueText);
                }
            }

            // 3. Discover or create SFX Volume Slider
            if (sfxVolumeSlider == null && optionsContainer != null)
            {
                Transform found = optionsContainer.transform.Find("SFXSliderContainer") ?? optionsContainer.transform.Find("SFXVolumeSlider");
                if (found != null && found.TryGetComponent<Slider>(out var s))
                {
                    sfxVolumeSlider = s;
                    sfxVolumeValueText = found.Find("ValueText")?.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    sfxVolumeSlider = CreateSliderElement("SFXSliderContainer", optionsContainer.transform, new Vector2(0f, 30f), "SFX Volume", out sfxVolumeValueText);
                }
            }

            // 4. Discover or create Test SFX Button centered directly below the SFX slider
            if (testSFXButton == null && optionsContainer != null)
            {
                Transform found = optionsContainer.transform.Find("TestSFXButton") ?? optionsContainer.transform.Find("TestSFXBtn");
                if (found != null && found.TryGetComponent<Button>(out var tb))
                {
                    testSFXButton = tb;
                }
                else
                {
                    GameObject testBtnGo = new GameObject("TestSFXButton", typeof(RectTransform), typeof(Image), typeof(Button));
                    testBtnGo.transform.SetParent(optionsContainer.transform, false);

                    var testRt = testBtnGo.GetComponent<RectTransform>();
                    testRt.anchorMin = new Vector2(0.5f, 0.5f);
                    testRt.anchorMax = new Vector2(0.5f, 0.5f);
                    testRt.pivot = new Vector2(0.5f, 0.5f);
                    testRt.anchoredPosition = new Vector2(0f, -25f);
                    testRt.sizeDelta = new Vector2(160f, 34f);

                    var testImg = testBtnGo.GetComponent<Image>();
                    testImg.color = new Color(0.22f, 0.24f, 0.35f, 0.95f);

                    GameObject testTxtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    testTxtGo.transform.SetParent(testBtnGo.transform, false);
                    var testTxtRt = testTxtGo.GetComponent<RectTransform>();
                    testTxtRt.anchorMin = Vector2.zero;
                    testTxtRt.anchorMax = Vector2.one;
                    testTxtRt.offsetMin = Vector2.zero;
                    testTxtRt.offsetMax = Vector2.zero;

                    var testTmp = testTxtGo.GetComponent<TextMeshProUGUI>();
                    testTmp.text = "Test SFX";
                    testTmp.fontSize = 16;
                    testTmp.fontStyle = FontStyles.Bold;
                    testTmp.alignment = TextAlignmentOptions.Center;
                    testTmp.color = Color.white;
                    testTmp.raycastTarget = false;

                    testSFXButton = testBtnGo.GetComponent<Button>();
                }
            }

            // 5. Discover or create Fullscreen Toggle Button (with spacing below Test SFX button)
            if (fullscreenToggleButton == null && optionsContainer != null)
            {
                Transform found = optionsContainer.transform.Find("FullscreenToggleButton") ?? optionsContainer.transform.Find("FullscreenBtn");
                if (found != null && found.TryGetComponent<Button>(out var fb))
                {
                    fullscreenToggleButton = fb;
                    fullscreenToggleText = fb.GetComponentInChildren<TextMeshProUGUI>();
                }
                else
                {
                    GameObject fullBtnGo = new GameObject("FullscreenToggleButton", typeof(RectTransform), typeof(Image), typeof(Button));
                    fullBtnGo.transform.SetParent(optionsContainer.transform, false);

                    var fullRt = fullBtnGo.GetComponent<RectTransform>();
                    fullRt.anchorMin = new Vector2(0.5f, 0.5f);
                    fullRt.anchorMax = new Vector2(0.5f, 0.5f);
                    fullRt.pivot = new Vector2(0.5f, 0.5f);
                    fullRt.anchoredPosition = new Vector2(0f, -75f);
                    fullRt.sizeDelta = new Vector2(240f, 38f);

                    var fullImg = fullBtnGo.GetComponent<Image>();
                    fullImg.color = new Color(0.18f, 0.20f, 0.28f, 0.95f);

                    GameObject fullTxtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    fullTxtGo.transform.SetParent(fullBtnGo.transform, false);
                    var fullTxtRt = fullTxtGo.GetComponent<RectTransform>();
                    fullTxtRt.anchorMin = Vector2.zero;
                    fullTxtRt.anchorMax = Vector2.one;
                    fullTxtRt.offsetMin = Vector2.zero;
                    fullTxtRt.offsetMax = Vector2.zero;

                    fullscreenToggleText = fullTxtGo.GetComponent<TextMeshProUGUI>();
                    fullscreenToggleText.fontSize = 17;
                    fullscreenToggleText.fontStyle = FontStyles.Bold;
                    fullscreenToggleText.alignment = TextAlignmentOptions.Center;
                    fullscreenToggleText.color = Color.white;
                    fullscreenToggleText.raycastTarget = false;

                    fullscreenToggleButton = fullBtnGo.GetComponent<Button>();
                }
            }

            // 6. Discover or create Difficulty Toggle Button (situated below Fullscreen Toggle)
            if (difficultyToggleButton == null && optionsContainer != null)
            {
                Transform found = optionsContainer.transform.Find("DifficultyToggleButton") ?? optionsContainer.transform.Find("DifficultyBtn");
                if (found != null && found.TryGetComponent<Button>(out var db))
                {
                    difficultyToggleButton = db;
                    difficultyToggleText = db.GetComponentInChildren<TextMeshProUGUI>();
                }
                else
                {
                    GameObject diffBtnGo = new GameObject("DifficultyToggleButton", typeof(RectTransform), typeof(Image), typeof(Button));
                    diffBtnGo.transform.SetParent(optionsContainer.transform, false);

                    var diffRt = diffBtnGo.GetComponent<RectTransform>();
                    diffRt.anchorMin = new Vector2(0.5f, 0.5f);
                    diffRt.anchorMax = new Vector2(0.5f, 0.5f);
                    diffRt.pivot = new Vector2(0.5f, 0.5f);
                    diffRt.anchoredPosition = new Vector2(0f, -125f);
                    diffRt.sizeDelta = new Vector2(240f, 38f);

                    var diffImg = diffBtnGo.GetComponent<Image>();
                    diffImg.color = new Color(0.18f, 0.20f, 0.28f, 0.95f);

                    GameObject diffTxtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    diffTxtGo.transform.SetParent(diffBtnGo.transform, false);
                    var diffTxtRt = diffTxtGo.GetComponent<RectTransform>();
                    diffTxtRt.anchorMin = Vector2.zero;
                    diffTxtRt.anchorMax = Vector2.one;
                    diffTxtRt.offsetMin = Vector2.zero;
                    diffTxtRt.offsetMax = Vector2.zero;

                    difficultyToggleText = diffTxtGo.GetComponent<TextMeshProUGUI>();
                    difficultyToggleText.fontSize = 17;
                    difficultyToggleText.fontStyle = FontStyles.Bold;
                    difficultyToggleText.alignment = TextAlignmentOptions.Center;
                    difficultyToggleText.color = Color.white;
                    difficultyToggleText.raycastTarget = false;

                    difficultyToggleButton = diffBtnGo.GetComponent<Button>();
                }
            }

            WireSliderListeners();
            if (optionsContainer != null) optionsContainer.SetActive(false);
        }

        private Slider CreateSliderElement(string name, Transform parent, Vector2 pos, string labelText, out TextMeshProUGUI valueTextOut)
        {
            GameObject container = new GameObject(name, typeof(RectTransform));
            container.transform.SetParent(parent, false);
            var containerRt = container.GetComponent<RectTransform>();
            containerRt.anchorMin = new Vector2(0.5f, 0.5f);
            containerRt.anchorMax = new Vector2(0.5f, 0.5f);
            containerRt.pivot = new Vector2(0.5f, 0.5f);
            containerRt.anchoredPosition = pos;
            containerRt.sizeDelta = new Vector2(540f, 46f);

            // Label Text (right aligned to slider start)
            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(container.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0.5f, 0.5f);
            labelRt.anchorMax = new Vector2(0.5f, 0.5f);
            labelRt.pivot = new Vector2(1f, 0.5f);
            labelRt.anchoredPosition = new Vector2(-75f, 0f);
            labelRt.sizeDelta = new Vector2(160f, 36f);
            var lblTmp = labelGo.GetComponent<TextMeshProUGUI>();
            lblTmp.text = labelText;
            lblTmp.fontSize = 20;
            lblTmp.fontStyle = FontStyles.Bold;
            lblTmp.alignment = TextAlignmentOptions.MidlineRight;
            lblTmp.color = Color.white;
            lblTmp.raycastTarget = false;

            // Slider Component (centered)
            GameObject sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(container.transform, false);
            var sliderRt = sliderGo.GetComponent<RectTransform>();
            sliderRt.anchorMin = new Vector2(0.5f, 0.5f);
            sliderRt.anchorMax = new Vector2(0.5f, 0.5f);
            sliderRt.pivot = new Vector2(0f, 0.5f);
            sliderRt.anchoredPosition = new Vector2(-60f, 0f);
            sliderRt.sizeDelta = new Vector2(210f, 22f);

            var slider = sliderGo.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;

            // Background Track
            GameObject bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(sliderGo.transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGo.GetComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.22f, 0.95f);

            // Fill Area & Fill Bar
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            var fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = Vector2.zero;
            fillAreaRt.anchorMax = Vector2.one;
            fillAreaRt.offsetMin = new Vector2(2f, 2f);
            fillAreaRt.offsetMax = new Vector2(-2f, -2f);

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var fillImg = fill.GetComponent<Image>();
            fillImg.color = new Color(0.18f, 0.80f, 0.44f, 1f);

            slider.fillRect = fillRt;
            slider.targetGraphic = bgImg;

            // Percentage Display Text
            GameObject valGo = new GameObject("ValueText", typeof(RectTransform), typeof(TextMeshProUGUI));
            valGo.transform.SetParent(container.transform, false);
            var valRt = valGo.GetComponent<RectTransform>();
            valRt.anchorMin = new Vector2(0.5f, 0.5f);
            valRt.anchorMax = new Vector2(0.5f, 0.5f);
            valRt.pivot = new Vector2(0f, 0.5f);
            valRt.anchoredPosition = new Vector2(165f, 0f);
            valRt.sizeDelta = new Vector2(60f, 36f);
            var valTmp = valGo.GetComponent<TextMeshProUGUI>();
            valTmp.text = "100%";
            valTmp.fontSize = 19;
            valTmp.fontStyle = FontStyles.Bold;
            valTmp.alignment = TextAlignmentOptions.MidlineLeft;
            valTmp.color = new Color(1f, 0.85f, 0.3f, 1f);
            valTmp.raycastTarget = false;

            valueTextOut = valTmp;
            return slider;
        }

        private void WireSliderListeners()
        {
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.RemoveAllListeners();
                musicVolumeSlider.onValueChanged.AddListener((val) =>
                {
                    AudioManager.Instance?.SetMusicVolume(val);
                    if (musicVolumeValueText != null)
                    {
                        musicVolumeValueText.text = $"{Mathf.RoundToInt(val * 100)}%";
                    }
                });
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.RemoveAllListeners();
                sfxVolumeSlider.onValueChanged.AddListener((val) =>
                {
                    AudioManager.Instance?.SetSFXVolume(val);
                    if (sfxVolumeValueText != null)
                    {
                        sfxVolumeValueText.text = $"{Mathf.RoundToInt(val * 100)}%";
                    }
                });
            }

            if (testSFXButton != null)
            {
                testSFXButton.onClick.RemoveAllListeners();
                testSFXButton.onClick.AddListener(TestSFXAudio);
            }

            if (fullscreenToggleButton != null)
            {
                fullscreenToggleButton.onClick.RemoveAllListeners();
                fullscreenToggleButton.onClick.AddListener(ToggleFullscreen);
            }

            if (difficultyToggleButton != null)
            {
                difficultyToggleButton.onClick.RemoveAllListeners();
                difficultyToggleButton.onClick.AddListener(OnDifficultyButtonClicked);
            }
        }

        public void OnDifficultyButtonClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CycleDifficulty();
            }
            else
            {
                int cur = PlayerPrefs.GetInt(GameManager.PREFS_DIFFICULTY, (int)GameDifficulty.Normal);
                int next = (cur + 1) % 3;
                PlayerPrefs.SetInt(GameManager.PREFS_DIFFICULTY, next);
                PlayerPrefs.Save();
            }

            UpdateDifficultyButtonVisuals();
            PlayButtonClickSound();
        }

        public void UpdateDifficultyButtonVisuals()
        {
            if (difficultyToggleText != null)
            {
                GameDifficulty diff = GameManager.Instance != null ? GameManager.Instance.CurrentDifficulty : (GameDifficulty)PlayerPrefs.GetInt(GameManager.PREFS_DIFFICULTY, (int)GameDifficulty.Normal);

                string diffFormatted;
                switch (diff)
                {
                    case GameDifficulty.Easy:
                        diffFormatted = "<color=#2ECC71>Easy</color>";
                        break;
                    case GameDifficulty.Hard:
                        diffFormatted = "<color=#E74C3C>Hard</color>";
                        break;
                    default:
                        diffFormatted = "<color=#F1C40F>Normal</color>";
                        break;
                }

                difficultyToggleText.text = $"Difficulty: {diffFormatted}";
            }
        }

        public void TestSFXAudio()
        {
            AudioClip clip = testSFXClip != null ? testSFXClip : (buttonClickSound != null ? buttonClickSound : startChimeSound);
            if (clip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(clip, 1f);
            }
        }

        private bool isEditorSimulatedFullscreen = false;

        public void ToggleFullscreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
            isEditorSimulatedFullscreen = !isEditorSimulatedFullscreen;
            UpdateFullscreenButtonVisuals();
            PlayButtonClickSound();

            if (Application.isEditor)
            {
                HUDController.Instance?.ShowNotification(isEditorSimulatedFullscreen ? "Fullscreen: ON (Simulated in Editor. Maximizes screen in built game/web)" : "Fullscreen: OFF", 2.5f);
            }
        }

        public void UpdateFullscreenButtonVisuals()
        {
            if (fullscreenToggleText != null)
            {
                bool isFull = Application.isEditor ? isEditorSimulatedFullscreen : Screen.fullScreen;
                fullscreenToggleText.text = isFull ? "Fullscreen: <color=#2ECC71>ON</color>" : "Fullscreen: <color=#FF8888>OFF</color>";
            }
        }

        public void EnsureCreditsUI()
        {
            if (creditsContainer != null && creditsText != null) return;

            Transform rootTr = (titleScreenRoot != null) ? titleScreenRoot.transform : transform;

            if (creditsContainer == null)
            {
                Transform found = rootTr.Find("CreditsContainer") ?? rootTr.Find("CreditsPanel");
                if (found != null)
                {
                    creditsContainer = found.gameObject;
                    creditsText = creditsContainer.GetComponentInChildren<TextMeshProUGUI>();
                }
                else
                {
                    creditsContainer = new GameObject("CreditsContainer", typeof(RectTransform));
                    creditsContainer.transform.SetParent(rootTr, false);

                    var containerRt = creditsContainer.GetComponent<RectTransform>();
                    containerRt.anchorMin = new Vector2(0.5f, 0.5f);
                    containerRt.anchorMax = new Vector2(0.5f, 0.5f);
                    containerRt.pivot = new Vector2(0.5f, 0.5f);

                    float targetX = 0f;
                    float targetY = 290f;
                    if (backButton != null && backButton.TryGetComponent<RectTransform>(out var backRt))
                    {
                        targetX = backRt.anchoredPosition.x;
                        targetY = backRt.anchoredPosition.y + 335f;
                    }
                    else if (creditsButton != null && creditsButton.TryGetComponent<RectTransform>(out var crRt))
                    {
                        targetX = crRt.anchoredPosition.x;
                        targetY = crRt.anchoredPosition.y + 290f;
                    }
                    containerRt.anchoredPosition = new Vector2(targetX, targetY);
                    containerRt.sizeDelta = new Vector2(900f, 120f);

                    GameObject textGo = new GameObject("CreditsText", typeof(RectTransform), typeof(TextMeshProUGUI));
                    textGo.transform.SetParent(creditsContainer.transform, false);

                    var textRt = textGo.GetComponent<RectTransform>();
                    textRt.anchorMin = Vector2.zero;
                    textRt.anchorMax = Vector2.one;
                    textRt.offsetMin = Vector2.zero;
                    textRt.offsetMax = Vector2.zero;

                    creditsText = textGo.GetComponent<TextMeshProUGUI>();
                    creditsText.text = "Developed by: Neo Kester";
                    creditsText.fontSize = 48;
                    creditsText.fontStyle = FontStyles.Bold;
                    creditsText.alignment = TextAlignmentOptions.Center;
                    creditsText.color = Color.white;
                    creditsText.raycastTarget = false;
                }
            }

            if (creditsContainer != null) creditsContainer.SetActive(false);
        }

        public void ShowMainMenu() => ShowMainMenu(playAudio: false);
        private void ShowMainMenuWithAudio() => ShowMainMenu(playAudio: true);

        public void ShowMainMenu(bool playAudio)
        {
            if (playAudio) PlayButtonClickSound();

            // Hide Options & Credits Containers
            if (optionsContainer != null) optionsContainer.SetActive(false);
            if (creditsContainer != null) creditsContainer.SetActive(false);

            // Show Main Menu buttons
            SetButtonVisible(newGameButton, true);
            SetButtonVisible(continueGameButton, true);
            SetButtonVisible(optionsButton, true);
            SetButtonVisible(creditsButton, true);

            // Hide Game Mode selection buttons & Back button
            SetButtonVisible(normalModeButton, false);
            SetButtonVisible(blitzModeButton, false);
            SetButtonVisible(backButton, false);

            UpdateContinueButtonState();
        }

        public void ShowModeSelect()
        {
            PlayButtonClickSound();

            // Hide Options & Credits Containers
            if (optionsContainer != null) optionsContainer.SetActive(false);
            if (creditsContainer != null) creditsContainer.SetActive(false);

            // Hide Main Menu buttons
            SetButtonVisible(newGameButton, false);
            SetButtonVisible(continueGameButton, false);
            SetButtonVisible(optionsButton, false);
            SetButtonVisible(creditsButton, false);

            // Show Game Mode selection buttons & Back button
            SetButtonVisible(normalModeButton, true);
            SetButtonVisible(blitzModeButton, true);
            SetButtonVisible(backButton, true);
        }

        public void ShowOptions()
        {
            PlayButtonClickSound();
            EnsureOptionsUI();

            // Hide Credits Container
            if (creditsContainer != null) creditsContainer.SetActive(false);

            // Hide Main Menu buttons
            SetButtonVisible(newGameButton, false);
            SetButtonVisible(continueGameButton, false);
            SetButtonVisible(optionsButton, false);
            SetButtonVisible(creditsButton, false);

            // Hide Game Mode selection buttons
            SetButtonVisible(normalModeButton, false);
            SetButtonVisible(blitzModeButton, false);

            // Show Options & Back Button
            if (optionsContainer != null) optionsContainer.SetActive(true);
            SetButtonVisible(backButton, true);

            // Sync slider positions with AudioManager values
            float curBgm = AudioManager.Instance != null ? AudioManager.Instance.MusicVolume : 0.5f;
            float curSfx = AudioManager.Instance != null ? AudioManager.Instance.SFXVolume : 1.0f;

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.SetValueWithoutNotify(curBgm);
            }
            if (musicVolumeValueText != null)
            {
                musicVolumeValueText.text = $"{Mathf.RoundToInt(curBgm * 100)}%";
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.SetValueWithoutNotify(curSfx);
            }
            if (sfxVolumeValueText != null)
            {
                sfxVolumeValueText.text = $"{Mathf.RoundToInt(curSfx * 100)}%";
            }

            UpdateFullscreenButtonVisuals();
            UpdateDifficultyButtonVisuals();
        }

        public void ShowCredits()
        {
            PlayButtonClickSound();
            EnsureCreditsUI();

            // Refresh position
            if (creditsContainer != null && creditsContainer.TryGetComponent<RectTransform>(out var containerRt))
            {
                float targetX = 0f;
                float targetY = 290f;
                if (backButton != null && backButton.TryGetComponent<RectTransform>(out var backRt))
                {
                    targetX = backRt.anchoredPosition.x;
                    targetY = backRt.anchoredPosition.y + 335f;
                }
                else if (creditsButton != null && creditsButton.TryGetComponent<RectTransform>(out var crRt))
                {
                    targetX = crRt.anchoredPosition.x;
                    targetY = crRt.anchoredPosition.y + 290f;
                }
                containerRt.anchoredPosition = new Vector2(targetX, targetY);
            }

            // Hide Options Container
            if (optionsContainer != null) optionsContainer.SetActive(false);

            // Hide Main Menu buttons
            SetButtonVisible(newGameButton, false);
            SetButtonVisible(continueGameButton, false);
            SetButtonVisible(optionsButton, false);
            SetButtonVisible(creditsButton, false);

            // Hide Game Mode selection buttons
            SetButtonVisible(normalModeButton, false);
            SetButtonVisible(blitzModeButton, false);

            // Show Credits & Back Button
            if (creditsContainer != null) creditsContainer.SetActive(true);
            SetButtonVisible(backButton, true);
        }

        private void SetButtonVisible(Button btn, bool visible)
        {
            if (btn != null && btn.gameObject != null)
            {
                btn.gameObject.SetActive(visible);
            }
        }

        public void StartNormalGame()
        {
            PlayStartSound();
            HideTitleScreen();
            OnGameStarted?.Invoke(GameMode.Normal);
            GameManager.Instance?.StartGame(GameMode.Normal);
        }

        public void StartBlitzGame()
        {
            PlayStartSound();
            HideTitleScreen();
            OnGameStarted?.Invoke(GameMode.Blitz);
            GameManager.Instance?.StartGame(GameMode.Blitz);
        }

        public void ContinueGame()
        {
            PlayStartSound();
            HideTitleScreen();
            GameManager.Instance?.ContinueSavedGame();
        }

        public void BringToFront()
        {
            if (titleScreenRoot != null)
            {
                titleScreenRoot.transform.SetAsLastSibling();
            }
            else
            {
                transform.SetAsLastSibling();
            }
        }

        public void OpenTitleScreen()
        {
            if (titleScreenRoot != null)
            {
                titleScreenRoot.SetActive(true);
            }
            else
            {
                gameObject.SetActive(true);
            }
            BringToFront();
            UpdateContinueButtonState();
            ShowMainMenu(playAudio: false);
        }

        public void HideTitleScreen()
        {
            if (titleScreenRoot != null)
            {
                titleScreenRoot.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void PlayButtonClickSound()
        {
            if (buttonClickSound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(buttonClickSound);
            }
        }

        private void PlayStartSound()
        {
            if (startChimeSound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(startChimeSound);
            }
            else
            {
                PlayButtonClickSound();
            }
        }
    }
}
