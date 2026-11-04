// 랩 59: 포신 조준
// 성벽 위 대포가 표적의 좌우 각도(yaw)와 위아래 각도(pitch)를 계산해 포신을 돌린다.
// 정면의 높은 표적이나 같은 높이의 옆 표적은 정확히 겨누는데, 옆쪽 높은 곳에 있는 표적은 엉뚱한 하늘을 겨눈다.
// 기대: 포신(transform.forward)이 어느 표적이든 정확히 표적을 향한다.
// ラボ 59: 砲身の照準
// 城壁の上の大砲が、標的の左右の角度 (yaw) と上下の角度 (pitch) を計算して砲身を回す。
// 正面の高い標的や同じ高さの横の標的は正確に狙うのに、横の高い所にある標的では見当違いの空を狙う。
// 期待: 砲身 (transform.forward) がどの標的でも正確に標的を向く。
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab59
{
    public class Cannon : MonoBehaviour
    {
        public void AimAt(Vector3 target)
        {
            Vector3 direction = (target - transform.position).normalized;
            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;   // 좌우 / 左右
            float pitch = -Mathf.Asin(direction.y) * Mathf.Rad2Deg;              // 위아래 (위가 음수) / 上下 (上が負)
            transform.rotation = Quaternion.Euler(pitch, 0f, 0f) * Quaternion.Euler(0f, yaw, 0f);   // 들고 나서 돌린다 / 上げてから回す
        }
    }

    [Lab(59, "포신 조준", Symptom = "옆쪽 높은 곳의 표적을 겨누면 포신이 엉뚱한 하늘을 향한다")]
    public class Lab59_CannonAim : Lab
    {
        public override IEnumerator Run()
        {
            var cannon = Spawn<Cannon>("Cannon", new Vector3(0f, 5f, 0f));
            yield return null;

            Vector3[] targets =
            {
                cannon.transform.position + Case(new Vector3(0f, 3f, 12f), new Vector3(-10f, 0f, 5f)),   // 축 하나만 / 軸 1 つだけ
                cannon.transform.position + Case(new Vector3(8f, 4f, 6f), new Vector3(-5f, 6f, -9f)),    // 옆쪽 높은 곳 / 横の高い所
            };
            for (int i = 0; i < targets.Length; ++i)
            {
                cannon.AimAt(targets[i]);
                Vector3 wanted = targets[i] - cannon.transform.position;
                Report.ExpectNear(Loc.F("표적 {0}: 포신 방향과의 차이(도)", i + 1), 0f, Vector3.Angle(cannon.transform.forward, wanted), 1f);
            }
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// AimAt 의 마지막 줄에서 조사식에 yaw, pitch, transform.forward, direction 을 넣고 표적 2 에서 비교해 보세요. yaw 와 pitch 계산은 맞나요?
// 맞다면 틀린 것은 두 회전을 합치는 방법입니다. Quaternion.Euler(pitch, 0f, 0f) * Quaternion.Euler(0f, yaw, 0f) 와 순서를 바꾼 식을 조사식에서 각각 * Vector3.forward 해 보세요.
//
// AimAt の最後の行で、ウォッチ式に yaw、pitch、transform.forward、direction を入れて標的 2 で比べてみてください。yaw と pitch の計算は合っていますか?
// 合っているなら、間違っているのは 2 つの回転を合わせる方法です。Quaternion.Euler(pitch, 0f, 0f) * Quaternion.Euler(0f, yaw, 0f) と順序を入れ替えた式を、ウォッチ式でそれぞれ * Vector3.forward してみてください。
#endregion

#region 힌트 2 / ヒント 2
// 쿼터니언 곱 A * B 는 "B 를 먼저 하고 그다음 A" 입니다(벡터에 가까운 쪽부터). 그래서 곱의 순서를 바꾸면 다른 회전이 됩니다.
// 지금 식은 먼저 월드 Y 축으로 좌우를 돌린 뒤, 월드 X 축으로 듭니다. 이미 옆을 보는 포신을 월드 X 축으로 들면 포신이 위가 아니라 옆으로 기울어집니다.
// 원하는 것은 "포신을 먼저 든 다음(자기 X), 받침대째 좌우로 돌린다(월드 Y)" 입니다. 표적이 정면이거나 같은 높이면 한쪽 회전이 0 이라 순서가 상관없어서 버그가 숨습니다.
//
// クォータニオンの積 A * B は「B を先に行い、次に A」です (ベクトルに近い側から)。そのため掛ける順序を変えると別の回転になります。
// いまの式は、先にワールドの Y 軸で左右に回してから、ワールドの X 軸で上げています。すでに横を向いている砲身をワールドの X 軸で上げると、砲身は上ではなく横に傾きます。
// 欲しいのは「先に砲身を上げてから (自分の X)、台座ごと左右に回す (ワールドの Y)」です。標的が正面か同じ高さなら片方の回転が 0 なので順序が関係なく、バグが隠れます。
#endregion

#region 힌트 3 / ヒント 3
//   transform.rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(pitch, 0f, 0f);   // 들고 → 돌린다 (오른쪽부터)
// Quaternion.Euler(pitch, yaw, 0f) 도 같은 결과입니다. Unity 의 Euler 는 Z, X, Y 순서로 돌리기 때문입니다(= Ry * Rx * Rz).
// 방향만 필요하면 각도를 거치지 않고 transform.rotation = Quaternion.LookRotation(direction); 이 가장 짧습니다.
//
//   transform.rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(pitch, 0f, 0f);   // 上げて → 回す (右から)
// Quaternion.Euler(pitch, yaw, 0f) も同じ結果です。Unity の Euler は Z、X、Y の順に回すからです (= Ry * Rx * Rz)。
// 向きだけが必要なら、角度を経由せずに transform.rotation = Quaternion.LookRotation(direction); がいちばん短いです。
#endregion
