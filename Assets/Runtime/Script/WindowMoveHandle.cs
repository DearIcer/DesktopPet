using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class WindowMoveHandle : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerUpHandler
{
    private WindowController _windowController;


    /// <summary>
    /// 当窗口处于最大化状态时是否禁用移动
    /// </summary>
    [Tooltip("Disable drag-move when the window is zoomed (maximized).")]
    public bool disableOnZoomed = true;

    private bool _isDragging = false;

    /// <summary>
    /// 是否允许进行拖动
    /// </summary>
    private bool IsEnabled
    {
        get { return enabled && (!disableOnZoomed || !IsZoomed); }
    }

    /// <summary>
    /// 是否适应屏幕或已最大化
    /// </summary>
    private bool IsZoomed
    {
        get { return (_windowController && (_windowController.ShouldFitMonitor || _windowController.IsZoomed)); }
    }

    /// <summary>
    /// 记录拖动前自动碰撞检测是否启用
    /// </summary>
    private bool _isHitTestEnabled;

    /// <summary>
    /// 拖动开始时的窗口内坐标[px]
    /// </summary>
    private Vector2 _dragStartedPosition;

    // 在第一个帧更新之前调用
    void Start()
    {
        // 获取场景中的 UniWindowController
        _windowController = GameObject.FindObjectOfType<WindowController>();
        if (_windowController) _isHitTestEnabled = _windowController.isHitTestEnabled;

        //// 下面这行看起来没有必要，所以被注释掉以避免不必要的改变
        //Input.simulateMouseWithTouches = false;
    }

    /// <summary>
    /// 拖动开始时的处理
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsEnabled)
        {
            return;
        }

        // 在Mac上需要调整行为
        // 实际上仅当Retina支持有效时才需要，但窗口坐标系和 eventData.position 的比例会不一致
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            _dragStartedPosition = _uniwinc.windowPosition - _uniwinc.cursorPosition;
#else
        _dragStartedPosition = eventData.position;
#endif

        // 如果 _isDragging 为 false，则认为即将开始拖动
        if (!_isDragging)
        {
            // 在拖动期间禁用碰撞检测
            _isHitTestEnabled = _windowController.isHitTestEnabled;
            _windowController.isHitTestEnabled = false;
            _windowController.IsClickThrough = false;
        }

        _isDragging = true;
    }

    /// <summary>
    /// 拖动结束时的处理
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        EndDragging();
    }

    /// <summary>
    /// 当鼠标抬起时也视为拖动结束
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerUp(PointerEventData eventData)
    {
        EndDragging();
    }

    /// <summary>
    /// 结束拖动
    /// </summary>
    private void EndDragging()
    {
        if (_isDragging)
        {
            _windowController.isHitTestEnabled = _isHitTestEnabled;
        }

        _isDragging = false;
    }

    /// <summary>
    /// 除非窗口最大化，否则通过鼠标拖动移动窗口
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (!_windowController || !_isDragging) return;

        // 如果拖动移动被禁用
        if (!IsEnabled)
        {
            EndDragging();
            return;
        }

        // 当按下左键时移动窗口
        if (eventData.button != PointerEventData.InputButton.Left) return;

        // 如果按下了任何修饰键则返回
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
                                            || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)
                                            || Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) return;

        // 如果全屏则不移动窗口
        // 在编辑器中会误判为 true，因此仅在非编辑器模式下检查
#if !UNITY_EDITOR
            if (Screen.fullScreen)
            {
                EndDragging();
                return;
            }
#endif

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            // 在Mac上，通过原生插件获取和设置光标位置
            _windowController.windowPosition = _windowController.cursorPosition + _dragStartedPosition;
#else
        // 在Windows上，为了支持触摸操作，使用 eventData.position
        // 将窗口移动到与起始位置相匹配的屏幕位置
        _windowController.WindowPosition += eventData.position - _dragStartedPosition;
#endif
    }
}