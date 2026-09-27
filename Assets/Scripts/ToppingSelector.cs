using UnityEngine;

/// <summary>
/// トッピングの名前を受け取り、トッピング生成を行うクラス
/// 軽量化のため、静的クラス化
/// </summary>
public static class ToppingSelector
{
    /// <summary>
    /// シューターから発射するトッピングオブジェクトの切り替え
    /// </summary>
    /// <param name="shooter">シーン内のシューター</param>
    /// <param name="vessel">シューターの子要素（復元先Object）</param>
    /// <param name="toppingName">生成するトッピングの名前</param>
    public static void UpdateShootingObj(
        Shooter shooter,
        GameObject vessel,
        string toppingName,
        string[] tags
    )
    {
        // 1. トッピングデータの取得
        RestoreToppingMesh(toppingName, vessel);

        // 2. Toppingクラスを設定→アタッチ
        Topping topping = vessel.GetComponent<Topping>();
        if (topping == null)
        {
            topping = vessel.AddComponent<Topping>();
        }
        topping.Name = toppingName;
        topping.Tags = tags;

        // 3. Shooterに渡す
        shooter.ShootingTopping = topping;
    }

    /// <summary>
    /// 適切なトッピングを、Resourceディレクトリから探索→器Objectに渡す
    /// </summary>
    /// <param name="toppingName">探索するトッピングの名前</param>
    /// <param name="vessel_obj">Object復元先の器</param>
    public static void RestoreToppingMesh(string toppingName, GameObject vessel_obj)
    {
        // Resourceファイルから探索

        Mesh mesh; // 生成するトッピングのメッシュ
        Material material; // 生成するトッピングのマテリアル

        mesh = Resources.Load<Mesh>(
            "food_test/models/" + toppingName + "/0/mesh.Meshes/geometry_0"
        );
        material = Resources.Load<Material>(
            "food_test/models/" + toppingName + "/0/mesh.Materials/material_00"
        );
        Texture2D mainTexture = Resources.Load<Texture2D>(
            "food_test/models/" + toppingName + "/0/mesh.Textures/00#GLB"
        );

        // メッシュ・マテリアルの検証
        if (mesh == null)
            Debug.LogError("メッシュのロードに失敗しています！");
        if (material == null)
            Debug.LogError("マテリアルのロードに失敗しています！");

        // マテリアルの設定をURPに対応させる
        Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLitShader != null)
        {
            material.shader = urpLitShader;

            // 💡 URP/Litシェーダーのメインテクスチャの内部名は "_BaseMap" です
            material.SetTexture("_BaseMap", mainTexture);
        }
        else
        {
            Debug.LogError(
                "URP/Lit シェーダーが見つかりませんでした。プロジェクトの設定を確認してください。"
            );
        }

        // 取得したトッピングデータをもとに、器Objectを更新

        // メッシュフィルターを取得or作成→設定
        MeshFilter meshFilter = vessel_obj.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = vessel_obj.AddComponent<MeshFilter>();
        }
        meshFilter.mesh = mesh;
        // メッシュレンダラーを取得or作成→設定
        MeshRenderer meshRenderer = vessel_obj.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = vessel_obj.AddComponent<MeshRenderer>();
        }
        meshRenderer.material = material;
    }
}
