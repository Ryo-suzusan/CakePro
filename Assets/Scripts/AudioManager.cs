using UnityEngine;

/// <summary>
/// ゲーム内のオーディオを管理
/// Audioコンポーネントをつける必要があるため、シングルトン。
/// </summary>
public class AudioManager : MonoBehaviour
{
    [Tooltip("BGM用のAudioSource")]
    [SerializeField] private AudioSource _bgm_audio_source;
    [Tooltip("SE用のAudioSource")]
    [SerializeField] private AudioSource _se_audio_source;

    [Tooltip("自身のシングルトンインスタンス")]
    public static AudioManager instance = null;

    [Tooltip("流す単体SEのファイル")]
    private AudioClip _single_se_clip;
    /// <summary>
    /// _single_se_clipのプロパティ。
    /// 更新時、効果音を流す
    /// </summary>
    public AudioClip SingleSEClip
    {
        set
        {
            _single_se_clip = value;
            PlaySingleSE();
        }
    }
    [Tooltip("流すループSEのファイル")]
    private AudioClip _loop_se_clip;
    /// <summary>
    /// _loop_se_clipのプロパティ。
    /// 更新値が空なら効果音再生を停止し、空でなければ再生する　
    /// </summary>
    public AudioClip LoopSEClip
    {
        set
        {
            if (value != null)
            {
                _loop_se_clip = value;
                PlayLoopSE();
            }
            else
            {
                _loop_se_clip = null;
                StopLoopSE();
            }
        }
    }


    /// <summary>
    /// シングルトン化
    /// </summary>
    private void Awake()
    {
        // 先に、BGM用のAudioSourceについているClipを各種変数として初期化
        _bgm_clip = _bgm_audio_source.clip;

        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            // BGMがシーン間で異なるなら、切り替えを実行
            if (instance.BGMClip != _bgm_clip)
            {
                instance.BGMClip = _bgm_clip;
            }

            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 単発で効果音を流す
    /// </summary>
    private void PlaySingleSE()
    {
        _se_audio_source.PlayOneShot(_single_se_clip);
    }

    /// <summary>
    /// ループする効果音を流す
    /// </summary>
    private void PlayLoopSE()
    {
        _se_audio_source.clip = _loop_se_clip;
        _se_audio_source.Play();
    }

    /// <summary>
    /// ループする効果音を止める
    /// </summary>
    private void StopLoopSE()
    {
        _se_audio_source.Stop();
    }

    [Tooltip("流すBGMのファイル")]
    private AudioClip _bgm_clip;
    /// <summary>
    /// _bgm_clipのプロパティ。
    /// 更新時、BGMを変化させる
    /// </summary>
    public AudioClip BGMClip
    {
        get
        {
            return _bgm_clip;
        }
        set
        {
            _bgm_clip = value;
            _bgm_audio_source.Stop();
            _bgm_audio_source.clip = value;
            _bgm_audio_source.Play();
        }
    }
}