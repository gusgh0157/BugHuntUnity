// 랩 11: 무기 데이터
// 플레이어 1 이 3발 쏘면 플레이어 2 의 탄창도 줄어 있다. 플레이를 멈췄다 다시 해도 줄어 있다(에디터를 껐다 켜야 돌아온다).
// 기대: 각자 탄창 30발로 시작, 플레이어 1 만 27발. 무기 데이터 에셋은 그대로.
// 데이터 클래스는 Lab11/WeaponData.cs 에 있다.
// ラボ 11: 武器データ
// プレイヤー 1 が 3 発撃つと、プレイヤー 2 の弾倉も減っている。再生を止めてやり直しても減ったまま (エディターを再起動しないと戻らない)。
// 期待: それぞれ弾倉 30 発で開始し、プレイヤー 1 だけ 27 発。武器データのアセットはそのまま。
// データクラスは Lab11/WeaponData.cs にある。
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab11
{
    public class Weapon : MonoBehaviour
    {
        public WeaponData Data;

        public int AmmoLeft => Data.Ammo;

        public void Equip(WeaponData data)
        {
            Data = data;
        }

        public bool Fire()
        {
            if (Data.Ammo <= 0)
            {
                return false;
            }
            --Data.Ammo;
            return true;
        }
    }

    [Lab(11, "무기 데이터", "武器データ", Symptom = "남의 탄창이 줄고, 플레이를 멈춰도 줄어 있다", SymptomJa = "他人の弾倉が減り、再生を止めても減ったまま")]
    public class Lab11_WeaponData : Lab
    {
        public override IEnumerator Run()
        {
            var rifleData = Resources.Load<WeaponData>("BugHunt/Rifle");
            int assetAmmoBefore = rifleData.Ammo;
            Report.Note(Loc.F("랩 시작 시 에셋 Rifle.asset 의 Ammo: {0} (파일에 저장된 값은 30)", "ラボ開始時のアセット Rifle.asset の Ammo: {0} (ファイルに保存された値は 30)", assetAmmoBefore));

            var player1 = Spawn<Weapon>("Player1_Rifle");
            var player2 = Spawn<Weapon>("Player2_Rifle");
            player1.Equip(rifleData);
            player2.Equip(rifleData);
            yield return null;

            int shots = Case(3, 5);
            for (int i = 0; i < shots; ++i)
            {
                player1.Fire();
                yield return null;
            }

            Report.Expect(Loc.L("플레이어 1 탄약", "プレイヤー 1 の弾薬"), rifleData.MaxAmmo - shots, player1.AmmoLeft);
            Report.Expect(Loc.L("플레이어 2 탄약", "プレイヤー 2 の弾薬"), rifleData.MaxAmmo, player2.AmmoLeft);
            Report.Expect(Loc.L("이번 실행 동안 에셋의 Ammo 변화량", "今回の実行中のアセットの Ammo の変化量"), 0, rifleData.Ammo - assetAmmoBefore);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// 랩을 두 번 연달아 실행해 보세요. 첫 줄의 "에셋의 Ammo" 가 어떻게 바뀌나요? 플레이 모드를 껐다 켜고 다시 실행하면?
// 플레이 중에 Project 창에서 Assets/BugHunt/Resources/BugHunt/Rifle.asset 을 고르고 Inspector 를 띄워 둔 채 실행해 보세요.
//
// ラボを 2 回続けて実行してみてください。1 行目の「アセットの Ammo」はどう変わりますか? プレイモードを終了して入り直し、もう一度実行すると?
// プレイ中にプロジェクト (Project) ウィンドウで Assets/BugHunt/Resources/BugHunt/Rifle.asset を選び、インスペクター (Inspector) を表示したまま実行してみてください。
#endregion

#region 힌트 2 / ヒント 2
// Fire 의 --Data.Ammo 줄에 중단점. 조사식에 player1.Data == player2.Data, 그리고 ReferenceEquals(Data, rifleData) 를 넣어 보세요.
// 두 무기가 각자의 데이터를 갖고 있나요, 같은 객체 하나를 가리키고 있나요?
// ScriptableObject 에셋은 프로젝트 전체에 하나뿐인 객체입니다. 에디터에서는 플레이 모드가 끝나도 메모리의 그 객체가 그대로라 바뀐 값이 남고,
// 에셋이 저장되면(Ctrl+S, 다른 수정과 함께) 파일에까지 들어갑니다. git status 로 Rifle.asset 이 바뀌었는지 확인해 보세요.
//
// Fire の --Data.Ammo の行にブレークポイント。ウォッチ式に player1.Data == player2.Data、そして ReferenceEquals(Data, rifleData) を入れてみてください。
// 2 つの武器はそれぞれ自分のデータを持っていますか、それとも同じオブジェクト 1 つを指していますか?
// ScriptableObject のアセットはプロジェクト全体に 1 つしかないオブジェクトです。エディターではプレイモードが終わってもメモリ上のそのオブジェクトがそのままなので、変わった値が残り、
// アセットが保存されると (Ctrl+S、ほかの変更と一緒に) ファイルにまで書き込まれます。git status で Rifle.asset が変わったか確認してみてください。
#endregion

#region 힌트 3 / ヒント 3
// 에셋은 "변하지 않는 설정(최대 탄약, 피해량)"만 담고, 게임 중 바뀌는 값(현재 탄약)은 무기 컴포넌트가 각자 들고 있게 하세요.
//   int ammo;  Equip 에서 ammo = data.MaxAmmo;  Fire 에서 --ammo;  AmmoLeft => ammo;
// 꼭 에셋 전체를 복제해야 한다면 Instantiate(data) 로 런타임 사본을 만듭니다.
//
// アセットには「変わらない設定 (最大弾薬数、ダメージ量)」だけを入れ、ゲーム中に変わる値 (現在の弾薬) は武器コンポーネントがそれぞれ持つようにしてください。
//   int ammo;  Equip で ammo = data.MaxAmmo;  Fire で --ammo;  AmmoLeft => ammo;
// どうしてもアセット全体を複製する必要があるなら、Instantiate(data) でランタイムのコピーを作ります。
#endregion
