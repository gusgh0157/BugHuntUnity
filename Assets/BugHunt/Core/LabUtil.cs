using UnityEngine;

namespace BugHunt
{
    // 랩 판정에 쓰는 조회 함수들. 여기에는 버그가 없다.
    public static class LabUtil
    {
        static MaterialPropertyBlock probeBlock;

        // 렌더러가 실제로 그릴 _Color. MaterialPropertyBlock 에 _Color 가 있으면 그것이, 없으면 머티리얼의 값이 이긴다.
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
    }
}
