// 랩 23: 손에 든 무기
// 캐릭터가 손을 흔들면 손에 든 횃불이 손을 한 박자 늦게 따라온다. 빠르게 흔들수록 손에서 떨어져 보인다.
// 기대: 매 프레임, 화면에 그려지기 직전에 횃불 위치 == 손 위치.
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab23
{
    public class HeldItem : MonoBehaviour
    {
        public Transform Hand;

        void Update()
        {
            transform.SetPositionAndRotation(Hand.position, Hand.rotation);
        }
    }

    // 판정용: 모든 LateUpdate 가 끝난 뒤(= 렌더링 직전) 두 위치를 비교한다.
    [DefaultExecutionOrder(10000)]
    public class FrameProbe : MonoBehaviour
    {
        public Transform Hand;
        public Transform Item;
        public int Frames;
        public int Mismatched;
        public float WorstGap;
        public float HandTravel;
        Vector3? lastHand;

        void LateUpdate()
        {
            if (lastHand.HasValue)
            {
                HandTravel += Vector3.Distance(lastHand.Value, Hand.position);
            }
            lastHand = Hand.position;

            float gap = Vector3.Distance(Hand.position, Item.position);
            ++Frames;
            if (gap > 0.001f)
            {
                ++Mismatched;
                WorstGap = Mathf.Max(WorstGap, gap);
            }
        }
    }

    [Lab(23, "손에 든 무기")]
    public class Lab23_WeaponAttach : Lab
    {
        float savedCaptureDeltaTime;

        public override IEnumerator Run()
        {
            // 60Hz 화면처럼 매 프레임 1/60 초가 흐르게 고정한다(PC 가 빨라도 손이 프레임마다 눈에 띄게 움직이도록).
            savedCaptureDeltaTime = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;

            GameObject character = Spawn("Character");
            var hand = new GameObject("Hand").transform;
            hand.SetParent(character.transform, false);
            var animator = character.AddComponent<Animator>();
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("BugHunt/Animation/Waver");

            var torch = Spawn<HeldItem>("Torch");
            torch.Hand = hand;
            yield return null;

            var probe = Spawn<FrameProbe>("FrameProbe");
            probe.Hand = hand;
            probe.Item = torch.transform;
            for (int i = 0; i < 40; ++i)
            {
                yield return null;
            }

            Report.Note($"{probe.Frames}프레임 동안 손이 움직인 거리 {probe.HandTravel:0.00}, 어긋난 프레임 {probe.Mismatched}, 가장 크게 벌어진 거리 {probe.WorstGap:0.000}");
            Report.ExpectTrue("손이 실제로 움직였다 (애니메이션이 돌고 있다)", probe.HandTravel > 0.5f);
            Report.Expect("손과 횃불이 어긋난 프레임", 0, probe.Mismatched);
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
// HeldItem.Update 와 FrameProbe.LateUpdate 에 추적점: "{UnityEngine.Time.frameCount} hand={Hand.position.x}".
// 같은 프레임 번호에서 두 곳이 본 손의 x 값이 같나요? 다르다면 그 사이에 누가 손을 움직였나요?
#endregion

#region 힌트 2
// Unity 한 프레임의 순서(일부): FixedUpdate → Update → 코루틴(yield null) → 애니메이션 평가(Animator 가 뼈/Transform 을 씀) → LateUpdate → 렌더링.
// Profiler 의 CPU 모듈 Timeline 보기에서 한 프레임을 펼치면 PreLateUpdate.DirectorUpdateAnimationBegin/End 가 Update 뒤, LateUpdate 앞에 보입니다.
// Update 에서 읽은 손 위치는 "이번 프레임 애니메이션 전", 즉 지난 프레임의 손 위치입니다.
#endregion

#region 힌트 3
// 애니메이션 결과(뼈 위치)를 따라가는 코드(무기 붙이기, 카메라 추적, 조준선, IK 보정)는 LateUpdate 에서 하세요.
// 더 간단하게는 횃불을 손의 자식으로 붙이면(SetParent(Hand)) Transform 계층이 알아서 따라갑니다.
// 순서가 꼬이면 랩 6 의 [DefaultExecutionOrder] 도 같이 쓰세요.
#endregion
