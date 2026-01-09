using UnityEngine;

public interface IInteractable
{
    // 返回物品名称，用于UI显示
    string GetInteractPrompt();
    // 执行交互逻辑
    void OnInteract(GameObject interactor);
}
