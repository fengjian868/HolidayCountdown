using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
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
        // 绑定主题前景色，确保在明暗主题下都可见
        _txt[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
        panel.Children.Add(_txt);
        Content = panel;

        // 同步初始化服务，避免首次 tick 时空白
        _svc = new HolidayService();
        HolidayService.SettingsChanged += OnSettingsChanged;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (s, e) => Update();
        _timer.Start();
        Update();
    }

    void OnSettingsChanged()
    {
        _svc?.LoadSettings();
        Dispatcher.UIThread.Post(Update);
    }

    void Update()
    {
        if (_svc == null) { _txt.Text = "加载中…"; return; }
        if (!_svc.Settings.ClassScheduleEnabled) { _txt.Text = "课表联动已禁用"; _txt[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush"); return; }

        try
        {
            // 优先从 MainViewModel 读取 UI 相关属性，其次 LessonsService
            var dataSource = GetMainViewModel() ?? GetLessonsService();
            if (dataSource == null)
            {
                _txt.Text = GetFallbackNoClassText();
                _txt[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
                return;
            }

            var currentStateObj = GetPropertyValue(dataSource, "CurrentState");
            if (currentStateObj == null)
            {
                _txt.Text = GetFallbackNoClassText();
                _txt[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
                return;
            }

            // 处理 TimeState 枚举（可空枚举需先解包）
            int state;
            var stateType = currentStateObj.GetType();
            if (currentStateObj is int i) state = i;
            else if (Nullable.GetUnderlyingType(stateType) != null)
                state = (int)Convert.ChangeType(currentStateObj, Nullable.GetUnderlyingType(stateType)!);
            else
                state = (int)currentStateObj;
            // TimeState: 0=None, 1=OnClass, 2=PrepareOnClass, 3=Breaking, 4=AfterSchool

            var currentSubject = GetPropertyValue(dataSource, "CurrentSubject");
            var nextSubject = GetPropertyValue(dataSource, "NextSubject");
            var isClassPlanLoaded = GetPropertyValue(dataSource, "IsClassPlanLoaded");
            var isClassPlanEnabled = GetPropertyValue(dataSource, "IsClassPlanEnabled");

            // 优先用 LessonsService 读取核心课表数据（MainViewModel 只是 UI 同步，可能缺字段/不同步）
            var lessons = GetLessonsService();
            if (lessons != null && !ReferenceEquals(dataSource, lessons))
            {
                currentSubject = GetPropertyValue(lessons, "CurrentSubject");
                nextSubject = GetPropertyValue(lessons, "NextSubject");
                isClassPlanLoaded = GetPropertyValue(lessons, "IsClassPlanLoaded");
                isClassPlanEnabled = GetPropertyValue(lessons, "IsClassPlanEnabled");
            }

            // 读取倒计时：尝试多种属性名，优先 LessonsService，其次 MainViewModel
            var onClassLeftTime = ReadTimeSpanMulti(lessons, "OnClassLeftTime", "OnClassTimeLeft", "CurrentLessonLeftTime")
                               ?? ReadTimeSpanMulti(dataSource, "OnClassLeftTime", "OnClassTimeLeft", "CurrentLessonLeftTime");
            var onBreakingTimeLeftTime = ReadTimeSpanMulti(lessons, "OnBreakingTimeLeftTime", "OnBreakingTimeLeft", "BreakTimeLeft")
                                      ?? ReadTimeSpanMulti(dataSource, "OnBreakingTimeLeftTime", "OnBreakingTimeLeft", "BreakTimeLeft");

            if (isClassPlanEnabled is bool enabled && !enabled) { _txt.Text = GetFallbackNoClassText(); _txt[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush"); return; }
            if (isClassPlanLoaded is bool loaded && !loaded)
            {
                _txt.Text = GetFallbackNoClassText();
                _txt[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
                return;
            }

            var leftTimeOnClass = onClassLeftTime ?? TimeSpan.Zero;
            var leftTimeBreaking = onBreakingTimeLeftTime ?? TimeSpan.Zero;
            var subjectName = _svc.Settings.ClassScheduleShowSubject ? GetSubjectName(currentSubject) : "";
            var nextName = GetNextSubjectName(dataSource!, currentSubject, nextSubject);

            string template;
            string stateText;
            string countdownText;
            string text = "";
            bool warning = false;

            // 各学科图标
            string curIcon = _svc.Settings.ClassScheduleShowIcon ? "📖" : "";
            string breakIcon = _svc.Settings.ClassScheduleShowIcon ? "☕" : "";
            string prepIcon = _svc.Settings.ClassScheduleShowIcon ? "🔔" : "";
            string afterIcon = _svc.Settings.ClassScheduleShowIcon ? "🏠" : "";
            string noClassIcon = _svc.Settings.ClassScheduleShowIcon ? "📅" : "";
            string nextIcon = _svc.Settings.ClassScheduleShowIcon ? "📚" : "";

            switch (state)
            {
                case 1: // OnClass
                    stateText = "上课中";
                    countdownText = leftTimeOnClass.TotalSeconds > 0 ? FormatTime(leftTimeOnClass) : "";
                    template = _svc.Settings.ClassScheduleOnClassTemplate;
                    break;
                case 3: // Breaking
                    stateText = "课间";
                    var breakLeft = leftTimeBreaking.TotalSeconds > 0 ? leftTimeBreaking : leftTimeOnClass;
                    countdownText = breakLeft.TotalSeconds > 0 ? FormatTime(breakLeft) : "";
                    // 当课间剩余时间 <= 警示分钟数时切换为准备上课模板
                    if (breakLeft.TotalSeconds > 0 && breakLeft.TotalMinutes <= _svc.Settings.BreakWarningMinutes)
                    {
                        template = _svc.Settings.ClassSchedulePrepareTemplate;
                        stateText = "准备上课";
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
                    countdownText = "";
                    template = _svc.Settings.ClassScheduleAfterSchoolTemplate;
                    break;
                case 2: // PrepareOnClass
                    stateText = "准备上课";
                    countdownText = leftTimeOnClass.TotalSeconds > 0 ? FormatTime(leftTimeOnClass) : "";
                    template = _svc.Settings.ClassSchedulePrepareTemplate;
                    break;
                default: // None
                    stateText = "暂无课程";
                    countdownText = leftTimeOnClass.TotalSeconds > 0 ? FormatTime(leftTimeOnClass) : "";
                    text = GetNoClassText();
                    template = _svc.Settings.ClassScheduleNoClassTemplate;
                    break;
            }

            if (string.IsNullOrWhiteSpace(template))
                template = "{curIcon}{curSubject} {curRemain} → {nextIcon}{nextSubject}";

            var result = template
                // 新变量名
                .Replace("{curIcon}", string.IsNullOrEmpty(curIcon) ? "" : $"{curIcon} ")
                .Replace("{curSubject}", subjectName)
                .Replace("{curRemain}", countdownText)
                .Replace("{breakIcon}", string.IsNullOrEmpty(breakIcon) ? "" : $"{breakIcon} ")
                .Replace("{breakRemain}", countdownText)
                .Replace("{prepIcon}", string.IsNullOrEmpty(prepIcon) ? "" : $"{prepIcon} ")
                .Replace("{prepRemain}", countdownText)
                .Replace("{nextIcon}", string.IsNullOrEmpty(nextIcon) ? "" : $"{nextIcon} ")
                .Replace("{nextSubject}", nextName)
                .Replace("{afterIcon}", string.IsNullOrEmpty(afterIcon) ? "" : $"{afterIcon} ")
                .Replace("{noClassIcon}", string.IsNullOrEmpty(noClassIcon) ? "" : $"{noClassIcon} ")
                .Replace("{text}", text)
                .Replace("{state}", stateText)
                // 旧变量名兼容
                .Replace("{icon}", string.IsNullOrEmpty(curIcon) ? "" : $"{curIcon} ")
                .Replace("{subject}", subjectName)
                .Replace("{countdown}", countdownText)
                .Replace("{next}", nextName);

            result = Regex.Replace(result, @"\s+", " ").Trim();

            if (string.IsNullOrWhiteSpace(result))
                result = GetFallbackNoClassText();

            _txt.Text = result;
            if (warning && Color.TryParse(_svc.Settings.BreakWarningColor, out var warnColor))
            {
                _txt.Foreground = new SolidColorBrush(warnColor);
            }
            else
            {
                // 恢复主题前景色绑定
                _txt[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
            }
        }
        catch
        {
            _txt.Text = GetFallbackNoClassText();
            _txt[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
        }
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
        var lessons = GetLessonsService();

        // 1. 优先读取 LessonsService.NextClassSubject（真正的下一节课科目）
        var nextClassSubject = GetPropertyValue(lessons, "NextClassSubject");
        if (nextClassSubject != null)
        {
            var name = GetSubjectName(nextClassSubject);
            if (!string.IsNullOrEmpty(name) && name != GetSubjectName(currentSubject))
                return name;
        }

        // 2. 尝试 LessonsService.NextClassTimeLayoutItem.Subject
        var nextClassItem = GetPropertyValue(lessons, "NextClassTimeLayoutItem");
        if (nextClassItem != null && IsLessonTimeLayoutItem(nextClassItem))
        {
            var subj = GetPropertyValue(nextClassItem, "Subject");
            if (subj != null)
            {
                var name = GetSubjectName(subj);
                if (!string.IsNullOrEmpty(name) && name != GetSubjectName(currentSubject))
                    return name;
            }
        }

        // 3. 兼容：dataSource 的 NextClassSubject / NextClassTimeLayoutItem
        nextClassSubject = GetPropertyValue(dataSource, "NextClassSubject");
        if (nextClassSubject != null)
        {
            var name = GetSubjectName(nextClassSubject);
            if (!string.IsNullOrEmpty(name) && name != GetSubjectName(currentSubject))
                return name;
        }
        nextClassItem = GetPropertyValue(dataSource, "NextClassTimeLayoutItem");
        if (nextClassItem != null && IsLessonTimeLayoutItem(nextClassItem))
        {
            var subj = GetPropertyValue(nextClassItem, "Subject");
            if (subj != null)
            {
                var name = GetSubjectName(subj);
                if (!string.IsNullOrEmpty(name) && name != GetSubjectName(currentSubject))
                    return name;
            }
        }

        // 4. 兼容：NextSubject（但排除和当前科目同名的）
        if (nextSubject != null)
        {
            var name = GetSubjectName(nextSubject);
            if (!string.IsNullOrEmpty(name) && name != GetSubjectName(currentSubject))
                return name;
        }

        // 5. 兜底：从时间布局中向后找第一个不同于当前科目的上课项
        var foundName = FindNextFromTimeLayout(lessons, dataSource, currentSubject);
        if (!string.IsNullOrEmpty(foundName))
            return foundName;

        // 6. 没有下节课
        return "已无课程";
    }

    string? FindNextFromTimeLayout(object? lessons, object dataSource, object? currentSubject)
    {
        var currentItem = GetPropertyValue(lessons, "CurrentTimeLayoutItem")
                       ?? GetPropertyValue(dataSource, "CurrentTimeLayoutItem");
        if (currentItem == null) return null;

        var timeLayout = GetPropertyValue(lessons, "CurrentTimeLayout")
                      ?? GetPropertyValue(dataSource, "CurrentTimeLayout")
                      ?? GetPropertyValue(lessons, "TimeLayout")
                      ?? GetPropertyValue(dataSource, "TimeLayout");
        if (timeLayout == null) return null;

        var itemsProp = timeLayout.GetType().GetProperty("Items", BindingFlags.Public | BindingFlags.Instance)
                      ?? timeLayout.GetType().GetProperty("LayoutItems", BindingFlags.Public | BindingFlags.Instance)
                      ?? timeLayout.GetType().GetProperty("Layouts", BindingFlags.Public | BindingFlags.Instance);
        if (itemsProp?.GetValue(timeLayout) is not System.Collections.IEnumerable items) return null;

        // 先找到当前项在列表中的索引
        var itemList = items.Cast<object>().ToList();
        int currentIdx = -1;

        // 方式1：通过 StartSecond 匹配（更可靠）
        var currentStart = GetPropertyValue(currentItem, "StartSecond");
        for (int i = 0; i < itemList.Count; i++)
        {
            var itemStart = GetPropertyValue(itemList[i], "StartSecond");
            if (itemStart != null && itemStart.Equals(currentStart))
            {
                currentIdx = i;
                break;
            }
        }

        // 方式2：回退到引用比较
        if (currentIdx < 0)
        {
            for (int i = 0; i < itemList.Count; i++)
            {
                if (ReferenceEquals(itemList[i], currentItem))
                {
                    currentIdx = i;
                    break;
                }
            }
        }

        if (currentIdx < 0) return null;

        // 从当前项之后找第一个上课项（且不是当前科目）
        var currentName = GetSubjectName(currentSubject);
        for (int i = currentIdx + 1; i < itemList.Count; i++)
        {
            var item = itemList[i];
            if (!IsLessonTimeLayoutItem(item))
                continue; // 跳过课间、午休等非课程项
            var subj = GetPropertyValue(item, "Subject");
            var name = subj != null ? GetSubjectName(subj) : GetSubjectName(item);
            if (!string.IsNullOrEmpty(name) && name != currentName)
                return name;
        }

        return null;
    }

    bool IsLessonTimeLayoutItem(object? item)
    {
        if (item == null) return false;
        // ClassIsland TimeType: 0=上课, 1=课间
        var typeValue = GetPropertyValue(item, "TimeType");
        if (typeValue == null)
        {
            // 没有 TimeType 时尝试按名称判断，排除明显是课间休息的项
            var name = GetSubjectName(item);
            if (!string.IsNullOrEmpty(name) && name.Contains("休息")) return false;
            return !string.IsNullOrEmpty(name);
        }
        var typeInt = typeValue is int i ? i : (int)Convert.ChangeType(typeValue, typeof(int));
        return typeInt == 0;
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

    TimeSpan? ReadTimeSpan(object? source, string propName)
    {
        if (source == null) return null;
        var value = GetPropertyValue(source, propName);
        if (value == null) return null;
        if (value is TimeSpan ts) return ts;
        try
        {
            return TimeSpan.Parse(value.ToString()!);
        }
        catch { return null; }
    }

    TimeSpan? ReadTimeSpanMulti(object? source, params string[] propNames)
    {
        if (source == null) return null;
        foreach (var name in propNames)
        {
            var result = ReadTimeSpan(source, name);
            if (result.HasValue && result.Value.TotalSeconds > 0)
                return result;
        }
        return null;
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
