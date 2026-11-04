// 랩 49: 등 뒤의 발소리
// 경비병이 화면 밖(플레이어 등 뒤)에서 걸어오는데 발소리가 하나도 나지 않는다. 카메라를 돌려 경비병을 보면 그제야 발소리가 난다.
// 기대: 걷기 애니메이션 1초에 발소리 이벤트 2번. 2초 남짓이면 4번 이상.
// ラボ 49: 背後の足音
// 警備兵が画面外 (プレイヤーの背後) から歩いてくるのに、足音がまったく鳴らない。カメラを回して警備兵を見ると、そこで初めて足音が鳴る。
// 期待: 歩きアニメーション 1 秒で足音イベント 2 回。2 秒ちょっとなら 4 回以上。
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab49
{
    public class Guard : MonoBehaviour
    {
        public int Footsteps;

        void Awake()
        {
            // 최적화: 화면 밖 NPC 의 애니메이션은 계산하지 않는다.
            // 最適化: 画面外の NPC のアニメーションは計算しない。
            GetComponent<Animator>().cullingMode = AnimatorCullingMode.CullCompletely;
        }

        // 걷기 클립의 Animation Event 가 부른다. (실제 게임이라면 여기서 발소리를 내고 주변에 소음을 알린다)
        // 歩きクリップの Animation Event が呼ぶ。(実際のゲームなら、ここで足音を鳴らして周囲に物音を知らせる)
        public void Footstep()
        {
            ++Footsteps;
        }
    }

    [Lab(49, "등 뒤의 발소리", Symptom = "화면 밖 경비병의 발소리가 안 난다")]
    public class Lab49_Footsteps : Lab
    {
        public override IEnumerator Run()
        {
            // 플레이어 카메라는 +Z 를 본다. 경비병은 한참 뒤(-Z)에 있다.
            // プレイヤーのカメラは +Z を向いている。警備兵はずっと後ろ (-Z) にいる。
            var cam = Spawn<Camera>("PlayerCamera", new Vector3(0, 1, 0));
            cam.transform.rotation = Quaternion.identity;

            GameObject guard = Spawn("Guard", new Vector3(0, 0, -60));
            GameObject body = SpawnPrimitive(PrimitiveType.Capsule, "Body");
            body.transform.SetParent(guard.transform, false);
            var animator = guard.AddComponent<Animator>();
            animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("BugHunt/Animation/Guard");
            var logic = guard.AddComponent<Guard>();

            yield return new WaitForSeconds(2.1f);

            Report.Note(Loc.F("경비병이 보이는가 (Renderer.isVisible): {0}", body.GetComponent<Renderer>().isVisible));
            Report.ExpectTrue(Loc.F("발소리 이벤트 4번 이상 (실제 {0}번)", logic.Footsteps), logic.Footsteps >= 4);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// Guard.Footstep 에 중단점. 한 번이라도 멈추나요?
// 랩이 도는 동안 Scene 뷰를 경비병(z = -60) 쪽으로 돌려 보세요. 그러면 멈출 수도 있습니다. 왜 "보느냐"에 따라 달라질까요?
//
// Guard.Footstep にブレークポイント。一度でも止まりますか?
// ラボの実行中に、シーンビューを警備兵 (z = -60) のほうへ向けてみてください。すると止まるかもしれません。なぜ「見ているかどうか」で変わるのでしょう?
#endregion

#region 힌트 2 / ヒント 2
// 플레이 중 Hierarchy 에서 Lab21/Guard 를 고르고 Inspector 의 Animator 컴포넌트를 보세요. Culling Mode 는?
// 그리고 Animator 창(Window → Animation → Animator)에서 Walk 상태의 진행 막대가 움직이나요?
// Culling Mode:  Always Animate      항상 계산
//                Cull Update Transforms  안 보이면 뼈 위치 쓰기·IK 생략(상태 머신과 이벤트는 돈다)
//                Cull Completely     안 보이면 전부 멈춤(이벤트도 없음)
//
// 再生中にヒエラルキーで Lab21/Guard を選び、インスペクターの Animator コンポーネントを見てください。カリングモード (Culling Mode) は?
// それから、アニメーターウィンドウ (ウィンドウ → アニメーション → アニメーター) で、Walk ステートの進行バーは動いていますか?
// カリングモード:  常にアニメーション化 (Always Animate)               常に計算
//                  トランスフォーム更新を除外 (Cull Update Transforms)  見えなければボーン位置の書き込み・IK を省略 (ステートマシンとイベントは動く)
//                  完全に除外 (Cull Completely)                       見えなければすべて停止 (イベントも出ない)
#endregion

#region 힌트 3 / ヒント 3
// 화면 밖에서도 게임 로직(발소리, 공격 판정, 이동)을 Animation Event 에 기대는 캐릭터는 Cull Completely 를 쓰면 안 됩니다.
// Cull Update Transforms 로 바꾸면 비용은 줄이면서 이벤트는 유지됩니다(그래도 안 되면 Always Animate).
// 판정이 중요한 로직은 애니메이션 이벤트가 아니라 코드의 타이머로 돌리는 설계도 고려하세요.
// 참고: 에디터에서는 Scene 뷰 카메라도 "보는 것"으로 칩니다. 빌드에서와 결과가 다를 수 있습니다.
//
// 画面外でもゲームロジック (足音、攻撃判定、移動) を Animation Event に頼っているキャラクターは、完全に除外 (Cull Completely) を使ってはいけません。
// トランスフォーム更新を除外 (Cull Update Transforms) に変えると、コストを抑えつつイベントは維持されます (それでもだめなら 常にアニメーション化 (Always Animate))。
// 判定が重要なロジックは、アニメーションイベントではなくコードのタイマーで動かす設計も検討してください。
// 参考: エディターではシーンビューのカメラも「見ている」ものとして扱われます。ビルドとは結果が異なることがあります。
#endregion
