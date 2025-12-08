using System.Collections.Generic;
using UnityEngine;

public class DecalManager : MonoBehaviour
{
    public static DecalManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private int maxDecals = 100;
    [SerializeField] private float decalOffset = 0.02f;

    private Queue<GameObject> decalQueue = new Queue<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    
    public void SpawnDecal(GameObject prefab, Vector3 position, Vector3 normal, float duration)
    {
        if (prefab == null) return;

        GameObject decalToUse = null;

        // 对象池逻辑
        if (decalQueue.Count >= maxDecals)
        {
            decalToUse = decalQueue.Dequeue();

            if (decalToUse == null) decalToUse = Instantiate(prefab, transform);
        }
        else
        {
            // 池子没满，生成新的
            decalToUse = Instantiate(prefab, transform);
        }

        decalQueue.Enqueue(decalToUse);

        // 位置和旋转补丁
        SetDecalTransform(decalToUse, position, normal);

        
        DecalLifeCycle lifeCycle = decalToUse.GetComponent<DecalLifeCycle>();
        if (lifeCycle == null) lifeCycle = decalToUse.AddComponent<DecalLifeCycle>();

        lifeCycle.Activate(duration);
    }

    private void SetDecalTransform(GameObject decal, Vector3 point, Vector3 normal)
    {
        // 位置偏移
        decal.transform.position = point + normal * decalOffset;

        // GPT修复
        // Quaternion.LookRotation(normal) 会让 Z轴 指向法线
        // 我们乘以 Euler(180, 0, 0) 将其绕 X 轴翻转 180 度
        // 这样原本朝里的正面就会朝外了
        decal.transform.rotation = Quaternion.LookRotation(normal) * Quaternion.Euler(180f, 0f, 0f);

        // 随机旋转 Z 轴 (增加视觉随机性)
        decal.transform.Rotate(Vector3.forward, Random.Range(0, 360f));
    }
}
