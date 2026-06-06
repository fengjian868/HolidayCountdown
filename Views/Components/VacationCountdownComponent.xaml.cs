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
    "F6A7B8C9-D0E1-2345-F012-123456789015",
    "寒暑假倒计时",
    "\uE7BE",
    "显示距离寒暑假的剩余周数和天数"
)]
public class VacationCountdownComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private StackPanel _main = null!;
    private HolidayService? _svc;

    public VacationCountdownComponent()
    {
        _main = new StackPanel { Orientation = Orientation.Vertical, Spacing = 2, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        Content = _main;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromHours(1) }; _timer.Tick += (s, e) => Update(); _timer.Start();
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
        if (_svc == null || !_svc.Settings.ShowVacationCountdown) return;
        var now = DateTime.Now; var s = _svc.Settings;
        var targets = new[] { ("暑假", s.SummerStart, s.SummerEnd), ("寒假", s.WinterStart, s.WinterEnd) };
        
        // 找出最近的一个假期
        var nearest = targets
            .Select(t =>
            {
                var (name, start, end) = t;
                if (now.Date < start.Date)
                    return new { Name = name, Start = start, End = end, Days = (start.Date - now.Date).Days, IsActive = false };
                else if (now.Date >= start.Date && now.Date <= end.Date)
                    return new { Name = name, Start = start, End = end, Days = (end.Date - now.Date).Days, IsActive = true };
                else
                    return null;
            })
            .Where(x => x != null)
            .OrderBy(x => x!.Days)
            .FirstOrDefault();

        if (nearest != null)
        {
            var weeks = nearest.Days / 7; var days = nearest.Days % 7;
            if (nearest.IsActive)
            {
                _main.Children.Add(new TextBlock 
                { 
                    Text = $"{nearest.Name}进行中", 
                    HorizontalAlignment = HorizontalAlignment.Center, 
                    Foreground = new SolidColorBrush(Color.Parse("#4CAF50")),
                    FontWeight = FontWeight.SemiBold,
                    Margin = new Thickness(0, 1, 0, 0)
                });
                _main.Children.Add(new TextBlock 
                { 
                    Text = $"剩余 {weeks} 周 {days} 天", 
                    HorizontalAlignment = HorizontalAlignment.Center, 
                    Foreground = new SolidColorBrush(Color.Parse("#4CAF50")),
                    Margin = new Thickness(0, 0, 0, 1)
                });
            }
            else
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                row.Children.Add(new TextBlock
                {
                    Text = $"距离{nearest.Name}还有",
                    Foreground = new SolidColorBrush(Color.Parse("#FF9800")),
                    FontWeight = FontWeight.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                });
                row.Children.Add(new TextBlock
                {
                    Text = $"{weeks} 周 {days} 天",
                    Foreground = new SolidColorBrush(Color.Parse("#FF9800")),
                    VerticalAlignment = VerticalAlignment.Center
                });
                _main.Children.Add(row);
            }
        }
        else
        {
            _main.Children.Add(new TextBlock { Text = "暂无寒暑假安排", HorizontalAlignment = HorizontalAlignment.Center, Opacity = 0.5 });
        }
    }
}
