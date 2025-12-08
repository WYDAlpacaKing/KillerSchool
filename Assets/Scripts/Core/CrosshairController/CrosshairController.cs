using UnityEngine;

public class CrosshairController : MonoBehaviour
{
    [Header("Crosshair Parts (拖入对应的子物体)")]
    [SerializeField] private RectTransform topPart;
    [SerializeField] private RectTransform bottomPart;
    [SerializeField] private RectTransform leftPart;
    [SerializeField] private RectTransform rightPart;

    [Header("Settings")]
    [Tooltip("基础间距：静止时准星方块距离屏幕中心的距离 (像素)")]
    [SerializeField] private float baseGap = 10f;

    [Tooltip("扩散倍率：1度的散布角度对应多少像素的位移")]
    [SerializeField] private float spreadMultiplier = 20f;

    [SerializeField] private float smoothSpeed = 15f;

    // 内部状态
    private float currentSpreadAngle = 0f;
    private float currentGapDisplay = 0f; // 当前显示的间距值(用于平滑)

    /// <summary>
    /// 由 ProceduralWeaponAnimator 调用
    /// 我们将当前的散布角度传入此方法
    /// </summary>
    /// <param name="angle">当前的散布角度</param>
    public void UpdateSpread(float angle)
    {
        currentSpreadAngle = angle;
    }

    private void Update()
    {
        //获得目标间距然后做插值 最后应用位置
        float targetGap = baseGap + (currentSpreadAngle * spreadMultiplier);
        currentGapDisplay = Mathf.Lerp(currentGapDisplay, targetGap, Time.deltaTime * smoothSpeed);

        if (topPart != null) topPart.anchoredPosition = new Vector2(0, currentGapDisplay);

        if (bottomPart != null) bottomPart.anchoredPosition = new Vector2(0, -currentGapDisplay);

        if (leftPart != null) leftPart.anchoredPosition = new Vector2(-currentGapDisplay, 0);

        if (rightPart != null) rightPart.anchoredPosition = new Vector2(currentGapDisplay, 0);
    }
}
