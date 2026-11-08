// 랩 24: 단축키 칸
// 단축키 1~4 에 무기를 넣는 단축 바. 디자이너가 인스펙터에서 칸 배열을 4칸에서 3칸으로 줄이자, 단축 바를 만드는 순간 예외가 난다.
// 빈 단축키를 눌렀을 때도 예외가 난다.
// 기대: 배열에 있는 칸만큼 단축 바가 만들어지고, 빈 단축키를 누르면 아무것도 고르지 않는다(null).
// ラボ 24: ショートカットスロット
// ショートカットキー 1〜4 に武器を入れるホットバー。デザイナーがインスペクターでスロットの配列を 4 つから 3 つに減らすと、ホットバーを作った瞬間に例外が出る。
// 空のショートカットキーを押したときも例外が出る。
// 期待: 配列にあるスロットの数だけホットバーが作られ、空のショートカットキーを押すと何も選ばない (null)。
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BugHunt.Labs.Lab24
{
    public class Hotbar : MonoBehaviour
    {
        public const int SlotCount = 4;   // 단축키 1~4 / ショートカットキー 1〜4

        public string[] Slots = { "검", "활", "물약", "폭탄" };   // 인스펙터에서 채운다 / インスペクターで埋める
        public List<string> Labels = new List<string>();

        public void Build()
        {
            Labels.Clear();
            for (int i = 0; i < SlotCount; ++i)
            {
                Labels.Add($"{i + 1}: {Slots[i]}");
            }
        }

        // key = 1 ~ 4
        public string Select(int key)
        {
            return Labels[key - 1];
        }
    }

    [Lab(24, "단축키 칸", Symptom = "디자이너가 칸 배열을 3칸으로 줄이자 IndexOutOfRangeException, 빈 단축키에서 ArgumentOutOfRangeException")]
    public class Lab24_Hotbar : Lab
    {
        public override IEnumerator Run()
        {
            var hotbar = Spawn<Hotbar>("Hotbar");
            // 디자이너가 인스펙터에서 칸 수를 줄였다(배열 크기 Size 를 바꾸면 뒤 칸이 사라진다).
            // デザイナーがインスペクターでスロット数を減らした (配列のサイズ (Size) を変えると後ろのスロットが消える)。
            hotbar.Slots = Case(new[] { Loc.T("검"), Loc.T("활"), Loc.T("물약") }, new[] { Loc.T("창"), Loc.T("방패") });
            int filled = hotbar.Slots.Length;
            yield return null;

            hotbar.Build();
            Report.Expect("단축 바 칸 수", filled, hotbar.Labels.Count);
            Report.Expect(Loc.F("단축키 {0}", filled), $"{filled}: {hotbar.Slots[filled - 1]}", hotbar.Select(filled));
            Report.Expect(Loc.F("빈 단축키 {0}", filled + 1), null, hotbar.Select(filled + 1));
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// 예외가 두 종류입니다. 배열 Slots[i] 는 IndexOutOfRangeException, List 의 Labels[i] 는 ArgumentOutOfRangeException 을 던집니다.
// 예외가 난 줄에서 조사식에 i(또는 key - 1)와 Slots.Length(또는 Labels.Count)를 나란히 넣어 보세요.
// 디버거 예외 설정에서 System.IndexOutOfRangeException 을 "throw 될 때 중단" 으로 켜 두면 잡히는 곳이 아니라 던지는 곳에서 멈춥니다.
//
// 例外は 2 種類です。配列の Slots[i] は IndexOutOfRangeException、List の Labels[i] は ArgumentOutOfRangeException を投げます。
// 例外が出た行で、ウォッチ式に i (または key - 1) と Slots.Length (または Labels.Count) を並べて入れてみてください。
// デバッガーの例外設定で System.IndexOutOfRangeException を「スローされたときに中断」にしておくと、キャッチされる場所ではなく投げる場所で止まります。
#endregion

#region 힌트 2 / ヒント 2
// 반복 횟수를 정한 상수 SlotCount 와 실제 데이터의 크기 Slots.Length 가 따로 놉니다. 인스펙터에서 배열 크기는 언제든 바뀝니다.
// 바깥에서 들어오는 번호(단축키, 네트워크 메시지, 세이브 파일)는 범위 안이라고 믿을 수 없습니다.
//
// ループ回数を決めた定数 SlotCount と、実際のデータのサイズ Slots.Length がばらばらです。インスペクターで配列のサイズはいつでも変わります。
// 外から入ってくる番号 (ショートカットキー、ネットワークメッセージ、セーブファイル) は、範囲内だと信じることはできません。
#endregion

#region 힌트 3 / ヒント 3
// 반복은 데이터 크기로:  for (int i = 0; i < Slots.Length; ++i)
// 바깥 번호는 확인 후 사용:  int index = key - 1;  return index >= 0 && index < Labels.Count ? Labels[index] : null;
// "칸은 항상 4개" 가 규칙이라면 OnValidate 에서 배열 크기를 강제하는 방법도 있습니다(인스펙터에서 바꾸는 순간 고친다).
//
// ループはデータのサイズで:  for (int i = 0; i < Slots.Length; ++i)
// 外からの番号は確認してから使う:  int index = key - 1;  return index >= 0 && index < Labels.Count ? Labels[index] : null;
// 「スロットは常に 4 つ」がルールなら、OnValidate で配列のサイズを強制する方法もあります (インスペクターで変えた瞬間に直す)。
#endregion
