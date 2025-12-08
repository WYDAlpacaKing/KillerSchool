using UnityEngine;

public class TestShootMgr : MonoBehaviour
{
    public static TestShootMgr Instance { get; private set; } // 单例实例

    public GameObject targetPrefab;
    public Transform CornerOnePos;
    public Transform CornerTwoPos;
    public float spawnInterval = 1f;
    public float totalDuration = 30f;
    public int lastScore = 0;

    private Coroutine spawnCoroutine;

    private void Start()
    {
        // 注册监听
        DebuggerManager.RegisterWatcher(DebuggerModuleType.TestShooting, "滞留时间", () => spawnInterval);
        DebuggerManager.RegisterWatcher(DebuggerModuleType.TestShooting, "总时间", () => totalDuration);
        DebuggerManager.RegisterWatcher(DebuggerModuleType.TestShooting, "上次得分", () => lastScore);
    }

    public void AddScore(int score)
    {
        lastScore += score;
    }

    private void ResetScore()
    {
        lastScore = 0;
    }

    public void SetSpawnParameters(float interval, float duration)
    {
        spawnInterval = interval;
        totalDuration = duration;
    }

    private bool isSpawning = false;
    public void StartShooting()
    {
        // 如果正在生成目标 则停止当前生成
        if (isSpawning && spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            isSpawning = false;
        }
        ResetScore();// 重置分数
        spawnCoroutine = StartCoroutine(SpawnTargets());
    }

    private System.Collections.IEnumerator SpawnTargets()
    {
        isSpawning = true;
        float elapsedTime = 0f;
        while (elapsedTime < totalDuration)
        {
            Vector3 randomPos = new Vector3(
                Random.Range(CornerOnePos.position.x, CornerTwoPos.position.x),
                Random.Range(CornerOnePos.position.y, CornerTwoPos.position.y),
                Random.Range(CornerOnePos.position.z, CornerTwoPos.position.z)
            );
            GameObject target = Instantiate(targetPrefab, randomPos, Quaternion.identity);
            target.GetComponent<DestructibleProp>().OnDestroyed += () => AddScore(1);
            target.GetComponent<Time2Suicide>().timeToSuicide = spawnInterval;
            yield return new WaitForSeconds(spawnInterval);
            elapsedTime += spawnInterval;
        }
        isSpawning = false;
    }


}
