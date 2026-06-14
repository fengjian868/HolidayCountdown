using System;
using System.Collections.Generic;
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

    void Update()
    {
        if (_svc == null || !_svc.Settings.WeatherGreetingEnabled) { _txt.Text = ""; return; }

        _ = UpdateAsync();
    }

    async Task UpdateAsync()
    {
        if (_svc == null) return;

        try
        {
            var (weatherText, temp, warning, icon, city, diag) = GetWeatherInfo();

            // 如果ClassIsland没有天气数据，尝试调用网络API（传入ClassIsland设置的城市）
            if (string.IsNullOrEmpty(weatherText) || !temp.HasValue)
            {
                var apiWeather = await GetWeatherFromApiAsync(city);
                if (!string.IsNullOrEmpty(apiWeather.weather))
                {
                    weatherText = apiWeather.weather;
                    temp = apiWeather.temp;
                }
            }

            // 如果仍然没有天气数据，显示诊断信息
            if (string.IsNullOrEmpty(weatherText) && !temp.HasValue)
            {
                _txt.Text = $"🌤️ {diag ?? "天气加载中..."}";
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

    async Task<(string weather, double? temp)> GetWeatherFromApiAsync(string? cityName)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            double lat, lon;
            string city;

            // 如果 ClassIsland 有设置城市，使用城市名查询经纬度
            if (!string.IsNullOrEmpty(cityName))
            {
                city = cityName;
                // 使用 Open-Meteo Geocoding API 查询城市经纬度
                var geoUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&count=1&language=zh&format=json";
                var geoJson = await client.GetStringAsync(geoUrl);
                using var geoDoc = JsonDocument.Parse(geoJson);
                if (geoDoc.RootElement.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
                {
                    var first = results[0];
                    lat = first.GetProperty("latitude").GetDouble();
                    lon = first.GetProperty("longitude").GetDouble();
                }
                else
                {
                    // 城市查询失败，回退到 IP 定位
                    return await GetWeatherFromIpAsync();
                }
            }
            else
            {
                // 没有城市设置，使用 IP 定位
                return await GetWeatherFromIpAsync();
            }

            // 使用 Open-Meteo 获取天气
            return await QueryOpenMeteoAsync(client, lat, lon, city);
        }
        catch { return ("", null); }
    }

    async Task<(string weather, double? temp)> GetWeatherFromIpAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var ipInfo = await client.GetStringAsync("https://ipapi.co/json/");
            using var ipDoc = JsonDocument.Parse(ipInfo);
            var lat = ipDoc.RootElement.GetProperty("latitude").GetDouble();
            var lon = ipDoc.RootElement.GetProperty("longitude").GetDouble();
            var city = ipDoc.RootElement.TryGetProperty("city", out var c) ? (c.GetString() ?? "") : "";
            return await QueryOpenMeteoAsync(client, lat, lon, city);
        }
        catch { return ("", null); }
    }

    async Task<(string weather, double? temp)> QueryOpenMeteoAsync(HttpClient client, double lat, double lon, string city)
    {
        var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current=temperature_2m,weather_code&timezone=auto";
        var weatherJson = await client.GetStringAsync(weatherUrl);
        using var wDoc = JsonDocument.Parse(weatherJson);
        var current = wDoc.RootElement.GetProperty("current");
        var temperature = current.GetProperty("temperature_2m").GetDouble();
        var weatherCode = current.GetProperty("weather_code").GetInt32();

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

    (string weather, double? temp, string? warning, string? icon, string? city, string? diag) GetWeatherInfo()
    {
        try
        {
            var settingsService = GetSettingsService();
            if (settingsService == null) return ("", null, null, null, null, "无SettingsService");

            var settings = GetPropertyValue(settingsService, "Settings");
            if (settings == null) return ("", null, null, null, null, "无Settings");

            // 尝试读取 ClassIsland 设置中的城市名
            var city = GetPropertyValue(settings, "WeatherCity")?.ToString()
                ?? GetPropertyValue(settings, "City")?.ToString()
                ?? "";

            // 尝试多种可能的天气属性名（ClassIsland 不同版本使用的属性名不同）
            var weatherObj = GetPropertyValue(settings, "Weather")
                ?? GetPropertyValue(settings, "CurrentWeather")
                ?? GetPropertyValue(settings, "WeatherInfo")
                ?? GetPropertyValue(settings, "LastWeatherInfo");
            if (weatherObj == null)
            {
                // 列出Settings所有属性名用于诊断
                var props = settings.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Select(p => p.Name)
                    .Where(n => n.Contains("Weather", StringComparison.OrdinalIgnoreCase) || n.Contains("City", StringComparison.OrdinalIgnoreCase));
                return ("", null, null, null, city, $"无Weather属性。相关属性: {string.Join(", ", props)}");
            }

            // WeatherInfo 对象结构：Current=CurrentWeather, Alerts=List<WeatherAlert>
            // 需要先读取 Current 属性获取实际天气数据
            var currentWeatherObj = GetPropertyValue(weatherObj, "Current");
            if (currentWeatherObj != null)
            {
                weatherObj = currentWeatherObj; // 切换到 CurrentWeather 对象读取数据
            }

            // 尝试多种可能的属性名读取天气文本
            var weatherText = GetPropertyValue(weatherObj, "WeatherText")?.ToString()
                ?? GetPropertyValue(weatherObj, "Text")?.ToString()
                ?? GetPropertyValue(weatherObj, "Weather")?.ToString()
                ?? GetPropertyValue(weatherObj, "Description")?.ToString()
                ?? "";

            // 尝试多种可能的属性名读取温度
            var temp = GetPropertyValue(weatherObj, "Temperature") as double?
                ?? (GetPropertyValue(weatherObj, "Temp") as double?)
                ?? (GetPropertyValue(weatherObj, "CurrentTemperature") as double?)
                ?? (GetPropertyValue(weatherObj, "Temperature2m") as double?);

            // 尝试多种可能的属性名读取预警（从 Alerts 列表读取）
            var alertsObj = GetPropertyValue(weatherObj, "Alerts");
            string? warning = null;
            if (alertsObj is System.Collections.IEnumerable alerts && alertsObj is not string)
            {
                var alertTexts = new List<string>();
                foreach (var alert in alerts)
                {
                    var alertText = GetPropertyValue(alert, "Title")?.ToString()
                        ?? GetPropertyValue(alert, "Description")?.ToString()
                        ?? GetPropertyValue(alert, "Alert")?.ToString()
                        ?? "";
                    if (!string.IsNullOrEmpty(alertText)) alertTexts.Add(alertText);
                }
                if (alertTexts.Count > 0) warning = string.Join("; ", alertTexts);
            }
            if (string.IsNullOrEmpty(warning))
            {
                warning = GetPropertyValue(weatherObj, "WeatherWarning")?.ToString()
                    ?? GetPropertyValue(weatherObj, "Warning")?.ToString()
                    ?? "";
            }

            // 尝试多种可能的属性名读取图标
            var icon = GetPropertyValue(weatherObj, "WeatherIcon")?.ToString()
                ?? GetPropertyValue(weatherObj, "Icon")?.ToString()
                ?? GetPropertyValue(weatherObj, "IconSource")?.ToString()
                ?? "";

            if (string.IsNullOrEmpty(weatherText) && !temp.HasValue)
            {
                var weatherProps = weatherObj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var propDict = weatherProps.ToDictionary(p => p.Name, p => p.GetValue(weatherObj)?.ToString() ?? "null");
                var propsInfo = string.Join(", ", propDict.Select(kv => $"{kv.Key}={kv.Value}"));
                return ("", null, null, null, city, $"Weather对象存在但无数据。类型: {weatherObj.GetType().Name}, 属性: {propsInfo}");
            }

            return (weatherText, temp, warning, icon, city, null);
        }
        catch (Exception ex) { return ("", null, null, null, null, $"异常: {ex.Message}"); }
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
