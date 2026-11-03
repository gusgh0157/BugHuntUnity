// 랩 26: 두 번째 판
// 우리 팀은 Play 를 빨리 시작하려고 Project Settings → Editor → Enter Play Mode Settings 에서 Domain Reload 를 껐다
// (When entering Play Mode: Reload Scene only).
// 그 뒤로 "Play 를 두 번째 누르면 웨이브 번호가 1 이 아니고, 레이더가 지난 판의 적을 가리키다 예외가 난다" 는 제보.
// 이 랩은 Play 를 다시 누른 것을 흉내 낸다: 지난 판의 오브젝트를 모두 지우고, Unity 가 Play 할 때 부르는
// [RuntimeInitializeOnLoadMethod] 들을 부른 뒤 새 판을 시작한다. Domain Reload 가 꺼져 있으면 정적(static) 필드는 그대로 남는다.
// 기대: 새 판의 첫 웨이브는 1, 레이더에는 이번 판의 적만 있고, 가장 가까운 적도 이번 판의 적이다.
// ラボ 26: 2 回目のプレイ
// うちのチームは再生を速く始めるため、プロジェクト設定 → エディター → 再生モード開始時設定 (Enter Play Mode Settings) で Domain Reload をオフにした
// (When entering Play Mode: Reload Scene only)。
// それ以来「2 回目に再生を押すとウェーブ番号が 1 ではなく、レーダーが前回のプレイの敵を指して例外が出る」という報告。
// このラボは再生を押し直したことをまねる: 前回のプレイのオブジェクトをすべて消し、Unity が再生時に呼ぶ
// [RuntimeInitializeOnLoadMethod] を呼んでから新しいプレイを始める。Domain Reload がオフなら静的 (static) フィールドはそのまま残る。
// 期待: 新しいプレイの最初のウェーブは 1、レーダーには今回のプレイの敵だけがいて、いちばん近い敵も今回のプレイの敵である。
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BugHunt.Labs.Lab26
{
    public class Enemy : MonoBehaviour
    {
        // 지금 살아 있는 적. 레이더, 미니맵, 웨이브 종료 판정이 모두 이 목록을 본다.
        // 今生きている敵。レーダー、ミニマップ、ウェーブ終了の判定がすべてこのリストを見る。
        public static readonly List<Enemy> Alive = new List<Enemy>();

        void Awake()
        {
            Alive.Add(this);
        }

        public void Die()
        {
            Alive.Remove(this);
            Destroy(gameObject);
        }
    }

    public static class WaveDirector
    {
        public static int Wave;   // 지금 몇 번째 웨이브인가 (판이 시작되면 0) / 今何番目のウェーブか (プレイが始まると 0)

        public static void NextWave()
        {
            ++Wave;
        }
    }

    public class Radar : MonoBehaviour
    {
        public Enemy Nearest()
        {
            return Enemy.Alive
                .OrderBy(e => Vector3.Distance(e.transform.position, transform.position))
                .FirstOrDefault();
        }
    }

    [Lab(26, "두 번째 판", Symptom = "Play 를 두 번째 누르면 웨이브가 이어지고 레이더가 지난 판의 적을 가리킨다")]
    public class Lab26_SecondRound : Lab
    {
        public override IEnumerator Run()
        {
            Report.Note(Loc.F("이 프로젝트의 Enter Play Mode 설정: {0}", LabUtil.EnterPlayModeDescription()));

            // ---- 1판 ----
            // ---- 1 回目のプレイ ----
            SimulateEnterPlayMode();
            GameObject round1 = Spawn("Round1");
            int round1Enemies = Case(3, 5);
            var first = new List<Enemy>();
            for (int i = 0; i < round1Enemies; ++i)
            {
                first.Add(SpawnEnemy(round1, $"R1_Enemy_{i}", new Vector3(0, 0, 2 + i)));
            }
            for (int w = 0; w < Case(3, 4); ++w)
            {
                WaveDirector.NextWave();
            }
            first[0].Die();   // 한 마리만 잡고 판이 끝났다 / 1 体だけ倒してプレイが終わった
            yield return null;
            Report.Note(Loc.F("1판 끝: 웨이브 {0}, 살아 있는 적 {1}", WaveDirector.Wave, Enemy.Alive.Count));

            // ---- 플레이를 멈추고 다시 Play ----
            // ---- 再生を止めて、もう一度再生 ----
            Destroy(round1);   // 씬이 내려가며 오브젝트가 모두 파괴된다 / シーンがアンロードされ、オブジェクトがすべて破棄される
            yield return null;
            SimulateEnterPlayMode();

            // ---- 2판 ----
            // ---- 2 回目のプレイ ----
            GameObject round2 = Spawn("Round2");
            int round2Enemies = Case(2, 3);
            for (int i = 0; i < round2Enemies; ++i)
            {
                SpawnEnemy(round2, $"R2_Enemy_{i}", new Vector3(0, 0, 10 + i));
            }
            var radar = round2.AddComponent<Radar>();
            WaveDirector.NextWave();
            yield return null;

            Report.Expect("2판 첫 웨이브 번호", 1, WaveDirector.Wave);
            Report.Expect("레이더의 적 수", round2Enemies, Enemy.Alive.Count);
            Enemy nearest = radar.Nearest();
            Report.Expect("가장 가까운 적", "R2_Enemy_0", nearest != null ? nearest.name : "없음");
        }

        Enemy SpawnEnemy(GameObject parent, string enemyName, Vector3 position)
        {
            var go = new GameObject(enemyName);
            go.transform.SetParent(parent.transform, false);
            go.transform.position = position;
            return go.AddComponent<Enemy>();
        }

        // Domain Reload 를 끈 Play 진입: 정적 필드는 손대지 않고, [RuntimeInitializeOnLoadMethod] 만 순서대로 부른다.
        // Domain Reload をオフにした再生開始: 静的フィールドには手を付けず、[RuntimeInitializeOnLoadMethod] だけを順番に呼ぶ。
        static void SimulateEnterPlayMode()
        {
            var order = new[]
            {
                RuntimeInitializeLoadType.SubsystemRegistration,
                RuntimeInitializeLoadType.AfterAssembliesLoaded,
                RuntimeInitializeLoadType.BeforeSplashScreen,
                RuntimeInitializeLoadType.BeforeSceneLoad,
                RuntimeInitializeLoadType.AfterSceneLoad,
            };
            var methods = typeof(Lab26_SecondRound).Assembly.GetTypes()
                .Where(t => t.Namespace == typeof(Lab26_SecondRound).Namespace)
                .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Select(m => (Method: m, Info: m.GetCustomAttribute<RuntimeInitializeOnLoadMethodAttribute>()))
                .Where(x => x.Info != null)
                .OrderBy(x => Array.IndexOf(order, x.Info.loadType))
                .ToList();
            foreach (var (method, _) in methods)
            {
                method.Invoke(null, null);
            }
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// "2판 시작" 바로 앞(SimulateEnterPlayMode 다음 줄)에 중단점을 걸고 조사식에 Enemy.Alive 와 WaveDirector.Wave 를 넣으세요.
// Alive 를 펼치면 원소가 "null" 로 보이나요? 그런데 Count 에는 들어 있습니다. 랩 1·8 에서 본 "파괴된 오브젝트" 입니다.
// 랩 출력 첫 줄이 이 프로젝트의 실제 설정입니다. 이 랩은 설정과 상관없이 "Domain Reload 를 끈 Play" 를 흉내 냅니다.
//
// 「2 回目のプレイ開始」の直前 (SimulateEnterPlayMode の次の行) にブレークポイントを置き、ウォッチ式に Enemy.Alive と WaveDirector.Wave を入れてください。
// Alive を展開すると要素が "null" に見えますか? それなのに Count には入っています。ラボ 1・8 で見た「破棄されたオブジェクト」です。
// ラボ出力の 1 行目がこのプロジェクトの実際の設定です。このラボは設定に関係なく「Domain Reload をオフにした再生」をまねます。
#endregion

#region 힌트 2 / ヒント 2
// Domain Reload 는 Play 할 때마다 C# 을 처음부터 다시 올리는 단계입니다. 이걸 끄면 Play 는 빨라지지만, 정적 필드와 정적 이벤트는
// 지난 Play 의 값을 그대로 들고 시작합니다. "static int Wave;" 의 초기값 0 은 Play 할 때마다 다시 적용되지 않습니다.
// Unity 문서 "Domain Reloading" 의 권장: 정적 상태는 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] 에서 직접 되돌린다.
// 이 랩은 그 메서드들을 Unity 처럼 불러 줍니다. 지금은 그런 메서드가 하나도 없습니다.
//
// Domain Reload は再生のたびに C# を最初から読み込み直す段階です。これをオフにすると再生は速くなりますが、静的フィールドと静的イベントは
// 前回の再生の値をそのまま持って始まります。"static int Wave;" の初期値 0 は再生のたびに適用し直されるわけではありません。
// Unity ドキュメント "Domain Reloading" の推奨: 静的な状態は [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] で自分で元に戻す。
// このラボはそのメソッドを Unity と同じように呼んでくれます。今はそういうメソッドが 1 つもありません。
#endregion

#region 힌트 3 / ヒント 3
// 정적 상태를 가진 클래스마다 되돌리는 메서드를 하나씩 둡니다.
//   [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
//   static void ResetStatics() { Alive.Clear(); }         // Enemy 안에
//   ... { Wave = 0; }                                      // WaveDirector 안에
// 덤: Die 를 거치지 않고 사라지는 적(씬 전환, 부모째 Destroy)도 목록에서 빠지게 OnDestroy 에서 Alive.Remove(this) 를 하면 더 튼튼합니다.
// 그것만으로는 Wave 가 이어지는 버그가 남습니다. 둘 다 하세요.
//
// 静的な状態を持つクラスごとに、元に戻すメソッドを 1 つずつ置きます。
//   [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
//   static void ResetStatics() { Alive.Clear(); }         // Enemy の中に
//   ... { Wave = 0; }                                      // WaveDirector の中に
// おまけ: Die を通らずに消える敵 (シーン切り替え、親ごと Destroy) もリストから外れるように、OnDestroy で Alive.Remove(this) をするとより堅牢です。
// それだけでは Wave が引き継がれるバグが残ります。両方やってください。
#endregion
