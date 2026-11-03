// 랩 6: HP 바
// 출혈 중인 영웅의 HP 바가 매 프레임 한 칸씩 늦다. 영웅이 쓰러진 프레임에도 HP 바는 1 을 보여 준다.
// 기대: 매 프레임 HP 바 숫자 == 영웅 HP.
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab06
{
    // 입력 처리 스크립트들 다음에 돌도록 순서를 뒤로 미뤘다.
    [DefaultExecutionOrder(100)]
    public class BleedingHero : MonoBehaviour
    {
        public int Health = 30;

        void Update()
        {
            if (Health > 0)
            {
                --Health;   // 출혈: 매 프레임 1
            }
        }
    }

    public class HealthBar : MonoBehaviour
    {
        public BleedingHero Hero;
        public int Displayed;

        void Update()
        {
            Displayed = Hero.Health;
        }
    }

    // 판정용: 모든 LateUpdate 가 끝난 뒤(= 화면에 그리기 직전) HP 바와 영웅 HP 를 비교한다.
    [DefaultExecutionOrder(10000)]
    public class FrameProbe : MonoBehaviour
    {
        public BleedingHero Hero;
        public HealthBar Bar;
        public int Frames;
        public int Mismatched;
        public int ShownWhenDead = -1;

        void LateUpdate()
        {
            if (Hero.Health > 0 || ShownWhenDead < 0)
            {
                ++Frames;
                if (Bar.Displayed != Hero.Health)
                {
                    if (Mismatched == 0)
                    {
                        Report.Note($"프레임 {Time.frameCount}: 영웅 HP {Hero.Health}, HP 바 {Bar.Displayed}");
                    }
                    ++Mismatched;
                }
                if (Hero.Health == 0)
                {
                    ShownWhenDead = Bar.Displayed;
                }
            }
        }
    }

    [Lab(6, "HP 바")]
    public class Lab06_HealthBar : Lab
    {
        public override IEnumerator Run()
        {
            var hero = Spawn<BleedingHero>("Hero");
            hero.Health = Case(30, 45);
            var bar = Spawn<HealthBar>("HealthBar");
            bar.Hero = hero;
            var probe = Spawn<FrameProbe>("FrameProbe");
            probe.Hero = hero;
            probe.Bar = bar;

            while (probe.ShownWhenDead < 0)
            {
                yield return null;
            }

            Report.Expect($"HP 바가 틀린 프레임 수 ({probe.Frames}프레임 중)", 0, probe.Mismatched);
            Report.Expect("영웅이 쓰러진 프레임의 HP 바", 0, probe.ShownWhenDead);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// 두 Update 에 추적점을 거세요: "{GetType().Name} frame={UnityEngine.Time.frameCount} hp={Health}" (HealthBar 쪽은 Hero.Health).
// 출력 창에서 한 프레임 안의 순서는 "영웅 → HP 바" 인가요, "HP 바 → 영웅" 인가요?
#endregion

#region 힌트 2
// 같은 프레임의 Update 끼리 누가 먼저 도는지는 Script Execution Order 가 정합니다.
// Edit → Project Settings → Script Execution Order 창을 보고, 이 파일에서 순서를 바꾸는 줄도 찾아보세요. 숫자가 작을수록 먼저입니다(기본 0).
#endregion

#region 힌트 3
// "남이 Update 에서 바꾼 값을 화면에 보여 주는" 코드는 LateUpdate 에서 읽으면 같은 프레임의 모든 Update 다음에 돕니다.
// 또는 HealthBar 에 [DefaultExecutionOrder(200)] 처럼 영웅보다 늦은 순서를 주세요. 순서가 중요하면 "우연"에 맡기지 말고 명시합니다.
#endregion
