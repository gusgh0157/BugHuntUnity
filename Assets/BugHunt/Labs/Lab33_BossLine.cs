// 랩 33: 보스 대사
// ⚠ 이 랩은 소리를 냅니다. 실행 전에 시스템 볼륨을 낮추고, 헤드폰을 쓴다면 귀에서 떼고 시작하세요.
// 병사 40명이 행진하는 장면에서 보스가 등장 대사를 하는데, 대사가 들리지 않는다. 병사 수를 줄이면 들린다.
// 기대: 보스 대사가 실제로 소리를 내고 있다(가상 음성으로 밀려나지 않음).
// ラボ 33: ボスのセリフ
// ⚠ このラボは音が出ます。実行する前にシステムの音量を下げ、ヘッドホンを使っているなら耳から外してから始めてください。
// 兵士 40 人が行進する場面でボスが登場のセリフを言うが、セリフが聞こえない。兵士の数を減らすと聞こえる。
// 期待: ボスのセリフが実際に音を出している (仮想音声に追いやられていない)。
using System.Collections;
using UnityEngine;

namespace BugHunt.Labs.Lab33
{
    public class MarchingSoldier : MonoBehaviour
    {
        public void StartMarching(AudioClip footsteps)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.clip = footsteps;
            source.loop = true;
            source.volume = 1f;
            source.Play();
        }
    }

    public class Boss : MonoBehaviour
    {
        public AudioSource Voice;

        public void Speak(AudioClip line)
        {
            Voice = gameObject.AddComponent<AudioSource>();
            Voice.clip = line;
            Voice.volume = 0.6f;   // 보스 목소리는 원본이 커서 줄였다 / ボスの声は元の音が大きいので下げた
            Voice.Play();
        }
    }

    [Lab(33, "보스 대사", MakesSound = true, Symptom = "병사가 많으면 보스 대사가 안 들린다")]
    public class Lab33_BossLine : Lab
    {
        public override IEnumerator Run()
        {
            AudioConfiguration config = AudioSettings.GetConfiguration();
            Report.Note($"Max Real Voices {config.numRealVoices}, Max Virtual Voices {config.numVirtualVoices}");

            var footsteps = Resources.Load<AudioClip>("BugHunt/Audio/footsteps");
            var line = Resources.Load<AudioClip>("BugHunt/Audio/boss");
            for (int i = 0; i < 40; ++i)
            {
                Spawn<MarchingSoldier>($"Soldier_{i}").StartMarching(footsteps);
            }
            yield return new WaitForSeconds(0.2f);

            var boss = Spawn<Boss>("Boss");
            boss.Speak(line);
            yield return new WaitForSeconds(0.3f);

            if (!boss.Voice.isPlaying)
            {
                Report.Skip("보스 대사", "오디오 출력 장치가 없거나 오디오가 꺼져 있습니다");
                yield break;
            }
            Report.ExpectTrue("보스 대사가 실제 음성으로 재생 중 (isVirtual == false)", !boss.Voice.isVirtual);
        }
    }
}

// ============================================================================
// 힌트: 막힐 때 하나씩만 펼치세요. (VS: Ctrl+M, Ctrl+O = 전부 접기)
// ヒント: 行き詰まったときに 1 つずつだけ開いてください。(VS: Ctrl+M, Ctrl+O = すべて折りたたむ)
// ============================================================================

#region 힌트 1 / ヒント 1 (먼저 직접 추적해 보고 펼치세요 / まず自分で追跡してから開いてください)
// 랩 출력의 Max Real Voices 를 보세요. 지금 동시에 재생하려는 소리는 몇 개인가요?
// 랩 마지막 줄에 중단점을 걸고 조사식에 boss.Voice.isPlaying, boss.Voice.isVirtual 을 넣으세요. "재생 중인데 가상" 이란 무슨 뜻일까요?
//
// ラボの出力の Max Real Voices を見てください。今、同時に再生しようとしている音はいくつですか?
// ラボの最後の行にブレークポイントを置き、ウォッチ式に boss.Voice.isPlaying、boss.Voice.isVirtual を入れてください。「再生中なのに仮想」とはどういう意味でしょう?
#endregion

#region 힌트 2 / ヒント 2
// Profiler → Audio 모듈에서 Playing Sources 와 Virtual(가상) 음성 수를 보세요. (Unity 6 는 Audio 모듈 아래쪽에 채널 목록이 나옵니다)
// 실제 음성 칸이 모자라면 Unity 는 "덜 중요한" 소리를 가상으로 돌립니다. 가상 소리는 시간만 흐르고 소리는 나지 않습니다.
// 무엇이 "덜 중요한지"는 먼저 AudioSource.priority(0 이 가장 중요, 기본 128), 그다음 들리는 크기(볼륨 × 거리 감쇠)로 정합니다.
//
// プロファイラー → オーディオ (Audio) モジュールで Playing Sources と Virtual (仮想) 音声の数を見てください。(Unity 6 ではオーディオモジュールの下側にチャンネルの一覧が出ます)
// 実音声の枠が足りないと、Unity は「重要度の低い」音を仮想に回します。仮想の音は時間だけが進み、音は鳴りません。
// 何が「重要度が低い」かは、まず AudioSource.priority (0 が最も重要、既定値 128)、次に聞こえる大きさ (音量 × 距離減衰) で決まります。
#endregion

#region 힌트 3 / ヒント 3
// 보스 대사처럼 반드시 들려야 하는 소리는 priority 를 낮은 숫자(예: 0)로 주세요. 발소리 같은 배경음은 높은 숫자(예: 200).
// 근본적으로는 같은 소리가 수십 개 동시에 날 필요가 없습니다. 발소리 수를 제한하는 것도 방법입니다.
// 한도 자체는 Project Settings → Audio → Max Real Voices 에서 바꿀 수 있지만 CPU 비용이 늘어납니다.
//
// ボスのセリフのように必ず聞こえなければならない音は、priority を小さい数 (例: 0) にしてください。足音のような背景音は大きい数 (例: 200)。
// 根本的には、同じ音が何十個も同時に鳴る必要はありません。足音の数を制限するのも 1 つの方法です。
// 上限そのものはプロジェクト設定 → オーディオ → 最大実音声数 (Max Real Voices) で変えられますが、CPU コストが増えます。
#endregion
