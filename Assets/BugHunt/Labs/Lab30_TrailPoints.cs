// 랩 30: 발자국
// 플레이어가 지나간 자리를 NativeList 에 쌓아 두고, 잡(Job)에 넘기기 쉽게 AsArray() 로 만든 NativeArray 를 캐시해 둔다.
// 출발점(첫 발자국)으로 돌아가는 기능이 있다. 걷기 전에는 괜찮은데, 한 걸음이라도 걸은 뒤 출발점을 읽는 줄에서 ObjectDisposedException 이 난다.
// 기대: 몇 걸음을 걸어도 출발점을 읽을 수 있고, 다 쓰면 메모리를 돌려준다.
// ラボ 30: 足跡
// プレイヤーが通った場所を NativeList にためておき、ジョブ (Job) に渡しやすいように AsArray() で作った NativeArray をキャッシュしておく。
// 出発点 (最初の足跡) に戻る機能がある。歩く前は大丈夫だが、1 歩でも歩いた後に出発点を読む行で ObjectDisposedException が出る。
// 期待: 何歩歩いても出発点を読むことができ、使い終わったらメモリを返す。
using System.Collections;
using Unity.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab30
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

    [Lab(30, "발자국", Symptom = "한 걸음만 걸어도 출발점을 읽는 줄에서 \"has been deallocated\"")]
    public class Lab30_TrailPoints : Lab
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
// Step 에 중단점을 걸고 조사식에 view[0] 을 넣어 두세요. 처음 멈췄을 때는 읽히는데, Step 을 한 번 지나간 뒤에는 어떻게 되나요?
// 예외 메시지는 "The Unity.Collections.NativeList ... has been deallocated" 입니다. NativeList 는 멀쩡한데 무엇이 해제되었다는 걸까요?
//
// Step にブレークポイントを置き、ウォッチ式に view[0] を入れておいてください。最初に止まったときは読めますが、Step を 1 回通った後はどうなりますか?
// 例外メッセージは "The Unity.Collections.NativeList ... has been deallocated" です。NativeList は無事なのに、何が解放されたというのでしょう?
#endregion

#region 힌트 2 / ヒント 2
// AsArray() 는 복사가 아니라 "지금 이 순간의 메모리" 를 가리키는 모습(view)입니다.
// NativeList 는 Capacity 가 모자라면 더 큰 메모리를 새로 잡아 복사하고 옛 메모리를 해제합니다(List<T> 와 같음). 그때 view 는 해제된 옛 메모리를 가리킵니다.
// 그래서 안전 검사(Safety System)는 메모리가 실제로 옮겨지기를 기다리지 않습니다. Add 처럼 길이를 바꾸는 쓰기는 매번 리스트의 "보조 버전"을 올리고,
// 그 전에 만든 view 는 그 순간 무효가 됩니다(Capacity 안에서 쓰는 AddNoResize 만 예외). 그래서 첫 Add 한 번에 예외가 납니다.
// 빌드(안전 검사 꺼짐)에서는 예외 없이, 메모리가 옮겨진 뒤 쓰레기 값을 읽거나 크래시합니다. 5부 랩 31~32와 같은 종류입니다.
// view 의 길이도 만든 순간의 길이로 굳어 있습니다. 메모리가 그대로여도 그 뒤에 늘어난 원소는 view 로 볼 수 없습니다.
//
// AsArray() はコピーではなく、「今この瞬間のメモリ」を指すビュー (view) です。
// NativeList は Capacity が足りなくなると、より大きなメモリを新しく確保してコピーし、古いメモリを解放します (List<T> と同じ)。そのとき view は解放された古いメモリを指します。
// そのため安全チェック (Safety System) はメモリが実際に移されるのを待ちません。Add のように長さを変える書き込みは毎回リストの「セカンダリバージョン」を上げ、
// それより前に作った view はその瞬間に無効になります (Capacity の範囲内で書く AddNoResize だけは例外)。だから最初の Add 1 回で例外が出ます。
// ビルド (安全チェックはオフ) では例外は出ず、メモリが移された後にゴミの値を読むかクラッシュします。第5部のラボ 31〜32 と同じ種類です。
// view の長さも作った瞬間の長さで固定されています。メモリがそのままでも、その後に増えた要素は view からは見えません。
#endregion

#region 힌트 3 / ヒント 3
// view 를 필드에 캐시하지 말고 쓸 때마다 만듭니다. 잡에 넘길 때도 스케줄 직전에 Points.AsArray() (또는 AsDeferredJobArray()).
//   public Vector3 StartPoint() { return Points[0]; }
// Capacity 를 넉넉히 잡아도 소용없습니다. Add 마다 view 가 무효가 되니, view 는 길이를 바꾸는 쓰기를 건너서 들고 있지 않는 것이 규칙입니다.
//
// view をフィールドにキャッシュせず、使うたびに作ります。ジョブに渡すときも、スケジュールの直前に Points.AsArray() (または AsDeferredJobArray())。
//   public Vector3 StartPoint() { return Points[0]; }
// Capacity を十分に確保しても無駄です。Add のたびに view が無効になるので、長さを変える書き込みをまたいで view を持ち続けないのがルールです。
#endregion
