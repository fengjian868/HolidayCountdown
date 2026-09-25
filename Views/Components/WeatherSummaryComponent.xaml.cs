using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    "F1E2D3C4-B5A6-7890-1234-567890ABCDEF",
    "天气总结",
    "\uE4DB",
    "显示今日天气概要：白天/夜间天气、温度范围、穿衣建议"
)]
public class WeatherSummaryComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private StackPanel _main = null!;
    private HolidayService? _svc;

    public WeatherSummaryComponent()
    {
        _main = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        Content = _main;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _timer.Tick += (s, e) => Dispatcher.UIThread.Post(Update);
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

        try
        {
            var settings = GetSettingsServiceSettings();
            if (settings == null) { _main.Children.Add(MakeText("天气未更新", 0.5)); return; }

            var lastWeatherInfo = GetPropertyValue(settings, "LastWeatherInfo");
            if (lastWeatherInfo == null) { _main.Children.Add(MakeText("天气未更新", 0.5)); return; }

            var current = GetPropertyValue(lastWeatherInfo, "Current");
            double? temp = null;
            string? weatherCode = null;
            string? weatherText = null;

            if (current != null)
            {
                var temperature = GetPropertyValue(current, "Temperature");
                if (temperature != null)
                {
                    var tempValue = GetPropertyValue(temperature, "Value")?.ToString();
                    if (double.TryParse(tempValue, out var t)) temp = t;
                }
                weatherCode = GetPropertyValue(current, "Weather")?.ToString();
                weatherText = GetWeatherTextByCode(weatherCode);
            }

            // 获取未来几天温度
            var dailyTemps = GetDailyTemps(lastWeatherInfo, 1);
            var todayTemps = dailyTemps.Count > 0 ? dailyTemps[0] : (High: (int?)null, Low: (int?)null);

            // 构建天气概要
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };

            var icon = GetWeatherIcon(weatherText);
            if (!string.IsNullOrEmpty(icon))
                row.Children.Add(new TextBlock { Text = icon, FontSize = 14, VerticalAlignment = VerticalAlignment.Center });

            var summary = "";
            if (!string.IsNullOrEmpty(weatherText))
                summary += weatherText;
            if (temp.HasValue)
                summary += $" {temp.Value:0}°";
            else if (todayTemps.High.HasValue && todayTemps.Low.HasValue)
                summary += $" {todayTemps.Low}~{todayTemps.High}°";

            if (!string.IsNullOrEmpty(summary))
            {
                var tb = new TextBlock { Text = summary, VerticalAlignment = VerticalAlignment.Center };
                tb[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
                row.Children.Add(tb);
            }

            _main.Children.Add(row);

            // 穿衣建议
            if (temp.HasValue)
            {
                var advice = GetDressAdvice(temp.Value);
                if (!string.IsNullOrEmpty(advice))
                {
                    var adviceTb = new TextBlock { Text = advice, FontSize = 10, Opacity = 0.6, HorizontalAlignment = HorizontalAlignment.Center };
                    adviceTb[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
                    _main.Children.Add(adviceTb);
                }
            }
        }
        catch { _main.Children.Clear(); }
    }

    string GetDressAdvice(double temp)
    {
        return temp switch
        {
            >= 35 => "高温防暑",
            >= 30 => "短袖防晒",
            >= 25 => "短袖即可",
            >= 20 => "薄长袖",
            >= 15 => "建议外套",
            >= 10 => "厚外套",
            >= 5 => "羽绒服",
            >= 0 => "注意保暖",
            _ => "严寒多穿"
        };
    }

    string GetWeatherIcon(string? weatherText)
    {
        if (string.IsNullOrEmpty(weatherText)) return "🌤️";
        if (weatherText.Contains("雷阵雨")) return "⛈️";
        if (weatherText.Contains("雨")) return "🌧️";
        if (weatherText.Contains("高温")) return "🥵";
        if (weatherText.Contains("晴")) return "☀️";
        if (weatherText.Contains("多云")) return "⛅";
        if (weatherText.Contains("阴")) return "☁️";
        if (weatherText.Contains("雪") || weatherText.Contains("冰雹")) return "❄️";
        if (weatherText.Contains("雾") || weatherText.Contains("霾")) return "🌫️";
        if (weatherText.Contains("风") || weatherText.Contains("沙尘")) return "🍃";
        return "🌤️";
    }

    TextBlock MakeText(string text, double opacity)
    {
        var tb = new TextBlock { Text = text, HorizontalAlignment = HorizontalAlignment.Center, Opacity = opacity };
        tb[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
        return tb;
    }

    #region ClassIsland 天气数据获取

    string? GetWeatherTextByCode(string? code)
    {
        if (string.IsNullOrEmpty(code)) return null;
        try
        {
            var weatherServiceType = Type.GetType("ClassIsland.Core.Abstractions.Services.IWeatherService, ClassIsland.Core")
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == "IWeatherService");
            if (weatherServiceType == null) return null;

            var appHostType = Type.GetType("ClassIsland.Shared.IAppHost, ClassIsland.Shared")
                ?? Type.GetType("ClassIsland.Shared.IAppHost, ClassIsland.Core")
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == "IAppHost");
            if (appHostType == null) return null;

            var tryGetService = appHostType.GetMethod("TryGetService", BindingFlags.Public | BindingFlags.Static);
            if (tryGetService == null || !tryGetService.IsGenericMethodDefinition) return null;

            var genericMethod = tryGetService.MakeGenericMethod(weatherServiceType);
            var weatherService = genericMethod.Invoke(null, null);
            if (weatherService == null) return null;

            var getWeatherText = weatherServiceType.GetMethod("GetWeatherTextByCode", BindingFlags.Public | BindingFlags.Instance);
            if (getWeatherText == null) return null;

            return getWeatherText.Invoke(weatherService, new object[] { code })?.ToString();
        }
        catch { return null; }
    }

    List<(int? High, int? Low)> GetDailyTemps(object data, int maxDays)
    {
        var result = new List<(int? High, int? Low)>();
        try
        {
            var forecastDaily = GetPropertyValue(data, "ForecastDaily");
            if (forecastDaily == null) return result;

            var tempList = forecastDaily as IList;
            if (tempList == null) return result;

            for (int i = 0; i < Math.Min(maxDays, tempList.Count); i++)
            {
                var day = tempList[i];
                var tempObj = GetPropertyValue(day, "Temp");
                var high = GetPropertyValue(tempObj, "Max")?.ToString();
                var low = GetPropertyValue(tempObj, "Min")?.ToString();
                int? h = int.TryParse(high, out var hv) ? hv : null;
                int? l = int.TryParse(low, out var lv) ? lv : null;
                result.Add((h, l));
            }
        }
        catch { }
        return result;
    }

    object? GetSettingsServiceSettings()
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

            var settingsServiceType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == "SettingsService");
            if (settingsServiceType == null) return null;

            var genericMethod = tryGetService.MakeGenericMethod(settingsServiceType);
            var settingsService = genericMethod.Invoke(null, null);
            if (settingsService == null) return null;

            var settingsProp = settingsServiceType.GetProperty("Settings", BindingFlags.Public | BindingFlags.Instance);
            return settingsProp?.GetValue(settingsService);
        }
        catch { return null; }
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

    #endregion
}
