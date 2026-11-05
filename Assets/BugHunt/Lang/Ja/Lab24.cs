// 랩 24 의 일본어 표 / ラボ 24 の日本語の表
namespace BugHunt
{
    public static partial class JaText
    {
        static readonly (string Ko, string Ja)[] Lab24 =
        {
            ("단축키 칸", "ショートカットスロット"),
            ("디자이너가 칸 배열을 3칸으로 줄이자 IndexOutOfRangeException, 빈 단축키에서 ArgumentOutOfRangeException", "デザイナーがスロットの配列を 3 つに減らすと IndexOutOfRangeException、空のショートカットキーで ArgumentOutOfRangeException"),
            ("단축 바 칸 수", "ホットバーのスロット数"),
            ("단축키 {0}", "ショートカットキー {0}"),
            ("빈 단축키 {0}", "空のショートカットキー {0}"),

            // 게임 데이터(아이템 이름) / ゲームデータ (アイテム名)
            ("검", "剣"),
            ("활", "弓"),
            ("물약", "ポーション"),
            ("폭탄", "爆弾"),
            ("창", "槍"),
            ("방패", "盾"),
        };
    }
}
