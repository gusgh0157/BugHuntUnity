// 랩 35: 작전 브리핑
// ⚠ 이 랩은 소리를 냅니다. 실행 전에 시스템 볼륨을 낮추고, 헤드폰을 쓴다면 귀에서 떼고 시작하세요.
// 무전기로 3초짜리 작전 브리핑이 나오는 중에 플레이어가 맞아서 "윽!" 소리를 내면 브리핑이 끊긴다.
// 기대: 맞는 소리는 겹쳐서 나고, 1초 뒤에도 브리핑은 계속 재생 중이다.
// ラボ 35: 作戦ブリーフィング
// ⚠ このラボは音が出ます。実行する前にシステムの音量を下げ、ヘッドホンを使っているなら耳から外してから始めてください。
// 無線で 3 秒の作戦ブリーフィングが流れている最中に、プレイヤーが攻撃を受けて「うっ!」と声を出すと、ブリーフィングが途切れる。
// 期待: 被弾の声は重なって鳴り、1 秒後にもブリーフィングは再生され続けている。
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab35
{
    // 플레이어 목소리 채널. 무전 브리핑과 짧은 신음 소리를 같은 AudioSource 로 낸다.
    // プレイヤーの声のチャンネル。無線のブリーフィングと短いうめき声を同じ AudioSource で鳴らす。
    public class PlayerVoice : MonoBehaviour
    {
        public AudioSource Source;

        void Awake()
        {
            Source = gameObject.AddComponent<AudioSource>();
            Source.playOnAwake = false;
        }

        public void PlayBriefing(AudioClip briefing)
        {
            Source.clip = briefing;
            Source.Play();
        }

        public void PlayHurt(AudioClip grunt)
        {
            Source.clip = grunt;
            Source.Play();
        }
    }

    [Lab(35, "작전 브리핑", MakesSound = true, Symptom = "맞는 소리 때문에 브리핑이 끊긴다")]
    public class Lab35_Radio : Lab
    {
        public override IEnumerator Run()
        {
            var briefing = Resources.Load<AudioClip>("BugHunt/Audio/briefing");
            var grunt = Resources.Load<AudioClip>("BugHunt/Audio/grunt");
            var voice = Spawn<PlayerVoice>("PlayerVoice");

            voice.PlayBriefing(briefing);
            yield return new WaitForSeconds(0.5f);
            bool audioWorks = voice.Source.isPlaying;   // 판정용 대조군: 맞기 전에는 재생 중이어야 한다 / 判定用の対照群: 被弾する前は再生中でなければならない
            voice.PlayHurt(grunt);
            yield return new WaitForSeconds(0.5f);

            if (!audioWorks)
            {
                Report.Skip("브리핑 재생", "오디오 출력 장치가 없거나 오디오가 꺼져 있습니다");
                yield break;
            }
            Report.Expect("1초 뒤 Source 에 걸린 클립", briefing, voice.Source.clip);
            Report.ExpectTrue("1초 뒤에도 브리핑 재생 중", voice.Source.isPlaying && voice.Source.clip == briefing);
            Report.ExpectNear("브리핑 재생 위치(초)", 1f, voice.Source.time, 0.25f);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// PlayHurt 에 중단점. F10 으로 한 줄씩 넘기며 조사식 Source.clip.name, Source.isPlaying, Source.time 을 보세요.
// AudioSource 하나는 "clip 하나를 재생하는 플레이어" 입니다. clip 을 바꾸고 Play() 하면 지금 재생 중이던 것은?
//
// PlayHurt にブレークポイント。F10 で 1 行ずつ進めながら、ウォッチ式 Source.clip.name、Source.isPlaying、Source.time を見てください。
// AudioSource 1 つは「clip を 1 つ再生するプレイヤー」です。clip を変えて Play() すると、今再生中だったものは?
#endregion

#region 힌트 2 / ヒント 2
// AudioSource 의 두 가지 재생 방식:
//   Play()             : Source.clip 을 처음부터 재생. 이미 재생 중이면 멈추고 다시 시작. Stop/Pause/time 으로 제어 가능.
//   PlayOneShot(clip)  : clip 을 "한 번 쏘고 잊기". Source.clip 은 건드리지 않고 기존 재생 위에 겹친다. 하나씩 멈출 수는 없다(Stop 하면 전부).
//
// AudioSource の 2 つの再生方法:
//   Play()             : Source.clip を最初から再生。すでに再生中なら止めて最初からやり直す。Stop/Pause/time で制御できる。
//   PlayOneShot(clip)  : clip を「1 回鳴らして忘れる」。Source.clip には触れず、今の再生の上に重ねる。1 つずつ止めることはできない (Stop するとすべて止まる)。
#endregion

#region 힌트 3 / ヒント 3
// 짧은 효과음(신음, 발사음, 발소리)은 PlayOneShot(grunt) 로 겹쳐 내고, 길고 제어가 필요한 것(브리핑, 음악)은 Play() 로 냅니다.
// 브리핑을 멈추거나 일시정지할 일이 많다면 아예 별도의 AudioSource 를 쓰는 것이 깔끔합니다.
//
// 短い効果音 (うめき声、発射音、足音) は PlayOneShot(grunt) で重ねて鳴らし、長くて制御が必要なもの (ブリーフィング、音楽) は Play() で鳴らします。
// ブリーフィングを止めたり一時停止したりすることが多いなら、最初から別の AudioSource を使うのがすっきりします。
#endregion
