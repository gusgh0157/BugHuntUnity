using System;
using System.Collections.Generic;
using UnityEngine;

namespace BugHunt
{
    // 랩 결과 출력. Console 창과 게임 화면(LabRunner) 양쪽에 찍는다.
    //   [BugHunt] [PASS] 이름 (값: 100)
    //   [BugHunt] [FAIL] 이름 (기대: 100, 실제: 70)
    // Console 검색창에 [BugHunt] 를 넣으면 랩 결과만 보인다.
    public static class Report
    {
        public const string Tag = "[BugHunt]";

        public static int Passed { get; private set; }
        public static int Failed { get; private set; }
        public static int Skipped { get; private set; }

        // LabRunner 가 화면에 그리려고 구독한다.
        public static event Action<string, Color> Printed;

        public static void ResetTally()
        {
            Passed = 0;
            Failed = 0;
            Skipped = 0;
        }

        public static bool Expect<T>(string what, T expected, T actual)
        {
            bool ok = EqualityComparer<T>.Default.Equals(expected, actual);
            if (ok)
            {
                Pass($"{what} (값: {Format(actual)})");
            }
            else
            {
                Fail($"{what} (기대: {Format(expected)}, 실제: {Format(actual)})");
            }
            return ok;
        }

        public static bool ExpectNear(string what, float expected, float actual, float tolerance)
        {
            bool ok = Mathf.Abs(expected - actual) <= tolerance;
            if (ok)
            {
                Pass($"{what} (값: {actual:0.###})");
            }
            else
            {
                Fail($"{what} (기대: {expected:0.###} ±{tolerance:0.###}, 실제: {actual:0.###})");
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
            ++Passed;
            Print($"[PASS] {message}", new Color(0.4f, 1f, 0.4f));
        }

        public static void Fail(string message)
        {
            ++Failed;
            Print($"[FAIL] {message}", new Color(1f, 0.4f, 0.4f));
        }

        public static void Skip(string what, string reason)
        {
            ++Skipped;
            Print($"[SKIP] {what} ({reason})", Color.yellow);
        }

        public static void Note(string message)
        {
            Print(message, Color.white);
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
            return value == null ? "null" : value.ToString();
        }
    }
}
