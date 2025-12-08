using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#region 数据结构定义
public enum DebuggerModuleType
{
    WeaponSystem,
    NoodleArm,
    TestShooting
}

[Serializable]
public class DebugButtonEntry
{
    public string buttonName;
    public UnityEvent onClickEvent;
}

[Serializable]
public class InspectorModuleConfig
{
    public DebuggerModuleType moduleName;
    public bool isExpanded = true;
    public List<DebugButtonEntry> staticButtons;
}

public class RuntimeModule //用于运行时存储模块数据
{
    public DebuggerModuleType Name;
    public bool IsExpanded = true;

    //按钮列表
    public List<DebugButtonEntry> Buttons = new List<DebugButtonEntry>();
    //存储只读数据监视器 字符串标签 到 获取数据的Lambda表达式 的映射
    public Dictionary<string, Func<string>> Watchers = new Dictionary<string, Func<string>>();
}
#endregion

public class DebuggerManager : MonoBehaviour
{
    public static DebuggerManager Instance { get; private set; } //单例实例

    [Header("调试面板设置")]
    [Tooltip("按此键开启/关闭面板")]
    public KeyCode toggleKey = KeyCode.F3;
    public Rect windowRect = new Rect(20, 20, 300, 500);

    [Header("Inspector 配置模块 (按钮事件)")]
    [SerializeField]
    private List<InspectorModuleConfig> inspectorModules = new List<InspectorModuleConfig>();

    //内部运行时模块字典
    private Dictionary<DebuggerModuleType, RuntimeModule> _modules = new Dictionary<DebuggerModuleType, RuntimeModule>();
    public bool IsVisible { get; private set; } = false;
    private Vector2 _scrollPosition;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
        InitializeInspectorModules();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleDebugger();
        }
    }

    private void ToggleDebugger()
    {
        IsVisible = !IsVisible;

        if (IsVisible)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void InitializeInspectorModules()
    {
        foreach (var config in inspectorModules)
        {
            var module = GetOrCreateModule(config.moduleName);
            module.IsExpanded = config.isExpanded;
            // 添加静态按钮
            foreach (var btn in config.staticButtons)
            {
                module.Buttons.Add(btn);
            }
        }
    }

    

    /// <summary>
    /// 获取或创建模块
    /// </summary>
    private RuntimeModule GetOrCreateModule(DebuggerModuleType moduleName)
    {
        if (!_modules.ContainsKey(moduleName))
        {
            _modules[moduleName] = new RuntimeModule { Name = moduleName };
        }
        return _modules[moduleName];
    }

    /// <summary>
    /// 只读数据监视器
    /// </summary>
    /// <param name="moduleName">模块名</param>
    /// <param name="key">数据标签</param>
    /// <param name="getter">获取数据的Lambda表达式</param>
    public static void RegisterWatcher(DebuggerModuleType moduleName, string key, Func<object> getter)
    {
        if (Instance == null) return;
        var module = Instance.GetOrCreateModule(moduleName);

        // 包装一下，处理null情况
        module.Watchers[key] = () =>
        {
            try { return getter.Invoke()?.ToString() ?? "null"; }
            catch { return "error"; }
        };
    }

    /// <summary>
    /// 用于注册按钮及其事件
    /// </summary>
    public static void RegisterAction(DebuggerModuleType moduleName, string btnName, UnityAction action)
    {
        if (Instance == null) return;
        var module = Instance.GetOrCreateModule(moduleName);

        UnityEvent uEvent = new UnityEvent();
        uEvent.AddListener(action);

        module.Buttons.Add(new DebugButtonEntry { buttonName = btnName, onClickEvent = uEvent });
    }

    //GUI 绘制

    private void OnGUI()
    {
        if (!IsVisible) return;

        //可拖拽窗口
        windowRect = GUI.Window(0, windowRect, DrawWindowContent, "Debugger Panel (F3)");
    }

    private void DrawWindowContent(int windowID)
    {
        //允许拖拽窗口头部
        GUI.DragWindow(new Rect(0, 0, 10000, 20));

        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

        foreach (var kvp in _modules)
        {
            RuntimeModule module = kvp.Value;

            //可折叠模块头部
            GUILayout.BeginHorizontal("box");
            if (GUILayout.Button(module.IsExpanded ? "▼" : "▶", GUILayout.Width(25)))
            {
                module.IsExpanded = !module.IsExpanded;
            }
            GUILayout.Label($"<b>{module.Name}</b>");
            GUILayout.EndHorizontal();

            if (module.IsExpanded)
            {
                GUILayout.BeginVertical("box");//模块内容区域

                //显示
                if (module.Watchers.Count > 0)
                {
                    GUILayout.Label("<color=yellow>--- Watchers ---</color>");
                    foreach (var watcher in module.Watchers)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(watcher.Key + ":", GUILayout.Width(120));

                        //调用 Lambda 获取最新值
                        GUILayout.Label(watcher.Value.Invoke());
                        GUILayout.EndHorizontal();
                    }
                }

                //按钮
                if (module.Buttons.Count > 0)
                {
                    if (module.Watchers.Count > 0) GUILayout.Space(5);
                    GUILayout.Label("<color=cyan>--- Actions ---</color>");

                    //每行两个按钮
                    int btnCount = 0;
                    foreach (var btn in module.Buttons)
                    {
                        if (btnCount % 2 == 0) GUILayout.BeginHorizontal();

                        if (GUILayout.Button(btn.buttonName))
                        {
                            btn.onClickEvent?.Invoke();
                        }

                        if (btnCount % 2 == 1 || btnCount == module.Buttons.Count - 1) GUILayout.EndHorizontal();
                        btnCount++;
                    }
                }

                GUILayout.EndVertical();
                GUILayout.Space(10);
            }
        }

        GUILayout.EndScrollView();
    }
}
