// 랩 22: 발자국
// 플레이어가 지나간 자리를 NativeList 에 쌓아 두고, 잡(Job)에 넘기기 쉽게 AsArray() 로 만든 NativeArray 를 캐시해 둔다.
// 출발점(첫 발자국)으로 돌아가는 기능이 있다. 처음 몇 걸음 동안은 괜찮은데, 걸음이 쌓이면 출발점을 읽는 줄에서 ObjectDisposedException 이 난다.
// 기대: 몇 걸음을 걸어도 출발점을 읽을 수 있고, 다 쓰면 메모리를 돌려준다.
// ラボ 22: 足跡
// プレイヤーが通った場所を NativeList にためておき、ジョブ (Job) に渡しやすいように AsArray() で作った NativeArray をキャッシュしておく。
// 出発点 (最初の足跡) に戻る機能がある。最初の数歩は大丈夫だが、歩数がたまると出発点を読む行で ObjectDisposedException が出る。
// 期待: 何歩歩いても出発点を読むことができ、使い終わったらメモリを返す。
using System.Collections;
using Unity.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab22
{
    public class Trail : MonoBehaviour
    {
        public NativeList<Vector3> Points;
        NativeArray<Vector3> view;   // 잡에 넘기려고 캐시한 배열 모습 / ジョブに渡すためにキャッシュした配列のビュー

        void Awake()
        {
            Points = new NativeList<Vector3>(4, Allocator.Persistent);
            Points.Add(transform.position);   // 출발점 / 出発点 (スタート地点)
            view = Points.AsArray();
        }

        public void Step(Vector3 position)
        {
            Points.Add(position);
        }

        public Vector3 StartPoint()
        {
            return view[0];
        }

        void OnDestroy()
        {
            if (Points.IsCreated)
            {
                Points.Dispose();
            }
        }
    }

    [Lab(22, "발자국", Symptom = "걸음이 쌓이면 출발점을 읽는 줄에서 \"has been deallocated\"")]
    public class Lab22_TrailPoints : Lab
    {
        public override IEnumerator Run()
        {
            Vector3 start = Case(new Vector3(3f, 0f, -2f), new Vector3(-7f, 1f, 4f));
            var trail = Spawn<Trail>("Trail", start);
            int steps = Case(10, 23);
            yield return null;

            for (int i = 1; i <= steps; ++i)
            {
                trail.Step(start + new Vector3(i, 0f, i * 0.5f));
            }
            Report.Expect("발자국 수(출발점 포함)", steps + 1, trail.Points.Length);
            Report.Expect("출발점", start, trail.StartPoint());

            NativeList<Vector3> points = trail.Points;
            Destroy(trail.gameObject);
            yield return null;
            Report.Expect("Trail 을 없앤 뒤 NativeList 를 돌려줌", false, points.IsCreated && LabUtil.NativeSafetyChecksEnabled && IsAlive(points));
        }

        static bool IsAlive(NativeList<Vector3> list)
        {
            try
            {
                return list.Length >= 0;
            }
            catch (System.ObjectDisposedException)
            {
                return false;
            }
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// Step 에 중단점을 걸고 매번 조사식에 Points.Capacity 와 Points.Length 를 보세요. 예외는 Length 가 Capacity 를 넘는 첫 Add 다음에 납니다.
// 예외 메시지는 "The Unity.Collections.NativeList ... has been deallocated" 입니다. NativeList 는 멀쩡한데 무엇이 해제되었다는 걸까요?
//
// Step にブレークポイントを置き、毎回ウォッチ式で Points.Capacity と Points.Length を見てください。例外は Length が Capacity を超える最初の Add の後に出ます。
// 例外メッセージは "The Unity.Collections.NativeList ... has been deallocated" です。NativeList は無事なのに、何が解放されたというのでしょう?
#endregion

#region 힌트 2 / ヒント 2
// NativeList 는 Capacity 가 모자라면 더 큰 메모리를 새로 잡아 복사하고 옛 메모리를 해제합니다(List<T> 와 같음).
// AsArray() 는 복사가 아니라 "지금 이 순간의 메모리" 를 가리키는 모습(view)입니다. 크기가 바뀌면 그 view 는 해제된 옛 메모리를 가리킵니다.
// 안전 검사(Safety System)가 켜져 있으니 예외로 알려 주지만, 빌드(안전 검사 꺼짐)에서는 쓰레기 값을 읽거나 크래시합니다. 4부 랩 23~24와 같은 종류입니다.
// view 의 길이도 만든 순간의 길이로 굳어 있습니다. 메모리가 그대로여도 그 뒤에 늘어난 원소는 view 로 볼 수 없습니다.
//
// NativeList は Capacity が足りなくなると、より大きなメモリを新しく確保してコピーし、古いメモリを解放します (List<T> と同じ)。
// AsArray() はコピーではなく、「今この瞬間のメモリ」を指すビュー (view) です。サイズが変わると、その view は解放された古いメモリを指します。
// 安全チェック (Safety System) がオンなので例外で知らせてくれますが、ビルド (安全チェックはオフ) ではゴミの値を読むかクラッシュします。第4部のラボ 23〜24 と同じ種類です。
// view の長さも作った瞬間の長さで固定されています。メモリがそのままでも、その後に増えた要素は view からは見えません。
#endregion

#region 힌트 3 / ヒント 3
// view 를 필드에 캐시하지 말고 쓸 때마다 만듭니다. 잡에 넘길 때도 스케줄 직전에 Points.AsArray() (또는 AsDeferredJobArray()).
//   public Vector3 StartPoint() { return Points[0]; }
// 크기가 정해져 있다면 처음부터 Capacity 를 넉넉히 잡을 수도 있지만, 그것은 버그를 숨길 뿐이니 view 를 오래 들고 있지 않는 쪽이 정답입니다.
//
// view をフィールドにキャッシュせず、使うたびに作ります。ジョブに渡すときも、スケジュールの直前に Points.AsArray() (または AsDeferredJobArray())。
// サイズが決まっているなら最初から Capacity を十分に確保することもできますが、それはバグを隠すだけなので、view を長く持ち続けない方が正解です。
#endregion
