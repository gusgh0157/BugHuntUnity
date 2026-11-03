// 랩 39: 팀 색
// 기획서의 팀 색은 #FF8040(주황)인데 게임 화면의 깃발은 물 빠진 연주황이다. 빨강 채널 FF 는 맞는다.
// 기대: 깃발을 찍은 화면 픽셀이 (255, 128, 64) ±3.
// 실제로 그려서 픽셀을 읽기 때문에 그래픽 장치가 필요하다(-nographics 에서는 SKIP).
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab39
{
    public class TeamBanner : MonoBehaviour
    {
        static readonly int ColorId = Shader.PropertyToID("_Color");
        MaterialPropertyBlock block;

        public void ApplyTeamColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color teamColor);
            block ??= new MaterialPropertyBlock();
            var body = GetComponent<Renderer>();
            body.GetPropertyBlock(block);
            block.SetVector(ColorId, teamColor);
            body.SetPropertyBlock(block);
        }
    }

    [Lab(39, "팀 색")]
    public class Lab39_TeamColor : Lab
    {
        RenderTexture target;
        Material bannerMaterial;
        Texture2D readback;

        public override IEnumerator Run()
        {
            Report.Note($"색 공간: {QualitySettings.activeColorSpace}");
            if (IsHeadless)
            {
                Report.Skip("깃발 픽셀", "그래픽 장치가 없습니다(-nographics). 에디터에서 Play 로 실행하세요");
                yield break;
            }

            target = new RenderTexture(8, 8, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var cam = Spawn<Camera>("BannerCamera", new Vector3(0, 0, -5));
            cam.orthographic = true;
            cam.orthographicSize = 1f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.targetTexture = target;
            cam.enabled = false;   // 랩이 원할 때만 Render()

            GameObject flag = SpawnPrimitive(PrimitiveType.Quad, "TeamBanner", Vector3.zero);
            flag.transform.localScale = Vector3.one * 4f;
            bannerMaterial = new Material(Shader.Find("Unlit/Color"));
            flag.GetComponent<Renderer>().sharedMaterial = bannerMaterial;
            string hex = Case("#FF8040", "#40A0FF");
            flag.AddComponent<TeamBanner>().ApplyTeamColor(hex);
            yield return null;

            cam.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            readback = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            readback.ReadPixels(new Rect(0, 0, 8, 8), 0, 0);
            readback.Apply();
            RenderTexture.active = previous;

            Color32 pixel = readback.GetPixel(4, 4);
            Report.Note($"깃발 픽셀: ({pixel.r}, {pixel.g}, {pixel.b})");
            ColorUtility.TryParseHtmlString(hex, out Color wanted);
            Color32 expected = wanted;
            Report.ExpectNear("빨강", expected.r, pixel.r, 3);
            Report.ExpectNear("초록", expected.g, pixel.g, 3);
            Report.ExpectNear("파랑", expected.b, pixel.b, 3);
        }

        void OnDestroy()
        {
            if (target != null)
            {
                target.Release();
                Destroy(target);
            }
            Destroy(bannerMaterial);
            Destroy(readback);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// 랩 출력의 픽셀 값과 기대값을 나란히 적으세요. 0x00 과 0xFF 는 맞고 중간값만 밝아졌다면 "곱하기" 가 아니라 "곡선" 문제입니다.
// Edit → Project Settings → Player → Other Settings → Color Space 가 무엇인지 보세요. (랩 출력 첫 줄에도 나옵니다)
#endregion

#region 힌트 2
// 0.5 를 sRGB 로 인코딩하면 약 0.735(=188) 입니다. 128/255 ≈ 0.502 와 188 을 비교해 보세요.
// Linear 색 공간에서 셰이더는 "선형" 값으로 계산하고, 화면(sRGB 렌더 타깃)에 쓸 때 sRGB 로 인코딩합니다.
// 그래서 사람이 고른 색(sRGB, 기획서의 hex)은 셰이더에 넘기기 전에 선형으로 바꿔야 합니다. 누가 바꿔 주나요?
// Frame Debugger 에서 깃발 드로 콜을 고르면 _Color 에 실제로 넘어간 숫자가 보입니다.
#endregion

#region 힌트 3
// MaterialPropertyBlock.SetColor / Material.SetColor 는 Linear 색 공간이면 sRGB → 선형 변환을 해 줍니다. SetVector 는 숫자를 그대로 넘깁니다.
// 색이면 SetColor(ColorId, teamColor) 를 쓰세요. (SetVector 를 꼭 써야 한다면 teamColor.linear)
// 텍스처도 같습니다: 색 텍스처는 Import Settings 의 sRGB (Color Texture) 를 켜고, 노멀/마스크/데이터 텍스처는 끕니다.
#endregion
