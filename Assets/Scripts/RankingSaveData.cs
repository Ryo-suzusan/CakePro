using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ランキングのセーブデータ
/// </summary>
[System.Serializable]
public class RankingSaveData
{
    /// <summary>
    /// トッピング1個が保持するセーブデータ
    /// </summary>
    [System.Serializable]
    public class ToppingData
    {
        [Tooltip("トッピングの種類")]
        public string name;
        [Tooltip("ケーキから見た座標")]
        public Vector3 local_pos;
        [Tooltip("ケーキから見た回転")]
        public Quaternion local_rot;
        [Tooltip("ケーキから見たスケール")]
        public Vector3 local_scale;
    }

    /// <summary>
    /// ケーキが保持するセーブデータ
    /// </summary>
    [System.Serializable]
    public class CakeData
    {
        [Tooltip("Balance点")]
        public int balance_score;
        [Tooltip("Quantity点")]
        public int quantity_score;
        [Tooltip("Variety点")]
        public int variety_score;
        [Tooltip("TagMatch点")]
        public int tag_match_score;
        [Tooltip("総合点")]
        public int total_score;

        [Tooltip("乗っかっているトッピングデータ一覧")]
        public List<ToppingData> toppings = new List<ToppingData>();
    }


    [Tooltip("ランキングに乗せるデータ個数")]
    public static readonly int ranking_capacity = 5;

    [Tooltip("Balance点のトップランキング")]
    public List<CakeData> balance_ranking = new List<CakeData>(new CakeData[ranking_capacity]);
    [Tooltip("Quantity点のトップランキング")]
    public List<CakeData> quantity_ranking = new List<CakeData>(new CakeData[ranking_capacity]);
    [Tooltip("Variety点のトップランキング")]
    public List<CakeData> variety_ranking = new List<CakeData>(new CakeData[ranking_capacity]);
    [Tooltip("TagMatch点のトップランキング")]
    public List<CakeData> tag_match_ranking = new List<CakeData>(new CakeData[ranking_capacity]);
    [Tooltip("総合点のトップランキング")]
    public List<CakeData> total_ranking = new List<CakeData>(new CakeData[ranking_capacity]);

    [Tooltip("直近で作られたケーキの保存先")]
    public List<CakeData> recently_created = new List<CakeData>(new CakeData[ranking_capacity]);
}