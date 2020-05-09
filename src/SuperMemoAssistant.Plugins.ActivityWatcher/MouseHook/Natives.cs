using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;


// Native code for the mouse hook
// From Process.Net

namespace SuperMemoAssistant.Plugins.ActivityWatcher
{

  public enum MouseMessages
  {
      WmMouseMove = 0x200,
      WmLButtonDown = 0x201,
      WmLButtonUp = 0x202,
      WmRButtonDown = 0x204,
      WmRButtonUp = 0x205,
      WmMButtonDown = 0x207,
      WmMButtonUp = 0x208,
      WmMouseWheel = 0x20A
  }

  public enum MouseEventNames
  {
    MouseMove,
    LeftButtonDown,
    LeftButtonUp,
    RightButtonDown,
    RightButtonUp,
    MiddleButtonDown,
    MiddleButtonUp,
    MouseWheel
  }

  public enum HookEventType
  {
    Keyboard,
    Mouse
  }

  public abstract class HookEventArgs : EventArgs
  {
    protected HookEventType EventType { get; set; }
  }

  public class MouseHookEventArgs : HookEventArgs
  {
    public MouseHookEventArgs(MSLLHOOKSTRUCT lparam)
    {
      EventType = HookEventType.Mouse;
      LParam = lparam;
    }

    private MSLLHOOKSTRUCT LParam { get; }

    public Point Position => LParam.Point;

    public MouseEventNames MouseEventName { get; internal set; }
  }

  /// <summary>
  ///     The <see cref="Point" /> structure defines the x and y coordinates of a point.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct Point
  {
    /// <summary>
    ///     The x-coordinate of the point.
    /// </summary>
    public int X;

    /// <summary>
    ///     The y-coordinate of the point.
    /// </summary>
    public int Y;

    /// <summary>
    ///     Returns a string that represents the current object.
    /// </summary>
    public override string ToString()
    {
      return $"X = {X} Y = {Y}";
    }
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct MSLLHOOKSTRUCT
  {
      public Point Point { get; set; }
      public int MouseData { get; set; }
      public int Flags { get; set; }
      public int Time { get; set; }
      public IntPtr DwExtraInfo { get; set; }
  }

  public enum HookType
  {
    WH_JOURNALRECORD = 0,
    WH_JOURNALPLAYBACK = 1,
    WH_KEYBOARD = 2,
    WH_GETMESSAGE = 3,
    WH_CALLWNDPROC = 4,
    WH_CBT = 5,
    WH_SYSMSGFILTER = 6,
    WH_MOUSE = 7,
    WH_HARDWARE = 8,
    WH_DEBUG = 9,
    WH_SHELL = 10,
    WH_FOREGROUNDIDLE = 11,
    WH_CALLWNDPROCRET = 12,
    WH_KEYBOARD_LL = 13,
    WH_MOUSE_LL = 14
  }

  /// <summary>
  /// For dlls
  /// </summary>
  public static class Natives
  {
    public delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);
    public delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true), SuppressUnmanagedCodeSecurity]
    public static extern int CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    /// <summary>
    ///     Retrieves a handle to the foreground window (the window with which the user is currently working).
    ///     The system assigns a slightly higher priority to the thread that creates the foreground window than it does to
    ///     other threads.
    /// </summary>
    /// <returns>
    ///     The return value is a handle to the foreground window. The foreground window can be NULL in certain
    ///     circumstances, such as when a window is losing activation.
    /// </returns>
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern IntPtr SetWindowsHookEx(HookType code,
        HookProc func,
        IntPtr hInstance,
        int threadId);

    [DllImport("user32.dll", EntryPoint = "SetWindowsHookEx")]
    public static extern IntPtr SetWindowsHookLowLevel(HookType code,
        LowLevelProc func,
        IntPtr hInstance,
        int threadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr GetModuleHandle(string lpModuleName);

    public static IntPtr SetWindowsHook(HookType hookType, LowLevelProc callback)
    {
        IntPtr hookId;
        using (var currentProcess = System.Diagnostics.Process.GetCurrentProcess())
        using (var currentModule = currentProcess.MainModule)
        {
            var handle = GetModuleHandle(currentModule.ModuleName);
            hookId = SetWindowsHookLowLevel(hookType, callback, handle, 0);
        }
        return hookId;
    }
  }
}
