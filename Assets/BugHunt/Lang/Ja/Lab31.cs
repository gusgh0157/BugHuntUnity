// 랩 31 의 일본어 표 / ラボ 31 の日本語の表
namespace BugHunt
{
    public static partial class JaText
    {
        static readonly (string Ko, string Ja)[] Lab31 =
        {
            ("경로 캐시", "経路キャッシュ"),
            ("예외는 없는데 오래 하면 메모리가 늘고, 멈추면 Leak Detected", "例外はないのに長く遊ぶとメモリが増え、止めると Leak Detected"),
            ("누수 감지 수준 (Preferences → Jobs → Leak Detection Level): {0}", "リーク検出レベル (環境設定 → Jobs → Leak Detection Level): {0}"),
            ("네이티브 버퍼 수명", "ネイティブバッファの寿命"),
            ("이 빌드는 컬렉션 안전 검사(ENABLE_UNITY_COLLECTIONS_CHECKS)가 꺼져 있습니다. 에디터에서 실행하세요", "このビルドはコレクションの安全チェック (ENABLE_UNITY_COLLECTIONS_CHECKS) がオフです。エディターで実行してください"),
            ("목표를 {0}번 바꾼 뒤 살아 있는 버퍼 {1}개 (지금 쓰는 것 1개면 정상)", "目標を {0} 回変えた後に生きているバッファ {1} 個 (今使っている 1 個なら正常)"),
            ("마지막 경로의 점 수", "最後の経路の点の数"),
            ("마지막 경로의 끝점", "最後の経路の終点"),
            ("쓰는 중에 살아 있는 버퍼 (지금 경로 하나)", "使用中に生きているバッファ (今の経路 1 つ)"),
            ("순찰병을 없앤 뒤 살아 있는 버퍼", "巡回兵を消した後に生きているバッファ"),
        };
    }
}
