// 랩 7: 점프
// 점프 버튼을 10번 눌렀는데 캐릭터는 몇 번만 뛴다. 60Hz 모니터에선 덜하고 120Hz 모니터에서 심하다는 제보.
// 기대: 10번 눌러 10번 점프.
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab07
{
    // 테스트용 가짜 입력. Input.GetButtonDown("Jump") 와 같은 규칙: 누른 "그 프레임" 동안만 true.
    public static class GameInput
    {
        static int jumpFrame = -1;

        public static bool JumpDown => Time.frameCount == jumpFrame;

        public static void PressJumpNextFrame()
        {
            jumpFrame = Time.frameCount + 1;
        }
    }

    public class Jumper : MonoBehaviour
    {
        public int Jumps;
        Rigidbody body;

        void Awake()
        {
            body = gameObject.AddComponent<Rigidbody>();
            body.useGravity = false;
        }

        // 물리(Rigidbody)는 FixedUpdate 에서 다룬다.
        void FixedUpdate()
        {
            if (GameInput.JumpDown)
            {
                body.linearVelocity = Vector3.up * 5f;
                ++Jumps;
            }
        }
    }

    [Lab(7, "점프")]
    public class Lab07_Jump : Lab
    {
        float savedCaptureDeltaTime;

        public override IEnumerator Run()
        {
            // 120Hz 모니터처럼 매 프레임 1/120 초가 흐르게 고정한다. (고정 시간 간격은 기본 0.02초 = 50Hz)
            savedCaptureDeltaTime = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 120f;
            Report.Note($"프레임 간격 {Time.captureDeltaTime * 1000f:0.0}ms, FixedUpdate 간격 {Time.fixedDeltaTime * 1000f:0.0}ms");

            var jumper = Spawn<Jumper>("Jumper");
            yield return null;

            int presses = Case(10, 13);
            int gapFrames = Case(7, 5);
            for (int i = 0; i < presses; ++i)
            {
                GameInput.PressJumpNextFrame();
                for (int f = 0; f < gapFrames; ++f)
                {
                    yield return null;
                }
            }

            Report.Expect("점프 횟수", presses, jumper.Jumps);
        }

        void OnDestroy()
        {
            Time.captureDeltaTime = savedCaptureDeltaTime;
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// Jumper 에 Update 를 하나 추가하고, Update 와 FixedUpdate 양쪽에 추적점을 거세요: "{nameof(Update)} {UnityEngine.Time.frameCount}" 처럼.
// 출력 창에서 프레임 번호마다 Update 는 몇 번, FixedUpdate 는 몇 번 찍히나요? 0번인 프레임이 있나요?
#endregion

#region 힌트 2
// GameInput.JumpDown 이 true 인 프레임에 FixedUpdate 가 한 번도 안 돌면 그 점프는 어떻게 되나요?
// 반대로 프레임이 느려서(예: 1/30초) 한 프레임에 FixedUpdate 가 두 번 돌면? Time.captureDeltaTime 을 1f/30f 로 바꿔 실행해 보세요.
// Profiler(Window → Analysis → Profiler)의 CPU 모듈 Hierarchy 에서 FixedUpdate.PhysicsFixedUpdate 의 Calls 열을 프레임마다 보면 0, 1, 2 가 섞여 있습니다.
#endregion

#region 힌트 3
// "이 프레임에 눌렸나" 같은 한 프레임짜리 입력은 Update 에서 읽어 플래그에 저장하고(bool jumpQueued),
// FixedUpdate 에서 그 플래그를 소비(처리 후 false)하세요. Update 는 매 프레임 정확히 한 번, FixedUpdate 는 0~여러 번입니다.
#endregion
