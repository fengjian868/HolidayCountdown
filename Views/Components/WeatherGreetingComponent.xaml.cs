using System;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using HolidayCountdown.Models;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.Components;

[ComponentInfo(
    "B2C3D4E5-F6A7-8901-BCDE-F23456789012",
    "天气问候",
    "\uE753",
    "根据ClassIsland天气显示问候语和穿衣提醒"
)]
public class WeatherGreetingComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private TextBlock _txt = null!;
    private HolidayService? _svc;

    public WeatherGreetingComponent()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        _txt = new TextBlock { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Opacity = 0.9 };
        panel.Children.Add(_txt);
        Content = panel;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _timer.Tick += (s, e) => Update();
        _timer.Start();
        Dispatcher.UIThread.Post(() => { _svc = new HolidayService(); HolidayService.SettingsChanged += OnSettingsChanged; Update(); });
    }

    void OnSettingsChanged()
    {
        _svc?.LoadSettings();
        Dispatcher.UIThread.Post(Update);
    }

    void Update()
    {
        if (_svc == null || !_svc.Settings.WeatherGreetingEnabled) { _txt.Text = ""; return; }

        try
        {
            var (weatherText, temp, warning, icon) = GetWeatherInfo();

            // 如果ClassIsland没有天气数据，显示温度提醒或默认提示
            if (string.IsNullOrEmpty(weatherText))
            {
                if (temp.HasValue)
                {
                    var tempReminder = GetTempReminder(temp);
                    _txt.Text = string.IsNullOrEmpty(tempReminder) ? "" : $"🌡️ {tempReminder}";
                }
                else
                {
                    _txt.Text = "";
                }
                return;
            }

            // 检查预警
            if (_svc.Settings.WeatherWarningOverride && !string.IsNullOrEmpty(warning))
            {
                _txt.Text = $"⚠️ {warning}";
                return;
            }

            // 匹配天气问候语
            var greeting = GetWeatherGreeting(weatherText);

            // 温度提醒
            var tempReminder2 = GetTempReminder(temp);
            if (!string.IsNullOrEmpty(tempReminder2))
            {
                greeting = string.IsNullOrEmpty(greeting) ? tempReminder2 : $"{greeting}，{tempReminder2}";
            }

            // 使用模板排版
            var template = _svc.Settings.WeatherTemplate ?? "{greeting}";
            var showIcon = _svc.Settings.WeatherShowIcon;
            var showTemp = _svc.Settings.WeatherShowTemp;

            var result = template
                .Replace("{greeting}", greeting)
                .Replace("{weather}", weatherText)
                .Replace("{warning}", warning ?? "")
                .Replace("{icon}", showIcon ? (icon ?? "") : "")
                .Replace("{temp}", showTemp && temp.HasValue ? $"{temp.Value}°C" : "");

            // 清理多余空格
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\s+", " ").Trim();

            _txt.Text = result;
        }
        catch { _txt.Text = ""; }
    }

    string GetWeatherGreeting(string weatherText)
    {
        if (string.IsNullOrEmpty(weatherText)) return "";

        // 按关键词匹配，优先匹配更长的关键词
        var items = _svc?.Settings.WeatherGreetingItems
            .Where(x => x.Keyword != "默认")
            .OrderByDescending(x => x.Keyword.Length)
            .ToList();

        if (items != null)
        {
            foreach (var item in items)
            {
                if (weatherText.Contains(item.Keyword))
                {
                    return item.Text.Replace("{weather}", weatherText);
                }
            }
        }

        // 默认
        var defaultItem = _svc?.Settings.WeatherGreetingItems.FirstOrDefault(x => x.Keyword == "默认");
        return defaultItem?.Text.Replace("{weather}", weatherText) ?? weatherText;
    }

    string? GetTempReminder(double? temp)
    {
        if (!temp.HasValue) return null;
        var t = temp.Value;
        var item = _svc?.Settings.TempGreetings
            .FirstOrDefault(x => t >= x.MinTemp && t < x.MaxTemp);
        return item?.Text;
    }

    (string weather, double? temp, string? warning, string? icon) GetWeatherInfo()
    {
        try
        {
            var settingsService = GetSettingsService();
            if (settingsService == null) return ("", null, null, null);

            var settings = GetPropertyValue(settingsService, "Settings");
            if (settings == null) return ("", null, null, null);

            var weatherObj = GetPropertyValue(settings, "Weather");
            if (weatherObj == null) return ("", null, null, null);

            var weatherText = GetPropertyValue(weatherObj, "WeatherText")?.ToString() ?? "";
            var temp = GetPropertyValue(weatherObj, "Temperature") as double?;
            var warning = GetPropertyValue(weatherObj, "WeatherWarning")?.ToString();
            var icon = GetPropertyValue(weatherObj, "WeatherIcon")?.ToString();

            return (weatherText, temp, warning, icon);
        }
        catch { return ("", null, null, null); }
    }

    object? GetSettingsService()
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

            var settingsServiceType = Type.GetType("ClassIsland.Core.Abstractions.Services.ISettingsService, ClassIsland.Core")
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == "ISettingsService" || t.Name == "SettingsService");

            if (settingsServiceType == null) return null;

            var genericMethod = tryGetService.MakeGenericMethod(settingsServiceType);
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
}
