// 랩 15: 일시정지 메뉴
// Esc 를 누르면 Time.timeScale = 0 으로 게임을 멈추고 일시정지 메뉴가 화면 옆에서 미끄러져 들어온다.
// 그런데 메뉴가 들어오지 않는다. "계속하기" 를 누르면 3, 2, 1 카운트다운 뒤 게임이 다시 돌아야 하는데 카운트다운도 멈춰 있다.
// 기대: 게임이 멈춰 있어도 메뉴는 정해진 시간 안에 다 들어오고, 카운트다운이 끝나면 timeScale 이 1 로 돌아온다.
// ラボ 15: ポーズメニュー
// Esc を押すと Time.timeScale = 0 でゲームを止め、ポーズメニューが画面の横からスライドして入ってくる。
// ところがメニューが入ってこない。「続ける」を押すと 3、2、1 のカウントダウンの後にゲームが再開するはずなのに、カウントダウンも止まっている。
// 期待: ゲームが止まっていても、メニューは決められた時間内に入りきり、カウントダウンが終わると timeScale が 1 に戻る。
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab15
{
    public class PauseMenu : MonoBehaviour
    {
        public float SlideSeconds = 0.3f;
        public float Shown;          // 0 = 숨김, 1 = 다 들어옴 / 0 = 非表示、1 = 入りきった
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

    [Lab(15, "일시정지 메뉴", Symptom = "timeScale = 0 에서 메뉴가 안 들어오고 카운트다운이 멈춘다")]
    public class Lab15_PauseMenu : Lab
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
            // 本当の時間で待つ (WaitForSeconds は timeScale 0 では終わらない)。
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
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// PauseMenu.Update 에 중단점을 걸어 보세요. 멈춘 동안에도 Update 는 불리나요? 불린다면 조사식에 Time.deltaTime, Time.unscaledDeltaTime, Time.timeScale 을 넣어 보세요.
// Countdown 의 yield 줄 다음에 중단점을 걸면 거기까지 오나요?
//
// PauseMenu.Update にブレークポイントを置いてみてください。止まっている間も Update は呼ばれますか? 呼ばれるなら、ウォッチ式に Time.deltaTime、Time.unscaledDeltaTime、Time.timeScale を入れてみてください。
// Countdown の yield の行の次にブレークポイントを置くと、そこまで来ますか?
#endregion

#region 힌트 2 / ヒント 2
// timeScale = 0 은 Update 를 멈추지 않습니다. Update 는 계속 불리고, Time.deltaTime 이 0 이 될 뿐입니다(FixedUpdate 는 멈춥니다).
// deltaTime 으로 움직이는 것은 모두 멈추고, WaitForSeconds 는 "게임 시간" 으로 기다리므로 영원히 끝나지 않습니다.
// 멈춘 동안 움직여야 하는 것(메뉴, 카운트다운, 일시정지 화면의 애니메이션)은 "진짜 시간" 으로 움직여야 합니다.
//
// timeScale = 0 は Update を止めません。Update は呼ばれ続け、Time.deltaTime が 0 になるだけです (FixedUpdate は止まります)。
// deltaTime で動くものはすべて止まり、WaitForSeconds は「ゲーム時間」で待つので永遠に終わりません。
// 止まっている間も動くべきもの (メニュー、カウントダウン、ポーズ画面のアニメーション) は「本当の時間」で動かす必要があります。
#endregion

#region 힌트 3 / ヒント 3
//   Shown = Mathf.MoveTowards(Shown, target, Time.unscaledDeltaTime / SlideSeconds);
//   yield return new WaitForSecondsRealtime(1f);
// Animator 라면 Update Mode 를 Unscaled Time 으로, 파티클은 Main 모듈의 Use Unscaled Time 을 켭니다.
// 이 랩은 기다리는 동안 진짜 시간을 쓰므로 Time.captureDeltaTime 을 쓰지 않습니다(unscaledDeltaTime 은 captureDeltaTime 을 무시합니다).
//
//   Shown = Mathf.MoveTowards(Shown, target, Time.unscaledDeltaTime / SlideSeconds);
//   yield return new WaitForSecondsRealtime(1f);
// Animator なら更新モード (Update Mode) をスケールされていない時間 (Unscaled Time) に、パーティクルはメインモジュールの Use Unscaled Time をオンにします。
// このラボは待つ間に本当の時間を使うので、Time.captureDeltaTime を使いません (unscaledDeltaTime は captureDeltaTime を無視します)。
#endregion
