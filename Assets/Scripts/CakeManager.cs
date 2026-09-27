using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ケーキに乗っているトッピングの状態を保持・管理するクラス
/// </summary>
[RequireComponent(typeof(CakeEvaluator))]
[RequireComponent(typeof(CakeController))]
public class CakeManager : MonoBehaviour
{
    [Tooltip("シーン内の監視者クラス")]
    private IObserver _iObserver;
    [Tooltip("シーン内の監視者クラスをアタッチしているObject")]
    [SerializeField] private GameObject _observer_obj;

    [Tooltip("ケーキに乗っているトッピング一覧。中心からの距離順にソート")]
    private List<Topping> _toppings = new List<Topping>();
    [Tooltip("ケーキに乗っているトッピングの識別名一覧。探索・追加は平均O(1)。")]
    private HashSet<string> _topping_ids = new HashSet<string>();
    [Tooltip("中心からの距離に応じたクラスタ")]
    private List<Cluster> _clusters = new List<Cluster>();

    [Tooltip("中心からの距離に応じたグループ分類の閾値")]
    [SerializeField] private float _radius_threshold = 0.08f;
    [Tooltip("高さに応じたグループ分類の閾値")]
    [SerializeField] private float _height_threshold = 0.2f;

    [Tooltip("採点処理を実行するクラス")]
    private CakeEvaluator _cakeEvaluator;
    [Tooltip("ケーキの挙動を担うクラス")]
    private CakeController _cakeController;

    [Tooltip("ケーキにトッピング命中時のSE")]
    [SerializeField] private AudioClip _hit_se;


    /// <summary>
    /// 各種初期化
    /// </summary>
    private void Start()
    {
        // クラス取得
        _cakeEvaluator = GetComponent<CakeEvaluator>();
        _cakeController = GetComponent<CakeController>();

        // インターフェース取得
        _iObserver = _observer_obj.GetComponent<IObserver>();
    }

    /// <summary>
    /// クラスタ = 1つの円を構成するトッピング群
    /// </summary>
    public class Cluster
    {
        [Tooltip("クラスタを構成するトッピングの種類")]
        public string topping_name;
        [Tooltip("高さの平均")]
        public float average_height;
        [Tooltip("ケーキからの距離の平均")]
        public float average_dist;

        [Tooltip("クラスタを構成するトッピングのインデックス")]
        public List<int> indices = new List<int>();
        [Tooltip("クラスタを構成するインデックスの個数")]
        public int size => indices.Count;

        [Tooltip("クラスタのバランス点")]
        public int balance_score;
    }


    /// <summary>
    /// トッピング追加処理
    /// </summary>
    public void AddTopping(Topping topping)
    {
        // 効果音
        AudioManager.instance.SingleSEClip = _hit_se;
        //// 振動（軽く短い）
        //JoyconPlayer.instance.Rumble(160, 320, 0.1f, 25);
        // エフェクト
        StartCoroutine(_cakeController.SpawnEffect(new Vector3(
            topping.gameObject.transform.position.x, 
            topping.gameObject.transform.position.y - 0.04f /* : 下側に補正 */, 
            topping.gameObject.transform.position.z 
            ) ));

        // トッピング一覧に追加
        _toppings.Add(topping);
        // トッピング名一覧に追加
        _topping_ids.Add(topping.Info.name);
        // 追加されたトッピングの添え字
        int topping_idx = _toppings.Count - 1;

        // 適切なクラスターに追加
        int added_cluster_id = AddClusterIndex(topping_idx);   // 追加先のクラスタの添え字も保持しておく

        //// 追加先のクラスターに応じて、色を付ける
        //_cakeController.ColorTopping(added_cluster_id, topping);

        // 採点処理

        // 採点結果
        Dictionary<IObserver.Evaluation, int> addition_scores = new Dictionary<IObserver.Evaluation, int>();
        // 各採点結果を初期化
        foreach (IObserver.Evaluation item in Enum.GetValues(typeof(IObserver.Evaluation)))
        {
            addition_scores[item] = 0;
        }
        // 1. Balance点
        int new_balance_score = _cakeEvaluator.EvaluateBalance(_toppings, _clusters[added_cluster_id], _radius_threshold);
        addition_scores[IObserver.Evaluation.Balance] = new_balance_score - _clusters[added_cluster_id].balance_score;
        _clusters[added_cluster_id].balance_score = new_balance_score;
        // 2. Quantity点
        int new_quantity_score = _cakeEvaluator.EvaluateQuantity(_toppings);
        addition_scores[IObserver.Evaluation.Quantity] = new_quantity_score - _iObserver.Scores[IObserver.Evaluation.Quantity];
        // 3. Variety点
        int new_variety_score = _cakeEvaluator.EvaluateVariety(_topping_ids);
        addition_scores[IObserver.Evaluation.Variety] = new_variety_score - _iObserver.Scores[IObserver.Evaluation.Variety];
        // 4. TagMatch点
        int new_tag_match_score = _cakeEvaluator.EvaluateTagMatch(_toppings, _iObserver.TargetTag);
        addition_scores[IObserver.Evaluation.TagMatch] = new_tag_match_score - _iObserver.Scores[IObserver.Evaluation.TagMatch];
        // 5. Total点
        addition_scores[IObserver.Evaluation.Total] =
            addition_scores[IObserver.Evaluation.Balance] +
            addition_scores[IObserver.Evaluation.Quantity] +
            addition_scores[IObserver.Evaluation.Variety] +
            addition_scores[IObserver.Evaluation.TagMatch];
        // 更新
        _iObserver.AddScores(addition_scores);
    }

    /// <summary>
    /// クラスタに新しいトッピングを追加する
    /// </summary>
    /// <param name="topping_idx">追加したいトッピングについての、toppingsにおける添え字</param>
    /// <returns>追加先のクラスタについての、clustersにおける添え字</returns>
    private int AddClusterIndex(int topping_idx)
    {
        // topping_idxが指すトッピングを取得
        Topping topping = _toppings[topping_idx];

        // 全クラスタとの、1. トッピングID, 2. 高さ平均, 3. 中心からの距離平均 を比較し、追加するべき最近クラスタを探索する
        Cluster closest_cluster = null;         // 最近クラスタ
        int closest_cluster_idx = 0;            // 最近クラスタのインデックス = 戻り値
        float closest_dist = _radius_threshold; // 最近クラスタに対する、中心からの距離平均との距離。初期値は閾値
        for (int i = 0; i < _clusters.Count; ++i)
        {
            Cluster cluster = _clusters[i];

            // 1. トッピングIDは一致しているか？
            if (cluster.topping_name != topping.Info.name)
            {
                continue;
            }

            // 2. 高さは閾値以内か？(高さが同じで、円の大きさが異なるクラスタは多数ある可能性が大いにあるため、平均法のような、ループ内での閾値の更新はしない)
            float height_diff = Mathf.Abs(topping.Info.height - cluster.average_height);
            if (height_diff >= _height_threshold)
            {
                continue;
            }

            // 3. ケーキ中心からの距離に関して、クラスター平均値との差が最も小さいものはどれか？
            float dist_diff = Mathf.Abs(topping.Info.radius - cluster.average_dist);
            if (dist_diff < closest_dist)
            {
                closest_cluster = cluster;
                closest_cluster_idx = i;
                closest_dist = dist_diff;
            }
        }

        // 距離 < 閾値であるクラスタは見つかったか？
        if (closest_cluster == null)
        {
            // 見つからなかったなら、クラスタ新規作成の必要あり
            Cluster new_cluster = new Cluster
            {
                indices = new List<int> { topping_idx },
                topping_name = topping.Info.name,
                average_height = topping.Info.height,
                average_dist = topping.Info.radius
            };
            _clusters.Add(new_cluster);

            // 作成したクラスタのインデックスを返す
            return _clusters.Count - 1;
        }

        // 見つかった最近クラスタに、topping_idxを追加
        closest_cluster.indices.Add(topping_idx);
        // 高さの平均値更新
        closest_cluster.average_height = (closest_cluster.average_height * (closest_cluster.size - 1) + topping.Info.height) / closest_cluster.size;
        // 中心からの距離の平均値更新
        closest_cluster.average_dist = (closest_cluster.average_dist * (closest_cluster.size - 1) + topping.Info.radius) / closest_cluster.size;

        // 最近クラスタのインデックスを返す
        return closest_cluster_idx;
    }

    /// <summary>
    /// ケーキ作り終了。作られたケーキをセーブする。
    /// </summary>
    // TODO: 後でprivateにすること
    public void FinishDecoration()
    {
        // セーブ用データオブジェクト作成
        RankingSaveData.CakeData my_cake = new RankingSaveData.CakeData();

        // スコアを代入
        my_cake.balance_score = _iObserver.Scores[IObserver.Evaluation.Balance];
        my_cake.quantity_score = _iObserver.Scores[IObserver.Evaluation.Quantity];
        my_cake.variety_score = _iObserver.Scores[IObserver.Evaluation.Variety];
        my_cake.tag_match_score = _iObserver.Scores[IObserver.Evaluation.TagMatch];
        my_cake.total_score = _iObserver.Scores[IObserver.Evaluation.Total];

        // デバッグ用ログ出力
        Debug.Log($"<color=cyan><b>【最終得点詳細】</b>\n" +
                  $"* Balance (バランス点): {my_cake.balance_score} 点\n" +
                  $"* Quantity (数量点): {my_cake.quantity_score} 点\n" +
                  $"* Variety (種類点): {my_cake.variety_score} 点\n" +
                  $"* TagMatch (タグ一致点): {my_cake.tag_match_score} 点\n" +
                  $"------------------------------------\n" +
                  $"* Total (総合得点): {my_cake.total_score} 点 (お題タグ: {_iObserver.TargetTag})</color>");

        // 乗っているトッピング一覧をデータに抽出する
        foreach (Topping topping in _toppings)
        {
            RankingSaveData.ToppingData toppingData = new RankingSaveData.ToppingData
            {
                name = topping.Info.name,
                local_pos = topping.transform.localPosition,
                local_rot = topping.transform.localRotation,
                local_scale = topping.transform.localScale
            };
            my_cake.toppings.Add(toppingData);
        }

        // ランキングマネージャーに登録
        RankingManager.instance.RegisterNewCake(my_cake);
    }
}