// 랩 3: 점수판
// 게임 매니저와 점수판 UI 를 같은 프레임에 만들자마자 NullReferenceException.
// 기대: 점수 10 을 얻으면 점수판이 10 을 보여 준다.
// ラボ 3: スコアボード
// ゲームマネージャーとスコアボード UI を同じフレームで作ったとたんに NullReferenceException。
// 期待: スコア 10 を得るとスコアボードが 10 を表示する。
using System;
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab03
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public event Action<int> ScoreChanged;
        public int Score;

        void Start()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void AddScore(int amount)
        {
            Score += amount;
            ScoreChanged?.Invoke(Score);
        }
    }

    public class ScoreboardUI : MonoBehaviour
    {
        public int Shown = -1;

        void OnEnable()
        {
            GameManager.Instance.ScoreChanged += Refresh;
            Refresh(GameManager.Instance.Score);
        }

        void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ScoreChanged -= Refresh;
            }
        }

        void Refresh(int score)
        {
            Shown = score;
        }
    }

    [Lab(3, "점수판", Symptom = "매니저와 UI 를 만들자마자 NullReferenceException")]
    public class Lab03_Scoreboard : Lab
    {
        public override IEnumerator Run()
        {
            var manager = Spawn<GameManager>("GameManager");
            var board = Spawn<ScoreboardUI>("ScoreboardUI");
            yield return null;

            int points = Case(10, 35);
            manager.AddScore(points);
            yield return null;

            Report.Expect("점수판에 보이는 점수", points, board.Shown);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// 스택 트레이스를 아래까지 읽으세요. ScoreboardUI.OnEnable 아래에 UnityEngine.GameObject:AddComponent 가 있고 그 아래에 랩의 Spawn 이 있습니다.
// 즉 OnEnable 은 "다음 프레임"이 아니라 AddComponent 를 부른 바로 그 순간에 불렸습니다.
//
// スタックトレースを下まで読んでください。ScoreboardUI.OnEnable の下に UnityEngine.GameObject:AddComponent があり、その下にラボの Spawn があります。
// つまり OnEnable は「次のフレーム」ではなく、AddComponent を呼んだまさにその瞬間に呼ばれました。
#endregion

#region 힌트 2 / ヒント 2
// GameManager.Awake, OnEnable, Start 와 ScoreboardUI.OnEnable 에 중단점(또는 Debug.Log($"{name} {Time.frameCount}"))을 걸고 순서를 적으세요.
// Instance 가 채워지는 것은 그중 어느 시점인가요? 점수판이 Instance 를 읽는 것은요?
//
// GameManager.Awake、OnEnable、Start と ScoreboardUI.OnEnable にブレークポイント (または Debug.Log($"{name} {Time.frameCount}")) を置いて、順序を書き留めてください。
// Instance が設定されるのはそのうちのどの時点ですか? スコアボードが Instance を読むのは?
#endregion

#region 힌트 3 / ヒント 3
// Unity 의 순서: (오브젝트마다) Awake → OnEnable 은 생성/활성화 즉시, Start 는 그 오브젝트의 첫 Update 직전.
// 남이 쓸 준비(싱글턴 등록, 참조 연결)는 Awake 에서, 남의 것을 가져다 쓰는 일은 Start 에서 한다.
// 같은 Awake/OnEnable 끼리의 오브젝트 간 순서는 믿지 마세요(씬에 놓인 순서, 생성 순서에 따라 바뀝니다).
//
// Unity の順序: (オブジェクトごとに) Awake → OnEnable は生成/有効化の直後、Start はそのオブジェクトの最初の Update の直前。
// 他から使われる準備 (シングルトンの登録、参照の接続) は Awake で、他のものを取ってきて使う処理は Start で行います。
// 同じ Awake/OnEnable 同士のオブジェクト間の順序は信用しないでください (シーンに置いた順序や生成順序によって変わります)。
#endregion
