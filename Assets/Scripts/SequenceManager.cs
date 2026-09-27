using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲームのシーン遷移やゲーム終了処理を担う
/// </summary>
public class SequenceManager : MonoBehaviour
{
    /// <summary>
    /// シーン状況を表す列挙型
    /// </summary>
    public enum Sequence
    {
        EndGame,    // ゲーム終了
        Title, 
        CakeMaking,
        Tutorial, 
        Result
    };

    [Tooltip("現在のシーン名")]
    private static Sequence _current_sequence = Sequence.Title;
    /// <summary>
    /// _currnet_sequenceのプロパティ
    /// 変更時、シーケンス遷移が起こる
    /// </summary>
    public  static Sequence CurrentSecuence
    {
        get
        {
            return _current_sequence;
        }
        set
        {
            // 更新値がEndGameなら、ゲーム終了
            if (value == Sequence.EndGame)
            {
                QuitGame();
            }
            // でなければ、更新値のシーンへ遷移
            else
            {
                ChangeScene(value.ToString());
            }
            _current_sequence = value;
        }
    }


    /// <summary>
    /// 現在のシーン名の取得
    /// </summary>
    private void Start()
    {
        string scene_name = SceneManager.GetActiveScene().name;
        // 文字列をSequence型に変換
        Sequence sequence = (Sequence)Enum.Parse(typeof(Sequence), scene_name);
        _current_sequence = sequence;
    }

    /// <summary>
    /// 文字列から_current_sequenceを更新する
    /// 非静的なので、Buttonから呼び出し可能
    /// </summary>
    /// <param name="sequence">更新値(string型)。スペルミスに注意</param>
    public void UpdateCurrentSequence(string sequence_str)
    {
        // 引数の文字列をSequence型に変換
        Sequence sequence = (Sequence)Enum.Parse(typeof(Sequence), sequence_str);

        CurrentSecuence = sequence;
    }

    /// <summary>
    /// ゲーム終了処理
    /// </summary>
    public static void QuitGame()
    {
        // エディターを終了
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        // アプリを終了
        #else
            Application.Quit();
        #endif
    }

    /// <summary>
    /// シーン遷移を行う
    /// </summary>
    /// <param name="scene_name">遷移先のシーン名</param>
    public static void ChangeScene(string scene_name)
    {
        SceneManager.LoadScene(scene_name);
    }
}
