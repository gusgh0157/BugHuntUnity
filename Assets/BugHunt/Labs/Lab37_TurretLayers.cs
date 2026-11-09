// 랩 37: 포탑의 조준
// 포탑은 Enemy 레이어(8번)만 맞히도록 레이캐스트에 레이어를 넘겼다. 그런데 앞에 있는 유리창(3번 레이어)에 맞고 적은 못 맞힌다.
// 레이어를 10번으로 옮겨도 마찬가지로 엉뚱한 것을 맞힌다.
// 기대: 포탑의 레이캐스트는 Enemy 레이어만 맞힌다. 유리창은 무시한다.
// ラボ 37: 砲台の照準
// 砲台は Enemy レイヤー (8 番) だけに当たるように、レイキャストにレイヤーを渡した。ところが手前にある窓ガラス (3 番レイヤー) に当たり、敵には当たらない。
// レイヤーを 10 番に移しても、同じように見当違いのものに当たる。
// 期待: 砲台のレイキャストは Enemy レイヤーだけに当たる。窓ガラスは無視する。
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab37
{
    public class Turret : MonoBehaviour
    {
        public int EnemyLayer = 8;
        public float Range = 50f;

        public string Aim()
        {
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, Range, EnemyLayer))
            {
                return hit.collider.name;
            }
            return null;
        }
    }

    [Lab(37, "포탑의 조준", "タレットの照準", Symptom = "Enemy 레이어만 쏘라고 했는데 유리창에 맞는다", SymptomJa = "Enemy レイヤーだけを撃つよう指定したのに窓ガラスに当たる")]
    public class Lab37_TurretLayers : Lab
    {
        public override IEnumerator Run()
        {
            const int GlassLayer = 3;
            int enemyLayer = Case(8, 10);

            var glass = SpawnPrimitive(PrimitiveType.Cube, "Glass", new Vector3(0f, 0f, 3f));
            glass.transform.localScale = new Vector3(3f, 3f, 0.05f);
            glass.layer = GlassLayer;
            var enemy = SpawnPrimitive(PrimitiveType.Capsule, "Enemy", new Vector3(0f, 0f, Case(9f, 14f)));
            enemy.layer = enemyLayer;

            var turret = Spawn<Turret>("Turret");
            turret.EnemyLayer = enemyLayer;
            yield return new WaitForFixedUpdate();   // 새로 만든 콜라이더가 물리 장면에 들어가도록 / 新しく作ったコライダーが物理シーンに入るように

            Report.Note(Loc.F("유리창 레이어 {0}, 적 레이어 {1}", "窓ガラスのレイヤー {0}、敵のレイヤー {1}", GlassLayer, enemyLayer));
            Report.Expect(Loc.L("포탑이 맞힌 것", "砲台が当てたもの"), "Enemy", turret.Aim());
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// Aim 에 중단점을 걸고 조사식에 EnemyLayer 와 Convert.ToString(EnemyLayer, 2)(2진수)를 넣어 보세요. 어느 비트가 켜져 있나요?
// Debug.DrawRay(transform.position, transform.forward * Range, Color.red, 5f) 로 레이를 그리고, Physics Debugger 의 Queries 탭에서 레이캐스트가 맞은 곳을 보세요.
//
// Aim にブレークポイントを置き、ウォッチ式に EnemyLayer と Convert.ToString(EnemyLayer, 2) (2 進数) を入れてみてください。どのビットが立っていますか?
// Debug.DrawRay(transform.position, transform.forward * Range, Color.red, 5f) でレイを描き、物理演算デバッガー (Physics Debugger) の Queries タブでレイキャストが当たった場所を見てください。
#endregion

#region 힌트 2 / ヒント 2
// Raycast 의 layerMask 인자는 "레이어 번호" 가 아니라 "비트 마스크" 입니다. n 번 레이어 = n 번째 비트 = 1 << n.
// 8 을 넘기면 2진수 1000 = 3번 비트 = 3번 레이어만 검사합니다. 10 은 1010 = 1번과 3번 레이어입니다.
// int 를 받는 자리라 컴파일러도 막지 않고, 예외도 없습니다.
// 같은 혼동: gameObject.layer 에는 번호를, LayerMask 필드·Physics 쿼리·Camera.cullingMask 에는 마스크를 씁니다.
//
// Raycast の layerMask 引数は「レイヤー番号」ではなく「ビットマスク」です。n 番レイヤー = n 番目のビット = 1 << n。
// 8 を渡すと 2 進数 1000 = 3 番ビット = 3 番レイヤーだけをチェックします。10 は 1010 = 1 番と 3 番のレイヤーです。
// int を受け取る引数なのでコンパイラーも止めず、例外もありません。
// 同じ混同: gameObject.layer には番号を、LayerMask フィールド・Physics のクエリ・Camera.cullingMask にはマスクを使います。
#endregion

#region 힌트 3 / ヒント 3
//   if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, Range, 1 << EnemyLayer))
// 더 좋은 방법: 인스펙터에서 고르는 LayerMask 필드를 씁니다. public LayerMask EnemyMask;  (코드로는 LayerMask.GetMask("Enemy"))
// 레이어에 이름을 붙이면(Project Settings → Tags and Layers) 숫자를 직접 쓸 일이 줄어듭니다.
//
//   if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, Range, 1 << EnemyLayer))
// もっとよい方法: インスペクターで選ぶ LayerMask フィールドを使います。public LayerMask EnemyMask;  (コードでは LayerMask.GetMask("Enemy"))
// レイヤーに名前を付けると (プロジェクト設定 → タグとレイヤー (Tags and Layers))、数字を直接書くことが減ります。
#endregion
