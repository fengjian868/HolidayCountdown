using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Threading;
using HolidayCountdown.Models;

namespace HolidayCountdown.Services;

/// <summary>
/// 下课自动还原后台服务：检测到下课后自动关闭非白名单进程窗口，还原桌面状态。
/// 由 Plugin.Initialize 在实验性功能开启时启动，无需在布局中添加组件。
/// </summary>
public class ClassResetService
{
    private readonly DispatcherTimer _timer;
    private readonly HolidayService _svc;
    private int _lastState = -1;
    private bool _resetPending;

    // 内置硬保护名单（不可移除）
    static readonly HashSet<string> HardProtectedProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "explorer", "ClassIsland", "csrss", "winlogon", "dwm", "sihost",
        "taskmgr", "ctfmon", "SearchUI", "StartMenuExperienceHost",
        "ShellExperienceHost", "RuntimeBroker", "SecurityHealthSystray",
        "SecurityHealthService", "smartscreen", "userinit", "lsass",
        "services", "spoolsv", "wininit", "fontdrvhost", "dllhost",
        "TabTip", "SearchHost", "SearchApp", "TextInputHost"
    };

    public ClassResetService(HolidayService svc)
    {
        _svc = svc;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _timer.Tick += (s, e) => Update();
    }

    public void Start()
    {
        HolidayService.SettingsChanged += OnSettingsChanged;
        _timer.Start();
        Update();
    }

    public void Stop()
    {
        HolidayService.SettingsChanged -= OnSettingsChanged;
        _timer.Stop();
    }

    void OnSettingsChanged()
    {
        Dispatcher.UIThread.Post(Update);
    }

    void Update()
    {
        if (!_svc.Settings.ClassResetEnabled)
        {
            _lastState = -1;
            return;
        }

        try
        {
            var state = GetCurrentState();
            // state: 0=放学/无课, 1=上课中, 2=课间休息
            if (_lastState == 1 && state != 1 && !_resetPending)
            {
                // 刚下课，延迟触发还原
                _resetPending = true;
                var delaySec = _svc.Settings.ClassResetTriggerDelay;
                DispatcherTimer.RunOnce(() => TryReset(), TimeSpan.FromSeconds(delaySec));
            }
            else if (state == 1)
            {
                _resetPending = false;
            }
            _lastState = state;
        }
        catch { }
    }

    void TryReset()
    {
        _resetPending = false;

        if (!_svc.Settings.ClassResetEnabled) return;

        // 如果已经开始上课了，跳过
        var state = GetCurrentState();
        if (state == 1) return;

        _ = DoResetAsync();
    }

    async Task DoResetAsync()
    {
        var keywords = _svc.Settings.ClassResetKeywords ?? new List<string>();
        var whitelist = BuildWhitelist();

        var taskbarWindows = GetTaskbarWindows();
        var toClose = new List<IntPtr>();

        foreach (var (hwnd, title, processName) in taskbarWindows)
        {
            if (HardProtectedProcesses.Contains(processName)) continue;
            if (whitelist.Contains(processName)) continue;
            // 有关键词配置时只关闭匹配关键词的窗口；无关键词则关闭所有非白名单窗口
            if (keywords.Count > 0 && !keywords.Any(k => title.Contains(k, StringComparison.OrdinalIgnoreCase) || processName.Contains(k, StringComparison.OrdinalIgnoreCase)))
                continue;
            toClose.Add(hwnd);
        }

        foreach (var hwnd in toClose)
        {
            CloseWindowGracefully(hwnd);
        }

        // 等待宽限期
        await Task.Delay(2000);

        // 强制结束仍在的进程
        foreach (var (hwnd, title, processName) in taskbarWindows)
        {
            if (HardProtectedProcesses.Contains(processName)) continue;
            if (whitelist.Contains(processName)) continue;
            if (keywords.Count > 0 && !keywords.Any(k => title.Contains(k, StringComparison.OrdinalIgnoreCase) || processName.Contains(k, StringComparison.OrdinalIgnoreCase)))
                continue;
            if (IsWindowStillVisible(hwnd))
                ForceKillProcess(hwnd);
        }
    }

    HashSet<string> BuildWhitelist()
    {
        var set = new HashSet<string>(HardProtectedProcesses, StringComparer.OrdinalIgnoreCase);
        if (_svc.Settings.ClassResetProcessWhitelist != null)
        {
            foreach (var p in _svc.Settings.ClassResetProcessWhitelist)
                set.Add(p);
        }
        return set;
    }

    #region Windows API

    [DllImport("user32.dll")]
    static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    const uint WM_CLOSE = 0x0010;
    const uint GW_OWNER = 4;
    const uint WS_EX_APPWINDOW = 0x00040000;
    const uint WS_EX_TOOLWINDOW = 0x00000080;

    [DllImport("user32.dll")]
    static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    [DllImport("user32.dll")]
    static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

    List<(IntPtr hwnd, string title, string processName)> GetTaskbarWindows()
    {
        var result = new List<(IntPtr, string, string)>();
        try
        {
            EnumWindowsProc callback = (h, l) =>
            {
                if (!IsWindowVisible(h)) return true;

                var exStyle = GetWindowLong(h, -20); // GWL_EXSTYLE = -20
                if ((exStyle & WS_EX_TOOLWINDOW) != 0 && (exStyle & WS_EX_APPWINDOW) == 0)
                    return true;

                var owner = GetWindow(h, GW_OWNER);
                if (owner != IntPtr.Zero) return true;

                var sb = new System.Text.StringBuilder(256);
                GetWindowText(h, sb, 256);
                var title = sb.ToString();
                if (string.IsNullOrEmpty(title)) return true;

                GetWindowThreadProcessId(h, out var pid);
                var procName = "";
                try { procName = Process.GetProcessById((int)pid).ProcessName; } catch { }

                result.Add((h, title, procName));
                return true;
            };
            EnumWindows(callback, IntPtr.Zero);
        }
        catch { }
        return result;
    }

    delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    void CloseWindowGracefully(IntPtr hwnd)
    {
        try { PostMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero); } catch { }
    }

    bool IsWindowStillVisible(IntPtr hwnd)
    {
        try { return IsWindow(hwnd) && IsWindowVisible(hwnd); } catch { return false; }
    }

    void ForceKillProcess(IntPtr hwnd)
    {
        try
        {
            GetWindowThreadProcessId(hwnd, out var pid);
            var proc = Process.GetProcessById((int)pid);
            proc.Kill();
        }
        catch { }
    }

    #endregion

    #region ClassIsland 课表状态

    int GetCurrentState()
    {
        try
        {
            var svc = GetLessonsService();
            if (svc == null) return 0;
            var stateObj = GetPropertyValue(svc, "CurrentState");
            return stateObj as int? ?? 0;
        }
        catch { return 0; }
    }

    object? GetLessonsService()
    {
        try
        {
            var appHostType = Type.GetType("ClassIsland.Shared.IAppHost, ClassIsland.Shared")
                ?? Type.GetType("ClassIsland.Shared.IAppHost, ClassIsland.Core")
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a =>
                    {
                        try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
                    })
                    .FirstOrDefault(t => t.Name == "IAppHost");
            if (appHostType == null) return null;

            var tryGetService = appHostType.GetMethod("TryGetService", BindingFlags.Public | BindingFlags.Static);
            if (tryGetService == null || !tryGetService.IsGenericMethodDefinition) return null;

            var lessonsServiceType = Type.GetType("ClassIsland.Core.Abstractions.Services.ILessonsService, ClassIsland.Core")
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a =>
                    {
                        try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
                    })
                    .FirstOrDefault(t => t.Name == "ILessonsService" || t.Name == "LessonsService");
            if (lessonsServiceType == null) return null;

            var genericMethod = tryGetService.MakeGenericMethod(lessonsServiceType);
            return genericMethod.Invoke(null, null);
        }
        catch { return null; }
    }

    object? GetPropertyValue(object obj, string propName)
    {
        try
        {
            var prop = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            return prop?.GetValue(obj);
        }
        catch { return null; }
    }

    #endregion
}
