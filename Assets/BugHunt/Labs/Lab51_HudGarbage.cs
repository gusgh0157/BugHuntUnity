// 랩 51: 체력 표시줄
// 부대원마다 머리 위에 체력 표시줄이 있다. 화면은 멀쩡한데 몇 초마다 프레임이 툭 끊긴다. Profiler 를 보면 GC.Collect 가 튄다.
// 아무도 맞지 않아 표시가 그대로인 프레임에도 쓰레기(GC 할당)가 계속 쌓인다.
// 기대: 값이 바뀌지 않은 프레임에는 GC 할당이 없다(부대 전체 합 프레임당 128바이트 이하). 값이 바뀌면 표시는 바로 바뀐다.
// ラボ 51: 体力バー
// 部隊員ごとに頭の上に体力バーがある。画面は問題ないのに、数秒ごとにフレームがガクッと途切れる。プロファイラーを見ると GC.Collect が跳ねている。
// 誰も攻撃を受けず表示が変わらないフレームでも、ゴミ (GC アロケーション) がたまり続ける。
// 期待: 値が変わらないフレームでは GC アロケーションがない (部隊全体の合計でフレームあたり 128 バイト以下)。値が変われば表示はすぐに変わる。
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BugHunt.Labs.Lab51
{
    public class Health : MonoBehaviour
    {
        public int Current = 100;
        public int Max = 100;
    }

    public class Squad : MonoBehaviour
    {
        public List<Health> Members = new List<Health>();
    }

    public class HealthBar : MonoBehaviour
    {
        public Squad Squad;
        public string Text;
        public string SquadText;

        void Update()
        {
            Health health = GetComponent<Health>();
            Text = "HP " + health.Current + " / " + health.Max;

            int alive = Squad.Members.Count(m => m.Current > 0);
            int total = 0;
            IEnumerable<Health> members = Squad.Members;
            foreach (Health member in members)
            {
                total += member.Current;
            }
            SquadText = Loc.F("생존 {0}명, 부대 HP {1}", alive, total);
        }
    }

    [Lab(51, "체력 표시줄", Symptom = "값이 그대로인데도 매 프레임 쓰레기가 쌓여 GC 로 끊긴다")]
    public class Lab51_HudGarbage : Lab
    {
        public override IEnumerator Run()
        {
            const long Budget = 128;
            int count = Case(20, 28);
            var squad = Spawn<Squad>("Squad");
            var bars = new List<HealthBar>();
            for (int i = 0; i < count; ++i)
            {
                var member = Spawn<Health>($"Member_{i}");
                squad.Members.Add(member);
                var bar = member.gameObject.AddComponent<HealthBar>();
                bar.Squad = squad;
                bar.enabled = false;
                bars.Add(bar);
            }

            var baseline = new GcMeter();
            yield return baseline.Measure(30);
            foreach (var bar in bars)
            {
                bar.enabled = true;
            }
            yield return null;
            yield return null;   // 첫 표시를 만드는 프레임은 뺀다 / 最初の表示を作るフレームは除く
            var work = new GcMeter();
            yield return work.Measure(30);
            long bytes = System.Math.Max(0, work.BytesPerFrame - baseline.BytesPerFrame);

            Report.Note(Loc.F("표시줄 {0}개, 값이 그대로인 프레임의 GC 할당: 프레임당 {1}바이트 (아무것도 안 할 때 {2}바이트)", count, bytes, baseline.BytesPerFrame));
            Report.ExpectTrue(Loc.F("값이 그대로인 프레임의 GC 할당 ≤ {0}바이트/프레임", Budget), bytes <= Budget);

            int hit = Case(3, 11);
            squad.Members[hit].Current = Case(70, 45);
            squad.Members[0].Current = 0;
            yield return null;
            int expectedTotal = squad.Members.Sum(m => m.Current);
            Report.Expect("맞은 부대원의 표시", $"HP {squad.Members[hit].Current} / 100", bars[hit].Text);
            Report.Expect("부대 표시", Loc.F("생존 {0}명, 부대 HP {1}", count - 1, expectedTotal), bars[hit].SquadText);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// Window → Analysis → Profiler → CPU Usage → 아래 창을 Hierarchy 로 두고 GC Alloc 열로 정렬하세요. HealthBar.Update 가 프레임마다 몇 바이트를 쓰나요?
// 어느 줄이 쓰는지 모르겠으면 Profiler 위쪽의 Deep Profile 을 켜고 다시 플레이하세요. HealthBar.Update 아래로 String.Concat, Enumerable.Count, 박싱된 열거자가 보입니다(Deep Profile 은 느려지므로 끝나면 끕니다).
// Deep Profile 대신 의심 가는 줄을 ProfilerMarker 로 감싸도 됩니다: static readonly ProfilerMarker s_Text = new ProfilerMarker("HealthBar.Text"); using (s_Text.Auto()) { ... }
//
// ウィンドウ (Window) → 分析 (Analysis) → プロファイラー (Profiler) → CPU Usage → 下のペインをヒエラルキー (Hierarchy) にして、GC アロケーション (GC Alloc) 列で並べ替えてください。HealthBar.Update はフレームごとに何バイト使っていますか?
// どの行が使っているか分からなければ、プロファイラー上部の 詳細プロファイル (Deep Profile) をオンにしてもう一度再生してください。HealthBar.Update の下に String.Concat、Enumerable.Count、ボックス化された列挙子が見えます (詳細プロファイルは遅くなるので、終わったらオフにします)。
// 詳細プロファイル (Deep Profile) の代わりに、怪しい行を ProfilerMarker で囲んでもかまいません: static readonly ProfilerMarker s_Text = new ProfilerMarker("HealthBar.Text"); using (s_Text.Auto()) { ... }
#endregion

#region 힌트 2 / ヒント 2
// 쓰레기를 만드는 것: ① 문자열 + 와 $"" 보간, string.Format(Loc.F 도 같다)(매번 새 string) ② LINQ(Count(람다) 는 람다가 지역 변수를 안 쓰면 캐시되지만 열거자는 매번 만든다)
// ③ List 를 IEnumerable<T> 로 받아 foreach(List 의 열거자는 struct 인데, 인터페이스로 받으면 박싱되어 힙에 올라간다)
// GetComponent 는 찾았을 때 할당하지 않지만(에디터에서 못 찾으면 할당) 매 프레임 찾을 이유가 없습니다.
// 공통점: 결과가 지난 프레임과 같은데 매 프레임 새로 만든다.
//
// ゴミを作るもの: ① 文字列の + と $"" 補間、string.Format (Loc.F も同じ) (毎回新しい string) ② LINQ (Count(ラムダ) はラムダがローカル変数を使わなければキャッシュされるが、列挙子は毎回作る)
// ③ List を IEnumerable<T> で受けて foreach (List の列挙子は struct だが、インターフェイスで受けるとボックス化されてヒープに載る)
// GetComponent は見つかったときはアロケーションしませんが (エディターで見つからなければアロケーションする)、毎フレーム探す理由がありません。
// 共通点: 結果が前のフレームと同じなのに、毎フレーム新しく作っている。
#endregion

#region 힌트 3 / ヒント 3
// 바뀔 때만 만든다:  if (health.Current != shownHp) { shownHp = health.Current; Text = ...; }   // 부대 표시도 alive, total 이 바뀔 때만
// 컴포넌트는 Awake 에서 한 번: Health health; void Awake() { health = GetComponent<Health>(); }
// LINQ 와 인터페이스 foreach 대신 for:  for (int i = 0; i < Squad.Members.Count; ++i) { var m = Squad.Members[i]; ... }
// UI 텍스트라면 TextMeshPro 의 SetText("HP {0} / {1}", hp, max) 는 할당 없이 숫자를 넣습니다.
//
// 変わったときだけ作る:  if (health.Current != shownHp) { shownHp = health.Current; Text = ...; }   // 部隊の表示も alive, total が変わったときだけ
// コンポーネントは Awake で 1 回: Health health; void Awake() { health = GetComponent<Health>(); }
// LINQ とインターフェイスの foreach の代わりに for:  for (int i = 0; i < Squad.Members.Count; ++i) { var m = Squad.Members[i]; ... }
// UI テキストなら、TextMeshPro の SetText("HP {0} / {1}", hp, max) はアロケーションなしで数字を入れます。
#endregion
