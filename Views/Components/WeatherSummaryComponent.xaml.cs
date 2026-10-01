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
    "用一句话总结今天的天气，含早间天气、全天天气、温度和穿衣建议"
)]
public class WeatherSummaryComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private TextBlock _main = null!;
    private HolidayService? _svc;

    public WeatherSummaryComponent()
    {
        _main = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        _main[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
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
        if (_svc == null) return;

        try
        {
            var settings = GetSettingsServiceSettings();
            if (settings == null) { _main.Text = "天气未更新"; _main.Opacity = 0.5; return; }

            var lastWeatherInfo = GetPropertyValue(settings, "LastWeatherInfo");
            if (lastWeatherInfo == null) { _main.Text = "天气未更新"; _main.Opacity = 0.5; return; }

            _main.Opacity = 1;

            // 获取当前天气
            var current = GetPropertyValue(lastWeatherInfo, "Current");
            string? currentWeatherText = null;
            double? currentTemp = null;

            if (current != null)
            {
                var tempObj = GetPropertyValue(current, "Temperature");
                if (tempObj != null)
                {
                    var tempVal = GetPropertyValue(tempObj, "Value")?.ToString();
                    if (double.TryParse(tempVal, out var t)) currentTemp = t;
                }
                var weatherCode = GetPropertyValue(current, "Weather")?.ToString();
                currentWeatherText = GetWeatherTextByCode(weatherCode);
            }

            // 获取今日温度范围
            var dailyTemps = GetDailyTemps(lastWeatherInfo, 1);
            int? todayHigh = dailyTemps.Count > 0 ? dailyTemps[0].High : null;
            int? todayLow = dailyTemps.Count > 0 ? dailyTemps[0].Low : null;

            // 获取今日白天/夜间天气
            var dailyForecast = GetDailyForecast(lastWeatherInfo, 1);
            string? dayWeather = null;
            string? nightWeather = null;
            if (dailyForecast.Count > 0)
            {
                dayWeather = dailyForecast[0].DayWeather;
                nightWeather = dailyForecast[0].NightWeather;
            }

            // 获取空气质量
            string? airQuality = GetAirQuality(lastWeatherInfo);

            // 构建天气总结句子
            var parts = new List<string>();

            // 早间天气
            var morningWeather = dayWeather ?? currentWeatherText ?? "未知";
            var quality = airQuality ?? GetQualityFromWeather(morningWeather);
            if (!string.IsNullOrEmpty(quality))
                parts.Add($"今天早上天气{morningWeather}（{quality}）");
            else
                parts.Add($"今天早上天气{morningWeather}");

            // 全天天气
            var todayWeather = currentWeatherText ?? dayWeather ?? morningWeather;
            if (todayWeather != morningWeather)
                parts.Add($"今天天气{todayWeather}");
            else
                parts.Add($"今天天气{todayWeather}");

            // 温度
            if (todayHigh.HasValue && todayLow.HasValue)
                parts.Add($"{todayLow}~{todayHigh}°");
            else if (currentTemp.HasValue)
                parts.Add($"{currentTemp.Value:0}°");

            // 穿衣建议
            var dressTemp = currentTemp ?? (todayHigh.HasValue ? todayHigh : (double?)null);
            if (dressTemp.HasValue)
            {
                var advice = GetDressAdvice(dressTemp.Value);
                if (!string.IsNullOrEmpty(advice))
                    parts.Add(advice);
            }

            _main.Text = string.Join("，", parts);
        }
        catch { _main.Text = ""; }
    }

    string GetQualityFromWeather(string? weather)
    {
        if (string.IsNullOrEmpty(weather)) return "";
        if (weather.Contains("晴")) return "良好";
        if (weather.Contains("多云")) return "较好";
        if (weather.Contains("阴")) return "一般";
        if (weather.Contains("雨")) return "较差";
        if (weather.Contains("雪") || weather.Contains("冰雹")) return "差";
        if (weather.Contains("雾") || weather.Contains("霾")) return "较差";
        return "良好";
    }

    string GetDressAdvice(double temp)
    {
        return temp switch
        {
            >= 35 => "注意防暑",
            >= 30 => "短袖防晒",
            >= 25 => "短袖即可",
            >= 20 => "薄长袖",
            >= 15 => "建议外套",
            >= 10 => "厚外套",
            >= 5 => "穿羽绒服",
            >= 0 => "注意保暖",
            _ => "严寒多穿"
        };
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

    string? GetAirQuality(object? weatherInfo)
    {
        try
        {
            var aqi = GetPropertyValue(weatherInfo, "Aqi");
            if (aqi == null) return null;

            var aqiVal = GetPropertyValue(aqi, "Value")?.ToString();
            if (int.TryParse(aqiVal, out var v))
            {
                return v switch
                {
                    <= 50 => "优",
                    <= 100 => "良",
                    <= 150 => "轻度污染",
                    <= 200 => "中度污染",
                    <= 300 => "重度污染",
                    _ => "严重污染"
                };
            }
        }
        catch { }
        return null;
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

    List<(string? DayWeather, string? NightWeather)> GetDailyForecast(object data, int maxDays)
    {
        var result = new List<(string? DayWeather, string? NightWeather)>();
        try
        {
            var forecastDaily = GetPropertyValue(data, "ForecastDaily");
            if (forecastDaily == null) return result;

            var tempList = forecastDaily as IList;
            if (tempList == null) return result;

            for (int i = 0; i < Math.Min(maxDays, tempList.Count); i++)
            {
                var day = tempList[i];
                var dayCode = GetPropertyValue(day, "DayWeather")?.ToString();
                var nightCode = GetPropertyValue(day, "NightWeather")?.ToString();
                var dayWeather = GetWeatherTextByCode(dayCode);
                var nightWeather = GetWeatherTextByCode(nightCode);
                result.Add((dayWeather, nightWeather));
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
