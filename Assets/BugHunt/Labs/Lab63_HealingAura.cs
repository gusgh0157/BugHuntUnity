// 랩 63: 치유 오라
// 사제 8명이 둘러선 아군을 매 프레임 치유한다(치유 진형). 기능은 맞는데 Profiler 의 GC Alloc 열에 Physics.OverlapSphere 가 매 프레임 찍힌다.
// 기대: 범위 안의 아군은 모두 치유되고, 범위 밖은 치유되지 않으며, 찾는 데 GC 할당이 없다(프레임당 128바이트 이하).
// ラボ 63: 回復オーラ
// 司祭 8 人が、周りを囲む味方を毎フレーム回復する (回復陣形)。機能は正しいのに、プロファイラーの GC アロケーション (GC Alloc) 列に Physics.OverlapSphere が毎フレーム出る。
// 期待: 範囲内の味方はすべて回復し、範囲外は回復せず、探すのに GC アロケーションがない (フレームあたり 128 バイト以下)。
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BugHunt.Labs.Lab63
{
    public class Ally : MonoBehaviour
    {
        public float Health = 10f;
    }

    public class HealingAura : MonoBehaviour
    {
        public float Radius = 6f;
        public float HealPerSecond = 5f;

        void Update()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, Radius);
            for (int i = 0; i < hits.Length; ++i)
            {
                if (hits[i].TryGetComponent(out Ally ally))
                {
                    ally.Health += HealPerSecond * Time.deltaTime;
                }
            }
        }
    }

    [Lab(63, "치유 오라", Symptom = "기능은 맞는데 매 프레임 OverlapSphere 가 배열을 만든다")]
    public class Lab63_HealingAura : Lab
    {
        float savedCaptureDeltaTime;

        public override IEnumerator Run()
        {
            savedCaptureDeltaTime = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            const long Budget = 128;

            var auras = new List<HealingAura>();
            for (int i = 0; i < 8; ++i)
            {
                var aura = Spawn<HealingAura>($"Priest_{i}", new Vector3((i % 4) * 0.3f, 0f, (i / 4) * 0.3f));
                aura.enabled = false;
                auras.Add(aura);
            }
            int inside = Case(24, 30);
            var allies = new List<Ally>();
            for (int i = 0; i < inside; ++i)
            {
                float angle = i * Mathf.PI * 2f / inside;
                var ally = SpawnPrimitive(PrimitiveType.Sphere, $"Ally_{i}", new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 4f).AddComponent<Ally>();
                allies.Add(ally);
            }
            var outsider = SpawnPrimitive(PrimitiveType.Sphere, "FarAlly", new Vector3(15f, 0f, 0f)).AddComponent<Ally>();
            yield return new WaitForFixedUpdate();

            var baseline = new GcMeter();
            yield return baseline.Measure(20);
            auras.ForEach(a => a.enabled = true);
            yield return null;
            var work = new GcMeter();
            yield return work.Measure(30);
            auras.ForEach(a => a.enabled = false);
            long bytes = System.Math.Max(0, work.BytesPerFrame - baseline.BytesPerFrame);

            int healed = allies.FindAll(a => a.Health > 11f).Count;
            Report.Note(Loc.F("범위 안 아군 {0}명, 프레임당 GC 할당 {1}바이트", inside, bytes));
            Report.Expect("범위 안에서 치유된 아군 수", inside, healed);
            Report.Expect("범위 밖 아군의 HP", 10f, outsider.Health);
            Report.ExpectTrue(Loc.F("찾는 데 GC 할당 ≤ {0}바이트/프레임", Budget), bytes <= Budget);
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
// Profiler → CPU Usage → Hierarchy → GC Alloc 열로 정렬. HealingAura.Update 아래 무엇이 할당하나요? 한 번에 몇 바이트이고, 그 크기는 범위 안 콜라이더 수에 따라 어떻게 바뀌나요?
// 메모리 할당을 직접 재려면 ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame") 의 LastValue 를 보세요(이 랩의 판정도 그 값입니다).
//
// プロファイラー (Profiler) → CPU Usage → ヒエラルキー (Hierarchy) → GC アロケーション (GC Alloc) 列で並べ替え。HealingAura.Update の下で何がアロケーションしていますか? 1 回に何バイトで、その大きさは範囲内のコライダーの数によってどう変わりますか?
// メモリアロケーションを直接測りたいなら、ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame") の LastValue を見てください (このラボの判定もその値です)。
#endregion

#region 힌트 2 / ヒント 2
// 결과를 배열로 "돌려주는" API 는 부를 때마다 새 배열을 만듭니다: Physics.OverlapSphere, RaycastAll, GetComponents<T>(), GetComponentsInChildren<T>(), FindObjectsByType, Mesh.vertices(!).
// 대부분 결과를 "넘긴 버퍼에 채우는" 짝이 있습니다: OverlapSphereNonAlloc, RaycastNonAlloc, GetComponents(List<T>), GetComponentsInChildren(List<T>), mesh.GetVertices(List<Vector3>).
// 채우는 API 의 버퍼가 작으면 넘치는 결과는 조용히 버려집니다(예외 없음). 버퍼 크기와 돌려받은 개수를 꼭 함께 보세요. 버퍼를 함수 밖으로 내보내면 랩 28 이 됩니다.
//
// 結果を配列で「返す」API は、呼ぶたびに新しい配列を作ります: Physics.OverlapSphere, RaycastAll, GetComponents<T>(), GetComponentsInChildren<T>(), FindObjectsByType, Mesh.vertices(!)。
// ほとんどに、結果を「渡したバッファーに詰める」対があります: OverlapSphereNonAlloc, RaycastNonAlloc, GetComponents(List<T>), GetComponentsInChildren(List<T>), mesh.GetVertices(List<Vector3>)。
// 詰める API のバッファーが小さいと、あふれた結果は黙って捨てられます (例外なし)。バッファーの大きさと返された個数を必ず一緒に見てください。バッファーを関数の外に出すとラボ 28 になります。
#endregion

#region 힌트 3 / ヒント 3
//   readonly Collider[] buffer = new Collider[32];
//   int count = Physics.OverlapSphereNonAlloc(transform.position, Radius, buffer);
//   if (count == buffer.Length) { Debug.LogWarning("치유 오라 버퍼가 가득 참"); }   // 넘쳤을 수 있다
//   for (int i = 0; i < count; ++i) { if (buffer[i].TryGetComponent(out Ally ally)) { ... } }
// 레이어 마스크를 함께 넘기면(랩 37) 아군 레이어만 받아 버퍼를 아낄 수 있습니다.
// 버퍼 크기를 지금 아군 수(24명)에 딱 맞추지 마세요. 채점기의 숨은 케이스는 아군 수가 다르고(30명), 넘친 아군은 경고 없이 치유에서 빠집니다.
//
//   readonly Collider[] buffer = new Collider[32];
//   int count = Physics.OverlapSphereNonAlloc(transform.position, Radius, buffer);
//   if (count == buffer.Length) { Debug.LogWarning("回復オーラのバッファーがいっぱい"); }   // あふれたかもしれない
//   for (int i = 0; i < count; ++i) { if (buffer[i].TryGetComponent(out Ally ally)) { ... } }
// レイヤーマスクも一緒に渡せば (ラボ 37)、味方のレイヤーだけを受け取ってバッファーを節約できます。
// バッファーの大きさを今の味方の数 (24 人) にぴったり合わせないでください。採点ツールの隠しケースでは味方の数が違い (30 人)、あふれた味方は警告なしに回復から外れます。
#endregion
