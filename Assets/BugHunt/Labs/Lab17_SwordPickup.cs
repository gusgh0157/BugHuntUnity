// 랩 17: 검 줍기
// 거대화 물약을 마신 기사(크기 2배)가 바닥의 검을 줍고, 조금 걸어가서 내려놓는다.
// 주운 검이 손에 오지 않고 바닥에 떠 있고, 내려놓으면 검이 맵 한구석으로 순간이동하면서 작아진다.
// 기대: 주우면 검 손잡이가 손에 오고 손과 같은 방향을 본다. 내려놓으면 그 자리에 보이는 크기 그대로 남는다.
// ラボ 17: 剣を拾う
// 巨大化ポーションを飲んだ騎士 (大きさ 2 倍) が床の剣を拾い、少し歩いてから置く。
// 拾った剣が手に来ずに床に浮いたままで、置くと剣がマップの片隅へ瞬間移動しながら小さくなる。
// 期待: 拾うと剣の柄が手に来て、手と同じ向きを向く。置くとその場に見た目の大きさのまま残る。
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab17
{
    public class Hand : MonoBehaviour
    {
        public Transform Held;

        public void PickUp(Transform item)
        {
            item.SetParent(transform);   // 손에 쥔다 / 手に握る
            Held = item;
        }

        public void Drop()
        {
            Held.SetParent(null, false);   // 손에서 떼어 땅에 둔다 / 手から離して地面に置く
            Held = null;
        }
    }

    [Lab(17, "검 줍기", Symptom = "주운 검이 손에 안 오고, 내려놓으면 엉뚱한 곳에서 작아진다")]
    public class Lab17_SwordPickup : Lab
    {
        Transform sword;

        public override IEnumerator Run()
        {
            Vector3 knightAt = Case(new Vector3(1f, 0f, 1f), new Vector3(-4f, 0f, 3f));
            var knight = Spawn("Knight", knightAt);
            knight.transform.localScale = Vector3.one * 2f;   // 거대화 물약 / 巨大化ポーション
            knight.transform.rotation = Quaternion.Euler(0f, Case(30f, -50f), 0f);
            var hand = Spawn<Hand>("Hand");
            hand.transform.SetParent(knight.transform, false);
            hand.transform.localPosition = new Vector3(0.4f, 0.8f, 0.3f);

            sword = Spawn("Sword", Case(new Vector3(5f, 0.05f, 2f), new Vector3(-1f, 0.05f, 7f))).transform;
            sword.rotation = Quaternion.Euler(0f, 70f, 90f);   // 바닥에 누워 있다 / 床に寝かせてある
            yield return null;

            hand.PickUp(sword);
            Report.ExpectNear("주운 직후 검 손잡이와 손 사이 거리(m)", 0f, Vector3.Distance(sword.position, hand.transform.position), 0.01f);
            Report.ExpectNear("손에 쥔 검과 손의 방향 차이(도)", 0f, Quaternion.Angle(sword.rotation, hand.transform.rotation), 0.5f);

            knight.transform.position += knight.transform.forward * 3f;
            yield return null;
            Vector3 handAtDrop = hand.transform.position;
            hand.Drop();
            Report.ExpectNear("내려놓은 검과 손이 있던 자리 사이 거리(m)", 0f, Vector3.Distance(sword.position, handAtDrop), 0.01f);
            Report.ExpectNear("내려놓은 검의 보이는 크기(lossyScale.x, 쥐고 있을 때 2)", 2f, sword.lossyScale.x, 0.01f);
        }

        void OnDestroy()
        {
            // 씬 루트로 떼어 낸 검은 랩 오브젝트와 함께 사라지지 않는다 / シーンのルートに切り離した剣は、ラボのオブジェクトと一緒には消えない
            if (sword != null)
            {
                Destroy(sword.gameObject);
            }
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// PickUp 과 Drop 의 SetParent 줄을 F10 으로 넘기면서 조사식에 item.position, item.localPosition, item.lossyScale, item.localScale 을 보세요.
// SetParent 전과 후에 바뀐 것은 월드 값인가요, 로컬 값인가요?
//
// PickUp と Drop の SetParent の行を F10 で進めながら、ウォッチ式で item.position、item.localPosition、item.lossyScale、item.localScale を見てください。
// SetParent の前と後で変わったのはワールドの値ですか、ローカルの値ですか?
#endregion

#region 힌트 2 / ヒント 2
// SetParent(parent) 는 SetParent(parent, worldPositionStays: true) 입니다. 월드 위치·회전·크기를 그대로 두고, 그렇게 보이도록 로컬 값을 다시 계산합니다.
// 그래서 주울 때는 검이 바닥 자리에 그대로 남습니다(부모만 손으로 바뀜).
// SetParent(parent, false) 는 로컬 값을 그대로 두고 새 부모에 붙입니다. 손에서 뗄 때 이걸 쓰면 손 기준 로컬 값(거의 0)이 이제 월드 원점 기준이 되고,
// 부모의 2배 크기가 빠지면서 보이는 크기도 절반이 됩니다.
//
// SetParent(parent) は SetParent(parent, worldPositionStays: true) です。ワールドの位置・回転・スケールをそのまま保ち、そう見えるようにローカルの値を計算し直します。
// そのため拾うときは剣が床の場所にそのまま残ります (親だけが手に変わる)。
// SetParent(parent, false) はローカルの値をそのままにして新しい親に付けます。手から離すときにこれを使うと、手を基準にしたローカルの値 (ほぼ 0) が今度はワールドの原点基準になり、
// 親の 2 倍のスケールが抜けて、見た目の大きさも半分になります。
#endregion

#region 힌트 3 / ヒント 3
// 붙일 때는 "로컬 자리에 끼운다", 뗄 때는 "보이는 그대로 둔다".
//   item.SetParent(transform, false); item.localPosition = Vector3.zero; item.localRotation = Quaternion.identity;   // 줍기
//   Held.SetParent(null);                                                                                           // 내려놓기 (월드 유지)
// SetParent(transform, false) 만으로는 부족합니다. 바닥에 있을 때의 로컬 값(= 월드 값)이 손 기준으로 남기 때문입니다.
//
// 付けるときは「ローカルの場所にはめる」、外すときは「見えるままにしておく」。
//   item.SetParent(transform, false); item.localPosition = Vector3.zero; item.localRotation = Quaternion.identity;   // 拾う
//   Held.SetParent(null);                                                                                           // 置く (ワールドを保つ)
// SetParent(transform, false) だけでは足りません。床にあったときのローカルの値 (= ワールドの値) が手を基準に残るからです。
#endregion
