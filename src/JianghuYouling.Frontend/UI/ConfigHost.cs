using UnityEngine;
using TMPro;

namespace JianghuYouling
{
    /// <summary>
    /// 常驻宿主:作为 MOD 窗口的协程载体 + 字体提供者。DontDestroyOnLoad。
    /// (综合配置使用系统设置中的 AI 设置;本 mod 不占用任何游戏快捷键。)
    /// 打开时尽量从场景里现成的 TMP 文本借一个游戏中文字体,避免缺字形。
    /// </summary>
    public sealed class ConfigHost : MonoBehaviour
    {
        public static ConfigHost Instance { get; private set; }

        // 缓存借来的游戏中文字体,避免每次开窗都全场景扫描
        TMP_FontAsset _font;

        void Update()
        {
            // Monthly model work is asynchronous and may still be running after the player
            // begins a trip. Observe the authoritative travel transition every frame so both
            // lanes are cancelled before any later tool dispatch.
            MonthlySettlement.ObserveTravelState();
        }

        public static void Initialize()
        {
            if (Instance != null) return;
            var go = new GameObject("JHYL_ConfigHost");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<ConfigHost>();
        }

        public static void Shutdown()
        {
            var instance = Instance;
            Instance = null;
            if (instance != null) try { Destroy(instance.gameObject); } catch { }
        }

        // 动态 TMP 组件会在 AddComponent 的 Awake 中读取默认字体，必须先完成字体解析与默认字体设置。
        internal TMP_FontAsset ResolveFont()
        {
            if (_font == null)
            {
                try
                {
                    var texts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
                    foreach (var t in texts)
                    {
                        if (t != null && t.font != null && t.gameObject.scene.IsValid())
                        {
                            _font = t.font;
                            break;
                        }
                    }
                    if (_font == null)
                    {
                        foreach (var t in texts)
                            if (t != null && t.font != null) { _font = t.font; break; }
                    }
                    if (_font == null)
                    {
                        var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                        if (fonts != null && fonts.Length > 0) _font = fonts[0];
                    }
                }
                catch { _font = null; }
            }
            ApplyTmpDefaultFont(_font);
            GlyphSanitizer.SetFont(_font);
            return _font;
        }

        static void ApplyTmpDefaultFont(TMP_FontAsset font)
        {
            if (font == null) return;
            try
            {
                var settings = TMP_Settings.instance;
                if (settings == null) return;
                var field = typeof(TMP_Settings).GetField("m_defaultFontAsset",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) field.SetValue(settings, font);
            }
            catch { }
        }
    }
}
