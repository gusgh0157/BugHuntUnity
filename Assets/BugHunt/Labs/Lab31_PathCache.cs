// 랩 31: 경로 캐시
// 순찰병이 목표가 바뀔 때마다 경로를 새로 계산해 NativeArray 에 담아 둔다. 오래 플레이하면 메모리가 조금씩 늘고,
// 플레이를 멈추거나 스크립트를 다시 컴파일하면 Console 에 "Leak Detected" 가 뜬다. 예외는 하나도 없다.
// 기대: 목표를 여러 번 바꾸고 순찰병을 없앤 뒤, 순찰병이 만든 네이티브 버퍼가 하나도 살아 있지 않다. 경로 점 수는 맞다.
// ラボ 31: 経路キャッシュ
// 巡回兵は目標が変わるたびに経路を計算し直して NativeArray に入れておく。長くプレイするとメモリが少しずつ増え、
// 再生を止めたりスクリプトを再コンパイルしたりするとコンソールに "Leak Detected" が出る。例外は 1 つもない。
// 期待: 目標を何度も変えて巡回兵を消した後、巡回兵が作ったネイティブバッファが 1 つも生きていない。経路の点の数は正しい。
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab31
{
    public class PathCache : MonoBehaviour
    {
        // 계산한 경로의 점들. 잡(Job)에 넘기려고 관리되는 배열 대신 NativeArray 를 쓴다.
        // 計算した経路の点。ジョブ (Job) に渡すため、マネージド配列の代わりに NativeArray を使う。
        public NativeArray<Vector3> Points;

        public void Rebuild(Vector3 from, Vector3 to, int segments)
        {
            Points = new NativeArray<Vector3>(segments + 1, Allocator.Persistent);
            for (int i = 0; i <= segments; ++i)
            {
                Points[i] = Vector3.Lerp(from, to, i / (float)segments);
            }
        }
    }

    [Lab(31, "경로 캐시", "経路キャッシュ", Symptom = "예외는 없는데 오래 하면 메모리가 늘고, 멈추면 Leak Detected", SymptomJa = "例外はないのに長く遊ぶとメモリが増え、止めると Leak Detected")]
    public class Lab31_PathCache : Lab
    {
        public override IEnumerator Run()
        {
            Report.Note(Loc.F("누수 감지 수준 (Preferences → Jobs → Leak Detection Level): {0}", "リーク検出レベル (環境設定 → Jobs → Leak Detection Level): {0}", NativeLeakDetection.Mode));
            if (!LabUtil.NativeSafetyChecksEnabled)
            {
                Report.Skip(Loc.L("네이티브 버퍼 수명", "ネイティブバッファの寿命"), Loc.L("이 빌드는 컬렉션 안전 검사(ENABLE_UNITY_COLLECTIONS_CHECKS)가 꺼져 있습니다. 에디터에서 실행하세요", "このビルドはコレクションの安全チェック (ENABLE_UNITY_COLLECTIONS_CHECKS) がオフです。エディターで実行してください"));
                yield break;
            }

            var cache = Spawn<PathCache>("PatrolPathCache");
            int retargets = Case(5, 8);
            int segments = Case(8, 13);

            // 판정용: 순찰병이 만든 버퍼를 하나도 빠짐없이 기록해 둔다(구조체 복사본이지만 같은 메모리와 안전 핸들을 가리킨다).
            // 判定用: 巡回兵が作ったバッファを 1 つ残らず記録しておく (構造体のコピーだが、同じメモリと安全ハンドルを指す)。
            var made = new List<NativeArray<Vector3>>();
            for (int i = 0; i < retargets; ++i)
            {
                cache.Rebuild(Vector3.zero, new Vector3(i + 1, 0, 10), segments);
                made.Add(cache.Points);
                yield return null;
            }

            int pointCount = cache.Points.Length;
            Vector3 last = cache.Points[pointCount - 1];
            int aliveWhileUsing = LabUtil.CountAliveNativeBuffers(made);
            Report.Note(Loc.F("목표를 {0}번 바꾼 뒤 살아 있는 버퍼 {1}개 (지금 쓰는 것 1개면 정상)", "目標を {0} 回変えた後に生きているバッファ {1} 個 (今使っている 1 個なら正常)", retargets, aliveWhileUsing));

            Destroy(cache.gameObject);
            yield return null;

            Report.Expect(Loc.L("마지막 경로의 점 수", "最後の経路の点の数"), segments + 1, pointCount);
            Report.Expect(Loc.L("마지막 경로의 끝점", "最後の経路の終点"), new Vector3(retargets, 0, 10), last);
            Report.Expect(Loc.L("쓰는 중에 살아 있는 버퍼 (지금 경로 하나)", "使用中に生きているバッファ (今の経路 1 つ)"), 1, aliveWhileUsing);
            Report.Expect(Loc.L("순찰병을 없앤 뒤 살아 있는 버퍼", "巡回兵を消した後に生きているバッファ"), 0, LabUtil.CountAliveNativeBuffers(made));
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// NativeArray 는 C# 의 배열이 아닙니다. 메모리는 Unity 의 네이티브 힙에 있고, C# 변수는 그 주소를 든 작은 struct 입니다.
// GC 는 이 메모리를 모릅니다. new 로 만든 만큼 누군가 Dispose 해야 합니다. 이 파일에서 Dispose 를 부르는 곳이 있나요?
// Edit → Preferences → Jobs → Leak Detection Level 을 "Enabled With Stack Trace" 로 바꾸고, 랩을 돌린 뒤 플레이를 멈춰 보세요.
// Console 의 누수 메시지에 "어디서 할당했나" 가 나옵니다.
//
// NativeArray は C# の配列ではありません。メモリは Unity のネイティブヒープにあり、C# の変数はそのアドレスを持つ小さな struct です。
// GC はこのメモリを知りません。new で作った分だけ、誰かが Dispose しなければなりません。このファイルに Dispose を呼ぶ場所はありますか?
// 編集 → 環境設定 (Preferences) → Jobs → Leak Detection Level を "Enabled With Stack Trace" に変え、ラボを実行してから再生を止めてみてください。
// コンソールのリークメッセージに「どこで割り当てたか」が出ます。
#endregion

#region 힌트 2 / ヒント 2
// 새 경로를 담기 전에, 원래 들고 있던 버퍼는 누가 치우나요? Points = new ... 는 옛 주소를 덮어쓸 뿐입니다.
// 컴포넌트(또는 게임 오브젝트)가 Destroy 될 때 들고 있던 버퍼는? MonoBehaviour 의 어느 이벤트가 그 순간에 불리나요?
// Window → Analysis → Memory Profiler 스냅샷 두 장(랩 전/후)을 비교하면 Native → Unity Subsystems 쪽에 남은 메모리가 보입니다.
//
// 新しい経路を入れる前に、もともと持っていたバッファは誰が片付けますか? Points = new ... は古いアドレスを上書きするだけです。
// コンポーネント (またはゲームオブジェクト) が Destroy されるとき、持っていたバッファは? MonoBehaviour のどのイベントがその瞬間に呼ばれますか?
// ウィンドウ → 分析 → Memory Profiler のスナップショット 2 枚 (ラボの前/後) を比べると、Native → Unity Subsystems 側に残ったメモリが見えます。
#endregion

#region 힌트 3 / ヒント 3
// Rebuild 첫 줄에서 옛 버퍼를 치우고, OnDestroy 에서 마지막 버퍼를 치우세요.
//   if (Points.IsCreated) Points.Dispose();
// IsCreated 는 "이 변수가 버퍼를 들고 있나" 입니다. Dispose 한 그 변수는 IsCreated 가 false 가 됩니다(복사본은 아닙니다).
//
// Rebuild の 1 行目で古いバッファを片付け、OnDestroy で最後のバッファを片付けてください。
//   if (Points.IsCreated) Points.Dispose();
// IsCreated は「この変数がバッファを持っているか」です。Dispose したその変数は IsCreated が false になります (コピーのほうは false になりません)。
#endregion
