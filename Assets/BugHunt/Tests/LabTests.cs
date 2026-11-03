using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BugHunt.Tests
{
    // 랩 하나 = 테스트 하나. 버그가 있는 상태에서는 실패하는 것이 정상이다.
    // 랩을 고친 뒤 Window → General → Test Runner → PlayMode → Run All 로 한꺼번에 확인할 수 있다.
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
                Assert.Ignore($"랩 {number}: 이 환경에서는 판정할 수 없어 SKIP (Console 의 [SKIP] 줄 참고)");
            }
            Assert.IsFalse(result.Hung, $"랩 {number}: 응답 없음 (무한루프 또는 데드락)");
            Assert.IsFalse(result.Crashed, $"랩 {number}: 예외로 중단됨");
            Assert.IsFalse(result.TimedOut, $"랩 {number}: 시간 초과");
            Assert.AreEqual(0, result.Failed, $"랩 {number}: FAIL {result.Failed}개");
            Assert.Greater(result.Passed, 0, $"랩 {number}: PASS 가 하나도 없음");
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
        [UnityTest] public IEnumerator Lab12_BlasterSound() => Run(12);
        [UnityTest] public IEnumerator Lab13_BossLine() => Run(13);
        [UnityTest] public IEnumerator Lab14_Barrel() => Run(14);
        [UnityTest] public IEnumerator Lab15_Radio() => Run(15);
        [UnityTest] public IEnumerator Lab16_CrateHighlight() => Run(16);
        [UnityTest] public IEnumerator Lab17_CrateTint() => Run(17);
        [UnityTest] public IEnumerator Lab18_ItemIcons() => Run(18);
        [UnityTest] public IEnumerator Lab19_TeamColor() => Run(19);
        [UnityTest] public IEnumerator Lab20_AttackButton() => Run(20);
        [UnityTest] public IEnumerator Lab21_Footsteps() => Run(21);
        [UnityTest] public IEnumerator Lab22_SwordHit() => Run(22);
        [UnityTest] public IEnumerator Lab23_WeaponAttach() => Run(23);
        [UnityTest] public IEnumerator Lab24_PathCache() => Run(24);
        [UnityTest] public IEnumerator Lab25_Watchtower() => Run(25);
        [UnityTest] public IEnumerator Lab26_SpawnPlan() => Run(26);
        [UnityTest] public IEnumerator Lab27_SecondRound() => Run(27);
        [UnityTest] public IEnumerator Lab28_Elevator() => Run(28);
        [UnityTest] public IEnumerator Lab29_Pathfinding() => Run(29);
        [UnityTest] public IEnumerator Lab30_ShopGold() => Run(30);
        [UnityTest] public IEnumerator Lab31_ReplayFastForward() => Run(31);
        [UnityTest] public IEnumerator Lab32_Hotbar() => Run(32);
        [UnityTest] public IEnumerator Lab33_Airstrike() => Run(33);
        [UnityTest] public IEnumerator Lab34_ShopPrices() => Run(34);
        [UnityTest] public IEnumerator Lab35_PatrolOrder() => Run(35);
        [UnityTest] public IEnumerator Lab36_SquadRoster() => Run(36);
        [UnityTest] public IEnumerator Lab37_ThreatTable() => Run(37);
        [UnityTest] public IEnumerator Lab38_TrailPoints() => Run(38);
    }
}
