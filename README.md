# BugHunt Unity — 유니티 C# 버그 추적 실습

[BugHunt](https://github.com/gusgh0157/BugHunt)(C++ 콘솔판)와 BugHunt UE(언리얼판)의 Unity 버전입니다.
버그가 **일부러 심어진** 작은 게임 코드 모음이고, 이번에는 **Unity 에서만 생기는** 버그들입니다.

- **1부: 크래시 / 예외 (랩 1~4)**: Console 에 빨간 줄이 뜬 순간, 스택 트레이스를 거슬러 올라가 원인을 찾습니다. `MissingReferenceException`, `GetComponent` 가 준 null, Awake/OnEnable/Start 순서, 꺼진 오브젝트의 코루틴.
- **2부: 흐름 추적 (랩 5~11)**: 게임은 멀쩡히 도는데 **결과가 틀립니다**. 이벤트 구독, 실행 순서, FixedUpdate, fake null, 구조체 복사, 직렬화된 값, ScriptableObject 처럼 "Unity 가 대신 해 주는 일"의 순서와 수명을 따라갑니다.
- **3부: 사운드 (랩 12~15)**: 소리가 늦게 나고, 안 들리고, 끊기는 버그. 로드 상태·가상 음성·재생 위치를 재서 판정합니다.
- **4부: 이미지 / 렌더링 (랩 16~19)**: 엉뚱한 상자까지 빨개지고, 메모리가 새고, 아이콘이 바뀌고, 색이 물 빠지는 버그. 머티리얼 값, Material 객체 수, 텍스처 이름, 화면 픽셀로 판정합니다.
- **5부: 애니메이션 (랩 20~23)**: 공격 모션이 안 나오고, 화면 밖 발소리가 안 나고, 검격 판정이 빠지고, 손에 든 무기가 한 박자 늦는 버그.
- **6부: 잘못된 참조와 네이티브 메모리 (랩 24~27)**: 예외 없이 새는 `NativeArray`, "분명히 Complete 했는데" 터지는 잡, 다음 프레임에 사라지는 `Allocator.Temp`, Domain Reload 를 끈 Play 에서 지난 판의 오브젝트를 붙잡는 정적 필드.
- **7부: 멈춤 (랩 28~31)**: 크래시 없이 에디터가 굳는 무한루프와 데드락. 모두 중단, 스레드 창, 메인 스레드의 호출 스택으로 갇힌 곳을 찾습니다. 연습 중에 정말로 굳지 않도록 `HangGuard` 가 2초 뒤 "응답 없음" 을 알리고 빠져나옵니다.

> 콘솔판·언리얼판과 같은 규칙입니다. **코드를 눈으로 훑어 정답을 찾지 마세요.** 1부는 **예외가 난 곳에서부터**, 2부부터는 **직접 멈춘 곳에서부터 값을 확인하며** 추적하세요.
> 콘솔판 1·3부를 먼저 끝냈다면 좋습니다. 호출 스택 창, 지역 창, 조건부 중단점, 추적점 사용법은 거기서 설명한 그대로 씁니다.

## BugHunt 시리즈

같은 방식(일부러 심은 버그를 디버거로 추적)으로 만든 네 가지 판입니다.

| 판 | 다루는 버그 | 저장소 |
|---|---|---|
| C++ 콘솔 | 호출 스택, 메모리 오류, 크래시 없는 흐름 추적 (랩 28개) | [BugHunt](https://github.com/gusgh0157/BugHunt) |
| C# 콘솔 | 예외와 스택 트레이스, 흐름 추적, 비동기/동시성, 리소스와 메모리 (랩 28개) | [BugHuntCS](https://github.com/gusgh0157/BugHuntCS) |
| 언리얼 엔진 | 크래시, 흐름 추적, 멀티플레이, 사운드, 이미지, 애니메이션 (랩 21개) | [BugHuntUE](https://github.com/gusgh0157/BugHuntUE) |
| 유니티 | 크래시/예외, 흐름 추적, 사운드, 이미지, 애니메이션, 잘못된 참조와 네이티브 메모리, 멈춤 (랩 31개) | [BugHuntUnity](https://github.com/gusgh0157/BugHuntUnity) **(지금 보는 저장소)** |

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
3. 결과는 Game 창 오른쪽과 **Console 창**(Window → General → Console)에 나옵니다. Console 검색창에 `[BugHunt]` 를 넣으면 랩 결과만 보입니다.

```
[BugHunt] ==== 랩 5: 전투 기록 창 ====
[BugHunt] [FAIL] 다시 연 창의 기록 줄 수 (기대: 1, 실제: 2)
[BugHunt] [FAIL] 띄운 알림 총합 (10, 7) (기대: 2, 실제: 4)
[BugHunt] ==== 랩 5 결과: PASS 1 / FAIL 3 ====
```

**같은 랩을 계속 돌릴 때**: 코드를 고치고 Unity 로 돌아오면 자동으로 다시 컴파일됩니다. 그다음 **BugHunt → 마지막 랩 다시 실행 (Ctrl+Alt+R)**. 플레이 중이면 플레이를 멈추지 않고 바로 다시 돌립니다.

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

랩 코드는 `Assets/BugHunt/Labs/LabNN_*.cs` 입니다(랩 10·11 은 `Lab10/`, `Lab11/` 폴더에 클래스가 하나 더 있습니다). `Assets/BugHunt/Core/`(랩 실행기, 7부의 `HangGuard`)와 `Editor/`, `Tests/` 에는 버그가 없습니다.
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

# 부록 A: 멀티플레이(Netcode for GameObjects)는?

언리얼판에는 리슨 서버 랩이 있지만 이 프로젝트에는 넣지 않았습니다. Netcode for GameObjects 의 권한/RPC 버그를 판정하려면 서버와 클라이언트 **두 개의 인스턴스**가 필요한데(Multiplayer Play Mode 또는 빌드 + 에디터), 이 프로젝트의 자동 검증은 에디터 하나에서 PlayMode 테스트로 돌기 때문입니다. 넷코드 랩은 별도 프로젝트로 다루는 것이 좋습니다.

# 부록 B: 자동 검증 (Test Runner)

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
| 버그 그대로 | 23개 모두 실패 | 22개 실패, 19 Skipped |
| SOLUTIONS.md 의 수정 적용 | 23개 모두 통과 | 22개 통과, 19 Skipped |
| 수정을 되돌림 | 23개 모두 실패 | (같은 코드라 생략) |

랩 7·23 은 `Time.captureDeltaTime` 으로 프레임 간격을 고정하므로 PC 성능과 상관없이 같은 결과가 나옵니다.

---

## 다시 풀고 싶을 때

이 폴더는 git 저장소입니다. 고친 뒤 `git restore Assets` 로 버그 상태로 되돌릴 수 있습니다.
랩 11·16 을 돌리면 `Rifle.asset`, `Crate.mat` 이 바뀐 채 저장될 수 있습니다(그게 그 랩의 교훈입니다). `git status` 로 확인하고 `git restore` 하세요.
