// 랩 12: 블래스터 발사음
// ⚠ 이 랩은 소리를 냅니다. 실행 전에 시스템 볼륨을 낮추고, 헤드폰을 쓴다면 귀에서 떼고 시작하세요.
// 무기를 들고 0.5초 뒤 첫 발을 쏘면 발사음이 섬광보다 늦게 난다. 두 번째 발부터는 괜찮다.
// 기대: 첫 발을 쏘는 순간 발사음 데이터가 이미 메모리에 올라와 있다(AudioDataLoadState.Loaded).
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab12
{
    public class Blaster : MonoBehaviour
    {
        public AudioClip FireClip;
        public int Shots;
        AudioSource source;

        public void Equip(AudioClip fireClip)
        {
            FireClip = fireClip;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
        }

        public void Fire()
        {
            ++Shots;   // 총구 섬광은 이 프레임에 그려진다
            source.PlayOneShot(FireClip);
        }
    }

    [Lab(12, "블래스터 발사음", MakesSound = true)]
    public class Lab12_BlasterSound : Lab
    {
        public override IEnumerator Run()
        {
            // 랩마다 처음 상태로: 이전 실행이 올려 둔 오디오 데이터를 내린다.
            var clip = Resources.Load<AudioClip>("BugHunt/Audio/lab12_blaster");
            if (clip.loadState == AudioDataLoadState.Loaded && !clip.preloadAudioData)
            {
                clip.UnloadAudioData();
            }

            var blaster = Spawn<Blaster>("Blaster");
            blaster.Equip(clip);
            yield return new WaitForSeconds(0.5f);   // 플레이어가 무기를 꺼내 조준하는 시간

            AudioDataLoadState stateAtFire = clip.loadState;
            int fireFrame = Time.frameCount;
            float fireTime = Time.realtimeSinceStartup;
            blaster.Fire();

            while (clip.loadState != AudioDataLoadState.Loaded && Time.realtimeSinceStartup - fireTime < 2f)
            {
                yield return null;
            }
            Report.Note($"발사 후 데이터가 올라오기까지: {Time.frameCount - fireFrame}프레임, {(Time.realtimeSinceStartup - fireTime) * 1000f:0.0}ms");
            Report.Expect("첫 발을 쏜 순간의 발사음 로드 상태", AudioDataLoadState.Loaded, stateAtFire);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// Blaster.Fire 에 중단점을 걸고 조사식에 FireClip.loadState, FireClip.preloadAudioData, FireClip.loadInBackground 를 넣으세요.
// 0.5초나 기다렸는데 왜 아직 Unloaded 일까요? 그 0.5초 동안 누가 이 클립을 로드하라고 했나요?
#endregion

#region 힌트 2
// Project 창에서 Assets/BugHunt/Resources/BugHunt/Audio/lab12_blaster.wav 를 고르고 Inspector 의 임포트 설정을 보세요.
// Load Type, Preload Audio Data, Load In Background 가 각각 무엇인가요?
// Profiler 의 Audio 모듈(또는 Memory 모듈)에서 첫 발 프레임 전후로 오디오 메모리가 언제 늘어나는지도 보세요.
#endregion

#region 힌트 3
// Preload Audio Data 를 끄면 클립을 로드해도 소리 데이터는 "처음 재생할 때" 올라옵니다. Load In Background 까지 켜져 있으면
// 재생은 데이터가 올라온 뒤로 미뤄집니다(끄면 대신 그 프레임이 멈칫합니다).
// 고치는 법 (택1): ① 임포트 설정에서 Preload Audio Data 를 켜고 Apply  ② 무기를 장착할 때 FireClip.LoadAudioData() 를 미리 부르기.
// 메모리를 아끼려고 끈 설정이라면 ②가 맞습니다. "언제 메모리에 올릴지"를 코드가 정하는 것입니다.
#endregion
