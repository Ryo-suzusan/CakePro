using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// メソッドを呼んでいるだけなので、static化
/// </summary>
public static class FastTextController
{
    private static string apiUrl = "http://localhost:8000/search";

    [System.Serializable]
    public class RequestData { public string[] words; }

    [System.Serializable]
    public class ResponseItem
    {
        public string input_word;
        public string asset_id;
        public float score;
        public string status;
        public string[] tags;
    }

    [System.Serializable]
    public class ResponseData
    {
        public ResponseItem[] results;
    }

    [System.Serializable]
    public class ToppingMatch
    {
        public string asset_id;
        public string[] tags;
    }

    private static TaskCompletionSource<List<ToppingMatch>> _analyzeTaskSource;

    public static async Task<List<ToppingMatch>> AnalyzeToppingsAsync(string[] nouns)
    {
        if (_analyzeTaskSource != null && !_analyzeTaskSource.Task.IsCompleted)
        {
            Debug.LogWarning("すでにAI通信が実行中です。");
            return await _analyzeTaskSource.Task;
        }

        if (nouns == null || nouns.Length == 0)
        {
            return new List<ToppingMatch>();
        }

        _analyzeTaskSource = new TaskCompletionSource<List<ToppingMatch>>();

        try
        {
            RequestData reqData = new RequestData { words = nouns };
            string jsonToSend = JsonUtility.ToJson(reqData);

            UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonToSend);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            var operation = request.SendWebRequest();
            operation.completed += OnRequestCompleted;

            List<ToppingMatch> result = await _analyzeTaskSource.Task;
            return result;
        }
        catch (Exception e)
        {
            Debug.LogError($"通信の開始中にエラーが発生しました: {e.Message}");
            _analyzeTaskSource.TrySetException(e);
            return new List<ToppingMatch>();
        }
    }

    private static void OnRequestCompleted(AsyncOperation operation)
    {
        // イベントから送られてきた情報を UnityWebRequest として取り出す
        var webRequestOp = operation as UnityWebRequestAsyncOperation;
        var request = webRequestOp.webRequest;

        try
        {
            List<ToppingMatch> topMatches = new List<ToppingMatch>();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("通信エラー: " + request.error);
            }
            else
            {
                ResponseData resData = JsonUtility.FromJson<ResponseData>(request.downloadHandler.text);

                if (resData != null && resData.results != null && resData.results.Length > 0)
                {
                    int count = Mathf.Min(resData.results.Length, 3);

                    for (int i = 0; i < count; i++)
                    {
                        var item = resData.results[i];

                        if (item.status == "success")
                        {
                            topMatches.Add(new ToppingMatch { asset_id = item.asset_id, tags = item.tags });
                            Debug.Log($"<color=lime>類似度判定 第{i + 1}位: [{item.input_word}] ⇒ {item.asset_id} (スコア: {item.score})</color>");
                        }
                    }
                }
            }

            _analyzeTaskSource.TrySetResult(topMatches);
        }
        catch (Exception e)
        {
            Debug.LogError($"解析中にエラーが発生しました: {e.Message}");
            _analyzeTaskSource.TrySetException(e);
        }
        finally
        {
            request.Dispose();
        }
    }
}