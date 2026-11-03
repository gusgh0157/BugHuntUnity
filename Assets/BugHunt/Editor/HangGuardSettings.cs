using UnityEditor;

namespace BugHunt.Editor
{
    // 메뉴 BugHunt → 멈춤 랩: 오래 버티기(30초).
    // 평소 HangGuard 는 2초 만에 "응답 없음" 을 선언하고 빠져나온다. 디버거를 붙이고 "모두 중단" 을 직접 눌러 보려면 그보다 오래 굳어 있어야 한다.
    // 채점기는 이 설정과 상관없이 기본 예산(2초)을 쓴다.
    // メニュー BugHunt → フリーズ系ラボ: 30 秒間固まったままにする。
    // 普段 HangGuard は 2 秒で「応答なし」を宣言して抜け出します。デバッガーをアタッチして「すべて中断」を自分で押してみるには、それより長く固まっている必要があります。
    // 採点ツールはこの設定に関係なく既定の予算 (2 秒) を使います。
    [InitializeOnLoad]
    static class HangGuardSettings
    {
        const string MenuPath = MenuPaths.HangGuardLong;
        const string PrefKey = "BugHunt.HangGuard.Long";
        const float LongBudgetSeconds = 30f;

        static HangGuardSettings()
        {
            Apply(EditorPrefs.GetBool(PrefKey, false));
        }

        [MenuItem(MenuPath, false, 40)]
        static void Toggle()
        {
            bool on = !EditorPrefs.GetBool(PrefKey, false);
            EditorPrefs.SetBool(PrefKey, on);
            Apply(on);
        }

        [MenuItem(MenuPath, true)]
        static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, EditorPrefs.GetBool(PrefKey, false));
            return true;
        }

        static void Apply(bool on)
        {
            HangGuard.BudgetSeconds = on ? LongBudgetSeconds : HangGuard.DefaultBudgetSeconds;
        }
    }
}
