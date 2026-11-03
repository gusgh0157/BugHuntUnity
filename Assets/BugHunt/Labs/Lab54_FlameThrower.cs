// 랩 54: 화염 방사기
// 화염 방사기는 매 프레임 불길 안의 적 모두에게 피해를 준다. 맞는 쪽이 무슨 컴포넌트인지 몰라도 되게 SendMessage 로 보냈다.
// 그런데 적이 피해를 받지 않는다. 에러도 경고도 없다.
// 기대: 불길 안의 적은 매 프레임 피해를 받고, 피해를 주는 데 GC 할당이 없다(프레임당 128바이트 이하).
// ラボ 54: 火炎放射器
// 火炎放射器は毎フレーム、炎の中の敵すべてにダメージを与える。受ける側が何のコンポーネントか知らなくて済むように SendMessage で送った。
// ところが敵がダメージを受けない。エラーも警告もない。
// 期待: 炎の中の敵は毎フレームダメージを受け、ダメージを与えるのに GC アロケーションがない (フレームあたり 128 バイト以下)。
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BugHunt.Labs.Lab54
{
    public class Enemy : MonoBehaviour
    {
        public float Health = 1000f;

        public void TakeDamage(float amount)
        {
            Health -= amount;
        }
    }

    public class FlameThrower : MonoBehaviour
    {
        public List<GameObject> InFlame = new List<GameObject>();
        public float DamagePerSecond = 60f;

        void Update()
        {
            for (int i = 0; i < InFlame.Count; ++i)
            {
                InFlame[i].SendMessage("TakeDamge", DamagePerSecond * Time.deltaTime, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    [Lab(54, "화염 방사기", Symptom = "적이 피해를 받지 않는다(에러 없음). 이름을 고쳐도 할당이 남는다")]
    public class Lab54_FlameThrower : Lab
    {
        float savedCaptureDeltaTime;

        public override IEnumerator Run()
        {
            savedCaptureDeltaTime = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            const long Budget = 128;

            var flamer = Spawn<FlameThrower>("FlameThrower");
            flamer.enabled = false;
            flamer.DamagePerSecond = Case(60f, 90f);
            var enemies = new List<Enemy>();
            for (int i = 0; i < Case(30, 45); ++i)
            {
                var enemy = Spawn<Enemy>($"Enemy_{i}");
                enemies.Add(enemy);
                flamer.InFlame.Add(enemy.gameObject);
            }

            var baseline = new GcMeter();
            yield return baseline.Measure(20);
            flamer.enabled = true;
            yield return null;
            var work = new GcMeter();
            yield return work.Measure(30);   // 앞뒤 두 프레임을 합쳐 33프레임 동안 탄다 / 前後のフレームを合わせて 33 フレームの間燃える
            flamer.enabled = false;
            long bytes = System.Math.Max(0, work.BytesPerFrame - baseline.BytesPerFrame);

            float burned = 1000f - enemies[0].Health;
            Report.Note(Loc.F("적 {0}마리, 프레임당 GC 할당 {1}바이트", enemies.Count, bytes));
            Report.ExpectTrue(Loc.F("불길 안의 적이 피해를 받음 (받은 피해 {0:0.0})", burned), burned > flamer.DamagePerSecond * 0.4f);
            Report.ExpectTrue(Loc.F("피해를 주는 데 GC 할당 ≤ {0}바이트/프레임", Budget), bytes <= Budget);
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
// Enemy.TakeDamage 에 중단점을 걸어 보세요. 멈추나요? 멈추지 않는다면 SendMessage 가 무엇을 찾고 있는지 문자열을 한 글자씩 읽어 보세요.
// SendMessageOptions.DontRequireReceiver 를 RequireReceiver 로 바꾸면 Console 에 무엇이 나오나요?
//
// Enemy.TakeDamage にブレークポイントを置いてみてください。止まりますか? 止まらないなら、SendMessage が何を探しているのか、文字列を 1 文字ずつ読んでみてください。
// SendMessageOptions.DontRequireReceiver を RequireReceiver に変えると、コンソールに何が出ますか?
#endregion

#region 힌트 2 / ヒント 2
// SendMessage 는 이름(문자열)으로 메서드를 찾아 리플렉션으로 부릅니다. 이름이 틀려도 컴파일러는 모르고, DontRequireReceiver 면 조용히 아무 일도 안 합니다.
// 이름을 고쳐도 끝이 아닙니다. 인자(float)는 object 로 박싱되어 매번 힙에 올라가고, 오브젝트의 모든 컴포넌트를 훑어 메서드를 찾습니다. Profiler 의 GC Alloc 열을 보세요.
// BroadcastMessage(자식까지), SendMessageUpwards(부모까지)도 같습니다.
//
// SendMessage は名前 (文字列) でメソッドを探し、リフレクションで呼びます。名前が間違っていてもコンパイラーは気づかず、DontRequireReceiver なら黙って何もしません。
// 名前を直しても終わりではありません。引数 (float) は object にボックス化されて毎回ヒープに載り、オブジェクトのすべてのコンポーネントをなめてメソッドを探します。プロファイラーの GC アロケーション (GC Alloc) 列を見てください。
// BroadcastMessage (子まで)、SendMessageUpwards (親まで) も同じです。
#endregion

#region 힌트 3 / ヒント 3
// "맞을 수 있는 것" 을 인터페이스로 만들고, 목록에 그 인터페이스를 담습니다.
//   public interface IDamageable { void TakeDamage(float amount); }
//   public class Enemy : MonoBehaviour, IDamageable { ... }
//   if (InFlame[i].TryGetComponent(out IDamageable target)) { target.TakeDamage(DamagePerSecond * Time.deltaTime); }
// 매 프레임 TryGetComponent 도 아끼려면 불길에 들어올 때(OnTriggerEnter) 한 번 찾아 List<IDamageable> 에 담아 둡니다.
// UnityEvent, C# event, 인터페이스는 이름 오타를 컴파일 시간에 잡아 줍니다.
//
// 「攻撃を受けられるもの」をインターフェイスにして、リストにそのインターフェイスを入れます。
//   public interface IDamageable { void TakeDamage(float amount); }
//   public class Enemy : MonoBehaviour, IDamageable { ... }
//   if (InFlame[i].TryGetComponent(out IDamageable target)) { target.TakeDamage(DamagePerSecond * Time.deltaTime); }
// 毎フレームの TryGetComponent も節約したいなら、炎に入ったとき (OnTriggerEnter) に 1 回探して List<IDamageable> に入れておきます。
// UnityEvent、C# の event、インターフェイスは名前のタイプミスをコンパイル時に捕まえてくれます。
#endregion
