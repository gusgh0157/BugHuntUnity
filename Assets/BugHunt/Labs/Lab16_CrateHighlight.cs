// 랩 16: 상자 강조
// 마우스를 올린 상자만 빨개져야 하는데 창고의 상자가 전부 빨개진다. 플레이를 멈춘 뒤에도 Crate 머티리얼이 빨갛게 남는 경우가 있다.
// 기대: 강조한 상자만 빨강, 옆 상자와 머티리얼 에셋은 흰색.
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab16
{
    public class Crate : MonoBehaviour
    {
        Renderer body;

        void Awake()
        {
            body = GetComponent<Renderer>();
        }

        public void OnHoverEnter()
        {
            body.sharedMaterial.color = Color.red;
        }

        public void OnHoverExit()
        {
            body.sharedMaterial.color = Color.white;
        }
    }

    [Lab(16, "상자 강조")]
    public class Lab16_CrateHighlight : Lab
    {
        public override IEnumerator Run()
        {
            var crateMaterial = Resources.Load<Material>("BugHunt/Materials/Crate");
            crateMaterial.color = Color.white;   // 이전 실행이 남긴 색을 지우고 시작

            var crates = new Crate[3];
            for (int i = 0; i < crates.Length; ++i)
            {
                GameObject go = SpawnPrimitive(PrimitiveType.Cube, $"Crate_{i}", new Vector3(i * 1.5f, 0, 5));
                go.GetComponent<Renderer>().sharedMaterial = crateMaterial;
                crates[i] = go.AddComponent<Crate>();
            }
            yield return null;

            crates[0].OnHoverEnter();
            yield return null;

            Color hovered = LabUtil.ShownColor(crates[0].GetComponent<Renderer>());
            Color neighbor = LabUtil.ShownColor(crates[1].GetComponent<Renderer>());
            Report.ExpectTrue($"강조한 상자는 빨강 (실제 {hovered})", LabUtil.SameColor(hovered, Color.red));
            Report.ExpectTrue($"옆 상자는 흰색 (실제 {neighbor})", LabUtil.SameColor(neighbor, Color.white));
            Report.ExpectTrue($"Crate.mat 에셋은 흰색 (실제 {crateMaterial.color})", LabUtil.SameColor(crateMaterial.color, Color.white));

            crates[0].OnHoverExit();
            yield return null;
            Color restored = LabUtil.ShownColor(crates[0].GetComponent<Renderer>());
            Report.ExpectTrue($"마우스를 떼면 흰색으로 (실제 {restored})", LabUtil.SameColor(restored, Color.white));
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// OnHoverEnter 에 중단점. 조사식에 body.sharedMaterial.name 과 body.sharedMaterial.GetInstanceID() 를 넣으세요.
// 다른 상자(Crate_1)의 렌더러에서 같은 조사식을 보면? (Hierarchy 에서 Crate_1 을 골라 Inspector 의 Materials 도 보세요)
#endregion

#region 힌트 2
// 랩을 돌린 뒤 플레이를 멈추고 Project 창에서 Assets/BugHunt/Resources/BugHunt/Materials/Crate.mat 의 색을 보세요. git status 도.
// sharedMaterial 은 "에셋 그 자체" 입니다. 같은 에셋을 쓰는 모든 렌더러가 바뀌고, 에디터에서는 에셋 파일에까지 저장될 수 있습니다.
#endregion

#region 힌트 3
// 이 렌더러만 바꾸려면 body.material 을 쓰세요. 처음 접근할 때 이 렌더러 전용 사본(이름에 "(Instance)")을 만들어 끼워 줍니다.
// 단, 그 사본은 여러분이 Destroy 해야 합니다(랩 17). 색 하나만 바꾸는 거라면 MaterialPropertyBlock 이 더 가볍습니다.
#endregion
