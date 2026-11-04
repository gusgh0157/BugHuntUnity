// 랩 62: 늘린 다리의 함정
// 레벨 디자이너가 1m 상자를 늘려(크기 12 x 0.4 x 3) 다리를 만들었다. 다리 위에 1m 상자 함정을 다리 중심에서 몇 m 떨어진 곳에 놓는다.
// 함정이 다리 끝 너머에 떨어져 있고, 납작하고 길쭉한 판자가 되어 다리에 반쯤 묻힌다.
// 기대: 함정은 다리 중심에서 다리 방향으로 offset m 떨어진, 다리 윗면 위에 1m 정육면체로 놓인다.
// ラボ 62: 引き伸ばした橋の罠
// レベルデザイナーが 1m の箱を引き伸ばして (スケール 12 x 0.4 x 3) 橋を作った。橋の上に 1m の箱の罠を、橋の中心から数 m 離れた場所に置く。
// 罠が橋の端の向こうに離れていて、平たく細長い板になって橋に半分埋まる。
// 期待: 罠は橋の中心から橋の方向へ offset m 離れた、橋の上面の上に 1m の立方体として置かれる。
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab62
{
    public class Bridge : MonoBehaviour
    {
        public Transform PlaceTrap(float offset)
        {
            var trap = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;   // 1m 상자 / 1m の箱
            trap.name = "Trap";
            trap.SetParent(transform, false);   // 다리와 함께 움직이게 / 橋と一緒に動くように
            trap.localPosition = new Vector3(offset, transform.localScale.y * 0.5f + 0.5f, 0f);   // 윗면 위 / 上面の上
            trap.localScale = Vector3.one;   // 1m 그대로 / 1m のまま
            return trap;
        }
    }

    [Lab(62, "늘린 다리의 함정", Symptom = "늘린 다리 위에 놓은 함정이 엉뚱한 곳에서 납작해진다")]
    public class Lab62_StretchedBridge : Lab
    {
        public override IEnumerator Run()
        {
            var bridgeObject = SpawnPrimitive(PrimitiveType.Cube, "Bridge", Case(new Vector3(0f, 2f, 5f), new Vector3(-6f, 3f, 2f)));
            bridgeObject.transform.localScale = new Vector3(Case(12f, 14f), 0.4f, 3f);
            bridgeObject.transform.rotation = Quaternion.Euler(0f, Case(0f, 30f), 0f);
            var bridge = bridgeObject.AddComponent<Bridge>();
            yield return null;

            float offset = Case(3f, -4f);
            Transform trap = bridge.PlaceTrap(offset);
            float halfThickness = bridge.transform.lossyScale.y * 0.5f;
            Vector3 expected = bridge.transform.position + bridge.transform.rotation * new Vector3(offset, halfThickness + 0.5f, 0f);
            Report.ExpectNear("함정 위치와 기대 위치 사이 거리(m)", 0f, Vector3.Distance(trap.position, expected), 0.01f);
            Report.ExpectNear("함정의 보이는 크기와 1m 정육면체의 차이(lossyScale)", 0f, Vector3.Distance(trap.lossyScale, Vector3.one), 0.01f);
            Report.ExpectNear("함정 바닥 높이 - 다리 윗면 높이(m)", 0f, (trap.position.y - trap.lossyScale.y * 0.5f) - (bridge.transform.position.y + halfThickness), 0.01f);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// PlaceTrap 의 return 줄에서 조사식에 trap.position, trap.localPosition, trap.lossyScale, trap.localScale 을 보세요.
// 플레이 중 Hierarchy 에서 Trap 을 고르면 Inspector 의 Scale 은 (1, 1, 1) 인데, Scene 뷰에서는 어떤 모양인가요?
//
// PlaceTrap の return の行で、ウォッチ式に trap.position、trap.localPosition、trap.lossyScale、trap.localScale を見てください。
// 再生中にヒエラルキーで Trap を選ぶと、インスペクターのスケール (Scale) は (1, 1, 1) なのに、シーンビューではどんな形ですか?
#endregion

#region 힌트 2 / ヒント 2
// 자식의 localPosition 과 localScale 은 부모의 크기를 거쳐 월드가 됩니다. 부모 크기가 (12, 0.4, 3) 이면 로컬 x 3 은 월드 36m, 로컬 크기 1 은 월드 (12, 0.4, 3) 입니다.
// lossyScale 은 부모들의 크기를 모두 곱한 "보이는 크기" 입니다. 이름이 lossy(손실 있는) 인 것은, 크기가 고르지 않은 부모 밑에서 자식이 회전하면
// 모양이 찌그러져(기울어진 평행육면체) 축 세 개짜리 크기로는 정확히 나타낼 수 없기 때문입니다.
//
// 子の localPosition と localScale は、親のスケールを経由してワールドになります。親のスケールが (12, 0.4, 3) なら、ローカルの x 3 はワールドで 36m、ローカルのスケール 1 はワールドで (12, 0.4, 3) です。
// lossyScale は親のスケールをすべて掛けた「見た目のスケール」です。名前が lossy (損失のある) なのは、スケールが均一でない親の下で子が回転すると
// 形がゆがんで (傾いた平行六面体)、3 軸のスケールでは正確に表せないからです。
#endregion

#region 힌트 3 / ヒント 3
// 월드 값으로 놓고, 크기는 부모 크기로 나눠 되돌립니다.
//   trap.position = transform.position + transform.rotation * new Vector3(offset, transform.localScale.y * 0.5f + 0.5f, 0f);
//   trap.localScale = new Vector3(1f / transform.lossyScale.x, 1f / transform.lossyScale.y, 1f / transform.lossyScale.z);
// 더 근본적인 고침은 크기를 붙이는 것들의 부모에 두지 않는 것입니다. Bridge(크기 1) 밑에 보이는 모양만 맡는 자식 Visual 을 두고 Visual 만 늘리면,
// 함정은 Bridge 의 자식으로 미터 단위 그대로 놓을 수 있습니다.
//
// ワールドの値で置き、スケールは親のスケールで割って戻します。
//   trap.position = transform.position + transform.rotation * new Vector3(offset, transform.localScale.y * 0.5f + 0.5f, 0f);
//   trap.localScale = new Vector3(1f / transform.lossyScale.x, 1f / transform.lossyScale.y, 1f / transform.lossyScale.z);
// もっと根本的な直し方は、スケールを物を付ける親に置かないことです。Bridge (スケール 1) の下に見た目だけを受け持つ子 Visual を置いて Visual だけを伸ばせば、
// 罠は Bridge の子としてメートル単位のまま置けます。
#endregion
