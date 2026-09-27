using System.Collections;
using UnityEngine;

public class ButtonUI : MonoBehaviour
{
    [Tooltip("通常時のスケール")]
    private Vector3 _default_scale;
    [Tooltip("ボタン押下時のスケール縮小率")]
    private float _pressed_scale_rate = 0.9f;
    [Tooltip("ボタン押下時の効果音")]
    [SerializeField] private AudioClip _pressed_se;


    /// <summary>
    /// 被押下時のメソッド群
    /// </summary>
    public void OnClick()
    {
        PlayClickSE();
        StartCoroutine(ShrinkScale());
    }

    /// <summary>
    /// サイズ縮小
    /// </summary>
    private IEnumerator ShrinkScale()
    {
        // 通常時スケールの保持
        _default_scale = transform.localScale;

        // スケール縮小
        gameObject.transform.localScale *= _pressed_scale_rate;
        // 0.1秒後、元に戻す
        yield return new WaitForSeconds(0.1f);
        gameObject.transform.localScale = _default_scale;
    }

    /// <summary>
    /// 押下時の効果音を流す
    /// </summary>
    private void PlayClickSE()
    {
        AudioManager.instance.SingleSEClip = _pressed_se;
    }
}
