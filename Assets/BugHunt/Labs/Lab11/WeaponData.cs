using UnityEngine;

namespace BugHunt.Labs.Lab11
{
    // 무기 데이터. 에셋 Resources/BugHunt/Rifle.asset
    // (ScriptableObject 도 파일 이름과 클래스 이름이 같아야 에셋으로 저장된다.)
    // 武器データ。アセット Resources/BugHunt/Rifle.asset
    // (ScriptableObject もファイル名とクラス名が同じでないとアセットとして保存されない。)
    [CreateAssetMenu(menuName = "BugHunt/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        public string DisplayName = "소총";
        public int MaxAmmo = 30;
        public int Ammo = 30;
        public int Damage = 12;
    }
}
