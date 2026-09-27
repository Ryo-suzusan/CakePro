using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Data.Common;


/// <summary>
/// ランキング表示機能を実装する。
/// </summary>
public class RankingDisplayer : MonoBehaviour
{
    [Tooltip("ランキングをセーブしているセーブデータ")]
    private RankingSaveData _ranking_data;

    [Tooltip("トッピング復元用Prefab")]
    [SerializeField] private GameObject _topping_prefab;
    [Tooltip("ランキング表示に用いるケーキオブジェクト")]
    [SerializeField] private GameObject _cake_base;

    [Tooltip("表示しているランキングの種類(RankingSaveDataが保持しているリストを上から0, 1, ...とする)")]
    private int _ranking_kind = 0;
    /// <summary>
    /// _ranking_kindのセットアクセサ
    /// </summary>
    /// <param name="value">更新したい値。元のRankingSaveDataをよく見ること。</param>
    public void SetRankingKind(int value)
    {
        _ranking_kind = value;
    }

    [Tooltip("表示しているケーキの順位(0始まり)")]
    private int _ranking_order = 0;
    /// <summary>
    /// _ranking_orderのセットアクセサ。
    /// </summary>
    /// <param name="value">更新したい値</param>
    public void SetRankingOrder(int value)
    {
        _ranking_order = value;
    }

    [Tooltip("スコア表示先")]
    [SerializeField] private TMP_Text[] _score_texts = new TMP_Text[1];

    [Tooltip("表示中の順位を示すためのアイコン")]
    [SerializeField] private GameObject _rank_icon;


    /// <summary>
    /// ランキングデータ取得
    /// </summary>
    private void Start()
    {
        // ランキング保存先
        _ranking_data = RankingManager.instance.ranking_data;

        // 始めは、もっとも最近つくられたケーキを復元しておく
        SetRankingKind(5);
        SetRankingOrder(0);

        // 表示
        DisplayCakeData();
    }

    /// <summary>
    /// ケーキのデータをRankingSaveDataから復元・表示する
    /// </summary>
    public void DisplayCakeData()
    {
        // 1. _ranking_kindランキングのデータ格納先、そしてその中の_ranking_order位であるデータを取得する
        // ランキングデータ
        List<RankingSaveData.CakeData> ranking = new List<RankingSaveData.CakeData>();
        switch (_ranking_kind)
        {
            case 0:
                ranking = _ranking_data.total_ranking;
                break;
            case 1:
                ranking = _ranking_data.balance_ranking;
                break;
            case 2:
                ranking = _ranking_data.quantity_ranking;
                break;
            case 3:
                ranking = _ranking_data.variety_ranking;
                break;
            case 4:
                ranking = _ranking_data.tag_match_ranking;
                break;
            case 5:
                ranking = _ranking_data.recently_created;
                break;
            default:    // とりあえず、総合点ランキングに初期化しておく
                ranking = _ranking_data.total_ranking;
                break;
        }
        // ランキング内のあるケーキデータ
        RankingSaveData.CakeData summon_cake = ranking[_ranking_order];

        // 2. ケーキの復元
        RestoreCake(summon_cake);

        // 3. スコア表示
        DisplayScore(summon_cake);
    }

    /// <summary>
    /// "_ranking_kind"ランキングの"_ranking_order"位のケーキをランキングデータから復元する
    /// </summary>
    /// <param name="summon_cake">復元したいケーキの情報</param>
    private void RestoreCake(RankingSaveData.CakeData summon_cake)
    {
        // まず、既にケーキに乗っているトッピングを全て削除する
        foreach (Transform child in _cake_base.transform)
        {
            // タグがBaseの物は除外
            if (child.tag != "Base")
            {
                Destroy(child.gameObject);
            }
        }


        // データをもとにトッピング生成→配置
        foreach (RankingSaveData.ToppingData topping in summon_cake.toppings)
        {
            // IDに対応するプレハブ生成
            GameObject spawned = Instantiate(_topping_prefab, _cake_base.transform);

            // メッシュを復元
            ToppingSelector.RestoreToppingMesh(topping.name, spawned);

            // 相対座標・角度・スケールを復元
            spawned.transform.localPosition = topping.local_pos;
            spawned.transform.localRotation = topping.local_rot;
            spawned.transform.localScale = topping.local_scale;

            // レイヤーを指定
            spawned.layer = LayerMask.NameToLayer("Ranking");

            // 復元した物は、物理挙動・クラスを削除しておく
            if (spawned.GetComponent<Rigidbody>() != null)
            {
                Destroy(spawned.GetComponent<Rigidbody>());
            }
            if (spawned.GetComponent<Topping>() != null)
            {
                Destroy(spawned.GetComponent<Topping>());
            }
        }
    }

    /// <summary>
    /// スコアを表示する。
    /// </summary>
    /// <param name="cake">スコア表示したいケーキの情報</param>
    private void DisplayScore(RankingSaveData.CakeData cake)
    {
        foreach (TMP_Text text in _score_texts)
        {
            // 表示中のランキング種類に応じて、表示するスコアを変更
            switch (_ranking_kind)
            {
                case 0:
                    text.text = cake.total_score.ToString();
                    break;
                case 1:
                    text.text = cake.balance_score.ToString();
                    break;
                case 2:
                    text.text = cake.quantity_score.ToString();
                    break;
                case 3:
                    text.text = cake.variety_score.ToString();
                    break;
                case 4:
                    text.text = cake.tag_match_score.ToString();
                    break;
                case 5:     // 最近つくられたケーキは、総合点を出力する（Resultシーン用）
                    text.text = cake.total_score.ToString();
                    break;
                default:
                    text.text = cake.total_score.ToString();
                    break;
            }
        }
    }

    /// <summary>
    /// ランクアイコンを、指定したy座標へ移動する
    /// </summary>
    /// <param name="target_rect">動かしたい位置（実際にはy座標のみ参照）</param>
    public void MoveRankIcon(RectTransform target_rect)
    {
        RectTransform icon_rect = _rank_icon.GetComponent<RectTransform>();
        Vector3 target_pos = target_rect.localPosition;

        Vector3 icon_pos = icon_rect.localPosition;
        icon_pos.y = target_pos.y;
        icon_rect.localPosition = icon_pos;
    }
}