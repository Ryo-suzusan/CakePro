using UnityEngine;
using Whisper;
using Whisper.Utils;

/// <summary>
/// 静的クラスVoiceRecognitionのフィールドを初期化する仲介クラス
/// </summary>
[RequireComponent(typeof(WhisperManager))]
[RequireComponent(typeof(MicrophoneRecord))]
public class VoiceRecognitionInitializer : MonoBehaviour
{
    [Tooltip("自身のゲームオブジェクトにアタッチされているWhisperManager")]
    private WhisperManager _whisperManager;
    [Tooltip("自身のゲームオブジェクトにアタッチされているMicrophoneRecord")]
    private MicrophoneRecord _microphoneRecord;

    /// <summary>
    /// フィールドアタッチ処理
    /// </summary>
    private void Awake()
    {
        _whisperManager = GetComponent<WhisperManager>();
        _microphoneRecord = GetComponent<MicrophoneRecord>();

        VoiceRecognition.whisper = _whisperManager;
        VoiceRecognition.mic = _microphoneRecord;
    }
}
