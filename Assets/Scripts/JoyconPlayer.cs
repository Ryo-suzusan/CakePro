using System;
using System.Collections;
using UnityEngine;

public class JoyconPlayer : MonoBehaviour {
    [Tooltip("自身のシングルトンインスタンス")]
    public static JoyconPlayer instance;

	[Tooltip("扱うジョイコン")]
	private Joycon _joycon;
	[Tooltip("扱うジョイコンの添え字")]
	[SerializeField] private int _joycon_id;

	[Tooltip("スティックの傾いている方向と度合い")]
    private float[] _stick;
	[Tooltip("ジャイロ(radian / sec)")]
    private Vector3 _gyro;
	[Tooltip("加速度(G)")]
    private Vector3 _accel;
	[Tooltip("Joyconの傾き")]
    private Quaternion _orientation;
    [Tooltip("軸リセット時にJoyConの回転を記憶する変数")]
    private Quaternion _center_orientation = Quaternion.identity;

	[Tooltip("L/Rボタンが押された時のアクション")]
	public event Action on_shoulder1_pressed = delegate { };
	[Tooltip("ZL/ZRボタンが押された時のアクション")]
	public event Action on_shoulder2_pressed = delegate { };
	[Tooltip("上/下/右/左ボタンが押された時のアクション")]
	public event Action on_dpad_pressed = delegate { };
	[Tooltip("傾きを検出した際のアクション")]
	public event Action<Quaternion> on_orientation_gotten = delegate { };


    /// <summary>
    /// シングルトン化
    /// </summary>
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 各種初期化
    /// </summary>
    private void Start ()
    {
        // JoyconManagerから、適切なJoyconを取得する
		if (JoyconManager.Instance.j.Count > _joycon_id)
		{
            _joycon = JoyconManager.Instance.j[_joycon_id];
		}

		// ジャイロと加速度リセット
        _gyro = new Vector3(0, 0, 0);
        _accel = new Vector3(0, 0, 0);

        // 各アクション群の初期化
        on_dpad_pressed += Recenter;
	}

	/// <summary>
	/// 各種入力の検出とイベント発火
	/// </summary>
    private void Update () {
        // _joyconが空なら、実行中止
        if (_joycon == null)
        {
            return;
        }

        // ボタン押下検出
        // L/Rボタン
        if (_joycon.GetButtonDown(Joycon.Button.SHOULDER_1))
        {
            on_shoulder1_pressed();
        }
        // ZL/ZRボタン
        if (_joycon.GetButtonDown(Joycon.Button.SHOULDER_2))
        {
            on_shoulder2_pressed();
        }
        // 上ボタン
        if (_joycon.GetButtonDown(Joycon.Button.DPAD_UP))
        {
            on_dpad_pressed();
        }
        // 下ボタン
        if (_joycon.GetButtonDown(Joycon.Button.DPAD_DOWN))
        {
            on_dpad_pressed();
        }
        // 右ボタン
        if (_joycon.GetButtonDown(Joycon.Button.DPAD_RIGHT))
        {
            on_dpad_pressed();
        }
        // 左ボタン
        if (_joycon.GetButtonDown(Joycon.Button.DPAD_LEFT))
        {
            on_dpad_pressed();
        }

        // スティックの傾き検出
        _stick = _joycon.GetStick();

        // ジャイロ検出
        _gyro = _joycon.GetGyro();

        // 加速度検出
        _accel = _joycon.GetAccel();

        // コントローラーの傾き検出（記憶済みの基準値からの相対的な回転）
        Quaternion raw_orientation = _joycon.GetVector();
        _orientation = Quaternion.Inverse(_center_orientation) * raw_orientation;
        _orientation = Quaternion.Euler(-90, 0, 0) * _orientation;   // Pointer.csの計算式が「画面中央」だと認識する角度をプラス

        on_orientation_gotten(_orientation);
    }

    /// <summary>
    /// Joyconの軸をリセットする
    /// </summary>
	private void Recenter()
    {
        // 上下
        _center_orientation = _joycon.GetVector();
    }

    /// <summary>
    /// 外部から振動を呼び出すためのメソッド
    /// </summary>
    /// <param name="low_freq">低周波数</param>
    /// <param name="high_freq">高周波数</param>
    /// <param name="amp">振幅</param>
    /// <param name="time_ms">振動時間（ms）</param>
    public void Rumble(float low_freq, float high_freq, float amp, int time_ms = 0)
    {
        if (_joycon != null)
        {
            _joycon.SetRumble(low_freq, high_freq, amp, time_ms);
        }
    }

    /// <summary>
    /// フェードアウトする振動を呼び出すメソッド
    /// </summary>
    /// <param name="low_freq">低周波数</param>
    /// <param name="high_freq">高周波数</param>
    /// <param name="start_amp">開始時の振幅</param>
    /// <param name="duration_s">フェードアウトに掛ける時間（秒）</param>
    public IEnumerator FadeOutRumble(float low_freq, float high_freq, float start_amp, float duration_s)
    {
        // 経過時刻
        float elapsed = 0f;

        while (elapsed < duration_s)
        {
            elapsed += Time.deltaTime;

            // 経過に合わせて振幅を線形補間する
            float current_amp = Mathf.Lerp(start_amp, 0f, elapsed / duration_s);
            // JoyConに振動を命令（時間は、更新時間より長めの時間を指定）
            Rumble(low_freq, high_freq, current_amp, 50);

            // フレーム待機
            yield return null;
        }

        // 完全にフェードアウトしたら、振動を停止
        Rumble(0, 0, 0, 0);
    }

    /// <summary>
    /// 2回続けて同じ振動を呼び出すメソッド
    /// </summary>
    /// <param name="low_freq">低周波数</param>
    /// <param name="high_freq">高周波数</param>
    /// <param name="amp">振幅</param>
    /// <param name="time_ms">それぞれの振動時間（s）</param>
    /// <param name="interval_ms">一回目の振動終了から二回目の振動開始までのインターバル（s）</param>
    public IEnumerator TwiceRumble(float low_freq, float high_freq, float amp, int time_ms, int interval_ms)
    {
        Rumble(low_freq, high_freq, amp, time_ms);

        yield return new WaitForSeconds((float)interval_ms / 1000);

        Rumble(low_freq, high_freq, amp, time_ms);
    }
}
