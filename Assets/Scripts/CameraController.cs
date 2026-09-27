using UnityEngine;

/// <summary>
/// インゲームにおけるカメラの挙動制御
/// </summary>
[RequireComponent (typeof(Animator))]
public class CameraController : MonoBehaviour
{
    [Tooltip("自身にアタッチされているアニメーター")]
    private Animator _animator;


    /// <summary>
    /// コンポーネント取得
    /// </summary>
    private void Start()
    {
        _animator = GetComponent<Animator> ();
    }

    /// <summary>
    /// 前を向くアニメーション
    /// </summary>
    public void LookForward()
    {
        _animator.SetTrigger("look_forward_trigger");
    }

    /// <summary>
    /// 下を向くアニメーション
    /// </summary>
    public void LookDown()
    {
        _animator.SetTrigger("look_down_trigger");
    }
}
