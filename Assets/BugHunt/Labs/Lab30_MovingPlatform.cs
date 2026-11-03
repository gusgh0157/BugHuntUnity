// 랩 30: 움직이는 발판
// 발판(Kinematic Rigidbody)이 1초에 2m 씩 옆으로 간다. 움직임이 덜덜 떨려서 Interpolate 를 켰더니 발판이 거의 움직이지 않는다.
// 발판 위에 올려 둔 상자도 실려 가지 않는다.
// 기대: 발판은 정해진 만큼 가고, 상자는 발판에 실려 함께 간다(떨어지지 않는다).
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab30
{
    public class MovingPlatform : MonoBehaviour
    {
        public Vector3 Velocity = new Vector3(2f, 0f, 0f);
        Rigidbody rb;

        void Awake()
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;   // 화면에서 부드럽게
        }

        void Update()
        {
            transform.position += Velocity * Time.deltaTime;
        }
    }

    [Lab(30, "움직이는 발판")]
    public class Lab30_MovingPlatform : Lab
    {
        float savedCaptureDeltaTime;

        public override IEnumerator Run()
        {
            savedCaptureDeltaTime = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;

            var platformGo = SpawnPrimitive(PrimitiveType.Cube, "Platform", Vector3.zero);
            platformGo.transform.localScale = new Vector3(4f, 0.5f, 4f);
            var box = SpawnPrimitive(PrimitiveType.Cube, "Box", new Vector3(0f, 0.76f, 0f));
            box.AddComponent<Rigidbody>();
            yield return new WaitForSeconds(0.5f);   // 상자가 발판 위에 자리 잡는다

            var platform = platformGo.AddComponent<MovingPlatform>();
            platform.Velocity = new Vector3(Case(2f, 1.6f), 0f, 0f);
            float seconds = 1.5f;
            float expectedX = platform.Velocity.x * seconds;
            yield return new WaitForSeconds(seconds);

            Report.ExpectNear("발판이 간 거리(m)", expectedX, platformGo.transform.position.x, 0.2f);
            Report.ExpectNear("상자가 실려 간 거리(m)", expectedX, box.transform.position.x, 0.3f);
            Report.ExpectTrue($"상자가 발판 위에 있음 (y = {box.transform.position.y:0.00})", box.transform.position.y > 0.6f);
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
// MovingPlatform.Update 에 추적점 "{Time.frameCount} {transform.position.x}" 를 걸어 보세요. 매 프레임 더한 만큼 x 가 늘어나 있나요? 다음 프레임에 다시 보면?
// 같은 줄을 FixedUpdate 로 옮겨 보세요. 이번에는 발판은 가는데 상자는 왜 제자리에 남아 떨어지나요?
#endregion

#region 힌트 2
// Rigidbody 가 붙은 오브젝트의 "진짜" 위치는 Rigidbody 가 갖고 있고, transform 은 그 결과를 보여 주는 쪽입니다.
// Interpolate 를 켜면 Unity 가 매 프레임 transform 을 "지난 두 물리 스텝 사이의 보간 위치" 로 덮어씁니다. Update 에서 바꾼 transform 은 대부분 지워집니다.
// FixedUpdate 에서 옮겨도 transform 으로 옮긴 Kinematic 은 물리 엔진 입장에서 "순간이동" 입니다. 속도가 0 인 채로 자리만 바뀌므로 마찰로 위의 상자를 끌고 가지 못합니다.
#endregion

#region 힌트 3
// Rigidbody 는 Rigidbody 로, FixedUpdate 에서 움직입니다.
//   void FixedUpdate() { rb.MovePosition(rb.position + Velocity * Time.fixedDeltaTime); }
// Kinematic 의 MovePosition 은 "이 속도로 갔다" 로 계산되어 위의 상자에 마찰이 전달되고, Interpolate 와도 함께 동작합니다.
// Dynamic Rigidbody 라면 rb.linearVelocity 나 AddForce 로 움직입니다. 회전은 rb.MoveRotation.
#endregion
