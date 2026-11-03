// 랩 17: 보급 상자 색
// 웨이브마다 보급 상자 100개를 무작위 색으로 뿌리고, 웨이브가 끝나면 지운다. 웨이브를 거듭할수록 메모리가 늘어난다.
// 기대: 웨이브 2번을 치르고 상자를 다 지운 뒤 Material 객체 수는 시작할 때와 같다. 상자 색은 제대로 칠해져 있다.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BugHunt.Labs.Lab17
{
    public class SupplyCrate : MonoBehaviour
    {
        public Color Tint;

        public void Paint(Color tint)
        {
            Tint = tint;
            GetComponent<Renderer>().material.color = tint;
        }
    }

    [Lab(17, "보급 상자 색")]
    public class Lab17_CrateTint : Lab
    {
        public override IEnumerator Run()
        {
            var crateMaterial = Resources.Load<Material>("BugHunt/Materials/Crate");
            yield return null;
            int materialsBefore = Resources.FindObjectsOfTypeAll<Material>().Length;

            var random = new System.Random(17);
            int wrongColors = 0;
            for (int wave = 1; wave <= 2; ++wave)
            {
                var crates = new List<SupplyCrate>();
                for (int i = 0; i < 100; ++i)
                {
                    GameObject go = SpawnPrimitive(PrimitiveType.Cube, $"SupplyCrate_{wave}_{i}", new Vector3(i % 10, wave * 2, i / 10));
                    go.GetComponent<Renderer>().sharedMaterial = crateMaterial;
                    var crate = go.AddComponent<SupplyCrate>();
                    crate.Paint(new Color((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble()));
                    crates.Add(crate);
                }
                yield return null;

                foreach (SupplyCrate crate in crates)
                {
                    if (!LabUtil.SameColor(LabUtil.ShownColor(crate.GetComponent<Renderer>()), crate.Tint))
                    {
                        ++wrongColors;
                    }
                }
                Report.Note($"웨이브 {wave}: 상자 100개, 지금 Material 객체 {Resources.FindObjectsOfTypeAll<Material>().Length}개");

                foreach (SupplyCrate crate in crates)
                {
                    Destroy(crate.gameObject);
                }
                yield return null;
            }

            int materialsAfter = Resources.FindObjectsOfTypeAll<Material>().Length;
            Report.Expect("색이 틀린 상자", 0, wrongColors);
            Report.Expect("상자를 다 지운 뒤 늘어난 Material 객체", 0, materialsAfter - materialsBefore);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// 랩 출력의 웨이브별 Material 객체 수를 보세요. 상자 하나당 몇 개씩 늘어나나요? 상자를 지운 뒤에는 줄어드나요?
// Profiler 의 Memory 모듈(Window → Analysis → Profiler)에서 Objects 수가 웨이브마다 어떻게 되는지도 보세요.
#endregion

#region 힌트 2
// Paint 에 중단점을 걸고 F10 전후로 GetComponent<Renderer>().sharedMaterial.name 을 보세요. 이름 끝에 무엇이 붙나요?
// renderer.material 은 처음 접근할 때 머티리얼을 복제합니다. 그 복제본은 게임 오브젝트를 지워도 같이 지워지지 않습니다(씬 전환이나 Resources.UnloadUnusedAssets 까지 남음).
// 정확히 누가 몇 개 남았는지는 Memory Profiler 패키지(Package Manager 에서 com.unity.memoryprofiler 설치 → Window → Analysis → Memory Profiler)로
// 스냅샷 두 개를 찍어 비교(Compare)하면 "Crate (Instance)" 가 줄줄이 보입니다.
// 상자들이 한 번에 그려지는지(배칭)는 Window → Analysis → Frame Debugger 로 드로 콜을 세어 보세요.
#endregion

#region 힌트 3
// 색만 다르게 하려면 머티리얼을 복제하지 말고 MaterialPropertyBlock 으로 렌더러별 값을 주세요.
//   static MaterialPropertyBlock block;  block ??= new MaterialPropertyBlock();
//   var r = GetComponent<Renderer>(); r.GetPropertyBlock(block); block.SetColor("_Color", tint); r.SetPropertyBlock(block);
// 꼭 renderer.material 을 써야 한다면 OnDestroy 에서 Destroy(그 머티리얼) 로 직접 지우세요.
#endregion
