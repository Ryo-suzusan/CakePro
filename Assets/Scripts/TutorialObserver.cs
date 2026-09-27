using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tutorialシーンの監視者。
/// </summary>
public class TutorialObserver : MonoBehaviour, IObserver
{
    [Tooltip("シーン内のメッセージ表示先")]
    [SerializeField] private MessageTelop _messageTelop;
    [Tooltip("シーン内のUIDrower")]
    [SerializeField] private InGameUIDrower _inGameUIDrower;
    [Tooltip("シーン内のカメラ")]
    [SerializeField] private CameraController _cameraController;
    [Tooltip("シーン内のキャラクター")]
    [SerializeField] private CharacterController _characterController;

    /// <summary>
    /// ターゲットタグ (チュートリアルでは使用しないため空文字列)
    /// </summary>
    public string TargetTag => "";

    [Tooltip("今、シューティング可能か")]
    public bool CanShoot {  get; private set; }
    [Tooltip("今、録音可能か")]
    public bool CanRecord {  get; private set; }
    [Tooltip("音声入力された文章")]
    private string _voice_input = null;
    public string VoiceInput {
        get
        {
            return _voice_input;
        }
        set
        {
            _voice_input = value;
            _inGameUIDrower.UpdateVoiceInput(_voice_input);
        }
    }
    [Tooltip("トッピング名として取得された文章")]
    private string _recorded = null;
    public string Recorded {
        get
        {
            return _recorded;
        }
        set
        {
            _recorded = value;
            _inGameUIDrower.UpdateToppingName(_recorded);
        }
    }

    [Tooltip("現在のケーキスコア")]
    private Dictionary<IObserver.Evaluation, int> _scores = new Dictionary<IObserver.Evaluation, int>();
    public Dictionary<IObserver.Evaluation, int> Scores { 
        get
        {
            return _scores;
        }
        set
        {
            // トッピングカウントを進める
            _tutorial_remain_count--;
            // トッピングカウントが0まで減ったなら
            if (_tutorial_remain_count <= 0)
            {
                if (_tutorial_num == 0)     // 音声入力チュートリアル
                {
                    // キャラとの会話再開（カメラティルト、シューティング・録音不可）
                    _cameraController.LookForward();
                    CanShoot = false;
                    CanRecord = false;

                    // メッセージ終了時の処理を追加
                    Action on_complete = delegate { };
                    on_complete += _cameraController.LookDown;
                    on_complete += (() => CanShoot = true);
                    on_complete += (() => CanRecord = true);
                    on_complete += (() => _topping_panel.SetActive(true));

                    // チュートリアルメッセージ表示
                    _messageTelop.ShowMessage(_recording_tutorial.Messages, on_complete);

                    // 値を初期化
                    _tutorial_remain_count = _tutorial_total_count;
                    // 次のチュートリアルに行く
                    _tutorial_num++;
                }
                else if (_tutorial_num == 1 && Recorded != null) // チュートリアル終了
                {
                    // キャラとの会話再開（カメラティルト、シューティング・録音不可）
                    _cameraController.LookForward();
                    CanShoot = false;
                    CanRecord = false;

                    // メッセージ終了時の処理を追加
                    Action on_complete = delegate { };
                    on_complete += (() => SequenceManager.CurrentSecuence = SequenceManager.Sequence.CakeMaking);

                    // チュートリアル終了メッセージ表示
                    _messageTelop.ShowMessage(_ending_tutorial.Messages, on_complete);

                    // 喜ばす
                    _characterController.BeJoyful();
                }
            }

            _scores = value;
        }
    }

    [Tooltip("今の音声入力状態")]
    private IObserver.RecordingState _current_rc_state = IObserver.RecordingState.Failure;
    public  IObserver.RecordingState CurrentRCState
    {
        get
        {
            return (_current_rc_state);
        }
        set
        {
            if (value == _current_rc_state)
            {
                return;
            }
            _current_rc_state = value;
            // 入力開始・終了に応じた演出
            _inGameUIDrower.ShowVoiceIcon(_current_rc_state);
        }
    }


    [Tooltip("シーン内の、トッピング情報提示パネル")]
    [SerializeField] private GameObject _topping_panel;

    [Tooltip("トッピングを合計いくつ置いたら、次のメッセージに行くか？")]
    private int _tutorial_total_count = 3;
    [Tooltip("トッピングを残りいくつ置いたら、次のメッセージに行くか？")]
    private int _tutorial_remain_count = 3;
    [Tooltip("今は何個目のチュートリアル中か？")]
    private int _tutorial_num = 0;

    [Tooltip("シーン開始時表示されるメッセージ")]
    [SerializeField] private MessageData _shooting_tutorial;
    [Tooltip("音声入力チュートリアル用メッセージ")]
    [SerializeField] private MessageData _recording_tutorial;
    [Tooltip("チュートリアル終了メッセージ")]
    [SerializeField] private MessageData _ending_tutorial;

    /// <summary>
    /// 各種初期化、メッセージ表示
    /// </summary>
    private void Start()
    {
        // 始めはシューティング・録音不可能
        CanShoot = false;
        CanRecord = false;
        // トッピング情報パネルは非表示
        _topping_panel.SetActive(false);

        // 各採点項目を初期化
        foreach (IObserver.Evaluation item in Enum.GetValues(typeof(IObserver.Evaluation)))
        {
            Scores[item] = 0;
        }

        // メッセージ終了時の処理を追加
        Action on_complete = delegate { };
        on_complete += _cameraController.LookDown;
        on_complete += (() => CanShoot = true);
        // 始めのチュートリアルメッセージ表示
        _messageTelop.ShowMessage(_shooting_tutorial.Messages, on_complete);
    }

    /// <summary>
    /// ケーキスコアをプラス
    /// </summary>
    /// <param name="addition">付加するスコア</param>
    public void AddScores(Dictionary<IObserver.Evaluation, int> addition)
    {
        // Scoresの更新値
        Dictionary<IObserver.Evaluation, int> new_score = new Dictionary<IObserver.Evaluation, int>();

        // 各採点項目について、new__scoreを算出
        foreach (IObserver.Evaluation item in Enum.GetValues(typeof(IObserver.Evaluation)))
        {
            new_score[item] = Scores[item] + addition[item];
        }

        // 更新
        Scores = new_score;
    }
}
