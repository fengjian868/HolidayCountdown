using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using HolidayCountdown.Models;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.Components;

[ComponentInfo(
    "C3D4E5F6-A7B8-9012-CDEF-123456789012",
    "24节气倒计时",
    "\uE9CA",
    "显示距离下一个24节气的剩余天数，可选弧形进度环，有网络时自动刷新"
)]
public class SolarTermComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private StackPanel _main = null!;
    private HolidayService? _svc;

    public SolarTermComponent()
    {
        _main = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        Content = _main;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(30) }; _timer.Tick += (s, e) => Update(); _timer.Start();
        Dispatcher.UIThread.Post(() => { _svc = new HolidayService(); HolidayService.SettingsChanged += OnSettingsChanged; _ = Task.Run(async () => await SolarTermData.TryRefreshAsync()); Update(); });
    }

    void OnSettingsChanged()
    {
        _svc?.LoadSettings();
        Dispatcher.UIThread.Post(Update);
    }

    void Update()
    {
        _main.Children.Clear();
        var next = SolarTermData.GetNext(); var prev = SolarTermData.GetPrev();
        if (next == null) { _main.Children.Add(new TextBlock { Text = "暂无节气数据", Opacity = 0.6, VerticalAlignment = VerticalAlignment.Center }); return; }
        var days = (int)(next.Date - DateTime.Now.Date).TotalDays;
        var color = _svc?.GetTermColor(next.Name) ?? Color.Parse("#2196F3");

        // 弧形进度环（可开关）
        if (_svc?.Settings.SolarTermShowProgressRing != false)
        {
            var size = 32.0;
            var grid = new Grid { Width = size, Height = size, VerticalAlignment = VerticalAlignment.Center };
            grid.Children.Add(new Arc { Width = size, Height = size, StartAngle = -90, SweepAngle = 360, Stroke = new SolidColorBrush(Color.Parse("#20FFFFFF")), StrokeThickness = 2.5, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center });
            double p = 0;
            if (prev != null) { var t = (next.Date - prev.Date).TotalDays; var pass = (DateTime.Now - prev.Date).TotalDays; p = Math.Max(0, Math.Min(1, pass / t)); }
            else p = Math.Max(0, Math.Min(1, 1 - days / 15.0));
            grid.Children.Add(new Arc { Width = size, Height = size, StartAngle = -90, SweepAngle = p * 360, Stroke = new SolidColorBrush(color), StrokeThickness = 2.5, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center });
            grid.Children.Add(new TextBlock { Text = next.Date.Day.ToString(), FontSize = 9, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(color), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center });
            _main.Children.Add(grid);
        }

        _main.Children.Add(new TextBlock { Text = "🌿", FontSize = 13, VerticalAlignment = VerticalAlignment.Center });
        _main.Children.Add(new TextBlock { Text = next.Name, FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(color), VerticalAlignment = VerticalAlignment.Center });
        _main.Children.Add(new TextBlock { Text = days == 0 ? "就是今天" : $"还有 {days} 天", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.85 });
        if (days == 0) _main.Children.Add(new TextBlock { Text = "✨", FontSize = 12, VerticalAlignment = VerticalAlignment.Center });
    }
}
