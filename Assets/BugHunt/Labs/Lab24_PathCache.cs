// 랩 24: 경로 캐시
// 순찰병이 목표가 바뀔 때마다 경로를 새로 계산해 NativeArray 에 담아 둔다. 오래 플레이하면 메모리가 조금씩 늘고,
// 플레이를 멈추거나 스크립트를 다시 컴파일하면 Console 에 "Leak Detected" 가 뜬다. 예외는 하나도 없다.
// 기대: 목표를 여러 번 바꾸고 순찰병을 없앤 뒤, 순찰병이 만든 네이티브 버퍼가 하나도 살아 있지 않다. 경로 점 수는 맞다.
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab24
{
    public class PathCache : MonoBehaviour
    {
        // 계산한 경로의 점들. 잡(Job)에 넘기려고 관리되는 배열 대신 NativeArray 를 쓴다.
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

    [Lab(24, "경로 캐시")]
    public class Lab24_PathCache : Lab
    {
        public override IEnumerator Run()
        {
            Report.Note($"누수 감지 수준 (Preferences → Jobs → Leak Detection Level): {NativeLeakDetection.Mode}");
            if (!LabUtil.NativeSafetyChecksEnabled)
            {
                Report.Skip("네이티브 버퍼 수명", "이 빌드는 컬렉션 안전 검사(ENABLE_UNITY_COLLECTIONS_CHECKS)가 꺼져 있습니다. 에디터에서 실행하세요");
                yield break;
            }

            var cache = Spawn<PathCache>("PatrolPathCache");
            int retargets = Case(5, 8);
            int segments = Case(8, 13);

            // 판정용: 순찰병이 만든 버퍼를 하나도 빠짐없이 기록해 둔다(구조체 복사본이지만 같은 메모리와 안전 핸들을 가리킨다).
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
            Report.Note($"목표를 {retargets}번 바꾼 뒤 살아 있는 버퍼 {aliveWhileUsing}개 (지금 쓰는 것 1개면 정상)");

            Destroy(cache.gameObject);
            yield return null;

            Report.Expect("마지막 경로의 점 수", segments + 1, pointCount);
            Report.Expect("마지막 경로의 끝점", new Vector3(retargets, 0, 10), last);
            Report.Expect("쓰는 중에 살아 있는 버퍼 (지금 경로 하나)", 1, aliveWhileUsing);
            Report.Expect("순찰병을 없앤 뒤 살아 있는 버퍼", 0, LabUtil.CountAliveNativeBuffers(made));
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// NativeArray 는 C# 의 배열이 아닙니다. 메모리는 Unity 의 네이티브 힙에 있고, C# 변수는 그 주소를 든 작은 struct 입니다.
// GC 는 이 메모리를 모릅니다. new 로 만든 만큼 누군가 Dispose 해야 합니다. 이 파일에서 Dispose 를 부르는 곳이 있나요?
// Edit → Preferences → Jobs → Leak Detection Level 을 "Enabled With Stack Trace" 로 바꾸고, 랩을 돌린 뒤 플레이를 멈춰 보세요.
// Console 의 누수 메시지에 "어디서 할당했나" 가 나옵니다.
#endregion

#region 힌트 2
// 새 경로를 담기 전에, 원래 들고 있던 버퍼는 누가 치우나요? Points = new ... 는 옛 주소를 덮어쓸 뿐입니다.
// 컴포넌트(또는 게임 오브젝트)가 Destroy 될 때 들고 있던 버퍼는? MonoBehaviour 의 어느 이벤트가 그 순간에 불리나요?
// Window → Analysis → Memory Profiler 스냅샷 두 장(랩 전/후)을 비교하면 Native → Unity Subsystems 쪽에 남은 메모리가 보입니다.
#endregion

#region 힌트 3
// Rebuild 첫 줄에서 옛 버퍼를 치우고, OnDestroy 에서 마지막 버퍼를 치우세요.
//   if (Points.IsCreated) Points.Dispose();
// IsCreated 는 "이 변수가 버퍼를 들고 있나" 입니다. Dispose 한 그 변수는 IsCreated 가 false 가 됩니다(복사본은 아닙니다).
#endregion
