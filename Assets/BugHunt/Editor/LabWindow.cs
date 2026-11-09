using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BugHunt.Editor
{
    // 메뉴 BugHunt → 랩 실행 창. 번호를 누르면 BugHunt 씬을 열고 Play 한 뒤 그 랩을 실행한다.
    // メニュー BugHunt → ラボ実行ウィンドウ。番号を押すと BugHunt シーンを開いて再生し、そのラボを実行します。
    // 메뉴 이름은 Unity 가 번역할 수 없으므로 두 언어를 나란히 쓴다(MenuPaths). 창 안의 글은 고른 언어로 나온다.
    // メニュー名は Unity が翻訳できないので 2 つの言語を並べて書きます (MenuPaths)。ウィンドウ内の文字は選んだ言語で表示されます。
    public class LabWindow : EditorWindow
    {
        const string ScenePath = "Assets/Scenes/BugHunt.unity";
        const string LastLabKey = "BugHunt.LastLab";
        static readonly string[] LanguageNames = { "한국어 (韓国語)", "日本語" };
        Vector2 scroll;

        [MenuItem(MenuPaths.LabWindow + " %&b")]
        static void Open()
        {
            GetWindow<LabWindow>().UpdateTitle();
        }

        [MenuItem(MenuPaths.RunLast + " %&r")]
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

        void OnEnable()
        {
            UpdateTitle();
        }

        void UpdateTitle()
        {
            titleContent = new GUIContent(Loc.L("BugHunt 랩", "BugHunt ラボ"));
        }

        void OnGUI()
        {
            // 언어 / 言語: EditorPrefs 에 기억한다. 명령줄 -bughuntLang 이 있으면 그쪽이 이긴다.
            // EditorPrefs に記憶します。コマンドラインに -bughuntLang があればそちらが優先されます。
            int chosen = EditorGUILayout.Popup("언어 / 言語", (int)Loc.Current, LanguageNames);
            if (chosen != (int)Loc.Current)
            {
                var language = (Language)chosen;
                EditorPrefs.SetString(Loc.PrefKey, Loc.Code(language));
                Loc.Current = language;
                UpdateTitle();
            }

            EditorGUILayout.HelpBox(Loc.L("번호를 누르면 BugHunt 씬에서 Play 하고 그 랩을 실행합니다. 결과는 Console 창(검색: [BugHunt])과 Game 창에 나옵니다.\n같은 랩 다시 실행: Ctrl+Alt+R", "番号を押すと BugHunt シーンで再生してそのラボを実行します。結果はコンソールウィンドウ (検索: [BugHunt]) とゲームビューに出ます。\n同じラボを再実行: Ctrl+Alt+R"), MessageType.Info);
            if (GUILayout.Button(Loc.L("모든 랩 채점 (Ctrl+Alt+G)", "全ラボを採点 (Ctrl+Alt+G)"), GUILayout.Height(22)))
            {
                EditorApplication.ExecuteMenuItem(MenuPaths.Grade);
            }
            scroll = EditorGUILayout.BeginScrollView(scroll);
            string currentPart = null;
            var symptomStyle = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            foreach (LabRunner.LabEntry entry in LabRunner.Labs)
            {
                string part = LabRunner.PartOf(entry.Number);
                if (part != currentPart)
                {
                    currentPart = part;
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField(part, EditorStyles.boldLabel);
                }
                string sound = entry.MakesSound ? Loc.L("  (소리 주의: 볼륨을 낮추세요)", "  (音に注意: 音量を下げてください)") : "";
                if (GUILayout.Button($"{entry.Number}. {entry.Title}{sound}", GUILayout.Height(22)))
                {
                    Run(entry.Number);
                }
                if (!string.IsNullOrEmpty(entry.Symptom))
                {
                    EditorGUILayout.LabelField(entry.Symptom, symptomStyle);
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

    // 메뉴 경로. 한국어 · 일본어 를 나란히 쓴다("/" 는 하위 메뉴가 되므로 " · " 로 나눈다).
    // メニューのパス。韓国語 · 日本語 を並べて書きます ("/" はサブメニューになるので " · " で区切ります)。
    static class MenuPaths
    {
        public const string Root = "BugHunt/";
        public const string LabWindow = Root + "랩 실행 창 · ラボ実行ウィンドウ";
        public const string RunLast = Root + "마지막 랩 다시 실행 · 最後のラボを再実行";
        public const string Grade = Root + "채점 (모든 랩) · 採点 (全ラボ)";
        public const string GradeWindow = Root + "채점 결과 창 · 採点結果ウィンドウ";
        public const string HangGuardLong = Root + "멈춤 랩: 오래 버티기(30초) · フリーズ系ラボ: 30 秒間固まったままにする";
        public const string RebuildAssets = Root + "개발용 · 開発用/랩 에셋 다시 만들기 · ラボのアセットを作り直す";
    }
}
