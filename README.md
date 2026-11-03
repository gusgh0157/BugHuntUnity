# BugHunt Unity — 유니티 C# 버그 추적 실습

[BugHunt](https://github.com/gusgh0157/BugHunt)(C++ 콘솔판)와 BugHunt UE(언리얼판)의 Unity 버전입니다.
버그가 **일부러 심어진** 작은 게임 코드 모음이고, 이번에는 **Unity 에서만 생기는** 버그들입니다.

- **1부: 크래시 / 예외 (랩 1~4)**: Console 에 빨간 줄이 뜬 순간, 스택 트레이스를 거슬러 올라가 원인을 찾습니다. `MissingReferenceException`, `GetComponent` 가 준 null, Awake/OnEnable/Start 순서, 꺼진 오브젝트의 코루틴.
- **2부: 흐름 추적 (랩 5~11)**: 게임은 멀쩡히 도는데 **결과가 틀립니다**. 이벤트 구독, 실행 순서, FixedUpdate, fake null, 구조체 복사, 직렬화된 값, ScriptableObject 처럼 "Unity 가 대신 해 주는 일"의 순서와 수명을 따라갑니다.
- **3부: 사운드 (랩 12~15)**: (⚠️ 소리가 납니다. 볼륨을 낮추고 시작하세요.) 소리가 늦게 나고, 안 들리고, 끊기는 버그. 로드 상태·가상 음성·재생 위치를 재서 판정합니다.
- **4부: 이미지 / 렌더링 (랩 16~19)**: 엉뚱한 상자까지 빨개지고, 메모리가 새고, 아이콘이 바뀌고, 색이 물 빠지는 버그. 머티리얼 값, Material 객체 수, 텍스처 이름, 화면 픽셀로 판정합니다.
- **5부: 애니메이션 (랩 20~23)**: 공격 모션이 안 나오고, 화면 밖 발소리가 안 나고, 검격 판정이 빠지고, 손에 든 무기가 한 박자 늦는 버그.
- **6부: 잘못된 참조와 네이티브 메모리 (랩 24~27)**: 예외 없이 새는 `NativeArray`, "분명히 Complete 했는데" 터지는 잡, 다음 프레임에 사라지는 `Allocator.Temp`, Domain Reload 를 끈 Play 에서 지난 판의 오브젝트를 붙잡는 정적 필드.
- **7부: 멈춤 (랩 28~31)**: 크래시 없이 에디터가 굳는 무한루프와 데드락. 모두 중단, 스레드 창, 메인 스레드의 호출 스택으로 갇힌 곳을 찾습니다. 연습 중에 정말로 굳지 않도록 `HangGuard` 가 2초 뒤 "응답 없음" 을 알리고 빠져나옵니다.
- **8부: 컨테이너 (랩 32~38)**: 인스펙터에서 줄인 배열, 반복 도중 다른 스크립트가 고친 목록, 복제하면 사라지는 `Dictionary`, 순서를 약속하지 않는 `FindObjectsByType`, 넘긴 List 를 비우는 API, 파괴된 키, 크기가 바뀐 `NativeList` 의 낡은 `AsArray`.
- **9부: 물리 (랩 39~43)**: 얇은 벽을 뚫는 빠른 탄환, 불리지 않는 `OnTriggerEnter`, 레이어 번호와 마스크, transform 으로 움직인 Rigidbody, 옮긴 직후 옛 자리를 보는 레이캐스트. Physics Debugger, Gizmos, `Debug.DrawRay` 로 봅니다.
- **10부: 프레임레이트 의존 (랩 44~47)**: 30fps 와 120fps 에서 결과가 다른 버그. 빠진 `Time.deltaTime`, 매 프레임 `Lerp(a, b, 0.1f)`, Update 의 `fixedDeltaTime`, `timeScale = 0` 에서 멈추는 메뉴.
- **11부: 성능 (랩 48~52)**: 결과는 맞는데 느린 버그. 매 프레임 쌓이는 GC 쓰레기(문자열, LINQ, 박싱), Update 의 `Find`, 돌려주지 않는 오브젝트 풀, `SendMessage`, 배열을 돌려주는 API. Profiler 의 GC Alloc 열, Deep Profile, ProfilerMarker 로 찾고 `ProfilerRecorder` 로 바이트를 재서 판정합니다.
- **12부: 데이터 레이스와 스레드 (랩 53~55)**: `Task.Run` 두 개가 끼어들어 한정품이 두 번 팔리고, 워커 스레드에서 부른 `transform` 의 예외가 Task 안에 삼켜지고, `Awaitable.BackgroundThreadAsync()` 뒤 메인 스레드로 돌아오지 않는 버그. 매번 같은 결과가 나오게 재현합니다.

> 콘솔판·언리얼판과 같은 규칙입니다. **코드를 눈으로 훑어 정답을 찾지 마세요.** 1부는 **예외가 난 곳에서부터**, 2부부터는 **직접 멈춘 곳에서부터 값을 확인하며** 추적하세요.
> 콘솔판 1·3부를 먼저 끝냈다면 좋습니다. 호출 스택 창, 지역 창, 조건부 중단점, 추적점 사용법은 거기서 설명한 그대로 씁니다.

## BugHunt 시리즈

같은 방식(일부러 심은 버그를 디버거로 추적)으로 만든 네 가지 판입니다.

| 판 | 다루는 버그 | 저장소 |
|---|---|---|
| C++ 콘솔 | 호출 스택, 메모리 오류, 크래시 없는 흐름 추적 (랩 28개) | [BugHunt](https://github.com/gusgh0157/BugHunt) |
| C# 콘솔 | 예외와 스택 트레이스, 흐름 추적, 비동기/동시성, 리소스와 메모리 (랩 28개) | [BugHuntCS](https://github.com/gusgh0157/BugHuntCS) |
| 언리얼 엔진 | 크래시, 흐름 추적, 멀티플레이, 사운드, 이미지, 애니메이션 (랩 21개) | [BugHuntUE](https://github.com/gusgh0157/BugHuntUE) |
| 유니티 | 크래시/예외, 흐름 추적, 사운드, 이미지, 애니메이션, 잘못된 참조와 네이티브 메모리, 멈춤, 컨테이너, 물리, 프레임레이트 의존, 성능, 데이터 레이스 (랩 55개) | [BugHuntUnity](https://github.com/gusgh0157/BugHuntUnity) **(지금 보는 저장소)** |

## 버그 범주 커버리지

이 판이 다루는 범주와 랩입니다. **미포함** 은 Unity 에서 흔한 대표 사례인데 아직 랩이 없다는 뜻입니다.

| 범주 | 랩 | 다루는 사례 | 미포함 (대표 사례) |
|---|---|---|---|
| 참조·수명·fake null | 1, 2, 4, 8, 14, 37 | 파괴된 오브젝트 접근, `GetComponent` 범위, 꺼진 오브젝트의 코루틴, `?.`·`??` 와 fake null, 오브젝트와 함께 사라지는 소리, Dictionary 의 파괴된 키 | `DontDestroyOnLoad` 싱글턴 중복, 씬 전환 뒤 끊긴 참조 |
| 흐름·순서 | 3, 5, 6, 7, 23 | Awake/OnEnable/Start 순서, 이벤트 구독 해제, Update 끼리 순서, Update/FixedUpdate 횟수, 애니메이션 평가 뒤 LateUpdate | `LoadSceneAsync` 의 활성화 순서, `OnDisable`/`OnDestroy` 순서 |
| C# 값과 참조 | 9, 36 | 구조체 복사(`List<Vector3>[i]`), 돌려쓰는 버퍼를 두 곳이 공유 | 클로저가 반복 변수를 캡처 |
| 직렬화·에셋 | 10, 11, 16, 34 | 직렬화 값이 초기값을 덮어씀, 공유되는 ScriptableObject, `sharedMaterial`, 직렬화되지 않는 컨테이너 | `[SerializeReference]` 다형성, 프리팹 오버라이드, 필드 이름 변경(`FormerlySerializedAs`) |
| 컨테이너 | 32, 33, 34, 35, 36, 37, 38 | 인스펙터에서 줄인 배열, 반복 중 수정, 직렬화 손실, `Find`/`GetComponents` 순서, 결과 재사용 API, 파괴된 키, 크기가 바뀐 `NativeList` 의 `AsArray` | 키로 쓴 객체의 해시가 바뀜, `List.Sort` 의 불안정 정렬 |
| 사운드 | 12, 13, 14, 15 | 로드 지연, 가상 음성, 오브젝트 수명, `Play`/`PlayOneShot` | AudioMixer 그룹·스냅샷, 3D 감쇠 거리 |
| 이미지·렌더링 | 16, 17, 18, 19 | 공유 머티리얼, 머티리얼 복제 누수, 늦게 도착한 비동기 로드, Linear/Gamma | 빌드에서 빠진 셰이더 변형(분홍), 투명 정렬, `RenderTexture` 해제 |
| 애니메이션 | 20, 21, 22, 23 | 파라미터 오타, Culling Mode, 트랜지션과 이벤트, 평가 순서 | Root Motion 과 스크립트 이동 충돌, Write Defaults |
| 네이티브 메모리·잡 | 24, 25, 26, 38 | `Dispose` 누락, 잡 의존 사슬, `Allocator.Temp` 수명, 낡은 `AsArray` | Burst 에서만 다른 결과 |
| 정적 상태·Domain Reload | 27 | Domain Reload 를 끈 Play 의 정적 필드 | 정적 이벤트 구독 누적 |
| 멈춤 | 28, 29, 30, 31 | yield 없는 while, 방문 체크 없는 BFS, `.Result` 데드락, float 누적 종료 조건 | `lock` 순서가 엇갈린 데드락 |
| 물리 | 39, 40, 41, 42, 43 | 빠른 탄환 터널링, Rigidbody 없는 트리거, 레이어 번호와 마스크, transform 으로 움직인 Rigidbody 와 보간, `SyncTransforms` | 관절(Joint) 폭주, 비균일 스케일 콜라이더, `OnCollisionStay` 비용 |
| 프레임레이트 의존 | 7, 44, 45, 46, 47 | 한 프레임짜리 입력을 FixedUpdate 가 놓침, `Time.deltaTime` 누락, 매 프레임 `Lerp(a, b, 0.1f)`, `fixedDeltaTime` 혼동, `timeScale = 0` 에서 멈추는 UI | Update 에서 `AddForce`(물리 스텝과 어긋남), 가변 deltaTime 의 큰 스파이크(첫 프레임, 로딩 직후) |
| 성능·GC | 17, 48, 49, 50, 51, 52 | 머티리얼 복제 누수, 매 프레임 GC 할당(문자열·LINQ·박싱), Update 의 `Find`/`FindObjectsByType`, 돌려주지 않는 풀, `SendMessage`, 배열을 돌려주는 API | 렌더링 비용(배칭 깨짐, 과도한 그림자), 큰 에셋 동기 로드로 멈춤, 매 프레임 Canvas 재빌드 |
| 데이터 레이스·스레드 | 30, 53, 54, 55 | 메인 스레드에서 Task 를 막고 기다림, check-then-act 레이스, 워커 스레드의 Unity API 와 삼켜진 예외, `await` 뒤 실행 스레드 | 잡 안의 static 공유 상태, `volatile` 없는 종료 플래그, 여러 스레드가 함께 쓰는 캐시 `Dictionary` |
| UI | 미포함 | | 레이캐스트를 가로막는 투명 이미지, Canvas 재빌드, 해상도별 앵커 |
| 입력 | 미포함 | | Input System 액션 맵 비활성, 포커스를 잃었을 때 눌린 채 남는 키 |
| 빌드 전용 | 미포함 | | IL2CPP 코드 스트리핑, 플랫폼별 경로·대소문자 |
| 멀티플레이 | 미포함 | | [부록 A](#부록-a-멀티플레이netcode-for-gameobjects는) 참고 |

## 준비

필요한 것: **Unity 6000.3.27f2** (Unity 6.3 LTS), **Visual Studio 2022 이상**(워크로드 "Unity를 사용한 게임 개발") 또는 **Rider**.

1. Unity Hub → **Add** → 이 폴더(`BugHuntUnity`)를 고릅니다. 처음 열 때 Library 를 만드느라 몇 분 걸립니다.
2. **Edit → Preferences → External Tools → External Script Editor** 를 Visual Studio 또는 Rider 로 정합니다.
3. **Assets → Open C# Project** 로 IDE 를 엽니다(솔루션 파일은 Unity 가 만들어 줍니다).
4. 에디터 오른쪽 아래 상태 표시줄의 **벌레 아이콘(Code Optimization)** 을 눌러 **Debug Mode** 로 바꿉니다. Release Mode 에서는 중단점이 안 걸리거나 변수가 "최적화되어 사용할 수 없음"으로 보입니다. (디버거를 붙이면 Unity 가 바꿀지 물어보기도 합니다.)

> 렌더 파이프라인은 기본(Built-in)이고, 색 공간은 **Linear** 입니다(랩 19). 패키지는 Test Framework 와 IDE 연동만 씁니다.

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
   > ⚠️ **사운드 랩(3부, 랩 12~15)은 소리를 냅니다. 실행 전에 시스템 볼륨을 낮추고, 헤드폰을 쓴다면 귀에서 떼고 시작하세요.** 버튼에 "소리 주의" 가 붙어 있습니다.
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

### Console 창 읽기

- 한 줄을 **클릭**하면 아래에 스택 트레이스가 나옵니다. **맨 위가 예외가 난 곳**, 아래로 갈수록 "누가 불렀나" 입니다.
- 파란 링크(`Assets/BugHunt/Labs/Lab01_Tower.cs:50`)를 누르면 IDE 에서 그 줄이 열립니다. 줄을 **더블클릭**해도 됩니다.
- `UnityEngine.` 으로 시작하는 줄은 엔진 코드, `BugHunt.Labs.` 로 시작하는 줄이 내 코드입니다. 엔진 줄은 건너뛰되 **어떤 엔진 기능을 거쳐 왔는지**(AddComponent, SetupCoroutine, 애니메이션 이벤트...)는 읽어 두세요. 그게 "왜 이 시점에 불렸나" 의 답입니다.
- 오른쪽 위 버튼: **Collapse**(같은 줄 묶기), **Error Pause**(에러가 나는 순간 플레이 일시정지), 그리고 로그/경고/에러 필터. 경고(⚠)를 꺼 두지 마세요(랩 20).

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

랩 코드는 `Assets/BugHunt/Labs/LabNN_*.cs` 입니다(랩 10·11 은 `Lab10/`, `Lab11/` 폴더에 클래스가 하나 더 있습니다). `Assets/BugHunt/Core/`(랩 실행기, 7부의 `HangGuard`, 11부의 `GcMeter`)와 `Editor/`, `Tests/` 에는 버그가 없습니다.
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
기록표 (2~5부용)
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
| **추적점** | 중단점 우클릭 → 작업(Actions), `{Time.frameCount} {damage}` 같은 식, "실행 계속" 체크 | 코드를 고치지 않고 호출 순서·횟수 기록 (랩 5, 6, 7) |
| **조건부 중단점 / 적중 횟수** | 중단점 우클릭 → 조건, 예: `Time.frameCount > 100 && Health <= 0` | 매 프레임 불리는 함수의 특정 순간만 |
| **`Debug.Break()`** | 코드에 한 줄. 그 프레임이 끝나면 에디터가 **일시정지** | 디버거 없이 "그 순간" 의 Hierarchy/Inspector 를 보고 싶을 때. 일시정지 중 **다음 프레임(Ctrl+Alt+P)** 으로 한 프레임씩 |
| **Inspector Debug 모드** | Inspector 탭 ⋮ → **Debug** | private 필드와 인스턴스 ID 까지 보기 |
| **Script Execution Order** | Edit → Project Settings → Script Execution Order | 스크립트끼리 Update 순서 (랩 6) |
| **Profiler** | Window → Analysis → Profiler (Ctrl+7), CPU 모듈 → Hierarchy / **Timeline** | 한 프레임 안의 순서, 함수가 몇 번 불렸나(Calls) (랩 7, 23) |
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

---

# 3부: 사운드

> **⚠️ 이 부의 랩은 소리를 냅니다. 실행 전에 시스템 볼륨을 낮추고, 헤드폰을 쓴다면 귀에서 떼고 시작하세요.** 랩이 도는 동안 전체 볼륨(`AudioListener.volume`)을 0.1 로 낮추고, 클립도 진폭 0.3 이하의 단일음이지만 랩 13 은 소리 수십 개가 한꺼번에 납니다. 랩 실행 창의 이 랩 버튼에는 "소리 주의" 가, Console 에는 `[주의] 소리가 납니다. 볼륨을 낮추세요` 가 먼저 찍힙니다.

"소리가 늦게 나요", "안 들려요", "끊겨요"는 귀로는 바로 들리지만, 버그를 **재현하고 판정**하려면 숫자가 필요합니다. 이 랩들은 클립의 로드 상태(`AudioClip.loadState`), 가상 음성 여부(`AudioSource.isVirtual`), 재생 중인지와 재생 위치(`isPlaying`, `time`)를 재서 `[PASS]`/`[FAIL]` 을 냅니다.

## 3부에서 쓰는 도구

| 도구 | 하는 법 | 언제 |
|---|---|---|
| 오디오 임포트 설정 | Project 창에서 .wav 선택 → Inspector: Load Type, Preload Audio Data, Load In Background, Compression | 첫 재생 지연, 메모리 (랩 12) |
| Profiler → **Audio** 모듈 | 재생 중인 소리 수, 가상 음성 수, 오디오 메모리, 채널 목록 | 동시 재생 한도 (랩 13) |
| Project Settings → **Audio** | Max Real Voices / Max Virtual Voices, DSP Buffer Size | 한도 자체 (랩 13) |
| Game 창 **Mute Audio** 토글 | Game 창 위 도구 모음 | 소리를 끈 채로 판정만 보기 |
| `Debug.Break()` + Hierarchy | 소리가 끊기는 프레임에서 일시정지 → AudioSource 가 붙은 오브젝트가 아직 있나 | 끊김 (랩 14) |

## 3부 랩 목록

| # | 이름 | 증상 | 새로 배우는 것 | 난이도 |
|---|---|---|---|---|
| 12 | 블래스터 발사음 | 첫 발만 발사음이 섬광보다 늦다 | **Preload Audio Data / Load In Background**, `loadState`, `LoadAudioData()` | ★★ |
| 13 | 보스 대사 | 병사가 많으면 보스 대사가 안 들린다 | **Max Real Voices**, 가상 음성, `AudioSource.priority`, Profiler Audio 모듈 | ★★ |
| 14 | 폭발 통 | 폭발음이 터지자마자 끊긴다 | 소리의 수명 = **AudioSource 가 붙은 오브젝트의 수명**, `PlayClipAtPoint` | ★ |
| 15 | 작전 브리핑 | 맞는 소리 때문에 브리핑이 끊긴다 | **`Play()` 와 `PlayOneShot()`** 의 차이, 채널(AudioSource) 나누기 | ★ |

> **헤드리스 실행:** 랩 13·14·15 는 소리가 실제로 재생되어야 판정됩니다. 오디오 출력 장치가 있는 PC 에서는 `-batchmode -nographics` 로도 판정됐습니다(아래 "자동 검증"). 출력 장치가 없는 서버/CI 에서는 대조군 소리가 재생되지 않으므로 `[SKIP]` 이 나옵니다. 랩 12 는 로드 상태만 보므로 장치와 상관없습니다.

---

# 4부: 이미지 / 렌더링

"색이 안 바뀌어요", "아이콘이 다른 아이템 거예요", "색이 기획서랑 달라요"는 눈으로 바로 보이지만, 눈대중으로는 "조금 다른 것 같다"에서 멈추기 쉽습니다. 이 랩들은 **렌더러에 끼워진 머티리얼의 값**, **Material 객체 수**, **칸에 들어간 텍스처 이름**, **실제로 그려진 픽셀**을 조회해서 판정합니다.

## 4부에서 쓰는 도구

| 도구 | 하는 법 | 언제 |
|---|---|---|
| Inspector 의 Materials | 플레이 중 Hierarchy 에서 오브젝트 선택. 이름 끝 `(Instance)` 면 복제본 | 공유 머티리얼인지 복제본인지 (랩 16, 17) |
| **Frame Debugger** | Window → Analysis → Frame Debugger → Enable. 드로 콜을 고르면 셰이더 속성 값이 보인다 | 실제로 넘어간 `_Color`, 배칭이 깨진 이유 (랩 17, 19) |
| Profiler → **Memory** 모듈 | Objects 수, Texture/Material 수 | 늘어나기만 하는 객체 (랩 17) |
| **Memory Profiler** | Package Manager → `com.unity.memoryprofiler` 설치 → Window → Analysis → Memory Profiler → 스냅샷 2개 → Compare | 누가 몇 개 남았나 (랩 17) |
| Player Settings → **Color Space** | Edit → Project Settings → Player → Other Settings | Gamma / Linear (랩 19) |
| 텍스처 임포트 설정의 **sRGB (Color Texture)** | 텍스처 선택 → Inspector | 색 텍스처는 켜고, 노멀/마스크는 끈다 |
| RenderDoc | Game 창 ⋮ → Load RenderDoc → 카메라 아이콘 | GPU 가 실제로 그린 것을 한 프레임 통째로 |

## 4부 랩 목록

| # | 이름 | 증상 | 새로 배우는 것 | 난이도 |
|---|---|---|---|---|
| 16 | 상자 강조 | 한 상자에 마우스를 올렸는데 전부 빨개진다 | **`sharedMaterial` 은 에셋 자체**, `material` 은 복제본, 에셋이 디스크에 저장되는 문제 | ★ |
| 17 | 보급 상자 색 | 웨이브마다 메모리가 늘어난다 | **`renderer.material` 복제본 누수**, **MaterialPropertyBlock**, Memory Profiler, Frame Debugger | ★★ |
| 18 | 인벤토리 아이콘 | 스크롤하면 이름은 물약인데 아이콘은 검 | 비동기 로드(`Resources.LoadAsync`) 콜백의 **늦은 도착**, 재사용되는 칸, 캐시 적중은 즉시 | ★★ |
| 19 | 팀 색 | 팀 색이 물 빠지고 밝다(0x00, 0xFF 는 맞음) | **Linear / Gamma 색 공간**, `SetColor` 와 `SetVector` 의 차이, Frame Debugger 로 값 보기 | ★★ |

> **헤드리스 실행:** 랩 19 는 카메라로 그린 픽셀을 다시 읽어서 판정하므로 그래픽 장치가 필요합니다. `-nographics` 에서는 `[SKIP]` 이 나옵니다. 랩 16~18 은 그래픽 장치 없이도 판정됩니다.

---

# 5부: 애니메이션

애니메이션 버그는 "가끔 안 맞는 것 같다"로 신고됩니다. 이 랩들은 **Animator 상태**, **애니메이션 이벤트가 불린 횟수**, **프레임마다 두 Transform 의 거리**를 재서 판정합니다. 버그 중 일부는 C# 이 아니라 **애니메이션 에셋**(컨트롤러, 클립)에 있습니다. 그래서 Animator 창과 Animation 창을 디버거처럼 쓰는 법을 배웁니다.

## 5부에서 쓰는 도구

| 도구 | 하는 법 | 언제 |
|---|---|---|
| **Animator 창 라이브 보기** | Window → Animation → Animator. 플레이 중 Hierarchy 에서 Animator 가 붙은 오브젝트를 고르면 현재 상태, 진행 막대, 트랜지션, 파라미터 값이 실시간으로 보인다 | 모든 랩. 랩이 끝나면 오브젝트가 지워지니 중단점이나 `Debug.Break()` 로 멈춘 채 본다 |
| Animation 창 | Window → Animation → Animation (Ctrl+6). 클립을 고르면 커브와 이벤트(타임라인 위 작은 막대) | 이벤트가 몇 초에 있나 (랩 21, 22) |
| 트랜지션 Inspector | Animator 창에서 화살표 클릭 → Has Exit Time, Exit Time, Transition Duration, Interruption Source | 상태가 언제 넘어가나 (랩 22) |
| Animator 컴포넌트의 **Culling Mode** | Inspector | 화면 밖에서 멈추나 (랩 21) |
| 일시정지 + **한 프레임씩** | Play 옆 ⏸ → ⏭ (Ctrl+Alt+P) | 트랜지션을 프레임 단위로 보기 |
| Profiler Timeline | `PreLateUpdate.DirectorUpdateAnimationBegin/End` 위치 | 애니메이션이 Update 와 LateUpdate 사이에 평가된다 (랩 23) |

## 5부 랩 목록

| # | 이름 | 증상 | 새로 배우는 것 | 난이도 |
|---|---|---|---|---|
| 20 | 공격 버튼 | 버튼을 눌러도 공격 모션이 안 나온다(예외 없음) | **Animator 파라미터 이름 오타**는 경고만, `StringToHash`, Animator 창 Parameters | ★ |
| 21 | 등 뒤의 발소리 | 화면 밖 경비병의 발소리가 안 난다 | **Culling Mode**, 화면 밖에서 애니메이션 이벤트가 사라짐, Scene 뷰도 "보는 것" | ★★ |
| 22 | 검격 판정 | 휘두르는데 피해가 안 들어간다 | **트랜지션이 이벤트보다 먼저 끝남**, Exit Time / Duration 읽기, Animation 창의 이벤트 | ★★ |
| 23 | 손에 든 무기 | 횃불이 손을 한 박자 늦게 따라온다 | **애니메이션은 Update 뒤에 평가**, LateUpdate, Profiler Timeline | ★★ |

> 랩 22 를 고치려면 `Assets/BugHunt/Resources/BugHunt/Animation/Swordsman.controller` 를 Animator 창에서 수정합니다. C# 을 고치는 랩이 아닙니다.

---

# 6부: 잘못된 참조와 네이티브 메모리

C# 은 GC 가 메모리를 치워 주지만, Unity 에는 **GC 가 모르는 메모리**가 있습니다. `NativeArray` 같은 네이티브 컨테이너는 Unity 의 네이티브 힙에 있고 C# 변수는 그 주소를 든 작은 struct 일 뿐이라서, 누가 언제 해제하는지(`Allocator`, `Dispose`)와 누가 지금 쓰고 있는지(잡)를 직접 관리해야 합니다. 대신 에디터에는 **안전 시스템(AtomicSafetyHandle)** 이 있어서 해제된 버퍼나 잡이 쓰는 중인 버퍼에 손대면 바로 예외를 던져 줍니다. 그리고 정적(static) 필드는 Domain Reload 를 끄면 **지난 Play 의 오브젝트**를 붙잡습니다.

1부의 MissingReferenceException, 2부의 fake null·구조체 복사, 4부의 sharedMaterial 과는 다른 종류입니다. 여기서는 "수명을 누가 정하나" 를 봅니다.

## 6부에서 쓰는 도구

| 도구 | 하는 법 | 언제 |
|---|---|---|
| **Leak Detection Level** | Edit → Preferences → **Jobs** → Leak Detection Level → *Enabled With Stack Trace* (코드로는 `NativeLeakDetection.Mode`) | 플레이를 멈추거나 다시 컴파일할 때 뜨는 `Leak Detected` 에 **할당한 곳의 스택**이 붙는다 (랩 24) |
| **안전 시스템 예외 읽기** | 예외 메시지에 **잡 이름과 버퍼(필드) 이름**이 나온다: `The previously scheduled job X writes to ... X.Count` | 잡과 메인 스레드가 같은 버퍼를 건드릴 때 (랩 25), 해제된 버퍼 (랩 26) |
| Profiler → CPU → **Timeline** | Worker 스레드 줄에서 잡이 언제 도는지, 메인 스레드가 `JobHandle.Complete` 에서 얼마나 기다리는지 | 잡 의존 관계 (랩 25) |
| **Memory Profiler** | 스냅샷 두 장 → Compare → Native / Unity Subsystems | 늘어나기만 하는 네이티브 메모리 (랩 24) |
| 조사식 `NativeArray` 펼치기 | `IsCreated`, `Length`. 해제된 버퍼를 펼치면 안전 시스템 예외 메시지가 값 대신 보인다 | 이 변수가 아직 살아 있나 |
| **Enter Play Mode Settings** | Edit → Project Settings → Editor → When entering Play Mode (*Reload Domain and Scene* / *Reload Scene only* / *Do not reload Domain or Scene*) | 정적 필드가 Play 사이에 살아남는가 (랩 27) |

## 6부 랩 목록

| # | 이름 | 증상 | 새로 배우는 것 | 난이도 |
|---|---|---|---|---|
| 24 | 경로 캐시 | 예외는 없는데 오래 하면 메모리가 늘고, 멈추면 `Leak Detected` | **NativeArray 는 GC 가 치우지 않는다**, `Dispose` / `OnDestroy`, Leak Detection Level, 구조체 복사본과 안전 핸들 | ★ |
| 25 | 망루의 시야 | 잡을 붙이자마자 InvalidOperationException ("분명히 Complete 했는데?") | **JobHandle 의존 사슬**, 끝 핸들을 Complete, 안전 시스템 메시지 읽기, Profiler Timeline 의 Worker | ★★ |
| 26 | 출현 계획표 | 첫 줄만 나오고 다음 프레임부터 "has been deallocated" | **Allocator 수명**(Temp / TempJob / Persistent), 프레임 끝에 되감기는 Temp | ★★ |
| 27 | 두 번째 판 | Play 를 두 번째 누르면 웨이브가 이어지고 레이더가 지난 판의 적을 가리킨다 | **Domain Reload 를 끈 Enter Play Mode**, 정적 상태 되돌리기 `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` | ★★ |

> 랩 24·26 은 에디터의 컬렉션 안전 검사(`ENABLE_UNITY_COLLECTIONS_CHECKS`)로 판정합니다. 에디터와 개발 빌드에서는 켜져 있습니다. 릴리스 빌드에서는 같은 버그가 예외 없이 **엉뚱한 값이나 크래시**로 나타납니다(그래서 에디터에서 잡아야 합니다).
>
> 랩 27 은 Play 를 다시 누른 것을 랩 안에서 흉내 냅니다(지난 판 오브젝트를 지우고 `[RuntimeInitializeOnLoadMethod]` 들을 부름). 이 프로젝트의 실제 설정(랩 출력 첫 줄)은 *Reload Domain and Scene* 이라서, 진짜 Play/Stop 을 반복해도 재현되지 않습니다. 직접 겪어 보려면 설정을 *Reload Scene only* 로 바꾸고 Play → Stop → Play 를 해 보세요(실험이 끝나면 되돌리기).

---

# 7부: 멈춤 (무한루프와 데드락)

예외도 로그도 없이 **에디터 전체가 굳는** 버그입니다. Unity 의 게임 코드는 거의 다 메인 스레드 하나에서 돌기 때문에, 메인 스레드가 한 프레임 안에서 빠져나오지 못하면 Update 도, 렌더링도, 에디터 UI 도 멈춥니다. 작업 관리자에는 "응답 없음", Console 은 조용합니다. 이런 버그는 **멈춘 그 순간의 메인 스레드 호출 스택**을 봐야 풀립니다.

## 진짜로 굳었을 때 하는 일

1. **CPU 를 본다.** 작업 관리자에서 Unity 가 CPU 한 코어(예: 8코어면 약 12%)를 꽉 쓰면 **무한루프**, 0% 근처면 **데드락**(서로 기다리기만 함)입니다. 메모리가 계속 늘면 무한루프 안에서 무언가를 쌓고 있습니다.
2. **디버거를 붙이고 모두 중단.** 굳은 에디터에도 디버거는 붙습니다. VS: **디버그 → 모두 중단 (Ctrl+Alt+Break)**, Rider: **Pause Program**(⏸).
3. **스레드 창에서 메인 스레드를 고른다.** VS: 디버그 → 창 → **스레드 (Ctrl+Alt+H)**, Rider: Debug 창의 Threads 드롭다운. `Main Thread`(또는 스레드 ID 1)를 더블클릭하면 호출 스택 창이 그 스레드로 바뀝니다. 맨 위의 내 코드 줄이 갇혀 있는 곳입니다.
4. **F10 을 몇 번 눌러 본다.** 같은 반복문 안을 맴돌면 무한루프. 지역 창에서 종료 조건에 쓰이는 값이 **바뀌고 있는지** 보세요(안 바뀌면 영원히 안 끝납니다).
5. 데드락이면 **병렬 스택**(VS: 디버그 → 창 → 병렬 스택)과 **작업(Tasks)** 창에서 누가 무엇을 기다리는지 봅니다.

## 이 프로젝트에서는 실제로 굳지 않습니다 (HangGuard)

연습 때마다 작업 관리자로 Unity 를 죽일 수는 없으니, 7부 랩의 반복문에는 `HangGuard.Tick(...)` 한 줄이, 막고 기다리는 곳에는 `HangGuard.Wait(task, ...)` 가 들어 있습니다(`Assets/BugHunt/Core/HangGuard.cs`).

- `Tick` 은 **아무것도 바꾸지 않고 아무것도 돌려주지 않습니다.** 반복문의 조건과 몸통은 버그 그대로입니다. 버그를 고쳐 반복문이 정상적으로 끝나면 `Tick` 은 있으나 마나입니다(가리는 것이 없습니다).
- 한 프레임 안에서 **2초**(시간 예산)를 넘기거나, 정해 둔 **반복 한도**(랩 29 처럼 돌 때마다 메모리를 먹는 반복문)를 넘으면 `[FAIL] 응답 없음: ...` 을 찍고 `HangException` 을 던져 빠져나옵니다. 랩 결과는 **TIMEOUT** 입니다.
- 그 예외의 **스택 트레이스 = 메인 스레드가 갇혀 있던 곳**입니다. "모두 중단" 으로 보는 호출 스택과 같습니다.
- `Wait` 는 `task.Result` 와 같습니다. 예산 안에 Task 가 끝나지 않으면 데드락으로 보고 빠져나옵니다.
- `Tick` 이 있는 반복문 안에서 중단점에 멈춰 있던 시간은 예산에 넣지 않습니다(한 줄씩 넘기며 살펴봐도 "응답 없음" 이 되지 않습니다).
- **직접 "모두 중단" 을 눌러 보려면** 메뉴 **BugHunt → 멈춤 랩: 오래 버티기(30초)** 를 켜세요. 30초 동안 정말로 굳어 있으니 그사이에 디버거를 붙이고 위 1~5단계를 해 볼 수 있습니다. (랩 29 는 메모리 때문에 반복 한도로 먼저 끊깁니다.) 채점기는 이 설정과 상관없이 2초를 씁니다.

> ⚠ **`HangGuard.Tick` / `HangGuard.Wait` 줄을 지우지 마세요.** 지운 채로 버그가 남아 있으면 에디터가 정말로 굳습니다. 그랬다면 작업 관리자에서 Unity 를 끝내고 다시 여세요(저장하지 않은 씬은 날아갑니다). 고칠 때 `Wait` 는 지워도 됩니다. 막고 기다리지 않게 바꾸는 것이 랩 30 의 정답이기 때문입니다.

## 7부 랩 목록

| # | 이름 | 증상 | 새로 배우는 것 | 난이도 |
|---|---|---|---|---|
| 28 | 엘리베이터 | 엘리베이터를 부르는 순간 게임 전체가 멈춘다(CPU 한 코어 100%) | **Update 가 바꿔 줄 값을 while 로 기다리기**, `yield return` 없는 코루틴 반복문, 코루틴은 스레드가 아니다, `WaitUntil`, 모두 중단 + 스레드 창 | ★ |
| 29 | 던전 길 찾기 | 문을 잠가 길이 끊기면 멈추고 메모리가 치솟는다 | **그래프 탐색의 방문 체크**, 고리와 양방향 간선, "멈춤 + 메모리 증가" 의 지문, 조건부 중단점 `queue.Count > 100` | ★★ |
| 30 | 상점의 금화 | 상점을 여는 순간 영원히 멈춘다(CPU 0%) | **메인 스레드에서 `.Result` 로 기다리면 UnitySynchronizationContext 때문에 데드락**, `await` 가 돌아오는 곳, 병렬 스택 / 작업 창, `ConfigureAwait(false)` 는 반쪽짜리 해결 | ★★ |
| 31 | 리플레이 빨리감기 | 서버를 일주일 넘게 켜 두면 빨리감기에서 멈춘다 | **float 누적 종료 조건**, 큰 수에서 작은 수를 더하면 그대로, 정수 틱 / double, `Time.timeAsDouble` | ★★ |

---

# 8부: 컨테이너

배열, `List`, `Dictionary`, `NativeList` 는 C# 에서도 흔한 버그 원인이지만, Unity 에서는 **엔진이 컨테이너를 만지는 곳**이 더 있습니다. 인스펙터는 배열 크기를 언제든 바꾸고, 직렬화는 모르는 컨테이너를 조용히 버리고, `Destroy` 는 컨테이너에 든 오브젝트를 fake null 로 바꾸고, `Find`/`GetComponents` 는 순서를 약속하지 않고, 할당을 줄이는 API 는 넘긴 컨테이너를 비우고 다시 채웁니다.

## 8부에서 쓰는 도구

| 도구 | 하는 법 | 언제 |
|---|---|---|
| **예외 설정: throw 될 때 중단** | VS: 디버그 → 창 → 예외 설정 → Common Language Runtime Exceptions 에서 `System.IndexOutOfRangeException`, `System.InvalidOperationException` 체크. Rider: Run → View Breakpoints → Exception Breakpoints | 예외가 잡히는 곳이 아니라 **던지는 곳**에서 멈추기 (랩 32, 33) |
| 조사식에 **인덱스와 크기를 나란히** | `i`, `Slots.Length`, `Labels.Count`, `Points.Capacity` | 범위 밖 인덱스 (랩 32), 크기가 바뀐 순간 (랩 38) |
| 조사식 **참조 비교** | `alphaRoster == bravoRoster`, `(object)key == null` 과 `key == null` 을 나란히 | 같은 컨테이너를 두 변수가 가리키나 (랩 36), fake null 키 (랩 37) |
| **Inspector 의 Debug 모드** | Inspector 오른쪽 위 ⋮ → Debug. private 직렬화 필드까지 보인다 | 이 필드가 직렬화되는가. 인스펙터에 안 보이면 Instantiate·저장에서도 빠진다 (랩 34) |
| `JsonUtility.ToJson(component)` 출력 | Console 에 찍어 보기 | 직렬화되는 필드만 나온다. 빠진 필드 = 복제·저장에서 사라지는 필드 (랩 34) |
| 컨테이너에 **중단점 + 호출 스택** | `List.Remove` 를 부르는 줄에 중단점 → 호출 스택에 바깥 `foreach` 가 보이나 | 반복 도중에 누가 목록을 고쳤나 (랩 33) |

## 8부 랩 목록

| # | 이름 | 증상 | 새로 배우는 것 | 난이도 |
|---|---|---|---|---|
| 32 | 단축키 칸 | 디자이너가 칸 배열을 3칸으로 줄이자 IndexOutOfRangeException, 빈 단축키에서 ArgumentOutOfRangeException | **상수 길이와 데이터 길이**, 인스펙터는 배열 크기를 바꾼다, 바깥 번호의 범위 확인, `OnValidate` | ★ |
| 33 | 공습 | 공습에 적이 쓰러지면 InvalidOperationException, 뒤쪽 적은 멀쩡 | **반복 도중 다른 스크립트가 목록을 고침**, 복사본·역순 반복, `RemoveAll` | ★ |
| 34 | 상점 가격표 | 복제하거나 저장했다 불러온 상점의 가격표와 진열대가 텅 빈다(경고 없음) | **Unity 직렬화가 버리는 컨테이너**(`Dictionary`, `List<List<T>>`, `T[,]`), `ISerializationCallbackReceiver`, `[Serializable]` 래퍼 | ★★ |
| 35 | 순찰 순서 | 경비병이 Order 번호와 상관없는 순서로 돈다. 맵마다, 실행 환경마다 맞기도 하고 틀리기도 한다 | **`FindObjectsByType` / `GetComponents` 의 순서는 약속이 아니다**, 직접 정렬, 인스펙터 배열 | ★ |
| 36 | 부대 편성 | 두 부대의 명단이 똑같다 | **결과를 채워 주는 API 가 넘긴 List 를 비운다**(`GetComponentsInChildren(List)`), 돌려쓰는 버퍼의 주인 | ★★ |
| 37 | 위협 목록 | 위협 1위가 죽자 보스가 MissingReferenceException, 목록 수도 그대로 | **파괴된 오브젝트가 Dictionary 키로 남음**(fake null 키), 죽은 키 치우기, `OnDestroy` 알림 | ★★ |
| 38 | 발자국 | 걸음이 쌓이면 출발점을 읽는 줄에서 "has been deallocated" | **`NativeList.AsArray()` 는 복사가 아니라 모습**, 크기가 바뀌면 옛 메모리, 캐시하지 않기 | ★★ |

> 랩 38 의 예외는 에디터의 컬렉션 안전 검사 덕분입니다(6부와 같음). 릴리스 빌드에서는 해제된 메모리를 읽어 엉뚱한 값이나 크래시가 납니다. `NativeArray` 의 `Dispose` 누락은 6부 랩 24 에서 다룹니다.

---

# 9부: 물리

물리 엔진(PhysX)은 Unity 의 transform 과 **따로 자기 장면을 갖고**, 프레임이 아니라 **고정 간격(fixedDeltaTime, 기본 0.02초)마다 순간이동하듯** 계산합니다. 그래서 물리 버그는 대개 예외 없이 "안 맞는다", "안 불린다", "엉뚱한 것을 맞힌다", "덜덜 떨린다" 로 나타납니다. 중단점보다 **눈으로 보는 도구**(Physics Debugger, Gizmos, `Debug.DrawRay`)가 먼저입니다.

## 9부에서 쓰는 도구

| 도구 | 하는 법 | 언제 |
|---|---|---|
| **Physics Debugger** | Window → Analysis → **Physics Debugger**. Scene 뷰에 콜라이더를 종류별 색(Static / Kinematic / Dynamic / Trigger)으로 그린다. *Queries* 탭은 레이캐스트·오버랩을, *Contacts* 탭은 접촉점을 보여 준다 | 이 콜라이더가 물리 엔진에 어떤 종류로 들어가 있나 (랩 40), 레이캐스트가 어디에 맞았나 (랩 41), 콜라이더가 실제로 어디 있나 (랩 43) |
| **`Debug.DrawRay` / `Debug.DrawLine`** | `Debug.DrawRay(origin, dir * length, Color.red, 5f)`. 마지막 인자는 남겨 둘 초. Scene 뷰(그리고 Game 뷰의 Gizmos 버튼)에서 보인다 | 레이의 방향과 길이 (랩 41), 물리 스텝마다 탄환이 간 거리 (랩 39) |
| **Gizmos** | 컴포넌트에 `void OnDrawGizmos() { Gizmos.DrawWireSphere(transform.position, radius); }` | 감지 범위, 트리거 크기를 늘 띄워 두기 |
| 일시정지 + **한 프레임씩** | Play 중 일시정지(Ctrl+Shift+P) → 다음 프레임(Ctrl+Alt+P) | 물리 스텝 사이에 무엇이 바뀌는지 (랩 39, 42, 43) |
| **Layer Collision Matrix** | Project Settings → Physics → Layer Collision Matrix | 두 레이어가 서로 충돌·트리거하도록 되어 있나 (랩 40) |
| 조사식에 **2진수** | `Convert.ToString(mask, 2)` | 레이어 마스크에 어느 비트가 켜져 있나 (랩 41) |
| 조사식 `collider.bounds.center` | transform.position 과 나란히 | transform 과 물리 장면의 위치가 같은가 (랩 43) |

## 9부 랩 목록

| # | 이름 | 증상 | 새로 배우는 것 | 난이도 |
|---|---|---|---|---|
| 39 | 레일건 | 빠른 탄환이 얇은 벽을 그냥 뚫는다(충돌 0번) | **터널링**, 물리 스텝마다 순간이동, `CollisionDetectionMode.Continuous / ContinuousDynamic / ContinuousSpeculative` | ★ |
| 40 | 가시 함정 | 함정 위를 지나가도 `OnTriggerEnter` 가 불리지 않는다 | **충돌 메시지는 한쪽에 Rigidbody 가 있어야**, 정적 콜라이더끼리, Kinematic Rigidbody, 충돌 액션 매트릭스 | ★ |
| 41 | 포탑의 조준 | Enemy 레이어만 쏘라고 했는데 유리창에 맞는다 | **레이어 번호 ≠ 레이어 마스크**, `1 << layer`, `LayerMask` 필드, `LayerMask.GetMask` | ★ |
| 42 | 움직이는 발판 | Interpolate 를 켜자 발판이 거의 안 움직이고, 위의 상자도 안 실려 간다 | **Rigidbody 를 transform 으로 움직이지 않기**, 보간이 transform 을 덮어씀, Kinematic 의 `MovePosition` 은 속도를 전달 | ★★ |
| 43 | 퍼즐 초기화 | 초기화한 그 프레임에 상자를 집으면 허공을, 옛 자리를 집으면 상자를 집는다 | **transform 과 물리 장면의 동기화 시점**, `Physics.SyncTransforms`, `rb.position` | ★★ |

> 9부의 판정은 충돌 횟수, 트리거 횟수, 레이캐스트가 맞은 이름, 최종 위치로 합니다. 물리는 `-batchmode -nographics` 에서도 똑같이 돌기 때문에 SKIP 은 없습니다. 랩 40·42 는 `Time.captureDeltaTime` 으로 60fps 를 고정합니다.

---

# 10부: 프레임레이트 의존

개발 PC 에서는 완벽한데 **다른 기기에서만** 빠르거나 느린 버그입니다. Update 는 1초에 몇 번 불릴지 모르고(30fps 노트북, 144Hz 모니터, 발열로 떨어진 휴대폰), 그 횟수에 기대는 코드는 기기마다 결과가 다릅니다. 2부 랩 7(한 프레임짜리 입력을 FixedUpdate 가 놓침)은 Update 와 FixedUpdate 의 **횟수 차이**를, 10부는 **한 번에 움직이는 양**을 다룹니다.

이 부의 랩은 같은 코드를 **30fps 와 120fps 에서 한 번씩** 돌려(`Time.captureDeltaTime` 으로 프레임 간격을 고정) 두 결과가 사양과 같은지 봅니다. 채점기의 숨은 케이스는 24fps 와 144fps 입니다. 랩 47 은 `timeScale = 0` 을 다루므로 진짜 시간으로 기다립니다.

## 10부에서 쓰는 도구

| 도구 | 하는 법 | 언제 |
|---|---|---|
| **fps 바꿔 재현** | `Application.targetFrameRate = 30;` (vSync 를 끄고: Quality → VSync Count = Don't Sync), 또는 `Time.captureDeltaTime = 1f / 30f;` 로 간격 고정 | "내 PC 에서는 되는데" 를 내 PC 에서 재현하기 |
| Game 뷰 **Stats** | Game 뷰 오른쪽 위 Stats | 지금 fps 와 프레임 시간 |
| 추적점으로 **프레임마다 값 찍기** | `{Time.frameCount} {Time.deltaTime} {transform.position.x}` | 한 프레임에 얼마씩 바뀌나, 그것이 deltaTime 에 비례하나 (랩 44, 45) |
| 조사식에 **시간 값 나란히** | `Time.deltaTime`, `Time.fixedDeltaTime`, `Time.unscaledDeltaTime`, `Time.timeScale` | 어느 시간이 fps 와 timeScale 에 따라 바뀌나 (랩 46, 47) |
| Profiler → CPU → **Timeline** | 프레임 하나에 Update 와 FixedUpdate 가 몇 번씩 들어 있나 | 고정 간격 물리와 프레임의 관계 |

| 시간 값 | Update 에서 | FixedUpdate 에서 | `timeScale = 0` 이면 |
|---|---|---|---|
| `Time.deltaTime` | 지난 프레임부터 흐른 게임 시간 | `fixedDeltaTime` 과 같은 값 | 0 |
| `Time.fixedDeltaTime` | 설정값(기본 0.02) 그대로. **fps 와 상관없음** | 같음 | 그대로(FixedUpdate 는 멈춤) |
| `Time.unscaledDeltaTime` | 지난 프레임부터 흐른 진짜 시간 | 진짜 시간 | 진짜 시간 |

## 10부 랩 목록

| # | 이름 | 증상 | 새로 배우는 것 | 난이도 |
|---|---|---|---|---|
| 44 | 호버카 | 30fps 에서 절반, 120fps 에서 두 배 속도 | **프레임당 값과 초당 값**, `* Time.deltaTime`, 단위를 이름에 | ★ |
| 45 | 따라오는 카메라 | 30fps 에서 카메라가 한참 처지고 120fps 에서 딱 붙는다 | **매 프레임 `Lerp(a, b, 0.1f)` 는 프레임 수의 거듭제곱**, `1 - exp(-k·dt)` 지수 감쇠, `SmoothDamp` | ★★ |
| 46 | 스태미나 | "시간을 곱했는데도" fps 에 따라 차는 속도가 다르다 | **`fixedDeltaTime` 은 Update 에서 쓰면 상수**, FixedUpdate 안의 `deltaTime` | ★ |
| 47 | 일시정지 메뉴 | `timeScale = 0` 에서 메뉴가 안 들어오고 카운트다운이 멈춘다 | **timeScale 0 에서도 Update 는 돈다**, `unscaledDeltaTime`, `WaitForSecondsRealtime`, Animator Unscaled Time | ★ |

---

# 11부: 성능 (GC 할당과 반복 탐색)

결과는 맞는데 **느린** 버그입니다. Unity 의 C# 은 GC(가비지 컬렉터)가 쓰레기를 모아 치우는 동안 메인 스레드를 멈추므로, **매 프레임 조금씩 쓰레기를 만드는 코드**는 몇 초마다 프레임을 툭 끊습니다. 그리고 "결과가 거의 늘 같은" 탐색(`Find`, `FindObjectsByType`)을 매 프레임 되풀이하면 오브젝트 수에 비례해 느려집니다. 이런 버그는 중단점으로는 안 보이고 **Profiler** 로 봅니다.

이 부의 랩은 **프레임당 GC 할당 바이트**로 판정합니다. Profiler 의 GC Alloc 열과 같은 카운터(`GC Allocated In Frame`)를 `ProfilerRecorder` 로 읽고(`Assets/BugHunt/Core/GcMeter.cs`), 아무것도 하지 않는 프레임의 값(에디터 자체의 몫)을 뺍니다. 시간(ms)은 PC 마다 다르므로 참고로만 찍습니다.

## 11부에서 쓰는 도구

| 도구 | 하는 법 | 언제 |
|---|---|---|
| **Profiler → CPU Usage → Hierarchy** | Window → Analysis → Profiler. 아래 창을 Hierarchy 로 두고 **GC Alloc** 열로 정렬. Calls, Time ms 열도 함께 | 어느 함수가 매 프레임 쓰레기를 만드나, 몇 번 불리나 (모든 랩) |
| **Deep Profile** | Profiler 위쪽 Deep Profile 을 켜고 다시 플레이 | 내 함수 안의 어느 호출(String.Concat, Enumerable.Count, 박싱)이 할당하나. 많이 느려지니 볼 때만 (랩 48) |
| **ProfilerMarker** | `static readonly ProfilerMarker s_Find = new ProfilerMarker("Turret.Find");` → `using (s_Find.Auto()) { ... }` | Deep Profile 없이 의심 구간만 Profiler 에 이름을 붙여 보기 (랩 48, 49) |
| **ProfilerRecorder** | `ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame")` → `LastValue` | 코드에서 할당량을 재서 테스트로 지키기 (이 부의 판정) |
| Profiler → **Timeline** | GC.Collect 가 언제, 얼마나 메인 스레드를 막나 | "몇 초마다 끊긴다" 의 정체 |
| **Profile Analyzer** (패키지) | 프레임 여러 개를 모아 함수별 평균·최대 | 고치기 전후 비교 |

## 11부 랩 목록

| # | 이름 | 증상 | 새로 배우는 것 | 난이도 |
|---|---|---|---|---|
| 48 | 체력 표시줄 | 값이 그대로인데도 매 프레임 쓰레기가 쌓여 GC 로 끊긴다 | **문자열 연결, LINQ, 인터페이스 foreach 박싱**, 매 프레임 `GetComponent`, 바뀔 때만 만들기, Deep Profile, ProfilerMarker | ★★ |
| 49 | 포탑의 표적 찾기 | 맵에 오브젝트가 많아지자 프레임이 무너지고 GC 가 튄다 | **Update 의 `GameObject.Find` / `FindObjectsByType`**, 한 번 찾기, 등록부(OnEnable/OnDisable), `Camera.main` 은 이제 캐시됨 | ★★ |
| 50 | 탄환 풀 | 풀을 만들었는데도 매 발 Instantiate, 풀은 늘 비어 있다 | **돌려주지 않는 오브젝트 풀**, Release 와 초기화, `UnityEngine.Pool.ObjectPool<T>` | ★ |
| 51 | 화염 방사기 | 적이 피해를 받지 않는다(에러 없음). 이름을 고쳐도 할당이 남는다 | **`SendMessage` 의 이름 오타와 박싱·리플렉션 비용**, 인터페이스, `TryGetComponent` | ★ |
| 52 | 치유 오라 | 기능은 맞는데 매 프레임 `OverlapSphere` 가 배열을 만든다 | **배열을 돌려주는 API 와 NonAlloc 짝**, 버퍼 크기와 돌려받은 개수 | ★ |

> GC 할당 판정은 에디터와 `-batchmode` 모두에서 같은 카운터를 씁니다. 에디터에서 Inspector 나 Profiler 창이 매 프레임 다시 그려지면 그 몫도 섞이므로, 버퍼를 넉넉히(프레임당 128~256바이트) 두었습니다. 고친 코드는 0 바이트 근처가 나옵니다.

---

# 12부: 데이터 레이스와 스레드

Unity 의 게임 코드는 대부분 메인 스레드 하나에서 돌지만, 무거운 일을 `Task.Run`, `Thread`, `Awaitable.BackgroundThreadAsync()` 로 넘기는 순간 **두 스레드가 같은 데이터를 만지는** 버그가 생깁니다. 7부 랩 30(메인 스레드에서 `.Result` 로 기다려 데드락)이 "서로 기다리기" 라면, 12부는 "서로 끼어들기" 와 "엉뚱한 스레드에서 Unity 부르기" 입니다.

레이스는 원래 가끔만 납니다. 연습이 되도록 **랩마다 매번 같은 결과**가 나오게 만들었습니다: 랩 53 은 랩이 넣은 결제 흉내가 동시에 들어온 요청끼리 서로를 최대 1초 기다리게 해서 "둘 다 확인을 통과한 상태" 를 확실하게 만들고, 랩 54·55 는 스레드가 바뀌는 것 자체가 버그라서 늘 재현됩니다.

## 12부에서 쓰는 도구

| 도구 | 하는 법 | 언제 |
|---|---|---|
| **스레드 창** | VS: 디버그 → 창 → 스레드 (Ctrl+Alt+H), Rider: Debug 창 Threads | 중단점에 멈춘 곳이 메인 스레드인가, 같은 줄에 스레드가 몇 개 있나 (모든 랩) |
| **병렬 스택 / 작업 창** | VS: 디버그 → 창 → 병렬 스택, 작업(Tasks) | 어떤 Task 가 어디까지 왔나, 끝났나, 예외로 끝났나 (랩 53, 54) |
| 중단점 **필터** / 추적점 | 중단점 → 조건 → 필터 `ThreadId == 1` / 추적점 `{Environment.CurrentManagedThreadId} Stock={Stock}` | 스레드별로 끼어드는 순서 보기. 추적점은 멈추지 않으므로 타이밍을 덜 바꾼다 (랩 53) |
| 조사식 `Thread.CurrentThread.ManagedThreadId` | await 앞뒤 줄에서 비교 | await 다음 줄이 어느 스레드에서 도나 (랩 55) |
| **예외 설정: throw 될 때 중단** | Common Language Runtime Exceptions 전체 체크 | Task 안에서 삼켜지는 예외를 던지는 자리에서 잡기 (랩 54) |
| 조사식 `task.Status`, `task.Exception` | Faulted / RanToCompletion, InnerException | "아무 일도 안 일어났다" 의 정체 (랩 54) |

## Job System 안전 시스템이 잡는 것과 못 잡는 것

6부(랩 25, 26)의 안전 시스템은 **NativeContainer 를 쓰는 잡**에 대해서만 레이스를 막아 줍니다.

| 상황 | 안전 시스템 | 이 판에서 |
|---|---|---|
| 두 잡이 같은 `NativeArray` 에 동시에 쓴다(의존 관계 없음) | **잡는다** (스케줄할 때 InvalidOperationException, 잡과 필드 이름이 나온다) | 랩 25 |
| 잡이 쓰는 중인 `NativeArray` 를 메인 스레드가 읽는다(Complete 전) | **잡는다** | 랩 25 |
| 해제된 `NativeArray` 를 잡이나 메인 스레드가 만진다 | **잡는다** (ObjectDisposedException) | 랩 26, 38 |
| `[ReadOnly]` 없이 두 잡이 같은 배열을 읽기만 한다 | 잡는다(쓰기로 보고 막는다). `[ReadOnly]` 를 붙이면 허용 | — |
| `Task.Run` / `Thread` 가 C# 객체(`List`, 필드, `Dictionary`)를 함께 쓴다 | **못 잡는다** (관리 힙은 감시 대상이 아니다) | 랩 53 |
| 워커 스레드에서 Unity API(`transform`, `gameObject`) 호출 | 안전 시스템이 아니라 **엔진이** UnityException 을 던진다. 단 Task 안이면 삼켜진다 | 랩 54, 55 |
| 잡 안에서 static 필드를 함께 쓴다 | **못 잡는다** (Burst 는 static 쓰기를 컴파일 오류로 막지만, Burst 를 끄면 그냥 돈다) | — |
| `NativeDisableParallelForRestriction`, `NativeDisableContainerSafetyRestriction` 을 붙인 필드 | 일부러 **끈 것**이라 못 잡는다 | — |

## 12부 랩 목록

| # | 이름 | 증상 | 새로 배우는 것 | 난이도 |
|---|---|---|---|---|
| 53 | 한정 판매 | 한 자루 남은 검을 두 명이 샀다. 재고 -1 | **확인하고 행동하기(check-then-act) 레이스**, `lock`, 공유 `List` 의 동시 Add, 안전 시스템이 못 잡는 관리 힙 | ★★ |
| 54 | 백그라운드 길 찾기 | 길 찾기는 끝까지 도는데 로봇이 안 움직인다. Console 은 조용하다 | **워커 스레드의 Unity API**, Task 안에서 삼켜지는 예외, `await Task.Run(...)` 뒤는 메인 스레드 | ★★ |
| 55 | 세이브 불러오기 | 백그라운드 해석으로 바꾸자 "can only be called from the main thread" | **await 다음 줄의 스레드**, `Awaitable.BackgroundThreadAsync` / `MainThreadAsync`, UnitySynchronizationContext | ★★ |

---

# 채점

모든 랩을 한 번에 돌려 랩마다 결과를 매기고 총점을 냅니다. 버그를 다 고쳤다고 생각되면 돌려 보세요. **버그가 있는 처음 상태에서는 0점이 정상**입니다.

| 결과 | 뜻 |
|---|---|
| **PASS** | 보통 입력과 숨은 입력 모두 통과 |
| **FAIL** | `[FAIL]` 이 하나라도 나옴, 또는 랩 도중 Console 에 에러 |
| **TIMEOUT** | HangGuard 가 "응답 없음" 을 선언(7부), 또는 랩 제한 시간(15초) 초과 |
| **CRASH** | 처리되지 않은 예외로 랩이 중단됨 (detail 에 예외 메시지 첫 줄) |
| **SKIP** | 이 환경에서는 판정할 수 없음: `-nographics` 의 랩 19, 오디오 장치가 없는 PC 의 랩 13~15. **총점에서 뺍니다** |

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
   | `-NoGraphics` | 그래픽 장치 없이(`-nographics`). 서버/CI 용. 랩 19 는 SKIP |
   | `-Labs 24,25,31` | 이 랩만 채점 |
   | `-Report 경로` | 결과 JSON 위치 (기본: 프로젝트 폴더의 `grade-report.json`) |
   | `-TimeoutMinutes 20` | 전체 제한 시간. 넘으면 Unity 를 강제로 끈다(아래) |

   Unity 로그는 `grade.log` 에 남습니다(컴파일 에러가 나면 여기서 `error CS` 를 찾으세요. grade.bat 도 처음 몇 줄을 보여 줍니다).

### 숨은 테스트 케이스

랩 결과의 숫자(기대값)만 보고 그 값을 코드에 박아 넣으면 보통 실행은 통과할 수 있습니다. 채점기는 보통 입력으로 PASS 한 랩을 **다른 입력(숨은 케이스)** 으로 한 번 더 돌립니다. 적의 수, 피해량, 좌표, 색, 세이브 값, 서버 가동 시간 같은 입력이 바뀌므로, 진짜 원인을 고쳤다면 그대로 통과하고 값을 박아 넣었다면 `[숨은 케이스] ...` 로 FAIL 합니다. 랩 코드에서 `Case(보통 값, 숨은 값)` 이 그 입력입니다(코드를 열면 보이지만, 거기에 맞추는 것은 의미가 없습니다). 입력이 하나뿐인 랩(10, 12~15, 20, 21, 23: 에셋 설정이나 엔진 동작을 고치는 랩)은 한 번만 돕니다.

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
        { "id": 28, "title": "엘리베이터", "part": 7, "result": "TIMEOUT", "ms": 2003, "detail": "응답 없음: Passenger.RideTo: 엘리베이터가 오기를 기다림 (...)" }
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

- 에디터: Window → General → **Test Runner** → **PlayMode** 탭 → Run All. 고친 랩만 초록색이 됩니다.
- 명령줄(에디터를 닫고, Unity.exe 경로는 설치 위치에 맞게):

```
"C:\Program Files\Unity\Hub\Editor\6000.3.27f2\Editor\Unity.exe" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults TestResults.xml -logFile -
```

| 실행 방식 | 판정되지 않는 랩 (`[SKIP]` → 테스트 Skipped) |
|---|---|
| 에디터에서 Play / Test Runner, `-batchmode` | 없음 |
| `-batchmode -nographics` | 19 (그래픽 장치가 없어 픽셀을 그릴 수 없음) |
| 오디오 출력 장치가 없는 PC/CI | 13, 14, 15 추가 |

이 저장소를 만들 때 Windows 11 + Unity 6000.3.27f2 에서 이렇게 확인했습니다.

| 상태 | `-batchmode` | `-batchmode -nographics` |
|---|---|---|
| 버그 그대로 | 55개 모두 실패 | 54개 실패, 19 Skipped |
| SOLUTIONS.md 의 수정 적용 | 55개 모두 통과 | 54개 통과, 19 Skipped |
| 수정을 되돌림 | 55개 모두 실패 | (같은 코드라 생략) |

채점기(`grade.bat`)도 같은 PC 에서 이렇게 확인했습니다.

| 상태 | `grade.bat` | `grade.bat -NoGraphics` |
|---|---|---|
| 버그 그대로 | 0 / 55 (CRASH 11, FAIL 40, TIMEOUT 4), 종료 코드 1 | 0 / 54 (SKIP 1: 랩 19), 종료 코드 1 |
| SOLUTIONS.md 의 수정 적용 | 55 / 55, 종료 코드 0 | 54 / 54 (SKIP 1: 랩 19), 종료 코드 0 |

- 보통 입력의 기대값만 코드에 박아 넣은 가짜 수정(랩 2, 31)은 `[숨은 케이스]` FAIL 로 걸러졌습니다.
- 랩 28 의 `HangGuard.Tick` 을 지워 에디터를 정말로 굳힌 상태에서 `grade.bat -TimeoutMinutes 1` 은 1분 뒤 Unity 를 끄고, 결과 파일에 랩 28 을 TIMEOUT, 그 뒤 랩을 FAIL 로 남겼습니다.

랩 7·23·40·42·44~46·50~52 는 `Time.captureDeltaTime` 으로 프레임 간격을 고정하므로 PC 성능과 상관없이 같은 결과가 나옵니다.

---

## 다시 풀고 싶을 때

이 폴더는 git 저장소입니다. 고친 뒤 `git restore Assets` 로 버그 상태로 되돌릴 수 있습니다.
랩 11·16 을 돌리면 `Rifle.asset`, `Crate.mat` 이 바뀐 채 저장될 수 있습니다(그게 그 랩의 교훈입니다). `git status` 로 확인하고 `git restore` 하세요.
