using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace BugHunt
{
    // 랩 판정에 쓰는 조회 함수들. 여기에는 버그가 없다.
    // ラボの判定に使う照会関数群。ここにはバグはありません。
    public static class LabUtil
    {
        static MaterialPropertyBlock probeBlock;

        // 렌더러가 실제로 그릴 _Color. MaterialPropertyBlock 에 _Color 가 있으면 그것이, 없으면 머티리얼의 값이 이긴다.
        // レンダラーが実際に描く _Color。MaterialPropertyBlock に _Color があればそれが、なければマテリアルの値が優先されます。
        public static Color ShownColor(Renderer renderer)
        {
            if (renderer.HasPropertyBlock())
            {
                probeBlock ??= new MaterialPropertyBlock();
                renderer.GetPropertyBlock(probeBlock);
                if (probeBlock.HasColor("_Color"))
                {
                    return probeBlock.GetColor("_Color");
                }
            }
            return renderer.sharedMaterial.color;
        }

        public static bool SameColor(Color a, Color b, float tolerance = 0.01f)
        {
            return Mathf.Abs(a.r - b.r) <= tolerance
                && Mathf.Abs(a.g - b.g) <= tolerance
                && Mathf.Abs(a.b - b.b) <= tolerance;
        }

        // 지금 이 클립을 재생 중인 AudioSource 가 하나라도 있나
        // 今このクリップを再生中の AudioSource が 1 つでもあるか
        public static bool IsClipPlayingAnywhere(AudioClip clip)
        {
            foreach (var source in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                if (source.clip == clip && source.isPlaying)
                {
                    return true;
                }
            }
            return false;
        }

        // Project Settings → Editor → Enter Play Mode Settings 를 사람이 읽을 수 있게 (랩 34)
        // プロジェクト設定 → エディター → 再生モード開始時設定 を人が読める形に (ラボ 34)
        public static string EnterPlayModeDescription()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorSettings.enterPlayModeOptionsEnabled)
            {
                return Loc.T("기본값 (Domain Reload 켜짐, Scene Reload 켜짐)");
            }
            var options = UnityEditor.EditorSettings.enterPlayModeOptions;
            bool domain = (options & UnityEditor.EnterPlayModeOptions.DisableDomainReload) == 0;
            bool scene = (options & UnityEditor.EnterPlayModeOptions.DisableSceneReload) == 0;
            return Loc.F("Domain Reload {0}, Scene Reload {1}", Loc.T(domain ? "켜짐" : "꺼짐"), Loc.T(scene ? "켜짐" : "꺼짐"));
#else
            return Loc.T("플레이어 빌드 (해당 없음)");
#endif
        }

        // 네이티브 컬렉션 안전 검사(AtomicSafetyHandle)가 켜져 있나. 에디터에서는 켜져 있고, 개발 빌드가 아닌 플레이어에서는 꺼진다.
        // ネイティブコレクションの安全チェック (AtomicSafetyHandle) がオンか。エディターではオンで、開発ビルドではないプレイヤーではオフになります。
        public static bool NativeSafetyChecksEnabled
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                return true;
#else
                return false;
#endif
            }
        }

        // 기록해 둔 NativeArray 복사본 가운데 아직 Dispose 되지 않은 버퍼 수.
        // 복사본은 원본과 같은 안전 핸들을 공유하므로, 원본(또는 다른 복사본)을 Dispose 하면 복사본의 핸들도 무효가 된다.
        // 記録しておいた NativeArray のコピーのうち、まだ Dispose されていないバッファーの数。
        // コピーは元と同じ安全ハンドルを共有するので、元 (または別のコピー) を Dispose するとコピーのハンドルも無効になります。
        public static int CountAliveNativeBuffers<T>(IEnumerable<NativeArray<T>> copies) where T : struct
        {
            int alive = 0;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            foreach (NativeArray<T> copy in copies)
            {
                AtomicSafetyHandle handle = NativeArrayUnsafeUtility.GetAtomicSafetyHandle(copy);
                if (copy.IsCreated && AtomicSafetyHandle.IsHandleValid(in handle))
                {
                    ++alive;
                }
            }
#endif
            return alive;
        }
    }
}
