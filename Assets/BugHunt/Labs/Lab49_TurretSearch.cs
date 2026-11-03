// 랩 49: 포탑의 표적 찾기
// 포탑 40대가 매 프레임 가장 가까운 적을 찾는다. 적이 몇 마리 없을 때는 괜찮았는데, 맵에 오브젝트가 많아지자 프레임이 무너지고 GC 가 튄다.
// 기대: 표적 찾기가 프레임마다 쓰레기를 만들지 않고(포탑 전체 합 프레임당 256바이트 이하), 새로 나타난 적도 바로 표적이 된다.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BugHunt.Labs.Lab49
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

    [Lab(49, "포탑의 표적 찾기")]
    public class Lab49_TurretSearch : Lab
    {
        public override IEnumerator Run()
        {
            const long Budget = 256;
            int enemies = Case(150, 220);
            Spawn("Player", new Vector3(0f, 0f, -5f));
            for (int i = 0; i < 1500; ++i)
            {
                Spawn($"Scenery_{i}");   // 나무, 돌, 장식(맵에 흔한 오브젝트)
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

            Report.Note($"포탑 40대, 적 {enemies}마리, 장식 1500개: 프레임당 GC 할당 {bytes}바이트, 22프레임 {stopwatch.ElapsedMilliseconds}ms (참고용)");
            Report.ExpectTrue($"표적 찾기의 GC 할당 ≤ {Budget}바이트/프레임", bytes <= Budget);
            Report.ExpectTrue("포탑이 플레이어를 알고 있음", turrets[0].Player != null && turrets[0].Player.name == "Player");

            // 새 적이 포탑 바로 앞에 나타난다.
            var ambusher = Spawn<Enemy>("Ambusher", new Vector3(Case(3f, 7f), 0f, 1f));
            yield return null;
            yield return null;
            Report.Expect("새로 나타난 적을 표적으로", "Ambusher", turrets[Case(6, 14)].Target != null ? turrets[Case(6, 14)].Target.name : null);
            Destroy(ambusher.gameObject);
            yield return null;
            yield return null;
            Report.ExpectTrue("사라진 적은 표적에서 빠짐", turrets[Case(6, 14)].Target != null && turrets[Case(6, 14)].Target.name != "Ambusher");
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// Profiler → CPU Usage → Hierarchy 에서 Turret.Update 를 펼치세요. GameObject.Find 와 FindObjectsByType 의 Time ms 와 GC Alloc 은 얼마인가요? Calls 는 몇 번인가요?
// Timeline 으로 바꾸면 한 프레임 안에서 포탑 40대의 Update 가 줄지어 있는 것이 보입니다.
#endregion

#region 힌트 2
// GameObject.Find 는 씬의 모든 활성 오브젝트를 이름으로 훑습니다(오브젝트 수에 비례). FindObjectsByType 은 모든 오브젝트를 훑고 결과를 새 배열에 담아 돌려줍니다(매번 할당).
// 이것을 포탑 수 × 매 프레임 합니다. 결과는 거의 늘 같은데요.
// 참고: Camera.main 은 Unity 2020.2 부터 내부에서 캐시되어 예전만큼 비싸지 않습니다. "Camera.main 은 금지" 는 옛 규칙이고, 이런 버그의 진짜 비용은 Find 류입니다.
#endregion

#region 힌트 3
// 바뀌지 않는 것은 한 번만 찾고(Awake/Start), 바뀌는 집합은 "등록부" 로 관리합니다.
//   public class Enemy : MonoBehaviour { public static readonly List<Enemy> All = new List<Enemy>(); void OnEnable() { All.Add(this); } void OnDisable() { All.Remove(this); } }
//   void Start() { Player = GameObject.Find("Player").transform; }   // 또는 인스펙터로 연결
//   for (int i = 0; i < Enemy.All.Count; ++i) { ... }               // foreach 도 List 를 직접 돌면 할당 없음
// 표적 찾기를 매 프레임 할 필요가 없다면 0.2초마다로 줄이는 것도 방법입니다.
#endregion
