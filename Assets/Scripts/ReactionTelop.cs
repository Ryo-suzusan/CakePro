using UnityEngine;
using TMPro;
using System.Collections; // IPointerClickHandlerを使うために必要

/// <summary>
/// キャラクターのメッセージを司る
/// </summary>
public class ReactionTelop : MonoBehaviour
{
    [Tooltip("メッセージ表示先Text")]
    [SerializeField] private TMP_Text _message_text;

    [Tooltip("メッセージウィンドウの親オブジェクト（表示・非表示の切り替え用）")]
    [SerializeField] private GameObject _message_window;

    [Tooltip("再生するメッセージ")]
    private string _current_message;

    [Tooltip("現在メッセージを表示中か")]
    private bool _is_showing = false;
    public  bool IsShowing { get { return _is_showing; } }
    [Tooltip("メッセージを表示する長さ(単位：秒)")]
    private int _showing_time = 2;


    /// <summary>
    /// リアクションメッセージの再生
    /// </summary>
    /// <param name="message">再生するメッセージ</param>
    public void ShowMessage(string message)
    {
        // データを初期化して保持
        _current_message = message;

        // ウィンドウを表示
        if (_message_window != null) _message_window.SetActive(true);
        _message_text.text = _current_message;

        // メッセージを`_showing_time`秒表示
        StartCoroutine(ShowReactionMessage(_showing_time));
    }

    /// <summary>
    /// メッセージを引数秒間表示
    /// </summary>
    /// <param name="count">何秒表示するか</param>
    private IEnumerator ShowReactionMessage(int count)
    {
        _is_showing = true;

        // `count`秒待って
        yield return new WaitForSeconds(count);

        // ウィンドウ非表示
        if (_message_window != null) _message_window.SetActive(false);
        _is_showing = false;
    }
}