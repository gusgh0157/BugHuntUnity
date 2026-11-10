// 랩 46: 인벤토리 아이콘
// 인벤토리를 빠르게 스크롤하면 칸에 적힌 이름은 "물약" 인데 아이콘은 "검" 이다.
// 기대: 칸의 아이템과 칸에 그려진 아이콘이 일치한다.
// ラボ 46: インベントリのアイコン
// インベントリを素早くスクロールすると、スロットに書かれた名前は「ポーション」なのにアイコンは「剣」になる。
// 期待: スロットのアイテムとスロットに描かれたアイコンが一致する。
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BugHunt.Labs.Lab46
{
    // 아이콘 텍스처를 비동기로 읽고, 한 번 읽은 것은 들고 있는다.
    // アイコンのテクスチャを非同期で読み込み、一度読み込んだものは保持しておく。
    public class IconLoader
    {
        readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        public void Request(string itemId, Action<Texture2D> onLoaded)
        {
            if (cache.TryGetValue(itemId, out Texture2D cached))
            {
                onLoaded(cached);
                return;
            }
            ResourceRequest request = Resources.LoadAsync<Texture2D>($"BugHunt/Icons/icon_{itemId}");
            request.completed += _ =>
            {
                var texture = (Texture2D)request.asset;
                cache[itemId] = texture;
                onLoaded(texture);
            };
        }
    }

    // 스크롤 목록의 한 칸. 화면 밖으로 나간 칸은 버리지 않고 새 아이템을 보여 주는 데 다시 쓴다.
    // スクロールリストの 1 スロット。画面外に出たスロットは捨てずに、新しいアイテムを表示するのに再利用する。
    public class InventorySlot : MonoBehaviour
    {
        public IconLoader Loader;
        public string ItemId;
        public Texture2D Icon;

        public void Show(string itemId)
        {
            ItemId = itemId;
            Icon = null;   // 로딩 중에는 빈칸 / ロード中は空欄
            Loader.Request(itemId, texture => Icon = texture);
        }
    }

    [Lab(46, "인벤토리 아이콘", "インベントリのアイコン", Symptom = "스크롤하면 이름은 물약인데 아이콘은 검", SymptomJa = "スクロールすると、名前はポーションなのにアイコンは剣")]
    public class Lab46_ItemIcons : Lab
    {
        public override IEnumerator Run()
        {
            var loader = new IconLoader();

            // 물약 아이콘은 상점 화면에서 이미 한 번 읽었다.
            // ポーションのアイコンはショップ画面ですでに一度読み込んである。
            string cached = Case("potion", "shield");
            string slow = Case("sword", "potion");
            bool cachedReady = false;
            loader.Request(cached, _ => cachedReady = true);
            while (!cachedReady)
            {
                yield return null;
            }

            var slot = Spawn<InventorySlot>("Slot_0");
            slot.Loader = loader;
            slot.Show(slow);
            Report.Note(Loc.F("프레임 {0}: 칸에 {1} 표시 요청", "フレーム {0}: スロットに {1} の表示を要求", Time.frameCount, slow));
            slot.Show(cached);   // 같은 프레임에 스크롤되어 칸이 물약으로 재사용됨 / 同じフレームでスクロールされ、スロットがポーションに再利用された
            Report.Note(Loc.F("프레임 {0}: 칸에 {1} 표시 요청 (아이콘 {2})", "フレーム {0}: スロットに {1} の表示を要求 (アイコン {2})", Time.frameCount, cached, slot.Icon ? slot.Icon.name : Loc.L("없음", "なし")));

            // 늦게 오는 아이콘이 다 읽힐 때까지 기다린다(그래픽 장치가 있으면 텍스처 업로드로 몇 프레임 더 걸린다).
            // 遅れて届くアイコンが読み込み終わるまで待つ (グラフィックスデバイスがあると、テクスチャのアップロードで数フレーム余計にかかる)。
            ResourceRequest slowLoad = Resources.LoadAsync<Texture2D>($"BugHunt/Icons/icon_{slow}");
            while (!slowLoad.isDone)
            {
                yield return null;
            }
            for (int i = 0; i < 3; ++i)
            {
                yield return null;   // 같은 에셋의 앞선 요청이 콜백을 부를 때까지 / 同じアセットへの先の要求がコールバックを呼ぶまで
            }

            string iconName = slot.Icon ? slot.Icon.name : Loc.L("없음", "なし");
            Report.Expect(Loc.L("칸의 아이템", "スロットのアイテム"), cached, slot.ItemId);
            Report.Expect(Loc.L("칸에 그려진 아이콘", "スロットに描かれたアイコン"), $"icon_{slot.ItemId}", iconName);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// InventorySlot.Show 안의 람다 texture => Icon = texture 에 중단점을 거세요(줄 가운데 람다 부분에 커서를 두고 F9).
// 멈출 때마다 조사식에 UnityEngine.Time.frameCount, texture.name, ItemId 를 적으세요. 몇 번, 어떤 순서로 불리나요?
//
// InventorySlot.Show の中のラムダ texture => Icon = texture にブレークポイントを置いてください (行の途中のラムダ部分にカーソルを置いて F9)。
// 止まるたびに、ウォッチ式で UnityEngine.Time.frameCount、texture.name、ItemId を書き留めてください。何回、どの順序で呼ばれますか?
#endregion

#region 힌트 2 / ヒント 2
// 요청은 검 → 물약 순서였는데 도착은 물약 → 검 순서입니다. 물약은 캐시에 있어서 그 자리에서, 검은 다음 프레임 이후에 왔습니다.
// 비동기 콜백은 "요청한 때의 상황"이 아니라 "도착한 때의 상황"에서 실행됩니다. 그 사이 칸이 다른 아이템을 보여 주게 됐다면?
// (ResourceRequest.completed 는 이미 로드된 에셋이어도 같은 프레임이 아니라 다음 프레임 이후에 불립니다.)
//
// 要求は 剣 → ポーション の順でしたが、到着は ポーション → 剣 の順です。ポーションはキャッシュにあったのでその場で、剣は次のフレーム以降に届きました。
// 非同期コールバックは「要求したときの状況」ではなく「届いたときの状況」で実行されます。その間にスロットが別のアイテムを表示するようになっていたら?
// (ResourceRequest.completed は、すでにロード済みのアセットでも同じフレームではなく、次のフレーム以降に呼ばれます。)
#endregion

#region 힌트 3 / ヒント 3
// 도착한 결과가 아직 유효한지 확인하세요. 요청할 때의 itemId 를 람다가 기억하고 있으니:
//   Loader.Request(itemId, texture => { if (ItemId == itemId) Icon = texture; });
// 요청 번호(세대 카운터)를 두는 방법, 칸이 재사용될 때 이전 요청을 취소하는 방법도 있습니다. Addressables 의 핸들도 같은 문제가 있습니다.
//
// 届いた結果がまだ有効かどうかを確認してください。要求したときの itemId をラムダが覚えているので:
//   Loader.Request(itemId, texture => { if (ItemId == itemId) Icon = texture; });
// 要求番号 (世代カウンター) を持たせる方法や、スロットが再利用されるときに前の要求をキャンセルする方法もあります。Addressables のハンドルにも同じ問題があります。
#endregion
