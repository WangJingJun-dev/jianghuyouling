using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JianghuYouling
{
    internal static class ExperiencePresetCardView
    {
        internal static void Create(Transform parent, string name, float left, float right,
            string heading, string subheading, string settings, Color background, bool featured,
            out Button button)
        {
            var card = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(parent, false);
            Anchor(card.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(0, 1),
                new Vector2(left, 78), new Vector2(right, -92));
            Image cardImage = card.GetComponent<Image>();
            cardImage.color = background;
            ChatTab.ApplyRoundedSkin(cardImage);
            button = card.GetComponent<Button>();
            button.targetGraphic = cardImage;

            var headingText = NewText(name + "Heading", card.transform, featured ? 22 : 21, TextAlignmentOptions.Center);
            Anchor(headingText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -54), new Vector2(-8, -14));
            headingText.text = heading;
            headingText.fontStyle = FontStyles.Bold;
            headingText.color = featured ? new Color(1f, 0.91f, 0.55f, 1f) : new Color(0.88f, 0.84f, 0.72f, 1f);

            var sub = NewText(name + "Subheading", card.transform, 16, TextAlignmentOptions.Center);
            Anchor(sub.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -90), new Vector2(-8, -58));
            sub.text = subheading;
            sub.color = featured ? new Color(0.96f, 0.81f, 0.43f, 1f) : new Color(0.65f, 0.71f, 0.66f, 1f);

            var details = NewText(name + "Details", card.transform, 17, TextAlignmentOptions.TopLeft);
            Anchor(details.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(20, 96), new Vector2(-16, -112));
            details.enableWordWrapping = true;
            details.text = settings;
            details.lineSpacing = 18f;
            details.color = new Color(0.88f, 0.87f, 0.80f, 1f);

            var selectText = NewText(name + "Select", card.transform, 17, TextAlignmentOptions.Center);
            Anchor(selectText.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(16, 18), new Vector2(-16, 66));
            selectText.text = featured ? "选择最佳体验" : "选择此档";
            selectText.color = featured ? new Color(1f, 0.97f, 0.78f, 1f) : new Color(0.92f, 0.90f, 0.82f, 1f);
            if (featured) selectText.fontStyle = FontStyles.Bold;
        }

        private static TextMeshProUGUI NewText(string name, Transform parent, float size, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            TMP_FontAsset font = ConfigHost.Instance != null ? ConfigHost.Instance.ResolveFont() : null;
            if (font != null) text.font = font;
            text.fontSize = size;
            text.alignment = align;
            text.raycastTarget = false;
            return text;
        }

        private static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }
    }
}
