// 랩 47: 상점의 금화
// 상점 창을 열 때 세이브 파일에서 금화를 읽어 온다. 읽기는 async 로 만들어 두었고, 창을 여는 쪽은 결과를 .Result 로 기다린다.
// 콘솔 앱에서 시험할 때는 잘 됐는데, 게임에서 상점을 여는 순간 영원히 멈춘다. CPU 는 0% 다(무한루프가 아니다).
// 기대: 상점 창이 열리고 1초 안에 금화가 보인다. 창을 여는 동안 게임(메인 스레드)이 멈추지 않는다.
// (HangGuard.Wait 은 task.Result 와 같습니다. 단, 진짜로 굳는 대신 "응답 없음" 을 알리고 빠져나옵니다. Core/HangGuard.cs 참고)
using System.Collections;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

namespace BugHunt.Labs.Lab47
{
    public class SaveFile
    {
        public int Gold = 1250;
        public int ReadDelayMs = 200;   // 느린 디스크

        public async Task<int> LoadGoldAsync()
        {
            await Task.Delay(ReadDelayMs);   // 파일을 읽는 척
            return Gold;
        }
    }

    public class ShopWindow : MonoBehaviour
    {
        public int ShownGold = -1;

        public void Open(SaveFile save)
        {
            ShownGold = HangGuard.Wait(save.LoadGoldAsync(), "ShopWindow.Open: 세이브에서 금화 읽기를 기다림");
        }
    }

    [Lab(47, "상점의 금화")]
    public class Lab47_ShopGold : Lab
    {
        public override IEnumerator Run()
        {
            Report.Note($"메인 스레드의 SynchronizationContext: {System.Threading.SynchronizationContext.Current?.GetType().Name ?? "없음"}");
            var save = new SaveFile { Gold = Case(1250, 4321) };
            var shop = Spawn<ShopWindow>("ShopWindow");
            yield return null;

            var blocked = Stopwatch.StartNew();
            shop.Open(save);
            blocked.Stop();

            float until = Time.realtimeSinceStartup + 1f;
            while (shop.ShownGold < 0 && Time.realtimeSinceStartup < until)
            {
                yield return null;
            }

            Report.Expect("상점에 보이는 금화", save.Gold, shop.ShownGold);
            Report.ExpectTrue($"Open 이 메인 스레드를 막은 시간 {blocked.ElapsedMilliseconds}ms (한 프레임 16ms 이하)", blocked.ElapsedMilliseconds <= 16);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// 무한루프와 데드락은 둘 다 "멈춤" 이지만 지문이 다릅니다. 무한루프는 CPU 한 코어가 100%, 데드락은 0% 근처입니다(서로 기다리기만 한다).
// 디버거를 붙이고 "모두 중단" → 스레드 창(VS: 디버그 → 창 → 스레드, Ctrl+Alt+H)에서 Main Thread 의 호출 스택을 보세요.
// 메인 스레드는 Task.Wait / Monitor.Wait 안에서 자고 있습니다. 그럼 LoadGoldAsync 의 await 다음 줄(return Gold)은 누가 실행해 줄까요?
// 병렬 스택 창(VS: 디버그 → 창 → 병렬 스택)과 작업 창(Tasks)도 같이 보세요. LoadGoldAsync 의 상태는 무엇인가요?
#endregion

#region 힌트 2
// await 는 기다린 뒤 "원래 있던 곳" 으로 돌아와서 다음 줄을 이어 갑니다. 원래 있던 곳 = SynchronizationContext.Current (랩 출력 첫 줄).
// Unity 메인 스레드에는 UnitySynchronizationContext 가 있어서, await 다음 줄은 "다음 프레임에 메인 스레드에서" 실행되도록 줄을 섭니다.
// 그런데 메인 스레드는 .Result 에서 그 결과를 기다리며 멈춰 있습니다. 서로가 서로를 기다립니다. 다음 프레임은 오지 않습니다.
// 콘솔 앱에는 SynchronizationContext 가 없어서(스레드 풀에서 이어 감) 같은 코드가 멀쩡했던 것입니다.
#endregion

#region 힌트 3
// 메인 스레드에서는 Task 를 막고 기다리지(.Result, .Wait(), .GetAwaiter().GetResult()) 마세요. 끝까지 비동기로 이어 가세요.
//   public async void Open(SaveFile save) { ShownGold = await save.LoadGoldAsync(); }      // Unity 이벤트/버튼 처리기라면 async void 가 흔하다
// 또는 코루틴에서:  var task = save.LoadGoldAsync(); while (!task.IsCompleted) yield return null; ShownGold = task.Result;
// Unity 6 이라면 Awaitable 도 있습니다. 라이브러리 쪽에서 await Task.Delay(...).ConfigureAwait(false) 를 쓰면 데드락은 풀리지만,
// Open 은 여전히 200ms 동안 메인 스레드를 막습니다(이 랩의 두 번째 [FAIL]). 진짜 해결은 "막고 기다리지 않기" 입니다.
#endregion
