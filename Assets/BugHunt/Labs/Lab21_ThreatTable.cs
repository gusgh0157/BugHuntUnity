// 랩 21: 위협 목록
// 보스는 자기를 때린 적마다 위협 점수를 쌓아 두고, 점수가 가장 높은 적을 공격한다.
// 위협 1위였던 적이 죽은 뒤 보스가 공격하려는 순간 MissingReferenceException 이 난다. 위협 목록의 수도 줄지 않는다.
// 기대: 죽은 적은 위협 목록에서 빠지고, 보스는 살아 있는 적 중 1위를 공격한다.
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BugHunt.Labs.Lab21
{
    public class Enemy : MonoBehaviour
    {
    }

    public class Boss : MonoBehaviour
    {
        public readonly Dictionary<Enemy, float> Threat = new Dictionary<Enemy, float>();
        public string LastTarget;

        public void OnHit(Enemy attacker, float damage)
        {
            Threat.TryGetValue(attacker, out float threat);
            Threat[attacker] = threat + damage;
        }

        public int ThreatCount => Threat.Count;

        public void AttackTop()
        {
            Enemy top = Threat.OrderByDescending(kv => kv.Value).First().Key;
            LastTarget = top.name;
            top.transform.LookAt(transform);   // 보스를 돌아보게 한다(공격 연출)
        }
    }

    [Lab(21, "위협 목록")]
    public class Lab21_ThreatTable : Lab
    {
        public override IEnumerator Run()
        {
            var boss = Spawn<Boss>("Boss");
            float[] damages = Case(new[] { 120f, 40f, 75f }, new[] { 30f, 200f, 90f, 60f });
            var enemies = new List<Enemy>();
            for (int i = 0; i < damages.Length; ++i)
            {
                var enemy = Spawn<Enemy>($"Enemy_{i}", new Vector3(i * 2f, 0f, 3f));
                enemies.Add(enemy);
                boss.OnHit(enemy, damages[i]);
            }
            yield return null;

            // 위협 1위가 쓰러진다.
            int topIndex = System.Array.IndexOf(damages, damages.Max());
            Destroy(enemies[topIndex].gameObject);
            yield return null;

            string expectedTarget = enemies[System.Array.IndexOf(damages, damages.Where((_, i) => i != topIndex).Max())].name;
            Report.Expect("위협 목록의 적 수", damages.Length - 1, boss.ThreatCount);
            boss.AttackTop();
            Report.Expect("보스가 공격한 적", expectedTarget, boss.LastTarget);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// AttackTop 의 top 줄에 중단점을 걸고 조사식에 top == null 과 (object)top == null 을 나란히 넣어 보세요. 하나는 true, 하나는 false 입니다.
// Threat 을 펼쳐 보면 키 중 하나가 "null" 로 보이는데 항목은 그대로 있습니다. 1부 랩 1(가짜 null)과 같은 상태입니다.
#endregion

#region 힌트 2
// Destroy 는 네이티브 쪽 오브젝트만 지웁니다. C# 쪽 Enemy 객체는 Dictionary 가 붙잡고 있으므로 그대로 남고, 키로서의 해시와 Equals 도 그대로입니다.
// 그래서 Dictionary 는 그 항목을 지우지 않고, 값(점수)도 남아 1위를 계속 차지합니다. Unity 오브젝트를 키나 원소로 담는 HashSet, List 도 같습니다.
// 이 항목은 아무도 지우지 않으므로 C# 객체가 GC 되지 않는 작은 누수이기도 합니다.
#endregion

#region 힌트 3
// 방법 1: 쓰기 전에 죽은 키를 치웁니다.
//   foreach (Enemy dead in Threat.Keys.Where(e => e == null).ToList()) { Threat.Remove(dead); }   // == null 은 Unity 의 재정의라 파괴된 것도 잡는다
// 방법 2: 적이 죽을 때 알립니다. Enemy.OnDestroy 에서 event 를 쏘고, Boss 가 받아 Threat.Remove(enemy).
// ThreatCount 도 죽은 적을 빼고 세어야 합니다. 치우는 곳을 한 곳(Prune 함수)으로 모아 두면 둘 다 해결됩니다.
#endregion
