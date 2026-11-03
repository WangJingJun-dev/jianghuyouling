using System;
using Game.Views.SystemSetting;
using JianghuYouling.Core.Behavior;
using UnityEngine;
using UnityEngine.UI;

namespace JianghuYouling
{
    internal sealed class NativeExperiencePresetSettingAttribute : SettingItemBaseAttribute
    {
        public override SettingUIType UIType => (SettingUIType)103;

        public NativeExperiencePresetSettingAttribute(ESettingSubCategory subCategory, int order, LanguageKey languageKey)
            : base(subCategory, order, languageKey)
        {
        }
    }

    internal sealed class NativeExperiencePresetSettingItem : SettingItemBase
    {
        private const float RowHeight = 380f;
        private int _selectedIndex;
        private Button[] _buttons;
        private Image[] _cards;
        private GameObject[] _selectionBorders;
        private Color[] _backgrounds;
        private Action<sbyte> _setValueAction;
        private bool _interactable = true;

        internal void Configure(TMPro.TextMeshProUGUI label)
        {
            labelText = label;
            if (label != null) label.gameObject.SetActive(false);
        }

        public override void Initialize(ISettingItemInfo info)
        {
            base.Initialize(info);
            if (!(info.Attribute is NativeExperiencePresetSettingAttribute) || !(info is SettingItemInfo<sbyte> typedInfo))
            {
                Debug.LogError("[JHYL_AI_SETTINGS] preset cards initialization failed: incompatible info");
                return;
            }
            _setValueAction = typedInfo.SetValue;
            Build();
            SetValue(typedInfo.GetValue());
        }

        private void Build()
        {
            if (_buttons != null) return;
            LayoutElement layout = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = RowHeight;
            LayoutElement outerLayout = transform.parent.GetComponent<LayoutElement>()
                ?? transform.parent.gameObject.AddComponent<LayoutElement>();
            outerLayout.preferredHeight = RowHeight;
            RectTransform root = transform as RectTransform;
            if (root != null) root.sizeDelta = new Vector2(root.sizeDelta.x, RowHeight);
            _buttons = new Button[3];
            _cards = new Image[3];
            _selectionBorders = new GameObject[3];
            _backgrounds = new Color[3];
            var row = new GameObject("PresetRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(transform, false);
            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = Vector2.zero;
            rowRect.anchorMax = Vector2.one;
            rowRect.offsetMin = Vector2.zero;
            rowRect.offsetMax = Vector2.zero;
            HorizontalLayoutGroup layoutGroup = row.GetComponent<HorizontalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.padding = new RectOffset(0, 0, 18, 18);
            layoutGroup.spacing = 12f;
            for (int i = 0; i < 3; i++) CreateCard(row.transform, i);
            RefreshSelection();
        }

        private void CreateCard(Transform parent, int index)
        {
            string id = index == 0 ? ExperiencePresetPolicy.EconomyId
                : index == 2 ? ExperiencePresetPolicy.BestId : ExperiencePresetPolicy.BalancedId;
            if (!ExperiencePresetPolicy.TryGet(id, out ExperiencePresetDefinition definition)) return;
            string heading = index == 2 ? "★ " + definition.DisplayName : definition.DisplayName;
            string subheading = index == 0 ? "适合控制消耗" : index == 1 ? "兼顾热闹与消耗" : "完整的活江湖";
            Color background = index == 0 ? new Color(0.22f, 0.28f, 0.27f, 0.98f)
                : index == 1 ? new Color(0.26f, 0.34f, 0.32f, 0.98f)
                    : new Color(0.47f, 0.32f, 0.12f, 0.99f);
            ExperiencePresetCardView.Create(parent, "PresetCard_" + index,
                heading, subheading, SettingsText(definition), background, index == 2,
                out Button button, out GameObject selectionBorder);
            button.onClick.AddListener(() => Select(index));
            _buttons[index] = button;
            _cards[index] = button.GetComponent<Image>();
            _selectionBorders[index] = selectionBorder;
            _backgrounds[index] = background;
            button.interactable = _interactable;
        }

        private static string SettingsText(ExperiencePresetDefinition definition)
        {
            string frequency = definition.AssistantFrequency == 0 ? "关闭"
                : definition.AssistantFrequency == 1 ? "低频" : definition.AssistantFrequency == 3 ? "高频" : "中频";
            return "江湖事件：" + (definition.MonthlyEvent ? "开启" : "关闭") + "\n"
                + "同道过月：" + (definition.CompanionMonthly ? "开启" : "关闭") + "\n"
                + "灵儿主动频率：" + frequency + "（需灵儿总开关开启）\n"
                + "群聊自动：" + definition.GroupRounds + " 轮\n"
                + "每月同道：" + definition.CompanionCount + " 人";
        }

        private void Select(int value)
        {
            if (value < 0 || value > 2) return;
            _selectedIndex = value;
            RefreshSelection();
            _setValueAction?.Invoke((sbyte)value);
        }

        private void RefreshSelection()
        {
            if (_cards == null) return;
            for (int i = 0; i < _cards.Length; i++)
            {
                if (_cards[i] != null)
                    _cards[i].color = i == _selectedIndex
                        ? Color.Lerp(_backgrounds[i], Color.white, 0.12f) : _backgrounds[i];
                if (_selectionBorders[i] != null)
                    _selectionBorders[i].SetActive(i == _selectedIndex);
            }
        }

        public override object GetValue() => _selectedIndex;

        public override void SetValue(object value)
        {
            _selectedIndex = Convert.ToSByte(value);
            RefreshSelection();
        }

        public override void SetInteractable(bool interactable)
        {
            _interactable = interactable;
            if (_buttons == null) return;
            foreach (Button button in _buttons)
                if (button != null) button.interactable = interactable;
        }

        internal Selectable[] Focusables() => _buttons ?? new Selectable[0];
    }
}
