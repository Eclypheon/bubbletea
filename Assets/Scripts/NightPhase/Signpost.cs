using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    public class Signpost : MonoBehaviour
    {
        [Header("Scene Object & Trigger")]
        [SerializeField] private Button signpostButton;
        [SerializeField] private Image sceneSignImage;
        [SerializeField] private Sprite signboardSprite;

        [Header("Zoomed Inspection Modal")]
        [SerializeField] private GameObject zoomModalRoot;
        [SerializeField] private RectTransform modalContentTransform;
        [SerializeField] private Image modalSignImage;
        [SerializeField] private TextMeshProUGUI loreTitleText;
        [SerializeField] private TextMeshProUGUI loreBodyText;
        [SerializeField] private Button closeModalButton;
        [SerializeField] private Button backdropCloseButton;

        [Header("Lore Content")]
        [SerializeField] private string defaultTitle = "🎋 Bamboo Grove Notice";
        [TextArea(4, 10)]
        [SerializeField] private string defaultLore = "<i>\"To travelers and tea brewers: The Bamboo Grove is home to timid Baby Yippees nesting beneath the brush.\n\nRustle the grass piles to startle them into the open, and catch them swiftly before they burrow away!\"</i>";

        [Header("Audio")]
        [SerializeField] private AudioClip openSignSound;
        [SerializeField] private AudioClip closeSignSound;

        private Coroutine zoomRoutine;

        private void Awake()
        {
            if (signpostButton == null)
            {
                signpostButton = GetComponent<Button>();
            }

            if (signpostButton != null)
            {
                signpostButton.onClick.AddListener(OpenSignInspection);
            }

            if (closeModalButton != null)
            {
                closeModalButton.onClick.AddListener(CloseSignInspection);
            }

            if (backdropCloseButton != null)
            {
                backdropCloseButton.onClick.AddListener(CloseSignInspection);
            }

            if (zoomModalRoot != null)
            {
                zoomModalRoot.SetActive(false);
            }

            if (sceneSignImage != null && signboardSprite != null)
            {
                sceneSignImage.sprite = signboardSprite;
            }

            if (modalSignImage != null && signboardSprite != null)
            {
                modalSignImage.sprite = signboardSprite;
            }
        }

        public void SetLoreContent(string title, string body)
        {
            if (loreTitleText != null) loreTitleText.text = title;
            if (loreBodyText != null) loreBodyText.text = body;
        }

        public void OpenSignInspection()
        {
            if (zoomModalRoot == null)
            {
                CreateAutoModal();
            }

            if (zoomModalRoot != null)
            {
                zoomModalRoot.SetActive(true);
                zoomModalRoot.transform.SetAsLastSibling();
            }

            if (loreTitleText != null && string.IsNullOrEmpty(loreTitleText.text))
            {
                loreTitleText.text = defaultTitle;
            }
            if (loreBodyText != null && string.IsNullOrEmpty(loreBodyText.text))
            {
                loreBodyText.text = defaultLore;
            }

            PlaySound(openSignSound);

            if (modalContentTransform != null)
            {
                if (zoomRoutine != null) StopCoroutine(zoomRoutine);
                zoomRoutine = StartCoroutine(AnimateZoom(0f, 1f, 0.25f));
            }
        }

        public void CloseSignInspection()
        {
            PlaySound(closeSignSound);

            if (modalContentTransform != null && zoomModalRoot != null && zoomModalRoot.activeSelf)
            {
                if (zoomRoutine != null) StopCoroutine(zoomRoutine);
                zoomRoutine = StartCoroutine(AnimateZoom(1f, 0f, 0.18f, () =>
                {
                    if (zoomModalRoot != null) zoomModalRoot.SetActive(false);
                }));
            }
            else if (zoomModalRoot != null)
            {
                zoomModalRoot.SetActive(false);
            }
        }

        private IEnumerator AnimateZoom(float startScale, float targetScale, float duration, Action onFinished = null)
        {
            if (modalContentTransform == null) yield break;

            float elapsed = 0f;
            modalContentTransform.localScale = Vector3.one * startScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Overshoot bounce when opening
                float curve = (targetScale > startScale)
                    ? Mathf.Sin(t * Mathf.PI * 0.5f) + (Mathf.Sin(t * Mathf.PI) * 0.1f)
                    : Mathf.SmoothStep(startScale, targetScale, t);

                modalContentTransform.localScale = Vector3.one * Mathf.Lerp(startScale, targetScale, curve);
                yield return null;
            }

            modalContentTransform.localScale = Vector3.one * targetScale;
            onFinished?.Invoke();
        }

        private void CreateAutoModal()
        {
            // Automatic modal fallback if not wired in Inspector
            Transform canvasParent = transform.root;
            zoomModalRoot = new GameObject("AutoSignModal", typeof(RectTransform), typeof(Image));
            zoomModalRoot.transform.SetParent(transform.parent != null ? transform.parent : canvasParent, false);

            var rootRt = zoomModalRoot.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            var bgImg = zoomModalRoot.GetComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.70f);

            var backdropBtn = zoomModalRoot.AddComponent<Button>();
            backdropBtn.onClick.AddListener(CloseSignInspection);

            // Modal Card Content
            GameObject cardObj = new GameObject("SignContentCard", typeof(RectTransform), typeof(Image));
            cardObj.transform.SetParent(zoomModalRoot.transform, false);
            modalContentTransform = cardObj.GetComponent<RectTransform>();
            modalContentTransform.anchorMin = new Vector2(0.5f, 0.5f);
            modalContentTransform.anchorMax = new Vector2(0.5f, 0.5f);
            modalContentTransform.pivot = new Vector2(0.5f, 0.5f);
            modalContentTransform.sizeDelta = new Vector2(520f, 360f);

            var cardImg = cardObj.GetComponent<Image>();
            cardImg.color = new Color(0.18f, 0.14f, 0.10f, 0.96f);
            if (signboardSprite != null) cardImg.sprite = signboardSprite;

            // Title
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(cardObj.transform, false);
            var titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot = new Vector2(0.5f, 1);
            titleRt.anchoredPosition = new Vector2(0, -25);
            titleRt.sizeDelta = new Vector2(-40, 45);

            loreTitleText = titleObj.GetComponent<TextMeshProUGUI>();
            loreTitleText.text = defaultTitle;
            loreTitleText.fontSize = 22;
            loreTitleText.alignment = TextAlignmentOptions.Center;
            loreTitleText.color = new Color(1f, 0.88f, 0.55f);

            // Body
            GameObject bodyObj = new GameObject("BodyText", typeof(RectTransform), typeof(TextMeshProUGUI));
            bodyObj.transform.SetParent(cardObj.transform, false);
            var bodyRt = bodyObj.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0, 0);
            bodyRt.anchorMax = new Vector2(1, 1);
            bodyRt.offsetMin = new Vector2(35, 60);
            bodyRt.offsetMax = new Vector2(-35, -75);

            loreBodyText = bodyObj.GetComponent<TextMeshProUGUI>();
            loreBodyText.text = defaultLore;
            loreBodyText.fontSize = 16;
            loreBodyText.alignment = TextAlignmentOptions.TopLeft;
            loreBodyText.color = Color.white;

            // Close Prompt / Button
            GameObject closeObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeObj.transform.SetParent(cardObj.transform, false);
            var closeRt = closeObj.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.5f, 0);
            closeRt.anchorMax = new Vector2(0.5f, 0);
            closeRt.pivot = new Vector2(0.5f, 0);
            closeRt.anchoredPosition = new Vector2(0, 16);
            closeRt.sizeDelta = new Vector2(160, 36);

            var cImg = closeObj.GetComponent<Image>();
            cImg.color = new Color(0.35f, 0.28f, 0.20f, 0.95f);

            GameObject closeTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            closeTextObj.transform.SetParent(closeObj.transform, false);
            var ctRt = closeTextObj.GetComponent<RectTransform>();
            ctRt.anchorMin = Vector2.zero;
            ctRt.anchorMax = Vector2.one;
            ctRt.offsetMin = Vector2.zero;
            ctRt.offsetMax = Vector2.zero;
            var cTmp = closeTextObj.GetComponent<TextMeshProUGUI>();
            cTmp.text = "Tap to Close";
            cTmp.fontSize = 15;
            cTmp.alignment = TextAlignmentOptions.Center;
            cTmp.color = Color.white;

            closeModalButton = closeObj.GetComponent<Button>();
            closeModalButton.onClick.AddListener(CloseSignInspection);
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(clip);
            }
        }
    }
}
