// 랩 48: 엘리베이터
// 승객이 엘리베이터를 부르는 순간 게임 전체가 멈춘다. 에디터도 "응답 없음". 예외도 로그도 없다.
// 엘리베이터는 Update 에서 한 층씩 움직이고, 승객은 코루틴에서 엘리베이터가 도착하기를 기다린다.
// 기대: 승객이 엘리베이터를 타고 목표 층에 내린다. 그동안 게임은 매 프레임 돈다.
// (HangGuard.Tick 은 지우지 마세요. 진짜로 굳는 대신 "응답 없음" 을 알리고 빠져나오게 하는 안전장치입니다. Core/HangGuard.cs 참고)
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab48
{
    public class Elevator : MonoBehaviour
    {
        public int Floor;
        public int TargetFloor;
        public float SecondsPerFloor = 0.05f;
        float travelled;

        public bool IsAt(int floor) => Floor == floor && TargetFloor == floor;

        public void Call(int floor)
        {
            TargetFloor = floor;
        }

        void Update()
        {
            if (Floor == TargetFloor)
            {
                return;
            }
            travelled += Time.deltaTime;
            if (travelled >= SecondsPerFloor)
            {
                travelled = 0f;
                Floor += TargetFloor > Floor ? 1 : -1;
            }
        }
    }

    public class Passenger : MonoBehaviour
    {
        public Elevator Elevator;
        public int Floor;
        public bool Arrived;

        public IEnumerator RideTo(int destination)
        {
            Elevator.Call(Floor);
            while (!Elevator.IsAt(Floor))   // 엘리베이터가 내 층에 올 때까지
            {
                HangGuard.Tick("Passenger.RideTo: 엘리베이터가 오기를 기다림");
            }

            yield return new WaitForSeconds(0.1f);   // 타는 시간
            Elevator.Call(destination);
            while (!Elevator.IsAt(destination))   // 목표 층에 닿을 때까지
            {
                HangGuard.Tick("Passenger.RideTo: 목표 층에 닿기를 기다림");
            }

            Floor = destination;
            Arrived = true;
        }
    }

    [Lab(48, "엘리베이터")]
    public class Lab48_Elevator : Lab
    {
        public override IEnumerator Run()
        {
            var elevator = Spawn<Elevator>("Elevator");
            elevator.Floor = Case(5, 8);
            elevator.TargetFloor = elevator.Floor;
            var passenger = Spawn<Passenger>("Passenger");
            passenger.Elevator = elevator;
            passenger.Floor = Case(1, 3);
            int destination = Case(9, 0);
            yield return null;

            int startFrame = Time.frameCount;
            passenger.StartCoroutine(passenger.RideTo(destination));
            float until = Time.time + 3f;
            while (!passenger.Arrived && Time.time < until)
            {
                yield return null;
            }

            Report.ExpectTrue("승객이 도착함", passenger.Arrived);
            Report.Expect("승객이 있는 층", destination, passenger.Floor);
            Report.ExpectTrue($"기다리는 동안 게임이 계속 돎 ({Time.frameCount - startFrame}프레임)", Time.frameCount - startFrame > 10);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// 진짜로 에디터가 굳었을 때 하는 일: IDE 에서 Unity 에 디버거를 붙이고 "모두 중단"(VS: Ctrl+Alt+Break, 디버그 → 모두 중단 / Rider: Pause) 을 누릅니다.
// 그리고 스레드 창(VS: Ctrl+Alt+H)에서 Main Thread 를 골라 호출 스택을 봅니다. 그 맨 위가 메인 스레드가 갇힌 곳입니다.
// 이 랩에서는 HangGuard 가 2초 뒤 같은 정보를 예외 스택 트레이스로 보여 줍니다. 오래 붙잡아 두고 직접 "모두 중단" 해 보려면
// 메뉴 BugHunt → 멈춤 랩: 오래 버티기(30초) 를 켜세요.
#endregion

#region 힌트 2
// 갇힌 while 의 조건은 Elevator.Floor 에 달려 있습니다. Floor 는 누가 바꾸나요? Elevator.Update 입니다.
// Update 는 언제 불리나요? 이 프레임의 코루틴이 돌려주고(yield) 나서, 다음 프레임입니다.
// 코루틴은 스레드가 아닙니다. yield return 을 만날 때까지는 그냥 메인 스레드에서 도는 보통 함수입니다.
// while 안에 yield 가 없으면 그 while 은 이 프레임 안에서 영원히 돕니다. Update 차례는 오지 않습니다.
#endregion

#region 힌트 3
// 기다리는 반복문마다 한 프레임씩 양보하세요:
//   while (!Elevator.IsAt(Floor)) { HangGuard.Tick(...); yield return null; }
// 또는 한 줄로:  yield return new WaitUntil(() => Elevator.IsAt(Floor));
// 같은 함정: Update 안에서 while 로 기다리기, Start 에서 while (!loaded) 로 기다리기, 코루틴의 for 안에 yield 를 빠뜨리기.
#endregion
