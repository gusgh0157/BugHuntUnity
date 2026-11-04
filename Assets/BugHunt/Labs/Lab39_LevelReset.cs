// 랩 39: 퍼즐 초기화
// 퍼즐을 초기화하면 상자들이 처음 자리로 돌아간다. 초기화 버튼을 누른 그 프레임에 플레이어가 바로 상자를 집으려 하면 허공을 집는다.
// 반대로 상자가 있던 옛 자리를 집으면 상자가 집힌다. 한 프레임 뒤에는 멀쩡하다.
// 기대: 초기화 직후에도 상자가 보이는 자리에서 집힌다.
// ラボ 39: パズルのリセット
// パズルをリセットすると箱が最初の位置に戻る。リセットボタンを押したそのフレームにプレイヤーがすぐ箱をつかもうとすると、何もない空中をつかむ。
// 逆に箱があった元の位置をつかむと箱がつかめる。1 フレーム後には正常になる。
// 期待: リセット直後でも、箱が見えている位置でつかめる。
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab39
{
    public class Puzzle : MonoBehaviour
    {
        public Transform[] Crates;
        public Vector3[] StartPositions;

        public void ResetCrates()
        {
            for (int i = 0; i < Crates.Length; ++i)
            {
                Crates[i].position = StartPositions[i];
            }
        }
    }

    public static class Grabber
    {
        public static string Grab(Vector3 eye, Vector3 direction)
        {
            return Physics.Raycast(eye, direction, out RaycastHit hit, 20f) ? hit.collider.name : null;
        }
    }

    [Lab(39, "퍼즐 초기화", Symptom = "초기화한 그 프레임에 상자를 집으면 허공을, 옛 자리를 집으면 상자를 집는다")]
    public class Lab39_LevelReset : Lab
    {
        public override IEnumerator Run()
        {
            Vector3 start = Case(new Vector3(0f, 0f, 4f), new Vector3(-3f, 0f, 6f));
            Vector3 moved = Case(new Vector3(8f, 0f, 4f), new Vector3(5f, 0f, 9f));

            var crate = SpawnPrimitive(PrimitiveType.Cube, "Crate", moved);   // 플레이어가 밀어 둔 자리 / プレイヤーが押して動かした位置
            var puzzle = Spawn<Puzzle>("Puzzle");
            puzzle.Crates = new[] { crate.transform };
            puzzle.StartPositions = new[] { start };
            yield return new WaitForFixedUpdate();

            puzzle.ResetCrates();
            // 같은 프레임에 집는다.
            // 同じフレームでつかむ。
            string atStart = Grabber.Grab(new Vector3(start.x, 0f, 0f), Vector3.forward);
            string atOld = Grabber.Grab(new Vector3(moved.x, 0f, 0f), Vector3.forward);

            Report.Expect("상자가 돌아간 자리에서 집은 것", "Crate", atStart);
            Report.Expect("상자가 있던 옛 자리에서 집은 것", null, atOld);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// ResetCrates 바로 뒤에 중단점을 걸고 조사식에 crate.transform.position 과 crate.GetComponent<Collider>().bounds.center 를 나란히 넣어 보세요.
// 같은 자리를 가리키나요? Physics Debugger 를 켜고 일시정지한 상태에서 한 프레임씩(Ctrl+Alt+P) 넘기며 콜라이더가 언제 따라오는지 보세요.
//
// ResetCrates の直後にブレークポイントを置き、ウォッチ式に crate.transform.position と crate.GetComponent<Collider>().bounds.center を並べて入れてみてください。
// 同じ位置を指していますか? 物理演算デバッガー (Physics Debugger) をオンにし、一時停止した状態で 1 フレームずつ (Ctrl+Alt+P) 進めながら、コライダーがいつ追いつくかを見てください。
#endregion

#region 힌트 2 / ヒント 2
// 물리 엔진은 Unity 의 transform 과 따로 자기 장면(PhysX scene)을 갖고 있습니다. transform 을 바꾸면 그 변경은 모아 두었다가
// 다음 물리 스텝 직전(또는 SyncTransforms 를 부를 때)에 한꺼번에 물리 장면으로 옮겨집니다.
// 그 사이에 하는 Raycast, OverlapSphere 같은 쿼리는 옛 자리의 콜라이더를 봅니다. Physics.autoSyncTransforms 를 켜면 쿼리마다 동기화하지만, 2018.3 부터 기본값이 꺼져 있고 쿼리마다 비용이 커서 권장하지 않습니다.
//
// 物理エンジンは Unity の transform とは別に、自分のシーン (PhysX scene) を持っています。transform を変えると、その変更はためておかれ、
// 次の物理ステップの直前 (または SyncTransforms を呼んだとき) にまとめて物理シーンへ反映されます。
// その間に行う Raycast や OverlapSphere のようなクエリは、元の位置のコライダーを見ます。Physics.autoSyncTransforms をオンにするとクエリのたびに同期しますが、2018.3 から既定値はオフで、クエリごとのコストが大きいので推奨されません。
#endregion

#region 힌트 3 / ヒント 3
// transform 을 바꾼 뒤 같은 프레임에 물리 쿼리를 하려면 동기화를 직접 부릅니다.
//   puzzle.ResetCrates(); Physics.SyncTransforms();    // ResetCrates 끝에 넣어도 된다
// 오브젝트가 Rigidbody 를 갖고 있다면 rb.position = start 로 옮기는 편이 낫습니다(물리 쪽 위치가 바로 바뀝니다).
// SyncTransforms 는 바뀐 transform 전부를 옮기므로 매 프레임 여러 번 부르면 비쌉니다. 쿼리 전에 한 번만.
//
// transform を変えた後、同じフレームで物理クエリを行うなら、同期を自分で呼びます。
//   puzzle.ResetCrates(); Physics.SyncTransforms();    // ResetCrates の最後に入れてもよい
// オブジェクトが Rigidbody を持っているなら、rb.position = start で動かすほうがよいです (物理側の位置がすぐ変わります)。
// SyncTransforms は変わった transform をすべて反映するので、毎フレーム何度も呼ぶと重くなります。クエリの前に 1 回だけ。
#endregion
