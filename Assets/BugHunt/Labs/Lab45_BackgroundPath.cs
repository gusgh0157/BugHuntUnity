// 랩 45: 백그라운드 길 찾기
// 길 찾기는 무거워서 Task.Run 으로 워커 스레드에 넘겼다. 게임은 끊기지 않게 되었는데, 이제 로봇이 목적지로 가지 않는다.
// Console 에는 아무것도 없다. 길 찾기 함수에 중단점을 걸면 분명히 끝까지 계산한다.
// 기대: 길 찾기는 워커 스레드에서 하고, 로봇은 경로의 끝(목적지)에 도착한다.
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BugHunt.Labs.Lab45
{
    public class Robot : MonoBehaviour
    {
        public bool Arrived;
        public int PathThreadId;

        public Task GoTo(Vector3 from, Vector3 goal)
        {
            return Task.Run(() =>
            {
                List<Vector3> path = FindPath(from, goal);
                transform.position = path[path.Count - 1];
                Arrived = true;
            });
        }

        List<Vector3> FindPath(Vector3 from, Vector3 goal)
        {
            PathThreadId = Thread.CurrentThread.ManagedThreadId;
            var path = new List<Vector3>();
            for (int i = 1; i <= 50; ++i)
            {
                path.Add(Vector3.Lerp(from, goal, i / 50f));   // (무거운 계산이라고 치자)
            }
            Thread.Sleep(50);
            return path;
        }
    }

    [Lab(45, "백그라운드 길 찾기")]
    public class Lab45_BackgroundPath : Lab
    {
        public override IEnumerator Run()
        {
            int mainThreadId = Thread.CurrentThread.ManagedThreadId;
            var robot = Spawn<Robot>("Robot");
            Vector3 goal = Case(new Vector3(12f, 0f, 5f), new Vector3(-8f, 0f, 17f));
            yield return null;

            Task task = robot.GoTo(robot.transform.position, goal);
            float deadline = Time.realtimeSinceStartup + 3f;
            while (!robot.Arrived && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Report.ExpectTrue("길 찾기를 워커 스레드에서 함", robot.PathThreadId != 0 && robot.PathThreadId != mainThreadId);
            Report.ExpectTrue("로봇이 도착했다고 알림", robot.Arrived);
            Report.ExpectNear("목적지까지 남은 거리(m)", 0f, Vector3.Distance(robot.transform.position, goal), 0.01f);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// transform.position 줄에 중단점을 걸고 F10 을 눌러 보세요. 다음 줄(Arrived = true)로 가나요? 디버그 → 창 → 스레드 에서 지금 멈춘 스레드가 Main Thread 인가요?
// 랩에서 GoTo 가 돌려준 task 를 조사식에 넣고 Status 와 Exception 을 펼쳐 보세요.
// VS 의 예외 설정에서 Common Language Runtime Exceptions 를 모두 "throw 될 때 중단" 으로 켜면 삼켜지는 예외도 그 자리에서 멈춥니다.
#endregion

#region 힌트 2
// Unity API(transform, GetComponent, Instantiate, Debug.DrawLine ...)는 거의 다 메인 스레드에서만 부를 수 있습니다. 다른 스레드에서 부르면 UnityException "can only be called from the main thread".
// 그런데 Task.Run 안에서 난 예외는 Task 에 담길 뿐 아무도 꺼내 보지 않으면(await, .Result, .Exception) Console 에 나오지 않습니다. 삼켜진 것입니다.
// 이 랩의 Thread.Sleep 처럼 오래 걸리는 계산만 워커로 보내고, 결과를 쓰는 일은 메인 스레드로 돌아와서 해야 합니다.
#endregion

#region 힌트 3
// 계산만 Task.Run 에 넣고, await 뒤에서 결과를 씁니다. Unity 에서 메인 스레드의 await 뒤는 UnitySynchronizationContext 덕분에 다시 메인 스레드입니다.
//   public async Task GoTo(Vector3 from, Vector3 goal)
//   {
//       List<Vector3> path = await Task.Run(() => FindPath(from, goal));
//       transform.position = path[path.Count - 1];
//       Arrived = true;
//   }
// 주의: Task.Run 안에서는 from 처럼 미리 꺼내 둔 값만 쓰고, transform.position 을 읽지도 마세요(읽기도 메인 스레드 전용입니다).
#endregion
