using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ケーキに乗せられたトッピングの情報をもとに、採点を行うクラス
/// </summary>
public class CakeEvaluator : MonoBehaviour
{
    [Tooltip("Balance点における、距離差・角度差それぞれの最大値")]
    [SerializeField]
    private int _each_balance_max = 20;

    [Tooltip("Quantity点における、トッピング1つ当たりの追加点")]
    [SerializeField]
    private int _each_topping_score = 3;

    [Tooltip("Variety点における、トッピング種類1つ当たりの追加点")]
    [SerializeField]
    private int _each_kind_score = 6;

    /// <summary>
    /// ケーキのBalance採点を実行する
    /// </summary>
    /// <param name="toppings">ケーキに乗っているトッピング一覧</param>
    /// <param name="cluster">採点対象のクラスタ</param>
    /// <param name="cluster_width">1クラスタの幅</param>
    public int EvaluateBalance(
        List<Topping> toppings,
        CakeManager.Cluster cluster,
        float cluster_width
    )
    {
        // 座標重心と、ケーキの中心との距離差の標準偏差を計算
        float distance_sd = CalcDistanceStandardDeviation(toppings, cluster);
        // 標準偏差が求められなかった場合、値は最悪値cluster_widthとする
        if (distance_sd == -1)
        {
            distance_sd = cluster_width;
        }

        // トッピング間の角度差の標準偏差を計算
        float angle_sd = CalcAngleStandardDeviation(toppings, cluster);
        // 標準偏差が求められなかった場合、値は最悪値2πとする
        if (angle_sd == -1)
        {
            angle_sd = 2 * Mathf.PI;
        }

        // それぞれ点数化(_each_balance_maxを最大値とし、そこから減点方式)
        float distance_score = (cluster_width - distance_sd) / cluster_width * _each_balance_max; // 距離差標準偏差の理論上の最悪値はcluster_width
        Debug.Log("距離差スコア：" + distance_score);
        float angle_score = (2 * Mathf.PI - angle_sd) / (2 * Mathf.PI) * _each_balance_max; // 角度差標準偏差の理論上の最悪値は2π
        Debug.Log("角度差スコア：" + angle_score);

        // 出力
        return (int)(distance_score + angle_score);
    }

    /// <summary>
    /// ケーキのQuantity採点を実行する
    /// </summary>
    /// <param name="toppings">ケーキに乗っているトッピング一覧</param>
    /// <param name="mode">評価モード(0:小さい, 1:普通, 2:大きい)</param>
    public int EvaluateQuantity(List<Topping> toppings, int mode = 1)
    {
        int count = toppings.Count;

        // modeに応じて頂点（目標数 target）とばらつき（sigma）を設定
        // 0: 小さい(5個), 1: 普通(10個), 2: 大きい(15個)
        float target = mode switch
        {
            0 => 10f,
            1 => 15f,
            2 => 20f,
            _ => 15f,
        };

        // ガウス関数を用いた採点関数
        int EvaluateComplex()
        {
            const float maxScore = 30f;
            float sigma = target / 2.0f; // 理想値の半分を標準偏差とすることで、0の時に点数が低くなるように調整

            // ガウス関数: Score = maxScore * exp( - (x - target)^2 / (2 * sigma^2) )
            float exponent = -Mathf.Pow(count - target, 2) / (2f * Mathf.Pow(sigma, 2));
            float score = maxScore * Mathf.Exp(exponent);

            return Mathf.Max(0, Mathf.FloorToInt(score));
        }

        // デバッグログを出力し、基本実装としてガウス関数モデルを返却
        Debug.Log($"[Quantity] count:{count}, mode:{mode} | Gauss:{EvaluateComplex()}");
        return EvaluateComplex();
    }

    /// <summary>
    /// ケーキのVariety採点を実行する
    /// </summary>
    /// <param name="topping_ids">トッピングの種類一覧</param>
    /// <param name="mode">評価モード(0:小さい, 1:普通, 2:大きい)</param>
    public int EvaluateVariety(HashSet<string> topping_ids, int mode = 1)
    {
        int count = topping_ids.Count;

        // modeに応じて頂点（目標数 target）とばらつき（sigma）を設定
        // 0: 小さい(2種), 1: 普通(4種), 2: 大きい(6種)
        float target = mode switch
        {
            0 => 3f,
            1 => 5f,
            2 => 7f,
            _ => 5f,
        };

        // ガウス関数を用いた採点関数
        int EvaluateComplex()
        {
            const float maxScore = 24f;
            float sigma = target / 2.0f;

            float exponent = -Mathf.Pow(count - target, 2) / (2f * Mathf.Pow(sigma, 2));
            float score = maxScore * Mathf.Exp(exponent);

            return Mathf.Max(0, Mathf.FloorToInt(score));
        }

        // デバッグログを出力し、基本実装としてガウス関数モデルを返却
        Debug.Log($"[Variety] count:{count}, mode:{mode} | Complex(Gauss):{EvaluateComplex()}");
        return EvaluateComplex();
    }

    /// <summary>
    /// クラスターのトッピングにおける、中心からの距離の標準偏差の計算
    /// </summary>
    /// <param name="toppings">ケーキに乗っかっているトッピング</param>
    /// <param name="cluster">計算に用いるクラスター</param>
    /// <returns>重心座標</returns>
    private float CalcDistanceStandardDeviation(List<Topping> toppings, CakeManager.Cluster cluster)
    {
        // 要素数が1以下なら、標準偏差は計算不可能。出力は-1とする
        if (cluster.size <= 1)
        {
            return -1;
        }

        // 中心からの距離の分散の計算(平均はクラスタ内で既に計算済み)
        float sum_deviation = 0;
        foreach (int idx in cluster.indices)
        {
            Vector2 topping_pos = new Vector2(
                toppings[idx].transform.localPosition.x,
                toppings[idx].transform.localPosition.z
            );
            float dist = Vector2.Distance(topping_pos, Vector2.zero); // ケーキオブジェクトがトッピングの親であるため、その中心座標は、絶対に0, 0
            sum_deviation += Mathf.Pow(cluster.average_dist - dist, 2);
        }
        float var = sum_deviation / (float)cluster.size;

        // 標準偏差を返す
        return Mathf.Sqrt(var);
    }

    /// <summary>
    /// クラスターのトッピングにおける、角度差の標準偏差の計算
    /// </summary>
    /// <param name="toppings">ケーキに乗っかっているトッピング</param>
    /// <param name="cluster">計算に用いるクラスター</param>
    /// <param name="center">ケーキの中心</param>
    /// <returns>標準偏差</returns>
    private float CalcAngleStandardDeviation(List<Topping> toppings, CakeManager.Cluster cluster)
    {
        // 要素数が1以下なら、角度差の標準偏差は計算不可能。出力は-1とする
        if (cluster.size <= 1)
        {
            return -1;
        }

        // 座標をリストに格納する
        List<Vector2> points = new List<Vector2>(); // 偏角ソートを行う用の座標格納リスト

        foreach (int i in cluster.indices)
        {
            float x = toppings[i].transform.localPosition.x;
            float y = toppings[i].transform.localPosition.z;
            Vector2 point = new Vector2(x, y);
            points.Add(point);
        }

        // 偏角でソートするためのメソッド
        int DeclinationSort(Vector2 p1, Vector2 p2)
        {
            return Mathf.Atan2(p1.y, p1.x).CompareTo(Mathf.Atan2(p2.y, p2.x));
        }

        // 偏角ソートを行う
        points.Sort((a, b) => DeclinationSort(a, b));
        // 最後の要素に最初の要素を追加
        points.Add(points[0]);

        // 角度差の平均の計算
        List<float> angle_diffs = new List<float>(); // 角度差格納用リスト
        float sum = 0;

        for (int i = 1; i < points.Count; i++)
        {
            float diff =
                Mathf.Atan2(points[i].y, points[i].x)
                - Mathf.Atan2(points[i - 1].y, points[i - 1].x); // 角度差
            // 角度差がマイナスになった = πから-πへのまたぎが発生した場合
            if (diff < 0)
            {
                diff += Mathf.PI * 2f; // 2πを足して帳尻を合わせる
            }

            angle_diffs.Add(diff); // 算出した角度差をリストに格納
            sum += diff;
        }
        float ave = sum / angle_diffs.Count; // きれいに並んでいれば、2π / Nに近づく

        // 角度差の分散の計算
        float sum_deviation = 0;
        foreach (float diff in angle_diffs)
        {
            sum_deviation += Mathf.Pow(ave - diff, 2);
        }
        float var = sum_deviation / angle_diffs.Count;

        // 標準偏差を返す
        return Mathf.Sqrt(var);
    }

    /// <summary>
    /// お題のタグと一致するトッピングの割合を評価する (最大30点)
    /// </summary>
    /// <param name="toppings">ケーキに乗っているトッピング一覧</param>
    /// <param name="targetTag">お題のターゲットタグ（"Sweet", "Spicy" など）</param>
    public int EvaluateTagMatch(List<Topping> toppings, string targetTag)
    {
        if (toppings == null || toppings.Count == 0 || string.IsNullOrEmpty(targetTag))
        {
            return 0;
        }

        int matchCount = 0;
        foreach (var topping in toppings)
        {
            if (topping.Info.tags != null && System.Array.IndexOf(topping.Info.tags, targetTag) >= 0)
            {
                matchCount++;
            }
        }

        // 割合の算出 (0.0 〜 1.0)
        float matchRate = (float)matchCount / toppings.Count;

        // 個数による減衰係数の算出 (1個=0.1, 2個=0.2, ... 10個以上=1.0)
        // これにより、トッピングが少量かつ全てマッチしている場合の満点獲得を防止します。
        float countMultiplier = Mathf.Min(1.0f, matchCount * 0.1f);

        // 最終スコアの算出 (最大30点)
        int score = Mathf.RoundToInt(matchRate * countMultiplier * 30f);
        
        Debug.Log($"[TagMatch] Match Count: {matchCount}/{toppings.Count} (お題: {targetTag}) | Rate: {matchRate:P0} | CountMultiplier: {countMultiplier:F1} | Score: {score}");
        return score;
    }
}
