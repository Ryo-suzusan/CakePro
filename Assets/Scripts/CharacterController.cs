using UnityEngine;

/// <summary>
/// キャラクターの挙動制御
/// </summary>
public class CharacterController : MonoBehaviour
{
    [Tooltip("自身にアタッチされているアニメーター")]
    private Animator _animator;


    /// <summary>
    /// 各種初期
    /// </summary>
    private void Start()
    {
        // コンポーネント取得
        _animator = GetComponent<Animator> ();
        // もしResultシーンなら、ギターを弾く
        if (SequenceManager.CurrentSecuence == SequenceManager.Sequence.Result)
        {
            _animator.SetBool("do_play_guitar", true);
        }
    }

    /// <summary>
    /// 喜ぶモーション実行
    /// </summary>
    public void BeJoyful()
    {
        _animator.SetTrigger("joyful_trigger");
    }

    /// <summary>
    /// 悩むモーション実行
    /// </summary>
    public void BeThinking()
    {
        _animator.SetTrigger("thinking_trigger");
    }
}
