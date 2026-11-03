// 랩 53: 탄환 풀
// 기관총이 1초에 20발을 쏜다. 탄환을 매번 만들고 부수면 GC 가 튀어 프레임이 끊기므로 오브젝트 풀(BulletPool)을 만들었다.
// 그런데 Profiler 를 보면 여전히 매 발 Instantiate 가 불리고, 풀은 늘 비어 있다.
// 기대: 동시에 날아다니는 탄환 수(12발)보다 많이 만들지 않는다. 다 쓴 탄환은 풀로 돌아가고 다시 꺼낼 때 처음 상태다.
// ラボ 53: 弾のプール
// 機関銃が 1 秒に 20 発撃つ。弾を毎回作って壊すと GC が跳ねてフレームが途切れるので、オブジェクトプール (BulletPool) を作った。
// ところがプロファイラーを見ると、相変わらず 1 発ごとに Instantiate が呼ばれ、プールはいつも空だ。
// 期待: 同時に飛んでいる弾の数 (12 発) より多くは作らない。使い終わった弾はプールに戻り、再び取り出すときは初期状態だ。
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BugHunt.Labs.Lab53
{
    public class Bullet : MonoBehaviour
    {
        public static int Created;   // 지금까지 Instantiate 된 수(랩이 센다) / これまでに Instantiate された数 (ラボが数える)

        public BulletPool Pool;
        public float Life;
        public Vector3 Velocity;

        void Awake()
        {
            Created++;
        }

        public void Launch(Vector3 position, Vector3 velocity, float life)
        {
            transform.position = position;
            Velocity = velocity;
            Life = life;
        }

        void Update()
        {
            transform.position += Velocity * Time.deltaTime;
            Life -= Time.deltaTime;
            if (Life <= 0f)
            {
                Destroy(gameObject);   // 수명이 다했다 / 寿命が尽きた
            }
        }
    }

    public class BulletPool : MonoBehaviour
    {
        public Bullet Prefab;
        readonly Stack<Bullet> idle = new Stack<Bullet>();

        public int IdleCount => idle.Count;

        public Bullet Get()
        {
            Bullet bullet = idle.Count > 0 ? idle.Pop() : Instantiate(Prefab, transform);
            bullet.Pool = this;
            bullet.gameObject.SetActive(true);
            return bullet;
        }

        public void Release(Bullet bullet)
        {
            bullet.gameObject.SetActive(false);
            idle.Push(bullet);
        }
    }

    [Lab(53, "탄환 풀", Symptom = "풀을 만들었는데도 매 발 Instantiate, 풀은 늘 비어 있다")]
    public class Lab53_BulletPool : Lab
    {
        float savedCaptureDeltaTime;

        public override IEnumerator Run()
        {
            savedCaptureDeltaTime = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;

            var prefab = Spawn<Bullet>("BulletPrefab");
            prefab.gameObject.SetActive(false);   // 원본은 꺼 둔다 / 元はオフにしておく
            var pool = Spawn<BulletPool>("BulletPool");
            pool.Prefab = prefab;
            Bullet.Created = 0;

            float life = Case(0.6f, 0.45f);
            int perSecond = Case(20, 30);
            int alive = Mathf.CeilToInt(life * perSecond) + 1;   // 동시에 날아다니는 최대 수 / 同時に飛んでいる最大数
            int shots = perSecond * 3;
            int framesPerShot = 60 / perSecond;
            yield return null;

            for (int i = 0; i < shots; ++i)
            {
                Bullet bullet = pool.Get();
                bullet.Launch(new Vector3(0f, 1f, 0f), new Vector3(0f, 0f, 40f), life);
                for (int f = 0; f < framesPerShot; ++f)
                {
                    yield return null;
                }
            }
            for (int f = 0; f < 60; ++f)
            {
                yield return null;   // 마지막 탄환이 수명을 다할 때까지 / 最後の弾が寿命を終えるまで
            }

            Report.Note(Loc.F("{0}발 발사, 수명 {1}초, 동시에 최대 {2}발", shots, life, alive));
            Report.ExpectTrue(Loc.F("만든 탄환 수 {0} ≤ {1}", Bullet.Created, alive), Bullet.Created <= alive);
            Report.Expect("다 쓴 탄환이 모두 풀로 돌아옴", Bullet.Created, pool.IdleCount);

            Bullet again = pool.Get();
            again.Launch(Vector3.zero, Vector3.forward, life);
            Report.ExpectTrue("풀에서 다시 꺼낸 탄환이 살아 있음", again != null && again.gameObject.activeSelf);
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
// BulletPool.Release 에 중단점을 걸어 보세요. 한 번이라도 멈추나요? 그렇다면 다 쓴 탄환은 어디로 갔을까요?
// Profiler → CPU → Hierarchy 에서 Instantiate, Destroy 를 검색하면 프레임마다 몇 번 불리는지(Calls) 보입니다. GC Alloc 열도 함께 보세요.
//
// BulletPool.Release にブレークポイントを置いてみてください。一度でも止まりますか? 止まらないなら、使い終わった弾はどこへ行ったのでしょう?
// プロファイラー (Profiler) → CPU → ヒエラルキー (Hierarchy) で Instantiate、Destroy を検索すると、フレームごとに何回呼ばれているか (呼び出し / Calls) が見えます。GC アロケーション (GC Alloc) 列も一緒に見てください。
#endregion

#region 힌트 2 / ヒント 2
// 풀을 만들어 놓고 돌려주는 쪽을 고치지 않았습니다. Bullet 은 예전처럼 Destroy(gameObject) 로 자신을 부숩니다.
// 풀이 늘 비어 있으니 Get 은 매번 Instantiate 합니다. 풀이 있는 척만 하고 비용은 그대로입니다.
//
// プールを作っておきながら、返す側を直していません。Bullet は以前のように Destroy(gameObject) で自分を壊しています。
// プールがいつも空なので、Get は毎回 Instantiate します。プールがあるふりをしているだけで、コストはそのままです。
#endregion

#region 힌트 3 / ヒント 3
//   if (Life <= 0f) { Pool.Release(this); }
// 풀에서 꺼낸 오브젝트는 이전 상태(위치, 속도, 남은 수명, 자식 이펙트, 트레일)를 그대로 갖고 있으니 Launch 같은 초기화 함수로 모두 되돌립니다.
// Unity 에는 UnityEngine.Pool.ObjectPool<T> 가 있습니다(createFunc, actionOnGet, actionOnRelease, actionOnDestroy).
//
//   if (Life <= 0f) { Pool.Release(this); }
// プールから取り出したオブジェクトは以前の状態 (位置、速度、残りの寿命、子のエフェクト、トレイル) をそのまま持っているので、Launch のような初期化関数ですべて元に戻します。
// Unity には UnityEngine.Pool.ObjectPool<T> があります (createFunc, actionOnGet, actionOnRelease, actionOnDestroy)。
#endregion
