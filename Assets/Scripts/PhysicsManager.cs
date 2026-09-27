using UnityEngine;

/// <summary>
/// 物理シミュレーションの速度をカスタム管理する静的クラス
/// </summary>
public static class PhysicsManager
{
    [Tooltip("物理演算の速さ")]
    public static float kSimulationSpeed = 0.5f;

    /// <summary>
    /// ゲーム起動時に自動的に呼び出され、物理シミュレーションの設定とループを開始します
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // 最新のUnityでは Physics.autoSimulation は非推奨（Deprecated）になったため、
        // simulationMode を使用してスクリプト主導のシミュレーションに切り替えます
        Physics.simulationMode = SimulationMode.Script;

        // シミュレーションを毎フレーム実行するためのループ用オブジェクトを裏で生成
        GameObject ticker = new GameObject("PhysicsTicker");
        ticker.AddComponent<PhysicsTicker>();
        Object.DontDestroyOnLoad(ticker);
    }

    /// <summary>
    /// 物理シミュレーションをステップ進行させます（内部のTickerから毎フレーム呼ばれます）
    /// </summary>
    public static void UpdateSimulation()
    {
        float fixedDeltaTime = Time.fixedDeltaTime;

        // 1. 整数部：速度に応じて複数回シミュレーションを回す
        int speedInt = (int)kSimulationSpeed;
        for (int i = 0; i < speedInt; ++i)
        {
            Physics.Simulate(fixedDeltaTime);
        }

        // 2. 小数点以下：余った端数の時間分だけシミュレーションを回す
        float speedDecimals = kSimulationSpeed - speedInt;
        if (speedDecimals > 0f)
        {
            Physics.Simulate(fixedDeltaTime * speedDecimals);
        }
    }

    /// <summary>
    /// FixedUpdateのタイミングをフックするためだけの内部用コンポーネント
    /// </summary>
    private class PhysicsTicker : MonoBehaviour
    {
        private void FixedUpdate()
        {
            PhysicsManager.UpdateSimulation();
        }
    }
}