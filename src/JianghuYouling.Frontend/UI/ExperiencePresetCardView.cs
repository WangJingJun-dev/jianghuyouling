using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JianghuYouling
{
    internal static class ExperiencePresetCardView
    {
        internal static void Create(Transform parent, string name,
            string heading, string subheading, string settings, Color background, bool featured,
            out Button button, out GameObject selectionBorder)
        {
            var card = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button),
                typeof(LayoutElement));
            card.transform.SetParent(parent, false);
            LayoutElement cardLayout = card.GetComponent<LayoutElement>();
            cardLayout.minWidth = 0f;
            cardLayout.preferredWidth = 260f;
            cardLayout.preferredHeight = 344f;
            Image cardImage = card.GetComponent<Image>();
            cardImage.color = background;
            ChatTab.ApplyRoundedSkin(cardImage);
            button = card.GetComponent<Button>();
            button.targetGraphic = cardImage;

            var headingText = NewText(name + "Heading", card.transform, featured ? 22 : 21, TextAlignmentOptions.Center);
            Anchor(headingText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -44), new Vector2(-8, -10));
            headingText.text = heading;
            headingText.fontStyle = FontStyles.Bold;
            headingText.color = featured ? new Color(1f, 0.91f, 0.55f, 1f) : new Color(0.88f, 0.84f, 0.72f, 1f);

            var sub = NewText(name + "Subheading", card.transform, 16, TextAlignmentOptions.Center);
            Anchor(sub.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -70), new Vector2(-8, -48));
            sub.text = subheading;
            sub.color = featured ? new Color(0.96f, 0.81f, 0.43f, 1f) : new Color(0.65f, 0.71f, 0.66f, 1f);

            var details = NewText(name + "Details", card.transform, 17, TextAlignmentOptions.TopLeft);
            Anchor(details.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(16, 48), new Vector2(-12, -84));
            details.enableWordWrapping = true;
            details.text = settings;
            details.lineSpacing = 8f;
            details.color = new Color(0.88f, 0.87f, 0.80f, 1f);

            var selectText = NewText(name + "Select", card.transform, 17, TextAlignmentOptions.Center);
            Anchor(selectText.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(16, 10), new Vector2(-16, 40));
            selectText.text = featured ? "选择最佳体验" : "选择此档";
            selectText.color = featured ? new Color(1f, 0.97f, 0.78f, 1f) : new Color(0.92f, 0.90f, 0.82f, 1f);
            if (featured) selectText.fontStyle = FontStyles.Bold;

            selectionBorder = new GameObject("SelectionBorder", typeof(RectTransform));
            selectionBorder.transform.SetParent(card.transform, false);
            Anchor(selectionBorder.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Color borderColor = new Color(0.95f, 0.76f, 0.34f, 1f);
            CreateEdge(selectionBorder.transform, "Top", new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -2), new Vector2(0, 0), borderColor);
            CreateEdge(selectionBorder.transform, "Bottom", Vector2.zero, new Vector2(1, 0),
                Vector2.zero, new Vector2(0, 2), borderColor);
            CreateEdge(selectionBorder.transform, "Left", Vector2.zero, new Vector2(0, 1),
                Vector2.zero, new Vector2(2, 0), borderColor);
            CreateEdge(selectionBorder.transform, "Right", new Vector2(1, 0), Vector2.one,
                new Vector2(-2, 0), Vector2.zero, borderColor);
            selectionBorder.SetActive(false);
        }

        private static void CreateEdge(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var edge = new GameObject(name, typeof(RectTransform), typeof(Image));
            edge.transform.SetParent(parent, false);
            Image image = edge.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            Anchor(edge.GetComponent<RectTransform>(), anchorMin, anchorMax, offsetMin, offsetMax);
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
