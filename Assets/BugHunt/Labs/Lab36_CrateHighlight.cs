// 랩 36: 상자 강조
// 마우스를 올린 상자만 빨개져야 하는데 창고의 상자가 전부 빨개진다. 플레이를 멈춘 뒤에도 Crate 머티리얼이 빨갛게 남는 경우가 있다.
// 기대: 강조한 상자만 빨강, 옆 상자와 머티리얼 에셋은 흰색.
// ラボ 36: 木箱のハイライト
// マウスを乗せた木箱だけが赤くなるはずが、倉庫の木箱がすべて赤くなる。再生を止めた後も Crate マテリアルが赤いまま残ることがある。
// 期待: ハイライトした木箱だけ赤、隣の木箱とマテリアルアセットは白。
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab36
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

    [Lab(36, "상자 강조", Symptom = "한 상자에 마우스를 올렸는데 전부 빨개진다")]
    public class Lab36_CrateHighlight : Lab
    {
        public override IEnumerator Run()
        {
            var crateMaterial = Resources.Load<Material>("BugHunt/Materials/Crate");
            crateMaterial.color = Color.white;   // 이전 실행이 남긴 색을 지우고 시작 / 前回の実行が残した色を消してから始める

            var crates = new Crate[3];
            for (int i = 0; i < crates.Length; ++i)
            {
                GameObject go = SpawnPrimitive(PrimitiveType.Cube, $"Crate_{i}", new Vector3(i * 1.5f, 0, 5));
                go.GetComponent<Renderer>().sharedMaterial = crateMaterial;
                crates[i] = go.AddComponent<Crate>();
            }
            yield return null;

            int pick = Case(0, 2);
            int other = 1;
            crates[pick].OnHoverEnter();
            yield return null;

            Color hovered = LabUtil.ShownColor(crates[pick].GetComponent<Renderer>());
            Color neighbor = LabUtil.ShownColor(crates[other].GetComponent<Renderer>());
            Report.ExpectTrue(Loc.F("강조한 상자는 빨강 (실제 {0})", hovered), LabUtil.SameColor(hovered, Color.red));
            Report.ExpectTrue(Loc.F("옆 상자는 흰색 (실제 {0})", neighbor), LabUtil.SameColor(neighbor, Color.white));
            Report.ExpectTrue(Loc.F("Crate.mat 에셋은 흰색 (실제 {0})", crateMaterial.color), LabUtil.SameColor(crateMaterial.color, Color.white));

            crates[pick].OnHoverExit();
            yield return null;
            Color restored = LabUtil.ShownColor(crates[pick].GetComponent<Renderer>());
            Report.ExpectTrue(Loc.F("마우스를 떼면 흰색으로 (실제 {0})", restored), LabUtil.SameColor(restored, Color.white));
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// OnHoverEnter 에 중단점. 조사식에 body.sharedMaterial.name 과 body.sharedMaterial.GetInstanceID() 를 넣으세요.
// 다른 상자(Crate_1)의 렌더러에서 같은 조사식을 보면? (Hierarchy 에서 Crate_1 을 골라 Inspector 의 Materials 도 보세요)
//
// OnHoverEnter にブレークポイント。ウォッチ式に body.sharedMaterial.name と body.sharedMaterial.GetInstanceID() を入れてください。
// 別の木箱 (Crate_1) のレンダラーで同じウォッチ式を見ると? (ヒエラルキーで Crate_1 を選び、インスペクターのマテリアル (Materials) も見てください)
#endregion

#region 힌트 2 / ヒント 2
// 랩을 돌린 뒤 플레이를 멈추고 Project 창에서 Assets/BugHunt/Resources/BugHunt/Materials/Crate.mat 의 색을 보세요. git status 도.
// sharedMaterial 은 "에셋 그 자체" 입니다. 같은 에셋을 쓰는 모든 렌더러가 바뀌고, 에디터에서는 에셋 파일에까지 저장될 수 있습니다.
//
// ラボを実行した後に再生を止め、プロジェクト (Project) ウィンドウで Assets/BugHunt/Resources/BugHunt/Materials/Crate.mat の色を見てください。git status も。
// sharedMaterial は「アセットそのもの」です。同じアセットを使うすべてのレンダラーが変わり、エディターではアセットファイルにまで保存されることがあります。
#endregion

#region 힌트 3 / ヒント 3
// 이 렌더러만 바꾸려면 body.material 을 쓰세요. 처음 접근할 때 이 렌더러 전용 사본(이름에 "(Instance)")을 만들어 끼워 줍니다.
// 단, 그 사본은 여러분이 Destroy 해야 합니다(랩 37). 색 하나만 바꾸는 거라면 MaterialPropertyBlock 이 더 가볍습니다.
//
// このレンダラーだけを変えるなら body.material を使ってください。最初にアクセスしたときに、このレンダラー専用のコピー (名前に "(Instance)") を作って差し替えてくれます。
// ただし、そのコピーは自分で Destroy しなければなりません (ラボ 37)。色を 1 つ変えるだけなら MaterialPropertyBlock のほうが軽いです。
#endregion
