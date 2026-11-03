// 랩 55: 세이브 불러오기
// 세이브 파일 해석이 무거워 Awaitable.BackgroundThreadAsync() 로 백그라운드에서 하도록 바꿨다. 불러오기 버튼을 누르면
// "get_gameObject can only be called from the main thread" 예외가 나고 캐릭터 이름과 크기가 그대로다.
// 기대: 해석은 백그라운드 스레드에서, 캐릭터에 적용하는 것은 메인 스레드에서 한다. 불러오기가 끝나면 이름과 크기가 세이브대로다.
using System.Collections;
using System.Threading;
using UnityEngine;

namespace BugHunt.Labs.Lab55
{
    [System.Serializable]
    public class SaveData
    {
        public string Name;
        public float Scale;
    }

    public class SaveLoader : MonoBehaviour
    {
        public bool Loaded;
        public int ParseThreadId;

        public async void Load(string json)
        {
            await Awaitable.BackgroundThreadAsync();
            SaveData data = Parse(json);

            gameObject.name = data.Name;
            transform.localScale = Vector3.one * data.Scale;
            Loaded = true;
        }

        SaveData Parse(string json)
        {
            ParseThreadId = Thread.CurrentThread.ManagedThreadId;
            Thread.Sleep(50);   // (무거운 해석이라고 치자)
            return JsonUtility.FromJson<SaveData>(json);
        }
    }

    [Lab(55, "세이브 불러오기")]
    public class Lab55_SaveLoader : Lab
    {
        public override IEnumerator Run()
        {
            int mainThreadId = Thread.CurrentThread.ManagedThreadId;
            var loader = Spawn<SaveLoader>("Hero");
            var save = new SaveData { Name = Case("Knight_Ari", "Mage_Bom"), Scale = Case(1.5f, 0.8f) };
            yield return null;

            loader.Load(JsonUtility.ToJson(save));
            float deadline = Time.realtimeSinceStartup + 3f;
            while (!loader.Loaded && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Report.ExpectTrue("해석을 백그라운드 스레드에서 함", loader.ParseThreadId != 0 && loader.ParseThreadId != mainThreadId);
            Report.ExpectTrue("불러오기 끝남", loader.Loaded);
            Report.Expect("캐릭터 이름", save.Name, loader.name);
            Report.ExpectNear("캐릭터 크기", save.Scale, loader.transform.localScale.x, 0.001f);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// Load 의 첫 줄과 gameObject.name 줄에 중단점을 걸고, 멈출 때마다 디버그 → 창 → 스레드 에서 어느 스레드인지 보세요(조사식 Thread.CurrentThread.ManagedThreadId 도).
// await 한 줄을 지나면서 스레드가 바뀝니다. 바뀐 뒤 다시 돌아오는 줄이 있나요?
#endregion

#region 힌트 2
// await 다음 줄이 어느 스레드에서 도는지는 "무엇을 await 했나" 가 정합니다.
//   보통의 await Task (메인 스레드에서 시작) → UnitySynchronizationContext 가 다음 줄을 메인 스레드로 돌려보낸다(랩 54 의 정답).
//   await Awaitable.BackgroundThreadAsync() → 다음 줄부터 스레드 풀. 돌아오라고 할 때까지 계속 백그라운드다.
//   await Task.Run(...).ConfigureAwait(false) → 다음 줄이 스레드 풀(랩 30 의 함정).
// 백그라운드에서 Unity API 를 부르면 UnityException 이 납니다. 이 랩은 async void 라서 예외가 Console 에 나옵니다(랩 54 의 Task 는 삼켰습니다).
#endregion

#region 힌트 3
//   await Awaitable.BackgroundThreadAsync();
//   SaveData data = Parse(json);
//   await Awaitable.MainThreadAsync();          // 여기부터 다시 메인 스레드
//   gameObject.name = data.Name; ...
// JsonUtility.FromJson 은 백그라운드 스레드에서 불러도 되는 몇 안 되는 Unity API 입니다(Unity 오브젝트가 아닌 일반 클래스로 받을 때).
// 오브젝트가 그사이 파괴됐을 수 있으니, 메인 스레드로 돌아온 뒤 if (this == null) return; 을 넣으면 더 안전합니다(destroyCancellationToken 도 있습니다).
#endregion
