using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BugHunt
{
    public enum Language { Korean, Japanese }

    // 출력 문자열의 언어. 코드에는 한국어 문장을 그대로 쓰고, 그 문장이 일본어 표(Assets/BugHunt/Lang/Ja/LabNN.cs)의 키다.
    // 出力文字列の言語。コードには韓国語の文をそのまま書き、その文が日本語の表 (Assets/BugHunt/Lang/Ja/LabNN.cs) のキーになります。
    //
    // 언어를 고르는 순서: 명령줄 -bughuntLang ko|ja > 랩 실행 창에서 고른 언어(EditorPrefs) > OS 언어가 일본어면 일본어 > 한국어.
    // 言語を選ぶ順序: コマンドライン -bughuntLang ko|ja > ラボ実行ウィンドウで選んだ言語 (EditorPrefs) > OS の言語が日本語なら日本語 > 韓国語。
    //
    // 빠진 번역은 EditMode 테스트(BugHunt.Tests.Editor.TranslationTests)가 찾아 준다. 여기에는 버그가 없다.
    // 抜けている翻訳は EditMode テスト (BugHunt.Tests.Editor.TranslationTests) が見つけます。ここにバグはありません。
    public static class Loc
    {
        public const string PrefKey = "BugHunt.Language";
        public const string CommandLineFlag = "-bughuntLang";

        static Language? current;
        static Dictionary<string, string> japanese;

        public static event Action Changed;

        public static Language Current
        {
            get => current ??= Resolve();
            set
            {
                current = value;
                Changed?.Invoke();
            }
        }

        public static bool IsJapanese => Current == Language.Japanese;

        // 랩 코드가 MonoBehaviour 생성자나 워커 스레드에서 처음 부르면 EditorPrefs·systemLanguage 를 읽을 수 없으므로 미리 정해 둔다.
        // ラボのコードが MonoBehaviour のコンストラクターやワーカースレッドで初めて呼ぶと EditorPrefs・systemLanguage を読めないので、先に決めておきます。
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitOnLoad()
        {
            current = Resolve();
        }

        public static Language Resolve()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; ++i)
            {
                if (string.Equals(args[i], CommandLineFlag, StringComparison.OrdinalIgnoreCase) && TryParse(args[i + 1], out Language fromArgs))
                {
                    return fromArgs;
                }
            }
#if UNITY_EDITOR
            if (TryParse(UnityEditor.EditorPrefs.GetString(PrefKey, ""), out Language saved))
            {
                return saved;
            }
#endif
            return Application.systemLanguage == SystemLanguage.Japanese ? Language.Japanese : Language.Korean;
        }

        public static bool TryParse(string text, out Language language)
        {
            switch ((text ?? "").Trim().ToLowerInvariant())
            {
                case "ko": case "kr": case "korean": language = Language.Korean; return true;
                case "ja": case "jp": case "japanese": language = Language.Japanese; return true;
                default: language = Language.Korean; return false;
            }
        }

        public static string Code(Language language) => language == Language.Japanese ? "ja" : "ko";

        // 한국어 문장 → 지금 언어. 표에 없으면 한국어 그대로.
        // 韓国語の文 → 今の言語。表になければ韓国語のまま。
        public static string T(string korean)
        {
            if (korean == null || Current == Language.Korean)
            {
                return korean;
            }
            return Japanese.TryGetValue(korean, out string ja) ? ja : korean;
        }

        // 자리 표시자({0}, {1:0.0})가 있는 한국어 문장. 보간 문자열($"...") 대신 쓴다: 보간하면 표의 키가 매번 달라진다.
        // プレースホルダー ({0}, {1:0.0}) のある韓国語の文。補間文字列 ($"...") の代わりに使います。補間するとキーが毎回変わってしまいます。
        public static string F(string koreanFormat, params object[] args)
        {
            return string.Format(T(koreanFormat), args);
        }

        public static IReadOnlyDictionary<string, string> Japanese => japanese ??= BuildJapanese();

        // JaText 의 (string Ko, string Ja)[] 필드를 모두 모은다. 필드 하나가 랩 하나(Lab05)거나 Core/Editor 공용이다.
        // JaText の (string Ko, string Ja)[] フィールドをすべて集めます。フィールド 1 つがラボ 1 つ (Lab05)、または Core/Editor 共用です。
        public static IEnumerable<(string Table, string Ko, string Ja)> JapaneseEntries()
        {
            foreach (FieldInfo field in typeof(JaText).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (field.GetValue(null) is ValueTuple<string, string>[] rows)
                {
                    foreach (var (ko, ja) in rows)
                    {
                        yield return (field.Name, ko, ja);
                    }
                }
            }
        }

        static Dictionary<string, string> BuildJapanese()
        {
            var table = new Dictionary<string, string>();
            foreach (var (_, ko, ja) in JapaneseEntries())
            {
                if (!string.IsNullOrEmpty(ko) && !string.IsNullOrEmpty(ja))
                {
                    table[ko] = ja;
                }
            }
            return table;
        }
    }

    // 일본어 표. 파일은 Assets/BugHunt/Lang/Ja/ 에 랩마다 하나(LabNN.cs)와 공용 하나(Core.cs).
    // 日本語の表。ファイルは Assets/BugHunt/Lang/Ja/ にラボごとに 1 つ (LabNN.cs) と共用の 1 つ (Core.cs)。
    public static partial class JaText
    {
    }
}
