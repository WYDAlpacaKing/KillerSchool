using UnityEngine;
[RequireComponent(typeof(AudioSource))]
public class SoundEmitter : MonoBehaviour
{
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void Init(AudioClip clip, float volume, float pitch, float spatialBlend)
    {
        if (clip == null)
        {
            Destroy(gameObject);
            return;
        }

        // 1. 关键：断绝父子关系，留在原地播放
        transform.parent = null;

        // 2. 配置参数
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.pitch = pitch;
        audioSource.spatialBlend = spatialBlend; // 0 = 2D, 1 = 3D

        // 3. 播放
        audioSource.Play();

        // 4. 自毁倒计时 (时长 + 0.1秒缓冲)
        Destroy(gameObject, clip.length / Mathf.Abs(pitch) + 0.1f);
    }
}
