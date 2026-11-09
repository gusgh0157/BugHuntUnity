// 랩 29: 위협 목록
// 보스는 자기를 때린 적마다 위협 점수를 쌓아 두고, 점수가 가장 높은 적을 공격한다.
// 위협 1위였던 적이 죽은 뒤 보스가 공격하려는 순간 MissingReferenceException 이 난다. 위협 목록의 수도 줄지 않는다.
// 기대: 죽은 적은 위협 목록에서 빠지고, 보스는 살아 있는 적 중 1위를 공격한다.
// ラボ 29: ヘイトリスト
// ボスは自分を攻撃した敵ごとにヘイト値をためておき、値が最も高い敵を攻撃する。
// ヘイト 1 位だった敵が死んだ後、ボスが攻撃しようとした瞬間に MissingReferenceException が出る。ヘイトリストの数も減らない。
// 期待: 死んだ敵はヘイトリストから外れ、ボスは生きている敵の中の 1 位を攻撃する。
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BugHunt.Labs.Lab29
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
            top.transform.LookAt(transform);   // 보스를 돌아보게 한다(공격 연출) / ボスの方を向かせる (攻撃の演出)
        }
    }

    [Lab(29, "위협 목록", "ヘイトリスト", Symptom = "위협 1위가 죽자 보스가 MissingReferenceException, 목록 수도 그대로", SymptomJa = "ヘイト 1 位が死ぬとボスが MissingReferenceException、リストの数もそのまま")]
    public class Lab29_ThreatTable : Lab
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
            // ヘイト 1 位が倒れる。
            int topIndex = System.Array.IndexOf(damages, damages.Max());
            Destroy(enemies[topIndex].gameObject);
            yield return null;

            string expectedTarget = enemies[System.Array.IndexOf(damages, damages.Where((_, i) => i != topIndex).Max())].name;
            Report.Expect(Loc.L("위협 목록의 적 수", "ヘイトリストの敵の数"), damages.Length - 1, boss.ThreatCount);
            boss.AttackTop();
            Report.Expect(Loc.L("보스가 공격한 적", "ボスが攻撃した敵"), expectedTarget, boss.LastTarget);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// AttackTop 의 top 줄에 중단점을 걸고 조사식에 top == null 과 (object)top == null 을 나란히 넣어 보세요. 하나는 true, 하나는 false 입니다.
// Threat 을 펼쳐 보면 키 중 하나가 "null" 로 보이는데 항목은 그대로 있습니다. 1부 랩 1(가짜 null)과 같은 상태입니다.
//
// AttackTop の top の行にブレークポイントを置き、ウォッチ式に top == null と (object)top == null を並べて入れてみてください。一方は true、もう一方は false です。
// Threat を展開すると、キーの 1 つが "null" と表示されるのに、項目はそのまま残っています。第1部のラボ 1 (偽の null) と同じ状態です。
#endregion

#region 힌트 2 / ヒント 2
// Destroy 는 네이티브 쪽 오브젝트만 지웁니다. C# 쪽 Enemy 객체는 Dictionary 가 붙잡고 있으므로 그대로 남고, 키로서의 해시와 Equals 도 그대로입니다.
// 그래서 Dictionary 는 그 항목을 지우지 않고, 값(점수)도 남아 1위를 계속 차지합니다. Unity 오브젝트를 키나 원소로 담는 HashSet, List 도 같습니다.
// 이 항목은 아무도 지우지 않으므로 C# 객체가 GC 되지 않는 작은 누수이기도 합니다.
//
// Destroy はネイティブ側のオブジェクトだけを消します。C# 側の Enemy オブジェクトは Dictionary がつかんでいるのでそのまま残り、キーとしてのハッシュと Equals もそのままです。
// そのため Dictionary はその項目を消さず、値 (ヘイト値) も残って 1 位を占め続けます。Unity オブジェクトをキーや要素として入れる HashSet、List も同じです。
// この項目は誰も消さないので、C# オブジェクトが GC されない小さなリークでもあります。
#endregion

#region 힌트 3 / ヒント 3
// 방법 1: 쓰기 전에 죽은 키를 치웁니다.
//   foreach (Enemy dead in Threat.Keys.Where(e => e == null).ToList()) { Threat.Remove(dead); }   // == null 은 Unity 의 재정의라 파괴된 것도 잡는다 / == null は Unity のオーバーライドなので破棄されたものも捕まえる
// 방법 2: 적이 죽을 때 알립니다. Enemy.OnDestroy 에서 event 를 쏘고, Boss 가 받아 Threat.Remove(enemy).
// ThreatCount 도 죽은 적을 빼고 세어야 합니다. 치우는 곳을 한 곳(Prune 함수)으로 모아 두면 둘 다 해결됩니다.
//
// 方法 1: 使う前に死んだキーを片付けます。
//   foreach (Enemy dead in Threat.Keys.Where(e => e == null).ToList()) { Threat.Remove(dead); }   // == null は Unity のオーバーライドなので破棄されたものも捕まえる
// 方法 2: 敵が死ぬときに知らせます。Enemy.OnDestroy で event を発火し、Boss が受け取って Threat.Remove(enemy)。
// ThreatCount も死んだ敵を除いて数える必要があります。片付ける場所を 1 か所 (Prune 関数) にまとめておけば、両方とも解決します。
#endregion
