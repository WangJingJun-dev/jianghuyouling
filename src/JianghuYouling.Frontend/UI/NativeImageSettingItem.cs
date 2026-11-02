using System;
using Game.Views.SystemSetting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JianghuYouling
{
    internal sealed class NativeImageSettingItem : SettingItemBase
    {
        private NativeImageSettingAttribute _attr;
        private Image _image;
        private TMP_FontAsset _font;
        private Material _sharedMaterial;
        private sbyte _value;

        internal void Configure(TextMeshProUGUI label, TMP_FontAsset font, Material sharedMaterial)
        {
            labelText = label;
            _font = font;
            _sharedMaterial = sharedMaterial;
        }

        public override void Initialize(ISettingItemInfo info)
        {
            base.Initialize(info);
            _attr = info.Attribute as NativeImageSettingAttribute;
            if (_attr == null || !(info is SettingItemInfo<sbyte> typedInfo))
            {
                Debug.LogError("[JHYL_AI_SETTINGS] image initialization failed: incompatible info");
                return;
            }
            Build();
            SetValue(typedInfo.GetValue());
        }

        private const float RowHeight = 240f;

        private void Build()
        {
            if (_image != null) return;
            LayoutElement layout = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = RowHeight;
            LayoutElement outerLayout = transform.parent.GetComponent<LayoutElement>()
                ?? transform.parent.gameObject.AddComponent<LayoutElement>();
            outerLayout.preferredHeight = RowHeight;
            RectTransform root = transform as RectTransform;
            if (root != null) root.sizeDelta = new Vector2(root.sizeDelta.x, RowHeight);

            var imageGo = new GameObject("JHYL_Image", typeof(RectTransform), typeof(Image));
            imageGo.transform.SetParent(transform, false);
            RectTransform imageRt = imageGo.GetComponent<RectTransform>();
            imageRt.anchorMin = imageRt.anchorMax = imageRt.pivot = new Vector2(1f, 1f);
            imageRt.sizeDelta = new Vector2(200f, 200f);
            imageRt.anchoredPosition = new Vector2(-8f, -8f);
            _image = imageGo.GetComponent<Image>();
            _image.sprite = _attr.Image;
            _image.preserveAspect = true;
            _image.raycastTarget = false;
            imageGo.SetActive(_attr.Image != null);

            var textGo = new GameObject("Caption", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(transform, false);
            TextMeshProUGUI text = textGo.GetComponent<TextMeshProUGUI>();
            if (_font != null) text.font = _font;
            if (_sharedMaterial != null) text.fontSharedMaterial = _sharedMaterial;
            text.text = _attr.Caption.Tr();
            text.fontSize = 16f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.82f, 0.84f, 0.76f, 1f);
            text.raycastTarget = false;
            RectTransform textRt = text.rectTransform;
            textRt.anchorMin = textRt.anchorMax = textRt.pivot = new Vector2(1f, 1f);
            textRt.sizeDelta = new Vector2(220f, 20f);
            textRt.anchoredPosition = new Vector2(-8f, -212f);
            if (root != null) LayoutRebuilder.MarkLayoutForRebuild(root);
        }

        public override object GetValue() => _value;

        public override void SetValue(object value) => _value = Convert.ToSByte(value);

        public override void SetInteractable(bool interactable)
        {
            if (_image != null) _image.enabled = interactable;
        }
    }
}
