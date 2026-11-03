// 랩 50: 리플레이 빨리감기
// 리플레이 빨리감기는 시뮬레이션을 고정 틱(1/64초)으로 목표 시각까지 돌린다. 개발 중에는 아무 문제가 없었다.
// 그런데 라이브 서버에서 "서버를 일주일 넘게 켜 두면 빨리감기 버튼을 누르는 순간 서버가 멈춘다" 는 제보가 왔다. 재부팅하면 괜찮아진다.
// 기대: 몇 초를 빨리감든 정확히 (초 × 64) 틱을 돌고 바로 끝난다. 서버가 켜진 지 오래되어도 같다.
// (HangGuard.Tick 은 지우지 마세요. Core/HangGuard.cs 참고)
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab50
{
    public class ReplaySimulator
    {
        public const float TickSeconds = 1f / 64f;

        public float SimTime;   // 서버가 켜진 뒤 흐른 시뮬레이션 시간(초)
        public int Ticks;

        public ReplaySimulator(float uptimeSeconds)
        {
            SimTime = uptimeSeconds;
        }

        public void FastForward(float seconds)
        {
            float target = SimTime + seconds;
            while (SimTime < target)
            {
                HangGuard.Tick("ReplaySimulator.FastForward");
                SimTime += TickSeconds;
                ++Ticks;
            }
        }
    }

    [Lab(50, "리플레이 빨리감기")]
    public class Lab50_ReplayFastForward : Lab
    {
        public override IEnumerator Run()
        {
            float seconds = Case(2f, 3.5f);
            int expectedTicks = Mathf.RoundToInt(seconds * 64f);

            var fresh = new ReplaySimulator(Case(90f, 600f));   // 막 켠 서버
            fresh.FastForward(seconds);
            Report.Expect($"켠 지 {fresh.SimTime:0}초 된 서버: {seconds}초 빨리감기 틱 수", expectedTicks, fresh.Ticks);
            yield return null;

            float uptime = Case(7f, 10f) * 24 * 60 * 60;
            Report.Note($"이번에는 켠 지 {uptime / 86400f:0}일({uptime:0}초) 된 서버");
            var old = new ReplaySimulator(uptime);
            old.FastForward(seconds);
            Report.Expect($"켠 지 {uptime / 86400f:0}일 된 서버: {seconds}초 빨리감기 틱 수", expectedTicks, old.Ticks);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// while 안에 조건부 중단점: Ticks > 1000. 멈추면 조사식에 SimTime, target, SimTime + TickSeconds, SimTime + TickSeconds == SimTime 을 넣으세요.
// 한 번 F10 으로 SimTime += TickSeconds 를 넘기고 SimTime 이 바뀌었는지 보세요. 바뀌지 않았다면 반복문은 영원히 끝나지 않습니다.
#endregion

#region 힌트 2
// float 은 유효 숫자가 약 7자리(가수 24비트)입니다. 숫자가 커질수록 "표현할 수 있는 다음 수" 와의 간격이 벌어집니다.
//   2^19 = 524288 초(약 6일) 이상에서 float 의 간격은 1/16 초. 그보다 작은 1/64 초를 더하면 반올림되어 그대로입니다.
// 막 켠 서버(작은 SimTime)에서는 멀쩡했던 이유가 이것입니다. Unity 의 Time.time 도 float 이라서 오래 켜 둔 게임에서 같은 일이 생깁니다
// (그래서 Time.timeAsDouble 이 있습니다).
#endregion

#region 힌트 3
// 큰 값을 작은 값으로 조금씩 누적하는 시간은 정수 틱으로 세고, 시간은 필요할 때 계산하세요:
//   public long TickCount;  public double SimTime => TickCount * (double)TickSeconds;
// 또는 SimTime 과 target 을 double 로. (1/64 는 2 의 거듭제곱이라 double 에서 정확합니다.)
// 반복 종료 조건을 float 의 누적 결과에 걸지 마세요. "몇 번 돌지" 를 먼저 정수로 정하는 것이 가장 안전합니다:
//   int steps = Mathf.RoundToInt(seconds / TickSeconds); for (int i = 0; i < steps; ++i) { ... }
#endregion
