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
    
    [Tooltip("Force windowed on startup")]
    public bool forceWindowed = false;
    
    /// <summary>
    /// Identifies the type of <see cref="OnStateChanged">OnStateChanged</see> event when it occurs
    /// </summary>
    [Flags]
    public enum WindowStateEventType : int
    {
        None = 0,
        StyleChanged = 1,
        Resized = 2,

        // 以降は仕様変更もありえる
        TopMostEnabled = 16 + 1 + 8,
        TopMostDisabled = 16 + 1,
        BottomMostEnabled = 32 + 1 + 8,
        BottomMostDisabled = 32 + 1,
        WallpaperModeEnabled = 64 + 1 + 8,
        WallpaperModeDisabled = 64 + 1,
    };
    
    [Tooltip("Main camera is used if None")]
    public Camera currentCamera;
    
    [Header("Advanced settings")]
    [Tooltip("Change camera background when the window is transparent")]
    public bool autoSwitchCameraBackground = true;
    
    [Header("For Windows only")]
    [Tooltip("Select the method. *Only available on Windows")]
    public TransparentType transparentType = TransparentType.Alpha;
    
    [Tooltip("Will be used the next time the window becomes transparent")]
    public Color32 keyColor = new Color32(0x01, 0x00, 0x01, 0x00);
    
    private CameraClearFlags _originalCameraClearFlags;
    
    private Color _originalCameraBackground;
    
    private float raycastMaxDepth = 100.0f;
    
    [Tooltip("Select the method")]
    public HitTestType hitTestType = HitTestType.Opacity;
    
    [Header("State")]
    [SerializeField, ReadOnly, Tooltip("Is the mouse pointer on an opaque pixel? (Read only)")]
    private bool onObject = true;

    [SerializeField, EditableProperty, Tooltip("Check to set bottommost on startup")]
    private bool _isBottommost = false;
    
    /// <summary>
    /// Low level class
    /// </summary>
    private WinCore _winCore = null;
    
    public bool isHitTestEnabled = true;
    
    private Rect originalWindowRectangle;
    
    /// <summary>
    /// Set editable a bool property
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class EditablePropertyAttribute : UnityEngine.PropertyAttribute { }
    
    private PointerEventData pointerEventData;
    public event OnMonitorChangedDelegate OnMonitorChanged;
    public delegate void OnMonitorChangedDelegate();
    
    public bool shouldFitMonitor
    {
        get { return _shouldFitMonitor; }
        set { FitToMonitor(value, _monitorToFit); }
    }
    [SerializeField, EditableProperty, Tooltip("Check to fit the window to the monitor")]
    private bool _shouldFitMonitor = false;
    
    public int monitorToFit
    {
        get { return _monitorToFit; }
        set { FitToMonitor(_shouldFitMonitor, value); }
    }
    private int _monitorToFit = 0;
    
    /// <summary>
    /// Is this window topmost
    /// </summary>
    public bool IsTopmost
    {
        get { return ((_winCore == null) ? isTopmost : isTopmost = _winCore.IsTopmost); }
        set { SetTopmost(value); }
    }
    [FormerlySerializedAs("_isTopmost")] [SerializeField, EditableProperty, Tooltip("Check to set topmost on startup")]
    private bool isTopmost = false;
    
    public bool IsTransparent
    {
        get { return isTransparent; }
        set { SetTransparent(value); }
    }
    public static WindowController current => _current ? _current : FindOrCreateInstance();
    private static WindowController _current;
    
    [FormerlySerializedAs("_isTransparent")] [SerializeField, EditableProperty, Tooltip("Check to set transparent on startup")]
    private bool isTransparent = false;
    
    private bool _isClickThrough = false;
    
    private int hitTestLayerMask;
    
    public float alphaValue
    {
        get { return _alphaValue; }
        set { SetAlphaValue(value); }
    }
    
    [SerializeField, EditableProperty, Tooltip("Window alpha"), Range(0f, 1f)]
    private float _alphaValue = 1.0f;
    
    private Texture2D colorPickerTexture = null;
    
    [SerializeField, ReadOnly, Tooltip("Pixel color under the mouse pointer. (Read only)")]
    public Color pickedColor;
    
    [Tooltip("Available on the hit test type is Opacity"), RangeAttribute(0f, 1f)]
    public float opacityThreshold = 0.1f;

    
    /// <summary>
    /// Set window alpha
    /// </summary>
    /// <param name="alpha">0.0 to 1.0</param>
    private void SetAlphaValue(float alpha)
    {
        _alphaValue = alpha;
        _winCore?.SetAlphaValue(_alphaValue);
    }
    
    public event OnStateChangedDelegate OnStateChanged;
    public delegate void OnStateChangedDelegate(WindowStateEventType type);
    
    /// <summary>
    /// Fit to the specified monitor
    /// </summary>
    /// <returns></returns>
    private bool FitToMonitor(bool shouldFit, int monitorIndex)
    {
        if (_winCore == null)
        {
            _shouldFitMonitor = shouldFit;
            _monitorToFit = monitorIndex;
            return false;
        }

        if (shouldFit)
        {
            if (!_shouldFitMonitor)
            {
                // 直前はフィットしない状態だった場合
                _monitorToFit = monitorIndex;
                _shouldFitMonitor = shouldFit;
                UpdateMonitorFitting();
            }
            else
            {
                if (_monitorToFit != monitorIndex)
                {
                    // フィット先モニタが変化した場合
                    _monitorToFit = monitorIndex;
                    UpdateMonitorFitting();
                }
            }
        } 
        else
        {
            if (_shouldFitMonitor)
            {
                // 直前はフィット状態で、解除された場合
                _monitorToFit = monitorIndex;
                _shouldFitMonitor = shouldFit;
                UpdateMonitorFitting();

                _winCore.SetZoomed(false);
            }
            else
            {
                // フィット中でなければ選択を変えるのみ
                _monitorToFit = monitorIndex;
            }
        }

        return true;
    }
    
    /// <summary>
    /// Fit to specified monitor
    /// </summary>
    private void UpdateMonitorFitting()
    {
        if (!_shouldFitMonitor) return;

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
        // シングルトンとする。既にインスタンスがあれば自分を破棄
        if (this != current)
        {
            Destroy(this.gameObject);
            return;
        }
        else
        {
            _current = this;
        }

        // フルスクリーン強制解除。エディタでは何もしない
#if !UNITY_EDITOR
            if (forceWindowed && Screen.fullScreen)
            {
                Screen.fullScreen = false;
            }
#endif

        if (!currentCamera)
        {
            // メインカメラを探す
            currentCamera = Camera.main;

            //// もしメインカメラが見つからなければ、Findで探す
            //if (!currentCamera)
            //{
            //    currentCamera = GameObject.FindObjectOfType<Camera>();
            //}
        }

        // カメラの元の背景を記憶
        if (currentCamera)
        {
            _originalCameraClearFlags = currentCamera.clearFlags;
            _originalCameraBackground = currentCamera.backgroundColor;

        }
            
        // マウスイベント情報
        pointerEventData = new PointerEventData(EventSystem.current);
            
        // Ignore Raycast 以外を有効とするマスク
        hitTestLayerMask = ~LayerMask.GetMask("Ignore Raycast");

        // マウス下描画色抽出用テクスチャを準備
        colorPickerTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);

        // ウィンドウ制御用のインスタンス作成
        _winCore = new WinCore();
    }
    void Start()
    {
        // マウスカーソル直下の色を取得するコルーチンを開始
        StartCoroutine(HitTestCoroutine());

        // Get the initial window size and position
        StoreOriginalWindowRectangle();

        // Fit to the selected monitor
        OnMonitorChanged += UpdateMonitorFitting;
        UpdateMonitorFitting();
    }
    
    void Update()
    {
        // 如果未成功获取自身窗口或窗口未激活，则尝试重新获取
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

        // 更新按键和鼠标操作对底层窗口的穿透状态
        UpdateClickThrough();
    }

    /// <summary>
    /// Check and process UniWinCore events
    /// </summary>
    private void UpdateEvents()
    {
        if (_winCore == null) return;
        

        if (_winCore.ObserveWindowStyleChanged(out var type))
        {
            // // モニタへのフィット指定がある状態で最大化解除された場合
            // if (shouldFitMonitor && !uniWinCore.GetZoomed())
            // {
            //     //StartCoroutine("ForceZoomed");    // 時間差で最大化を強制
            //     //SetZoomed(true);        // 強制的に最大化　←必ずしも働かない
            //     //shouldFitMonitor = false;    // フィットを無効化
            // }
            if (_shouldFitMonitor) StartCoroutine("ForceZoomed"); // 時間差で最大化を強制
                
            OnStateChanged?.Invoke((WindowStateEventType)type);
        }
    }
    private IEnumerator HitTestCoroutine()
    {
        while (Application.isPlaying)
        {
            yield return new WaitForEndOfFrame();

            // Windowsの場合、単色での透過ならばヒットテストはOSに任せるため、常にヒット
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
                // ヒットテスト無しの場合は常にtrue
                onObject = true;
            }
        }
        yield return null;
    }
    
    private void HitTestByRaycast()
    {
        var position = Input.mousePosition;
            
        // // uGUIの上か否かを判定
        var raycastResults = new List<RaycastResult>();
        pointerEventData.position = position;
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);
        foreach (var result in raycastResults)
        {
            // レイヤーマスクを考慮（Ignore Raycast 以外ならヒット）
            if (((1 << result.gameObject.layer) & hitTestLayerMask) > 0)
            {
                onObject = true;
                return;
            }
        }

        if (currentCamera && currentCamera.isActiveAndEnabled)
        {
            Ray ray = currentCamera.ScreenPointToRay(position);

            // 3Dオブジェクトの上か否かを判定
            if (Physics.Raycast(ray, out _, raycastMaxDepth))
            {
                onObject = true;
                return;
            }

            // 2Dオブジェクトの上か判定
            var rayHit2D = Physics2D.GetRayIntersection(ray);
            Debug.DrawRay(ray.origin, ray.direction, Color.blue, 2f, false);
            if (rayHit2D.collider != null)
            {
                onObject = true;
                return;
            }
        } else
        {
            // カメラが有効でなければメインカメラを取得
            currentCamera = Camera.main;
        }

        // いずれもヒットしなければオブジェクト上ではないと判断
        onObject = false;
    }
    
    private void HitTestByOpaquePixel()
    {
        Vector2 mousePos = Input.mousePosition;

        // マウス座標を調べる
        if (GetOnOpaquePixel(mousePos))
        {
            //Debug.Log("Mouse " + mousePos);
            onObject = true;
            //activeFingerId = -1;    // タッチ追跡は解除
            return;
        }
        else
        {
            onObject = false;
        }
    }
    
    private bool GetOnOpaquePixel(Vector2 mousePos)
    {
        float w = Screen.width;
        float h = Screen.height;
        //Debug.Log(w + ", " + h);

        // 画面外であれば透明と同様
        if (
            mousePos.x < 0 || mousePos.x >= w
                           || mousePos.y < 0 || mousePos.y >= h
        )
        {
            return false;
        }

        // 透過状態でなければ、範囲内なら不透過扱いとする
        if (!isTransparent) return true;

        // LayeredWindowならばクリックスルーはOSに任せるため、ウィンドウ内ならtrueを返しておく
        if (transparentType == TransparentType.ColorKey) return true;

        // 指定座標の描画結果を見て判断
        try   // WaitForEndOfFrame のタイミングで実行すればtryは無くても大丈夫な気はする
        {
            // Reference http://tsubakit1.hateblo.jp/entry/20131203/1386000440
            colorPickerTexture.ReadPixels(new Rect(mousePos, Vector2.one), 0, 0);
            Color color = colorPickerTexture.GetPixels32()[0];
            pickedColor = color;

            return (color.a >= opacityThreshold);  // αがしきい値以上ならば不透過とする
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex.Message);
            return false;
        }
    }
    void StoreOriginalWindowRectangle()
    {
        if (_winCore != null)
        {
            var size = _winCore.GetWindowSize();
            var pos = _winCore.GetWindowPosition();
            originalWindowRectangle = new Rect(pos, size);
        }
    }
    
    void SetCameraBackground(bool transparent)
    {
        // todo:首先检查currentCamera是否存在以及是否允许自动切换相机背景。如果这两个条件中的任何一个不满足，则方法直接返回，不做任何操作
        if (!currentCamera || !autoSwitchCameraBackground) return;

        // todo: 处理透明
        if (transparent)
        {
            currentCamera.clearFlags = CameraClearFlags.SolidColor;
            if (transparentType == TransparentType.ColorKey)
            {
                currentCamera.backgroundColor = keyColor;
            }
            else
            {
                currentCamera.backgroundColor = Color.clear;
            }
        }
        else
        {
            currentCamera.clearFlags = _originalCameraClearFlags;
            currentCamera.backgroundColor = _originalCameraBackground;
        }
    }
    
    private void SetTopmost(bool topmost)
    {
        if (_winCore == null) return;
        _winCore.EnableTopmost(topmost);
        isTopmost = _winCore.IsTopmost;
    }
    
    /// <summary>
    /// todo: 确保在更换相机时能恢复或应用正确的背景设置
    /// </summary>
    /// <param name="newCamera"></param>
    public void SetCamera(Camera newCamera)
    {
        if (newCamera != currentCamera)
        {
            SetCameraBackground(false);
        }

        currentCamera = newCamera;
        
        if (currentCamera)
        {
            _originalCameraClearFlags = currentCamera.clearFlags;
            _originalCameraBackground = currentCamera.backgroundColor;

            SetCameraBackground(isTransparent);
        }
    }
    
    /// <summary>
    /// todo: 切换透明状态
    /// </summary>
    /// <param name="transparent"></param>
    private void SetTransparent(bool transparent)
    {
        isTransparent = transparent;
        SetCameraBackground(transparent);
#if !UNITY_EDITOR
            if (_winCore != null)
            {
                _winCore.EnableTransparent(transparent);
            }
#endif
        UpdateClickThrough();
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
                SetClickThrough(false);
            }
        }
        else
        {
            // 如果当前不是点击穿透状态，并且是透明的且未命中，则设置为点击穿透
            if (isTransparent && !isHit)
            {
                SetClickThrough(true);
            }
        }
    }
    
    void SetClickThrough(bool isThrough)
    {
        _winCore?.EnableClickThrough(isThrough);
        _isClickThrough = isThrough;
    }
    
    private static WindowController FindOrCreateInstance()
    {
        var instance = FindObjectOfType<WindowController>();
        return instance;
    }
    
    /// <summary>
    /// 如果自身的窗口句柄不确定，则重新搜索
    /// </summary>
    private void UpdateTargetWindow()
    {
        if (_winCore == null)
        {
            _winCore = new WinCore();
        }

        // 如果尚未获取到窗口，则执行获取窗口的操作
        if (!_winCore.IsActive)
        {
            _winCore.AttachMyWindow();

            // 成功获取窗口后，设置初始值
            if (_winCore.IsActive)
            {
                _winCore.SetTransparentType((WinCore.TransparentType)transparentType);
                _winCore.SetKeyColor(keyColor);
                _winCore.SetAlphaValue(_alphaValue);
                SetTransparent(isTransparent);
                if (_isBottommost)
                {
                    SetBottommost(_isBottommost);
                }
                else
                {
                    SetTopmost(isTopmost);
                }
                SetClickThrough(_isClickThrough);

                // 在获取窗口时，执行类似于显示器变更的处理
                OnMonitorChanged?.Invoke();
            }
        }
        else
        {
#if UNITY_EDITOR
            // 在编辑器环境中，由于游戏视图可能被关闭或停靠，如有变化则更换目标窗口
            // 如果当前活动窗口与目标窗口相同，则不执行任何操作
            _winCore.AttachMyActiveWindow();
#endif
        }
    }
    
    private void SetBottommost(bool bottommost)
    {
        if (_winCore == null) return;

        _winCore.EnableBottommost(bottommost);
        _isBottommost = _winCore.IsBottommost;
        isTopmost = _winCore.IsTopmost;
    }

}
