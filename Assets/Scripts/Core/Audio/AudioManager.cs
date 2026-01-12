using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("=== Settings ===")]
    [SerializeField] private SoundEmitter emitterPrefab; // 拖入制作好的 Prefab

    [Header("=== Music System (New) ===")]
    // 专门用于播放 BGM 的声源，它必须是 Loop 的
    private AudioSource musicSource;
    private Coroutine currentFadeRoutine; // 记录当前的淡入淡出进程，防止冲突

    [Header("=== Audio Library (Sound Banks) ===")]
    // 武器音效
    public AudioClip[] pistolShoots;
    public AudioClip[] smgShoots;
    public AudioClip[] dryFire; // 没有子弹的声音

    // 命中音效
    public AudioClip[] bulletImpactFlesh; // 打中肉体
    public AudioClip[] bulletImpactMetal; // 打中金属
    public AudioClip[] bulletImpactStone; // 打中墙壁

    public AudioClip[] playerDead;
    public AudioClip[] fall2Water;
    public AudioClip[] OpenEngine;
    public AudioClip[] Money;

    // 交互音效
    public AudioClip[] uiClicks;
    public AudioClip[] itemPickup;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        // --- 初始化 BGM 专用轨道 ---
        // 我们在代码里动态创建一个 AudioSource，挂在 AudioManager 上
        GameObject musicObj = new GameObject("MusicChannel");
        musicObj.transform.parent = transform;
        musicSource = musicObj.AddComponent<AudioSource>();

        musicSource.loop = true;        // BGM 必须循环
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;  // 0 = 2D (听到原本的声音), 1 = 3D (有空间感)
        musicSource.volume = 0f;        // 初始音量为0，等待淡入
    }

    /// <summary>
    /// 播放 3D 音效 (枪声、爆炸、脚步)
    /// </summary>
    public void PlaySound3D(AudioClip[] clips, Vector3 position, float volume = 1f, float pitchVar = 0.1f)
    {
        if (clips == null || clips.Length == 0) return;

        // 1. 随机挑选一个片段
        AudioClip clip = clips[Random.Range(0, clips.Length)];

        // 2. 计算随机音调 (Juicy 核心)
        float randomPitch = Random.Range(1f - pitchVar, 1f + pitchVar);
        float randomVol = Random.Range(volume * 0.9f, volume * 1.1f);

        // 3. 生成发射器
        SoundEmitter emitter = Instantiate(emitterPrefab, position, Quaternion.identity);
        emitter.Init(clip, randomVol, randomPitch, 1.0f); // 1.0f = 3D Sound
    }

    /// <summary>
    /// 播放 2D 音效 (UI、系统提示)
    /// </summary>
    public void PlaySound2D(AudioClip[] clips, float volume = 1f, float pitchVar = 0.05f)
    {
        if (clips == null || clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        float randomPitch = Random.Range(1f - pitchVar, 1f + pitchVar);

        // 位置无所谓，因为是 2D
        SoundEmitter emitter = Instantiate(emitterPrefab, Vector3.zero, Quaternion.identity);
        emitter.Init(clip, volume, randomPitch, 0.0f); // 0.0f = 2D Sound
    }

    /// <summary>
    /// 播放指定 BGM (带淡入效果)
    /// </summary>
    /// <param name="clip">音乐片段</param>
    /// <param name="fadeDuration">淡入所需时间</param>
    /// <param name="targetVolume">目标最大音量</param>
    public void PlayBGM(AudioClip clip, float fadeDuration = 1.0f, float targetVolume = 0.5f)
    {
        if (clip == null) return;

        // 如果当前正在放这首歌，且正在播放中，就不需要重新开始了
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        // 如果有正在进行的淡入淡出，先停止，防止冲突
        if (currentFadeRoutine != null) StopCoroutine(currentFadeRoutine);

        // 开启协程：切换音乐并淡入
        currentFadeRoutine = StartCoroutine(FadeToNewBGM(clip, fadeDuration, targetVolume));
    }

    /// <summary>
    /// 停止 BGM (带淡出效果)
    /// </summary>
    public void StopBGM(float fadeDuration = 1.0f)
    {
        if (!musicSource.isPlaying) return;

        if (currentFadeRoutine != null) StopCoroutine(currentFadeRoutine);
        currentFadeRoutine = StartCoroutine(FadeOutBGM(fadeDuration));
    }

    // --- 协程逻辑：淡出旧的 -> 换碟 -> 淡入新的 ---
    private IEnumerator FadeToNewBGM(AudioClip newClip, float duration, float targetVol)
    {
        // 1. 如果当前有声音，先快速淡出旧音乐 (取一半的时间)
        float halfDuration = duration * 0.5f;

        if (musicSource.isPlaying && musicSource.volume > 0)
        {
            float startVol = musicSource.volume;
            for (float t = 0; t < halfDuration; t += Time.deltaTime)
            {
                musicSource.volume = Mathf.Lerp(startVol, 0f, t / halfDuration);
                yield return null;
            }
            musicSource.volume = 0f;
            musicSource.Stop();
        }

        // 2. 换碟
        musicSource.clip = newClip;
        musicSource.Play();

        // 3. 淡入新音乐
        for (float t = 0; t < halfDuration; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(0f, targetVol, t / halfDuration);
            yield return null;
        }
        musicSource.volume = targetVol;
    }

    // --- 协程逻辑：单纯淡出停止 ---
    private IEnumerator FadeOutBGM(float duration)
    {
        float startVol = musicSource.volume;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
            yield return null;
        }

        musicSource.volume = 0f;
        musicSource.Stop();
        musicSource.clip = null; // 清空引用
    }
}
