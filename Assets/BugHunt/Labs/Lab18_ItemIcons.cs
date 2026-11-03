// 랩 18: 인벤토리 아이콘
// 인벤토리를 빠르게 스크롤하면 칸에 적힌 이름은 "물약" 인데 아이콘은 "검" 이다.
// 기대: 칸의 아이템과 칸에 그려진 아이콘이 일치한다.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BugHunt.Labs.Lab18
{
    // 아이콘 텍스처를 비동기로 읽고, 한 번 읽은 것은 들고 있는다.
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
    public class InventorySlot : MonoBehaviour
    {
        public IconLoader Loader;
        public string ItemId;
        public Texture2D Icon;

        public void Show(string itemId)
        {
            ItemId = itemId;
            Icon = null;   // 로딩 중에는 빈칸
            Loader.Request(itemId, texture => Icon = texture);
        }
    }

    [Lab(18, "인벤토리 아이콘")]
    public class Lab18_ItemIcons : Lab
    {
        public override IEnumerator Run()
        {
            var loader = new IconLoader();

            // 물약 아이콘은 상점 화면에서 이미 한 번 읽었다.
            bool potionReady = false;
            loader.Request("potion", _ => potionReady = true);
            while (!potionReady)
            {
                yield return null;
            }

            var slot = Spawn<InventorySlot>("Slot_0");
            slot.Loader = loader;
            slot.Show("sword");
            Report.Note($"프레임 {Time.frameCount}: 칸에 검 표시 요청");
            slot.Show("potion");   // 같은 프레임에 스크롤되어 칸이 물약으로 재사용됨
            Report.Note($"프레임 {Time.frameCount}: 칸에 물약 표시 요청 (아이콘 {(slot.Icon ? slot.Icon.name : "없음")})");

            for (int i = 0; i < 10; ++i)
            {
                yield return null;
            }

            string iconName = slot.Icon ? slot.Icon.name : "없음";
            Report.Expect("칸의 아이템", "potion", slot.ItemId);
            Report.Expect("칸에 그려진 아이콘", $"icon_{slot.ItemId}", iconName);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// InventorySlot.Show 안의 람다 texture => Icon = texture 에 중단점을 거세요(줄 가운데 람다 부분에 커서를 두고 F9).
// 멈출 때마다 조사식에 UnityEngine.Time.frameCount, texture.name, ItemId 를 적으세요. 몇 번, 어떤 순서로 불리나요?
#endregion

#region 힌트 2
// 요청은 검 → 물약 순서였는데 도착은 물약 → 검 순서입니다. 물약은 캐시에 있어서 그 자리에서, 검은 다음 프레임 이후에 왔습니다.
// 비동기 콜백은 "요청한 때의 상황"이 아니라 "도착한 때의 상황"에서 실행됩니다. 그 사이 칸이 다른 아이템을 보여 주게 됐다면?
// (ResourceRequest.completed 는 이미 로드된 에셋이어도 같은 프레임이 아니라 다음 프레임 이후에 불립니다.)
#endregion

#region 힌트 3
// 도착한 결과가 아직 유효한지 확인하세요. 요청할 때의 itemId 를 람다가 기억하고 있으니:
//   Loader.Request(itemId, texture => { if (ItemId == itemId) Icon = texture; });
// 요청 번호(세대 카운터)를 두는 방법, 칸이 재사용될 때 이전 요청을 취소하는 방법도 있습니다. Addressables 의 핸들도 같은 문제가 있습니다.
#endregion
