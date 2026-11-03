using System.Collections.Generic;
using System.IO;
using FrameWork;
using FrameWork.UISystem.UIElements;
using UnityEngine;
using UnityEngine.UI;

namespace JianghuYouling
{
    internal static class RoundActionIconFactory
    {
        internal const string VoiceSprite = "JHYL_Voice_Clean";
        internal const string ImageSprite = "JHYL_Image_Landscape";
        internal const string RetrySprite = "JHYL_Retry_Clean";
        internal const string DeleteSprite = "JHYL_Delete_Clean";
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        internal static GameObject Create(Transform parent, string name, string spriteName,
            string tooltip, out CButton button)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(CImage), typeof(CButton), typeof(TooltipInvoker), typeof(LayoutElement));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(28f, 28f);
            LayoutElement layout = go.GetComponent<LayoutElement>();
            layout.minWidth = layout.preferredWidth = 28f;
            layout.minHeight = layout.preferredHeight = 28f;

            CImage image = go.GetComponent<CImage>();
            image.AutoSize = false;
            image.preserveAspect = true;
            image.raycastTarget = true;
            image.color = Color.white;
            Sprite normalized = LoadSprite(spriteName);
            if (normalized != null) image.sprite = normalized;
            else image.SetSprite(spriteName);
            if (image.sprite == null)
                Debug.LogWarning("[JHYL_ROUND_ACTION] missing icon=" + spriteName);

            button = go.GetComponent<CButton>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.84f, 0.86f, 0.80f, 0.92f);
            colors.highlightedColor = new Color(0.98f, 0.83f, 0.52f, 1f);
            colors.pressedColor = new Color(0.75f, 0.67f, 0.45f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.50f, 0.53f, 0.49f, 0.35f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            TooltipInvoker invoker = go.GetComponent<TooltipInvoker>();
            invoker.Type = TipType.Simple;
            invoker.IsLanguageKey = false;
            invoker.NeedRefresh = false;
            invoker.ShowOnLeft = false;
            invoker.ShowOnTop = true;
            invoker.PresetParam = new[] { tooltip ?? string.Empty, string.Empty };
            invoker.triggerByChildRaycast = true;
            return go;
        }

        private static Sprite LoadSprite(string sourceName)
        {
            string resource;
            if (sourceName == VoiceSprite) resource = "voice";
            else if (sourceName == ImageSprite) resource = "image";
            else if (sourceName == RetrySprite) resource = "retry";
            else if (sourceName == DeleteSprite) resource = "delete";
            else return null;
            if (Cache.TryGetValue(resource, out Sprite cached)) return cached;
            try
            {
                string name = "JianghuYouling.Frontend.Assets.RoundActions." + resource + ".png";
                using (Stream stream = typeof(RoundActionIconFactory).Assembly.GetManifestResourceStream(name))
                {
                    if (stream == null) return null;
                    byte[] bytes = new byte[stream.Length];
                    int offset = 0;
                    while (offset < bytes.Length)
                    {
                        int read = stream.Read(bytes, offset, bytes.Length - offset);
                        if (read <= 0) return null;
                        offset += read;
                    }
                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!ImageConversion.LoadImage(texture, bytes)) return null;
                    texture.hideFlags = HideFlags.HideAndDontSave;
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 128, 128),
                        new Vector2(0.5f, 0.5f), 100f);
                    sprite.hideFlags = HideFlags.HideAndDontSave;
                    Cache[resource] = sprite;
                    return sprite;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
