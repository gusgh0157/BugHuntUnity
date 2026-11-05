// 랩 55 의 일본어 표 / ラボ 55 の日本語の表
namespace BugHunt
{
    public static partial class JaText
    {
        static readonly (string Ko, string Ja)[] Lab55 =
        {
            ("상점의 금화", "ショップの金貨"),
            ("상점을 여는 순간 영원히 멈춘다(CPU 0%)", "ショップを開いた瞬間に永遠に止まる (CPU 0%)"),
            ("ShopWindow.Open: 세이브에서 금화 읽기를 기다림", "ShopWindow.Open: セーブからの金貨の読み込みを待機"),
            ("메인 스레드의 SynchronizationContext: {0}", "メインスレッドの SynchronizationContext: {0}"),
            ("상점에 보이는 금화", "ショップに表示される金貨"),
            ("Open 이 돌아온 순간 금화는 아직 읽는 중 (보인 값 {0}, 기대 -1). 메인 스레드를 막지 않음", "Open が戻った瞬間、金貨はまだ読み込み中 (表示値 {0}、期待 -1)。メインスレッドをブロックしない"),
        };
    }
}
