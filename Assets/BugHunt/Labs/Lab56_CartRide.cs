// 랩 56: 수레 탑승
// 상인이 수레에 올라타면 수레의 자식이 되어 함께 움직인다. 수레가 맵 한가운데(원점)에 있을 때 만든 기능이라 그때는 자리에 딱 앉았다.
// 수레가 다른 곳에 서 있으면 상인이 자리에서 멀리 떨어진 허공에 앉고, 수레가 움직이면 그 허공째로 따라간다.
// 기대: 상인은 어디서 타든 수레의 자리(Seat)에 앉고, 수레가 가도 자리에 그대로 있다.
// ラボ 56: 荷車に乗る
// 商人が荷車に乗ると、荷車の子になって一緒に動く。荷車がマップの真ん中 (原点) にあるときに作った機能なので、そのときは席にぴったり座れた。
// 荷車がほかの場所に止まっていると、商人は席から遠く離れた宙に座り、荷車が動くとその宙ごとついていく。
// 期待: 商人はどこで乗っても荷車の席 (Seat) に座り、荷車が進んでも席にそのままいる。
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab56
{
    public class Cart : MonoBehaviour
    {
        public Transform Seat;   // 수레의 자식. 탄 사람이 앉을 자리 / 荷車の子。乗る人が座る場所
        public Vector3 Velocity;

        public void Board(Transform rider)
        {
            rider.SetParent(transform);
            rider.localPosition = Seat.position;   // 자리에 앉힌다 / 席に座らせる
        }

        void Update()
        {
            transform.position += Velocity * Time.deltaTime;
        }
    }

    [Lab(56, "수레 탑승", Symptom = "수레에 탄 상인이 자리에서 멀리 떨어진 허공에 앉는다")]
    public class Lab56_CartRide : Lab
    {
        public override IEnumerator Run()
        {
            Vector3 cartAt = Case(new Vector3(6f, 0f, 4f), new Vector3(-9f, 0f, 7f));
            var cart = Spawn<Cart>("Cart", cartAt);
            var seat = Spawn("Seat");
            seat.transform.SetParent(cart.transform, false);
            seat.transform.localPosition = new Vector3(0f, 1f, -0.5f);
            cart.Seat = seat.transform;
            var merchant = Spawn("Merchant", cartAt + new Vector3(2f, 0f, 0f));
            yield return null;

            cart.Board(merchant.transform);
            Report.ExpectNear("탄 직후 상인과 자리 사이 거리(m)", 0f, Vector3.Distance(merchant.transform.position, seat.transform.position), 0.01f);

            cart.Velocity = new Vector3(0f, 0f, 3f);
            yield return new WaitForSeconds(0.5f);
            Report.ExpectTrue("수레가 움직였다", cart.transform.position.z > cartAt.z + 0.5f);
            Report.ExpectNear("수레가 간 뒤 상인과 자리 사이 거리(m)", 0f, Vector3.Distance(merchant.transform.position, seat.transform.position), 0.01f);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// Board 의 마지막 줄 다음에 중단점을 걸고 조사식에 Seat.position, Seat.localPosition, rider.position, rider.localPosition 을 나란히 넣어 보세요.
// 플레이 중에 Hierarchy 에서 Merchant 를 고르면 Inspector 의 Position 은 어느 쪽 값인가요? Scene 뷰 툴바의 Local / Global 토글을 바꿔 가며 기즈모도 보세요.
//
// Board の最後の行の次にブレークポイントを置き、ウォッチ式に Seat.position、Seat.localPosition、rider.position、rider.localPosition を並べて入れてみてください。
// 再生中にヒエラルキーで Merchant を選ぶと、インスペクターの位置 (Position) はどちらの値ですか? シーンビューのツールバーの ローカル / グローバル (Local / Global) トグルを切り替えながらギズモも見てください。
#endregion

#region 힌트 2 / ヒント 2
// position 은 월드 좌표, localPosition 은 부모 기준 좌표입니다. Inspector 의 Transform 이 보여 주는 것은 localPosition 입니다.
// 부모가 원점에 회전·크기 없이 있으면 둘이 같아서, 원점에서만 시험하면 버그가 보이지 않습니다.
// 월드 좌표를 localPosition 에 넣으면 부모의 위치(와 회전, 크기)가 한 번 더 더해집니다. 수레가 (6, 0, 4) 에 있으면 상인은 자리에서 그만큼 더 간 곳에 앉습니다.
//
// position はワールド座標、localPosition は親を基準にした座標です。インスペクターのトランスフォーム (Transform) が表示しているのは localPosition です。
// 親が原点に回転・スケールなしでいると 2 つは同じなので、原点だけで試すとバグが見えません。
// ワールド座標を localPosition に入れると、親の位置 (と回転、スケール) がもう一度加わります。荷車が (6, 0, 4) にあれば、商人は席からその分だけ先に座ります。
#endregion

#region 힌트 3 / ヒント 3
// 좌표계를 맞춥니다. 월드끼리 또는 로컬끼리.
//   rider.SetParent(transform);
//   rider.position = Seat.position;                  // 월드 = 월드
// 또는 자리 자체를 부모로 삼아 로컬 0 에 앉힙니다:  rider.SetParent(Seat, false); rider.localPosition = Vector3.zero;
// 변수 이름에 좌표계를 남기면(worldSeat, localOffset) 섞어 쓰는 줄이 눈에 띕니다.
//
// 座標系をそろえます。ワールド同士、またはローカル同士。
//   rider.SetParent(transform);
//   rider.position = Seat.position;                  // ワールド = ワールド
// または席そのものを親にして、ローカル 0 に座らせます:  rider.SetParent(Seat, false); rider.localPosition = Vector3.zero;
// 変数名に座標系を残しておくと (worldSeat、localOffset)、混ぜて使っている行が目に付きます。
#endregion
