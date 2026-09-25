using System;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
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
    "0a94127d-25c6-4c36-a158-649334e4aad6",
    "节假日倒计时",
    "\uE34A",
    "显示距离最近节假日的倒计时，横向排列，带弧形进度环"
)]
public class HolidayCountdownComponent : ComponentBase
{
    private HolidayService _svc = null!;
    private DispatcherTimer _timer = null!;
    private StackPanel _main = null!;
    private int _lastLessonState = -1;
    private int _lastLessonIndex = -1;

    public HolidayCountdownComponent()
    {
        _main = new StackPanel { Orientation = Orientation.Vertical, Spacing = 0, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        Content = _main;
        Dispatcher.UIThread.Post(() =>
        {
            _svc = new HolidayService();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
            _timer.Tick += (s, e) => Update();
            _timer.Start();
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
        _main.Children.Clear();
        if (_svc == null) return;

        // 检查是否需要发送ci原生提醒
        CheckLessonReminder();

        var wr = _svc.GetNextWorkdayReminder();
        if (wr != null)
        {
            var rd = (int)(wr.Date.Date - DateTime.Now.Date).TotalDays;
            if (rd <= _svc.Settings.WorkdayReminderDays)
                _main.Children.Add(new TextBlock { Text = rd == 0 ? "⚠️ 明天调休上课" : $"⚠️ {rd}天后调休上课", Foreground = new SolidColorBrush(Colors.Orange), FontWeight = FontWeight.SemiBold, HorizontalAlignment = HorizontalAlignment.Center });
        }

        // 根据设置选择是否包含明年的节日
        var hs = _svc.Settings.ShowNextYearHolidays
            ? _svc.GetNextHolidaysWithNextYear(_svc.Settings.DisplayCount)
            : _svc.GetNextHolidays(_svc.Settings.DisplayCount);

        if (hs.Count > 0)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            for (int i = 0; i < hs.Count; i++)
            {
                var h = hs[i]; var days = (int)(h.Date.Date - DateTime.Now.Date).TotalDays;
                var color = _svc.Settings.AutoHolidayColor ? _svc.GetHolidayColor(h.Name) : Color.Parse("#2196F3");

                var item = new StackPanel { Orientation = Orientation.Vertical, Spacing = 0, VerticalAlignment = VerticalAlignment.Center };

                var firstRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
                if (_svc.Settings.ShowProgressRing && i == 0)
                {
                    var prev = _svc.GetPrevHoliday();
                    firstRow.Children.Add(CreateArc(days, prev, h, color));
                }
                else firstRow.Children.Add(new TextBlock { Text = h.IsCustom ? "🎂" : "📅", VerticalAlignment = VerticalAlignment.Center, FontSize = 12 });

                var nameDaysRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 3, VerticalAlignment = VerticalAlignment.Center };
                // 明年节日显示年份标注
                var displayName = h.Date.Year > DateTime.Now.Year ? $"{h.Date.Year}年{h.Name}" : h.Name;
                nameDaysRow.Children.Add(new TextBlock { Text = displayName, Foreground = new SolidColorBrush(color), FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center });
                var daysText = days == 0 ? "今天" : $"还有{days}天";
                var daysTb = new TextBlock { Text = daysText, VerticalAlignment = VerticalAlignment.Center, Opacity = 0.8 };
                daysTb[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
                nameDaysRow.Children.Add(daysTb);
                firstRow.Children.Add(nameDaysRow);
                item.Children.Add(firstRow);

                if (i == 0 && h.DaysOff > 1)
                {
                    var daysOffTb = new TextBlock { Text = $"放假{h.DaysOff}天", HorizontalAlignment = HorizontalAlignment.Left, FontSize = 10, Opacity = 0.5, Margin = new Thickness(36, 0, 0, 0) };
                    daysOffTb[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
                    item.Children.Add(daysOffTb);
                }

                row.Children.Add(item);
            }

            _main.Children.Add(row);
        }
        else
        {
            var emptyTb = new TextBlock { Text = "暂无节假日", HorizontalAlignment = HorizontalAlignment.Center, Opacity = 0.5 };
            emptyTb[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
            _main.Children.Add(emptyTb);
        }
    }

    /// <summary>
    /// 检查是否在第N节课下课时发送提醒
    /// </summary>
    void CheckLessonReminder()
    {
        var lessonNum = _svc.Settings.HolidayReminderLessonNumber;
        if (lessonNum <= 0) return;

        try
        {
            var lessonsService = GetLessonsService();
            if (lessonsService == null) return;

            var state = GetPropertyValue(lessonsService, "CurrentState") as int? ?? 0;
            var lessonIndex = GetPropertyValue(lessonsService, "CurrentLessonIndex") as int? ?? -1;

            // 从上课(1)变为课间(2)且是第N节课时发送提醒
            if (_lastLessonState == 1 && state == 2 && _lastLessonIndex == lessonNum - 1)
            {
                var hs = _svc.GetNextHolidays(1);
                if (hs.Count > 0)
                {
                    var h = hs[0];
                    var days = (int)(h.Date.Date - DateTime.Now.Date).TotalDays;
                    var msg = days == 0 ? $"今天就是{h.Name}！" : $"距离{h.Name}还有{days}天";
                    SendNotification(msg);
                }
            }

            _lastLessonState = state;
            _lastLessonIndex = lessonIndex;
        }
        catch { }
    }

    void SendNotification(string message)
    {
        try
        {
            var appHostType = Type.GetType("ClassIsland.Shared.IAppHost, ClassIsland.Shared")
                ?? Type.GetType("ClassIsland.Shared.IAppHost, ClassIsland.Core")
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == "IAppHost");
            if (appHostType == null) return;

            var tryGetService = appHostType.GetMethod("TryGetService", BindingFlags.Public | BindingFlags.Static);
            if (tryGetService == null || !tryGetService.IsGenericMethodDefinition) return;

            var notifType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == "INotificationService" || t.Name == "NotificationService");
            if (notifType == null) return;

            var genericMethod = tryGetService.MakeGenericMethod(notifType);
            var notifService = genericMethod.Invoke(null, null);
            if (notifService == null) return;

            // 尝试调用 Notify 方法
            var notifyMethod = notifType.GetMethod("Notify", BindingFlags.Public | BindingFlags.Instance);
            if (notifyMethod != null)
                notifyMethod.Invoke(notifService, new object[] { message });
        }
        catch { }
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

    Control CreateArc(int days, Holiday? prev, Holiday next, Color color)
    {
        var container = new Border
        {
            Width = 32,
            Height = 32,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Background = Brushes.Transparent
        };

        var inner = new Grid { Width = 28, Height = 28, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        inner.Children.Add(new Arc { Width = 28, Height = 28, StartAngle = -90, SweepAngle = 360, Stroke = new SolidColorBrush(Color.Parse("#20FFFFFF")), StrokeThickness = 2.5, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center });
        double p = 0;
        if (prev != null) { var t = (next.Date - prev.Date).TotalDays; var pass = (DateTime.Now - prev.Date).TotalDays; p = Math.Max(0, Math.Min(1, pass / t)); }
        else p = Math.Max(0, Math.Min(1, 1 - days / 30.0));
        inner.Children.Add(new Arc { Width = 28, Height = 28, StartAngle = -90, SweepAngle = p * 360, Stroke = new SolidColorBrush(color), StrokeThickness = 2.5, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center });
        inner.Children.Add(new TextBlock { Text = next.Date.Day.ToString(), FontSize = 9, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(color), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center });
        container.Child = inner;
        return container;
    }
}
