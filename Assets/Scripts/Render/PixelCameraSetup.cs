using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public class PixelCameraSetup : MonoBehaviour
{
    [Header("Color Grading")]
    public Shader retroShader;
    [Range(0, 2)] public float brightness = 1f;
    [Range(0, 2)] public float saturation = 1.2f;
    [Range(0, 2)] public float contrast = 1.1f;

    private Camera cam;
    private Material postProcessMat;

    void Start()
    {
        cam = GetComponent<Camera>();

        // 初始化材质
        if (retroShader != null)
        {
            postProcessMat = new Material(retroShader);
        }

        // === 关键修改：去 Manager 领取全局纹理 ===
        if (GraphicsManager.Instance != null)
        {
            cam.targetTexture = GraphicsManager.Instance.GlobalPixelTexture;
        }
    }

    // 当这个摄像机被启用时 (SetActive true)，再次确保连接了纹理
    void OnEnable()
    {
        if (cam == null) cam = GetComponent<Camera>();

        if (GraphicsManager.Instance != null && GraphicsManager.Instance.GlobalPixelTexture != null)
        {
            cam.targetTexture = GraphicsManager.Instance.GlobalPixelTexture;
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (postProcessMat != null)
        {
            postProcessMat.SetFloat("_BrightnessAmount", brightness);
            postProcessMat.SetFloat("_SaturationAmount", saturation);
            postProcessMat.SetFloat("_ContrastAmount", contrast);
            Graphics.Blit(source, destination, postProcessMat);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
}
