using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace PUPlayer.App.Playback;

public sealed class MpvSurface : HwndHost
{
    private const int WsChild = 0x40000000;
    private const int WsVisible = 0x10000000;
    private const int WsClipSiblings = 0x04000000;
    private const int WsTabStop = 0x00010000;
    private const int SsNotify = 0x00000100;
    private const int SsBlackRect = 0x00000004;
    private nint handle;
    private bool dragging;
    private int lastX;
    private int lastY;
    private long lastClick;
    private bool moved;
    private int mouseX = int.MinValue;
    private int mouseY = int.MinValue;

    public event Action<nint>? HandleReady;
    public event Action<int, double, double>? ZoomWheel;
    public event Action<double, double>? Dragged;
    public event Action<int>? KeyPressed;
    public event Action<double, double>? Clicked;
    public event Action? DoubleClicked;
    public event Action? MouseMoved;

    protected override HandleRef BuildWindowCore(HandleRef hwndParent)
    {
        handle = CreateWindowEx(0, "STATIC", null, WsChild | WsVisible | WsClipSiblings | WsTabStop | SsNotify | SsBlackRect,
            0, 0, 1, 1, hwndParent.Handle, nint.Zero, nint.Zero, nint.Zero);
        if (handle == nint.Zero) throw new InvalidOperationException("No se pudo crear la superficie de video.");
        HandleReady?.Invoke(handle);
        return new(this, handle);
    }

    protected override void DestroyWindowCore(HandleRef hwnd) => DestroyWindow(hwnd.Handle);

    protected override nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        var x = unchecked((short)(lParam.ToInt64() & 0xffff));
        var y = unchecked((short)((lParam.ToInt64() >> 16) & 0xffff));
        switch (msg)
        {
            case 0x020A:
                var point = new NativePoint(x, y);
                ScreenToClient(hwnd, ref point);
                var delta = unchecked((short)((wParam.ToInt64() >> 16) & 0xffff));
                ZoomWheel?.Invoke(delta, Normalize(point.X, ActualWidth), Normalize(point.Y, ActualHeight));
                handled = true;
                break;
            case 0x0201:
                SetFocus(hwnd);
                SetCapture(hwnd);
                dragging = true;
                moved = false;
                lastX = x;
                lastY = y;
                var now = Environment.TickCount64;
                if (now - lastClick <= GetDoubleClickTime()) { DoubleClicked?.Invoke(); lastClick = 0; }
                else lastClick = now;
                break;
            case 0x0203:
                DoubleClicked?.Invoke();
                handled = true;
                break;
            case 0x0200:
                if (x != mouseX || y != mouseY)
                {
                    mouseX = x; mouseY = y;
                    MouseMoved?.Invoke();
                }
                if (dragging)
                {
                    moved |= Math.Abs(x - lastX) + Math.Abs(y - lastY) > 2;
                    Dragged?.Invoke((x - lastX) / Math.Max(ActualWidth, 1), (y - lastY) / Math.Max(ActualHeight, 1));
                    lastX = x;
                    lastY = y;
                    handled = true;
                }
                break;
            case 0x0202:
                dragging = false;
                ReleaseCapture();
                if (!moved) Clicked?.Invoke(Normalize(x, ActualWidth), Normalize(y, ActualHeight));
                break;
            case 0x0100:
                KeyPressed?.Invoke(unchecked((int)wParam));
                handled = true;
                break;
        }
        return base.WndProc(hwnd, msg, wParam, lParam, ref handled);
    }

    private static double Normalize(int value, double size) => Math.Clamp(value / Math.Max(size, 1), 0, 1);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint(int x, int y) { public int X = x; public int Y = y; }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowEx(int exStyle, string className, string? windowName, int style,
        int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(nint hwnd);

    [DllImport("user32.dll")] private static extern nint SetFocus(nint hwnd);
    [DllImport("user32.dll")] private static extern nint SetCapture(nint hwnd);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool ReleaseCapture();
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool ScreenToClient(nint hwnd, ref NativePoint point);
    [DllImport("user32.dll")] private static extern uint GetDoubleClickTime();
}
