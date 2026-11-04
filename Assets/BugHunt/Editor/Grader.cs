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
    // 採点ツール。すべてのラボを順に回し、ラボごとに PASS / FAIL / TIMEOUT / CRASH / SKIP を付けて grade-report.json を書きます。
    //   - エディター: メニュー BugHunt → 採点 (全ラボ) (Ctrl+Alt+G)。終わると採点結果ウィンドウが開きます。
    //   - コマンドライン: grade.bat (中で Unity.exe -batchmode -executeMethod BugHunt.Editor.Grader.RunFromCommandLine を呼びます)
    // 通常の入力で PASS したラボは、隠しテストケース (Lab.HiddenCase) でもう一度回します。両方 PASS なら PASS。
    // JSON 形式は BugHunt の 4 つの版 (C++、C#、Unreal、Unity) で共通です。ここにはバグはありません。
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

        [MenuItem(MenuPaths.Grade + " %&g", false, 20)]
        static void GradeFromMenu()
        {
            Begin(false, Path.GetFullPath(DefaultReportFile), "");
        }

        [MenuItem(MenuPaths.GradeWindow, false, 21)]
        static void OpenResults()
        {
            GradeWindow.Open();
        }

        // 명령줄 진입점. 인자(모두 선택):
        //   -bugHuntReport <경로>   결과 JSON 경로 (기본: 프로젝트 폴더의 grade-report.json)
        //   -bugHuntLabs 31,32,58   이 랩만 채점
        //   -bughuntLang ja         출력 언어(ko / ja). 결과 JSON 의 title·detail 도 이 언어로, result 값과 키는 그대로
        // コマンドラインの入口。引数 (すべて任意):
        //   -bugHuntReport <パス>    結果 JSON のパス (既定: プロジェクトフォルダーの grade-report.json)
        //   -bugHuntLabs 31,32,58   このラボだけ採点
        //   -bughuntLang ja         出力言語 (ko / ja)。結果 JSON の title・detail もこの言語で、result の値とキーはそのまま
        // -quit 을 붙이지 마세요. 채점이 끝나면 채점기가 직접 에디터를 끄고, 만점이면 0, 아니면 1 을 종료 코드로 돌려준다.
        // -quit を付けないでください。採点が終わると採点ツールが自分でエディターを終了し、満点なら 0、そうでなければ 1 を終了コードとして返します。
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
                EditorUtility.DisplayDialog(Loc.T("BugHunt 채점"), Loc.T("플레이를 멈춘 뒤에 채점하세요."), Loc.T("확인"));
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
            Debug.Log($"{Report.Tag} " + Loc.F("[채점] 시작: {0}, 그래픽 장치 {1}, 결과 → {2}", Loc.T(batch ? "명령줄" : "에디터"), SystemInfo.graphicsDeviceType, reportPath));
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
                    runner = new GameObject("LabRunner (Grader)").AddComponent<LabRunner>();
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
                    Debug.LogWarning($"{Report.Tag} " + Loc.F("[채점] 끝나기 전에 플레이가 멈췄습니다. {0} 에는 거기까지의 결과가 있습니다.", ReportPath));
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
            yield return null;   // 씬의 Start 들이 먼저 돌게 한 프레임 쉰다 / シーンの Start が先に回るよう 1 フレーム待つ

            HashSet<int> only = ParseLabs(SessionState.GetString(LabsKey, ""));
            List<LabRunner.LabEntry> labs = LabRunner.Labs.Where(e => only == null || only.Contains(e.Number)).ToList();
            var report = new GradeReport { total = labs.Count };
            float savedBudget = HangGuard.BudgetSeconds;
            HangGuard.BudgetSeconds = HangGuard.DefaultBudgetSeconds;   // "오래 버티기" 를 켜 두었어도 채점은 기본 예산으로 / 「30 秒間固まったままにする」をオンにしていても、採点は既定の予算で

            for (int i = 0; i < labs.Count; ++i)
            {
                LabRunner.LabEntry entry = labs[i];
                // 이 랩에서 에디터가 정말로 굳어 강제 종료되더라도 결과 파일은 남도록, 돌리기 전에 "이 랩에서 멈춤" 으로 미리 써 둔다.
                // このラボでエディターが本当に固まって強制終了されても結果ファイルが残るよう、回す前に「このラボで停止」としてあらかじめ書いておきます。
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
                        detail = Loc.F("[숨은 케이스] {0} (보통 입력에서는 통과: 값을 코드에 박아 넣지 않았나요?)", hiddenDetail);
                    }
                }

                report.labs.Add(new LabRow
                {
                    id = entry.Number,
                    title = Loc.T(entry.Title),
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
                return ("CRASH", FirstLine(r.FirstException) ?? Loc.T("예외로 중단됨"));
            }
            if (r.TimedOut)
            {
                return ("TIMEOUT", FirstLine(r.FirstFailure) ?? Loc.T("시간 초과"));
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
            return ("FAIL", Loc.T("판정이 하나도 나오지 않음"));
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
                    title = Loc.T(labs[i].Title),
                    part = PartNumber(labs[i].Number),
                    result = i == current ? "TIMEOUT" : "FAIL",
                    ms = 0,
                    detail = Loc.T(i == current ? "채점이 이 랩에서 끝나지 않았습니다 (에디터가 멈췄거나 강제로 종료됨)" : "앞 랩에서 채점이 멈춰 실행하지 못함"),
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
            text.AppendLine($"{Report.Tag} " + Loc.F("[채점] 결과 ({0}, 그래픽 장치 {1})", report.edition, SystemInfo.graphicsDeviceType));
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
            text.Append(Loc.F("총점: {0} / {1}", report.passed, report.Gradable));
            if (skipped > 0)
            {
                text.Append(Loc.F(" (SKIP {0}개 제외)", skipped));
            }
            text.Append($"   FAIL {report.Count("FAIL")}, TIMEOUT {report.Count("TIMEOUT")}, CRASH {report.Count("CRASH")}");
            Debug.Log(text.ToString());
        }
    }
}
