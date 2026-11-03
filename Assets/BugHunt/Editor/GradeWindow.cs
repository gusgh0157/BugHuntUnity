using UnityEditor;
using UnityEngine;

namespace BugHunt.Editor
{
    // 메뉴 BugHunt → 채점 결과 창. 마지막 grade-report.json 을 표로 보여 준다.
    // メニュー BugHunt → 採点結果ウィンドウ。最後の grade-report.json を表で表示します。
    public class GradeWindow : EditorWindow
    {
        Grader.GradeReport report;
        Vector2 scroll;

        public static void Open()
        {
            var window = GetWindow<GradeWindow>(Loc.T("BugHunt 채점"));
            window.Reload();
            window.Show();
        }

        void OnEnable()
        {
            Reload();
        }

        void Reload()
        {
            report = Grader.LoadReport();
            Repaint();
        }

        void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = !EditorApplication.isPlaying;
                if (GUILayout.Button(Loc.T("다시 채점"), GUILayout.Width(100)))
                {
                    EditorApplication.ExecuteMenuItem(MenuPaths.Grade);
                }
                GUI.enabled = true;
                if (GUILayout.Button(Loc.T("새로 읽기"), GUILayout.Width(80)))
                {
                    Reload();
                }
                if (GUILayout.Button(Loc.T("JSON 위치 열기"), GUILayout.Width(110)))
                {
                    EditorUtility.RevealInFinder(Grader.ReportPath);
                }
            }

            if (report == null)
            {
                EditorGUILayout.HelpBox(Loc.F("아직 채점 결과가 없습니다 ({0}).\n메뉴 BugHunt → 채점 (Ctrl+Alt+G) 을 누르면 모든 랩을 돌려 채점합니다. 1~2분 걸립니다.", Grader.ReportPath), MessageType.Info);
                return;
            }

            int skipped = report.Count("SKIP");
            string score = Loc.F("총점 {0} / {1}", report.passed, report.Gradable) + (skipped > 0 ? Loc.F("  (SKIP {0}개 제외)", skipped) : "");
            EditorGUILayout.LabelField(score, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"FAIL {report.Count("FAIL")}   TIMEOUT {report.Count("TIMEOUT")}   CRASH {report.Count("CRASH")}   SKIP {skipped}");
            EditorGUILayout.Space();

            var resultStyle = new GUIStyle(EditorStyles.boldLabel);
            var detailStyle = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            scroll = EditorGUILayout.BeginScrollView(scroll);
            int currentPart = -1;
            foreach (Grader.LabRow row in report.labs)
            {
                if (row.part != currentPart)
                {
                    currentPart = row.part;
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField(LabRunner.PartOf(row.id), EditorStyles.boldLabel);
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"{row.id}. {TitleOf(row)}", GUILayout.Width(200));
                    resultStyle.normal.textColor = ColorOf(row.result);
                    EditorGUILayout.LabelField(row.result, resultStyle, GUILayout.Width(70));
                    EditorGUILayout.LabelField($"{row.ms}ms", GUILayout.Width(60));
                    EditorGUILayout.LabelField(row.detail, detailStyle);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        // 채점한 뒤 언어를 바꿨어도 이름은 지금 언어로 / 採点後に言語を変えても、名前は今の言語で
        static string TitleOf(Grader.LabRow row)
        {
            foreach (LabRunner.LabEntry entry in LabRunner.Labs)
            {
                if (entry.Number == row.id)
                {
                    return Loc.T(entry.Title);
                }
            }
            return row.title;
        }

        static Color ColorOf(string result)
        {
            switch (result)
            {
                case "PASS": return new Color(0.3f, 0.8f, 0.3f);
                case "SKIP": return new Color(0.8f, 0.7f, 0.2f);
                case "TIMEOUT": return new Color(0.9f, 0.5f, 0.1f);
                default: return new Color(0.9f, 0.3f, 0.3f);
            }
        }
    }
}
