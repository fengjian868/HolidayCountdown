using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
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

    async void Update()
    {
        if (_svc == null || !_svc.Settings.WeatherGreetingEnabled) { _txt.Text = ""; return; }

        try
        {
            var (weatherText, temp, warning, icon) = GetWeatherInfo();

            // 如果ClassIsland没有天气数据，尝试调用网络API
            if (string.IsNullOrEmpty(weatherText) || !temp.HasValue)
            {
                var apiWeather = await GetWeatherFromApiAsync();
                if (!string.IsNullOrEmpty(apiWeather.weather))
                {
                    weatherText = apiWeather.weather;
                    temp = apiWeather.temp;
                }
            }

            // 如果仍然没有天气数据，显示温度提醒或默认提示
            if (string.IsNullOrEmpty(weatherText) && !temp.HasValue)
            {
                _txt.Text = "";
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
            var tempReminder = GetTempReminder(temp);
            if (!string.IsNullOrEmpty(tempReminder))
            {
                greeting = string.IsNullOrEmpty(greeting) ? tempReminder : $"{greeting}，{tempReminder}";
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

    async Task<(string weather, double? temp)> GetWeatherFromApiAsync()
    {
        try
        {
            // 使用 IP 定位获取城市，然后查询天气
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

            // 1. 通过 IP 获取大致位置
            var ipInfo = await client.GetStringAsync("https://ipapi.co/json/");
            using var ipDoc = JsonDocument.Parse(ipInfo);
            var lat = ipDoc.RootElement.GetProperty("latitude").GetDouble();
            var lon = ipDoc.RootElement.GetProperty("longitude").GetDouble();
            var city = ipDoc.RootElement.TryGetProperty("city", out var c) ? c.GetString() : "";

            // 2. 使用 Open-Meteo 获取天气（免费，无需 API Key）
            var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current=temperature_2m,weather_code&timezone=auto";
            var weatherJson = await client.GetStringAsync(weatherUrl);
            using var wDoc = JsonDocument.Parse(weatherJson);
            var current = wDoc.RootElement.GetProperty("current");
            var temperature = current.GetProperty("temperature_2m").GetDouble();
            var weatherCode = current.GetProperty("weather_code").GetInt32();

            // WMO Weather interpretation codes
            var weatherText = weatherCode switch
            {
                0 => "晴",
                1 or 2 or 3 => "多云",
                45 or 48 => "雾",
                51 or 53 or 55 => "小雨",
                56 or 57 => "冻雨",
                61 or 63 or 65 => "雨",
                66 or 67 => "雨夹雪",
                71 or 73 or 75 => "雪",
                77 => "小雪",
                80 or 81 or 82 => "阵雨",
                85 or 86 => "阵雪",
                95 => "雷雨",
                96 or 99 => "雷暴",
                _ => "多云"
            };

            // 缓存位置信息
            try
            {
                var cachePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ClassIsland", "Plugins", "HolidayCountdown", "weather_location.json");
                var cache = new { Lat = lat, Lon = lon, City = city, Date = DateTime.Now.Date };
                File.WriteAllText(cachePath, JsonSerializer.Serialize(cache));
            }
            catch { }

            return (weatherText, temperature);
        }
        catch { return ("", null); }
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
