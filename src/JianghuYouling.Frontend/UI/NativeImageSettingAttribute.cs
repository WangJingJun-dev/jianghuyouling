using Game.Views.SystemSetting;
using UnityEngine;

namespace JianghuYouling
{
    internal sealed class NativeImageSettingAttribute : SettingItemBaseAttribute
    {
        public override SettingUIType UIType => (SettingUIType)102;
        public Sprite Image { get; set; }
        public LanguageKey Caption { get; set; }

        public NativeImageSettingAttribute(ESettingSubCategory subCategory, int order, LanguageKey languageKey)
            : base(subCategory, order, languageKey)
        {
        }
    }
}
