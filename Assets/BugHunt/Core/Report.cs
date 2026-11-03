using System;
using System.Collections.Generic;
using UnityEngine;

namespace BugHunt
{
    // 랩 결과 출력. Console 창과 게임 화면(LabRunner) 양쪽에 찍는다.
    //   [BugHunt] [PASS] 이름 (값: 100)
    //   [BugHunt] [FAIL] 이름 (기대: 100, 실제: 70)
    // Console 검색창에 [BugHunt] 를 넣으면 랩 결과만 보인다.
    // ラボ結果の出力。コンソールウィンドウとゲーム画面 (LabRunner) の両方に出します。
    //   [BugHunt] [PASS] 名前 (値: 100)
    //   [BugHunt] [FAIL] 名前 (期待: 100, 実際: 70)
    // コンソールの検索欄に [BugHunt] と入れるとラボ結果だけが表示されます。
    public static class Report
    {
        public const string Tag = "[BugHunt]";

        public static int Passed { get; private set; }
        public static int Failed { get; private set; }
        public static int Skipped { get; private set; }

        // 이번 랩에서 처음 나온 [FAIL] / [SKIP] 줄. 채점 결과의 detail 에 쓴다.
        // 今回のラボで最初に出た [FAIL] / [SKIP] の行。採点結果の detail に使います。
        public static string FirstFailure { get; private set; }
        public static string FirstSkip { get; private set; }

        // LabRunner 가 화면에 그리려고 구독한다.
        // LabRunner が画面に描くために購読します。
        public static event Action<string, Color> Printed;

        public static void ResetTally()
        {
            Passed = 0;
            Failed = 0;
            Skipped = 0;
            FirstFailure = null;
            FirstSkip = null;
        }

        // what, message, reason 은 한국어 문장(표의 키)이나 이미 번역한 문장(Loc.F 의 결과)을 받는다. 여기서 Loc.T 로 지금 언어로 바꾼다.
        // what, message, reason は韓国語の文 (表のキー) か、翻訳済みの文 (Loc.F の結果) を受け取ります。ここで Loc.T により今の言語に変えます。
        public static bool Expect<T>(string what, T expected, T actual)
        {
            bool ok = EqualityComparer<T>.Default.Equals(expected, actual);
            if (ok)
            {
                PassText(Loc.F("{0} (값: {1})", Loc.T(what), Format(actual)));
            }
            else
            {
                FailText(Loc.F("{0} (기대: {1}, 실제: {2})", Loc.T(what), Format(expected), Format(actual)));
            }
            return ok;
        }

        public static bool ExpectNear(string what, float expected, float actual, float tolerance)
        {
            bool ok = Mathf.Abs(expected - actual) <= tolerance;
            if (ok)
            {
                PassText(Loc.F("{0} (값: {1:0.###})", Loc.T(what), actual));
            }
            else
            {
                FailText(Loc.F("{0} (기대: {1:0.###} ±{2:0.###}, 실제: {3:0.###})", Loc.T(what), expected, tolerance, actual));
            }
            return ok;
        }

        public static bool ExpectTrue(string what, bool condition)
        {
            if (condition)
            {
                Pass(what);
            }
            else
            {
                Fail(what);
            }
            return condition;
        }

        public static void Pass(string message)
        {
            PassText(Loc.T(message));
        }

        public static void Fail(string message)
        {
            FailText(Loc.T(message));
        }

        public static void Skip(string what, string reason)
        {
            ++Skipped;
            string text = $"{Loc.T(what)} ({Loc.T(reason)})";
            FirstSkip ??= text;
            Print($"[SKIP] {text}", Color.yellow);
        }

        public static void Note(string message)
        {
            Print(Loc.T(message), Color.white);
        }

        static void PassText(string text)
        {
            ++Passed;
            Print($"[PASS] {text}", new Color(0.4f, 1f, 0.4f));
        }

        static void FailText(string text)
        {
            ++Failed;
            FirstFailure ??= text;
            Print($"[FAIL] {text}", new Color(1f, 0.4f, 0.4f));
        }

        public static void Print(string message, Color color)
        {
            Debug.Log($"{Tag} {message}");
            Printed?.Invoke(message, color);
        }

        static string Format<T>(T value)
        {
            if (value is UnityEngine.Object obj)
            {
                return obj == null ? "null" : obj.name;
            }
            if (value is string text)
            {
                return Loc.T(text);   // 비교는 원래 값으로 하고, 보여 줄 때만 번역한다 / 比較は元の値で行い、表示するときだけ翻訳します
            }
            return value == null ? "null" : value.ToString();
        }
    }
}
