using UnityEngine;

public class WeaponPickup : MonoBehaviour, IInteractable
{
    [Header("Config")]
    [Tooltip("捡起来后实际生成的武器Prefab")]
    public WeaponBase weaponPrefab;
    public string interactText = "Pick up Weapon";

    public string GetInteractPrompt() => interactText;

    public void OnInteract(GameObject interactor)
    {
        var inventory = interactor.GetComponent<InventoryManager>();
        if (inventory != null)
        {
            // 1. 调用方法，并接收返回值
            bool isSuccess = inventory.ReplaceCurrentItem(weaponPrefab);

            // 2. 只有在成功的情况下，才销毁自己
            if (isSuccess)
            {
                Destroy(gameObject);
            }
            else
            {
                // 如果失败（比如手里拿着望远镜），什么都不做
                // 这里也可以加一个简单的反馈音效，比如 "Error_Beep"
                Debug.Log("替换失败，保留地面物品");
            }
        }
    }
}
