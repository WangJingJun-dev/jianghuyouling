using System;
using Game.Views.SystemSetting;
using JianghuYouling.Core.Web;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JianghuYouling
{
    internal sealed class NativeImageSizeSettingAttribute : SettingItemBaseAttribute
    {
        public override SettingUIType UIType => (SettingUIType)105;

        public NativeImageSizeSettingAttribute(ESettingSubCategory subCategory, int order, LanguageKey languageKey)
            : base(subCategory, order, languageKey)
        {
        }
    }

    internal sealed class NativeImageSizeSettingItem : SettingItemBase
    {
        private NativeImageSizeSettingAttribute _attr;
        private Func<string> _getValueAction;
        private Action<string> _setValueAction;
        private TMP_InputField _widthInput;
        private TMP_InputField _heightInput;
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
            _attr = info.Attribute as NativeImageSizeSettingAttribute;
            if (_attr == null || !(info is SettingItemInfo<string> typedInfo))
            {
                Debug.LogError("[JHYL_AI_SETTINGS] image size initialization failed: incompatible info");
                return;
            }
            _getValueAction = typedInfo.GetValue;
            _setValueAction = typedInfo.SetValue;
            Build();
            SetTypedValue(typedInfo.GetValue());
        }

        private void Build()
        {
            if (_widthInput != null) return;
            _widthInput = CreateInput("JHYL_WidthInput", -208f);
            _heightInput = CreateInput("JHYL_HeightInput", -8f);
            CreateSeparator();
        }

        private TMP_InputField CreateInput(string name, float right)
        {
            var inputGo = new GameObject(name, typeof(RectTransform), typeof(Image));
            inputGo.transform.SetParent(transform, false);
            inputGo.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.07f, 0.72f);
            RectTransform inputRt = inputGo.GetComponent<RectTransform>();
            inputRt.anchorMin = inputRt.anchorMax = inputRt.pivot = new Vector2(1f, 0.5f);
            inputRt.sizeDelta = new Vector2(180f, 36f);
            inputRt.anchoredPosition = new Vector2(right, 0f);
            TMP_InputField input = UiInput.Setup(inputGo, _font, 18f, false,
                TMP_InputField.LineType.SingleLine, false, null, 4, false,
                out TextMeshProUGUI textComp);
            if (_sharedMaterial != null) textComp.fontSharedMaterial = _sharedMaterial;
            Navigation navigation = input.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            input.navigation = navigation;
            input.onEndEdit.AddListener(_ => Commit());
            return input;
        }

        private void CreateSeparator()
        {
            var textGo = new GameObject("JHYL_SizeSeparator", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(transform, false);
            var text = textGo.GetComponent<TextMeshProUGUI>();
            if (_font != null) text.font = _font;
            if (_sharedMaterial != null) text.fontSharedMaterial = _sharedMaterial;
            text.text = "×";
            text.fontSize = 18f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.86f, 0.87f, 0.80f, 1f);
            text.raycastTarget = false;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(20f, 36f);
            rect.anchoredPosition = new Vector2(-198f, 0f);
        }

        private void Commit()
        {
            if (_widthInput == null || _heightInput == null
                || _getValueAction == null || _setValueAction == null) return;
            try
            {
                if (!int.TryParse(_widthInput.text, out int width)
                    || !int.TryParse(_heightInput.text, out int height))
                {
                    SetTypedValue(_getValueAction());
                    return;
                }
                _setValueAction(width + "x" + height);
                SetTypedValue(_getValueAction());
                NotifyChanged();
                Debug.Log("[JHYL_AI_SETTINGS] image size commit ok=true");
            }
            catch (Exception ex)
            {
                Debug.LogError("[JHYL_AI_SETTINGS] image size commit error=" + ex.GetType().Name);
                try { SetTypedValue(_getValueAction()); } catch { }
            }
        }

        internal Selectable[] Focusables()
        {
            if (_widthInput == null || _heightInput == null) return new Selectable[0];
            return new Selectable[] { _widthInput, _heightInput };
        }

        public override object GetValue() => _value;

        public override void SetValue(object value) => SetTypedValue(value as string);

        private void SetTypedValue(string value)
        {
            if (!ImageGenerationClient.TryParseSize(value, out int width, out int height)) return;
            _value = width + "x" + height;
            if (_widthInput != null) _widthInput.SetTextWithoutNotify(width.ToString());
            if (_heightInput != null) _heightInput.SetTextWithoutNotify(height.ToString());
        }

        public override void SetInteractable(bool interactable)
        {
            if (_widthInput != null) _widthInput.interactable = interactable;
            if (_heightInput != null) _heightInput.interactable = interactable;
        }
    }
}
