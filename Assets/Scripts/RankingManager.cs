using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// ランキングのセーブデータ更新・読み込み等を管理する。
/// OnDestroyを用いるため、non-staticのシングルトン。
/// </summary>
public class RankingManager : MonoBehaviour
{
    [Tooltip("自身のシングルトンインスタンス")]
    public static RankingManager instance;

    [Tooltip("json変換するデータのクラス")]
    [HideInInspector] public RankingSaveData ranking_data = new RankingSaveData();
    [Tooltip("jsonファイルのパス")]
    private string _file_path;
    [Tooltip("jsonファイル名")]
    private string _file_name = "RankingData.json";


    /// <summary>
    /// シングルトン化、ファイルチェック、読み込み
    /// </summary>
    private void Awake()
    {
        // シングルトン化
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // セーブファイル読み込み

        // パス名取得
        _file_path = Application.dataPath + "/" + _file_name;
        // ファイルがないとき、新しいセーブファイルを作成
        if (!File.Exists(_file_path))
        {
            Save(ranking_data);
        }

        // ファイルを読み込んでdataに格納
        ranking_data = Load(_file_path);
    }

    /// <summary>
    /// jsonとしてデータを保存
    /// </summary>
    /// <param name="data">セーブする内容</param>
    private void Save(RankingSaveData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data);                         // jsonとして変換
            using (StreamWriter wr = new StreamWriter(_file_path, false))   // ファイル書き込み指定
            {
                wr.WriteLine(json);                                         // json変換した情報を書き込み
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ランキングの保存中にエラーが発生しました: {e.Message}");
        }
    }

    /// <summary>
    /// jsonファイル読み込み
    /// </summary>
    /// <param name="path">読み込み先</param>
    /// <returns>読み込んだ内容</returns>
    private RankingSaveData Load(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return new RankingSaveData();
            }

            using (StreamReader rd = new StreamReader(path))            // ファイル読み込み指定
            {
                string json = rd.ReadToEnd();                           // ファイル内容全て読み込む
                return JsonUtility.FromJson<RankingSaveData>(json);     // jsonファイルを型に戻して返す
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ランキングの読み込み中にエラーが発生しました。パス: {path}\nエラー: {e.Message}");
            return new RankingSaveData(); // 読み込み失敗時は新規データを返す
        }
    }

    /// <summary>
    /// ゲーム終了時に保存
    /// </summary>
    private void OnDestroy()
    {
        Save(ranking_data);
    }

    /// <summary>
    /// ランキングに新しいケーキを追加して更新
    /// </summary>
    /// <param name="cakeData">新しく作られたケーキ</param>
    public void RegisterNewCake(RankingSaveData.CakeData cakeData)
    {
        int capacity = RankingSaveData.ranking_capacity;    // ランキング内に入れられるデータ数

        // 1. ランキング
        // 一度リストに追加
        ranking_data.balance_ranking.Add(cakeData);
        ranking_data.quantity_ranking.Add(cakeData);
        ranking_data.variety_ranking.Add(cakeData);
        ranking_data.tag_match_ranking.Add(cakeData);
        ranking_data.total_ranking.Add(cakeData);

        // LINQを用い、それぞれ点数が高い順にソートし、capacity分抽出
        ranking_data.balance_ranking = ranking_data.balance_ranking.OrderByDescending(e => e.balance_score).Take(capacity).ToList();
        ranking_data.quantity_ranking = ranking_data.quantity_ranking.OrderByDescending(e => e.quantity_score).Take(capacity).ToList();
        ranking_data.variety_ranking = ranking_data.variety_ranking.OrderByDescending(e => e.variety_score).Take(capacity).ToList();
        ranking_data.tag_match_ranking = ranking_data.tag_match_ranking.OrderByDescending(e => e.tag_match_score).Take(capacity).ToList();
        ranking_data.total_ranking = ranking_data.total_ranking.OrderByDescending(e => e.total_score).Take(capacity).ToList();

        // 2. 直近作成ケーキ
        // リストの先頭に追加し、capacity分抽出
        ranking_data.recently_created.Insert(0, cakeData);
        ranking_data.recently_created = ranking_data.recently_created.Take(capacity).ToList();

        // 更新されたランキングをセーブする
        Save(ranking_data);
    }
}