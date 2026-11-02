using Game.Views.SystemSetting;

namespace JianghuYouling
{
    internal sealed class NativeActionSettingAttribute : SettingItemBaseAttribute
    {
        public override SettingUIType UIType => (SettingUIType)101;
        public LanguageKey ActionText { get; set; }

        public NativeActionSettingAttribute(ESettingSubCategory subCategory, int order, LanguageKey languageKey)
            : base(subCategory, order, languageKey)
        {
        }
    }
}
