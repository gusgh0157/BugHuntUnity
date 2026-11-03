// 랩 8: 궁수
// 궁수가 쓰러진 허수아비를 계속 쏘면서 점수를 번다. 나머지 허수아비는 그대로 서 있다.
// 기대: 화살 6발로 허수아비 3개(HP 20)를 모두 쓰러뜨리고 점수 300.
// ラボ 8: 弓兵
// 弓兵が倒れたかかしを撃ち続けてスコアを稼ぐ。残りのかかしはそのまま立っている。
// 期待: 矢 6 本でかかし 3 体 (HP 20) をすべて倒し、スコア 300。
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BugHunt.Labs.Lab08
{
    public class Dummy : MonoBehaviour
    {
        public int Health = 20;
        bool fallen;

        // 쓰러졌으면 true
        // 倒れたら true
        public bool TakeHit(int damage)
        {
            Health -= damage;
            if (Health > 0)
            {
                return false;
            }
            if (!fallen)
            {
                fallen = true;   // Destroy 를 두 번 부르지 않게 / Destroy を 2 回呼ばないように
                Destroy(gameObject);
            }
            return true;
        }
    }

    public class Archer : MonoBehaviour
    {
        public List<Dummy> Field = new List<Dummy>();
        public Dummy Target;
        public int Score;

        public void ShootArrow()
        {
            Target ??= FindNearest();
            if (Target?.TakeHit(10) == true)
            {
                Score += 100;
            }
        }

        Dummy FindNearest()
        {
            return Field
                .Where(d => d != null)
                .OrderBy(d => Vector3.Distance(d.transform.position, transform.position))
                .FirstOrDefault();
        }
    }

    [Lab(8, "궁수", Symptom = "쓰러진 허수아비로 점수를 계속 번다")]
    public class Lab08_Archer : Lab
    {
        public override IEnumerator Run()
        {
            var archer = Spawn<Archer>("Archer");
            int dummies = Case(3, 4);
            for (int i = 0; i < dummies; ++i)
            {
                archer.Field.Add(Spawn<Dummy>($"Dummy_{i}", new Vector3(i * 2, 0, 10)));
            }

            for (int arrow = 1; arrow <= dummies * 2; ++arrow)
            {
                archer.ShootArrow();
                yield return null;   // 화살은 한 프레임에 한 발 / 矢は 1 フレームに 1 本
            }

            int standing = archer.Field.Count(d => d != null);
            Report.Expect("서 있는 허수아비", 0, standing);
            Report.Expect("점수", dummies * 100, archer.Score);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// Score += 100 줄에 중단점. 두 번째로 멈췄을 때 Target 의 이름과 Health 는? 그 허수아비는 Hierarchy 에 아직 있나요?
//
// Score += 100 の行にブレークポイント。2 回目に止まったとき、Target の名前と Health は? そのかかしはヒエラルキー (Hierarchy) にまだありますか?
#endregion

#region 힌트 2 / ヒント 2
// 멈춘 상태에서 조사식에 다음 셋을 넣고 비교하세요.
//   Target == null              ← Unity 가 오버로드한 ==: "파괴됐나"까지 본다
//   (object)Target == null      ← C# 의 참조 비교: "변수가 비었나"만 본다
//   Target is null
// 파괴된 Unity 오브젝트는 C# 쪽 껍데기만 남아 있어서 "fake null" 이라고 부릅니다.
//
// 止まった状態で、ウォッチ式に次の 3 つを入れて比べてください。
//   Target == null              ← Unity がオーバーロードした ==: 「破棄されたか」まで見る
//   (object)Target == null      ← C# の参照比較: 「変数が空か」だけを見る
//   Target is null
// 破棄された Unity オブジェクトは C# 側の抜け殻だけが残っているので、"fake null" と呼びます。
#endregion

#region 힌트 3 / ヒント 3
// ?. ?? ??= is null 은 Unity 의 == 를 거치지 않고 C# 참조만 봅니다. 그래서 파괴된 오브젝트를 "살아 있다"고 판단합니다.
// UnityEngine.Object 에는 if (Target == null) Target = FindNearest(); 처럼 == 를 쓰세요.
// (VS/Rider 의 Unity 분석기가 문제의 두 줄(??=, ?.)에 UNT0023 / UNT0008 제안을 띄웁니다. 물결 밑줄이 보이나요?)
// 참고: Destroy 는 프레임 끝에 일어납니다. 같은 프레임 안에서는 == null 도 아직 false 입니다.
//
// ?. ?? ??= is null は Unity の == を通らず、C# の参照だけを見ます。そのため破棄されたオブジェクトを「生きている」と判断します。
// UnityEngine.Object には if (Target == null) Target = FindNearest(); のように == を使ってください。
// (VS/Rider の Unity アナライザーが問題の 2 行 (??=、?.) に UNT0023 / UNT0008 の提案を出します。波線の下線が見えますか?)
// 参考: Destroy はフレームの終わりに行われます。同じフレーム内では == null もまだ false です。
#endregion
