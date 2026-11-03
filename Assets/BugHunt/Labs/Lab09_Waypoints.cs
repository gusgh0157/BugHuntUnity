// 랩 9: 순찰 경로
// 레벨 디자이너가 웨이포인트를 공중(높이 2.5)에 찍어 두었다. 그래서 시작할 때 바닥 높이로 맞추는데, 순찰병이 공중으로 날아간다.
// 기대: 웨이포인트 3개를 모두 지나고, 순찰병은 한 번도 바닥(높이 0)을 떠나지 않는다.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BugHunt.Labs.Lab09
{
    public class Patrol : MonoBehaviour
    {
        public List<Vector3> Waypoints = new List<Vector3>();
        public float GroundHeight;
        public float Speed = 10f;
        public int Reached;
        public float MaxHeight;

        int index;

        public void SnapWaypointsToGround()
        {
            for (int i = 0; i < Waypoints.Count; ++i)
            {
                Waypoints[i].Set(Waypoints[i].x, GroundHeight, Waypoints[i].z);
            }
        }

        void Update()
        {
            if (index >= Waypoints.Count)
            {
                return;
            }
            Vector3 target = Waypoints[index];
            transform.position = Vector3.MoveTowards(transform.position, target, Speed * Time.deltaTime);
            MaxHeight = Mathf.Max(MaxHeight, transform.position.y);
            if (Vector3.Distance(transform.position, target) < 0.01f)
            {
                ++index;
                ++Reached;
            }
        }
    }

    [Lab(9, "순찰 경로")]
    public class Lab09_Waypoints : Lab
    {
        public override IEnumerator Run()
        {
            var patrol = Spawn<Patrol>("Patrol", Vector3.zero);
            patrol.Waypoints.Add(new Vector3(5, 2.5f, 0));
            patrol.Waypoints.Add(new Vector3(5, 2.5f, 5));
            patrol.Waypoints.Add(new Vector3(0, 2.5f, 5));
            patrol.GroundHeight = 0f;
            patrol.SnapWaypointsToGround();
            Report.Note($"맞춘 뒤 첫 웨이포인트: {patrol.Waypoints[0]}");

            yield return new WaitForSeconds(2.5f);

            Report.Expect("지난 웨이포인트", 3, patrol.Reached);
            Report.ExpectNear("순찰병의 최고 높이", 0f, patrol.MaxHeight, 0.01f);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// 랩 출력의 "맞춘 뒤 첫 웨이포인트" 를 보세요. y 가 0 인가요?
// SnapWaypointsToGround 의 for 안에 중단점을 걸고 F10 으로 한 줄 넘긴 뒤 조사식 Waypoints[i] 를 보세요. 바뀌었나요?
#endregion

#region 힌트 2
// Vector3 는 class 가 아니라 struct(값 형식)입니다. List<Vector3> 의 인덱서 Waypoints[i] 는 칸 안의 값이 아니라 "복사본"을 돌려줍니다.
// 복사본의 Set(...) 을 부르면 복사본만 바뀌고 버려집니다. 컴파일러는 막아 주지 않습니다.
// 비교: Waypoints[i].y = 0; 이라고 쓰면 CS1612 컴파일 에러가 납니다. transform.position.y = 0; 이 안 되는 것과 같은 이유입니다.
#endregion

#region 힌트 3
// 복사해서 고친 뒤 다시 넣으세요:  Vector3 p = Waypoints[i]; p.y = GroundHeight; Waypoints[i] = p;
// (배열 Vector3[] 은 칸을 직접 가리키므로 arr[i].Set(...) 이 동작합니다. 같은 코드가 List 에선 안 됩니다.)
#endregion
