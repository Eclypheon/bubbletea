using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    public class SpeechBubbleUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private TextMeshProUGUI recipeSummaryText;
        [SerializeField] private RectTransform bubbleContainer;

        private Coroutine fadeRoutine;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            HideBubbleInstant();
        }

        public void ShowOrder(DrinkOrder order)
        {
            if (canvasGroup == null) return;
            
            if (dialogueText != null) dialogueText.text = $"\"{order.dialogueText}\"";
            if (recipeSummaryText != null) recipeSummaryText.text = "";

            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeCanvasGroup(0f, 1f, 0.25f));
        }

        public void ShowReaction(string reactionText, int stars)
        {
            string starString = new string('★', stars) + new string('☆', 5 - stars);
            if (dialogueText != null) dialogueText.text = $"\"{reactionText}\"";
            if (recipeSummaryText != null) recipeSummaryText.text = $"<color=#FFD700><size=120%>{starString}</size></color>";

            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeCanvasGroup(0f, 1f, 0.2f));
        }

        public void HideBubble()
        {
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeCanvasGroup(canvasGroup.alpha, 0f, 0.2f));
        }

        public void HideBubbleInstant()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        private IEnumerator FadeCanvasGroup(float start, float target, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, target, elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = target;
            canvasGroup.blocksRaycasts = target > 0.5f;
        }
    }
}
