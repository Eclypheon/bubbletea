using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    public class CupSealer : MonoBehaviour
    {
        public static CupSealer Instance { get; private set; }

        [Header("Elements")]
        [SerializeField] private Button sealButton;
        [SerializeField] private RectTransform pressHeadTransform;

        private bool isSealing = false;
        private Coroutine highlightRoutine;

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
            if (sealButton != null)
            {
                sealButton.onClick.AddListener(TriggerSeal);
            }
        }

        public void TriggerSeal()
        {
            if (isSealing) return;

            if (CupStation.Instance == null || !CupStation.Instance.CurrentCup.hasCup)
            {
                HUDController.Instance?.ShowNotification("No cup placed on the counter!");
                return;
            }

            if (CupStation.Instance.CurrentCup.isSealed)
            {
                HUDController.Instance?.ShowNotification("Cup is already sealed!");
                return;
            }

            if (CupStation.Instance.CurrentCup.tea == TeaBase.None && CupStation.Instance.CurrentCup.toppings.Count == 0)
            {
                HUDController.Instance?.ShowNotification("Cannot seal an empty cup! Add tea or toppings first.");
                return;
            }

            bool hasAutoSealer = UpgradeManager.Instance != null && UpgradeManager.Instance.HasUpgrade(UpgradeType.AutoSealer);
            if (hasAutoSealer)
            {
                // Instant seal
                CupStation.Instance?.SealCup();
                HUDController.Instance?.ShowNotification("🧋 Cup sealed! Ready to serve.", 1.5f);
            }
            else
            {
                StartCoroutine(SealAnimationRoutine());
            }
        }

        public void HighlightSealer()
        {
            if (highlightRoutine != null) StopCoroutine(highlightRoutine);
            highlightRoutine = StartCoroutine(HighlightPulseRoutine());
        }

        private IEnumerator HighlightPulseRoutine()
        {
            Transform target = (pressHeadTransform != null) ? pressHeadTransform : transform;
            Vector3 originalScale = Vector3.one;
            Vector3 bigScale = new Vector3(1.2f, 1.2f, 1f);

            for (int i = 0; i < 2; i++)
            {
                float elapsed = 0f;
                while (elapsed < 0.1f)
                {
                    elapsed += Time.deltaTime;
                    target.localScale = Vector3.Lerp(originalScale, bigScale, elapsed / 0.1f);
                    yield return null;
                }
                elapsed = 0f;
                while (elapsed < 0.1f)
                {
                    elapsed += Time.deltaTime;
                    target.localScale = Vector3.Lerp(bigScale, originalScale, elapsed / 0.1f);
                    yield return null;
                }
            }

            target.localScale = originalScale;
        }

        private IEnumerator SealAnimationRoutine()
        {
            isSealing = true;
            if (sealButton != null) sealButton.interactable = false;

            // Animate sealer press down
            yield return new WaitForSeconds(0.35f);

            CupStation.Instance?.SealCup();
            HUDController.Instance?.ShowNotification("🧋 Cup sealed! Ready to serve.", 1.5f);

            yield return new WaitForSeconds(0.15f);
            isSealing = false;
            if (sealButton != null) sealButton.interactable = true;
        }
    }
}
