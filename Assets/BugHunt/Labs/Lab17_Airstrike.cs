// 랩 17: 공습
// 공습이 분대 전원에게 피해를 준다. 공습에 쓰러진 적이 하나라도 있으면 그 자리에서 예외가 나고, 뒤쪽 적은 피해를 받지 않는다.
// 기대: 분대 전원이 피해를 받고, 쓰러진 적만 분대 목록에서 빠진다.
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BugHunt.Labs.Lab17
{
    public class Enemy : MonoBehaviour
    {
        public int Health;
        public Squad Squad;

        public void TakeDamage(int amount)
        {
            Health -= amount;
            if (Health <= 0)
            {
                Die();
            }
        }

        void Die()
        {
            Squad.Members.Remove(this);   // 분대에서 빠진다
            Destroy(gameObject);
        }
    }

    public class Squad : MonoBehaviour
    {
        public List<Enemy> Members = new List<Enemy>();

        public void Airstrike(int damage)
        {
            foreach (Enemy enemy in Members)
            {
                enemy.TakeDamage(damage);
            }
        }
    }

    [Lab(17, "공습")]
    public class Lab17_Airstrike : Lab
    {
        public override IEnumerator Run()
        {
            var squad = Spawn<Squad>("Squad");
            int[] healths = Case(new[] { 50, 10, 80, 15, 60 }, new[] { 5, 70, 12, 90, 18, 40 });
            const int Damage = 20;
            foreach (int health in healths)
            {
                var enemy = Spawn<Enemy>($"Enemy_{squad.Members.Count}");
                enemy.Health = health;
                enemy.Squad = squad;
                squad.Members.Add(enemy);
            }
            List<int> expected = healths.Where(h => h > Damage).Select(h => h - Damage).ToList();
            yield return null;

            squad.Airstrike(Damage);
            yield return null;

            Report.Expect("살아남은 분대원 수", expected.Count, squad.Members.Count);
            Report.Expect("살아남은 분대원의 HP", string.Join(",", expected), string.Join(",", squad.Members.Select(e => e.Health)));
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// 예외 메시지 "Collection was modified; enumeration operation may not execute." 와 스택 트레이스를 보세요. 맨 위는 foreach 줄입니다.
// 그런데 foreach 안의 코드는 목록을 바꾸지 않습니다. 목록을 바꾼 것은 누구일까요? Members.Remove 줄에 중단점을 걸고 호출 스택을 보세요.
#endregion

#region 힌트 2
// List 의 foreach 는 시작할 때의 "버전" 을 기억하고, 매 반복마다 목록이 그대로인지 확인합니다. 도중에 Add/Remove 가 있었으면 다음 MoveNext 에서 예외.
// 이번에는 Squad 가 아니라 Enemy.Die 가(다른 스크립트가) 반복 도중에 목록을 고쳤습니다. 이벤트 처리기, OnDestroy, 콜백도 같은 일을 합니다.
// 참고: Destroy 는 프레임 끝에 일어나므로, 목록을 OnDestroy 에서 고쳤다면 이 예외 대신 "파괴된 적" 이 목록에 남는 문제(랩 21)가 됩니다.
#endregion

#region 힌트 3
// 반복하는 동안 바뀔 수 있는 목록은 복사본을 돌거나 뒤에서부터 인덱스로 돕니다.
//   foreach (Enemy enemy in Members.ToArray()) { enemy.TakeDamage(damage); }
//   for (int i = Members.Count - 1; i >= 0; --i) { Members[i].TakeDamage(damage); }   // 뒤에서부터: 지워도 앞쪽 인덱스는 그대로
// 또는 지울 것을 모아 두었다가 반복이 끝난 뒤 한꺼번에 지웁니다(RemoveAll).
#endregion
