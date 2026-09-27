using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// ゲーム内のポインター制御
/// </summary>
public class Pointer : MonoBehaviour
{
    [Tooltip("参照するJoyconInputManager")]
    private JoyconPlayer _joyconPlayer;

    [Tooltip("CanvasについているGraphicRayCaster")]
    [SerializeField] private GraphicRaycaster _graphicRaycaster;
    [Tooltip("Canvasに付いているEventCamera")]
    private Camera _target_camera;
    public  Camera TargetCamera
    {
        get
        {
            return _target_camera;
        }
    }
    [Tooltip("シーンのイベントシステム")]
    [SerializeField] private EventSystem _eventSystem;
    [Tooltip("自身のPointerEventData")]
    private PointerEventData _pointerEventData;
    [Tooltip("ポインターのレイキャスト結果(フレームごとに更新)")]
    private RaycastResult _front_raycast_result;

    [Tooltip("自身のRectTransform")]
    private RectTransform _rectTransform;
    public  RectTransform RectTransform
    {
        get { 
            return _rectTransform; 
        }
    }

    [Tooltip("ポインター座標算出に使用する、仮想平面のz座標(大きいほど感度が上がる)")]
    private float _virtual_plane_zpos = 20;
    [Tooltip("ポインターの感度倍率")]
    private float _sensitivity = 50;

    [Tooltip("一度クリックした後のインターバル（秒）")]
    private float _click_interval = 0.1f;
    [Tooltip("インターバルの計算用")]
    private float _click_timer = 0f;


    /// <summary>
    /// 各種初期化
    /// </summary>
    private void Start()
    {
        _joyconPlayer = JoyconPlayer.instance;
        _rectTransform = GetComponent<RectTransform>();
        if (_graphicRaycaster != null )
        {
            Canvas canvas = _graphicRaycaster.GetComponent<Canvas>();
            _target_camera = canvas.worldCamera;
        }

        // Joyconの角度が取得されたとき、ポインターの座標を更新させる
        _joyconPlayer.on_orientation_gotten += UpdatePointerCoordinate;

        // JoyconのShoulderボタンが押されたとき、最前のUIをクリック処理
        _joyconPlayer.on_shoulder2_pressed += (() => { 
            Click(_front_raycast_result.gameObject); 
        });
    }

    /// <summary>
    /// ポインターのレイキャストを実行し、UIの挙動制御をおこなう
    /// </summary>
    private void Update()
    {
        // クリックインターバル計算用タイマー
        if (_click_timer > 0f)
        {
            _click_timer -= Time.deltaTime;
        }

        // レイキャスト実行
        RaycastResult new_front_raycast_result = RaycastFrontUI();

        GameObject new_front_obj  = new_front_raycast_result.gameObject;
        GameObject last_front_obj = _front_raycast_result.gameObject;
        // 直前のフレームにおけるレイキャスト実行結果と異なるデータなら・・・
        if (new_front_obj != last_front_obj)
        {
            // 直前にいたUIからExit
            PointerExit(last_front_obj);
            // 新しく入ったUIにEnter
            PointerEnter(new_front_obj);

            // 新しく入ったUIがボタンであるならば、振動
            if (new_front_obj != null && new_front_obj.GetComponent<Button>() != null)
            {
                _joyconPlayer.Rumble(160, 320, 0.1f, 50);
            }
        }

        // データ更新
        _front_raycast_result = new_front_raycast_result;
    }

    /// <summary>
    /// 自身のポインター座標更新処理を、アクション群から削除
    /// </summary>
    private void OnDestroy()
    {
        _joyconPlayer.on_orientation_gotten -= UpdatePointerCoordinate;
    }

    /// <summary>
    /// 自身のポインター座標を更新する
    /// </summary>
    /// <param name="joycon_orientation">Joyconの回転</param>
    private void UpdatePointerCoordinate(Quaternion joycon_orientation)
    {
        // joyconの向きベクトルを取得(正面方向のベクトルを"joycon_orientation"分傾かせる)
        Vector3 joycon_vec = joycon_orientation * Vector3.forward;

        // joyconの向いている方向に伸びる仮想線形直線と、(0, "_virtual_plane_zpos", 0)に位置する仮想平面の交点を求める。

        Vector3 plane_n = transform.up;                 // 仮想平面の法線
        float dot = Vector3.Dot(plane_n, joycon_vec);   // 仮想平面とJoyConの向きの内積

        // 内積が0以下、つまりJoyconが仮想平面に向いてない場合は、終了
        if (dot <= 0)
        {
            return;
        }

        // 交点
        Vector3 intersection = _virtual_plane_zpos / dot * joycon_vec;

        // 上記交点をRectTransformの座標に同期
        _rectTransform.anchoredPosition = new Vector3(
            intersection.x * _sensitivity,
            intersection.z * -_sensitivity,
            0
            );
    }

    /// <summary>
    /// レイキャスト実行→最前のUIを返す
    /// </summary>
    private RaycastResult RaycastFrontUI()
    {
        // ポインターのスクリーン座標取得
        // Canvasに付いているイベントカメラから見て、ポインターは画面のどこにあるか？を計算
        Vector2 screen_coord = RectTransformUtility.WorldToScreenPoint(_target_camera, _rectTransform.position);

        // レイキャストを飛ばす

        _pointerEventData = new PointerEventData(_eventSystem);  // ポインターイベントを現在のEventSystemで更新
        _pointerEventData.position = screen_coord;               // レイキャストの位置を指定
        List<RaycastResult> results = new List<RaycastResult>(); // レイキャスト実行結果の保管場所
        _graphicRaycaster.Raycast(_pointerEventData, results);   // レイキャスト実行, 更新

        // レイキャスト結果内の最前のUIを返す
        RaycastResult front_result = new RaycastResult();
        if (results.Count > 0)
        {
            front_result = results[0];
        }
        return front_result;
    }

    /// <summary>
    /// GameObjectをClick処理(nullでも可)
    /// </summary>
    private void Click(GameObject obj)
    {
        // クリックのクールタイム中なら無視
        if (_click_timer > 0)
        {
            return;
        }

        if (obj != null)
        {
            // クリック成功時、タイマーをリセット
            _click_timer = _click_interval;

            bool is_executed = ExecuteEvents.Execute(obj, _pointerEventData, ExecuteEvents.pointerClickHandler);

            // クリックの際に何らかのメソッドが発生するなら、振動
            if (is_executed)
            {
                _joyconPlayer.Rumble(160, 320, 0.2f, 70);
            }
        }
    }

    /// <summary>
    /// 最前のUIのPointerEnter処理(nullでも可)
    /// </summary>
    private void PointerEnter(GameObject obj)
    {
        if (obj != null)
        {
            ExecuteEvents.Execute(obj, _pointerEventData, ExecuteEvents.pointerEnterHandler);
        }
    }

    /// <summary>
    /// 最前のUIのPointerExit処理(nullでも可)
    /// </summary>
    private void PointerExit(GameObject obj)
    {
        if (obj != null)
        {
            ExecuteEvents.Execute(obj, _pointerEventData, ExecuteEvents.pointerExitHandler);
        }
    }

    // ----- 以下デバッグ用 -----

    /// <summary>
    /// 【デバッグ用】マウスの2D座標をポインタの座標に同期
    /// </summary>
    /// <param name="mouse_position"></param>
    public void UpdatePointerWithMouse(Vector2 mouse_position)
    {
        if (_target_camera == null)
        {
            return;
        }

        // 親、つまりCanvasのRectTransformを取得
        RectTransform canvas_rect = transform.parent as RectTransform;
        if (canvas_rect == null)
        {
            return;
        }

        // スクリーン座標を、_target_cameraを通してCanvas元のローカル座標に変換
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas_rect,
            mouse_position,
            _target_camera,
            out Vector2 local_point
        ))
        {
            // 算出されたローカル座標をポインターに適用
            _rectTransform.anchoredPosition = local_point;
        }
    }
}
