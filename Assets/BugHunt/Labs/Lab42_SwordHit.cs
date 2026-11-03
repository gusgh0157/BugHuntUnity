// 랩 42: 검격 판정
// 검을 휘두르는 애니메이션은 끝까지 나오는 것 같은데 피해가 한 번도 들어가지 않는다.
// 기대: 3번 휘두르면 Hit 애니메이션 이벤트가 3번.
// 이 랩의 버그는 C# 코드가 아니라 애니메이션 에셋에 있다: Assets/BugHunt/Resources/BugHunt/Animation/
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab42
{
    public class Swordsman : MonoBehaviour
    {
        static readonly int SwingId = Animator.StringToHash("Swing");
        public int HitsLanded;
        Animator animator;

        void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public void Swing()
        {
            animator.SetTrigger(SwingId);
        }

        // Swing 클립의 Animation Event 가 부른다. 칼날이 맞는 순간.
        public void Hit()
        {
            ++HitsLanded;
        }
    }

    [Lab(42, "검격 판정")]
    public class Lab42_SwordHit : Lab
    {
        public override IEnumerator Run()
        {
            GameObject go = Spawn("Swordsman");
            new GameObject("Blade").transform.SetParent(go.transform, false);
            var animator = go.AddComponent<Animator>();
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("BugHunt/Animation/Swordsman");
            var swordsman = go.AddComponent<Swordsman>();
            yield return null;

            int swings = Case(3, 4);
            for (int i = 0; i < swings; ++i)
            {
                swordsman.Swing();
                yield return new WaitForSeconds(1.3f);
            }
            Report.Expect("들어간 검격", swings, swordsman.HitsLanded);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// Swordsman.Hit 에 중단점. 멈추나요? 코드에는 이상이 없어 보이니 에셋을 봅시다.
// Project 창에서 Animation/Swordsman_Swing.anim 을 더블클릭해 Animation 창을 여세요. Hit 이벤트(타임라인 위 작은 막대)는 몇 초에 있나요? 클립 길이는?
#endregion

#region 힌트 2
// Animator 창에서 Swordsman 컨트롤러를 열고 Swing → Idle 트랜지션(화살표)을 고르세요. Inspector 의 Exit Time 과 Transition Duration 은?
// Exit Time 0.7 = 클립의 70% 지점에서 Idle 로 넘어가기 시작, Duration 0.1 = 클립 길이의 10% 동안 섞으며 넘어감.
// 그러면 Swing 은 클립의 몇 % 지점에서 완전히 끝나나요? Hit 이벤트는 몇 % 지점에 있나요?
// 플레이 중 Animator 창을 띄워 두면 상태의 진행 막대와 트랜지션이 실시간으로 보입니다(Debug.Break 이나 일시정지 후 한 프레임씩: Ctrl+Alt+P).
#endregion

#region 힌트 3
// 트랜지션이 끝나면 원래 상태의 클립은 더 이상 재생되지 않으므로, 그 뒤에 있는 이벤트는 영영 불리지 않습니다.
// 고치는 법 (택1): ① Swing → Idle 트랜지션의 Exit Time 을 0.95 처럼 이벤트 뒤로 ② Hit 이벤트를 0.5초 무렵(칼날이 실제로 닿는 프레임)으로 앞당기기.
// 공격 판정처럼 꼭 불려야 하는 이벤트는 트랜지션이 시작되기 "전"에 두는 것이 안전합니다. 피격으로 공격이 끊기는(인터럽트) 경우도 같은 문제를 겪습니다.
#endregion
