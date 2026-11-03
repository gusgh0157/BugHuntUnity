// 랩 5: 전투 기록 창
// 창을 다시 열면 기록이 두 줄씩 찍히고, 창이 닫혀 있을 때 맞은 피해까지 알림이 뜬다.
// 기대: 창이 열려 있을 때 받은 10, 7 만 알림 (2번). 다시 연 창에는 7 한 줄. 창을 파괴한 뒤에는 알림 없음.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BugHunt.Labs.Lab05
{
    public class Fighter : MonoBehaviour
    {
        public int Health = 100;
        public event Action<Fighter, int> Damaged;

        public void TakeHit(int damage)
        {
            Health -= damage;
            Damaged?.Invoke(this, damage);
        }
    }

    // 전투 기록 창. 매번 새로 만들지 않고 SetActive 로 켜고 끈다(오브젝트 풀링).
    public class CombatLogPanel : MonoBehaviour
    {
        public Fighter Target;
        public List<string> Lines = new List<string>();
        public int NotificationsShown;

        void OnEnable()
        {
            Lines.Clear();
            if (Target != null)
            {
                Target.Damaged += HandleDamaged;
            }
        }

        void HandleDamaged(Fighter victim, int damage)
        {
            Lines.Add($"{victim.name}: -{damage}");
            ++NotificationsShown;
        }
    }

    [Lab(5, "전투 기록 창")]
    public class Lab05_CombatLog : Lab
    {
        public override IEnumerator Run()
        {
            var knight = Spawn<Fighter>("Knight");
            int[] hits = Case(new[] { 10, 5, 7, 3 }, new[] { 6, 9, 4, 2 });

            GameObject panelObject = Spawn("CombatLogPanel");
            panelObject.SetActive(false);
            var panel = panelObject.AddComponent<CombatLogPanel>();
            panel.Target = knight;

            panelObject.SetActive(true);
            knight.TakeHit(hits[0]);
            panelObject.SetActive(false);

            knight.TakeHit(hits[1]);      // 창이 닫혀 있을 때 맞은 피해는 기록하지 않는다.

            panelObject.SetActive(true);
            knight.TakeHit(hits[2]);

            foreach (string line in panel.Lines)
            {
                Report.Note($"전투 기록: {line}");
            }
            Report.Expect("다시 연 창의 기록 줄 수", 1, panel.Lines.Count);
            Report.Expect($"띄운 알림 총합 ({hits[0]}, {hits[2]})", 2, panel.NotificationsShown);

            Destroy(panelObject);   // 전투 종료: 창을 버린다
            yield return null;
            knight.TakeHit(hits[3]);
            Report.Expect("창을 파괴한 뒤에도 알림 총합", 2, panel.NotificationsShown);
            Report.Expect("기사 HP", 100 - hits[0] - hits[1] - hits[2] - hits[3], knight.Health);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// 크래시가 없습니다. HandleDamaged 첫 줄에 중단점을 걸고, 멈출 때마다 지역 창의 damage 를 적고 F5(계속).
// 기대: 10, 7 (두 번). 실제로는 몇 번, 어떤 값으로 멈추나요? 그때 panelObject 는 켜져 있었나요? (조사식: this.isActiveAndEnabled)
#endregion

#region 힌트 2
// 중단점을 우클릭 → 작업(Actions)으로 바꿔서 "HandleDamaged {damage} {Time.frameCount}" 를 출력하고 "실행 계속"을 체크하면 멈추지 않고 순서가 남습니다(추적점).
// Fighter.TakeHit 의 Invoke 줄에서 조사식에 Damaged.GetInvocationList().Length 를 넣어 보세요. 처음, 끈 뒤, 다시 켠 뒤, 파괴한 뒤를 비교하세요.
// 파괴한 뒤에도 불린다는 것은? C# 이벤트는 Unity 오브젝트의 수명을 모릅니다. 대리자가 this 를 붙잡고 있을 뿐입니다.
#endregion

#region 힌트 3
// OnEnable 에서 구독(+=)했다면 짝이 되는 OnDisable 에서 해제(-=)해야 합니다. OnDisable 은 SetActive(false) 때도, Destroy 때도 불립니다.
// (Awake/Start 에서 구독했다면 짝은 OnDestroy.)
#endregion
