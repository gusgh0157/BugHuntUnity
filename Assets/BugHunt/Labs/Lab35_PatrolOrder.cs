// 랩 35: 순찰 순서
// 경비병이 씬에 놓인 순찰 지점을 찾아 순서대로 돈다. 지점마다 Order 번호(0, 1, 2 ...)를 붙여 두었는데, 경비병은 번호와 상관없는 순서로 돈다.
// 어떤 맵에서는 맞게 돌기도 하고, 같은 맵이 에디터와 테스트에서 다르게 돌기도 한다.
// 기대: 경비병은 Order 번호 순서로 돈다.
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BugHunt.Labs.Lab35
{
    public class RoutePoint : MonoBehaviour
    {
        public int Order;
    }

    public class Guard : MonoBehaviour
    {
        public List<RoutePoint> Route = new List<RoutePoint>();

        public void LearnRoute()
        {
            Route = FindObjectsByType<RoutePoint>(FindObjectsSortMode.None).ToList();
        }
    }

    [Lab(35, "순찰 순서")]
    public class Lab35_PatrolOrder : Lab
    {
        public override IEnumerator Run()
        {
            // 레벨 디자이너가 맵마다 지점을 놓은 순서. 번호 순서와 다르다.
            int[][] maps = Case(
                new[] { new[] { 2, 0, 1 }, new[] { 1, 2, 0 }, new[] { 0, 2, 1, 3 } },
                new[] { new[] { 3, 1, 0, 2 }, new[] { 2, 3, 1, 0 }, new[] { 1, 0, 2 } });
            for (int map = 0; map < maps.Length; ++map)
            {
                int[] placed = maps[map];
                var level = Spawn($"Map_{map}");
                for (int i = 0; i < placed.Length; ++i)
                {
                    // 이름은 놓은 순서(A, B, C ...)다. Order 와는 상관없다.
                    var point = Spawn<RoutePoint>($"RoutePoint_{(char)('A' + i)}", new Vector3(placed[i] * 5f, 0f, map * 10f));
                    point.Order = placed[i];
                    point.transform.SetParent(level.transform, true);
                }
                var guard = Spawn<Guard>("Guard");
                guard.transform.SetParent(level.transform, false);
                yield return null;

                guard.LearnRoute();
                string expected = string.Join(",", Enumerable.Range(0, placed.Length));
                Report.Expect($"맵 {map + 1}: 순찰 순서(Order)", expected, string.Join(",", guard.Route.Select(p => p.Order)));

                Destroy(level);
                yield return null;
            }
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// LearnRoute 가 끝난 뒤 조사식에 Route 를 넣고 펼쳐 보세요. 각 원소의 Order 는 몇 번인가요? 그 순서는 어디서 왔을까요?
// Hierarchy 의 순서, 만든 순서, 이름 순서 중 어느 것과도 같다고 믿으면 안 됩니다.
#endregion

#region 힌트 2
// FindObjectsSortMode.None 은 "정렬하지 않음 = 순서 보장 없음" 입니다. 엔진 내부 저장 순서가 그대로 나오고, 에디터·빌드·버전마다 달라질 수 있습니다.
// GetComponents, GetComponentsInChildren, FindObjectsByType(InstanceID 정렬), 이벤트 구독 순서, Dictionary 열거 순서 모두 "게임 규칙의 순서" 가 아닙니다.
// 게임에 의미 있는 순서가 있다면 그 순서를 데이터로 갖고 직접 정렬해야 합니다.
#endregion

#region 힌트 3
//   Route = FindObjectsByType<RoutePoint>(FindObjectsSortMode.None).OrderBy(p => p.Order).ToList();
// 더 좋은 방법은 찾지 않는 것입니다. 경비병에 [SerializeField] RoutePoint[] 을 두고 인스펙터에서 순서대로 끌어다 놓으면 순서가 씬 파일에 저장됩니다.
// FindObjectsSortMode.InstanceID 도 "만든 순서" 를 보장하지 않습니다(씬을 다시 불러오면 ID 가 바뀝니다).
#endregion
