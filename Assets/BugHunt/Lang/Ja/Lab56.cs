// 랩 56 의 일본어 표 / ラボ 56 の日本語の表
namespace BugHunt
{
    public static partial class JaText
    {
        static readonly (string Ko, string Ja)[] Lab56 =
        {
            ("엘리베이터", "エレベーター"),
            ("엘리베이터를 부르는 순간 게임 전체가 멈춘다(CPU 한 코어 100%)", "エレベーターを呼んだ瞬間にゲーム全体が止まる (CPU 1 コア 100%)"),
            ("Passenger.RideTo: 엘리베이터가 오기를 기다림", "Passenger.RideTo: エレベーターが来るのを待機"),
            ("Passenger.RideTo: 목표 층에 닿기를 기다림", "Passenger.RideTo: 目的の階に着くのを待機"),
            ("승객이 도착함", "乗客が到着した"),
            ("승객이 있는 층", "乗客がいる階"),
            ("기다리는 동안 게임이 계속 돎 ({0}프레임)", "待つ間もゲームが回り続けた ({0} フレーム)"),
        };
    }
}
