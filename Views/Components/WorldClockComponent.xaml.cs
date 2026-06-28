using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using HolidayCountdown.Models.ComponentSettings;

namespace HolidayCountdown.Views.Components;

[ComponentInfo(
    "C3D4E5F6-A7B8-9012-CDEF-345678901234",
    "世界时钟",
    "fluent(\uE823)",
    "显示多个国家/城市的时间，默认北京时间，最多5个城市"
)]
public class WorldClockComponent : ComponentBase<WorldClockSettings>
{
    private DispatcherTimer _timer = null!;
    private StackPanel _root = null!;

    public WorldClockComponent()
    {
        _root = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        Content = _root;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (s, e) => Update();
        _timer.Start();

        Dispatcher.UIThread.Post(Update);
    }

    protected override void OnSettingsChanged()
    {
        base.OnSettingsChanged();
        Dispatcher.UIThread.Post(Update);
    }

    void Update()
    {
        _root.Children.Clear();
        if (Settings == null) return;

        var cities = Settings.Cities.Take(5).ToList();
        if (cities.Count == 0) cities.Add(new WorldClockCity { Name = "北京", TimeZoneId = "China Standard Time" });

        var fg = Color.TryParse(Settings.TextColor, out var c) ? new SolidColorBrush(c) : Brushes.White;

        foreach (var city in cities)
        {
            var time = GetCityTime(city.TimeZoneId);
            var format = Settings.ShowSeconds ? "HH:mm:ss" : "HH:mm";
            var timeText = time.ToString(format);
            var dateText = Settings.ShowDate ? time.ToString("MM/dd") : "";

            var cityPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 0,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var nameBlock = new TextBlock
            {
                Text = city.Name,
                FontSize = 10,
                Opacity = 0.7,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = fg
            };
            var timeBlock = new TextBlock
            {
                Text = timeText,
                FontSize = 13,
                FontWeight = FontWeight.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = fg
            };
            cityPanel.Children.Add(nameBlock);
            cityPanel.Children.Add(timeBlock);

            if (!string.IsNullOrEmpty(dateText))
            {
                cityPanel.Children.Add(new TextBlock
                {
                    Text = dateText,
                    FontSize = 9,
                    Opacity = 0.6,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = fg
                });
            }

            _root.Children.Add(cityPanel);
        }
    }

    static DateTime GetCityTime(string timeZoneId)
    {
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return TimeZoneInfo.ConvertTime(DateTime.Now, tz);
        }
        catch
        {
            // 兜底：尝试 Asia/Shanghai 或 UTC
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai");
                return TimeZoneInfo.ConvertTime(DateTime.Now, tz);
            }
            catch { }
        }
        return DateTime.Now;
    }
}
