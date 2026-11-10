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
        public bool Hung;   // HangGuard 가 "응답 없음" 을 선언했다 (TimedOut 도 true) / HangGuard が「応答なし」を宣言した (TimedOut も true)
        public bool HiddenCase;
        public bool UsedCase;
        public string FirstFailure;
        public string FirstSkip;
        public string FirstException;   // 크래시였다면 처음 난 예외의 메시지 한 줄 / クラッシュだった場合、最初に起きた例外のメッセージ 1 行
        public int ErrorLogs;
    }

    // 랩 실행기. BugHunt 씬에 하나 있고, 자동 테스트도 이걸로 랩을 돌린다. 여기에는 버그가 없다.
    // ラボ実行器。BugHunt シーンに 1 つあり、自動テストもこれでラボを回します。ここにはバグはありません。
    public class LabRunner : MonoBehaviour
    {
        public struct LabEntry
        {
            public int Number;
            public string TitleKo;
            public string TitleJa;
            public string SymptomKo;
            public string SymptomJa;
            public bool MakesSound;
            public Type Type;

            // 지금 언어의 이름과 증상 / 今の言語の名前と症状
            public string Title => Loc.L(TitleKo, TitleJa);
            public string Symptom => Loc.L(SymptomKo, SymptomJa);
        }

        public static string SoundWarning => Loc.L("[주의] 소리가 납니다. 볼륨을 낮추세요", "[注意] 音が出ます。音量を下げてください");
        public const float SoundLabListenerVolume = 0.1f;   // 소리 랩 동안의 전체 볼륨. 클립 자체도 진폭 0.3 이하다 / 音のラボの間の全体音量。クリップ自体も振幅 0.3 以下

        // 부 이름. PartOf 가 지금 언어 쪽을 고른다 / 部の名前。PartOf が今の言語の方を選ぶ
        public static readonly (int First, int Last, string Ko, string Ja)[] Parts =
        {
            (1, 4, "1부: 크래시 / 예외", "第1部: クラッシュ / 例外"),   // ko+ja
            (5, 15, "2부: 흐름 추적", "第2部: 処理の流れの追跡"),   // ko+ja
            (16, 23, "3부: 트랜스폼과 공간", "第3部: トランスフォームと空間"),   // ko+ja
            (24, 30, "4부: 컨테이너", "第4部: コンテナ"),   // ko+ja
            (31, 34, "5부: 잘못된 참조와 네이티브 메모리", "第5部: 不正な参照とネイティブメモリ"),   // ko+ja
            (35, 39, "6부: 물리", "第6部: 物理"),   // ko+ja
            (40, 43, "7부: 사운드", "第7部: サウンド"),   // ko+ja
            (44, 47, "8부: 이미지 / 렌더링", "第8部: 画像 / レンダリング"),   // ko+ja
            (48, 51, "9부: 애니메이션", "第9部: アニメーション"),   // ko+ja
            (52, 58, "10부: 동시성과 멈춤", "第10部: 並行処理とフリーズ"),   // ko+ja
            (59, 63, "11부: 성능", "第11部: パフォーマンス"),   // ko+ja
        };

        static List<LabEntry> labs;

        public static IReadOnlyList<LabEntry> Labs => labs ??= Discover();

        public bool IsRunning { get; private set; }
        public LabResult LastResult { get; private set; }

        readonly List<(string Text, Color Color)> lines = new List<(string, Color)>();
        bool crashed;
        bool labDone;
        int errorLogs;
        string firstException;
        Vector2 listScroll;
        Vector2 logScroll;

        static List<LabEntry> Discover()
        {
            return typeof(LabRunner).Assembly.GetTypes()
                .Where(t => !t.IsAbstract && typeof(Lab).IsAssignableFrom(t))
                .Select(t => (Type: t, Info: t.GetCustomAttribute<LabAttribute>()))
                .Where(x => x.Info != null)
                .Select(x => new LabEntry { Number = x.Info.Number, TitleKo = x.Info.Title, TitleJa = x.Info.TitleJa, SymptomKo = x.Info.Symptom, SymptomJa = x.Info.SymptomJa, MakesSound = x.Info.MakesSound, Type = x.Type })
                .OrderBy(e => e.Number)
                .ToList();
        }

        public static string PartOf(int number)
        {
            foreach (var part in Parts)
            {
                if (number >= part.First && number <= part.Last)
                {
                    return Loc.L(part.Ko, part.Ja);
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
            // エディターのメニュー BugHunt → ラボ実行ウィンドウで選んだラボ
            autoLab = UnityEditor.SessionState.GetInt("BugHunt.AutoLab", 0);
            UnityEditor.SessionState.EraseInt("BugHunt.AutoLab");
#endif
            // 빌드한 플레이어: BugHuntUnity.exe -bugHuntLab 5
            // ビルドしたプレイヤー: BugHuntUnity.exe -bugHuntLab 5
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
                Report.Print(Loc.L("이미 랩이 실행 중입니다.", "すでにラボを実行中です。"), Color.yellow);
                yield break;
            }

            LabEntry entry = Labs.FirstOrDefault(e => e.Number == number);
            if (entry.Type == null)
            {
                Report.Print(Loc.F("랩 {0} 은(는) 없습니다.", "ラボ {0} はありません。", number), Color.yellow);
                yield break;
            }

            IsRunning = true;
            lines.Clear();
            Report.ResetTally();
            crashed = false;
            labDone = false;
            errorLogs = 0;
            firstException = null;
            HangGuard.Reset();
            Lab.UsedCase = false;

            float savedListenerVolume = AudioListener.volume;
            if (entry.MakesSound)
            {
                Report.Print(SoundWarning, Color.yellow);
                AudioListener.volume = SoundLabListenerVolume;   // 모든 소리를 같은 비율로 줄인다(가상 음성 순위는 그대로) / すべての音を同じ比率で下げる (仮想ボイスの順位はそのまま)
            }
            Report.Print(Loc.F("==== 랩 {0}: {1} ====", "==== ラボ {0}: {1} ====", number, entry.Title), Color.cyan);
            if (Lab.HiddenCase)
            {
                Report.Note(Loc.L("(채점: 숨은 테스트 케이스로 실행)", "(採点: 隠しテストケースで実行)"));
            }
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
                Report.Fail(Loc.L("예외로 중단됨: Console 창의 빨간 줄을 클릭해 스택 트레이스를 보세요", "例外で中断: コンソールウィンドウの赤い行をクリックしてスタックトレースを見てください"));
            }
            else if (timedOut && !hung)
            {
                Report.Fail(Loc.F("시간 초과 ({0}초)", "タイムアウト ({0} 秒)", lab.TimeoutSeconds));
            }
            if (errorLogs > 0)
            {
                Report.Fail(Loc.F("랩 도중 Console 에 에러 {0}개", "ラボの途中でコンソールにエラー {0} 個", errorLogs));
            }

            Destroy(root);
            yield return null;   // Destroy 는 프레임 끝에 일어난다. 랩의 OnDestroy 정리가 끝난 뒤 결과를 낸다. / Destroy はフレームの終わりに起きる。ラボの OnDestroy の後始末が終わってから結果を出す。
            AudioListener.volume = savedListenerVolume;

            string skipped = Report.Skipped > 0 ? $" / SKIP {Report.Skipped}" : "";
            Report.Print(Loc.F("==== 랩 {0} 결과: PASS {1} / FAIL {2}{3} ====", "==== ラボ {0} の結果: PASS {1} / FAIL {2}{3} ====", number, Report.Passed, Report.Failed, skipped), Color.cyan);
            LastResult = new LabResult
            {
                Number = number,
                Passed = Report.Passed,
                Failed = Report.Failed,
                Skipped = Report.Skipped,
                Crashed = crashed,
                TimedOut = timedOut,
                Hung = hung,
                HiddenCase = Lab.HiddenCase,
                UsedCase = Lab.UsedCase,
                FirstFailure = Report.FirstFailure,
                FirstSkip = Report.FirstSkip,
                FirstException = firstException,
                ErrorLogs = errorLogs,
            };
            IsRunning = false;
        }

        // 랩 코루틴을 감싸기만 한다. 예외를 잡지 않는다: 잡으면 디버거가 "처리되지 않은 예외"로 멈추지 않는다.
        // ラボのコルーチンを包むだけです。例外はキャッチしません: キャッチするとデバッガーが「ハンドルされない例外」で止まらなくなります。
        IEnumerator Drive(Lab lab)
        {
            yield return lab.Run();
            labDone = true;
        }

        void OnLog(string condition, string stackTrace, LogType type)
        {
            // HangGuard 가 던진 HangException 은 크래시가 아니라 "응답 없음" 이다(이미 [FAIL] 로 셌다).
            // HangGuard が投げた HangException はクラッシュではなく「応答なし」です (すでに [FAIL] として数えました)。
            if (type == LogType.Exception && !HangGuard.Tripped)
            {
                crashed = true;
                firstException ??= condition;
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
            GUILayout.Label(Loc.L("BugHunt Unity: 랩을 고르세요", "BugHunt Unity: ラボを選んでください"));
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
                if (GUILayout.Button($"{entry.Number}. {entry.Title}{(entry.MakesSound ? Loc.L(" (소리 주의)", " (音に注意)") : "")}"))
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
