using System;
using UnityEngine;

namespace BubbleTeaShop
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("State")]
        [SerializeField] private GameState currentState = GameState.MorningPrep;
        public GameState CurrentState => currentState;

        public event Action<GameState> OnStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            StartNewDaySequence();
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
            }
        }

        public void OnShutterClosed()
        {
            if (currentState == GameState.ShopClosing || DayManager.Instance.IsDayFinished)
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
            // Check rent if today is rent day
            int currentDay = DayManager.Instance.CurrentDay;
            if (EconomyManager.Instance.GetDaysUntilRent(currentDay) == 0)
            {
                if (!EconomyManager.Instance.TryPayRent(currentDay))
                {
                    TriggerGameOver("Could not afford weekly rent! The landlord reclaimed the shop.");
                    return;
                }
            }

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
            Debug.Log("🎉 [VICTORY] You bought out the location! You are now the master of Bubble Tea!");
        }
    }
}
