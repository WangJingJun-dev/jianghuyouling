using FrameWork.UISystem.UIElements;
using Game.Views.SystemSetting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JianghuYouling
{
    internal static class AISettingsStatusView
    {
        private const string StatusName = "JHYL_Status";

        internal static void Refresh(SettingGroupItem group)
        {
            if (group == null) return;
            string status = StatusFor(group.SubCategory);
            TextMeshProUGUI title = typeof(SettingGroupItem).GetField("title",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.GetValue(group) as TextMeshProUGUI;
            Transform titleContainer = title == null ? null : title.transform.parent;
            Transform existing = group.transform.Find(StatusName);
            if (titleContainer != null)
            {
                Transform nested = titleContainer.Find(StatusName);
                if (nested != null) existing = nested;
            }
            if (string.IsNullOrWhiteSpace(status))
            {
                if (existing != null) existing.gameObject.SetActive(false);
                Debug.Log("[JHYL_AI_STATUS] sub=" + group.SubCategory + " chars=0 visible=False");
                return;
            }
            if (title == null || titleContainer == null) return;
            TextMeshProUGUI text = existing == null ? null : existing.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = Object.Instantiate(title, title.transform.parent, false);
                text.name = StatusName;
                TooltipInvoker tooltip = text.GetComponent<TooltipInvoker>();
                if (tooltip != null) tooltip.enabled = false;
            }
            else if (text.transform.parent != titleContainer)
            {
                text.transform.SetParent(titleContainer, false);
            }
            foreach (CButton clonedButton in text.GetComponentsInChildren<CButton>(true))
            {
                clonedButton.gameObject.SetActive(false);
                Object.Destroy(clonedButton.gameObject);
            }
            LayoutElement layout = text.GetComponent<LayoutElement>();
            if (layout == null) layout = text.gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            text.gameObject.SetActive(true);
            text.text = status;
            text.fontSize = 14f;
            text.alignment = TextAlignmentOptions.Right;
            text.color = new Color(0.68f, 0.72f, 0.66f, 0.94f);
            text.raycastTarget = false;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(420f, 28f);
            float right = -12f;
            CButton reset = typeof(SettingGroupItem).GetField("resetButton",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.GetValue(group) as CButton;
            if (reset != null && reset.gameObject.activeSelf)
                right -= reset.GetComponent<RectTransform>().rect.width + 12f;
            rect.anchoredPosition = new Vector2(right, 0f);
            Debug.Log("[JHYL_AI_STATUS] sub=" + group.SubCategory + " chars=" + text.text.Length
                + " visible=" + text.gameObject.activeInHierarchy
                + " size=" + rect.rect.width + "x" + rect.rect.height);
        }

        private static string StatusFor(ESettingSubCategory subCategory)
        {
            if (subCategory == AISettingsCategorySync.ModelSubCategory) return AISettingsModelPane.TestStatus;
            if (subCategory == AISettingsCategorySync.ExperienceSubCategory) return AISettingsExperiencePane.CurrentStatus;
            if (subCategory == AISettingsCategorySync.VoiceSubCategory) return AISettingsVoicePane.TestStatus;
            if (subCategory == AISettingsCategorySync.ImageSubCategory) return AISettingsImagePane.TestStatus;
            if (subCategory == AISettingsCategorySync.WorldBookSubCategory) return AISettingsWorldBookEditor.CurrentStatus;
            if (subCategory == AISettingsCategorySync.MonthlySubCategory) return AISettingsMonthlyCandidateView.CurrentStatus;
            return null;
        }
    }
}
