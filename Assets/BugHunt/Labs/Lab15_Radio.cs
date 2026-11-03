// 랩 15: 작전 브리핑
// ⚠ 이 랩은 소리를 냅니다. 실행 전에 시스템 볼륨을 낮추고, 헤드폰을 쓴다면 귀에서 떼고 시작하세요.
// 무전기로 3초짜리 작전 브리핑이 나오는 중에 플레이어가 맞아서 "윽!" 소리를 내면 브리핑이 끊긴다.
// 기대: 맞는 소리는 겹쳐서 나고, 1초 뒤에도 브리핑은 계속 재생 중이다.
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab15
{
    // 플레이어 목소리 채널. 무전 브리핑과 짧은 신음 소리를 같은 AudioSource 로 낸다.
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

    [Lab(15, "작전 브리핑", MakesSound = true)]
    public class Lab15_Radio : Lab
    {
        public override IEnumerator Run()
        {
            var briefing = Resources.Load<AudioClip>("BugHunt/Audio/lab15_briefing");
            var grunt = Resources.Load<AudioClip>("BugHunt/Audio/lab15_grunt");
            var voice = Spawn<PlayerVoice>("PlayerVoice");

            voice.PlayBriefing(briefing);
            yield return new WaitForSeconds(0.5f);
            bool audioWorks = voice.Source.isPlaying;   // 판정용 대조군: 맞기 전에는 재생 중이어야 한다
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
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// PlayHurt 에 중단점. F10 으로 한 줄씩 넘기며 조사식 Source.clip.name, Source.isPlaying, Source.time 을 보세요.
// AudioSource 하나는 "clip 하나를 재생하는 플레이어" 입니다. clip 을 바꾸고 Play() 하면 지금 재생 중이던 것은?
#endregion

#region 힌트 2
// AudioSource 의 두 가지 재생 방식:
//   Play()             : Source.clip 을 처음부터 재생. 이미 재생 중이면 멈추고 다시 시작. Stop/Pause/time 으로 제어 가능.
//   PlayOneShot(clip)  : clip 을 "한 번 쏘고 잊기". Source.clip 은 건드리지 않고 기존 재생 위에 겹친다. 하나씩 멈출 수는 없다(Stop 하면 전부).
#endregion

#region 힌트 3
// 짧은 효과음(신음, 발사음, 발소리)은 PlayOneShot(grunt) 로 겹쳐 내고, 길고 제어가 필요한 것(브리핑, 음악)은 Play() 로 냅니다.
// 브리핑을 멈추거나 일시정지할 일이 많다면 아예 별도의 AudioSource 를 쓰는 것이 깔끔합니다.
#endregion
