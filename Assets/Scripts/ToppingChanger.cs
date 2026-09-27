using UnityEngine;

/// <summary>
/// トッピング変更のための音声認識を開始制御する
/// </summary>
public class ToppingChanger : MonoBehaviour
{
    [SerializeField]
    private WordFuncIntegration wordFuncIntegration;

    private bool isProcess = false;

    [Tooltip("シーン内の監視者クラス")]
    private IObserver _iObserver;
    [Tooltip("シーン内の監視者クラスをアタッチしているObject")]
    [SerializeField] private GameObject _observer_obj;
    [Tooltip("シーン内のシューター")]
    [SerializeField] private Shooter _shooter;

    [Tooltip("発射トッピング復元時の器")]
    [SerializeField] private GameObject _shooter_vessel;
    [Tooltip("JoyConからの音声認識を実装")]
    private JoyconPlayer _joyconPlayer;

    [Tooltip("一度クリックした後のインターバル（秒）")]
    private float _record_interval = 0.5f;
    [Tooltip("インターバルの計算用")]
    private float _record_timer = 0f;

    [Tooltip("音声認識開始SE")]
    [SerializeField] private AudioClip _recording_se;
    [Tooltip("音声認識成功SE")]
    [SerializeField] private AudioClip _success_se;
    [Tooltip("音声認識失敗SE")]
    [SerializeField] private AudioClip _failure_se;


    /// <summary>
    /// 各種初期化
    /// </summary>
    private void Start()
    {
        // インターフェース取得
        _iObserver = _observer_obj.GetComponent<IObserver>();
        // インスタンス取得
        _joyconPlayer = JoyconPlayer.instance;

        // アクション群に、自身の音声認識を追加
        _joyconPlayer.on_shoulder1_pressed += ToppingChange;

        // ゲーム起動時に初期トッピングであるイチゴを動的ロード
        ToppingSelector.UpdateShootingObj(_shooter, _shooter_vessel, "fresh_strawberry", new string[0]);
    }

    /// <summary>
    /// タイマー計算
    /// </summary>
    private void Update()
    {
        // クリックインターバル計算用タイマー
        if (_record_timer > 0f)
        {
            _record_timer -= Time.deltaTime;
        }
    }

    /// <summary>
    /// トッピング切り替えメソッド（非同期）
    /// </summary>
    public async void ToppingChange()
    {
        // 録音不可能またはクールタイム中なら終わり
        if (!_iObserver.CanRecord || _record_timer > 0)
        {
            return;
        }
        // 入力成功時、タイマーをリセット
        _record_timer = _record_interval;

        // 現在既に録音中の場合、録音を停止して現在までの音声で解析に回す
        if (isProcess)
        {
            wordFuncIntegration.StopPipeline();
            return;
        }

        // 録音開始SE
        AudioManager.instance.SingleSEClip = _recording_se;
        // 振動（コロン・・・）
        StartCoroutine(JoyconPlayer.instance.TwiceRumble(160, 320, 0.4f, 10, 200));

        isProcess = true;
        // トッピング名を"認識中・・・"としてObserverクラスに渡す
        _iObserver.VoiceInput = "録音中（L/Rで終了）";
        // 状態を認識中とする
        _iObserver.CurrentRCState = IObserver.RecordingState.Recording;

        try
        {
            // トッピング名取得
            FastTextController.ToppingMatch match = await wordFuncIntegration.WordsPipeline();

            // トッピング名が取得できたか
            // シーンを挟むnullチェック
            if (_iObserver == null)
            {
                return;
            }

            if (match != null)
            {
                // メッシュ復元 (タグも渡す)
                ToppingSelector.UpdateShootingObj(_shooter, _shooter_vessel, match.asset_id, match.tags);
                // トッピング名をObserverクラスに渡す
                _iObserver.Recorded = match.asset_id;

                // 状態を認識成功とする
                _iObserver.CurrentRCState = IObserver.RecordingState.Success;
                // 録音成功SE
                AudioManager.instance.SingleSEClip = _success_se;
            }
            // できていなければ
            else
            {
                // 状態を認識失敗とする
                _iObserver.CurrentRCState = IObserver.RecordingState.Failure;
                // 録音失敗SE
                AudioManager.instance.SingleSEClip = _failure_se;
            }
        }
        finally
        {
            isProcess = false;

            // 振動（長め）
            JoyconPlayer.instance.Rumble(160, 320, 0.2f, 500);
        }
    }

    /// <summary>
    /// 非アクティブ化時はメソッドを削除
    /// </summary>
    private void OnDestroy()
    {
        // JoyConの入力アクションから、自身のメソッドを削除
        _joyconPlayer.on_shoulder1_pressed -= ToppingChange;
        // 録音中の場合、処理を中止する
        wordFuncIntegration.StopPipeline();
    }
}
