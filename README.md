# BugHunt Unity — 유니티 C# 버그 추적 실습 / Unity C# バグ追跡ラボ

한국어 또는 日本語를 펼쳐 읽으세요. / 韓国語または日本語を開いてお読みください。

<details>
<summary><b>한국어</b></summary>

[BugHunt](https://github.com/gusgh0157/BugHunt)(C++ 콘솔판)와 BugHunt UE(언리얼판)의 Unity 버전입니다.
버그가 **일부러 심어진** 작은 게임 코드 모음이고, 이번에는 **Unity 에서만 생기는** 버그들입니다.

- **1부: 크래시 / 예외 (랩 1~4)**: Console 에 빨간 줄이 뜬 순간, 스택 트레이스를 거슬러 올라가 원인을 찾습니다. `MissingReferenceException`, `GetComponent` 가 준 null, Awake/OnEnable/Start 순서, 꺼진 오브젝트의 코루틴.
- **2부: 흐름 추적 (랩 5~15)**: 게임은 멀쩡히 도는데 **결과가 틀립니다**. 이벤트 구독, 실행 순서, FixedUpdate, fake null, 구조체 복사, 직렬화된 값, ScriptableObject 처럼 "Unity 가 대신 해 주는 일"의 순서와 수명을 따라갑니다. 30fps 와 120fps 에서 결과가 다른 **프레임레이트 의존** 버그(빠진 `Time.deltaTime`, 매 프레임 `Lerp(a, b, 0.1f)`, Update 의 `fixedDeltaTime`, `timeScale = 0` 에서 멈추는 메뉴)도 여기서 다룹니다.
- **3부: 컨테이너 (랩 16~22)**: 인스펙터에서 줄인 배열, 반복 도중 다른 스크립트가 고친 목록, 복제하면 사라지는 `Dictionary`, 순서를 약속하지 않는 `FindObjectsByType`, 넘긴 List 를 비우는 API, 파괴된 키, 크기가 바뀐 `NativeList` 의 낡은 `AsArray`.
- **4부: 잘못된 참조와 네이티브 메모리 (랩 23~26)**: 예외 없이 새는 `NativeArray`, "분명히 Complete 했는데" 터지는 잡, 다음 프레임에 사라지는 `Allocator.Temp`, Domain Reload 를 끈 Play 에서 지난 판의 오브젝트를 붙잡는 정적 필드.
- **5부: 물리 (랩 27~31)**: 얇은 벽을 뚫는 빠른 탄환, 불리지 않는 `OnTriggerEnter`, 레이어 번호와 마스크, transform 으로 움직인 Rigidbody, 옮긴 직후 옛 자리를 보는 레이캐스트. Physics Debugger, Gizmos, `Debug.DrawRay` 로 봅니다.
- **6부: 사운드 (랩 32~35)**: (⚠️ 소리가 납니다. 볼륨을 낮추고 시작하세요.) 소리가 늦게 나고, 안 들리고, 끊기는 버그. 로드 상태·가상 음성·재생 위치를 재서 판정합니다.
- **7부: 이미지 / 렌더링 (랩 36~39)**: 엉뚱한 상자까지 빨개지고, 메모리가 새고, 아이콘이 바뀌고, 색이 물 빠지는 버그. 머티리얼 값, Material 객체 수, 텍스처 이름, 화면 픽셀로 판정합니다.
- **8부: 애니메이션 (랩 40~43)**: 공격 모션이 안 나오고, 화면 밖 발소리가 안 나고, 검격 판정이 빠지고, 손에 든 무기가 한 박자 늦는 버그.
- **9부: 동시성과 멈춤 (랩 44~50)**: `Task.Run` 두 개가 끼어들어 한정품이 두 번 팔리고, 워커 스레드에서 부른 `transform` 의 예외가 Task 안에 삼켜지고, `Awaitable.BackgroundThreadAsync()` 뒤 메인 스레드로 돌아오지 않는 버그. 매번 같은 결과가 나오게 재현합니다. 이어서 크래시 없이 에디터가 굳는 무한루프와 데드락. 모두 중단, 스레드 창, 메인 스레드의 호출 스택으로 갇힌 곳을 찾습니다. 연습 중에 정말로 굳지 않도록 `HangGuard` 가 2초 뒤 "응답 없음" 을 알리고 빠져나옵니다.
- **10부: 성능 (랩 51~55)**: 결과는 맞는데 느린 버그. 매 프레임 쌓이는 GC 쓰레기(문자열, LINQ, 박싱), Update 의 `Find`, 돌려주지 않는 오브젝트 풀, `SendMessage`, 배열을 돌려주는 API. Profiler 의 GC Alloc 열, Deep Profile, ProfilerMarker 로 찾고 `ProfilerRecorder` 로 바이트를 재서 판정합니다.

> 콘솔판·언리얼판과 같은 규칙입니다. **코드를 눈으로 훑어 정답을 찾지 마세요.** 1부는 **예외가 난 곳에서부터**, 2부부터는 **직접 멈춘 곳에서부터 값을 확인하며** 추적하세요.
> 콘솔판 1·3부를 먼저 끝냈다면 좋습니다. 호출 스택 창, 지역 창, 조건부 중단점, 추적점 사용법은 거기서 설명한 그대로 씁니다.

## BugHunt 시리즈

같은 방식(일부러 심은 버그를 디버거로 추적)으로 만든 네 가지 판입니다.

| 판 | 다루는 버그 | 저장소 |
|---|---|---|
| C++ 콘솔 (표준 C++20, CMake) | 호출 스택, 흐름 추적, 연산자, 컨테이너, 언어 함정, 인터페이스와 상속, 파일 입출력, 직렬화, 포인터와 메모리, 비동기와 동시성, 멈춤, 성능 (랩 98개 + 챌린지 11개) | [BugHunt](https://github.com/gusgh0157/BugHunt) |
| C# 콘솔 (.NET 10) | 🚧 **공사중** — 예외, 흐름 추적, 컨테이너, 언어 함정, 인터페이스와 상속, 파일 입출력, 직렬화, 비동기와 동시성, 리소스와 메모리, unsafe, 멈춤, 성능 (랩 95개 + 챌린지 11개). 부하 아래 결정성·검토 수정은 PR 진행 중 | [BugHuntCS](https://github.com/gusgh0157/BugHuntCS) |
| 언리얼 엔진 (UE 5.8) | 🚧 **공사중** — 크래시, 흐름 추적, 멀티플레이, 사운드, 이미지, 애니메이션 (공개된 master는 랩 21개). 컨테이너, 포인터와 수명, 물리, 프레임레이트, 트랜스폼, 멈춤, 성능, 채점기, 일본어판은 작업 브랜치에서 진행 중 | [BugHuntUE](https://github.com/gusgh0157/BugHuntUE) |
| 유니티 (Unity 6) | 🚧 **공사중** — 크래시/예외, 흐름 추적(프레임레이트 포함), 컨테이너, 참조와 네이티브 메모리, 물리, 사운드, 이미지, 애니메이션, 멈춤, 성능, 데이터 레이스 (랩 55개). 트랜스폼 부 추가와 재배치는 작업 브랜치에서 진행 중 | [BugHuntUnity](https://github.com/gusgh0157/BugHuntUnity) **(지금 보는 저장소)** |

## 버그 범주 커버리지

이 판이 다루는 범주와 랩입니다. **미포함** 은 Unity 에서 흔한 대표 사례인데 아직 랩이 없다는 뜻입니다.

| 범주 | 랩 | 다루는 사례 | 미포함 (대표 사례) |
|---|---|---|---|
| 참조·수명·fake null | 1, 2, 4, 8, 21, 34 | 파괴된 오브젝트 접근, `GetComponent` 범위, 꺼진 오브젝트의 코루틴, `?.`·`??` 와 fake null, 오브젝트와 함께 사라지는 소리, Dictionary 의 파괴된 키 | `DontDestroyOnLoad` 싱글턴 중복, 씬 전환 뒤 끊긴 참조 |
| 흐름·순서 | 3, 5, 6, 7, 43 | Awake/OnEnable/Start 순서, 이벤트 구독 해제, Update 끼리 순서, Update/FixedUpdate 횟수, 애니메이션 평가 뒤 LateUpdate | `LoadSceneAsync` 의 활성화 순서, `OnDisable`/`OnDestroy` 순서 |
| C# 값과 참조 | 9, 20 | 구조체 복사(`List<Vector3>[i]`), 돌려쓰는 버퍼를 두 곳이 공유 | 클로저가 반복 변수를 캡처 |
| 직렬화·에셋 | 10, 11, 18, 36 | 직렬화 값이 초기값을 덮어씀, 공유되는 ScriptableObject, `sharedMaterial`, 직렬화되지 않는 컨테이너 | `[SerializeReference]` 다형성, 프리팹 오버라이드, 필드 이름 변경(`FormerlySerializedAs`) |
| 프레임레이트 의존 | 7, 12, 13, 14, 15 | 한 프레임짜리 입력을 FixedUpdate 가 놓침, `Time.deltaTime` 누락, 매 프레임 `Lerp(a, b, 0.1f)`, `fixedDeltaTime` 혼동, `timeScale = 0` 에서 멈추는 UI | Update 에서 `AddForce`(물리 스텝과 어긋남), 가변 deltaTime 의 큰 스파이크(첫 프레임, 로딩 직후) |
| 컨테이너 | 16, 17, 18, 19, 20, 21, 22 | 인스펙터에서 줄인 배열, 반복 중 수정, 직렬화 손실, `Find`/`GetComponents` 순서, 결과 재사용 API, 파괴된 키, 크기가 바뀐 `NativeList` 의 `AsArray` | 키로 쓴 객체의 해시가 바뀜, `List.Sort` 의 불안정 정렬 |
| 네이티브 메모리·잡 | 22, 23, 24, 25 | `Dispose` 누락, 잡 의존 사슬, `Allocator.Temp` 수명, 낡은 `AsArray` | Burst 에서만 다른 결과 |
| 정적 상태·Domain Reload | 26 | Domain Reload 를 끈 Play 의 정적 필드 | 정적 이벤트 구독 누적 |
| 물리 | 27, 28, 29, 30, 31 | 빠른 탄환 터널링, Rigidbody 없는 트리거, 레이어 번호와 마스크, transform 으로 움직인 Rigidbody 와 보간, `SyncTransforms` | 관절(Joint) 폭주, 비균일 스케일 콜라이더, `OnCollisionStay` 비용 |
| 사운드 | 32, 33, 34, 35 | 로드 지연, 가상 음성, 오브젝트 수명, `Play`/`PlayOneShot` | AudioMixer 그룹·스냅샷, 3D 감쇠 거리 |
| 이미지·렌더링 | 36, 37, 38, 39 | 공유 머티리얼, 머티리얼 복제 누수, 늦게 도착한 비동기 로드, Linear/Gamma | 빌드에서 빠진 셰이더 변형(분홍), 투명 정렬, `RenderTexture` 해제 |
| 애니메이션 | 40, 41, 42, 43 | 파라미터 오타, Culling Mode, 트랜지션과 이벤트, 평가 순서 | Root Motion 과 스크립트 이동 충돌, Write Defaults |
| 데이터 레이스·스레드 | 44, 45, 46, 47 | 메인 스레드에서 Task 를 막고 기다림, check-then-act 레이스, 워커 스레드의 Unity API 와 삼켜진 예외, `await` 뒤 실행 스레드 | 잡 안의 static 공유 상태, `volatile` 없는 종료 플래그, 여러 스레드가 함께 쓰는 캐시 `Dictionary` |
| 멈춤 | 47, 48, 49, 50 | yield 없는 while, 방문 체크 없는 BFS, `.Result` 데드락, float 누적 종료 조건 | `lock` 순서가 엇갈린 데드락 |
| 성능·GC | 37, 51, 52, 53, 54, 55 | 머티리얼 복제 누수, 매 프레임 GC 할당(문자열·LINQ·박싱), Update 의 `Find`/`FindObjectsByType`, 돌려주지 않는 풀, `SendMessage`, 배열을 돌려주는 API | 렌더링 비용(배칭 깨짐, 과도한 그림자), 큰 에셋 동기 로드로 멈춤, 매 프레임 Canvas 재빌드 |
| UI | 미포함 | | 레이캐스트를 가로막는 투명 이미지, Canvas 재빌드, 해상도별 앵커 |
| 입력 | 미포함 | | Input System 액션 맵 비활성, 포커스를 잃었을 때 눌린 채 남는 키 |
| 빌드 전용 | 미포함 | | IL2CPP 코드 스트리핑, 플랫폼별 경로·대소문자 |
| 멀티플레이 | 미포함 | | [부록 A](#부록-a-멀티플레이netcode-for-gameobjects는) 참고 |

<details>
<summary>예전 랩 번호 → 새 번호 (부 순서를 기초 → 심화로 바꾸기 전에 받은 경우)</summary>

| 예전 번호 | 새 번호 | 내용 |
|---|---|---|
| 1~11 | 1~11 (그대로) | 크래시/예외, 흐름 추적 |
| 12~15 | 32~35 | 사운드 |
| 16~19 | 36~39 | 이미지 / 렌더링 |
| 20~23 | 40~43 | 애니메이션 |
| 24~27 | 23~26 | 잘못된 참조와 네이티브 메모리 |
| 28, 29, 30, 31 | 48, 49, 47, 50 | 멈춤 (엘리베이터, 던전 길 찾기, 상점의 금화, 리플레이 빨리감기) |
| 32~38 | 16~22 | 컨테이너 |
| 39~43 | 27~31 | 물리 |
| 44~47 | 12~15 | 프레임레이트 의존 (2부로 들어감) |
| 48~52 | 51~55 | 성능 |
| 53~55 | 44~46 | 데이터 레이스와 스레드 |

파일 이름(`LabNN_*.cs`), 클래스·네임스페이스(`BugHunt.Labs.LabNN`), 테스트 이름도 새 번호를 따릅니다. 사운드 클립은 이름에서 번호를 뺐습니다(`lab12_blaster.wav` → `blaster.wav`).

</details>

## 준비

필요한 것: **Unity 6000.3.27f2** (Unity 6.3 LTS), **Visual Studio 2022 이상**(워크로드 "Unity를 사용한 게임 개발") 또는 **Rider**.

1. Unity Hub → **Add** → 이 폴더(`BugHuntUnity`)를 고릅니다. 처음 열 때 Library 를 만드느라 몇 분 걸립니다.
2. **Edit → Preferences → External Tools → External Script Editor** 를 Visual Studio 또는 Rider 로 정합니다.
3. **Assets → Open C# Project** 로 IDE 를 엽니다(솔루션 파일은 Unity 가 만들어 줍니다).
4. 에디터 오른쪽 아래 상태 표시줄의 **벌레 아이콘(Code Optimization)** 을 눌러 **Debug Mode** 로 바꿉니다. Release Mode 에서는 중단점이 안 걸리거나 변수가 "최적화되어 사용할 수 없음"으로 보입니다. (디버거를 붙이면 Unity 가 바꿀지 물어보기도 합니다.)

> 렌더 파이프라인은 기본(Built-in)이고, 색 공간은 **Linear** 입니다(랩 39). 패키지는 Test Framework 와 IDE 연동만 씁니다.

### 디버거를 붙이는 방법

| 방법 | 하는 법 | 언제 |
|---|---|---|
| Visual Studio | 도구 모음의 **Unity에 연결(Attach to Unity)** ▶, 또는 **디버그 → Unity 디버거 연결** → 목록에서 에디터 선택 | 기본 |
| Rider | 오른쪽 위 Run 구성 **Attach to Unity Editor** → 벌레 버튼 | Rider 사용자 |
| 붙인 뒤 Play 까지 한 번에 | VS: **Attach to Unity and Play** / Rider: **Attach to Unity Editor & Play** | 매번 Play 누르기 귀찮을 때 |

- Unity 의 C# 디버거는 **이미 켜져 있는 에디터에 붙는** 방식입니다. 언리얼처럼 IDE 에서 에디터를 띄우지 않습니다. 붙인 상태로 Play 를 누르면 중단점이 걸립니다.
- 디버거가 붙어 있을 때 랩에서 예외가 나면 IDE 가 그 줄에서 멈춥니다. 안 멈춘다면 VS: **디버그 → 창 → 예외 설정** 에서 *Common Language Runtime Exceptions*, Rider: **Run → View Breakpoints → Any Exception** 의 "Break on unhandled" 를 확인하세요.
- 중단점에 멈춰 있는 동안 Unity 에디터는 **응답 없음** 상태가 됩니다. 정상입니다. IDE 에서 F5(계속)를 누르면 돌아옵니다.

## 랩 실행하기

1. 메뉴 **BugHunt → 랩 실행 창** (Ctrl+Alt+B).
2. 번호를 누르면 `Assets/Scenes/BugHunt.unity` 를 열고 Play 한 뒤 그 랩을 바로 실행합니다. (씬에서 그냥 Play 를 누르면 Game 창 왼쪽에 랩 목록 버튼이 나옵니다.)
   > ⚠️ **사운드 랩(6부, 랩 32~35)은 소리를 냅니다. 실행 전에 시스템 볼륨을 낮추고, 헤드폰을 쓴다면 귀에서 떼고 시작하세요.** 버튼에 "소리 주의" 가 붙어 있습니다.
3. 결과는 Game 창 오른쪽과 **Console 창**(Window → General → Console)에 나옵니다. Console 검색창에 `[BugHunt]` 를 넣으면 랩 결과만 보입니다.

```
[BugHunt] ==== 랩 5: 전투 기록 창 ====
[BugHunt] [FAIL] 다시 연 창의 기록 줄 수 (기대: 1, 실제: 2)
[BugHunt] [FAIL] 띄운 알림 총합 (10, 7) (기대: 2, 실제: 4)
[BugHunt] ==== 랩 5 결과: PASS 1 / FAIL 3 ====
```

**같은 랩을 계속 돌릴 때**: 코드를 고치고 Unity 로 돌아오면 자동으로 다시 컴파일됩니다. 그다음 **BugHunt → 마지막 랩 다시 실행 (Ctrl+Alt+R)**. 플레이 중이면 플레이를 멈추지 않고 바로 다시 돌립니다.

**다 고쳤으면**: **BugHunt → 채점 (모든 랩)** (Ctrl+Alt+G) 또는 명령줄의 `grade.bat` 으로 모든 랩을 한 번에 채점합니다. 아래 [채점](#채점) 을 보세요.

> ⚠ **플레이 중에 코드를 고치면** Unity 는 기본 설정(Preferences → General → Script Changes While Playing: *Recompile And Continue Playing*)에서 플레이 중에 다시 컴파일하고 정적 변수와 직렬화되지 않은 필드를 날립니다. 헷갈리면 **플레이를 멈추고 고치세요.**

### 언어 (한국어 / 日本語)

랩 실행 창, Console 의 판정 줄, 채점 결과는 한국어와 일본어로 볼 수 있습니다. 메뉴 이름은 두 언어를 나란히 씁니다(`랩 실행 창 · ラボ実行ウィンドウ`). 언어는 이 순서로 정해집니다.

1. 명령줄 `-bughuntLang ko` / `-bughuntLang ja` (채점: `grade.bat -Lang ja`)
2. 랩 실행 창 맨 위의 **언어 / 言語** 에서 고른 언어 (EditorPrefs 에 기억)
3. OS 언어가 일본어면 일본어
4. 그 밖에는 한국어

랩 코드의 주석과 접힌 힌트는 한 파일 안에 한국어 다음 일본어로 나란히 있습니다. 출력 문장은 코드에 한국어로 쓰여 있고, 그 문장이 `Assets/BugHunt/Lang/Ja/LabNN.cs` 의 일본어 표의 키입니다. `grade-report.json` 은 `title`·`detail` 만 고른 언어로 나오고 `result` 값과 키는 그대로입니다.

### Console 창 읽기

- 한 줄을 **클릭**하면 아래에 스택 트레이스가 나옵니다. **맨 위가 예외가 난 곳**, 아래로 갈수록 "누가 불렀나" 입니다.
- 파란 링크(`Assets/BugHunt/Labs/Lab01_Tower.cs:50`)를 누르면 IDE 에서 그 줄이 열립니다. 줄을 **더블클릭**해도 됩니다.
- `UnityEngine.` 으로 시작하는 줄은 엔진 코드, `BugHunt.Labs.` 로 시작하는 줄이 내 코드입니다. 엔진 줄은 건너뛰되 **어떤 엔진 기능을 거쳐 왔는지**(AddComponent, SetupCoroutine, 애니메이션 이벤트...)는 읽어 두세요. 그게 "왜 이 시점에 불렸나" 의 답입니다.
- 오른쪽 위 버튼: **Collapse**(같은 줄 묶기), **Error Pause**(에러가 나는 순간 플레이 일시정지), 그리고 로그/경고/에러 필터. 경고(⚠)를 꺼 두지 마세요(랩 40).

---

# 1부: 크래시 / 예외

## 추적 4단계 (콘솔판과 같음)

1. **어디서 멈췄나?** 스택 트레이스에서 **내 코드(`BugHunt.Labs.`)가 처음 나오는 줄**을 찾는다.
2. **무엇이 이상한가?** 지역/조사식 창에서 null, 파괴된 오브젝트, 이상한 값을 찾는다.
3. **그 값은 어디서 왔나?** 호출 스택을 내려가며 출처를 찾는다.
4. **진짜 원인은?** 이상한 값이 **처음 만들어진 곳**을 고친다.

```
기록표 (1부용)
멈춘 함수:              
이상한 값:              
그 값이 온 곳:           
거쳐 온 엔진 기능:        
진짜 원인:              
수정:                   
```

## Unity 예외 세 가지

| 예외 / 메시지 | 뜻 |
|---|---|
| `NullReferenceException` | C# 변수가 정말 비어 있다. `GetComponent`, `Find` 가 못 찾으면 조용히 null 을 준다 |
| `MissingReferenceException: The object of type 'X' has been destroyed but you are still trying to access it` | 변수는 그대로인데 **Destroy 된 오브젝트**를 가리킨다. C# 껍데기만 남은 상태 |
| `UnassignedReferenceException: The variable X of Y has not been assigned` | Inspector 에서 연결해야 할 필드를 비워 뒀다 |

## 따라 해 보기: 랩 1 (감시탑)

1. IDE 에서 Unity 에 디버거를 붙이고, **BugHunt → 랩 실행 창 → 1**.
2. 디버거가 앞으로 나오며 멈춥니다: **MissingReferenceException**. (디버거 없이 했다면 Console 의 빨간 줄을 클릭하세요.)
3. 호출 스택(VS: Ctrl+Alt+C) 맨 위가 `Tower.Update` 입니다. 그 아래가 없습니다: **Unity 가 매 프레임 부르는 Update** 라서 내 코드 중 "누가 불렀나"는 없습니다. → *멈춘 함수*
4. 조사식에 `Target` 을 넣고 펼칩니다. 이름도, Health 도 보이는데 `Target == null` 은 **true** 입니다. → *이상한 값*: 파괴된 적
5. 누가 파괴했는지 찾습니다. `Enemy.TakeDamage` 의 `Destroy(gameObject)` 줄에 **F9** 를 걸고 플레이를 멈췄다가 다시 랩 1. 처음 멈췄을 때 호출 스택에 누가 있나요? → *그 값이 온 곳*
6. 감시탑의 Update 는 "내가 쏴서 죽였을 때만" 표적을 바꿉니다. 남이 파괴한 경우는? → *진짜 원인*
7. 고친 뒤 **Ctrl+Alt+R** → `[PASS]` 두 개.
8. 보너스: 디버거 없이 **Console → Error Pause** 를 켜고 랩 1 을 돌려 보세요. 예외가 난 프레임에 플레이가 일시정지되어 Hierarchy 와 Inspector 로 그 순간을 볼 수 있습니다.

## 1부 랩 목록

| # | 이름 | 증상 | 새로 배우는 것 | 난이도 |
|---|---|---|---|---|
| 1 | 감시탑 | 폭탄이 적을 날리자 예외가 쏟아진다 | **MissingReferenceException**, 파괴된 오브젝트, Destroy 에 중단점, Error Pause | ★ |
| 2 | 헤드샷 | 몸통은 괜찮은데 머리를 쏘면 NullReferenceException | **GetComponent 는 자기 오브젝트만** 본다, `GetComponentInParent`, `TryGetComponent` | ★ |
| 3 | 점수판 | 매니저와 UI 를 만들자마자 NullReferenceException | **Awake / OnEnable / Start 순서**, AddComponent 안에서 바로 불리는 Awake·OnEnable | ★★ |
| 4 | 부활 | 쓰러진 플레이어가 영영 부활하지 않는다(에러 한 줄) | **꺼진 오브젝트의 코루틴**, 코루틴의 주인, "에러를 없애도 FAIL" | ★★ |

## 힌트 보는 법

콘솔판과 같습니다. 각 랩 `.cs` 파일 **맨 아래**에 `#region 힌트 N` 으로 접혀 있습니다.

1. 랩 파일을 처음 열면 **Ctrl+M, Ctrl+O**(VS) / **Ctrl+Shift+NumPad -**(Rider)로 전부 접습니다.
2. 막힐 때 **힌트 1**만 펼칩니다. 그래도 막히면 다음 힌트를 하나씩.

랩 코드는 `Assets/BugHunt/Labs/LabNN_*.cs` 입니다(랩 10·11 은 `Lab10/`, `Lab11/` 폴더에 클래스가 하나 더 있습니다). `Assets/BugHunt/Core/`(랩 실행기, 9부의 `HangGuard`, 10부의 `GcMeter`, 언어 전환 `Loc`)와 `Editor/`, `Lang/`, `Tests/` 에는 버그가 없습니다.
다 풀면 [SOLUTIONS.md](SOLUTIONS.md)로 맞춰 보세요.

---

# 2부: 흐름 추적 (크래시 없음)

콘솔판 3부와 같은 4단계입니다. Unity 에서는 **"내가 부르지 않았는데 불리는 코드"**(Update, FixedUpdate, 이벤트, 코루틴, 직렬화, 애니메이션 이벤트)가 대부분이라서, "언제, 누가, 몇 번 불렀나"를 보는 도구가 특히 중요합니다.

## 흐름 추적 4단계

1. **기대를 먼저 적는다.** "HP 바는 매 프레임 영웅 HP 와 같아야 한다"처럼.
2. **틀린 결과에서 가장 가까운 곳부터 멈춘다.**
3. **기대와 처음 달라지는 곳을 찾는다.** 매 프레임 도는 코드는 F10 대신 추적점, `Debug.Log($"{Time.frameCount} ...")`, Profiler 로 기록한다.
4. **"처음 달라진 곳"을 고친다.**

```
기록표 (2부부터)
틀린 결과:              
멈춘 위치 / 쓴 도구:     
기대값 → 실제값:         
처음 어긋난 곳:          
진짜 원인:              
수정:                   
```

## 2부에서 쓰는 도구

| 도구 | 하는 법 | 언제 |
|---|---|---|
| **Console + 프레임 번호** | `Debug.Log($"{Time.frameCount} hp={hp}", this)` — 두 번째 인자로 오브젝트를 넘기면 줄을 클릭할 때 Hierarchy 에서 그 오브젝트가 강조된다 | 모든 랩 |
| **추적점** | 중단점 우클릭 → 작업(Actions), `{Time.frameCount} {damage}` 같은 식, "실행 계속" 체크 | 코드를 고치지 않고 호출 순서·횟수 기록 (랩 5~7) |
| **조건부 중단점 / 적중 횟수** | 중단점 우클릭 → 조건, 예: `Time.frameCount > 100 && Health <= 0` | 매 프레임 불리는 함수의 특정 순간만 |
| **`Debug.Break()`** | 코드에 한 줄. 그 프레임이 끝나면 에디터가 **일시정지** | 디버거 없이 "그 순간" 의 Hierarchy/Inspector 를 보고 싶을 때. 일시정지 중 **다음 프레임(Ctrl+Alt+P)** 으로 한 프레임씩 |
| **Inspector Debug 모드** | Inspector 탭 ⋮ → **Debug** | private 필드와 인스턴스 ID 까지 보기 |
| **Script Execution Order** | Edit → Project Settings → Script Execution Order | 스크립트끼리 Update 순서 (랩 6) |
| **Profiler** | Window → Analysis → Profiler (Ctrl+7), CPU 모듈 → Hierarchy / **Timeline** | 한 프레임 안의 순서, 함수가 몇 번 불렸나(Calls) (랩 7, 43) |
| **Test Runner** | Window → General → Test Runner → PlayMode | 고친 랩을 한꺼번에 확인 |

## 따라 해 보기: 랩 5 (전투 기록 창)

1. **랩 실행 창 → 5**. 아무것도 멈추지 않고 `[FAIL] 다시 연 창의 기록 줄 수 (기대: 1, 실제: 2)`.
2. **기대를 적습니다.** 기사는 10, 5, 7, 3 피해를 받습니다. 창이 열려 있는 것은 10 과 7 을 받을 때뿐이므로 `HandleDamaged` 는 **두 번**(10, 7) 불려야 합니다.
3. `Lab05_CombatLog.cs` 의 `HandleDamaged` 첫 줄에 **F9**. 디버거를 붙이고 다시 랩 5.
4. 멈출 때마다 지역 창의 `damage` 와 조사식 `this.isActiveAndEnabled` 를 적고 F5. 몇 번 멈추나요?
5. 기대(10, 7)와 다르게 **처음** 멈춘 값이 무엇인지 보세요. 그때 창은 켜져 있었나요? → *처음 어긋난 곳*
6. 호출 스택에서 `Fighter.TakeHit` 을 더블클릭 → 조사식에 `Damaged.GetInvocationList().Length`. 켜기/끄기/다시 켜기/파괴 때마다 몇 개인가요?
7. 플레이를 멈추고 고친 뒤 Ctrl+Alt+R → [PASS] 네 개.

## 2부 랩 목록

| # | 이름 | 증상 | 새로 배우는 것 | 난이도 |
|---|---|---|---|---|
| 5 | 전투 기록 창 | 창을 다시 열면 기록이 두 줄씩, 창을 버린 뒤에도 알림 | **이벤트 구독 해제 누락**, OnEnable/OnDisable 짝, 추적점, `GetInvocationList` | ★ |
| 6 | HP 바 | HP 바가 매 프레임 한 칸씩 늦다 | **Update 끼리의 순서**, Script Execution Order, `[DefaultExecutionOrder]`, LateUpdate | ★★ |
| 7 | 점프 | 120Hz 모니터에서 점프가 씹힌다 | **Update 와 FixedUpdate 의 횟수 차이**, 한 프레임짜리 입력, Profiler Calls, `Time.captureDeltaTime` | ★★ |
| 8 | 궁수 | 쓰러진 허수아비로 점수를 계속 번다 | **fake null**, `==` 와 `?.` `??=` `is null` 의 차이, Destroy 는 프레임 끝 | ★★ |
| 9 | 순찰 경로 | 바닥에 맞췄다는 웨이포인트로 순찰병이 날아간다 | **구조체 복사**, `List<Vector3>[i].Set()`, `transform.position.x =` 가 컴파일 안 되는 이유 | ★★ |
| 10 | 밸런스 패치 | 코드에서 HP 를 150 으로 올렸는데 게임에선 100 | **직렬화된 값이 필드 초기값을 덮어씀**, 프리팹 YAML 읽기, 생성 → 역직렬화 → Awake 순서 | ★★ |
| 11 | 무기 데이터 | 남의 탄창이 줄고, 플레이를 멈춰도 줄어 있다 | **ScriptableObject 는 공유되는 에셋**, 플레이 모드 후에도 남는 값, `git status` 로 에셋 변경 확인 | ★★ |

## 프레임레이트 의존 (랩 12~15)

개발 PC 에서는 완벽한데 **다른 기기에서만** 빠르거나 느린 버그입니다. Update 는 1초에 몇 번 불릴지 모르고(30fps 노트북, 144Hz 모니터, 발열로 떨어진 휴대폰), 그 횟수에 기대는 코드는 기기마다 결과가 다릅니다. 앞의 랩 7(한 프레임짜리 입력을 FixedUpdate 가 놓침)은 Update 와 FixedUpdate 의 **횟수 차이**를, 이 절의 랩은 **한 번에 움직이는 양**을 다룹니다.

이 절의 랩은 같은 코드를 **30fps 와 120fps 에서 한 번씩** 돌려(`Time.captureDeltaTime` 으로 프레임 간격을 고정) 두 결과가 사양과 같은지 봅니다. 채점기의 숨은 케이스는 24fps 와 144fps 입니다. 랩 15 는 `timeScale = 0` 을 다루므로 진짜 시간으로 기다립니다.

### 쓰는 도구

| 도구 | 하는 법 | 언제 |
|---|---|---|
| **fps 바꿔 재현** | `Application.targetFrameRate = 30;` (vSync 를 끄고: Quality → VSync Count = Don't Sync), 또는 `Time.captureDeltaTime = 1f / 30f;` 로 간격 고정 | "내 PC 에서는 되는데" 를 내 PC 에서 재현하기 |
| Game 뷰 **Stats** | Game 뷰 오른쪽 위 Stats | 지금 fps 와 프레임 시간 |
| 추적점으로 **프레임마다 값 찍기** | `{Time.frameCount} {Time.deltaTime} {transform.position.x}` | 한 프레임에 얼마씩 바뀌나, 그것이 deltaTime 에 비례하나 (랩 12, 13) |
| 조사식에 **시간 값 나란히** | `Time.deltaTime`, `Time.fixedDeltaTime`, `Time.unscaledDeltaTime`, `Time.timeScale` | 어느 시간이 fps 와 timeScale 에 따라 바뀌나 (랩 14, 15) |
| Profiler → CPU → **Timeline** | 프레임 하나에 Update 와 FixedUpdate 가 몇 번씩 들어 있나 | 고정 간격 물리와 프레임의 관계 |

| 시간 값 | Update 에서 | FixedUpdate 에서 | `timeScale = 0` 이면 |
|---|---|---|---|
| `Time.deltaTime` | 지난 프레임부터 흐른 게임 시간 | `fixedDeltaTime` 과 같은 값 | 0 |
| `Time.fixedDeltaTime` | 설정값(기본 0.02) 그대로. **fps 와 상관없음** | 같음 | 그대로(FixedUpdate 는 멈춤) |
| `Time.unscaledDeltaTime` | 지난 프레임부터 흐른 진짜 시간 | 진짜 시간 | 진짜 시간 |

### 랩 목록

| # | 이름 | 증상 | 새로 배우는 것 | 난이도 |
|---|---|---|---|---|
| 12 | 호버카 | 30fps 에서 절반, 120fps 에서 두 배 속도 | **프레임당 값과 초당 값**, `* Time.deltaTime`, 단위를 이름에 | ★ |
| 13 | 따라오는 카메라 | 30fps 에서 카메라가 한참 처지고 120fps 에서 딱 붙는다 | **매 프레임 `Lerp(a, b, 0.1f)` 는 프레임 수의 거듭제곱**, `1 - exp(-k·dt)` 지수 감쇠, `SmoothDamp` | ★★ |
| 14 | 스태미나 | "시간을 곱했는데도" fps 에 따라 차는 속도가 다르다 | **`fixedDeltaTime` 은 Update 에서 쓰면 상수**, FixedUpdate 안의 `deltaTime` | ★ |
| 15 | 일시정지 메뉴 | `timeScale = 0` 에서 메뉴가 안 들어오고 카운트다운이 멈춘다 | **timeScale 0 에서도 Update 는 돈다**, `unscaledDeltaTime`, `WaitForSecondsRealtime`, Animator Unscaled Time | ★ |

---

# 3부: 컨테이너

배열, `List`, `Dictionary`, `NativeList` 는 C# 에서도 흔한 버그 원인이지만, Unity 에서는 **엔진이 컨테이너를 만지는 곳**이 더 있습니다. 인스펙터는 배열 크기를 언제든 바꾸고, 직렬화는 모르는 컨테이너를 조용히 버리고, `Destroy` 는 컨테이너에 든 오브젝트를 fake null 로 바꾸고, `Find`/`GetComponents` 는 순서를 약속하지 않고, 할당을 줄이는 API 는 넘긴 컨테이너를 비우고 다시 채웁니다.

## 3부에서 쓰는 도구

| 도구 | 하는 법 | 언제 |
|---|---|---|
| **예외 설정: throw 될 때 중단** | VS: 디버그 → 창 → 예외 설정 → Common Language Runtime Exceptions 에서 `System.IndexOutOfRangeException`, `System.InvalidOperationException` 체크. Rider: Run → View Breakpoints → Exception Breakpoints | 예외가 잡히는 곳이 아니라 **던지는 곳**에서 멈추기 (랩 16, 17) |
| 조사식에 **인덱스와 크기를 나란히** | `i`, `Slots.Length`, `Labels.Count`, `view[0]` | 범위 밖 인덱스 (랩 16), 길이를 바꾼 쓰기 직후 (랩 22) |
| 조사식 **참조 비교** | `alphaRoster == bravoRoster`, `(object)key == null` 과 `key == null` 을 나란히 | 같은 컨테이너를 두 변수가 가리키나 (랩 20), fake null 키 (랩 21) |
| **Inspector 의 Debug 모드** | Inspector 오른쪽 위 ⋮ → Debug. private 직렬화 필드까지 보인다 | 이 필드가 직렬화되는가. 인스펙터에 안 보이면 Instantiate·저장에서도 빠진다 (랩 18) |
| `JsonUtility.ToJson(component)` 출력 | Console 에 찍어 보기 | 직렬화되는 필드만 나온다. 빠진 필드 = 복제·저장에서 사라지는 필드 (랩 18) |
| 컨테이너에 **중단점 + 호출 스택** | `List.Remove` 를 부르는 줄에 중단점 → 호출 스택에 바깥 `foreach` 가 보이나 | 반복 도중에 누가 목록을 고쳤나 (랩 17) |

## 3부 랩 목록

| # | 이름 | 증상 | 새로 배우는 것 | 난이도 |
|---|---|---|---|---|
| 16 | 단축키 칸 | 디자이너가 칸 배열을 3칸으로 줄이자 IndexOutOfRangeException, 빈 단축키에서 ArgumentOutOfRangeException | **상수 길이와 데이터 길이**, 인스펙터는 배열 크기를 바꾼다, 바깥 번호의 범위 확인, `OnValidate` | ★ |
| 17 | 공습 | 공습에 적이 쓰러지면 InvalidOperationException, 뒤쪽 적은 멀쩡 | **반복 도중 다른 스크립트가 목록을 고침**, 복사본·역순 반복, `RemoveAll` | ★ |
| 18 | 상점 가격표 | 복제하거나 저장했다 불러온 상점의 가격표와 진열대가 텅 빈다(경고 없음) | **Unity 직렬화가 버리는 컨테이너**(`Dictionary`, `List<List<T>>`, `T[,]`), `ISerializationCallbackReceiver`, `[Serializable]` 래퍼 | ★★ |
| 19 | 순찰 순서 | 경비병이 Order 번호와 상관없는 순서로 돈다. 맵마다, 실행 환경마다 맞기도 하고 틀리기도 한다 | **`FindObjectsByType` / `GetComponents` 의 순서는 약속이 아니다**, 직접 정렬, 인스펙터 배열 | ★ |
| 20 | 부대 편성 | 두 부대의 명단이 똑같다 | **결과를 채워 주는 API 가 넘긴 List 를 비운다**(`GetComponentsInChildren(List)`), 돌려쓰는 버퍼의 주인 | ★★ |
| 21 | 위협 목록 | 위협 1위가 죽자 보스가 MissingReferenceException, 목록 수도 그대로 | **파괴된 오브젝트가 Dictionary 키로 남음**(fake null 키), 죽은 키 치우기, `OnDestroy` 알림 | ★★ |
| 22 | 발자국 | 한 걸음만 걸어도 출발점을 읽는 줄에서 "has been deallocated" | **`NativeList.AsArray()` 는 복사가 아니라 모습**, 길이를 바꾸는 쓰기마다 무효, 캐시하지 않기 | ★★ |

> 랩 22 의 예외는 에디터의 컬렉션 안전 검사 덕분입니다(4부와 같음). 릴리스 빌드에서는 해제된 메모리를 읽어 엉뚱한 값이나 크래시가 납니다. `NativeArray` 의 `Dispose` 누락은 4부 랩 23 에서 다룹니다.

---

# 4부: 잘못된 참조와 네이티브 메모리

C# 은 GC 가 메모리를 치워 주지만, Unity 에는 **GC 가 모르는 메모리**가 있습니다. `NativeArray` 같은 네이티브 컨테이너는 Unity 의 네이티브 힙에 있고 C# 변수는 그 주소를 든 작은 struct 일 뿐이라서, 누가 언제 해제하는지(`Allocator`, `Dispose`)와 누가 지금 쓰고 있는지(잡)를 직접 관리해야 합니다. 대신 에디터에는 **안전 시스템(AtomicSafetyHandle)** 이 있어서 해제된 버퍼나 잡이 쓰는 중인 버퍼에 손대면 바로 예외를 던져 줍니다. 그리고 정적(static) 필드는 Domain Reload 를 끄면 **지난 Play 의 오브젝트**를 붙잡습니다.

1부의 MissingReferenceException, 2부의 fake null·구조체 복사, 7부의 sharedMaterial 과는 다른 종류입니다. 여기서는 "수명을 누가 정하나" 를 봅니다.

## 4부에서 쓰는 도구

| 도구 | 하는 법 | 언제 |
|---|---|---|
| **Leak Detection Level** | Edit → Preferences → **Jobs** → Leak Detection Level → *Enabled With Stack Trace* (코드로는 `NativeLeakDetection.Mode`) | 플레이를 멈추거나 다시 컴파일할 때 뜨는 `Leak Detected` 에 **할당한 곳의 스택**이 붙는다 (랩 23) |
| **안전 시스템 예외 읽기** | 예외 메시지에 **잡 이름과 버퍼(필드) 이름**이 나온다: `The previously scheduled job X writes to ... X.Count` | 잡과 메인 스레드가 같은 버퍼를 건드릴 때 (랩 24), 해제된 버퍼 (랩 25) |
| Profiler → CPU → **Timeline** | Worker 스레드 줄에서 잡이 언제 도는지, 메인 스레드가 `JobHandle.Complete` 에서 얼마나 기다리는지 | 잡 의존 관계 (랩 24) |
| **Memory Profiler** | 스냅샷 두 장 → Compare → Native / Unity Subsystems | 늘어나기만 하는 네이티브 메모리 (랩 23) |
| 조사식 `NativeArray` 펼치기 | `IsCreated`, `Length`. 해제된 버퍼를 펼치면 안전 시스템 예외 메시지가 값 대신 보인다 | 이 변수가 아직 살아 있나 |
| **Enter Play Mode Settings** | Edit → Project Settings → Editor → When entering Play Mode (*Reload Domain and Scene* / *Reload Scene only* / *Do not reload Domain or Scene*) | 정적 필드가 Play 사이에 살아남는가 (랩 26) |

## 4부 랩 목록

| # | 이름 | 증상 | 새로 배우는 것 | 난이도 |
|---|---|---|---|---|
| 23 | 경로 캐시 | 예외는 없는데 오래 하면 메모리가 늘고, 멈추면 `Leak Detected` | **NativeArray 는 GC 가 치우지 않는다**, `Dispose` / `OnDestroy`, Leak Detection Level, 구조체 복사본과 안전 핸들 | ★ |
| 24 | 망루의 시야 | 잡을 붙이자마자 InvalidOperationException ("분명히 Complete 했는데?") | **JobHandle 의존 사슬**, 끝 핸들을 Complete, 안전 시스템 메시지 읽기, Profiler Timeline 의 Worker | ★★ |
| 25 | 출현 계획표 | 웨이브를 시작한 다음 프레임, 첫 줄부터 "has been deallocated" | **Allocator 수명**(Temp / TempJob / Persistent), 프레임 끝에 되감기는 Temp | ★★ |
| 26 | 두 번째 판 | Play 를 두 번째 누르면 웨이브가 이어지고 레이더가 지난 판의 적을 가리킨다 | **Domain Reload 를 끈 Enter Play Mode**, 정적 상태 되돌리기 `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` | ★★ |

> 랩 23·25 는 에디터의 컬렉션 안전 검사(`ENABLE_UNITY_COLLECTIONS_CHECKS`)로 판정합니다. 에디터와 개발 빌드에서는 켜져 있습니다. 릴리스 빌드에서는 같은 버그가 예외 없이 **엉뚱한 값이나 크래시**로 나타납니다(그래서 에디터에서 잡아야 합니다).
>
> 랩 26 은 Play 를 다시 누른 것을 랩 안에서 흉내 냅니다(지난 판 오브젝트를 지우고 `[RuntimeInitializeOnLoadMethod]` 들을 부름). 이 프로젝트의 실제 설정(랩 출력 첫 줄)은 *Reload Domain and Scene* 이라서, 진짜 Play/Stop 을 반복해도 재현되지 않습니다. 직접 겪어 보려면 설정을 *Reload Scene only* 로 바꾸고 Play → Stop → Play 를 해 보세요(실험이 끝나면 되돌리기).

---

# 5부: 물리

물리 엔진(PhysX)은 Unity 의 transform 과 **따로 자기 장면을 갖고**, 프레임이 아니라 **고정 간격(fixedDeltaTime, 기본 0.02초)마다 순간이동하듯** 계산합니다. 그래서 물리 버그는 대개 예외 없이 "안 맞는다", "안 불린다", "엉뚱한 것을 맞힌다", "덜덜 떨린다" 로 나타납니다. 중단점보다 **눈으로 보는 도구**(Physics Debugger, Gizmos, `Debug.DrawRay`)가 먼저입니다.

## 5부에서 쓰는 도구

| 도구 | 하는 법 | 언제 |
|---|---|---|
| **Physics Debugger** | Window → Analysis → **Physics Debugger**. Scene 뷰에 콜라이더를 종류별 색(Static / Kinematic / Dynamic / Trigger)으로 그린다. *Queries* 탭은 레이캐스트·오버랩을, *Contacts* 탭은 접촉점을 보여 준다 | 이 콜라이더가 물리 엔진에 어떤 종류로 들어가 있나 (랩 28), 레이캐스트가 어디에 맞았나 (랩 29), 콜라이더가 실제로 어디 있나 (랩 31) |
| **`Debug.DrawRay` / `Debug.DrawLine`** | `Debug.DrawRay(origin, dir * length, Color.red, 5f)`. 마지막 인자는 남겨 둘 초. Scene 뷰(그리고 Game 뷰의 Gizmos 버튼)에서 보인다 | 레이의 방향과 길이 (랩 29), 물리 스텝마다 탄환이 간 거리 (랩 27) |
| **Gizmos** | 컴포넌트에 `void OnDrawGizmos() { Gizmos.DrawWireSphere(transform.position, radius); }` | 감지 범위, 트리거 크기를 늘 띄워 두기 |
| 일시정지 + **한 프레임씩** | Play 중 일시정지(Ctrl+Shift+P) → 다음 프레임(Ctrl+Alt+P) | 물리 스텝 사이에 무엇이 바뀌는지 (랩 27, 30, 31) |
| **Layer Collision Matrix** | Project Settings → Physics → Layer Collision Matrix | 두 레이어가 서로 충돌·트리거하도록 되어 있나 (랩 28) |
| 조사식에 **2진수** | `Convert.ToString(mask, 2)` | 레이어 마스크에 어느 비트가 켜져 있나 (랩 29) |
| 조사식 `collider.bounds.center` | transform.position 과 나란히 | transform 과 물리 장면의 위치가 같은가 (랩 31) |

## 5부 랩 목록

| # | 이름 | 증상 | 새로 배우는 것 | 난이도 |
|---|---|---|---|---|
| 27 | 레일건 | 빠른 탄환이 얇은 벽을 그냥 뚫는다(충돌 0번) | **터널링**, 물리 스텝마다 순간이동, `CollisionDetectionMode.Continuous / ContinuousDynamic / ContinuousSpeculative` | ★ |
| 28 | 가시 함정 | 함정 위를 지나가도 `OnTriggerEnter` 가 불리지 않는다 | **충돌 메시지는 한쪽에 Rigidbody 가 있어야**, 정적 콜라이더끼리, Kinematic Rigidbody, 충돌 액션 매트릭스 | ★ |
| 29 | 포탑의 조준 | Enemy 레이어만 쏘라고 했는데 유리창에 맞는다 | **레이어 번호 ≠ 레이어 마스크**, `1 << layer`, `LayerMask` 필드, `LayerMask.GetMask` | ★ |
| 30 | 움직이는 발판 | Interpolate 를 켜자 발판이 거의 안 움직이고, 위의 상자도 안 실려 간다 | **Rigidbody 를 transform 으로 움직이지 않기**, 보간이 transform 을 덮어씀, Kinematic 의 `MovePosition` 은 속도를 전달 | ★★ |
| 31 | 퍼즐 초기화 | 초기화한 그 프레임에 상자를 집으면 허공을, 옛 자리를 집으면 상자를 집는다 | **transform 과 물리 장면의 동기화 시점**, `Physics.SyncTransforms`, `rb.position` | ★★ |

> 5부의 판정은 충돌 횟수, 트리거 횟수, 레이캐스트가 맞은 이름, 최종 위치로 합니다. 물리는 `-batchmode -nographics` 에서도 똑같이 돌기 때문에 SKIP 은 없습니다. 랩 28·30 은 `Time.captureDeltaTime` 으로 60fps 를 고정합니다.

---

# 6부: 사운드

> **⚠️ 이 부의 랩은 소리를 냅니다. 실행 전에 시스템 볼륨을 낮추고, 헤드폰을 쓴다면 귀에서 떼고 시작하세요.** 랩이 도는 동안 전체 볼륨(`AudioListener.volume`)을 0.1 로 낮추고, 클립도 진폭 0.3 이하의 단일음이지만 랩 33 은 소리 수십 개가 한꺼번에 납니다. 랩 실행 창의 이 랩 버튼에는 "소리 주의" 가, Console 에는 `[주의] 소리가 납니다. 볼륨을 낮추세요` 가 먼저 찍힙니다.

"소리가 늦게 나요", "안 들려요", "끊겨요"는 귀로는 바로 들리지만, 버그를 **재현하고 판정**하려면 숫자가 필요합니다. 이 랩들은 클립의 로드 상태(`AudioClip.loadState`), 가상 음성 여부(`AudioSource.isVirtual`), 재생 중인지와 재생 위치(`isPlaying`, `time`)를 재서 `[PASS]`/`[FAIL]` 을 냅니다.

## 6부에서 쓰는 도구

| 도구 | 하는 법 | 언제 |
|---|---|---|
| 오디오 임포트 설정 | Project 창에서 .wav 선택 → Inspector: Load Type, Preload Audio Data, Load In Background, Compression | 첫 재생 지연, 메모리 (랩 32) |
| Profiler → **Audio** 모듈 | 재생 중인 소리 수, 가상 음성 수, 오디오 메모리, 채널 목록 | 동시 재생 한도 (랩 33) |
| Project Settings → **Audio** | Max Real Voices / Max Virtual Voices, DSP Buffer Size | 한도 자체 (랩 33) |
| Game 창 **Mute Audio** 토글 | Game 창 위 도구 모음 | 소리를 끈 채로 판정만 보기 |
| `Debug.Break()` + Hierarchy | 소리가 끊기는 프레임에서 일시정지 → AudioSource 가 붙은 오브젝트가 아직 있나 | 끊김 (랩 34) |

## 6부 랩 목록

| # | 이름 | 증상 | 새로 배우는 것 | 난이도 |
|---|---|---|---|---|
| 32 | 블래스터 발사음 | 첫 발만 발사음이 섬광보다 늦다 | **Preload Audio Data / Load In Background**, `loadState`, `LoadAudioData()` | ★★ |
| 33 | 보스 대사 | 병사가 많으면 보스 대사가 안 들린다 | **Max Real Voices**, 가상 음성, `AudioSource.priority`, Profiler Audio 모듈 | ★★ |
| 34 | 폭발 통 | 폭발음이 터지자마자 끊긴다 | 소리의 수명 = **AudioSource 가 붙은 오브젝트의 수명**, `PlayClipAtPoint` | ★ |
| 35 | 작전 브리핑 | 맞는 소리 때문에 브리핑이 끊긴다 | **`Play()` 와 `PlayOneShot()`** 의 차이, 채널(AudioSource) 나누기 | ★ |

> **헤드리스 실행:** 랩 33~35 는 소리가 실제로 재생되어야 판정됩니다. 오디오 출력 장치가 있는 PC 에서는 `-batchmode -nographics` 로도 판정됐습니다(아래 "자동 검증"). 출력 장치가 없는 서버/CI 에서는 대조군 소리가 재생되지 않으므로 `[SKIP]` 이 나옵니다. 랩 32 는 로드 상태만 보므로 장치와 상관없습니다.

---

# 7부: 이미지 / 렌더링

"색이 안 바뀌어요", "아이콘이 다른 아이템 거예요", "색이 기획서랑 달라요"는 눈으로 바로 보이지만, 눈대중으로는 "조금 다른 것 같다"에서 멈추기 쉽습니다. 이 랩들은 **렌더러에 끼워진 머티리얼의 값**, **Material 객체 수**, **칸에 들어간 텍스처 이름**, **실제로 그려진 픽셀**을 조회해서 판정합니다.

## 7부에서 쓰는 도구

| 도구 | 하는 법 | 언제 |
|---|---|---|
| Inspector 의 Materials | 플레이 중 Hierarchy 에서 오브젝트 선택. 이름 끝 `(Instance)` 면 복제본 | 공유 머티리얼인지 복제본인지 (랩 36, 37) |
| **Frame Debugger** | Window → Analysis → Frame Debugger → Enable. 드로 콜을 고르면 셰이더 속성 값이 보인다 | 실제로 넘어간 `_Color`, 배칭이 깨진 이유 (랩 37, 39) |
| Profiler → **Memory** 모듈 | Objects 수, Texture/Material 수 | 늘어나기만 하는 객체 (랩 37) |
| **Memory Profiler** | Package Manager → `com.unity.memoryprofiler` 설치 → Window → Analysis → Memory Profiler → 스냅샷 2개 → Compare | 누가 몇 개 남았나 (랩 37) |
| Player Settings → **Color Space** | Edit → Project Settings → Player → Other Settings | Gamma / Linear (랩 39) |
| 텍스처 임포트 설정의 **sRGB (Color Texture)** | 텍스처 선택 → Inspector | 색 텍스처는 켜고, 노멀/마스크는 끈다 |
| RenderDoc | Game 창 ⋮ → Load RenderDoc → 카메라 아이콘 | GPU 가 실제로 그린 것을 한 프레임 통째로 |

## 7부 랩 목록

| # | 이름 | 증상 | 새로 배우는 것 | 난이도 |
|---|---|---|---|---|
| 36 | 상자 강조 | 한 상자에 마우스를 올렸는데 전부 빨개진다 | **`sharedMaterial` 은 에셋 자체**, `material` 은 복제본, 에셋이 디스크에 저장되는 문제 | ★ |
| 37 | 보급 상자 색 | 웨이브마다 메모리가 늘어난다 | **`renderer.material` 복제본 누수**, **MaterialPropertyBlock**, Memory Profiler, Frame Debugger | ★★ |
| 38 | 인벤토리 아이콘 | 스크롤하면 이름은 물약인데 아이콘은 검 | 비동기 로드(`Resources.LoadAsync`) 콜백의 **늦은 도착**, 재사용되는 칸, 캐시 적중은 즉시 | ★★ |
| 39 | 팀 색 | 팀 색이 물 빠지고 밝다(0x00, 0xFF 는 맞음) | **Linear / Gamma 색 공간**, `SetColor` 와 `SetVector` 의 차이, Frame Debugger 로 값 보기 | ★★ |

> **헤드리스 실행:** 랩 39 는 카메라로 그린 픽셀을 다시 읽어서 판정하므로 그래픽 장치가 필요합니다. `-nographics` 에서는 `[SKIP]` 이 나옵니다. 랩 36~38 은 그래픽 장치 없이도 판정됩니다.

---

# 8부: 애니메이션

애니메이션 버그는 "가끔 안 맞는 것 같다"로 신고됩니다. 이 랩들은 **Animator 상태**, **애니메이션 이벤트가 불린 횟수**, **프레임마다 두 Transform 의 거리**를 재서 판정합니다. 버그 중 일부는 C# 이 아니라 **애니메이션 에셋**(컨트롤러, 클립)에 있습니다. 그래서 Animator 창과 Animation 창을 디버거처럼 쓰는 법을 배웁니다.

## 8부에서 쓰는 도구

| 도구 | 하는 법 | 언제 |
|---|---|---|
| **Animator 창 라이브 보기** | Window → Animation → Animator. 플레이 중 Hierarchy 에서 Animator 가 붙은 오브젝트를 고르면 현재 상태, 진행 막대, 트랜지션, 파라미터 값이 실시간으로 보인다 | 모든 랩. 랩이 끝나면 오브젝트가 지워지니 중단점이나 `Debug.Break()` 로 멈춘 채 본다 |
| Animation 창 | Window → Animation → Animation (Ctrl+6). 클립을 고르면 커브와 이벤트(타임라인 위 작은 막대) | 이벤트가 몇 초에 있나 (랩 41, 42) |
| 트랜지션 Inspector | Animator 창에서 화살표 클릭 → Has Exit Time, Exit Time, Transition Duration, Interruption Source | 상태가 언제 넘어가나 (랩 42) |
| Animator 컴포넌트의 **Culling Mode** | Inspector | 화면 밖에서 멈추나 (랩 41) |
| 일시정지 + **한 프레임씩** | Play 옆 ⏸ → ⏭ (Ctrl+Alt+P) | 트랜지션을 프레임 단위로 보기 |
| Profiler Timeline | `PreLateUpdate.DirectorUpdateAnimationBegin/End` 위치 | 애니메이션이 Update 와 LateUpdate 사이에 평가된다 (랩 43) |

## 8부 랩 목록

| # | 이름 | 증상 | 새로 배우는 것 | 난이도 |
|---|---|---|---|---|
| 40 | 공격 버튼 | 버튼을 눌러도 공격 모션이 안 나온다(예외 없음) | **Animator 파라미터 이름 오타**는 경고만, `StringToHash`, Animator 창 Parameters | ★ |
| 41 | 등 뒤의 발소리 | 화면 밖 경비병의 발소리가 안 난다 | **Culling Mode**, 화면 밖에서 애니메이션 이벤트가 사라짐, Scene 뷰도 "보는 것" | ★★ |
| 42 | 검격 판정 | 휘두르는데 피해가 안 들어간다 | **트랜지션이 이벤트보다 먼저 끝남**, Exit Time / Duration 읽기, Animation 창의 이벤트 | ★★ |
| 43 | 손에 든 무기 | 횃불이 손을 한 박자 늦게 따라온다 | **애니메이션은 Update 뒤에 평가**, LateUpdate, Profiler Timeline | ★★ |

> 랩 42 를 고치려면 `Assets/BugHunt/Resources/BugHunt/Animation/Swordsman.controller` 를 Animator 창에서 수정합니다. C# 을 고치는 랩이 아닙니다.

---

# 9부: 동시성과 멈춤

두 스레드가 같은 데이터를 만지거나(데이터 레이스), 서로를 기다리거나(데드락), 끝나지 않는 반복문에 갇히는(무한루프) 버그입니다. 앞쪽 랩 44~46 은 결과가 틀리거나 예외가 조용히 삼켜지고, 뒤쪽 랩 47~50 은 에디터가 굳습니다. 어느 쪽이든 **스레드 창과 호출 스택**으로 "지금 어느 스레드가 어디에 있나" 를 보는 것이 출발점입니다.

## 데이터 레이스와 스레드 (랩 44~46)

Unity 의 게임 코드는 대부분 메인 스레드 하나에서 돌지만, 무거운 일을 `Task.Run`, `Thread`, `Awaitable.BackgroundThreadAsync()` 로 넘기는 순간 **두 스레드가 같은 데이터를 만지는** 버그가 생깁니다. 뒤의 랩 47(메인 스레드에서 `.Result` 로 기다려 데드락)이 "서로 기다리기" 라면, 이 절의 랩은 "서로 끼어들기" 와 "엉뚱한 스레드에서 Unity 부르기" 입니다.

레이스는 원래 가끔만 납니다. 연습이 되도록 **랩마다 매번 같은 결과**가 나오게 만들었습니다: 랩 44 는 랩이 넣은 결제 흉내가 동시에 들어온 요청끼리 서로를 최대 1초 기다리게 해서 "둘 다 확인을 통과한 상태" 를 확실하게 만들고, 랩 45·46 은 스레드가 바뀌는 것 자체가 버그라서 늘 재현됩니다.

### 쓰는 도구

| 도구 | 하는 법 | 언제 |
|---|---|---|
| **스레드 창** | VS: 디버그 → 창 → 스레드 (Ctrl+Alt+H), Rider: Debug 창 Threads | 중단점에 멈춘 곳이 메인 스레드인가, 같은 줄에 스레드가 몇 개 있나 (모든 랩) |
| **병렬 스택 / 작업 창** | VS: 디버그 → 창 → 병렬 스택, 작업(Tasks) | 어떤 Task 가 어디까지 왔나, 끝났나, 예외로 끝났나 (랩 44, 45) |
| 중단점 **필터** / 추적점 | 중단점 → 조건 → 필터 `ThreadId == 1` / 추적점 `{Environment.CurrentManagedThreadId} Stock={Stock}` | 스레드별로 끼어드는 순서 보기. 추적점은 멈추지 않으므로 타이밍을 덜 바꾼다 (랩 44) |
| 조사식 `Thread.CurrentThread.ManagedThreadId` | await 앞뒤 줄에서 비교 | await 다음 줄이 어느 스레드에서 도나 (랩 46) |
| **예외 설정: throw 될 때 중단** | Common Language Runtime Exceptions 전체 체크 | Task 안에서 삼켜지는 예외를 던지는 자리에서 잡기 (랩 45) |
| 조사식 `task.Status`, `task.Exception` | Faulted / RanToCompletion, InnerException | "아무 일도 안 일어났다" 의 정체 (랩 45) |

### Job System 안전 시스템이 잡는 것과 못 잡는 것

4부(랩 24, 25)의 안전 시스템은 **NativeContainer 를 쓰는 잡**에 대해서만 레이스를 막아 줍니다.

| 상황 | 안전 시스템 | 이 판에서 |
|---|---|---|
| 두 잡이 같은 `NativeArray` 에 동시에 쓴다(의존 관계 없음) | **잡는다** (스케줄할 때 InvalidOperationException, 잡과 필드 이름이 나온다) | 랩 24 |
| 잡이 쓰는 중인 `NativeArray` 를 메인 스레드가 읽는다(Complete 전) | **잡는다** | 랩 24 |
| 해제된 `NativeArray` 를 잡이나 메인 스레드가 만진다 | **잡는다** (ObjectDisposedException) | 랩 22, 25 |
| `[ReadOnly]` 없이 두 잡이 같은 배열을 읽기만 한다 | 잡는다(쓰기로 보고 막는다). `[ReadOnly]` 를 붙이면 허용 | — |
| `Task.Run` / `Thread` 가 C# 객체(`List`, 필드, `Dictionary`)를 함께 쓴다 | **못 잡는다** (관리 힙은 감시 대상이 아니다) | 랩 44 |
| 워커 스레드에서 Unity API(`transform`, `gameObject`) 호출 | 안전 시스템이 아니라 **엔진이** UnityException 을 던진다. 단 Task 안이면 삼켜진다 | 랩 45, 46 |
| 잡 안에서 static 필드를 함께 쓴다 | **못 잡는다** (Burst 는 static 쓰기를 컴파일 오류로 막지만, Burst 를 끄면 그냥 돈다) | — |
| `NativeDisableParallelForRestriction`, `NativeDisableContainerSafetyRestriction` 을 붙인 필드 | 일부러 **끈 것**이라 못 잡는다 | — |

### 랩 목록

| # | 이름 | 증상 | 새로 배우는 것 | 난이도 |
|---|---|---|---|---|
| 44 | 한정 판매 | 한 자루 남은 검을 두 명이 샀다. 재고 -1 | **확인하고 행동하기(check-then-act) 레이스**, `lock`, 공유 `List` 의 동시 Add, 안전 시스템이 못 잡는 관리 힙 | ★★ |
| 45 | 백그라운드 길 찾기 | 길 찾기는 끝까지 도는데 로봇이 안 움직인다. Console 은 조용하다 | **워커 스레드의 Unity API**, Task 안에서 삼켜지는 예외, `await Task.Run(...)` 뒤는 메인 스레드 | ★★ |
| 46 | 세이브 불러오기 | 백그라운드 해석으로 바꾸자 "can only be called from the main thread" | **await 다음 줄의 스레드**, `Awaitable.BackgroundThreadAsync` / `MainThreadAsync`, UnitySynchronizationContext | ★★ |

## 멈춤: 무한루프와 데드락 (랩 47~50)

예외도 로그도 없이 **에디터 전체가 굳는** 버그입니다. Unity 의 게임 코드는 거의 다 메인 스레드 하나에서 돌기 때문에, 메인 스레드가 한 프레임 안에서 빠져나오지 못하면 Update 도, 렌더링도, 에디터 UI 도 멈춥니다. 작업 관리자에는 "응답 없음", Console 은 조용합니다. 이런 버그는 **멈춘 그 순간의 메인 스레드 호출 스택**을 봐야 풀립니다.

### 진짜로 굳었을 때 하는 일

1. **CPU 를 본다.** 작업 관리자에서 Unity 가 CPU 한 코어(예: 8코어면 약 12%)를 꽉 쓰면 **무한루프**, 0% 근처면 **데드락**(서로 기다리기만 함)입니다. 메모리가 계속 늘면 무한루프 안에서 무언가를 쌓고 있습니다.
2. **디버거를 붙이고 모두 중단.** 굳은 에디터에도 디버거는 붙습니다. VS: **디버그 → 모두 중단 (Ctrl+Alt+Break)**, Rider: **Pause Program**(⏸).
3. **스레드 창에서 메인 스레드를 고른다.** VS: 디버그 → 창 → **스레드 (Ctrl+Alt+H)**, Rider: Debug 창의 Threads 드롭다운. `Main Thread`(또는 스레드 ID 1)를 더블클릭하면 호출 스택 창이 그 스레드로 바뀝니다. 맨 위의 내 코드 줄이 갇혀 있는 곳입니다.
4. **F10 을 몇 번 눌러 본다.** 같은 반복문 안을 맴돌면 무한루프. 지역 창에서 종료 조건에 쓰이는 값이 **바뀌고 있는지** 보세요(안 바뀌면 영원히 안 끝납니다).
5. 데드락이면 **병렬 스택**(VS: 디버그 → 창 → 병렬 스택)과 **작업(Tasks)** 창에서 누가 무엇을 기다리는지 봅니다.

### 이 프로젝트에서는 실제로 굳지 않습니다 (HangGuard)

연습 때마다 작업 관리자로 Unity 를 죽일 수는 없으니, 멈춤 랩(47~50)의 반복문에는 `HangGuard.Tick(...)` 한 줄이, 막고 기다리는 곳에는 `HangGuard.Wait(task, ...)` 가 들어 있습니다(`Assets/BugHunt/Core/HangGuard.cs`).

- `Tick` 은 **아무것도 바꾸지 않고 아무것도 돌려주지 않습니다.** 반복문의 조건과 몸통은 버그 그대로입니다. 버그를 고쳐 반복문이 정상적으로 끝나면 `Tick` 은 있으나 마나입니다(가리는 것이 없습니다).
- 한 프레임 안에서 **2초**(시간 예산)를 넘기거나, 정해 둔 **반복 한도**(랩 49 처럼 돌 때마다 메모리를 먹는 반복문)를 넘으면 `[FAIL] 응답 없음: ...` 을 찍고 `HangException` 을 던져 빠져나옵니다. 랩 결과는 **TIMEOUT** 입니다.
- 그 예외의 **스택 트레이스 = 메인 스레드가 갇혀 있던 곳**입니다. "모두 중단" 으로 보는 호출 스택과 같습니다.
- `Wait` 는 `task.Result` 와 같습니다. 예산 안에 Task 가 끝나지 않으면 데드락으로 보고 빠져나옵니다.
- `Tick` 이 있는 반복문 안에서 중단점에 멈춰 있던 시간은 예산에 넣지 않습니다(한 줄씩 넘기며 살펴봐도 "응답 없음" 이 되지 않습니다).
- **직접 "모두 중단" 을 눌러 보려면** 메뉴 **BugHunt → 멈춤 랩: 오래 버티기(30초)** 를 켜세요. 30초 동안 정말로 굳어 있으니 그사이에 디버거를 붙이고 위 1~5단계를 해 볼 수 있습니다. (랩 49 는 메모리 때문에 반복 한도로 먼저 끊깁니다.) 채점기는 이 설정과 상관없이 2초를 씁니다.

> ⚠ **`HangGuard.Tick` / `HangGuard.Wait` 줄을 지우지 마세요.** 지운 채로 버그가 남아 있으면 에디터가 정말로 굳습니다. 그랬다면 작업 관리자에서 Unity 를 끝내고 다시 여세요(저장하지 않은 씬은 날아갑니다). 고칠 때 `Wait` 는 지워도 됩니다. 막고 기다리지 않게 바꾸는 것이 랩 47 의 정답이기 때문입니다.

### 랩 목록

| # | 이름 | 증상 | 새로 배우는 것 | 난이도 |
|---|---|---|---|---|
| 47 | 상점의 금화 | 상점을 여는 순간 영원히 멈춘다(CPU 0%) | **메인 스레드에서 `.Result` 로 기다리면 UnitySynchronizationContext 때문에 데드락**, `await` 가 돌아오는 곳, 병렬 스택 / 작업 창, `ConfigureAwait(false)` 는 반쪽짜리 해결 | ★★ |
| 48 | 엘리베이터 | 엘리베이터를 부르는 순간 게임 전체가 멈춘다(CPU 한 코어 100%) | **Update 가 바꿔 줄 값을 while 로 기다리기**, `yield return` 없는 코루틴 반복문, 코루틴은 스레드가 아니다, `WaitUntil`, 모두 중단 + 스레드 창 | ★ |
| 49 | 던전 길 찾기 | 문을 잠가 길이 끊기면 멈추고 메모리가 치솟는다 | **그래프 탐색의 방문 체크**, 고리와 양방향 간선, "멈춤 + 메모리 증가" 의 지문, 조건부 중단점 `queue.Count > 100` | ★★ |
| 50 | 리플레이 빨리감기 | 서버를 일주일 넘게 켜 두면 빨리감기에서 멈춘다 | **float 누적 종료 조건**, 큰 수에서 작은 수를 더하면 그대로, 정수 틱 / double, `Time.timeAsDouble` | ★★ |

---

# 10부: 성능 (GC 할당과 반복 탐색)

결과는 맞는데 **느린** 버그입니다. Unity 의 C# 은 GC(가비지 컬렉터)가 쓰레기를 모아 치우는 동안 메인 스레드를 멈추므로, **매 프레임 조금씩 쓰레기를 만드는 코드**는 몇 초마다 프레임을 툭 끊습니다. 그리고 "결과가 거의 늘 같은" 탐색(`Find`, `FindObjectsByType`)을 매 프레임 되풀이하면 오브젝트 수에 비례해 느려집니다. 이런 버그는 중단점으로는 안 보이고 **Profiler** 로 봅니다.

이 부의 랩은 **프레임당 GC 할당 바이트**로 판정합니다. Profiler 의 GC Alloc 열과 같은 카운터(`GC Allocated In Frame`)를 `ProfilerRecorder` 로 읽고(`Assets/BugHunt/Core/GcMeter.cs`), 아무것도 하지 않는 프레임의 값(에디터 자체의 몫)을 뺍니다. 시간(ms)은 PC 마다 다르므로 참고로만 찍습니다.

## 10부에서 쓰는 도구

| 도구 | 하는 법 | 언제 |
|---|---|---|
| **Profiler → CPU Usage → Hierarchy** | Window → Analysis → Profiler. 아래 창을 Hierarchy 로 두고 **GC Alloc** 열로 정렬. Calls, Time ms 열도 함께 | 어느 함수가 매 프레임 쓰레기를 만드나, 몇 번 불리나 (모든 랩) |
| **Deep Profile** | Profiler 위쪽 Deep Profile 을 켜고 다시 플레이 | 내 함수 안의 어느 호출(String.Concat, Enumerable.Count, 박싱)이 할당하나. 많이 느려지니 볼 때만 (랩 51) |
| **ProfilerMarker** | `static readonly ProfilerMarker s_Find = new ProfilerMarker("Turret.Find");` → `using (s_Find.Auto()) { ... }` | Deep Profile 없이 의심 구간만 Profiler 에 이름을 붙여 보기 (랩 51, 52) |
| **ProfilerRecorder** | `ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame")` → `LastValue` | 코드에서 할당량을 재서 테스트로 지키기 (이 부의 판정) |
| Profiler → **Timeline** | GC.Collect 가 언제, 얼마나 메인 스레드를 막나 | "몇 초마다 끊긴다" 의 정체 |
| **Profile Analyzer** (패키지) | 프레임 여러 개를 모아 함수별 평균·최대 | 고치기 전후 비교 |

## 10부 랩 목록

| # | 이름 | 증상 | 새로 배우는 것 | 난이도 |
|---|---|---|---|---|
| 51 | 체력 표시줄 | 값이 그대로인데도 매 프레임 쓰레기가 쌓여 GC 로 끊긴다 | **문자열 연결, LINQ, 인터페이스 foreach 박싱**, 매 프레임 `GetComponent`, 바뀔 때만 만들기, Deep Profile, ProfilerMarker | ★★ |
| 52 | 포탑의 표적 찾기 | 맵에 오브젝트가 많아지자 프레임이 무너지고 GC 가 튄다 | **Update 의 `GameObject.Find` / `FindObjectsByType`**, 한 번 찾기, 등록부(OnEnable/OnDisable), `Camera.main` 은 이제 캐시됨 | ★★ |
| 53 | 탄환 풀 | 풀을 만들었는데도 매 발 Instantiate, 풀은 늘 비어 있다 | **돌려주지 않는 오브젝트 풀**, Release 와 초기화, `UnityEngine.Pool.ObjectPool<T>` | ★ |
| 54 | 화염 방사기 | 적이 피해를 받지 않는다(에러 없음). 이름을 고쳐도 할당이 남는다 | **`SendMessage` 의 이름 오타와 박싱·리플렉션 비용**, 인터페이스, `TryGetComponent` | ★ |
| 55 | 치유 오라 | 기능은 맞는데 매 프레임 `OverlapSphere` 가 배열을 만든다 | **배열을 돌려주는 API 와 NonAlloc 짝**, 버퍼 크기와 돌려받은 개수 | ★ |

> GC 할당 판정은 에디터와 `-batchmode` 모두에서 같은 카운터를 씁니다. 에디터에서 Inspector 나 Profiler 창이 매 프레임 다시 그려지면 그 몫도 섞이므로, 버퍼를 넉넉히(프레임당 128~256바이트) 두었습니다. 고친 코드는 0 바이트 근처가 나옵니다.

---

# 채점

모든 랩을 한 번에 돌려 랩마다 결과를 매기고 총점을 냅니다. 버그를 다 고쳤다고 생각되면 돌려 보세요. **버그가 있는 처음 상태에서는 0점이 정상**입니다.

| 결과 | 뜻 |
|---|---|
| **PASS** | 보통 입력과 숨은 입력 모두 통과 |
| **FAIL** | `[FAIL]` 이 하나라도 나옴, 또는 랩 도중 Console 에 에러 |
| **TIMEOUT** | HangGuard 가 "응답 없음" 을 선언(9부), 또는 랩 제한 시간(15초) 초과 |
| **CRASH** | 처리되지 않은 예외로 랩이 중단됨 (detail 에 예외 메시지 첫 줄) |
| **SKIP** | 이 환경에서는 판정할 수 없음: `-nographics` 의 랩 39, 오디오 장치가 없는 PC 의 랩 33~35. **총점에서 뺍니다** |

**총점 = PASS 수 / (전체 − SKIP)**. 만점이면 종료 코드 0, 아니면 1, 채점 자체가 안 되면(컴파일 에러 등) 2 입니다.

### 실행하는 두 가지 방법

1. **에디터:** 메뉴 **BugHunt → 채점 (모든 랩)** (Ctrl+Alt+G, 랩 실행 창 맨 위 버튼도 같음). BugHunt 씬에서 Play 를 시작해 랩 55개를 차례로 돌리고(1~2분), 끝나면 Play 를 멈추고 **채점 결과 창**을 엽니다. Console 에도 표가 찍힙니다(검색: `[채점]`). 결과 창은 **BugHunt → 채점 결과 창** 으로 다시 열 수 있습니다.
2. **명령줄:** **에디터를 닫고** 프로젝트 폴더에서
   ```bash
   grade.bat
   ```
   batchmode 로 Unity 를 띄워 채점하고 콘솔에 표를 찍습니다. Unity 는 `ProjectSettings/ProjectVersion.txt` 의 버전을 Unity Hub 설치 위치에서 찾습니다. 못 찾으면 `-Unity "경로\Unity.exe"` 또는 환경 변수 `BUGHUNT_UNITY` 를 쓰세요.

   | 옵션 | 뜻 |
   |---|---|
   | `-NoGraphics` | 그래픽 장치 없이(`-nographics`). 서버/CI 용. 랩 39 는 SKIP |
   | `-Labs 23,24,50` | 이 랩만 채점 |
   | `-Report 경로` | 결과 JSON 위치 (기본: 프로젝트 폴더의 `grade-report.json`) |
   | `-Lang ja` | 출력 언어 `ko` / `ja`. 없으면 랩 실행 창에서 고른 언어, 그다음 OS 언어 |
   | `-TimeoutMinutes 20` | 전체 제한 시간. 넘으면 Unity 를 강제로 끈다(아래) |

   Unity 로그는 `grade.log` 에 남습니다(컴파일 에러가 나면 여기서 `error CS` 를 찾으세요. grade.bat 도 처음 몇 줄을 보여 줍니다).

### 숨은 테스트 케이스

랩 결과의 숫자(기대값)만 보고 그 값을 코드에 박아 넣으면 보통 실행은 통과할 수 있습니다. 채점기는 보통 입력으로 PASS 한 랩을 **다른 입력(숨은 케이스)** 으로 한 번 더 돌립니다. 적의 수, 피해량, 좌표, 색, 세이브 값, 서버 가동 시간 같은 입력이 바뀌므로, 진짜 원인을 고쳤다면 그대로 통과하고 값을 박아 넣었다면 `[숨은 케이스] ...` 로 FAIL 합니다. 랩 코드에서 `Case(보통 값, 숨은 값)` 이 그 입력입니다(코드를 열면 보이지만, 거기에 맞추는 것은 의미가 없습니다). 입력이 하나뿐인 랩(10, 32~35, 40, 41, 43: 에셋 설정이나 엔진 동작을 고치는 랩)은 한 번만 돕니다.

### 결과 파일 grade-report.json

BugHunt 네 판(C++, C#, 언리얼, 유니티)이 같은 형식을 씁니다.

```json
{
    "edition": "unity",
    "version": 1,
    "total": 55,
    "passed": 53,
    "labs": [
        { "id": 1, "title": "감시탑", "part": 1, "result": "PASS", "ms": 3294, "detail": "" },
        { "id": 48, "title": "엘리베이터", "part": 9, "result": "TIMEOUT", "ms": 2003, "detail": "응답 없음: Passenger.RideTo: 엘리베이터가 오기를 기다림 (...)" }
    ]
}
```

- `total` 은 채점한 랩 수(SKIP 포함), `passed` 는 PASS 수, `part` 는 부 번호, `ms` 는 그 랩에 걸린 시간(숨은 케이스 포함)입니다.
- 채점기는 랩을 돌리기 **전에** 그 랩을 `TIMEOUT` 으로 적어 둔 파일을 먼저 씁니다. `HangGuard` 줄을 지워 에디터가 정말로 굳으면 grade.bat 이 제한 시간 뒤 Unity 를 끄고, 파일에는 그 랩이 `TIMEOUT`("채점이 이 랩에서 끝나지 않았습니다"), 그 뒤 랩들이 `FAIL`("실행하지 못함")로 남습니다.

---

# 부록 A: 멀티플레이(Netcode for GameObjects)는?

언리얼판에는 리슨 서버 랩이 있지만 이 프로젝트에는 넣지 않았습니다. Netcode for GameObjects 의 권한/RPC 버그를 판정하려면 서버와 클라이언트 **두 개의 인스턴스**가 필요한데(Multiplayer Play Mode 또는 빌드 + 에디터), 이 프로젝트의 자동 검증은 에디터 하나에서 PlayMode 테스트로 돌기 때문입니다. 넷코드 랩은 별도 프로젝트로 다루는 것이 좋습니다.

# 부록 B: 자동 검증 (Test Runner)

채점기([채점](#채점))와 별개로, 랩마다 PlayMode 테스트가 하나씩 있습니다. 채점기와 달리 숨은 케이스는 돌리지 않습니다.

랩 하나가 PlayMode 테스트 하나입니다(`Assets/BugHunt/Tests/LabTests.cs`). **버그가 있는 지금 상태에서는 실패하는 것이 정상**입니다.

**EditMode** 탭에는 번역 점검 테스트(`Assets/BugHunt/Tests/Editor/TranslationTests.cs`)가 있습니다. 코드의 한국어 출력 문장마다 일본어 표에 번역이 있는지, 보간 문자열(`$"..."`)에 한국어를 쓰지 않았는지(대신 `Loc.F`), 표에 쓰지 않는 키나 자리 표시자가 어긋난 번역이 없는지 봅니다. 이 테스트는 버그 상태에서도 통과합니다. 새 랩을 만들 때는 처음부터 두 언어로 쓰고 이 테스트로 확인하세요.

- 에디터: Window → General → **Test Runner** → **PlayMode** 탭 → Run All. 고친 랩만 초록색이 됩니다.
- 명령줄(에디터를 닫고, Unity.exe 경로는 설치 위치에 맞게):

```
"C:\Program Files\Unity\Hub\Editor\6000.3.27f2\Editor\Unity.exe" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults TestResults.xml -logFile -
```

| 실행 방식 | 판정되지 않는 랩 (`[SKIP]` → 테스트 Skipped) |
|---|---|
| 에디터에서 Play / Test Runner, `-batchmode` | 없음 |
| `-batchmode -nographics` | 39 (그래픽 장치가 없어 픽셀을 그릴 수 없음) |
| 오디오 출력 장치가 없는 PC/CI | 33, 34, 35 추가 |

이 저장소를 만들 때 Windows 11 + Unity 6000.3.27f2 에서 이렇게 확인했습니다.

| 상태 | `-batchmode` | `-batchmode -nographics` |
|---|---|---|
| 버그 그대로 | 55개 모두 실패 | 54개 실패, 39 Skipped |
| SOLUTIONS.md 의 수정 적용 | 55개 모두 통과 | 54개 통과, 39 Skipped |
| 수정을 되돌림 | 55개 모두 실패 | (같은 코드라 생략) |

채점기(`grade.bat`)도 같은 PC 에서 이렇게 확인했습니다.

| 상태 | `grade.bat` | `grade.bat -NoGraphics` |
|---|---|---|
| 버그 그대로 | 0 / 55 (CRASH 11, FAIL 40, TIMEOUT 4), 종료 코드 1 | 0 / 54 (SKIP 1: 랩 39), 종료 코드 1 |
| SOLUTIONS.md 의 수정 적용 | 55 / 55, 종료 코드 0 | 54 / 54 (SKIP 1: 랩 39), 종료 코드 0 |

- 보통 입력의 기대값만 코드에 박아 넣은 가짜 수정(랩 2, 50)은 `[숨은 케이스]` FAIL 로 걸러졌습니다.
- 랩 48 의 `HangGuard.Tick` 을 지워 에디터를 정말로 굳힌 상태에서 `grade.bat -TimeoutMinutes 1` 은 1분 뒤 Unity 를 끄고, 결과 파일에 랩 48 을 TIMEOUT, 그 뒤 랩을 FAIL 로 남겼습니다.

랩 7·12~14·28·30·43·53~55 는 `Time.captureDeltaTime` 으로 프레임 간격을 고정하므로 PC 성능과 상관없이 같은 결과가 나옵니다.

---

## 다시 풀고 싶을 때

이 폴더는 git 저장소입니다. 고친 뒤 `git restore Assets` 로 버그 상태로 되돌릴 수 있습니다.
랩 11·36 을 돌리면 `Rifle.asset`, `Crate.mat` 이 바뀐 채 저장될 수 있습니다(그게 그 랩의 교훈입니다). `git status` 로 확인하고 `git restore` 하세요.

</details>

<details>
<summary><b>日本語</b></summary>

[BugHunt](https://github.com/gusgh0157/BugHunt)(C++ コンソール版)と BugHunt UE(Unreal 版)の Unity 版です。
バグが**わざと仕込まれた**小さなゲームコードの集まりで、今回は **Unity でしか起きない**バグを集めています。

- **第1部: クラッシュ / 例外 (ラボ 1~4)**: コンソールに赤い行が出た瞬間から、スタックトレースをさかのぼって原因を探します。`MissingReferenceException`、`GetComponent` が返した null、Awake/OnEnable/Start の順序、非アクティブなオブジェクトのコルーチン。
- **第2部: 処理の流れの追跡 (ラボ 5~15)**: ゲームは普通に動いているのに**結果が間違っています**。イベント購読、実行順序、FixedUpdate、fake null、構造体のコピー、シリアライズされた値、ScriptableObject のように「Unity が代わりにやってくれること」の順序と寿命を追いかけます。30fps と 120fps で結果が変わる**フレームレート依存**のバグ(抜けている `Time.deltaTime`、毎フレームの `Lerp(a, b, 0.1f)`、Update の `fixedDeltaTime`、`timeScale = 0` で止まるメニュー)もここで扱います。
- **第3部: コンテナ (ラボ 16~22)**: インスペクターで縮めた配列、ループの途中で別のスクリプトが書き換えたリスト、複製すると消える `Dictionary`、順序を約束しない `FindObjectsByType`、渡した List を空にする API、破棄されたキー、サイズが変わった `NativeList` の古い `AsArray`。
- **第4部: 不正な参照とネイティブメモリ (ラボ 23~26)**: 例外なしでリークする `NativeArray`、「確かに Complete したのに」落ちるジョブ、次のフレームで消える `Allocator.Temp`、Domain Reload を切った Play で前回のオブジェクトをつかんだままの静的フィールド。
- **第5部: 物理 (ラボ 27~31)**: 薄い壁をすり抜ける速い弾、呼ばれない `OnTriggerEnter`、レイヤー番号とマスク、transform で動かした Rigidbody、移動した直後に元の場所を見るレイキャスト。物理演算デバッガー、ギズモ、`Debug.DrawRay` で確認します。
- **第6部: サウンド (ラボ 32~35)**: (⚠️ 音が出ます。音量を下げてから始めてください。)音が遅れて鳴る、聞こえない、途切れるバグ。ロード状態・仮想ボイス・再生位置を測って判定します。
- **第7部: 画像 / レンダリング (ラボ 36~39)**: 関係ない箱まで赤くなる、メモリがリークする、アイコンが入れ替わる、色が薄くなるバグ。マテリアルの値、Material オブジェクトの数、テクスチャ名、画面のピクセルで判定します。
- **第8部: アニメーション (ラボ 40~43)**: 攻撃モーションが出ない、画面外の足音が鳴らない、斬撃の判定が抜ける、手に持った武器がワンテンポ遅れるバグ。
- **第9部: 並行処理とフリーズ (ラボ 44~50)**: 2 つの `Task.Run` が割り込み合って限定品が 2 回売れる、ワーカースレッドで呼んだ `transform` の例外が Task の中で握りつぶされる、`Awaitable.BackgroundThreadAsync()` の後にメインスレッドに戻らないバグ。毎回同じ結果になるように再現します。続いて、クラッシュせずにエディターが固まる無限ループとデッドロック。すべて中断、スレッドウィンドウ、メインスレッドのコールスタックで閉じ込められた場所を探します。練習中に本当に固まらないよう、`HangGuard` が 2 秒後に「応答なし」を知らせて抜け出します。
- **第10部: パフォーマンス (ラボ 51~55)**: 結果は正しいのに遅いバグ。毎フレーム溜まる GC ゴミ(文字列、LINQ、ボックス化)、Update の `Find`、返却しないオブジェクトプール、`SendMessage`、配列を返す API。プロファイラーの GC アロケーション列、詳細プロファイル、ProfilerMarker で見つけ、`ProfilerRecorder` でバイト数を測って判定します。

> コンソール版・Unreal 版と同じルールです。**コードを目で流し読みして答えを探さないでください。** 第1部は**例外が起きた場所から**、第2部以降は**自分で止めた場所から値を確認しながら**追跡してください。
> コンソール版の第1部・第3部を先に終えているとよいです。コールスタックウィンドウ、ローカルウィンドウ、条件付きブレークポイント、トレースポイントの使い方は、そこで説明したとおりに使います。

## BugHunt シリーズ

同じ方式(わざと仕込んだバグをデバッガーで追跡する)で作った 4 つの版です。

| 版 | 扱うバグ | リポジトリ |
|---|---|---|
| C++ コンソール (標準 C++20、CMake) | 呼び出し履歴、フロー追跡、演算子、コンテナ、言語の落とし穴、インターフェースと継承、ファイル入出力、シリアライズ、ポインターとメモリ、非同期と並行性、ハング、パフォーマンス (ラボ 98 個 + チャレンジ 11 個) | [BugHunt](https://github.com/gusgh0157/BugHunt) |
| C# コンソール (.NET 10) | 🚧 **工事中** — 例外、フロー追跡、コンテナ、言語の落とし穴、インターフェースと継承、ファイル入出力、シリアライズ、非同期と並行性、リソースとメモリ、unsafe、ハング、パフォーマンス (ラボ 95 個 + チャレンジ 11 個)。負荷下の決定性・レビュー修正は PR 進行中 | [BugHuntCS](https://github.com/gusgh0157/BugHuntCS) |
| Unreal Engine (UE 5.8) | 🚧 **工事中** — クラッシュ、フロー追跡、マルチプレイ、サウンド、画像、アニメーション (公開中の master はラボ 21 個)。コンテナ、ポインターと寿命、物理、フレームレート、トランスフォーム、ハング、パフォーマンス、採点ツール、日本語版は作業ブランチで進行中 | [BugHuntUE](https://github.com/gusgh0157/BugHuntUE) |
| Unity (Unity 6) | 🚧 **工事中** — クラッシュ/例外、フロー追跡(フレームレート依存を含む)、コンテナ、参照とネイティブメモリ、物理、サウンド、画像、アニメーション、ハング、パフォーマンス、データレース (ラボ 55 個)。トランスフォーム部の追加と再配置は作業ブランチで進行中 | [BugHuntUnity](https://github.com/gusgh0157/BugHuntUnity) **(今見ているリポジトリ)** |

## バグのカテゴリー別カバー範囲

この版が扱うカテゴリーとラボです。**未収録** は Unity でよくある代表的な例なのに、まだラボがないという意味です。

| カテゴリー | ラボ | 扱う例 | 未収録 (代表例) |
|---|---|---|---|
| 参照・寿命・fake null | 1, 2, 4, 8, 21, 34 | 破棄されたオブジェクトへのアクセス、`GetComponent` の範囲、非アクティブなオブジェクトのコルーチン、`?.`・`??` と fake null、オブジェクトと一緒に消える音、Dictionary の破棄されたキー | `DontDestroyOnLoad` シングルトンの重複、シーン切り替え後に切れた参照 |
| 流れ・順序 | 3, 5, 6, 7, 43 | Awake/OnEnable/Start の順序、イベント購読の解除、Update 同士の順序、Update/FixedUpdate の回数、アニメーション評価後の LateUpdate | `LoadSceneAsync` のアクティブ化の順序、`OnDisable`/`OnDestroy` の順序 |
| C# の値と参照 | 9, 20 | 構造体のコピー(`List<Vector3>[i]`)、使い回すバッファを 2 か所で共有 | クロージャがループ変数をキャプチャ |
| シリアライズ・アセット | 10, 11, 18, 36 | シリアライズされた値が初期値を上書き、共有される ScriptableObject、`sharedMaterial`、シリアライズされないコンテナ | `[SerializeReference]` のポリモーフィズム、プレハブのオーバーライド、フィールド名の変更(`FormerlySerializedAs`) |
| フレームレート依存 | 7, 12, 13, 14, 15 | 1 フレームだけの入力を FixedUpdate が取りこぼす、`Time.deltaTime` の抜け、毎フレーム `Lerp(a, b, 0.1f)`、`fixedDeltaTime` の取り違え、`timeScale = 0` で止まる UI | Update での `AddForce`(物理ステップとずれる)、可変 deltaTime の大きなスパイク(最初のフレーム、ロード直後) |
| コンテナ | 16, 17, 18, 19, 20, 21, 22 | インスペクターで縮めた配列、ループ中の変更、シリアライズでの消失、`Find`/`GetComponents` の順序、結果を再利用する API、破棄されたキー、サイズが変わった `NativeList` の `AsArray` | キーにしたオブジェクトのハッシュが変わる、`List.Sort` の不安定ソート |
| ネイティブメモリ・ジョブ | 22, 23, 24, 25 | `Dispose` の抜け、ジョブの依存チェーン、`Allocator.Temp` の寿命、古い `AsArray` | Burst でだけ結果が変わる |
| 静的状態・Domain Reload | 26 | Domain Reload を切った Play の静的フィールド | 静的イベント購読の積み重なり |
| 物理 | 27, 28, 29, 30, 31 | 速い弾のトンネリング、Rigidbody のないトリガー、レイヤー番号とマスク、transform で動かした Rigidbody と補間、`SyncTransforms` | ジョイント(Joint)の暴走、非均一スケールのコライダー、`OnCollisionStay` のコスト |
| サウンド | 32, 33, 34, 35 | ロードの遅れ、仮想ボイス、オブジェクトの寿命、`Play`/`PlayOneShot` | AudioMixer のグループ・スナップショット、3D 減衰距離 |
| 画像・レンダリング | 36, 37, 38, 39 | 共有マテリアル、マテリアル複製のリーク、遅れて届いた非同期ロード、Linear/Gamma | ビルドで抜け落ちたシェーダーバリアント(ピンク)、半透明のソート、`RenderTexture` の解放 |
| アニメーション | 40, 41, 42, 43 | パラメーター名のタイプミス、カリングモード、トランジションとイベント、評価順序 | Root Motion とスクリプトによる移動の衝突、Write Defaults |
| データレース・スレッド | 44, 45, 46, 47 | メインスレッドで Task をブロックして待つ、check-then-act レース、ワーカースレッドの Unity API と握りつぶされた例外、`await` の後に実行されるスレッド | ジョブ内の static 共有状態、`volatile` のない終了フラグ、複数スレッドで共用するキャッシュ `Dictionary` |
| フリーズ | 47, 48, 49, 50 | yield のない while、訪問チェックのない BFS、`.Result` のデッドロック、float の累積による終了条件 | `lock` の順序が食い違ったデッドロック |
| パフォーマンス・GC | 37, 51, 52, 53, 54, 55 | マテリアル複製のリーク、毎フレームの GC アロケーション(文字列・LINQ・ボックス化)、Update の `Find`/`FindObjectsByType`、返却しないプール、`SendMessage`、配列を返す API | レンダリングコスト(バッチングの崩れ、過剰な影)、大きなアセットの同期ロードによる停止、毎フレームの Canvas 再構築 |
| UI | 未収録 | | レイキャストをさえぎる透明な画像、Canvas の再構築、解像度ごとのアンカー |
| 入力 | 未収録 | | Input System のアクションマップの無効化、フォーカスを失ったときに押されたままになるキー |
| ビルド専用 | 未収録 | | IL2CPP のコードストリッピング、プラットフォームごとのパス・大文字小文字 |
| マルチプレイ | 未収録 | | [付録 A](#付録-a-マルチプレイ-netcode-for-gameobjects-について) を参照 |

<details>
<summary>以前のラボ番号 → 新しい番号 (部の順序を基礎 → 応用に変える前に入手した場合)</summary>

| 以前の番号 | 新しい番号 | 内容 |
|---|---|---|
| 1~11 | 1~11 (そのまま) | クラッシュ/例外、処理の流れの追跡 |
| 12~15 | 32~35 | サウンド |
| 16~19 | 36~39 | 画像 / レンダリング |
| 20~23 | 40~43 | アニメーション |
| 24~27 | 23~26 | 不正な参照とネイティブメモリ |
| 28, 29, 30, 31 | 48, 49, 47, 50 | フリーズ (エレベーター、ダンジョンの経路探索、ショップの金貨、リプレイの早送り) |
| 32~38 | 16~22 | コンテナ |
| 39~43 | 27~31 | 物理 |
| 44~47 | 12~15 | フレームレート依存 (第2部に移動) |
| 48~52 | 51~55 | パフォーマンス |
| 53~55 | 44~46 | データレースとスレッド |

ファイル名(`LabNN_*.cs`)、クラス・名前空間(`BugHunt.Labs.LabNN`)、テスト名も新しい番号に従います。サウンドクリップは名前から番号を外しました(`lab12_blaster.wav` → `blaster.wav`)。

</details>

## 準備

必要なもの: **Unity 6000.3.27f2** (Unity 6.3 LTS)、**Visual Studio 2022 以降**(ワークロード「Unity によるゲーム開発」)または **Rider**。

1. Unity Hub → **追加 (Add)** → このフォルダー(`BugHuntUnity`)を選びます。初めて開くときは Library を作るので数分かかります。
2. **編集 → 環境設定 → 外部ツール → 外部のスクリプトエディター** (Edit → Preferences → External Tools → External Script Editor) を Visual Studio または Rider にします。
3. **アセット → C# プロジェクトを開く** (Assets → Open C# Project) で IDE を開きます(ソリューションファイルは Unity が作ってくれます)。
4. エディター右下のステータスバーにある**虫のアイコン(Code Optimization)** を押して**デバッグモード (Debug Mode)** に切り替えます。Release Mode ではブレークポイントで止まらなかったり、変数が「最適化されているため使用できません」と表示されたりします。(デバッガーをアタッチすると Unity が切り替えるかどうか聞いてくることもあります。)

> レンダーパイプラインは標準(Built-in)で、色空間は **Linear** です(ラボ 39)。パッケージは Test Framework と IDE 連携だけを使います。

### デバッガーをアタッチする方法

| 方法 | やり方 | いつ |
|---|---|---|
| Visual Studio | ツールバーの **Unity にアタッチ (Attach to Unity)** ▶、または **デバッグ → Unity デバッガーのアタッチ** → 一覧からエディターを選ぶ | 基本 |
| Rider | 右上の Run 構成 **Attach to Unity Editor** → 虫のボタン | Rider ユーザー |
| アタッチ後に Play まで一度に | VS: **Attach to Unity and Play** / Rider: **Attach to Unity Editor & Play** | 毎回 Play を押すのが面倒なとき |

- Unity の C# デバッガーは**すでに起動しているエディターにアタッチする**方式です。Unreal のように IDE からエディターを起動するわけではありません。アタッチした状態で Play を押すとブレークポイントで止まります。
- デバッガーがアタッチされているときにラボで例外が起きると、IDE がその行で止まります。止まらない場合は VS: **デバッグ → ウィンドウ → 例外設定** の *Common Language Runtime Exceptions*、Rider: **Run → View Breakpoints → Any Exception** の "Break on unhandled" を確認してください。
- ブレークポイントで止まっている間、Unity エディターは**応答なし**の状態になります。正常です。IDE で F5(続行)を押せば戻ります。

## ラボを実行する

1. メニュー **BugHunt → ラボ実行ウィンドウ** (Ctrl+Alt+B)。
2. 番号を押すと `Assets/Scenes/BugHunt.unity` を開いて Play し、そのラボをすぐに実行します。(シーンでそのまま Play を押すと、ゲームビューの左側にラボ一覧のボタンが出ます。)
   > ⚠️ **サウンドラボ(第6部、ラボ 32~35)は音を出します。実行前にシステムの音量を下げ、ヘッドホンを使っている場合は耳から外してから始めてください。** ボタンに「音に注意」が付いています。
3. 結果はゲームビューの右側と **コンソール ウィンドウ**(ウィンドウ → 一般 → コンソール / Window → General → Console)に出ます。コンソールの検索欄に `[BugHunt]` と入れると、ラボの結果だけが表示されます。

```
[BugHunt] ==== ラボ 5: 戦闘ログウィンドウ ====
[BugHunt] [FAIL] 開き直したウィンドウのログの行数 (期待: 1, 実際: 2)
[BugHunt] [FAIL] 出した通知の合計 (10, 7) (期待: 2, 実際: 4)
[BugHunt] ==== ラボ 5 の結果: PASS 1 / FAIL 3 ====
```

**同じラボを繰り返し実行するとき**: コードを直して Unity に戻ると自動で再コンパイルされます。そのあと **BugHunt → 最後のラボを再実行 (Ctrl+Alt+R)**。再生中なら再生を止めずにすぐ実行し直します。

**全部直したら**: **BugHunt → 採点 (全ラボ)** (Ctrl+Alt+G) またはコマンドラインの `grade.bat` で全ラボを一度に採点します。下の [採点](#採点) を見てください。

> ⚠ **再生中にコードを直すと**、Unity はデフォルト設定(環境設定 → 一般 → 再生中のスクリプト変更: *再コンパイルして再生を続行* / Preferences → General → Script Changes While Playing: *Recompile And Continue Playing*)では再生中に再コンパイルし、静的変数とシリアライズされていないフィールドを消してしまいます。混乱したら**再生を止めてから直してください。**

### 言語 (한국어 / 日本語)

ラボ実行ウィンドウ、コンソールの判定行、採点結果は韓国語と日本語で表示できます。メニュー名は 2 つの言語を並べて書いています(`랩 실행 창 · ラボ実行ウィンドウ`)。言語は次の順で決まります。

1. コマンドライン `-bughuntLang ko` / `-bughuntLang ja` (採点: `grade.bat -Lang ja`)
2. ラボ実行ウィンドウの一番上にある **언어 / 言語** で選んだ言語 (EditorPrefs に記憶)
3. OS の言語が日本語なら日本語
4. それ以外は韓国語

ラボコードのコメントと折りたたまれたヒントは、1 つのファイルの中に韓国語、その後に日本語の順で並んでいます。出力される文はコードに韓国語で書かれていて、その文が `Assets/BugHunt/Lang/Ja/LabNN.cs` にある日本語の表のキーになっています。`grade-report.json` は `title`・`detail` だけが選んだ言語で出力され、`result` の値とキーはそのままです。

### コンソール ウィンドウの読み方

- 1 行を**クリック**すると、下にスタックトレースが表示されます。**一番上が例外の起きた場所**で、下に行くほど「誰が呼んだか」です。
- 青いリンク(`Assets/BugHunt/Labs/Lab01_Tower.cs:50`)を押すと、IDE でその行が開きます。行を**ダブルクリック**しても同じです。
- `UnityEngine.` で始まる行はエンジンのコード、`BugHunt.Labs.` で始まる行が自分のコードです。エンジンの行は飛ばしてかまいませんが、**どのエンジン機能を経由してきたか**(AddComponent、SetupCoroutine、アニメーションイベント...)は読んでおいてください。それが「なぜこのタイミングで呼ばれたか」の答えです。
- 右上のボタン: **折りたたむ** (Collapse、同じ行をまとめる)、**エラーで一時停止** (Error Pause、エラーが起きた瞬間に再生を一時停止)、そしてログ/警告/エラーのフィルター。警告(⚠)をオフにしないでください(ラボ 40)。

---

# 第1部: クラッシュ / 例外

## 追跡の 4 ステップ (コンソール版と同じ)

1. **どこで止まったか?** スタックトレースで**自分のコード(`BugHunt.Labs.`)が最初に出てくる行**を探す。
2. **何がおかしいか?** ローカル/ウォッチ式ウィンドウで null、破棄されたオブジェクト、おかしな値を探す。
3. **その値はどこから来たか?** コールスタックを下りながら出どころを探す。
4. **本当の原因は?** おかしな値が**最初に作られた場所**を直す。

```
記録表 (第1部用)
止まった関数:            
おかしな値:              
その値が来た場所:         
経由したエンジン機能:     
本当の原因:              
修正:                   
```

## Unity の 3 つの例外

| 例外 / メッセージ | 意味 |
|---|---|
| `NullReferenceException` | C# の変数が本当に空。`GetComponent`、`Find` は見つからないと黙って null を返す |
| `MissingReferenceException: The object of type 'X' has been destroyed but you are still trying to access it` | 変数はそのままなのに **Destroy されたオブジェクト**を指している。C# の殻だけが残った状態 |
| `UnassignedReferenceException: The variable X of Y has not been assigned` | インスペクターで割り当てるべきフィールドを空のままにしている |

## やってみよう: ラボ 1 (監視塔)

1. IDE から Unity にデバッガーをアタッチし、**BugHunt → ラボ実行ウィンドウ → 1**。
2. デバッガーが前面に出て止まります: **MissingReferenceException**。(デバッガーなしで実行した場合は、コンソールの赤い行をクリックしてください。)
3. コールスタック(VS: Ctrl+Alt+C)の一番上が `Tower.Update` です。その下はありません: **Unity が毎フレーム呼ぶ Update** なので、自分のコードの中に「誰が呼んだか」はありません。→ *止まった関数*
4. ウォッチ式に `Target` を入れて展開します。名前も Health も見えるのに、`Target == null` は **true** です。→ *おかしな値*: 破棄された敵
5. 誰が破棄したのかを探します。`Enemy.TakeDamage` の `Destroy(gameObject)` の行で **F9** を押してブレークポイントを置き、再生を止めてからもう一度ラボ 1。最初に止まったとき、コールスタックには誰がいますか? → *その値が来た場所*
6. 監視塔の Update は「自分が撃って倒したときだけ」ターゲットを切り替えます。他の誰かが破棄した場合は? → *本当の原因*
7. 直したら **Ctrl+Alt+R** → `[PASS]` が 2 つ。
8. ボーナス: デバッガーなしで **コンソール → エラーで一時停止** (Console → Error Pause) をオンにしてラボ 1 を実行してみてください。例外が起きたフレームで再生が一時停止し、ヒエラルキーとインスペクターでその瞬間を確認できます。

## 第1部 ラボ一覧

| # | 名前 | 症状 | 新しく学ぶこと | 難易度 |
|---|---|---|---|---|
| 1 | 監視塔 | 爆弾が敵を吹き飛ばしたとたん、例外が大量に出る | **MissingReferenceException**、破棄されたオブジェクト、Destroy にブレークポイント、エラーで一時停止 (Error Pause) | ★ |
| 2 | ヘッドショット | 胴体は大丈夫なのに、頭を撃つと NullReferenceException | **GetComponent は自分のオブジェクトしか**見ない、`GetComponentInParent`、`TryGetComponent` | ★ |
| 3 | スコアボード | マネージャーと UI を作ったとたん NullReferenceException | **Awake / OnEnable / Start の順序**、AddComponent の中ですぐ呼ばれる Awake・OnEnable | ★★ |
| 4 | リスポーン | 倒れたプレイヤーがいつまでもリスポーンしない(エラー 1 行) | **非アクティブなオブジェクトのコルーチン**、コルーチンの持ち主、「エラーを消しても FAIL」 | ★★ |

## ヒントの見方

コンソール版と同じです。各ラボの `.cs` ファイルの**一番下**に `#region ヒント N` として折りたたまれています。

1. ラボのファイルを最初に開いたら、**Ctrl+M, Ctrl+O**(VS) / **Ctrl+Shift+NumPad -**(Rider)ですべて折りたたみます。
2. 行き詰まったら**ヒント 1** だけを開きます。それでも行き詰まったら、次のヒントを 1 つずつ。

ラボのコードは `Assets/BugHunt/Labs/LabNN_*.cs` です(ラボ 10・11 は `Lab10/`、`Lab11/` フォルダーにクラスがもう 1 つあります)。`Assets/BugHunt/Core/`(ラボ実行機能、第9部の `HangGuard`、第10部の `GcMeter`、言語切り替えの `Loc`)と `Editor/`、`Lang/`、`Tests/` にはバグはありません。
全部解けたら [SOLUTIONS.ja.md](SOLUTIONS.ja.md) で答え合わせをしてください。

---

# 第2部: 処理の流れの追跡 (クラッシュなし)

コンソール版の第3部と同じ 4 ステップです。Unity では**「自分で呼んでいないのに呼ばれるコード」**(Update、FixedUpdate、イベント、コルーチン、シリアライズ、アニメーションイベント)がほとんどなので、「いつ、誰が、何回呼んだか」を見るツールが特に重要です。

## 処理の流れの追跡 4 ステップ

1. **期待を先に書く。**「HP バーは毎フレーム、ヒーローの HP と同じでなければならない」のように。
2. **間違った結果に一番近いところから止める。**
3. **期待と最初に食い違う場所を探す。** 毎フレーム実行されるコードは F10 の代わりにトレースポイント、`Debug.Log($"{Time.frameCount} ...")`、プロファイラーで記録する。
4. **「最初に食い違った場所」を直す。**

```
記録表 (第2部以降)
間違った結果:              
止めた位置 / 使ったツール:   
期待値 → 実際の値:          
最初にずれた場所:           
本当の原因:                
修正:                     
```

## 第2部で使うツール

| ツール | やり方 | いつ |
|---|---|---|
| **コンソール + フレーム番号** | `Debug.Log($"{Time.frameCount} hp={hp}", this)` — 2 番目の引数にオブジェクトを渡すと、行をクリックしたときにヒエラルキーでそのオブジェクトが強調表示される | すべてのラボ |
| **トレースポイント** | ブレークポイントを右クリック → アクション、`{Time.frameCount} {damage}` のような式、「コードの実行を続行する」にチェック | コードを書き換えずに呼び出しの順序・回数を記録 (ラボ 5~7) |
| **条件付きブレークポイント / ヒットカウント** | ブレークポイントを右クリック → 条件、例: `Time.frameCount > 100 && Health <= 0` | 毎フレーム呼ばれる関数の特定の瞬間だけ |
| **`Debug.Break()`** | コードに 1 行。そのフレームが終わるとエディターが**一時停止** | デバッガーなしで「その瞬間」のヒエラルキー/インスペクターを見たいとき。一時停止中は **ステップ (Ctrl+Alt+P)** で 1 フレームずつ |
| **インスペクターのデバッグモード** | インスペクタータブの ⋮ → **デバッグ** (Debug) | private フィールドやインスタンス ID まで見る |
| **スクリプト実行順序** | 編集 → プロジェクト設定 → スクリプト実行順序 (Edit → Project Settings → Script Execution Order) | スクリプト同士の Update の順序 (ラボ 6) |
| **プロファイラー** | ウィンドウ → 分析 → プロファイラー (Window → Analysis → Profiler、Ctrl+7)、CPU モジュール → ヒエラルキー / **タイムライン** | 1 フレームの中の順序、関数が何回呼ばれたか(呼び出し / Calls) (ラボ 7, 43) |
| **テストランナー** | ウィンドウ → 一般 → テストランナー (Window → General → Test Runner) → PlayMode | 直したラボをまとめて確認 |

## やってみよう: ラボ 5 (戦闘ログウィンドウ)

1. **ラボ実行ウィンドウ → 5**。何も止まらずに `[FAIL] 開き直したウィンドウのログの行数 (期待: 1, 実際: 2)`。
2. **期待を書きます。** 騎士は 10、5、7、3 のダメージを受けます。ウィンドウが開いているのは 10 と 7 を受けるときだけなので、`HandleDamaged` は **2 回**(10, 7)呼ばれるはずです。
3. `Lab05_CombatLog.cs` の `HandleDamaged` の最初の行で **F9**。デバッガーをアタッチしてもう一度ラボ 5。
4. 止まるたびにローカルウィンドウの `damage` とウォッチ式 `this.isActiveAndEnabled` を書き留めて F5。何回止まりますか?
5. 期待(10, 7)と違う値で**最初に**止まったのはどれかを見てください。そのときウィンドウは開いていましたか? → *最初にずれた場所*
6. コールスタックで `Fighter.TakeHit` をダブルクリック → ウォッチ式に `Damaged.GetInvocationList().Length`。開く/閉じる/もう一度開く/破棄するたびに、いくつになっていますか?
7. 再生を止めて直したら Ctrl+Alt+R → [PASS] が 4 つ。

## 第2部 ラボ一覧

| # | 名前 | 症状 | 新しく学ぶこと | 難易度 |
|---|---|---|---|---|
| 5 | 戦闘ログウィンドウ | ウィンドウを開き直すとログが 2 行ずつ出て、ウィンドウを破棄した後も通知が出る | **イベント購読の解除漏れ**、OnEnable/OnDisable の対、トレースポイント、`GetInvocationList` | ★ |
| 6 | HP バー | HP バーが毎フレーム 1 コマずつ遅れる | **Update 同士の順序**、スクリプト実行順序、`[DefaultExecutionOrder]`、LateUpdate | ★★ |
| 7 | ジャンプ | 120Hz モニターだとジャンプが反応しない | **Update と FixedUpdate の回数の違い**、1 フレームだけの入力、プロファイラーの呼び出し数、`Time.captureDeltaTime` | ★★ |
| 8 | 弓兵 | 倒れたかかしでスコアを稼ぎ続ける | **fake null**、`==` と `?.` `??=` `is null` の違い、Destroy はフレームの終わり | ★★ |
| 9 | 巡回ルート | 地面に合わせたはずのウェイポイントへ、巡回兵が飛んでいく | **構造体のコピー**、`List<Vector3>[i].Set()`、`transform.position.x =` がコンパイルできない理由 | ★★ |
| 10 | バランス調整パッチ | コードで HP を 150 に上げたのに、ゲームでは 100 | **シリアライズされた値がフィールドの初期値を上書きする**、プレハブの YAML の読み方、生成 → デシリアライズ → Awake の順序 | ★★ |
| 11 | 武器データ | 他人のマガジンの弾が減り、再生を止めても減ったまま | **ScriptableObject は共有されるアセット**、再生モード後も残る値、`git status` でアセットの変更を確認 | ★★ |

## フレームレート依存 (ラボ 12~15)

開発 PC では完璧なのに、**ほかの機器でだけ**速すぎたり遅すぎたりするバグです。Update は 1 秒に何回呼ばれるかわからず(30fps のノート PC、144Hz のモニター、発熱で性能が落ちたスマートフォン)、その回数に頼るコードは機器ごとに結果が変わります。前のラボ 7(1 フレームだけの入力を FixedUpdate が取りこぼす)は Update と FixedUpdate の**回数の違い**を、この節のラボは **1 回に動く量**を扱います。

この節のラボは、同じコードを **30fps と 120fps で 1 回ずつ**実行し(`Time.captureDeltaTime` でフレーム間隔を固定)、2 つの結果が仕様どおりかを確認します。採点ツールの隠しケースは 24fps と 144fps です。ラボ 15 は `timeScale = 0` を扱うので、実時間で待ちます。

### 使うツール

| ツール | やり方 | いつ |
|---|---|---|
| **fps を変えて再現** | `Application.targetFrameRate = 30;` (VSync を切って: 品質 → VSync 数 = 同期しない / Quality → VSync Count = Don't Sync)、または `Time.captureDeltaTime = 1f / 30f;` で間隔を固定 | 「自分の PC では動くのに」を自分の PC で再現する |
| ゲームビューの **統計情報** (Stats) | ゲームビュー右上の 統計情報 | 今の fps とフレーム時間 |
| トレースポイントで**フレームごとに値を出力** | `{Time.frameCount} {Time.deltaTime} {transform.position.x}` | 1 フレームでどれだけ変わるか、それが deltaTime に比例しているか (ラボ 12, 13) |
| ウォッチ式に**時間の値を並べる** | `Time.deltaTime`、`Time.fixedDeltaTime`、`Time.unscaledDeltaTime`、`Time.timeScale` | どの時間が fps と timeScale によって変わるか (ラボ 14, 15) |
| プロファイラー → CPU → **タイムライン** | 1 フレームに Update と FixedUpdate が何回ずつ入っているか | 固定間隔の物理とフレームの関係 |

| 時間の値 | Update では | FixedUpdate では | `timeScale = 0` なら |
|---|---|---|---|
| `Time.deltaTime` | 前のフレームから経過したゲーム内時間 | `fixedDeltaTime` と同じ値 | 0 |
| `Time.fixedDeltaTime` | 設定値(デフォルト 0.02)そのまま。**fps とは無関係** | 同じ | そのまま(FixedUpdate は止まる) |
| `Time.unscaledDeltaTime` | 前のフレームから経過した実時間 | 実時間 | 実時間 |

### ラボ一覧

| # | 名前 | 症状 | 新しく学ぶこと | 難易度 |
|---|---|---|---|---|
| 12 | ホバーカー | 30fps では半分、120fps では 2 倍の速度 | **フレームあたりの値と秒あたりの値**、`* Time.deltaTime`、単位を名前に入れる | ★ |
| 13 | 追従カメラ | 30fps ではカメラが大きく遅れ、120fps ではぴったりくっつく | **毎フレームの `Lerp(a, b, 0.1f)` はフレーム数のべき乗**、`1 - exp(-k·dt)` の指数減衰、`SmoothDamp` | ★★ |
| 14 | スタミナ | 「時間を掛けたのに」fps によって回復の速さが違う | **`fixedDeltaTime` は Update で使うと定数**、FixedUpdate の中の `deltaTime` | ★ |
| 15 | ポーズメニュー | `timeScale = 0` でメニューが出てこず、カウントダウンが止まる | **timeScale が 0 でも Update は回る**、`unscaledDeltaTime`、`WaitForSecondsRealtime`、アニメーターの スケールされていない時間 (Unscaled Time) | ★ |

---

# 第3部: コンテナ

配列、`List`、`Dictionary`、`NativeList` は C# でもよくあるバグの原因ですが、Unity では**エンジンがコンテナに触れる場所**がさらにあります。インスペクターは配列のサイズをいつでも変え、シリアライズは知らないコンテナを黙って捨て、`Destroy` はコンテナに入っているオブジェクトを fake null に変え、`Find`/`GetComponents` は順序を約束せず、アロケーションを減らす API は渡したコンテナを空にしてから詰め直します。

## 第3部で使うツール

| ツール | やり方 | いつ |
|---|---|---|
| **例外設定: スローされたときに中断** | VS: デバッグ → ウィンドウ → 例外設定 → Common Language Runtime Exceptions で `System.IndexOutOfRangeException`、`System.InvalidOperationException` にチェック。Rider: Run → View Breakpoints → Exception Breakpoints | 例外がキャッチされる場所ではなく**スローする場所**で止める (ラボ 16, 17) |
| ウォッチ式に**インデックスとサイズを並べる** | `i`、`Slots.Length`、`Labels.Count`、`view[0]` | 範囲外のインデックス (ラボ 16)、長さを変える書き込みの直後 (ラボ 22) |
| ウォッチ式で**参照を比較** | `alphaRoster == bravoRoster`、`(object)key == null` と `key == null` を並べる | 同じコンテナを 2 つの変数が指しているか (ラボ 20)、fake null のキー (ラボ 21) |
| **インスペクターのデバッグモード** | インスペクター右上の ⋮ → デバッグ。private のシリアライズフィールドまで見える | このフィールドはシリアライズされるか。インスペクターに出ないものは Instantiate・保存でも抜け落ちる (ラボ 18) |
| `JsonUtility.ToJson(component)` の出力 | コンソールに出力してみる | シリアライズされるフィールドだけが出る。出ないフィールド = 複製・保存で消えるフィールド (ラボ 18) |
| コンテナに**ブレークポイント + コールスタック** | `List.Remove` を呼ぶ行にブレークポイント → コールスタックに外側の `foreach` が見えるか | ループの途中で誰がリストを書き換えたか (ラボ 17) |

## 第3部 ラボ一覧

| # | 名前 | 症状 | 新しく学ぶこと | 難易度 |
|---|---|---|---|---|
| 16 | ショートカットスロット | デザイナーがスロット配列を 3 つに縮めたら IndexOutOfRangeException、空のショートカットで ArgumentOutOfRangeException | **定数の長さとデータの長さ**、インスペクターは配列のサイズを変える、外から来た番号の範囲チェック、`OnValidate` | ★ |
| 17 | 空爆 | 空爆で敵が倒れると InvalidOperationException、後ろのほうの敵は無傷 | **ループの途中で別のスクリプトがリストを書き換える**、コピー・逆順ループ、`RemoveAll` | ★ |
| 18 | ショップの価格表 | 複製したり、保存して読み込んだりしたショップの価格表と陳列棚が空っぽになる(警告なし) | **Unity のシリアライズが捨てるコンテナ**(`Dictionary`、`List<List<T>>`、`T[,]`)、`ISerializationCallbackReceiver`、`[Serializable]` ラッパー | ★★ |
| 19 | 巡回順序 | 警備兵が Order 番号と関係ない順番で回る。マップごと、実行環境ごとに合ったり合わなかったりする | **`FindObjectsByType` / `GetComponents` の順序は約束ではない**、自分でソート、インスペクターの配列 | ★ |
| 20 | 部隊編成 | 2 つの部隊の名簿がまったく同じ | **結果を詰めてくれる API は渡した List を空にする**(`GetComponentsInChildren(List)`)、使い回すバッファの持ち主 | ★★ |
| 21 | ヘイトリスト | 脅威 1 位が死んだらボスが MissingReferenceException、リストの数もそのまま | **破棄されたオブジェクトが Dictionary のキーに残る**(fake null のキー)、死んだキーの片付け、`OnDestroy` での通知 | ★★ |
| 22 | 足跡 | 1 歩歩いただけで出発点を読む行で "has been deallocated" | **`NativeList.AsArray()` はコピーではなくビュー**、長さを変える書き込みのたびに無効、キャッシュしない | ★★ |

> ラボ 22 の例外は、エディターのコレクション安全チェックのおかげで出ます(第4部と同じ)。リリースビルドでは解放されたメモリを読んで、おかしな値やクラッシュになります。`NativeArray` の `Dispose` 漏れは第4部のラボ 23 で扱います。

---

# 第4部: 不正な参照とネイティブメモリ

C# では GC がメモリを片付けてくれますが、Unity には **GC が知らないメモリ**があります。`NativeArray` のようなネイティブコンテナは Unity のネイティブヒープにあり、C# の変数はそのアドレスを持つ小さな struct にすぎないため、誰がいつ解放するか(`Allocator`、`Dispose`)と、誰が今使っているか(ジョブ)を自分で管理しなければなりません。その代わり、エディターには**安全システム(AtomicSafetyHandle)** があり、解放されたバッファやジョブが使用中のバッファに触れるとすぐに例外を投げてくれます。そして静的(static)フィールドは、Domain Reload を切ると**前回の Play のオブジェクト**をつかんだままになります。

第1部の MissingReferenceException、第2部の fake null・構造体のコピー、第7部の sharedMaterial とは別の種類です。ここでは「寿命を誰が決めるのか」を見ます。

## 第4部で使うツール

| ツール | やり方 | いつ |
|---|---|---|
| **Leak Detection Level** | 編集 → 環境設定 → **Jobs** → Leak Detection Level → *Enabled With Stack Trace* (コードでは `NativeLeakDetection.Mode`) | 再生を止めたり再コンパイルしたりするときに出る `Leak Detected` に、**確保した場所のスタック**が付く (ラボ 23) |
| **安全システムの例外を読む** | 例外メッセージに**ジョブ名とバッファ(フィールド)名**が出る: `The previously scheduled job X writes to ... X.Count` | ジョブとメインスレッドが同じバッファに触れるとき (ラボ 24)、解放されたバッファ (ラボ 25) |
| プロファイラー → CPU → **タイムライン** | Worker スレッドの行でジョブがいつ実行されるか、メインスレッドが `JobHandle.Complete` でどれだけ待っているか | ジョブの依存関係 (ラボ 24) |
| **Memory Profiler** | スナップショットを 2 枚 → Compare → Native / Unity Subsystems | 増える一方のネイティブメモリ (ラボ 23) |
| ウォッチ式で `NativeArray` を展開 | `IsCreated`、`Length`。解放されたバッファを展開すると、値の代わりに安全システムの例外メッセージが表示される | この変数はまだ生きているか |
| **再生モード開始時設定** (Enter Play Mode Settings) | 編集 → プロジェクト設定 → エディター → When entering Play Mode (*Reload Domain and Scene* / *Reload Scene only* / *Do not reload Domain or Scene*) | 静的フィールドが Play をまたいで生き残るか (ラボ 26) |

## 第4部 ラボ一覧

| # | 名前 | 症状 | 新しく学ぶこと | 難易度 |
|---|---|---|---|---|
| 23 | 経路キャッシュ | 例外は出ないのに、長く遊ぶとメモリが増え、止めると `Leak Detected` | **NativeArray は GC が片付けない**、`Dispose` / `OnDestroy`、Leak Detection Level、構造体のコピーと安全ハンドル | ★ |
| 24 | 見張り塔の視界 | ジョブを追加したとたん InvalidOperationException (「確かに Complete したのに?」) | **JobHandle の依存チェーン**、最後のハンドルを Complete、安全システムのメッセージを読む、プロファイラーのタイムラインの Worker | ★★ |
| 25 | 出現計画表 | ウェーブを始めた次のフレーム、1 行目から "has been deallocated" | **Allocator の寿命**(Temp / TempJob / Persistent)、フレームの終わりに巻き戻される Temp | ★★ |
| 26 | 2 回目のプレイ | 2 回目に Play を押すとウェーブが続きから始まり、レーダーが前回の敵を指す | **Domain Reload を切った Enter Play Mode**、静的状態を元に戻す `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` | ★★ |

> ラボ 23・25 は、エディターのコレクション安全チェック(`ENABLE_UNITY_COLLECTIONS_CHECKS`)で判定します。エディターと開発ビルドでは有効になっています。リリースビルドでは、同じバグが例外なしで**おかしな値やクラッシュ**として現れます(だからエディターで捕まえる必要があります)。
>
> ラボ 26 は、Play をもう一度押したことをラボの中で再現しています(前回のオブジェクトを消して `[RuntimeInitializeOnLoadMethod]` たちを呼ぶ)。このプロジェクトの実際の設定(ラボ出力の 1 行目)は *Reload Domain and Scene* なので、本物の Play/Stop を繰り返しても再現しません。自分で体験してみたい場合は、設定を *Reload Scene only* に変えて Play → Stop → Play をしてみてください(実験が終わったら元に戻すこと)。

---

# 第5部: 物理

物理エンジン(PhysX)は Unity の transform とは**別に自分のシーンを持ち**、フレームごとではなく**固定間隔(fixedDeltaTime、デフォルト 0.02 秒)ごとに瞬間移動するように**計算します。そのため物理のバグはたいてい例外なしで、「当たらない」「呼ばれない」「違うものに当たる」「ガタガタ震える」という形で現れます。ブレークポイントよりも、**目で見るツール**(物理演算デバッガー、ギズモ、`Debug.DrawRay`)が先です。

## 第5部で使うツール

| ツール | やり方 | いつ |
|---|---|---|
| **物理演算デバッガー** (Physics Debugger) | ウィンドウ → 分析 → **物理演算デバッガー** (Window → Analysis → Physics Debugger)。シーンビューにコライダーを種類別の色(静的 / キネマティック / Dynamic / トリガー)で描く。*Queries* タブはレイキャスト・オーバーラップを、*接触* (Contacts) タブは接触点を表示する | このコライダーが物理エンジンにどの種類として入っているか (ラボ 28)、レイキャストがどこに当たったか (ラボ 29)、コライダーが実際にどこにあるか (ラボ 31) |
| **`Debug.DrawRay` / `Debug.DrawLine`** | `Debug.DrawRay(origin, dir * length, Color.red, 5f)`。最後の引数は表示しておく秒数。シーンビュー(とゲームビューの ギズモ ボタン)で見える | レイの方向と長さ (ラボ 29)、物理ステップごとに弾が進んだ距離 (ラボ 27) |
| **ギズモ** (Gizmos) | コンポーネントに `void OnDrawGizmos() { Gizmos.DrawWireSphere(transform.position, radius); }` | 検知範囲やトリガーの大きさを常に表示しておく |
| 一時停止 + **1 フレームずつ** | 再生中に一時停止(Ctrl+Shift+P) → ステップ(Ctrl+Alt+P) | 物理ステップの間に何が変わるか (ラボ 27, 30, 31) |
| **レイヤーの衝突表** (Layer Collision Matrix) | プロジェクト設定 → 物理 → レイヤーの衝突表 | 2 つのレイヤーが互いに衝突・トリガーするようになっているか (ラボ 28) |
| ウォッチ式で **2 進数** | `Convert.ToString(mask, 2)` | レイヤーマスクのどのビットが立っているか (ラボ 29) |
| ウォッチ式 `collider.bounds.center` | transform.position と並べる | transform と物理シーンの位置が同じか (ラボ 31) |

## 第5部 ラボ一覧

| # | 名前 | 症状 | 新しく学ぶこと | 難易度 |
|---|---|---|---|---|
| 27 | レールガン | 速い弾が薄い壁をそのまま貫通する(衝突 0 回) | **トンネリング**、物理ステップごとに瞬間移動、`CollisionDetectionMode.Continuous / ContinuousDynamic / ContinuousSpeculative` | ★ |
| 28 | トゲの罠 | 罠の上を通っても `OnTriggerEnter` が呼ばれない | **衝突メッセージにはどちらかに Rigidbody が必要**、静的コライダー同士、Kinematic Rigidbody、衝突アクションの表 | ★ |
| 29 | タレットの照準 | Enemy レイヤーだけを撃つように指定したのに、窓ガラスに当たる | **レイヤー番号 ≠ レイヤーマスク**、`1 << layer`、`LayerMask` フィールド、`LayerMask.GetMask` | ★ |
| 30 | 動く足場 | 補間 (Interpolate) をオンにすると足場がほとんど動かず、上の箱も運ばれない | **Rigidbody を transform で動かさない**、補間が transform を上書きする、Kinematic の `MovePosition` は速度を伝える | ★★ |
| 31 | パズルのリセット | リセットしたそのフレームに箱をつかむと空をつかみ、元の場所をつかむと箱をつかむ | **transform と物理シーンの同期タイミング**、`Physics.SyncTransforms`、`rb.position` | ★★ |

> 第5部の判定は、衝突回数、トリガー回数、レイキャストが当たった名前、最終的な位置で行います。物理は `-batchmode -nographics` でも同じように動くので、SKIP はありません。ラボ 28・30 は `Time.captureDeltaTime` で 60fps に固定します。

---

# 第6部: サウンド

> **⚠️ この部のラボは音を出します。実行前にシステムの音量を下げ、ヘッドホンを使っている場合は耳から外してから始めてください。** ラボの実行中は全体の音量(`AudioListener.volume`)を 0.1 に下げ、クリップも振幅 0.3 以下の単音ですが、ラボ 33 では数十個の音が一斉に鳴ります。ラボ実行ウィンドウのこのラボのボタンには「音に注意」が、コンソールには `[注意] 音が出ます。音量を下げてください` が最初に出力されます。

「音が遅れて鳴る」「聞こえない」「途切れる」は耳ですぐにわかりますが、バグを**再現して判定する**には数字が必要です。これらのラボは、クリップのロード状態(`AudioClip.loadState`)、仮想ボイスかどうか(`AudioSource.isVirtual`)、再生中かどうかと再生位置(`isPlaying`、`time`)を測って `[PASS]`/`[FAIL]` を出します。

## 第6部で使うツール

| ツール | やり方 | いつ |
|---|---|---|
| オーディオのインポート設定 | プロジェクトウィンドウで .wav を選択 → インスペクター: ロードタイプ (Load Type)、Preload Audio Data、Load In Background、圧縮 (Compression) | 最初の再生の遅れ、メモリ (ラボ 32) |
| プロファイラー → **オーディオ** モジュール | 再生中の音の数、仮想ボイスの数、オーディオメモリ、チャンネル一覧 | 同時再生の上限 (ラボ 33) |
| プロジェクト設定 → **オーディオ** | 最大実音声数 / 最大仮想音声数 (Max Real Voices / Max Virtual Voices)、DSPバッファサイズ | 上限そのもの (ラボ 33) |
| ゲームビューの **オーディオをミュート** (Mute Audio) トグル | ゲームビュー上部のツールバー | 音を消したまま判定だけを見る |
| `Debug.Break()` + ヒエラルキー | 音が途切れるフレームで一時停止 → AudioSource が付いたオブジェクトがまだあるか | 途切れ (ラボ 34) |

## 第6部 ラボ一覧

| # | 名前 | 症状 | 新しく学ぶこと | 難易度 |
|---|---|---|---|---|
| 32 | ブラスターの発射音 | 最初の 1 発だけ、発射音がマズルフラッシュより遅れる | **Preload Audio Data / Load In Background**、`loadState`、`LoadAudioData()` | ★★ |
| 33 | ボスのセリフ | 兵士が多いとボスのセリフが聞こえない | **最大実音声数 (Max Real Voices)**、仮想ボイス、`AudioSource.priority`、プロファイラーのオーディオモジュール | ★★ |
| 34 | 爆発する樽 | 爆発音が鳴った瞬間に途切れる | 音の寿命 = **AudioSource が付いたオブジェクトの寿命**、`PlayClipAtPoint` | ★ |
| 35 | 作戦ブリーフィング | 被弾音のせいでブリーフィングが途切れる | **`Play()` と `PlayOneShot()`** の違い、チャンネル(AudioSource)を分ける | ★ |

> **ヘッドレス実行:** ラボ 33~35 は、音が実際に再生されないと判定できません。オーディオ出力デバイスがある PC では `-batchmode -nographics` でも判定できました(下の「自動検証」)。出力デバイスがないサーバー/CI では比較用の音が再生されないため `[SKIP]` になります。ラボ 32 はロード状態だけを見るので、デバイスに関係ありません。

---

# 第7部: 画像 / レンダリング

「色が変わらない」「アイコンが別のアイテムのものになっている」「色が仕様書と違う」は目ですぐにわかりますが、目分量では「少し違う気がする」で止まりがちです。これらのラボは、**レンダラーに割り当てられたマテリアルの値**、**Material オブジェクトの数**、**スロットに入ったテクスチャの名前**、**実際に描かれたピクセル**を調べて判定します。

## 第7部で使うツール

| ツール | やり方 | いつ |
|---|---|---|
| インスペクターの マテリアル (Materials) | 再生中にヒエラルキーでオブジェクトを選択。名前の末尾が `(Instance)` なら複製 | 共有マテリアルか複製か (ラボ 36, 37) |
| **フレームデバッガー** (Frame Debugger) | ウィンドウ → 分析 → フレームデバッガー (Window → Analysis → Frame Debugger) → 有効にする (Enable)。ドローコールを選ぶとシェーダープロパティの値が見える | 実際に渡された `_Color`、バッチングが崩れた理由 (ラボ 37, 39) |
| プロファイラー → **メモリ** モジュール | Objects の数、Texture/Material の数 | 増える一方のオブジェクト (ラボ 37) |
| **Memory Profiler** | パッケージマネージャー → `com.unity.memoryprofiler` をインストール → ウィンドウ → 分析 → Memory Profiler → スナップショットを 2 つ → Compare | 誰がいくつ残っているか (ラボ 37) |
| プレイヤー設定の **色空間** (Color Space) | 編集 → プロジェクト設定 → プレイヤー → その他の設定 (Edit → Project Settings → Player → Other Settings) | ガンマ / Linear (ラボ 39) |
| テクスチャインポート設定の **sRGB (カラーテクスチャ)** | テクスチャを選択 → インスペクター | 色のテクスチャはオン、ノーマル/マスクはオフ |
| RenderDoc | ゲームビューの ⋮ → RenderDoc をロード → カメラアイコン | GPU が実際に描いたものを 1 フレーム丸ごと |

## 第7部 ラボ一覧

| # | 名前 | 症状 | 新しく学ぶこと | 難易度 |
|---|---|---|---|---|
| 36 | 木箱のハイライト | 1 つの箱にマウスを乗せたのに、全部赤くなる | **`sharedMaterial` はアセットそのもの**、`material` は複製、アセットがディスクに保存されてしまう問題 | ★ |
| 37 | 補給箱の色 | ウェーブごとにメモリが増えていく | **`renderer.material` の複製のリーク**、**MaterialPropertyBlock**、Memory Profiler、フレームデバッガー | ★★ |
| 38 | インベントリのアイコン | スクロールすると、名前はポーションなのにアイコンは剣 | 非同期ロード(`Resources.LoadAsync`)のコールバックの**遅れた到着**、再利用されるスロット、キャッシュヒットは即時 | ★★ |
| 39 | チームカラー | チームカラーが色あせて明るい(0x00、0xFF は合っている) | **Linear / Gamma 色空間**、`SetColor` と `SetVector` の違い、フレームデバッガーで値を見る | ★★ |

> **ヘッドレス実行:** ラボ 39 はカメラで描いたピクセルを読み戻して判定するため、グラフィックスデバイスが必要です。`-nographics` では `[SKIP]` になります。ラボ 36~38 はグラフィックスデバイスなしでも判定できます。

---

# 第8部: アニメーション

アニメーションのバグは「たまに合っていない気がする」という形で報告されます。これらのラボは **Animator の状態**、**アニメーションイベントが呼ばれた回数**、**フレームごとの 2 つの Transform の距離**を測って判定します。バグの一部は C# ではなく **アニメーションのアセット**(コントローラー、クリップ)にあります。そのため、アニメーターウィンドウとアニメーションウィンドウをデバッガーのように使う方法を学びます。

## 第8部で使うツール

| ツール | やり方 | いつ |
|---|---|---|
| **アニメーターウィンドウのライブ表示** | ウィンドウ → アニメーション → アニメーター (Window → Animation → Animator)。再生中にヒエラルキーで Animator が付いたオブジェクトを選ぶと、現在のステート、進行バー、トランジション、パラメーターの値がリアルタイムで見える | すべてのラボ。ラボが終わるとオブジェクトが消えるので、ブレークポイントや `Debug.Break()` で止めたまま見る |
| アニメーションウィンドウ | ウィンドウ → アニメーション → アニメーション (Window → Animation → Animation、Ctrl+6)。クリップを選ぶとカーブとイベント(タイムライン上の小さな棒) | イベントが何秒にあるか (ラボ 41, 42) |
| トランジションのインスペクター | アニメーターウィンドウで矢印をクリック → 終了時間あり (Has Exit Time)、終了時間 (Exit Time)、遷移間隔 (Transition Duration)、中断要因 (Interruption Source) | ステートがいつ切り替わるか (ラボ 42) |
| Animator コンポーネントの **カリングモード** (Culling Mode) | インスペクター | 画面外で止まるか (ラボ 41) |
| 一時停止 + **1 フレームずつ** | 再生の横の ⏸ → ⏭ (Ctrl+Alt+P) | トランジションをフレーム単位で見る |
| プロファイラーのタイムライン | `PreLateUpdate.DirectorUpdateAnimationBegin/End` の位置 | アニメーションは Update と LateUpdate の間に評価される (ラボ 43) |

## 第8部 ラボ一覧

| # | 名前 | 症状 | 新しく学ぶこと | 難易度 |
|---|---|---|---|---|
| 40 | 攻撃ボタン | ボタンを押しても攻撃モーションが出ない(例外なし) | **Animator のパラメーター名のタイプミス**は警告だけ、`StringToHash`、アニメーターウィンドウのパラメーター | ★ |
| 41 | 背後の足音 | 画面外にいる警備兵の足音が鳴らない | **カリングモード**、画面外でアニメーションイベントが消える、シーンビューも「見ている」 | ★★ |
| 42 | 斬撃の判定 | 剣を振っているのにダメージが入らない | **トランジションがイベントより先に終わる**、Exit Time / Duration の読み方、アニメーションウィンドウのイベント | ★★ |
| 43 | 手に持った武器 | たいまつがワンテンポ遅れて手についてくる | **アニメーションは Update の後に評価される**、LateUpdate、プロファイラーのタイムライン | ★★ |

> ラボ 42 を直すには、`Assets/BugHunt/Resources/BugHunt/Animation/Swordsman.controller` をアニメーターウィンドウで修正します。C# を直すラボではありません。

---

# 第9部: 並行処理とフリーズ

2 つのスレッドが同じデータに触れたり(データレース)、お互いを待ったり(デッドロック)、終わらないループに閉じ込められたり(無限ループ)するバグです。前半のラボ 44~46 は結果が間違っているか例外が黙って握りつぶされ、後半のラボ 47~50 はエディターが固まります。どちらの場合も、**スレッドウィンドウとコールスタック**で「今どのスレッドがどこにいるか」を見るのが出発点です。

## データレースとスレッド (ラボ 44~46)

Unity のゲームコードはほとんどがメインスレッド 1 つで動きますが、重い処理を `Task.Run`、`Thread`、`Awaitable.BackgroundThreadAsync()` に任せた瞬間、**2 つのスレッドが同じデータに触れる**バグが生まれます。後ろのラボ 47(メインスレッドで `.Result` で待ってデッドロック)が「お互いを待つ」なら、この節のラボは「お互いに割り込む」と「違うスレッドで Unity を呼ぶ」です。

レースは本来たまにしか起きません。練習になるよう、**ラボごとに毎回同じ結果**が出るようにしてあります: ラボ 44 は、ラボが入れた決済のまねごとが同時に来たリクエスト同士をお互い最大 1 秒待たせて「両方がチェックを通過した状態」を確実に作り、ラボ 45・46 はスレッドが切り替わること自体がバグなので常に再現します。

### 使うツール

| ツール | やり方 | いつ |
|---|---|---|
| **スレッドウィンドウ** | VS: デバッグ → ウィンドウ → スレッド (Ctrl+Alt+H)、Rider: Debug ウィンドウの Threads | ブレークポイントで止まった場所はメインスレッドか、同じ行にスレッドがいくつあるか (すべてのラボ) |
| **並列スタック / タスクウィンドウ** | VS: デバッグ → ウィンドウ → 並列スタック、タスク | どの Task がどこまで進んだか、終わったか、例外で終わったか (ラボ 44, 45) |
| ブレークポイントの**フィルター** / トレースポイント | ブレークポイント → 条件 → フィルター `ThreadId == 1` / トレースポイント `{Environment.CurrentManagedThreadId} Stock={Stock}` | スレッドごとに割り込む順序を見る。トレースポイントは止まらないので、タイミングを変えにくい (ラボ 44) |
| ウォッチ式 `Thread.CurrentThread.ManagedThreadId` | await の前後の行で比べる | await の次の行がどのスレッドで動いているか (ラボ 46) |
| **例外設定: スローされたときに中断** | Common Language Runtime Exceptions 全体にチェック | Task の中で握りつぶされる例外を、スローする場所で捕まえる (ラボ 45) |
| ウォッチ式 `task.Status`、`task.Exception` | Faulted / RanToCompletion、InnerException | 「何も起きなかった」の正体 (ラボ 45) |

### Job System の安全システムが捕まえるものと捕まえられないもの

第4部(ラボ 24, 25)の安全システムは、**NativeContainer を使うジョブ**についてだけレースを防いでくれます。

| 状況 | 安全システム | この版では |
|---|---|---|
| 2 つのジョブが同じ `NativeArray` に同時に書き込む(依存関係なし) | **捕まえる** (スケジュール時に InvalidOperationException、ジョブとフィールドの名前が出る) | ラボ 24 |
| ジョブが書き込み中の `NativeArray` をメインスレッドが読む(Complete 前) | **捕まえる** | ラボ 24 |
| 解放された `NativeArray` にジョブやメインスレッドが触れる | **捕まえる** (ObjectDisposedException) | ラボ 22, 25 |
| `[ReadOnly]` なしで 2 つのジョブが同じ配列を読むだけ | 捕まえる(書き込みとみなして防ぐ)。`[ReadOnly]` を付ければ許可 | — |
| `Task.Run` / `Thread` が C# オブジェクト(`List`、フィールド、`Dictionary`)を共用する | **捕まえられない** (マネージヒープは監視対象ではない) | ラボ 44 |
| ワーカースレッドで Unity API(`transform`、`gameObject`)を呼ぶ | 安全システムではなく**エンジンが** UnityException を投げる。ただし Task の中なら握りつぶされる | ラボ 45, 46 |
| ジョブの中で static フィールドを共用する | **捕まえられない** (Burst は static への書き込みをコンパイルエラーで防ぐが、Burst を切るとそのまま動く) | — |
| `NativeDisableParallelForRestriction`、`NativeDisableContainerSafetyRestriction` を付けたフィールド | わざと**切ったもの**なので捕まえられない | — |

### ラボ一覧

| # | 名前 | 症状 | 新しく学ぶこと | 難易度 |
|---|---|---|---|---|
| 44 | 限定販売 | 残り 1 本の剣を 2 人が買えてしまった。在庫 -1 | **確認してから行動する(check-then-act)レース**、`lock`、共有 `List` への同時 Add、安全システムが捕まえられないマネージヒープ | ★★ |
| 45 | バックグラウンド経路探索 | 経路探索は最後まで動くのにロボットが動かない。コンソールは静か | **ワーカースレッドの Unity API**、Task の中で握りつぶされる例外、`await Task.Run(...)` の後はメインスレッド | ★★ |
| 46 | セーブデータの読み込み | バックグラウンド解析に変えたら "can only be called from the main thread" | **await の次の行のスレッド**、`Awaitable.BackgroundThreadAsync` / `MainThreadAsync`、UnitySynchronizationContext | ★★ |

## フリーズ: 無限ループとデッドロック (ラボ 47~50)

例外もログもなく、**エディター全体が固まる**バグです。Unity のゲームコードはほとんどすべてメインスレッド 1 つで動くので、メインスレッドが 1 フレームの中から抜け出せないと、Update も、レンダリングも、エディターの UI も止まります。タスクマネージャーには「応答なし」、コンソールは静か。こうしたバグは、**固まったその瞬間のメインスレッドのコールスタック**を見ないと解けません。

### 本当に固まったときにすること

1. **CPU を見る。** タスクマネージャーで Unity が CPU 1 コア分(例: 8 コアなら約 12%)を使い切っていれば**無限ループ**、0% 近くなら**デッドロック**(お互いを待っているだけ)です。メモリが増え続けるなら、無限ループの中で何かを溜め込んでいます。
2. **デバッガーをアタッチしてすべて中断。** 固まったエディターにもデバッガーはアタッチできます。VS: **デバッグ → すべて中断 (Ctrl+Alt+Break)**、Rider: **Pause Program**(⏸)。
3. **スレッドウィンドウでメインスレッドを選ぶ。** VS: デバッグ → ウィンドウ → **スレッド (Ctrl+Alt+H)**、Rider: Debug ウィンドウの Threads ドロップダウン。`Main Thread`(またはスレッド ID 1)をダブルクリックすると、コールスタックウィンドウがそのスレッドに切り替わります。一番上の自分のコードの行が、閉じ込められている場所です。
4. **F10 を何回か押してみる。** 同じループの中をぐるぐる回っていれば無限ループ。ローカルウィンドウで、終了条件に使われている値が**変わっているかどうか**を見てください(変わらなければ永遠に終わりません)。
5. デッドロックなら、**並列スタック**(VS: デバッグ → ウィンドウ → 並列スタック)と **タスク** ウィンドウで、誰が何を待っているかを見ます。

### このプロジェクトでは実際には固まりません (HangGuard)

練習のたびにタスクマネージャーで Unity を終了させるわけにはいかないので、フリーズ系ラボ(47~50)のループには `HangGuard.Tick(...)` が 1 行、ブロックして待つ場所には `HangGuard.Wait(task, ...)` が入っています(`Assets/BugHunt/Core/HangGuard.cs`)。

- `Tick` は**何も変えず、何も返しません。** ループの条件と本体はバグのままです。バグを直してループが正常に終われば、`Tick` はあってもなくても同じです(何も隠しません)。
- 1 フレームの中で **2 秒**(時間の予算)を超えたり、決めておいた**反復の上限**(ラボ 49 のように回るたびにメモリを食うループ)を超えたりすると、`[FAIL] 応答なし: ...` を出力し、`HangException` を投げて抜け出します。ラボの結果は **TIMEOUT** です。
- その例外の**スタックトレース = メインスレッドが閉じ込められていた場所**です。「すべて中断」で見るコールスタックと同じです。
- `Wait` は `task.Result` と同じです。予算内に Task が終わらなければ、デッドロックとみなして抜け出します。
- `Tick` があるループの中でブレークポイントに止まっていた時間は、予算に含めません(1 行ずつステップ実行しながら調べても「応答なし」になりません)。
- **自分で「すべて中断」を押してみたい場合は**、メニュー **BugHunt → フリーズ系ラボ: 30 秒間固まったままにする** をオンにしてください。30 秒間本当に固まったままになるので、その間にデバッガーをアタッチして上の 1~5 のステップを試せます。(ラボ 49 はメモリの関係で、反復の上限で先に打ち切られます。)採点ツールはこの設定に関係なく 2 秒を使います。

> ⚠ **`HangGuard.Tick` / `HangGuard.Wait` の行を消さないでください。** 消したままバグが残っていると、エディターが本当に固まります。そうなったら、タスクマネージャーで Unity を終了して開き直してください(保存していないシーンは失われます)。直すときに `Wait` は消してもかまいません。ブロックして待たないように変えるのが、ラボ 47 の正解だからです。

### ラボ一覧

| # | 名前 | 症状 | 新しく学ぶこと | 難易度 |
|---|---|---|---|---|
| 47 | ショップの金貨 | ショップを開いた瞬間、永遠に止まる(CPU 0%) | **メインスレッドで `.Result` で待つと UnitySynchronizationContext のせいでデッドロック**、`await` が戻ってくる場所、並列スタック / タスクウィンドウ、`ConfigureAwait(false)` は半分だけの解決 | ★★ |
| 48 | エレベーター | エレベーターを呼んだ瞬間、ゲーム全体が止まる(CPU 1 コア 100%) | **Update が変えてくれる値を while で待つ**、`yield return` のないコルーチンのループ、コルーチンはスレッドではない、`WaitUntil`、すべて中断 + スレッドウィンドウ | ★ |
| 49 | ダンジョンの経路探索 | 扉を閉めて道が途切れると止まり、メモリが跳ね上がる | **グラフ探索の訪問チェック**、ループと双方向の辺、「フリーズ + メモリ増加」という指紋、条件付きブレークポイント `queue.Count > 100` | ★★ |
| 50 | リプレイの早送り | サーバーを 1 週間以上動かし続けると、早送りで止まる | **float の累積による終了条件**、大きな数に小さな数を足しても変わらない、整数のティック / double、`Time.timeAsDouble` | ★★ |

---

# 第10部: パフォーマンス (GC アロケーションと繰り返しの検索)

結果は正しいのに**遅い**バグです。Unity の C# は、GC(ガベージコレクター)がゴミを集めて片付ける間メインスレッドを止めるので、**毎フレーム少しずつゴミを作るコード**は数秒ごとにフレームをカクッと途切れさせます。また、「結果がほぼ毎回同じ」検索(`Find`、`FindObjectsByType`)を毎フレーム繰り返すと、オブジェクトの数に比例して遅くなります。こうしたバグはブレークポイントでは見えず、**プロファイラー**で見ます。

この部のラボは**フレームあたりの GC アロケーションのバイト数**で判定します。プロファイラーの GC アロケーション列と同じカウンター(`GC Allocated In Frame`)を `ProfilerRecorder` で読み(`Assets/BugHunt/Core/GcMeter.cs`)、何もしないフレームの値(エディター自体の分)を引きます。時間(ms)は PC ごとに違うので、参考として出力するだけです。

## 第10部で使うツール

| ツール | やり方 | いつ |
|---|---|---|
| **プロファイラー → CPU Usage → ヒエラルキー** | ウィンドウ → 分析 → プロファイラー。下のペインをヒエラルキーにして **GC アロケーション** (GC Alloc) 列でソート。呼び出し (Calls)、時間 (ミリ秒) (Time ms) 列もあわせて | どの関数が毎フレームゴミを作っているか、何回呼ばれているか (すべてのラボ) |
| **詳細プロファイル** (Deep Profile) | プロファイラー上部の 詳細プロファイル をオンにして再生し直す | 自分の関数の中のどの呼び出し(String.Concat、Enumerable.Count、ボックス化)がアロケーションしているか。かなり遅くなるので見るときだけ (ラボ 51) |
| **ProfilerMarker** | `static readonly ProfilerMarker s_Find = new ProfilerMarker("Turret.Find");` → `using (s_Find.Auto()) { ... }` | 詳細プロファイル (Deep Profile) なしで、怪しい区間だけにプロファイラー上の名前を付けて見る (ラボ 51, 52) |
| **ProfilerRecorder** | `ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame")` → `LastValue` | コードでアロケーション量を測り、テストで守る (この部の判定) |
| プロファイラー → **タイムライン** | GC.Collect がいつ、どれだけメインスレッドを止めているか | 「数秒ごとに途切れる」の正体 |
| **Profile Analyzer** (パッケージ) | 複数フレームを集めて関数ごとの平均・最大 | 直す前と後の比較 |

## 第10部 ラボ一覧

| # | 名前 | 症状 | 新しく学ぶこと | 難易度 |
|---|---|---|---|---|
| 51 | 体力ゲージ | 値は変わっていないのに毎フレームゴミが溜まり、GC でカクつく | **文字列連結、LINQ、インターフェース foreach のボックス化**、毎フレームの `GetComponent`、変わったときだけ作る、詳細プロファイル (Deep Profile)、ProfilerMarker | ★★ |
| 52 | タレットの標的探し | マップのオブジェクトが増えるとフレームレートが崩れ、GC が跳ねる | **Update での `GameObject.Find` / `FindObjectsByType`**、一度だけ探す、登録簿(OnEnable/OnDisable)、`Camera.main` は今はキャッシュされる | ★★ |
| 53 | 弾のプール | プールを作ったのに毎発 Instantiate され、プールはいつも空 | **返却しないオブジェクトプール**、Release と初期化、`UnityEngine.Pool.ObjectPool<T>` | ★ |
| 54 | 火炎放射器 | 敵がダメージを受けない(エラーなし)。名前を直してもアロケーションが残る | **`SendMessage` の名前のタイプミスと、ボックス化・リフレクションのコスト**、インターフェース、`TryGetComponent` | ★ |
| 55 | 回復オーラ | 機能は正しいのに、毎フレーム `OverlapSphere` が配列を作る | **配列を返す API と NonAlloc の対**、バッファのサイズと返ってきた個数 | ★ |

> GC アロケーションの判定は、エディターでも `-batchmode` でも同じカウンターを使います。エディターでインスペクターやプロファイラーのウィンドウが毎フレーム再描画されるとその分も混ざるので、余裕(フレームあたり 128~256 バイト)を持たせています。直したコードは 0 バイト近くになります。

---

# 採点

全ラボを一度に実行してラボごとに結果を付け、総合点を出します。バグを全部直したと思ったら実行してみてください。**バグがある最初の状態では 0 点が正常**です。

| 結果 | 意味 |
|---|---|
| **PASS** | 通常の入力と隠し入力の両方に合格 |
| **FAIL** | `[FAIL]` が 1 つでも出た、またはラボの途中でコンソールにエラー |
| **TIMEOUT** | HangGuard が「応答なし」を宣言した(第9部)、またはラボの制限時間(15 秒)を超えた |
| **CRASH** | 処理されなかった例外でラボが中断された (detail に例外メッセージの 1 行目) |
| **SKIP** | この環境では判定できない: `-nographics` でのラボ 39、オーディオデバイスがない PC でのラボ 33~35。**総合点から除きます** |

**総合点 = PASS の数 / (全体 − SKIP)**。満点なら終了コード 0、そうでなければ 1、採点自体ができなければ(コンパイルエラーなど) 2 です。

### 実行する 2 つの方法

1. **エディター:** メニュー **BugHunt → 採点 (全ラボ)** (Ctrl+Alt+G、ラボ実行ウィンドウの一番上のボタンも同じ)。BugHunt シーンで Play を始めて 55 個のラボを順に実行し(1~2 分)、終わると Play を止めて**採点結果ウィンドウ**を開きます。コンソールにも表が出力されます(検索: `[採点]`)。結果ウィンドウは **BugHunt → 採点結果ウィンドウ** で開き直せます。
2. **コマンドライン:** **エディターを閉じて**、プロジェクトフォルダーで
   ```bash
   grade.bat
   ```
   batchmode で Unity を起動して採点し、コンソールに表を出力します。Unity は `ProjectSettings/ProjectVersion.txt` のバージョンを Unity Hub のインストール場所から探します。見つからなければ `-Unity "パス\Unity.exe"` または環境変数 `BUGHUNT_UNITY` を使ってください。

   | オプション | 意味 |
   |---|---|
   | `-NoGraphics` | グラフィックスデバイスなしで(`-nographics`)。サーバー/CI 用。ラボ 39 は SKIP |
   | `-Labs 23,24,50` | このラボだけを採点 |
   | `-Report パス` | 結果 JSON の場所 (デフォルト: プロジェクトフォルダーの `grade-report.json`) |
   | `-Lang ja` | 出力言語 `ko` / `ja`。なければラボ実行ウィンドウで選んだ言語、次に OS の言語 |
   | `-TimeoutMinutes 20` | 全体の制限時間。超えると Unity を強制終了する(下記) |

   Unity のログは `grade.log` に残ります(コンパイルエラーが出たら、ここで `error CS` を探してください。grade.bat も最初の数行を表示します)。

### 隠しテストケース

ラボ結果の数字(期待値)だけを見てその値をコードに直接書き込めば、通常の実行は通過できるかもしれません。採点ツールは、通常の入力で PASS したラボを**別の入力(隠しケース)** でもう一度実行します。敵の数、ダメージ量、座標、色、セーブの値、サーバーの稼働時間といった入力が変わるので、本当の原因を直していればそのまま通過し、値を書き込んでいれば `[隠しケース] ...` で FAIL します。ラボコードの `Case(通常の値, 隠しの値)` がその入力です(コードを開けば見えますが、それに合わせることに意味はありません)。入力が 1 つしかないラボ(10、32~35、40、41、43: アセットの設定やエンジンの動作を直すラボ)は 1 回だけ実行します。

### 結果ファイル grade-report.json

BugHunt の 4 つの版(C++、C#、Unreal、Unity)は同じ形式を使います。

```json
{
    "edition": "unity",
    "version": 1,
    "total": 55,
    "passed": 53,
    "labs": [
        { "id": 1, "title": "監視塔", "part": 1, "result": "PASS", "ms": 3294, "detail": "" },
        { "id": 48, "title": "エレベーター", "part": 9, "result": "TIMEOUT", "ms": 2003, "detail": "応答なし: Passenger.RideTo: エレベーターが来るのを待機 (...)" }
    ]
}
```

- `total` は採点したラボの数(SKIP を含む)、`passed` は PASS の数、`part` は部の番号、`ms` はそのラボにかかった時間(隠しケースを含む)です。
- 採点ツールはラボを実行する**前に**、そのラボを `TIMEOUT` と書いたファイルを先に書き出します。`HangGuard` の行を消してエディターが本当に固まると、grade.bat は制限時間の後に Unity を終了し、ファイルにはそのラボが `TIMEOUT`(「採点がこのラボで終わりませんでした」)、その後のラボが `FAIL`(「実行できなかった」)として残ります。

---

# 付録 A: マルチプレイ (Netcode for GameObjects) について

Unreal 版にはリッスンサーバーのラボがありますが、このプロジェクトには入れていません。Netcode for GameObjects の権限/RPC のバグを判定するにはサーバーとクライアントの **2 つのインスタンス**が必要ですが(Multiplayer Play Mode またはビルド + エディター)、このプロジェクトの自動検証はエディター 1 つの中で PlayMode テストとして動くからです。ネットコードのラボは別のプロジェクトで扱うのがよいでしょう。

# 付録 B: 自動検証 (テストランナー)

採点ツール([採点](#採点))とは別に、ラボごとに PlayMode テストが 1 つずつあります。採点ツールと違い、隠しケースは実行しません。

ラボ 1 つが PlayMode テスト 1 つです(`Assets/BugHunt/Tests/LabTests.cs`)。**バグがある今の状態では失敗するのが正常**です。

**EditMode** タブには翻訳チェックのテスト(`Assets/BugHunt/Tests/Editor/TranslationTests.cs`)があります。コードの韓国語の出力文それぞれに日本語の表の翻訳があるか、補間文字列(`$"..."`)に韓国語を書いていないか(代わりに `Loc.F`)、表に使われていないキーやプレースホルダーが食い違った翻訳がないかを確認します。このテストはバグがある状態でも通過します。新しいラボを作るときは最初から 2 つの言語で書き、このテストで確認してください。

- エディター: ウィンドウ → 一般 → **テストランナー** → **PlayMode** タブ → Run All。直したラボだけが緑になります。
- コマンドライン(エディターを閉じて。Unity.exe のパスはインストール場所に合わせて):

```
"C:\Program Files\Unity\Hub\Editor\6000.3.27f2\Editor\Unity.exe" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults TestResults.xml -logFile -
```

| 実行方法 | 判定されないラボ (`[SKIP]` → テストは Skipped) |
|---|---|
| エディターで Play / テストランナー、`-batchmode` | なし |
| `-batchmode -nographics` | 39 (グラフィックスデバイスがなく、ピクセルを描けない) |
| オーディオ出力デバイスがない PC/CI | 33, 34, 35 を追加 |

このリポジトリを作るときに、Windows 11 + Unity 6000.3.27f2 で次のように確認しました。

| 状態 | `-batchmode` | `-batchmode -nographics` |
|---|---|---|
| バグのまま | 55 個すべて失敗 | 54 個失敗、39 は Skipped |
| SOLUTIONS.ja.md の修正を適用 | 55 個すべて合格 | 54 個合格、39 は Skipped |
| 修正を元に戻す | 55 個すべて失敗 | (同じコードなので省略) |

採点ツール(`grade.bat`)も同じ PC で次のように確認しました。

| 状態 | `grade.bat` | `grade.bat -NoGraphics` |
|---|---|---|
| バグのまま | 0 / 55 (CRASH 11, FAIL 40, TIMEOUT 4)、終了コード 1 | 0 / 54 (SKIP 1: ラボ 39)、終了コード 1 |
| SOLUTIONS.ja.md の修正を適用 | 55 / 55、終了コード 0 | 54 / 54 (SKIP 1: ラボ 39)、終了コード 0 |

- 通常の入力の期待値だけをコードに書き込んだ偽の修正(ラボ 2, 50)は、`[隠しケース]` の FAIL ではじかれました。
- ラボ 48 の `HangGuard.Tick` を消してエディターを本当に固まらせた状態で、`grade.bat -TimeoutMinutes 1` は 1 分後に Unity を終了し、結果ファイルにラボ 48 を TIMEOUT、その後のラボを FAIL として残しました。

ラボ 7・12~14・28・30・43・53~55 は `Time.captureDeltaTime` でフレーム間隔を固定するので、PC の性能に関係なく同じ結果になります。

---

## もう一度解きたいとき

このフォルダーは git リポジトリです。直した後に `git restore Assets` でバグのある状態に戻せます。
ラボ 11・36 を実行すると、`Rifle.asset`、`Crate.mat` が変更されたまま保存されることがあります(それがそのラボの教訓です)。`git status` で確認して `git restore` してください。

</details>
