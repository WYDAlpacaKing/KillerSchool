using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FallTrigger : MonoBehaviour
{
    [Header("设置")]
    public AudioClip splashSound; // 落水声
    [Range(0f, 1f)]
    public float volume = 1f;

    private AudioSource _audioSource;

    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f; // 2D声音，保证玩家听得很清楚，或者设为1做3D音效
    }

    void OnTriggerEnter(Collider other)
    {
        // 检查是不是玩家掉进来了
        if (other.CompareTag("Player"))
        {
            // 1. 播放声音
            if (splashSound != null)
            {
                _audioSource.PlayOneShot(splashSound, volume);
            }

            // 2. 获取玩家身上的记录脚本并归位
            SafeGroundRecorder recorder = other.GetComponent<SafeGroundRecorder>();
            if (recorder != null)
            {
                recorder.Respawn();
            }
        }
    }
}
