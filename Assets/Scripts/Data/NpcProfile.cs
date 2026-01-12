using UnityEngine;

public class NpcProfile : MonoBehaviour
{
    public enum NpcType { Civilian, Guard, Bandit, Target }

    [Header("身份")]
    public NpcType type = NpcType.Civilian;
    public string npcName = "Unknown";

    [Header("性格参数")]
    public float detectionRange = 15f; // 警戒范围
    public float combatRange = 8f;     // 战斗范围
    public bool hasGun = false;        // 是否有枪
    public bool isCoward = true;       // 是否胆小 (听到枪声是跑还是反击)
}
