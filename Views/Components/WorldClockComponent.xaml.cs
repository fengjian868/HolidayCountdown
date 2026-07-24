using System;
using System.Linq;
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
    "E7F8A9B0-C1D2-3456-7890-ABCDEF123456",
    "世界时钟[测试版]",
    "\uF1B2",
    "显示多个国家/城市的时间，默认北京时间，最多5个城市"
)]
public class WorldClockComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private StackPanel _root = null!;
    private HolidayService _svc = new();

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

        HolidayService.SettingsChanged += OnSettingsChanged;
        Update();
    }

    void OnSettingsChanged()
    {
        _svc.LoadSettings();
        Dispatcher.UIThread.Post(Update);
    }

    void Update()
    {
        _root.Children.Clear();

        var cities = _svc.Settings.WorldClockCities.Take(5).ToList();
        if (cities.Count == 0) cities.Add(new WorldClockCity { Name = "北京", TimeZoneId = "China Standard Time" });

        // 黑白/灰度颜色跟随主题，带颜色则保持用户设置
        var customFg = Color.TryParse(_svc.Settings.WorldClockTextColor, out var parsedColor) ? parsedColor : Colors.White;
        var followTheme = IsMonochromeOrGrayscale(customFg);

        foreach (var city in cities)
        {
            var time = GetCityTime(city.TimeZoneId);
            var format = _svc.Settings.WorldClockShowSeconds ? "HH:mm:ss" : "HH:mm";
            var timeText = time.ToString(format);
            var dateText = _svc.Settings.WorldClockShowDate ? time.ToString("MM/dd") : "";

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
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var timeBlock = new TextBlock
            {
                Text = timeText,
                FontSize = 13,
                FontWeight = FontWeight.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            ApplyForeground(nameBlock, customFg, followTheme);
            ApplyForeground(timeBlock, customFg, followTheme);
            cityPanel.Children.Add(nameBlock);
            cityPanel.Children.Add(timeBlock);

            if (!string.IsNullOrEmpty(dateText))
            {
                var dateBlock = new TextBlock
                {
                    Text = dateText,
                    FontSize = 9,
                    Opacity = 0.6,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                ApplyForeground(dateBlock, customFg, followTheme);
                cityPanel.Children.Add(dateBlock);
            }

            _root.Children.Add(cityPanel);
        }
    }

    static bool IsMonochromeOrGrayscale(Color color)
    {
        // 纯白、纯黑、灰度（R=G=B）都视为黑白，跟随主题
        return color.R == color.G && color.G == color.B;
    }

    static void ApplyForeground(TextBlock tb, Color customColor, bool followTheme)
    {
        if (followTheme)
        {
            // 跟随 ClassIsland 主题前景色
            tb[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
        }
        else
        {
            tb.Foreground = new SolidColorBrush(customColor);
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
