using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.Components;

[ComponentInfo(
    "E5F6A7B8-C9D0-1234-EF01-123456789014",
    "自定义节日倒计时",
    "fluent(\uE70F)",
    "只显示你添加的自定义节日倒计时"
)]
public class CustomHolidayComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private StackPanel _main = null!;
    private HolidayService? _svc;

    public CustomHolidayComponent()
    {
        _main = new StackPanel { Orientation = Orientation.Vertical, Spacing = 3, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        Content = _main;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) }; _timer.Tick += (s, e) => Update(); _timer.Start();
        Dispatcher.UIThread.Post(() => { _svc = new HolidayService(); HolidayService.SettingsChanged += OnSettingsChanged; Update(); });
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
        var s = _svc.Settings; var now = DateTime.Now;
        var list = s.CustomHolidays.Select(h =>
        {
            var d = h.Date;
            if (h.RepeatYearly && d.Year < now.Year) d = new DateTime(now.Year, h.Date.Month, h.Date.Day);
            if (h.RepeatYearly && h.Date.Month == 2 && h.Date.Day == 29 && !DateTime.IsLeapYear(now.Year)) d = new DateTime(now.Year, 2, 28);
            return new { h.Name, Date = d, h.RepeatYearly };
        }).Where(h => h.Date.Date >= now.Date).OrderBy(h => h.Date).Take(s.CustomHolidayDisplayCount).ToList();

        if (list.Count == 0) { _main.Children.Add(new TextBlock { Text = "暂无自定义节日", Opacity = 0.5, HorizontalAlignment = HorizontalAlignment.Center, Foreground = Brushes.Black }); return; }

        // 所有节日横向排列
        var container = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        foreach (var h in list)
        {
            var days = (int)(h.Date.Date - now.Date).TotalDays;
            var item = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
            if (s.CustomHolidayShowIcon) item.Children.Add(new TextBlock { Text = "🎂", FontSize = 13, VerticalAlignment = VerticalAlignment.Center });
            item.Children.Add(new TextBlock { Text = h.Name, FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Color.Parse("#E91E63")), VerticalAlignment = VerticalAlignment.Center });
            if (s.CustomHolidayShowDays) item.Children.Add(new TextBlock { Text = days == 0 ? "就是今天！" : $"还有 {days} 天", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.8, Foreground = Brushes.Black });
            container.Children.Add(item);
        }
        _main.Children.Add(container);
    }
}
