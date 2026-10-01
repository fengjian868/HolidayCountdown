using System;
using System.Collections.Generic;
using System.Linq;
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
    "A1B2C3D4-E5F6-7890-ABCD-EF1234567890",
    "小节日倒计时",
    "\uE34A",
    "显示距离非法定小节日的倒计时，如儿童节、母亲节、教师节等"
)]
public class MinorHolidayCountdownComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private StackPanel _main = null!;
    private HolidayService? _svc;

    static readonly List<(string Name, int Month, int Day, string Icon, bool IsFixed)> FixedHolidays = new()
    {
        ("情人节", 2, 14, "🌹", true),
        ("妇女节", 3, 8, "💐", true),
        ("植树节", 3, 12, "🌱", true),
        ("消费者权益日", 3, 15, "🛒", true),
        ("愚人节", 4, 1, "🤡", true),
        ("世界读书日", 4, 23, "📚", true),
        ("劳动节", 5, 1, "👔", true),
        ("青年节", 5, 4, "🎓", true),
        ("儿童节", 6, 1, "🎈", true),
        ("建党节", 7, 1, "🔴", true),
        ("建军节", 8, 1, "🎖️", true),
        ("教师节", 9, 10, "🎓", true),
        ("万圣节", 10, 31, "🎃", true),
        ("平安夜", 12, 24, "🎄", true),
        ("圣诞节", 12, 25, "🎅", true),
    };

    static readonly List<(string Name, int Month, DayOfWeek DOW, int N, string Icon)> NthHolidays = new()
    {
        ("母亲节", 5, DayOfWeek.Sunday, 2, "💝"),
        ("父亲节", 6, DayOfWeek.Sunday, 3, "👨"),
        ("感恩节", 11, DayOfWeek.Thursday, 4, "🙏"),
    };

    static List<(string Name, DateTime Date, string Icon)> GetBuiltInHolidays(int year)
    {
        var list = new List<(string, DateTime, string)>();
        foreach (var (name, month, day, icon, _) in FixedHolidays)
            list.Add((name, new DateTime(year, month, day), icon));
        foreach (var (name, month, dow, n, icon) in NthHolidays)
            list.Add((name, NthDayOfMonth(year, month, dow, n), icon));
        return list;
    }

    static DateTime NthDayOfMonth(int year, int month, DayOfWeek dayOfWeek, int n)
    {
        var first = new DateTime(year, month, 1);
        var daysToAdd = ((int)dayOfWeek - (int)first.DayOfWeek + 7) % 7;
        return first.AddDays(daysToAdd + (n - 1) * 7);
    }

    public MinorHolidayCountdownComponent()
    {
        _main = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        Content = _main;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
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
        _main.Children.Clear();
        if (_svc == null) return;

        var now = DateTime.Now;
        var count = _svc.Settings.MinorHolidayDisplayCount > 0 ? _svc.Settings.MinorHolidayDisplayCount : 3;
        var showIcon = _svc.Settings.MinorHolidayShowIcon;
        var showDays = _svc.Settings.MinorHolidayShowDays;
        var disabled = _svc.Settings.MinorHolidayDisabled ?? new List<string>();

        var all = new List<(string Name, DateTime Date, string Icon)>();

        for (int y = now.Year; y <= now.Year + 1; y++)
        {
            foreach (var (name, date, icon) in GetBuiltInHolidays(y))
            {
                if (date.Date >= now.Date && !disabled.Contains(name))
                    all.Add((name, date, icon));
            }
        }

        var next = all.OrderBy(x => x.Date).Take(count).ToList();
        if (next.Count == 0)
        {
            var tb = new TextBlock { Text = "暂无小节日", Opacity = 0.5, HorizontalAlignment = HorizontalAlignment.Center };
            tb[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
            _main.Children.Add(tb);
            return;
        }

        foreach (var (name, date, icon) in next)
        {
            var days = (int)(date.Date - now.Date).TotalDays;
            var item = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 3, VerticalAlignment = VerticalAlignment.Center };

            if (showIcon && !string.IsNullOrEmpty(icon))
                item.Children.Add(new TextBlock { Text = icon, FontSize = 12, VerticalAlignment = VerticalAlignment.Center });

            item.Children.Add(new TextBlock { Text = name, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center });
            item[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");

            if (showDays)
            {
                var daysText = days == 0 ? "今天" : $"{days}天";
                var daysTb = new TextBlock { Text = daysText, VerticalAlignment = VerticalAlignment.Center, Opacity = 0.8 };
                daysTb[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
                item.Children.Add(daysTb);
            }

            _main.Children.Add(item);
        }
    }
}
