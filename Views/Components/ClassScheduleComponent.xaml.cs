using System;
using System.Collections.Generic;
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
    "课程表联动",
    "\uE7BE",
    "读取ClassIsland课程表，显示当前课程/下节课信息或课间倒计时"
)]
public class ClassScheduleComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private TextBlock _txt = null!;
    private HolidayService? _svc;

    public ClassScheduleComponent()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        _txt = new TextBlock { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Opacity = 0.9, Foreground = Brushes.Black };
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
        if (_svc == null) { _txt.Text = ""; ResetColor(); return; }
        try
        {
            var lessonsService = GetLessonsService();
            if (lessonsService == null) { _txt.Text = ""; ResetColor(); return; }

            var currentStateObj = GetPropertyValue(lessonsService, "CurrentState");
            if (currentStateObj == null) { _txt.Text = ""; ResetColor(); return; }

            int state = (int)currentStateObj;
            // TimeState: 0=None, 1=OnClass, 2=PrepareOnClass, 3=Breaking, 4=AfterSchool

            var currentSubject = GetPropertyValue(lessonsService, "CurrentSubject");
            var nextSubject = GetPropertyValue(lessonsService, "NextSubject");
            var onClassLeftTime = GetPropertyValue(lessonsService, "OnClassLeftTime");
            var onBreakingTimeLeftTime = GetPropertyValue(lessonsService, "OnBreakingTimeLeftTime");
            var isClassPlanLoaded = GetPropertyValue(lessonsService, "IsClassPlanLoaded");
            var isClassPlanEnabled = GetPropertyValue(lessonsService, "IsClassPlanEnabled");

            if (isClassPlanEnabled is bool enabled && !enabled) { _txt.Text = ""; ResetColor(); return; }
            if (isClassPlanLoaded is bool loaded && !loaded) { _txt.Text = GetNoClassText(); ResetColor(); return; }

            ResetColor();
            string result = "";

            switch (state)
            {
                case 1: // OnClass
                    {
                        var subjectName = GetSubjectName(currentSubject);
                        var leftTime = onClassLeftTime as TimeSpan? ?? TimeSpan.Zero;
                        var icon = _svc.Settings.ClassScheduleShowIcon ? GetSubjectIcon(subjectName) + " " : "";
                        var subjectDisplay = _svc.Settings.ClassScheduleShowSubject ? subjectName : "";
                        result = ApplyTemplate(_svc.Settings.ClassScheduleOnClassTemplate, new Dictionary<string, string>
                        {
                            ["icon"] = icon,
                            ["subject"] = subjectDisplay,
                            ["remaining"] = FormatTime(leftTime)
                        });
                    }
                    break;
                case 3: // Breaking
                    {
                        var nextName = GetSubjectName(nextSubject);
                        var leftTime = onClassLeftTime as TimeSpan? ?? TimeSpan.Zero;
                        var icon = _svc.Settings.ClassScheduleShowIcon ? "☕ " : "";
                        var total = TryGetNextClassTotalTime(lessonsService);
                        var totalStr = total.HasValue ? $"（{FormatTime(total.Value)}）" : "";
                        var template = leftTime.TotalMinutes <= _svc.Settings.PreClassMinutes && !string.IsNullOrEmpty(nextName) && nextName != "未安排"
                            ? _svc.Settings.ClassSchedulePrepareTemplate
                            : _svc.Settings.ClassScheduleBreakTemplate;
                        result = ApplyTemplate(template, new Dictionary<string, string>
                        {
                            ["icon"] = icon,
                            ["remaining"] = FormatTime(leftTime),
                            ["next"] = nextName,
                            ["total"] = totalStr
                        });

                        // 课间时长警示
                        if (_svc.Settings.BreakWarningEnabled && leftTime.TotalMinutes <= _svc.Settings.BreakWarningMinutes && leftTime.TotalSeconds > 0)
                            _txt.Foreground = new SolidColorBrush(Color.Parse(_svc.Settings.BreakWarningColor));
                    }
                    break;
                case 4: // AfterSchool
                    {
                        var icon = _svc.Settings.ClassScheduleShowIcon ? "🏠 " : "";
                        result = ApplyTemplate(_svc.Settings.ClassScheduleAfterSchoolTemplate, new Dictionary<string, string>
                        {
                            ["icon"] = icon
                        });
                    }
                    break;
                case 2: // PrepareOnClass
                    {
                        var nextName = GetSubjectName(nextSubject);
                        var icon = _svc.Settings.ClassScheduleShowIcon ? "🔔 " : "";
                        var total = TryGetNextClassTotalTime(lessonsService);
                        var totalStr = total.HasValue ? $"（{FormatTime(total.Value)}）" : "";
                        result = ApplyTemplate(_svc.Settings.ClassSchedulePrepareTemplate, new Dictionary<string, string>
                        {
                            ["icon"] = icon,
                            ["next"] = nextName,
                            ["total"] = totalStr
                        });
                    }
                    break;
                default: // None
                    {
                        var nextName = GetSubjectName(nextSubject);
                        if (!string.IsNullOrEmpty(nextName) && nextName != "未安排")
                        {
                            var icon = _svc.Settings.ClassScheduleShowIcon ? "📅 " : "";
                            result = $"{icon}下节: {nextName}";
                        }
                        else
                        {
                            result = ApplyTemplate(_svc.Settings.ClassScheduleNoClassTemplate, new Dictionary<string, string>
                            {
                                ["icon"] = "",
                                ["text"] = GetNoClassText()
                            });
                        }
                    }
                    break;
            }

            _txt.Text = result;
        }
        catch { _txt.Text = ""; ResetColor(); }
    }

    void ResetColor() => _txt.Foreground = Brushes.Black;

    string GetNoClassText()
    {
        if (_svc == null) return "";
        var now = DateTime.Now;
        var minutes = now.Hour * 60 + now.Minute;
        foreach (var slot in _svc.Settings.NoClassTimeSlots.OrderBy(x => x.StartHour * 60 + x.StartMinute))
        {
            var start = slot.StartHour * 60 + slot.StartMinute;
            var end = slot.EndHour * 60 + slot.EndMinute;
            if (start <= end)
            {
                if (minutes >= start && minutes < end) return slot.Text;
            }
            else
            {
                // 跨天时段（如 18:00 ~ 05:00）
                if (minutes >= start || minutes < end) return slot.Text;
            }
        }
        return "";
    }

    string GetSubjectIcon(string subjectName)
    {
        var s = subjectName.ToLower();
        if (s.Contains("语文")) return "📖";
        if (s.Contains("数学")) return "📐";
        if (s.Contains("英语")) return "🔤";
        if (s.Contains("物理")) return "⚛️";
        if (s.Contains("化学")) return "🧪";
        if (s.Contains("生物")) return "🧬";
        if (s.Contains("历史")) return "🏛️";
        if (s.Contains("地理")) return "🌍";
        if (s.Contains("政治")) return "🏛️";
        if (s.Contains("体育")) return "🏃";
        if (s.Contains("音乐")) return "🎵";
        if (s.Contains("美术")) return "🎨";
        if (s.Contains("信息") || s.Contains("电脑") || s.Contains("计算机")) return "💻";
        return "📖";
    }

    TimeSpan? TryGetNextClassTotalTime(object? lessonsService)
    {
        try
        {
            var nextItem = GetPropertyValue(lessonsService, "NextTimeLayoutItem") ?? GetPropertyValue(lessonsService, "CurrentTimeLayoutItem");
            if (nextItem == null) return null;
            var start = GetPropertyValue(nextItem, "StartTime");
            var end = GetPropertyValue(nextItem, "EndTime");
            if (start == null || end == null) return null;
            var startTs = ToTimeSpan(start);
            var endTs = ToTimeSpan(end);
            if (startTs.HasValue && endTs.HasValue)
            {
                var dur = endTs.Value - startTs.Value;
                if (dur.TotalMinutes < 0) dur += TimeSpan.FromHours(24);
                return dur;
            }
        }
        catch { }
        return null;
    }

    TimeSpan? ToTimeSpan(object obj)
    {
        if (obj is TimeSpan ts) return ts;
        if (obj is DateTime dt) return dt.TimeOfDay;
        if (obj is TimeOnly to) return to.ToTimeSpan();
        if (TimeSpan.TryParse(obj?.ToString(), out var parsed)) return parsed;
        return null;
    }

    string ApplyTemplate(string template, Dictionary<string, string> values)
    {
        if (string.IsNullOrEmpty(template)) return "";
        foreach (var kv in values)
            template = template.Replace($"{{{kv.Key}}}", kv.Value ?? "");
        return template.Trim();
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
