// 랩 58 의 일본어 표 / ラボ 58 の日本語の表
namespace BugHunt
{
    public static partial class JaText
    {
        static readonly (string Ko, string Ja)[] Lab58 =
        {
            ("풍차", "風車"),
            ("풍차 날개가 위를 가리킬 즈음 떨면서 멈춘다", "風車の羽根が真上を指すあたりで震えて止まる"),
            ("{0} 프레임 동안 돌았어야 할 각도 {1:0}도", "{0} フレームの間に回っているはずの角度 {1:0} 度"),
            ("날개 방향(transform.up)과 기대 방향의 차이(도)", "羽根の向き (transform.up) と期待する向きの差 (度)"),
        };
    }
}
