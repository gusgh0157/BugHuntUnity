using System;
using System.Collections;
using UnityEngine;

namespace BugHunt
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class LabAttribute : Attribute
    {
        public int Number { get; }
        public string Title { get; }

        // 소리를 내는 랩. 랩 실행 창과 Console 첫 줄에 볼륨 주의를 띄운다.
        // 音が出るラボ。ラボ実行ウィンドウとコンソールの 1 行目に音量の注意を出します。
        public bool MakesSound { get; set; }

        // 랩 실행 창에 이름과 함께 보이는 한 줄 증상(README 랩 표의 "증상"). Title 처럼 일본어 표의 키다.
        // ラボ実行ウィンドウで名前と一緒に表示される 1 行の症状 (README のラボ表の「症状」)。Title と同じく日本語の表のキーです。
        public string Symptom { get; set; }

        public LabAttribute(int number, string title)
        {
            Number = number;
            Title = title;
        }
    }

    // 모든 랩의 부모. LabRunner 가 빈 게임 오브젝트 "LabNN" 에 붙이고 Run() 을 코루틴으로 돌린다.
    // 랩이 끝나면(또는 예외로 멈추면) 그 오브젝트를 통째로 Destroy 한다. Spawn 으로 만든 것은 전부 그 아래에 들어간다.
    // すべてのラボの親。LabRunner が空のゲームオブジェクト "LabNN" に付け、Run() をコルーチンとして回します。
    // ラボが終わったら (または例外で止まったら) そのオブジェクトを丸ごと Destroy します。Spawn で作ったものはすべてその下に入ります。
    public abstract class Lab : MonoBehaviour
    {
        public virtual float TimeoutSeconds => 15f;

        // 채점기가 켜는 "숨은 테스트 케이스". 켜져 있으면 Case(보통, 숨은) 가 두 번째 값을 준다.
        // 정답을 상수로 박아 넣은(하드코딩) 코드를 걸러 내려고, 같은 버그를 다른 입력으로 한 번 더 돌린다.
        // 採点ツールがオンにする「隠しテストケース」。オンのときは Case(通常, 隠し) が 2 番目の値を返します。
        // 正解を定数で埋め込んだ (ハードコードした) コードをはじくため、同じバグを別の入力でもう一度回します。
        public static bool HiddenCase { get; set; }

        // 이번 실행에서 Case 를 한 번이라도 불렀나. 안 불렀다면 숨은 케이스로 돌려도 똑같으므로 채점기가 건너뛴다.
        // 今回の実行で Case を一度でも呼んだか。呼んでいなければ隠しケースで回しても同じなので、採点ツールは飛ばします。
        public static bool UsedCase { get; internal set; }

        protected static T Case<T>(T normal, T hidden)
        {
            UsedCase = true;
            return HiddenCase ? hidden : normal;
        }

        public abstract IEnumerator Run();

        protected GameObject Spawn(string objectName, Vector3 position = default)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            return go;
        }

        // 주의: AddComponent 는 그 자리에서 Awake 와 OnEnable 을 부른다. Start 는 다음 Update 직전이다.
        // 注意: AddComponent はその場で Awake と OnEnable を呼びます。Start は次の Update の直前です。
        protected T Spawn<T>(string objectName, Vector3 position = default) where T : Component
        {
            return Spawn(objectName, position).AddComponent<T>();
        }

        protected GameObject SpawnPrimitive(PrimitiveType type, string objectName, Vector3 position = default)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = objectName;
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            return go;
        }

        // 그래픽 장치 없이(-nographics) 실행 중인가. 화면을 그려야 판정할 수 있는 랩은 SKIP 한다.
        // グラフィックスデバイスなし (-nographics) で実行中か。画面を描かないと判定できないラボは SKIP します。
        protected static bool IsHeadless => SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;
    }
}
