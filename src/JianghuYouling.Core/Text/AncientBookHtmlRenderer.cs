using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace JianghuYouling.Core.Text
{
    /// <summary>
    /// 「往事成书」的古书式书稿渲染器：把人物小说正文渲染成一个完全离线、单文件、
    /// 无网络请求的古书式 HTML。正文一律作为数据处理（HTML 编码 / JSON 转义后由脚本
    /// 以 textContent 落页），模型返回的 HTML、CSS、JavaScript、事件属性与外部资源
    /// 均无法进入文档可执行上下文。样式、脚本与纹理全部内嵌，不引用外部字体或 CDN。
    /// </summary>
    public static class AncientBookHtmlRenderer
    {
        public const int MaxBookChars = 2 * 1024 * 1024;

        public sealed class BookInfo
        {
            public string PersonName;
            public string BookTitle;
            public string GeneratedAt;
            public string SourceNote;
            public string[] Paragraphs;
        }

        /// <summary>把小说正文切成排版段落；空行分段，段首段尾空白裁掉。</summary>
        public static string[] SplitParagraphs(string content)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(content)) return result.ToArray();
            string[] lines = content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var sb = new StringBuilder();
            foreach (string raw in lines)
            {
                string line = (raw ?? string.Empty).Trim();
                if (line.Length == 0)
                {
                    Flush(sb, result);
                    continue;
                }
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(line);
            }
            Flush(sb, result);
            return result.ToArray();
        }

        static void Flush(StringBuilder sb, List<string> result)
        {
            if (sb.Length == 0) return;
            result.Add(sb.ToString());
            sb.Length = 0;
        }

        /// <summary>保守的 HTML 文本编码：&lt; &gt; &amp; &quot; &#39; 之外不动。</summary>
        public static string HtmlEncode(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var sb = new StringBuilder(value.Length + 16);
            foreach (char c in value)
            {
                switch (c)
                {
                    case '<': sb.Append("&lt;"); break;
                    case '>': sb.Append("&gt;"); break;
                    case '&': sb.Append("&amp;"); break;
                    case '"': sb.Append("&quot;"); break;
                    case '\'': sb.Append("&#39;"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }

        public static string RenderBook(BookInfo info)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            string person = string.IsNullOrWhiteSpace(info.PersonName) ? "无名氏" : info.PersonName.Trim();
            string title = string.IsNullOrWhiteSpace(info.BookTitle)
                ? person + "传" : info.BookTitle.Trim();
            string when = string.IsNullOrWhiteSpace(info.GeneratedAt) ? "时间未详" : info.GeneratedAt.Trim();
            string source = string.IsNullOrWhiteSpace(info.SourceNote)
                ? "太吾与此人的单聊、其人实际参与的群聊、长期记忆与本体生平经历辑录而成。"
                : info.SourceNote.Trim();
            string[] paragraphs = info.Paragraphs ?? new string[0];

            var head = new StringBuilder(64 * 1024);
            head.Append("<!DOCTYPE html>\n<html lang=\"zh-CN\">\n<head>\n<meta charset=\"utf-8\">\n");
            head.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n");
            head.Append("<meta http-equiv=\"Content-Security-Policy\" content=\"default-src 'none'; img-src data:; style-src 'unsafe-inline'; script-src 'unsafe-inline'\">\n");
            head.Append("<title>").Append(HtmlEncode(title)).Append(" · 往事成书</title>\n");
            head.Append("<style>").Append(Styles).Append("</style>\n</head>\n<body>\n");

            var body = new StringBuilder(Math.Min(MaxBookChars, paragraphs.Length * 24 + 8192));
            body.Append("<div class=\"desk\">\n");
            body.Append("<div class=\"reader-bar\">");
            body.Append("<span class=\"bar-title\">").Append(HtmlEncode(title)).Append("</span>");
            body.Append("<span class=\"bar-controls\">");
            body.Append("<button type=\"button\" id=\"toggle-orient\" title=\"切换横排 / 竖排\">竖排</button>");
            body.Append("<button type=\"button\" id=\"toggle-anim\" title=\"开启 / 关闭翻页动效\">动效·开</button>");
            body.Append("</span></div>\n");

            body.Append("<div class=\"stage\" id=\"stage\" aria-live=\"polite\"></div>\n");

            body.Append("<div class=\"pagenav\">");
            body.Append("<button type=\"button\" id=\"prev-page\">上一页</button>");
            body.Append("<span class=\"page-indicator\"><input id=\"page-jump\" inputmode=\"numeric\" aria-label=\"页码\">");
            body.Append("<span id=\"page-total\"></span></span>");
            body.Append("<button type=\"button\" id=\"next-page\">下一页</button>");
            body.Append("</div>\n");

            // 模型正文只经 JSON 编码进入 script 块；&lt; 等转义使脚本块不可能被正文提前闭合。
            string json = JsonConvert.SerializeObject(new
            {
                title = title,
                person = person,
                when = when,
                source = source,
                seal = SealText(person),
                paragraphs = paragraphs,
            });
            json = EscapeForScriptBlock(json);

            body.Append("<script type=\"application/json\" id=\"book-data\">").Append(json).Append("</script>\n");
            body.Append("<script>").Append(Script).Append("</script>\n");

            // 禁用 JavaScript 时：封面、完整正文与牌记照常可读。
            body.Append("<noscript>\n<div class=\"fallback\">\n");
            body.Append("<section class=\"cover\">\n");
            body.Append("<div class=\"cover-strip\">\n");
            body.Append("<div class=\"cover-title\">").Append(HtmlEncode(title)).Append("</div>\n");
            body.Append("<div class=\"cover-seal\" aria-hidden=\"true\">").Append(HtmlEncode(SealText(person))).Append("</div>\n");
            body.Append("</div>\n");
            body.Append("<div class=\"cover-side\">往事成书</div>\n");
            body.Append("<div class=\"cover-imprint\">太吾绘卷 · 江湖有灵</div>\n");
            body.Append("</section>\n");
            body.Append("<section class=\"fallback-flow\">\n");
            foreach (string para in paragraphs)
            {
                bool chapter = IsChapterHeading(para);
                body.Append(chapter
                    ? "<h2 class=\"chapter\">" : "<p>")
                    .Append(HtmlEncode(para))
                    .Append(chapter ? "</h2>\n" : "</p>\n");
            }
            body.Append("</section>\n<section class=\"colophon\">\n");
            body.Append("<p class=\"colo-title\">").Append(HtmlEncode(title)).Append("</p>\n");
            body.Append("<p>人物：").Append(HtmlEncode(person)).Append("</p>\n");
            body.Append("<p>成书：").Append(HtmlEncode(when)).Append("</p>\n");
            body.Append("<p>素材：").Append(HtmlEncode(source)).Append("</p>\n");
            body.Append("<p class=\"colo-imprint\">太吾绘卷 · 江湖有灵 辑录</p>\n");
            body.Append("</section>\n</div>\n</noscript>\n");
            body.Append("</div>\n</body>\n</html>\n");

            return head.ToString() + body;
        }

        /// <summary>印章用字：取人物名前两字，不够则以「有灵」补。</summary>
        static string SealText(string person)
        {
            if (string.IsNullOrEmpty(person)) return "有灵";
            return person.Length <= 2 ? person : person.Substring(0, 2);
        }

        static readonly Regex ChapterPattern = new Regex(
            @"^(第[〇零一二三四五六七八九十百千两0-9]{1,8}[章回卷节]|【[^】]{1,20}】|序章|序篇|楔子|尾声|终章|后记)",
            RegexOptions.Compiled);

        public static bool IsChapterHeading(string paragraph)
            => !string.IsNullOrEmpty(paragraph)
                && paragraph.Length <= 28 && ChapterPattern.IsMatch(paragraph);

        /// <summary>JSON 嵌入 script 块：封死 &lt;/script&gt; 与 HTML 解析面。</summary>
        static string EscapeForScriptBlock(string json)
        {
            if (string.IsNullOrEmpty(json)) return string.Empty;
            return json.Replace("<", "\\u003c").Replace(">", "\\u003e")
                .Replace("&", "\\u0026")
                .Replace("\u2028", "\\u2028").Replace("\u2029", "\\u2029");
        }

        const string Styles = @"
:root{
  --paper:#ece0c2; --paper-hi:#f3ebd4; --paper-edge:#d3c49c;
  --ink:#2d2820; --ink-soft:#71654e;
  --wood:#6d5433; --wood-deep:#3b2d1e;
  --cover:#43301f; --cover-hi:#584330; --thread:#e7dcbe;
  --cinnabar:#a23a26; --hair:rgba(96,76,46,.4);
}
*{box-sizing:border-box}
html,body{margin:0;padding:0;}
body{
  background:
    radial-gradient(1100px 420px at 50% -8%, rgba(255,238,196,.07), transparent 60%),
    repeating-linear-gradient(92deg, rgba(255,220,160,.025) 0 2px, transparent 2px 90px),
    repeating-linear-gradient(88deg, rgba(0,0,0,.16) 0 1px, transparent 1px 47px),
    linear-gradient(180deg,#2b2115 0%, #1e170e 70%, #191309 100%);
  color:var(--ink);
  font-family:""Kaiti SC"",STKaiti,KaiTi,""TW-Kai"",FangSong,""STFangsong"",SimSun,""Songti SC"",serif;
}
.desk{min-height:100vh;display:flex;flex-direction:column;align-items:center;padding:20px 14px 30px;}
.reader-bar{
  width:min(1240px,96vw);display:flex;justify-content:space-between;align-items:baseline;
  padding:4px 6px 16px;color:#cbbb92;letter-spacing:.2em;
}
.bar-title{font-size:18px;text-shadow:0 1px 2px rgba(0,0,0,.6);}
.bar-controls button,.pagenav button{
  font:inherit;font-size:14px;letter-spacing:.28em;color:#d9caa4;cursor:pointer;
  background:linear-gradient(180deg,rgba(96,74,46,.5),rgba(52,40,24,.55));
  border:1px solid rgba(217,202,164,.35);border-top-color:rgba(217,202,164,.55);
  padding:6px 18px 5px;margin-left:10px;
  box-shadow:0 2px 4px rgba(0,0,0,.4), inset 0 1px 0 rgba(255,240,200,.12);
}
.bar-controls button:hover,.pagenav button:hover{color:#241b0e;background:#d9caa4;}
.bar-controls button[aria-pressed=""false""]{opacity:.55}
.stage{width:min(1240px,96vw);flex:1;display:flex;justify-content:center;align-items:stretch;min-height:60vh;perspective:2400px;}
.spread{display:flex;width:100%;justify-content:center;align-items:stretch;}
.leaf{
  position:relative;width:min(590px,47vw);min-height:68vh;
  background:
    url(""data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='260' height='260'%3E%3Cfilter id='b'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.012 0.02' numOctaves='3' seed='7'/%3E%3CfeColorMatrix values='0 0 0 0 0.48 0 0 0 0 0.38 0 0 0 0 0.22 0 0 0 0.06 0'/%3E%3C/filter%3E%3Crect width='260' height='260' filter='url(%23b)'/%3E%3C/svg%3E""),
    url(""data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='180' height='180'%3E%3Cfilter id='f'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.85' numOctaves='2' stitchTiles='stitch'/%3E%3CfeColorMatrix values='0 0 0 0 0.42 0 0 0 0 0.35 0 0 0 0 0.24 0 0 0 0.045 0'/%3E%3C/filter%3E%3Crect width='180' height='180' filter='url(%23f)'/%3E%3C/svg%3E""),
    radial-gradient(150% 110% at 50% -10%, rgba(255,250,235,.55), rgba(255,250,235,0) 55%),
    radial-gradient(120% 130% at 50% 115%, rgba(98,74,40,.22), rgba(98,74,40,0) 55%),
    linear-gradient(103deg, var(--paper-hi) 0%, var(--paper) 55%, #e2d4b0 100%);
  box-shadow:
    0 24px 48px rgba(0,0,0,.55),
    0 3px 8px rgba(0,0,0,.45);
  transform-style:preserve-3d;
  backface-visibility:hidden;
}
/* 书口页层：外缘露出的一叠书页 */
.leaf.right::after{
  content:"""""";position:absolute;top:5px;bottom:5px;right:-8px;width:8px;
  background:repeating-linear-gradient(90deg,#e6dabd 0 1px,#c4b28a 1px 2px,#efe5cb 2px 3px);
  box-shadow:2px 0 4px rgba(0,0,0,.35);
}
.leaf.left::after{
  content:"""""";position:absolute;top:5px;bottom:5px;left:-8px;width:8px;
  background:repeating-linear-gradient(90deg,#efe5cb 0 1px,#c4b28a 1px 2px,#e6dabd 2px 3px);
  box-shadow:-2px 0 4px rgba(0,0,0,.35);
}
.gutter{
  width:34px;flex:none;align-self:stretch;position:relative;
  background:linear-gradient(90deg, rgba(0,0,0,.5), rgba(0,0,0,.16) 42%, rgba(0,0,0,.16) 58%, rgba(0,0,0,.5));
}
.gutter::before{
  content:"""""";position:absolute;inset:8px 15px;
  background:linear-gradient(90deg, rgba(0,0,0,.35), rgba(0,0,0,.05), rgba(0,0,0,.35));
  border-radius:50%;
}
.page-inner{
  position:absolute;inset:0;padding:56px 52px 60px;overflow:hidden;
  font-size:19.5px;line-height:2.08;letter-spacing:.05em;text-align:justify;
}
.page-inner .runhead{
  position:absolute;top:22px;left:52px;right:52px;font-size:12.5px;letter-spacing:.42em;
  color:rgba(110,95,65,.8);display:flex;justify-content:space-between;
}
.page-inner p{margin:0 0 .85em;text-indent:2em;}
.page-inner p.chapter{
  text-indent:0;text-align:center;font-size:23px;letter-spacing:.34em;
  margin:1.2em 0 1.5em;
}
.page-inner p.chapter::before{content:""◆"";font-size:9px;color:var(--cinnabar);margin-right:1.6em;vertical-align:3px;opacity:.85}
.page-inner p.chapter::after{content:""◆"";font-size:9px;color:var(--cinnabar);margin-left:1.6em;vertical-align:3px;opacity:.85}
.page-inner p.colo-title{font-size:23px;text-indent:0;text-align:center;letter-spacing:.34em;margin-top:1em;}
.page-inner p.colo-imprint{margin-top:3em;text-align:right;color:var(--wood);font-size:15px;}
/* 版心：靠装订线的鱼尾与书名页码 */
.banxin{
  position:absolute;top:52px;bottom:56px;width:16px;
  display:flex;flex-direction:column;align-items:center;justify-content:space-between;
  color:rgba(110,95,65,.85);font-size:11px;letter-spacing:.18em;
}
.banxin .yuwei{width:0;height:0;border-left:5px solid transparent;border-right:5px solid transparent;border-bottom:5px solid rgba(110,95,65,.7);}
.banxin .yuwei:last-child{transform:rotate(180deg);}
.banxin .bx-text{writing-mode:vertical-rl;max-height:62%;overflow:hidden;white-space:nowrap;}
.leaf.right .banxin{left:12px;}
.leaf.left .banxin{right:12px;}
body.vertical .page-inner{writing-mode:vertical-rl;text-orientation:mixed;}
body.vertical .page-inner p{text-indent:0;margin:0 0 .85em;}
body.vertical .banxin .bx-text{writing-mode:horizontal-tb;}
/* 掀页动画 */
.leaf.flip-in-r{transform-origin:left center;transform:rotateY(-155deg);}
.leaf.flip-in-l{transform-origin:right center;transform:rotateY(155deg);}
.leaf.flip-in-r.flipping,.leaf.flip-in-l.flipping{
  transition:transform .6s cubic-bezier(.28,.72,.26,1);transform:rotateY(0deg);
}
/* 封面：栗壳布面线装 */
.leaf.cover-leaf{
  background:
    url(""data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='220' height='220'%3E%3Cfilter id='c'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.16 0.24' numOctaves='4' seed='11'/%3E%3CfeColorMatrix values='0 0 0 0 0.9 0 0 0 0 0.78 0 0 0 0 0.58 0 0 0 0.05 0'/%3E%3C/filter%3E%3Crect width='220' height='220' filter='url(%23c)'/%3E%3C/svg%3E""),
    radial-gradient(120% 100% at 30% 0%, rgba(255,235,200,.1), transparent 55%),
    linear-gradient(112deg, var(--cover-hi) 0%, var(--cover) 52%, #33241278 100%),
    linear-gradient(112deg, #584330, #33241a);
  border:1px solid rgba(20,12,5,.6);
}
.leaf.cover-leaf::after{
  content:"""""";position:absolute;top:5px;bottom:5px;left:-10px;width:10px;
  background:repeating-linear-gradient(90deg,#d9cba6 0 2px,#b3a077 2px 3px,#e6dab9 3px 5px);
  box-shadow:-3px 0 5px rgba(0,0,0,.5);
}
.leaf.cover-leaf .stitch{
  position:absolute;left:-3px;width:20px;height:2px;background:var(--thread);
  box-shadow:0 1px 1px rgba(0,0,0,.5);opacity:.9;
}
.cover-strip{
  position:absolute;top:11%;bottom:11%;right:15%;width:96px;
  background:
    url(""data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='140' height='140'%3E%3Cfilter id='s'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.7' numOctaves='2' stitchTiles='stitch'/%3E%3CfeColorMatrix values='0 0 0 0 0.5 0 0 0 0 0.42 0 0 0 0 0.3 0 0 0 0.05 0'/%3E%3C/filter%3E%3Crect width='140' height='140' filter='url(%23s)'/%3E%3C/svg%3E""),
    linear-gradient(180deg,#f3ead2,#e9dcba);
  border:1px solid rgba(58,42,22,.55);
  box-shadow:inset 0 0 14px rgba(122,95,55,.28), 3px 4px 8px rgba(0,0,0,.45);
  display:flex;align-items:center;justify-content:center;
}
.cover-title{
  writing-mode:vertical-rl;font-size:33px;letter-spacing:.3em;color:#241e15;
  font-weight:700;text-shadow:0 1px 0 rgba(255,250,235,.5);
  max-height:78%;
}
.cover-seal{
  position:absolute;left:50%;bottom:7%;transform:translateX(-50%) rotate(-4deg);
  width:44px;height:44px;background:var(--cinnabar);color:#f5e9d2;
  display:flex;align-items:center;justify-content:center;
  font-size:15px;line-height:1.05;text-align:center;padding:3px;
  border-radius:5px;opacity:.92;
  box-shadow:inset 0 0 5px rgba(70,8,0,.5), 1px 1px 2px rgba(0,0,0,.35);
}
.cover-side{
  position:absolute;left:16%;top:50%;transform:translateY(-50%);
  writing-mode:vertical-rl;font-size:15px;letter-spacing:.7em;color:rgba(238,224,190,.75);
}
.cover-imprint{
  position:absolute;bottom:24px;left:0;right:0;text-align:center;
  font-size:12.5px;letter-spacing:.6em;color:rgba(238,224,190,.6);
}
/* 底部翻页 */
.pagenav{width:min(1240px,96vw);display:flex;justify-content:center;align-items:center;gap:16px;padding-top:20px;}
.pagenav button{font-size:15px;padding:8px 30px 7px;margin:0;}
.pagenav button:disabled{opacity:.35;cursor:default;}
.page-indicator{color:#cbbb92;font-size:14px;letter-spacing:.14em;display:flex;align-items:center;gap:6px;}
#page-jump{
  width:54px;font:inherit;font-size:14px;text-align:center;color:#d9caa4;background:transparent;
  border:none;border-bottom:1px solid rgba(217,202,164,.5);padding:2px 0;outline:none;
}
/* 无脚本回退：连续排版，仍可读完每一字 */
noscript .fallback{width:min(760px,94vw);margin:0 auto;}
noscript .cover{
  margin:22px 0;min-height:62vh;position:relative;
  background:
    url(""data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='220' height='220'%3E%3Cfilter id='c'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.16 0.24' numOctaves='4' seed='11'/%3E%3CfeColorMatrix values='0 0 0 0 0.9 0 0 0 0 0.78 0 0 0 0 0.58 0 0 0 0.05 0'/%3E%3C/filter%3E%3Crect width='220' height='220' filter='url(%23c)'/%3E%3C/svg%3E""),
    linear-gradient(112deg, #584330, #33241a);
  border:1px solid rgba(20,12,5,.6);
  box-shadow:0 24px 48px rgba(0,0,0,.55);
}
noscript .cover-strip{position:absolute;top:11%;bottom:11%;right:15%;width:96px;
  background:linear-gradient(180deg,#f3ead2,#e9dcba);border:1px solid rgba(58,42,22,.55);
  box-shadow:inset 0 0 14px rgba(122,95,55,.28), 3px 4px 8px rgba(0,0,0,.45);
  display:flex;align-items:center;justify-content:center;}
noscript .cover-title{writing-mode:vertical-rl;font-size:33px;letter-spacing:.3em;color:#241e15;font-weight:700;}
noscript .cover-seal{position:absolute;left:50%;bottom:7%;transform:translateX(-50%) rotate(-4deg);
  width:44px;height:44px;background:var(--cinnabar);color:#f5e9d2;
  display:flex;align-items:center;justify-content:center;font-size:15px;border-radius:5px;}
noscript .cover-side{position:absolute;left:16%;top:50%;transform:translateY(-50%);
  writing-mode:vertical-rl;font-size:15px;letter-spacing:.7em;color:rgba(238,224,190,.75);}
noscript .cover-imprint{position:absolute;bottom:24px;left:0;right:0;text-align:center;
  font-size:12.5px;letter-spacing:.6em;color:rgba(238,224,190,.6);}
noscript .fallback-flow{background:var(--paper);padding:60px 56px;font-size:19px;line-height:2.08;}
noscript .fallback-flow p{text-indent:2em;margin:0 0 .85em;text-align:justify;}
noscript .fallback-flow h2.chapter{font-size:23px;letter-spacing:.34em;text-align:center;font-weight:normal;margin:1.2em 0;}
noscript .colophon{background:var(--paper-edge);padding:42px 56px;font-size:16px;line-height:2;color:var(--ink-soft);}
noscript .colo-imprint{text-align:right;color:var(--wood);}
@media (max-width:820px){
  .spread{flex-direction:column;align-items:center;}
  .leaf{width:min(590px,94vw);min-height:64vh;}
  .gutter{display:none;}
  .leaf + .leaf{margin-top:18px;}
  .page-inner{padding:48px 34px 54px;font-size:17.5px;}
  .page-inner .runhead{left:34px;right:34px;}
  .leaf.right .banxin{left:6px;}
  .leaf.left .banxin{right:6px;}
  .cover-title{font-size:28px;}
  .cover-strip{width:80px;}
}
@media (prefers-reduced-motion:reduce){
  .leaf.flip-in-r,.leaf.flip-in-l,.leaf.flipping{transform:none;transition:none;}
}
";

        const string Script = @"
(function(){
  'use strict';
  var dataEl = document.getElementById('book-data');
  if (!dataEl) return;
  var book;
  try { book = JSON.parse(dataEl.textContent); } catch (e) { return; }
  if (!book || !book.paragraphs) return;

  var stage = document.getElementById('stage');
  var prevBtn = document.getElementById('prev-page');
  var nextBtn = document.getElementById('next-page');
  var jumpInput = document.getElementById('page-jump');
  var totalEl = document.getElementById('page-total');
  var orientBtn = document.getElementById('toggle-orient');
  var animBtn = document.getElementById('toggle-anim');

  var pages = [];
  var cursor = 0;
  var spreadMode = true;
  var animOn = true;
  var navLock = false;

  function isChapter(t){
    return t.length <= 28 && /^(第[〇零一二三四五六七八九十百千两0-9]{1,8}[章回卷节]|【[^】]{1,20}】|序章|序篇|楔子|尾声|终章|后记)/.test(t);
  }

  // ---- 页面盒测量 ----
  var probe = document.createElement('div');
  probe.className = 'page-inner';
  probe.style.cssText = 'position:absolute;visibility:hidden;left:-10000px;top:0;';
  probe.style.width = '500px'; probe.style.height = '600px';
  document.body.appendChild(probe);

  function measureSetup(leafWidth, innerHeight){
    probe.style.width = leafWidth + 'px';
    probe.style.height = innerHeight + 'px';
    probe.innerHTML = '';
  }
  function fits(el){
    return el.scrollHeight <= el.clientHeight + 1 &&
           el.scrollWidth <= el.clientWidth + 1;
  }
  function newMeasurePage(){
    var page = document.createElement('div');
    page.className = 'page-inner';
    return page;
  }

  // 贪心分页：段落能放则放；放不下且当前页非空则开新页；
  // 单段超页则按字二分截断，保证任何长度的正文都不会丢失。
  function paginate(innerWidth, innerHeight){
    measureSetup(innerWidth, innerHeight);
    var result = [];
    var current = [];
    var measure = newMeasurePage();
    probe.appendChild(measure);

    function flush(){
      if (current.length) result.push({ kind:'text', blocks: current });
      current = [];
      if (measure.parentNode) measure.parentNode.removeChild(measure);
      measure = newMeasurePage();
      probe.appendChild(measure);
    }
    function appendBlock(text, chapter){
      var p = document.createElement('p');
      if (chapter) p.className = 'chapter';
      p.textContent = text;   // 模型正文只以纯文本进入文档
      measure.appendChild(p);
      current.push({ text: text, chapter: !!chapter });
      return p;
    }

    for (var i = 0; i < book.paragraphs.length; i++){
      var para = book.paragraphs[i];
      if (!para) continue;
      var chapter = isChapter(para);
      if (chapter && current.length) flush();
      var rest = para;
      for (;;){
        var p = appendBlock(rest, chapter);
        if (fits(measure)) break;
        measure.removeChild(p); current.pop();
        if (!current.length){
          // 单段超页：二分截断
          var lo = 1, hi = rest.length, best = 0;
          while (lo <= hi){
            var mid = (lo + hi) >> 1;
            p = appendBlock(rest.substring(0, mid), chapter);
            if (fits(measure)){ best = mid; lo = mid + 1; measure.removeChild(p); current.pop(); }
            else { measure.removeChild(p); current.pop(); hi = mid - 1; }
          }
          if (best <= 0) best = 1;   // 极端情况下逐字兜底
          appendBlock(rest.substring(0, best), chapter);
          rest = rest.substring(best);
          flush();
          chapter = false;
          if (!rest) break;
        } else {
          flush();   // 整段挪到下一页
          continue;
        }
      }
    }
    flush();
    probe.innerHTML = '';   // 分页完毕即清空测量盒，隐藏探针不残留任何正文副本
    return result;
  }

  // ---- 渲染 ----
  function banxin(index){
    var bx = document.createElement('div');
    bx.className = 'banxin';
    var t = document.createElement('div'); t.className = 'yuwei';
    var m = document.createElement('div'); m.className = 'bx-text';
    m.textContent = book.title + ' · ' + index;
    var b = document.createElement('div'); b.className = 'yuwei';
    bx.appendChild(t); bx.appendChild(m); bx.appendChild(b);
    return bx;
  }

  function textPage(page, index, side){
    var leaf = document.createElement('div');
    leaf.className = 'leaf ' + side;
    leaf.setAttribute('data-side', side);
    var inner = document.createElement('div');
    inner.className = 'page-inner';
    var head = document.createElement('div');
    head.className = 'runhead';
    var h1 = document.createElement('span'); h1.textContent = book.title;
    var h2 = document.createElement('span'); h2.textContent = book.person;
    head.appendChild(h1); head.appendChild(h2);
    inner.appendChild(head);
    for (var i = 0; i < page.blocks.length; i++){
      var b = page.blocks[i];
      var p = document.createElement('p');
      if (b.chapter) p.className = 'chapter';
      p.textContent = b.text;
      inner.appendChild(p);
    }
    leaf.appendChild(inner);
    leaf.appendChild(banxin(index));
    return leaf;
  }

  function coverLeaf(){
    var leaf = document.createElement('div');
    leaf.className = 'leaf cover-leaf';
    leaf.setAttribute('data-side', 'left');
    var strip = document.createElement('div');
    strip.className = 'cover-strip';
    var t = document.createElement('div');
    t.className = 'cover-title'; t.textContent = book.title;
    var seal = document.createElement('div');
    seal.className = 'cover-seal'; seal.textContent = book.seal;
    strip.appendChild(t); strip.appendChild(seal);
    var side = document.createElement('div');
    side.className = 'cover-side'; side.textContent = '往事成书';
    var imp = document.createElement('div');
    imp.className = 'cover-imprint'; imp.textContent = '太吾绘卷 · 江湖有灵';
    leaf.appendChild(strip); leaf.appendChild(side); leaf.appendChild(imp);
    var pos = ['16%','38%','62%','84%'];
    for (var i = 0; i < pos.length; i++){
      var st = document.createElement('div');
      st.className = 'stitch'; st.style.top = pos[i];
      leaf.appendChild(st);
    }
    return leaf;
  }

  function colophonLeaf(index, side){
    var leaf = document.createElement('div');
    leaf.className = 'leaf ' + side;
    leaf.setAttribute('data-side', side);
    var inner = document.createElement('div');
    inner.className = 'page-inner';
    function line(text, cls){
      var p = document.createElement('p');
      if (cls) p.className = cls;
      p.textContent = text;
      inner.appendChild(p);
    }
    line(book.title, 'colo-title');
    line('人物：' + book.person);
    line('成书：' + book.when);
    line('素材：' + book.source);
    line('太吾绘卷 · 江湖有灵 辑录', 'colo-imprint');
    leaf.appendChild(inner);
    leaf.appendChild(banxin(index));
    return leaf;
  }

  function layout(){
    spreadMode = window.innerWidth > 820;
    var stageWidth = stage.clientWidth;
    var leafWidth = spreadMode ? Math.floor((stageWidth - 48) / 2) : Math.min(590, stageWidth);
    leafWidth = Math.min(leafWidth, 590);
    var innerHeight = Math.max(430, Math.min(window.innerHeight - 240, 940));
    var innerWidth = leafWidth - (spreadMode ? 104 : 70);
    pages = paginate(innerWidth, innerHeight);
    render();
  }

  function leafFor(i){
    if (i < 0) return null;
    if (i === 0) return coverLeaf();
    var contentIndex = i - 1;
    if (contentIndex < pages.length){
      var side = spreadMode ? (i % 2 === 1 ? 'right' : 'left') : 'right';
      return textPage(pages[contentIndex], i, side);
    }
    if (contentIndex === pages.length){
      var side2 = spreadMode ? (i % 2 === 1 ? 'right' : 'left') : 'right';
      return colophonLeaf(i, side2);
    }
    return null;
  }

  function render(){
    stage.innerHTML = '';
    var spread = document.createElement('div');
    spread.className = 'spread';
    if (spreadMode){
      var first = leafFor(cursor);
      var second = leafFor(cursor + 1);
      if (first && second){
        var gutter = document.createElement('div');
        gutter.className = 'gutter';
        if (cursor % 2 === 1){
          first.setAttribute('data-side', 'left');
          second.setAttribute('data-side', 'right');
          spread.appendChild(first);
          spread.appendChild(gutter);
          spread.appendChild(second);
        } else {
          spread.appendChild(first);
          spread.appendChild(gutter);
          spread.appendChild(second);
        }
      } else if (first){
        first.setAttribute('data-side', 'right');
        spread.appendChild(first);
      }
    } else {
      var one = leafFor(cursor);
      if (one){ one.setAttribute('data-side', 'right'); spread.appendChild(one); }
    }
    stage.appendChild(spread);
    var total = pages.length + 1;
    totalEl.textContent = '／ 共 ' + total + ' 页';
    jumpInput.value = String(cursor + 1);
    prevBtn.disabled = cursor <= 0 || navLock;
    nextBtn.disabled = cursor >= total - 1 || navLock;
  }

  function setNavLock(locked){
    navLock = locked;
    prevBtn.disabled = cursor <= 0 || locked;
    nextBtn.disabled = cursor >= pages.length || locked;
  }

  function reducedMotion(){
    return window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  }

  function goTo(i, animate){
    var total = pages.length + 1;
    if (i < 0) i = 0;
    if (i > total - 1) i = total - 1;
    var forward = i > cursor;
    if (i === cursor){ render(); return; }
    cursor = i;
    render();
    if (!animate || !animOn || reducedMotion()) return;
    var sel = spreadMode
      ? (forward ? '.leaf[data-side=""right""]' : '.leaf[data-side=""left""]')
      : '.leaf';
    var el = stage.querySelector(sel);
    if (!el) return;
    setNavLock(true);
    el.classList.add(forward ? 'flip-in-r' : 'flip-in-l');
    void el.offsetWidth;
    el.classList.add('flipping');
    setTimeout(function(){
      el.classList.remove('flip-in-r', 'flip-in-l', 'flipping');
      setNavLock(false);
    }, 620);
  }

  prevBtn.addEventListener('click', function(){ goTo(cursor - (spreadMode ? 2 : 1), true); });
  nextBtn.addEventListener('click', function(){ goTo(cursor + (spreadMode ? 2 : 1), true); });
  jumpInput.addEventListener('change', function(){
    var v = parseInt(jumpInput.value, 10);
    if (!isNaN(v)) goTo(v - 1, false);
  });
  document.addEventListener('keydown', function(e){
    if (e.target === jumpInput) return;
    if (e.key === 'ArrowLeft') goTo(cursor - (spreadMode ? 2 : 1), true);
    else if (e.key === 'ArrowRight') goTo(cursor + (spreadMode ? 2 : 1), true);
  });
  orientBtn.addEventListener('click', function(){
    var v = document.body.classList.toggle('vertical');
    orientBtn.textContent = v ? '横排' : '竖排';
    layout();
  });
  animBtn.addEventListener('click', function(){
    animOn = !animOn;
    animBtn.textContent = animOn ? '动效·开' : '动效·关';
    animBtn.setAttribute('aria-pressed', animOn ? 'true' : 'false');
  });

  var resizeTimer = null;
  window.addEventListener('resize', function(){
    if (resizeTimer) clearTimeout(resizeTimer);
    resizeTimer = setTimeout(function(){
      var wasSpread = spreadMode;
      layout();
      if (wasSpread !== spreadMode) cursor = 0;
    }, 160);
  });

  layout();
})();
";
    }
}
