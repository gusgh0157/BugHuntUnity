// 랩 18: 풍차
// 풍차 날개가 가로축(X)을 중심으로 일정한 속도로 돈다. 처음 몇 초는 잘 도는데, 날개가 위를 똑바로 가리킬 즈음 덜덜 떨며 멈춘다.
// 기대: 날개는 1초에 90도씩 멈추지 않고 계속 돈다. 풍차가 어느 쪽을 바라보고 서 있든 같다.
// ラボ 18: 風車
// 風車の羽根が横軸 (X) を中心に一定の速さで回る。最初の数秒はうまく回るが、羽根が真上を指すあたりでガタガタ震えて止まる。
// 期待: 羽根は 1 秒に 90 度ずつ止まらずに回り続ける。風車がどちらを向いて立っていても同じ。
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab18
{
    public class Windmill : MonoBehaviour
    {
        public float DegreesPerSecond = 90f;

        void Update()
        {
            transform.eulerAngles += new Vector3(DegreesPerSecond * Time.deltaTime, 0f, 0f);   // X 축으로 돌린다 / X 軸で回す
        }
    }

    [Lab(18, "풍차", Symptom = "풍차 날개가 위를 가리킬 즈음 떨면서 멈춘다")]
    public class Lab18_Windmill : Lab
    {
        float savedCaptureDeltaTime;

        public override IEnumerator Run()
        {
            // 매 프레임 1/60 초가 흐르게 고정한다 / 毎フレーム 1/60 秒が流れるよう固定する
            savedCaptureDeltaTime = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;

            float yaw = Case(0f, 40f);
            int frames = Case(120, 150);
            var windmill = Spawn<Windmill>("Windmill");
            windmill.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            windmill.enabled = false;
            yield return null;

            windmill.enabled = true;
            for (int i = 0; i < frames; ++i)
            {
                yield return null;
            }

            float turned = windmill.DegreesPerSecond * frames / 60f;
            Vector3 expectedUp = Quaternion.Euler(0f, yaw, 0f) * Quaternion.AngleAxis(turned, Vector3.right) * Vector3.up;
            Report.Note(Loc.F("{0} 프레임 동안 돌았어야 할 각도 {1:0}도", frames, turned));
            Report.ExpectNear("날개 방향(transform.up)과 기대 방향의 차이(도)", 0f, Vector3.Angle(expectedUp, windmill.transform.up), 3f);
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
// Update 에 중단점을 걸고, 날개가 멈추기 직전부터 조사식에 transform.eulerAngles 를 보면서 몇 프레임 F5 로 넘겨 보세요.
// 방금 X 에 1.5 를 더해 넣었는데, 다음 프레임에 읽은 X 는 얼마인가요? Y 와 Z 는 왜 갑자기 180 이 되었나요?
// 플레이 중 Inspector 의 Rotation 칸도 같이 보세요.
//
// Update にブレークポイントを置き、羽根が止まる直前からウォッチ式で transform.eulerAngles を見ながら、数フレーム F5 で進めてみてください。
// いま X に 1.5 を足して入れたのに、次のフレームで読んだ X はいくつですか? Y と Z はなぜ急に 180 になったのですか?
// 再生中のインスペクターの回転 (Rotation) 欄も一緒に見てください。
#endregion

#region 힌트 2 / ヒント 2
// Unity 는 회전을 쿼터니언으로 저장합니다. eulerAngles 는 읽을 때마다 쿼터니언에서 다시 계산한 값이고, 같은 회전을 나타내는 오일러 값은 여러 개입니다.
// X 가 90 을 넘으면 Unity 는 (91, 0, 0) 대신 같은 회전인 (89, 180, 180) 을 돌려줍니다. 여기에 1.5 를 더하면 다시 90 쪽으로 돌아가므로 날개가 90 근처를 오갑니다.
// X 가 ±90 이면 Y 와 Z 가 같은 축이 되어 버리는 짐벌 락도 같은 자리입니다. 오일러 값을 읽어서 더하고 다시 쓰는 것은 처음부터 위험합니다.
//
// Unity は回転をクォータニオンで保存しています。eulerAngles は読むたびにクォータニオンから計算し直した値で、同じ回転を表すオイラー値は複数あります。
// X が 90 を超えると、Unity は (91, 0, 0) の代わりに同じ回転である (89, 180, 180) を返します。ここに 1.5 を足すとまた 90 のほうへ戻るので、羽根は 90 付近を行ったり来たりします。
// X が ±90 だと Y と Z が同じ軸になってしまうジンバルロックも同じ場所です。オイラー値を読んで足し、また書き込むこと自体が最初から危険です。
#endregion

#region 힌트 3 / ヒント 3
// 읽어서 더하지 말고, 회전을 곱합니다.
//   transform.Rotate(DegreesPerSecond * Time.deltaTime, 0f, 0f);   // 자기 X 축으로 조금 더 돌린다 (= rotation * Quaternion.Euler(...))
// 또는 각도를 내 변수로 따로 들고 매번 처음부터 만듭니다:  angle += DegreesPerSecond * Time.deltaTime; transform.localRotation = baseRotation * Quaternion.Euler(angle, 0f, 0f);
// 원점에서 Y 회전 없이만 시험하면 Y·Z 가 180 으로 뒤집히는 순간을 놓치기 쉽습니다.
//
// 読んで足すのではなく、回転を掛けます。
//   transform.Rotate(DegreesPerSecond * Time.deltaTime, 0f, 0f);   // 自分の X 軸でもう少し回す (= rotation * Quaternion.Euler(...))
// または角度を自分の変数で別に持ち、毎回最初から作ります:  angle += DegreesPerSecond * Time.deltaTime; transform.localRotation = baseRotation * Quaternion.Euler(angle, 0f, 0f);
// 原点で Y 回転なしだけで試すと、Y・Z が 180 に反転する瞬間を見逃しやすいです。
#endregion
