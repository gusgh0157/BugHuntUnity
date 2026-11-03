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
        public bool MakesSound { get; set; }

        public LabAttribute(int number, string title)
        {
            Number = number;
            Title = title;
        }
    }

    // 모든 랩의 부모. LabRunner 가 빈 게임 오브젝트 "LabNN" 에 붙이고 Run() 을 코루틴으로 돌린다.
    // 랩이 끝나면(또는 예외로 멈추면) 그 오브젝트를 통째로 Destroy 한다. Spawn 으로 만든 것은 전부 그 아래에 들어간다.
    public abstract class Lab : MonoBehaviour
    {
        public virtual float TimeoutSeconds => 15f;

        // 채점기가 켜는 "숨은 테스트 케이스". 켜져 있으면 Case(보통, 숨은) 가 두 번째 값을 준다.
        // 정답을 상수로 박아 넣은(하드코딩) 코드를 걸러 내려고, 같은 버그를 다른 입력으로 한 번 더 돌린다.
        public static bool HiddenCase { get; set; }

        // 이번 실행에서 Case 를 한 번이라도 불렀나. 안 불렀다면 숨은 케이스로 돌려도 똑같으므로 채점기가 건너뛴다.
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
        protected static bool IsHeadless => SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;
    }
}
