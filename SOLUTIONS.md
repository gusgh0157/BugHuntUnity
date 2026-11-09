# 해답 (스포일러!)

**한국어** | [日本語](SOLUTIONS.ja.md)

수치(프레임 번호, ms)는 PC 와 실행 방식에 따라 조금씩 다릅니다. 판정에 쓰는 값(기대/실제)은 같습니다.
채점기의 숨은 테스트 케이스는 입력(적의 수, 좌표, 피해량, 서버 가동 시간 등)만 바꾸므로 해답은 같습니다. 원인을 고쳤다면 숨은 케이스도 그대로 통과합니다.

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

## Lab 12 — 프레임당 이동량에 deltaTime 을 곱하지 않음
```
틀린 결과:     2초 동안 30fps 6m, 120fps 24m (기대 둘 다 12m)
도구:          Update 추적점 "{Time.frameCount} {transform.position.z}" → 한 번에 0.1m 씩, 1초에 Update 30번/120번
진짜 원인:     Speed 는 "프레임당 0.1m" 다. 1초에 가는 거리 = 0.1 × fps. 60fps 에서만 6m/s 다
```
**수정:**
```csharp
public float Speed = 6f;   // 1초에 6m
transform.position += Vector3.forward * Speed * Time.deltaTime;
```
**교훈:** Update 에서 바꾸는 양은 "초당 값 × `Time.deltaTime`" 이어야 한다. 단위(MetersPerSecond)를 이름이나 주석에 남기면 프레임당 값이 섞여 들어올 때 눈에 띈다. 고정 간격이 필요한 계산은 FixedUpdate 로 보낸다.

## Lab 13 — 매 프레임 Lerp(a, b, 0.1f)
```
틀린 결과:     0.5초 뒤 남은 거리 30fps 2.06m, 120fps 0.02m (기대 둘 다 0.42m)
도구:          LateUpdate 추적점 → 남은 거리가 프레임마다 0.9 배. 30fps 0.5초 = 15프레임 → 0.9^15, 120fps = 60프레임 → 0.9^60
진짜 원인:     비율을 고정한 Lerp 는 "프레임 수" 에 대한 거듭제곱이다. deltaTime 을 그냥 곱해도(0.1f * dt * 60) 1차 근사일 뿐 맞지 않는다
```
**수정:**
```csharp
public float Sharpness = 6.32f;   // = -ln(1 - 0.1) * 60 : 60fps 에서 0.1 과 같은 느낌
transform.position = Vector3.Lerp(transform.position, Target, 1f - Mathf.Exp(-Sharpness * Time.deltaTime));
```
**교훈:** "매 프레임 남은 것의 몇 %" 는 지수 감쇠다. 한 프레임 비율을 `1 - e^(-k·dt)` 로 두면 프레임을 어떻게 나누든 1초 뒤 결과가 같다(`e^(-k·a) · e^(-k·b) = e^(-k·(a+b))`). `Vector3.SmoothDamp`, `Mathf.SmoothDamp` 도 프레임과 무관하다(곡선 모양은 다르다).

## Lab 14 — Update 에서 Time.fixedDeltaTime 을 곱함
```
틀린 결과:     1.5초 동안 30fps 18, 120fps 72 (기대 둘 다 30)
도구:          Update 조사식: Time.fixedDeltaTime 은 fps 와 상관없이 0.02, Time.deltaTime 은 0.0333 / 0.0083
진짜 원인:     fixedDeltaTime 은 FixedUpdate 간격 설정값(상수)이다. Update 에서 곱하면 Update 횟수 × 0.02 가 된다. 50fps 에서만 맞다
```
**수정:**
```csharp
Value += RegenPerSecond * Time.deltaTime;
```
**교훈:** 어디서든 `Time.deltaTime` 을 쓰면 된다. FixedUpdate 안에서 읽으면 Unity 가 알아서 `fixedDeltaTime` 을 돌려준다. `fixedDeltaTime` 을 직접 쓸 곳은 FixedUpdate 간격 자체를 바꿀 때뿐이다.

## Lab 15 — timeScale 0 에서 deltaTime 과 WaitForSeconds 를 씀
```
틀린 결과:     멈춘 동안 메뉴가 들어온 정도 0 (기대 1), 카운트다운 뒤 timeScale 0 (기대 1)
도구:          PauseMenu.Update 중단점 → 멈춘 동안에도 불린다. Time.deltaTime == 0, Time.unscaledDeltaTime == 0.016
              Countdown 의 yield 다음 줄에 중단점 → 영원히 오지 않는다
진짜 원인:     timeScale = 0 은 Update 를 멈추지 않고 게임 시간만 멈춘다. deltaTime 이 0 이라 메뉴가 안 움직이고, WaitForSeconds 는 게임 시간으로 기다리므로 끝나지 않는다
```
**수정:**
```csharp
Shown = Mathf.MoveTowards(Shown, target, Time.unscaledDeltaTime / SlideSeconds);
...
yield return new WaitForSecondsRealtime(1f);
```
**교훈:** 일시정지 중에도 움직여야 하는 것(메뉴, 카운트다운, 일시정지 화면의 애니메이션·파티클)은 진짜 시간을 쓴다: `unscaledDeltaTime`, `WaitForSecondsRealtime`, Animator 의 Update Mode = Unscaled Time, 파티클의 Use Unscaled Time. 반대로 게임 안의 것은 일부러 `deltaTime` 을 써서 함께 멈추게 한다. "멈춘 동안 무엇이 움직여야 하나" 를 먼저 정하자.

---

# 3부: 트랜스폼과 공간

## Lab 16 — 월드 좌표를 localPosition 에 넣음
```
틀린 결과:     탄 직후 상인과 자리 사이 거리 7.211m (수레가 간 뒤에도 7.211m)
멈춘 위치:     Cart.Board 끝 → 조사식 Seat.position (6, 1, 3.5), rider.localPosition (6, 1, 3.5), rider.position (12, 1, 7.5)
처음 어긋난 곳: rider.position - Seat.position = (6, 0, 4) = 수레의 위치. 월드 좌표에 부모 위치가 한 번 더 더해졌다
진짜 원인:     Seat.position 은 월드 좌표인데 부모(수레) 기준인 localPosition 에 넣었다. 수레가 원점에 있을 때만 둘이 같다
```
**수정:**
```csharp
public void Board(Transform rider)
{
    rider.SetParent(transform);
    rider.position = Seat.position;   // 월드 = 월드
}
```
**교훈:** `position`/`rotation`/`lossyScale` 은 월드, `localPosition`/`localRotation`/`localScale` 은 부모 기준이다. Inspector 의 Transform 이 보여 주는 것은 **로컬** 값이다. 부모가 원점·회전 없음·크기 1 이면 둘이 같아서 시험할 때 숨으므로, 부모를 옮기고 돌려 놓고 시험한다. 자리 자체를 부모로 삼아(`SetParent(Seat, false)` + `localPosition = Vector3.zero`) 좌표계를 하나로 줄여도 된다.

## Lab 17 — SetParent 의 worldPositionStays 를 반대로 씀
```
틀린 결과:     주운 직후 검과 손 사이 3.496m, 방향 차이 96.7도. 내려놓은 검은 손이 있던 자리에서 4.258m, 보이는 크기 0.5 (기대 2)
멈춘 위치:     Hand.PickUp 의 SetParent 다음 → item.position 은 그대로(바닥), item.localPosition 만 바뀜
              Hand.Drop 의 SetParent 다음 → item.localPosition 은 그대로, item.position 이 원점 근처로, lossyScale 1 → 0.5
진짜 원인:     SetParent(p) 는 worldPositionStays = true: 월드 값을 유지하도록 로컬 값을 다시 계산한다(검이 바닥에 남음)
              SetParent(null, false) 는 로컬 값을 유지: 손 기준 로컬 값이 월드 값이 되고, 부모(기사 2배 × 손)의 크기가 빠진다
```
**수정:**
```csharp
public void PickUp(Transform item)
{
    item.SetParent(transform, false);   // 손의 로컬 자리에 끼운다
    item.localPosition = Vector3.zero;
    item.localRotation = Quaternion.identity;
    Held = item;
}

public void Drop()
{
    Held.SetParent(null);               // 보이는 위치·회전·크기 그대로 둔다
    Held = null;
}
```
**교훈:** "붙일 때는 로컬 자리에 끼우고(`false` + 로컬 값 지정), 뗄 때는 보이는 그대로 둔다(`true`)". 크기가 1 이 아닌 부모 밑으로 들어가거나 나올 때 `worldPositionStays = true` 는 보이는 크기를 지키려고 localScale 을 바꾼다. 씬 루트로 뗀 오브젝트는 부모와 함께 파괴되지 않으므로 직접 정리해야 한다(이 랩의 `OnDestroy`).

## Lab 18 — eulerAngles 를 읽어서 더하고 다시 씀
```
틀린 결과:     120 프레임(180도) 뒤 날개 방향 차이 90도. 날개가 90도 근처에서 떤다
멈춘 위치:     Windmill.Update 에 조건부 중단점 transform.eulerAngles.x > 85
              → 88.5 에 1.5 를 더해 90 을 넣은 다음 프레임, 91.5 를 넣었더니 읽은 값은 (88.5, 180, 180)
진짜 원인:     eulerAngles 는 쿼터니언에서 매번 다시 계산한 값이고, X 는 90 을 넘지 않는 값(0~90 또는 270~360)으로 돌아온다. X 가 90 을 넘으면 같은 회전 (180-x, y+180, z+180) 으로 읽혀
              다시 더한 값이 90 쪽으로 돌아간다 (X = ±90 은 Y 와 Z 가 같은 축이 되는 짐벌 락 자리)
```
**수정:**
```csharp
void Update()
{
    transform.Rotate(DegreesPerSecond * Time.deltaTime, 0f, 0f);   // rotation *= Quaternion.Euler(...)
}
```
**교훈:** 회전을 누적할 때는 오일러 값을 읽어 고쳐 쓰지 말고 쿼터니언을 곱한다(`Rotate`, `rotation * Quaternion.AngleAxis(...)`). 각도 하나만 움직이는 연출이면 각도를 내 변수로 들고 매번 `baseRotation * Quaternion.Euler(angle, 0, 0)` 으로 만든다. Inspector 의 Rotation 칸은 에디터가 따로 기억하는 값이라 스크립트의 `eulerAngles` 와 다를 수 있다.

## Lab 19 — 쿼터니언 곱의 순서를 거꾸로 씀
```
틀린 결과:     표적 1(축 하나만 다름)은 0도, 표적 2(옆쪽 높은 곳)는 9.131도 어긋남
멈춘 위치:     Cannon.AimAt 끝 → yaw, pitch 는 맞다. 조사식 Quaternion.Euler(0, yaw, 0) * Quaternion.Euler(pitch, 0, 0) * Vector3.forward 는 direction 과 같다
진짜 원인:     A * B 는 B 를 먼저 적용한다. Euler(pitch) * Euler(yaw) 는 먼저 좌우로 돌린 뒤 월드 X 축으로 들어서, 옆을 보는 포신이 위가 아니라 옆으로 기운다
              pitch 나 yaw 가 0 이면 순서가 상관없어 정면·같은 높이 표적에서는 숨는다
```
**수정:**
```csharp
transform.rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(pitch, 0f, 0f);
// 같은 결과: Quaternion.Euler(pitch, yaw, 0f)  (Unity 의 Euler 는 Z → X → Y 순서 = Ry * Rx * Rz)
// 각도가 필요 없으면: Quaternion.LookRotation(direction)
```
**교훈:** 쿼터니언 곱은 교환되지 않는다. `부모 쪽(월드) 회전 * 자식 쪽(로컬) 회전` 으로 읽으면 순서를 맞추기 쉽다. `Quaternion.Euler(x, y, z)` 는 Z, X, Y 순서로 돌린 결과라, 다른 엔진·툴(예: XYZ 순서)에서 가져온 각도는 그대로 넣으면 안 된다.

## Lab 20 — Vector3.forward 로 앞으로 움직임
```
틀린 결과:     화살이 날아간 방향과 적 방향 차이 123.69도 (적이 남동쪽인데 화살은 북쪽, +Z)
멈춘 위치:     Arrow.Update → 조사식 Vector3.forward (0, 0, 1), transform.forward (0.83, 0, -0.55)
진짜 원인:     Vector3.forward 는 상수 월드 +Z. 화살의 회전은 맞게 들어갔지만 이동에 쓰지 않았다
```
**수정:**
```csharp
transform.position += transform.forward * Speed * Time.deltaTime;
// 또는 transform.Translate(0f, 0f, Speed * Time.deltaTime);   // 기본 Space.Self
```
**교훈:** `Vector3.forward/right/up` 은 월드 축, `transform.forward/right/up` 은 그 오브젝트의 축이다. `Translate` 는 기본이 자기 축(Space.Self), 월드로 움직이려면 `Space.World`. Scene 뷰의 Local / Global 토글을 Local 로 두면 기즈모가 오브젝트 축을 보여 준다. 적을 늘 +Z 쪽에 두고 시험하면 둘이 같아서 숨는다.

## Lab 21 — 방향에 TransformPoint, 위치에 InverseTransformDirection
```
틀린 결과:     탐조등 방향 차이 81.994도. 드론 왼쪽 앞 침입자를 오른쪽으로 판정 (숨은 케이스에서는 오른쪽 침입자를 왼쪽으로)
멈춘 위치:     Drone.BeamDirection → 조사식 transform.TransformPoint(LocalBeam) 은 드론 위치 근처의 점, TransformDirection(LocalBeam) 이 원하는 방향
              Drone.IsOnLeft → InverseTransformDirection(worldPoint).x 에는 드론 위치를 돌린 값이 섞여 있다
진짜 원인:     Point 는 이동(위치)까지 적용하고 Direction 은 회전만 적용한다. 방향과 위치에 반대 함수를 썼다. 드론이 원점에 있으면 둘이 같다
```
**수정:**
```csharp
public Vector3 BeamDirection()
{
    return transform.TransformDirection(LocalBeam);
}

public bool IsOnLeft(Vector3 worldPoint)
{
    return transform.InverseTransformPoint(worldPoint).x < 0f;
}
```
**교훈:** 위치는 `TransformPoint`/`InverseTransformPoint`(회전·크기·이동), 방향은 `TransformDirection`(회전만, 길이 유지), 크기가 있는 벡터(오프셋, 속도)는 `TransformVector`(회전·크기, 이동 없음). 이름에 `worldPoint`, `localDirection` 처럼 무엇인지 남겨 두면 짝이 안 맞는 호출이 보인다.

## Lab 22 — 크기가 고르지 않은 부모 밑에 로컬 값으로 놓음
```
틀린 결과:     함정이 기대 위치에서 33.003m, 보이는 크기와 1m 정육면체의 차이 11.196 (lossyScale (12, 0.4, 3)), 바닥이 다리 윗면보다 0.12m 아래
멈춘 위치:     Bridge.PlaceTrap 의 return → trap.localPosition (3, 0.7, 0), trap.position 은 다리 중심에서 x 로 36m, trap.lossyScale (12, 0.4, 3)
진짜 원인:     자식의 localPosition 과 localScale 은 부모 크기 (12, 0.4, 3) 를 거쳐 월드가 된다. 미터 단위 값을 그대로 로컬에 넣었다
```
**수정:**
```csharp
trap.SetParent(transform, false);
trap.position = transform.position + transform.rotation * new Vector3(offset, transform.localScale.y * 0.5f + 0.5f, 0f);
trap.localScale = new Vector3(1f / transform.lossyScale.x, 1f / transform.lossyScale.y, 1f / transform.lossyScale.z);
```
**교훈:** 늘린 오브젝트(크기 ≠ 1)의 자식은 위치·크기가 모두 부모 크기에 곱해진다. 크기가 고르지 않은 부모 밑에서 자식을 돌리면 모양이 찌그러지고(그래서 `lossyScale` 은 근삿값) 콜라이더도 이상해진다. 근본적으로는 루트(크기 1) 밑에 **보이는 모양만 맡는 자식**을 두고 그 자식만 늘린다.

## Lab 23 — 피벗이 메시 가운데인 모델
```
틀린 결과:     연 뒤 경첩 쪽 끝이 경첩에서 2.121m (= 1.5 × √2). 석상 발이 바닥보다 1m 아래
멈춘 위치:     Gate.Open → Rotate 는 transform.position(문짝 가운데)을 축으로 돈다. 조사식 gateMesh.bounds: center (0, 0, 0), min (-1.5, -2, -0.15)
              Plaza.PlaceOnFloor → statueMesh.bounds.min.y = -1
도구:          Scene 뷰 Pivot / Center 토글: 이 모델은 두 위치가 같다(피벗이 가운데)
진짜 원인:     Transform 의 회전·위치는 피벗(메시 원점) 기준. 코드는 피벗이 경첩·발에 있다고 가정했지만 모델은 가운데에 피벗이 있다
```
**수정:**
```csharp
public void Open(float degrees)
{
    transform.RotateAround(Hinge, Vector3.up, degrees);
}

public static void PlaceOnFloor(Transform statue, Vector3 floorPoint)
{
    float bottom = statue.GetComponent<MeshFilter>().sharedMesh.bounds.min.y * statue.lossyScale.y;
    statue.position = floorPoint - Vector3.up * bottom;
}
```
**교훈:** 모델의 피벗이 어디인지는 코드가 아니라 에셋이 정한다. 문·레버·석상처럼 "어느 점을 기준으로" 다루는 모델은 모델링 툴에서 원점을 맞추거나, 빈 부모(경첩 위치, 발 위치)를 만들어 모델을 그 자식으로 둔다. 그래픽 장치가 없을 때나 꺼진 렌더러의 `Renderer.bounds` 는 비어 있을 수 있으니, 판정에는 메시 bounds 를 쓴다.

---

# 4부: 컨테이너

## Lab 24 — 상수 길이로 배열을 돌고, 바깥 번호를 확인하지 않음
```
증상:          단축 바를 만드는 순간 IndexOutOfRangeException (Hotbar.Build)
도구:          예외 설정에서 IndexOutOfRangeException 을 throw 될 때 중단 → 조사식 i == 3, Slots.Length == 3
처음 어긋난 곳: 반복 횟수는 SlotCount(4), 데이터는 인스펙터에서 3칸으로 줄어든 Slots
진짜 원인:     ① 반복을 데이터 길이가 아니라 상수로 돌았다 ② Select(key) 는 바깥에서 온 key 를 확인 없이 인덱스로 썼다(빈 단축키 4 → ArgumentOutOfRangeException)
```
**수정:**
```csharp
for (int i = 0; i < Slots.Length; ++i) { ... }

public string Select(int key)
{
    int index = key - 1;
    return index >= 0 && index < Labels.Count ? Labels[index] : null;
}
```
**교훈:** 인스펙터에 보이는 배열의 크기는 코드가 정하지 않는다. 반복은 `Length`/`Count` 로, 입력·네트워크·세이브 파일에서 온 번호는 범위를 확인한 뒤 쓴다. 크기가 규칙이라면 `OnValidate` 에서 강제한다. 배열은 `IndexOutOfRangeException`, `List` 는 `ArgumentOutOfRangeException` 이라 메시지만 보고도 어느 쪽인지 안다.

## Lab 25 — foreach 도중 다른 스크립트가 목록에서 자신을 지움
```
증상:          공습에 HP 10 인 적이 쓰러지는 순간 InvalidOperationException: Collection was modified
도구:          Enemy.Die 의 Members.Remove 에 중단점 → 호출 스택: Die ← TakeDamage ← Squad.Airstrike(foreach)
진짜 원인:     Squad 는 Members 를 foreach 로 돌고, 그 안에서 불린 Enemy.Die 가 같은 Members 를 고쳤다. 다음 MoveNext 가 버전이 바뀐 것을 보고 던진다
```
**수정 (택1):**
```csharp
foreach (Enemy enemy in Members.ToArray()) { enemy.TakeDamage(damage); }        // 복사본을 돈다
for (int i = Members.Count - 1; i >= 0; --i) { Members[i].TakeDamage(damage); }  // 뒤에서부터 돈다
```
**교훈:** 반복하는 동안 불리는 코드(이벤트, 콜백, 다른 컴포넌트의 함수)가 같은 목록을 고칠 수 있으면 복사본이나 역순으로 돈다. 앞에서부터 인덱스로 돌면서 지우면 예외 없이 **하나씩 건너뛰는** 더 조용한 버그가 된다. `Destroy` 가 부르는 `OnDestroy` 는 프레임 끝이라 이 예외 대신 랩 29 같은 "파괴된 원소가 남는" 문제가 된다.

## Lab 26 — Unity 직렬화가 Dictionary 와 List<List<T>> 를 버림
```
틀린 결과:     복제본과 불러온 상점의 potion 가격 -1, 진열대 0개. 이름과 금화는 정상
도구:          랩 출력 "저장된 JSON: {"ShopName":"대장간","Gold":500}" — Prices, Shelves 가 없다
              Inspector 에도 두 필드가 보이지 않는다
진짜 원인:     Instantiate 와 JsonUtility 는 Unity 직렬화로 값을 옮긴다. Dictionary 와 중첩 List 는 직렬화 대상이 아니라서 경고 없이 빠진다
```
**수정:**
```csharp
[System.Serializable]
public class Shelf { public List<string> Items = new List<string>(); }

public class Shop : MonoBehaviour, ISerializationCallbackReceiver
{
    public Dictionary<string, int> Prices = new Dictionary<string, int>();
    public List<Shelf> Shelves = new List<Shelf>();
    [SerializeField] List<string> priceKeys = new List<string>();
    [SerializeField] List<int> priceValues = new List<int>();

    public void OnBeforeSerialize()
    {
        priceKeys.Clear(); priceValues.Clear();
        foreach (var kv in Prices) { priceKeys.Add(kv.Key); priceValues.Add(kv.Value); }
    }

    public void OnAfterDeserialize()
    {
        Prices = new Dictionary<string, int>();
        for (int i = 0; i < priceKeys.Count; ++i) { Prices[priceKeys[i]] = priceValues[i]; }
    }

    public void AddShelf(params string[] items) { Shelves.Add(new Shelf { Items = new List<string>(items) }); }
    public string ItemAt(int shelf, int slot) { return Shelves[shelf].Items[slot]; }
}
```
**교훈:** 인스펙터에 안 보이는 필드는 프리팹, 씬 저장, Instantiate, Undo, `JsonUtility` 에서도 빠진다. `Dictionary`, `T[,]`, `T[][]`, `List<List<T>>` 는 `[Serializable]` 래퍼나 `ISerializationCallbackReceiver` 로 직렬화 가능한 모양으로 바꾼다. 콜백은 직렬화 스레드에서 불릴 수 있으니 그 안에서 Unity API 를 부르지 않는다.

## Lab 27 — FindObjectsByType 의 순서를 게임 순서로 씀
```
틀린 결과:     세 맵 모두 틀린다(2,1,0, 1,3,0,2 처럼. 틀린 순서는 실행 환경마다 다르다). 랩은 놓는 순서를 어떤 단조 순서와도 안 맞게 골라 두었다.
               실제 게임에서는 "어떤 맵은 맞다"로 나타나는 버그다
도구:          LearnRoute 뒤 조사식 Route → 각 원소의 Order 와 name(놓은 순서 A, B, C)
진짜 원인:     FindObjectsSortMode.None 은 순서를 보장하지 않는다. 엔진 내부 순서가 그대로 나오고, 그 순서는 놓은 순서와도, 실행 환경과도 무관하게 바뀐다. 가끔 맞는 것이 가장 위험하다
```
**수정:**
```csharp
Route = FindObjectsByType<RoutePoint>(FindObjectsSortMode.None).OrderBy(p => p.Order).ToList();
```
**교훈:** `FindObjectsByType`, `GetComponents`, `GetComponentsInChildren`, `Dictionary` 열거, 이벤트 호출 순서는 게임 규칙의 순서가 아니다. 의미 있는 순서는 데이터(번호)로 갖고 직접 정렬하거나, 인스펙터 배열에 순서대로 넣어 씬 파일에 저장한다. 이 버그는 "에디터에선 되는데 빌드에선 순서가 다름" 으로 나타나기 쉽다.

## Lab 28 — 돌려쓰는 결과 버퍼를 그대로 반환
```
틀린 결과:     Alpha 명단 2명(기대 3), Alpha 명단에 Bravo 병사. alphaRoster == bravoRoster 가 true
도구:          조사식 참조 비교. alphaRoster 줄 뒤 Count 3 → bravoRoster 줄 뒤 Count 2 로 바뀐다
진짜 원인:     GetComponentsInChildren(List) 는 넘긴 List 를 비우고 채운다. SoldiersOf 는 static 버퍼 자체를 돌려주므로 두 호출이 같은 List 를 받는다
```
**수정:**
```csharp
unit.GetComponentsInChildren(results);
return new List<Soldier>(results);
// 또는: public static void SoldiersOf(GameObject unit, List<Soldier> into) { unit.GetComponentsInChildren(into); }
```
**교훈:** 할당을 줄이는 API(`GetComponents(List)`, `GetComponentsInChildren(List)`, `Physics.RaycastNonAlloc`, `OverlapSphereNonAlloc`)의 버퍼는 **다음 호출이 덮어쓴다**. 버퍼는 함수 안에서만 쓰고 밖으로 내보낼 때는 복사하거나, 호출하는 쪽이 버퍼를 갖게 한다.

## Lab 29 — 파괴된 오브젝트가 Dictionary 키로 남음
```
틀린 결과:     위협 목록 3명(기대 2). AttackTop 에서 MissingReferenceException
도구:          AttackTop 에 중단점 → 조사식 top == null 은 true, (object)top == null 은 false
진짜 원인:     Destroy 는 네이티브 오브젝트만 지운다. Dictionary 는 C# 객체를 키로 붙잡고 있어 항목과 점수가 그대로 남고, 점수 1위라 계속 골라진다
```
**수정:**
```csharp
void Prune()
{
    foreach (Enemy dead in Threat.Keys.Where(e => e == null).ToList()) { Threat.Remove(dead); }
}
public int ThreatCount { get { Prune(); return Threat.Count; } }
public void AttackTop() { Prune(); ... }
```
(또는 `Enemy.OnDestroy` 에서 이벤트를 쏘고 `Boss` 가 받아 `Threat.Remove(enemy)`)

**교훈:** `UnityEngine.Object` 를 담은 컨테이너(`Dictionary` 키, `HashSet`, `List`)는 `Destroy` 를 모른다. 꺼낼 때 `== null`(Unity 재정의)로 걸러 치우거나, 죽을 때 알려서 지운다. 치우지 않은 항목은 C# 객체를 붙잡아 작은 누수도 된다. 1부 랩 1·8 의 fake null 이 컨테이너 안에 숨은 모양이다.

## Lab 30 — 길이가 바뀐 NativeList 의 AsArray 를 캐시해 둠
```
증상:          출발점을 읽는 줄에서 ObjectDisposedException: NativeList ... has been deallocated
도구:          Step 에 중단점, 조사식 view[0] → 첫 Step(첫 Add) 을 지나자마자 읽을 수 없다. Capacity 4 를 넘기 전인데도
진짜 원인:     Awake 에서 만든 view 는 그 순간의 메모리를 가리킨다(길이도 1 로 굳어 있다). 메모리가 실제로 옮겨지는 것은 Capacity 를 넘는 Add 때지만,
               안전 검사는 길이를 바꾸는 모든 쓰기(Add 마다)에서 리스트의 보조(secondary) 버전을 올려 그 전에 만든 view 를 무효로 만든다. AddNoResize 만 예외
```
**수정:**
```csharp
public Vector3 StartPoint() { return Points[0]; }   // view 를 캐시하지 않는다. 필요하면 쓸 때마다 Points.AsArray()
```
**교훈:** `NativeList.AsArray()`, `List<T>` 에서 얻은 `Span`, `NativeArray.GetSubArray` 는 복사가 아니라 **지금 메모리의 모습**이다. 길이를 바꾸는 쓰기를 건너서 들고 있지 않는다(Capacity 를 넉넉히 잡아도 소용없다). 잡에 넘길 때는 스케줄 직전에 만들거나 `AsDeferredJobArray()` 를 쓴다. 에디터의 안전 검사가 예외로 알려 주지만 릴리스 빌드에서는 해제된 메모리를 조용히 읽는다.

---

# 5부: 잘못된 참조와 네이티브 메모리

## Lab 31 — NativeArray 를 Dispose 하지 않음
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

## Lab 32 — 앞 잡만 Complete 하고 뒤 잡의 출력을 읽음
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

## Lab 33 — Allocator.Temp 버퍼를 다음 프레임에 읽음
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

## Lab 34 — Domain Reload 를 끈 Play 에서 정적 상태가 남음
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

---

# 6부: 물리

## Lab 35 — 빠른 Rigidbody 가 Discrete 충돌 감지로 벽을 터널링
```
틀린 결과:     벽에 맞은 횟수 0(기대 1), 탄환 x = 60 (벽 x = 10)
도구:          FixedUpdate 에 추적점 "{transform.position.x}" → 0, 6, 12, 18 ... 벽(9.9 ~ 10.1) 안에 있었던 스텝이 없다
              Debug.DrawRay 로 스텝마다 간 거리 6m 를 그리면 벽을 건너뛰는 것이 보인다
진짜 원인:     기본값 Discrete 는 스텝이 끝난 위치에서만 겹침을 본다. 300m/s × 0.02초 = 6m 로 0.2m 벽을 한 번에 넘었다
```
**수정:**
```csharp
rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
```
**교훈:** "한 스텝에 가는 거리 > 부딪힐 것의 두께" 면 터널링을 의심한다. 빠른 것만 Continuous 계열로 켜고(비싸다), 아주 많은 총알은 Rigidbody 대신 스텝마다 레이캐스트로 판정한다. 벽을 두껍게 하거나 fixedDeltaTime 을 줄이는 것은 속도가 더 빨라지면 다시 터진다.

## Lab 36 — 양쪽 다 Rigidbody 가 없어 트리거 메시지가 생기지 않음
```
틀린 결과:     함정 발동 0번, HP 100 그대로
도구:          OnTriggerEnter 중단점이 한 번도 걸리지 않는다 → "조건이 틀렸다" 가 아니라 "안 불렸다"
              Physics Debugger 에서 Player 와 SpikeTrap 이 둘 다 Static 으로 나온다
진짜 원인:     Rigidbody 가 없는 콜라이더는 정적 콜라이더다. 정적 콜라이더끼리는 겹쳐도 충돌·트리거 검사를 하지 않는다
```
**수정:**
```csharp
void Awake()
{
    gameObject.AddComponent<Rigidbody>().isKinematic = true;   // 코드로 움직이는 쪽에 Kinematic Rigidbody
}
```
**교훈:** `OnTriggerEnter`/`OnCollisionEnter` 가 안 불리면 ① 둘 중 하나에 Rigidbody(또는 CharacterController)가 있나 ② Layer Collision Matrix 에서 두 레이어가 켜져 있나 ③ 트리거끼리라면 한쪽은 Rigidbody 가 있나 ④ 콜라이더가 꺼져 있지 않나를 차례로 본다. 움직이는 콜라이더에는 Rigidbody 를 붙이는 것이 성능에도 좋다(정적 콜라이더를 옮기면 물리 장면을 다시 만든다).

## Lab 37 — 레이어 번호를 레이어 마스크로 넘김
```
틀린 결과:     포탑이 맞힌 것 Glass (기대 Enemy)
도구:          조사식 Convert.ToString(EnemyLayer, 2) == "1000" → 3번 비트. 유리창이 3번 레이어
              Physics Debugger → Queries 에서 레이가 유리창에서 멈춘다
진짜 원인:     Raycast 의 layerMask 는 비트 마스크다. 8 은 "8번 레이어" 가 아니라 "3번 레이어만" 이다(10 은 1번과 3번)
```
**수정:**
```csharp
Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, Range, 1 << EnemyLayer)
// 더 좋게: public LayerMask EnemyMask;  → 인스펙터에서 고른다
```
**교훈:** `gameObject.layer` 는 번호(0~31), Physics 쿼리·`Camera.cullingMask`·`LayerMask` 는 마스크(비트 묶음)다. 둘 다 int 라 컴파일러가 막지 않는다. 숫자 대신 `LayerMask` 필드나 `LayerMask.GetMask("Enemy")` 를 쓰면 이 혼동이 사라진다.

## Lab 38 — Interpolate 를 켠 Rigidbody 를 Update 에서 transform 으로 움직임
```
틀린 결과:     발판이 1.5초 동안 1.03m(기대 3m). 상자는 0m
도구:          Update 에 추적점 "{Time.frameCount} {transform.position.x}" → 더한 만큼 늘었다가 다음 프레임에 도로 줄어 있다
              transform 이동을 FixedUpdate 로 옮기면 발판은 3m 가지만 상자는 제자리에 남아 떨어진다
진짜 원인:     ① 보간은 매 프레임 transform 을 물리 위치 사이의 보간값으로 덮어쓴다. Update 에서 바꾼 transform 은 대부분 지워진다
              ② transform 으로 옮긴 Kinematic 은 물리 입장에서 순간이동(속도 0)이라 마찰로 상자를 끌지 못한다
```
**수정:**
```csharp
void FixedUpdate()
{
    rb.MovePosition(rb.position + Velocity * Time.fixedDeltaTime);
}
```
**교훈:** Rigidbody 가 붙은 오브젝트는 transform 이 아니라 Rigidbody 로 움직인다. Kinematic 은 FixedUpdate 에서 `MovePosition`/`MoveRotation`, Dynamic 은 `linearVelocity`/`AddForce`. Interpolate 는 이렇게 움직일 때 화면을 부드럽게 해 주는 옵션이지, transform 으로 움직인 것을 부드럽게 해 주지 않는다.

## Lab 39 — transform 을 옮긴 같은 프레임에 물리 쿼리
```
틀린 결과:     돌아간 자리에서 집은 것 null(기대 Crate), 옛 자리에서 집은 것 Crate(기대 null)
도구:          ResetCrates 뒤 조사식: transform.position == (0,0,4), GetComponent<Collider>().bounds.center == (8,0,4)
진짜 원인:     transform 변경은 다음 물리 스텝 직전에 물리 장면으로 옮겨진다. 그 사이의 Raycast 는 옛 자리의 콜라이더를 본다
              (Physics.autoSyncTransforms 를 켜면 쿼리마다 동기화하지만 2018.3 부터 기본 꺼짐. 쿼리마다 비용이 커서 권장하지 않고,
               필요한 곳에서 Physics.SyncTransforms() 를 한 번 부르는 것이 정석이다)
```
**수정:**
```csharp
for (int i = 0; i < Crates.Length; ++i) { Crates[i].position = StartPositions[i]; }
Physics.SyncTransforms();
```
**교훈:** 순간이동, 레벨 초기화, 오브젝트 풀에서 꺼내 자리 잡기 직후의 Raycast·Overlap 은 옛 자리를 본다. 같은 프레임에 쿼리해야 하면 `Physics.SyncTransforms()` 를 한 번 부르거나, Rigidbody 가 있으면 `rb.position` 으로 옮긴다. 한 프레임 기다리면 "저절로 고쳐지는" 버그라 재현이 잘 안 된다.

---

# 7부: 사운드

## Lab 40 — Preload Audio Data 를 꺼 둔 클립을 미리 로드하지 않음
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
② `blaster.wav` 임포트 설정에서 **Preload Audio Data** 를 켜고 Apply.
**교훈:** 오디오 임포트 설정 셋의 의미: **Load Type**(메모리에 어떤 형태로: 풀어서/압축째/스트리밍), **Preload Audio Data**(씬·에셋이 로드될 때 데이터도 같이), **Load In Background**(로드를 기다리지 않고 미루기). 끄면 메모리를 아끼는 대신 "언제 올릴지"를 코드가 책임져야 한다. Load In Background 를 끄면 지연 대신 그 프레임이 멈칫한다(히치).

## Lab 41 — 실제 음성 한도를 넘었고 보스 대사의 우선순위가 같음
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

## Lab 42 — AudioSource 가 붙은 오브젝트를 바로 Destroy
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

## Lab 43 — 짧은 효과음을 Play() 로 내서 긴 소리를 끊음
```
틀린 결과:     1초 뒤 Source.clip == grunt, 브리핑 재생 안 함
멈춘 위치:     PlayHurt → F10 전후: Source.clip briefing → grunt, Source.time 0.5 → 0
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

# 8부: 이미지 / 렌더링

## Lab 44 — sharedMaterial 을 고쳐서 에셋 자체를 바꿈
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
(사본의 정리는 랩 45 참고. 색만 바꾼다면 MaterialPropertyBlock 이 더 좋다.)
**교훈:** `sharedMaterial`/`sharedMesh` = 에셋(모두 공유), `material`/`mesh` = 첫 접근 때 복제한 내 것. 에디터에서 `sharedMaterial` 을 런타임에 고치면 플레이를 멈춰도 돌아오지 않는다. `git status` 에 `.mat` 이 뜨면 이 버그를 의심하자.

## Lab 45 — renderer.material 복제본이 누수됨
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

## Lab 46 — 늦게 도착한 비동기 로드 결과가 재사용된 칸을 덮어씀
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

## Lab 47 — Linear 색 공간에서 색을 SetVector 로 넘김
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

# 9부: 애니메이션

## Lab 48 — Animator 파라미터 이름의 대소문자
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

## Lab 49 — Culling Mode = Cull Completely 라 화면 밖에서 이벤트가 사라짐
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

## Lab 50 — 트랜지션이 이벤트보다 먼저 끝남
```
틀린 결과:     3번 휘둘러 Hit 0번
도구:          Animation 창: Swordsman_Swing 길이 1초, Hit 이벤트 0.9초
              Animator 창: Swing → Idle 트랜지션 Has Exit Time ✔, Exit Time 0.7, Transition Duration 0.1 (Fixed Duration 끔 = 클립 길이의 10%)
처음 어긋난 곳: Swing 은 0.7 에 빠지기 시작해 0.8 에 완전히 끝난다. 0.9 의 이벤트는 재생되지 않는 구간
진짜 원인:     애니메이션 에셋. 이벤트가 트랜지션 뒤에 있다.
```
**수정 (택1):** Animator 창에서 Swing → Idle 트랜지션의 **Exit Time 을 0.95** 로(이벤트 뒤에서 넘어가기 시작). 또는 Animation 창에서 **Hit 이벤트를 0.5초** 무렵(칼날이 실제로 닿는 지점)으로 옮기기.
**교훈:** 트랜지션 도중에는 두 상태의 클립이 같이 재생되며 양쪽 이벤트가 불리지만, 트랜지션이 **끝난 뒤** 원래 클립은 더 진행되지 않는다. 판정용 이벤트는 트랜지션 시작 전에 둔다. 피격·회피 같은 인터럽트로 공격이 끊길 때 판정이 빠지는 것도 같은 원리이고, 그건 의도된 동작일 수도 있다(기획과 정하자).

## Lab 51 — 애니메이션 결과를 Update 에서 읽음
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

---

# 10부: 동시성과 멈춤

## Lab 52 — 재고 확인과 차감 사이에 다른 스레드가 끼어듦
```
틀린 결과:     팔린 수 2(기대 1), 남은 재고 -1(기대 0), 구매자 명단 2명
도구:          TryBuy 의 if 줄에 중단점 → 스레드 창: 워커 두 개가 같은 줄에 있고 둘 다 Stock == 1
              추적점 "{Environment.CurrentManagedThreadId} Stock={Stock}" → 두 스레드 모두 1 을 보고 통과
진짜 원인:     "Stock > 0 확인 → 결제 → Stock--" 이 한 덩어리가 아니다. 결제가 끝나기 전에 두 번째 요청도 확인을 통과한다
```
**수정:**
```csharp
readonly object gate = new object();

public bool TryBuy(string buyer)
{
    lock (gate)
    {
        if (Stock > 0) { Payment(buyer); Stock--; Buyers.Add(buyer); return true; }
        return false;
    }
}
```
**교훈:** 공유 상태를 "확인하고 → 행동" 하는 코드는 둘 사이에 끼어들 틈이 없게 한 덩어리로 묶는다(`lock`). 결제처럼 오래 걸리는 일을 lock 안에 두기 싫다면 lock 안에서 재고를 먼저 예약(Stock--)하고 실패 시 되돌린다. `Stock--` 와 `List.Add` 자체도 원자적이지 않다(`Interlocked.Decrement`, `ConcurrentQueue`). Job System 안전 시스템은 NativeContainer 만 감시하므로 이런 C# 객체 레이스는 아무도 알려 주지 않는다.

## Lab 53 — Task.Run 안에서 transform 을 씀
```
틀린 결과:     길 찾기는 워커 스레드에서 했지만 도착 false, 위치 그대로. Console 은 조용하다
도구:          transform.position 줄에서 F10 → 다음 줄로 가지 않고 사라진다
              조사식 task.Status == Faulted, task.Exception.InnerException: UnityException "get_transform can only be called from the main thread"
진짜 원인:     Unity API 는 메인 스레드 전용이다. Task 안에서 난 예외는 Task 에 담기고, 아무도 await 하지 않아 Console 에 나오지 않는다
```
**수정:**
```csharp
public async Task GoTo(Vector3 from, Vector3 goal)
{
    List<Vector3> path = await Task.Run(() => FindPath(from, goal));   // 계산만 워커에서
    transform.position = path[path.Count - 1];                          // await 뒤는 메인 스레드
    Arrived = true;
}
```
**교훈:** 워커 스레드에는 계산만 보내고, Unity 오브젝트를 만지는 일은 메인 스레드로 돌아와서 한다. 메인 스레드에서 시작한 `await` 는 UnitySynchronizationContext 가 다음 줄을 메인 스레드로 돌려보낸다. 돌려받은 Task 를 버리면(fire-and-forget) 예외도 함께 버려지므로, await 하거나 `ContinueWith` 로 `Exception` 을 로그로 남긴다. Task 안에서는 `transform.position` 을 **읽는 것**도 안 된다(값을 미리 꺼내 넘긴다).

## Lab 54 — BackgroundThreadAsync 뒤에 메인 스레드로 돌아오지 않음
```
증상:          불러오기 시 UnityException: get_gameObject can only be called from the main thread
도구:          Load 첫 줄과 gameObject.name 줄에서 조사식 Thread.CurrentThread.ManagedThreadId → 1 에서 다른 번호로 바뀌었다
진짜 원인:     await Awaitable.BackgroundThreadAsync() 다음 줄부터는 스레드 풀이다. 돌아오라고 하기 전까지 계속 백그라운드다
```
**수정:**
```csharp
await Awaitable.BackgroundThreadAsync();
SaveData data = Parse(json);
await Awaitable.MainThreadAsync();      // 여기부터 메인 스레드
gameObject.name = data.Name;
transform.localScale = Vector3.one * data.Scale;
```
**교훈:** await 다음 줄의 스레드는 "무엇을 await 했나" 가 정한다. 보통의 `await Task`(메인 스레드에서 시작)는 메인으로 돌아오고, `Awaitable.BackgroundThreadAsync()` 와 `ConfigureAwait(false)` 는 돌아오지 않는다. 백그라운드 구간은 `MainThreadAsync()` 로 닫는 짝을 만든다. 돌아온 뒤 오브젝트가 이미 파괴됐을 수 있으니 `if (this == null) return;` 이나 `destroyCancellationToken` 을 쓴다.

## Lab 55 — 메인 스레드에서 .Result 로 기다려 데드락
```
증상:          상점을 여는 순간 응답 없음. CPU 는 0% 근처(데드락)
모두 중단:      Main Thread 호출 스택: ShopWindow.Open → (HangGuard.Wait =) Task.Wait → Monitor.Wait. 자고 있다
              작업(Tasks) 창 / 병렬 스택: LoadGoldAsync 는 "대기 중", await Task.Delay 다음 줄을 실행할 차례를 기다린다
처음 어긋난 곳: Task.Delay 는 200ms 뒤에 끝났다. 그런데 await 다음 줄(return Gold)은 UnitySynchronizationContext 를 통해 "메인 스레드, 다음 프레임" 에 예약됐다
진짜 원인:     메인 스레드는 그 Task 가 끝나기를 막고 기다리고, Task 는 메인 스레드가 비기를 기다린다. 서로 기다린다
              (콘솔 앱은 SynchronizationContext 가 없어 스레드 풀에서 이어 가므로 멀쩡했다)
```
**수정:**
```csharp
public async void Open(SaveFile save)
{
    ShownGold = await save.LoadGoldAsync();
}
```
(코루틴이라면 `var task = save.LoadGoldAsync(); while (!task.IsCompleted) yield return null; ShownGold = task.Result;`)
**교훈:** 메인 스레드에서 `Task` 를 막고 기다리지 말 것(`.Result`, `.Wait()`, `.GetAwaiter().GetResult()`). 끝까지 `await` 로 잇는다. 라이브러리 쪽에 `ConfigureAwait(false)` 를 붙이면 데드락은 풀리지만 `Open` 은 여전히 디스크를 읽는 200ms 동안 게임을 멈춘다(이 랩의 두 번째 `[FAIL]`). 그리고 `ConfigureAwait(false)` 다음 줄은 워커 스레드에서 돌기 때문에 거기서 Unity API(`transform`, `GetComponent`)를 부르면 "can only be called from the main thread" 예외가 난다. 데드락은 CPU 0%, 무한루프는 CPU 100% 라는 지문을 기억하자.

## Lab 56 — Update 가 바꿔 줄 값을 yield 없이 while 로 기다림
```
증상:          엘리베이터를 부르는 순간 응답 없음. 작업 관리자에서 Unity 가 CPU 한 코어를 꽉 씀(무한루프)
모두 중단:      스레드 창의 Main Thread 호출 스택 맨 위 = Passenger.RideTo 의 첫 while (HangGuard 의 예외 스택 트레이스도 같은 곳)
이상한 값:      F10 을 계속 눌러도 Elevator.Floor 가 5 에서 바뀌지 않는다
처음 어긋난 곳: Floor 를 바꾸는 것은 Elevator.Update. 그런데 Update 는 이 코루틴이 yield 해야 다음 프레임에 불린다
진짜 원인:     코루틴은 스레드가 아니다. yield 를 만나기 전까지는 메인 스레드의 보통 함수다. while 안에 yield 가 없으니 이 프레임에서 영원히 돈다
```
**수정:**
```csharp
while (!Elevator.IsAt(Floor))
{
    HangGuard.Tick(Loc.L("Passenger.RideTo: 엘리베이터가 오기를 기다림", "Passenger.RideTo: エレベーターが来るのを待機"));
    yield return null;   // 한 프레임 양보 → 그동안 Elevator.Update 가 돈다
}
...
while (!Elevator.IsAt(destination))
{
    HangGuard.Tick(Loc.L("Passenger.RideTo: 목표 층에 닿기를 기다림", "Passenger.RideTo: 目的の階に着くのを待機"));
    yield return null;
}
```
(또는 `yield return new WaitUntil(() => Elevator.IsAt(Floor));`)
**교훈:** "다른 무언가가 바꿔 줄 값" 을 기다리는 반복문은 반드시 그 무언가에게 차례를 넘겨야 한다. Unity 에서는 `yield return null`(코루틴), `await Awaitable.NextFrameAsync()`(async), 아니면 Update 에서 상태를 확인하는 방식. Update/Start/버튼 처리기 안의 `while (!loaded) {}` 도 똑같이 에디터를 굳힌다. 굳었을 때는 **모두 중단 → 스레드 창 → Main Thread** 가 첫 수다.

## Lab 57 — BFS 에 방문 체크가 없음
```
증상:          길이 이어져 있을 때는 정답(5). 3-6 문을 잠근 뒤 응답 없음, 메모리 급증
도구:          while 첫 줄 조건부 중단점 queue.Count > 100 → 방이 8개뿐인데 걸린다. queue 를 펼치면 1, 2, 4, 5 가 수십 번씩
처음 어긋난 곳: 0 → 1 → (0, 2, 4) 에서 다시 0 을 넣는다. 양방향 문과 1-2-5-4 고리를 따라 같은 방을 끝없이 다시 넣는다
진짜 원인:     목표에 닿을 수 있으면 BFS 는 언젠가 목표를 꺼내 return 하므로 버그가 숨어 있었다. 닿을 수 없으면 큐가 비지 않는다
```
**수정:**
```csharp
var queue = new Queue<(int Room, int Doors)>();
queue.Enqueue((start, 0));
var visited = new bool[dungeon.Doors.Length];
visited[start] = true;
while (queue.Count > 0)
{
    ...
    foreach (int next in dungeon.Doors[room])
    {
        if (!visited[next])
        {
            visited[next] = true;
            queue.Enqueue((next, doors + 1));
        }
    }
}
```
**교훈:** 그래프(방, 웨이포인트, 대화 노드, 전선, 아이템 조합)를 순회하는 코드는 **방문 기록**이 기본이다. 테스트 데이터에 고리나 끊긴 길이 없으면 영영 드러나지 않다가, 레벨 디자이너가 문 하나를 잠그는 순간 터진다. "멈춤 + 메모리 증가" 면 무한루프 안에서 무언가를 쌓고 있다는 뜻이다. 재귀 DFS 에서 같은 실수를 하면 멈춤 대신 StackOverflowException(에디터 크래시)으로 나타난다.

## Lab 58 — float 시간에 작은 틱을 누적
```
증상:          막 켠 서버(90초)는 128틱으로 정상. 켠 지 7일(604800초) 된 서버는 응답 없음
도구:          while 안 조건부 중단점 Ticks > 1000 → 조사식 SimTime == 604800, SimTime + TickSeconds == SimTime 이 true
진짜 원인:     float 의 유효 숫자는 24비트. 2^19 = 524288 이상에서 이웃한 float 의 간격은 1/16. 1/64 를 더하면 반올림되어 그대로다.
              SimTime < target 이 영원히 참이다
```
**수정 (택1):**
```csharp
public double SimTime;                       // ① double: 1/64 는 2 의 거듭제곱이라 double 에서 정확하다
...
double target = SimTime + seconds;

// ② 정수 틱으로 세고 시간은 계산한다 (가장 튼튼하다)
public long TickCount;
public double SimTime => TickCount * (double)TickSeconds;
public void FastForward(float seconds)
{
    int steps = Mathf.RoundToInt(seconds / TickSeconds);
    for (int i = 0; i < steps; ++i) { ... ++TickCount; }
}
```
**교훈:** 반복문의 종료 조건을 **float 누적 결과**에 걸지 말자. 큰 값에 작은 값을 더하면 그대로일 수 있고(멈춤), 작은 값끼리 더해도 `0.1f` 열 번이 `1f` 가 아니다(`!=` 로 끝내면 멈춤). Unity 의 `Time.time` 도 float 이라 며칠 켜 둔 서버·키오스크·스트리밍 빌드에서 같은 일이 생긴다(`Time.timeAsDouble`, `Time.frameCount` 를 쓴다). 개발 중에는 절대 재현되지 않는 "오래 켜 두면" 류 버그의 대표다.

---

# 11부: 성능 (GC 할당과 반복 탐색)

## Lab 59 — 값이 그대로인데 매 프레임 문자열·LINQ·박싱
```
틀린 결과:     값이 그대로인 프레임의 GC 할당 프레임당 약 5600바이트, 표시줄 20개 (기대 128바이트 이하)
도구:          Profiler → CPU → Hierarchy → GC Alloc 열 → HealthBar.Update
              Deep Profile: String.Concat, String.Format, Enumerable.Count, List<T>.GetEnumerator(박싱) 이 그 아래에 보인다
진짜 원인:     결과가 지난 프레임과 같은데 매 프레임 새 문자열을 만들고, LINQ 열거자와 인터페이스로 박싱된 열거자를 만든다
```
**수정:**
```csharp
Health health;
int shownHp = -1, shownMax = -1, shownAlive = -1, shownTotal = -1;

void Awake() { health = GetComponent<Health>(); }

void Update()
{
    if (health.Current != shownHp || health.Max != shownMax)
    {
        shownHp = health.Current; shownMax = health.Max;
        Text = "HP " + shownHp + " / " + shownMax;
    }
    int alive = 0, total = 0;
    for (int i = 0; i < Squad.Members.Count; ++i)
    {
        Health member = Squad.Members[i];
        if (member.Current > 0) { alive++; }
        total += member.Current;
    }
    if (alive != shownAlive || total != shownTotal)
    {
        shownAlive = alive; shownTotal = total;
        SquadText = Loc.F("생존 {0}명, 부대 HP {1}", "生存 {0} 人、部隊の HP {1}", alive, total);
    }
}
```
**교훈:** 매 프레임 도는 코드에서는 "값이 바뀔 때만 만든다". 문자열 연결·보간, LINQ, `IEnumerable<T>` 로 받은 foreach(박싱), 람다가 지역 변수를 캡처하는 클로저, `new List` 는 모두 힙 할당이다. 한 번에 수십 바이트라도 컴포넌트 수 × 매 프레임이면 GC 를 부른다. UI 텍스트는 TextMeshPro 의 `SetText(format, number)` 가 할당 없이 숫자를 넣는다.

## Lab 60 — Update 에서 GameObject.Find 와 FindObjectsByType
```
틀린 결과:     표적 찾기의 GC 할당 프레임당 약 98KB (기대 256바이트 이하)
도구:          Profiler → Hierarchy → Turret.Update 아래 GameObject.Find, FindObjectsByType: Calls 40, Time ms 가 오브젝트 수에 비례
진짜 원인:     포탑 40대가 매 프레임 씬 전체를 훑는다. FindObjectsByType 은 결과를 새 배열로 돌려준다(매번 할당)
```
**수정:**
```csharp
public class Enemy : MonoBehaviour
{
    public static readonly List<Enemy> All = new List<Enemy>();
    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }
}

void Start() { Player = GameObject.Find("Player").transform; }   // 한 번만
void Update() { foreach (Enemy enemy in Enemy.All) { ... } }      // List 를 직접 돌면 할당 없음
```
**교훈:** 바뀌지 않는 참조는 한 번 찾아 두고(인스펙터 연결이 가장 좋다), 바뀌는 집합은 OnEnable/OnDisable 로 등록부를 관리한다. 등록부는 새로 나타난 적, 사라진 적을 바로 반영한다(한 번 찾고 끝내면 랩의 두 번째 판정에서 FAIL). `Camera.main` 은 2020.2 부터 내부 캐시라 예전처럼 비싸지 않다. 정적 등록부는 Domain Reload 를 끈 Play 에서 비워 줘야 한다(랩 34).

## Lab 61 — 풀을 만들고 돌려주지 않음
```
틀린 결과:     만든 탄환 수 60(기대 ≤ 13), 풀로 돌아온 탄환 0
도구:          BulletPool.Release 에 중단점 → 한 번도 멈추지 않는다
              Profiler → Hierarchy 에서 Instantiate / Destroy 의 Calls 가 발사 수만큼
진짜 원인:     Bullet 은 수명이 다하면 예전처럼 Destroy(gameObject) 한다. 풀은 늘 비어 있으므로 Get 이 매번 Instantiate 한다
```
**수정:**
```csharp
if (Life <= 0f) { Pool.Release(this); }
```
**교훈:** 풀은 "꺼내기" 보다 "돌려주기" 가 버그가 잘 난다. 돌려줄 때 끄고(SetActive(false)), 꺼낼 때 위치·속도·수명·트레일 같은 상태를 모두 초기화한다. `UnityEngine.Pool.ObjectPool<T>` 는 createFunc/actionOnGet/actionOnRelease 를 받아 이 짝을 강제하고, 같은 것을 두 번 돌려주면 에러를 낸다(collectionCheck).

## Lab 62 — SendMessage 이름 오타와 박싱
```
틀린 결과:     받은 피해 0. 이름을 TakeDamage 로 고쳐도 GC 할당이 프레임당 약 500바이트(적 30마리)
도구:          Enemy.TakeDamage 중단점이 걸리지 않는다. RequireReceiver 로 바꾸면 "SendMessage TakeDamge has no receiver!"
              Profiler → GC Alloc: SendMessage 아래 박싱된 float
진짜 원인:     SendMessage 는 문자열로 메서드를 찾는다. 오타는 컴파일러가 모르고, DontRequireReceiver 라 조용하다. 인자는 object 로 박싱된다
```
**수정:**
```csharp
public interface IDamageable { void TakeDamage(float amount); }
public class Enemy : MonoBehaviour, IDamageable { ... }

if (InFlame[i].TryGetComponent(out IDamageable target)) { target.TakeDamage(DamagePerSecond * Time.deltaTime); }
```
**교훈:** `SendMessage`/`BroadcastMessage`/`SendMessageUpwards` 는 오타가 조용하고, 매번 모든 컴포넌트를 리플렉션으로 훑으며, 인자를 박싱한다. 인터페이스(`IDamageable`), C# event, UnityEvent 는 이름을 컴파일 시간에 확인한다. 매 프레임 `TryGetComponent` 도 아끼려면 들어올 때 한 번 찾아 목록에 담는다.

## Lab 63 — 매 프레임 배열을 돌려주는 OverlapSphere
```
틀린 결과:     치유는 맞지만 GC 할당 프레임당 1792바이트 = 사제 8명 × (아군 24명 × 8 + 배열 머리 32) (기대 128바이트 이하)
도구:          Profiler → GC Alloc 열 → HealingAura.Update → Physics.OverlapSphere
진짜 원인:     결과를 새 Collider[] 로 돌려주는 API 를 매 프레임 부른다
```
**수정:**
```csharp
readonly Collider[] hits = new Collider[32];

void Update()
{
    int count = Physics.OverlapSphereNonAlloc(transform.position, Radius, hits);
    for (int i = 0; i < count; ++i) { ... }
}
```
**교훈:** 배열을 돌려주는 API(`OverlapSphere`, `RaycastAll`, `GetComponents<T>()`, `GetComponentsInChildren<T>()`, `FindObjectsByType`, `Mesh.vertices`)에는 대개 버퍼를 채우는 짝(`NonAlloc`, `List<T>` 를 받는 오버로드, `GetVertices(List)`)이 있다. 버퍼가 작으면 넘친 결과가 **조용히 버려지므로** 돌려받은 개수가 버퍼 크기와 같을 때 경고를 남긴다(숨은 케이스는 아군 30명). 버퍼를 함수 밖으로 내보내면 랩 28 의 공유 버그가 된다.

