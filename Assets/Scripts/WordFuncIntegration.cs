using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class WordFuncIntegration : MonoBehaviour
{
    [Tooltip("シーン内の監視者クラス（音声入力内容の表示用）")]
    private IObserver _iObserver;
    [Tooltip("シーン内の監視者クラスをアタッチしているObject")]
    [SerializeField] private GameObject _observer_obj;


    /// <summary>
    /// インターフェース取得
    /// </summary>
    void Start()
    {
        _iObserver = _observer_obj.GetComponent<IObserver>();
    }

    public async Task<FastTextController.ToppingMatch> WordsPipeline()
    {
        try
        {
            string result = await VoiceRecognition.AudioLoading();
            // 音声認識結果を渡す
            _iObserver.VoiceInput = result;

            string[] words = VoiceRecognition.SplitTextToWords(result);
            Debug.Log($"分割された単語数: {words.Length}");

            List<FastTextController.ToppingMatch> FastTextResult = await FastTextController.AnalyzeToppingsAsync(words);
            foreach (var match in FastTextResult)
            {
                Debug.Log(match.asset_id + " (Tags: " + string.Join(", ", match.tags) + ")");
            }
            if (FastTextResult.Count > 0)
            {
                return FastTextResult[0];
            }
            else
            {
                Debug.LogWarning("高類似度のオブジェクトなし");
                return null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"WordsPipeline実行中にエラーが発生しました: {e.Message}");
            return null;
        }
    }

    public void StopPipeline()
    {
        VoiceRecognition.StopRecord();
    }
}
