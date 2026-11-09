// 랩 58: 리플레이 빨리감기
// 리플레이 빨리감기는 시뮬레이션을 고정 틱(1/64초)으로 목표 시각까지 돌린다. 개발 중에는 아무 문제가 없었다.
// 그런데 라이브 서버에서 "서버를 일주일 넘게 켜 두면 빨리감기 버튼을 누르는 순간 서버가 멈춘다" 는 제보가 왔다. 재부팅하면 괜찮아진다.
// 기대: 몇 초를 빨리감든 정확히 (초 × 64) 틱을 돌고 바로 끝난다. 서버가 켜진 지 오래되어도 같다.
// (HangGuard.Tick 은 지우지 마세요. Core/HangGuard.cs 참고)
// ラボ 58: リプレイの早送り
// リプレイの早送りは、シミュレーションを固定ティック (1/64 秒) で目標時刻まで回す。開発中は何の問題もなかった。
// ところがライブサーバーで「サーバーを 1 週間以上つけっぱなしにすると、早送りボタンを押した瞬間にサーバーが止まる」という報告が来た。再起動すると直る。
// 期待: 何秒早送りしても、ちょうど (秒 × 64) ティック回ってすぐに終わる。サーバーが起動してから長く経っても同じ。
// (HangGuard.Tick は消さないでください。Core/HangGuard.cs 参照)
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab58
{
    public class ReplaySimulator
    {
        public const float TickSeconds = 1f / 64f;

        public float SimTime;   // 서버가 켜진 뒤 흐른 시뮬레이션 시간(초) / サーバー起動後に流れたシミュレーション時間 (秒)
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

    [Lab(58, "리플레이 빨리감기", "リプレイの早送り", Symptom = "서버를 일주일 넘게 켜 두면 빨리감기에서 멈춘다", SymptomJa = "サーバーを 1 週間以上つけっぱなしにすると早送りで止まる")]
    public class Lab58_ReplayFastForward : Lab
    {
        public override IEnumerator Run()
        {
            float seconds = Case(2f, 3.5f);
            int expectedTicks = Mathf.RoundToInt(seconds * 64f);

            var fresh = new ReplaySimulator(Case(90f, 600f));   // 막 켠 서버 / 起動したばかりのサーバー
            fresh.FastForward(seconds);
            Report.Expect(Loc.F("켠 지 {0:0}초 된 서버: {1}초 빨리감기 틱 수", "起動して {0:0} 秒のサーバー: {1} 秒早送りのティック数", fresh.SimTime, seconds), expectedTicks, fresh.Ticks);
            yield return null;

            float uptime = Case(7f, 10f) * 24 * 60 * 60;
            Report.Note(Loc.F("이번에는 켠 지 {0:0}일({1:0}초) 된 서버", "今度は起動して {0:0} 日 ({1:0} 秒) のサーバー", uptime / 86400f, uptime));
            var old = new ReplaySimulator(uptime);
            old.FastForward(seconds);
            Report.Expect(Loc.F("켠 지 {0:0}일 된 서버: {1}초 빨리감기 틱 수", "起動して {0:0} 日のサーバー: {1} 秒早送りのティック数", uptime / 86400f, seconds), expectedTicks, old.Ticks);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// while 안에 조건부 중단점: Ticks > 1000. 멈추면 조사식에 SimTime, target, SimTime + TickSeconds, SimTime + TickSeconds == SimTime 을 넣으세요.
// 한 번 F10 으로 SimTime += TickSeconds 를 넘기고 SimTime 이 바뀌었는지 보세요. 바뀌지 않았다면 반복문은 영원히 끝나지 않습니다.
//
// while の中に条件付きブレークポイント: Ticks > 1000。止まったらウォッチ式に SimTime, target, SimTime + TickSeconds, SimTime + TickSeconds == SimTime を入れてください。
// F10 で 1 回 SimTime += TickSeconds を通過させて、SimTime が変わったか見てください。変わっていなければ、ループは永遠に終わりません。
#endregion

#region 힌트 2 / ヒント 2
// float 은 유효 숫자가 약 7자리(가수 24비트)입니다. 숫자가 커질수록 "표현할 수 있는 다음 수" 와의 간격이 벌어집니다.
//   2^19 = 524288 초(약 6일) 이상에서 float 의 간격은 1/16 초. 그보다 작은 1/64 초를 더하면 반올림되어 그대로입니다.
// 막 켠 서버(작은 SimTime)에서는 멀쩡했던 이유가 이것입니다. Unity 의 Time.time 도 float 이라서 오래 켜 둔 게임에서 같은 일이 생깁니다
// (그래서 Time.timeAsDouble 이 있습니다).
//
// float の有効桁数は約 7 桁 (仮数 24 ビット) です。数が大きくなるほど「表現できる次の数」との間隔が広がります。
//   2^19 = 524288 秒 (約 6 日) 以上では float の間隔は 1/16 秒。それより小さい 1/64 秒を足すと、丸められて元のままです。
// 起動したばかりのサーバー (小さい SimTime) では問題なかった理由がこれです。Unity の Time.time も float なので、長くつけっぱなしのゲームで同じことが起きます
// (だから Time.timeAsDouble があります)。
#endregion

#region 힌트 3 / ヒント 3
// 큰 값을 작은 값으로 조금씩 누적하는 시간은 정수 틱으로 세고, 시간은 필요할 때 계산하세요:
//   public long TickCount;  public double SimTime => TickCount * (double)TickSeconds;
// 또는 SimTime 과 target 을 double 로. (1/64 는 2 의 거듭제곱이라 double 에서 정확합니다.)
// 반복 종료 조건을 float 의 누적 결과에 걸지 마세요. "몇 번 돌지" 를 먼저 정수로 정하는 것이 가장 안전합니다:
//   int steps = Mathf.RoundToInt(seconds / TickSeconds); for (int i = 0; i < steps; ++i) { ... }
//
// 大きな値に小さな値を少しずつ積み上げる時間は整数のティックで数え、時間は必要なときに計算してください:
//   public long TickCount;  public double SimTime => TickCount * (double)TickSeconds;
// または SimTime と target を double に。(1/64 は 2 のべき乗なので double で正確です。)
// ループの終了条件を float の積み上げ結果にかけないでください。「何回回すか」を先に整数で決めるのが一番安全です:
//   int steps = Mathf.RoundToInt(seconds / TickSeconds); for (int i = 0; i < steps; ++i) { ... }
#endregion
