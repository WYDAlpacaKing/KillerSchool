using UnityEngine;

public class Time2Suicide : MonoBehaviour
{
    [Tooltip("这个物体存在的时间")]
    public float timeToSuicide = 5f;

    private void Start()
    {
        Destroy(gameObject, timeToSuicide);
    }
}
