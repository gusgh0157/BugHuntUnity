using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BugHunt.Tests
{
    // 랩 하나 = 테스트 하나. 버그가 있는 상태에서는 실패하는 것이 정상이다.
    // 랩을 고친 뒤 Window → General → Test Runner → PlayMode → Run All 로 한꺼번에 확인할 수 있다.
    // ラボ 1 つ = テスト 1 つ。バグがある状態では失敗するのが正常です。
    // ラボを直したあと、ウィンドウ → 一般 → テストランナー → PlayMode → Run All でまとめて確認できます。
    public class LabTests
    {
        static IEnumerator Run(int number)
        {
            var go = new GameObject("LabRunner (Test)");
            var runner = go.AddComponent<LabRunner>();
            yield return runner.RunLab(number);
            LabResult result = runner.LastResult;
            Object.Destroy(go);
            yield return null;

            if (result.Skipped > 0 && result.Passed == 0 && result.Failed == 0)
            {
                Assert.Ignore(Loc.F("랩 {0}: 이 환경에서는 판정할 수 없어 SKIP (Console 의 [SKIP] 줄 참고)", "ラボ {0}: この環境では判定できないので SKIP (コンソールの [SKIP] の行を参照)", number));
            }
            Assert.IsFalse(result.Hung, Loc.F("랩 {0}: 응답 없음 (무한루프 또는 데드락)", "ラボ {0}: 応答なし (無限ループまたはデッドロック)", number));
            Assert.IsFalse(result.Crashed, Loc.F("랩 {0}: 예외로 중단됨", "ラボ {0}: 例外で中断", number));
            Assert.IsFalse(result.TimedOut, Loc.F("랩 {0}: 시간 초과", "ラボ {0}: タイムアウト", number));
            Assert.AreEqual(0, result.Failed, Loc.F("랩 {0}: FAIL {1}개", "ラボ {0}: FAIL {1} 個", number, result.Failed));
            Assert.Greater(result.Passed, 0, Loc.F("랩 {0}: PASS 가 하나도 없음", "ラボ {0}: PASS が 1 つもない", number));
        }

        [UnityTest] public IEnumerator Lab01_Tower() => Run(1);
        [UnityTest] public IEnumerator Lab02_Headshot() => Run(2);
        [UnityTest] public IEnumerator Lab03_Scoreboard() => Run(3);
        [UnityTest] public IEnumerator Lab04_Respawn() => Run(4);
        [UnityTest] public IEnumerator Lab05_CombatLog() => Run(5);
        [UnityTest] public IEnumerator Lab06_HealthBar() => Run(6);
        [UnityTest] public IEnumerator Lab07_Jump() => Run(7);
        [UnityTest] public IEnumerator Lab08_Archer() => Run(8);
        [UnityTest] public IEnumerator Lab09_Waypoints() => Run(9);
        [UnityTest] public IEnumerator Lab10_Balance() => Run(10);
        [UnityTest] public IEnumerator Lab11_WeaponData() => Run(11);
        [UnityTest] public IEnumerator Lab12_Hovercar() => Run(12);
        [UnityTest] public IEnumerator Lab13_FollowCamera() => Run(13);
        [UnityTest] public IEnumerator Lab14_Stamina() => Run(14);
        [UnityTest] public IEnumerator Lab15_PauseMenu() => Run(15);
        [UnityTest] public IEnumerator Lab16_CartRide() => Run(16);
        [UnityTest] public IEnumerator Lab17_SwordPickup() => Run(17);
        [UnityTest] public IEnumerator Lab18_Windmill() => Run(18);
        [UnityTest] public IEnumerator Lab19_CannonAim() => Run(19);
        [UnityTest] public IEnumerator Lab20_TurnAndShoot() => Run(20);
        [UnityTest] public IEnumerator Lab21_DroneSearchlight() => Run(21);
        [UnityTest] public IEnumerator Lab22_StretchedBridge() => Run(22);
        [UnityTest] public IEnumerator Lab23_GateAndStatue() => Run(23);
        [UnityTest] public IEnumerator Lab24_Hotbar() => Run(24);
        [UnityTest] public IEnumerator Lab25_Airstrike() => Run(25);
        [UnityTest] public IEnumerator Lab26_ShopPrices() => Run(26);
        [UnityTest] public IEnumerator Lab27_PatrolOrder() => Run(27);
        [UnityTest] public IEnumerator Lab28_SquadRoster() => Run(28);
        [UnityTest] public IEnumerator Lab29_ThreatTable() => Run(29);
        [UnityTest] public IEnumerator Lab30_TrailPoints() => Run(30);
        [UnityTest] public IEnumerator Lab31_PathCache() => Run(31);
        [UnityTest] public IEnumerator Lab32_Watchtower() => Run(32);
        [UnityTest] public IEnumerator Lab33_SpawnPlan() => Run(33);
        [UnityTest] public IEnumerator Lab34_SecondRound() => Run(34);
        [UnityTest] public IEnumerator Lab35_Railgun() => Run(35);
        [UnityTest] public IEnumerator Lab36_SpikeTrap() => Run(36);
        [UnityTest] public IEnumerator Lab37_TurretLayers() => Run(37);
        [UnityTest] public IEnumerator Lab38_MovingPlatform() => Run(38);
        [UnityTest] public IEnumerator Lab39_LevelReset() => Run(39);
        [UnityTest] public IEnumerator Lab40_BlasterSound() => Run(40);
        [UnityTest] public IEnumerator Lab41_BossLine() => Run(41);
        [UnityTest] public IEnumerator Lab42_Barrel() => Run(42);
        [UnityTest] public IEnumerator Lab43_Radio() => Run(43);
        [UnityTest] public IEnumerator Lab44_CrateHighlight() => Run(44);
        [UnityTest] public IEnumerator Lab45_CrateTint() => Run(45);
        [UnityTest] public IEnumerator Lab46_ItemIcons() => Run(46);
        [UnityTest] public IEnumerator Lab47_TeamColor() => Run(47);
        [UnityTest] public IEnumerator Lab48_AttackButton() => Run(48);
        [UnityTest] public IEnumerator Lab49_Footsteps() => Run(49);
        [UnityTest] public IEnumerator Lab50_SwordHit() => Run(50);
        [UnityTest] public IEnumerator Lab51_WeaponAttach() => Run(51);
        [UnityTest] public IEnumerator Lab52_LimitedStock() => Run(52);
        [UnityTest] public IEnumerator Lab53_BackgroundPath() => Run(53);
        [UnityTest] public IEnumerator Lab54_SaveLoader() => Run(54);
        [UnityTest] public IEnumerator Lab55_ShopGold() => Run(55);
        [UnityTest] public IEnumerator Lab56_Elevator() => Run(56);
        [UnityTest] public IEnumerator Lab57_Pathfinding() => Run(57);
        [UnityTest] public IEnumerator Lab58_ReplayFastForward() => Run(58);
        [UnityTest] public IEnumerator Lab59_HudGarbage() => Run(59);
        [UnityTest] public IEnumerator Lab60_TurretSearch() => Run(60);
        [UnityTest] public IEnumerator Lab61_BulletPool() => Run(61);
        [UnityTest] public IEnumerator Lab62_FlameThrower() => Run(62);
        [UnityTest] public IEnumerator Lab63_HealingAura() => Run(63);
    }
}
