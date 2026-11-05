// 랩 48: 공격 버튼
// 공격 버튼을 눌러도 기사가 칼을 휘두르지 않는다. 예외도 없고 게임은 멀쩡히 돈다.
// 기대: 버튼을 누르고 0.5초 안에 Animator 가 Attack 상태에 들어간다.
// ラボ 48: 攻撃ボタン
// 攻撃ボタンを押しても騎士が剣を振らない。例外もなく、ゲームは普通に動いている。
// 期待: ボタンを押してから 0.5 秒以内に Animator が Attack ステートに入る。
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab48
{
    public class Knight : MonoBehaviour
    {
        static readonly int AttackId = Animator.StringToHash("attack");
        Animator animator;

        void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public void OnAttackButton()
        {
            animator.SetTrigger(AttackId);
        }
    }

    [Lab(48, "공격 버튼", Symptom = "버튼을 눌러도 공격 모션이 안 나온다(예외 없음)")]
    public class Lab48_AttackButton : Lab
    {
        public override IEnumerator Run()
        {
            GameObject go = Spawn("Knight");
            new GameObject("Body").transform.SetParent(go.transform, false);
            var animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("BugHunt/Animation/Knight");
            var knight = go.AddComponent<Knight>();
            yield return null;

            knight.OnAttackButton();

            bool attacked = false;
            float until = Time.time + 0.5f;
            while (Time.time < until && !attacked)
            {
                yield return null;
                attacked = animator.GetCurrentAnimatorStateInfo(0).IsName("Attack")
                    || animator.GetNextAnimatorStateInfo(0).IsName("Attack");
            }
            Report.ExpectTrue("0.5초 안에 Attack 상태 진입", attacked);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// Console 창 오른쪽 위의 노란 경고(⚠) 버튼이 켜져 있나요? 랩을 돌린 뒤 경고 한 줄이 있습니다. 그 문장을 그대로 읽어 보세요.
// 예외가 아니라 경고라서 게임은 멈추지 않습니다. 경고를 꺼 두고 개발하는 습관이 이런 버그를 오래 숨깁니다.
//
// コンソールウィンドウの右上にある黄色い警告 (⚠) ボタンはオンになっていますか? ラボを実行した後、警告が 1 行出ています。その文をそのまま読んでみてください。
// 例外ではなく警告なので、ゲームは止まりません。警告を非表示にしたまま開発する習慣が、こういうバグを長く隠してしまいます。
#endregion

#region 힌트 2 / ヒント 2
// Window → Animation → Animator 를 띄우고, 플레이 중에 Hierarchy 에서 Lab48/Knight 를 고르세요(랩이 끝나면 지워지니 OnAttackButton 에 중단점을 걸고 멈춘 상태에서).
// 왼쪽 Parameters 탭에 어떤 이름의 파라미터가 있나요? 대소문자까지 비교하세요. 트리거가 눌리면 동그라미가 잠깐 켜집니다.
// 조사식으로도 볼 수 있습니다: animator.parameters (펼쳐서 name), animator.GetParameter(0).nameHash 와 AttackId 비교.
//
// ウィンドウ → アニメーション → アニメーター を開き、再生中にヒエラルキーで Lab48/Knight を選んでください (ラボが終わると消えるので、OnAttackButton にブレークポイントを置いて止めた状態で)。
// 左のパラメーター (Parameters) タブには、どんな名前のパラメーターがありますか? 大文字・小文字まで比べてください。トリガーが押されると丸が一瞬点灯します。
// ウォッチ式でも見られます: animator.parameters (展開して name)、animator.GetParameter(0).nameHash と AttackId を比較。
#endregion

#region 힌트 3 / ヒント 3
// Animator 파라미터 이름은 대소문자를 구분하고, 틀리면 경고만 찍고 아무 일도 하지 않습니다(해시로 넘겨도 같습니다).
// 이름은 한 곳(static readonly int ... = Animator.StringToHash("Attack"))에만 적고, 컨트롤러의 파라미터 이름과 정확히 맞추세요.
// 시작할 때 한 번 검사하는 습관도 좋습니다: foreach (var p in animator.parameters) 로 필요한 이름이 다 있는지 확인.
//
// Animator のパラメーター名は大文字と小文字を区別し、間違っていると警告を出すだけで何もしません (ハッシュで渡しても同じです)。
// 名前は 1 か所 (static readonly int ... = Animator.StringToHash("Attack")) にだけ書き、コントローラーのパラメーター名と正確に合わせてください。
// 開始時に一度チェックする習慣も良いです: foreach (var p in animator.parameters) で必要な名前が全部あるか確認。
#endregion
