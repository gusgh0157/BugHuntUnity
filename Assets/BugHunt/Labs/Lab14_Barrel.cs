// 랩 14: 폭발 통
// 통이 터지는 순간 폭발음이 "틱" 하고 끊긴다.
// 기대: 터지고 0.2초 뒤에도 폭발음(1.5초짜리)이 재생 중이다. 통은 사라졌다.
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab14
{
    public class ExplosiveBarrel : MonoBehaviour
    {
        public AudioClip ExplosionClip;
        AudioSource source;

        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
        }

        public void Explode()
        {
            source.clip = ExplosionClip;
            source.Play();
            Destroy(gameObject);   // 터진 통은 사라진다
        }
    }

    [Lab(14, "폭발 통")]
    public class Lab14_Barrel : Lab
    {
        public override IEnumerator Run()
        {
            var clip = Resources.Load<AudioClip>("BugHunt/Audio/lab14_explosion");
            var barrel = Spawn<ExplosiveBarrel>("ExplosiveBarrel", new Vector3(0, 0, 3));
            barrel.ExplosionClip = clip;

            // 판정용 대조군: 이 PC 에서 소리가 재생되기는 하는가 (오디오 출력 장치가 없으면 isPlaying 이 false)
            var probe = Spawn<AudioSource>("AudioProbe");
            probe.clip = clip;
            probe.volume = 0f;
            probe.Play();
            yield return null;
            bool audioWorks = probe.isPlaying;
            Destroy(probe.gameObject);
            yield return null;

            barrel.Explode();
            yield return new WaitForSeconds(0.2f);

            Report.ExpectTrue("통이 사라짐", barrel == null);
            if (!audioWorks)
            {
                Report.Skip("폭발음 재생", "오디오 출력 장치가 없거나 오디오가 꺼져 있습니다");
                yield break;
            }
            Report.ExpectTrue("0.2초 뒤에도 폭발음 재생 중", LabUtil.IsClipPlayingAnywhere(clip));
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ============================================================================

#region 힌트 1 (먼저 직접 추적해 보고 펼치세요)
// 소리를 내는 것은 AudioClip 이 아니라 AudioSource "컴포넌트" 입니다. 그 컴포넌트는 어느 게임 오브젝트에 붙어 있나요?
// Explode 를 F10 으로 넘기면서 Hierarchy 창을 보세요. Destroy 다음 프레임에 그 오브젝트와 AudioSource 는 어떻게 되나요?
#endregion

#region 힌트 2
// Debug.Break() 를 Explode 끝에 넣으면 그 프레임 끝에서 에디터가 일시 정지합니다. Hierarchy 에서 AudioSource 가 붙은 오브젝트를 찾아 보세요.
// 소리의 수명 = AudioSource 의 수명 = 게임 오브젝트의 수명. 오브젝트가 사라지면 재생 중이던 소리도 그 자리에서 끊깁니다.
#endregion

#region 힌트 3
// 소리를 통과 다른 수명을 가진 곳에서 재생하세요.
//   AudioSource.PlayClipAtPoint(ExplosionClip, transform.position);  ← 임시 오브젝트 "One shot audio" 를 만들고 클립이 끝나면 스스로 지운다
// 또는 소리 전용 오브젝트를 떼어 내서(SetParent(null)) 재생하고 Destroy(그오브젝트, clip.length) 로 나중에 지우기.
// 실제 프로젝트라면 사운드 매니저의 AudioSource 풀에서 빌려 쓰는 것이 일반적입니다.
#endregion
