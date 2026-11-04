// 랩 52: 한정 판매
// 전설 검을 딱 한 자루 판다. 결제는 오래 걸리므로 구매 요청마다 Task.Run 으로 처리한다.
// 두 플레이어가 거의 동시에 사자 둘 다 샀다. 재고는 -1.
// 기대: 재고만큼만 팔리고, 나머지 구매는 실패한다. 재고는 0 아래로 내려가지 않는다.
// ラボ 52: 限定販売
// 伝説の剣をたった 1 本だけ売る。決済は時間がかかるので、購入リクエストごとに Task.Run で処理する。
// 2 人のプレイヤーがほぼ同時に買ったら、2 人とも買えてしまった。在庫は -1。
// 期待: 在庫の数だけ売れ、残りの購入は失敗する。在庫は 0 より下にならない。
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BugHunt.Labs.Lab52
{
    public class Shop
    {
        public int Stock;
        public readonly List<string> Buyers = new List<string>();
        public Action<string> Payment = _ => { };   // 결제 서버와 통신(오래 걸린다) / 決済サーバーと通信 (時間がかかる)

        public bool TryBuy(string buyer)
        {
            if (Stock > 0)
            {
                Payment(buyer);
                Stock--;
                Buyers.Add(buyer);
                return true;
            }
            return false;
        }

        public Task<bool> BuyAsync(string buyer)
        {
            return Task.Run(() => TryBuy(buyer));
        }
    }

    [Lab(52, "한정 판매", Symptom = "한 자루 남은 검을 두 명이 샀다. 재고 -1")]
    public class Lab52_LimitedStock : Lab
    {
        public override IEnumerator Run()
        {
            var shop = new Shop { Stock = Case(1, 2) };
            string[] buyers = Case(new[] { "Ari", "Bom" }, new[] { "Cha", "Dan", "Eun" });
            int stock = shop.Stock;

            // 결제 서버 흉내: 요청이 동시에 몰리면 같이 처리된다. 랩이 매번 같은 결과를 내도록,
            // 결제 중인 요청끼리 서로를 최대 1초 기다렸다가 함께 끝난다(동시에 들어온 상황을 확실하게 만든다).
            // 決済サーバーのまね: リクエストが同時に集中すると一緒に処理される。ラボが毎回同じ結果を出すように、
            // 決済中のリクエスト同士が互いを最大 1 秒待ってから一緒に終わる (同時に来た状況を確実に作る)。
            using var together = new Barrier(buyers.Length);
            shop.Payment = buyer => together.SignalAndWait(1000);

            var tasks = buyers.Select(b => shop.BuyAsync(b)).ToArray();
            var all = Task.WhenAll(tasks);
            // 시간이 아니라 요청이 모두 끝났는지로 기다린다(CPU 가 바쁘면 워커 스레드가 늦게 돈다). 끝내 안 끝나면 랩 제한 시간에 걸린다.
            // 時間ではなくリクエストがすべて終わったかどうかで待つ (CPU が忙しいとワーカースレッドの実行が遅れる)。最後まで終わらなければラボの制限時間に引っかかる。
            while (!all.IsCompleted)
            {
                yield return null;
            }

            Report.Expect("팔린 수", stock, tasks.Count(t => t.Result));
            Report.Expect("남은 재고", 0, shop.Stock);
            Report.Expect("구매자 명단 수", stock, shop.Buyers.Count);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// TryBuy 의 if 줄에 중단점을 걸고 디버그 → 창 → 스레드(Ctrl+Alt+H)를 여세요. 이 줄에 멈춘 스레드가 몇 개인가요? 각 스레드에서 Stock 은 몇인가요?
// 병렬 스택 창(디버그 → 창 → 병렬 스택)에서 두 작업이 같은 함수 안에 동시에 있는 것을 볼 수 있습니다.
// 중단점에 "스레드 ID" 필터를 걸거나 추적점으로 "{Environment.CurrentManagedThreadId} Stock={Stock}" 을 찍으면 끼어드는 순서가 보입니다.
//
// TryBuy の if の行にブレークポイントを置き、デバッグ → ウィンドウ → スレッド (Ctrl+Alt+H) を開いてください。この行で止まっているスレッドはいくつですか? 各スレッドで Stock はいくつですか?
// 並列スタックウィンドウ (デバッグ → ウィンドウ → 並列スタック) で、2 つのタスクが同じ関数の中に同時にいるのが見えます。
// ブレークポイントに「スレッド ID」のフィルターを付けるか、トレースポイントで "{Environment.CurrentManagedThreadId} Stock={Stock}" を出力すると、割り込む順序が見えます。
#endregion

#region 힌트 2 / ヒント 2
// "확인하고 → 행동" (check-then-act) 사이에 다른 스레드가 끼어들면, 둘 다 확인을 통과한 뒤 둘 다 행동합니다.
// 결제처럼 오래 걸리는 일이 확인과 행동 사이에 있으면 끼어들 틈이 커집니다. 개발 PC 에서는 거의 안 나고 서버 부하가 클 때만 나는 버그입니다.
// Stock-- 와 List.Add 자체도 원자적이지 않습니다(읽기, 계산, 쓰기 세 단계). List 는 동시에 Add 하면 원소가 사라지거나 예외가 납니다.
//
// 「確認して → 行動する」(check-then-act) の間に別のスレッドが割り込むと、両方が確認を通過した後、両方が行動します。
// 決済のように時間のかかる処理が確認と行動の間にあると、割り込む隙が大きくなります。開発 PC ではほとんど起きず、サーバー負荷が高いときだけ起きるバグです。
// Stock-- と List.Add 自体もアトミックではありません (読み取り、計算、書き込みの 3 段階)。List は同時に Add すると要素が消えたり例外が出たりします。
#endregion

#region 힌트 3 / ヒント 3
// 확인과 행동을 한 덩어리로 묶습니다.
//   readonly object gate = new object();
//   public bool TryBuy(string buyer) { lock (gate) { if (Stock <= 0) return false; Payment(buyer); Stock--; Buyers.Add(buyer); return true; } }
// 결제를 lock 밖으로 빼고 싶다면 재고를 먼저 "예약" 합니다: lock 안에서 Stock-- 하고, 결제가 실패하면 lock 안에서 되돌립니다.
// Unity 의 Job System 안전 시스템은 NativeContainer 를 두 잡이 동시에 쓰는 것은 잡아 주지만, 이런 관리 힙(C# 객체)의 레이스는 잡지 못합니다.
//
// 確認と行動を 1 つのかたまりにまとめます。
//   readonly object gate = new object();
//   public bool TryBuy(string buyer) { lock (gate) { if (Stock <= 0) return false; Payment(buyer); Stock--; Buyers.Add(buyer); return true; } }
// 決済を lock の外に出したいなら、在庫を先に「予約」します: lock の中で Stock-- し、決済が失敗したら lock の中で元に戻します。
// Unity の Job System の安全システムは、2 つのジョブが NativeContainer に同時に書き込むことは捕まえてくれますが、このようなマネージドヒープ (C# オブジェクト) のレースは捕まえられません。
#endregion
