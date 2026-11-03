// 랩 1: 감시탑
// 감시탑이 가장 가까운 적부터 쏜다. 도중에 폭탄이 적 하나를 날려 버리면 게임이 예외를 뿜는다.
// 기대: 감시탑이 나머지 둘을 처치해서 적 3마리가 모두 사라진다.
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BugHunt.Labs.Lab01
{
    public class Enemy : MonoBehaviour
    {
        public int Health = 30;

        // 쓰러졌으면 true
        public bool TakeDamage(int amount)
        {
            Health -= amount;
            if (Health <= 0)
            {
                Destroy(gameObject);
                return true;
            }
            return false;
        }
    }

    public class Tower : MonoBehaviour
    {
        public List<Enemy> Enemies = new List<Enemy>();
        public Enemy Target;
        public int Kills;

        const float FireInterval = 0.1f;
        float cooldown;

        void Start()
        {
            Target = PickTarget();
        }

        void Update()
        {
            cooldown -= Time.deltaTime;
            if (cooldown > 0f)
            {
                return;
            }
            cooldown = FireInterval;

            // 표적 쪽으로 포신을 돌리고 쏜다.
            Vector3 aim = Target.transform.position - transform.position;
            transform.rotation = Quaternion.LookRotation(aim);
            if (Target.TakeDamage(10))
            {
                ++Kills;
                Target = PickTarget();
                if (Target == null)
                {
                    enabled = false;   // 남은 적이 없다
                }
            }
        }

        Enemy PickTarget()
        {
            return Enemies
                .Where(e => e != null && e.Health > 0)
                .OrderBy(e => Vector3.Distance(e.transform.position, transform.position))
                .FirstOrDefault();
        }
    }

    public class Bomb : MonoBehaviour
    {
        public void Explode(IEnumerable<Enemy> enemies, float radius)
        {
            foreach (Enemy enemy in enemies.ToList())
            {
                if (enemy != null && Vector3.Distance(enemy.transform.position, transform.position) <= radius)
                {
                    enemy.TakeDamage(100);
                }
            }
        }
    }

    [Lab(1, "감시탑")]
    public class Lab01_Tower : Lab
    {
        public override IEnumerator Run()
        {
            var tower = Spawn<Tower>("Tower", Vector3.zero);
            int enemyCount = Case(3, 4);
            for (int i = 0; i < enemyCount; ++i)
            {
                tower.Enemies.Add(Spawn<Enemy>($"Enemy_{i}", new Vector3(0, 0, 5 + i * 3)));
            }

            yield return new WaitForSeconds(0.15f);
            Report.Note("쾅! 폭탄이 가장 가까운 적 근처에서 터진다.");
            var bomb = Spawn<Bomb>("Bomb", new Vector3(0, 0, 5));
            bomb.Explode(tower.Enemies, 1.5f);

            yield return new WaitForSeconds(1.5f);
            int alive = tower.Enemies.Count(e => e != null);
            Report.Expect("남은 적", 0, alive);
            Report.Expect("감시탑이 처치한 적", enemyCount - 1, tower.Kills);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기 / Rider: Ctrl+Shift+NumPad -)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// Console 창의 빨간 줄(MissingReferenceException)을 한 번 클릭하면 아래에 스택 트레이스가 나옵니다.
// 맨 위의 내 코드 줄(파란 링크)을 더블클릭하면 그 줄로 갑니다. 어떤 변수의 무엇에 접근하다 죽었나요?
// 메시지를 끝까지 읽으세요. "has been destroyed but you are still trying to access it" 은 null 이 아니라 "파괴된 오브젝트" 라는 뜻입니다.
#endregion

#region 힌트 2
// 그 적은 누가 파괴했을까요? Enemy.TakeDamage 의 Destroy 줄에 중단점을 걸고 다시 실행하세요.
// 처음 멈췄을 때 호출 스택에 Tower 가 있나요, Bomb 이 있나요?
// 디버거를 붙인 상태라면 예외가 난 줄에서 VS/Rider 가 멈춥니다. 조사식에 Target == null 을 넣어 보세요. true 인데 Target 을 펼치면 필드가 보입니다.
#endregion

#region 힌트 3
// 감시탑은 "자기가 처치했을 때만" 다음 표적을 고릅니다. 남이(폭탄, 다른 플레이어, 시간 제한) 표적을 파괴하는 경우를 생각하지 않았습니다.
// Update 맨 앞에서 Target 이 아직 살아 있는지(Unity 의 == null) 확인하고, 아니면 다시 고르세요.
#endregion
