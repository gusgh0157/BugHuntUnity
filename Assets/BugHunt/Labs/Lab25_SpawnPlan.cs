// 랩 25: 출현 계획표
// 웨이브가 시작될 때 "몇 프레임째에 몇 마리" 계획표를 한 번 만들어 두고, 매 프레임 한 줄씩 읽어 적을 내보낸다.
// 첫 줄은 잘 나오는데 둘째 프레임부터 예외가 나거나, 엉뚱한 마릿수가 나온다.
// 기대: 계획표대로 전부 나온다(합계가 맞다). 예외 없음.
// ラボ 25: 出現計画表
// ウェーブが始まるときに「何フレーム目に何体」という計画表を一度作っておき、毎フレーム 1 行ずつ読んで敵を出す。
// 1 行目はちゃんと出るのに、2 フレーム目から例外が出たり、おかしな数が出たりする。
// 期待: 計画表どおりにすべて出る (合計が正しい)。例外なし。
using System.Collections;
using Unity.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab25
{
    public class SpawnDirector : MonoBehaviour
    {
        public int Spawned;
        public bool Finished;

        NativeArray<int> plan;   // plan[i] = i 번째 프레임에 내보낼 적 수 / plan[i] = i 番目のフレームで出す敵の数
        int next;

        public void StartWave(int[] perFrame)
        {
            // 계획표는 이번 웨이브에만 쓰고 버리니 가장 빠른 할당자로.
            // 計画表は今回のウェーブだけで使って捨てるので、いちばん速いアロケーターで。
            plan = new NativeArray<int>(perFrame.Length, Allocator.Temp);
            plan.CopyFrom(perFrame);
            next = 0;
            Finished = false;
        }

        void Update()
        {
            if (Finished || !plan.IsCreated)
            {
                return;
            }
            Spawned += plan[next];
            ++next;
            if (next == plan.Length)
            {
                Finished = true;
            }
        }
    }

    [Lab(25, "출현 계획표", Symptom = "첫 줄만 나오고 다음 프레임부터 \"has been deallocated\"")]
    public class Lab25_SpawnPlan : Lab
    {
        public override IEnumerator Run()
        {
            if (!LabUtil.NativeSafetyChecksEnabled)
            {
                Report.Skip("Temp 할당 수명", "이 빌드는 컬렉션 안전 검사가 꺼져 있습니다. 에디터에서 실행하세요");
                yield break;
            }

            var director = Spawn<SpawnDirector>("SpawnDirector");
            int[] plan = Case(new[] { 3, 1, 4, 1, 5, 9, 2, 6 }, new[] { 2, 7, 1, 8, 2, 8 });
            int expected = 0;
            foreach (int n in plan)
            {
                expected += n;
            }
            yield return null;

            director.StartWave(plan);
            for (int i = 0; i < plan.Length + 3 && !director.Finished; ++i)
            {
                yield return null;
            }

            Report.ExpectTrue("계획표를 끝까지 읽음", director.Finished);
            Report.Expect("나온 적 합계", expected, director.Spawned);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// 예외 메시지를 읽으세요. "deallocated" 라는 말이 있나요? 그런데 이 파일에는 Dispose 가 없습니다. 누가 해제했을까요?
// Update 첫 줄에 중단점을 걸고 매 프레임 Time.frameCount 와 plan[next] 를 조사식으로 보세요. StartWave 를 부른 프레임과 비교하세요.
//
// 例外メッセージを読んでください。"deallocated" という言葉はありますか? でもこのファイルには Dispose がありません。誰が解放したのでしょう?
// Update の 1 行目にブレークポイントを置き、毎フレーム Time.frameCount と plan[next] をウォッチ式で見てください。StartWave を呼んだフレームと比べてください。
#endregion

#region 힌트 2 / ヒント 2
// Allocator 는 "이 메모리를 얼마나 오래 쓸 건가" 의 약속입니다.
//   Temp:       이 프레임 안에서만. 프레임이 끝나면 Unity 가 한꺼번에 되감는다(Dispose 를 안 해도 되지만, 다음 프레임에는 없다). 잡에 넘길 수 없다.
//   TempJob:    잡에 넘겨서 4프레임 안에 Dispose. 넘기면 경고.
//   Persistent: 직접 Dispose 할 때까지. 가장 느리지만 수명 제한이 없다.
// 필드에 넣어 두고 다음 프레임에 읽는 버퍼에 Temp 를 쓰면 어떻게 될까요?
//
// Allocator は「このメモリをどれくらい長く使うか」の約束です。
//   Temp:       このフレームの中だけ。フレームが終わると Unity がまとめて巻き戻す (Dispose しなくてもよいが、次のフレームにはもうない)。ジョブに渡せない。
//   TempJob:    ジョブに渡して 4 フレーム以内に Dispose。超えると警告。
//   Persistent: 自分で Dispose するまで。いちばん遅いが寿命の制限がない。
// フィールドに入れておいて次のフレームで読むバッファに Temp を使うと、どうなるでしょう?
#endregion

#region 힌트 3 / ヒント 3
// 여러 프레임에 걸쳐 쓰는 버퍼는 Persistent 로 만들고 직접 치웁니다(랩 23 과 같은 규칙).
//   StartWave: if (plan.IsCreated) plan.Dispose();  plan = new NativeArray<int>(perFrame.Length, Allocator.Persistent);
//   OnDestroy: if (plan.IsCreated) plan.Dispose();
// (다 읽은 순간 Update 에서 Dispose 해도 됩니다.) 관리되는 int[] 로 바꾸는 것도 맞는 답입니다. 잡에 안 넘긴다면 NativeArray 일 이유가 없습니다.
//
// 複数フレームにわたって使うバッファは Persistent で作り、自分で片付けます (ラボ 23 と同じルール)。
//   StartWave: if (plan.IsCreated) plan.Dispose();  plan = new NativeArray<int>(perFrame.Length, Allocator.Persistent);
//   OnDestroy: if (plan.IsCreated) plan.Dispose();
// (読み終えた瞬間に Update で Dispose してもかまいません。) マネージドの int[] に変えるのも正解です。ジョブに渡さないなら NativeArray にする理由はありません。
#endregion
