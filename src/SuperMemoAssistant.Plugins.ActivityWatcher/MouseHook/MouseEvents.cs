using SuperMemoAssistant.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


/// <summary>
///  All from proces.net
/// </summary>
namespace SuperMemoAssistant.Plugins.ActivityWatcher
{

  public class MouseMoveHook
  {
    private readonly Natives.LowLevelProc _callback;
    private IntPtr _hookId;
    public bool IsActive { get; set; } = false;
    public bool IsDisposed { get; set; } = false;

    public MouseMoveHook()
    {
      _callback = MouseHookCallback;
      _hookId = Natives.SetWindowsHook(HookType.WH_MOUSE_LL, _callback);
    }

    ~MouseMoveHook()
    {
      Dispose();
    }

    /// <summary>
    ///     Call this method to unhook the Mouse Hook, and to release resources allocated to it.
    /// </summary>
    public void Dispose()
    {
      if (IsDisposed)
        return;
      IsDisposed = true;
      if (IsActive)
        Disable();
      GC.SuppressFinalize(this);
    }

    public void Disable()
    {
        if (!IsActive) return;
        Natives.UnhookWindowsHookEx(_hookId);
        IsActive = false;
    }

    /// <summary>
    ///     This is the callback method that is called whenever a low level mouse event is triggered.
    ///     We use it to call our individual custom events.
    /// </summary>
    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
      if (nCode >= 0)
      {
        var lParamStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
        var e = new MouseHookEventArgs(lParamStruct);
        switch ((MouseMessages)wParam)
        {
          case MouseMessages.WmMouseMove:
            TriggerMouseEvent(e, MouseEventNames.MouseMove, OnMove);
            break;
        }
      }
      return (IntPtr)Natives.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public event EventHandler<MouseHookEventArgs> Move;
    private void OnMove(MouseHookEventArgs e)
    {
      if (Svc.SM.UI.ElementWdw.IsAvailable)
      {
        IntPtr SMElementWdw = Svc.SM.UI.ElementWdw.Handle;
        IntPtr ForegroundWdw = Natives.GetForegroundWindow();

        if (SMElementWdw == ForegroundWdw)
        {
          Move?.Invoke(this, e);
        }
      }
    }

    private static void TriggerMouseEvent(MouseHookEventArgs e, MouseEventNames name,
        Action<MouseHookEventArgs> method)
    {
      e.MouseEventName = name;
      method(e);
    }
  }
}
