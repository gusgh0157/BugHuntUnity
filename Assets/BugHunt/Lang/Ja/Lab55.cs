// 랩 55 의 일본어 표 / ラボ 55 の日本語の表
namespace BugHunt
{
    public static partial class JaText
    {
        static readonly (string Ko, string Ja)[] Lab55 =
        {
            ("치유 오라", "回復オーラ"),
            ("기능은 맞는데 매 프레임 OverlapSphere 가 배열을 만든다", "機能は正しいのに、毎フレーム OverlapSphere が配列を作る"),
            ("범위 안 아군 {0}명, 프레임당 GC 할당 {1}바이트", "範囲内の味方 {0} 人、フレームあたりの GC アロケーション {1} バイト"),
            ("범위 안에서 치유된 아군 수", "範囲内で回復した味方の数"),
            ("범위 밖 아군의 HP", "範囲外の味方の HP"),
            ("찾는 데 GC 할당 ≤ {0}바이트/프레임", "探すときの GC アロケーション ≤ {0} バイト/フレーム"),
        };
    }
}
