using System.Runtime.InteropServices;

namespace Titan.ChiefOfStaff;

public sealed class NativeOverlay : IOverlay
{
    private readonly HostPaths _paths;
    private readonly FileOverlay _file;
    private readonly Thread? _thread;
    private readonly CancellationTokenSource _cts = new();
    private nint _hwnd;
    private OverlayState _state;

    public bool Closed { get; private set; }

    public NativeOverlay(HostPaths paths, FileOverlay file)
    {
        _paths = paths;
        _file = file;
        _state = OverlayState.Waiting("Sign in as Bordine, Glenn. COS starts when the session is live.");
        _file.Publish(_state);
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        _thread = new Thread(MessagePump)
        {
            IsBackground = true,
            Name = "TitanCOS-Overlay"
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
    }

    public void Publish(OverlayState state)
    {
        _state = state;
        _file.Publish(state);
        if (_hwnd != 0)
        {
            InvalidateRect(_hwnd, nint.Zero, true);
        }

        if (_file.Closed)
        {
            Closed = true;
        }
    }

    public void Dispose()
    {
        Closed = true;
        _cts.Cancel();
        if (_hwnd != 0)
        {
            PostMessage(_hwnd, WM_CLOSE, nint.Zero, nint.Zero);
        }

        _file.Dispose();
        _cts.Dispose();
    }

    private void MessagePump()
    {
        try
        {
            var className = "TitanCosHud";
            var wndProc = new WndProc(WindowProc);
            var wc = new WNDCLASSEX
            {
                cbSize = Marshal.SizeOf<WNDCLASSEX>(),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(wndProc),
                hInstance = GetModuleHandle(null),
                lpszClassName = className,
                hbrBackground = GetStockObject(4),
                hCursor = LoadCursor(nint.Zero, 32512)
            };
            GC.KeepAlive(wndProc);
            RegisterClassEx(ref wc);
            var width = 440;
            var height = 260;
            var x = GetSystemMetrics(SM_CXSCREEN) - width - 24;
            var y = 24;
            _hwnd = CreateWindowEx(
                WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_LAYERED | WS_EX_NOACTIVATE,
                className,
                "Titan COS",
                WS_POPUP | WS_VISIBLE | WS_BORDER,
                x,
                y,
                width,
                height,
                nint.Zero,
                nint.Zero,
                wc.hInstance,
                nint.Zero);
            if (_hwnd == 0)
            {
                return;
            }

            SetLayeredWindowAttributes(_hwnd, 0, 235, LWA_ALPHA);
            SetWindowPos(_hwnd, HWND_TOPMOST, x, y, width, height, SWP_SHOWWINDOW);
            ShowWindow(_hwnd, SW_SHOWNOACTIVATE);
            while (!_cts.IsCancellationRequested && GetMessage(out var msg, nint.Zero, 0, 0) > 0)
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
        }
        catch
        {
            // File overlay remains the live HUD if Win32 create fails.
        }

        Closed = true;
    }

    private nint WindowProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        if (msg == WM_PAINT)
        {
            var hdc = BeginPaint(hWnd, out var ps);
            try
            {
                FillRect(hdc, ref ps.rcPaint, GetStockObject(4));
                SetBkMode(hdc, 1);
                SetTextColor(hdc, 0x00D4E8FF);
                var text = _state.Render();
                var rect = ps.rcPaint;
                DrawText(hdc, text, -1, ref rect, DT_LEFT | DT_TOP | DT_WORDBREAK);
            }
            finally
            {
                EndPaint(hWnd, ref ps);
            }

            return nint.Zero;
        }

        if (msg == WM_DESTROY || msg == WM_CLOSE)
        {
            File.WriteAllText(_paths.StopFlag, EasternClock.Now().ToString("o"));
            Closed = true;
            PostQuitMessage(0);
            return nint.Zero;
        }

        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    private const uint WM_PAINT = 0x000F;
    private const uint WM_CLOSE = 0x0010;
    private const uint WM_DESTROY = 0x0002;
    private const int WS_POPUP = unchecked((int)0x80000000);
    private const int WS_VISIBLE = 0x10000000;
    private const int WS_BORDER = 0x00800000;
    private const int WS_EX_TOPMOST = 0x00000008;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const uint LWA_ALPHA = 0x00000002;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const int SW_SHOWNOACTIVATE = 4;
    private const int SM_CXSCREEN = 0;
    private const uint DT_LEFT = 0x0000;
    private const uint DT_TOP = 0x0000;
    private const uint DT_WORDBREAK = 0x0010;
    private static readonly nint HWND_TOPMOST = new(-1);

    private delegate nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEX
    {
        public int cbSize;
        public int style;
        public nint lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public nint hInstance;
        public nint hIcon;
        public nint hCursor;
        public nint hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
        public nint hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public nint hwnd;
        public uint message;
        public nint wParam;
        public nint lParam;
        public uint time;
        public POINT pt;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PAINTSTRUCT
    {
        public nint hdc;
        public int fErase;
        public RECT rcPaint;
        public int fRestore;
        public int fIncUpdate;
        public long reserved1;
        public long reserved2;
        public long reserved3;
        public long reserved4;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowEx(int dwExStyle, string lpClassName, string lpWindowName, int dwStyle, int x, int y, int nWidth, int nHeight, nint hWndParent, nint hMenu, nint hInstance, nint lpParam);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool SetLayeredWindowAttributes(nint hwnd, uint crKey, byte bAlpha, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out MSG lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern nint DispatchMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern nint DefWindowProc(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern void PostQuitMessage(int nExitCode);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(nint hWnd, uint Msg, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool InvalidateRect(nint hWnd, nint lpRect, bool bErase);

    [DllImport("user32.dll")]
    private static extern nint BeginPaint(nint hWnd, out PAINTSTRUCT lpPaint);

    [DllImport("user32.dll")]
    private static extern bool EndPaint(nint hWnd, ref PAINTSTRUCT lpPaint);

    [DllImport("user32.dll")]
    private static extern int FillRect(nint hDC, ref RECT lprc, nint hbr);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int DrawText(nint hdc, string lpchText, int nCount, ref RECT lprc, uint uFormat);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern nint LoadCursor(nint hInstance, int lpCursorName);

    [DllImport("gdi32.dll")]
    private static extern nint GetStockObject(int i);

    [DllImport("gdi32.dll")]
    private static extern int SetBkMode(nint hdc, int mode);

    [DllImport("gdi32.dll")]
    private static extern int SetTextColor(nint hdc, int color);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandle(string? lpModuleName);
}
