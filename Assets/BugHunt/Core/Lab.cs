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
