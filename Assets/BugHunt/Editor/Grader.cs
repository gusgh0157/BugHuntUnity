using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BugHunt.Editor
{
    // 채점기. 모든 랩을 차례로 돌려 랩마다 PASS / FAIL / TIMEOUT / CRASH / SKIP 을 매기고 grade-report.json 을 쓴다.
    //   - 에디터: 메뉴 BugHunt → 채점 (Ctrl+Alt+G). 끝나면 채점 결과 창이 뜬다.
    //   - 명령줄: grade.bat (안에서 Unity.exe -batchmode -executeMethod BugHunt.Editor.Grader.RunFromCommandLine 을 부른다)
    // 보통 입력으로 PASS 한 랩은 숨은 테스트 케이스(Lab.HiddenCase)로 한 번 더 돌린다. 둘 다 PASS 여야 PASS.
    // JSON 형식은 BugHunt 네 판(C++, C#, 언리얼, 유니티)이 같이 쓴다. 여기에는 버그가 없다.
    [InitializeOnLoad]
    public static class Grader
    {
        public const string Edition = "unity";
        public const int FormatVersion = 1;
        public const string DefaultReportFile = "grade-report.json";

        const string ScenePath = "Assets/Scenes/BugHunt.unity";
        const string ActiveKey = "BugHunt.Grade.Active";
        const string DoneKey = "BugHunt.Grade.Done";
        const string BatchKey = "BugHunt.Grade.Batch";
        const string ReportKey = "BugHunt.Grade.Report";
        const string LabsKey = "BugHunt.Grade.Labs";
        const string ExitCodeKey = "BugHunt.Grade.ExitCode";

        [Serializable]
        public class LabRow
        {
            public int id;
            public string title;
            public int part;
            public string result;
            public long ms;
            public string detail;
        }

        [Serializable]
        public class GradeReport
        {
            public string edition = Edition;
            public int version = FormatVersion;
            public int total;
            public int passed;
            public List<LabRow> labs = new List<LabRow>();

            public int Count(string result) => labs.Count(l => l.result == result);
            public int Gradable => total - Count("SKIP");
            public bool Perfect => Gradable > 0 && passed == Gradable;
        }

        public static string ReportPath => SessionState.GetString(ReportKey, Path.GetFullPath(DefaultReportFile));
        public static bool IsGrading => SessionState.GetBool(ActiveKey, false);

        static Grader()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("BugHunt/채점 (모든 랩) %&g", false, 20)]
        static void GradeFromMenu()
        {
            Begin(false, Path.GetFullPath(DefaultReportFile), "");
        }

        [MenuItem("BugHunt/채점 결과 창", false, 21)]
        static void OpenResults()
        {
            GradeWindow.Open();
        }

        // 명령줄 진입점. 인자(모두 선택):
        //   -bugHuntReport <경로>   결과 JSON 경로 (기본: 프로젝트 폴더의 grade-report.json)
        //   -bugHuntLabs 23,24,50   이 랩만 채점
        // -quit 을 붙이지 마세요. 채점이 끝나면 채점기가 직접 에디터를 끄고, 만점이면 0, 아니면 1 을 종료 코드로 돌려준다.
        public static void RunFromCommandLine()
        {
            string report = Path.GetFullPath(DefaultReportFile);
            string labs = "";
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; ++i)
            {
                if (args[i] == "-bugHuntReport")
                {
                    report = Path.GetFullPath(args[i + 1]);
                }
                else if (args[i] == "-bugHuntLabs")
                {
                    labs = args[i + 1];
                }
            }
            Begin(true, report, labs);
        }

        static void Begin(bool batch, string reportPath, string labs)
        {
            if (EditorApplication.isPlaying || IsGrading)
            {
                EditorUtility.DisplayDialog("BugHunt 채점", "플레이를 멈춘 뒤에 채점하세요.", "확인");
                return;
            }
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(DoneKey, false);
            SessionState.SetBool(BatchKey, batch);
            SessionState.SetString(ReportKey, reportPath);
            SessionState.SetString(LabsKey, labs);
            SessionState.SetInt(ExitCodeKey, 2);
            Debug.Log($"{Report.Tag} [채점] 시작: {(batch ? "명령줄" : "에디터")}, 그래픽 장치 {SystemInfo.graphicsDeviceType}, 결과 → {reportPath}");
            EditorApplication.EnterPlaymode();
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!IsGrading)
            {
                return;
            }
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                var runner = UnityEngine.Object.FindAnyObjectByType<LabRunner>();
                if (runner == null)
                {
                    runner = new GameObject("LabRunner (채점)").AddComponent<LabRunner>();
                }
                runner.StartCoroutine(GradeAll(runner));
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                bool done = SessionState.GetBool(DoneKey, false);
                bool batch = SessionState.GetBool(BatchKey, false);
                int exitCode = SessionState.GetInt(ExitCodeKey, 2);
                SessionState.EraseBool(ActiveKey);
                Lab.HiddenCase = false;
                if (!done)
                {
                    Debug.LogWarning($"{Report.Tag} [채점] 끝나기 전에 플레이가 멈췄습니다. {ReportPath} 에는 거기까지의 결과가 있습니다.");
                }
                if (batch)
                {
                    EditorApplication.Exit(done ? exitCode : 2);
                }
                else
                {
                    GradeWindow.Open();
                }
            }
        }

        static IEnumerator GradeAll(LabRunner runner)
        {
            yield return null;   // 씬의 Start 들이 먼저 돌게 한 프레임 쉰다

            HashSet<int> only = ParseLabs(SessionState.GetString(LabsKey, ""));
            List<LabRunner.LabEntry> labs = LabRunner.Labs.Where(e => only == null || only.Contains(e.Number)).ToList();
            var report = new GradeReport { total = labs.Count };
            float savedBudget = HangGuard.BudgetSeconds;
            HangGuard.BudgetSeconds = HangGuard.DefaultBudgetSeconds;   // "오래 버티기" 를 켜 두었어도 채점은 기본 예산으로

            for (int i = 0; i < labs.Count; ++i)
            {
                LabRunner.LabEntry entry = labs[i];
                // 이 랩에서 에디터가 정말로 굳어 강제 종료되더라도 결과 파일은 남도록, 돌리기 전에 "이 랩에서 멈춤" 으로 미리 써 둔다.
                WriteReport(Provisional(report, labs, i));

                var clock = Stopwatch.StartNew();
                Lab.HiddenCase = false;
                yield return runner.RunLab(entry.Number);
                LabResult normal = runner.LastResult;
                (string result, string detail) = Classify(normal);

                if (result == "PASS" && normal.UsedCase)
                {
                    Lab.HiddenCase = true;
                    yield return runner.RunLab(entry.Number);
                    Lab.HiddenCase = false;
                    (string hiddenResult, string hiddenDetail) = Classify(runner.LastResult);
                    if (hiddenResult != "PASS" && hiddenResult != "SKIP")
                    {
                        result = hiddenResult;
                        detail = $"[숨은 케이스] {hiddenDetail} (보통 입력에서는 통과: 값을 코드에 박아 넣지 않았나요?)";
                    }
                }

                report.labs.Add(new LabRow
                {
                    id = entry.Number,
                    title = entry.Title,
                    part = PartNumber(entry.Number),
                    result = result,
                    ms = clock.ElapsedMilliseconds,
                    detail = detail ?? "",
                });
                if (result == "PASS")
                {
                    ++report.passed;
                }
            }

            HangGuard.BudgetSeconds = savedBudget;
            WriteReport(report);
            PrintSummary(report);
            SessionState.SetInt(ExitCodeKey, report.Perfect ? 0 : 1);
            SessionState.SetBool(DoneKey, true);
            EditorApplication.ExitPlaymode();
        }

        public static (string Result, string Detail) Classify(LabResult r)
        {
            if (r.Crashed)
            {
                return ("CRASH", FirstLine(r.FirstException) ?? "예외로 중단됨");
            }
            if (r.TimedOut)
            {
                return ("TIMEOUT", FirstLine(r.FirstFailure) ?? "시간 초과");
            }
            if (r.Failed > 0)
            {
                return ("FAIL", FirstLine(r.FirstFailure));
            }
            if (r.Skipped > 0)
            {
                return ("SKIP", FirstLine(r.FirstSkip));
            }
            if (r.Passed > 0)
            {
                return ("PASS", "");
            }
            return ("FAIL", "판정이 하나도 나오지 않음");
        }

        static GradeReport Provisional(GradeReport done, List<LabRunner.LabEntry> labs, int current)
        {
            var provisional = new GradeReport { total = done.total, passed = done.passed };
            provisional.labs.AddRange(done.labs);
            for (int i = current; i < labs.Count; ++i)
            {
                provisional.labs.Add(new LabRow
                {
                    id = labs[i].Number,
                    title = labs[i].Title,
                    part = PartNumber(labs[i].Number),
                    result = i == current ? "TIMEOUT" : "FAIL",
                    ms = 0,
                    detail = i == current ? "채점이 이 랩에서 끝나지 않았습니다 (에디터가 멈췄거나 강제로 종료됨)" : "앞 랩에서 채점이 멈춰 실행하지 못함",
                });
            }
            return provisional;
        }

        public static int PartNumber(int lab)
        {
            for (int i = 0; i < LabRunner.Parts.Length; ++i)
            {
                if (lab >= LabRunner.Parts[i].First && lab <= LabRunner.Parts[i].Last)
                {
                    return i + 1;
                }
            }
            return 0;
        }

        static HashSet<int> ParseLabs(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }
            var set = new HashSet<int>();
            foreach (string piece in text.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(piece, out int n))
                {
                    set.Add(n);
                }
            }
            return set;
        }

        static string FirstLine(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }
            int newline = text.IndexOfAny(new[] { '\r', '\n' });
            string line = newline >= 0 ? text.Substring(0, newline) : text;
            return line.Length > 300 ? line.Substring(0, 300) + "…" : line;
        }

        static void WriteReport(GradeReport report)
        {
            string path = ReportPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(report, true) + "\n", new UTF8Encoding(false));
        }

        public static GradeReport LoadReport()
        {
            string path = ReportPath;
            if (!File.Exists(path))
            {
                return null;
            }
            try
            {
                return JsonUtility.FromJson<GradeReport>(File.ReadAllText(path, Encoding.UTF8));
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        static void PrintSummary(GradeReport report)
        {
            var text = new StringBuilder();
            text.AppendLine($"{Report.Tag} [채점] 결과 ({report.edition}, 그래픽 장치 {SystemInfo.graphicsDeviceType})");
            foreach (LabRow row in report.labs)
            {
                text.Append($"  {row.id,2}. {row.title,-12} {row.result,-7} {row.ms,6}ms");
                if (!string.IsNullOrEmpty(row.detail))
                {
                    text.Append($"  {row.detail}");
                }
                text.AppendLine();
            }
            int skipped = report.Count("SKIP");
            text.Append($"총점: {report.passed} / {report.Gradable}");
            if (skipped > 0)
            {
                text.Append($" (SKIP {skipped}개 제외)");
            }
            text.Append($"   FAIL {report.Count("FAIL")}, TIMEOUT {report.Count("TIMEOUT")}, CRASH {report.Count("CRASH")}");
            Debug.Log(text.ToString());
        }
    }
}
