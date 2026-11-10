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
        public string DisplayName = "소총";   // ko-only: Rifle.asset 에 저장된 데이터. 랩 출력에는 나오지 않는다 / Rifle.asset に保存されたデータ (小銃)。ラボの出力には出ない
        public int MaxAmmo = 30;
        public int Ammo = 30;
        public int Damage = 12;
    }
}
