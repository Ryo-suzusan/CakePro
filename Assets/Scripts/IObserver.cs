using System.Collections.Generic;

/// <summary>
/// ケーキやシューティングといった、シーン内の状態監視処理を実装する
/// </summary>
public interface IObserver
{
    /// <summary>
    /// 採点項目
    /// </summary>
    [System.Serializable]
    public enum Evaluation
    {
        Total = 0,
        Balance = 1,
        Quantity = 2,
        Variety = 3,
        TagMatch = 4,
    }

    /// <summary>
    /// 現在食べたいケーキのターゲットタグ（英語）
    /// </summary>
    string TargetTag { get; }

    /// <summary>
    /// 現在のケーキスコア
    /// </summary>
    Dictionary<Evaluation, int> Scores { get; set; }

    /// <summary>
    /// ケーキスコアをプラス
    /// </summary>
    /// <param name="addition">付加するスコア</param>
    void AddScores(Dictionary<Evaluation, int> addition);

    /// <summary>
    /// 今、シューティング可能か
    /// </summary>
    bool CanShoot { get; }

    /// <summary>
    /// 今、音声入力可能か
    /// </summary>
    bool CanRecord { get; }

    /// <summary>
    /// 音声入力された文章
    /// </summary>
    string VoiceInput { get; set; }

    /// <summary>
    /// トッピング名として取得した文章
    /// </summary>
    string Recorded { get; set; }

    /// <summary>
    /// 音声入力の状態
    /// </summary>
    public enum RecordingState
    {
        Recording,
        Success,
        Failure
    }

    /// <summary>
    /// 現在の入力の状態
    /// </summary>
    RecordingState CurrentRCState { get; set; }
}
