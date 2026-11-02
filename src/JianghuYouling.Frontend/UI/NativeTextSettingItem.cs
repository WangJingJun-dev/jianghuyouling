using System;
using Game.Views.SystemSetting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JianghuYouling
{
    internal sealed class NativeTextSettingItem : SettingItemBase
    {
        private NativeTextSettingAttribute _attr;
        private Func<string> _getValueAction;
        private Action<string> _setValueAction;
        private TMP_InputField _input;
        private Button _saveButton;
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
            _attr = info.Attribute as NativeTextSettingAttribute;
            if (_attr == null || !(info is SettingItemInfo<string> typedInfo))
            {
                Debug.LogError("[JHYL_AI_SETTINGS] text initialization failed: incompatible info");
                return;
            }
            _getValueAction = typedInfo.GetValue;
            _setValueAction = typedInfo.SetValue;
            Build();
            SetTypedValue(typedInfo.GetValue());
        }

        private void Build()
        {
            if (_input != null) return;
            Debug.Log("[JHYL_AI_SETTINGS] text-build sub=" + _attr.SubCategory
                + " font=" + (_font == null ? "null" : _font.name));
            var inputGo = new GameObject("JHYL_TextInput", typeof(RectTransform), typeof(Image));
            inputGo.transform.SetParent(transform, false);
            inputGo.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.07f, 0.72f);
            _input = inputGo.GetComponent<TMP_InputField>();
            RectTransform inputRt = inputGo.GetComponent<RectTransform>();
            LayoutElement layout = null;
            LayoutElement outerLayout = null;
            if (_attr.Multiline)
            {
                inputRt.anchorMin = new Vector2(0.34f, 1f);
                inputRt.anchorMax = new Vector2(1f, 1f);
                inputRt.pivot = new Vector2(1f, 1f);
                inputRt.offsetMin = new Vector2(0f, -256f);
                inputRt.offsetMax = new Vector2(-8f, -8f);
                layout = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
                layout.preferredHeight = 320f;
                outerLayout = transform.parent.GetComponent<LayoutElement>()
                    ?? transform.parent.gameObject.AddComponent<LayoutElement>();
                outerLayout.preferredHeight = layout.preferredHeight;
            }
            else
            {
                inputRt.anchorMin = inputRt.anchorMax = inputRt.pivot = new Vector2(1f, 0.5f);
                inputRt.sizeDelta = new Vector2(420f, 36f);
                inputRt.anchoredPosition = new Vector2(-8f, 0f);
            }

            _input = UiInput.Setup(inputGo, _font, 18f, _attr.Multiline,
                _attr.Multiline ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine,
                _attr.Password, null, Math.Max(0, _attr.CharacterLimit), _attr.Multiline,
                out TextMeshProUGUI textComp);
            if (_sharedMaterial != null) textComp.fontSharedMaterial = _sharedMaterial;
            _input.readOnly = _attr.ReadOnly;
            Navigation navigation = _input.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            _input.navigation = navigation;
            if (_attr.Multiline)
            {
                _saveButton = CreateSaveButton();
                _saveButton.onClick.AddListener(Commit);
                RectTransform saveRt = _saveButton.GetComponent<RectTransform>();
                saveRt.anchorMin = saveRt.anchorMax = saveRt.pivot = new Vector2(1f, 1f);
                saveRt.anchoredPosition = new Vector2(-8f, -272f);
                NativeSettingResizeHandle.Create(transform, inputRt, layout, outerLayout, _input, saveRt, _font, _sharedMaterial);
            }
            else
            {
                _input.onEndEdit.AddListener(_ => Commit());
            }
        }

        private Button CreateSaveButton()
        {
            var go = new GameObject("JHYL_SaveText", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(transform, false);
            go.GetComponent<Image>().color = new Color(0.25f, 0.34f, 0.30f, 0.96f);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(92f, 30f);
            rt.anchoredPosition = new Vector2(-8f, -140f);
            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.GetComponent<TextMeshProUGUI>();
            if (_font != null) text.font = _font;
            if (_sharedMaterial != null) text.fontSharedMaterial = _sharedMaterial;
            text.text = "保存";
            text.fontSize = 17f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.92f, 0.90f, 0.82f, 1f);
            text.raycastTarget = false;
            RectTransform textRt = text.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            return go.GetComponent<Button>();
        }

        private void Commit()
        {
            if (_input == null || _getValueAction == null || _setValueAction == null) return;
            try
            {
                _setValueAction(_input.text ?? string.Empty);
                SetTypedValue(_getValueAction());
                NotifyChanged();
                Debug.Log("[JHYL_AI_SETTINGS] text commit sub=" + _attr.SubCategory + " ok=true");
            }
            catch (Exception ex)
            {
                Debug.LogError("[JHYL_AI_SETTINGS] text commit sub=" + _attr.SubCategory
                    + " error=" + ex.GetType().Name);
                try { SetTypedValue(_getValueAction()); } catch { }
            }
        }

        internal Selectable[] Focusables()
        {
            var controls = new System.Collections.Generic.List<Selectable>();
            if (_input != null) controls.Add(_input);
            if (_input != null && _input.verticalScrollbar != null) controls.Add(_input.verticalScrollbar);
            if (_saveButton != null) controls.Add(_saveButton);
            return controls.ToArray();
        }

        public override object GetValue() => _value;

        public override void SetValue(object value) => SetTypedValue(value as string);

        public void SetTypedValue(string value)
        {
            _value = value ?? string.Empty;
            if (_input != null) _input.SetTextWithoutNotify(_value);
        }

        public override void SetInteractable(bool interactable)
        {
            if (_input != null) _input.interactable = interactable && !_attr.ReadOnly;
            if (_saveButton != null) _saveButton.interactable = interactable;
        }

        private void OnDestroy()
        {
            if (_saveButton != null) _saveButton.onClick.RemoveListener(Commit);
        }
    }

    internal sealed class NativeSettingResizeHandle : MonoBehaviour, UnityEngine.EventSystems.IDragHandler
    {
        private RectTransform _row;
        private RectTransform _inputRt;
        private RectTransform _saveRt;
        private LayoutElement _layout;
        private LayoutElement _outerLayout;
        private TMP_InputField _input;

        internal static void Create(Transform parent, RectTransform inputRt, LayoutElement layout,
            LayoutElement outerLayout, TMP_InputField input, RectTransform saveRt, TMP_FontAsset font, Material sharedMaterial)
        {
            var go = new GameObject("JHYL_ResizeInput", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(inputRt.transform, false);
            go.GetComponent<Image>().color = new Color(0.32f, 0.40f, 0.35f, 0.90f);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
            rt.sizeDelta = new Vector2(18f, 18f);
            rt.anchoredPosition = new Vector2(-3f, 3f);
            NativeSettingResizeHandle handle = go.AddComponent<NativeSettingResizeHandle>();
            handle._row = parent as RectTransform;
            handle._inputRt = inputRt;
            handle._saveRt = saveRt;
            handle._layout = layout;
            handle._outerLayout = outerLayout;
            handle._input = input;

            var textGo = new GameObject("Grip", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            TextMeshProUGUI text = textGo.GetComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            if (sharedMaterial != null) text.fontSharedMaterial = sharedMaterial;
            text.text = "//";
            text.fontSize = 12f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.86f, 0.84f, 0.75f, 1f);
            text.raycastTarget = false;
            RectTransform textRt = text.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
        }

        public void OnDrag(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_row == null || _inputRt == null || _layout == null || _input == null || !_input.interactable) return;
            Canvas canvas = _row.GetComponentInParent<Canvas>();
            float scale = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
            Vector2 delta = eventData.delta / scale;
            _layout.preferredHeight = Mathf.Clamp(_layout.preferredHeight - delta.y, 220f, 720f);
            if (_outerLayout != null) _outerLayout.preferredHeight = _layout.preferredHeight;
            Vector2 offsetMin = _inputRt.offsetMin;
            float minOffset = 8f - _row.rect.width * _inputRt.anchorMin.x;
            offsetMin.x = Mathf.Clamp(offsetMin.x + delta.x, minOffset, 0f);
            offsetMin.y = 64f - _layout.preferredHeight;
            _inputRt.offsetMin = offsetMin;
            if (_saveRt != null) _saveRt.anchoredPosition = new Vector2(-8f, offsetMin.y - 16f);
            LayoutRebuilder.MarkLayoutForRebuild(_row);
        }
    }
}
