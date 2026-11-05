// 랩 36: 가시 함정
// 플레이어가 가시 함정 위를 걸어 지나가는데 함정이 발동하지 않는다. OnTriggerEnter 에 중단점을 걸어도 멈추지 않는다.
// 함정의 콜라이더는 Is Trigger 가 켜져 있고, 플레이어도 콜라이더가 있다.
// 기대: 함정을 지나가면 한 번 발동해 플레이어 HP 가 줄어든다.
// ラボ 36: トゲの罠
// プレイヤーがトゲの罠の上を歩いて通り過ぎるのに、罠が発動しない。OnTriggerEnter にブレークポイントを置いても止まらない。
// 罠のコライダーは トリガーにする (Is Trigger) がオンで、プレイヤーにもコライダーがある。
// 期待: 罠を通り過ぎると 1 回発動して、プレイヤーの HP が減る。
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab36
{
    public class Player : MonoBehaviour
    {
        public int Health = 100;
        public float Speed = 4f;

        void Update()
        {
            transform.position += Vector3.right * Speed * Time.deltaTime;   // 직접 걷는다(CharacterController 없이) / 自分で歩く (CharacterController なし)
        }
    }

    public class SpikeTrap : MonoBehaviour
    {
        public int Damage = 30;
        public int Triggered;

        void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out Player player))
            {
                Triggered++;
                player.Health -= Damage;
            }
        }
    }

    [Lab(36, "가시 함정", Symptom = "함정 위를 지나가도 OnTriggerEnter 가 불리지 않는다")]
    public class Lab36_SpikeTrap : Lab
    {
        float savedCaptureDeltaTime;

        public override IEnumerator Run()
        {
            savedCaptureDeltaTime = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;

            float trapX = Case(4f, 6f);
            var trap = SpawnPrimitive(PrimitiveType.Cube, "SpikeTrap", new Vector3(trapX, 0f, 0f)).AddComponent<SpikeTrap>();
            trap.transform.localScale = new Vector3(1f, 0.5f, 2f);
            trap.GetComponent<Collider>().isTrigger = true;
            trap.Damage = Case(30, 45);

            var player = SpawnPrimitive(PrimitiveType.Capsule, "Player", Vector3.zero).AddComponent<Player>();
            yield return new WaitForSeconds((trapX + 2f) / player.Speed);   // 함정을 지나 2m 더 간다 / 罠を通り過ぎてさらに 2m 進む

            Report.Expect("함정 발동 횟수", 1, trap.Triggered);
            Report.Expect("플레이어 HP", 100 - trap.Damage, player.Health);
        }

        void OnDestroy()
        {
            Time.captureDeltaTime = savedCaptureDeltaTime;
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// 중단점이 한 번도 걸리지 않을 때는 "불렸는데 조건이 틀렸다" 가 아니라 "애초에 불리지 않았다" 입니다. 엔진이 언제 이 메시지를 보내는지부터 확인하세요.
// Window → Analysis → Physics Debugger 를 열고 플레이 중에 Player 와 SpikeTrap 을 골라 보세요. 각각 어떤 종류(Static / Kinematic / Dynamic)로 나오나요?
//
// ブレークポイントで一度も止まらないときは、「呼ばれたが条件が違った」ではなく「そもそも呼ばれていない」です。エンジンがいつこのメッセージを送るのかから確かめてください。
// ウィンドウ → 分析 → 物理演算デバッガー (Physics Debugger) を開き、再生中に Player と SpikeTrap を選んでみてください。それぞれどの種類 (Static / Kinematic / Dynamic) と表示されますか?
#endregion

#region 힌트 2 / ヒント 2
// 충돌·트리거 메시지는 둘 중 적어도 한쪽에 Rigidbody 가 있어야 생깁니다. Rigidbody 가 없는 콜라이더는 "움직이지 않는 정적 콜라이더" 로 취급되고,
// 정적 콜라이더끼리는 서로 겹쳐도 검사하지 않습니다(Unity 매뉴얼의 "충돌 액션 매트릭스" 표).
// 플레이어를 transform 으로 움직여도 콜라이더는 따라가지만, 물리 엔진 입장에서는 여전히 정적 콜라이더 두 개입니다.
// 레이어가 다르다면 Project Settings → Physics → Layer Collision Matrix 에서 두 레이어의 칸도 확인하세요(이 랩은 둘 다 Default).
//
// 衝突・トリガーのメッセージは、2 つのうち少なくとも片方に Rigidbody がないと発生しません。Rigidbody のないコライダーは「動かない静的コライダー」として扱われ、
// 静的コライダー同士は重なってもチェックされません (Unity マニュアルの「衝突アクションマトリクス」の表)。
// プレイヤーを transform で動かしてもコライダーはついていきますが、物理エンジンから見れば相変わらず静的コライダー 2 つです。
// レイヤーが違うなら、プロジェクト設定 → 物理 → レイヤーの衝突表 (Layer Collision Matrix) で 2 つのレイヤーのマスも確認してください (このラボはどちらも Default)。
#endregion

#region 힌트 3 / ヒント 3
// 움직이는 쪽(플레이어)에 Kinematic Rigidbody 를 붙입니다. 물리에 밀리지 않고 코드로 움직이면서 트리거 메시지를 받을 수 있습니다.
//   var rb = gameObject.AddComponent<Rigidbody>(); rb.isKinematic = true;   // Player.Awake 에서(또는 인스펙터에서 추가)
// CharacterController 로 움직이는 캐릭터도 트리거 메시지를 받습니다(CharacterController 는 그 자체로 메시지를 보냅니다).
//
// 動く側 (プレイヤー) にキネマティック (Kinematic) の Rigidbody を付けます。物理に押されず、コードで動かしながらトリガーのメッセージを受け取れます。
//   var rb = gameObject.AddComponent<Rigidbody>(); rb.isKinematic = true;   // Player.Awake で (またはインスペクターで追加)
// CharacterController で動くキャラクターもトリガーのメッセージを受け取ります (CharacterController はそれ自体でメッセージを送ります)。
#endregion
