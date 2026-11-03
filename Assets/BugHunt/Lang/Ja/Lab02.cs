// 랩 2 의 일본어 표 / ラボ 2 の日本語の表
namespace BugHunt
{
    public static partial class JaText
    {
        static readonly (string Ko, string Ja)[] Lab02 =
        {
            ("헤드샷", "ヘッドショット"),
            ("몸통은 괜찮은데 머리를 쏘면 NullReferenceException", "胴体は平気なのに頭を撃つと NullReferenceException"),
            ("몸통 두 발 뒤 HP {0}", "胴体に 2 発当てた後の HP {0}"),
            ("명중 수", "命中数"),
            ("허수아비 HP", "かかしの HP"),
        };
    }
}
