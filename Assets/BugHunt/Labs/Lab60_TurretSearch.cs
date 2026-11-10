// 랩 60: 포탑의 표적 찾기
// 포탑 40대가 매 프레임 가장 가까운 적을 찾는다. 적이 몇 마리 없을 때는 괜찮았는데, 맵에 오브젝트가 많아지자 프레임이 무너지고 GC 가 튄다.
// 기대: 표적 찾기가 프레임마다 쓰레기를 만들지 않고(포탑 전체 합 프레임당 256바이트 이하), 새로 나타난 적도 바로 표적이 된다.
// ラボ 60: タレットの標的探し
// タレット 40 台が毎フレーム一番近い敵を探す。敵が数匹のときは問題なかったが、マップのオブジェクトが増えるとフレームレートが崩れ、GC が跳ねる。
// 期待: 標的探しがフレームごとにゴミを作らず (タレット全体の合計でフレームあたり 256 バイト以下)、新しく現れた敵もすぐに標的になる。
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BugHunt.Labs.Lab60
{
    public class Enemy : MonoBehaviour
    {
    }

    public class Turret : MonoBehaviour
    {
        public Enemy Target;
        public Transform Player;

        void Update()
        {
            Player = GameObject.Find("Player").transform;

            Enemy best = null;
            float bestDistance = float.MaxValue;
            foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            {
                float distance = (enemy.transform.position - transform.position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = enemy;
                }
            }
            Target = best;
        }
    }

    [Lab(60, "포탑의 표적 찾기", "タレットの標的探し", Symptom = "맵에 오브젝트가 많아지자 프레임이 무너지고 GC 가 튄다", SymptomJa = "マップのオブジェクトが増えるとフレームレートが崩れ、GC が跳ねる")]
    public class Lab60_TurretSearch : Lab
    {
        public override IEnumerator Run()
        {
            const long Budget = 256;
            int enemies = Case(150, 220);
            Spawn("Player", new Vector3(0f, 0f, -5f));
            for (int i = 0; i < 1500; ++i)
            {
                Spawn($"Scenery_{i}");   // 나무, 돌, 장식(맵에 흔한 오브젝트) / 木、岩、装飾 (マップによくあるオブジェクト)
            }
            for (int i = 0; i < enemies; ++i)
            {
                Spawn<Enemy>($"Enemy_{i}", new Vector3(20f + (i % 30) * 2f, 0f, 20f + (i / 30) * 2f));
            }
            var turrets = new List<Turret>();
            for (int i = 0; i < 40; ++i)
            {
                var turret = Spawn<Turret>($"Turret_{i}", new Vector3(i * 0.5f, 0f, 0f));
                turret.enabled = false;
                turrets.Add(turret);
            }

            var baseline = new GcMeter();
            yield return baseline.Measure(20);
            foreach (var turret in turrets)
            {
                turret.enabled = true;
            }
            yield return null;
            yield return null;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var work = new GcMeter();
            yield return work.Measure(20);
            stopwatch.Stop();
            long bytes = System.Math.Max(0, work.BytesPerFrame - baseline.BytesPerFrame);

            Report.Note(Loc.F("포탑 40대, 적 {0}마리, 장식 1500개: 프레임당 GC 할당 {1}바이트, 22프레임 {2}ms (참고용)", "タレット 40 台、敵 {0} 体、装飾 1500 個: フレームあたりの GC アロケーション {1} バイト、22 フレーム {2}ms (参考)", enemies, bytes, stopwatch.ElapsedMilliseconds));
            Report.ExpectTrue(Loc.F("표적 찾기의 GC 할당 ≤ {0}바이트/프레임", "標的探しの GC アロケーション ≤ {0} バイト/フレーム", Budget), bytes <= Budget);
            Report.ExpectTrue(Loc.L("포탑이 플레이어를 알고 있음", "タレットがプレイヤーを知っている"), turrets[0].Player != null && turrets[0].Player.name == "Player");

            // 새 적이 포탑 바로 앞에 나타난다.
            // 新しい敵がタレットのすぐ前に現れる。
            var ambusher = Spawn<Enemy>("Ambusher", new Vector3(Case(3f, 7f), 0f, 1f));
            yield return null;
            yield return null;
            Report.Expect(Loc.L("새로 나타난 적을 표적으로", "新しく現れた敵を標的に"), "Ambusher", turrets[Case(6, 14)].Target != null ? turrets[Case(6, 14)].Target.name : null);
            Destroy(ambusher.gameObject);
            yield return null;
            yield return null;
            Report.ExpectTrue(Loc.L("사라진 적은 표적에서 빠짐", "消えた敵は標的から外れた"), turrets[Case(6, 14)].Target != null && turrets[Case(6, 14)].Target.name != "Ambusher");
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// Profiler → CPU Usage → Hierarchy 에서 Turret.Update 를 펼치세요. GameObject.Find 와 FindObjectsByType 의 Time ms 와 GC Alloc 은 얼마인가요? Calls 는 몇 번인가요?
// Timeline 으로 바꾸면 한 프레임 안에서 포탑 40대의 Update 가 줄지어 있는 것이 보입니다.
//
// プロファイラー (Profiler) → CPU Usage → ヒエラルキー (Hierarchy) で Turret.Update を展開してください。GameObject.Find と FindObjectsByType の 時間 (ミリ秒) (Time ms) と GC アロケーション (GC Alloc) はいくつですか? 呼び出し (Calls) は何回ですか?
// タイムライン (Timeline) に切り替えると、1 フレームの中にタレット 40 台の Update が並んでいるのが見えます。
#endregion

#region 힌트 2 / ヒント 2
// GameObject.Find 는 씬의 모든 활성 오브젝트를 이름으로 훑습니다(오브젝트 수에 비례). FindObjectsByType 은 모든 오브젝트를 훑고 결과를 새 배열에 담아 돌려줍니다(매번 할당).
// 이것을 포탑 수 × 매 프레임 합니다. 결과는 거의 늘 같은데요.
// 참고: Camera.main 은 Unity 2020.2 부터 내부에서 캐시되어 예전만큼 비싸지 않습니다. "Camera.main 은 금지" 는 옛 규칙이고, 이런 버그의 진짜 비용은 Find 류입니다.
//
// GameObject.Find はシーンのすべてのアクティブなオブジェクトを名前でなめます (オブジェクト数に比例)。FindObjectsByType はすべてのオブジェクトをなめ、結果を新しい配列に入れて返します (毎回アロケーション)。
// これをタレットの数 × 毎フレーム行います。結果はほとんどいつも同じなのに。
// 参考: Camera.main は Unity 2020.2 から内部でキャッシュされ、昔ほど高くありません。「Camera.main は禁止」は古いルールで、この種のバグの本当のコストは Find 系です。
#endregion

#region 힌트 3 / ヒント 3
// 바뀌지 않는 것은 한 번만 찾고(Awake/Start), 바뀌는 집합은 "등록부" 로 관리합니다.
//   public class Enemy : MonoBehaviour { public static readonly List<Enemy> All = new List<Enemy>(); void OnEnable() { All.Add(this); } void OnDisable() { All.Remove(this); } }
//   void Start() { Player = GameObject.Find("Player").transform; }   // 또는 인스펙터로 연결
//   for (int i = 0; i < Enemy.All.Count; ++i) { ... }               // foreach 도 List 를 직접 돌면 할당 없음
// 등록부로 바꾼 뒤라면 표적 찾기를 몇 프레임마다로 줄이는 것도 실제 게임에서는 방법이지만, 이 랩은 새 적에 2프레임 안에 반응하기를 요구합니다.
// FindObjectsByType 을 둔 채 주기만 늘리는 것은 답이 아닙니다(할당은 그대로입니다).
//
// 変わらないものは 1 回だけ探し (Awake/Start)、変わる集合は「登録簿」で管理します。
//   public class Enemy : MonoBehaviour { public static readonly List<Enemy> All = new List<Enemy>(); void OnEnable() { All.Add(this); } void OnDisable() { All.Remove(this); } }
//   void Start() { Player = GameObject.Find("Player").transform; }   // またはインスペクターでつなぐ
//   for (int i = 0; i < Enemy.All.Count; ++i) { ... }               // foreach も List を直接回せばアロケーションなし
// 登録簿に変えた後なら、標的探しを数フレームごとに減らすのも実際のゲームでは手ですが、このラボは新しい敵に 2 フレーム以内に反応することを求めます。
// FindObjectsByType を残したまま周期だけ延ばすのは答えではありません (アロケーションはそのままです)。
#endregion
