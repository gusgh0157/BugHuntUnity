// 랩 10: 밸런스 패치
// 코드에서 슬라임 HP 를 150 으로 올렸는데 게임에 나오는 슬라임은 여전히 100 이다.
// 기대: 프리팹으로 만든 슬라임의 MaxHealth 150.
// 이 랩의 게임 코드는 Lab10/SlimeStats.cs 에 있다.
// ラボ 10: バランス調整パッチ
// コードでスライムの HP を 150 に上げたのに、ゲームに出てくるスライムは相変わらず 100 だ。
// 期待: プレハブから作ったスライムの MaxHealth 150。
// このラボのゲームコードは Lab10/SlimeStats.cs にある。
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab10
{
    [Lab(10, "밸런스 패치", Symptom = "코드에서 HP 를 150 으로 올렸는데 게임에선 100")]
    public class Lab10_Balance : Lab
    {
        public override IEnumerator Run()
        {
            var prefab = Resources.Load<GameObject>("BugHunt/Slime");
            SlimeStats fromPrefab = Instantiate(prefab, transform).GetComponent<SlimeStats>();
            SlimeStats fromCode = Spawn<SlimeStats>("Slime (AddComponent)");
            yield return null;

            Report.Note(Loc.F("AddComponent 로 만든 슬라임: MaxHealth {0}, MoveSpeed {1}", fromCode.MaxHealth, fromCode.MoveSpeed));
            Report.Note(Loc.F("프리팹으로 만든 슬라임:     MaxHealth {0}, MoveSpeed {1}", fromPrefab.MaxHealth, fromPrefab.MoveSpeed));
            Report.Expect("프리팹 슬라임 MaxHealth", 150, fromPrefab.MaxHealth);
            Report.ExpectNear("프리팹 슬라임 MoveSpeed", 2.5f, fromPrefab.MoveSpeed, 0.001f);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// 두 슬라임의 값이 다릅니다. 같은 클래스, 같은 필드 초기값(= 150)인데 왜일까요?
// SlimeStats 에 void Awake() { } 를 추가하고 중단점을 걸어 보세요. Awake 에 들어온 순간 MaxHealth 는 이미 몇인가요?
//
// 2 体のスライムの値が違います。同じクラス、同じフィールド初期値 (= 150) なのに、なぜでしょうか?
// SlimeStats に void Awake() { } を追加してブレークポイントを置いてみてください。Awake に入った瞬間、MaxHealth はすでにいくつですか?
#endregion

#region 힌트 2 / ヒント 2
// Project 창에서 Assets/BugHunt/Resources/BugHunt/Slime.prefab 을 고르고 Inspector 를 보세요.
// 또는 그 파일을 메모장으로 열어 보세요(이 프로젝트는 에셋을 텍스트로 저장합니다). MaxHealth: 다음에 무슨 숫자가 적혀 있나요?
// Unity 는 오브젝트를 만들 때 ① C# 필드 초기값으로 채운 뒤 ② 프리팹/씬에 "저장된 값"으로 덮어쓰고 ③ Awake 를 부릅니다.
//
// プロジェクト (Project) ウィンドウで Assets/BugHunt/Resources/BugHunt/Slime.prefab を選び、インスペクター (Inspector) を見てください。
// またはそのファイルをメモ帳で開いてみてください (このプロジェクトはアセットをテキストで保存します)。MaxHealth: の次に何の数字が書かれていますか?
// Unity はオブジェクトを作るとき、① C# のフィールド初期値で埋めた後、② プレハブ/シーンに「保存された値」で上書きし、③ Awake を呼びます。
#endregion

#region 힌트 3 / ヒント 3
// 프리팹이 처음 만들어질 때(패치 전) 100 이 저장됐고, 그 뒤 코드의 초기값을 바꿔도 이미 저장된 값은 그대로입니다.
// 프리팹을 열어 MaxHealth 를 150 으로 고치세요(또는 컴포넌트 ⋮ 메뉴의 Reset 으로 코드 초기값을 다시 가져오기. 단, 다른 필드도 전부 초기화됩니다).
// 밸런스 값의 "주인"을 한 곳으로 정하세요: 코드냐, 프리팹이냐, ScriptableObject 데이터냐.
//
// プレハブが最初に作られたとき (パッチ前) に 100 が保存され、その後コードの初期値を変えても、すでに保存された値はそのままです。
// プレハブを開いて MaxHealth を 150 に直してください (またはコンポーネントの ⋮ メニューの「リセット (Reset)」でコードの初期値を取り込み直す。ただし、ほかのフィールドもすべて初期化されます)。
// バランス値の「持ち主」を 1 か所に決めてください: コードか、プレハブか、ScriptableObject のデータか。
#endregion
