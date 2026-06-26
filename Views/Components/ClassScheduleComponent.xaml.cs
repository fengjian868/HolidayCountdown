using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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

            var leftTimeOnClass = onClassLeftTime as TimeSpan? ?? TimeSpan.Zero;
            var leftTimeBreaking = onBreakingTimeLeftTime as TimeSpan? ?? TimeSpan.Zero;
            var subjectName = _svc.Settings.ClassScheduleShowSubject ? GetSubjectName(currentSubject) : "";
            var nextName = GetNextSubjectName(lessonsService!, currentSubject, nextSubject);

            string template;
            string stateText;
            string countdownText;
            string iconText;
            string text = "";
            bool warning = false;

            switch (state)
            {
                case 1: // OnClass
                    stateText = "上课中";
                    iconText = _svc.Settings.ClassScheduleShowIcon ? "📖" : "";
                    countdownText = leftTimeOnClass.TotalSeconds > 0 ? FormatTime(leftTimeOnClass) : "";
                    template = _svc.Settings.ClassScheduleOnClassTemplate;
                    break;
                case 3: // Breaking
                    stateText = "课间";
                    iconText = _svc.Settings.ClassScheduleShowIcon ? "☕" : "";
                    var breakLeft = leftTimeBreaking.TotalSeconds > 0 ? leftTimeBreaking : leftTimeOnClass;
                    countdownText = breakLeft.TotalSeconds > 0 ? FormatTime(breakLeft) : "";
                    if (breakLeft.TotalSeconds > 0 && breakLeft.TotalMinutes <= _svc.Settings.PreClassMinutes)
                    {
                        template = _svc.Settings.ClassSchedulePrepareTemplate;
                        stateText = "准备上课";
                        iconText = _svc.Settings.ClassScheduleShowIcon ? "🔔" : "";
                    }
                    else
                    {
                        template = _svc.Settings.ClassScheduleBreakTemplate;
                        if (_svc.Settings.BreakWarningEnabled && breakLeft.TotalSeconds > 0 && breakLeft.TotalMinutes <= _svc.Settings.BreakWarningMinutes)
                            warning = true;
                    }
                    break;
                case 4: // AfterSchool
                    stateText = "放学";
                    iconText = _svc.Settings.ClassScheduleShowIcon ? "🏠" : "";
                    countdownText = "";
                    template = _svc.Settings.ClassScheduleAfterSchoolTemplate;
                    break;
                case 2: // PrepareOnClass
                    stateText = "准备上课";
                    iconText = _svc.Settings.ClassScheduleShowIcon ? "🔔" : "";
                    countdownText = leftTimeOnClass.TotalSeconds > 0 ? FormatTime(leftTimeOnClass) : "";
                    template = _svc.Settings.ClassSchedulePrepareTemplate;
                    break;
                default: // None
                    stateText = "暂无课程";
                    iconText = _svc.Settings.ClassScheduleShowIcon ? "📅" : "";
                    countdownText = leftTimeOnClass.TotalSeconds > 0 ? FormatTime(leftTimeOnClass) : "";
                    text = GetNoClassText();
                    template = _svc.Settings.ClassScheduleNoClassTemplate;
                    break;
            }

            if (string.IsNullOrWhiteSpace(template))
                template = "{icon}{subject} 还有{countdown}";

            var result = template
                .Replace("{icon}", string.IsNullOrEmpty(iconText) ? "" : $"{iconText} ")
                .Replace("{subject}", subjectName)
                .Replace("{next}", nextName)
                .Replace("{countdown}", countdownText)
                .Replace("{state}", stateText)
                .Replace("{text}", text);

            result = Regex.Replace(result, @"\s+", " ").Trim();

            _txt.Text = result;
            _txt.Foreground = warning && Color.TryParse(_svc.Settings.BreakWarningColor, out var warnColor)
                ? new SolidColorBrush(warnColor)
                : null;
        }
        catch { _txt.Text = ""; }
    }

    string GetNoClassText()
    {
        var now = DateTime.Now;
        var slots = _svc?.Settings.NoClassTimeSlots
            .OrderBy(x => x.StartHour * 60 + x.StartMinute)
            .ToList() ?? new List<NoClassTimeSlot>();
        foreach (var slot in slots)
        {
            var start = slot.StartHour * 60 + slot.StartMinute;
            var end = slot.EndHour * 60 + slot.EndMinute;
            var cur = now.Hour * 60 + now.Minute;
            if (start <= end)
            {
                if (cur >= start && cur < end) return slot.Text;
            }
            else
            {
                if (cur >= start || cur < end) return slot.Text;
            }
        }
        return "";
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

    string GetNextSubjectName(object lessonsService, object? currentSubject, object? nextSubject)
    {
        // 1. 直接尝试 NextSubject.Name
        if (nextSubject != null)
        {
            var name = GetSubjectName(nextSubject);
            if (!string.IsNullOrEmpty(name)) return name;
        }

        // 2. 尝试 NextTimeLayoutItem.Subject.Name
        var nextItem = GetPropertyValue(lessonsService, "NextTimeLayoutItem");
        if (nextItem != null)
        {
            var subj = GetPropertyValue(nextItem, "Subject");
            if (subj != null)
            {
                var name = GetSubjectName(subj);
                if (!string.IsNullOrEmpty(name)) return name;
            }
            // 有些版本 NextTimeLayoutItem 本身就是 Subject
            var name2 = GetSubjectName(nextItem);
            if (!string.IsNullOrEmpty(name2)) return name2;
        }

        // 3. 尝试 CurrentTimeLayoutItem 的下一个项目
        var currentItem = GetPropertyValue(lessonsService, "CurrentTimeLayoutItem");
        if (currentItem != null)
        {
            var timeLayout = GetPropertyValue(lessonsService, "CurrentTimeLayout")
                          ?? GetPropertyValue(lessonsService, "TimeLayout");
            if (timeLayout != null)
            {
                var itemsProp = timeLayout.GetType().GetProperty("Items", BindingFlags.Public | BindingFlags.Instance)
                              ?? timeLayout.GetType().GetProperty("LayoutItems", BindingFlags.Public | BindingFlags.Instance);
                if (itemsProp?.GetValue(timeLayout) is System.Collections.IEnumerable items)
                {
                    object? found = null;
                    foreach (var item in items)
                    {
                        if (found == null && item?.ToString() == currentItem?.ToString())
                        {
                            found = item;
                            continue;
                        }
                        if (found != null)
                        {
                            var subj = GetPropertyValue(item, "Subject");
                            if (subj != null)
                            {
                                var name = GetSubjectName(subj);
                                if (!string.IsNullOrEmpty(name)) return name;
                            }
                            var name3 = GetSubjectName(item);
                            if (!string.IsNullOrEmpty(name3)) return name3;
                            break;
                        }
                    }
                }
            }
        }

        // 4. 兜底：当前科目不为空时直接用它作为 next（至少不空）
        var currentName = GetSubjectName(currentSubject);
        if (!string.IsNullOrEmpty(currentName)) return currentName;

        return "";
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
