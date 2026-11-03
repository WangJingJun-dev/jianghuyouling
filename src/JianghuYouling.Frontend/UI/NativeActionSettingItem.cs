using System;
using Game.Views.SystemSetting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JianghuYouling
{
    internal sealed class NativeActionSettingItem : SettingItemBase
    {
        private NativeActionSettingAttribute _attr;
        private Func<sbyte> _getValueAction;
        private Action<sbyte> _setValueAction;
        private Button _button;
        private Button _secondaryButton;
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
            _attr = info.Attribute as NativeActionSettingAttribute;
            if (_attr == null || !(info is SettingItemInfo<sbyte> typedInfo))
            {
                Debug.LogError("[JHYL_AI_SETTINGS] action initialization failed: incompatible info");
                return;
            }
            _getValueAction = typedInfo.GetValue;
            _setValueAction = typedInfo.SetValue;
            Build();
            SetValue(typedInfo.GetValue());
        }

        private void Build()
        {
            if (_button != null) return;
            Debug.Log("[JHYL_AI_SETTINGS] action-build sub=" + _attr.SubCategory
                + " font=" + (_font == null ? "null" : _font.name));
            float primaryRight = _attr.SecondaryAction == null ? -8f : -196f;
            _button = CreateButton("JHYL_Action", _attr.ActionText, primaryRight, InvokeAction);
            if (_attr.SecondaryAction != null)
                _secondaryButton = CreateButton("JHYL_SecondaryAction", _attr.SecondaryActionText,
                    -8f, InvokeSecondaryAction);
        }

        private Button CreateButton(string name, LanguageKey actionText, float right,
            UnityEngine.Events.UnityAction handler)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(transform, false);
            go.GetComponent<Image>().color = new Color(0.25f, 0.34f, 0.30f, 0.96f);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(180f, 36f);
            rt.anchoredPosition = new Vector2(right, 0f);
            Button button = go.GetComponent<Button>();
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            button.navigation = navigation;
            button.onClick.AddListener(handler);

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.GetComponent<TextMeshProUGUI>();
            if (_font != null) text.font = _font;
            if (_sharedMaterial != null) text.fontSharedMaterial = _sharedMaterial;
            text.text = actionText.Tr();
            text.fontSize = 17f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.92f, 0.90f, 0.82f, 1f);
            text.raycastTarget = false;
            RectTransform textRt = text.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            return button;
        }

        private void InvokeAction()
        {
            if (_setValueAction == null) return;
            try
            {
                _setValueAction(1);
                _value = 0;
                NotifyChanged();
                Debug.Log("[JHYL_AI_SETTINGS] action sub=" + _attr.SubCategory + " invoked=true");
            }
            catch (Exception ex)
            {
                Debug.LogError("[JHYL_AI_SETTINGS] action sub=" + _attr.SubCategory
                    + " error=" + ex.GetType().Name);
            }
        }

        private void InvokeSecondaryAction()
        {
            if (_attr.SecondaryAction == null) return;
            try
            {
                _attr.SecondaryAction.Invoke();
                NotifyChanged();
                Debug.Log("[JHYL_AI_SETTINGS] action sub=" + _attr.SubCategory + " secondaryInvoked=true");
            }
            catch (Exception ex)
            {
                Debug.LogError("[JHYL_AI_SETTINGS] secondary action sub=" + _attr.SubCategory
                    + " error=" + ex.GetType().Name);
            }
        }

        internal Selectable[] Focusables()
        {
            if (_button == null) return new Selectable[0];
            return _secondaryButton == null
                ? new Selectable[] { _button }
                : new Selectable[] { _button, _secondaryButton };
        }

        public override object GetValue() => _value;

        public override void SetValue(object value)
        {
            _value = Convert.ToSByte(value);
            if (_getValueAction != null) _value = _getValueAction();
        }

        public override void SetInteractable(bool interactable)
        {
            if (_button != null) _button.interactable = interactable;
            if (_secondaryButton != null) _secondaryButton.interactable = interactable;
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(InvokeAction);
            if (_secondaryButton != null) _secondaryButton.onClick.RemoveListener(InvokeSecondaryAction);
        }
    }
}
