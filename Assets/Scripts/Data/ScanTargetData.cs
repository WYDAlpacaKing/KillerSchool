using UnityEngine;

public class ScanTargetData : MonoBehaviour
{
    public enum TargetType
    {
        Civilian,
        Guard,
        Bandit,
        Target
    }

    [Header("Identity")]
    public string characterName = "UNKNOWN";
    public TargetType type = TargetType.Guard;
}
