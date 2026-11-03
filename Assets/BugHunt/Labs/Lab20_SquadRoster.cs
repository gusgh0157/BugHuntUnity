// 랩 20: 부대 편성
// 두 부대(Alpha, Bravo)의 병사 명단을 만든다. 쓰레기(GC)를 줄이려고 GetComponentsInChildren 결과를 담을 List 하나를 돌려쓰는데,
// 명단을 다 만들고 보면 두 부대의 명단이 똑같다.
// 기대: 각 부대의 명단에는 그 부대의 병사만 있다.
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BugHunt.Labs.Lab20
{
    public class Soldier : MonoBehaviour
    {
    }

    public static class Roster
    {
        static readonly List<Soldier> results = new List<Soldier>();   // 할당을 줄이려고 돌려쓴다

        public static List<Soldier> SoldiersOf(GameObject unit)
        {
            unit.GetComponentsInChildren(results);   // results 를 비우고 다시 채운다
            return results;
        }
    }

    [Lab(20, "부대 편성")]
    public class Lab20_SquadRoster : Lab
    {
        public override IEnumerator Run()
        {
            int alphaCount = Case(3, 4);
            int bravoCount = Case(2, 5);
            GameObject alpha = Spawn("Alpha");
            GameObject bravo = Spawn("Bravo");
            for (int i = 0; i < alphaCount; ++i)
            {
                Spawn<Soldier>($"Alpha_{i}").transform.SetParent(alpha.transform, false);
            }
            for (int i = 0; i < bravoCount; ++i)
            {
                Spawn<Soldier>($"Bravo_{i}").transform.SetParent(bravo.transform, false);
            }
            yield return null;

            List<Soldier> alphaRoster = Roster.SoldiersOf(alpha);
            List<Soldier> bravoRoster = Roster.SoldiersOf(bravo);

            Report.Expect("Alpha 명단 인원", alphaCount, alphaRoster.Count);
            Report.Expect("Bravo 명단 인원", bravoCount, bravoRoster.Count);
            Report.Expect("Alpha 명단이 모두 Alpha 소속", true, alphaRoster.All(s => s.transform.parent == alpha.transform));
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// bravoRoster 를 만든 직후 조사식에 alphaRoster == bravoRoster 를 넣어 보세요(참조 비교). true 라면 두 변수는 같은 List 를 가리킵니다.
// alphaRoster 줄을 지난 뒤 alphaRoster.Count 를 적어 두고, 한 줄 더 진행한 뒤 다시 보세요. 내가 고치지 않은 변수가 바뀌었습니다.
#endregion

#region 힌트 2
// GetComponentsInChildren(List<T>), GetComponents(List<T>), Physics.RaycastNonAlloc(배열) 같은 "결과를 넘긴 컨테이너에 채우는" API 는
// 시작할 때 그 컨테이너를 비웁니다(배열 버전은 앞에서부터 덮어씁니다). 돌려쓰는 버퍼를 그대로 반환하면 다음 호출이 이전 결과를 지웁니다.
// 같은 버그: static 버퍼를 코루틴 두 개가 함께 쓰기, 이벤트 인자 객체를 재사용하는데 받는 쪽이 보관하기.
#endregion

#region 힌트 3
// 돌려쓰는 버퍼는 함수 안에서만 쓰고, 바깥으로는 복사본을 줍니다.
//   unit.GetComponentsInChildren(results); return new List<Soldier>(results);
// 또는 호출하는 쪽이 자기 List 를 넘기게 합니다(void SoldiersOf(GameObject unit, List<Soldier> into)). 그러면 누가 버퍼의 주인인지 분명해집니다.
#endregion
