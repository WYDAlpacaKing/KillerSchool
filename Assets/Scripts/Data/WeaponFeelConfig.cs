using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponFeel", menuName = "Killer/Weapon Feel Config")]
public class WeaponFeelConfig: ScriptableObject
{
    [Header("=== Module A: Spring Settings (物理弹簧) ===")]

    [Tooltip("位置弹簧配置：控制枪械位移的物理弹性。\n" +
             "Stiffness(刚度): 值越大回弹越快，像轻武器；值越小越拖沓，像重武器。\n" +
             "Damping(阻尼): 值越小晃动次数越多(果冻感)；值越大停得越快(水阻感)。")]
    public Spring positionSpring = new Spring { stiffness = 200f, damping = 15f, mass = 1f };

    [Tooltip("旋转弹簧配置：控制枪械旋转的物理弹性。\n" +
             "通常旋转的刚度(Stiffness)需要设置得比位移更高，以保持枪口指向的稳定性。")]
    public Spring rotationSpring = new Spring { stiffness = 400f, damping = 20f, mass = 1f };



    [Header("=== Module B: Weapon Kickback (模型动作) ===")]

    [Tooltip("枪身模型受到的瞬间冲击力向量。\n" +
             "X: 随机左右抖动的力度。\n" +
             "Y: 枪口上跳的高度。\n" +
             "Z: 枪身向后撞击屏幕的距离(负数)，这是打击感来源。")]
    public Vector3 kickbackForce = new Vector3(0.05f, 0.1f, -0.2f);

    [Tooltip("爆发速度：枪械受到后坐力时，从静止瞬间达到最大位移/旋转的插值速度。\n" +
             "值越大，后坐力发生得越突然、越干脆。")]
    public float snappiness = 20f;

    [Tooltip("复位速度：枪械从后坐力状态回到原始待机位置的平滑速度。\n" +
             "值越小，枪械回到原位的过程越慢，感觉越沉重。")]
    public float returnSpeed = 10f;



    [Header("=== Module C: Camera Recoil (镜头上跳) ===")]

    [Tooltip("垂直后坐力范围：每一发子弹导致相机（准星）向上抬起的角度最小值和最大值。")]
    public Vector2 verticalRecoil = new Vector2(2f, 3f);

    [Tooltip("水平后坐力范围：每一发子弹导致相机向左右偏转的角度范围（负数为左，正数为右）。")]
    public Vector2 horizontalRecoil = new Vector2(-0.5f, 0.5f);

    [Tooltip("后坐力恢复曲线：定义准星自动回正的节奏。\n" +
             "X轴为时间百分比(0-1)，Y轴为恢复力度的强度。建议设为 EaseOut 曲线。")]
    public AnimationCurve recoilRecoveryCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("恢复时长：从开火结束到准星完全自动回正所需的时间（秒）。")]
    public float recoilRecoveryDuration = 0.3f;


    [Header("=== Module C: Camera Shake & FOV (震动特效) ===")]

    [Tooltip("震动幅度：屏幕震动的剧烈程度（位移量）。大口径武器应调大此值。")]
    public float shakeAmplitude = 1.0f;

    [Tooltip("震动频率：Perlin Noise的采样速度。\n" +
             "低频(5-8): 像大炮一样的沉重摇晃。\n" +
             "高频(15-20): 像冲锋枪一样的急促抖动。")]
    public float shakeFrequency = 10.0f;

    [Tooltip("震动持续时间：单次开火导致的屏幕震动衰减时长（秒）。")]
    public float shakeDuration = 0.2f;

    [Tooltip("FOV冲击：开火瞬间视场角(FOV)瞬间缩小的度数。\n" +
             "例如设为2，开火时视野会瞬间放大一点点然后弹回，制造视觉上的'吸附感'。")]
    public float fovKick = 2.0f;


    [Header("=== Module D: Dynamic Spread (准星扩散) ===")]

    [Tooltip("基础散布：静止状态下的射击误差角度（0为指哪打哪，数值越大越不准）。")]
    public float baseSpread = 0.5f;

    [Tooltip("单发扩散：每开一枪，散布角度增加的数值。")]
    public float spreadPerShot = 0.2f;

    [Tooltip("最大散布：连续射击（泼水）时，散布角度的硬上限，防止准星大到屏幕外。")]
    public float maxSpread = 4.0f;

    [Tooltip("散布恢复速度：停止射击后，准星缩小回基础散布的插值速度。")]
    public float spreadRecoverySpeed = 5.0f;

    [Tooltip("移动惩罚系数：当角色移动时，基础散布会乘以这个系数（例如2.0表示移动时精度减半）。")]
    public float movementSpreadPenalty = 2.0f;


    
    [Header("=== Module E: Sway & Bob (惯性与呼吸) ===")]

    [Tooltip("鼠标滞后幅度：鼠标移动时，枪械向反方向拖拽的距离系数。\n" +
             "用来模拟枪械的重量惯性，避免枪像贴图一样死死粘在屏幕中心。")]
    public float swayAmount = 0.02f;

    [Tooltip("滞后平滑度：枪械追上鼠标准星的速度。\n" +
             "值越小越'飘'（重武器），值越大越跟手（轻武器）。")]
    public float swaySmoothing = 10f;

    [Tooltip("最大滞后限制：防止快速甩鼠标时枪械模型飞出屏幕边缘的最大距离。")]
    public float maxSwayAmount = 0.06f;

    [Tooltip("摆动频率：呼吸或走动时枪械做正弦波运动的速度（通常与脚步节奏同步）。")]
    public float bobFrequency = 1f;

    [Tooltip("摆动幅度：呼吸或走动时枪械上下左右晃动的范围。")]
    public float bobAmplitude = 0.01f;
}
