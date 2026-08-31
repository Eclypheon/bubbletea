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

        [Header("Visual Glow")]
        [SerializeField] private bool enablePulsingGlow = true;
        [SerializeField] private Image glowAuraImage;

        private Coroutine zoomRoutine;
        private Coroutine pulseRoutine;
        private Vector3 baseScale = Vector3.one;

        private void Awake()
        {
            if (signpostButton == null)
            {
                signpostButton = GetComponent<Button>();
            }

            if (sceneSignImage == null)
            {
                sceneSignImage = GetComponent<Image>();
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

            baseScale = transform.localScale;
            CreateGlowAura();
        }

        private void OnEnable()
        {
            if (enablePulsingGlow)
            {
                if (pulseRoutine != null) StopCoroutine(pulseRoutine);
                pulseRoutine = StartCoroutine(SignPulseGlowRoutine());
            }
        }

        private void OnDisable()
        {
            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
                pulseRoutine = null;
            }
            transform.localScale = baseScale;
        }

        private void CreateGlowAura()
        {
            if (glowAuraImage != null) return;

            GameObject auraObj = new GameObject("SignGlowAura", typeof(RectTransform), typeof(Image));
            auraObj.transform.SetParent(transform, false);
            auraObj.transform.SetAsFirstSibling();

            var rt = auraObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(400f, 700f);
            rt.anchoredPosition = Vector2.zero;

            glowAuraImage = auraObj.GetComponent<Image>();
            glowAuraImage.color = new Color(1f, 0.92f, 0.5f, 0.25f);
            glowAuraImage.preserveAspect = true;
            glowAuraImage.raycastTarget = false;
            if (signboardSprite != null) glowAuraImage.sprite = signboardSprite;
        }

        private IEnumerator SignPulseGlowRoutine()
        {
            // Keep the signpost itself completely stationary and static
            transform.localScale = baseScale;

            while (enabled)
            {
                float t = Time.time * 2.2f;
                float pulse = (Mathf.Sin(t) + 1f) * 0.2f; // 0 to 1

                // Only pulse the surrounding outer glow aura
                if (glowAuraImage != null)
                {
                    float alpha = Mathf.Lerp(0.12f, 0.42f, pulse);
                    glowAuraImage.color = new Color(1f, 0.90f, 0.45f, alpha);
                    glowAuraImage.transform.localScale = Vector3.one * Mathf.Lerp(1.04f, 1.16f, pulse);
                }

                yield return null;
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
            Transform canvasParent = transform.root;
            zoomModalRoot = new GameObject("AutoSignModal", typeof(RectTransform), typeof(Image));
            zoomModalRoot.transform.SetParent(transform.parent != null ? transform.parent : canvasParent, false);

            var rootRt = zoomModalRoot.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            var bgImg = zoomModalRoot.GetComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.75f);

            var backdropBtn = zoomModalRoot.AddComponent<Button>();
            backdropBtn.onClick.AddListener(CloseSignInspection);

            // 2x Larger Modal Card Content (1020 x 680) with clean wooden notice board background
            GameObject cardObj = new GameObject("SignContentCard", typeof(RectTransform), typeof(Image));
            cardObj.transform.SetParent(zoomModalRoot.transform, false);
            modalContentTransform = cardObj.GetComponent<RectTransform>();
            modalContentTransform.anchorMin = new Vector2(0.5f, 0.5f);
            modalContentTransform.anchorMax = new Vector2(0.5f, 0.5f);
            modalContentTransform.pivot = new Vector2(0.5f, 0.5f);
            modalContentTransform.sizeDelta = new Vector2(1020f, 680f);

            var cardImg = cardObj.GetComponent<Image>();
            cardImg.color = new Color(0.14f, 0.11f, 0.08f, 0.97f); // Clean wooden parchment backing (no squished stretching)

            // Inner Decorative Border
            GameObject borderObj = new GameObject("InnerBorder", typeof(RectTransform), typeof(Image));
            borderObj.transform.SetParent(cardObj.transform, false);
            var borderRt = borderObj.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(14, 14);
            borderRt.offsetMax = new Vector2(-14, -14);
            var borderImg = borderObj.GetComponent<Image>();
            borderImg.color = new Color(0.32f, 0.24f, 0.15f, 0.85f);
            borderImg.raycastTarget = false;

            // Inner Content Panel
            GameObject innerObj = new GameObject("InnerPanel", typeof(RectTransform), typeof(Image));
            innerObj.transform.SetParent(borderObj.transform, false);
            var innerRt = innerObj.GetComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(6, 6);
            innerRt.offsetMax = new Vector2(-6, -6);
            var innerImg = innerObj.GetComponent<Image>();
            innerImg.color = new Color(0.18f, 0.14f, 0.10f, 0.98f);
            innerImg.raycastTarget = false;

            // Optional Signboard Graphic with preserved aspect ratio
            if (signboardSprite != null)
            {
                GameObject signGraphic = new GameObject("SignboardIcon", typeof(RectTransform), typeof(Image));
                signGraphic.transform.SetParent(innerObj.transform, false);
                var sgRt = signGraphic.GetComponent<RectTransform>();
                sgRt.anchorMin = new Vector2(0, 1);
                sgRt.anchorMax = new Vector2(0, 1);
                sgRt.pivot = new Vector2(0, 1);
                sgRt.anchoredPosition = new Vector2(25, -20);
                sgRt.sizeDelta = new Vector2(90, 110);
                var sgImg = signGraphic.GetComponent<Image>();
                sgImg.sprite = signboardSprite;
                sgImg.preserveAspect = true;
                sgImg.raycastTarget = false;
            }

            // 2x Larger Title
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(cardObj.transform, false);
            var titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot = new Vector2(0.5f, 1);
            titleRt.anchoredPosition = new Vector2(0, -45);
            titleRt.sizeDelta = new Vector2(-80, 75);

            loreTitleText = titleObj.GetComponent<TextMeshProUGUI>();
            loreTitleText.text = defaultTitle;
            loreTitleText.fontSize = 40;
            loreTitleText.alignment = TextAlignmentOptions.Center;
            loreTitleText.color = new Color(1f, 0.88f, 0.55f);

            // 2x Larger Body Text with Word Wrapping
            GameObject bodyObj = new GameObject("BodyText", typeof(RectTransform), typeof(TextMeshProUGUI));
            bodyObj.transform.SetParent(cardObj.transform, false);
            var bodyRt = bodyObj.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0, 0);
            bodyRt.anchorMax = new Vector2(1, 1);
            bodyRt.offsetMin = new Vector2(70, 130);
            bodyRt.offsetMax = new Vector2(-70, -140);

            loreBodyText = bodyObj.GetComponent<TextMeshProUGUI>();
            loreBodyText.text = defaultLore;
            loreBodyText.fontSize = 28;
            loreBodyText.lineSpacing = 16;
            loreBodyText.alignment = TextAlignmentOptions.TopLeft;
            loreBodyText.color = new Color(0.96f, 0.96f, 0.94f, 1f);

            // 2x Larger Close Button
            GameObject closeObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeObj.transform.SetParent(cardObj.transform, false);
            var closeRt = closeObj.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.5f, 0);
            closeRt.anchorMax = new Vector2(0.5f, 0);
            closeRt.pivot = new Vector2(0.5f, 0);
            closeRt.anchoredPosition = new Vector2(0, 35);
            closeRt.sizeDelta = new Vector2(280, 64);

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
            cTmp.fontSize = 26;
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
