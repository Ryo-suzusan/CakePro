using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

public class PythonServerManager : MonoBehaviour
{
    public static PythonServerManager Instance { get; private set; }

    [Header("Server Connection Settings")]
    [SerializeField] private string host = "127.0.0.1";
    [SerializeField] private int port = 8000;
    [Tooltip("実行ファイルの拡張子を除いた名前 (例: main, main_2 など)")]
    [SerializeField] private string executableName = "main_2";
    
    private Process _serverProcess;
    private bool _isServerReady = false;

    /// <summary>
    /// Pythonサーバーが完全に起動し、APIを受け付ける準備ができたかどうか
    /// </summary>
    public bool IsServerReady => _isServerReady;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        StartServerProcess();
    }

    private void StartServerProcess()
    {
        try
        {
            // 既存の同名ゾンビプロセスが残っている場合は事前に強制終了する
            try
            {
                foreach (var p in Process.GetProcessesByName(executableName))
                {
                    try
                    {
                        UnityEngine.Debug.Log($"古いPythonサーバープロセスを検出したため、終了します: PID {p.Id}");
                        p.Kill();
                        p.WaitForExit(1000);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning($"古いプロセスの終了中にエラーが発生しました: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"既存プロセスの検出中に例外が発生しました: {ex.Message}");
            }

            string serverPath = "";
            string workingDir = "";

            // 実行プラットフォーム（あるいはエディタ上のOS）に応じて実行ファイルパスを切り替え
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            string binaryName = executableName + ".exe";
            serverPath = Path.Combine(Application.streamingAssetsPath, $"Server/Windows/{binaryName}");
            workingDir = Path.Combine(Application.streamingAssetsPath, "Server/Windows");
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            string binaryName = executableName;
            serverPath = Path.Combine(Application.streamingAssetsPath, $"Server/Mac/{binaryName}");
            workingDir = Path.Combine(Application.streamingAssetsPath, "Server/Mac");

            // macOSにおける実行権限（chmod +x）の付与
            GiveExecutionPermission(serverPath);
#else
            UnityEngine.Debug.LogWarning("サポートされていないプラットフォームです。Pythonサーバーの自動起動は行われません。");
            return;
#endif


            // パス修正（空白やエスケープ対策）
            serverPath = Path.GetFullPath(serverPath);
            workingDir = Path.GetFullPath(workingDir);

            if (!File.Exists(serverPath))
            {
                UnityEngine.Debug.LogError($"Pythonサーバーの実行ファイルが見つかりません: {serverPath}\n" +
                                           $"StreamingAssets/Server/ 配下にビルドしたフォルダが正しく配置されているか確認してください。");
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = serverPath,
                WorkingDirectory = workingDir,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            _serverProcess = new Process { StartInfo = startInfo };

            // サーバーの標準出力/標準エラー出力をUnityのコンソールへ転送
            _serverProcess.OutputDataReceived += (sender, e) => {
                if (!string.IsNullOrEmpty(e.Data)) UnityEngine.Debug.Log($"[Python Server]: {e.Data}");
            };
            _serverProcess.ErrorDataReceived += (sender, e) => {
                if (!string.IsNullOrEmpty(e.Data)) UnityEngine.Debug.LogWarning($"[Python Server Error]: {e.Data}");
            };

            _serverProcess.Start();
            _serverProcess.BeginOutputReadLine();
            _serverProcess.BeginErrorReadLine();

            UnityEngine.Debug.Log($"Pythonサーバープロセスを起動しました:\nファイル: {serverPath}\nカレントディレクトリ: {workingDir}");

            // 非同期でサーバーの起動状態（ヘルスチェック）を監視
            Task.Run(WaitForServerReady);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Pythonサーバーの起動中にエラーが発生しました: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// macOS/Linux向けに実行ファイルへの実行権限を付与する
    /// </summary>
    private void GiveExecutionPermission(string filePath)
    {
        try
        {
            UnityEngine.Debug.Log($"macOSの実行権限（chmod +x）を付与しています: {filePath}");
            var procInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"chmod +x '{filePath}'\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };
            using (var proc = Process.Start(procInfo))
            {
                proc.WaitForExit();
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"実行権限の付与中にエラーが発生しました: {e.Message}");
        }
    }

    /// <summary>
    /// ローカルHTTPサーバーがリクエストを受信できるようになるまでポーリングで確認する
    /// </summary>
    private async Task WaitForServerReady()
    {
        // FastAPIのdocsエンドポイント等を利用して疎通確認を行う
        string checkUrl = $"http://{host}:{port}/docs";
        using (var client = new HttpClient())
        {
            client.Timeout = TimeSpan.FromSeconds(1.5);
            int maxRetries = 40; // 最大約40秒（モデルロード時間を考慮して長めに設定）
            
            for (int i = 0; i < maxRetries; i++)
            {
                if (_serverProcess == null || _serverProcess.HasExited)
                {
                    UnityEngine.Debug.LogError("Pythonサーバーが起動直後に強制終了しました。ログを確認してください。");
                    break;
                }

                try
                {
                    var response = await client.GetAsync(checkUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        _isServerReady = true;
                        UnityEngine.Debug.Log("<color=lime>Pythonサーバーの起動と言語モデルのロードが完了しました！準備OKです。</color>");
                        break;
                    }
                }
                catch
                {
                    // 起動中のため通信エラーになるが、想定内のため無視してループを継続
                }

                await Task.Delay(1000); // 1秒間隔でリトライ
            }
        }

        if (!_isServerReady)
        {
            UnityEngine.Debug.LogError("Pythonサーバーの起動確認がタイムアウトしました。モデルのサイズが大きいか、起動エラーが発生している可能性があります。");
        }
    }

    private void OnApplicationQuit()
    {
        StopServerProcess();
    }

    private void OnDestroy()
    {
        StopServerProcess();
    }

    /// <summary>
    /// バックグラウンドのサーバープロセスを安全に強制終了する
    /// </summary>
    private void StopServerProcess()
    {
        if (_serverProcess != null && !_serverProcess.HasExited)
        {
            try
            {
                UnityEngine.Debug.Log("Pythonサーバープロセスを終了しています...");
                _serverProcess.Kill();
                // 最大二秒待機（二秒以内にプロセスをkillできなかった場合、Warningを表示）
                bool exited = _serverProcess.WaitForExit(2000);

                if (!exited)
                {
                    UnityEngine.Debug.LogWarning("Pythonプロセスが2秒以内に終了しなかったため、強制的にDisposeします。OS側に残る可能性があります。");
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"プロセスの終了中にエラーが発生しました: {e.Message}");
            }
            finally
            {
                _serverProcess.Dispose();
                _serverProcess = null;
                _isServerReady = false;
                UnityEngine.Debug.Log("Pythonサーバープロセスを完全にクローズしました。");
            }
        }
    }
}
