// 랩 48: 체력 표시줄
// 부대원마다 머리 위에 체력 표시줄이 있다. 화면은 멀쩡한데 몇 초마다 프레임이 툭 끊긴다. Profiler 를 보면 GC.Collect 가 튄다.
// 아무도 맞지 않아 표시가 그대로인 프레임에도 쓰레기(GC 할당)가 계속 쌓인다.
// 기대: 값이 바뀌지 않은 프레임에는 GC 할당이 없다(부대 전체 합 프레임당 128바이트 이하). 값이 바뀌면 표시는 바로 바뀐다.
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BugHunt.Labs.Lab48
{
    public class Health : MonoBehaviour
    {
        public int Current = 100;
        public int Max = 100;
    }

    public class Squad : MonoBehaviour
    {
        public List<Health> Members = new List<Health>();
    }

    public class HealthBar : MonoBehaviour
    {
        public Squad Squad;
        public string Text;
        public string SquadText;

        void Update()
        {
            Health health = GetComponent<Health>();
            Text = "HP " + health.Current + " / " + health.Max;

            int alive = Squad.Members.Count(m => m.Current > 0);
            int total = 0;
            IEnumerable<Health> members = Squad.Members;
            foreach (Health member in members)
            {
                total += member.Current;
            }
            SquadText = $"생존 {alive}명, 부대 HP {total}";
        }
    }

    [Lab(48, "체력 표시줄")]
    public class Lab48_HudGarbage : Lab
    {
        public override IEnumerator Run()
        {
            const long Budget = 128;
            int count = Case(20, 28);
            var squad = Spawn<Squad>("Squad");
            var bars = new List<HealthBar>();
            for (int i = 0; i < count; ++i)
            {
                var member = Spawn<Health>($"Member_{i}");
                squad.Members.Add(member);
                var bar = member.gameObject.AddComponent<HealthBar>();
                bar.Squad = squad;
                bar.enabled = false;
                bars.Add(bar);
            }

            var baseline = new GcMeter();
            yield return baseline.Measure(30);
            foreach (var bar in bars)
            {
                bar.enabled = true;
            }
            yield return null;
            yield return null;   // 첫 표시를 만드는 프레임은 뺀다
            var work = new GcMeter();
            yield return work.Measure(30);
            long bytes = System.Math.Max(0, work.BytesPerFrame - baseline.BytesPerFrame);

            Report.Note($"표시줄 {count}개, 값이 그대로인 프레임의 GC 할당: 프레임당 {bytes}바이트 (아무것도 안 할 때 {baseline.BytesPerFrame}바이트)");
            Report.ExpectTrue($"값이 그대로인 프레임의 GC 할당 ≤ {Budget}바이트/프레임", bytes <= Budget);

            int hit = Case(3, 11);
            squad.Members[hit].Current = Case(70, 45);
            squad.Members[0].Current = 0;
            yield return null;
            int expectedTotal = squad.Members.Sum(m => m.Current);
            Report.Expect("맞은 부대원의 표시", $"HP {squad.Members[hit].Current} / 100", bars[hit].Text);
            Report.Expect("부대 표시", $"생존 {count - 1}명, 부대 HP {expectedTotal}", bars[hit].SquadText);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// Window → Analysis → Profiler → CPU Usage → 아래 창을 Hierarchy 로 두고 GC Alloc 열로 정렬하세요. HealthBar.Update 가 프레임마다 몇 바이트를 쓰나요?
// 어느 줄이 쓰는지 모르겠으면 Profiler 위쪽의 Deep Profile 을 켜고 다시 플레이하세요. HealthBar.Update 아래로 String.Concat, Enumerable.Count, 박싱된 열거자가 보입니다(Deep Profile 은 느려지므로 끝나면 끕니다).
// Deep Profile 대신 의심 가는 줄을 ProfilerMarker 로 감싸도 됩니다: static readonly ProfilerMarker s_Text = new ProfilerMarker("HealthBar.Text"); using (s_Text.Auto()) { ... }
#endregion

#region 힌트 2
// 쓰레기를 만드는 것: ① 문자열 + 와 $"" 보간(매번 새 string) ② LINQ(Count(람다) 는 람다가 지역 변수를 안 쓰면 캐시되지만 열거자는 매번 만든다)
// ③ List 를 IEnumerable<T> 로 받아 foreach(List 의 열거자는 struct 인데, 인터페이스로 받으면 박싱되어 힙에 올라간다)
// GetComponent 는 찾았을 때 할당하지 않지만(에디터에서 못 찾으면 할당) 매 프레임 찾을 이유가 없습니다.
// 공통점: 결과가 지난 프레임과 같은데 매 프레임 새로 만든다.
#endregion

#region 힌트 3
// 바뀔 때만 만든다:  if (health.Current != shownHp) { shownHp = health.Current; Text = ...; }   // 부대 표시도 alive, total 이 바뀔 때만
// 컴포넌트는 Awake 에서 한 번: Health health; void Awake() { health = GetComponent<Health>(); }
// LINQ 와 인터페이스 foreach 대신 for:  for (int i = 0; i < Squad.Members.Count; ++i) { var m = Squad.Members[i]; ... }
// UI 텍스트라면 TextMeshPro 의 SetText("HP {0} / {1}", hp, max) 는 할당 없이 숫자를 넣습니다.
#endregion
