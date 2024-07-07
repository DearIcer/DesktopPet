using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public class WindowController : MonoBehaviour
{
    public enum TransparentType : int
    {
        None = 0,
        Alpha = 1,
        ColorKey = 2,
    }

    public enum HitTestType : int
    {
        None = 0,
        Opacity = 1,
        Raycast = 2,
    }

// 强制在启动时使用窗口化模式
    [Tooltip("启动时强制使用窗口模式")] public bool forceWindowed = false;

    /// <summary>
    /// 标识发生<see cref="OnStateChanged">状态改变事件</see>的类型
    /// </summary>
    [Flags]
    public enum WindowStateEventType : int
    {
        None = 0, // 无事件
        StyleChanged = 1, // 风格改变
        Resized = 2, // 大小改变

        // 以下可能随规范变动
        TopMostEnabled = 16 + 1 + 8, // 置顶启用
        TopMostDisabled = 16 + 1, // 置顶禁用
        BottomMostEnabled = 32 + 1 + 8, // 置底启用
        BottomMostDisabled = 32 + 1, // 置底禁用
        WallpaperModeEnabled = 64 + 1 + 8, // 壁纸模式启用
        WallpaperModeDisabled = 64 + 1, // 壁纸模式禁用
    };

    // 当未指定时使用主摄像机
    [Tooltip("如果没有指定，则使用主摄像机")] public Camera currentCamera;

    // 高级设置标题
    [Header("高级设置")]
    // 当窗口透明时更改摄像机背景
    [Tooltip("当窗口透明时切换摄像机背景")]
    public bool autoSwitchCameraBackground = true;

    // 仅限Windows标题
    [Header("仅限Windows平台")]
    // 选择透明方法，*仅在Windows上可用
    [Tooltip("选择透明方法，仅在Windows上可用")]
    public TransparentType transparentType = TransparentType.Alpha;

    // 下次窗口变为透明时使用的键控颜色
    [Tooltip("下次窗口变透明时使用的键控颜色")] public Color32 keyColor = new Color32(0x01, 0x00, 0x01, 0x00);

    // 摄像机原本的清除标志
    private CameraClearFlags _originalCameraClearFlags;

    // 摄像机原本的背景颜色
    private Color _originalCameraBackground;

    // 最大射线检测深度
    private float _raycastMaxDepth = 100.0f;

    // 选择方法
    [Tooltip("选择检测方法")] public HitTestType hitTestType = HitTestType.Opacity;

    // 状态标题
    [Header("状态")]
    // 是否鼠标指针位于不透明像素上？（只读）
    [SerializeField, ReadOnly, Tooltip("鼠标指针是否位于不透明像素上？（只读）")]
    private bool onObject = true;

    // 序列化，可编辑属性，用于检查启动时是否置底
    [SerializeField, EditableProperty, Tooltip("勾选以在启动时设置窗口置底")]
    private bool _isBottommost = false;


    /// <summary>
    /// 低级类实例，用于窗口核心操作
    /// </summary>
    private WinCore _winCore = null;

    private Rect _originalWindowRect = new Rect();

    /// <summary>
    /// 表示是否启用碰撞检测
    /// </summary>
    public bool isHitTestEnabled = true;

    /// <summary>
    /// 获取或设置窗口的位置坐标
    /// </summary>
    public Vector2 WindowPosition
    {
        get { return (_winCore != null ? _winCore.GetWindowPosition() : Vector2.zero); }
        set { _winCore?.SetWindowPosition(value); }
    }

    /// <summary>
    /// 获取或设置窗口是否允许穿透点击
    /// </summary>
    public bool IsClickThrough
    {
        get { return _isClickThrough; }
        set { SetClickThrough(value); }
    }

    private bool _isClickThrough = false;


    [AttributeUsage(AttributeTargets.Field)]
    public class EditablePropertyAttribute : PropertyAttribute
    {
    }

    /// <summary>
    /// 用于处理鼠标指针事件的数据
    /// </summary>
    private PointerEventData _pointerEventData;

    /// <summary>
    /// 当监视器改变时触发的事件委托
    /// </summary>
    public event OnMonitorChangedDelegate OnMonitorChanged;

    /// <summary>
    /// 监视器改变事件的委托类型
    /// </summary>
    public delegate void OnMonitorChangedDelegate();

    /// <summary>
    /// 获取或设置是否应该使窗口适应监视器
    /// </summary>
    public bool ShouldFitMonitor
    {
        get { return shouldFitMonitor; }
        set { FitToMonitor(value, _monitorToFit); }
    }

    /// <summary>
    /// 应该适应监视器的布尔字段，并且在 Unity 编辑器中可编辑
    /// </summary>
    [FormerlySerializedAs("_shouldFitMonitor")] [SerializeField, EditableProperty, Tooltip("勾选以使窗口适应监视器")]
    private bool shouldFitMonitor = false;

    /// <summary>
    /// 要适应的监视器编号
    /// </summary>
    private int _monitorToFit = 0;

    /// <summary>
    /// 获取或设置窗口是否为最上层窗口
    /// </summary>
    public bool IsTopmost
    {
        get { return ((_winCore == null) ? isTopmost : isTopmost = _winCore.IsTopmost); }
        set { SetTopmost(value); }
    }

    /// <summary>
    /// 是否为最上层窗口的私有字段，在启动时可编辑
    /// </summary>
    [FormerlySerializedAs("_isTopmost")] [SerializeField, EditableProperty, Tooltip("勾选以在启动时设置为最上层")]
    private bool isTopmost = false;

    /// <summary>
    /// 获取当前活动的窗口控制器实例
    /// </summary>
    public static WindowController current => _current ?? FindOrCreateInstance();

    private static WindowController _current;

    /// <summary>
    /// 是否透明的私有字段，在启动时可编辑
    /// </summary>
    [FormerlySerializedAs("_isTransparent")] [SerializeField, EditableProperty, Tooltip("勾选以在启动时设置为透明")]
    private bool isTransparent = false;

    /// <summary>
    /// 用于碰撞检测的图层掩码
    /// </summary>
    private int _hitTestLayerMask;

    
    /// <summary>
    /// 窗口的 Alpha 值，在 0 到 1 的范围内
    /// </summary>
    [FormerlySerializedAs("_alphaValue")] [SerializeField, EditableProperty, Tooltip("窗口的透明度"), Range(0f, 1f)]
    private float alphaValue = 1.0f;

    /// <summary>
    /// 用于颜色选择的纹理
    /// </summary>
    private Texture2D _colorPickerTexture = null;

    /// <summary>
    /// 鼠标指针下像素的颜色，只读
    /// </summary>
    [SerializeField, ReadOnly, Tooltip("鼠标指针下的像素颜色。（只读）")]
    public Color pickedColor;

    /// <summary>
    /// 可用于“不透明度”类型的碰撞检测的阈值
    /// </summary>
    [Tooltip("仅当碰撞检测类型为不透明度时可用"), RangeAttribute(0f, 1f)]
    public float opacityThreshold = 0.1f;

    /// <summary>
    /// 设置窗口的 Alpha 值
    /// </summary>
    /// <param name="alpha">Alpha 值，范围从 0.0 到 1.0</param>
    private void SetAlphaValue(float alpha)
    {
        alphaValue = alpha;
        _winCore?.SetAlphaValue(alphaValue);
    }

    /// <summary>
    /// 状态改变时触发的事件委托
    /// </summary>
    public event OnStateChangedDelegate OnStateChanged;

    /// <summary>
    /// 状态改变事件的委托类型
    /// </summary>
    public delegate void OnStateChangedDelegate(WindowStateEventType type);

    /// <summary>
    /// 使窗口适应指定的监视器
    /// </summary>
    /// <param name="shouldFit">是否应该适应</param>
    /// <param name="monitorIndex">目标监视器的索引</param>
    /// <returns>操作是否成功</returns>
    private bool FitToMonitor(bool shouldFit, int monitorIndex)
    {
        if (_winCore == null)
        {
            shouldFitMonitor = shouldFit;
            _monitorToFit = monitorIndex;
            return false;
        }

        if (shouldFit)
        {
            if (!shouldFitMonitor)
            {
                // 如果之前不是适应状态
                _monitorToFit = monitorIndex;
                shouldFitMonitor = shouldFit;
                UpdateMonitorFitting();
            }
            else
            {
                if (_monitorToFit != monitorIndex)
                {
                    // 如果目标监视器已改变
                    _monitorToFit = monitorIndex;
                    UpdateMonitorFitting();
                }
            }
        }
        else
        {
            if (shouldFitMonitor)
            {
                // 如果之前处于适应状态，现在取消
                _monitorToFit = monitorIndex;
                shouldFitMonitor = shouldFit;
                UpdateMonitorFitting();

                _winCore.SetZoomed(false);
            }
            else
            {
                // 如果不在适应中，仅改变选择
                _monitorToFit = monitorIndex;
            }
        }

        return true;
    }

    /// <summary>
    /// 更新监视器适应状态
    /// </summary>
    private void UpdateMonitorFitting()
    {
        if (!shouldFitMonitor) return;

        int monitors = WinCore.GetMonitorCount();
        int targetMonitorIndex = _monitorToFit;

        if (targetMonitorIndex < 0)
        {
            targetMonitorIndex = 0;
        }

        if (monitors <= targetMonitorIndex)
        {
            targetMonitorIndex = monitors - 1;
        }

        if (targetMonitorIndex >= 0)
        {
            _winCore.FitToMonitor(targetMonitorIndex);
        }
    }

    void Awake()
    {
        // 实现单例模式，如果已经存在实例则销毁自己
        if (this != current)
        {
            Destroy(this.gameObject);
            return;
        }
        else
        {
            _current = this;
        }

        // 强制退出全屏模式，但在编辑器中不做任何处理
#if !UNITY_EDITOR
    if (forceWindowed && Screen.fullScreen)
    {
        Screen.fullScreen = false;
    }
#endif

        if (!currentCamera)
        {
            // 查找主相机
            currentCamera = Camera.main;
        }

        // 记录相机原始的清除标志和背景颜色
        if (currentCamera)
        {
            _originalCameraClearFlags = currentCamera.clearFlags;
            _originalCameraBackground = currentCamera.backgroundColor;
        }

        // 初始化鼠标事件数据
        _pointerEventData = new PointerEventData(EventSystem.current);

        // 设置碰撞检测掩码，忽略 "Ignore Raycast" 图层
        _hitTestLayerMask = ~LayerMask.GetMask("Ignore Raycast");

        // 准备用于提取鼠标下像素颜色的纹理
        _colorPickerTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);

        // 创建用于窗口控制的实例
        _winCore = new WinCore();
    }

    void Start()
    {
        // 开始一个协程，用于获取鼠标光标下的颜色
        StartCoroutine(HitTestCoroutine());

        // 存储初始窗口大小和位置
        StoreOriginalWindowRectangle();

        // 适应选定的监视器
        OnMonitorChanged += UpdateMonitorFitting;
        UpdateMonitorFitting();
    }

    void Update()
    {
        // 如果窗口控制实例不存在或窗口未激活，则尝试重新获取
        if (_winCore == null || !_winCore.IsActive)
        {
            UpdateTargetWindow();
        }
        else
        {
            // 更新窗口核心状态
            _winCore.Update();
        }

        // 处理事件
        UpdateEvents();

        // 更新键盘和鼠标操作对底层窗口的穿透状态
        UpdateClickThrough();
    }


    private void UpdateEvents()
    {
        if (_winCore == null) return;

        if (_winCore.ObserveWindowStyleChanged(out var type))
        {
            if (shouldFitMonitor) StartCoroutine("ForceZoomed"); // 延迟强制最大化

            OnStateChanged?.Invoke((WindowStateEventType)type);
        }
    }

    private IEnumerator HitTestCoroutine()
    {
        while (Application.isPlaying)
        {
            yield return new WaitForEndOfFrame();
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            if (transparentType == TransparentType.ColorKey)
            {
                onObject = true;
            }
            else
#endif
            if (hitTestType == HitTestType.Opacity)
            {
                HitTestByOpaquePixel();
            }
            else if (hitTestType == HitTestType.Raycast)
            {
                HitTestByRaycast();
            }
            else
            {
                // 如果没有进行击打测试，则始终为真
                onObject = true;
            }
        }

        yield return null;
    }

    private void HitTestByRaycast()
    {
        var position = Input.mousePosition;

        // 判断是否位于 uGUI 上方
        var raycastResults = new List<RaycastResult>();
        _pointerEventData.position = position;
        EventSystem.current.RaycastAll(_pointerEventData, raycastResults);
        foreach (var result in raycastResults)
        {
            // 考虑图层掩码（如果不是 Ignore Raycast 则视为命中）
            if (((1 << result.gameObject.layer) & _hitTestLayerMask) > 0)
            {
                onObject = true;
                return;
            }
        }

        if (currentCamera && currentCamera.isActiveAndEnabled)
        {
            Ray ray = currentCamera.ScreenPointToRay(position);

            // 判断是否位于 3D 对象上方
            if (Physics.Raycast(ray, out _, _raycastMaxDepth))
            {
                onObject = true;
                return;
            }

            // 判断是否位于 2D 对象上方
            var rayHit2D = Physics2D.GetRayIntersection(ray);
            Debug.DrawRay(ray.origin, ray.direction, Color.blue, 2f, false);
            if (rayHit2D.collider != null)
            {
                onObject = true;
                return;
            }
        }
        else
        {
            // 如果相机无效，则获取主相机
            currentCamera = Camera.main;
        }

        // 如果都没有命中，则判断不在对象上
        onObject = false;
    }

    private void HitTestByOpaquePixel()
    {
        Vector2 mousePos = Input.mousePosition;

        // 检查鼠标坐标
        if (GetOnOpaquePixel(mousePos))
        {
            onObject = true;
        }
        else
        {
            onObject = false;
        }
    }

// 检查鼠标位置下的像素是否为不透明
    private bool GetOnOpaquePixel(Vector2 mousePos)
    {
        float w = Screen.width; // 获取屏幕宽度
        float h = Screen.height; // 获取屏幕高度

        // 如果鼠标位置在屏幕之外，则认为是透明的
        if (mousePos.x < 0 || mousePos.x >= w || mousePos.y < 0 || mousePos.y >= h)
        {
            return false;
        }

        // 如果当前不是透明模式，则只要在屏幕内就认为是不透明的
        if (!isTransparent) return true;

        // 如果是ColorKey类型的透明，点击穿透交给操作系统处理，只要在窗口内就返回true
        if (transparentType == TransparentType.ColorKey) return true;

        // 检查指定坐标的像素颜色来判断是否不透明
        try
        {
            _colorPickerTexture.ReadPixels(new Rect(mousePos, Vector2.one), 0, 0); // 读取像素
            Color color = _colorPickerTexture.GetPixels32()[0]; // 获取颜色信息
            pickedColor = color; // 记录选取的颜色

            // 如果alpha值大于或等于设定的阈值，则认为是不透明的
            return (color.a >= opacityThreshold);
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message); // 输出错误信息
            return false;
        }
    }

// 存储原始窗口矩形
    void StoreOriginalWindowRectangle()
    {
        if (_winCore != null)
        {
            var size = _winCore.GetWindowSize(); // 获取窗口大小
            var pos = _winCore.GetWindowPosition(); // 获取窗口位置
            _originalWindowRect = new Rect(pos, size); // 创建并存储原始窗口矩形
        }
    }

// 设置相机背景
    void SetCameraBackground(bool transparent)
    {
        // 首先检查currentCamera是否有效且允许自动切换相机背景
        if (!currentCamera || !autoSwitchCameraBackground) return;

        // 如果是透明模式
        if (transparent)
        {
            currentCamera.clearFlags = CameraClearFlags.SolidColor; // 设置相机清除标志为纯色
            if (transparentType == TransparentType.ColorKey)
            {
                currentCamera.backgroundColor = keyColor; // 设置背景颜色为键控颜色
            }
            else
            {
                currentCamera.backgroundColor = Color.clear; // 设置背景颜色为完全透明
            }
        }
        else
        {
            // 恢复原始相机清除标志和背景颜色
            currentCamera.clearFlags = _originalCameraClearFlags;
            currentCamera.backgroundColor = _originalCameraBackground;
        }
    }

// 设置窗口是否置顶
    private void SetTopmost(bool topmost)
    {
        if (_winCore == null) return;
        _winCore.EnableTopmost(topmost); // 设置窗口置顶状态
        isTopmost = _winCore.IsTopmost; // 更新置顶状态变量
    }

    /// <summary>
    /// 确保在更换相机时能恢复或应用正确的背景设置
    /// </summary>
    /// <param name="newCamera">新的相机实例</param>
    public void SetCamera(Camera newCamera)
    {
        if (newCamera != currentCamera)
        {
            SetCameraBackground(false); // 当更换相机时，先关闭透明背景
        }

        currentCamera = newCamera; // 更新当前相机

        if (currentCamera)
        {
            _originalCameraClearFlags = currentCamera.clearFlags; // 保存相机的清除标志
            _originalCameraBackground = currentCamera.backgroundColor; // 保存相机的背景颜色

            SetCameraBackground(isTransparent); // 根据当前的透明状态设置相机背景
        }
    }
    
    /// <summary>
    /// 切换透明状态
    /// </summary>
    /// <param name="transparent">是否开启透明</param>
    private void SetTransparent(bool transparent)
    {
        isTransparent = transparent; // 更新透明状态
        SetCameraBackground(transparent); // 根据新的透明状态更新相机背景

#if !UNITY_EDITOR // 只在非编辑器模式下执行
    if (_winCore != null)
    {
        _winCore.EnableTransparent(transparent); // 启用或禁用窗口的透明效果
    }
#endif
        UpdateClickThrough(); // 更新点击穿透状态
    }

    void UpdateClickThrough()
    {
        // 如果不启用自动命中测试或命中测试类型为无，则结束
        if (!isHitTestEnabled || hitTestType == HitTestType.None) return;

        // 如果鼠标光标不可见，则当作点击在透明像素上处理
        bool isHit = onObject;

        if (_isClickThrough)
        {
            // 如果当前是点击穿透状态，则仅在命中时恢复非穿透状态
            if (isHit)
            {
                SetClickThrough(false); // 关闭点击穿透
            }
        }
        else
        {
            // 如果当前不是点击穿透状态，并且是透明的且未命中，则设置为点击穿透
            if (isTransparent && !isHit)
            {
                SetClickThrough(true); // 开启点击穿透
            }
        }
    }

// 设置点击穿透功能
    void SetClickThrough(bool isThrough)
    {
        // 如果_winCore存在，调用其EnableClickThrough方法设置点击穿透
        _winCore?.EnableClickThrough(isThrough);
        // 更新_isClickThrough变量以记录当前的点击穿透状态
        _isClickThrough = isThrough;
    }

// 查找或创建WindowController实例
    private static WindowController FindOrCreateInstance()
    {
        // 使用FindObjectOfType查找场景中的WindowController实例
        var instance = FindObjectOfType<WindowController>();
        // 返回找到的WindowController实例
        return instance;
    }

    /// <summary>
    /// 如果自身的窗口句柄不确定，则重新搜索
    /// </summary>
    private void UpdateTargetWindow()
    {
        // 如果_winCore为空，创建一个新的WinCore实例
        if (_winCore == null)
        {
            _winCore = new WinCore();
        }

        // 如果窗口尚未激活
        if (!_winCore.IsActive)
        {
            // 尝试附加当前窗口
            _winCore.AttachMyWindow();

            // 如果窗口已经激活
            if (_winCore.IsActive)
            {
                // 设置透明类型
                _winCore.SetTransparentType((WinCore.TransparentType)transparentType);
                // 设置键控颜色
                _winCore.SetKeyColor(keyColor);
                // 设置Alpha值
                _winCore.SetAlphaValue(alphaValue);
                // 设置透明状态
                SetTransparent(isTransparent);
                // 如果需要置底，调用SetBottommost方法
                if (_isBottommost)
                {
                    SetBottommost(_isBottommost);
                }
                // 否则，如果需要置顶，调用SetTopmost方法
                else
                {
                    SetTopmost(isTopmost);
                }

                // 设置点击穿透状态
                SetClickThrough(_isClickThrough);

                // 如果有OnMonitorChanged委托，调用它来处理类似显示器变更的情况
                OnMonitorChanged?.Invoke();
            }
        }
        else
        {
#if UNITY_EDITOR
            // 在编辑器模式下，如果游戏视图状态改变，更新目标窗口
            // 如果当前活动窗口与目标窗口相同，则不做任何操作
            _winCore.AttachMyActiveWindow();
#endif
        }
    }

// 设置窗口是否置底
    private void SetBottommost(bool bottommost)
    {
        // 如果_winCore存在
        if (_winCore != null)
        {
            // 调用_winCore的EnableBottommost方法设置窗口是否置底
            _winCore.EnableBottommost(bottommost);
            // 更新_isBottommost变量以记录当前的置底状态
            _isBottommost = _winCore.IsBottommost;
            // 更新isTopmost变量以记录当前的置顶状态
            isTopmost = _winCore.IsTopmost;
        }
    }
}