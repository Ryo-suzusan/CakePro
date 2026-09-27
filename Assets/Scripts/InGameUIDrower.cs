using UnityEngine;
using System;
using TMPro;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// インゲーム内のUI描画を担う。
/// </summary>
public class InGameUIDrower : MonoBehaviour
{
    [Tooltip("点数の出力先。添え字は各採点項目(Cakemanagerの列挙型)に対応")]
    [SerializeField] private List<TMP_Text> _score_text;
    [Tooltip("スコア増減演出を行うアニメータ―")]
    [SerializeField] private Animator _score_animator;
    [Tooltip("残り時間の出力先")]
    [SerializeField] private TMP_Text _timer_text;
    [Tooltip("開始/終了カウントの出力先")]
    [SerializeField] private TMP_Text _begin_end_text;
    [Tooltip("シーン内のトッピング名表示先")]
    [SerializeField] private TMP_Text _topping_name;
    [Tooltip("シーン内の音声入力内容表示先")]
    [SerializeField] private TMP_Text _voice_input_text;
    [Tooltip("録音開始・終了を表すアイコンアニメータ―")]
    [SerializeField] private Animator _voice_icon_animator;
    [Tooltip("シーン内のリクエストケーキ表示先")]
    [SerializeField] private TMP_Text _request;


    /// <summary>
    /// スコアUIの更新
    /// </summary>
    /// <param name="scores">スコア更新内容</param>
    public void UpdateScore(Dictionary<IObserver.Evaluation, int> scores)
    {
        // ケーキの評価項目用列挙型の個数分、ループを回してUIを更新する
        foreach (IObserver.Evaluation item in Enum.GetValues(typeof(IObserver.Evaluation)))
        {
            // itemが示す評価項目に対応するUIがあるならば、更新
            if ((int)item >= 0 && (int)item < _score_text.Count)
            {
                _score_text[(int)item].SetText("SCORE : " + scores[item]);
            }
        }
    }

    /// <summary>
    /// 変化した分のスコアをアニメーションで表示
    /// </summary>
    /// <param name="additional_score"></param>
    public void ShowAdditionalScore(float additional_score)
    {
        // 数値をテキストに渡す
        TMP_Text additional_text = _score_animator.gameObject.GetComponent<TMP_Text>();
        if (additional_text != null)
        {
            additional_text.text = additional_score.ToString();
            if (additional_score > 0)
            {
                additional_text.text = "+" + additional_text.text;
            }
        }

        // ±に応じてアニメーションを変更
        if (additional_score < 0)
        {
            _score_animator.SetTrigger("down_trigger");
        }
        else
        {
            _score_animator.SetTrigger("up_trigger");
        }
    }

    /// <summary>
    /// タイマーUIの更新
    /// </summary>
    /// <param name="left_time_s">残り時間</param>
    public void UpdateTimer(float left_time_s)
    {
        _timer_text.text = "TIME : " + string.Format("{0:000}", left_time_s);
    }

    /// <summary>
    /// 音声入力表示UIを更新
    /// </summary>
    /// <param name="voice_input">音声入力内容</param>
    public void UpdateVoiceInput(string voice_input)
    {
        _voice_input_text.text = voice_input;
    }

    /// <summary>
    /// トッピング名表示UIを更新
    /// </summary>
    /// <param name="name">更新するトッピング名</param>
    public void UpdateToppingName(string name)
    {
        _topping_name.text = name;
    }

    /// <summary>
    /// 3, 2, 1, START!のコルーチン
    /// </summary>
    /// <param name="count">何秒数えるのか</param>
    public IEnumerator StartingCount(int count, System.Action callback)
    {
        _begin_end_text.gameObject.SetActive(true);

        // count秒数える
        while (count > 0)
        {
            _begin_end_text.text = count.ToString();
            count--;
            yield return new WaitForSeconds(1);
        }

        // Start!と出した後で
        _begin_end_text.text = "START!";
        yield return new WaitForSeconds(1);

        // テキストを隠す
        _begin_end_text.gameObject.SetActive(false);

        // ゲーム開始
        callback();
    }

    /// <summary>
    /// ゲーム終了の合図
    /// </summary>
    public IEnumerator ShowFinish(System.Action callback)
    {
        _begin_end_text.gameObject.SetActive(true);

        // FINISH!と出した後で三秒待ち
        _begin_end_text.text = "FINISH!";
        yield return new WaitForSeconds(3);

        // Ingameの終了→Resultへ遷移
        callback();
    }

    /// <summary>
    /// 録音開始・終了（成功）・失敗に応じてアイコンを表示する
    /// </summary>
    /// <param name="recording_state">録音の状態</param>
    public void ShowVoiceIcon(IObserver.RecordingState recording_state)
    {
        switch (recording_state)
        {
            case IObserver.RecordingState.Recording:
                _voice_icon_animator.SetTrigger("start_trigger");
                break;
            case IObserver.RecordingState.Success:
                _voice_icon_animator.SetTrigger("end_trigger");
                break;
            case IObserver.RecordingState.Failure:
                _voice_icon_animator.SetTrigger("failure_trigger");
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// リクエストUIの更新
    /// </summary>
    /// <param name="request">○○ケーキ</param>
    public void UpdateRequest(string request)
    {
        _request.text = request;
    }
}