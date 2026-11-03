using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BugHunt
{
    public struct LabResult
    {
        public int Number;
        public int Passed;
        public int Failed;
        public int Skipped;
        public bool Crashed;
        public bool TimedOut;
        public bool Hung;   // HangGuard 가 "응답 없음" 을 선언했다 (TimedOut 도 true)
        public int ErrorLogs;
    }

    // 랩 실행기. BugHunt 씬에 하나 있고, 자동 테스트도 이걸로 랩을 돌린다. 여기에는 버그가 없다.
    public class LabRunner : MonoBehaviour
    {
        public struct LabEntry
        {
            public int Number;
            public string Title;
            public Type Type;
        }

        public static readonly (int First, int Last, string Name)[] Parts =
        {
            (1, 4, "1부: 크래시 / 예외"),
            (5, 11, "2부: 흐름 추적"),
            (12, 15, "3부: 사운드"),
            (16, 19, "4부: 이미지 / 렌더링"),
            (20, 23, "5부: 애니메이션"),
            (24, 27, "6부: 잘못된 참조와 네이티브 메모리"),
            (28, 31, "7부: 멈춤 (무한루프와 데드락)"),
        };

        static List<LabEntry> labs;

        public static IReadOnlyList<LabEntry> Labs => labs ??= Discover();

        public bool IsRunning { get; private set; }
        public LabResult LastResult { get; private set; }

        readonly List<(string Text, Color Color)> lines = new List<(string, Color)>();
        bool crashed;
        bool labDone;
        int errorLogs;
        Vector2 listScroll;
        Vector2 logScroll;

        static List<LabEntry> Discover()
        {
            return typeof(LabRunner).Assembly.GetTypes()
                .Where(t => !t.IsAbstract && typeof(Lab).IsAssignableFrom(t))
                .Select(t => (Type: t, Info: t.GetCustomAttribute<LabAttribute>()))
                .Where(x => x.Info != null)
                .Select(x => new LabEntry { Number = x.Info.Number, Title = x.Info.Title, Type = x.Type })
                .OrderBy(e => e.Number)
                .ToList();
        }

        public static string PartOf(int number)
        {
            foreach (var part in Parts)
            {
                if (number >= part.First && number <= part.Last)
                {
                    return part.Name;
                }
            }
            return "";
        }

        void OnEnable()
        {
            Report.Printed += OnPrinted;
        }

        void OnDisable()
        {
            Report.Printed -= OnPrinted;
        }

        void Start()
        {
            int autoLab = 0;
#if UNITY_EDITOR
            // 에디터 메뉴 BugHunt → 랩 실행 창에서 고른 랩
            autoLab = UnityEditor.SessionState.GetInt("BugHunt.AutoLab", 0);
            UnityEditor.SessionState.EraseInt("BugHunt.AutoLab");
#endif
            // 빌드한 플레이어: BugHuntUnity.exe -bugHuntLab 5
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; ++i)
            {
                if (args[i] == "-bugHuntLab" && int.TryParse(args[i + 1], out int n))
                {
                    autoLab = n;
                }
            }
            if (autoLab > 0)
            {
                StartCoroutine(RunLab(autoLab));
            }
        }

        public IEnumerator RunLab(int number)
        {
            if (IsRunning)
            {
                Report.Print("이미 랩이 실행 중입니다.", Color.yellow);
                yield break;
            }

            LabEntry entry = Labs.FirstOrDefault(e => e.Number == number);
            if (entry.Type == null)
            {
                Report.Print($"랩 {number} 은(는) 없습니다.", Color.yellow);
                yield break;
            }

            IsRunning = true;
            lines.Clear();
            Report.ResetTally();
            crashed = false;
            labDone = false;
            errorLogs = 0;
            HangGuard.Reset();

            Report.Print($"==== 랩 {number}: {entry.Title} ====", Color.cyan);
            Application.logMessageReceived += OnLog;

            var root = new GameObject($"Lab{number:00}");
            var lab = (Lab)root.AddComponent(entry.Type);
            Coroutine drive = StartCoroutine(Drive(lab));
            float deadline = Time.realtimeSinceStartup + lab.TimeoutSeconds;
            while (!labDone && !crashed && !HangGuard.Tripped && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            bool hung = HangGuard.Tripped;
            bool timedOut = hung || (!labDone && !crashed);

            if (!labDone && drive != null)
            {
                StopCoroutine(drive);
            }
            Application.logMessageReceived -= OnLog;

            if (crashed)
            {
                Report.Fail("예외로 중단됨: Console 창의 빨간 줄을 클릭해 스택 트레이스를 보세요");
            }
            else if (timedOut && !hung)
            {
                Report.Fail($"시간 초과 ({lab.TimeoutSeconds}초)");
            }
            if (errorLogs > 0)
            {
                Report.Fail($"랩 도중 Console 에 에러 {errorLogs}개");
            }

            Destroy(root);
            yield return null;   // Destroy 는 프레임 끝에 일어난다. 랩의 OnDestroy 정리가 끝난 뒤 결과를 낸다.

            string skipped = Report.Skipped > 0 ? $" / SKIP {Report.Skipped}" : "";
            Report.Print($"==== 랩 {number} 결과: PASS {Report.Passed} / FAIL {Report.Failed}{skipped} ====", Color.cyan);
            LastResult = new LabResult
            {
                Number = number,
                Passed = Report.Passed,
                Failed = Report.Failed,
                Skipped = Report.Skipped,
                Crashed = crashed,
                TimedOut = timedOut,
                Hung = hung,
                ErrorLogs = errorLogs,
            };
            IsRunning = false;
        }

        // 랩 코루틴을 감싸기만 한다. 예외를 잡지 않는다: 잡으면 디버거가 "처리되지 않은 예외"로 멈추지 않는다.
        IEnumerator Drive(Lab lab)
        {
            yield return lab.Run();
            labDone = true;
        }

        void OnLog(string condition, string stackTrace, LogType type)
        {
            // HangGuard 가 던진 HangException 은 크래시가 아니라 "응답 없음" 이다(이미 [FAIL] 로 셌다).
            if (type == LogType.Exception && !HangGuard.Tripped)
            {
                crashed = true;
            }
            else if (type == LogType.Error || type == LogType.Assert)
            {
                ++errorLogs;
            }
        }

        void OnPrinted(string text, Color color)
        {
            lines.Add((text, color));
            logScroll.y = float.MaxValue;
        }

        void OnGUI()
        {
            const float listWidth = 260f;
            GUILayout.BeginArea(new Rect(10, 10, listWidth, Screen.height - 20), GUI.skin.box);
            GUILayout.Label("BugHunt Unity: 랩을 고르세요");
            listScroll = GUILayout.BeginScrollView(listScroll);
            string currentPart = null;
            foreach (LabEntry entry in Labs)
            {
                string part = PartOf(entry.Number);
                if (part != currentPart)
                {
                    currentPart = part;
                    GUILayout.Label(part);
                }
                GUI.enabled = !IsRunning;
                if (GUILayout.Button($"{entry.Number}. {entry.Title}"))
                {
                    StartCoroutine(RunLab(entry.Number));
                }
                GUI.enabled = true;
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(20 + listWidth, 10, Screen.width - listWidth - 30, Screen.height - 20), GUI.skin.box);
            logScroll = GUILayout.BeginScrollView(logScroll);
            var style = new GUIStyle(GUI.skin.label) { wordWrap = true };
            foreach (var (text, color) in lines)
            {
                style.normal.textColor = color;
                GUILayout.Label(text, style);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
