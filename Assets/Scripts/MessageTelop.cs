using UnityEngine;
using TMPro;
using System;
using UnityEngine.EventSystems; // IPointerClickHandlerを使うために必要

/// <summary>
/// キャラクターのメッセージを司る
/// </summary>
public class MessageTelop : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("メッセージ表示先Text")]
    [SerializeField] private TMP_Text _message_text;
    [Tooltip("メッセージを次に進める効果音")]
    [SerializeField] private AudioClip _message_se;

    [Tooltip("メッセージウィンドウの親オブジェクト（表示・非表示の切り替え用）")]
    [SerializeField] private GameObject _message_window;

    [Tooltip("再生するメッセージ群")]
    private string[] _current_messages;
    [Tooltip("現在何ページ目を読んでいるか")]
    private int _current_idx;
    [Tooltip("メッセージがすべて終わったときに実行する予約処理")]
    private event Action _onCompleteCallback = delegate { };
    [Tooltip("現在メッセージ表示中かどうかのフラグ")]
    private bool _is_showing = false;


    /// <summary>
    /// クリックで次に進むメッセージの再生
    /// </summary>
    /// <param name="messages">再生するメッセージ</param>
    /// <param name="on_complete">【省略可】終わった後に実行したいメソッドや処理</param>
    public void ShowMessage(string[] messages, Action on_complete = null)
    {
        // データを初期化して保持
        _current_messages = messages;
        _current_idx = 0;
        _onCompleteCallback = on_complete;
        _is_showing = true;

        // ウィンドウを表示して1ページ目を出す
        if (_message_window != null) _message_window.SetActive(true);
        DisplayCurrentMessage();
    }

    /// <summary>
    /// 現在のインデックスのメッセージを画面に反映
    /// </summary>
    private void DisplayCurrentMessage()
    {
        _message_text.text = _current_messages[_current_idx];
    }

    /// <summary>
    /// Joy-Conポインター等のクリックを検知して次のメッセージに進む
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_is_showing) return;

        _current_idx++;

        // 効果音
        AudioManager.instance.SingleSEClip = _message_se;

        // まだ次のセリフがあるか？
        if (_current_idx < _current_messages.Length)
        {
            DisplayCurrentMessage();
        }
        else
        {
            // すべて読み終わったので閉じる
            EndMessage();
        }
    }

    // メッセージ表示を終了し、予約されていた処理を実行する
    private void EndMessage()
    {
        _is_showing = false;
        if (_message_window != null) _message_window.SetActive(false);

        // 「終わった時にやってね」と頼まれていた処理（引数で渡されたAction）をここで実行する
        _onCompleteCallback();
    }
}