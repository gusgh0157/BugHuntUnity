using System.Collections;
using Unity.Profiling;
using UnityEngine;

namespace BugHunt
{
    // 프레임마다 GC 힙에 할당된 바이트를 잰다. Profiler → CPU → Hierarchy 의 "GC Alloc" 열과 같은 카운터(GC Allocated In Frame)다.
    // 이 카운터는 지난 프레임의 값을 준다. 재는 동안에는 Report 로 출력하지 않는다(출력도 할당이라 값에 섞인다).
    // 여기에는 버그가 없다.
    public sealed class GcMeter
    {
        public long BytesPerFrame { get; private set; }
        public long PeakBytes { get; private set; }

        // frames 프레임 동안의 프레임당 평균. 시작 전 두 프레임은 버린다(카운터가 첫 값을 채우는 동안).
        public IEnumerator Measure(int frames)
        {
            using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            yield return null;
            yield return null;
            long total = 0;
            long peak = 0;
            for (int i = 0; i < frames; ++i)
            {
                yield return null;
                long bytes = recorder.LastValue;
                total += bytes;
                if (bytes > peak)
                {
                    peak = bytes;
                }
            }
            BytesPerFrame = total / frames;
            PeakBytes = peak;
        }

        // 아무 일도 하지 않는 프레임의 값(에디터 자체가 할당하는 몫)을 빼고 본다.
        public static IEnumerator MeasureAboveBaseline(int frames, System.Action begin, System.Action<long, long> done)
        {
            var baseline = new GcMeter();
            yield return baseline.Measure(frames);
            begin();
            var work = new GcMeter();
            yield return work.Measure(frames);
            done(System.Math.Max(0, work.BytesPerFrame - baseline.BytesPerFrame), baseline.BytesPerFrame);
        }
    }
}
