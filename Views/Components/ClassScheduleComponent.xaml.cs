using System;
using System.Collections.Generic;
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
            // 优先从 MainViewModel 读取 UI 相关属性，其次 LessonsService
            var dataSource = GetMainViewModel() ?? GetLessonsService();
            if (dataSource == null)
            {
                _txt.Text = GetFallbackNoClassText();
                return;
            }

            var currentStateObj = GetPropertyValue(dataSource, "CurrentState");
            if (currentStateObj == null)
            {
                _txt.Text = GetFallbackNoClassText();
                return;
            }

            int state = currentStateObj is int i ? i : (int)currentStateObj;
            // TimeState: 0=None, 1=OnClass, 2=PrepareOnClass, 3=Breaking, 4=AfterSchool

            var currentSubject = GetPropertyValue(dataSource, "CurrentSubject");
            var nextSubject = GetPropertyValue(dataSource, "NextSubject");
            var onClassLeftTime = GetPropertyValue(dataSource, "OnClassLeftTime");
            var onBreakingTimeLeftTime = GetPropertyValue(dataSource, "OnBreakingTimeLeftTime");
            var isClassPlanLoaded = GetPropertyValue(dataSource, "IsClassPlanLoaded");
            var isClassPlanEnabled = GetPropertyValue(dataSource, "IsClassPlanEnabled");

            // 若数据源本身没有这些属性，尝试从 LessonsService 再读一次
            var lessons = GetLessonsService();
            if (lessons != null && !ReferenceEquals(dataSource, lessons))
            {
                if (currentSubject == null) currentSubject = GetPropertyValue(lessons, "CurrentSubject");
                if (nextSubject == null) nextSubject = GetPropertyValue(lessons, "NextSubject");
                if (onClassLeftTime == null) onClassLeftTime = GetPropertyValue(lessons, "OnClassLeftTime");
                if (onBreakingTimeLeftTime == null) onBreakingTimeLeftTime = GetPropertyValue(lessons, "OnBreakingTimeLeftTime");
                if (isClassPlanLoaded == null) isClassPlanLoaded = GetPropertyValue(lessons, "IsClassPlanLoaded");
                if (isClassPlanEnabled == null) isClassPlanEnabled = GetPropertyValue(lessons, "IsClassPlanEnabled");
            }

            if (isClassPlanEnabled is bool enabled && !enabled) { _txt.Text = GetFallbackNoClassText(); return; }
            if (isClassPlanLoaded is bool loaded && !loaded)
            {
                _txt.Text = GetFallbackNoClassText();
                return;
            }

            var leftTimeOnClass = onClassLeftTime as TimeSpan? ?? TimeSpan.Zero;
            var leftTimeBreaking = onBreakingTimeLeftTime as TimeSpan? ?? TimeSpan.Zero;
            var subjectName = _svc.Settings.ClassScheduleShowSubject ? GetSubjectName(currentSubject) : "";
            var nextName = GetNextSubjectName(dataSource!, currentSubject, nextSubject);

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
        catch { _txt.Text = GetFallbackNoClassText(); }
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
        return GetFallbackNoClassText();
    }

    string GetFallbackNoClassText()
    {
        var icon = _svc?.Settings.ClassScheduleShowIcon ?? true ? "📅 " : "";
        return $"{icon}暂无课程";
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

    string GetNextSubjectName(object dataSource, object? currentSubject, object? nextSubject)
    {
        // 1. 直接尝试 NextSubject.Name
        if (nextSubject != null)
        {
            var name = GetSubjectName(nextSubject);
            if (!string.IsNullOrEmpty(name)) return name;
        }

        // 2. 尝试 NextTimeLayoutItem.Subject.Name
        var nextItem = GetPropertyValue(dataSource, "NextTimeLayoutItem");
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
        var currentItem = GetPropertyValue(dataSource, "CurrentTimeLayoutItem");
        if (currentItem != null)
        {
            var timeLayout = GetPropertyValue(dataSource, "CurrentTimeLayout")
                          ?? GetPropertyValue(dataSource, "TimeLayout");
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
            // 1. 尝试接口 ILessonsService
            var type = FindType("ClassIsland.Core.Abstractions.Services.ILessonsService", "ClassIsland.Core")
                    ?? FindTypeByName("ILessonsService");
            var svc = type != null ? ResolveService(type) : null;
            if (svc != null) return svc;

            // 2. 尝试具体类 LessonsService
            type = FindTypeByName("LessonsService");
            svc = type != null ? ResolveService(type) : null;
            if (svc != null) return svc;
        }
        catch { }
        return null;
    }

    object? GetMainViewModel()
    {
        try
        {
            var type = FindType("ClassIsland.ViewModels.MainViewModel", "ClassIsland")
                    ?? FindTypeByName("MainViewModel");
            if (type == null) return null;
            return ResolveService(type);
        }
        catch { }
        return null;
    }

    Type? FindType(string fullName, string assemblyName)
    {
        try
        {
            return Type.GetType($"{fullName}, {assemblyName}")
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.FullName == fullName);
        }
        catch { return null; }
    }

    Type? FindTypeByName(string name)
    {
        try
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == name);
        }
        catch { return null; }
    }

    object? ResolveService(Type serviceType)
    {
        try
        {
            var appHostType = Type.GetType("ClassIsland.Shared.IAppHost, ClassIsland.Shared")
                ?? Type.GetType("ClassIsland.Shared.IAppHost, ClassIsland.Core")
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == "IAppHost");

            if (appHostType == null) return null;

            // 1. 尝试 IAppHost.TryGetService<T>()
            var tryGetService = appHostType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "TryGetService" && m.IsGenericMethodDefinition);
            if (tryGetService != null)
            {
                var genericMethod = tryGetService.MakeGenericMethod(serviceType);
                var result = genericMethod.Invoke(null, null);
                if (result != null) return result;
            }

            // 2. 回退到 IAppHost.Host.Services.GetService(type)
            var hostProp = appHostType.GetProperty("Host", BindingFlags.Public | BindingFlags.Static);
            var host = hostProp?.GetValue(null);
            if (host == null) return null;

            var servicesProp = host.GetType().GetProperty("Services", BindingFlags.Public | BindingFlags.Instance);
            var services = servicesProp?.GetValue(host);
            if (services == null) return null;

            var getService = services.GetType().GetMethod("GetService", new[] { typeof(Type) })
                ?? services.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == "GetService" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(Type));
            return getService?.Invoke(services, new object[] { serviceType });
        }
        catch { return null; }
    }

    object? GetPropertyValue(object? obj, string propName)
    {
        if (obj == null) return null;
        try
        {
            var prop = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            return prop?.GetValue(obj);
        }
        catch { return null; }
    }
}
