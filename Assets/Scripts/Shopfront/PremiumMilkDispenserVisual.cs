using UnityEngine;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    public class PremiumMilkDispenserVisual : MonoBehaviour
    {
        [Tooltip("Optional: If assigned to a child or external object, will toggle SetActive on it. If empty or assigned to this GameObject, will toggle Image/Graphic components so the script stays active.")]
        [SerializeField] private GameObject visualRoot;

        private void Start()
        {
            UpdateVisibility();
        }

        private void OnEnable()
        {
            UpdateVisibility();
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnInventoryUpdated += UpdateVisibility;
            }
            if (DayManager.Instance != null)
            {
                DayManager.Instance.OnDayStarted += HandleDayStarted;
            }
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDisable()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnInventoryUpdated -= UpdateVisibility;
            }
            if (DayManager.Instance != null)
            {
                DayManager.Instance.OnDayStarted -= HandleDayStarted;
            }
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleDayStarted(int day) => UpdateVisibility();
        private void HandleStateChanged(GameState state) => UpdateVisibility();

        public void UpdateVisibility()
        {
            bool isUnlocked = (InventoryManager.Instance != null && InventoryManager.Instance.HasPremiumMilkDispenser) 
                           || (DayManager.Instance != null && DayManager.Instance.CurrentDay >= 3);

            if (visualRoot != null && visualRoot != gameObject)
            {
                visualRoot.SetActive(isUnlocked);
            }
            else
            {
                // Toggle Image and UI Graphics without deactivating the GameObject itself,
                // ensuring the script and its event listeners remain active and functional.
                var graphics = GetComponentsInChildren<Graphic>(true);
                foreach (var g in graphics)
                {
                    g.enabled = isUnlocked;
                }

                var renderers = GetComponentsInChildren<CanvasRenderer>(true);
                foreach (var r in renderers)
                {
                    r.cull = !isUnlocked;
                }

                if (TryGetComponent<CanvasGroup>(out var cg))
                {
                    cg.alpha = isUnlocked ? 1f : 0f;
                    cg.interactable = isUnlocked;
                    cg.blocksRaycasts = isUnlocked;
                }
            }
        }
    }
}
