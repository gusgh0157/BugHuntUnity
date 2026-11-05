// 랩 20: 돌아서 쏘기
// 궁수가 적 쪽으로 몸을 돌리고(LookAt) 화살을 쏜다. 화살은 궁수가 바라보는 방향으로 나가야 한다.
// 적이 북쪽(월드 +Z)에 있을 때는 잘 맞는데, 옆이나 뒤에 있는 적을 향해 돌아서 쏘면 화살이 늘 북쪽으로 날아간다.
// 기대: 화살은 쏠 때 궁수가 바라보던 방향(= 적 쪽)으로 날아간다.
// ラボ 20: 振り向き撃ち
// 弓兵が敵のほうへ体を向け (LookAt)、矢を撃つ。矢は弓兵が向いている方向へ飛ぶはず。
// 敵が北 (ワールド +Z) にいるときはよく当たるのに、横や後ろにいる敵へ振り向いて撃つと、矢がいつも北へ飛んでいく。
// 期待: 矢は撃ったときに弓兵が向いていた方向 (= 敵のほう) へ飛ぶ。
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab20
{
    public class Arrow : MonoBehaviour
    {
        public float Speed = 20f;

        void Update()
        {
            transform.position += Vector3.forward * Speed * Time.deltaTime;   // 앞으로 날아간다 / 前へ飛ぶ
        }
    }

    [Lab(20, "돌아서 쏘기", Symptom = "옆이나 뒤의 적에게 돌아서 쏜 화살이 늘 북쪽으로 날아간다")]
    public class Lab20_TurnAndShoot : Lab
    {
        public override IEnumerator Run()
        {
            Vector3 archerAt = Case(new Vector3(2f, 0f, 3f), new Vector3(-6f, 0f, -2f));
            var archer = Spawn("Archer", archerAt);
            var enemy = Spawn("Enemy", archerAt + Case(new Vector3(9f, 0f, -6f), new Vector3(-8f, 0f, 3f)));
            yield return null;

            archer.transform.LookAt(enemy.transform);
            Vector3 muzzle = archer.transform.position + archer.transform.forward * 0.5f;
            var arrow = Spawn<Arrow>("Arrow", muzzle);
            arrow.transform.rotation = archer.transform.rotation;
            yield return new WaitForSeconds(0.3f);

            Vector3 flown = arrow.transform.position - muzzle;
            Report.ExpectTrue("화살이 날아갔다", flown.magnitude > 1f);
            Report.ExpectNear("화살이 날아간 방향과 적 방향의 차이(도)", 0f, Vector3.Angle(flown, enemy.transform.position - archer.transform.position), 2f);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// Arrow.Update 에 중단점을 걸고 조사식에 Vector3.forward 와 transform.forward 를 나란히 넣어 보세요. 화살의 회전은 제대로 들어가 있나요?
// 플레이 중 Scene 뷰 툴바의 Local / Global 토글을 Local 로 두고 Arrow 를 고르면, 파란 화살표(Z)가 어디를 가리키나요?
//
// Arrow.Update にブレークポイントを置き、ウォッチ式に Vector3.forward と transform.forward を並べて入れてみてください。矢の回転はちゃんと入っていますか?
// 再生中にシーンビューのツールバーの ローカル / グローバル (Local / Global) トグルをローカル (Local) にして Arrow を選ぶと、青い矢印 (Z) はどこを指していますか?
#endregion

#region 힌트 2 / ヒント 2
// Vector3.forward 는 그냥 상수 (0, 0, 1) 입니다. 오브젝트가 어디를 보든 늘 월드 +Z 입니다.
// transform.forward 는 그 오브젝트의 회전을 적용한 "자기 앞" 방향입니다(= transform.rotation * Vector3.forward).
// 테스트할 때 적을 늘 +Z 쪽에 두었다면 둘이 같아서 버그가 보이지 않습니다.
//
// Vector3.forward はただの定数 (0, 0, 1) です。オブジェクトがどこを向いていても、いつもワールドの +Z です。
// transform.forward はそのオブジェクトの回転を適用した「自分の前」の方向です (= transform.rotation * Vector3.forward)。
// テストのときに敵をいつも +Z 側に置いていたなら、2 つが同じなのでバグが見えません。
#endregion

#region 힌트 3 / ヒント 3
//   transform.position += transform.forward * Speed * Time.deltaTime;
// 또는 transform.Translate(0f, 0f, Speed * Time.deltaTime); 도 됩니다. Translate 의 기본 공간은 Space.Self(자기 축)입니다.
// 반대로 월드 기준으로 움직이고 싶을 때만 Vector3.forward 나 Translate(..., Space.World) 를 씁니다.
//
//   transform.position += transform.forward * Speed * Time.deltaTime;
// または transform.Translate(0f, 0f, Speed * Time.deltaTime); でも構いません。Translate の既定の空間は Space.Self (自分の軸) です。
// 逆にワールド基準で動かしたいときだけ Vector3.forward や Translate(..., Space.World) を使います。
#endregion
