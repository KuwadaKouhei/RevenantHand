// ============================================================
// PlayerController.cs
// プレイヤーの移動を担当するスクリプト
// 
// 【このスクリプトがやること】
// - WASDキー（またはゲームパッドの左スティック）でプレイヤーを8方向に移動させる
// - カメラの向きを基準にして「W=画面の奥方向」「A=画面の左方向」のように動く
// - 移動方向にプレイヤーの向きを自動で回転させる
//
// 【使い方】
// 1. このスクリプトを Player ゲームオブジェクトにドラッグ&ドロップでアタッチする
// 2. Player には Rigidbody コンポーネントが必要（回転固定 X/Y/Z すべてON）
// 3. インスペクターで moveSpeed（移動速度）や rotationSpeed（回転速度）を調整できる
// ============================================================

using UnityEngine;
using UnityEngine.InputSystem;

// MonoBehaviour を継承すると、Unityのゲームオブジェクトにアタッチできるスクリプトになる。
// Webでいえば「Reactコンポーネント」のようなもの。
public class PlayerController : MonoBehaviour
{
  // ──────────────────────────────────────
  // インスペクターから調整できる変数
  // ──────────────────────────────────────
  // [SerializeField] を付けると、private のままでもインスペクターに表示される。
  // [Header("...")] はインスペクター上のセクション見出し。見やすさのために付ける。
  // [Tooltip("...")] はインスペクターでマウスホバー時に表示される説明文。

  [Header("移動設定")]
  [Tooltip("プレイヤーの移動速度。大きいほど速く動く")]
  [SerializeField] private float moveSpeed = 7f;

  [Tooltip("プレイヤーの回転速度。大きいほど素早く振り向く")]
  [SerializeField] private float rotationSpeed = 15f;

  // ──────────────────────────────────────
  // 内部で使う変数（private なのでインスペクターには出ない）
  // ──────────────────────────────────────

  // 入力アクションアセットから自動生成されたクラス。
  // STEP 2 で作成した PlayerInputActions から生成されたもの。
  private PlayerInputActions inputActions;

  // Rigidbody への参照。物理演算ベースの移動に使う。
  private Rigidbody rb;

  // カメラへの参照。移動方向をカメラの向き基準にするため。
  private Transform cameraTransform;

  // 現在のフレームでの入力値を保持する変数。
  // Vector2 は (x, y) の2つの値を持つ構造体。
  // x = 左右の入力（A=-1, D=+1）、y = 前後の入力（S=-1, W=+1）
  private Vector2 moveInput;

  // ──────────────────────────────────────
  // Unityのライフサイクルメソッド
  // ──────────────────────────────────────
  // Unityのスクリプトには「決まったタイミングで自動的に呼ばれるメソッド」がある。
  // Webでいえば React の useEffect や onMount のようなもの。
  // 
  // 呼ばれる順番:
  //   Awake() → OnEnable() → Start() → Update()(毎フレーム) → FixedUpdate()(一定間隔) → OnDisable() → OnDestroy()

  /// <summary>
  /// Awake: このスクリプトが読み込まれた直後に1回だけ呼ばれる。
  /// 他のスクリプトから参照される前に、自分自身の初期化を行う場所。
  /// </summary>
  private void Awake()
  {
    // ── 入力アクションの準備 ──
    // PlayerInputActions クラスをインスタンス化する。
    // これは STEP 2 で作った入力アクションアセットへのアクセス手段。
    inputActions = new PlayerInputActions();

    // ── Rigidbody の取得 ──
    // GetComponent<T>() は、同じゲームオブジェクトに付いている
    // コンポーネントを取得するメソッド。
    // Webでいえば document.querySelector で同じ要素内のパーツを取得するイメージ。
    rb = GetComponent<Rigidbody>();

    // ── カメラの取得 ──
    // Camera.main はシーン内の「MainCamera」タグが付いたカメラを返す。
    // Unity が最初から用意してくれている Main Camera にはこのタグが付いている。
    cameraTransform = Camera.main.transform;
  }

  /// <summary>
  /// OnEnable: このスクリプト（コンポーネント）が有効化されたときに呼ばれる。
  /// 入力を受け付ける開始タイミング。
  /// </summary>
  private void OnEnable()
  {
    // 入力アクションを有効化する。
    // これを呼ばないと、キーを押しても何も起きない。
    inputActions.Player.Enable();
  }

  /// <summary>
  /// OnDisable: このスクリプトが無効化されたときに呼ばれる。
  /// ゲームオブジェクトが非アクティブになった時や、シーン遷移時など。
  /// </summary>
  private void OnDisable()
  {
    // 入力アクションを無効化する。
    // メモリリークやゴースト入力を防ぐため、OnEnable で有効化したものは
    // OnDisable で必ず無効化する（Webでいう addEventListener / removeEventListener の対）。
    inputActions.Player.Disable();
  }

  /// <summary>
  /// Update: 毎フレーム呼ばれる（1秒間に60回程度）。
  /// 入力の読み取りや、見た目の更新をここで行う。
  /// 
  /// ※ 物理演算に関わる処理（Rigidbody の移動）は Update ではなく FixedUpdate で行う。
  ///   Update はフレームレートに依存して呼ばれる間隔が変わるが、
  ///   FixedUpdate は一定間隔（デフォルト0.02秒=50回/秒）で呼ばれるため、
  ///   物理演算が安定する。
  /// </summary>
  private void Update()
  {
    // ── 入力値の読み取り ──
    // ReadValue<Vector2>() で、このフレームでの入力値を取得する。
    // W を押していれば (0, 1)、D を押していれば (1, 0)、
    // W+D を同時に押していれば (0.71, 0.71)（正規化された値）が返る。
    moveInput = inputActions.Player.Move.ReadValue<Vector2>();
  }

  /// <summary>
  /// FixedUpdate: 物理演算の更新タイミングで呼ばれる（一定間隔）。
  /// Rigidbody を使った移動はここで行う。
  /// </summary>
  private void FixedUpdate()
  {
    // ── 移動処理 ──
    Move();
  }

  // ──────────────────────────────────────
  // 自分で定義したメソッド
  // ──────────────────────────────────────

  /// <summary>
  /// プレイヤーを移動させる。
  /// カメラの向きを基準にして、WASDの入力方向に動かす。
  /// </summary>
  private void Move()
  {
    // 入力がなければ何もしない（微小な入力を無視するため 0.1f で閾値を設ける）
    if (moveInput.magnitude < 0.1f)
    {
      return;
    }

    // ── カメラ基準の移動方向を計算する ──
    //
    // なぜカメラ基準にするのか？
    // もし「ワールド座標のZ方向 = 前」と固定すると、カメラが斜めに見下ろしている場合に
    // 「Wを押したら画面の右奥に動く」という直感に反する動きになる。
    // カメラの向いている方向を基準にすることで、
    // 「W = 画面の奥方向」「A = 画面の左方向」になり、直感的に操作できる。

    // カメラの前方ベクトルを取得し、Y成分を消して水平にする
    // （カメラが下を向いていても、プレイヤーは水平に動いてほしいため）
    Vector3 cameraForward = cameraTransform.forward;
    cameraForward.y = 0f;
    cameraForward.Normalize(); // ベクトルの長さを1にする（方向だけの情報にする）

    // カメラの右方向ベクトルも同様に水平化
    Vector3 cameraRight = cameraTransform.right;
    cameraRight.y = 0f;
    cameraRight.Normalize();

    // 最終的な移動方向 = カメラの前方 × 入力Y + カメラの右方向 × 入力X
    // 例: W (moveInput.y=1) → カメラの前方に移動
    //     D (moveInput.x=1) → カメラの右方向に移動
    //     W+D → カメラの右前方に移動
    Vector3 moveDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;

    // ── Rigidbody で移動する ──
    // MovePosition は「指定した位置にスムーズに移動させる」メソッド。
    // 壁などのコライダーとの衝突判定も自動で行われる。
    //
    // Time.fixedDeltaTime は FixedUpdate 1回あたりの経過時間（秒）。
    // 速度 × 時間 = 距離 で、フレームレートに関係なく一定速度で動く。
    Vector3 newPosition = rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
    rb.MovePosition(newPosition);

    // ── プレイヤーの向きを回転させる ──
    // Quaternion.LookRotation は「指定した方向を向く回転」を生成する。
    // Quaternion.Slerp は「現在の回転から目標の回転へなめらかに補間する」メソッド。
    // rotationSpeed が大きいほど素早く振り向く。
    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
    transform.rotation = Quaternion.Slerp(
        transform.rotation,       // 現在の回転
        targetRotation,            // 目標の回転
        rotationSpeed * Time.fixedDeltaTime  // 補間の速さ
    );
  }
}