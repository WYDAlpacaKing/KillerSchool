using System.Collections;
using UnityEngine;

public class DecalLifeCycle : MonoBehaviour
{
    private float lifeTime = 5f;
    private Coroutine hideCoroutine;

    //激活并设置生命周期 外面调
    public void Activate(float duration)
    {
        lifeTime = duration;
        gameObject.SetActive(true);

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);

        hideCoroutine = StartCoroutine(DisableDelay());
    }

    private IEnumerator DisableDelay()
    {
        yield return new WaitForSeconds(lifeTime);
        gameObject.SetActive(false); // 返回池子
    }

    private void OnDisable()
    {
        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
    }
}
