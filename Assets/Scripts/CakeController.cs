using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ケーキの挙動・描画処理を担う
/// </summary>
public class CakeController : MonoBehaviour
{
    [Tooltip("y軸周りの回転の速さ")]
    [SerializeField] private float _rotation_speed = 0.2f;

    [Tooltip("トッピング命中時エフェクト")]
    [SerializeField] private GameObject _effect_prefab;

    [Tooltip("クラスタの色分けに用いるパレット")]
    private List<Color> _color_palette = new List<Color>
    {
        Color.red,
        Color.green,
        Color.blue,
        Color.cyan,
        Color.magenta,
        Color.yellow,
        Color.white,
        Color.black
    };

    /// <summary>
    /// 回す
    /// </summary>
    private void FixedUpdate()
    {
        transform.Rotate(0, _rotation_speed, 0);
    }

    /// <summary>
    /// トッピングに、所属クラスタのIDに応じた色を付ける
    /// </summary>
    /// <param name="cluster_id">所属クラスタid</param>
    /// <param name="topping">色を付けたいトッピング</param>
    public void ColorTopping(int cluster_id, Topping topping)
    {
        // プロパティブロックを新規作成し、色を上から付ける
        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

        // cluster_id番目のカラーがパレットに存在しないなら、ランダムに作成
        if (cluster_id >= _color_palette.Count)
        {
            Color new_color = Random.ColorHSV();
            _color_palette.Add(new_color);
        }

        propertyBlock.SetColor("_BaseColor", _color_palette[cluster_id]);  // 作成済みのカラーパレットの、クラスタ番号の色
        topping.GetComponent<MeshRenderer>().SetPropertyBlock(propertyBlock);
    }

    /// <summary>
    /// 命中時エフェクトの生成→破棄
    /// </summary>
    /// <param name="position">エフェクト生成位置</param>
    public IEnumerator SpawnEffect(Vector3 position)
    {
        GameObject effect = Instantiate(_effect_prefab, position, Quaternion.Euler(-90, 0, 0));
        yield return new WaitForSeconds(0.4f);
        Destroy(effect);
    }
}
