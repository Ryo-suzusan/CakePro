using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// トッピングを制御・情報保持する
/// </summary>
public class Topping : MonoBehaviour
{
    [Tooltip("自身のRigidBody")]
    private Rigidbody _rigidbody;

    [Tooltip("自身のデータの追加先クラス")]
    private CakeManager _cakeManager;

    [Tooltip("被発射後、どこかにぶつかり、ケーキの一部となったか")]
    private bool _is_decolated = false;

    [Tooltip("トッピング識別名(オブジェクトファイルの名前と一致させる)")]
    [SerializeField] private string _name;
    public  string Name
    {
        // トッピング生成時に、名前の設定を行うこと
        set
        {
            _name = value;
        }
    }

    [Tooltip("トッピングに紐付くタグ")]
    public string[] Tags { get; set; }

    /// <summary>
    /// 自身のトッピング情報を格納する構造体
    /// </summary>
    public struct ToppingInfo
    {
        [Tooltip("トッピングの種類")]
        public string name;
        [Tooltip("中心からの距離")]
        public float radius;
        [Tooltip("Y座標 = 高さ")]
        public float height;
        [Tooltip("トッピングのタグ")]
        public string[] tags;
    }

    [Tooltip("採点ロジックで必要な、自身のトッピング情報")]
    private ToppingInfo _info;
    public  ToppingInfo Info
    {
        get
        {
            return _info;
        }
    }


    /// <summary>
    /// Toppingのオブジェクト生成→発射を、コンストラクタ風に実装
    /// </summary>
    /// <param name="topping">インスタンス化に用いる情報</param>
    /// <param name="start_point">発射地点</param>
    /// <param name="scale">自身のスケール</param>
    /// <param name="velocity">初速度</param>
    /// <param name="cakeManager">自身のデータの追加先</param>
    /// <returns>生成したオブジェクト</returns>
    public static Topping Shoot(Topping topping, Vector3 start_point, Vector3 scale, Vector3 velocity, CakeManager cakeManager)
    {
        // インスタンス化に用いるToppingクラス
        Topping obj = Instantiate(topping, start_point, Quaternion.identity);

        // タグの情報を複製先に引き継ぐ (オートプロパティはInstantiateでは自動複製されないため)
        obj.Tags = topping.Tags;

        // スケールを指定
        obj.transform.localScale = scale;

        // タグを設定
        obj.gameObject.tag = "Topping";

        // 発射処理に必要なコンポーネントをアタッチ
        obj._rigidbody = obj.AddComponent<Rigidbody>();
        MeshCollider meshCollider = obj.AddComponent<MeshCollider>();

        // 当たり判定の簡略化(凸型)
        meshCollider.convex = true;
        // 回転の無効化
        obj._rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

        // 射出方向に初速度を与える
        obj._rigidbody.AddForce(velocity * obj._rigidbody.mass, ForceMode.Impulse);
        // シーン内ケーキの参照
        obj._cakeManager = cakeManager;

        return obj;
    }

    /// <summary>
    /// オブジェクトと衝突した際の処理
    /// </summary>
    /// <param name="collision"></param>
    private void OnCollisionEnter(Collision collision)
    {
        // 既にケーキに衝突済み = ケーキのデコレーションの一部となっているなら、抜ける
        if (_is_decolated)
        {
            return;
        }

        // "Base" or "Topping"タグのオブジェクトに触れたとき
        if (collision.gameObject.tag == "Base" || collision.gameObject.tag == "Topping")
        {
            // 停止する
            _rigidbody.isKinematic = true;

            // ケーキの子要素とする
            transform.parent = _cakeManager.transform;

            // 自身のトッピング情報を初期化する

            // 衝突済みフラグを立てる
            _is_decolated = true;

            // ケーキ中心からの距離を計算
            Vector2 topping_p = new Vector2(transform.localPosition.x, transform.localPosition.z);
            _info.radius = Vector2.Distance(Vector2.zero, topping_p);
            // 高さ情報
            _info.height = transform.localPosition.y;
            // トッピング名
            _info.name = _name;
            // トッピングのタグ
            _info.tags = Tags;

            // CakeDataHolderに、トッピングの追加を知らせる
            _cakeManager.AddTopping(this);
        }
    }
}
