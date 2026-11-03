// 랩 41: 등 뒤의 발소리
// 경비병이 화면 밖(플레이어 등 뒤)에서 걸어오는데 발소리가 하나도 나지 않는다. 카메라를 돌려 경비병을 보면 그제야 발소리가 난다.
// 기대: 걷기 애니메이션 1초에 발소리 이벤트 2번. 2초 남짓이면 4번 이상.
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab41
{
    public class Guard : MonoBehaviour
    {
        public int Footsteps;

        void Awake()
        {
            // 최적화: 화면 밖 NPC 의 애니메이션은 계산하지 않는다.
            GetComponent<Animator>().cullingMode = AnimatorCullingMode.CullCompletely;
        }

        // 걷기 클립의 Animation Event 가 부른다. (실제 게임이라면 여기서 발소리를 내고 주변에 소음을 알린다)
        public void Footstep()
        {
            ++Footsteps;
        }
    }

    [Lab(41, "등 뒤의 발소리")]
    public class Lab41_Footsteps : Lab
    {
        public override IEnumerator Run()
        {
            // 플레이어 카메라는 +Z 를 본다. 경비병은 한참 뒤(-Z)에 있다.
            var cam = Spawn<Camera>("PlayerCamera", new Vector3(0, 1, 0));
            cam.transform.rotation = Quaternion.identity;

            GameObject guard = Spawn("Guard", new Vector3(0, 0, -60));
            GameObject body = SpawnPrimitive(PrimitiveType.Capsule, "Body");
            body.transform.SetParent(guard.transform, false);
            var animator = guard.AddComponent<Animator>();
            animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("BugHunt/Animation/Guard");
            var logic = guard.AddComponent<Guard>();

            yield return new WaitForSeconds(2.1f);

            Report.Note($"경비병이 보이는가 (Renderer.isVisible): {body.GetComponent<Renderer>().isVisible}");
            Report.ExpectTrue($"발소리 이벤트 4번 이상 (실제 {logic.Footsteps}번)", logic.Footsteps >= 4);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// Guard.Footstep 에 중단점. 한 번이라도 멈추나요?
// 랩이 도는 동안 Scene 뷰를 경비병(z = -60) 쪽으로 돌려 보세요. 그러면 멈출 수도 있습니다. 왜 "보느냐"에 따라 달라질까요?
#endregion

#region 힌트 2
// 플레이 중 Hierarchy 에서 Lab21/Guard 를 고르고 Inspector 의 Animator 컴포넌트를 보세요. Culling Mode 는?
// 그리고 Animator 창(Window → Animation → Animator)에서 Walk 상태의 진행 막대가 움직이나요?
// Culling Mode:  Always Animate      항상 계산
//                Cull Update Transforms  안 보이면 뼈 위치 쓰기·IK 생략(상태 머신과 이벤트는 돈다)
//                Cull Completely     안 보이면 전부 멈춤(이벤트도 없음)
#endregion

#region 힌트 3
// 화면 밖에서도 게임 로직(발소리, 공격 판정, 이동)을 Animation Event 에 기대는 캐릭터는 Cull Completely 를 쓰면 안 됩니다.
// Cull Update Transforms 로 바꾸면 비용은 줄이면서 이벤트는 유지됩니다(그래도 안 되면 Always Animate).
// 판정이 중요한 로직은 애니메이션 이벤트가 아니라 코드의 타이머로 돌리는 설계도 고려하세요.
// 참고: 에디터에서는 Scene 뷰 카메라도 "보는 것"으로 칩니다. 빌드에서와 결과가 다를 수 있습니다.
#endregion
