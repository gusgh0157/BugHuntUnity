using UnityEngine;

namespace BugHunt.Labs.Lab10
{
    // 슬라임 능력치. 프리팹 Resources/BugHunt/Slime.prefab 에 붙어 있다.
    // (MonoBehaviour 를 프리팹/씬에 붙이려면 파일 이름과 클래스 이름이 같아야 해서 따로 뺐다.)
    // スライムの能力値。プレハブ Resources/BugHunt/Slime.prefab に付いている。
    // (MonoBehaviour をプレハブ/シーンに付けるにはファイル名とクラス名が同じでなければならないので、別ファイルに分けた。)
    public class SlimeStats : MonoBehaviour
    {
        // 1.1 밸런스 패치: "슬라임이 너무 약하다" 피드백으로 100 → 150
        // 1.1 バランス調整パッチ: 「スライムが弱すぎる」というフィードバックで 100 → 150
        public int MaxHealth = 150;
        public float MoveSpeed = 2.5f;
    }
}
