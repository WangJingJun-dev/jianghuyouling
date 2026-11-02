using Game.Views.SystemSetting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JianghuYouling
{
    internal sealed class NativeReadonlyTextSettingAttribute : SettingItemBaseAttribute
    {
        public override SettingUIType UIType => (SettingUIType)104;
        public float Height = 520f;

        public NativeReadonlyTextSettingAttribute(ESettingSubCategory subCategory, int order, LanguageKey languageKey)
            : base(subCategory, order, languageKey)
        {
        }
    }

    internal sealed class NativeReadonlyTextSettingItem : SettingItemBase
    {
        private NativeReadonlyTextSettingAttribute _attr;
        private TextMeshProUGUI _text;
        private TMP_FontAsset _font;
        private Material _sharedMaterial;
        private string _value = string.Empty;

        internal void Configure(TextMeshProUGUI label, TMP_FontAsset font, Material sharedMaterial)
        {
            labelText = label;
            _font = font;
            _sharedMaterial = sharedMaterial;
        }

        public override void Initialize(ISettingItemInfo info)
        {
            base.Initialize(info);
            _attr = info.Attribute as NativeReadonlyTextSettingAttribute;
            if (_attr == null || !(info is SettingItemInfo<string> typedInfo))
            {
                Debug.LogError("[JHYL_AI_SETTINGS] readonly text initialization failed: incompatible info");
                return;
            }
            Build();
            SetValue(typedInfo.GetValue());
        }

        private void Build()
        {
            if (_text != null) return;
            float height = Mathf.Max(44f, _attr.Height);
            LayoutElement layout = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            LayoutElement outerLayout = transform.parent.GetComponent<LayoutElement>()
                ?? transform.parent.gameObject.AddComponent<LayoutElement>();
            outerLayout.preferredHeight = height;
            RectTransform root = transform as RectTransform;
            if (root != null) root.sizeDelta = new Vector2(root.sizeDelta.x, height);

            var textGo = new GameObject("JHYL_ReadonlyText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(transform, false);
            _text = textGo.GetComponent<TextMeshProUGUI>();
            if (_font != null) _text.font = _font;
            if (_sharedMaterial != null) _text.fontSharedMaterial = _sharedMaterial;
            _text.fontSize = 18f;
            _text.alignment = TextAlignmentOptions.TopLeft;
            _text.color = new Color(0.86f, 0.87f, 0.80f, 1f);
            _text.raycastTarget = false;
            RectTransform textRt = _text.rectTransform;
            textRt.anchorMin = new Vector2(0.34f, 1f);
            textRt.anchorMax = new Vector2(1f, 1f);
            textRt.pivot = new Vector2(1f, 1f);
            textRt.offsetMin = new Vector2(0f, -height + 8f);
            textRt.offsetMax = new Vector2(-8f, -8f);
            _text.text = _value;
        }

        public override object GetValue() => _value;

        public override void SetValue(object value)
        {
            _value = value as string ?? string.Empty;
            if (_text != null) _text.text = _value;
        }

        public override void SetInteractable(bool interactable)
        {
        }
    }
}
