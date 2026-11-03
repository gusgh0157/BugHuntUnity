using System.IO;
using BugHunt.Labs.Lab10;
using BugHunt.Labs.Lab11;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BugHunt.Editor
{
    // 랩이 쓰는 에셋(소리, 아이콘, 머티리얼, 프리팹, 애니메이션, 씬)을 코드로 만든다.
    // 저장소에는 결과물이 이미 들어 있으므로 평소에는 실행할 필요가 없다. 버그가 심어진 값(랩 10 프리팹의 100, 랩 12 의 Preload 끔,
    // 랩 22 의 Exit Time 0.7)도 여기서 정해진다. 학습자가 고친 에셋을 원래대로 되돌리려면 git restore 를 쓰는 편이 낫다.
    public static class BugHuntAssetBuilder
    {
        const string Root = "Assets/BugHunt/Resources/BugHunt";

        [MenuItem("BugHunt/개발용/랩 에셋 다시 만들기")]
        static void BuildFromMenu()
        {
            if (EditorUtility.DisplayDialog("랩 에셋 다시 만들기", "Resources/BugHunt 아래 에셋과 BugHunt 씬을 덮어씁니다. 고친 에셋도 버그 상태로 돌아갑니다.", "덮어쓰기", "취소"))
            {
                BuildAll();
            }
        }

        // 명령줄: Unity.exe -batchmode -quit -projectPath . -executeMethod BugHunt.Editor.BugHuntAssetBuilder.BuildAll
        public static void BuildAll()
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            EditorSettings.serializationMode = SerializationMode.ForceText;

            foreach (string folder in new[] { Root, $"{Root}/Audio", $"{Root}/Icons", $"{Root}/Materials", $"{Root}/Animation", "Assets/Scenes" })
            {
                Directory.CreateDirectory(folder);
            }

            BuildAudio();
            BuildIcons();
            BuildMaterials();
            BuildSlimePrefab();
            BuildWeaponData();
            BuildAnimation();
            BuildScene();
            AssetDatabase.SaveAssets();
            Debug.Log($"{Report.Tag} 랩 에셋을 만들었습니다.");
        }

        // ---------------------------------------------------------------- 소리
        static void BuildAudio()
        {
            WriteTone("lab12_blaster", 0.3f, 880f, 660f);
            WriteTone("lab13_footsteps", 0.4f, 120f, 90f);
            WriteTone("lab13_boss", 3f, 160f, 140f);
            WriteTone("lab14_explosion", 1.5f, 70f, 40f);
            WriteTone("lab15_briefing", 3f, 440f, 440f);
            WriteTone("lab15_grunt", 0.3f, 300f, 200f);
            AssetDatabase.Refresh();

            // 랩 12 의 버그: 메모리를 아끼려고 미리 로드를 껐다.
            var importer = (AudioImporter)AssetImporter.GetAtPath($"{Root}/Audio/lab12_blaster.wav");
            importer.loadInBackground = true;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.preloadAudioData = false;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        // 16비트 모노 WAV. 주파수가 from → to 로 미끄러지고 끝으로 갈수록 작아지는 소리.
        static void WriteTone(string name, float seconds, float fromHz, float toHz)
        {
            const int SampleRate = 44100;
            int samples = Mathf.RoundToInt(seconds * SampleRate);
            using var stream = new FileStream($"{Root}/Audio/{name}.wav", FileMode.Create);
            using var writer = new BinaryWriter(stream);
            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + samples * 2);
            writer.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(SampleRate);
            writer.Write(SampleRate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(samples * 2);
            double phase = 0;
            for (int i = 0; i < samples; ++i)
            {
                float t = (float)i / samples;
                float hz = Mathf.Lerp(fromHz, toHz, t);
                phase += 2 * Mathf.PI * hz / SampleRate;
                float envelope = Mathf.Min(1f, i / 200f) * (1f - t);
                writer.Write((short)(Mathf.Sin((float)phase) * envelope * 0.5f * short.MaxValue));
            }
        }

        // ---------------------------------------------------------------- 아이콘
        static void BuildIcons()
        {
            WriteIcon("sword", new Color(0.75f, 0.8f, 0.9f));
            WriteIcon("potion", new Color(0.9f, 0.2f, 0.3f));
            WriteIcon("shield", new Color(0.3f, 0.5f, 0.9f));
            AssetDatabase.Refresh();
        }

        static void WriteIcon(string itemId, Color color)
        {
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var pixels = new Color[64 * 64];
            for (int y = 0; y < 64; ++y)
            {
                for (int x = 0; x < 64; ++x)
                {
                    bool border = x < 4 || y < 4 || x >= 60 || y >= 60;
                    pixels[y * 64 + x] = border ? Color.black : color;
                }
            }
            texture.SetPixels(pixels);
            File.WriteAllBytes($"{Root}/Icons/icon_{itemId}.png", texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        // ---------------------------------------------------------------- 머티리얼
        static void BuildMaterials()
        {
            var crate = new Material(Shader.Find("Standard")) { color = Color.white };
            AssetDatabase.CreateAsset(crate, $"{Root}/Materials/Crate.mat");
        }

        // ---------------------------------------------------------------- 랩 10: 패치 전 값이 저장된 프리팹
        static void BuildSlimePrefab()
        {
            var go = new GameObject("Slime");
            var stats = go.AddComponent<SlimeStats>();
            var serialized = new SerializedObject(stats);
            serialized.FindProperty(nameof(SlimeStats.MaxHealth)).intValue = 100;   // 1.0 때 만든 프리팹
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(go, $"{Root}/Slime.prefab");
            Object.DestroyImmediate(go);
        }

        // ---------------------------------------------------------------- 랩 11
        static void BuildWeaponData()
        {
            var rifle = ScriptableObject.CreateInstance<WeaponData>();
            AssetDatabase.CreateAsset(rifle, $"{Root}/Rifle.asset");
        }

        // ---------------------------------------------------------------- 애니메이션
        static void BuildAnimation()
        {
            string dir = $"{Root}/Animation";

            // 랩 20: Idle --(Attack 트리거)--> Attack --(끝나면)--> Idle
            AnimationClip knightIdle = Clip($"{dir}/Knight_Idle.anim", "Body", 1f, true, 0f, 0.05f);
            AnimationClip knightAttack = Clip($"{dir}/Knight_Attack.anim", "Body", 0.6f, false, 0f, 0.5f);
            var knight = AnimatorController.CreateAnimatorControllerAtPath($"{dir}/Knight.controller");
            knight.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            AnimatorStateMachine knightSm = knight.layers[0].stateMachine;
            AnimatorState idle = knightSm.AddState("Idle");
            idle.motion = knightIdle;
            AnimatorState attack = knightSm.AddState("Attack");
            attack.motion = knightAttack;
            knightSm.defaultState = idle;
            AnimatorStateTransition toAttack = idle.AddTransition(attack);
            toAttack.hasExitTime = false;
            toAttack.duration = 0.05f;
            toAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            AnimatorStateTransition attackDone = attack.AddTransition(idle);
            attackDone.hasExitTime = true;
            attackDone.exitTime = 1f;
            attackDone.duration = 0.1f;

            // 랩 21: 걷기 1초, 발소리 이벤트 0.25초·0.75초
            AnimationClip walk = Clip($"{dir}/Guard_Walk.anim", "Body", 1f, true, 0f, 0.1f);
            AnimationUtility.SetAnimationEvents(walk, new[]
            {
                new AnimationEvent { time = 0.25f, functionName = "Footstep" },
                new AnimationEvent { time = 0.75f, functionName = "Footstep" },
            });
            var guard = AnimatorController.CreateAnimatorControllerAtPath($"{dir}/Guard.controller");
            guard.layers[0].stateMachine.AddState("Walk").motion = walk;

            // 랩 22: Swing 1초, Hit 이벤트 0.9초. 버그: Swing → Idle 이 0.7 에 시작해 0.8 에 끝난다.
            AnimationClip swordIdle = Clip($"{dir}/Swordsman_Idle.anim", "Blade", 1f, true, 0f, 0f);
            AnimationClip swing = Clip($"{dir}/Swordsman_Swing.anim", "Blade", 1f, false, 0f, 1f);
            AnimationUtility.SetAnimationEvents(swing, new[]
            {
                new AnimationEvent { time = 0.9f, functionName = "Hit" },
            });
            var swordsman = AnimatorController.CreateAnimatorControllerAtPath($"{dir}/Swordsman.controller");
            swordsman.AddParameter("Swing", AnimatorControllerParameterType.Trigger);
            AnimatorStateMachine swordSm = swordsman.layers[0].stateMachine;
            AnimatorState swordIdleState = swordSm.AddState("Idle");
            swordIdleState.motion = swordIdle;
            AnimatorState swingState = swordSm.AddState("Swing");
            swingState.motion = swing;
            swordSm.defaultState = swordIdleState;
            AnimatorStateTransition toSwing = swordIdleState.AddTransition(swingState);
            toSwing.hasExitTime = false;
            toSwing.duration = 0f;
            toSwing.AddCondition(AnimatorConditionMode.If, 0, "Swing");
            AnimatorStateTransition swingDone = swingState.AddTransition(swordIdleState);
            swingDone.hasExitTime = true;
            swingDone.exitTime = 0.7f;
            swingDone.hasFixedDuration = false;
            swingDone.duration = 0.1f;

            // 랩 23: 손이 0.5초 주기로 좌우로 흔들린다
            var wave = new AnimationClip { name = "Waver_Wave" };
            wave.SetCurve("Hand", typeof(Transform), "m_LocalPosition.x",
                new AnimationCurve(new Keyframe(0f, -1f), new Keyframe(0.25f, 1f), new Keyframe(0.5f, -1f)));
            wave.SetCurve("Hand", typeof(Transform), "m_LocalPosition.y", AnimationCurve.Constant(0f, 0.5f, 1f));
            wave.SetCurve("Hand", typeof(Transform), "m_LocalPosition.z", AnimationCurve.Constant(0f, 0.5f, 0f));
            SetLoop(wave, true);
            AssetDatabase.CreateAsset(wave, $"{dir}/Waver_Wave.anim");
            var waver = AnimatorController.CreateAnimatorControllerAtPath($"{dir}/Waver.controller");
            waver.layers[0].stateMachine.AddState("Wave").motion = wave;
        }

        // path 자식의 localPosition.y 를 from → to 로 움직이는 클립
        static AnimationClip Clip(string assetPath, string bonePath, float length, bool loop, float from, float to)
        {
            var clip = new AnimationClip { name = Path.GetFileNameWithoutExtension(assetPath) };
            clip.SetCurve(bonePath, typeof(Transform), "m_LocalPosition.x", AnimationCurve.Constant(0f, length, 0f));
            clip.SetCurve(bonePath, typeof(Transform), "m_LocalPosition.y", AnimationCurve.Linear(0f, from, length, to));
            clip.SetCurve(bonePath, typeof(Transform), "m_LocalPosition.z", AnimationCurve.Constant(0f, length, 0f));
            SetLoop(clip, loop);
            AssetDatabase.CreateAsset(clip, assetPath);
            return clip;
        }

        static void SetLoop(AnimationClip clip, bool loop)
        {
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        // ---------------------------------------------------------------- 씬
        static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var runner = new GameObject("LabRunner");
            runner.AddComponent<LabRunner>();
            const string path = "Assets/Scenes/BugHunt.unity";
            EditorSceneManager.SaveScene(scene, path);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
        }
    }
}
