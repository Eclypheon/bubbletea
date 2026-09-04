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
        [SerializeField] private Button changelogButton;

        [Header("Game Mode Selection Buttons")]
        [SerializeField] private Button normalModeButton;
        [SerializeField] private Button blitzModeButton;
        [SerializeField] private Button casualModeButton;
        [SerializeField] private Button backButton;

        [Header("Version & Info UI")]
        [Tooltip("Text component displaying the version number on the Title Screen.")]
        [SerializeField] private TextMeshProUGUI versionText;
        [SerializeField] private string gameVersion = "v1.7.0";

        [Header("Options / Audio Settings UI")]
        [Tooltip("Container GameObject for Options sliders & labels. Auto-created if unassigned.")]
        [SerializeField] private GameObject optionsContainer;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TextMeshProUGUI musicVolumeValueText;
        [SerializeField] private TextMeshProUGUI sfxVolumeValueText;
        [SerializeField] private Button fullscreenToggleButton;
        [SerializeField] private TextMeshProUGUI fullscreenToggleText;
        [SerializeField] private Button difficultyToggleButton;
        [SerializeField] private TextMeshProUGUI difficultyToggleText;
        [SerializeField] private Button testSFXButton;
        [SerializeField] private AudioClip testSFXClip;
        [SerializeField] private Button optionsBackButton;

        [Header("Title Screen Audio")]
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip startChimeSound;

        [Header("Credits UI")]
        [Tooltip("Container GameObject for Credits text. Auto-created if unassigned.")]
        [SerializeField] private GameObject creditsContainer;
        [SerializeField] private TextMeshProUGUI creditsText;
        [SerializeField] private Button creditsBackButton;

        [Header("Changelog UI")]
        [Tooltip("Container GameObject for Changelog text. Auto-created if unassigned.")]
        [SerializeField] private GameObject changelogContainer;
        [SerializeField] private TextMeshProUGUI changelogContentText;
        [SerializeField] private Button changelogBackButton;
        [TextArea(10, 30)]
        [SerializeField] private string changelogText;

        public TextMeshProUGUI ChangelogTextComponent => changelogContentText;

        public Button NewGameButton => newGameButton;
        public Button ContinueGameButton => continueGameButton;
        public Button OptionsButton => optionsButton;
        public Button CreditsButton => creditsButton;
        public Button ChangelogButton => changelogButton;
        public Button NormalModeButton => normalModeButton;
        public Button BlitzModeButton => blitzModeButton;
        public Button CasualModeButton => casualModeButton;
        public Button BackButton => backButton;
        public TextMeshProUGUI VersionText => versionText;
        public Slider MusicVolumeSlider => musicVolumeSlider;
        public Slider SfxVolumeSlider => sfxVolumeSlider;
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

            if (string.IsNullOrEmpty(changelogText) || !changelogText.Contains("[v1.7.0]"))
            {
                changelogText = DEFAULT_CHANGELOG_TEXT;
            }

            EnsureReferences();
            InitializeBobbingItems();
            BringToFront();

            // Automatically place buttons in main menu state on initialization
            ShowMainMenu(playAudio: false);
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(changelogText) || !changelogText.Contains("[v1.7.0]"))
            {
                changelogText = DEFAULT_CHANGELOG_TEXT;
            }
            UpdateVersionDisplay();
        }

        private void Start()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Screen.fullScreen = false;
#endif
            WireButtonListeners();
            UpdateContinueButtonState();
            UpdateFullscreenButtonVisuals();
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
            if (changelogButton == null) changelogButton = FindButtonByName("ChangelogButton", "ChangelogBtn", "BtnChangelog", "Changelog", "PatchNotesButton", "UpdatesButton", "PatchNotes");

            if (normalModeButton == null) normalModeButton = FindButtonByName("NormalModeButton", "NormalBtn", "BtnNormal", "Normal");
            if (blitzModeButton == null) blitzModeButton = FindButtonByName("BlitzModeButton", "BlitzBtn", "BtnBlitz", "Blitz");
            if (casualModeButton == null) casualModeButton = FindButtonByName("CasualModeButton", "CasualBtn", "BtnCasual", "Casual", "CasualMode", "Casual Mode");
            if (casualModeButton == null && blitzModeButton != null)
            {
                var go = Instantiate(blitzModeButton.gameObject, blitzModeButton.transform.parent);
                go.name = "Casual Mode";
                if (go.TryGetComponent<RectTransform>(out var rt) && blitzModeButton.TryGetComponent<RectTransform>(out var blitzRt))
                {
                    rt.anchoredPosition = new Vector2(blitzRt.anchoredPosition.x, blitzRt.anchoredPosition.y - 100f);
                }
                var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = "Casual Mode";
                }
                casualModeButton = go.GetComponent<Button>();
            }
            if (backButton == null) backButton = FindButtonByName("BackButton", "BackBtn", "ModeSelectBackButton", "ModeBackButton", "BackToMenuBtn");

            // Auto-discover version text if unassigned
            if (versionText == null)
            {
                Transform found = transform.Find("VersionText") ??
                                  transform.Find("Version") ??
                                  transform.Find("GameVersion") ??
                                  transform.Find("VersionNumber") ??
                                  transform.Find("BuildVersion");
                if (found != null && found.TryGetComponent<TextMeshProUGUI>(out var vt))
                {
                    versionText = vt;
                }
            }
            UpdateVersionDisplay();

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

            if (changelogButton != null)
            {
                changelogButton.onClick.RemoveListener(ShowChangelog);
                changelogButton.onClick.AddListener(ShowChangelog);
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

            if (casualModeButton != null)
            {
                casualModeButton.onClick.RemoveListener(StartCasualGame);
                casualModeButton.onClick.AddListener(StartCasualGame);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(ShowMainMenuWithAudio);
                backButton.onClick.AddListener(ShowMainMenuWithAudio);
            }
        }

        public void UpdateVersionDisplay()
        {
            if (versionText != null)
            {
                versionText.text = !string.IsNullOrEmpty(gameVersion) ? gameVersion : $"v{Application.version}";
            }
        }

        public void UpdateContinueButtonState()
        {
            if (continueGameButton != null)
            {
                bool hasSave = (SaveManager.Instance != null && SaveManager.Instance.HasSave()) ||
                               (GameManager.Instance != null && GameManager.Instance.HasSavedProgress());

                continueGameButton.interactable = hasSave;

                var txt = continueGameButton.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                {
                    if (hasSave)
                    {
                        var data = SaveManager.Instance != null ? SaveManager.Instance.GetCurrentSaveData() : null;
                        int day = (data != null) ? data.currentDay : 1;
                        txt.text = $"Continue (Day {day})";
                        txt.color = new Color(1f, 1f, 1f, 1f);
                    }
                    else
                    {
                        txt.text = "Continue";
                        txt.color = new Color(0.6f, 0.6f, 0.6f, 0.45f);
                    }
                }

                if (continueGameButton.TryGetComponent<CanvasGroup>(out var cg))
                {
                    cg.alpha = hasSave ? 1.0f : 0.45f;
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

        public void EnsureChangelogUI()
        {
            Transform rootTr = (titleScreenRoot != null) ? titleScreenRoot.transform : transform;

            if (changelogContainer == null)
            {
                Transform found = rootTr.Find("ChangelogContainer") ?? rootTr.Find("ChangelogPanel") ?? rootTr.Find("PatchNotesPanel");
                if (found != null)
                {
                    changelogContainer = found.gameObject;
                    changelogContentText = changelogContainer.GetComponentInChildren<TextMeshProUGUI>();
                }
            }

            if (changelogContainer == null)
            {
                changelogContainer = new GameObject("ChangelogContainer", typeof(RectTransform), typeof(Image));
                changelogContainer.transform.SetParent(rootTr, false);

                var bgImg = changelogContainer.GetComponent<Image>();
                bgImg.color = new Color(0.12f, 0.14f, 0.20f, 0.96f);

                // Header Title
                GameObject headerGo = new GameObject("HeaderTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
                headerGo.transform.SetParent(changelogContainer.transform, false);
                var headerRt = headerGo.GetComponent<RectTransform>();
                headerRt.anchorMin = new Vector2(0.5f, 1f);
                headerRt.anchorMax = new Vector2(0.5f, 1f);
                headerRt.pivot = new Vector2(0.5f, 1f);
                headerRt.sizeDelta = new Vector2(800f, 40f);
                headerRt.anchoredPosition = new Vector2(0f, -12f);

                var headerTmp = headerGo.GetComponent<TextMeshProUGUI>();
                headerTmp.text = "<b>CHANGELOG & UPDATES</b>";
                headerTmp.fontSize = 22;
                headerTmp.alignment = TextAlignmentOptions.Center;
                headerTmp.color = new Color(1f, 0.85f, 0.35f, 1f);

                // Scroll View
                GameObject scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
                scrollGo.transform.SetParent(changelogContainer.transform, false);
                var scrollRt = scrollGo.GetComponent<RectTransform>();
                scrollRt.anchorMin = new Vector2(0.02f, 0.03f);
                scrollRt.anchorMax = new Vector2(0.98f, 0.90f);
                scrollRt.offsetMin = Vector2.zero;
                scrollRt.offsetMax = Vector2.zero;

                var scrollImg = scrollGo.GetComponent<Image>();
                scrollImg.color = new Color(0f, 0f, 0f, 0.25f);
                scrollImg.raycastTarget = true;

                var scrollRect = scrollGo.GetComponent<ScrollRect>();
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.scrollSensitivity = 45f;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;

                // Viewport
                GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
                viewportGo.transform.SetParent(scrollGo.transform, false);
                var viewRt = viewportGo.GetComponent<RectTransform>();
                viewRt.anchorMin = Vector2.zero;
                viewRt.anchorMax = Vector2.one;
                viewRt.offsetMin = Vector2.zero;
                viewRt.offsetMax = Vector2.zero;

                var viewImg = viewportGo.GetComponent<Image>();
                viewImg.color = Color.clear;
                viewImg.raycastTarget = true;

                // Content
                GameObject contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                contentGo.transform.SetParent(viewportGo.transform, false);
                var contentRt = contentGo.GetComponent<RectTransform>();
                contentRt.anchorMin = new Vector2(0f, 1f);
                contentRt.anchorMax = new Vector2(1f, 1f);
                contentRt.pivot = new Vector2(0.5f, 1f);
                contentRt.anchoredPosition = Vector2.zero;
                contentRt.sizeDelta = new Vector2(0f, 0f);

                var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
                vlg.childControlHeight = true;
                vlg.childControlWidth = true;
                vlg.childForceExpandHeight = false;
                vlg.childForceExpandWidth = true;
                vlg.padding = new RectOffset(16, 16, 12, 16);

                var csf = contentGo.GetComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

                scrollRect.viewport = viewRt;
                scrollRect.content = contentRt;

                // Text Content
                GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textGo.transform.SetParent(contentGo.transform, false);
                var textRt = textGo.GetComponent<RectTransform>();
                textRt.anchorMin = new Vector2(0f, 1f);
                textRt.anchorMax = new Vector2(1f, 1f);
                textRt.pivot = new Vector2(0.5f, 1f);
                textRt.anchoredPosition = Vector2.zero;
                textRt.sizeDelta = new Vector2(0f, 0f);

                changelogContentText = textGo.GetComponent<TextMeshProUGUI>();
                changelogContentText.fontSize = 15;
                changelogContentText.alignment = TextAlignmentOptions.TopLeft;
                changelogContentText.color = new Color(0.92f, 0.92f, 0.95f, 1f);
                changelogContentText.textWrappingMode = TextWrappingModes.Normal;
                changelogContentText.lineSpacing = 4f;
                changelogContentText.raycastTarget = false;
            }

            // Always ensure container size (+300px height -> 640px) and proper position
            if (changelogContainer != null && changelogContainer.TryGetComponent<RectTransform>(out var containerRt))
            {
                containerRt.anchorMin = new Vector2(0.5f, 0.5f);
                containerRt.anchorMax = new Vector2(0.5f, 0.5f);
                containerRt.pivot = new Vector2(0.5f, 0.5f);

                float targetX = 0f;
                float targetY = 70f;
                if (backButton != null && backButton.TryGetComponent<RectTransform>(out var backRt))
                {
                    targetX = backRt.anchoredPosition.x;
                    targetY = backRt.anchoredPosition.y + 360f;
                }
                containerRt.anchoredPosition = new Vector2(targetX, targetY);
                containerRt.sizeDelta = new Vector2(840f, 640f);
            }

            if (changelogContentText != null)
            {
                changelogContentText.text = GetFormattedChangelogText();
            }

            if (changelogContainer != null) changelogContainer.SetActive(false);
        }

        private const string DEFAULT_CHANGELOG_TEXT =
            "<color=#00E5FF><b>[v1.7.0] - 2026-09-04</b></color>\n\n" +
            "<b>Endless Casual Mode:</b>\n" +
            "• <b>Zen & Cozy Game Mode:</b> Added a dedicated Casual Mode alongside Normal and Blitz for a relaxing, pressure-free bubble tea brewing experience.\n" +
            "• <b>No Patience Pressure:</b> Customers have unlimited patience and the patience bar is hidden—brew each drink at your own relaxed pace.\n" +
            "• <b>Endless Customer Flow:</b> Continuous stream of patrons visiting your shop with immediate queue replenishment and zero day limits.\n" +
            "• <b>Infinite Ingredients & Stock:</b> Unlimited cups, teas, milks, and toppings with no inventory depletion and no financial barrier.\n" +
            "• <b>Gourmet Week 4+ Recipes:</b> Full access to gourmet order distributions including triple-topping recipes, premium milks, and artisanal combinations.\n" +
            "• <b>Peaceful Atmosphere:</b> No rent deadlines, no landlady visits, no mentor interruptions, and no market event swings.\n" +
            "• <b>Calibrated Zen HUD:</b> HUD text and counters updated for casual play, with instant Quit to Title support whenever you're done brewing.\n\n" +
            "<color=#FF66CC><b>[v1.6.0] - 2026-09-03</b></color>\n\n" +
            "<b>Market Events Overhaul & Impactful Economics:</b>\n" +
            "• <b>Impactful Market Events:</b> Made market events more impactful to players with dynamic supply & demand economics, 75% trigger rate, and realistic price swings.\n" +
            "• <b>Progressive 10-Event Pool:</b> Events unlock progressively across weeks (7 in W1, 9 in W2, 10 in W3+) matching available ingredients.\n" +
            "• <b>Wholesale Clearance Sale:</b> Added a 3-day flash clearance offering -70% off all stock at the wholesale market.\n" +
            "• <b>Weather Ice Demands:</b> Summer Heatwave secretly craves 100% Full Ice, while Chilly Monsoon craves 0% No Ice for full tips.\n" +
            "• <b>Tiered Tipping Rebalance:</b> 5-star drinks earn 100% full tips, 4-star drinks (1 mistake or missed weather ice) earn a 50% partial tip, and 3 stars or lower earn $0 tip.\n\n" +
            "<b>Chairwoman Chubi & Sabbatical Vacation Royalties:</b>\n" +
            "• <b>Honorary Chairwoman Lore:</b> Buying out the shop deed ($1,500) transitions weekly Endless Mode payments from rent into Chubi's 'Vacation Royalties'.\n" +
            "• <b>Tsundere Travel Visits:</b> Chubi visits in person between luxury world trips (Paris, Kyoto, Hawaii, Monaco) with comical tsundere dialogues.\n" +
            "• <b>Dynamic HUD Labels:</b> Timers and buttons dynamically switch between 'Rent' (pre-buyout) and 'Royalty' (post-buyout).\n\n" +
            "<color=#00E5FF><b>[v1.5.0] - 2026-09-03</b></color>\n\n" +
            "<b>Title Screen Overhaul & Options:</b>\n" +
            "• <b>Title Screen & Mode Selection:</b> Comprehensive Title Screen featuring New Game, Options, Credits, and Changelog panels, alongside Game Mode selection (Normal vs Blitz Mode).\n" +
            "• <b>Persistent Audio Settings:</b> Added BGM and SFX volume sliders with real-time percentage readouts (0%–100%) and a Test SFX button, persisting across sessions.\n" +
            "• <b>Difficulty Modes:</b> Introduced three calibrated difficulty settings with live economy/patience modifiers (Easy: +20% patience & prices, Normal: 1.0x, Hard: -10% patience & -15% prices).\n" +
            "• <b>Fullscreen Support:</b> Added a toggleable Fullscreen button in the Options menu.\n" +
            "• <b>Credits & Changelog:</b> Dedicated single-panel Credits view and interactive scrollable Changelog viewer.\n" +
            "• <b>Version Display:</b> Customizable version badge (v1.5.0) on Title Screen with Inspector configuration.\n\n" +
            "<b>Cash Register Inventory Upgrades Tab:</b>\n" +
            "• <b>Interactive Tab Switcher:</b> Toggle button in storefront Cash Register modal switching between stock (Items) and shop enhancements (Upgrades).\n" +
            "• <b>Week 2 / Day 8 Unlock Gating:</b> Upgrades tab unlocks on Day 8, remaining cleanly greyed out as 'Upgrades (Day 8)' during Days 1–7.\n" +
            "• <b>2-Column Upgrades Display:</b> Recycled 2-column cards displaying active owned upgrades (ACTIVE badge) alongside locked upgrades (LOCKED badge) with effects and descriptions.\n" +
            "• <b>Double-Toggle & Color Fixes:</b> Debounced button listeners and preserved custom button color palettes.\n\n" +
            "<color=#2ECC71><b>[v1.4.0] - 2026-09-02</b></color>\n\n" +
            "<b>Foraging & Prep Area Audio Immersion:</b>\n" +
            "• <b>Expedition Sound Effects:</b> Integrated custom sound effects for foraging interactions across Bamboo Grove (rustling grass, scurrying Yippees, Yippee catch audio), Honey Meadows (tree kicks and jelly drops hitting the soil), and Mist Mountains (rock wall impacts and Golden Dew catching in the bucket).\n" +
            "• <b>Baby Yippee Looping Scurry SFX:</b> Each active Baby Yippee plays looping movement audio with randomized pitch variation while running across the screen.\n" +
            "• <b>Staggered Spawning Cadence:</b> Flushed Baby Yippees now emerge with a natural 0.1s to 0.7s stagger delay.\n" +
            "• <b>Kitchen Prep Audio:</b> Integrated dedicated SFX for prep equipment processing including Blender blending, Chopping Log slices, and High-Speed Centrifuge spinning.\n\n" +
            "<b>Endless Mode & Milestone Progression:</b>\n" +
            "• <b>Endless Mode Post-Buyout:</b> Added an interactive Endless Mode button to the Victory Modal upon buying out the shop location ($1,500). Players can seamlessly continue playing into indefinite weeks.\n" +
            "• <b>Exponential Rent Escalation:</b> In Endless Mode, weekly rent scales exponentially (+35% compounding per week past Week 4: e.g., $405 in Week 5, $547 in Week 6, $738 in Week 7, $996 in Week 8).\n" +
            "• <b>Landlady Chubi Friendly Morning Visit:</b> On the first morning after activating Endless Mode, Landlady Chubi pays a goodwill visit asking for her favourite drink (Oolong Tea with Fresh Milk, 100% Sugar, Less/Regular Ice, and Tapioca Pearls) for free!\n" +
            "• <b>Multi-Line Dialogue Navigation:</b> Equipped Chubi's visit with an interactive multi-line navigation panel (Next / Got it! / Skip) jumping directly to the recipe line.\n" +
            "• <b>Victory Fanfare & Physics Confetti:</b> Celebrated shop buyout with immediate victory audio playback and an 8-color physics-simulated confetti explosion bursting across the screen.\n\n" +
            "<b>Simplified Star Rating & Tip System:</b>\n" +
            "• <b>Linear 1-Star per Mistake Model:</b> Simplified drink rating to deduct exactly 1 star per mistake (wrong tea base, milk type, sweetness, ice, or missing/extra toppings), capped at 4 deductions (min 1 star).\n" +
            "• <b>Slowness Star Deductions:</b> If patience drops below 20%, 1 star is deducted for every 5% elapsed.\n" +
            "• <b>Calibrated Tips & 90% Patience Threshold:</b> Rebalanced tipping to a 10% base tip on 4-star and 5-star drinks, with an intuitive speed bonus of up to +30% when served above 90% patience.\n" +
            "• <b>Real-Time Order Payout Indicator:</b> Added a centered HUD panel at the bottom of the screen displaying real-time financial payout: Min (30% unhappy payout), Current (live payout with dynamic RGB gradient), and Max (full tip & speed bonus).\n" +
            "• <b>Floating Cash Feedback:</b> Added animated green (+$x.xx) and red (-$x.xx) cash indicators for earnings, tips, and supermarket purchases.\n" +
            "• <b>10-Cent Financial Rounding:</b> Calibrated all prices, tips, payouts, ingredient costs, and ranges to round cleanly to the nearest $0.10.\n\n" +
            "<color=#FFAA00><b>[v1.3.0] - 2026-09-01</b></color>\n\n" +
            "<b>Market Conditions Badge & Event System:</b>\n" +
            "• <b>Market Event HUD Indicator:</b> Added an interactive event badge in the HUD displaying event item icons, trend indicators, and remaining duration.\n" +
            "• <b>Multi-Icon Badge Support:</b> Market event badges dynamically render single or dual icons (e.g. Milk & Ice for Summer Heatwave).\n" +
            "• <b>Dynamic Day Counter Docking:</b> Positional docking where Day counter shifts left and the badge aligns seamlessly alongside it.\n" +
            "• <b>Shutter-Synchronized Visibility:</b> Badges remain concealed behind closed shutters and cleanly appear when the shop opens.\n\n" +
            "<b>Cup Visuals & Multi-Topping Layering:</b>\n" +
            "• <b>Dynamic Multi-Topping Stacking:</b> Cups dynamically instantiate and stack separate visual layers with calibrated vertical spacing when multiple bottom toppings are added.\n" +
            "• <b>Aspect Ratio Preservation:</b> Maintained circular proportions across all topping sprites without vertical squishing.\n" +
            "• <b>Calibrated Cheese Foam Layer:</b> Added support for Cheese Foam sitting across the top rim of the cup with fine-tuned width, positioning, and thickness.\n" +
            "• <b>Week 4 Triple-Topping Orders:</b> Customer orders in Week 4 can now request up to two bottom toppings plus Cheese Foam (3 toppings total).\n\n" +
            "<b>Customer Dismissal Bell Safety:</b>\n" +
            "• <b>Accidental Dismissal Confirmation:</b> Ringing the counter bell while a customer is waiting prompts for confirmation on the first ring and skips only on the second ring.\n\n" +
            "<b>Upgrades & Economy:</b>\n" +
            "• <b>Commercial Auto-Sealer:</b> Added a permanent shop upgrade ($20.00) that automatically seals cups when serving.\n" +
            "• <b>Owned-Items Inventory Filter:</b> Cash Register and Nightly Ledger exclusively display unlocked/purchased items.\n" +
            "• <b>Lowered Buyout Target:</b> Rebalanced final shop buyout goal from $5,000 to $1,500 for a balanced 4-week progression.\n\n" +
            "<color=#3498DB><b>[v1.2.0] - 2026-08-28</b></color>\n\n" +
            "<b>Foraging Expeditions & Kitchen Prep:</b>\n" +
            "• <b>Foraging Locations:</b> Added playable expeditions across Bamboo Grove, Honey Meadows, and Misty Mountains.\n" +
            "• <b>Kitchen Prep Area:</b> Added equipment stations for Blender & Sieve (Popping Boba), Chopping Board (Grass & Coconut Jellies), and High-Speed Centrifuge (Cheese Foam & Golden Honey Pearls).\n" +
            "• <b>Shop Upgrades System:</b> Added permanent upgrades for Storefront Beautification, Advertisements, Supply Contracts, and Lucky Cat charm.\n" +
            "• <b>Mentor Dialogue Skip:</b> Added skip buttons to accelerate through morning briefings.\n\n" +
            "<color=#9B59B6><b>[v1.1.0] - 2026-08-20</b></color>\n\n" +
            "<b>Wholesale Market & Inventory:</b>\n" +
            "• <b>Wholesale Night Market:</b> Buy bulk cups, milks, and raw ingredients during the night phase.\n" +
            "• <b>Inventory Stock Tracking:</b> Full inventory tracking with interactive Cash Register UI inspection.\n" +
            "• <b>Dynamic Market Events:</b> Daily market conditions affecting customer demand and pricing.\n" +
            "• <b>Order Ticket UI:</b> Physical clipped order tickets for customer drink requests.\n\n" +
            "<color=#E67E22><b>[v1.0.0] - 2026-08-10</b></color>\n\n" +
            "<b>Initial Release:</b>\n" +
            "• <b>Core Tea Brewing:</b> Dispensers for tea bases, sweetness, ice sliders, milk layering, topping station, and cup sealing.\n" +
            "• <b>Customer Archetypes:</b> Neurodivergent customer personalities (ADHD, Autism, Anxiety, Tourettes, Dyscalculia, Dyslexia) with unique quirks and patience mechanics.\n" +
            "• <b>Daily Loop:</b> Day shift service, drink rating evaluations, tips, weekly rent cycle, and shop closing ledger.";

        public string GetFormattedChangelogText()
        {
            if (!string.IsNullOrEmpty(changelogText))
            {
                return changelogText;
            }

            return DEFAULT_CHANGELOG_TEXT;
        }

        public void ShowMainMenu() => ShowMainMenu(playAudio: false);
        private void ShowMainMenuWithAudio() => ShowMainMenu(playAudio: true);

        public void ShowMainMenu(bool playAudio)
        {
            if (playAudio) PlayButtonClickSound();

            // Hide Containers
            if (optionsContainer != null) optionsContainer.SetActive(false);
            if (creditsContainer != null) creditsContainer.SetActive(false);
            if (changelogContainer != null) changelogContainer.SetActive(false);

            // Show Main Menu buttons
            SetButtonVisible(newGameButton, true);
            SetButtonVisible(continueGameButton, true);
            SetButtonVisible(optionsButton, true);
            SetButtonVisible(creditsButton, true);
            SetButtonVisible(changelogButton, true);

            // Hide Game Mode selection buttons & Back button
            SetButtonVisible(normalModeButton, false);
            SetButtonVisible(blitzModeButton, false);
            SetButtonVisible(casualModeButton, false);
            SetButtonVisible(backButton, false);

            UpdateContinueButtonState();
        }

        public void ShowModeSelect()
        {
            PlayButtonClickSound();

            // Hide Containers
            if (optionsContainer != null) optionsContainer.SetActive(false);
            if (creditsContainer != null) creditsContainer.SetActive(false);
            if (changelogContainer != null) changelogContainer.SetActive(false);

            // Hide Main Menu buttons
            SetButtonVisible(newGameButton, false);
            SetButtonVisible(continueGameButton, false);
            SetButtonVisible(optionsButton, false);
            SetButtonVisible(creditsButton, false);
            SetButtonVisible(changelogButton, false);

            // Show Game Mode selection buttons & Back button
            SetButtonVisible(normalModeButton, true);
            SetButtonVisible(blitzModeButton, true);
            SetButtonVisible(casualModeButton, true);
            SetButtonVisible(backButton, true);
        }

        public void ShowOptions()
        {
            PlayButtonClickSound();
            EnsureOptionsUI();

            // Hide other containers
            if (creditsContainer != null) creditsContainer.SetActive(false);
            if (changelogContainer != null) changelogContainer.SetActive(false);

            // Hide Main Menu buttons
            SetButtonVisible(newGameButton, false);
            SetButtonVisible(continueGameButton, false);
            SetButtonVisible(optionsButton, false);
            SetButtonVisible(creditsButton, false);
            SetButtonVisible(changelogButton, false);

            // Hide Game Mode selection buttons
            SetButtonVisible(normalModeButton, false);
            SetButtonVisible(blitzModeButton, false);
            SetButtonVisible(casualModeButton, false);

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

            // Hide other containers
            if (optionsContainer != null) optionsContainer.SetActive(false);
            if (changelogContainer != null) changelogContainer.SetActive(false);

            // Hide Main Menu buttons
            SetButtonVisible(newGameButton, false);
            SetButtonVisible(continueGameButton, false);
            SetButtonVisible(optionsButton, false);
            SetButtonVisible(creditsButton, false);
            SetButtonVisible(changelogButton, false);

            // Hide Game Mode selection buttons
            SetButtonVisible(normalModeButton, false);
            SetButtonVisible(blitzModeButton, false);
            SetButtonVisible(casualModeButton, false);

            // Show Credits & Back Button
            if (creditsContainer != null) creditsContainer.SetActive(true);
            SetButtonVisible(backButton, true);
        }

        public void ShowChangelog()
        {
            PlayButtonClickSound();
            EnsureChangelogUI();

            if (changelogContentText != null)
            {
                changelogContentText.text = GetFormattedChangelogText();
            }

            // Hide other containers
            if (optionsContainer != null) optionsContainer.SetActive(false);
            if (creditsContainer != null) creditsContainer.SetActive(false);

            // Hide Main Menu buttons
            SetButtonVisible(newGameButton, false);
            SetButtonVisible(continueGameButton, false);
            SetButtonVisible(optionsButton, false);
            SetButtonVisible(creditsButton, false);
            SetButtonVisible(changelogButton, false);

            // Hide Game Mode selection buttons
            SetButtonVisible(normalModeButton, false);
            SetButtonVisible(blitzModeButton, false);
            SetButtonVisible(casualModeButton, false);

            // Show Changelog & Back Button
            if (changelogContainer != null)
            {
                changelogContainer.transform.SetAsLastSibling();
                changelogContainer.SetActive(true);

                Canvas.ForceUpdateCanvases();
                var sr = changelogContainer.GetComponentInChildren<ScrollRect>();
                if (sr != null)
                {
                    sr.verticalNormalizedPosition = 1f;
                }
            }
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

        public void StartCasualGame()
        {
            PlayStartSound();
            HideTitleScreen();
            OnGameStarted?.Invoke(GameMode.Casual);
            GameManager.Instance?.StartGame(GameMode.Casual);
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
