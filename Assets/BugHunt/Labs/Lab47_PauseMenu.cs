// 랩 47: 일시정지 메뉴
// Esc 를 누르면 Time.timeScale = 0 으로 게임을 멈추고 일시정지 메뉴가 화면 옆에서 미끄러져 들어온다.
// 그런데 메뉴가 들어오지 않는다. "계속하기" 를 누르면 3, 2, 1 카운트다운 뒤 게임이 다시 돌아야 하는데 카운트다운도 멈춰 있다.
// 기대: 게임이 멈춰 있어도 메뉴는 정해진 시간 안에 다 들어오고, 카운트다운이 끝나면 timeScale 이 1 로 돌아온다.
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab47
{
    public class PauseMenu : MonoBehaviour
    {
        public float SlideSeconds = 0.3f;
        public float Shown;          // 0 = 숨김, 1 = 다 들어옴
        public bool Paused;
        public int CountdownLeft;

        public void Pause()
        {
            Paused = true;
            Time.timeScale = 0f;
        }

        public void Resume(int countdown)
        {
            StartCoroutine(Countdown(countdown));
        }

        IEnumerator Countdown(int seconds)
        {
            for (CountdownLeft = seconds; CountdownLeft > 0; --CountdownLeft)
            {
                yield return new WaitForSeconds(1f);
            }
            Paused = false;
            Time.timeScale = 1f;
        }

        void Update()
        {
            float target = Paused ? 1f : 0f;
            Shown = Mathf.MoveTowards(Shown, target, Time.deltaTime / SlideSeconds);
        }
    }

    [Lab(47, "일시정지 메뉴")]
    public class Lab47_PauseMenu : Lab
    {
        public override float TimeoutSeconds => 20f;

        float savedTimeScale;

        public override IEnumerator Run()
        {
            savedTimeScale = Time.timeScale;
            var menu = Spawn<PauseMenu>("PauseMenu");
            menu.SlideSeconds = Case(0.3f, 0.5f);
            yield return null;

            menu.Pause();
            // 진짜 시간으로 기다린다(WaitForSeconds 는 timeScale 0 에서 끝나지 않는다).
            yield return new WaitForSecondsRealtime(menu.SlideSeconds + 0.3f);
            Report.ExpectNear("멈춘 동안 메뉴가 들어온 정도", 1f, menu.Shown, 0.01f);

            int countdown = Case(2, 1);
            menu.Resume(countdown);
            yield return new WaitForSecondsRealtime(countdown + 0.5f);
            Report.Expect("카운트다운이 끝난 뒤 timeScale", 1f, Time.timeScale);
            Report.Expect("카운트다운이 끝난 뒤 남은 숫자", 0, menu.CountdownLeft);
        }

        void OnDestroy()
        {
            Time.timeScale = savedTimeScale;
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// PauseMenu.Update 에 중단점을 걸어 보세요. 멈춘 동안에도 Update 는 불리나요? 불린다면 조사식에 Time.deltaTime, Time.unscaledDeltaTime, Time.timeScale 을 넣어 보세요.
// Countdown 의 yield 줄 다음에 중단점을 걸면 거기까지 오나요?
#endregion

#region 힌트 2
// timeScale = 0 은 Update 를 멈추지 않습니다. Update 는 계속 불리고, Time.deltaTime 이 0 이 될 뿐입니다(FixedUpdate 는 멈춥니다).
// deltaTime 으로 움직이는 것은 모두 멈추고, WaitForSeconds 는 "게임 시간" 으로 기다리므로 영원히 끝나지 않습니다.
// 멈춘 동안 움직여야 하는 것(메뉴, 카운트다운, 일시정지 화면의 애니메이션)은 "진짜 시간" 으로 움직여야 합니다.
#endregion

#region 힌트 3
//   Shown = Mathf.MoveTowards(Shown, target, Time.unscaledDeltaTime / SlideSeconds);
//   yield return new WaitForSecondsRealtime(1f);
// Animator 라면 Update Mode 를 Unscaled Time 으로, 파티클은 Main 모듈의 Use Unscaled Time 을 켭니다.
// 이 랩은 기다리는 동안 진짜 시간을 쓰므로 Time.captureDeltaTime 을 쓰지 않습니다(unscaledDeltaTime 은 captureDeltaTime 을 무시합니다).
#endregion
