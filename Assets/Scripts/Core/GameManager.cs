using System;
using UnityEngine;

namespace BubbleTeaShop
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private const string PREFS_HAS_SAVE = "BT_HasSavedGame";
        private const string PREFS_SAVED_MODE = "BT_SavedMode";
        public const string PREFS_DIFFICULTY = "BT_GameDifficulty";

        [Header("Game Mode & State")]
        [SerializeField] private GameMode currentGameMode = GameMode.Normal;
        [SerializeField] private GameDifficulty currentDifficulty = GameDifficulty.Normal;
        [SerializeField] private GameState currentState = GameState.MorningPrep;
        [SerializeField] private bool isGameStarted = false;

        [Header("Blitz Mode Timer (60s Day)")]
        [SerializeField] private float blitzTimeRemaining = 60f;
        public const float DefaultBlitzDayDuration = 60f;

        public GameMode CurrentGameMode => currentGameMode;
        public GameDifficulty CurrentDifficulty => currentDifficulty;
        public GameState CurrentState => currentState;
        public bool IsBlitzMode => currentGameMode == GameMode.Blitz;
        public bool IsGameStarted => isGameStarted;
        public float BlitzTimeRemaining => blitzTimeRemaining;

        public float DifficultyPatienceMultiplier
        {
            get
            {
                switch (currentDifficulty)
                {
                    case GameDifficulty.Easy: return 1.20f;
                    case GameDifficulty.Hard: return 0.90f;
                    default: return 1.0f;
                }
            }
        }

        public float DifficultyPriceMultiplier
        {
            get
            {
                switch (currentDifficulty)
                {
                    case GameDifficulty.Easy: return 1.20f;
                    case GameDifficulty.Hard: return 0.85f;
                    default: return 1.0f;
                }
            }
        }

        public event Action<GameState> OnStateChanged;
        public event Action<float> OnBlitzTimerUpdated;
        public event Action<GameDifficulty> OnDifficultyChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            currentDifficulty = (GameDifficulty)PlayerPrefs.GetInt(PREFS_DIFFICULTY, (int)GameDifficulty.Normal);
        }

        public void SetDifficulty(GameDifficulty difficulty)
        {
            currentDifficulty = difficulty;
            PlayerPrefs.SetInt(PREFS_DIFFICULTY, (int)currentDifficulty);
            PlayerPrefs.Save();
            OnDifficultyChanged?.Invoke(currentDifficulty);
        }

        public void CycleDifficulty()
        {
            int next = ((int)currentDifficulty + 1) % 3;
            SetDifficulty((GameDifficulty)next);
        }

        private void Start()
        {
            // Initialize Day 1 in MorningPrep so the storefront & HUD are ready behind the Title Screen
            StartNewDaySequence();

            // If Title Screen is present and active, wait for player mode selection
            if (TitleScreenController.Instance != null && TitleScreenController.Instance.IsTitleScreenActive)
            {
                isGameStarted = false;
                Debug.Log("[GameManager] Storefront initialized in background. Waiting on Title Screen for game start...");
                return;
            }

            // Fallback if testing scene directly without Title Screen
            StartGame(GameMode.Normal);
        }

        private void Update()
        {
            bool isStorefrontRunning = currentState == GameState.ShopOpen ||
                                       currentState == GameState.CustomerWaiting ||
                                       currentState == GameState.DrinkBrewing ||
                                       currentState == GameState.CustomerReacting ||
                                       (ShutterController.Instance != null && ShutterController.Instance.IsOpen);

            if (isGameStarted && IsBlitzMode && isStorefrontRunning)
            {
                blitzTimeRemaining -= Time.deltaTime;
                OnBlitzTimerUpdated?.Invoke(blitzTimeRemaining);
                HUDController.Instance?.UpdateBlitzTimer(blitzTimeRemaining);

                if (blitzTimeRemaining <= 0f)
                {
                    blitzTimeRemaining = 0f;
                    OnBlitzTimerUpdated?.Invoke(0f);
                    HUDController.Instance?.UpdateBlitzTimer(0f);
                    HUDController.Instance?.ShowNotification("Time's Up! The shop is closing for the day.", 3.5f);

                    if (ShutterController.Instance != null && ShutterController.Instance.IsOpen)
                    {
                        ShutterController.Instance.ForceCloseShutter();
                    }
                    else
                    {
                        TransitionToNightPhase();
                    }
                }
            }
        }

        public void StartGame(GameMode mode)
        {
            currentGameMode = mode;
            isGameStarted = true;
            blitzTimeRemaining = DefaultBlitzDayDuration;

            PlayerPrefs.SetInt(PREFS_HAS_SAVE, 1);
            PlayerPrefs.SetInt(PREFS_SAVED_MODE, (int)mode);
            PlayerPrefs.Save();

            if (mode == GameMode.Blitz)
            {
                Debug.Log("[GameManager] Starting BLITZ MODE: 30s timer, infinite ingredients, perfect ambience, endless progression.");
                if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.SetEndlessMode(true);
                }
            }
            else
            {
                Debug.Log("[GameManager] Starting NORMAL MODE: Standard progression.");
            }

            blitzTimeRemaining = DefaultBlitzDayDuration;
            OnBlitzTimerUpdated?.Invoke(blitzTimeRemaining);
            HUDController.Instance?.UpdateBlitzTimer(blitzTimeRemaining);
            HUDController.Instance?.SetStatusHint("Open the shutter to start the day!");
        }

        public bool HasSavedProgress()
        {
            return PlayerPrefs.GetInt(PREFS_HAS_SAVE, 0) == 1;
        }

        public void ContinueSavedGame()
        {
            GameMode savedMode = (GameMode)PlayerPrefs.GetInt(PREFS_SAVED_MODE, (int)GameMode.Normal);
            StartGame(savedMode);
        }

        public void SetState(GameState newState)
        {
            if (currentState == newState) return;
            currentState = newState;
            Debug.Log($"[GameManager] Game State -> {currentState}");
            OnStateChanged?.Invoke(currentState);
        }

        public void StartNewDaySequence()
        {
            blitzTimeRemaining = DefaultBlitzDayDuration;
            OnBlitzTimerUpdated?.Invoke(blitzTimeRemaining);
            HUDController.Instance?.UpdateBlitzTimer(blitzTimeRemaining);

            SetState(GameState.MorningPrep);
            DayManager.Instance?.StartNewDay();
            
            // Check if Pearl Farm upgrade exists -> harvest passive pearls!
            if (UpgradeManager.Instance != null && UpgradeManager.Instance.HasUpgrade(UpgradeType.PearlFarm))
            {
                InventoryManager.Instance?.AddToppingStock(ToppingType.TapiocaPearls, 12);
                Debug.Log("[Upgrade] Pearl Farm harvested 12 fresh Tapioca Pearls!");
            }
        }

        public void OnShutterOpened()
        {
            if (currentState == GameState.MorningPrep || currentState == GameState.ShopClosing)
            {
                SetState(GameState.ShopOpen);

                if (IsBlitzMode)
                {
                    blitzTimeRemaining = DefaultBlitzDayDuration;
                    OnBlitzTimerUpdated?.Invoke(blitzTimeRemaining);
                    HUDController.Instance?.UpdateBlitzTimer(blitzTimeRemaining);

                    // Immediately spawn the first customer in Blitz mode
                    CustomerManager.Instance?.TryCallNextCustomer();
                }
            }
        }

        public void OnShutterClosed()
        {
            if (IsBlitzMode || currentState == GameState.ShopClosing || (DayManager.Instance != null && DayManager.Instance.IsDayFinished))
            {
                TransitionToNightPhase();
            }
        }

        public void TransitionToNightPhase()
        {
            SetState(GameState.NightPhase);
            DayManager.Instance?.CompleteDay();
        }

        public void EndNightAndSleep()
        {
            StartNewDaySequence();
        }

        public void TriggerGameOver(string reason)
        {
            SetState(GameState.GameOver);
            Debug.LogWarning($"[GAME OVER] {reason}");
        }

        public void TriggerGameWon()
        {
            SetState(GameState.GameWon);
            Debug.Log("[VICTORY] You bought out the location! You are now the master of Bubble Tea!");
        }
    }
}
