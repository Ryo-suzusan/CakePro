using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;

/// <summary>
/// InGameにおいて、制限時間などのカウントや、状態の管理を担う
/// </summary>
public class InGameObserver : MonoBehaviour, IObserver
{
    [Tooltip("制限時間(単位:秒)")]
    [SerializeField] private int _time_limit_s = 180;
    [Tooltip("経過時間(単位:秒)")]
    private float _current_time_s = 0;
    [Tooltip("タイマーを動かすか否か")]
    private bool _do_count = false;

    [Tooltip("シーン内のメッセージ表示先")]
    [SerializeField] private MessageTelop _messageTelop;
    [Tooltip("シーン内のキャラリアクション表示先")]
    [SerializeField] private ReactionTelop _reactionTelop;
    [Tooltip("シーン内のキャラクター")]
    [SerializeField] private CharacterController _characterController;
    [Tooltip("シーン内のカメラ")]
    [SerializeField] private CameraController _cameraController;
    [Tooltip("シーン内のUIDrower")]
    [SerializeField] private InGameUIDrower _inGameUIDrower;

    [Tooltip("現在食べたいケーキのターゲットタグ（英語）")]
    public string TargetTag { get; private set; }

    [Tooltip("シーン内のケーキ")]
    [SerializeField] CakeManager _cakeManager;

    [Tooltip("現在のケーキスコア")]
    private Dictionary<IObserver.Evaluation, int> _scores = new Dictionary<IObserver.Evaluation, int>();
    public Dictionary<IObserver.Evaluation, int> Scores { 
        get
        {
            return _scores;
        }
        set
        {
            // 差分を取得
            Dictionary<IObserver.Evaluation, int> diffs = new Dictionary<IObserver.Evaluation, int>();
            foreach (IObserver.Evaluation item in Enum.GetValues(typeof(IObserver.Evaluation)))
            {
                diffs[item] = value[item] - _scores[item];
            }

            // もし現在、キャラが何らかのリアクション中でなければ
            if (!_reactionTelop.IsShowing)
            {
                // 差分の内容に応じて、キャラクターの反応をランダムに出力

                // キャラが喋りうる内容（タプル型）
                var options = new List<(string dialogue /* = セリフ内容 */, bool is_good /* = 真なら喜び、偽なら悩み */)>();

                // 1. 総合点変化に対するリアクション
                int total_diff = diffs[IObserver.Evaluation.Total];
                if (total_diff > 0)
                {
                    options.Add(("わあ！\n美味しそう！", true));
                }
                // 2. バランス点変化に対するリアクション
                int balance_diff = diffs[IObserver.Evaluation.Balance];
                if (balance_diff > 0)
                {
                    options.Add(("キレイに並んでるね！", true));
                }
                else if (balance_diff < 0)
                {
                    options.Add(("ありゃ、ちょっとずれちゃった？", false));
                }
                // 3. 数量点変化に対するリアクション
                int quantity_diff = diffs[IObserver.Evaluation.Quantity];
                if (quantity_diff > 0)
                {
                    options.Add(("やった！\nトッピングがいっぱいだ！", true));
                }
                else if (quantity_diff < 0)
                {
                    options.Add(("うーん・・・\nこれ以上は食べきれないかも・・・", false));
                }
                // 4. 種類数点変化に対するリアクション
                int variety_diff = diffs[IObserver.Evaluation.Variety];
                if (variety_diff > 0)
                {
                    options.Add(("わーい、新しいトッピングだ！", true));
                }
                else if (variety_diff < 0)
                {
                    options.Add(("ちょっとゴチャゴチャしてきたなあ", false));
                }
                // 5. 好み一致点変化に対するリアクション
                int tag_match_diff = diffs[IObserver.Evaluation.TagMatch];
                if (tag_match_diff > 0)
                {
                    options.Add(("あ、ボクの好きなトッピングだ！", true));
                }

                // dialoguesの中から、ランダムにリアクションを実行

                // もし喋れる内容があるなら
                if (options.Count > 0)
                {
                    // 喋る内容をランダムに決定→出力
                    int random_idx = UnityEngine.Random.Range(0, options.Count);
                    string target_dialogue = options[random_idx].dialogue;
                    _reactionTelop.ShowMessage(target_dialogue);
                    // 内容にしたがって、キャラモーションを実行
                    bool target_is_good = options[random_idx].is_good;
                    if (target_is_good)
                    {
                        _characterController.BeJoyful();
                    }
                    else
                    {
                        _characterController.BeThinking();
                    }
                }
            }

            // 総合点の差分に応じて、得点表示に演出が起こる
            _inGameUIDrower.ShowAdditionalScore(diffs[IObserver.Evaluation.Total]);

            // スコアを更新
            _scores = value;

            // UI表示も更新
            _inGameUIDrower.UpdateScore(_scores);
        }
    }

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

    [Tooltip("シーン開始時表示されるメッセージ")]
    [SerializeField] private MessageData _starting_message;

    [Tooltip("ゲーム中BGM")]
    [SerializeField] private AudioClip _ingame_bgm;
    [Tooltip("ゲームスタートSE")]
    [SerializeField] private AudioClip _start_se;
    [Tooltip("ゲーム終了SE")]
    [SerializeField] private AudioClip _finish_se;


    /// <summary>
    /// ゲーム開始処理
    /// </summary>
    private void Start()
    {
        // 始めはシューティング・録音不可能
        CanShoot = false;
        CanRecord = false;
        // 不必要なUIは隠す
        _reactionTelop.gameObject.SetActive(false);

        // 各採点項目を初期化
        foreach (IObserver.Evaluation item in Enum.GetValues(typeof(IObserver.Evaluation)))
        {
            Scores[item] = 0;
        }

        // 何の種類のケーキが食べたいか？を決定
        // 始めのメッセージの冒頭で「○○ケーキが食べたいな」と言わせる
        string request_tag = DecideCakeTagRandomly();
        _starting_message.Messages[0] = request_tag + "ケーキが食べたいな";

        // リクエストされた○○ケーキを、UIに表示
        _inGameUIDrower.UpdateRequest(request_tag + "ケーキ");

        // メッセージ終了時の処理を追加
        Action on_message_complete = delegate { };
        on_message_complete += _cameraController.LookDown;
        on_message_complete += (() => AudioManager.instance.SingleSEClip = _start_se);

        // 3, 2, 1, スタート表示後のアクション群を作成→コルーチンに渡す
        Action on_game_started = delegate { };
        on_game_started += (() => _do_count = true);
        on_game_started += (() => CanShoot = true);
        on_game_started += (() => CanRecord = true);
        on_game_started += (() => AudioManager.instance.BGMClip = _ingame_bgm);
        on_message_complete += (() => StartCoroutine(_inGameUIDrower.StartingCount(3, on_game_started)));

        // 始めのチュートリアルメッセージ表示
        _messageTelop.ShowMessage(_starting_message.Messages, on_message_complete);
    }

    /// <summary>
    /// タイマーを測る
    /// </summary>
    private void Update()
    {
        // タイマーを動かさないなら出る
        if (!_do_count)
        {
            return;
        }

        // タイマーを測る
        _current_time_s += Time.deltaTime;

        // 残り時間の算出
        float left_time_s = Mathf.Clamp(_time_limit_s - _current_time_s, 0f, _time_limit_s);
        // 残り時間を表示
        _inGameUIDrower.UpdateTimer(left_time_s);

        // もし残り時間0なら終了処理
        if (left_time_s <= 0)
        {
            ReportFinish();
        }
    }

    /// <summary>
    /// キャラクターが食べたいケーキの種類をランダムに決定
    /// </summary>
    /// <returns>キャラクターが今回食べたいケーキの種類</returns>
    private string DecideCakeTagRandomly()
    {
        // 選択肢 (表示名と内部タグのペア)
        var options = new (string displayName, string tag)[]
        {
            ("甘い", "Sweet"),
            ("辛い", "Spicy"),
            ("フルーツ", "Fruit"),
            ("お食事", "Savory"),
            ("高級", "Luxury")
        };

        int random_idx = UnityEngine.Random.Range(0, options.Length);
        TargetTag = options[random_idx].tag;
        return options[random_idx].displayName;
    }

    /// <summary>
    /// ケーキスコアをプラス
    /// </summary>
    /// <param name="addition">付加するスコア</param>
    public void AddScores(Dictionary<IObserver.Evaluation, int> addition)
    {
        // Scoresの更新値
        Dictionary<IObserver.Evaluation, int> new_score = new Dictionary<IObserver.Evaluation, int>();

        // 各採点項目について、new_scoreを算出
        foreach (IObserver.Evaluation item in Enum.GetValues(typeof(IObserver.Evaluation)))
        {
            new_score[item] = Scores[item] + addition[item];
        }

        // 更新
        Scores = new_score;
    }

    /// <summary>
    /// ゲーム終了処理。外部からも呼べる
    /// </summary>
    public void ReportFinish()
    {
        // ゲーム終了。各処理を無効化する
        StopAllCoroutines();
        _do_count = false;
        CanShoot = false;
        CanRecord = false;

        // FINISH!表示後のアクション群を作成（Ingameの終了→Resultへ遷移）
        Action on_game_finished = delegate { };
        on_game_finished += (() => _cakeManager.FinishDecoration());
        on_game_finished += (() => SequenceManager.CurrentSecuence = SequenceManager.Sequence.Result);

        StartCoroutine(_inGameUIDrower.ShowFinish(on_game_finished));
        // 効果音
        AudioManager.instance.SingleSEClip = _finish_se;
    }
}
