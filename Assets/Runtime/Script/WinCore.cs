using System;
using System.Runtime.InteropServices;
using System.Text;
using AOT;
using UnityEditor;
using UnityEngine;


internal class WinCore : IDisposable
{
    /// <summary>
    /// Type of transparent method for Windows only
    /// </summary>
    public enum TransparentType : int
    {
        None = 0,
        Alpha = 1,
        ColorKey = 2,
    }


    /// <summary>
    /// State changed event type (Experimental)
    /// </summary>
    [Flags]
    public enum WindowStateEventType : int
    {
        None = 0,
        StyleChanged = 1,
        Resized = 2,

        TopMostEnabled = 16 + 1 + 8,
        TopMostDisabled = 16 + 1,
        BottomMostEnabled = 32 + 1 + 8,
        BottomMostDisabled = 32 + 1,
        WallpaperModeEnabled = 64 + 1 + 8,
        WallpaperModeDisabled = 64 + 1,
    };

    #region Native functions

    protected class LibUniWinC
    {
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        public delegate void StringCallback([MarshalAs(UnmanagedType.LPWStr)] string returnString);

        [UnmanagedFunctionPointer((CallingConvention.Winapi))]
        public delegate void IntCallback([MarshalAs(UnmanagedType.I4)] int value);


        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsActive();

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsTransparent();

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsBorderless();

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsTopmost();

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsBottommost();

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsMaximized();

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AttachMyWindow();

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AttachMyOwnerWindow();

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AttachMyActiveWindow();

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DetachWindow();

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        public static extern void Update();

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        public static extern void SetTransparent([MarshalAs(UnmanagedType.U1)] bool bEnabled);

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        public static extern void SetBorderless([MarshalAs(UnmanagedType.U1)] bool bEnabled);

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        public static extern void SetAlphaValue(float alpha);

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        public static extern void SetClickThrough([MarshalAs(UnmanagedType.U1)] bool bEnabled);

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        public static extern void SetTopmost([MarshalAs(UnmanagedType.U1)] bool bEnabled);

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        public static extern void SetBottommost([MarshalAs(UnmanagedType.U1)] bool bEnabled);

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        public static extern void SetMaximized([MarshalAs(UnmanagedType.U1)] bool bZoomed);

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        public static extern void SetPosition(float x, float y);

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetPosition(out float x, out float y);

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        public static extern void SetSize(float x, float y);

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetSize(out float x, out float y);

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetClientSize(out float x, out float y);

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterDropFilesCallback(
            [MarshalAs(UnmanagedType.FunctionPtr)] StringCallback callback);

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterDropFilesCallback();

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterMonitorChangedCallback(
            [MarshalAs(UnmanagedType.FunctionPtr)] IntCallback callback);

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterMonitorChangedCallback();

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterWindowStyleChangedCallback(
            [MarshalAs(UnmanagedType.FunctionPtr)] IntCallback callback);

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterWindowStyleChangedCallback();

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetAllowDrop([MarshalAs(UnmanagedType.U1)] bool enabled);

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        public static extern int GetCurrentMonitor();

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        public static extern int GetMonitorCount();

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetMonitorRectangle(int index, out float x, out float y, out float width,
            out float height);

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        public static extern void SetCursorPosition(float x, float y);

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPosition(out float x, out float y);


        #region Working on Windows only

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        public static extern void SetTransparentType(int type);

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        public static extern void SetKeyColor(uint colorref);

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        public static extern int GetDebugInfo();

        [DllImport("LibUniWinC", CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AttachWindowHandle(IntPtr hWnd);

        #endregion
    }

    #endregion

    private static string[] _lastDroppedFiles;
    private static bool _wasDropped = false;
    private static bool _wasMonitorChanged = false;
    private static bool _wasWindowStyleChanged = false;
    private static WindowStateEventType _windowStateEventType = WindowStateEventType.None;

#if UNITY_EDITOR
    /// <summary>
    /// Get the Unity editor window
    /// </summary>
    /// <returns></returns>
    /// <seealso href="http://baba-s.hatenablog.com/entry/2017/09/17/135018"/>
    public static EditorWindow GetGameView()
    {
        var assembly = typeof(EditorWindow).Assembly;
        var type = assembly.GetType("UnityEditor.GameView");
        var gameView = EditorWindow.GetWindow(type);
        return gameView;
    }
#endif

    /// <summary>
    /// Determines whether a window is attached and available
    /// </summary>
    /// <value><c>true</c> if this instance is active; otherwise, <c>false</c>.</value>
    public bool IsActive { get; private set; } = false;

    /// <summary>
    /// Determines whether the attached window is always on the front
    /// </summary>
    public bool IsTopmost
    {
        get { return (IsActive && _isTopmost); }
    }

    private bool _isTopmost = false;

    /// <summary>
    /// Determines whether the attached window is always on the bottom
    /// </summary>
    public bool IsBottommost
    {
        get { return (IsActive && _isBottommost); }
    }

    private bool _isBottommost = false;

    /// <summary>
    /// Determines whether the attached window is transparent
    /// </summary>
    public bool IsTransparent
    {
        get { return (IsActive && _isTransparent); }
    }

    private bool _isTransparent = false;

    /// <summary>
    /// Determines whether the attached window is click-through (i.e., does not receive any mouse action)
    /// </summary>
    public bool IsClickThrough
    {
        get { return (IsActive && _isClickThrough); }
    }

    private bool _isClickThrough = false;

    /// <summary>
    /// Type of transparent method for Windows
    /// </summary>
    private TransparentType _transparentType = TransparentType.Alpha;

    /// <summary>
    /// The color to use for transparency when the transparentType is ColorKey
    /// </summary>
    private Color32 _keyColor = new Color32(1, 0, 1, 0);


    #region Constructor or destructor

    public WinCore()
    {
        IsActive = false;
    }

    ~WinCore()
    {
        Dispose();
    }

    public void Dispose()
    {
        LibUniWinC.UnregisterDropFilesCallback();
        LibUniWinC.UnregisterMonitorChangedCallback();
        LibUniWinC.UnregisterWindowStyleChangedCallback();
    }

    #endregion


    #region Callbacks

    /// <summary>
    /// todo；监测到显示器或分辨率发生变化时被调用。
    /// </summary>
    /// <param name="monitorCount"></param>
    [MonoPInvokeCallback(typeof(LibUniWinC.IntCallback))]
    private static void _monitorChangedCallback([MarshalAs(UnmanagedType.I4)] int monitorCount)
    {
        _wasMonitorChanged = true;
    }

    /// <summary>
    /// todo: 监控窗口样式变化事件，当接收到这类事件的通知时，通过非托管代码触发此托管回调方法
    /// </summary>
    /// <param name="e"></param>
    [MonoPInvokeCallback(typeof(LibUniWinC.IntCallback))]
    private static void _windowStyleChangedCallback([MarshalAs(UnmanagedType.I4)] int e)
    {
        _wasWindowStyleChanged = true;
        _windowStateEventType = (WindowStateEventType)e;
    }

    /// <summary>
    /// todo : 当文件或文件夹被拖放到应用程序中时会被调用
    /// </summary>
    /// <param name="paths"></param>
    [MonoPInvokeCallback(typeof(LibUniWinC.StringCallback))]
    private static void _dropFilesCallback([MarshalAs(UnmanagedType.LPWStr)] string paths)
    {
        // 将包含拖放路径的单个字符串转换为路径数组
        string[] files = ParsePaths(paths);

        if (files.Length > 0)
        {
            _lastDroppedFiles = new string[files.Length];
            files.CopyTo(_lastDroppedFiles, 0);

            _wasDropped = true;
        }
    }

    /// <summary>
    /// 将双引号包围且以换行符（LF）或空字符(null)分隔的字符串转换为数组并返回。
    /// </summary>
    /// <param name="text">待解析的文本字符串。</param>
    /// <returns>解析后的字符串数组。</returns>
    internal static string[] ParsePaths(string text)
    {
        System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>();
        bool inEscaped = false;
        int len = text.Length;
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < len; i++)
        {
            char c = text[i];
            if (c == '"')
            {
                if (inEscaped)
                {
                    if (((i + 1) < len) && text[i + 1] == '"')
                    {
                        i++;
                        sb.Append(c); // 连续的双引号被视为一个双引号
                        continue;
                    }
                }

                inEscaped = !inEscaped; // 如果不是连续的，则切换是否在引号内的状态
            }
            else if (c == '\n')
            {
                if (inEscaped)
                {
                    // 如果在引号内，则视为路径的一部分
                    sb.Append(c);
                }
                else
                {
                    // 如果不在引号内，则作为分隔符，移动到下一个路径
                    if (sb.Length > 0)
                    {
                        list.Add(sb.ToString());
                        //sb.Clear();   // 适用于.NET 4或更高版本
                        sb.Length = 0; // 适用于.NET 2
                    }
                }
            }
            else if (c == '\0')
            {
                // 空字符始终作为分隔符，移动到下一个路径
                if (sb.Length > 0)
                {
                    list.Add(sb.ToString());
                    //sb.Clear();   // 适用于.NET 4或更高版本
                    sb.Length = 0; // 适用于.NET 2
                }
            }
            else
            {
                sb.Append(c);
            }
        }

        if (sb.Length > 0)
        {
            list.Add(sb.ToString());
        }

        // 移除所有空字符串元素
        list.RemoveAll(v => v.Length == 0);
        return list.ToArray();
    }

    #endregion

    #region Find, attach or detach

    /// <summary>
    /// 将窗口状态重置为初始状态并从操作目标中解除。
    /// </summary>
    public void DetachWindow()
    {
#if UNITY_EDITOR
        // 在编辑器中，窗口样式可能不会总是保持在最前，因此默认情况下不处于最前，
        // 在解除关联时将其禁用。
        EnableTopmost(false);
#endif
        LibUniWinC.DetachWindow();
    }

    /// <summary>
    /// 寻找自己的窗口（如果游戏视图是独立窗口，则寻找它）并将其作为操作目标。
    /// </summary>
    /// <returns>是否成功找到并关联窗口。</returns>
    public bool AttachMyWindow()
    {
#if UNITY_EDITOR_WIN
        // 由于没有确保获得游戏视图的方法，所以给予焦点并在之后立即获取活动窗口。
        var gameView = GetGameView();
        if (gameView)
        {
            gameView.Focus();
            LibUniWinC.AttachMyActiveWindow();
        }
#else
        LibUniWinC.AttachMyWindow();
#endif
        // 添加事件处理器
        LibUniWinC.RegisterDropFilesCallback(_dropFilesCallback);
        LibUniWinC.RegisterMonitorChangedCallback(_monitorChangedCallback);
        LibUniWinC.RegisterWindowStyleChangedCallback(_windowStyleChangedCallback);

        IsActive = LibUniWinC.IsActive();
        return IsActive;
    }

    public bool AttachWindowHandle(IntPtr hWnd)
    {
        LibUniWinC.AttachWindowHandle(hWnd);
        IsActive = LibUniWinC.IsActive();
        return IsActive;
    }

    /// <summary>
    /// 选择当前进程中活动的窗口。
    /// 在编辑器中，由于窗口可能会关闭或停靠，因此在获得焦点时调用此方法。
    /// </summary>
    /// <returns>是否成功选择活动窗口。</returns>
    public bool AttachMyActiveWindow()
    {
        LibUniWinC.AttachMyActiveWindow();
        IsActive = LibUniWinC.IsActive();
        return IsActive;
    }

    #endregion

    #region About window status

    /// <summary>
    /// 定期调用此方法以维护窗口样式。
    /// </summary>
    public void Update()
    {
        LibUniWinC.Update();
    }

    string GetDebugWindowSizeInfo()
    {
        float x, y, cx, cy;
        LibUniWinC.GetSize(out x, out y);
        LibUniWinC.GetClientSize(out cx, out cy);
        return $"W:{x},H:{y} CW:{cx},CH:{cy}";
    }

    /// <summary>
    /// 设置/解除透明度。
    /// </summary>
    /// <param name="isTransparent">是否启用透明度。</param>
    public void EnableTransparent(bool isTransparent)
    {
        // 编辑器可能无法实现透明度，或者边框与常规情况不同，因此跳过。
#if !UNITY_EDITOR
        LibUniWinC.SetTransparent(isTransparent);
        LibUniWinC.SetBorderless(isTransparent);
#endif
        this._isTransparent = isTransparent;
    }

    /// <summary>
    /// 设置窗口的透明度（Alpha值）。
    /// </summary>
    /// <param name="alpha">范围0.0 - 1.0。</param>
    public void SetAlphaValue(float alpha)
    {
        // 在Windows编辑器中，一旦设为半透明，显示可能不再更新，因此禁用。在Mac上则可以。
#if !UNITY_EDITOR_WIN
        LibUniWinC.SetAlphaValue(alpha);
#endif
    }

    /// <summary>
    /// 设置窗口的Z顺序（是否置于最前）。
    /// </summary>
    /// <param name="isTopmost">如果设置为<c>true</c>，则窗口置于最前。</param>
    public void EnableTopmost(bool isTopmost)
    {
        LibUniWinC.SetTopmost(isTopmost);
        this._isTopmost = isTopmost;
        this._isBottommost = false; // 排他性
    }

    /// <summary>
    /// 设置窗口的Z顺序（是否置于最后）。
    /// </summary>
    /// <param name="isBottommost">如果设置为<c>true</c>，则窗口置于最后。</param>
    public void EnableBottommost(bool isBottommost)
    {
        LibUniWinC.SetBottommost(isBottommost);
        this._isBottommost = isBottommost;
        this._isTopmost = false; // 排他性
    }

    /// <summary>
    /// 设置/解除点击穿透。
    /// </summary>
    /// <param name="isThrough">是否启用点击穿透。</param>
    public void EnableClickThrough(bool isThrough)
    {
        // 在编辑器中如果启用了点击穿透，可能会导致无法操作，因此跳过。
#if !UNITY_EDITOR
        LibUniWinC.SetClickThrough(isThrough);
#endif
        this._isClickThrough = isThrough;
    }

    /// <summary>
    /// 将窗口最大化（在Mac上为缩放）。
    /// 最大化后可能会发生尺寸更改，在当前状态下，可能无法保证正常工作。
    /// </summary>
    public void SetZoomed(bool isZoomed)
    {
        LibUniWinC.SetMaximized(isZoomed);
    }

    /// <summary>
    /// 获取窗口是否已被最大化（在Mac上为缩放）。
    /// 最大化后可能会发生尺寸更改，在当前状态下，可能无法保证正常工作。
    /// </summary>
    public bool GetZoomed()
    {
        return LibUniWinC.IsMaximized();
    }

    /// <summary>
    /// 设置窗口的位置。
    /// </summary>
    /// <param name="position">位置。</param>
    public void SetWindowPosition(Vector2 position)
    {
        LibUniWinC.SetPosition(position.x, position.y);
    }

    /// <summary>
    /// 获取窗口的位置。
    /// </summary>
    /// <returns>位置。</returns>
    public Vector2 GetWindowPosition()
    {
        Vector2 pos = Vector2.zero;
        LibUniWinC.GetPosition(out pos.x, out pos.y);
        return pos;
    }


    /// <summary>
    /// Set the window size.
    /// </summary>
    /// <param name="size">x is width and y is height</param>
    public void SetWindowSize(Vector2 size)
    {
        LibUniWinC.SetSize(size.x, size.y);
    }

    /// <summary>
    /// Get the window Size.
    /// </summary>
    /// <returns>x is width and y is height</returns>
    public Vector2 GetWindowSize()
    {
        Vector2 size = Vector2.zero;
        LibUniWinC.GetSize(out size.x, out size.y);
        return size;
    }

    /// <summary>
    /// Get the client area ize.
    /// </summary>
    /// <returns>x is width and y is height</returns>
    public Vector2 GetClientSize()
    {
        Vector2 size = Vector2.zero;
        LibUniWinC.GetClientSize(out size.x, out size.y);
        return size;
    }

    #endregion


    #region Event observers

    /// <summary>
    /// Check files dropping and unset the dropped flag
    /// </summary>
    /// <param name="files"></param>
    /// <returns>true if files were dropped</returns>
    public bool ObserveDroppedFiles(out string[] files)
    {
        files = _lastDroppedFiles;

        if (!_wasDropped || files == null) return false;

        _wasDropped = false;
        return true;
    }

    /// <summary>
    /// Check the numbers of display or resolution changing, and unset the flag 
    /// </summary>
    /// <returns>true if changed</returns>
    public bool ObserveMonitorChanged()
    {
        if (!_wasMonitorChanged) return false;

        _wasMonitorChanged = false;
        return true;
    }

    /// <summary>
    /// Check window style was changed, and unset the flag 
    /// </summary>
    /// <returns>True if window styel was changed</returns>
    public bool ObserveWindowStyleChanged()
    {
        if (!_wasWindowStyleChanged) return false;

        _windowStateEventType = WindowStateEventType.None;
        _wasWindowStyleChanged = false;
        return true;
    }

    /// <summary>
    /// Check window style was changed, and unset the flag 
    /// </summary>
    /// <returns>True if window styel was changed</returns>
    public bool ObserveWindowStyleChanged(out WindowStateEventType type)
    {
        if (!_wasWindowStyleChanged)
        {
            type = WindowStateEventType.None;
            return false;
        }

        type = _windowStateEventType;
        _windowStateEventType = WindowStateEventType.None;
        _wasWindowStyleChanged = false;
        return true;
    }

    #endregion

    #region About mouse cursor

    /// <summary>
    /// Set the mouse pointer position.
    /// </summary>
    /// <param name="position">Position.</param>
    public static void SetCursorPosition(Vector2 position)
    {
        LibUniWinC.SetCursorPosition(position.x, position.y);
    }

    /// <summary>
    /// Get the mouse pointer position.
    /// </summary>
    /// <returns>The position.</returns>
    public static Vector2 GetCursorPosition()
    {
        Vector2 pos = Vector2.zero;
        LibUniWinC.GetCursorPosition(out pos.x, out pos.y);
        return pos;
    }

    // Not implemented
    public static bool GetCursorVisible()
    {
        return true;
    }

    #endregion

    #region for Windows only

    /// <summary>
    /// 指定透明度方法（仅支持Windows）
    /// </summary>
    /// <param name="type">透明类型</param>
    public void SetTransparentType(TransparentType type)
    {
        LibUniWinC.SetTransparentType((Int32)type);
        _transparentType = type;
    }

    /// <summary>
    /// 指定单色透明时的透明颜色（仅支持Windows）
    /// </summary>
    /// <param name="color">颜色</param>
    public void SetKeyColor(Color32 color)
    {
        LibUniWinC.SetKeyColor((UInt32)(color.b * 0x10000 + color.g * 0x100 + color.r));
        _keyColor = color;
    }

    #region 关于显示器

    /// <summary>
    /// 获取窗口所在的显示器索引
    /// </summary>
    /// <returns>显示器索引</returns>
    public int GetCurrentMonitor()
    {
        return LibUniWinC.GetCurrentMonitor();
    }

    /// <summary>
    /// 获取连接的显示器数量
    /// </summary>
    /// <returns>数量</returns>
    public static int GetMonitorCount()
    {
        return LibUniWinC.GetMonitorCount();
    }

    /// <summary>
    /// 获取显示器的位置和尺寸
    /// </summary>
    /// <param name="index">显示器索引</param>
    /// <param name="position">位置</param>
    /// <param name="size">尺寸</param>
    /// <returns>是否成功</returns>
    public static bool GetMonitorRectangle(int index, out Vector2 position, out Vector2 size)
    {
        return LibUniWinC.GetMonitorRectangle(index, out position.x, out position.y, out size.x, out size.y);
    }

    /// <summary>
    /// 将窗口适配到指定的显示器
    /// </summary>
    /// <param name="monitorIndex">显示器索引</param>
    /// <returns>是否成功</returns>
    public bool FitToMonitor(int monitorIndex)
    {
        float dx, dy, dw, dh;
        if (LibUniWinC.GetMonitorRectangle(monitorIndex, out dx, out dy, out dw, out dh))
        {
            // 如果处于最大化状态，则先恢复原状
            if (LibUniWinC.IsMaximized()) LibUniWinC.SetMaximized(false);

            // 计算指定显示器的中心坐标
            float cx = dx + (dw / 2);
            float cy = dy + (dh / 2);

            // 将窗口中心移动到指定显示器的中心
            float ww, wh;
            LibUniWinC.GetSize(out ww, out wh);
            float wx = cx - (ww / 2);
            float wy = cy - (wh / 2);
            LibUniWinC.SetPosition(wx, wy);

            // 最大化窗口
            LibUniWinC.SetMaximized(true);

            //Debug.Log(String.Format("Monitor {4} : {0},{1} - {2},{3}", dx, dy, dw, dh, monitorIndex));
            return true;
        }

        return false;
    }

    #endregion


    /// <summary>
    /// Print monitor list
    /// </summary>
    [Obsolete]
    public static void DebugMonitorInfo()
    {
        int monitors = LibUniWinC.GetMonitorCount();

        int currentMonitorIndex = LibUniWinC.GetCurrentMonitor();

        string message = "Current monitor: " + currentMonitorIndex + "\r\n";

        for (int i = 0; i < monitors; i++)
        {
            float x, y, w, h;
            bool result = LibUniWinC.GetMonitorRectangle(i, out x, out y, out w, out h);
            message += String.Format(
                "Monitor {0}: X:{1}, Y:{2} - W:{3}, H:{4}\r\n",
                i, x, y, w, h
            );
        }

        Debug.Log(message);
    }

    #endregion
}