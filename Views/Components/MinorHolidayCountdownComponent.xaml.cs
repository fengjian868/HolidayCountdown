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

    static List<MinorHoliday> GetBuiltInHolidays(int year)
    {
        var list = new List<MinorHoliday>
        {
            new("情人节", new DateTime(year, 2, 14), "🌹"),
            new("妇女节", new DateTime(year, 3, 8), "💐"),
            new("植树节", new DateTime(year, 3, 12), "🌱"),
            new("消费者权益日", new DateTime(year, 3, 15), "🛒"),
            new("愚人节", new DateTime(year, 4, 1), "🤡"),
            new("世界读书日", new DateTime(year, 4, 23), "📚"),
            new("劳动节", new DateTime(year, 5, 1), " workers"),
            new("青年节", new DateTime(year, 5, 4), " youth"),
            new("母亲节", NthDayOfMonth(year, 5, DayOfWeek.Sunday, 2), "💝"),
            new("儿童节", new DateTime(year, 6, 1), "🎈"),
            new("父亲节", NthDayOfMonth(year, 6, DayOfWeek.Sunday, 3), "👨"),
            new("建党节", new DateTime(year, 7, 1), "🔴"),
            new("建军节", new DateTime(year, 8, 1), "🎖️"),
            new("教师节", new DateTime(year, 9, 10), "🎓"),
            new("万圣节", new DateTime(year, 10, 31), "🎃"),
            new("感恩节", NthDayOfMonth(year, 11, DayOfWeek.Thursday, 4), "🙏"),
            new("平安夜", new DateTime(year, 12, 24), "🎄"),
            new("圣诞节", new DateTime(year, 12, 25), "🎅")
        };
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
        var now = DateTime.Now;
        var count = _svc?.Settings.DisplayCount ?? 3;
        var all = new List<(MinorHoliday h, DateTime date)>();

        for (int y = now.Year; y <= now.Year + 1; y++)
        {
            foreach (var h in GetBuiltInHolidays(y))
            {
                if (h.Date.Date >= now.Date)
                    all.Add((h, h.Date));
            }
        }

        var next = all.OrderBy(x => x.date).Take(count).ToList();
        if (next.Count == 0)
        {
            var tb = new TextBlock { Text = "暂无小节日", Opacity = 0.5, HorizontalAlignment = HorizontalAlignment.Center };
            tb[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
            _main.Children.Add(tb);
            return;
        }

        foreach (var (h, date) in next)
        {
            var days = (int)(date.Date - now.Date).TotalDays;
            var item = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 3, VerticalAlignment = VerticalAlignment.Center };

            if (!string.IsNullOrEmpty(h.Icon))
                item.Children.Add(new TextBlock { Text = h.Icon, FontSize = 12, VerticalAlignment = VerticalAlignment.Center });

            item.Children.Add(new TextBlock { Text = h.Name, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center });

            var daysText = days == 0 ? "今天" : $"{days}天";
            var daysTb = new TextBlock { Text = daysText, VerticalAlignment = VerticalAlignment.Center, Opacity = 0.8 };
            daysTb[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
            item.Children.Add(daysTb);

            _main.Children.Add(item);
        }
    }

    record MinorHoliday(string Name, DateTime Date, string Icon);
}
