using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace BugHunt.Tests.Editor
{
    // 빠진 번역을 찾는 EditMode 테스트. Test Runner → EditMode → Run All.
    // 抜けている翻訳を見つける EditMode テスト。テストランナー → EditMode → Run All。
    //
    // Assets/BugHunt 아래 .cs(Lang/ 와 Tests/ 제외)의 문자열 리터럴을 읽어서:
    //   - 한글이 든 리터럴은 일본어 표(Assets/BugHunt/Lang/Ja)에 키로 있어야 한다. 가나가 함께 든 리터럴(메뉴 이름처럼 두 언어를 나란히 쓴 것)은 제외.
    //   - 보간 문자열($"...")의 글자 부분에는 한글을 쓰지 않는다. 대신 Loc.F("... {0} ...", 값).
    // 표 쪽은: 일본어가 비어 있지 않고, 한글이 남아 있지 않고, 자리 표시자 {0} {1} 가 같고, 코드에서 쓰지 않는 키가 없어야 한다.
    //
    // Assets/BugHunt 以下の .cs (Lang/ と Tests/ を除く) の文字列リテラルを読んで:
    //   - ハングルを含むリテラルは日本語の表 (Assets/BugHunt/Lang/Ja) にキーとしてなければなりません。かなも含むリテラル (メニュー名のように 2 言語を並べたもの) は除きます。
    //   - 補間文字列 ($"...") の文字部分にはハングルを書きません。代わりに Loc.F("... {0} ...", 値)。
    // 表の側は: 日本語が空でなく、ハングルが残っておらず、プレースホルダー {0} {1} が同じで、コードで使っていないキーがないこと。
    public class TranslationTests
    {
        struct Literal
        {
            public string File;
            public int Line;
            public string Text;          // 이스케이프를 푼 값 / エスケープを解いた値
            public bool Interpolated;
        }

        static readonly Regex Placeholder = new Regex(@"\{(\d+)(?:[,:][^}]*)?\}");

        static bool HasHangul(string s) => s.Any(c => (c >= '\uAC00' && c <= '\uD7A3') || (c >= '\u3130' && c <= '\u318F'));
        // \uAC00\uB098\uB098 \uD55C\uC790\uAC00 \uD568\uAED8 \uC788\uC73C\uBA74 \uC774\uBBF8 \uB450 \uC5B8\uC5B4\uB97C \uB098\uB780\uD788 \uC4F4 \uBB38\uC790\uC5F4\uC774\uB2E4 / \u304B\u306A\u3084\u6F22\u5B57\u304C\u4E00\u7DD2\u306B\u3042\u308C\u3070\u3001\u3059\u3067\u306B 2 \u8A00\u8A9E\u3092\u4E26\u3079\u305F\u6587\u5B57\u5217\u3067\u3059
        static bool HasKana(string s) => s.Any(c => (c >= '\u3040' && c <= '\u30FF') || (c >= '\u4E00' && c <= '\u9FFF'));

        static string Root => Path.Combine(Application.dataPath, "BugHunt");

        static IEnumerable<string> SourceFiles()
        {
            foreach (string path in Directory.GetFiles(Root, "*.cs", SearchOption.AllDirectories))
            {
                string rel = path.Substring(Root.Length + 1).Replace('\\', '/');
                if (rel.StartsWith("Lang/") || rel.StartsWith("Tests/"))
                {
                    continue;
                }
                yield return path;
            }
        }

        static string Rel(string path) => "Assets/BugHunt/" + path.Substring(Root.Length + 1).Replace('\\', '/');

        [Test]
        public void KoreanLiteralsHaveJapanese()
        {
            IReadOnlyDictionary<string, string> table = Loc.Japanese;
            var problems = new List<string>();
            foreach (Literal lit in AllLiterals())
            {
                if (!HasHangul(lit.Text) || HasKana(lit.Text))
                {
                    continue;
                }
                if (lit.Interpolated)
                {
                    problems.Add($"{lit.File}:{lit.Line}  보간 문자열에 한글 → Loc.F(...) 로 / 補間文字列にハングル → Loc.F(...) に: $\"{lit.Text}\"");
                }
                else if (!table.ContainsKey(lit.Text))
                {
                    problems.Add($"{lit.File}:{lit.Line}  일본어 없음 / 日本語なし: \"{Escape(lit.Text)}\"");
                }
            }
            Assert.IsEmpty(problems, $"번역이 빠진 문자열 {problems.Count}개 / 翻訳が抜けている文字列 {problems.Count} 個\n" + string.Join("\n", problems));
        }

        [Test]
        public void JapaneseTableIsClean()
        {
            var used = new HashSet<string>(AllLiterals().Where(l => !l.Interpolated).Select(l => l.Text));
            var seen = new Dictionary<string, (string Table, string Ja)>();
            var problems = new List<string>();
            foreach (var (tableName, ko, ja) in Loc.JapaneseEntries())
            {
                string where = $"JaText.{tableName}: \"{Escape(ko)}\"";
                if (string.IsNullOrWhiteSpace(ja))
                {
                    problems.Add($"{where}  일본어가 비어 있음 / 日本語が空");
                    continue;
                }
                if (HasHangul(ja))
                {
                    problems.Add($"{where}  일본어에 한글이 남아 있음 / 日本語にハングルが残っている: \"{Escape(ja)}\"");
                }
                if (!SamePlaceholders(ko, ja))
                {
                    problems.Add($"{where}  자리 표시자가 다름 / プレースホルダーが違う: \"{Escape(ja)}\"");
                }
                if (seen.TryGetValue(ko, out var earlier) && earlier.Ja != ja)
                {
                    problems.Add($"{where}  JaText.{earlier.Table} 와 번역이 다름 / JaText.{earlier.Table} と翻訳が違う");
                }
                seen[ko] = (tableName, ja);
                if (!used.Contains(ko))
                {
                    problems.Add($"{where}  코드에서 쓰지 않는 키 / コードで使っていないキー");
                }
            }
            Assert.IsEmpty(problems, $"일본어 표의 문제 {problems.Count}개 / 日本語の表の問題 {problems.Count} 個\n" + string.Join("\n", problems));
        }

        [Test]
        public void EveryLabHasTitleAndSymptomInJapanese()
        {
            var problems = new List<string>();
            foreach (LabRunner.LabEntry entry in LabRunner.Labs)
            {
                if (string.IsNullOrEmpty(entry.Symptom))
                {
                    problems.Add($"랩 {entry.Number}: Symptom 이 없음 / ラボ {entry.Number}: Symptom がない");
                }
                foreach (string ko in new[] { entry.Title, entry.Symptom })
                {
                    if (!string.IsNullOrEmpty(ko) && !Loc.Japanese.ContainsKey(ko))
                    {
                        problems.Add($"랩 {entry.Number}: 일본어 없음 / ラボ {entry.Number}: 日本語なし: \"{ko}\"");
                    }
                }
            }
            foreach (var part in LabRunner.Parts)
            {
                if (!Loc.Japanese.ContainsKey(part.Name))
                {
                    problems.Add($"부 이름 / 部の名前: \"{part.Name}\"");
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        static bool SamePlaceholders(string ko, string ja)
        {
            var a = Placeholder.Matches(ko).Cast<Match>().Select(m => m.Groups[1].Value).Distinct().OrderBy(s => s);
            var b = Placeholder.Matches(ja).Cast<Match>().Select(m => m.Groups[1].Value).Distinct().OrderBy(s => s);
            return a.SequenceEqual(b);
        }

        static string Escape(string s) => s.Replace("\n", "\\n").Replace("\"", "\\\"");

        static IEnumerable<Literal> AllLiterals()
        {
            return SourceFiles().SelectMany(path => Scan(Rel(path), File.ReadAllText(path, Encoding.UTF8)));
        }

        // C# 문자열 리터럴만 골라내는 작은 스캐너. 주석, 문자 리터럴('"'), 보간 구멍 안의 중첩 문자열을 처리한다.
        // C# の文字列リテラルだけを取り出す小さなスキャナー。コメント、文字リテラル ('"')、補間の穴の中の入れ子の文字列を扱います。
        static List<Literal> Scan(string file, string src)
        {
            var result = new List<Literal>();
            int i = 0;
            int line = 1;
            ScanCode(file, src, ref i, ref line, result, stopAtBrace: false);
            return result;
        }

        static void ScanCode(string file, string src, ref int i, ref int line, List<Literal> result, bool stopAtBrace)
        {
            int depth = 0;
            while (i < src.Length)
            {
                char c = src[i];
                char next = i + 1 < src.Length ? src[i + 1] : '\0';
                if (c == '\n')
                {
                    ++line;
                    ++i;
                }
                else if (c == '/' && next == '/')
                {
                    while (i < src.Length && src[i] != '\n')
                    {
                        ++i;
                    }
                }
                else if (c == '/' && next == '*')
                {
                    i += 2;
                    while (i + 1 < src.Length && !(src[i] == '*' && src[i + 1] == '/'))
                    {
                        if (src[i] == '\n')
                        {
                            ++line;
                        }
                        ++i;
                    }
                    i += 2;
                }
                else if (c == '\'')
                {
                    i += 1;
                    while (i < src.Length && src[i] != '\'')
                    {
                        i += src[i] == '\\' ? 2 : 1;
                    }
                    ++i;
                }
                else if (c == '"' || ((c == '$' || c == '@') && (next == '"' || next == '$' || next == '@')))
                {
                    bool interpolated = false, verbatim = false;
                    while (src[i] == '$' || src[i] == '@')
                    {
                        interpolated |= src[i] == '$';
                        verbatim |= src[i] == '@';
                        ++i;
                    }
                    int startLine = line;
                    ++i;   // 여는 " / 開く "
                    string text = ScanString(file, src, ref i, ref line, result, interpolated, verbatim);
                    result.Add(new Literal { File = file, Line = startLine, Text = text, Interpolated = interpolated });
                }
                else if (stopAtBrace && c == '{')
                {
                    ++depth;
                    ++i;
                }
                else if (stopAtBrace && c == '}')
                {
                    if (depth == 0)
                    {
                        return;
                    }
                    --depth;
                    ++i;
                }
                else
                {
                    ++i;
                }
            }
        }

        // 닫는 " 다음까지 읽고, 보간 구멍을 뺀 글자 부분을 돌려준다.
        // 閉じる " の次まで読み、補間の穴を除いた文字部分を返します。
        static string ScanString(string file, string src, ref int i, ref int line, List<Literal> result, bool interpolated, bool verbatim)
        {
            var text = new StringBuilder();
            while (i < src.Length)
            {
                char c = src[i];
                char next = i + 1 < src.Length ? src[i + 1] : '\0';
                if (c == '"')
                {
                    if (verbatim && next == '"')
                    {
                        text.Append('"');
                        i += 2;
                        continue;
                    }
                    ++i;
                    return text.ToString();
                }
                if (c == '\n')
                {
                    ++line;
                }
                if (!verbatim && c == '\\')
                {
                    switch (next)
                    {
                        case 'n': text.Append('\n'); break;
                        case 't': text.Append('\t'); break;
                        case 'r': text.Append('\r'); break;
                        case '0': text.Append('\0'); break;
                        default: text.Append(next); break;
                    }
                    i += 2;
                    continue;
                }
                if (interpolated && c == '{')
                {
                    if (next == '{')
                    {
                        text.Append('{');
                        i += 2;
                        continue;
                    }
                    ++i;
                    ScanCode(file, src, ref i, ref line, result, stopAtBrace: true);
                    ++i;   // 닫는 } / 閉じる }
                    text.Append("{}");
                    continue;
                }
                if (interpolated && c == '}' && next == '}')
                {
                    text.Append('}');
                    i += 2;
                    continue;
                }
                text.Append(c);
                ++i;
            }
            return text.ToString();
        }
    }
}
