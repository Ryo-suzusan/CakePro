using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// トッピングの発射元
/// </summary>
public class Shooter : MonoBehaviour
{
    [Tooltip("シーン内の監視者クラス")]
    private IObserver _iObserver;
    [Tooltip("シーン内の監視者クラスをアタッチしているObject")]
    [SerializeField] private GameObject _observer_obj;
    [Tooltip("トッピングデータ追加対象のケーキ")]
    [SerializeField] private CakeManager _cakeManager;
    [Tooltip("発射するトッピングクラスobj(MeshFilter/MeshRendererはアタッチ済みとする)")]
    [SerializeField] private Topping _shooting_topping;
    public                   Topping ShootingTopping
    {
        // 外部から更新可能。音声認識内容に対する最高類似度オブジェクトで更新する時、等。
        set
        {
            _shooting_topping = value;
        }
    }

    [Tooltip("発射照準とするPointer")]
    [SerializeField] private Pointer _pointer;
    [Tooltip("発射メソッドの実行元")]
    private JoyconPlayer _joyconPlayer;
    [Tooltip("射出する角度")]
    [SerializeField] private float _shooting_angle_deg;
    [Tooltip("射出のy座標補正")]
    [SerializeField] private float _shooting_y_correction = 0.1f;
    [Tooltip("連打までのインターバル")]
    [SerializeField] private float _interval_sec = 0.5f;
    [Tooltip("タイマー")]
    private float _timer_sec;

    [Tooltip("発射SE")]
    [SerializeField] private AudioClip _shot_se;
    [Tooltip("発射エフェクト")]
    [SerializeField] private ParticleSystem _shot_effect;


    /// <summary>
    /// 各種初期化
    /// </summary>
    private void Start()
    {
        // インスタンス取得
        _joyconPlayer = JoyconPlayer.instance;
        // インターフェース取得
        _iObserver = _observer_obj.GetComponent<IObserver>();

        // ジョイコンからトッピング発射メソッドを実行できるようにする
        _joyconPlayer.on_shoulder2_pressed += Aim;

        // ゲーム開始時はインターバルを待つ必要なし
        _timer_sec = _interval_sec;
    }

    /// <summary>
    /// 入力を受け取って、トッピングオブジェクトを発射
    /// </summary>
    private void Update()
    {
        // タイマーを更新
        _timer_sec += Time.deltaTime;

        // タイマーがインターバルを超えたとき、保持しているトッピングをアクティブにする
        if (_timer_sec > _interval_sec)
        {
            _shooting_topping.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 発射ボタンが押された際のメソッド
    /// </summary>
    private void Aim()
    {
        // 現在シューティング不可能なら終わり
        if (!_iObserver.CanShoot)
        {
            return;
        }
        // タイマーがインターバルを超えていなければ終わり
        if (_timer_sec < _interval_sec)
        {
            return;
        }

        // タイマーをリセット
        _timer_sec = 0;

        // ポインターの座標からRayを飛ばす
        // 1. レイの発射元はイベントカメラ
        Vector3 ray_origin = _pointer.TargetCamera.transform.position;
        // 2. レイの方向は、イベントカメラから3D空間上のポインターへ向かうベクトル
        Vector3 ray_direction = (_pointer.RectTransform.position - ray_origin).normalized;
        // 3. レイの組み立て
        Ray ray = new Ray(ray_origin, ray_direction);

        // 障害物との交点を取得   
        RaycastHit hit;
        // 交点が取得できたら
        if (Physics.Raycast(ray, out hit, 30.0f))
        {
            // 中間変数を作成
            Vector3 target_point = hit.point;
            float angle_deg = _shooting_angle_deg;
            Vector3 velocity = CalcVelocity(transform.position, target_point, angle_deg);

            // 交点に向かってトッピング発射
            ShootTopping(hit.point, velocity);

            // 効果音
            AudioManager.instance.SingleSEClip = _shot_se;
            // 振動（火薬の爆発（低音）と金属の作動音（高音）のイメージ）
            StartCoroutine(_joyconPlayer.FadeOutRumble(160, 320, 0.6f, 0.3f));
            // エフェクト
            _shot_effect.Play();
        }
    }

    /// <summary>
    /// トッピングオブジェクトを発射する
    /// </summary>
    /// <param name="target_point">目標地点</param>
    /// <param name="velocity">射出速度</param>
    private void ShootTopping(Vector3 target_point, Vector3 velocity)
    {
        Topping obj = Topping.Shoot(
            _shooting_topping, 
            new Vector3(transform.position.x, transform.position.y + _shooting_y_correction, transform.position.z),     // 立体視の表現を強調するために、位置を高めに調整
            transform.localScale, 
            velocity, 
            _cakeManager);

        // トッピングを非アクティブにする
        _shooting_topping.gameObject.SetActive(false);
    }

    /// <summary>
    /// 目標地点に命中させる射出速度を計算
    /// </summary>
    /// <param name="start_point">射出開始座標</param>
    /// <param name="target_point">目標座標</param>
    /// <param name="angle_deg">射出角度</param>
    /// <returns>射出速度</returns>
    private Vector3 CalcVelocity(Vector3 start_point, Vector3 target_point, float angle_deg)
    {
        // 角度をラジアンに変換
        float angle_rad = angle_deg * Mathf.PI / 180;

        // 水平方向の距離
        float x = Vector2.Distance(new Vector2(start_point.x, start_point.z), new Vector2(target_point.x, target_point.z));
        // 垂直方向の距離
        float y = target_point.y - start_point.y;

        // 斜方投射の公式
        float speed = Mathf.Sqrt(Physics.gravity.y * Mathf.Pow(x, 2) / (2 * Mathf.Pow(Mathf.Cos(angle_rad), 2) * (y - x * Mathf.Tan(angle_rad))));

        // 初速が算出できなければゼロベクトルを返す
        if (float.IsNaN(speed))
        {
            return Vector3.zero;
        }

        // 速さを速度に変換して返す
        return new Vector3(
            target_point.x - start_point.x, 
            Mathf.Tan(angle_rad) * x, 
            target_point.z - start_point.z
            ).normalized * speed;
    }

    // ----- 以下デバッグ用 -----

    /// <summary>
    /// マウスクリックによるトッピング発射
    /// </summary>
    /// <param name="mouse">マウス</param>
    public void AimWithMouse(Mouse mouse)
    {
        // Joycon用処理と同じもので良い
        Aim();
    }
}
