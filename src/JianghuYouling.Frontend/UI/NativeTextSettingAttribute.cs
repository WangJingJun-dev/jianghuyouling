using Game.Views.SystemSetting;

namespace JianghuYouling
{
    internal sealed class NativeTextSettingAttribute : SettingItemBaseAttribute
    {
        public override SettingUIType UIType => (SettingUIType)100;
        public bool Multiline { get; set; }
        public bool Password { get; set; }
        public int CharacterLimit { get; set; }
        public bool ReadOnly { get; set; }

        public NativeTextSettingAttribute(ESettingSubCategory subCategory, int order, LanguageKey languageKey)
            : base(subCategory, order, languageKey)
        {
        }
    }
}
