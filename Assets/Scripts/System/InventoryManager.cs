using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Header("Defaults")]
    [SerializeField] private WeaponBase defaultSMGPrefab;   // Slot 0
    [SerializeField] private WeaponBase defaultPistolPrefab;// Slot 1
    [SerializeField] private WeaponBase defaultBinosPrefab; // Slot 2 (望远镜)

    [Header("References")]
    [SerializeField] private PlayerInputHandler inputHandler;
    [SerializeField] private NoodleArmController armController;
    [SerializeField] private Camera playerCamera;

    [Header("Interaction Settings")]
    [SerializeField] private float interactRange = 3.0f;
    [SerializeField] private LayerMask interactLayer;

    // 背包数据结构：3个槽位
    private WeaponBase[] weaponSlots = new WeaponBase[3];
    private int currentSlotIndex = 0; // 0=SMG, 1=Pistol, 2=Binos

    [Header("Slot Settings")]
    // 在Inspector里把Element 2勾选为True
    [SerializeField] private bool[] lockedSlots = new bool[] { false, false, true };

    private void Start()
    {
        // 初始化三个槽位
        InitializeSlot(0, defaultSMGPrefab);
        InitializeSlot(1, defaultPistolPrefab);
        InitializeSlot(2, defaultBinosPrefab);

        // 默认装备第一个
        EquipSlot(0);
    }

    private void Update()
    {
        HandleSlotSwitching();
        HandleInteraction();
    }

    // --- 逻辑部分 ---

    private void InitializeSlot(int index, WeaponBase prefab)
    {
        if (prefab == null) return;

        // 实例化武器，但先隐藏
        WeaponBase instance = Instantiate(prefab, transform.position, Quaternion.identity);
        // 如果是望远镜，手动赋值单例（防止Awake不运行）
        if (instance is Binoculars bino)
        {
            // 这一步虽然脏一点，但对于不知道执行顺序的情况最稳
            // 更好的方式是让 Binoculars 脚本去处理，这里先确保它不会找不到
        }
        instance.gameObject.SetActive(false); // 初始隐藏
        weaponSlots[index] = instance;
    }

    private void HandleSlotSwitching()
    {
        if (inputHandler.SwitchWeaponTriggered)
        {
            // 循环切换逻辑： 0 -> 1 -> 2 -> 0
            int nextSlot = (currentSlotIndex + 1) % 3;
            EquipSlot(nextSlot);
        }
    }

    private void EquipSlot(int index)
    {
        // 1. 验证索引
        if (index < 0 || index >= weaponSlots.Length) return;

        // 2. 隐藏当前武器 (只是SetInactive，不要销毁)
        if (weaponSlots[currentSlotIndex] != null)
        {
            weaponSlots[currentSlotIndex].OnUnequip();
            weaponSlots[currentSlotIndex].gameObject.SetActive(false);
        }

        // 3. 更新索引
        currentSlotIndex = index;

        // 4. 拿出新武器
        WeaponBase newWeapon = weaponSlots[currentSlotIndex];
        if (newWeapon != null)
        {
            newWeapon.gameObject.SetActive(true);
            // 通知面条手系统：手里的家伙换了
            armController.EquipWeapon(newWeapon);
        }
        else
        {
            // 如果这个槽位是空的（比如还没获得望远镜），告诉面条手现在是空手
            armController.EquipWeapon(null);
        }
    }

    // --- 接口实现部分：替换物品 ---

    /// <summary>
    /// 将当前手持的槽位替换为新的武器Prefab
    /// </summary>
    public bool ReplaceCurrentItem(WeaponBase newWeaponPrefab)
    {
        // 检查是否锁定
        if (currentSlotIndex == 2)
        {
            // 2. 如果锁定，返回 false (告诉对方：失败了)
            Debug.Log("该槽位锁定，无法替换");
            return false;
        }

        if (newWeaponPrefab == null) return false;

        // --- 执行替换逻辑 ---
        WeaponBase oldWeapon = weaponSlots[currentSlotIndex];
        if (oldWeapon != null)
        {
            oldWeapon.OnUnequip();
            Destroy(oldWeapon.gameObject);
        }

        WeaponBase newInstance = Instantiate(newWeaponPrefab, transform.position, Quaternion.identity);
        weaponSlots[currentSlotIndex] = newInstance;
        newInstance.gameObject.SetActive(true);
        armController.EquipWeapon(newInstance);

        // 3. 如果走到这里，说明成功了，返回 true
        return true;
    }

    // --- 交互部分 (Raycast) ---

    private void HandleInteraction()
    {
        if (!inputHandler.InteractTriggered) return;

        // 从相机中心发射射线
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                interactable.OnInteract(this.gameObject);
            }
        }
    }

   
}
