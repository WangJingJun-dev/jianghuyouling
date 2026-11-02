using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using FrameWork.UISystem.UIElements;
using Game.Views.SystemSetting;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace JianghuYouling
{
    internal static class AISettingsHost
    {
        private const string HolderName = "JHYL_AiSettingsHolder";
        private const string ToggleName = "JHYL_AiSettingsToggle";
        private static SettingCategoryData[] _baseCategories;
        private static int _aiCategoryIndex = -1;
        private static SettingCategoryData _aiCategory;

        private static readonly FieldInfo ScrollContentField = typeof(ViewSystemSetting).GetField(
            "settingScrollContent", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo CategoryGroupField = typeof(ViewSystemSetting).GetField(
            "categoryToggleGroup", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo CurrentCategoryField = typeof(ViewSystemSetting).GetField(
            "_currentCategoryIndex", BindingFlags.Instance | BindingFlags.NonPublic);
        private static ViewSystemSetting _showCallbackWindow;
        private static readonly FieldInfo InfosField = typeof(ViewSystemSetting).GetField(
            "_subCategoryInfos", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo CategoryGroupsField = typeof(ViewSystemSetting).GetField(
            "_categoryGroups", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo CategoriesField = typeof(ViewSystemSetting).GetField("Categories");
        private static readonly FieldInfo PrefabMapField = typeof(ViewSystemSetting).GetField(
            "_prefabMap", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static void Initialize()
        {
            System.Array raw = ViewSystemSetting.Categories;
            if (_baseCategories == null && raw is SettingCategoryData[] typed)
            {
                _baseCategories = new SettingCategoryData[typed.Length];
                System.Array.Copy(typed, _baseCategories, typed.Length);
            }
            LocalStringManager.RegisterGetLanguageCustomHandler(null, Localize);
            EnsureCategories();
        }

        private static string Localize(ushort id)
        {
            string dynamicText = AISettingsModelProfileView.Localize(id);
            if (dynamicText != null) return dynamicText;
            dynamicText = AISettingsWorldBookEditor.Localize(id);
            if (dynamicText != null) return dynamicText;
            dynamicText = AISettingsMonthlyCandidateView.Localize(id);
            if (dynamicText != null) return dynamicText;
            switch ((LanguageKey)id)
            {
                case AISettingsCategorySync.AssistantTitle: return "助手";
                case AISettingsCategorySync.ModelTitle: return "模型";
                case AISettingsCategorySync.ExperienceTitle: return "体验档位";
                case AISettingsCategorySync.VoiceTitle: return "语音";
                case AISettingsCategorySync.ImageTitle: return "生图";
                case AISettingsCategorySync.ConversationTitle: return "对话";
                case AISettingsCategorySync.WorldBookTitle: return "世界书";
                case AISettingsCategorySync.MonthlyTitle: return "过月";
                case AISettingsAssistantPane.EnabledTitle: return "悬浮助手「灵儿」";
                case AISettingsAssistantPane.ProactiveTitle: return "主动消息频率";
                case AISettingsAssistantPane.CommissionTitle: return "委托发送频率";
                case AISettingsAssistantPane.OffText: return "关";
                case AISettingsAssistantPane.LowText: return "低";
                case AISettingsAssistantPane.MidText: return "中";
                case AISettingsAssistantPane.HighText: return "高";
                case AISettingsAssistantPane.NameTitle: return "助手名字";
                case AISettingsAssistantPane.FaceTitle: return "助手形象图";
                case AISettingsAssistantPane.PersonaTitle: return "助手人设";
                case AISettingsConversationPane.DifficultyTitle: return "难度";
                case AISettingsConversationPane.ReplyLengthTitle: return "回复篇幅";
                case AISettingsConversationPane.FontSizeTitle: return "字体大小";
                case AISettingsConversationPane.TaiwuVoiceTitle: return "太吾口吻";
                case AISettingsConversationPane.GhostwriteLengthTitle: return "代笔篇幅";
                case AISettingsConversationPane.GhostwriteLearningTitle: return "代笔自学习";
                case AISettingsConversationPane.StreamTitle: return "流式输出";
                case AISettingsConversationPane.ThinkingTitle: return "显示思量";
                case AISettingsConversationPane.GroupRoundsTitle: return "群聊自动轮数";
                case AISettingsConversationPane.ProactiveEnabledTitle: return "主动名单人物来信";
                case AISettingsConversationPane.ProactiveFrequencyTitle: return "主动来信频率";
                case AISettingsConversationPane.ThinkingPromptTitle: return "思考表达提示词";
                case AISettingsConversationPane.EasyText: return "简单";
                case AISettingsConversationPane.BalancedText: return "均衡";
                case AISettingsConversationPane.HardText: return "困难";
                case AISettingsConversationPane.ShortText: return "简短";
                case AISettingsConversationPane.MediumText: return "适中";
                case AISettingsConversationPane.DetailedText: return "详细";
                case AISettingsConversationPane.UnlimitedText: return "不限";
                case AISettingsConversationPane.SmallText: return "小";
                case AISettingsConversationPane.FontMediumText: return "中";
                case AISettingsConversationPane.LargeText: return "大";
                case AISettingsConversationPane.OneRoundText: return "1 轮";
                case AISettingsConversationPane.TwoRoundsText: return "2 轮";
                case AISettingsConversationPane.ThreeRoundsText: return "3 轮";
                case AISettingsConversationPane.VeryLowText: return "极低";
                case AISettingsMonthlyPane.AiEventTitle: return "每月 AI 江湖事件";
                case AISettingsMonthlyPane.CompanionTitle: return "过月主动行事";
                case AISettingsMonthlyPane.PopupTitle: return "过月自动打开纪事";
                case AISettingsMonthlyPane.CountTitle: return "每月主动人物数量";
                case AISettingsMonthlyPane.CandidatesTitle: return "主动人物名单";
                case AISettingsModelProfileView.SelectTitle: return "模型配置档案";
                case AISettingsModelProfileView.NameTitle: return "新档案名称";
                case AISettingsModelProfileView.SaveTitle: return "保存当前为档案";
                case AISettingsModelProfileView.ActivateTitle: return "立即切换档案";
                case AISettingsModelProfileView.DeleteTitle: return "删除档案";
                case AISettingsModelProfileView.StatusTitle: return "档案状态";
                case AISettingsModelProfileView.SaveActionText: return "保存档案";
                case AISettingsModelProfileView.ActivateActionText: return "立即切换";
                case AISettingsModelProfileView.DeleteActionText: return "删除档案";
                case AISettingsModelProfileView.EmptyText: return "暂无已保存档案";
                case AISettingsModelPane.BaseUrlTitle: return "接口地址 baseUrl";
                case AISettingsModelPane.ApiKeyTitle: return "密钥 apiKey";
                case AISettingsModelPane.ModelTitle: return "主模型 model";
                case AISettingsModelPane.BgModelTitle: return "后台模型 bgModel";
                case AISettingsModelPane.MaxTokensTitle: return "回复上限 max_tokens";
                case AISettingsModelPane.ContextWindowTitle: return "上下文窗口 context_window";
                case AISettingsModelPane.TestTitle: return "接口连通性";
                case AISettingsModelPane.TestActionText: return "测试接口";
                case AISettingsModelPane.TestStatusTitle: return "接口测试状态";
                case AISettingsVoicePane.BaseUrlTitle: return "语音接口";
                case AISettingsVoicePane.ApiKeyTitle: return "语音密钥 apiKey";
                case AISettingsVoicePane.ModelTitle: return "语音资源模型";
                case AISettingsVoicePane.DialogueTitle: return "朗读内容";
                case AISettingsVoicePane.FullText: return "全文";
                case AISettingsVoicePane.DialogueOnlyText: return "仅说话";
                case AISettingsVoicePane.EnabledTitle: return "启用语音功能";
                case AISettingsVoicePane.TestTitle: return "语音接口连通性";
                case AISettingsVoicePane.TestActionText: return "测试接口";
                case AISettingsVoicePane.TestStatusTitle: return "语音接口测试状态";
                case AISettingsImagePane.EnabledTitle: return "启用生图功能";
                case AISettingsImagePane.TestTitle: return "生图接口连通性";
                case AISettingsImagePane.TestActionText: return "测试接口";
                case AISettingsImagePane.TestStatusTitle: return "生图接口测试状态";
                case AISettingsImagePane.ProviderTitle: return "生图协议";
                case AISettingsImagePane.EndpointTitle: return "生图接口 endpoint";
                case AISettingsImagePane.ApiKeyTitle: return "生图密钥 apiKey";
                case AISettingsImagePane.ModelTitle: return "生图模型 model";
                case AISettingsImagePane.SizeTitle: return "输出尺寸";
                case AISettingsImagePane.WorkflowTitle: return "ComfyUI 工作流路径";
                case AISettingsImagePane.StyleTitle: return "附加画面风格";
                case AISettingsImagePane.WatermarkTitle: return "生成图片水印";
                case AISettingsExperiencePane.PresetTitle: return "体验档位";
                case AISettingsExperiencePane.ApplyTitle: return "应用所选档位";
                case AISettingsExperiencePane.ApplyActionText: return "应用档位";
                case AISettingsExperiencePane.SummaryTitle: return "当前体验摘要";
                case AISettingsExperiencePane.EconomyText: return "省钱";
                case AISettingsExperiencePane.BalancedText: return "均衡";
                case AISettingsExperiencePane.BestText: return "最佳体验";
                case AISettingsWorldBookEditor.CategoryFilterTitle: return "世界书分类";
                case AISettingsWorldBookEditor.EntryTitle: return "世界书条目";
                case AISettingsWorldBookEditor.EnabledTitle: return "条目启用";
                case AISettingsWorldBookEditor.ModeTitle: return "触发方式";
                case AISettingsWorldBookEditor.PriorityTitle: return "同层优先级";
                case AISettingsWorldBookEditor.NameTitle: return "条目名称";
                case AISettingsWorldBookEditor.CategoryTitle: return "所属分类";
                case AISettingsWorldBookEditor.KeywordsTitle: return "触发关键词";
                case AISettingsWorldBookEditor.ContentTitle: return "条目正文";
                case AISettingsWorldBookEditor.SaveTitle: return "保存世界书";
                case AISettingsWorldBookEditor.AddTitle: return "添加条目";
                case AISettingsWorldBookEditor.DeleteTitle: return "删除条目";
                case AISettingsWorldBookEditor.ImportPathTitle: return "导入 JSON 路径";
                case AISettingsWorldBookEditor.ImportTitle: return "导入世界书";
                case AISettingsWorldBookEditor.ExportTitle: return "导出世界书";
                case AISettingsWorldBookEditor.OpenExchangeTitle: return "世界书交换目录";
                case AISettingsWorldBookEditor.SaveActionText: return "保存";
                case AISettingsWorldBookEditor.AddActionText: return "添加";
                case AISettingsWorldBookEditor.DeleteActionText: return "删除";
                case AISettingsWorldBookEditor.ImportActionText: return "导入并保存";
                case AISettingsWorldBookEditor.ExportActionText: return "导出";
                case AISettingsWorldBookEditor.OpenExchangeActionText: return "打开目录";
                case AISettingsWorldBookEditor.AlwaysText: return "常驻";
                case AISettingsWorldBookEditor.KeywordText: return "关键词";
                case AISettingsWorldBookEditor.EmptyText: return "暂无条目";
                case AISettingsWorldBookEditor.AllCategoriesText: return "全部";
                case AISettingsMonthlyCandidateView.SelectedTitle: return "主动人物";
                case AISettingsMonthlyCandidateView.ToggleTitle: return "调整主动人物";
                case AISettingsMonthlyCandidateView.ToggleActionText: return "移除 / 重新加入";
                case AISettingsMonthlyCandidateView.EmptyText: return "暂无可显示人物";
                case AISettingsCategorySync.AboutTitle: return "关于";
                case AISettingsAboutPane.OpenSourceTitle: return "开源项目";
                case AISettingsAboutPane.OpenLinkTitle: return "项目地址";
                case AISettingsAboutPane.CopyLinkTitle: return "复制项目地址";
                case AISettingsAboutPane.SupportTitle: return "自愿支持";
                case AISettingsAboutPane.QrTitle: return "支持二维码";
                case AISettingsAboutPane.OpenLinkActionText: return "打开 GitHub";
                case AISettingsAboutPane.CopyLinkActionText: return "复制链接";
                case AISettingsAboutPane.QrCaption: return "爱发电 · 自愿支持";
                case AISettingsAboutPane.QrFallbackCaption: return "二维码载入失败 · 爱发电账号：天选之人srf";
                default: return null;
            }
        }

        private static SettingCategoryData[] EnsureCategories()
        {
            System.Array raw = ViewSystemSetting.Categories;
            bool recovery = AISettingsCategorySync.RequiresRecovery(raw);
            if (recovery && _baseCategories == null)
            {
                Debug.LogError("[JHYL_AI_SETTINGS] incompatible category array and no native baseline: "
                    + (raw == null ? "null" : raw.GetType().FullName));
                return new SettingCategoryData[0];
            }
            SettingCategoryData[] categories = AISettingsCategorySync.Synchronize(
                recovery ? null : raw as SettingCategoryData[], recovery ? _baseCategories : null);
            if (CategoriesField == null) CategoriesField = typeof(ViewSystemSetting).GetField("Categories");
            Debug.Log("[JHYL_AI_SETTINGS] categories prepared=" + categories.Length
                + " field=" + (CategoriesField == null ? "missing" : "ready"));
            if (CategoriesField != null) CategoriesField.SetValue(null, categories);
            Debug.Log("[JHYL_AI_SETTINGS] input=" + (raw == null ? "null" : raw.GetType().FullName)
                + " recovery=" + recovery + " categories="
                + string.Join(">", System.Array.ConvertAll(categories, value => value.Category.ToString())));
            return categories;
        }

        private static void PrepareHolder(RectTransform holder)
        {
            for (int i = holder.childCount - 1; i >= 1; i--)
                UnityEngine.Object.DestroyImmediate(holder.GetChild(i).gameObject);
            if (holder.childCount == 0) return;
            SettingGroupItem group = holder.GetChild(0).GetComponent<SettingGroupItem>();
            if (group == null) return;
            RectTransform template = typeof(SettingGroupItem).GetField("settingItemFrameTemp",
                BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(group) as RectTransform;
            if (template == null) return;
            Transform rows = template.parent;
            for (int rowIndex = 0; rowIndex < rows.childCount; rowIndex++)
            {
                Transform row = rows.GetChild(rowIndex);
                for (int i = row.childCount - 1; i >= 1; i--)
                    UnityEngine.Object.DestroyImmediate(row.GetChild(i).gameObject);
            }
        }

        internal static void EnsureView(ViewSystemSetting window)
        {
            SettingCategoryData[] categories = EnsureCategories();
            int categoryIndex = System.Array.FindIndex(categories,
                category => category.Category == AISettingsCategorySync.AiCategory);
            _aiCategoryIndex = categoryIndex;
            _aiCategory = categoryIndex >= 0 ? categories[categoryIndex] : null;
            RectTransform content = ScrollContentField?.GetValue(window) as RectTransform;
            if (content != null)
            {
                RectTransform holder = content.Find(HolderName) as RectTransform;
                if (holder == null && categoryIndex >= 0 && categoryIndex < content.childCount)
                {
                    RectTransform candidate = content.GetChild(categoryIndex) as RectTransform;
                    if (candidate != null && (candidate.name == HolderName || candidate.name
                        == "Holder_" + (int)AISettingsCategorySync.AiCategory)) holder = candidate;
                }
                if (holder == null)
                {
                    holder = (RectTransform)UnityEngine.Object.Instantiate(content.GetChild(0), content, false);
                    holder.name = HolderName;
                    holder.SetAsLastSibling();
                    holder.gameObject.SetActive(false);
                }
                PrepareHolder(holder);
                for (int i = 0; i < categories.Length && i < content.childCount; i++)
                {
                    Transform child = content.GetChild(i);
                    if (child == null || child is RectTransform) continue;
                    Debug.Log("[JHYL_AI_SETTINGS] add RectTransform to category child=" + child.name);
                    child.gameObject.AddComponent<RectTransform>();
                }
            }
            CToggleGroup group = CategoryGroupField?.GetValue(window) as CToggleGroup;
            if (group != null && categoryIndex >= 0 && FindAiToggle(group) == null)
            {
                CToggle toggle = UnityEngine.Object.Instantiate(group.Get(0), group.transform, false);
                toggle.name = ToggleName;
                toggle.isOn = false;
                SetToggleTitle(toggle);
                group.Add(toggle);
            }
            int currentCategory = CurrentCategoryField?.GetValue(window) is int value ? value : -1;
            if (group != null && (currentCategory < 0 || currentCategory >= categories.Length))
                group.Set(0, true);
            if (!ReferenceEquals(_showCallbackWindow, window))
            {
                _showCallbackWindow = window;
                UIElement element = window.Element;
                element.OnShowed = (Action)Delegate.Combine(element.OnShowed,
                    (Action)(() => SetToggleTitle(FindAiToggle(group))));
            }
            HorizontalLayoutGroup layout = group == null ? null : group.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.childControlWidth = true;
                layout.childForceExpandWidth = true;
            }
            if (group != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(group.transform as RectTransform);
            var prefabMap = PrefabMapField?.GetValue(window) as Dictionary<SettingUIType, SettingItemBase>;
            NativeSettingControlFactory.Register(prefabMap, window);
            EnsureInfos(window);
            Debug.Log("[JHYL_AI_SETTINGS] holders=" + (content == null ? -1 : content.childCount)
                + " toggles=" + (group == null ? -1 : group.Count()) + " aiIndex=" + categoryIndex);
        }

        private static CToggle FindAiToggle(CToggleGroup group)
        {
            if (group == null) return null;
            foreach (CToggle toggle in group.GetAll())
                if (toggle != null && toggle.name == ToggleName) return toggle;
            return null;
        }

        private static void EnsureInfos(ViewSystemSetting window)
        {
            var infos = (InfosField?.GetValue(window)) as Dictionary<ESettingSubCategory, List<ISettingItemInfo>>;
            if (infos == null) return;
            if (!infos.ContainsKey(AISettingsCategorySync.AssistantSubCategory))
                infos.Add(AISettingsCategorySync.AssistantSubCategory, AISettingsAssistantPane.CreateInfos());
            if (!infos.ContainsKey(AISettingsCategorySync.ConversationSubCategory))
                infos.Add(AISettingsCategorySync.ConversationSubCategory, AISettingsConversationPane.CreateInfos());
            if (!infos.ContainsKey(AISettingsCategorySync.VoiceSubCategory))
                infos.Add(AISettingsCategorySync.VoiceSubCategory, AISettingsVoicePane.CreateInfos());
            if (!infos.ContainsKey(AISettingsCategorySync.ImageSubCategory))
                infos.Add(AISettingsCategorySync.ImageSubCategory, AISettingsImagePane.CreateInfos());
            if (!infos.ContainsKey(AISettingsCategorySync.ExperienceSubCategory))
                infos.Add(AISettingsCategorySync.ExperienceSubCategory, AISettingsExperiencePane.CreateInfos());
            if (!infos.ContainsKey(AISettingsCategorySync.AboutSubCategory))
                infos.Add(AISettingsCategorySync.AboutSubCategory, AISettingsAboutPane.CreateInfos());
            infos[AISettingsCategorySync.ModelSubCategory] = AISettingsModelPane.CreateInfos();
            infos[AISettingsCategorySync.WorldBookSubCategory] = AISettingsWorldBookPane.CreateInfos();
            infos[AISettingsCategorySync.MonthlySubCategory] = AISettingsMonthlyPane.CreateInfos();
        }

        private static void RefreshAiGroups(ViewSystemSetting window)
        {
            EnsureInfos(window);
            int categoryIndex = _aiCategoryIndex;
            SettingCategoryData category = _aiCategory;
            var groups = CategoryGroupsField?.GetValue(window) as Dictionary<int, List<SettingGroupItem>>;
            var infos = InfosField?.GetValue(window) as Dictionary<ESettingSubCategory, List<ISettingItemInfo>>;
            var prefabMap = PrefabMapField?.GetValue(window) as Dictionary<SettingUIType, SettingItemBase>;
            if (categoryIndex < 0 || category == null || groups == null || infos == null || prefabMap == null
                || !groups.TryGetValue(categoryIndex, out List<SettingGroupItem> aiGroups)) return;
            foreach (SettingGroupItem group in aiGroups)
            {
                ESettingSubCategory subCategory = group.SubCategory;
                if (!infos.TryGetValue(subCategory, out List<ISettingItemInfo> items) || items == null) continue;
                int subIndex = System.Array.FindIndex(category.SubCategories,
                    value => value.SubCategory == subCategory);
                if (subIndex < 0) continue;
                group.Set(subCategory, category.SubCategories[subIndex].Title, items, prefabMap);
            }
            RefreshToggle(window);
            Debug.Log("[JHYL_AI_SETTINGS] refreshed AI groups=" + aiGroups.Count);
        }

        internal static void QueueRefresh()
        {
            if (ConfigHost.Instance == null) return;
            ConfigHost.Instance.StartCoroutine(RefreshAfterFrame());
        }

        private static IEnumerator RefreshAfterFrame()
        {
            yield return null;
            var window = UIElement.SystemSetting.UiBaseAs<ViewSystemSetting>();
            if (window != null) RefreshAiGroups(window);
        }

        internal static void OpenAssistantSettings()
            => OpenSubCategory(AISettingsCategorySync.AssistantSubCategory);

        internal static void OpenModelSettings()
            => OpenSubCategory(AISettingsCategorySync.ModelSubCategory);

        internal static void OpenVoiceSettings()
            => OpenSubCategory(AISettingsCategorySync.VoiceSubCategory);

        internal static void OpenImageSettings()
            => OpenSubCategory(AISettingsCategorySync.ImageSubCategory);

        private static void OpenSubCategory(ESettingSubCategory subCategory)
        {
            EnsureCategories();
            UIElement.SystemSetting.Show();
            if (ConfigHost.Instance != null)
                ConfigHost.Instance.StartCoroutine(SelectSubCategoryAfterFrames(subCategory));
        }

        private static IEnumerator SelectSubCategoryAfterFrames(ESettingSubCategory subCategory)
        {
            yield return null;
            yield return null;
            var window = UIElement.SystemSetting.UiBaseAs<ViewSystemSetting>();
            if (window == null) yield break;
            int categoryIndex = _aiCategoryIndex;
            if (categoryIndex < 0 || _aiCategory == null) yield break;
            var categoryGroup = CategoryGroupField?.GetValue(window) as CToggleGroup;
            categoryGroup?.Set(categoryIndex, true);
            yield return null;
            SubCategoryInfo[] subCategories = _aiCategory.SubCategories;
            int subIndex = System.Array.FindIndex(subCategories,
                category => category.SubCategory == subCategory);
            if (subIndex < 0) yield break;
            var subGroup = typeof(ViewSystemSetting).GetField("currentSubCategoryToggleGroup",
                BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(window) as CToggleGroup;
            subGroup?.Set(subIndex, true);
            RefreshToggle(window);
            Debug.Log("[JHYL_AI_SETTINGS] open sub=" + subCategory);
        }

        private static void SetToggleTitle(CToggle toggle)
        {
            ToggleStyle style = toggle.GetComponent<ToggleStyle>();
            if (style != null && style.Label != null) style.Label.text = "AI设置";
            Game.Components.Common.CToggleNameAndLineHelper helper =
                toggle.GetComponent<Game.Components.Common.CToggleNameAndLineHelper>();
            if (helper != null) helper.SetName("AI设置");
            foreach (TMPro.TextMeshProUGUI text in toggle.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
                if (text.text == "游戏设置") text.text = "AI设置";
        }

        internal static void RefreshToggle(ViewSystemSetting window)
        {
            CToggleGroup group = CategoryGroupField?.GetValue(window) as CToggleGroup;
            CToggle toggle = FindAiToggle(group);
            if (toggle == null) return;
            SetToggleTitle(toggle);
        }
    }

    [HarmonyPatch(typeof(ViewSystemSetting), "Refresh")]
    internal static class AISettingsRefreshPatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(ViewSystemSetting __instance)
        {
            AISettingsHost.EnsureView(__instance);
        }

        private static void Postfix(ViewSystemSetting __instance)
        {
            AISettingsHost.RefreshToggle(__instance);
        }
    }
}
