using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.Components;

[ComponentInfo(
    "B2C3D4E5-F6A7-8901-BCDE-F23456789012",
    "下课自动还原[测试版]",
    "\uE74E",
    "下课后自动关闭非白名单进程的窗口，还原桌面状态"
)]
public class ClassResetComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private TextBlock _status = null!;
    private HolidayService? _svc;
    private int _lastState = -1;
    private DateTime? _classEndTime;

    // 内置硬保护名单（不可移除）
    static readonly HashSet<string> HardProtectedProcesses = new()
    {
        "explorer", "ClassIsland", "csrss", "winlogon", "dwm", "sihost",
        "taskmgr", "ctfmon", "SearchUI", "StartMenuExperienceHost",
        "ShellExperienceHost", "RuntimeBroker", "SecurityHealthSystray",
        "SecurityHealthService", "smartscreen", "userinit", "lsass",
        "services", "spoolsv", "wininit", "fontdrvhost", "dllhost",
        "TabTip", "SearchHost", "SearchApp", "TextInputHost"
    };

    public ClassResetComponent()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        _status = new TextBlock { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Opacity = 0.9 };
        _status[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
        panel.Children.Add(_status);
        Content = panel;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _timer.Tick += (s, e) => Update();
        _timer.Start();

        Dispatcher.UIThread.Post(() =>
        {
            _svc = new HolidayService();
            HolidayService.SettingsChanged += OnSettingsChanged;
            Update();
        });
    }

    void OnSettingsChanged()
    {
        _svc?.LoadSettings();
        Dispatcher.UIThread.Post(Update);
    }

    void Update()
    {
        if (_svc == null || !_svc.Settings.ClassResetEnabled)
        {
            _status.Text = "";
            return;
        }

        try
        {
            var state = GetCurrentState();
            // state: 0=放学/无课, 1=上课中, 2=课间休息
            if (_lastState == 1 && state != 1)
            {
                // 刚下课
                _classEndTime = DateTime.Now;
                _status.Text = "⏳ 等待还原…";
                // 延迟触发
                var delaySec = _svc.Settings.ClassResetTriggerDelay;
                DispatcherTimer.RunOnce(() => TryReset(), TimeSpan.FromSeconds(delaySec));
            }
            else if (state == 1)
            {
                _classEndTime = null;
                _status.Text = "";
            }
            _lastState = state;
        }
        catch { _status.Text = ""; }
    }

    void TryReset()
    {
        if (_svc == null || !_svc.Settings.ClassResetEnabled) return;

        // 如果已经开始上课了，跳过
        var state = GetCurrentState();
        if (state == 1) { _status.Text = ""; return; }

        _status.Text = "🔄 正在还原…";
        Dispatcher.UIThread.Post(async () =>
        {
            await DoResetAsync();
            _status.Text = "";
        });
    }

    async Task DoResetAsync()
    {
        var keywords = _svc?.Settings.ClassResetKeywords ?? new List<string>();
        var whitelist = BuildWhitelist();

        var taskbarWindows = GetTaskbarWindows();
        var toClose = new List<IntPtr>();

        foreach (var (hwnd, title, processName) in taskbarWindows)
        {
            // 跳过硬保护进程
            if (HardProtectedProcesses.Contains(processName)) continue;
            // 跳过用户白名单
            if (whitelist.Contains(processName)) continue;
            // 如果有关键词配置，只关闭匹配关键词的窗口；无关键词配置则关闭所有非白名单窗口
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
        if (_svc?.Settings.ClassResetProcessWhitelist != null)
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

    [DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
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
            IntPtr hwnd = IntPtr.Zero;
            hwnd = GetWindow(IntPtr.Zero, 0); // GW_HWNDFIRST = 0 via GetDesktopWindow chain
            // 枚举所有顶层窗口
            EnumWindowsProc callback = (h, l) =>
            {
                if (!IsWindowVisible(h)) return true;

                // 排除工具窗口
                var exStyle = GetWindowLong(h, -20); // GWL_EXSTYLE = -20
                if ((exStyle & WS_EX_TOOLWINDOW) != 0 && (exStyle & WS_EX_APPWINDOW) == 0)
                    return true;

                // 排除有 Owner 的窗口
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
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == "IAppHost");
            if (appHostType == null) return null;

            var tryGetService = appHostType.GetMethod("TryGetService", BindingFlags.Public | BindingFlags.Static);
            if (tryGetService == null || !tryGetService.IsGenericMethodDefinition) return null;

            var lessonsServiceType = Type.GetType("ClassIsland.Core.Abstractions.Services.ILessonsService, ClassIsland.Core")
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
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
