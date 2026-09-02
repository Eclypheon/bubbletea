using UnityEngine;

namespace BubbleTeaShop
{
    [ExecuteAlways]
    public class AspectRatioScaler : MonoBehaviour
    {
        private const float TargetAspectRatio = 16f / 9f; // 1.777778f
        private RectTransform rectTransform;
        private int lastWidth = -1;
        private int lastHeight = -1;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            UpdateScale();
        }

        private void Start()
        {
            UpdateScale();
        }

        private void Update()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight)
            {
                UpdateScale();
            }
        }

        public void UpdateScale()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            if (rectTransform == null) return;

            lastWidth = Screen.width;
            lastHeight = Screen.height;

            if (lastWidth <= 0 || lastHeight <= 0) return;

            float currentRatio = (float)lastWidth / (float)lastHeight;

            // If the screen is 16:10 or taller than 16:9 (currentRatio < 1.7778), scale down by delta
            if (currentRatio < TargetAspectRatio)
            {
                float scale = currentRatio / TargetAspectRatio;
                rectTransform.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                rectTransform.localScale = Vector3.one;
            }
        }
    }
}
