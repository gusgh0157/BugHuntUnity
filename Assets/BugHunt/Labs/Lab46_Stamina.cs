// 랩 46: 스태미나
// 스태미나는 1초에 20 씩 찬다. 개발 PC 에서는 맞는 것 같았는데, 30fps 노트북에서는 너무 느리게, 120fps 에서는 너무 빨리 찬다.
// 분명히 시간을 곱했다(Time.fixedDeltaTime).
// 기대: 몇 fps 에서 돌든 1초에 20 씩 찬다.
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab46
{
    public class Stamina : MonoBehaviour
    {
        public float Value;
        public float RegenPerSecond = 20f;

        void Update()
        {
            Value += RegenPerSecond * Time.fixedDeltaTime;
        }
    }

    [Lab(46, "스태미나")]
    public class Lab46_Stamina : Lab
    {
        float savedCaptureDeltaTime;

        public override IEnumerator Run()
        {
            savedCaptureDeltaTime = Time.captureDeltaTime;
            float seconds = Case(1.5f, 2.5f);
            foreach (int fps in Case(new[] { 30, 120 }, new[] { 24, 144 }))
            {
                Time.captureDeltaTime = 1f / fps;
                var stamina = Spawn<Stamina>($"Stamina_{fps}fps");
                stamina.RegenPerSecond = Case(20f, 35f);
                yield return null;
                stamina.Value = 0f;
                int frames = Mathf.RoundToInt(seconds * fps);
                for (int i = 0; i < frames; ++i)
                {
                    yield return null;
                }
                Report.ExpectNear($"{fps}fps 에서 {seconds}초 동안 찬 스태미나", stamina.RegenPerSecond * seconds, stamina.Value, 0.5f);
                Destroy(stamina.gameObject);
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
// Update 에 중단점을 걸고 조사식에 Time.fixedDeltaTime 과 Time.deltaTime 을 나란히 넣어 보세요. fps 를 바꿔도 변하는 쪽은 어느 것인가요?
#endregion

#region 힌트 2
// Time.fixedDeltaTime 은 "FixedUpdate 사이의 간격" 설정값(기본 0.02초)입니다. Update 가 몇 번 불리든 늘 같은 값입니다.
// Update 에서 곱하면 "1초에 Update 횟수 × 0.02" 만큼 찹니다. 50fps 일 때만 우연히 맞습니다.
// 반대로 FixedUpdate 안에서 Time.deltaTime 을 읽으면 Unity 가 알아서 fixedDeltaTime 을 돌려줍니다. 그래서 "어디서나 Time.deltaTime" 이 정답입니다.
#endregion

#region 힌트 3
//   Value += RegenPerSecond * Time.deltaTime;
// 물리처럼 고정 간격이 필요한 계산이라면 FixedUpdate 로 옮기고, 그 안에서도 Time.deltaTime(= fixedDeltaTime)을 씁니다.
#endregion
