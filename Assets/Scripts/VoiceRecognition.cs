using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Whisper;
using Whisper.Utils;
using NMeCab.Specialized;
using UnityEngine;

/// <summary>
/// メソッドを呼んでいるだけなので、static化
/// </summary>
public static class VoiceRecognition
{
    [Header("Whisper Components")]
    public static WhisperManager whisper;
    public static MicrophoneRecord mic;

    [Header("AudioRecognizeResult")]
    public static string inputText;
    private static TaskCompletionSource<string> _recognitionTaskSource;

    [Header("TextToWord")]
    private static MeCabIpaDicTagger tagger;
    private static bool isInitialized = false;

    [Header("postposition")]
    private static string[] postpositions =
    {
        // 格助詞
        "が",
        "の",
        "を",
        "に",
        "へ",
        "と",
        "より",
        "から",
        "にて",
        "して",
        // 接続助詞
        "ば",
        "とも",
        "ど",
        "ども",
        "が",
        "に",
        "を",
        "て",
        "して",
        "で",
        "つつ",
        "ながら",
        "ものの",
        "ものを",
        "ものから",
        // 副助詞
        "だに",
        "すら",
        "さへ",
        "し",
        "い",
        "のみ",
        "ばかり",
        "など",
        "まで",
        // 係助詞
        "は",
        "も",
        "ぞ",
        "なむ",
        "や",
        "か",
        "こそ",
        // 終助詞
        "な",
        "そ",
        "ばや",
        "なむ",
        "に",
        "しか",
        "しが",
        "てしか",
        "てしがな",
        "もがな",
        "か",
        "かな",
        "かし",
        // 間投助詞
        "や",
        "よ",
        "を",
    };

    public static async Task<string> AudioLoading()
    {
        if (_recognitionTaskSource != null && !_recognitionTaskSource.Task.IsCompleted)
        {
            Debug.LogWarning("すでに音声認識が実行中です。");
            return await _recognitionTaskSource.Task;
        }

        _recognitionTaskSource = new TaskCompletionSource<string>();

        try
        {
            mic.OnRecordStop += OnVADStop;
            mic.StartRecord();
            Debug.Log("音声認識を開始しました。認識終了まで待機...");

            inputText = await _recognitionTaskSource.Task;
            return inputText;
        }
        catch (Exception e)
        {
            Debug.LogError($"音声認識の開始または待機中にエラーが発生しました: {e.Message}");
            _recognitionTaskSource.TrySetException(e);
            return string.Empty;
        }
        finally
        {
            mic.OnRecordStop -= OnVADStop;
        }
    }

    public static void StopRecord()
    {
        if (mic != null && mic.IsRecording)
        {
            mic.StopRecord();
        }
    }

    private static async void OnVADStop(AudioChunk chunk)
    {
        Debug.Log("発話終了を検知しました。解析中...");
        try
        {
            var res = await whisper.GetTextAsync(chunk.Data, chunk.Frequency, 1);

            if (res != null)
            {
                _recognitionTaskSource.TrySetResult(res.Result);
            }
            else
            {
                _recognitionTaskSource.TrySetResult(string.Empty);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"音声解析中にエラーが発生しました: {e.Message}");
            _recognitionTaskSource.TrySetException(e);
        }
    }

    private static void InitializeMeCab()
    {
        string dicPath = Path.Combine(Application.streamingAssetsPath, "NMeCab/dic/ipadic");

        if (!Directory.Exists(dicPath))
        {
            Debug.LogError($"[MeCab]辞書フォルダが見つかりません: {dicPath}");
            isInitialized = false;
            return;
        }

        try
        {
            tagger = MeCabIpaDicTagger.Create(dicPath);
            isInitialized = true;
            Debug.Log("[MeCab]初期化が完了しました。");
        }
        catch (Exception e)
        {
            Debug.LogError($"[MeCab]初期化中にエラーが発生しました: {e.Message}");
            isInitialized = false;
        }
    }

    private static bool IsPostposition(string word)
    {
        foreach (string postposition in postpositions)
        {
            if (postposition == word)
            {
                return true;
            }
        }
        return false;
    }

    public static string[] SplitTextToWords(string inputText)
    {
        if (!isInitialized || string.IsNullOrEmpty(inputText))
        {
            Debug.LogWarning("[Mecab]初期化が未完了か、入力が空です。");
            return new string[0];
        }
        List<string> wordList = new List<string>();

        foreach (var node in tagger.Parse(inputText))
        {
            if (!string.IsNullOrEmpty(node.Surface))
            {
                string splited_word = node.Surface;
                if (!IsPostposition(splited_word))
                {
                    wordList.Add(splited_word);
                }
            }
        }

        return wordList.ToArray();
    }

    /// <summary>
    /// MeCab初期化処理
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        Debug.Log("初期化プロセス...");

        InitializeMeCab();
    }
}
