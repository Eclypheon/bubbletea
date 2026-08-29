using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    public class CupSealer : MonoBehaviour
    {
        [Header("Elements")]
        [SerializeField] private Button sealButton;
        [SerializeField] private RectTransform pressHeadTransform;

        private bool isSealing = false;

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

            bool hasAutoSealer = UpgradeManager.Instance != null && UpgradeManager.Instance.HasUpgrade(UpgradeType.AutoSealer);
            if (hasAutoSealer)
            {
                // Instant seal
                CupStation.Instance?.SealCup();
            }
            else
            {
                StartCoroutine(SealAnimationRoutine());
            }
        }

        private IEnumerator SealAnimationRoutine()
        {
            isSealing = true;
            if (sealButton != null) sealButton.interactable = false;

            // Animate sealer press down
            yield return new WaitForSeconds(0.4f);

            CupStation.Instance?.SealCup();

            yield return new WaitForSeconds(0.2f);
            isSealing = false;
            if (sealButton != null) sealButton.interactable = true;
        }
    }
}
