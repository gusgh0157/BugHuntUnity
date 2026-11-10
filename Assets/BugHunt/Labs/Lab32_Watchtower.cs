// 랩 32: 망루의 시야
// 망루가 적 수백 마리까지의 거리를 잡(Job)으로 나눠 계산하고, 사거리 안의 적을 센다. 잡을 붙이자마자 예외가 난다.
// "거리 계산 잡은 분명히 Complete 했는데?"
// 기대: 사거리 안의 적 수가 맞고, 예외가 없다.
// ラボ 32: 見張り塔の視界
// 見張り塔が数百体の敵までの距離をジョブ (Job) で分けて計算し、射程内の敵を数える。ジョブを付けたとたんに例外が出る。
// 「距離計算のジョブは確かに Complete したのに?」
// 期待: 射程内の敵の数が正しく、例外が出ない。
using System.Collections;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace BugHunt.Labs.Lab32
{
    // 적 하나당 하나씩, 여러 워커 스레드에서 나눠 돈다.
    // 敵 1 体につき 1 つずつ、複数のワーカースレッドで分けて実行される。
    public struct MeasureDistanceJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Vector3> Enemies;
        public Vector3 Eye;
        [WriteOnly] public NativeArray<float> Distances;

        public void Execute(int index)
        {
            Distances[index] = Vector3.Distance(Enemies[index], Eye);
        }
    }

    // 거리 계산이 끝난 뒤에 돌아야 한다(Schedule 의 dependsOn).
    // 距離計算が終わった後に実行されなければならない (Schedule の dependsOn)。
    public struct CountInRangeJob : IJob
    {
        [ReadOnly] public NativeArray<float> Distances;
        public float Range;
        public NativeArray<int> Count;

        public void Execute()
        {
            int count = 0;
            for (int i = 0; i < Distances.Length; ++i)
            {
                if (Distances[i] <= Range)
                {
                    ++count;
                }
            }
            Count[0] = count;
        }
    }

    public class Watchtower : MonoBehaviour
    {
        public float Range = 20f;
        public int EnemiesInRange = -1;

        NativeArray<Vector3> enemies;
        NativeArray<float> distances;
        NativeArray<int> count;
        JobHandle running;

        public void Scan(Vector3[] positions)
        {
            enemies = new NativeArray<Vector3>(positions, Allocator.Persistent);
            distances = new NativeArray<float>(positions.Length, Allocator.Persistent);
            count = new NativeArray<int>(1, Allocator.Persistent);

            var measure = new MeasureDistanceJob { Enemies = enemies, Eye = transform.position, Distances = distances };
            JobHandle measured = measure.Schedule(positions.Length, 64);
            var tally = new CountInRangeJob { Distances = distances, Range = Range, Count = count };
            running = tally.Schedule(measured);

            measured.Complete();   // 거리 계산이 끝날 때까지 기다린다 / 距離計算が終わるまで待つ
            EnemiesInRange = count[0];
        }

        void OnDestroy()
        {
            running.Complete();   // 아직 도는 잡이 쓰는 버퍼는 Dispose 할 수 없다 / まだ実行中のジョブが使うバッファは Dispose できない
            if (enemies.IsCreated)
            {
                enemies.Dispose();
                distances.Dispose();
                count.Dispose();
            }
        }
    }

    [Lab(32, "망루의 시야", "見張り塔の視界", Symptom = "잡을 붙이자마자 InvalidOperationException (\"분명히 Complete 했는데?\")", SymptomJa = "ジョブを付けたとたんに InvalidOperationException (「確かに Complete したのに?」)")]
    public class Lab32_Watchtower : Lab
    {
        public override IEnumerator Run()
        {
            var tower = Spawn<Watchtower>("Watchtower", Vector3.zero);
            tower.Range = Case(20.05f, 33.05f);

            // 망루를 중심으로 반지름 1.0, 1.1, 1.2 ... 인 원 위에 적을 하나씩 세운다(사거리 경계에 딱 걸리는 적은 없다).
            // 見張り塔を中心に半径 1.0、1.1、1.2 ... の円の上に敵を 1 体ずつ置く (射程の境界ちょうどにいる敵はいない)。
            int enemyCount = Case(500, 777);
            var positions = new Vector3[enemyCount];
            int expected = 0;
            for (int i = 0; i < enemyCount; ++i)
            {
                float radius = 1f + i * 0.1f;
                float angle = i * 2.4f;
                positions[i] = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                if (radius <= tower.Range)
                {
                    ++expected;
                }
            }
            yield return null;

            tower.Scan(positions);
            yield return null;

            Report.Expect(Loc.F("사거리 {0} 안의 적", "射程 {0} 内の敵", tower.Range), expected, tower.EnemiesInRange);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// 예외 메시지를 끝까지, 천천히 읽으세요. Unity 의 잡 안전 시스템(Safety System)은 "어느 잡이 어느 버퍼에 쓰는 중인지" 를 알려 줍니다.
// 메시지에 나오는 잡 이름이 MeasureDistanceJob 인가요, CountInRangeJob 인가요? 그 잡을 Complete 한 줄이 있나요?
//
// 例外メッセージを最後まで、ゆっくり読んでください。Unity のジョブ安全システム (Safety System) は「どのジョブがどのバッファに書き込み中か」を教えてくれます。
// メッセージに出てくるジョブ名は MeasureDistanceJob ですか、CountInRangeJob ですか? そのジョブを Complete した行はありますか?
#endregion

#region 힌트 2 / ヒント 2
// Schedule 은 잡을 "예약" 만 하고 바로 돌아옵니다. 잡이 실제로 끝났는지와 상관없이, 메인 스레드가 그 잡의 출력 버퍼를 읽으려면
// 그 잡의 JobHandle 을 Complete 해야 합니다. 앞 잡(measured)을 기다린다고 뒤 잡(tally)이 끝나지는 않습니다. 의존 관계는 한 방향입니다.
// Window → Analysis → Profiler → CPU → Timeline 에서 Worker 스레드 줄을 보면 두 잡이 언제 도는지 보입니다.
//
// Schedule はジョブを「予約」するだけで、すぐに戻ってきます。ジョブが実際に終わったかどうかに関係なく、メインスレッドがそのジョブの出力バッファを読むには
// そのジョブの JobHandle を Complete しなければなりません。前のジョブ (measured) を待っても、後ろのジョブ (tally) が終わるわけではありません。依存関係は一方向です。
// ウィンドウ → 分析 → プロファイラー → CPU → タイムライン (Timeline) で Worker スレッドの行を見ると、2 つのジョブがいつ実行されるかが見えます。
#endregion

#region 힌트 3 / ヒント 3
// 마지막 잡의 핸들(사슬의 끝)을 Complete 하세요:  running.Complete();  EnemiesInRange = count[0];
// 앞 잡까지 사슬로 이어져 있으므로 끝을 기다리면 앞도 끝나 있습니다. 결과를 다음 프레임에 읽고 싶다면 Update 에서 Schedule 하고
// LateUpdate(또는 다음 프레임)에서 Complete 하는 식으로 메인 스레드와 겹치게 돌릴 수 있습니다. 그래도 읽기 전에 Complete 는 반드시.
//
// 最後のジョブのハンドル (チェーンの末尾) を Complete してください:  running.Complete();  EnemiesInRange = count[0];
// 前のジョブまでチェーンでつながっているので、末尾を待てば前も終わっています。結果を次のフレームで読みたいなら、Update で Schedule して
// LateUpdate (または次のフレーム) で Complete するようにして、メインスレッドと重ねて実行できます。それでも読む前の Complete は必ず。
#endregion
