# 解答 (ネタバレ注意!)

[한국어](SOLUTIONS.md) | **日本語**

数値 (フレーム番号、ms) は PC や実行方法によって少しずつ異なります。判定に使う値 (期待値/実際の値) は同じです。
採点ツールの隠しテストケースは入力 (敵の数、座標、ダメージ量、サーバー稼働時間など) だけを変えるので、解答は同じです。原因を直していれば隠しケースもそのまま通ります。

# 第1部: クラッシュ / 例外

## ラボ 1 — 他人が破壊した標的を撃ち続ける
```
止まった関数:      Tower.Update (Target.transform にアクセス)
おかしな値:        Target == null が true。なのにウォッチ式で展開すると Health などのフィールドが見える (破壊されたオブジェクト)
その値の出どころ:  Enemy.TakeDamage の Destroy ← Bomb.Explode ← Lab01_Tower.Run  (監視塔ではなく爆弾が破壊)
経由したエンジン機能: Update は Unity が毎フレーム呼ぶ (下に自分のコードのフレームがない)。Destroy はフレームの最後に実際に破壊する
本当の原因:        監視塔は「自分が倒したときだけ」次の標的を選ぶ。他人が標的を破壊するケースを考えていなかった。
```
**修正:**
```csharp
void Update()
{
    if (Target == null)          // Unity の == : 破壊されたオブジェクトも null とみなす
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
**教訓:** Unity オブジェクトを持っているフィールドは **いつでも他人に破壊される可能性があります。** 使う直前に `== null` で確認するのが基本です。`MissingReferenceException` は「null だから」ではなく「破壊されたのにアクセスしたから」です。誰が破壊したかは `Destroy` の行 (または `OnDestroy`) にブレークポイントを置いてコールスタックを見ればすぐにわかります。

## ラボ 2 — GetComponent は当たったオブジェクト自身しか見ない
```
止まった関数:      Rifle.Shoot (health.TakeDamage) — 3 発目
おかしな値:        health == null, hit.collider.name == "Head"
その値の出どころ:  hit.collider.GetComponent<Health>(): 当たったコライダーは子の Head にあり、Health は親の TrainingDummy にある
本当の原因:        胴体のコライダーは Health と同じオブジェクトなので偶然動いていた。子のコライダーでは GetComponent が null。
```
**修正:**
```csharp
var health = hit.collider.GetComponentInParent<Health>();
if (health == null)
{
    return;   // 壁や床のように体力のないものに当たった
}
health.TakeDamage(headshot ? Damage * 2 : Damage);
```
(`hit.rigidbody`/`hit.collider.attachedRigidbody` がある構造なら、そのオブジェクトから探す方法もよく使われます。)
**教訓:** `GetComponent<T>()` は失敗しても例外を出さずに null を返します。キャラクターは普通「ルートにロジック、子にコライダー/モデル」という構造なので、部位判定でよく問題になります。存在しないことがあるなら `TryGetComponent` か null チェック、必ず存在すべきなら `[RequireComponent]` を使います。

## ラボ 3 — シングルトンの登録を Start で行う
```
止まった関数:      ScoreboardUI.OnEnable
おかしな値:        GameManager.Instance == null
その値の出どころ:  スタック: ScoreboardUI.OnEnable ← GameObject.AddComponent ← Lab.Spawn ← Lab03_Scoreboard.Run
                 つまり AddComponent の中で OnEnable がすぐに呼ばれた。そのとき GameManager は Awake/OnEnable だけが終わっていて Start はまだ。
順序:             GameManager.Awake → GameManager.OnEnable → ScoreboardUI.Awake → ScoreboardUI.OnEnable(ここで落ちる) → … 次のフレーム → GameManager.Start
本当の原因:        Instance を Start で設定していた。
```
**修正:**
```csharp
void Awake()
{
    Instance = this;
}
```
**教訓:** 「他人が自分を使うための準備」は Awake、「自分が他人を使う処理」は Start です。AddComponent/Instantiate/SetActive(true) はその行の中で Awake・OnEnable を呼びます。シーンに最初から置かれたオブジェクト同士の Awake の順序は決まっていないので、このバグは「シーンでは動くのにプレハブにすると動かない」という形でも現れます。

## ラボ 4 — ルートを無効にするとそのオブジェクトのコルーチンも終わる
```
エラー:            Coroutine couldn't be started because the the game object 'Player' is inactive!  ← Player.Die
おかしな値:        Die の中で SetActive(false) の次の行が StartCoroutine
おまけの実験:      順序を入れ替えるとエラーは消えるが、RespawnAfter の yield の次の行に到達しない → やはり FAIL
本当の原因:        コルーチンは開始した MonoBehaviour のものだ。そのゲームオブジェクトが非アクティブになるとコルーチンは停止 (終了) し、再びアクティブにしても再開しない。
```
**修正 (いずれか 1 つ):**
```csharp
// ① 見える部分だけを隠す (このラボの正解)
Model.SetActive(false);
StartCoroutine(RespawnAfter(0.5f));

// ② 常にアクティブな別のオブジェクトで待つ
GameSession.Instance.StartCoroutine(RespawnAfter(0.5f));
```
**教訓:** エラーを消すこととバグを直すことは違います。「無効になるオブジェクト」で待つ処理 (復活、クールダウン、フェード) をしないこと。`enabled = false` (コンポーネントだけを無効にする) はコルーチンを止めませんが、`SetActive(false)` と `Destroy` は止めます。

---

# 第2部: 処理の流れの追跡

## ラボ 5 — OnEnable で購読して OnDisable で解除しない
```
間違った結果:     再び開いたウィンドウの記録 2 行、通知 4 回、ウィンドウを破壊した後に通知 6 回
止まった位置:     HandleDamaged のブレークポイント → damage が 10, 5, 7, 7, 3, 3 の順
期待 → 実際:      10, 7 → 10, 5 (ウィンドウが無効なのに!), 7, 7, 3, 3 (ウィンドウが破壊されたのに!)
最初にずれた所:   ウィンドウを無効にした後の TakeHit(5)。Damaged.GetInvocationList().Length: 有効 1 → 無効 1 → 再び有効 2 → 破壊 2
本当の原因:       OnEnable で += だけして -= がない。無効になっても破壊されても、C# のイベントはデリゲート (とその中の this) を持ち続ける。
```
**修正:**
```csharp
void OnDisable()
{
    if (Target != null)
    {
        Target.Damaged -= HandleDamaged;
    }
}
```
**教訓:** C# のイベント・`Action`・`UnityEvent.AddListener` は Unity オブジェクトの寿命を知りません。**購読した場所ごとに対になる解除** (OnEnable↔OnDisable、Awake/Start↔OnDestroy) を置きます。破壊された購読者が `transform` のような Unity API に触れると、そこで初めて MissingReferenceException が出ますが、フィールドに触れるだけなら静かに間違えます。破壊されたオブジェクトを掴んだままなので、メモリリークでもあります。

## ラボ 6 — 読む側が書く側より先に Update される
```
間違った結果:     30 フレームすべてで HP バーが 1 大きく、倒れたフレームで HP バーが 1
止まった位置:     2 つの Update にトレースポイント → 毎フレーム「HealthBar → BleedingHero」の順
最初にずれた所:   最初のフレームから (英雄 29、HP バー 30)
本当の原因:       BleedingHero の [DefaultExecutionOrder(100)]。HealthBar はデフォルトの 0 なので先に実行され、前のフレームの HP を読む。
```
**修正 (いずれか 1 つ):**
```csharp
// ① HealthBar: 他人が変えた値を表示する側は LateUpdate
void LateUpdate()
{
    Displayed = Hero.Health;
}

// ② 順序を明示
[DefaultExecutionOrder(200)]
public class HealthBar : MonoBehaviour { ... }
```
**教訓:** スクリプト実行順序 (Script Execution Order) が同じスクリプト同士の Update の順序は **決まっていません** (たいてい生成順ですが、当てにしてはいけません)。順序が重要なら LateUpdate、`[DefaultExecutionOrder]`、または 1 つのマネージャーが順番に呼ぶ構造で明示します。`[DefaultExecutionOrder]` はプロジェクト設定 (Project Settings) ウィンドウに表示されないことがあり、「誰も順序を変えていないんですが」の原因になります。

## ラボ 7 — 1 フレームだけの入力を FixedUpdate で読む
```
間違った結果:     10 回押して 5 回ジャンプ
使ったツール:     Update/FixedUpdate にトレースポイント → フレーム間隔 8.3ms、FixedUpdate 間隔 20ms: FixedUpdate は 2〜3 フレームに 1 回
最初にずれた所:   JumpDown が true のフレームで FixedUpdate が 0 回だった最初のジャンプ
本当の原因:       GameInput.JumpDown (= Input.GetButtonDown) はそのフレームの間だけ true。FixedUpdate がそのフレームで実行されないと取りこぼし、
                 遅い PC (1 フレームに FixedUpdate 2 回) では逆に 2 回ジャンプする。
```
**修正:**
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
**教訓:** Update はフレームごとに正確に 1 回、FixedUpdate は 0〜複数回です。入力は Update で受け取り、物理は FixedUpdate で消費します。「モニター/PC によって違う」という報告は、まずこの 2 つのループの回数の違いを疑いましょう。このラボのように `Time.captureDeltaTime` でフレーム間隔を固定すると確実に再現できます。

## ラボ 8 — fake null: `??=` と `?.` は破壊を知らない
```
間違った結果:     カカシが 2 体立っている、スコア 500
止まった位置:     Score += 100 の 2 回目のヒット → Target = Dummy_0、Health -10
ウォッチ式:       Target == null → true,  (object)Target == null → false,  Target is null → false
最初にずれた所:   矢 3: Dummy_0 は矢 2 で破壊されたのに、Target ??= が「空ではない」と見てそのままにした → Target?.TakeHit もそのまま呼ばれる
本当の原因:       ?? ??= ?. is null は Unity がオーバーロードした == を使わない。破壊されたオブジェクトの C# の抜け殻 (fake null) を生きているとみなす。
```
**修正:**
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
**教訓:** `UnityEngine.Object` には `==`/`!=`/`bool` 変換だけを使います。VS/Rider の Unity アナライザー (UNT0007、UNT0008、UNT0023) が警告してくれます。同じファイルの `FindNearest` がすでに `d != null` を使っていたこともヒントです。逆に、`Destroy` 直後の同じフレームでは `== null` も false です (実際の破壊はフレームの最後)。

## ラボ 9 — List のインデクサーが返した構造体のコピーを変更する
```
間違った結果:     最高の高さ 2.5
止まった位置:     SnapWaypointsToGround の for の中。F10 の前後で Waypoints[i] がそのまま (5.00, 2.50, 0.00)
本当の原因:       Vector3 は struct。List<Vector3>[i] は要素のコピーを返し、コピーに対する Set() は捨てられる。
                 (Waypoints[i].y = 0 は CS1612 でコンパイラーが防ぐが、メソッド呼び出しは防がない)
```
**修正:**
```csharp
for (int i = 0; i < Waypoints.Count; ++i)
{
    Vector3 p = Waypoints[i];
    p.y = GroundHeight;
    Waypoints[i] = p;
}
```
**教訓:** `transform.position`、`rigidbody.linearVelocity`、`List<Vector3>[i]`、`Dictionary` の値のように **構造体を返すプロパティ/インデクサー** はすべてコピーです。「読んで → 直して → 代入し直す」。配列 (`Vector3[]`) は要素を直接指すので `arr[i].Set()` が動く、という違いのせいでリファクタリング中に発生することもあります。(例外: `particleSystem.main` のようなモジュール構造体は内部で元のデータを指すように作られているので、コピーに書き込んでも構いません。)

## ラボ 10 — プレハブに保存された値がコードの初期値に勝つ
```
間違った結果:     プレハブのスライム MaxHealth 100 (AddComponent のスライムは 150)
止まった位置:     SlimeStats.Awake にブレークポイント → 入った瞬間すでに 100
確認:             Slime.prefab をテキストで開くと "MaxHealth: 100"
最初にずれた所:   Instantiate が ① フィールド初期値 150 で作った後 ② プレハブに保存された 100 で上書き → ③ Awake
本当の原因:       1.0 のときに作ったプレハブが 100 を保存している。コードの初期値は「最初にアタッチするときのデフォルト値」にすぎない。
```
**修正:** `Assets/BugHunt/Resources/BugHunt/Slime.prefab` を開き、インスペクター (Inspector) で MaxHealth を 150 に変えて保存します。(コンポーネントの ⋮ → **リセット** (Reset) でもできますが、他のフィールドもコードの初期値に戻ります。)
**教訓:** シリアライズされるフィールド (`public`、`[SerializeField]`) の値は `フィールド初期値 → プレハブ/シーンに保存された値 → プレハブインスタンスのオーバーライド (太字)` の順に上書きされます。「コードのデフォルト値を変えたのに変わらない」なら、プレハブ、プレハブバリアント、シーンに置かれたインスタンスを順に疑いましょう。フィールド名を変えると保存された値が消えますが、維持するには `[FormerlySerializedAs("旧名")]` を使います。

## ラボ 11 — ゲーム中に変わる値を ScriptableObject に書き込む
```
間違った結果:     プレイヤー 2 の弾薬 27、アセットの Ammo の変化量 -3。ラボをもう一度実行すると「ラボ開始時のアセット Ammo: 27」→ 24 → …
止まった位置:     Fire の --Data.Ammo → ウォッチ式 player1.Data == player2.Data → true (同じアセットオブジェクト)
本当の原因:       WeaponData アセットはプロジェクトに 1 つしかないオブジェクト。2 つの武器が同じオブジェクトの Ammo を減らしている。
                 エディターでは再生モードが終わってもメモリ上のアセットオブジェクトがそのままなので値が残り、アセットが保存されるとファイルにも書き込まれる。
```
**修正:**
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
(`WeaponData.Ammo` フィールドは削除して構いません。)
**教訓:** ScriptableObject には **読み取り専用の設定** だけを置きます。ゲーム中に変わる状態はコンポーネント (インスタンス) が持ちます。実行時のコピーが必要なら `Instantiate(data)` を使います。ビルドではプログラムを再起動すると元の値に戻るので「エディターでだけ起きるバグ」のように見えますが、ビルドでも **同じ実行の中では** 2 人のプレイヤーが値を共有する本物のバグです。すでに値が減っているなら、エディターを再起動するかアセットを再インポート (Reimport) しましょう (`git status` で Rifle.asset が変わっていないかも確認)。

## ラボ 12 — フレームごとの移動量に deltaTime を掛けていない
```
間違った結果:     2 秒間で 30fps 6m、120fps 24m (期待値はどちらも 12m)
ツール:           Update にトレースポイント "{Time.frameCount} {transform.position.z}" → 1 回に 0.1m ずつ、1 秒に Update 30 回/120 回
本当の原因:       Speed は「フレームあたり 0.1m」。1 秒に進む距離 = 0.1 × fps。60fps のときだけ 6m/s になる
```
**修正:**
```csharp
public float Speed = 6f;   // 1 秒に 6m
transform.position += Vector3.forward * Speed * Time.deltaTime;
```
**教訓:** Update で変える量は「毎秒の値 × `Time.deltaTime`」でなければなりません。単位 (MetersPerSecond) を名前やコメントに残しておくと、フレームあたりの値が紛れ込んだときに目立ちます。固定間隔が必要な計算は FixedUpdate に回します。

## ラボ 13 — 毎フレーム Lerp(a, b, 0.1f)
```
間違った結果:     0.5 秒後の残り距離 30fps 2.06m、120fps 0.02m (期待値はどちらも 0.42m)
ツール:           LateUpdate にトレースポイント → 残り距離がフレームごとに 0.9 倍。30fps 0.5 秒 = 15 フレーム → 0.9^15、120fps = 60 フレーム → 0.9^60
本当の原因:       比率を固定した Lerp は「フレーム数」についてのべき乗だ。deltaTime を単純に掛けても (0.1f * dt * 60) 一次近似にすぎず合わない
```
**修正:**
```csharp
public float Sharpness = 6.32f;   // = -ln(1 - 0.1) * 60 : 60fps で 0.1 と同じ感触
transform.position = Vector3.Lerp(transform.position, Target, 1f - Mathf.Exp(-Sharpness * Time.deltaTime));
```
**教訓:** 「毎フレーム、残りの何 %」は指数減衰です。1 フレームの比率を `1 - e^(-k·dt)` にすれば、フレームをどう分割しても 1 秒後の結果は同じになります (`e^(-k·a) · e^(-k·b) = e^(-k·(a+b))`)。`Vector3.SmoothDamp`、`Mathf.SmoothDamp` もフレームに依存しません (カーブの形は違います)。

## ラボ 14 — Update で Time.fixedDeltaTime を掛ける
```
間違った結果:     1.5 秒間で 30fps 18、120fps 72 (期待値はどちらも 30)
ツール:           Update のウォッチ式: Time.fixedDeltaTime は fps に関係なく 0.02、Time.deltaTime は 0.0333 / 0.0083
本当の原因:       fixedDeltaTime は FixedUpdate 間隔の設定値 (定数) だ。Update で掛けると Update 回数 × 0.02 になる。50fps のときだけ合う
```
**修正:**
```csharp
Value += RegenPerSecond * Time.deltaTime;
```
**教訓:** どこでも `Time.deltaTime` を使えば大丈夫です。FixedUpdate の中で読めば Unity が自動的に `fixedDeltaTime` を返してくれます。`fixedDeltaTime` を直接使うのは FixedUpdate の間隔そのものを変えるときだけです。

## ラボ 15 — timeScale 0 で deltaTime と WaitForSeconds を使う
```
間違った結果:     一時停止中にメニューが入ってきた割合 0 (期待値 1)、カウントダウン後の timeScale 0 (期待値 1)
ツール:           PauseMenu.Update にブレークポイント → 一時停止中も呼ばれる。Time.deltaTime == 0、Time.unscaledDeltaTime == 0.016
                 Countdown の yield の次の行にブレークポイント → 永遠に来ない
本当の原因:       timeScale = 0 は Update を止めず、ゲーム時間だけを止める。deltaTime が 0 なのでメニューが動かず、WaitForSeconds はゲーム時間で待つので終わらない
```
**修正:**
```csharp
Shown = Mathf.MoveTowards(Shown, target, Time.unscaledDeltaTime / SlideSeconds);
...
yield return new WaitForSecondsRealtime(1f);
```
**教訓:** 一時停止中も動くべきもの (メニュー、カウントダウン、ポーズ画面のアニメーション・パーティクル) は実時間を使います: `unscaledDeltaTime`、`WaitForSecondsRealtime`、Animator の 更新モード (Update Mode) = スケールされていない時間 (Unscaled Time)、パーティクルの Use Unscaled Time。逆にゲーム内のものは、あえて `deltaTime` を使って一緒に止まるようにします。「一時停止中に何が動くべきか」を先に決めましょう。

---

# 第3部: トランスフォームと空間

## ラボ 16 — ワールド座標を localPosition に入れた
```
間違った結果:   乗った直後の商人と席の距離 7.211m (荷車が進んだ後も 7.211m)
止まった位置:   Cart.Board の最後 → ウォッチ式 Seat.position (6, 1, 3.5)、rider.localPosition (6, 1, 3.5)、rider.position (12, 1, 7.5)
最初にずれた所: rider.position - Seat.position = (6, 0, 4) = 荷車の位置。ワールド座標に親の位置がもう一度足された
本当の原因:     Seat.position はワールド座標なのに、親 (荷車) 基準の localPosition に入れた。荷車が原点にあるときだけ 2 つが同じになる
```
**修正:**
```csharp
public void Board(Transform rider)
{
    rider.SetParent(transform);
    rider.position = Seat.position;   // ワールド = ワールド
}
```
**教訓:** `position`/`rotation`/`lossyScale` はワールド、`localPosition`/`localRotation`/`localScale` は親基準です。インスペクターのトランスフォーム (Transform) が表示しているのは **ローカル** の値です。親が原点・回転なし・スケール 1 だと 2 つが同じなのでテストで隠れます。親を動かし、回してからテストします。席そのものを親にして (`SetParent(Seat, false)` + `localPosition = Vector3.zero`) 座標系を 1 つに減らしても構いません。

## ラボ 17 — SetParent の worldPositionStays を逆に使った
```
間違った結果:   拾った直後の剣と手の距離 3.496m、向きの差 96.7 度。置いた剣は手があった場所から 4.258m、見た目の大きさ 0.5 (期待 2)
止まった位置:   Hand.PickUp の SetParent の次 → item.position はそのまま (床)、item.localPosition だけが変わる
              Hand.Drop の SetParent の次 → item.localPosition はそのまま、item.position が原点付近に、lossyScale 1 → 0.5
本当の原因:     SetParent(p) は worldPositionStays = true: ワールドの値を保つようにローカルの値を計算し直す (剣が床に残る)
              SetParent(null, false) はローカルの値を保つ: 手を基準にしたローカルの値がワールドの値になり、親 (騎士の 2 倍 × 手) のスケールが抜ける
```
**修正:**
```csharp
public void PickUp(Transform item)
{
    item.SetParent(transform, false);   // 手のローカルの場所にはめる
    item.localPosition = Vector3.zero;
    item.localRotation = Quaternion.identity;
    Held = item;
}

public void Drop()
{
    Held.SetParent(null);               // 見えている位置・回転・大きさのままにする
    Held = null;
}
```
**教訓:** 「付けるときはローカルの場所にはめ (`false` + ローカルの値を指定)、外すときは見えるままにする (`true`)」。スケールが 1 でない親の下に入る・出るとき、`worldPositionStays = true` は見た目の大きさを保つために localScale を変えます。シーンのルートに切り離したオブジェクトは親と一緒には破棄されないので、自分で片付ける必要があります (このラボの `OnDestroy`)。

## ラボ 18 — eulerAngles を読んで足し、また書き込んだ
```
間違った結果:   120 フレーム (180 度) 後の羽根の向きの差 90 度。羽根が 90 度付近で震える
止まった位置:   Windmill.Update に条件付きブレークポイント transform.eulerAngles.x > 85
              → 88.5 に 1.5 を足して 90 を入れた次のフレーム、91.5 を入れたのに読んだ値は (88.5, 180, 180)
本当の原因:     eulerAngles はクォータニオンから毎回計算し直した値で、X は 90 を超えない値 (0~90 または 270~360) で返ってくる。X が 90 を超えると同じ回転 (180-x, y+180, z+180) として読まれ、
              足し直した値が 90 のほうへ戻る (X = ±90 は Y と Z が同じ軸になるジンバルロックの場所)
```
**修正:**
```csharp
void Update()
{
    transform.Rotate(DegreesPerSecond * Time.deltaTime, 0f, 0f);   // rotation *= Quaternion.Euler(...)
}
```
**教訓:** 回転を積み重ねるときは、オイラー値を読んで書き換えるのではなくクォータニオンを掛けます (`Rotate`、`rotation * Quaternion.AngleAxis(...)`)。角度 1 つだけを動かす演出なら、角度を自分の変数で持って毎回 `baseRotation * Quaternion.Euler(angle, 0, 0)` で作ります。インスペクターの回転 (Rotation) 欄はエディターが別に覚えている値なので、スクリプトの `eulerAngles` と違うことがあります。

## ラボ 19 — クォータニオンの積の順序を逆にした
```
間違った結果:   標的 1 (軸 1 つだけが違う) は 0 度、標的 2 (横の高い所) は 9.131 度ずれる
止まった位置:   Cannon.AimAt の最後 → yaw、pitch は正しい。ウォッチ式 Quaternion.Euler(0, yaw, 0) * Quaternion.Euler(pitch, 0, 0) * Vector3.forward は direction と同じ
本当の原因:     A * B は B を先に適用する。Euler(pitch) * Euler(yaw) は先に左右に回してからワールドの X 軸で上げるので、横を向いた砲身が上ではなく横に傾く
              pitch か yaw が 0 なら順序は関係ないので、正面・同じ高さの標的では隠れる
```
**修正:**
```csharp
transform.rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(pitch, 0f, 0f);
// 同じ結果: Quaternion.Euler(pitch, yaw, 0f)  (Unity の Euler は Z → X → Y の順 = Ry * Rx * Rz)
// 角度が要らなければ: Quaternion.LookRotation(direction)
```
**教訓:** クォータニオンの積は交換できません。`親側 (ワールド) の回転 * 子側 (ローカル) の回転` と読むと順序を合わせやすくなります。`Quaternion.Euler(x, y, z)` は Z、X、Y の順に回した結果なので、ほかのエンジン・ツール (例: XYZ の順) から持ってきた角度をそのまま入れてはいけません。

## ラボ 20 — Vector3.forward で前へ動かした
```
間違った結果:   矢が飛んだ方向と敵の方向の差 123.69 度 (敵は南東なのに矢は北、+Z)
止まった位置:   Arrow.Update → ウォッチ式 Vector3.forward (0, 0, 1)、transform.forward (0.83, 0, -0.55)
本当の原因:     Vector3.forward は定数のワールド +Z。矢の回転は正しく入っているが、移動に使っていなかった
```
**修正:**
```csharp
transform.position += transform.forward * Speed * Time.deltaTime;
// または transform.Translate(0f, 0f, Speed * Time.deltaTime);   // 既定は Space.Self
```
**教訓:** `Vector3.forward/right/up` はワールドの軸、`transform.forward/right/up` はそのオブジェクトの軸です。`Translate` の既定は自分の軸 (Space.Self) で、ワールドで動かすなら `Space.World`。シーンビューの ローカル / グローバル (Local / Global) トグルをローカルにすると、ギズモがオブジェクトの軸を表示します。敵をいつも +Z 側に置いてテストすると 2 つが同じなので隠れます。

## ラボ 21 — 方向に TransformPoint、位置に InverseTransformDirection
```
間違った結果:   サーチライトの向きの差 81.994 度。ドローンの左前の侵入者を右と判定 (隠しケースでは右の侵入者を左と判定)
止まった位置:   Drone.BeamDirection → ウォッチ式 transform.TransformPoint(LocalBeam) はドローンの位置付近の点、TransformDirection(LocalBeam) が欲しい方向
              Drone.IsOnLeft → InverseTransformDirection(worldPoint).x には、ドローンの位置を回した値が混ざっている
本当の原因:     Point は移動 (位置) まで適用し、Direction は回転だけを適用する。方向と位置に逆の関数を使った。ドローンが原点にあれば 2 つは同じ
```
**修正:**
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
**教訓:** 位置は `TransformPoint`/`InverseTransformPoint` (回転・スケール・移動)、方向は `TransformDirection` (回転だけ、長さは保つ)、大きさを持つベクトル (オフセット、速度) は `TransformVector` (回転・スケール、移動なし)。名前に `worldPoint`、`localDirection` のように何なのかを残しておくと、組み合わせの合わない呼び出しが見えます。

## ラボ 22 — スケールが均一でない親の下にローカルの値で置いた
```
間違った結果:   罠が期待する位置から 33.003m、見た目の大きさと 1m の立方体の差 11.196 (lossyScale (12, 0.4, 3))、底が橋の上面より 0.12m 下
止まった位置:   Bridge.PlaceTrap の return → trap.localPosition (3, 0.7, 0)、trap.position は橋の中心から x 方向に 36m、trap.lossyScale (12, 0.4, 3)
本当の原因:     子の localPosition と localScale は、親のスケール (12, 0.4, 3) を経由してワールドになる。メートル単位の値をそのままローカルに入れた
```
**修正:**
```csharp
trap.SetParent(transform, false);
trap.position = transform.position + transform.rotation * new Vector3(offset, transform.localScale.y * 0.5f + 0.5f, 0f);
trap.localScale = new Vector3(1f / transform.lossyScale.x, 1f / transform.lossyScale.y, 1f / transform.lossyScale.z);
```
**教訓:** 引き伸ばしたオブジェクト (スケール ≠ 1) の子は、位置もスケールも親のスケールが掛かります。スケールが均一でない親の下で子を回すと形がゆがみ (そのため `lossyScale` は近似値)、コライダーもおかしくなります。根本的には、ルート (スケール 1) の下に **見た目だけを受け持つ子** を置いて、その子だけを伸ばします。

## ラボ 23 — ピボットがメッシュの真ん中にあるモデル
```
間違った結果:   開いた後のちょうつがい側の端がちょうつがいから 2.121m (= 1.5 × √2)。石像の足が床より 1m 下
止まった位置:   Gate.Open → Rotate は transform.position (扉の真ん中) を軸に回る。ウォッチ式 gateMesh.bounds: center (0, 0, 0)、min (-1.5, -2, -0.15)
              Plaza.PlaceOnFloor → statueMesh.bounds.min.y = -1
ツール:         シーンビューの ピボット / 中心 (Pivot / Center) トグル: このモデルでは 2 つの位置が同じ (ピボットが真ん中)
本当の原因:     Transform の回転・位置はピボット (メッシュの原点) 基準。コードはピボットがちょうつがい・足にあると仮定していたが、モデルは真ん中にピボットがある
```
**修正:**
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
**教訓:** モデルのピボットがどこにあるかは、コードではなくアセットが決めます。扉・レバー・石像のように「どの点を基準に」扱うモデルは、モデリングツールで原点を合わせるか、空の親 (ちょうつがいの位置、足の位置) を作ってモデルをその子にします。グラフィックスデバイスがないときや無効なレンダラーの `Renderer.bounds` は空のことがあるので、判定にはメッシュの bounds を使います。

---

# 第4部: コンテナ

## ラボ 24 — 定数の長さで配列を回し、外から来た番号を確認しない
```
症状:             ホットバーを作った瞬間に IndexOutOfRangeException (Hotbar.Build)
ツール:           例外設定で IndexOutOfRangeException をスローされたときに中断 → ウォッチ式 i == 3、Slots.Length == 3
最初にずれた所:   ループ回数は SlotCount (4)、データはインスペクターで 3 枠に減らされた Slots
本当の原因:       ① ループをデータの長さではなく定数で回した ② Select(key) は外から来た key を確認せずにインデックスとして使った (空のショートカットキー 4 → ArgumentOutOfRangeException)
```
**修正:**
```csharp
for (int i = 0; i < Slots.Length; ++i) { ... }

public string Select(int key)
{
    int index = key - 1;
    return index >= 0 && index < Labels.Count ? Labels[index] : null;
}
```
**教訓:** インスペクターに表示される配列のサイズはコードが決めるものではありません。ループは `Length`/`Count` で回し、入力・ネットワーク・セーブファイルから来た番号は範囲を確認してから使います。サイズがルールなら `OnValidate` で強制します。配列は `IndexOutOfRangeException`、`List` は `ArgumentOutOfRangeException` なので、メッセージを見ただけでどちらかわかります。

## ラボ 25 — foreach の途中で別のスクリプトがリストから自分を削除する
```
症状:             空爆で HP 10 の敵が倒れた瞬間に InvalidOperationException: Collection was modified
ツール:           Enemy.Die の Members.Remove にブレークポイント → コールスタック: Die ← TakeDamage ← Squad.Airstrike(foreach)
本当の原因:       Squad は Members を foreach で回していて、その中で呼ばれた Enemy.Die が同じ Members を変更した。次の MoveNext がバージョンの変化を見て例外を投げる
```
**修正 (いずれか 1 つ):**
```csharp
foreach (Enemy enemy in Members.ToArray()) { enemy.TakeDamage(damage); }        // コピーを回す
for (int i = Members.Count - 1; i >= 0; --i) { Members[i].TakeDamage(damage); }  // 後ろから回す
```
**教訓:** ループ中に呼ばれるコード (イベント、コールバック、他のコンポーネントの関数) が同じリストを変更しうるなら、コピーか逆順で回します。前からインデックスで回しながら削除すると、例外なしで **1 つずつ飛ばす** もっと静かなバグになります。`Destroy` が呼ぶ `OnDestroy` はフレームの最後なので、この例外ではなくラボ 29 のような「破壊された要素が残る」問題になります。

## ラボ 26 — Unity のシリアライズが Dictionary と List<List<T>> を捨てる
```
間違った結果:     複製した店と読み込んだ店の potion の価格 -1、陳列棚 0 個。名前とゴールドは正常
ツール:           ラボの出力「保存された JSON: {"ShopName":"鍛冶屋","Gold":500}」— Prices、Shelves がない
                 インスペクターにも 2 つのフィールドが表示されない
本当の原因:       Instantiate と JsonUtility は Unity のシリアライズで値を移す。Dictionary とネストした List はシリアライズ対象ではないので、警告なしに抜け落ちる
```
**修正:**
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
**教訓:** インスペクターに表示されないフィールドは、プレハブ、シーンの保存、Instantiate、Undo、`JsonUtility` でも抜け落ちます。`Dictionary`、`T[,]`、`T[][]`、`List<List<T>>` は `[Serializable]` のラッパーや `ISerializationCallbackReceiver` でシリアライズ可能な形に変えます。コールバックはシリアライズ用のスレッドで呼ばれることがあるので、その中で Unity API を呼ばないでください。

## ラボ 27 — FindObjectsByType の順序をゲームの順序として使う
```
間違った結果:     3 つのマップすべてで間違う (2,1,0 や 1,3,0,2 のように。間違い方は実行環境ごとに違う)。ラボは置く順をどの単調な順序とも合わないように選んである。
                  実際のゲームでは「あるマップでは合う」という形で現れるバグだ
ツール:           LearnRoute の後にウォッチ式 Route → 各要素の Order と name (置いた順 A, B, C)
本当の原因:       FindObjectsSortMode.None は順序を保証しない。エンジン内部の順序がそのまま出てきて、その順序は置いた順とも実行環境とも無関係に変わる。たまに合うのが一番危険だ
```
**修正:**
```csharp
Route = FindObjectsByType<RoutePoint>(FindObjectsSortMode.None).OrderBy(p => p.Order).ToList();
```
**教訓:** `FindObjectsByType`、`GetComponents`、`GetComponentsInChildren`、`Dictionary` の列挙、イベントの呼び出し順は、ゲームのルール上の順序ではありません。意味のある順序はデータ (番号) として持って自分でソートするか、インスペクターの配列に順番に入れてシーンファイルに保存します。このバグは「エディターでは動くのにビルドでは順序が違う」という形で現れがちです。

## ラボ 28 — 使い回す結果バッファをそのまま返す
```
間違った結果:     Alpha の名簿 2 人 (期待値 3)、Alpha の名簿に Bravo の兵士。alphaRoster == bravoRoster が true
ツール:           ウォッチ式で参照を比較。alphaRoster の行の後 Count 3 → bravoRoster の行の後 Count 2 に変わる
本当の原因:       GetComponentsInChildren(List) は渡した List を空にしてから詰める。SoldiersOf は static バッファそのものを返すので、2 回の呼び出しが同じ List を受け取る
```
**修正:**
```csharp
unit.GetComponentsInChildren(results);
return new List<Soldier>(results);
// または: public static void SoldiersOf(GameObject unit, List<Soldier> into) { unit.GetComponentsInChildren(into); }
```
**教訓:** アロケーションを減らす API (`GetComponents(List)`、`GetComponentsInChildren(List)`、`Physics.RaycastNonAlloc`、`OverlapSphereNonAlloc`) のバッファは **次の呼び出しで上書きされます**。バッファは関数の中だけで使い、外に出すときはコピーするか、呼び出す側にバッファを持たせます。

## ラボ 29 — 破壊されたオブジェクトが Dictionary のキーとして残る
```
間違った結果:     ヘイトリスト 3 人 (期待値 2)。AttackTop で MissingReferenceException
ツール:           AttackTop にブレークポイント → ウォッチ式 top == null は true、(object)top == null は false
本当の原因:       Destroy はネイティブオブジェクトだけを消す。Dictionary は C# オブジェクトをキーとして掴んでいるので項目とスコアがそのまま残り、スコア 1 位なので選ばれ続ける
```
**修正:**
```csharp
void Prune()
{
    foreach (Enemy dead in Threat.Keys.Where(e => e == null).ToList()) { Threat.Remove(dead); }
}
public int ThreatCount { get { Prune(); return Threat.Count; } }
public void AttackTop() { Prune(); ... }
```
(または `Enemy.OnDestroy` でイベントを発行し、`Boss` が受け取って `Threat.Remove(enemy)`)

**教訓:** `UnityEngine.Object` を入れたコンテナ (`Dictionary` のキー、`HashSet`、`List`) は `Destroy` を知りません。取り出すときに `== null` (Unity の再定義) で除外して片付けるか、死ぬときに通知して削除します。片付けていない項目は C# オブジェクトを掴んだままなので、小さなリークにもなります。第1部のラボ 1・8 の fake null がコンテナの中に隠れた形です。

## ラボ 30 — 長さが変わった NativeList の AsArray をキャッシュしておく
```
症状:             出発点を読む行で ObjectDisposedException: NativeList ... has been deallocated
ツール:           Step にブレークポイント、ウォッチ式 view[0] → 最初の Step (最初の Add) を通った直後から読めない。Capacity 4 を超える前なのに
本当の原因:       Awake で作った view はその瞬間のメモリを指している (長さも 1 のまま固まっている)。メモリが実際に移されるのは Capacity を超える Add のときだが、
                  安全チェックは長さを変えるすべての書き込み (Add のたび) でリストのセカンダリバージョンを上げ、それより前に作った view を無効にする。AddNoResize だけは例外
```
**修正:**
```csharp
public Vector3 StartPoint() { return Points[0]; }   // view をキャッシュしない。必要なら使うたびに Points.AsArray()
```
**教訓:** `NativeList.AsArray()`、`List<T>` から得た `Span`、`NativeArray.GetSubArray` はコピーではなく **今のメモリの姿** です。長さを変える書き込みをまたいで持ち続けません (Capacity を十分に確保しても無駄です)。ジョブに渡すときはスケジュールの直前に作るか、`AsDeferredJobArray()` を使います。エディターの安全チェックが例外で知らせてくれますが、リリースビルドでは解放されたメモリを黙って読みます。

---

# 第5部: 不正な参照とネイティブメモリ

## ラボ 31 — NativeArray を Dispose しない
```
間違った結果:     目標を 5 回変えた後に生きているバッファ 5 個 (期待値 1)、巡回兵を消した後も 5 個 (期待値 0)。例外はない
ツール:           再生を止めるとコンソールに "Leak Detected : Persistent allocates 5 individual allocations"
                 環境設定 → Jobs → Leak Detection Level → Enabled With Stack Trace にして再実行すると、確保した場所が PathCache.Rebuild と表示される
最初にずれた所:   2 回目の Rebuild。Points = new ... が最初のバッファのアドレスを上書きして、誰もそのバッファを解放できなくなった
本当の原因:       NativeArray は GC が片付けない。上書きする前にも、コンポーネントが消えるときにも Dispose がない
```
**修正:**
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
**教訓:** `new NativeArray(..., Allocator.Persistent)` は `new` より `malloc` に近いものです。作った場所と対になる `Dispose` を一緒に書きます (フィールドなら `OnDestroy`/`OnDisable`)。リークは例外なしでメモリだけが増えるので、Leak Detection のメッセージと Memory Profiler で探します。`IsCreated` は「この変数が」バッファを持っているかどうかにすぎません。構造体のコピーは元が Dispose された後も `IsCreated == true` で、アクセスすると安全システムが `ObjectDisposedException` を投げます (ラボはこれで生きているバッファを数えています)。

## ラボ 32 — 前のジョブだけ Complete して後のジョブの出力を読む
```
止まった所:       Watchtower.Scan の count[0]
例外:             InvalidOperationException: The previously scheduled job CountInRangeJob writes to the NativeArray CountInRangeJob.Count.
                 You must call JobHandle.Complete() on the job CountInRangeJob, before you can read from the NativeArray safely.
おかしな値:       measured.Complete() は呼んだ。なのにメッセージのジョブは MeasureDistanceJob ではなく CountInRangeJob
本当の原因:       tally は measured に依存している (measured が終わってから tally が始まる)。逆方向ではない。measured を待っても tally はまだ予約状態かもしれない
```
**修正:**
```csharp
running = tally.Schedule(measured);
running.Complete();   // チェーンの末尾を待つ (前のジョブも一緒に終わっている)
EnemiesInRange = count[0];
```
**教訓:** メインスレッドがバッファを読む・書く・Dispose するには、そのバッファを使う **すべての** ジョブのハンドルが Complete されている必要があります。実際にジョブが終わっているかどうかに関係なく、安全システムが阻止します (だからタイミング次第でたまに起きるバグではなく、常に起きるバグになります。ありがたいことです)。依存チェーンは末尾のハンドル 1 つを Complete すればよく、複数の枝は `JobHandle.CombineDependencies` でまとめます。メッセージは **ジョブ名.フィールド名** まで教えてくれるので、最後まで読みましょう。

## ラボ 33 — Allocator.Temp のバッファを次のフレームで読む
```
止まった所:       SpawnDirector.Update の plan[next] (StartWave の次のフレーム、next == 0)
例外:             ObjectDisposedException: The NativeArray has been deallocated, it is not allowed to access it
おかしな値:       Dispose を呼んだ所がないのに "deallocated"
本当の原因:       Allocator.Temp はそのフレームの中でだけ有効だ。フレームが終わると Unity が Temp メモリを丸ごと巻き戻す。フィールドに入れて次のフレームで読むと、もう存在しない
```
**修正:**
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
(ジョブに渡さないデータなら、単に `int[]` に変えるのも正しい答えです。)
**教訓:** Allocator は寿命の約束です。Temp = このフレーム、関数内のローカル変数用。TempJob = ジョブに渡して 4 フレーム以内に Dispose。Persistent = 自分で Dispose するまで。フィールドに入れた瞬間に Temp は間違った選択です。エディターでは安全システムがすぐに例外を投げてくれますが、リリースビルドには安全チェックがないので、他のコードが上書きしたメモリを読んで **見当違いの匹数** が出ることがあります。

## ラボ 34 — Domain Reload を切った再生で静的な状態が残る
```
間違った結果:     2 回目の最初のウェーブ 4 (期待値 1)、レーダーの敵 4 (期待値 2)、radar.Nearest() で MissingReferenceException
ツール:           SimulateEnterPlayMode() の次の行にブレークポイント → ウォッチ式 Enemy.Alive: Count 4 なのに先頭の 2 つは "null" (破壊された 1 回目の敵)
                 WaveDirector.Wave == 3: 1 回目の値のまま
本当の原因:       Domain Reload を切ると再生時に C# を再読み込みしないので、静的フィールドの初期値 (= 0、= new List) が再適用されない。
                 1 回目の終了時に生きていた敵は Die() を通らずにシーンと一緒に破壊され、リストに残った
```
**修正:**
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
(おまけに `void OnDestroy() { Alive.Remove(this); }` を置けば、シーン切り替えで消えた敵もリストから外れます。ただしそれだけでは Wave が引き継がれるバグが残ります。)
**教訓:** *Reload Scene only* (Domain Reload を切る) は再生を数秒ずつ速くしてくれますが、その代わりに **静的フィールド、静的イベント、シングルトンの Instance** を自分で元に戻す必要があります。Unity のドキュメントが推奨するのは `SubsystemRegistration` タイミングの `[RuntimeInitializeOnLoadMethod]` です。静的イベント (`static event Action`) も同じです: 購読者が前回の再生の破壊されたオブジェクトなので、第1部の MissingReferenceException が見当違いの場所で起きます。ビルドしたゲームでは毎回新しく始まるので、「エディターでだけ起きるバグ」として報告されがちです。

---

# 第6部: 物理

## ラボ 35 — 高速な Rigidbody が Discrete の衝突判定で壁をトンネリングする
```
間違った結果:     壁に当たった回数 0 (期待値 1)、弾丸 x = 60 (壁 x = 10)
ツール:           FixedUpdate にトレースポイント "{transform.position.x}" → 0, 6, 12, 18 ... 壁 (9.9 〜 10.1) の中にいたステップがない
                 Debug.DrawRay でステップごとの移動距離 6m を描くと、壁を飛び越えているのが見える
本当の原因:       デフォルトの Discrete (非連続的) はステップが終わった位置でしか重なりを見ない。300m/s × 0.02 秒 = 6m で 0.2m の壁を一度に越えた
```
**修正:**
```csharp
rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
```
**教訓:** 「1 ステップで進む距離 > ぶつかる物の厚さ」ならトンネリングを疑います。速いものだけ Continuous 系 (連続的かつ動的など) をオンにし (高コストです)、非常に多い弾丸は Rigidbody の代わりにステップごとのレイキャストで判定します。壁を厚くしたり fixedDeltaTime を小さくしたりする方法は、速度がさらに上がるとまた問題になります。

## ラボ 36 — 両方とも Rigidbody がなくトリガーメッセージが発生しない
```
間違った結果:     罠の発動 0 回、HP 100 のまま
ツール:           OnTriggerEnter のブレークポイントが一度も止まらない →「条件が間違っている」のではなく「呼ばれていない」
                 物理演算デバッガー (Physics Debugger) で Player と SpikeTrap がどちらも Static (静的) と表示される
本当の原因:       Rigidbody のないコライダーは静的コライダーだ。静的コライダー同士は重なっても衝突・トリガーの判定をしない
```
**修正:**
```csharp
void Awake()
{
    gameObject.AddComponent<Rigidbody>().isKinematic = true;   // コードで動かす側に Kinematic Rigidbody
}
```
**教訓:** `OnTriggerEnter`/`OnCollisionEnter` が呼ばれないなら、① どちらかに Rigidbody (または CharacterController) があるか ② レイヤーの衝突表 (Layer Collision Matrix) で 2 つのレイヤーがオンになっているか ③ トリガー同士なら片方に Rigidbody があるか ④ コライダーが無効になっていないか、を順に確認します。動くコライダーに Rigidbody を付けるのはパフォーマンスの面でも有利です (静的コライダーを動かすと物理シーンを作り直します)。

## ラボ 37 — レイヤー番号をレイヤーマスクとして渡す
```
間違った結果:     タレットが当てたもの Glass (期待値 Enemy)
ツール:           ウォッチ式 Convert.ToString(EnemyLayer, 2) == "1000" → 3 番ビット。ガラス窓が 3 番レイヤー
                 物理演算デバッガー → Queries でレイがガラス窓で止まる
本当の原因:       Raycast の layerMask はビットマスクだ。8 は「8 番レイヤー」ではなく「3 番レイヤーだけ」(10 は 1 番と 3 番)
```
**修正:**
```csharp
Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, Range, 1 << EnemyLayer)
// さらに良い方法: public LayerMask EnemyMask;  → インスペクターで選ぶ
```
**教訓:** `gameObject.layer` は番号 (0〜31)、Physics のクエリ・`Camera.cullingMask`・`LayerMask` はマスク (ビットの集まり) です。どちらも int なのでコンパイラーは防いでくれません。数値の代わりに `LayerMask` フィールドや `LayerMask.GetMask("Enemy")` を使えば、この混同はなくなります。

## ラボ 38 — Interpolate をオンにした Rigidbody を Update で transform で動かす
```
間違った結果:     足場が 1.5 秒間で 1.03m (期待値 3m)。箱は 0m
ツール:           Update にトレースポイント "{Time.frameCount} {transform.position.x}" → 足した分だけ増えて、次のフレームには元に戻っている
                 transform の移動を FixedUpdate に移すと足場は 3m 進むが、箱はその場に残って落ちる
本当の原因:       ① 補間 (Interpolate) は毎フレーム transform を物理位置の間の補間値で上書きする。Update で変えた transform はほとんど消される
                 ② transform で動かした Kinematic は物理から見ると瞬間移動 (速度 0) なので、摩擦で箱を引っ張れない
```
**修正:**
```csharp
void FixedUpdate()
{
    rb.MovePosition(rb.position + Velocity * Time.fixedDeltaTime);
}
```
**教訓:** Rigidbody が付いたオブジェクトは transform ではなく Rigidbody で動かします。Kinematic は FixedUpdate で `MovePosition`/`MoveRotation`、Dynamic は `linearVelocity`/`AddForce`。Interpolate はこのように動かしたときに画面を滑らかにするオプションであって、transform で動かしたものを滑らかにするものではありません。

## ラボ 39 — transform を動かした同じフレームで物理クエリ
```
間違った結果:     戻した位置で拾ったもの null (期待値 Crate)、元の位置で拾ったもの Crate (期待値 null)
ツール:           ResetCrates の後のウォッチ式: transform.position == (0,0,4)、GetComponent<Collider>().bounds.center == (8,0,4)
本当の原因:       transform の変更は次の物理ステップの直前に物理シーンへ反映される。その間の Raycast は古い位置のコライダーを見る
                 (Physics.autoSyncTransforms をオンにするとクエリごとに同期するが、2018.3 から既定でオフ。クエリごとのコストが大きいので推奨されず、
                  必要な所で Physics.SyncTransforms() を 1 回呼ぶのが定石)
```
**修正:**
```csharp
for (int i = 0; i < Crates.Length; ++i) { Crates[i].position = StartPositions[i]; }
Physics.SyncTransforms();
```
**教訓:** 瞬間移動、レベルの初期化、オブジェクトプールから取り出して配置した直後の Raycast・Overlap は古い位置を見ます。同じフレームでクエリする必要があるなら `Physics.SyncTransforms()` を一度呼ぶか、Rigidbody があるなら `rb.position` で動かします。1 フレーム待つと「勝手に直る」バグなので、再現しにくいです。

---

# 第7部: サウンド

## ラボ 40 — オーディオデータをプリロード (Preload Audio Data) をオフにしたクリップを事前にロードしていない
```
間違った結果:   最初の一発を撃った瞬間 loadState == Unloaded。発射後、データがロードされるまで数フレーム
止めた位置:     Blaster.Fire → FireClip.loadState: Unloaded, preloadAudioData: false, loadInBackground: true
本当の原因:     インポート設定でオーディオデータをプリロードをオフにした。Resources.Load でクリップオブジェクトは作られたが、音のデータは「最初に再生するとき」にロードされる。
              バックグラウンドで実行 (Load In Background) がオンなので、再生はデータのロード後に先送りされる → マズルフラッシュより遅れる音。
```
**修正 (いずれか1つ):**
```csharp
// ① 装備するときに事前にロードする (メモリ節約のためにオフにした設定を活かす方法)
public void Equip(AudioClip fireClip)
{
    FireClip = fireClip;
    source = gameObject.AddComponent<AudioSource>();
    source.playOnAwake = false;
    FireClip.LoadAudioData();
}
```
② `blaster.wav` のインポート設定で **オーディオデータをプリロード** をオンにして「適用する」。
**教訓:** オーディオインポート設定 3 つの意味: **ロードタイプ** (Load Type、メモリ上にどんな形で置くか: 展開済み / 圧縮のまま / ストリーミング)、**オーディオデータをプリロード** (シーンやアセットのロード時にデータも一緒に)、**バックグラウンドで実行** (ロードを待たずに後回しにする)。オフにするとメモリを節約できる代わりに「いつロードするか」をコードが責任を持つ必要があります。バックグラウンドで実行をオフにすると、遅延の代わりにそのフレームがカクつきます (ヒッチ)。

## ラボ 41 — 実ボイス数の上限を超え、ボスのセリフの優先度が同じ
```
間違った結果:   boss.Voice.isPlaying == true なのに isVirtual == true
ツール:         プロファイラーのオーディオモジュール → 再生 41、実 32、仮想 9
本当の原因:     最大実音声数 (Max Real Voices) 32 < 同時に再生しようとする音 41。優先度がすべて 128 なので、「より小さく聞こえる」ボスのセリフ (音量 0.6) が仮想に押し出された。
```
**修正:**
```csharp
Voice.priority = 0;   // 0 が最も重要
```
(足音の側を `priority = 200` に下げても構いません。根本的には同じ音を何十個も同時に鳴らす必要はないので、足音の数を制限します。)
**教訓:** 「音が聞こえない」の大きな原因の 1 つが **同時再生数の上限** です。仮想ボイスは位置だけ計算し続けて音は出さず、空きができると続きから鳴ります。重要な音 (セリフ、UI、警報) は priority で保護します。

## ラボ 42 — AudioSource が付いたオブジェクトをすぐに Destroy
```
間違った結果:   樽は消え、0.2 秒後に爆発音を再生中の AudioSource が存在しない
ツール:         Explode の最後に Debug.Break() → 次のフレームのヒエラルキーで ExplosiveBarrel とその AudioSource が消えている
本当の原因:     音の寿命 = AudioSource の寿命 = ゲームオブジェクトの寿命。Destroy が再生中の音を切った。
```
**修正:**
```csharp
public void Explode()
{
    AudioSource.PlayClipAtPoint(ExplosionClip, transform.position);
    Destroy(gameObject);
}
```
**教訓:** 消えるもの (弾、敵、箱) が出す音は **別の寿命を持つ場所** から鳴らします: `PlayClipAtPoint` (一時的な "One shot audio" オブジェクト)、切り離した子 + `Destroy(go, clip.length)`、サウンドマネージャーの AudioSource プール。`PlayClipAtPoint` は AudioSource の設定 (ミキサーグループ、3D 設定) を指定できないので、大きなプロジェクトではプールが一般的です。

## ラボ 43 — 短い効果音を Play() で鳴らして長い音を切った
```
間違った結果:   1 秒後 Source.clip == grunt、ブリーフィングは再生されていない
止めた位置:     PlayHurt → F10 の前後: Source.clip briefing → grunt、Source.time 0.5 → 0
本当の原因:     AudioSource 1 つは clip を 1 つ再生する。clip を替えて Play() すると、再生中だったブリーフィングは止まる。
```
**修正:**
```csharp
public void PlayHurt(AudioClip grunt)
{
    Source.PlayOneShot(grunt);
}
```
**教訓:** `Play()` = このソースの「メイントラック」を最初から (制御可能: Stop、Pause、time)。`PlayOneShot()` = メイントラックに触れずに重ねて鳴らす (個別の制御は不可、`Stop()` すると一緒に止まる)。セリフや音楽のように制御が必要なものと効果音は、AudioSource を分けるのがすっきりします。

---

# 第8部: 画像 / レンダリング

## ラボ 44 — sharedMaterial を変更してアセット自体を変えてしまった
```
間違った結果:   隣の箱も赤、Crate.mat アセットも赤
止めた位置:     OnHoverEnter → body.sharedMaterial.GetInstanceID() がすべての箱で同じ = Crate.mat アセット
本当の原因:     sharedMaterial はアセットそのもの。同じマテリアルを使うすべてのレンダラーが変わり、エディターではアセットファイルにも保存されうる。
```
**修正:**
```csharp
public void OnHoverEnter()
{
    body.material.color = Color.red;   // このレンダラー専用のコピー "Crate (Instance)"
}

public void OnHoverExit()
{
    body.material.color = Color.white;
}
```
(コピーの後始末はラボ 45 を参照。色だけを変えるなら MaterialPropertyBlock のほうが適しています。)
**教訓:** `sharedMaterial`/`sharedMesh` = アセット (全員で共有)、`material`/`mesh` = 最初のアクセス時に複製した自分専用のもの。エディターで `sharedMaterial` を実行時に変更すると、再生を止めても元に戻りません。`git status` に `.mat` が出てきたらこのバグを疑いましょう。

## ラボ 45 — renderer.material の複製がリークする
```
間違った結果:   ウェーブごとに Material オブジェクト +100、箱をすべて消した後も +200
ツール:         Paint の前後の sharedMaterial.name: "Crate" → "Crate (Instance)"。Memory Profiler のスナップショット比較: Material "Crate (Instance)" 200 個
本当の原因:     renderer.material はマテリアルを複製する。複製はゲームオブジェクトを消しても一緒には消えない (シーンのアンロードや UnloadUnusedAssets まで残る)。
              さらに箱ごとにマテリアルが異なるのでバッチングも崩れる (フレームデバッガーでドローコール数を確認)。
```
**修正:**
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
(どうしても複製を使うなら `OnDestroy` で `Destroy(GetComponent<Renderer>().material)`。ただし `OnDestroy` で初めて `.material` にアクセスするとその場でまた複製されるので、Paint のときに受け取っておいた参照を破棄します。)
**教訓:** レンダラーごとに値だけが違うなら **MaterialPropertyBlock**。マテリアルを複製せずにレンダラー別の値を与えられます。参考: URP/HDRP の SRP Batcher は、MaterialPropertyBlock を使うとそのレンダラーをバッチングから外します。SRP のプロジェクトなら、マテリアルのバリエーションを事前に作っておくか、GPU インスタンシング / シェーダープロパティ (`[PerRendererData]`、`UNITY_INSTANCING_BUFFER`) を使います。

## ラボ 46 — 遅れて届いた非同期ロードの結果が再利用されたスロットを上書きする
```
間違った結果:   スロットのアイテムは potion、アイコンは icon_sword
止めた位置:     ラムダ texture => Icon = texture → 2 回止まる: 同じフレームに potion (キャッシュ、即時)、次のフレーム以降に sword (LoadAsync 完了)
期待 → 実際:   要求の順序 sword → potion、到着の順序 potion → sword
本当の原因:     コールバックは「到着したときのスロット」にアイコンを入れる。その間にスロットがポーションに再利用されたのに確認していなかった。
```
**修正:**
```csharp
public void Show(string itemId)
{
    ItemId = itemId;
    Icon = null;
    Loader.Request(itemId, texture =>
    {
        if (ItemId == itemId)   // まだそのアイテムを表示しているスロットか?
        {
            Icon = texture;
        }
    });
}
```
**教訓:** 非同期の結果は「要求したとき」ではなく「到着したとき」の世界に落ちてきます。到着したときに **まだ有効か** (同じアイテムか、オブジェクトは生きているか) を確認します。`ResourceRequest.completed` や Addressables の `Completed` は、すでにロード済みのアセットでも同じフレームに呼ばれないことがあります。キャッシュヒット (即時) とミス (後で) が混ざると順序が逆転します。

## ラボ 47 — Linear 色空間で色を SetVector で渡した
```
間違った結果:   旗のピクセル (255, 188, 137)、期待 (255, 128, 64)
記録:          0x80/255 = 0.502 → 188 = sRGB エンコード(0.502) ≈ 0.735。0x40 → 137 も同じカーブ。0x00 と 0xFF はカーブの両端なのでそのまま
ツール:         フレームデバッガー → TeamBanner のドローコール → _Color の値 (1, 0.502, 0.251): sRGB の値がリニアの値の場所にそのまま入っている
本当の原因:     色空間 (Color Space) = Linear。シェーダーはリニアの値を期待し、画面に書き込むときに sRGB にエンコードする。
              SetVector は数値をそのまま渡すので、sRGB の色 (企画書の hex) が変換されずに「リニアの値」として使われた → もう一度明るくなった。
```
**修正:**
```csharp
block.SetColor(ColorId, teamColor);   // Linear 色空間なら sRGB → リニアの変換をしてくれる
// または block.SetVector(ColorId, teamColor.linear);
```
**教訓:** 人が選んだ色 (カラーピッカー、hex、テクスチャの色) は sRGB、シェーダーの計算はリニアです。変換は `SetColor`/`Color.linear`/テクスチャの **sRGB (カラーテクスチャ)** のチェックが行ってくれます。法線マップ、マスク、データテクスチャは sRGB を **オフにする** 必要があります (オンにすると逆に値が暗くなります)。「0 と 255 は合っているのに中間だけが違う」ならガンマカーブを疑いましょう。

---

# 第9部: アニメーション

## ラボ 48 — Animator パラメーター名の大文字・小文字
```
間違った結果:   Attack ステートに入らない
コンソール:     ⚠ Parameter 'Hash 1203776827' does not exist.
アニメーター:   パラメーター タブの名前は "Attack"。コードは Animator.StringToHash("attack")
本当の原因:     パラメーター名は大文字・小文字を区別する。間違えると警告を出すだけで何もしない。
```
**修正:**
```csharp
static readonly int AttackId = Animator.StringToHash("Attack");
```
**教訓:** 文字列のパラメーターはコンパイラーがチェックしてくれません。名前は 1 か所に定数として置き、開始時に `animator.parameters` で必要な名前があるかを検査する習慣をつけましょう。警告フィルターをオフにしていると、このバグは何週間も隠れます。

## ラボ 49 — カリングモード = 完全に除外 (Cull Completely) なので画面外でイベントが消える
```
間違った結果:   2.1 秒間に足音 0 回、Renderer.isVisible == false
ツール:         インスペクターの Animator → カリングモード (Culling Mode): 完全に除外 (Cull Completely)。アニメーターウィンドウで Walk の進行バーが止まっている。
              シーンビューを警備兵の方に向けると進行バーが動き、Footstep のブレークポイントで止まる。
本当の原因:     完全に除外 (Cull Completely) は、レンダラーが 1 つも見えていないとアニメーションを丸ごと止める。アニメーションイベントも呼ばれない。
```
**修正:**
```csharp
GetComponent<Animator>().cullingMode = AnimatorCullingMode.CullUpdateTransforms;   // または AlwaysAnimate
```
**教訓:** 画面外でもゲームロジックがアニメーション (イベント、ルートモーション) に頼っているキャラクターに 完全に除外 (Cull Completely) を使ってはいけません。トランスフォーム更新を除外 (Cull Update Transforms) は、ステートマシンとイベントを維持しつつボーンの書き込みと IK を省略します。エディターでは **シーンビューのカメラも「見ている」** ことになるので、エディターでは動くのにビルドでは動かない (またはその逆の) バグとして報告されがちです。

## ラボ 50 — トランジションがイベントより先に終わる
```
間違った結果:   3 回振って Hit 0 回
ツール:         アニメーションウィンドウ: Swordsman_Swing の長さ 1 秒、Hit イベント 0.9 秒
              アニメーターウィンドウ: Swing → Idle トランジション 終了時間あり (Has Exit Time) ✔、終了時間 (Exit Time) 0.7、遷移間隔 (Transition Duration) 0.1 (Fixed Duration オフ = クリップ長の 10%)
最初にずれた所: Swing は 0.7 で抜け始め 0.8 で完全に終わる。0.9 のイベントは再生されない区間にある
本当の原因:     アニメーションアセット。イベントがトランジションの後ろにある。
```
**修正 (いずれか1つ):** アニメーターウィンドウで Swing → Idle トランジションの **終了時間を 0.95** に (イベントの後で遷移し始める)。またはアニメーションウィンドウで **Hit イベントを 0.5 秒** 付近 (刃が実際に当たる位置) に移動する。
**教訓:** トランジションの途中では 2 つのステートのクリップが一緒に再生されて両方のイベントが呼ばれますが、トランジションが **終わった後** は元のクリップはそれ以上進みません。判定用のイベントはトランジション開始前に置きます。被弾や回避のような割り込みで攻撃が中断されたときに判定が抜けるのも同じ原理で、それは意図した動作の場合もあります (企画と決めましょう)。

## ラボ 51 — アニメーションの結果を Update で読んでいる
```
間違った結果:   ほぼすべてのフレームで手と松明がずれる
ツール:         Update と LateUpdate のトレースポイント → 同じフレームなのに手の x が違う
              プロファイラーのタイムライン: Update.ScriptRunBehaviourUpdate → PreLateUpdate.DirectorUpdateAnimationBegin/End → PreLateUpdate.ScriptRunBehaviourLateUpdate
本当の原因:     Animator は Update の後、LateUpdate の前にボーン (Transform) を書き込む。Update で読んだ手の位置は前のフレームのもの。
```
**修正:**
```csharp
void LateUpdate()
{
    transform.SetPositionAndRotation(Hand.position, Hand.rotation);
}
```
(最も簡単なのは松明を手の子にすること: `transform.SetParent(Hand, false)`。)
**教訓:** アニメーションが動かしたボーンを追うものすべて (武器の取り付け、カメラ、照準、IK 補正、エフェクトの位置) は LateUpdate で。1 フレームの遅れは画面上で「揺れ / ブレ」に見え、判定に使われれば本物のバグになります。

---

# 第10部: 並行処理とフリーズ

## ラボ 52 — 在庫の確認と減算の間に別のスレッドが割り込む
```
間違った結果:   販売数 2 (期待 1)、残り在庫 -1 (期待 0)、購入者リスト 2 人
ツール:         TryBuy の if の行にブレークポイント → スレッドウィンドウ: ワーカー 2 つが同じ行にいて、どちらも Stock == 1
              トレースポイント "{Environment.CurrentManagedThreadId} Stock={Stock}" → 両方のスレッドが 1 を見て通過
本当の原因:     「Stock > 0 を確認 → 決済 → Stock--」が 1 つのまとまりになっていない。決済が終わる前に 2 つ目の要求も確認を通過する
```
**修正:**
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
**教訓:** 共有状態を「確認して → 行動する」コードは、その間に割り込む隙がないよう 1 つのまとまりにします (`lock`)。決済のように時間のかかる処理を lock の中に置きたくなければ、lock の中で先に在庫を予約 (Stock--) し、失敗したら戻します。`Stock--` や `List.Add` 自体もアトミックではありません (`Interlocked.Decrement`、`ConcurrentQueue`)。Job System の安全システムは NativeContainer しか監視しないので、このような C# オブジェクトのレースは誰も教えてくれません。

## ラボ 53 — Task.Run の中で transform を使っている
```
間違った結果:   経路探索はワーカースレッドで行ったが、到着 false、位置もそのまま。コンソールは静か
ツール:         transform.position の行で F10 → 次の行に進まずに消える
              ウォッチ式 task.Status == Faulted、task.Exception.InnerException: UnityException "get_transform can only be called from the main thread"
本当の原因:     Unity API はメインスレッド専用。Task の中で起きた例外は Task に格納され、誰も await していないのでコンソールに出ない
```
**修正:**
```csharp
public async Task GoTo(Vector3 from, Vector3 goal)
{
    List<Vector3> path = await Task.Run(() => FindPath(from, goal));   // 計算だけワーカーで
    transform.position = path[path.Count - 1];                          // await の後はメインスレッド
    Arrived = true;
}
```
**教訓:** ワーカースレッドには計算だけを送り、Unity オブジェクトに触れる処理はメインスレッドに戻ってから行います。メインスレッドで開始した `await` は、UnitySynchronizationContext が次の行をメインスレッドに戻してくれます。返された Task を捨てると (fire-and-forget) 例外も一緒に捨てられるので、await するか `ContinueWith` で `Exception` をログに残します。Task の中では `transform.position` を **読むこと** もできません (値を事前に取り出して渡します)。

## ラボ 54 — BackgroundThreadAsync の後にメインスレッドへ戻っていない
```
症状:          ロード時に UnityException: get_gameObject can only be called from the main thread
ツール:         Load の最初の行と gameObject.name の行でウォッチ式 Thread.CurrentThread.ManagedThreadId → 1 から別の番号に変わった
本当の原因:     await Awaitable.BackgroundThreadAsync() の次の行からはスレッドプール。戻れと指示するまでずっとバックグラウンド
```
**修正:**
```csharp
await Awaitable.BackgroundThreadAsync();
SaveData data = Parse(json);
await Awaitable.MainThreadAsync();      // ここからメインスレッド
gameObject.name = data.Name;
transform.localScale = Vector3.one * data.Scale;
```
**教訓:** await の次の行のスレッドは「何を await したか」で決まります。普通の `await Task` (メインスレッドで開始) はメインに戻り、`Awaitable.BackgroundThreadAsync()` と `ConfigureAwait(false)` は戻りません。バックグラウンド区間は `MainThreadAsync()` で閉じる対を作ります。戻った後にはオブジェクトがすでに破棄されている可能性があるので、`if (this == null) return;` や `destroyCancellationToken` を使います。

## ラボ 55 — メインスレッドで .Result で待ってデッドロック
```
症状:          ショップを開いた瞬間に応答なし。CPU は 0% 付近 (デッドロック)
すべて中断:     Main Thread のコールスタック: ShopWindow.Open → (HangGuard.Wait =) Task.Wait → Monitor.Wait。眠っている
              タスク (Tasks) ウィンドウ / 並列スタック: LoadGoldAsync は「待機中」、await Task.Delay の次の行を実行する順番を待っている
最初にずれた所: Task.Delay は 200ms 後に終わった。ところが await の次の行 (return Gold) は UnitySynchronizationContext を通じて「メインスレッド、次のフレーム」に予約された
本当の原因:     メインスレッドはその Task が終わるのをブロックして待ち、Task はメインスレッドが空くのを待つ。互いに待っている
              (コンソールアプリには SynchronizationContext がなくスレッドプールで続行するので、問題なかった)
```
**修正:**
```csharp
public async void Open(SaveFile save)
{
    ShownGold = await save.LoadGoldAsync();
}
```
(コルーチンなら `var task = save.LoadGoldAsync(); while (!task.IsCompleted) yield return null; ShownGold = task.Result;`)
**教訓:** メインスレッドで `Task` をブロックして待たないこと (`.Result`、`.Wait()`、`.GetAwaiter().GetResult()`)。最後まで `await` でつなぎます。ライブラリ側に `ConfigureAwait(false)` を付けるとデッドロックは解けますが、`Open` はディスクを読む 200ms の間、依然としてゲームを止めます (このラボの 2 つ目の `[FAIL]`)。さらに `ConfigureAwait(false)` の次の行はワーカースレッドで動くので、そこで Unity API (`transform`、`GetComponent`) を呼ぶと "can only be called from the main thread" 例外が出ます。デッドロックは CPU 0%、無限ループは CPU 100% という特徴を覚えておきましょう。

## ラボ 56 — Update が変えてくれる値を yield なしの while で待っている
```
症状:          エレベーターを呼んだ瞬間に応答なし。タスクマネージャーで Unity が CPU 1 コアを使い切っている (無限ループ)
すべて中断:     スレッドウィンドウの Main Thread のコールスタックの一番上 = Passenger.RideTo の最初の while (HangGuard の例外スタックトレースも同じ場所)
おかしな値:     F10 を押し続けても Elevator.Floor が 5 から変わらない
最初にずれた所: Floor を変えるのは Elevator.Update。ところが Update は、このコルーチンが yield しないと次のフレームに呼ばれない
本当の原因:     コルーチンはスレッドではない。yield に出会うまではメインスレッドの普通の関数。while の中に yield がないので、このフレームで永遠に回る
```
**修正:**
```csharp
while (!Elevator.IsAt(Floor))
{
    HangGuard.Tick("Passenger.RideTo: 엘리베이터가 오기를 기다림");
    yield return null;   // 1 フレーム譲る → その間に Elevator.Update が回る
}
...
while (!Elevator.IsAt(destination))
{
    HangGuard.Tick("Passenger.RideTo: 목표 층에 닿기를 기다림");
    yield return null;
}
```
(または `yield return new WaitUntil(() => Elevator.IsAt(Floor));`)
**教訓:** 「ほかの何かが変えてくれる値」を待つループは、必ずその何かに順番を渡さなければなりません。Unity では `yield return null` (コルーチン)、`await Awaitable.NextFrameAsync()` (async)、あるいは Update で状態を確認する方法です。Update/Start/ボタンハンドラーの中の `while (!loaded) {}` も同じようにエディターを固まらせます。固まったときは **すべて中断 → スレッドウィンドウ → Main Thread** が最初の一手です。

## ラボ 57 — BFS に訪問チェックがない
```
症状:          道がつながっているときは正解 (5)。3-6 の扉をロックした後に応答なし、メモリ急増
ツール:         while の最初の行に条件付きブレークポイント queue.Count > 100 → 部屋が 8 つしかないのに止まる。queue を展開すると 1, 2, 4, 5 が何十回も
最初にずれた所: 0 → 1 → (0, 2, 4) で再び 0 を入れる。双方向の扉と 1-2-5-4 のループをたどって同じ部屋を延々と入れ直す
本当の原因:     目標に到達できるなら、BFS はいつか目標を取り出して return するのでバグが隠れていた。到達できないとキューが空にならない
```
**修正:**
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
**教訓:** グラフ (部屋、ウェイポイント、会話ノード、電線、アイテム合成) を巡回するコードは **訪問記録** が基本です。テストデータにループや途切れた道がないと永遠に表に出ず、レベルデザイナーが扉を 1 つロックした瞬間に爆発します。「フリーズ + メモリ増加」なら、無限ループの中で何かを積み上げているということです。再帰 DFS で同じミスをすると、フリーズの代わりに StackOverflowException (エディターのクラッシュ) として現れます。

## ラボ 58 — float の時間に小さなティックを累積
```
症状:          起動したばかりのサーバー (90 秒) は 128 ティックで正常。起動から 7 日 (604800 秒) のサーバーは応答なし
ツール:         while の中に条件付きブレークポイント Ticks > 1000 → ウォッチ式 SimTime == 604800、SimTime + TickSeconds == SimTime が true
本当の原因:     float の有効桁は 24 ビット。2^19 = 524288 以上では隣り合う float の間隔は 1/16。1/64 を足すと丸められて変わらない。
              SimTime < target が永遠に真
```
**修正 (いずれか1つ):**
```csharp
public double SimTime;                       // ① double: 1/64 は 2 の累乗なので double で正確
...
double target = SimTime + seconds;

// ② 整数のティックで数え、時間は計算で求める (最も堅牢)
public long TickCount;
public double SimTime => TickCount * (double)TickSeconds;
public void FastForward(float seconds)
{
    int steps = Mathf.RoundToInt(seconds / TickSeconds);
    for (int i = 0; i < steps; ++i) { ... ++TickCount; }
}
```
**教訓:** ループの終了条件を **float の累積結果** にかけないようにしましょう。大きな値に小さな値を足しても変わらないことがあり (フリーズ)、小さな値同士を足しても `0.1f` を 10 回足して `1f` にはなりません (`!=` で終わらせるとフリーズ)。Unity の `Time.time` も float なので、何日も動かし続けるサーバー・キオスク・ストリーミングビルドで同じことが起きます (`Time.timeAsDouble`、`Time.frameCount` を使います)。開発中には決して再現しない「長時間動かすと」系バグの代表です。

---

# 第11部: パフォーマンス (GC アロケーションと繰り返し検索)

## ラボ 59 — 値が変わらないのに毎フレーム文字列・LINQ・ボックス化
```
間違った結果:   値が変わらないフレームの GC アロケーションがフレームあたり約 5600 バイト、表示行 20 (期待 128 バイト以下)
ツール:         プロファイラー → CPU → ヒエラルキー → GC アロケーション列 → HealthBar.Update
              詳細プロファイル (Deep Profile): String.Concat、String.Format、Enumerable.Count、List<T>.GetEnumerator (ボックス化) がその下に見える
本当の原因:     結果が前のフレームと同じなのに毎フレーム新しい文字列を作り、LINQ の列挙子とインターフェースにボックス化された列挙子を作っている
```
**修正:**
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
        SquadText = Loc.F("생존 {0}명, 부대 HP {1}", alive, total);
    }
}
```
**教訓:** 毎フレーム回るコードでは「値が変わったときだけ作る」。文字列の連結・補間、LINQ、`IEnumerable<T>` で受け取った foreach (ボックス化)、ラムダがローカル変数をキャプチャするクロージャ、`new List` はすべてヒープアロケーションです。1 回あたり数十バイトでも、コンポーネント数 × 毎フレームなら GC を呼びます。UI テキストは TextMeshPro の `SetText(format, number)` がアロケーションなしで数値を入れてくれます。

## ラボ 60 — Update で GameObject.Find と FindObjectsByType
```
間違った結果:   標的探しの GC アロケーションがフレームあたり約 98KB (期待 256 バイト以下)
ツール:         プロファイラー → ヒエラルキー → Turret.Update の下に GameObject.Find、FindObjectsByType: 呼び出し (Calls) 40、時間 (ミリ秒) がオブジェクト数に比例
本当の原因:     タレット 40 基が毎フレームシーン全体をなめている。FindObjectsByType は結果を新しい配列で返す (毎回アロケーション)
```
**修正:**
```csharp
public class Enemy : MonoBehaviour
{
    public static readonly List<Enemy> All = new List<Enemy>();
    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }
}

void Start() { Player = GameObject.Find("Player").transform; }   // 1 回だけ
void Update() { foreach (Enemy enemy in Enemy.All) { ... } }      // List を直接回せばアロケーションなし
```
**教訓:** 変わらない参照は一度探して保持し (インスペクターでの接続が最善)、変わる集合は OnEnable/OnDisable で登録簿を管理します。登録簿は新しく現れた敵や消えた敵をすぐに反映します (一度探して終わりにすると、ラボの 2 つ目の判定で FAIL)。`Camera.main` は 2020.2 から内部キャッシュなので、以前ほど重くありません。静的な登録簿は、Domain Reload をオフにした再生では空にしてやる必要があります (ラボ 34)。

## ラボ 61 — プールを作ったのに返していない
```
間違った結果:   作られた弾の数 60 (期待 ≤ 13)、プールに戻った弾 0
ツール:         BulletPool.Release にブレークポイント → 一度も止まらない
              プロファイラー → ヒエラルキーで Instantiate / Destroy の呼び出し数が発射数と同じ
本当の原因:     Bullet は寿命が尽きると以前のまま Destroy(gameObject) している。プールは常に空なので Get が毎回 Instantiate する
```
**修正:**
```csharp
if (Life <= 0f) { Pool.Release(this); }
```
**教訓:** プールは「取り出し」より「返却」のほうがバグを生みやすいです。返すときに無効化し (SetActive(false))、取り出すときに位置・速度・寿命・トレイルなどの状態をすべて初期化します。`UnityEngine.Pool.ObjectPool<T>` は createFunc/actionOnGet/actionOnRelease を受け取ってこの対を強制し、同じものを 2 回返すとエラーを出します (collectionCheck)。

## ラボ 62 — SendMessage の名前のタイプミスとボックス化
```
間違った結果:   受けたダメージ 0。名前を TakeDamage に直しても GC アロケーションがフレームあたり約 500 バイト (敵 30 体)
ツール:         Enemy.TakeDamage のブレークポイントで止まらない。RequireReceiver に変えると "SendMessage TakeDamge has no receiver!"
              プロファイラー → GC アロケーション: SendMessage の下にボックス化された float
本当の原因:     SendMessage は文字列でメソッドを探す。タイプミスはコンパイラーにはわからず、DontRequireReceiver なので静か。引数は object にボックス化される
```
**修正:**
```csharp
public interface IDamageable { void TakeDamage(float amount); }
public class Enemy : MonoBehaviour, IDamageable { ... }

if (InFlame[i].TryGetComponent(out IDamageable target)) { target.TakeDamage(DamagePerSecond * Time.deltaTime); }
```
**教訓:** `SendMessage`/`BroadcastMessage`/`SendMessageUpwards` はタイプミスが静かで、毎回すべてのコンポーネントをリフレクションでなめ、引数をボックス化します。インターフェース (`IDamageable`)、C# event、UnityEvent は名前をコンパイル時に確認します。毎フレームの `TryGetComponent` も節約したいなら、入ってきたときに一度探してリストに入れておきます。

## ラボ 63 — 毎フレーム配列を返す OverlapSphere
```
間違った結果:   回復は正しいが GC アロケーションがフレームあたり 1792 バイト = 司祭 8 人 × (味方 24 人 × 8 + 配列ヘッダー 32) (期待 128 バイト以下)
ツール:         プロファイラー → GC アロケーション列 → HealingAura.Update → Physics.OverlapSphere
本当の原因:     結果を新しい Collider[] で返す API を毎フレーム呼んでいる
```
**修正:**
```csharp
readonly Collider[] hits = new Collider[32];

void Update()
{
    int count = Physics.OverlapSphereNonAlloc(transform.position, Radius, hits);
    for (int i = 0; i < count; ++i) { ... }
}
```
**教訓:** 配列を返す API (`OverlapSphere`、`RaycastAll`、`GetComponents<T>()`、`GetComponentsInChildren<T>()`、`FindObjectsByType`、`Mesh.vertices`) には、たいていバッファを埋める対 (`NonAlloc`、`List<T>` を受け取るオーバーロード、`GetVertices(List)`) があります。バッファが小さいとあふれた結果は **黙って捨てられる** ので、返ってきた個数がバッファサイズと同じときは警告を残します (隠しケースは味方 30 人)。バッファを関数の外に出すと、ラボ 28 の共有バグになります。

