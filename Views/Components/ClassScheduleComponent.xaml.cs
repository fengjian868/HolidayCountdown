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
            var nextSubject = GetPropertyValue(dataSource, "NextClassSubject") ?? GetPropertyValue(dataSource, "NextSubject");
            var isClassPlanLoaded = GetPropertyValue(dataSource, "IsClassPlanLoaded");
            var isClassPlanEnabled = GetPropertyValue(dataSource, "IsClassPlanEnabled");

            // 优先用 LessonsService 读取核心课表数据（MainViewModel 只是 UI 同步，可能缺字段/不同步）
            var lessons = GetLessonsService();
            if (lessons != null && !ReferenceEquals(dataSource, lessons))
            {
                currentSubject = GetPropertyValue(lessons, "CurrentSubject");
                nextSubject = GetPropertyValue(lessons, "NextClassSubject") ?? GetPropertyValue(lessons, "NextSubject");
                isClassPlanLoaded = GetPropertyValue(lessons, "IsClassPlanLoaded");
                isClassPlanEnabled = GetPropertyValue(lessons, "IsClassPlanEnabled");
            }

            // 读取倒计时：尝试多种属性名，优先 LessonsService，其次 MainViewModel
            var onClassLeftTime = ReadTimeSpanMulti(lessons, "OnClassLeftTime", "OnClassTimeLeft", "CurrentLessonLeftTime", "LeftTimeOnClass", "CurrentTimeLeft", "TimeLeft")
                               ?? ReadTimeSpanMulti(dataSource, "OnClassLeftTime", "OnClassTimeLeft", "CurrentLessonLeftTime", "LeftTimeOnClass", "CurrentTimeLeft", "TimeLeft");
            var onBreakingTimeLeftTime = ReadTimeSpanMulti(lessons, "OnBreakingTimeLeftTime", "OnBreakingTimeLeft", "BreakTimeLeft", "BreakingTimeLeft", "BreakLeftTime")
                                      ?? ReadTimeSpanMulti(dataSource, "OnBreakingTimeLeftTime", "OnBreakingTimeLeft", "BreakTimeLeft", "BreakingTimeLeft", "BreakLeftTime");

            if (isClassPlanEnabled is bool enabled && !enabled) { _txt.Text = GetFallbackNoClassText(); _txt[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush"); return; }
            if (isClassPlanLoaded is bool loaded && !loaded)
            {
                _txt.Text = GetFallbackNoClassText();
                _txt[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
                return;
            }

            var leftTimeOnClass = onClassLeftTime ?? TimeSpan.Zero;
            var leftTimeBreaking = onBreakingTimeLeftTime ?? TimeSpan.Zero;

            // 如果倒计时为0，尝试从时间布局项的结束时间推算
            // ClassIsland 语义：
            //   state==1 (OnClass):       估算的是"距下课"，应填入 leftTimeBreaking
            //   state==2 (PrepareOnClass):估算的是"距上课"，应填入 leftTimeOnClass (预留分支,实际不会进入)
            //   state==3 (Breaking):     估算的是"距课间结束"，数值上等于"距下节课开始"，应填入 leftTimeBreaking
            if (leftTimeOnClass.TotalSeconds <= 0 && leftTimeBreaking.TotalSeconds <= 0)
            {
                var estimatedLeft = EstimateTimeLeftFromLayout(dataSource, lessons);
                if (estimatedLeft.HasValue && estimatedLeft.Value.TotalSeconds > 0)
                {
                    if (state == 1)
                        leftTimeBreaking = estimatedLeft.Value;
                    else if (state == 2)
                        leftTimeOnClass = estimatedLeft.Value;
                    else if (state == 3)
                        leftTimeBreaking = estimatedLeft.Value;
                }
            }
            var subjectName = _svc.Settings.ClassScheduleShowSubject ? GetSubjectName(currentSubject) : "";

            // 下节课：完全从时间布局遍历获取，不依赖CL的NextSubject
            var nextName = GetNextSubjectFromTimeLayout(dataSource, lessons, state);

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
                    // ClassIsland 在 OnClass 状态下：OnClassLeftTime=0, OnBreakingTimeLeftTime=距下课
                    stateText = "上课中";
                    countdownText = leftTimeBreaking.TotalSeconds > 0 ? FormatTime(leftTimeBreaking) : "";
                    template = _svc.Settings.ClassScheduleOnClassTemplate;
                    break;
                case 3: // Breaking
                    stateText = "课间";
                    // ClassIsland 在非 OnClass 状态下：OnClassLeftTime=距下节课开始, OnBreakingTimeLeftTime=0
                    // 课间"距下课"数值上等于"距下节课开始"
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
                case 2: // PrepareOnClass (预留,ClassIsland 不会主动设置)
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
                template = "{A}{B} {C} → {D}{E}";

            var result = template
                // 新短变量名
                .Replace("{A}", string.IsNullOrEmpty(curIcon) ? "" : $"{curIcon} ")
                .Replace("{B}", subjectName)
                .Replace("{C}", countdownText)
                .Replace("{D}", string.IsNullOrEmpty(nextIcon) ? "" : $"{nextIcon} ")
                .Replace("{E}", nextName)
                .Replace("{F}", string.IsNullOrEmpty(breakIcon) ? "" : $"{breakIcon} ")
                .Replace("{G}", countdownText)
                .Replace("{H}", string.IsNullOrEmpty(prepIcon) ? "" : $"{prepIcon} ")
                .Replace("{I}", countdownText)
                .Replace("{J}", string.IsNullOrEmpty(afterIcon) ? "" : $"{afterIcon} ")
                .Replace("{K}", string.IsNullOrEmpty(noClassIcon) ? "" : $"{noClassIcon} ")
                .Replace("{L}", stateText)
                .Replace("{M}", text)
                // 旧长变量名兼容
                .Replace("{curIcon}", string.IsNullOrEmpty(curIcon) ? "" : $"{curIcon} ")
                .Replace("{curSubject}", subjectName)
                .Replace("{curRemain}", countdownText)
                .Replace("{nextIcon}", string.IsNullOrEmpty(nextIcon) ? "" : $"{nextIcon} ")
                .Replace("{nextSubject}", nextName)
                .Replace("{breakIcon}", string.IsNullOrEmpty(breakIcon) ? "" : $"{breakIcon} ")
                .Replace("{breakRemain}", countdownText)
                .Replace("{prepIcon}", string.IsNullOrEmpty(prepIcon) ? "" : $"{prepIcon} ")
                .Replace("{prepRemain}", countdownText)
                .Replace("{afterIcon}", string.IsNullOrEmpty(afterIcon) ? "" : $"{afterIcon} ")
                .Replace("{noClassIcon}", string.IsNullOrEmpty(noClassIcon) ? "" : $"{noClassIcon} ")
                .Replace("{text}", text)
                .Replace("{state}", stateText)
                // 旧短别名兼容
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
            // ClassIsland 的 ISubjectInfo 可能有多个名称属性，依次尝试
            var type = subject.GetType();
            foreach (var propName in new[] { "Name", "MainWindowName", "SimpleName", "SubjectName", "DisplayName", "Title" })
            {
                var prop = type.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                if (prop != null)
                {
                    var v = prop.GetValue(subject)?.ToString();
                    if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
                }
            }
            return "";
        }
        catch { return ""; }
    }

    /// <summary>
    /// 判断科目名是否有意义：非空，且不是 ClassIsland 的占位科目（"???"、"课间休息"等）
    /// </summary>
    bool IsMeaningfulSubjectName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var n = name.Trim();
        // ClassIsland Subject.Fallback.Name = "???"，Subject.Breaking.Name = "课间休息"
        if (n == "???" || n == "课间休息" || n == "?") return false;
        return true;
    }

    string GetNextSubjectFromTimeLayout(object dataSource, object? lessons, int currentState)
    {
        try
        {
            var source = lessons ?? dataSource;

            // 优先尝试直接读取 NextClassSubject / NextClassTimeLayoutItem
            // 注意：ClassIsland 的真实属性名是 NextClassSubject / NextClassTimeLayoutItem（带 Class 前缀）
            // MainViewModel（dataSource）是 UI 绑定源，最可靠；LessonsService 作为 fallback
            // 同时兼容旧版/别名的 NextSubject / NextTimeLayoutItem
            var nextSubject = GetPropertyValue(dataSource, "NextClassSubject")
                           ?? GetPropertyValue(source, "NextClassSubject")
                           ?? GetPropertyValue(dataSource, "NextSubject")
                           ?? GetPropertyValue(source, "NextSubject");
            if (nextSubject != null)
            {
                var name = GetSubjectName(nextSubject);
                if (IsMeaningfulSubjectName(name)) return name;
            }
            var nextItem = GetPropertyValue(dataSource, "NextClassTimeLayoutItem")
                        ?? GetPropertyValue(source, "NextClassTimeLayoutItem")
                        ?? GetPropertyValue(dataSource, "NextTimeLayoutItem")
                        ?? GetPropertyValue(source, "NextTimeLayoutItem");
            if (nextItem != null)
            {
                var name = GetSubjectNameFromItem(nextItem);
                if (IsMeaningfulSubjectName(name)) return name;
            }

            var timeLayout = GetPropertyValue(source, "CurrentTimeLayout")
                          ?? GetPropertyValue(dataSource, "CurrentTimeLayout")
                          ?? GetPropertyValue(source, "TimeLayout")
                          ?? GetPropertyValue(dataSource, "TimeLayout");
            if (timeLayout == null) return "";

            var itemsProp = timeLayout.GetType().GetProperty("Items", BindingFlags.Public | BindingFlags.Instance)
                          ?? timeLayout.GetType().GetProperty("LayoutItems", BindingFlags.Public | BindingFlags.Instance)
                          ?? timeLayout.GetType().GetProperty("Layouts", BindingFlags.Public | BindingFlags.Instance);
            if (itemsProp?.GetValue(timeLayout) is not System.Collections.IEnumerable items) return "";

            var itemList = items.Cast<object>().ToList();
            if (itemList.Count == 0) return "";

            // 找到当前时间布局项的索引
            var currentItem = GetPropertyValue(source, "CurrentTimeLayoutItem")
                           ?? GetPropertyValue(dataSource, "CurrentTimeLayoutItem");

            int currentIdx = -1;
            if (currentItem != null)
            {
                // 方式1：通过引用比较
                for (int i = 0; i < itemList.Count; i++)
                {
                    if (ReferenceEquals(itemList[i], currentItem))
                    {
                        currentIdx = i;
                        break;
                    }
                }

                // 方式2：通过 StartSecond 匹配
                if (currentIdx < 0)
                {
                    var currentStart = GetPropertyValue(currentItem, "StartSecond");
                    if (currentStart != null)
                    {
                        for (int i = 0; i < itemList.Count; i++)
                        {
                            var itemStart = GetPropertyValue(itemList[i], "StartSecond");
                            if (itemStart != null && itemStart.Equals(currentStart))
                            {
                                currentIdx = i;
                                break;
                            }
                        }
                    }
                }
            }

            // 如果找不到当前项，用当前时间推算
            if (currentIdx < 0)
            {
                var now = DateTime.Now;
                var nowSeconds = now.Hour * 3600 + now.Minute * 60 + now.Second;
                for (int i = 0; i < itemList.Count; i++)
                {
                    var start = GetPropertyValue(itemList[i], "StartSecond");
                    var end = GetPropertyValue(itemList[i], "EndSecond");
                    if (start != null && end != null)
                    {
                        var s = Convert.ToInt64(start);
                        var e = Convert.ToInt64(end);
                        if (nowSeconds >= s && nowSeconds < e)
                        {
                            currentIdx = i;
                            break;
                        }
                    }
                }
            }

            if (currentIdx < 0) return "";

            // 从当前项之后找第一个上课类型的项（跳过课间/休息）
            for (int i = currentIdx + 1; i < itemList.Count; i++)
            {
                var item = itemList[i];
                if (!IsLessonTimeLayoutItem(item))
                    continue;
                var name = GetSubjectNameFromItem(item);
                if (IsMeaningfulSubjectName(name))
                    return name;
            }

            return "";
        }
        catch
        {
            return "";
        }
    }

    string GetSubjectNameFromItem(object item)
    {
        // 优先读取 Subject 属性
        var subject = GetPropertyValue(item, "Subject");
        if (subject != null)
        {
            var name = GetSubjectName(subject);
            if (!string.IsNullOrEmpty(name)) return name;
        }
        // 其次直接读取 Name
        return GetSubjectName(item);
    }

    TimeSpan? EstimateTimeLeftFromLayout(object dataSource, object? lessons)
    {
        try
        {
            var source = lessons ?? dataSource;
            var currentItem = GetPropertyValue(source, "CurrentTimeLayoutItem")
                           ?? GetPropertyValue(dataSource, "CurrentTimeLayoutItem");
            if (currentItem == null) return null;

            // 优先读 EndTime (TimeSpan) — ClassIsland 推荐字段
            var endTime = GetPropertyValue(currentItem, "EndTime");
            if (endTime is TimeSpan ts)
            {
                var now = DateTime.Now.TimeOfDay;
                var diff = ts - now;
                if (diff.TotalSeconds > 0) return diff;
                return null;
            }

            // 兜底读 EndSecond (int/秒数,ClassIsland 已标 [Obsolete])
            var endSecond = GetPropertyValue(currentItem, "EndSecond");
            if (endSecond == null) return null;
            var endSec = Convert.ToInt64(endSecond);
            var now2 = DateTime.Now;
            var nowSec = now2.Hour * 3600 + now2.Minute * 60 + now2.Second;
            var diff2 = endSec - nowSec;
            if (diff2 > 0) return TimeSpan.FromSeconds(diff2);
            return null;
        }
        catch { return null; }
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
        // ClassIsland 的 OnClassLeftTime / OnBreakingTimeLeftTime 是 TimeSpan 类型
        if (value is TimeSpan ts) return ts;
        // 兜底: 解析字符串(用于反射读老式 EndSecond 字段等)
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
