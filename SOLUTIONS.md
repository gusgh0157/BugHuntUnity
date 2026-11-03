# 해답 (스포일러!)

수치(프레임 번호, ms)는 PC 와 실행 방식에 따라 조금씩 다릅니다. 판정에 쓰는 값(기대/실제)은 같습니다.

# 1부: 크래시 / 예외

## Lab 1 — 남이 파괴한 표적을 계속 쏨
```
멈춘 함수:        Tower.Update (Target.transform 에 접근)
이상한 값:        Target == null 이 true. 그런데 조사식으로 펼치면 Health 등 필드가 보인다 (파괴된 오브젝트)
그 값이 온 곳:     Enemy.TakeDamage 의 Destroy ← Bomb.Explode ← Lab01_Tower.Run  (감시탑이 아니라 폭탄이 파괴)
거쳐 온 엔진 기능: Update 는 Unity 가 매 프레임 부른다(아래에 내 코드 프레임 없음). Destroy 는 프레임 끝에 실제로 파괴
진짜 원인:        감시탑은 "자기가 처치했을 때만" 다음 표적을 고른다. 남이 표적을 파괴하는 경우를 생각하지 않았다.
```
**수정:**
```csharp
void Update()
{
    if (Target == null)          // Unity 의 == : 파괴된 오브젝트도 null 로 본다
    {
        Target = PickTarget();
        if (Target == null)
        {
            return;
        }
    }
    cooldown -= Time.deltaTime;
    ...
}
```
**교훈:** Unity 오브젝트를 들고 있는 필드는 **언제든 남에게 파괴될 수 있다.** 쓰기 직전에 `== null` 로 확인하는 것이 기본이다. `MissingReferenceException` 은 "null 이라서"가 아니라 "파괴됐는데 접근해서"다. 누가 파괴했는지는 `Destroy` 줄(또는 `OnDestroy`)에 중단점을 걸어 호출 스택을 보면 바로 나온다.

## Lab 2 — GetComponent 는 맞은 오브젝트 자신만 본다
```
멈춘 함수:        Rifle.Shoot (health.TakeDamage) — 세 번째 발
이상한 값:        health == null, hit.collider.name == "Head"
그 값이 온 곳:     hit.collider.GetComponent<Health>(): 맞은 콜라이더는 자식 Head 에 있고, Health 는 부모 TrainingDummy 에 있다
진짜 원인:        몸통 콜라이더는 Health 와 같은 오브젝트라 우연히 동작했다. 자식 콜라이더에서는 GetComponent 가 null.
```
**수정:**
```csharp
var health = hit.collider.GetComponentInParent<Health>();
if (health == null)
{
    return;   // 벽, 바닥처럼 체력이 없는 것을 맞혔다
}
health.TakeDamage(headshot ? Damage * 2 : Damage);
```
(`hit.rigidbody`/`hit.collider.attachedRigidbody` 가 있는 구조라면 그 오브젝트에서 찾는 방법도 흔하다.)
**교훈:** `GetComponent<T>()` 는 실패해도 예외 없이 null 을 준다. 캐릭터는 보통 "루트에 로직, 자식에 콜라이더/모델" 구조라서 부위 판정에서 자주 터진다. 없을 수 있다면 `TryGetComponent` 나 null 검사, 반드시 있어야 하면 `[RequireComponent]`.

## Lab 3 — 싱글턴 등록을 Start 에서 함
```
멈춘 함수:        ScoreboardUI.OnEnable
이상한 값:        GameManager.Instance == null
그 값이 온 곳:     스택: ScoreboardUI.OnEnable ← GameObject.AddComponent ← Lab.Spawn ← Lab03_Scoreboard.Run
                 즉 AddComponent 안에서 OnEnable 이 바로 불렸다. 그때 GameManager 는 Awake/OnEnable 만 끝났고 Start 는 아직.
순서:            GameManager.Awake → GameManager.OnEnable → ScoreboardUI.Awake → ScoreboardUI.OnEnable(여기서 죽음) → … 다음 프레임 → GameManager.Start
진짜 원인:        Instance 를 Start 에서 채웠다.
```
**수정:**
```csharp
void Awake()
{
    Instance = this;
}
```
**교훈:** "남이 나를 쓸 준비"는 Awake, "내가 남을 쓰는 일"은 Start. AddComponent/Instantiate/SetActive(true) 는 그 줄 안에서 Awake·OnEnable 을 부른다. 씬에 처음부터 놓인 오브젝트끼리의 Awake 순서는 정해져 있지 않으므로, 이 버그는 "씬에서는 되는데 프리팹으로 만들면 안 된다"는 식으로도 나타난다.

## Lab 4 — 루트를 끄면 그 오브젝트의 코루틴도 끝남
```
에러:            Coroutine couldn't be started because the the game object 'Player' is inactive!  ← Player.Die
이상한 값:        Die 안에서 SetActive(false) 다음 줄이 StartCoroutine
보너스 실험:      순서를 바꾸면 에러는 사라지지만 RespawnAfter 의 yield 다음 줄에 도착하지 않는다 → 여전히 FAIL
진짜 원인:        코루틴은 시작한 MonoBehaviour 의 것이다. 그 게임 오브젝트가 비활성화되면 코루틴은 중지(종료)되고, 다시 켜도 이어지지 않는다.
```
**수정 (택1):**
```csharp
// ① 보이는 부분만 숨긴다 (이 랩의 정답)
Model.SetActive(false);
StartCoroutine(RespawnAfter(0.5f));

// ② 항상 켜져 있는 다른 오브젝트에서 기다린다
GameSession.Instance.StartCoroutine(RespawnAfter(0.5f));
```
**교훈:** 에러를 없애는 것과 버그를 고치는 것은 다르다. "꺼지는 오브젝트"에서 기다리는 일(부활, 쿨다운, 페이드)을 하지 말 것. `enabled = false`(컴포넌트만 끄기)는 코루틴을 멈추지 않지만 `SetActive(false)` 와 `Destroy` 는 멈춘다.

---

# 2부: 흐름 추적

## Lab 5 — OnEnable 에서 구독하고 OnDisable 에서 해제하지 않음
```
틀린 결과:     다시 연 창의 기록 2줄, 알림 4번, 창을 파괴한 뒤 알림 6번
멈춘 위치:     HandleDamaged 중단점 → damage 가 10, 5, 7, 7, 3, 3 순서
기대 → 실제:   10, 7 → 10, 5(창이 꺼져 있는데!), 7, 7, 3, 3(창이 파괴됐는데!)
처음 어긋난 곳: 창을 끈 뒤의 TakeHit(5). Damaged.GetInvocationList().Length: 켬 1 → 끔 1 → 다시 켬 2 → 파괴 2
진짜 원인:     OnEnable 에서 += 만 하고 -= 가 없다. 꺼지거나 파괴돼도 C# 이벤트는 대리자(와 그 안의 this)를 계속 들고 있다.
```
**수정:**
```csharp
void OnDisable()
{
    if (Target != null)
    {
        Target.Damaged -= HandleDamaged;
    }
}
```
**교훈:** C# 이벤트·`Action`·`UnityEvent.AddListener` 는 Unity 오브젝트의 수명을 모른다. **구독한 곳마다 짝이 되는 해제**(OnEnable↔OnDisable, Awake/Start↔OnDestroy)를 둔다. 파괴된 구독자가 `transform` 같은 Unity API 를 건드리면 그때서야 MissingReferenceException 이 나고, 필드만 건드리면 조용히 틀린다. 파괴된 오브젝트를 붙잡고 있으니 메모리 누수이기도 하다.

## Lab 6 — 읽는 쪽이 쓰는 쪽보다 먼저 Update
```
틀린 결과:     30프레임 모두 HP 바가 1 크고, 쓰러진 프레임에 HP 바 1
멈춘 위치:     두 Update 에 추적점 → 매 프레임 "HealthBar → BleedingHero" 순서
처음 어긋난 곳: 첫 프레임부터 (영웅 29, HP 바 30)
진짜 원인:     BleedingHero 의 [DefaultExecutionOrder(100)]. HealthBar 는 기본 0 이라 먼저 돌아 지난 프레임의 HP 를 읽는다.
```
**수정 (택1):**
```csharp
// ① HealthBar: 남이 바꾼 값을 보여 주는 쪽은 LateUpdate
void LateUpdate()
{
    Displayed = Hero.Health;
}

// ② 순서를 명시
[DefaultExecutionOrder(200)]
public class HealthBar : MonoBehaviour { ... }
```
**교훈:** Script Execution Order 가 같은 스크립트끼리의 Update 순서는 **정해져 있지 않다**(대개 생성 순서지만 믿으면 안 된다). 순서가 중요하면 LateUpdate, `[DefaultExecutionOrder]`, 또는 한 매니저가 순서대로 부르는 구조로 명시한다. `[DefaultExecutionOrder]` 는 Project Settings 창에 안 보이는 경우가 있어 "아무도 순서를 안 바꿨는데요"의 원인이 된다.

## Lab 7 — 한 프레임짜리 입력을 FixedUpdate 에서 읽음
```
틀린 결과:     10번 눌러 5번 점프
쓴 도구:       Update/FixedUpdate 추적점 → 프레임 간격 8.3ms, FixedUpdate 간격 20ms: FixedUpdate 는 2~3 프레임에 한 번
처음 어긋난 곳: JumpDown 이 true 인 프레임에 FixedUpdate 가 0번 돈 첫 점프
진짜 원인:     GameInput.JumpDown(= Input.GetButtonDown) 은 그 프레임 동안만 true. FixedUpdate 가 그 프레임에 안 돌면 놓치고,
              느린 PC(한 프레임에 FixedUpdate 2번)에서는 반대로 두 번 뛴다.
```
**수정:**
```csharp
bool jumpQueued;

void Update()
{
    if (GameInput.JumpDown)
    {
        jumpQueued = true;
    }
}

void FixedUpdate()
{
    if (jumpQueued)
    {
        jumpQueued = false;
        body.linearVelocity = Vector3.up * 5f;
        ++Jumps;
    }
}
```
**교훈:** Update 는 프레임마다 정확히 1번, FixedUpdate 는 0~여러 번. 입력은 Update 에서 받고 물리는 FixedUpdate 에서 소비한다. "모니터/PC 에 따라 다르다"는 신고는 이 두 루프의 횟수 차이를 먼저 의심하자. 이 랩처럼 `Time.captureDeltaTime` 으로 프레임 간격을 고정하면 재현이 확실해진다.

## Lab 8 — fake null: `??=` 와 `?.` 는 파괴를 모른다
```
틀린 결과:     허수아비 2개가 서 있음, 점수 500
멈춘 위치:     Score += 100 두 번째 적중 → Target = Dummy_0, Health -10
조사식:        Target == null → true,  (object)Target == null → false,  Target is null → false
처음 어긋난 곳: 화살 3: Dummy_0 은 화살 2 에서 파괴됐는데 Target ??= 이 "비어 있지 않다"고 보고 그대로 둠 → Target?.TakeHit 도 그대로 호출
진짜 원인:     ?? ??= ?. is null 은 Unity 가 오버로드한 == 를 쓰지 않는다. 파괴된 오브젝트의 C# 껍데기(fake null)를 살아 있다고 본다.
```
**수정:**
```csharp
if (Target == null)
{
    Target = FindNearest();
}
if (Target != null && Target.TakeHit(10))
{
    Score += 100;
}
```
**교훈:** `UnityEngine.Object` 에는 `==`/`!=`/`bool` 변환만 쓴다. VS/Rider 의 Unity 분석기(UNT0007, UNT0008, UNT0023)가 경고해 준다. 같은 파일의 `FindNearest` 가 이미 `d != null` 을 쓰고 있었다는 점도 단서. 반대로 `Destroy` 직후 같은 프레임에서는 `== null` 도 false 다(실제 파괴는 프레임 끝).

## Lab 9 — List 인덱서가 돌려준 구조체 복사본을 수정
```
틀린 결과:     최고 높이 2.5
멈춘 위치:     SnapWaypointsToGround 의 for 안. F10 전후로 Waypoints[i] 가 그대로 (5.00, 2.50, 0.00)
진짜 원인:     Vector3 는 struct. List<Vector3>[i] 는 칸의 복사본을 돌려주고, 복사본의 Set() 은 버려진다.
              (Waypoints[i].y = 0 은 CS1612 로 컴파일러가 막지만, 메서드 호출은 막지 않는다)
```
**수정:**
```csharp
for (int i = 0; i < Waypoints.Count; ++i)
{
    Vector3 p = Waypoints[i];
    p.y = GroundHeight;
    Waypoints[i] = p;
}
```
**교훈:** `transform.position`, `rigidbody.linearVelocity`, `List<Vector3>[i]`, `Dictionary` 값처럼 **구조체를 돌려주는 속성/인덱서**는 전부 복사본이다. "읽고 → 고치고 → 다시 대입". 배열(`Vector3[]`)은 칸을 직접 가리켜서 `arr[i].Set()` 이 동작한다는 차이 때문에 리팩터링하다 생기기도 한다. (예외: `particleSystem.main` 같은 모듈 구조체는 내부적으로 원본을 가리키도록 만들어져 있어서 복사본에 써도 된다.)

## Lab 10 — 프리팹에 저장된 값이 코드 초기값을 이김
```
틀린 결과:     프리팹 슬라임 MaxHealth 100 (AddComponent 슬라임은 150)
멈춘 위치:     SlimeStats.Awake 에 중단점 → 들어온 순간 이미 100
확인:          Slime.prefab 을 텍스트로 열면 "MaxHealth: 100"
처음 어긋난 곳: Instantiate 가 ① 필드 초기값 150 으로 만든 뒤 ② 프리팹에 저장된 100 으로 덮어씀 → ③ Awake
진짜 원인:     1.0 때 만든 프리팹이 100 을 저장하고 있다. 코드 초기값은 "처음 붙일 때의 기본값"일 뿐이다.
```
**수정:** `Assets/BugHunt/Resources/BugHunt/Slime.prefab` 을 열어 Inspector 에서 MaxHealth 를 150 으로 바꾸고 저장. (컴포넌트 ⋮ → **Reset** 도 되지만 다른 필드도 코드 초기값으로 돌아간다.)
**교훈:** 직렬화되는 필드(`public`, `[SerializeField]`)의 값은 `필드 초기값 → 프리팹/씬에 저장된 값 → 프리팹 인스턴스의 오버라이드(굵은 글씨)` 순으로 덮인다. "코드 기본값을 바꿨는데 안 바뀐다"면 프리팹, 프리팹 Variant, 씬에 놓인 인스턴스를 차례로 의심하자. 필드 이름을 바꾸면 저장된 값이 사라지는데, 유지하려면 `[FormerlySerializedAs("옛이름")]`.

## Lab 11 — 게임 중 바뀌는 값을 ScriptableObject 에 씀
```
틀린 결과:     플레이어 2 탄약 27, 에셋 Ammo 변화량 -3. 랩을 다시 돌리면 "랩 시작 시 에셋 Ammo: 27" → 24 → …
멈춘 위치:     Fire 의 --Data.Ammo → 조사식 player1.Data == player2.Data → true (같은 에셋 객체)
진짜 원인:     WeaponData 에셋은 프로젝트에 하나뿐인 객체. 두 무기가 같은 객체의 Ammo 를 깎는다.
              에디터에서는 플레이 모드가 끝나도 메모리의 에셋 객체가 그대로라 값이 남고, 에셋이 저장되면 파일에도 들어간다.
```
**수정:**
```csharp
public class Weapon : MonoBehaviour
{
    public WeaponData Data;
    int ammo;

    public int AmmoLeft => ammo;

    public void Equip(WeaponData data)
    {
        Data = data;
        ammo = data.MaxAmmo;
    }

    public bool Fire()
    {
        if (ammo <= 0)
        {
            return false;
        }
        --ammo;
        return true;
    }
}
```
(`WeaponData.Ammo` 필드는 지워도 된다.)
**교훈:** ScriptableObject 에는 **읽기 전용 설정**만. 게임 중 바뀌는 상태는 컴포넌트(인스턴스)가 들고 있는다. 런타임 사본이 필요하면 `Instantiate(data)`. 빌드에서는 프로그램을 껐다 켜면 원래 값으로 돌아가므로 "에디터에서만 생기는 버그"처럼 보이지만, 빌드에서도 **같은 실행 안에서는** 두 플레이어가 값을 공유하는 진짜 버그다. 이미 값이 줄어 있다면 에디터를 다시 켜거나 에셋을 Reimport 하자(`git status` 로 Rifle.asset 이 바뀌었는지도 확인).

---

# 3부: 사운드

## Lab 12 — Preload Audio Data 를 꺼 둔 클립을 미리 로드하지 않음
```
틀린 결과:     첫 발을 쏜 순간 loadState == Unloaded. 발사 후 데이터가 올라오기까지 몇 프레임
멈춘 위치:     Blaster.Fire → FireClip.loadState: Unloaded, preloadAudioData: false, loadInBackground: true
진짜 원인:     임포트 설정에서 Preload Audio Data 를 껐다. Resources.Load 로 클립 객체는 생겼지만 소리 데이터는 "처음 재생할 때" 올라온다.
              Load In Background 가 켜져 있어 재생이 데이터가 올라온 뒤로 미뤄진다 → 섬광보다 늦은 소리.
```
**수정 (택1):**
```csharp
// ① 장착할 때 미리 올린다 (메모리를 아끼려고 끈 설정을 살리는 방법)
public void Equip(AudioClip fireClip)
{
    FireClip = fireClip;
    source = gameObject.AddComponent<AudioSource>();
    source.playOnAwake = false;
    FireClip.LoadAudioData();
}
```
② `lab12_blaster.wav` 임포트 설정에서 **Preload Audio Data** 를 켜고 Apply.
**교훈:** 오디오 임포트 설정 셋의 의미: **Load Type**(메모리에 어떤 형태로: 풀어서/압축째/스트리밍), **Preload Audio Data**(씬·에셋이 로드될 때 데이터도 같이), **Load In Background**(로드를 기다리지 않고 미루기). 끄면 메모리를 아끼는 대신 "언제 올릴지"를 코드가 책임져야 한다. Load In Background 를 끄면 지연 대신 그 프레임이 멈칫한다(히치).

## Lab 13 — 실제 음성 한도를 넘었고 보스 대사의 우선순위가 같음
```
틀린 결과:     boss.Voice.isPlaying == true 인데 isVirtual == true
도구:          Profiler Audio 모듈 → 재생 41개, 실제 32개, 가상 9개
진짜 원인:     Max Real Voices 32 < 동시에 재생하려는 소리 41. 우선순위가 모두 128 이라 "더 작게 들리는" 보스 대사(볼륨 0.6)가 가상으로 밀려났다.
```
**수정:**
```csharp
Voice.priority = 0;   // 0 이 가장 중요
```
(발소리 쪽을 `priority = 200` 으로 낮춰도 된다. 근본적으로는 같은 소리를 수십 개 동시에 낼 필요가 없으니 발소리 수를 제한한다.)
**교훈:** "소리가 안 들린다"의 큰 원인 하나가 **동시 재생 한도**. 가상 음성은 위치만 계속 계산하고 소리는 내지 않다가 자리가 나면 이어서 나온다. 중요한 소리(대사, UI, 경보)는 priority 로 보호한다.

## Lab 14 — AudioSource 가 붙은 오브젝트를 바로 Destroy
```
틀린 결과:     통은 사라졌고, 0.2초 뒤 폭발음을 재생 중인 AudioSource 가 없다
도구:          Explode 끝에 Debug.Break() → 다음 프레임 Hierarchy 에서 ExplosiveBarrel 과 그 AudioSource 가 사라짐
진짜 원인:     소리의 수명 = AudioSource 의 수명 = 게임 오브젝트의 수명. Destroy 가 재생 중인 소리를 끊었다.
```
**수정:**
```csharp
public void Explode()
{
    AudioSource.PlayClipAtPoint(ExplosionClip, transform.position);
    Destroy(gameObject);
}
```
**교훈:** 사라지는 것(총알, 적, 상자)이 내는 소리는 **다른 수명을 가진 곳**에서 낸다: `PlayClipAtPoint`(임시 "One shot audio" 오브젝트), 떼어 낸 자식 + `Destroy(go, clip.length)`, 사운드 매니저의 AudioSource 풀. `PlayClipAtPoint` 는 AudioSource 설정(믹서 그룹, 3D 설정)을 줄 수 없어서 큰 프로젝트에서는 풀이 일반적이다.

## Lab 15 — 짧은 효과음을 Play() 로 내서 긴 소리를 끊음
```
틀린 결과:     1초 뒤 Source.clip == grunt, 브리핑 재생 안 함
멈춘 위치:     PlayHurt → F10 전후: Source.clip lab15_briefing → lab15_grunt, Source.time 0.5 → 0
진짜 원인:     AudioSource 하나는 clip 하나를 재생한다. clip 을 바꾸고 Play() 하면 지금 재생 중이던 브리핑은 멈춘다.
```
**수정:**
```csharp
public void PlayHurt(AudioClip grunt)
{
    Source.PlayOneShot(grunt);
}
```
**교훈:** `Play()` = 이 소스의 "주 트랙"을 처음부터(제어 가능: Stop, Pause, time). `PlayOneShot()` = 주 트랙을 건드리지 않고 겹쳐 쏘기(개별 제어 불가, `Stop()` 하면 같이 멈춤). 대사·음악처럼 제어가 필요한 것과 효과음은 AudioSource 를 나누는 것이 깔끔하다.

---

# 4부: 이미지 / 렌더링

## Lab 16 — sharedMaterial 을 고쳐서 에셋 자체를 바꿈
```
틀린 결과:     옆 상자도 빨강, Crate.mat 에셋도 빨강
멈춘 위치:     OnHoverEnter → body.sharedMaterial.GetInstanceID() 가 모든 상자에서 같음 = Crate.mat 에셋
진짜 원인:     sharedMaterial 은 에셋 그 자체. 같은 머티리얼을 쓰는 모든 렌더러가 바뀌고, 에디터에서는 에셋 파일에도 저장될 수 있다.
```
**수정:**
```csharp
public void OnHoverEnter()
{
    body.material.color = Color.red;   // 이 렌더러 전용 사본 "Crate (Instance)"
}

public void OnHoverExit()
{
    body.material.color = Color.white;
}
```
(사본의 정리는 랩 17 참고. 색만 바꾼다면 MaterialPropertyBlock 이 더 좋다.)
**교훈:** `sharedMaterial`/`sharedMesh` = 에셋(모두 공유), `material`/`mesh` = 첫 접근 때 복제한 내 것. 에디터에서 `sharedMaterial` 을 런타임에 고치면 플레이를 멈춰도 돌아오지 않는다. `git status` 에 `.mat` 이 뜨면 이 버그를 의심하자.

## Lab 17 — renderer.material 복제본이 누수됨
```
틀린 결과:     웨이브마다 Material 객체 +100, 상자를 다 지운 뒤에도 +200
도구:          Paint 전후 sharedMaterial.name: "Crate" → "Crate (Instance)". Memory Profiler 스냅샷 비교: Material "Crate (Instance)" 200개
진짜 원인:     renderer.material 은 머티리얼을 복제한다. 복제본은 게임 오브젝트를 지워도 함께 지워지지 않는다(씬 언로드나 UnloadUnusedAssets 까지 남음).
              게다가 상자마다 머티리얼이 달라 배칭도 깨진다(Frame Debugger 에서 드로 콜 수 확인).
```
**수정:**
```csharp
public class SupplyCrate : MonoBehaviour
{
    public Color Tint;
    static MaterialPropertyBlock block;
    static readonly int ColorId = Shader.PropertyToID("_Color");

    public void Paint(Color tint)
    {
        Tint = tint;
        block ??= new MaterialPropertyBlock();
        var body = GetComponent<Renderer>();
        body.GetPropertyBlock(block);
        block.SetColor(ColorId, tint);
        body.SetPropertyBlock(block);
    }
}
```
(복제본을 꼭 써야 한다면 `OnDestroy` 에서 `Destroy(GetComponent<Renderer>().material)`. 단 `OnDestroy` 에서 `.material` 에 처음 접근하면 그 자리에서 또 복제하므로 Paint 때 받아 둔 참조를 지운다.)
**교훈:** 렌더러마다 값만 다르면 **MaterialPropertyBlock**. 머티리얼 복제 없이 렌더러별 값을 준다. 참고: URP/HDRP 의 SRP Batcher 는 MaterialPropertyBlock 을 쓰면 그 렌더러를 배칭에서 뺀다. SRP 프로젝트라면 머티리얼 변형을 미리 만들어 두거나 GPU 인스턴싱/셰이더 속성(`[PerRendererData]`, `UNITY_INSTANCING_BUFFER`)을 쓴다.

## Lab 18 — 늦게 도착한 비동기 로드 결과가 재사용된 칸을 덮어씀
```
틀린 결과:     칸의 아이템 potion, 아이콘 icon_sword
멈춘 위치:     람다 texture => Icon = texture → 두 번 멈춤: 같은 프레임에 potion(캐시, 즉시), 다음 프레임 이후 sword(LoadAsync 완료)
기대 → 실제:   요청 순서 sword → potion, 도착 순서 potion → sword
진짜 원인:     콜백은 "도착한 때의 칸"에 아이콘을 넣는다. 그 사이 칸이 물약으로 재사용됐는데 확인하지 않았다.
```
**수정:**
```csharp
public void Show(string itemId)
{
    ItemId = itemId;
    Icon = null;
    Loader.Request(itemId, texture =>
    {
        if (ItemId == itemId)   // 아직도 그 아이템을 보여 주는 칸인가?
        {
            Icon = texture;
        }
    });
}
```
**교훈:** 비동기 결과는 "요청한 때"가 아니라 "도착한 때"의 세상에 떨어진다. 도착했을 때 **아직 유효한지**(같은 아이템인가, 오브젝트가 살아 있나) 확인한다. `ResourceRequest.completed` 와 Addressables 의 `Completed` 는 이미 로드된 에셋이어도 같은 프레임에 불리지 않을 수 있다. 캐시 적중(즉시)과 미스(나중)가 섞이면 순서가 뒤집힌다.

## Lab 19 — Linear 색 공간에서 색을 SetVector 로 넘김
```
틀린 결과:     깃발 픽셀 (255, 188, 137), 기대 (255, 128, 64)
기록:          0x80/255 = 0.502 → 188 = sRGB 인코딩(0.502) ≈ 0.735. 0x40 → 137 도 같은 곡선. 0x00, 0xFF 는 곡선의 양 끝이라 그대로
도구:          Frame Debugger → TeamBanner 드로 콜 → _Color 값 (1, 0.502, 0.251): sRGB 값이 선형 값 자리에 그대로 들어감
진짜 원인:     Color Space = Linear. 셰이더는 선형 값을 기대하고, 화면에 쓸 때 sRGB 로 인코딩한다.
              SetVector 는 숫자를 그대로 넘겨서 sRGB 색(기획서 hex)이 변환 없이 "선형 값"으로 쓰였다 → 한 번 더 밝아짐.
```
**수정:**
```csharp
block.SetColor(ColorId, teamColor);   // Linear 색 공간이면 sRGB → 선형 변환을 해 준다
// 또는 block.SetVector(ColorId, teamColor.linear);
```
**교훈:** 사람이 고른 색(색상 선택기, hex, 텍스처의 색)은 sRGB, 셰이더 계산은 선형. 변환은 `SetColor`/`Color.linear`/텍스처의 **sRGB (Color Texture)** 체크가 해 준다. 노멀 맵, 마스크, 데이터 텍스처는 sRGB 를 **꺼야** 한다(켜면 반대로 값이 어두워진다). "0 과 255 는 맞는데 중간만 틀리다"면 감마 곡선을 의심하자.

---

# 5부: 애니메이션

## Lab 20 — Animator 파라미터 이름의 대소문자
```
틀린 결과:     Attack 상태에 들어가지 않음
Console:        ⚠ Parameter 'Hash 1203776827' does not exist.
Animator 창:   Parameters 탭의 이름은 "Attack". 코드는 Animator.StringToHash("attack")
진짜 원인:     파라미터 이름은 대소문자를 구분한다. 틀리면 경고만 찍고 아무 일도 하지 않는다.
```
**수정:**
```csharp
static readonly int AttackId = Animator.StringToHash("Attack");
```
**교훈:** 문자열 파라미터는 컴파일러가 검사해 주지 않는다. 이름은 한 곳에 상수로 두고, 시작할 때 `animator.parameters` 로 필요한 이름이 있는지 검사하는 습관을 들이자. 경고 필터를 꺼 두면 이 버그는 몇 주씩 숨는다.

## Lab 21 — Culling Mode = Cull Completely 라 화면 밖에서 이벤트가 사라짐
```
틀린 결과:     2.1초 동안 발소리 0번, Renderer.isVisible == false
도구:          Inspector 의 Animator → Culling Mode: Cull Completely. Animator 창에서 Walk 진행 막대가 멈춰 있다.
              Scene 뷰를 경비병 쪽으로 돌리면 진행 막대가 움직이고 Footstep 중단점이 걸린다.
진짜 원인:     Cull Completely 는 렌더러가 하나도 안 보이면 애니메이션을 통째로 멈춘다. 애니메이션 이벤트도 불리지 않는다.
```
**수정:**
```csharp
GetComponent<Animator>().cullingMode = AnimatorCullingMode.CullUpdateTransforms;   // 또는 AlwaysAnimate
```
**교훈:** 화면 밖에서도 게임 로직이 애니메이션(이벤트, 루트 모션)에 기대는 캐릭터에 Cull Completely 를 쓰면 안 된다. Cull Update Transforms 는 상태 머신과 이벤트를 유지하면서 뼈 쓰기·IK 를 생략한다. 에디터에서는 **Scene 뷰 카메라도 "보는 것"** 이라서, 에디터에서는 되고 빌드에서는 안 되는(또는 반대) 버그로 신고되기 쉽다.

## Lab 22 — 트랜지션이 이벤트보다 먼저 끝남
```
틀린 결과:     3번 휘둘러 Hit 0번
도구:          Animation 창: Swordsman_Swing 길이 1초, Hit 이벤트 0.9초
              Animator 창: Swing → Idle 트랜지션 Has Exit Time ✔, Exit Time 0.7, Transition Duration 0.1 (Fixed Duration 끔 = 클립 길이의 10%)
처음 어긋난 곳: Swing 은 0.7 에 빠지기 시작해 0.8 에 완전히 끝난다. 0.9 의 이벤트는 재생되지 않는 구간
진짜 원인:     애니메이션 에셋. 이벤트가 트랜지션 뒤에 있다.
```
**수정 (택1):** Animator 창에서 Swing → Idle 트랜지션의 **Exit Time 을 0.95** 로(이벤트 뒤에서 넘어가기 시작). 또는 Animation 창에서 **Hit 이벤트를 0.5초** 무렵(칼날이 실제로 닿는 지점)으로 옮기기.
**교훈:** 트랜지션 도중에는 두 상태의 클립이 같이 재생되며 양쪽 이벤트가 불리지만, 트랜지션이 **끝난 뒤** 원래 클립은 더 진행되지 않는다. 판정용 이벤트는 트랜지션 시작 전에 둔다. 피격·회피 같은 인터럽트로 공격이 끊길 때 판정이 빠지는 것도 같은 원리이고, 그건 의도된 동작일 수도 있다(기획과 정하자).

## Lab 23 — 애니메이션 결과를 Update 에서 읽음
```
틀린 결과:     거의 모든 프레임에서 손과 횃불이 어긋남
도구:          Update 와 LateUpdate 의 추적점 → 같은 프레임인데 손의 x 가 다르다
              Profiler Timeline: Update.ScriptRunBehaviourUpdate → PreLateUpdate.DirectorUpdateAnimationBegin/End → PreLateUpdate.ScriptRunBehaviourLateUpdate
진짜 원인:     Animator 는 Update 뒤, LateUpdate 앞에서 뼈(Transform)를 쓴다. Update 에서 읽은 손 위치는 지난 프레임 것이다.
```
**수정:**
```csharp
void LateUpdate()
{
    transform.SetPositionAndRotation(Hand.position, Hand.rotation);
}
```
(가장 간단하게는 횃불을 손의 자식으로: `transform.SetParent(Hand, false)`.)
**교훈:** 애니메이션이 움직인 뼈를 따라가는 모든 것(무기 붙이기, 카메라, 조준선, IK 보정, 이펙트 위치)은 LateUpdate. 한 프레임 늦음은 화면에서 "흔들림/떨림"으로 보이고, 판정에 쓰이면 진짜 버그가 된다.

# 6부: 잘못된 참조와 네이티브 메모리

## Lab 24 — NativeArray 를 Dispose 하지 않음
```
틀린 결과:     목표를 5번 바꾼 뒤 살아 있는 버퍼 5개(기대 1), 순찰병을 없앤 뒤에도 5개(기대 0). 예외는 없다
도구:          플레이를 멈추면 Console: "Leak Detected : Persistent allocates 5 individual allocations"
              Preferences → Jobs → Leak Detection Level → Enabled With Stack Trace 로 다시 돌리면 할당한 곳이 PathCache.Rebuild 로 찍힌다
처음 어긋난 곳: 두 번째 Rebuild. Points = new ... 가 첫 버퍼의 주소를 덮어써서 아무도 그 버퍼를 해제할 수 없게 됐다
진짜 원인:     NativeArray 는 GC 가 치우지 않는다. 덮어쓰기 전에도, 컴포넌트가 사라질 때도 Dispose 가 없다
```
**수정:**
```csharp
public void Rebuild(Vector3 from, Vector3 to, int segments)
{
    if (Points.IsCreated)
    {
        Points.Dispose();
    }
    Points = new NativeArray<Vector3>(segments + 1, Allocator.Persistent);
    ...
}

void OnDestroy()
{
    if (Points.IsCreated)
    {
        Points.Dispose();
    }
}
```
**교훈:** `new NativeArray(..., Allocator.Persistent)` 는 `new` 보다 `malloc` 에 가깝다. 만든 곳과 짝이 되는 `Dispose` 를 같이 적는다(필드라면 `OnDestroy`/`OnDisable`). 누수는 예외 없이 메모리만 늘기 때문에 Leak Detection 메시지와 Memory Profiler 로 찾는다. `IsCreated` 는 "이 변수가" 버퍼를 들고 있는지일 뿐이다. 구조체 복사본은 원본이 Dispose 된 뒤에도 `IsCreated == true` 이고, 접근하면 안전 시스템이 `ObjectDisposedException` 을 던진다(랩은 이것으로 살아 있는 버퍼를 센다).

## Lab 25 — 앞 잡만 Complete 하고 뒤 잡의 출력을 읽음
```
멈춘 곳:        Watchtower.Scan 의 count[0]
예외:          InvalidOperationException: The previously scheduled job CountInRangeJob writes to the NativeArray CountInRangeJob.Count.
              You must call JobHandle.Complete() on the job CountInRangeJob, before you can read from the NativeArray safely.
이상한 값:      measured.Complete() 는 불렀다. 그런데 메시지의 잡은 MeasureDistanceJob 이 아니라 CountInRangeJob
진짜 원인:     tally 는 measured 에 의존한다(measured 가 끝나야 tally 가 시작). 반대 방향은 아니다. measured 를 기다려도 tally 는 아직 예약 상태일 수 있다
```
**수정:**
```csharp
running = tally.Schedule(measured);
running.Complete();   // 사슬의 끝을 기다린다 (앞 잡도 함께 끝나 있다)
EnemiesInRange = count[0];
```
**교훈:** 메인 스레드가 버퍼를 읽거나 쓰거나 Dispose 하려면, 그 버퍼를 쓰는 **모든** 잡의 핸들이 Complete 되어 있어야 한다. 실제로 잡이 끝났는지와 상관없이 안전 시스템이 막는다(그래서 타이밍에 따라 가끔 터지는 버그가 아니라 항상 터지는 버그가 된다. 고마운 일이다). 의존 사슬은 끝 핸들 하나를 Complete 하면 되고, 여러 갈래는 `JobHandle.CombineDependencies` 로 묶는다. 메시지는 **잡 이름.필드 이름**까지 알려 주니 끝까지 읽자.

## Lab 26 — Allocator.Temp 버퍼를 다음 프레임에 읽음
```
멈춘 곳:        SpawnDirector.Update 의 plan[next] (StartWave 다음 프레임, next == 0)
예외:          ObjectDisposedException: The NativeArray has been deallocated, it is not allowed to access it
이상한 값:      Dispose 를 부른 곳이 없는데 "deallocated"
진짜 원인:     Allocator.Temp 는 그 프레임 안에서만 유효하다. 프레임이 끝나면 Unity 가 Temp 메모리를 통째로 되감는다. 필드에 넣어 다음 프레임에 읽으면 이미 없다
```
**수정:**
```csharp
public void StartWave(int[] perFrame)
{
    if (plan.IsCreated)
    {
        plan.Dispose();
    }
    plan = new NativeArray<int>(perFrame.Length, Allocator.Persistent);
    plan.CopyFrom(perFrame);
    ...
}

void OnDestroy()
{
    if (plan.IsCreated)
    {
        plan.Dispose();
    }
}
```
(잡에 넘기지 않는 데이터라면 그냥 `int[]` 로 바꾸는 것도 맞는 답이다.)
**교훈:** Allocator 는 수명 약속이다. Temp = 이 프레임, 함수 안 지역 변수용. TempJob = 잡에 넘겨 4프레임 안에 Dispose. Persistent = 직접 Dispose 할 때까지. 필드에 넣는 순간 Temp 는 틀린 선택이다. 에디터에서는 안전 시스템이 바로 예외를 던져 주지만, 릴리스 빌드에서는 안전 검사가 없으니 다른 코드가 덮어쓴 메모리를 읽어 **엉뚱한 마릿수**가 나올 수 있다.

## Lab 27 — Domain Reload 를 끈 Play 에서 정적 상태가 남음
```
틀린 결과:     2판 첫 웨이브 4 (기대 1), 레이더의 적 4 (기대 2), radar.Nearest() 에서 MissingReferenceException
도구:          SimulateEnterPlayMode() 다음 줄에 중단점 → 조사식 Enemy.Alive: Count 4 인데 앞의 2개는 "null"(파괴된 1판의 적)
              WaveDirector.Wave == 3: 1판의 값 그대로
진짜 원인:     Domain Reload 를 끄면 Play 할 때 C# 을 다시 올리지 않으므로 정적 필드의 초기값(= 0, = new List)이 다시 적용되지 않는다.
              1판이 끝날 때 살아 있던 적은 Die() 를 거치지 않고 씬과 함께 파괴되어 목록에 남았다
```
**수정:**
```csharp
public class Enemy : MonoBehaviour
{
    public static readonly List<Enemy> Alive = new List<Enemy>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Alive.Clear();
    }
    ...
}

public static class WaveDirector
{
    public static int Wave;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Wave = 0;
    }
    ...
}
```
(덤으로 `void OnDestroy() { Alive.Remove(this); }` 를 두면 씬 전환으로 사라진 적도 빠진다. 하지만 그것만으로는 Wave 가 이어지는 버그가 남는다.)
**교훈:** *Reload Scene only*(Domain Reload 끔)는 Play 를 몇 초씩 빠르게 해 주지만, 그 대가로 **정적 필드, 정적 이벤트, 싱글턴 Instance** 를 직접 되돌려야 한다. Unity 문서의 권장은 `SubsystemRegistration` 시점의 `[RuntimeInitializeOnLoadMethod]`. 정적 이벤트(`static event Action`)도 같다: 구독자가 지난 Play 의 파괴된 오브젝트라서 1부의 MissingReferenceException 이 엉뚱한 곳에서 난다. 빌드한 게임에서는 항상 새로 시작하므로 "에디터에서만 나는 버그" 로 신고되기 쉽다.

