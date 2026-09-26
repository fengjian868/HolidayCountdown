using System;
using System.Runtime.InteropServices;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;

namespace HolidayCountdown.Automation.Triggers;

/// <summary>
/// 当前台窗口变化时触发。
/// </summary>
[TriggerInfo(
    "holidaycountdown.trigger.foregroundWindowChanged",
    "前台窗口变化时",
    "\uE7F4"
)]
public class ForegroundWindowChangedTrigger : TriggerBase
{
    private IntPtr _hook;
    private WinEventProc? _proc;
    private bool _loaded;

    public override void Loaded()
    {
        if (_loaded) return;
        _loaded = true;
        _proc = new WinEventProc(OnForegroundChanged);
        _hook = SetWinEventHook(
            EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero, _proc, 0, 0, WINEVENT_OUTOFCONTEXT);
    }

    public override void UnLoaded()
    {
        if (!_loaded) return;
        _loaded = false;
        if (_hook != IntPtr.Zero)
        {
            UnhookWinEvent(_hook);
            _hook = IntPtr.Zero;
        }
        _proc = null;
    }

    void OnForegroundChanged(IntPtr hWinEventHook, uint eventCode, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        // 只处理顶层窗口，忽略子对象
        if (idObject != 0 || idChild != 0) return;
        Trigger();
    }

    #region P/Invoke
    const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
    const uint WINEVENT_OUTOFCONTEXT = 0x0000;

    delegate void WinEventProc(IntPtr hWinEventHook, uint eventCode, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    [DllImport("user32.dll")]
    static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventProc lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    static extern bool UnhookWinEvent(IntPtr hWinEventHook);
    #endregion
}
