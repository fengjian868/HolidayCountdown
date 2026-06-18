using System;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using HolidayCountdown.Models;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.Components;

[ComponentInfo(
    "D4E5F6A7-B8C9-0123-DEF0-1234567890AB",
    "课程表联动 [测试版]",
    "\uE7BE",
    "读取ClassIsland课程表，显示当前课程/下节课信息或课间倒计时（测试版，不稳定）"
)]
public class ClassScheduleComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private TextBlock _txt = null!;
    private HolidayService? _svc;

    public ClassScheduleComponent()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        _txt = new TextBlock { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Opacity = 0.9 };
        panel.Children.Add(_txt);
        Content = panel;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (s, e) => Update();
        _timer.Start();
        Dispatcher.UIThread.Post(() => { _svc = new HolidayService(); HolidayService.SettingsChanged += OnSettingsChanged; Update(); });
    }

    void OnSettingsChanged()
    {
        _svc?.LoadSettings();
        Dispatcher.UIThread.Post(Update);
    }

    void Update()
    {
        if (_svc == null || !_svc.Settings.ClassScheduleEnabled) { _txt.Text = ""; return; }
        try
        {
            var lessonsService = GetLessonsService();
            if (lessonsService == null) { _txt.Text = ""; return; }

            var currentStateObj = GetPropertyValue(lessonsService, "CurrentState");
            if (currentStateObj == null) { _txt.Text = ""; return; }

            int state = (int)currentStateObj;
            // TimeState: 0=None, 1=OnClass, 2=PrepareOnClass, 3=Breaking, 4=AfterSchool

            var currentSubject = GetPropertyValue(lessonsService, "CurrentSubject");
            var nextSubject = GetPropertyValue(lessonsService, "NextSubject");
            var onClassLeftTime = GetPropertyValue(lessonsService, "OnClassLeftTime");
            var onBreakingTimeLeftTime = GetPropertyValue(lessonsService, "OnBreakingTimeLeftTime");
            var isClassPlanLoaded = GetPropertyValue(lessonsService, "IsClassPlanLoaded");
            var isClassPlanEnabled = GetPropertyValue(lessonsService, "IsClassPlanEnabled");

            if (isClassPlanEnabled is bool enabled && !enabled) { _txt.Text = ""; return; }
            if (isClassPlanLoaded is bool loaded && !loaded) { _txt.Text = ""; return; }

            string result = "";

            switch (state)
            {
                case 1: // OnClass
                    {
                        var subjectName = GetSubjectName(currentSubject);
                        var leftTime = onClassLeftTime as TimeSpan? ?? TimeSpan.Zero;
                        var icon = _svc.Settings.ClassScheduleShowIcon ? "📖 " : "";
                        if (leftTime.TotalSeconds > 0)
                            result = $"{icon}{subjectName} 还有{FormatTime(leftTime)}";
                        else
                            result = $"{icon}{subjectName}";
                    }
                    break;
                case 3: // Breaking
                    {
                        var nextName = GetSubjectName(nextSubject);
                        // 课间状态时，ClassIsland 的 OnClassLeftTime 表示距离下节课开始的剩余时间
                        var leftTime = onClassLeftTime as TimeSpan? ?? TimeSpan.Zero;
                        var icon = _svc.Settings.ClassScheduleShowIcon ? "☕ " : "";
                        if (!string.IsNullOrEmpty(nextName) && nextName != "未安排")
                            result = $"{icon}课间 还有{FormatTime(leftTime)} → {nextName}";
                        else
                            result = $"{icon}课间休息 还有{FormatTime(leftTime)}";
                    }
                    break;
                case 4: // AfterSchool
                    {
                        var icon = _svc.Settings.ClassScheduleShowIcon ? "🏠 " : "";
                        result = $"{icon}放学了";
                    }
                    break;
                case 2: // PrepareOnClass
                    {
                        var nextName = GetSubjectName(nextSubject);
                        var icon = _svc.Settings.ClassScheduleShowIcon ? "🔔 " : "";
                        result = $"{icon}准备上课 → {nextName}";
                    }
                    break;
                default: // None
                    {
                        // Try to show next class info if available
                        var nextName = GetSubjectName(nextSubject);
                        if (!string.IsNullOrEmpty(nextName) && nextName != "未安排")
                        {
                            var icon = _svc.Settings.ClassScheduleShowIcon ? "📅 " : "";
                            result = $"{icon}下节: {nextName}";
                        }
                        else
                        {
                            result = "";
                        }
                    }
                    break;
            }

            _txt.Text = result;
        }
        catch { _txt.Text = ""; }
    }

    string GetSubjectName(object? subject)
    {
        if (subject == null) return "";
        try
        {
            var nameProp = subject.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
            return nameProp?.GetValue(subject)?.ToString() ?? "";
        }
        catch { return ""; }
    }

    string FormatTime(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}小时{ts.Minutes}分";
        if (ts.TotalMinutes >= 1)
            return $"{(int)ts.TotalMinutes}分{ts.Seconds}秒";
        return $"{ts.Seconds}秒";
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

            // Try ILessonsService first, then LessonsService
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
}
