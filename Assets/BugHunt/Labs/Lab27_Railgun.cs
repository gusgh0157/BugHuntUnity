// 랩 27: 레일건
// 레일건 탄환이 얇은 철판 벽을 그냥 뚫고 지나간다. 느린 권총 탄은 벽에 맞는다. 예외도 경고도 없다.
// 기대: 레일건 탄환도 벽에 맞아(충돌 1번) 벽 앞에서 멈춘다.
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab27
{
    public class Slug : MonoBehaviour
    {
        public int Hits;
        Rigidbody rb;

        void Awake()
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
        }

        public void Fire(Vector3 velocity)
        {
            rb.linearVelocity = velocity;
        }

        void OnCollisionEnter(Collision collision)
        {
            Hits++;
            rb.linearVelocity = Vector3.zero;   // 박힌다
            rb.isKinematic = true;
        }
    }

    [Lab(27, "레일건")]
    public class Lab27_Railgun : Lab
    {
        public override IEnumerator Run()
        {
            float wallX = Case(10f, 13f);
            float speed = Case(300f, 450f);
            var wall = SpawnPrimitive(PrimitiveType.Cube, "SteelWall", new Vector3(wallX, 0f, 0f));
            wall.transform.localScale = new Vector3(0.2f, 4f, 4f);   // 두께 20cm

            var slug = SpawnPrimitive(PrimitiveType.Sphere, "Slug", Vector3.zero).AddComponent<Slug>();
            slug.transform.localScale = Vector3.one * 0.2f;
            yield return new WaitForFixedUpdate();

            Report.Note($"탄속 {speed}m/s, 물리 한 스텝 {Time.fixedDeltaTime * 1000f:0}ms 에 {speed * Time.fixedDeltaTime:0.0}m 를 간다. 벽 두께 0.2m");
            slug.Fire(new Vector3(speed, 0f, 0f));
            for (int i = 0; i < 10; ++i)
            {
                yield return new WaitForFixedUpdate();
            }

            Report.Expect("벽에 맞은 횟수", 1, slug.Hits);
            Report.ExpectTrue($"탄환이 벽 앞에 있음 (x = {slug.transform.position.x:0.00}, 벽 x = {wallX})", slug.transform.position.x < wallX);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// Slug 에 FixedUpdate 를 하나 만들어 Debug.DrawRay(transform.position, rb.linearVelocity * Time.fixedDeltaTime, Color.red, 5f) 를 그려 보세요(Scene 뷰, Gizmos 켜기).
// 물리 스텝마다 탄환이 어디에 있었는지 점을 찍어 보면(추적점: "{transform.position.x}") 벽 안에 있었던 스텝이 한 번이라도 있나요?
// Window → Analysis → Physics Debugger 의 Queries / Contacts 탭에서 접촉이 한 번이라도 생겼는지 볼 수 있습니다.
#endregion

#region 힌트 2
// 물리는 연속으로 움직이지 않고 fixedDeltaTime(0.02초)마다 순간이동합니다. 300m/s 면 한 스텝에 6m 입니다.
// 기본 충돌 감지(Discrete)는 "스텝이 끝난 위치에서 겹쳤나" 만 봅니다. 앞 스텝은 벽 앞, 다음 스텝은 벽 뒤라면 한 번도 겹치지 않았으니 충돌도 없습니다(터널링).
// 느린 탄은 어느 스텝에서든 벽과 겹치므로 맞습니다. 벽을 두껍게 하거나 fixedDeltaTime 을 줄이면 숨겨질 뿐입니다.
#endregion

#region 힌트 3
// 빠른 Rigidbody 는 연속 충돌 감지를 켭니다.
//   rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;   // 움직이는 것끼리도 잡는다(가장 비싸다)
//   (CollisionDetectionMode.Continuous: 정적 콜라이더에 대해서만, ContinuousSpeculative: 싸지만 회전이 빠르면 놓칠 수 있다)
// 총알이 아주 많다면 Rigidbody 대신 매 스텝 Physics.Raycast(이전 위치 → 현재 위치)로 맞았는지 직접 봅니다.
#endregion
