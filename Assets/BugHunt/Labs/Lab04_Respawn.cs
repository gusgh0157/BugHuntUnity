// 랩 4: 부활
// 플레이어가 쓰러지면 몸을 숨기고 0.5초 뒤에 시작 지점에서 부활해야 한다. 그런데 영영 돌아오지 않는다.
// 기대: 1초 뒤 플레이어가 살아 있고, 보이고, 시작 지점에 있다.
// ラボ 4: リスポーン
// プレイヤーが倒れたら体を隠し、0.5 秒後に開始地点でリスポーンしなければならない。ところが二度と戻ってこない。
// 期待: 1 秒後にプレイヤーが生きていて、見えていて、開始地点にいる。
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab04
{
    public class Player : MonoBehaviour
    {
        public Vector3 SpawnPoint;
        public GameObject Model;   // 보이는 몸 (자식 오브젝트) / 見える体 (子オブジェクト)
        public bool Alive = true;
        public int Respawns;

        public void Die()
        {
            Alive = false;
            gameObject.SetActive(false);   // 쓰러진 몸을 숨긴다 / 倒れた体を隠す
            StartCoroutine(RespawnAfter(0.5f));
        }

        IEnumerator RespawnAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            transform.position = SpawnPoint;
            gameObject.SetActive(true);
            Model.SetActive(true);
            Alive = true;
            ++Respawns;
        }
    }

    [Lab(4, "부활", Symptom = "쓰러진 플레이어가 영영 부활하지 않는다(에러 한 줄)")]
    public class Lab04_Respawn : Lab
    {
        public override IEnumerator Run()
        {
            var player = Spawn<Player>("Player", new Vector3(3, 0, 7));
            player.SpawnPoint = Case(Vector3.zero, new Vector3(-2, 0, 4));
            player.Model = SpawnPrimitive(PrimitiveType.Capsule, "Model");
            player.Model.transform.SetParent(player.transform, false);
            yield return null;

            Report.Note("플레이어가 함정에 빠졌다.");
            player.Die();
            yield return new WaitForSeconds(1f);

            Report.Expect("부활 횟수", 1, player.Respawns);
            Report.ExpectTrue("살아 있음", player.Alive);
            Report.ExpectTrue("몸이 보임", player.isActiveAndEnabled && player.Model.activeInHierarchy);
            Report.Expect("위치", player.SpawnPoint, player.transform.position);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// 이번엔 예외가 아니라 빨간 "에러" 한 줄입니다. 메시지를 그대로 읽어 보세요. 어떤 게임 오브젝트가 어떤 상태라서 무엇을 못 했나요?
// 스택 트레이스의 내 코드 줄로 가서, 그 바로 위 줄이 무엇을 하는지 보세요.
//
// 今回は例外ではなく、赤い「エラー」が 1 行です。メッセージをそのまま読んでみてください。どのゲームオブジェクトがどんな状態だったせいで、何ができなかったのですか?
// スタックトレースの自分のコードの行に移動して、そのすぐ上の行が何をしているか見てください。
#endregion

#region 힌트 2 / ヒント 2
// 두 줄의 순서를 바꿔 보세요(먼저 StartCoroutine, 그다음 SetActive(false)). 에러는 사라집니다. 그런데 [PASS] 가 나오나요?
// RespawnAfter 의 첫 줄과 yield 다음 줄에 중단점을 걸어 보세요. 두 번째 중단점에 도착하나요?
// 코루틴은 그것을 시작한 MonoBehaviour 의 것입니다. 그 게임 오브젝트가 비활성화되면 코루틴은 "멈추는" 게 아니라 "끝납니다". 다시 켜도 이어지지 않습니다.
//
// 2 行の順序を入れ替えてみてください (先に StartCoroutine、次に SetActive(false))。エラーは消えます。でも [PASS] は出ますか?
// RespawnAfter の 1 行目と yield の次の行にブレークポイントを置いてみてください。2 つ目のブレークポイントに到達しますか?
// コルーチンはそれを開始した MonoBehaviour のものです。そのゲームオブジェクトが非アクティブになると、コルーチンは「一時停止する」のではなく「終了します」。もう一度オンにしても続きません。
#endregion

#region 힌트 3 / ヒント 3
// 기다리는 동안 꺼지면 안 되는 오브젝트에서 코루틴을 돌리거나(항상 켜져 있는 매니저의 StartCoroutine),
// 루트 오브젝트 대신 "보이는 부분"만 숨기세요. 이 플레이어에는 이미 Model 자식이 있습니다.
//
// 待っている間にオフになってはいけないオブジェクトでコルーチンを回すか (常にオンのマネージャーの StartCoroutine)、
// ルートオブジェクトの代わりに「見える部分」だけを隠してください。このプレイヤーにはすでに Model という子があります。
#endregion
