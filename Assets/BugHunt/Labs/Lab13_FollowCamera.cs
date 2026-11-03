// 랩 13: 따라오는 카메라
// 카메라가 플레이어를 부드럽게 따라간다. deltaTime 을 곱하지 않는 것도 아닌데(Lerp 비율이라 곱할 데가 없다), 30fps 에서는 카메라가 한참 처지고 120fps 에서는 딱 붙는다.
// 기대: 몇 fps 에서 돌든 0.5초 뒤 카메라와 목표의 남은 거리가 같다.
//       개발 PC(60fps)에서 맞춘 느낌 그대로: 1초에 남은 거리가 e^(-6.32) 배가 되도록(60fps 에서 매 프레임 0.1 씩 다가가는 것과 같음).
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab13
{
    public class FollowCamera : MonoBehaviour
    {
        public Vector3 Target;
        public float Smoothing = 0.1f;   // 매 프레임 남은 거리의 10% 만큼 다가간다

        void LateUpdate()
        {
            transform.position = Vector3.Lerp(transform.position, Target, Smoothing);
        }
    }

    [Lab(13, "따라오는 카메라")]
    public class Lab13_FollowCamera : Lab
    {
        float savedCaptureDeltaTime;

        public override IEnumerator Run()
        {
            savedCaptureDeltaTime = Time.captureDeltaTime;
            const float Sharpness = 6.32f;   // = -ln(0.9) * 60
            float seconds = Case(0.5f, 0.25f);
            float distance = Case(10f, 25f);
            foreach (int fps in Case(new[] { 30, 120 }, new[] { 24, 144 }))
            {
                Time.captureDeltaTime = 1f / fps;
                var cam = Spawn<FollowCamera>($"Camera_{fps}fps");
                yield return null;   // 프레임 간격이 바뀐 뒤부터 잰다
                cam.transform.position = Vector3.zero;
                cam.Target = new Vector3(distance, 0f, 0f);
                int frames = Mathf.RoundToInt(seconds * fps);
                for (int i = 0; i < frames; ++i)
                {
                    yield return null;
                }
                float gap = Vector3.Distance(cam.transform.position, cam.Target);
                float expectedGap = distance * Mathf.Exp(-Sharpness * frames / (float)fps);   // 실제로 흐른 시간 = 프레임 수 × 간격
                Report.ExpectNear($"{fps}fps 에서 {seconds}초 뒤 남은 거리(m)", expectedGap, gap, expectedGap * 0.05f + 0.01f);
                Destroy(cam.gameObject);
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
// LateUpdate 에 추적점(중단하지 않고 출력)을 걸고 "{Time.frameCount} {Vector3.Distance(transform.position, Target)}" 를 찍어 보세요.
// 한 프레임마다 남은 거리는 몇 배가 되나요? 30fps 의 0.5초와 120fps 의 0.5초는 각각 몇 프레임인가요?
#endregion

#region 힌트 2
// Lerp(a, b, 0.1f) 를 매 프레임 하면 남은 거리는 프레임마다 0.9 배 → n 프레임 뒤 0.9^n 배. 시간이 아니라 "프레임 수" 의 함수입니다.
// 30fps 0.5초 = 15프레임 → 0.9^15 ≈ 0.21, 120fps 0.5초 = 60프레임 → 0.9^60 ≈ 0.002.
// 여기에 0.1f * Time.deltaTime 처럼 deltaTime 을 그냥 곱해도 맞지 않습니다(곱셈이 아니라 거듭제곱의 문제라서). 지수 감쇠 공식이 필요합니다.
#endregion

#region 힌트 3
// 1초에 남은 거리가 e^(-k) 배가 되게 하려면, 한 프레임의 비율을 1 - e^(-k * deltaTime) 으로 둡니다.
//   public float Sharpness = 6.32f;   // 60fps 에서 0.1 과 같은 느낌: -ln(1 - 0.1) * 60
//   float t = 1f - Mathf.Exp(-Sharpness * Time.deltaTime);
//   transform.position = Vector3.Lerp(transform.position, Target, t);
// Vector3.SmoothDamp(ref velocity, smoothTime) 도 프레임과 상관없이 동작합니다(다만 곡선 모양이 다르므로 이 랩의 기대값과는 다릅니다).
#endregion
