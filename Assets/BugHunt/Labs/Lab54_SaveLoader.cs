// 랩 54: 세이브 불러오기
// 세이브 파일 해석이 무거워 Awaitable.BackgroundThreadAsync() 로 백그라운드에서 하도록 바꿨다. 불러오기 버튼을 누르면
// "get_gameObject can only be called from the main thread" 예외가 나고 캐릭터 이름과 크기가 그대로다.
// 기대: 해석은 백그라운드 스레드에서, 캐릭터에 적용하는 것은 메인 스레드에서 한다. 불러오기가 끝나면 이름과 크기가 세이브대로다.
// ラボ 54: セーブの読み込み
// セーブファイルの解析が重いので、Awaitable.BackgroundThreadAsync() でバックグラウンドで行うように変えた。読み込みボタンを押すと
// "get_gameObject can only be called from the main thread" 例外が出て、キャラクターの名前と大きさが元のままだ。
// 期待: 解析はバックグラウンドスレッドで、キャラクターへの適用はメインスレッドで行う。読み込みが終わると名前と大きさがセーブどおりになる。
using System.Collections;
using System.Threading;
using UnityEngine;

namespace BugHunt.Labs.Lab54
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
            Thread.Sleep(50);   // (무거운 해석이라고 치자) / (重い解析だとしておく)
            return JsonUtility.FromJson<SaveData>(json);
        }
    }

    [Lab(54, "세이브 불러오기", "セーブデータの読み込み", Symptom = "백그라운드 해석으로 바꾸자 \"can only be called from the main thread\"", SymptomJa = "バックグラウンド解析に変えたら \"can only be called from the main thread\"")]
    public class Lab54_SaveLoader : Lab
    {
        Application.LogCallback onLog;

        public override IEnumerator Run()
        {
            int mainThreadId = Thread.CurrentThread.ManagedThreadId;
            var loader = Spawn<SaveLoader>("Hero");
            var save = new SaveData { Name = Case("Knight_Ari", "Mage_Bom"), Scale = Case(1.5f, 0.8f) };
            yield return null;

            // 시간이 아니라 "끝났거나 예외가 났거나" 로 기다린다(CPU 가 바쁘면 백그라운드 해석이 늦게 끝난다).
            // 時間ではなく「終わったか、例外が出たか」で待つ (CPU が忙しいとバックグラウンドの解析が遅れて終わる)。
            bool failed = false;
            onLog = (message, stackTrace, type) => failed |= type == LogType.Exception || type == LogType.Error;
            Application.logMessageReceivedThreaded += onLog;
            loader.Load(JsonUtility.ToJson(save));
            while (!loader.Loaded && !failed)
            {
                yield return null;
            }
            Application.logMessageReceivedThreaded -= onLog;

            Report.ExpectTrue(Loc.L("해석을 백그라운드 스레드에서 함", "解析をバックグラウンドスレッドで行った"), loader.ParseThreadId != 0 && loader.ParseThreadId != mainThreadId);
            Report.ExpectTrue(Loc.L("불러오기 끝남", "読み込みが終わった"), loader.Loaded);
            Report.Expect(Loc.L("캐릭터 이름", "キャラクターの名前"), save.Name, loader.name);
            Report.ExpectNear(Loc.L("캐릭터 크기", "キャラクターの大きさ"), save.Scale, loader.transform.localScale.x, 0.001f);
        }

        void OnDestroy()
        {
            Application.logMessageReceivedThreaded -= onLog;
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// Load 의 첫 줄과 gameObject.name 줄에 중단점을 걸고, 멈출 때마다 디버그 → 창 → 스레드 에서 어느 스레드인지 보세요(조사식 Thread.CurrentThread.ManagedThreadId 도).
// await 한 줄을 지나면서 스레드가 바뀝니다. 바뀐 뒤 다시 돌아오는 줄이 있나요?
//
// Load の 1 行目と gameObject.name の行にブレークポイントを置き、止まるたびに デバッグ → ウィンドウ → スレッド でどのスレッドか見てください (ウォッチ式 Thread.CurrentThread.ManagedThreadId も)。
// await の 1 行を通過するときにスレッドが変わります。変わった後、また戻ってくる行はありますか?
#endregion

#region 힌트 2 / ヒント 2
// await 다음 줄이 어느 스레드에서 도는지는 "무엇을 await 했나" 가 정합니다.
//   보통의 await Task (메인 스레드에서 시작) → UnitySynchronizationContext 가 다음 줄을 메인 스레드로 돌려보낸다(랩 53 의 정답).
//   await Awaitable.BackgroundThreadAsync() → 다음 줄부터 스레드 풀. 돌아오라고 할 때까지 계속 백그라운드다.
//   await Task.Run(...).ConfigureAwait(false) → 다음 줄이 스레드 풀(랩 55 의 함정).
// 백그라운드에서 Unity API 를 부르면 UnityException 이 납니다. 이 랩은 async void 라서 예외가 Console 에 나옵니다(랩 53 의 Task 는 삼켰습니다).
//
// await の次の行がどのスレッドで動くかは「何を await したか」で決まります。
//   普通の await Task (メインスレッドで開始) → UnitySynchronizationContext が次の行をメインスレッドに戻す (ラボ 53 の正解)。
//   await Awaitable.BackgroundThreadAsync() → 次の行からスレッドプール。戻れと言うまでずっとバックグラウンドだ。
//   await Task.Run(...).ConfigureAwait(false) → 次の行がスレッドプール (ラボ 55 の落とし穴)。
// バックグラウンドで Unity API を呼ぶと UnityException が出ます。このラボは async void なので例外がコンソールに出ます (ラボ 53 の Task は握りつぶしました)。
#endregion

#region 힌트 3 / ヒント 3
//   await Awaitable.BackgroundThreadAsync();
//   SaveData data = Parse(json);
//   await Awaitable.MainThreadAsync();          // 여기부터 다시 메인 스레드
//   gameObject.name = data.Name; ...
// JsonUtility.FromJson 은 백그라운드 스레드에서 불러도 되는 몇 안 되는 Unity API 입니다(Unity 오브젝트가 아닌 일반 클래스로 받을 때).
// 오브젝트가 그사이 파괴됐을 수 있으니, 메인 스레드로 돌아온 뒤 if (this == null) return; 을 넣으면 더 안전합니다(destroyCancellationToken 도 있습니다).
//
//   await Awaitable.BackgroundThreadAsync();
//   SaveData data = Parse(json);
//   await Awaitable.MainThreadAsync();          // ここから再びメインスレッド
//   gameObject.name = data.Name; ...
// JsonUtility.FromJson は、バックグラウンドスレッドから呼んでもよい数少ない Unity API です (Unity オブジェクトではない普通のクラスで受け取るとき)。
// その間にオブジェクトが破棄されているかもしれないので、メインスレッドに戻った後に if (this == null) return; を入れるとより安全です (destroyCancellationToken もあります)。
#endregion
