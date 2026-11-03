// 랩 30 의 일본어 표 / ラボ 30 の日本語の表
namespace BugHunt
{
    public static partial class JaText
    {
        static readonly (string Ko, string Ja)[] Lab30 =
        {
            ("움직이는 발판", "動く足場"),
            ("Interpolate 를 켜자 발판이 거의 안 움직이고, 위의 상자도 안 실려 간다", "補間 (Interpolate) をオンにしたら足場がほとんど動かず、上の箱も運ばれない"),
            ("발판이 간 거리(m)", "足場が進んだ距離 (m)"),
            ("상자가 실려 간 거리(m)", "箱が運ばれた距離 (m)"),
            ("상자가 발판 위에 있음 (y = {0:0.00})", "箱が足場の上にある (y = {0:0.00})"),
        };
    }
}
