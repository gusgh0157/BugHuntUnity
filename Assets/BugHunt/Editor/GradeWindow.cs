using UnityEditor;
using UnityEngine;

namespace BugHunt.Editor
{
    // 메뉴 BugHunt → 채점 결과 창. 마지막 grade-report.json 을 표로 보여 준다.
    public class GradeWindow : EditorWindow
    {
        Grader.GradeReport report;
        Vector2 scroll;

        public static void Open()
        {
            var window = GetWindow<GradeWindow>("BugHunt 채점");
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
                if (GUILayout.Button("다시 채점", GUILayout.Width(100)))
                {
                    EditorApplication.ExecuteMenuItem("BugHunt/채점 (모든 랩)");
                }
                GUI.enabled = true;
                if (GUILayout.Button("새로 읽기", GUILayout.Width(80)))
                {
                    Reload();
                }
                if (GUILayout.Button("JSON 위치 열기", GUILayout.Width(110)))
                {
                    EditorUtility.RevealInFinder(Grader.ReportPath);
                }
            }

            if (report == null)
            {
                EditorGUILayout.HelpBox($"아직 채점 결과가 없습니다 ({Grader.ReportPath}).\n메뉴 BugHunt → 채점 (Ctrl+Alt+G) 을 누르면 모든 랩을 돌려 채점합니다. 1~2분 걸립니다.", MessageType.Info);
                return;
            }

            int skipped = report.Count("SKIP");
            string score = $"총점 {report.passed} / {report.Gradable}" + (skipped > 0 ? $"  (SKIP {skipped}개 제외)" : "");
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
                    EditorGUILayout.LabelField($"{row.id}. {row.title}", GUILayout.Width(200));
                    resultStyle.normal.textColor = ColorOf(row.result);
                    EditorGUILayout.LabelField(row.result, resultStyle, GUILayout.Width(70));
                    EditorGUILayout.LabelField($"{row.ms}ms", GUILayout.Width(60));
                    EditorGUILayout.LabelField(row.detail, detailStyle);
                }
            }
            EditorGUILayout.EndScrollView();
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
