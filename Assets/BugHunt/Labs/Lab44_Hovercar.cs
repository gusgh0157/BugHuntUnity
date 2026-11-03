// 랩 44: 호버카
// 개발 PC(60fps)에서 초속 6m 로 맞춘 호버카. QA 의 노트북(30fps)에서는 느릿느릿, 게이밍 PC(120fps)에서는 두 배로 빠르다.
// 기대: 몇 fps 에서 돌든 호버카는 1초에 6m 를 간다.
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab44
{
    public class Hovercar : MonoBehaviour
    {
        public float Speed = 0.1f;   // 60fps 에서 1초에 6m

        void Update()
        {
            transform.position += Vector3.forward * Speed;
        }
    }

    [Lab(44, "호버카")]
    public class Lab44_Hovercar : Lab
    {
        float savedCaptureDeltaTime;

        public override IEnumerator Run()
        {
            savedCaptureDeltaTime = Time.captureDeltaTime;
            const float MetersPerSecond = 6f;
            float seconds = Case(2f, 1.5f);
            foreach (int fps in Case(new[] { 30, 120 }, new[] { 24, 144 }))
            {
                Time.captureDeltaTime = 1f / fps;
                var car = Spawn<Hovercar>($"Hovercar_{fps}fps");
                yield return null;   // 첫 Update 전에 시작점을 잡는다
                float startZ = car.transform.position.z;
                int frames = Mathf.RoundToInt(seconds * fps);
                for (int i = 0; i < frames; ++i)
                {
                    yield return null;
                }
                Report.ExpectNear($"{fps}fps 에서 {seconds}초 동안 간 거리(m)", MetersPerSecond * seconds, car.transform.position.z - startZ, 0.2f);
                Destroy(car.gameObject);
            }
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
// Hovercar.Update 에 중단점을 걸고 조사식에 Time.deltaTime, Time.frameCount, transform.position.z 를 넣어 F5 를 몇 번 눌러 보세요.
// 한 번 불릴 때 z 가 얼마나 늘어나나요? 1초에 Update 는 몇 번 불리나요? Profiler 의 CPU 모듈에서 프레임 시간을 보면 1초에 몇 프레임인지 알 수 있습니다.
#endregion

#region 힌트 2
// "한 번 불릴 때 얼마" 로 쓴 값은 "1초에 몇 번 불리나" 에 곱해집니다. Update 가 1초에 몇 번 불릴지는 기기마다, 순간마다 다릅니다(vSync, 발열, 씬 복잡도).
// Time.deltaTime 은 "지난 프레임에서 지금까지 흐른 초" 입니다. 초당 값에 이것을 곱하면 프레임 수와 상관없이 1초에 같은 양이 됩니다.
// Game 뷰의 Stats 창이나 Application.targetFrameRate 로 fps 를 바꿔 직접 재현해 보세요(이 랩은 Time.captureDeltaTime 으로 30/120fps 를 흉내 냅니다).
#endregion

#region 힌트 3
//   public float Speed = 6f;   // 1초에 6m
//   transform.position += Vector3.forward * Speed * Time.deltaTime;
// 단위를 이름이나 주석에 남기세요(MetersPerSecond). "프레임당" 값은 그 자체로 버그 냄새입니다.
#endregion
