// 랩 2: 헤드샷
// 몸통을 쏘면 멀쩡한데 머리를 쏘는 순간 NullReferenceException 이 난다.
// 기대: 몸통 20 + 몸통 20 + 머리 40(두 배) = 80 피해, 허수아비 HP 20.
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab02
{
    public class Health : MonoBehaviour
    {
        public int Current = 100;

        public void TakeDamage(int amount)
        {
            Current -= amount;
        }
    }

    public class Rifle : MonoBehaviour
    {
        public int Damage = 20;
        public int Hits;

        public void Shoot(Vector3 from, Vector3 direction)
        {
            if (!Physics.Raycast(from, direction, out RaycastHit hit, 100f))
            {
                return;
            }

            ++Hits;
            bool headshot = hit.collider.name == "Head";
            var health = hit.collider.GetComponent<Health>();
            health.TakeDamage(headshot ? Damage * 2 : Damage);
        }
    }

    [Lab(2, "헤드샷")]
    public class Lab02_Headshot : Lab
    {
        public override IEnumerator Run()
        {
            // 허수아비: 몸통(캡슐 콜라이더 + Health)과 자식 "Head"(구 콜라이더)
            GameObject dummy = Spawn("TrainingDummy", new Vector3(0, 1, 10));
            dummy.AddComponent<CapsuleCollider>().height = 2f;
            var health = dummy.AddComponent<Health>();

            var head = new GameObject("Head");
            head.transform.SetParent(dummy.transform, false);
            head.transform.localPosition = new Vector3(0, 1.3f, 0);
            head.AddComponent<SphereCollider>().radius = 0.3f;

            var rifle = Spawn<Rifle>("Rifle", Vector3.zero);
            rifle.Damage = Case(20, 15);
            Physics.SyncTransforms();   // 방금 만든 콜라이더를 레이캐스트가 보게 한다
            yield return null;

            rifle.Shoot(new Vector3(0, 1, 0), Vector3.forward);      // 몸통
            yield return null;
            rifle.Shoot(new Vector3(0, 0.5f, 0), Vector3.forward);   // 몸통
            yield return null;
            Report.Note($"몸통 두 발 뒤 HP {health.Current}");
            rifle.Shoot(new Vector3(0, 2.3f, 0), Vector3.forward);   // 머리
            yield return null;

            Report.Expect("명중 수", 3, rifle.Hits);
            Report.Expect("허수아비 HP", 100 - 4 * rifle.Damage, health.Current);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// Console 의 NullReferenceException 스택 트레이스 맨 위 줄로 가세요. 그 줄에서 null 일 수 있는 것은 무엇무엇인가요?
// 그 줄에 중단점을 걸고 다시 실행해서, 멈출 때마다 조사식으로 hit.collider.name 과 health 를 보세요. 몇 번째 멈춤에서 health 가 null 인가요?
#endregion

#region 힌트 2
// 멈췄을 때 조사식에 hit.collider.gameObject 를 넣고, Hierarchy 창에서 TrainingDummy 를 펼쳐 보세요.
// Health 컴포넌트는 어느 게임 오브젝트의 Inspector 에 붙어 있나요? 맞은 콜라이더는 어느 오브젝트에 붙어 있나요?
#endregion

#region 힌트 3
// GetComponent<T>() 는 "그 게임 오브젝트 자신"만 찾습니다. 부모나 자식은 보지 않고, 없으면 조용히 null 을 줍니다.
// 맞은 부위에서 위로 올라가며 찾으려면 GetComponentInParent<T>() (자기 자신 포함). 없을 수도 있다면 null 검사나 TryGetComponent 도 같이.
#endregion
