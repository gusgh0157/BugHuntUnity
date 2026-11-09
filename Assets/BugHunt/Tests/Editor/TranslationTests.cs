using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace BugHunt.Tests.Editor
{
    // 두 언어 짝이 빠진 문자열을 찾는 EditMode 테스트. Test Runner → EditMode → Run All.
    // 2 つの言語の対が抜けている文字列を見つける EditMode テスト。テストランナー → EditMode → Run All。
    //
    // Assets/BugHunt 아래 .cs 의 문자열 리터럴을 읽어서, 한글이 든 리터럴은 다음 중 하나여야 한다:
    //   - Loc.L("한국어", "日本語") / Loc.F("한국어 {0}", "日本語 {0}", 값) 의 첫 인자(한국어 쪽) 안
    //   - [Lab(번호, "이름", "名前", Symptom = "증상", SymptomJa = "症状")] 특성 안 (일본어 쪽은 아래 랩 테스트가 본다)
    //   - 가나·한자가 함께 든 리터럴 (메뉴 이름처럼 두 언어를 한 문자열에 나란히 쓴 것)
    //   - "// ko+ja" 표시가 붙고 가나·한자도 든 줄 (표의 한 줄에 한국어와 일본어를 나란히 쓴 것: LabRunner.Parts)
    //   - "// ko-only" 표시가 붙은 줄 (직렬화 데이터의 기본값처럼 일부러 한국어만 둔 것)
    // 주석은 보지 않는다. 그리고 Loc.F 의 두 서식 문자열은 리터럴이어야 하고 자리 표시자 {0} {1} 가 같아야 한다.
    // Loc.L 에 보간 문자열($"...{x}...")을 넘기면 두 쪽을 다 만들고 식도 두 번 계산하므로 Loc.F 를 쓴다.
    //
    // Assets/BugHunt 以下の .cs の文字列リテラルを読み、ハングルを含むリテラルは次のどれかでなければなりません:
    //   - Loc.L("한국어", "日本語") / Loc.F("한국어 {0}", "日本語 {0}", 値) の最初の引数 (韓国語側) の中
    //   - [Lab(番号, "이름", "名前", Symptom = "증상", SymptomJa = "症状")] 属性の中 (日本語側は下のラボのテストが見ます)
    //   - かな・漢字も含むリテラル (メニュー名のように 2 つの言語を 1 つの文字列に並べて書いたもの)
    //   - 「// ko+ja」の印が付き、かな・漢字も含む行 (表の 1 行に韓国語と日本語を並べて書いたもの: LabRunner.Parts)
    //   - 「// ko-only」の印が付いた行 (シリアライズされたデータの既定値のように、わざと韓国語だけにしたもの)
    // コメントは見ません。また、Loc.F の 2 つの書式文字列はリテラルで、プレースホルダー {0} {1} が同じでなければなりません。
    // Loc.L に補間文字列 ($"...{x}...") を渡すと両方を作って式も 2 回計算するので、Loc.F を使います。
    public class TranslationTests
    {
        public const string KoreanOnlyMark = "// ko-only";
        public const string BothLanguagesMark = "// ko+ja";

        static readonly Regex Placeholder = new Regex(@"\{(\d+)(?:[,:][^}]*)?\}");

        static bool HasKana(string s) => s.Any(c => (c >= '぀' && c <= 'ヿ') || (c >= '一' && c <= '鿿'));

        static string Root => Path.Combine(Application.dataPath, "BugHunt");

        [Test]
        public void KoreanLiteralsArePairedWithJapanese()
        {
            var problems = new List<string>();
            foreach (string path in Directory.GetFiles(Root, "*.cs", SearchOption.AllDirectories).OrderBy(p => p, System.StringComparer.Ordinal))
            {
                string rel = "Assets/BugHunt/" + path.Substring(Root.Length + 1).Replace('\\', '/');
                string src = File.ReadAllText(path, Encoding.UTF8);
                string[] lines = src.Split('\n');
                foreach (var (at, message) in new LiteralScanner(src).Problems())
                {
                    int line = 0;
                    for (int i = 0; i < at; ++i)
                    {
                        if (src[i] == '\n')
                        {
                            ++line;
                        }
                    }
                    if (lines[line].Contains(KoreanOnlyMark) || (lines[line].Contains(BothLanguagesMark) && HasKana(lines[line])))
                    {
                        continue;
                    }
                    problems.Add($"{rel}:{line + 1}: {message}");
                }
            }
            Assert.IsEmpty(problems, $"짝이 맞지 않는 문자열 {problems.Count}개 / 対になっていない文字列 {problems.Count} 個\n" + string.Join("\n", problems));
        }

        [Test]
        public void EveryLabHasTitleAndSymptomInJapanese()
        {
            var problems = new List<string>();
            foreach (LabRunner.LabEntry entry in LabRunner.Labs)
            {
                foreach (var (name, ko, ja) in new[] { ("Title", entry.TitleKo, entry.TitleJa), ("Symptom", entry.SymptomKo, entry.SymptomJa) })
                {
                    if (string.IsNullOrEmpty(ko))
                    {
                        problems.Add($"랩 {entry.Number}: {name} 이 없음 / ラボ {entry.Number}: {name} がない");
                    }
                    if (string.IsNullOrEmpty(ja) || Loc.HasHangul(ja))
                    {
                        problems.Add($"랩 {entry.Number}: {name}Ja 가 없거나 한글이 남아 있음 / ラボ {entry.Number}: {name}Ja がないかハングルが残っている: \"{ja}\"");
                    }
                }
            }
            foreach (var part in LabRunner.Parts)
            {
                if (string.IsNullOrEmpty(part.Ja) || Loc.HasHangul(part.Ja))
                {
                    problems.Add($"부 이름 / 部の名前: \"{part.Ko}\" → \"{part.Ja}\"");
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void ScannerFindsUnpairedLiterals()
        {
            // 스캐너 자체 점검: 짝이 맞는 것은 통과, 빠진 것은 걸린다 / スキャナー自体の点検: 対になっているものは通り、抜けているものは引っかかる
            string ok = "class A { void M() { Report.Note(Loc.L(\"가\", \"か\")); Report.Note(Loc.F(\"가 {0}\", \"か {0}\", 1)); string s = $\"{Loc.L(\"가\", \"か\")}\"; } }\n"
                      + "[Lab(1, \"가\", \"か\", Symptom = \"나\", SymptomJa = \"な\")] class B { } // \"주석\"\n"
                      + "class C { string d = \"가 / か\"; }";
            Assert.IsEmpty(new LiteralScanner(ok).Problems().Select(p => p.Message));
            string bad = "class A { void M() { Report.Note(\"가\"); Report.Note(Loc.L(\"가\", \"나\")); Report.Note(Loc.F(\"가 {0}\", \"か\", 1)); Report.Note(Loc.F(\"가 {0}\", x, 1));"
                       + " Report.Note(Loc.L($\"가 {n}\", $\"か {n}\")); string s = $\"{(f ? \"가\" : \"\")}\"; } }";
            Assert.AreEqual(6, new LiteralScanner(bad).Problems().Count, string.Join("\n", new LiteralScanner(bad).Problems().Select(p => p.Message)));
        }

        static string Short(string s)
        {
            s = s.Replace("\r", "").Replace("\n", "\\n");
            return s.Length > 80 ? s.Substring(0, 80) + "…" : s;
        }

        // C# 소스를 훑어 한글 문자열 리터럴이 어디에 있는지 본다. 주석, 문자 리터럴, 보간 구멍과 그 안의 문자열, 축자 문자열(@"")을 처리한다.
        // (BugHunt C# 판 Strings.cs 의 스캐너를 Unity 의 C# 9 에 맞게 옮기고 Loc.F 와 [Lab] 을 더했다.)
        // C# のソースをなめて、ハングルの文字列リテラルがどこにあるかを見ます。コメント、文字リテラル、補間の穴とその中の文字列、逐語的文字列 (@"") を扱います。
        // (BugHunt C# 版 Strings.cs のスキャナーを Unity の C# 9 に合わせて移し、Loc.F と [Lab] を加えました。)
        sealed class LiteralScanner
        {
            enum Kind { Other, L, F, LabAttribute }

            // 여는 괄호 ( [ { 마다 하나 / 開く括弧 ( [ { ごとに 1 つ
            sealed class Frame
            {
                public Kind Kind;
                public int Start;
                public int Arg;
                public readonly List<ArgInfo> Args = new List<ArgInfo> { new ArgInfo() };
            }

            // 인자 하나가 리터럴 하나로만 되어 있나 / 引数 1 つがリテラル 1 つだけでできているか
            sealed class ArgInfo
            {
                public int Literals;
                public bool Other;
                public string Text;
                public bool Interpolated;
                public bool HasHoles;
            }

            readonly string src;
            int i;
            readonly List<Frame> frames = new List<Frame>();
            readonly List<(int At, string Message)> found = new List<(int, string)>();

            public LiteralScanner(string source)
            {
                src = source;
            }

            public List<(int At, string Message)> Problems()
            {
                Code(false);
                return found;
            }

            Frame Top => frames.Count > 0 ? frames[frames.Count - 1] : null;

            void Code(bool inHole)
            {
                int bottom = frames.Count;
                while (i < src.Length)
                {
                    char c = src[i];
                    bool top = frames.Count == bottom;
                    if (inHole && top && (c == '}' || c == ':' || c == ','))
                    {
                        return;
                    }
                    if (!inHole && (Starts("//") || (c == '#' && LineStartBefore(i))))
                    {
                        int nl = src.IndexOf('\n', i);
                        i = nl < 0 ? src.Length : nl;
                    }
                    else if (Starts("/*"))
                    {
                        int end = src.IndexOf("*/", i + 2, System.StringComparison.Ordinal);
                        i = end < 0 ? src.Length : end + 2;
                    }
                    else if (c == '\'')
                    {
                        MarkOther();
                        ++i;
                        while (i < src.Length && src[i] != '\'')
                        {
                            i += src[i] == '\\' ? 2 : 1;
                        }
                        ++i;
                    }
                    else if (c == '"' || ((c == '$' || c == '@') && StringPrefixAt(i)))
                    {
                        Literal();
                    }
                    else if (c == '(' || c == '[' || c == '{')
                    {
                        MarkOther();
                        frames.Add(new Frame { Kind = c == '(' ? KindBefore(i) : Kind.Other, Start = i });
                        ++i;
                    }
                    else if (c == ')' || c == ']' || c == '}')
                    {
                        if (frames.Count > bottom)
                        {
                            Close(frames[frames.Count - 1]);
                            frames.RemoveAt(frames.Count - 1);
                        }
                        ++i;
                    }
                    else if (c == ',' && frames.Count > 0)
                    {
                        Frame f = Top;
                        ++f.Arg;
                        f.Args.Add(new ArgInfo());
                        ++i;
                    }
                    else
                    {
                        if (!char.IsWhiteSpace(c))
                        {
                            MarkOther();
                        }
                        ++i;
                    }
                }
            }

            void MarkOther()
            {
                Frame f = Top;
                if (f != null)
                {
                    f.Args[f.Arg].Other = true;
                }
            }

            Kind KindBefore(int open)
            {
                int j = open - 1;
                while (j >= 0 && char.IsWhiteSpace(src[j]))
                {
                    --j;
                }
                int end = j + 1;
                while (j >= 0 && (char.IsLetterOrDigit(src[j]) || src[j] == '_' || src[j] == '.'))
                {
                    --j;
                }
                string name = src.Substring(j + 1, end - j - 1);
                if (name == "Loc.L")
                {
                    return Kind.L;
                }
                if (name == "Loc.F")
                {
                    return Kind.F;
                }
                if (name == "Lab" || name == "LabAttribute")
                {
                    while (j >= 0 && char.IsWhiteSpace(src[j]))
                    {
                        --j;
                    }
                    if (j >= 0 && (src[j] == '[' || src[j] == ','))
                    {
                        return Kind.LabAttribute;
                    }
                }
                return Kind.Other;
            }

            void Close(Frame f)
            {
                if (f.Kind == Kind.F)
                {
                    ArgInfo ko = f.Args[0];
                    ArgInfo ja = f.Args.Count > 1 ? f.Args[1] : null;
                    if (ja == null || !IsPlainLiteral(ko) || !IsPlainLiteral(ja))
                    {
                        found.Add((f.Start, "Loc.F 의 처음 두 인자는 한국어·일본어 서식 문자열 리터럴이어야 함 / Loc.F の最初の 2 つの引数は韓国語・日本語の書式文字列リテラルでなければならない"));
                    }
                    else if (!SamePlaceholders(ko.Text, ja.Text))
                    {
                        found.Add((f.Start, $"자리 표시자가 다름 / プレースホルダーが違う: \"{Short(ko.Text)}\" ↔ \"{Short(ja.Text)}\""));
                    }
                }
                else if (f.Kind == Kind.L)
                {
                    if (f.Args.Count != 2)
                    {
                        found.Add((f.Start, "Loc.L 은 인자가 둘(한국어, 일본어) / Loc.L は引数が 2 つ (韓国語、日本語)"));
                    }
                    else if (f.Args.Any(a => a.Interpolated && a.HasHoles))
                    {
                        found.Add((f.Start, "Loc.L 에 보간 문자열 → Loc.F(\"... {0}\", \"... {0}\", 값) 로 / Loc.L に補間文字列 → Loc.F(\"... {0}\", \"... {0}\", 値) に"));
                    }
                }
            }

            static bool IsPlainLiteral(ArgInfo a) => a.Literals == 1 && !a.Other && !a.Interpolated;

            // 가장 안쪽의 Loc.L / Loc.F / [Lab] 이 어디인가 / 最も内側の Loc.L / Loc.F / [Lab] はどこか
            Frame Owner()
            {
                for (int k = frames.Count - 1; k >= 0; --k)
                {
                    if (frames[k].Kind != Kind.Other)
                    {
                        return frames[k];
                    }
                }
                return null;
            }

            void Literal()
            {
                int start = i;
                int dollars = 0;
                bool verbatim = false;
                while (src[i] == '$' || src[i] == '@')
                {
                    if (src[i] == '$')
                    {
                        ++dollars;
                    }
                    else
                    {
                        verbatim = true;
                    }
                    ++i;
                }
                ++i;   // 여는 " / 開く "
                var text = new StringBuilder();
                bool holes = false;
                while (i < src.Length)
                {
                    char c = src[i];
                    if (c == '\\' && !verbatim)
                    {
                        char next = src[i + 1];
                        text.Append(next == 'n' ? '\n' : next == 't' ? '\t' : next);
                        i += 2;
                        continue;
                    }
                    if (c == '"')
                    {
                        if (verbatim && Starts("\"\""))
                        {
                            text.Append('"');
                            i += 2;
                            continue;
                        }
                        ++i;
                        break;
                    }
                    if (dollars > 0 && c == '{')
                    {
                        if (Starts("{{"))
                        {
                            text.Append('{');
                            i += 2;
                            continue;
                        }
                        ++i;
                        holes = true;
                        Hole();
                        text.Append("{}");
                        continue;
                    }
                    if (dollars > 0 && Starts("}}"))
                    {
                        text.Append('}');
                        i += 2;
                        continue;
                    }
                    text.Append(c);
                    ++i;
                }

                string value = text.ToString();
                Frame top = Top;
                if (top != null)
                {
                    ArgInfo arg = top.Args[top.Arg];
                    ++arg.Literals;
                    arg.Text = value;
                    arg.Interpolated |= dollars > 0;
                    arg.HasHoles |= holes;
                }
                if (!Loc.HasHangul(value) || HasKana(value))
                {
                    return;
                }
                Frame owner = Owner();
                if (owner == null)
                {
                    found.Add((start, $"한글 문자열에 일본어 짝이 없음 → Loc.L(\"한국어\", \"日本語\") / ハングルの文字列に日本語の対がない → Loc.L(\"한국어\", \"日本語\"): \"{Short(value)}\""));
                }
                else if (owner.Kind != Kind.LabAttribute && owner.Arg >= 1)
                {
                    found.Add((start, $"일본어 쪽에 한글 / 日本語側にハングル: \"{Short(value)}\""));
                }
            }

            // 보간 구멍 { 식 , 정렬 : 서식 } 에서 식만 코드로 읽고, 정렬·서식은 닫는 } 까지 건너뛴다.
            // 補間の穴 { 式 , 整列 : 書式 } のうち式だけをコードとして読み、整列・書式は閉じる } まで飛ばします。
            void Hole()
            {
                Code(true);
                while (i < src.Length && src[i] != '}')
                {
                    ++i;
                }
                ++i;
            }

            bool Starts(string s) => string.CompareOrdinal(src, i, s, 0, s.Length) == 0;

            bool StringPrefixAt(int k)
            {
                while (k < src.Length && (src[k] == '$' || src[k] == '@'))
                {
                    ++k;
                }
                return k < src.Length && src[k] == '"';
            }

            bool LineStartBefore(int k)
            {
                int nl = src.LastIndexOf('\n', System.Math.Max(k - 1, 0));
                return src.Substring(nl + 1, k - nl - 1).Trim().Length == 0;
            }

            static bool SamePlaceholders(string ko, string ja)
            {
                var a = Placeholder.Matches(ko).Cast<Match>().Select(m => m.Groups[1].Value).Distinct().OrderBy(s => s);
                var b = Placeholder.Matches(ja).Cast<Match>().Select(m => m.Groups[1].Value).Distinct().OrderBy(s => s);
                return a.SequenceEqual(b);
            }
        }
    }
}
