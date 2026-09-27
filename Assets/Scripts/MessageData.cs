using UnityEngine;

/// <summary>
/// セリフファイルを定義するデータ構造
/// </summary>
[CreateAssetMenu(fileName = "NewMessageData", menuName = "Dialog/MessageData")]
public class MessageData : ScriptableObject
{
    [Tooltip("どんなイベント用のセリフか（メモ用）")]
    public string EventName;

    [Tooltip("順番に表示したいセリフの配列")]
    public string[] Messages;
}