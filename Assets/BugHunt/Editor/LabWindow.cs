using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BugHunt.Editor
{
    // 메뉴 BugHunt → 랩 실행 창. 번호를 누르면 BugHunt 씬을 열고 Play 한 뒤 그 랩을 실행한다.
    public class LabWindow : EditorWindow
    {
        const string ScenePath = "Assets/Scenes/BugHunt.unity";
        const string LastLabKey = "BugHunt.LastLab";
        Vector2 scroll;

        [MenuItem("BugHunt/랩 실행 창 %&b")]
        static void Open()
        {
            GetWindow<LabWindow>("BugHunt 랩");
        }

        [MenuItem("BugHunt/마지막 랩 다시 실행 %&r")]
        static void RunLast()
        {
            int last = EditorPrefs.GetInt(LastLabKey, 0);
            if (last > 0)
            {
                Run(last);
            }
            else
            {
                Open();
            }
        }

        void OnGUI()
        {
            EditorGUILayout.HelpBox("번호를 누르면 BugHunt 씬에서 Play 하고 그 랩을 실행합니다. 결과는 Console 창(검색: [BugHunt])과 Game 창에 나옵니다.\n같은 랩 다시 실행: Ctrl+Alt+R", MessageType.Info);
            if (GUILayout.Button("모든 랩 채점 (Ctrl+Alt+G)", GUILayout.Height(22)))
            {
                EditorApplication.ExecuteMenuItem("BugHunt/채점 (모든 랩)");
            }
            scroll = EditorGUILayout.BeginScrollView(scroll);
            string currentPart = null;
            foreach (LabRunner.LabEntry entry in LabRunner.Labs)
            {
                string part = LabRunner.PartOf(entry.Number);
                if (part != currentPart)
                {
                    currentPart = part;
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField(part, EditorStyles.boldLabel);
                }
                if (GUILayout.Button($"{entry.Number}. {entry.Title}{(entry.MakesSound ? "  (소리 주의: 볼륨을 낮추세요)" : "")}", GUILayout.Height(22)))
                {
                    Run(entry.Number);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        static void Run(int number)
        {
            EditorPrefs.SetInt(LastLabKey, number);
            if (EditorApplication.isPlaying)
            {
                var runner = Object.FindAnyObjectByType<LabRunner>();
                if (runner == null)
                {
                    runner = new GameObject("LabRunner").AddComponent<LabRunner>();
                }
                runner.StartCoroutine(runner.RunLab(number));
                return;
            }

            if (EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return;
                }
                EditorSceneManager.OpenScene(ScenePath);
            }
            SessionState.SetInt("BugHunt.AutoLab", number);
            EditorApplication.EnterPlaymode();
        }
    }
}
