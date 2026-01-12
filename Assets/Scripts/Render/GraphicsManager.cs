using UnityEngine;
using UnityEngine.UI;

public class GraphicsManager : MonoBehaviour
{
    public static GraphicsManager Instance { get; private set; }

    [Header("Settings")]
    public int targetHeight = 270;
    public RawImage displayScreen; // 你的 Canvas_PixelScreen 里的 RawImage

    // 全局唯一的像素纹理
    public RenderTexture GlobalPixelTexture { get; private set; }

    void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;

        CreateTexture();
    }

    void CreateTexture()
    {
        float aspect = (float)Screen.width / Screen.height;
        int width = Mathf.RoundToInt(targetHeight * aspect);

        GlobalPixelTexture = new RenderTexture(width, targetHeight, 24);
        GlobalPixelTexture.filterMode = FilterMode.Point;
        GlobalPixelTexture.antiAliasing = 1;
        GlobalPixelTexture.name = "GlobalPixelRT";

        if (displayScreen != null)
        {
            displayScreen.texture = GlobalPixelTexture;
        }
    }
}
