// 랩 34: 상점 가격표
// 상점 가격표와 진열대를 코드로 채운 상점을 복제(Instantiate)하거나 JsonUtility 로 저장했다 불러오면 가격표와 진열대가 텅 빈다.
// 예외도 경고도 없다. 상점 이름과 금고 돈은 멀쩡히 복사된다.
// 기대: 복제본과 불러온 상점에도 같은 가격과 같은 진열대가 있다.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BugHunt.Labs.Lab34
{
    public class Shop : MonoBehaviour
    {
        public string ShopName = "상점";
        public int Gold;
        public Dictionary<string, int> Prices = new Dictionary<string, int>();
        public List<List<string>> Shelves = new List<List<string>>();   // 진열대마다 물건 목록

        public void SetPrice(string item, int price)
        {
            Prices[item] = price;
        }

        public int PriceOf(string item)
        {
            return Prices.TryGetValue(item, out int price) ? price : -1;
        }

        public void AddShelf(params string[] items)
        {
            Shelves.Add(new List<string>(items));
        }

        public int ShelfCount => Shelves.Count;

        public string ItemAt(int shelf, int slot)
        {
            return Shelves[shelf][slot];
        }
    }

    [Lab(34, "상점 가격표")]
    public class Lab34_ShopPrices : Lab
    {
        public override IEnumerator Run()
        {
            var shop = Spawn<Shop>("Shop");
            shop.ShopName = Case("대장간", "약초상");
            shop.Gold = Case(500, 1200);
            string item = Case("potion", "herb");
            int price = Case(50, 35);
            shop.SetPrice(item, price);
            shop.SetPrice("sword", 300);
            shop.AddShelf("sword", "shield");
            shop.AddShelf(item, "bomb", "arrow");
            yield return null;

            // 복제: 프리팹 인스턴스, Ctrl+D, Undo 와 같은 Unity 직렬화를 거친다.
            Shop clone = Instantiate(shop, transform);
            // 저장했다 불러오기
            string json = JsonUtility.ToJson(shop);
            Report.Note($"저장된 JSON: {json}");
            var loaded = Spawn<Shop>("LoadedShop");
            JsonUtility.FromJsonOverwrite(json, loaded);
            yield return null;

            foreach (var (label, copy) in new[] { ("복제본", clone), ("불러온 상점", loaded) })
            {
                Report.Expect($"{label}: 상점 이름", shop.ShopName, copy.ShopName);
                Report.Expect($"{label}: {item} 가격", price, copy.PriceOf(item));
                Report.Expect($"{label}: 진열대 수", 2, copy.ShelfCount);
                Report.Expect($"{label}: 두 번째 진열대의 첫 물건", item, copy.ShelfCount == 2 ? copy.ItemAt(1, 0) : "없음");
            }
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// 랩 출력의 "저장된 JSON" 을 보세요. ShopName 과 Gold 는 있는데 Prices 와 Shelves 는 어디 있나요?
// 플레이 중 Hierarchy 에서 Shop 을 골라 Inspector 를 보세요. Prices 와 Shelves 칸이 보이나요? 인스펙터에 안 보이는 필드는 대개 저장도 안 됩니다.
#endregion

#region 힌트 2
// Instantiate, 프리팹, 씬 저장, Undo, JsonUtility 는 모두 같은 Unity 직렬화 규칙을 씁니다. 규칙에 맞지 않는 필드는 조용히 건너뜁니다(경고 없음).
// 직렬화되지 않는 것: Dictionary, 다차원 배열 int[,], 가변 배열 int[][], 중첩 List<List<T>>, 프로퍼티, static, readonly, [Serializable] 이 없는 클래스.
// 직렬화되는 것: public(또는 [SerializeField]) 필드 중 기본형, string, Unity 오브젝트 참조, T[] / List<T>(T 가 직렬화 가능할 때), [Serializable] 클래스/구조체.
#endregion

#region 힌트 3
// 진열대: 안쪽 List 를 [Serializable] 클래스로 한 번 감쌉니다.
//   [System.Serializable] public class Shelf { public List<string> Items = new List<string>(); }
//   public List<Shelf> Shelves = new List<Shelf>();     // AddShelf / ItemAt 도 Shelves[shelf].Items[slot] 로
// 가격표: ISerializationCallbackReceiver 로 저장 직전에 키·값 목록으로 펼치고, 불러온 직후 Dictionary 를 다시 만듭니다.
//   [SerializeField] List<string> priceKeys = new List<string>(); [SerializeField] List<int> priceValues = new List<int>();
//   public void OnBeforeSerialize() { priceKeys.Clear(); priceValues.Clear(); foreach (var kv in Prices) { priceKeys.Add(kv.Key); priceValues.Add(kv.Value); } }
//   public void OnAfterDeserialize() { Prices = new Dictionary<string, int>(); for (int i = 0; i < priceKeys.Count; ++i) Prices[priceKeys[i]] = priceValues[i]; }
#endregion
