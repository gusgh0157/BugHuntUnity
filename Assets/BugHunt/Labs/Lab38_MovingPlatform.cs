// 랩 38: 움직이는 발판
// 발판(Kinematic Rigidbody)이 1초에 2m 씩 옆으로 간다. 움직임이 덜덜 떨려서 Interpolate 를 켰더니 발판이 거의 움직이지 않는다.
// 발판 위에 올려 둔 상자도 실려 가지 않는다.
// 기대: 발판은 정해진 만큼 가고, 상자는 발판에 실려 함께 간다(떨어지지 않는다).
// ラボ 38: 動く足場
// 足場 (Kinematic Rigidbody) が 1 秒に 2m ずつ横に動く。動きがガタガタ震えるので 補間 (Interpolate) をオンにしたら、足場がほとんど動かなくなった。
// 足場の上に載せた箱も運ばれていかない。
// 期待: 足場は決まった分だけ進み、箱は足場に載って一緒に進む (落ちない)。
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab38
{
    public class MovingPlatform : MonoBehaviour
    {
        public Vector3 Velocity = new Vector3(2f, 0f, 0f);
        Rigidbody rb;

        void Awake()
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;   // 화면에서 부드럽게 / 画面上でなめらかに
        }

        void Update()
        {
            transform.position += Velocity * Time.deltaTime;
        }
    }

    [Lab(38, "움직이는 발판", Symptom = "Interpolate 를 켜자 발판이 거의 안 움직이고, 위의 상자도 안 실려 간다")]
    public class Lab38_MovingPlatform : Lab
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
            yield return new WaitForSeconds(0.5f);   // 상자가 발판 위에 자리 잡는다 / 箱が足場の上に落ち着く

            var platform = platformGo.AddComponent<MovingPlatform>();
            platform.Velocity = new Vector3(Case(2f, 1.6f), 0f, 0f);
            float seconds = 1.5f;
            float expectedX = platform.Velocity.x * seconds;
            yield return new WaitForSeconds(seconds);

            Report.ExpectNear("발판이 간 거리(m)", expectedX, platformGo.transform.position.x, 0.2f);
            Report.ExpectNear("상자가 실려 간 거리(m)", expectedX, box.transform.position.x, 0.5f);   // 마찰로 따라붙는 지연(약 v²/2a) 만큼 여유 / 摩擦で追いつくまでの遅れ (約 v²/2a) の分の余裕
            Report.ExpectTrue(Loc.F("상자가 발판 위에 있음 (y = {0:0.00})", box.transform.position.y), box.transform.position.y > 0.6f);
        }

        void OnDestroy()
        {
            Time.captureDeltaTime = savedCaptureDeltaTime;
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// MovingPlatform.Update 에 추적점 "{Time.frameCount} {transform.position.x}" 를 걸어 보세요. 매 프레임 더한 만큼 x 가 늘어나 있나요? 다음 프레임에 다시 보면?
// 같은 줄을 FixedUpdate 로 옮겨 보세요. 이번에는 발판은 가는데 상자는 왜 제자리에 남아 떨어지나요?
//
// MovingPlatform.Update にトレースポイント "{Time.frameCount} {transform.position.x}" を置いてみてください。毎フレーム足した分だけ x が増えていますか? 次のフレームでもう一度見ると?
// 同じ行を FixedUpdate に移してみてください。今度は足場は進むのに、箱はなぜその場に残って落ちるのでしょう?
#endregion

#region 힌트 2 / ヒント 2
// Rigidbody 가 붙은 오브젝트의 "진짜" 위치는 Rigidbody 가 갖고 있고, transform 은 그 결과를 보여 주는 쪽입니다.
// Interpolate 를 켜면 Unity 가 매 프레임 transform 을 "지난 두 물리 스텝 사이의 보간 위치" 로 덮어씁니다. Update 에서 바꾼 transform 은 대부분 지워집니다.
// FixedUpdate 에서 옮겨도 transform 으로 옮긴 Kinematic 은 물리 엔진 입장에서 "순간이동" 입니다. 속도가 0 인 채로 자리만 바뀌므로 마찰로 위의 상자를 끌고 가지 못합니다.
//
// Rigidbody が付いたオブジェクトの「本当の」位置は Rigidbody が持っていて、transform はその結果を表示する側です。
// 補間 (Interpolate) をオンにすると、Unity は毎フレーム transform を「直近 2 つの物理ステップの間の補間位置」で上書きします。Update で変えた transform はほとんど消されます。
// FixedUpdate で動かしても、transform で動かした Kinematic は物理エンジンから見れば「瞬間移動」です。速度 0 のまま位置だけが変わるので、摩擦で上の箱を引っ張っていけません。
#endregion

#region 힌트 3 / ヒント 3
// Rigidbody 는 Rigidbody 로, FixedUpdate 에서 움직입니다.
//   void FixedUpdate() { rb.MovePosition(rb.position + Velocity * Time.fixedDeltaTime); }
// Kinematic 의 MovePosition 은 "이 속도로 갔다" 로 계산되어 위의 상자에 마찰이 전달되고, Interpolate 와도 함께 동작합니다.
// Dynamic Rigidbody 라면 rb.linearVelocity 나 AddForce 로 움직입니다. 회전은 rb.MoveRotation.
//
// Rigidbody は Rigidbody で、FixedUpdate の中で動かします。
//   void FixedUpdate() { rb.MovePosition(rb.position + Velocity * Time.fixedDeltaTime); }
// Kinematic の MovePosition は「この速度で進んだ」として計算されるので上の箱に摩擦が伝わり、補間 (Interpolate) とも一緒に動作します。
// Dynamic の Rigidbody なら rb.linearVelocity や AddForce で動かします。回転は rb.MoveRotation。
#endregion
