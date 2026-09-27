using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// デバッグインプット用の機能
/// </summary>
public class DebugManager : MonoBehaviour
{
    [Tooltip("マウスクリック時のアクション群")]
    public event Action<Mouse> _on_clicked = delegate { };
    [Tooltip("スペースキー押下時のアクション群")]
    public event Action _on_space_pressed = delegate { };
    [Tooltip("ESCキー押下時のアクション群")]
    public event Action _on_esc_pressed = delegate { };

    [Tooltip("トッピング発射用")]
    [SerializeField] private Shooter _shooter;
    [Tooltip("音声認識実行用")]
    [SerializeField] private ToppingChanger _toppingChanger;
    [Tooltip("同期対象のポインター")]
    [SerializeField] private Pointer _pointer;

    [Tooltip("デバッグインプットの有効設定")]
    private static bool _do_debug = false;


    /// <summary>
    /// メソッドの追加
    /// </summary>
    private void Start()
    {
        if (_shooter != null)
        {
            _on_clicked += _shooter.AimWithMouse;
        }
        if (_toppingChanger != null)
        {
            _on_space_pressed += _toppingChanger.ToppingChange;
        }
    }

    /// <summary>
    /// マウス操作の読み取り・キーボード操作
    /// </summary>
    private void Update()
    {
        var mouse = Mouse.current;
        var keyboard = Keyboard.current;

        // デバッグ有効化・無効化
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            _do_debug = !_do_debug;
        }

        // デバッグ用インプットアクション

        if (!_do_debug)
        {
            return;
        }

        // マウス
        if (mouse == null)
        {
            return;
        }

        // スペースキー押下
        if (mouse.leftButton.wasPressedThisFrame)
        {
            _on_clicked(mouse);
        }
        // 画面内マウス座標の読み込み→ポインタに同期
        if (_pointer != null)
        {
            _pointer.UpdatePointerWithMouse(mouse.position.ReadValue());
        }

        // キーボード
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            _on_space_pressed();
        }
    }
}
