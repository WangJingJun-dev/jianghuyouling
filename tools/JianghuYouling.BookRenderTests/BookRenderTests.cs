using System;
using JianghuYouling.Core.Text;

namespace JianghuYouling.BookRenderTests
{
    /// <summary>
    /// 古书式书稿渲染器的安全与结构回归：正文必须始终作为数据，无法注入脚本、
    /// 事件属性或外部资源；禁用脚本时正文仍完整可读；页面不得引用网络地址。
    /// </summary>
    internal static class BookRenderTests
    {
        private static int _passed;
        private static int _failed;

        public static int Main()
        {
            HostileNovelTextCannotInjectScriptOrMarkup();
            HostileMetadataIsEncodedEverywhere();
            ScriptBlockCannotBeClosedByNovelText();
            OutputDeclaresOfflineContentSecurityPolicy();
            OutputContainsNoNetworkReferences();
            NoscriptFallbackCarriesCompletePlainText();
            ChapterHeadingsAreDetectedDeterministically();
            ParagraphSplittingPreservesEveryCharacter();

            Console.WriteLine("BookRenderTests: passed=" + _passed + " failed=" + _failed);
            return _failed == 0 ? 0 : 1;
        }

        private static void HostileNovelTextCannotInjectScriptOrMarkup()
        {
            string hostile = "他说道：<script>alert(1)</script>不可信。\n"
                + "链接<img src=x onerror=alert(1)>与事件<a href=\"javascript:alert(1)\">点</a>。\n"
                + "正常段落收尾。";
            string html = Render(person: "试剑", paragraphs: new[] { hostile });
            AssertFalse("novel <script> tag never emitted verbatim",
                html.Contains("<script>alert"));
            AssertFalse("novel live <img> tag never emitted",
                html.Contains("<img"));
            AssertFalse("novel live <a> tag never emitted",
                html.Contains("<a href"));
            AssertFalse("novel javascript: url never emitted as live attribute",
                html.Contains("href=\"javascript:"));
            AssertTrue("novel markup appears only encoded",
                html.Contains("&lt;script&gt;alert(1)&lt;/script&gt;")
                && html.Contains("&lt;img src=x onerror=alert(1)&gt;"));
        }

        private static void HostileMetadataIsEncodedEverywhere()
        {
            string html = Render(person: "甲<b>", bookTitle: "书名<c>&", when: "2026\" onmouseover=\"x",
                paragraphs: new[] { "正文。" });
            AssertFalse("person name raw markup never emitted",
                html.Contains("甲<b>"));
            AssertTrue("person name encoded in title bar and colophon",
                html.Contains("甲&lt;b&gt;"));
            AssertFalse("generation time attribute breakout never emitted",
                html.Contains("2026\" onmouseover=\"x"));
        }

        private static void ScriptBlockCannotBeClosedByNovelText()
        {
            string closing = "正文</script><script>alert(2)</script>之后";
            string html = Render(person: "试剑", paragraphs: new[] { closing });
            int firstClose = html.IndexOf("</script>", StringComparison.Ordinal);
            AssertTrue("novel text cannot close the data block early",
                firstClose > 0);
            string dataBlock = html.Substring(0, firstClose);
            AssertFalse("no raw </script> inside data block",
                dataBlock.Contains("</script><script>"));
            AssertTrue("closing tag encoded inside JSON data",
                dataBlock.Contains("\\u003c/script\\u003e"));
        }

        private static void OutputDeclaresOfflineContentSecurityPolicy()
        {
            string html = Render(person: "试剑", paragraphs: new[] { "正文。" });
            AssertTrue("CSP meta forbids default network access",
                html.Contains("Content-Security-Policy")
                && html.Contains("default-src 'none'"));
        }

        private static void OutputContainsNoNetworkReferences()
        {
            string html = Render(person: "试剑", paragraphs: new[] { "正文 http://example.com 与 https://a.b/c。" });
            // 正文中的 URL 只以纯文本/编码形态出现，文档自身不得引用外部资源。
            // SVG 命名空间标识符（xmlns）不是网络请求，先剥除再检查。
            int styleEnd = html.IndexOf("</style>", StringComparison.Ordinal);
            string head = html.Substring(0, styleEnd < 0 ? 0 : styleEnd)
                .Replace("http://www.w3.org/", string.Empty);
            AssertFalse("no external url in styles",
                head.Contains("http://") || head.Contains("https://"));
            AssertFalse("no external url in scripts",
                ScriptSection(html).Contains("http://") || ScriptSection(html).Contains("https://"));
            AssertFalse("no link/import/font-face references",
                html.Contains("@import") || html.Contains("@font-face")
                || html.Contains("<link"));
        }

        private static void NoscriptFallbackCarriesCompletePlainText()
        {
            string[] paragraphs =
            {
                "第一章 起",
                "那日山雨欲来，他在渡口等人。",
                "第二章 承",
                "剑出鞘半寸，又缓缓推了回去。",
            };
            string html = Render(person: "试剑", paragraphs: paragraphs);
            string noscript = Section(html, "<noscript>", "</noscript>");
            AssertTrue("noscript renders cover", noscript.Contains("cover"));
            foreach (string p in paragraphs)
                AssertTrue("noscript keeps paragraph: " + p, noscript.Contains(p));
            AssertTrue("noscript carries colophon with person and time",
                noscript.Contains("人物：试剑") && noscript.Contains("成书："));
        }

        private static void ChapterHeadingsAreDetectedDeterministically()
        {
            AssertTrue("第N章 detected", AncientBookHtmlRenderer.IsChapterHeading("第三章 夜雨"));
            AssertTrue("第N回 detected", AncientBookHtmlRenderer.IsChapterHeading("第十二回 初试锋芒"));
            AssertTrue("bracketed chapter detected",
                AncientBookHtmlRenderer.IsChapterHeading("【卷一 少年游】"));
            AssertTrue("短正文 not chapter",
                !AncientBookHtmlRenderer.IsChapterHeading("他说今日天气好。"));
            AssertFalse("long paragraph not chapter",
                AncientBookHtmlRenderer.IsChapterHeading("第一章 夜雨" + new string('长', 40)));
        }

        private static void ParagraphSplittingPreservesEveryCharacter()
        {
            string content = "甲段第一行。\n甲段第二行。\n\n\n乙段。\n\n\n\n丙段收尾";
            string[] parts = AncientBookHtmlRenderer.SplitParagraphs(content);
            AssertEqual("three paragraphs", 3, parts.Length);
            AssertEqual("lines joined inside paragraph", "甲段第一行。\n甲段第二行。", parts[0]);
            AssertEqual("trailing paragraph kept", "丙段收尾", parts[2]);
        }

        // ---- helpers ----

        private static string Render(string person, string[] paragraphs,
            string bookTitle = null, string when = "2026-10-31 20:00:00")
            => AncientBookHtmlRenderer.RenderBook(new AncientBookHtmlRenderer.BookInfo
            {
                PersonName = person,
                BookTitle = bookTitle,
                GeneratedAt = when,
                Paragraphs = paragraphs,
            });

        private static string ScriptSection(string html)
        {
            int start = html.IndexOf("<script>", StringComparison.Ordinal);
            if (start < 0) return string.Empty;
            int end = html.IndexOf("</script>", start, StringComparison.Ordinal);
            return end < 0 ? string.Empty : html.Substring(start, end - start);
        }

        private static string Section(string html, string open, string close)
        {
            int start = html.IndexOf(open, StringComparison.Ordinal);
            if (start < 0) return string.Empty;
            int end = html.IndexOf(close, start, StringComparison.Ordinal);
            return end < 0 ? string.Empty : html.Substring(start, end - start);
        }

        private static void AssertTrue(string name, bool condition)
        {
            if (condition) { _passed++; return; }
            _failed++;
            Console.Error.WriteLine("FAIL: " + name);
        }

        private static void AssertFalse(string name, bool condition) => AssertTrue(name, !condition);

        private static void AssertEqual<T>(string name, T expected, T actual)
        {
            if (Equals(expected, actual)) { _passed++; return; }
            _failed++;
            Console.Error.WriteLine("FAIL: " + name + " expected=" + expected + " actual=" + actual);
        }
    }
}
