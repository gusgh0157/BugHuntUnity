// 랩 61: 드론 탐조등
// 경비 드론이 앞쪽 아래로 탐조등을 비추고, 침입자가 드론의 왼쪽에 있는지 오른쪽에 있는지 판정해 그쪽으로 돈다.
// 원점 근처 시험장에서는 멀쩡했는데, 맵 구석에 배치하자 탐조등이 엉뚱한 쪽을 비추고 좌우 판정도 틀린다.
// 기대: 탐조등 방향은 드론 기준 (0, -1, 1) 을 월드로 바꾼 방향이고, 드론 왼쪽의 침입자는 왼쪽, 오른쪽은 오른쪽으로 판정한다.
// ラボ 61: ドローンのサーチライト
// 警備ドローンが前方下へサーチライトを照らし、侵入者がドローンの左にいるか右にいるかを判定してそちらへ向く。
// 原点近くの試験場では問題なかったのに、マップの隅に配置するとサーチライトが見当違いの方向を照らし、左右の判定も間違える。
// 期待: サーチライトの方向はドローン基準の (0, -1, 1) をワールドに変えた方向で、ドローンの左の侵入者は左、右の侵入者は右と判定する。
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab61
{
    public class Drone : MonoBehaviour
    {
        public Vector3 LocalBeam = new Vector3(0f, -1f, 1f).normalized;   // 드론 기준 앞쪽 아래 / ドローン基準の前方下

        public Vector3 BeamDirection()
        {
            return transform.TransformPoint(LocalBeam).normalized;   // 로컬 → 월드 / ローカル → ワールド
        }

        public bool IsOnLeft(Vector3 worldPoint)
        {
            return transform.InverseTransformDirection(worldPoint).x < 0f;   // 월드 → 로컬 / ワールド → ローカル
        }
    }

    [Lab(61, "드론 탐조등", Symptom = "맵 구석에 둔 드론의 탐조등 방향과 좌우 판정이 틀린다")]
    public class Lab61_DroneSearchlight : Lab
    {
        public override IEnumerator Run()
        {
            var drone = Spawn<Drone>("Drone", Case(new Vector3(30f, 10f, -20f), new Vector3(-25f, 8f, 15f)));
            drone.transform.rotation = Quaternion.Euler(0f, Case(60f, -40f), 0f);
            yield return null;

            Vector3 expectedBeam = drone.transform.rotation * drone.LocalBeam;
            Report.ExpectNear("탐조등 방향과 기대 방향의 차이(도)", 0f, Vector3.Angle(expectedBeam, drone.BeamDirection()), 0.5f);

            Vector3 leftIntruder = drone.transform.position + drone.transform.rotation * new Vector3(-3f, 0f, 4f);
            Vector3 rightIntruder = drone.transform.position + drone.transform.rotation * new Vector3(3f, 0f, 4f);
            Report.Expect("드론 왼쪽 앞 침입자를 왼쪽으로 판정", true, drone.IsOnLeft(leftIntruder));
            Report.Expect("드론 오른쪽 앞 침입자를 왼쪽으로 판정", false, drone.IsOnLeft(rightIntruder));
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// BeamDirection 과 IsOnLeft 안에서 조사식에 transform.position, transform.TransformPoint(LocalBeam), transform.TransformDirection(LocalBeam) 을 비교해 보세요.
// 드론을 원점으로 옮기면(조사식이나 Inspector 에서) 결과가 어떻게 바뀌나요?
//
// BeamDirection と IsOnLeft の中で、ウォッチ式に transform.position、transform.TransformPoint(LocalBeam)、transform.TransformDirection(LocalBeam) を比べてみてください。
// ドローンを原点に動かすと (ウォッチ式やインスペクターで)、結果はどう変わりますか?
#endregion

#region 힌트 2 / ヒント 2
// Point 는 "위치" 를 바꿉니다: 회전, 크기, 그리고 위치(이동)까지 적용합니다. Direction 은 "방향" 을 바꿉니다: 회전만 적용합니다(크기·이동 없음).
// 방향 (0, -1, 1) 을 TransformPoint 로 바꾸면 드론 위치가 더해져 "드론에서 조금 떨어진 점" 이 되고, 그걸 normalize 하면 원점에서 그 점을 보는 방향이 됩니다.
// 반대로 InverseTransformDirection 에 월드 위치를 넣으면 드론 위치를 빼지 않으므로, 드론이 원점에서 멀수록 좌우가 드론 위치에 휘둘립니다.
// (Vector 쪽은 회전과 크기를 적용하고 이동만 뺍니다: TransformVector / InverseTransformVector)
//
// Point は「位置」を変換します: 回転、スケール、そして位置 (移動) まで適用します。Direction は「方向」を変換します: 回転だけを適用します (スケール・移動なし)。
// 方向 (0, -1, 1) を TransformPoint で変換するとドローンの位置が足されて「ドローンから少し離れた点」になり、それを normalize すると原点からその点を見る方向になります。
// 逆に InverseTransformDirection にワールドの位置を入れるとドローンの位置を引かないので、ドローンが原点から遠いほど、左右がドローンの位置に振り回されます。
// (Vector のほうは回転とスケールを適用し、移動だけを除きます: TransformVector / InverseTransformVector)
#endregion

#region 힌트 3 / ヒント 3
//   return transform.TransformDirection(LocalBeam);                 // 방향은 Direction (길이도 그대로라 normalized 불필요)
//   return transform.InverseTransformPoint(worldPoint).x < 0f;      // 위치는 Point
// 넣는 값이 위치인지 방향인지 변수 이름에 남기면(worldPoint, localBeamDirection) 짝이 안 맞는 호출이 눈에 띕니다.
//
//   return transform.TransformDirection(LocalBeam);                 // 方向は Direction (長さもそのままなので normalized は不要)
//   return transform.InverseTransformPoint(worldPoint).x < 0f;      // 位置は Point
// 渡す値が位置か方向かを変数名に残しておくと (worldPoint、localBeamDirection)、組み合わせの合わない呼び出しが目に付きます。
#endregion
