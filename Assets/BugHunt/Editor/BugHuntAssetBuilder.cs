using System.IO;
using BugHunt.Labs.Lab10;
using BugHunt.Labs.Lab11;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BugHunt.Editor
{
    // 랩이 쓰는 에셋(소리, 아이콘, 머티리얼, 프리팹, 애니메이션, 모델 메시, 씬)을 코드로 만든다.
    // 저장소에는 결과물이 이미 들어 있으므로 평소에는 실행할 필요가 없다. 버그가 심어진 값(랩 10 프리팹의 100, 랩 40 의 Preload 끔,
    // 랩 50 의 Exit Time 0.7)도 여기서 정해진다. 학습자가 고친 에셋을 원래대로 되돌리려면 git restore 를 쓰는 편이 낫다.
    // ラボが使うアセット (音、アイコン、マテリアル、プレハブ、アニメーション、モデルのメッシュ、シーン) をコードで作ります。
    // リポジトリには生成物がすでに入っているので、普段は実行する必要はありません。バグが仕込まれた値 (ラボ 10 のプレハブの 100、ラボ 40 の Preload オフ、
    // ラボ 50 の 終了時間 0.7) もここで決まります。学習者が直したアセットを元に戻すには git restore を使うほうがよいです。
    public static class BugHuntAssetBuilder
    {
        const string Root = "Assets/BugHunt/Resources/BugHunt";

        [MenuItem(MenuPaths.RebuildAssets)]
        static void BuildFromMenu()
        {
            if (EditorUtility.DisplayDialog(Loc.L("랩 에셋 다시 만들기", "ラボのアセットを作り直す"), Loc.L("Resources/BugHunt 아래 에셋과 BugHunt 씬을 덮어씁니다. 고친 에셋도 버그 상태로 돌아갑니다.", "Resources/BugHunt 以下のアセットと BugHunt シーンを上書きします。修正したアセットもバグのある状態に戻ります。"), Loc.L("덮어쓰기", "上書き"), Loc.L("취소", "キャンセル")))
            {
                BuildAll();
            }
        }

        // 명령줄: Unity.exe -batchmode -quit -projectPath . -executeMethod BugHunt.Editor.BugHuntAssetBuilder.BuildAll
        // コマンドライン: Unity.exe -batchmode -quit -projectPath . -executeMethod BugHunt.Editor.BugHuntAssetBuilder.BuildAll
        public static void BuildAll()
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            EditorSettings.serializationMode = SerializationMode.ForceText;

            foreach (string folder in new[] { Root, $"{Root}/Audio", $"{Root}/Icons", $"{Root}/Materials", $"{Root}/Animation", $"{Root}/Models", "Assets/Scenes" })
            {
                Directory.CreateDirectory(folder);
            }

            BuildAudio();
            BuildIcons();
            BuildMaterials();
            BuildSlimePrefab();
            BuildWeaponData();
            BuildAnimation();
            BuildModels();
            BuildScene();
            AssetDatabase.SaveAssets();
            Debug.Log($"{Report.Tag} {Loc.L("랩 에셋을 만들었습니다.", "ラボのアセットを作りました。")}");
        }

        // ---------------------------------------------------------------- 소리 / 音
        // 소리만 다시 만들기: Unity.exe -batchmode -quit -projectPath . -executeMethod BugHunt.Editor.BugHuntAssetBuilder.BuildAudio
        // 파일마다 음 높이를 달리해 소리끼리 구분된다. 길이는 판정에 쓰인다(랩 42: 0.2초 뒤, 랩 43: 1초 뒤 재생 중이어야 함).
        // 音だけ作り直す: Unity.exe -batchmode -quit -projectPath . -executeMethod BugHunt.Editor.BugHuntAssetBuilder.BuildAudio
        // ファイルごとに音の高さを変えてあるので、音同士を聞き分けられます。長さは判定に使われます (ラボ 42: 0.2 秒後、ラボ 43: 1 秒後に再生中であること)。
        public static void BuildAudio()
        {
            WriteTone("blaster", 0.3f, 880f);
            WriteTone("footsteps", 0.4f, 220f);
            WriteTone("boss", 3f, 330f);
            WriteTone("explosion", 1.5f, 110f);
            WriteTone("briefing", 3f, 440f);
            WriteTone("grunt", 0.3f, 660f);
            AssetDatabase.Refresh();

            // 랩 40 의 버그: 메모리를 아끼려고 미리 로드를 껐다.
            // ラボ 40 のバグ: メモリを節約しようとして事前ロードをオフにした。
            var importer = (AudioImporter)AssetImporter.GetAtPath($"{Root}/Audio/blaster.wav");
            importer.loadInBackground = true;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.preloadAudioData = false;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        // 16비트 모노 WAV. 고정 주파수 하나의 단일음, 일정한 볼륨. 클릭 소리가 나지 않게 앞뒤 10ms 만 페이드.
        // 16 ビットモノラル WAV。固定周波数 1 つの単音で、音量は一定。クリック音が出ないよう前後 10ms だけフェード。
        static void WriteTone(string name, float seconds, float hz)
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
            const float Volume = 0.3f;
            const int FadeSamples = SampleRate / 100;   // 10ms
            for (int i = 0; i < samples; ++i)
            {
                float envelope = Mathf.Min(1f, Mathf.Min(i, samples - 1 - i) / (float)FadeSamples);
                double phase = 2 * System.Math.PI * hz * i / SampleRate;
                writer.Write((short)(System.Math.Sin(phase) * envelope * Volume * short.MaxValue));
            }
        }

        // ---------------------------------------------------------------- 아이콘 / アイコン
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

        // ---------------------------------------------------------------- 머티리얼 / マテリアル
        static void BuildMaterials()
        {
            var crate = new Material(Shader.Find("Standard")) { color = Color.white };
            AssetDatabase.CreateAsset(crate, $"{Root}/Materials/Crate.mat");
        }

        // ---------------------------------------------------------------- 랩 23: 피벗이 가운데인 모델 / ラボ 23: ピボットが真ん中のモデル
        // 모델만 다시 만들기: Unity.exe -batchmode -quit -projectPath . -executeMethod BugHunt.Editor.BugHuntAssetBuilder.BuildModels
        // 랩 23 의 버그 재료: 외주 모델처럼 피벗(메시 원점)이 경첩이나 발이 아니라 메시 한가운데에 있다.
        // モデルだけ作り直す: Unity.exe -batchmode -quit -projectPath . -executeMethod BugHunt.Editor.BugHuntAssetBuilder.BuildModels
        // ラボ 23 のバグの材料: 外注のモデルのように、ピボット (メッシュの原点) がちょうつがいや足ではなくメッシュの真ん中にある。
        public static void BuildModels()
        {
            Directory.CreateDirectory($"{Root}/Models");
            AssetDatabase.Refresh();
            WriteBoxMesh("Gate", new Vector3(3f, 4f, 0.3f));
            WriteBoxMesh("Statue", new Vector3(1f, 2f, 1f));
            AssetDatabase.SaveAssets();
        }

        // 원점이 가운데인 상자. 면마다 정점을 따로 두어 법선이 면을 향한다.
        // 原点が真ん中の箱。面ごとに頂点を別に持たせて、法線が面を向くようにする。
        static void WriteBoxMesh(string name, Vector3 size)
        {
            Vector3 h = size * 0.5f;
            Vector3[] normals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            var vertices = new System.Collections.Generic.List<Vector3>();
            var faceNormals = new System.Collections.Generic.List<Vector3>();
            var triangles = new System.Collections.Generic.List<int>();
            foreach (Vector3 n in normals)
            {
                Vector3 u = Mathf.Abs(n.y) > 0.5f ? Vector3.right : Vector3.up;
                Vector3 v = Vector3.Cross(n, u);
                int start = vertices.Count;
                foreach (var (a, b) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) })
                {
                    vertices.Add(Vector3.Scale(n + u * a + v * b, h));
                    faceNormals.Add(n);
                }
                triangles.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetNormals(faceNormals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            string path = $"{Root}/Models/{name}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
        }

        // ---------------------------------------------------------------- 랩 10: 패치 전 값이 저장된 프리팹 / ラボ 10: パッチ前の値が保存されたプレハブ
        static void BuildSlimePrefab()
        {
            var go = new GameObject("Slime");
            var stats = go.AddComponent<SlimeStats>();
            var serialized = new SerializedObject(stats);
            serialized.FindProperty(nameof(SlimeStats.MaxHealth)).intValue = 100;   // 1.0 때 만든 프리팹 / 1.0 のときに作ったプレハブ
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(go, $"{Root}/Slime.prefab");
            Object.DestroyImmediate(go);
        }

        // ---------------------------------------------------------------- 랩 11 / ラボ 11
        static void BuildWeaponData()
        {
            var rifle = ScriptableObject.CreateInstance<WeaponData>();
            AssetDatabase.CreateAsset(rifle, $"{Root}/Rifle.asset");
        }

        // ---------------------------------------------------------------- 애니메이션 / アニメーション
        static void BuildAnimation()
        {
            string dir = $"{Root}/Animation";

            // 랩 48: Idle --(Attack 트리거)--> Attack --(끝나면)--> Idle
            // ラボ 48: Idle --(Attack トリガー)--> Attack --(終わったら)--> Idle
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

            // 랩 49: 걷기 1초, 발소리 이벤트 0.25초·0.75초
            // ラボ 49: 歩き 1 秒、足音イベント 0.25 秒・0.75 秒
            AnimationClip walk = Clip($"{dir}/Guard_Walk.anim", "Body", 1f, true, 0f, 0.1f);
            AnimationUtility.SetAnimationEvents(walk, new[]
            {
                new AnimationEvent { time = 0.25f, functionName = "Footstep" },
                new AnimationEvent { time = 0.75f, functionName = "Footstep" },
            });
            var guard = AnimatorController.CreateAnimatorControllerAtPath($"{dir}/Guard.controller");
            guard.layers[0].stateMachine.AddState("Walk").motion = walk;

            // 랩 50: Swing 1초, Hit 이벤트 0.9초. 버그: Swing → Idle 이 0.7 에 시작해 0.8 에 끝난다.
            // ラボ 50: Swing 1 秒、Hit イベント 0.9 秒。バグ: Swing → Idle が 0.7 で始まり 0.8 で終わる。
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

            // 랩 51: 손이 0.5초 주기로 좌우로 흔들린다
            // ラボ 51: 手が 0.5 秒周期で左右に揺れる
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
        // path の子の localPosition.y を from → to へ動かすクリップ
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

        // ---------------------------------------------------------------- 씬 / シーン
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
