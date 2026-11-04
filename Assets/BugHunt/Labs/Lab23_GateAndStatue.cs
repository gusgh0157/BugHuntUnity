// 랩 23: 성문과 석상
// 외주에서 받은 성문(폭 3m, 높이 4m)과 석상(높이 2m) 모델을 쓴다. 성문은 왼쪽 끝 경첩을 축으로 90도 열리고, 석상은 광장 바닥의 지정된 점에 세운다.
// 성문이 경첩이 아니라 가운데를 축으로 회전문처럼 돌고, 석상은 바닥에 허리까지 묻힌다.
// 기대: 연 뒤에도 성문의 경첩 쪽 끝은 경첩 자리에 그대로 있다. 석상의 발(메시 맨 아래)은 바닥 높이에 닿는다.
// ラボ 23: 城門と石像
// 外注から受け取った城門 (幅 3m、高さ 4m) と石像 (高さ 2m) のモデルを使う。城門は左端のちょうつがいを軸に 90 度開き、石像は広場の床の指定された点に立てる。
// 城門がちょうつがいではなく真ん中を軸に回転ドアのように回り、石像は床に腰まで埋まる。
// 期待: 開いた後も、城門のちょうつがい側の端はちょうつがいの場所にそのままある。石像の足 (メッシュのいちばん下) は床の高さに接する。
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab23
{
    public class Gate : MonoBehaviour
    {
        public Vector3 Hinge;   // 경첩 위치 (월드) / ちょうつがいの位置 (ワールド)

        public void Open(float degrees)
        {
            transform.Rotate(0f, degrees, 0f);   // 경첩을 축으로 연다 / ちょうつがいを軸に開く
        }
    }

    public static class Plaza
    {
        public static void PlaceOnFloor(Transform statue, Vector3 floorPoint)
        {
            statue.position = floorPoint;   // 바닥에 세운다 / 床に立てる
        }
    }

    [Lab(23, "성문과 석상", Symptom = "성문이 가운데를 축으로 돌고, 석상이 바닥에 묻힌다")]
    public class Lab23_GateAndStatue : Lab
    {
        public override IEnumerator Run()
        {
            var gateMesh = Resources.Load<Mesh>("BugHunt/Models/Gate");
            var statueMesh = Resources.Load<Mesh>("BugHunt/Models/Statue");
            if (gateMesh == null || statueMesh == null)
            {
                Report.Fail("Resources/BugHunt/Models 의 Gate, Statue 메시가 없습니다");
                yield break;
            }

            var gate = Spawn<Gate>("Gate", Case(new Vector3(4f, 2f, 8f), new Vector3(-7f, 3f, 5f)));
            gate.transform.rotation = Quaternion.Euler(0f, Case(0f, 25f), 0f);
            gate.gameObject.AddComponent<MeshFilter>().sharedMesh = gateMesh;
            gate.gameObject.AddComponent<MeshRenderer>();
            var hingeLocal = new Vector3(gateMesh.bounds.min.x, 0f, 0f);   // 문짝의 왼쪽 끝 / 扉の左端
            gate.Hinge = gate.transform.TransformPoint(hingeLocal);

            var statue = Spawn("Statue");
            statue.AddComponent<MeshFilter>().sharedMesh = statueMesh;
            statue.AddComponent<MeshRenderer>();
            yield return null;

            Quaternion closed = gate.transform.rotation;
            gate.Open(90f);
            Report.ExpectNear("성문이 열린 각도(도)", 90f, Quaternion.Angle(closed, gate.transform.rotation), 0.5f);
            Report.ExpectNear("연 뒤 경첩 쪽 끝과 경첩 사이 거리(m)", 0f, Vector3.Distance(gate.transform.TransformPoint(hingeLocal), gate.Hinge), 0.01f);

            Vector3 floorPoint = Case(new Vector3(2f, 0f, -3f), new Vector3(-5f, 1.5f, 6f));
            Plaza.PlaceOnFloor(statue.transform, floorPoint);
            // 렌더러 bounds 는 그래픽 장치가 없으면 비어 있을 수 있어 메시 bounds 로 잰다 / レンダラーの bounds はグラフィックスデバイスがないと空のことがあるので、メッシュの bounds で測る
            float feet = statue.transform.TransformPoint(statueMesh.bounds.min).y;
            Report.ExpectNear("석상 발 높이 - 바닥 높이(m)", 0f, feet - floorPoint.y, 0.01f);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// 플레이 중 Hierarchy 에서 Gate 와 Statue 를 골라 보세요. 이동 기즈모(오브젝트의 원점)는 모델의 어디에 있나요?
// Scene 뷰 툴바의 Pivot / Center 토글을 바꿔 보세요. Center 는 bounds 의 가운데, Pivot 은 오브젝트의 진짜 원점입니다. 이 모델에선 둘이 같은 자리인가요?
// 조사식에 statueMesh.bounds 를 넣어 min 과 max 도 보세요.
//
// 再生中にヒエラルキーで Gate と Statue を選んでみてください。移動ギズモ (オブジェクトの原点) はモデルのどこにありますか?
// シーンビューのツールバーの ピボット / 中心 (Pivot / Center) トグルを切り替えてみてください。中心 (Center) は bounds の中心、ピボット (Pivot) はオブジェクトの本当の原点です。このモデルでは 2 つは同じ場所ですか?
// ウォッチ式に statueMesh.bounds を入れて、min と max も見てください。
#endregion

#region 힌트 2 / ヒント 2
// Transform 의 회전과 위치는 모두 모델의 피벗(메시 좌표의 원점)을 기준으로 합니다. 이 모델들은 피벗이 메시 한가운데에 있습니다.
// 그래서 Rotate 는 문짝 가운데를 축으로 돌고, position 에 바닥 점을 넣으면 석상의 가운데가 바닥에 놓여 아래 절반이 묻힙니다.
// 코드는 "피벗이 경첩에 있다", "피벗이 발에 있다" 고 가정했습니다. 모델이 그렇게 만들어졌는지는 코드만 봐서는 알 수 없습니다.
//
// Transform の回転と位置は、どちらもモデルのピボット (メッシュ座標の原点) を基準にします。このモデルはピボットがメッシュの真ん中にあります。
// そのため Rotate は扉の真ん中を軸に回り、position に床の点を入れると石像の真ん中が床に置かれて下半分が埋まります。
// コードは「ピボットがちょうつがいにある」「ピボットが足にある」と仮定していました。モデルがそう作られているかは、コードを見ただけでは分かりません。
#endregion

#region 힌트 3 / ヒント 3
// 피벗이 아닌 점을 축으로 돌리거나 기준으로 놓으려면 그 점을 직접 씁니다.
//   transform.RotateAround(Hinge, Vector3.up, degrees);                                       // 경첩을 지나는 축으로
//   float bottom = statue.GetComponent<MeshFilter>().sharedMesh.bounds.min.y * statue.lossyScale.y;
//   statue.position = floorPoint - Vector3.up * bottom;                                       // 발이 바닥에 오게 (bottom 은 음수)
// 근본적인 고침은 모델의 피벗을 맞추는 것입니다: 모델링 툴에서 원점을 옮기거나, 빈 부모(경첩 위치, 발 위치)를 만들어 모델을 그 자식으로 옮겨 둡니다.
//
// ピボットではない点を軸に回したり基準に置いたりするには、その点を直接使います。
//   transform.RotateAround(Hinge, Vector3.up, degrees);                                       // ちょうつがいを通る軸で
//   float bottom = statue.GetComponent<MeshFilter>().sharedMesh.bounds.min.y * statue.lossyScale.y;
//   statue.position = floorPoint - Vector3.up * bottom;                                       // 足が床に来るように (bottom は負)
// 根本的な直し方はモデルのピボットを合わせることです: モデリングツールで原点を動かすか、空の親 (ちょうつがいの位置、足の位置) を作ってモデルをその子に移しておきます。
#endregion
