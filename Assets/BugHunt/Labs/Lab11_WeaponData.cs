// 랩 11: 무기 데이터
// 플레이어 1 이 3발 쏘면 플레이어 2 의 탄창도 줄어 있다. 플레이를 멈췄다 다시 해도 줄어 있다(에디터를 껐다 켜야 돌아온다).
// 기대: 각자 탄창 30발로 시작, 플레이어 1 만 27발. 무기 데이터 에셋은 그대로.
// 데이터 클래스는 Lab11/WeaponData.cs 에 있다.
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

    [Lab(11, "무기 데이터")]
    public class Lab11_WeaponData : Lab
    {
        public override IEnumerator Run()
        {
            var rifleData = Resources.Load<WeaponData>("BugHunt/Rifle");
            int assetAmmoBefore = rifleData.Ammo;
            Report.Note($"랩 시작 시 에셋 Rifle.asset 의 Ammo: {assetAmmoBefore} (파일에 저장된 값은 30)");

            var player1 = Spawn<Weapon>("Player1_Rifle");
            var player2 = Spawn<Weapon>("Player2_Rifle");
            player1.Equip(rifleData);
            player2.Equip(rifleData);
            yield return null;

            for (int i = 0; i < 3; ++i)
            {
                player1.Fire();
                yield return null;
            }

            Report.Expect("플레이어 1 탄약", rifleData.MaxAmmo - 3, player1.AmmoLeft);
            Report.Expect("플레이어 2 탄약", rifleData.MaxAmmo, player2.AmmoLeft);
            Report.Expect("이번 실행 동안 에셋의 Ammo 변화량", 0, rifleData.Ammo - assetAmmoBefore);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// 랩을 두 번 연달아 실행해 보세요. 첫 줄의 "에셋의 Ammo" 가 어떻게 바뀌나요? 플레이 모드를 껐다 켜고 다시 실행하면?
// 플레이 중에 Project 창에서 Assets/BugHunt/Resources/BugHunt/Rifle.asset 을 고르고 Inspector 를 띄워 둔 채 실행해 보세요.
#endregion

#region 힌트 2
// Fire 의 --Data.Ammo 줄에 중단점. 조사식에 player1.Data == player2.Data, 그리고 ReferenceEquals(Data, rifleData) 를 넣어 보세요.
// 두 무기가 각자의 데이터를 갖고 있나요, 같은 객체 하나를 가리키고 있나요?
// ScriptableObject 에셋은 프로젝트 전체에 하나뿐인 객체입니다. 에디터에서는 플레이 모드가 끝나도 메모리의 그 객체가 그대로라 바뀐 값이 남고,
// 에셋이 저장되면(Ctrl+S, 다른 수정과 함께) 파일에까지 들어갑니다. git status 로 Rifle.asset 이 바뀌었는지 확인해 보세요.
#endregion

#region 힌트 3
// 에셋은 "변하지 않는 설정(최대 탄약, 피해량)"만 담고, 게임 중 바뀌는 값(현재 탄약)은 무기 컴포넌트가 각자 들고 있게 하세요.
//   int ammo;  Equip 에서 ammo = data.MaxAmmo;  Fire 에서 --ammo;  AmmoLeft => ammo;
// 꼭 에셋 전체를 복제해야 한다면 Instantiate(data) 로 런타임 사본을 만듭니다.
#endregion
