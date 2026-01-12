using UnityEngine;

[RequireComponent(typeof(BoxCollider))] // 强制要求有 Collider
public class BGMZone : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("进入该区域播放的音乐")]
    public AudioClip zoneMusic;

    [Tooltip("音量大小 (0~1)")]
    [Range(0f, 1f)]
    public float targetVolume = 0.5f;

    [Tooltip("淡入淡出需要的时间 (秒)")]
    public float fadeTime = 2.0f;

    [Header("Debug")]
    [Tooltip("勾选后，会在Scene窗口显示颜色，方便调节范围")]
    public bool showGizmo = true;
    public Color gizmoColor = new Color(0, 1, 0, 0.3f);

    private void Awake()
    {
        // 自动把 Collider 设置为 Trigger，防止把玩家挡住
        GetComponent<Collider>().isTrigger = true;
    }

    // 玩家进入区域
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // 确保你的玩家 Tag 是 "Player"
        {
            AudioManager.Instance.PlayBGM(zoneMusic, fadeTime, targetVolume);
        }
    }

    // 玩家离开区域
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 只有当当前播放的音乐是本区域的音乐时，才停止
            // 防止：玩家从区域A直接走到区域B，区域B刚开始放歌，
            // 结果区域A的Exit触发了，把B的歌给停了。
            // (虽然 AudioManager 里的协程已经处理了这种情况，但这里加个判断更保险)
            AudioManager.Instance.StopBGM(fadeTime);
        }
    }

    // 画个框框，方便在 Scene 窗口看范围
    private void OnDrawGizmos()
    {
        if (showGizmo)
        {
            Gizmos.color = gizmoColor;
            // 考虑到物体可能有旋转缩放，使用 Matrix 正确绘制 Cube
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }
}
