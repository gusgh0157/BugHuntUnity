using System;
using System.Diagnostics;
using UnityEngine;

namespace BugHunt
{
    public enum Language { Korean, Japanese }

    // 출력 문자열의 언어. 코드에는 두 언어를 나란히 쓴다: Loc.L("한국어", "日本語"), Loc.F("한국어 {0}", "日本語 {0}", 값).
    // 出力文字列の言語。コードには 2 つの言語を並べて書きます: Loc.L("한국어", "日本語")、Loc.F("한국어 {0}", "日本語 {0}", 値)。
    //
    // 언어를 고르는 순서: 명령줄 -bughuntLang ko|ja > 랩 실행 창에서 고른 언어(EditorPrefs) > OS 언어가 일본어면 일본어 > 한국어.
    // 言語を選ぶ順序: コマンドライン -bughuntLang ko|ja > ラボ実行ウィンドウで選んだ言語 (EditorPrefs) > OS の言語が日本語なら日本語 > 韓国語。
    //
    // 짝이 없는 한글 문자열은 EditMode 테스트(BugHunt.Tests.Editor.TranslationTests)가 찾아 준다. 여기에는 버그가 없다.
    // 対になっていないハングルの文字列は EditMode テスト (BugHunt.Tests.Editor.TranslationTests) が見つけます。ここにバグはありません。
    public static class Loc
    {
        public const string PrefKey = "BugHunt.Language";
        public const string CommandLineFlag = "-bughuntLang";

        static Language? current;

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

        // 한국어와 일본어 중 지금 언어의 문구를 고른다. 화면에 나오는 문구는 쓰는 자리에서 두 언어를 함께 적는다:
        //   Report.Expect(Loc.L("칸에 그려진 아이콘", "スロットに描かれたアイコン"), ...)
        // 그래서 소스를 읽는 사람은 어느 언어로든 그 줄이 무엇을 찍는지 바로 알 수 있다. 따로 떨어진 번역 표는 없다.
        // 韓国語と日本語のうち、今の言語の文言を選びます。画面に出る文言は、使う場所で 2 つの言語を一緒に書きます:
        //   Report.Expect(Loc.L("칸에 그려진 아이콘", "スロットに描かれたアイコン"), ...)
        // なのでソースを読む人は、どちらの言語でもその行が何を出力するかがすぐ分かります。別に離れた翻訳表はありません。
        [DebuggerStepThrough]
        public static string L(string ko, string ja)
        {
            return IsJapanese ? ja : ko;
        }

        // 자리 표시자({0}, {1:0.0})가 있는 문구. 두 언어의 서식 문자열을 받아 고른 쪽만 string.Format 으로 만든다.
        // 값(args)은 두 언어가 같이 쓰므로 호출하는 자리에서 한 번만 계산된다. 보간 문자열 두 개($"...", $"...")를 쓰면
        // 두 문자열을 모두 만들고 식도 두 번 계산하므로(성능 랩은 할당을 센다) 이렇게 나눈다.
        // (C# 판은 보간 문자열 핸들러로 고른 쪽만 만들지만, Unity 의 C# 9 에는 그 기능이 없다.)
        // 숫자 서식은 예전처럼 CurrentCulture 를 따른다.
        // プレースホルダー ({0}, {1:0.0}) のある文言。2 つの言語の書式文字列を受け取り、選んだ方だけを string.Format で作ります。
        // 値 (args) は 2 つの言語で共用なので、呼び出す場所で一度だけ計算されます。補間文字列を 2 つ ($"...", $"...") 使うと
        // 両方の文字列を作り、式も 2 回計算してしまうので (パフォーマンスのラボはアロケーションを数えます)、こう分けています。
        // (C# 版は補間文字列ハンドラーで選んだ方だけを作りますが、Unity の C# 9 にはその機能がありません。)
        // 数値の書式は以前と同じく CurrentCulture に従います。
        [DebuggerStepThrough]
        public static string F(string koFormat, string jaFormat, params object[] args)
        {
            return string.Format(IsJapanese ? jaFormat : koFormat, args);
        }

        // 한글(음절, 자모)이 들어 있나. 일본어로 채점할 때 출력에 한글이 섞이면 채점기가 경고한다.
        // ハングル (音節、字母) を含むか。日本語で採点するとき出力にハングルが混ざると、採点ツールが警告します。
        public static bool HasHangul(string text)
        {
            if (text == null)
            {
                return false;
            }
            foreach (char c in text)
            {
                if ((c >= '\uAC00' && c <= '\uD7A3') || (c >= '\u1100' && c <= '\u11FF') || (c >= '\u3130' && c <= '\u318F'))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
