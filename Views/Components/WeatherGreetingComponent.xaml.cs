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
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.Components;

[ComponentInfo(
    "A7B8C9D0-E1F2-3456-0123-123456789016",
    "天气问候",
    "\uE753",
    "根据ClassIsland天气温度显示穿衣提醒，支持自定义排版模板"
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
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) }; _timer.Tick += (s, e) => Update(); _timer.Start();
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

        var (temp, weatherCode, warning) = GetWeatherData();
        var weatherText = !string.IsNullOrEmpty(weatherCode) ? GetWeatherTextByCode(weatherCode) : "";

        // 获取问候语
        var greet = GetTempGreeting(temp);
        if (string.IsNullOrEmpty(greet) && !string.IsNullOrEmpty(weatherText))
            greet = GetWeatherGreeting(weatherText);

        // 使用模板排版
        var template = _svc.Settings.WeatherTemplate ?? "{greeting}";
        var result = template
            .Replace("{greeting}", greet ?? "")
            .Replace("{temp}", temp.HasValue ? $"{temp.Value:0}°C" : "")
            .Replace("{weather}", weatherText ?? "")
            .Replace("{warning}", warning ?? "")
            .Replace("{icon}", GetWeatherIcon(weatherText));

        // 清理空括号和多余空格
        while (result.Contains("  ")) result = result.Replace("  ", " ");
        result = result.Trim();

        _txt.Text = result;
    }

    /// <summary>
    /// 根据天气文本返回对应图标
    /// </summary>
    string GetWeatherIcon(string? weatherText)
    {
        if (!_svc!.Settings.WeatherShowIcon || string.IsNullOrEmpty(weatherText)) return "";
        if (weatherText.Contains("雨")) return "🌧️";
        if (weatherText.Contains("雪")) return "❄️";
        if (weatherText.Contains("晴")) return "☀️";
        if (weatherText.Contains("云") || weatherText.Contains("阴")) return "⛅";
        if (weatherText.Contains("雾") || weatherText.Contains("霾")) return "🌫️";
        if (weatherText.Contains("风")) return "🍃";
        if (weatherText.Contains("雷")) return "⚡";
        if (weatherText.Contains("沙")) return "😷";
        return "🌤️";
    }

    /// <summary>
    /// 根据温度给出穿衣提醒
    /// </summary>
    string GetTempGreeting(double? temp)
    {
        if (temp == null) return "";
        var t = temp.Value;
        return t switch
        {
            >= 35 => "高温预警，注意防暑 🌡️",
            >= 30 => "很热，穿短袖注意防晒 ☀️",
            >= 25 => "较热，短袖即可 👕",
            >= 20 => "舒适，薄长袖或短袖 🍃",
            >= 15 => "微凉，建议穿外套 🧥",
            >= 10 => "较冷，穿厚外套 🧣",
            >= 5 => "冷，穿羽绒服或棉衣 ❄️",
            >= 0 => "很冷，注意保暖 🥶",
            _ => "严寒，多穿点别冻着 🧊"
        };
    }

    /// <summary>
    /// 根据天气文本匹配问候语（备用）
    /// </summary>
    string GetWeatherGreeting(string weatherText)
    {
        if (string.IsNullOrEmpty(weatherText)) return "";
        var match = _svc!.Settings.WeatherGreetings
            .Where(kv => kv.Key != "默认" && weatherText.Contains(kv.Key))
            .OrderByDescending(kv => kv.Key.Length)
            .FirstOrDefault();
        var greet = match.Value ?? "";
        if (string.IsNullOrEmpty(greet) && _svc.Settings.WeatherGreetings.TryGetValue("默认", out var def))
            greet = def.Replace("{weather}", weatherText);
        return greet;
    }

    /// <summary>
    /// 获取天气数据：温度、天气代码、预警
    /// </summary>
    (double? temp, string? weatherCode, string? warning) GetWeatherData()
    {
        try
        {
            var settings = GetSettingsServiceSettings();
            if (settings == null) return (null, null, null);

            var lastWeatherInfo = GetPropertyValue(settings, "LastWeatherInfo");
            if (lastWeatherInfo == null) return (null, null, null);

            var current = GetPropertyValue(lastWeatherInfo, "Current");
            double? temp = null;
            string? weatherCode = null;
            if (current != null)
            {
                var temperature = GetPropertyValue(current, "Temperature");
                if (temperature != null)
                {
                    var tempValue = GetPropertyValue(temperature, "Value")?.ToString();
                    if (double.TryParse(tempValue, out var t)) temp = t;
                }
                weatherCode = GetPropertyValue(current, "Weather")?.ToString();
            }

            var warning = GetFirstAlertTitle(lastWeatherInfo);
            return (temp, weatherCode, warning);
        }
        catch { return (null, null, null); }
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

    object? GetPropertyValue(object obj, string propName)
    {
        try
        {
            var prop = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            return prop?.GetValue(obj);
        }
        catch { return null; }
    }

    string? GetFirstAlertTitle(object lastWeatherInfo)
    {
        try
        {
            var alerts = GetPropertyValue(lastWeatherInfo, "Alerts");
            if (alerts == null) return null;

            var countProp = alerts.GetType().GetProperty("Count");
            var count = (int?)countProp?.GetValue(alerts) ?? 0;
            if (count == 0) return null;

            var firstMethod = alerts.GetType().GetMethods()
                .FirstOrDefault(m => m.Name == "FirstOrDefault" && m.GetParameters().Length == 0);
            if (firstMethod != null)
            {
                var firstAlert = firstMethod.Invoke(alerts, null);
                if (firstAlert != null)
                {
                    var titleProp = firstAlert.GetType().GetProperty("Title", BindingFlags.Public | BindingFlags.Instance);
                    return titleProp?.GetValue(firstAlert)?.ToString();
                }
            }
            return null;
        }
        catch { return null; }
    }

    string GetWeatherTextByCode(string code)
    {
        if (string.IsNullOrEmpty(code)) return "";
        try
        {
            var appHostType = Type.GetType("ClassIsland.Shared.IAppHost, ClassIsland.Shared")
                ?? Type.GetType("ClassIsland.Shared.IAppHost, ClassIsland.Core")
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == "IAppHost");

            if (appHostType == null) return "";

            var tryGetService = appHostType.GetMethod("TryGetService", BindingFlags.Public | BindingFlags.Static);
            if (tryGetService == null || !tryGetService.IsGenericMethodDefinition) return "";

            var weatherServiceType = Type.GetType("ClassIsland.Core.Abstractions.Services.IWeatherService, ClassIsland.Core")
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == "IWeatherService");

            if (weatherServiceType == null) return "";

            var genericMethod = tryGetService.MakeGenericMethod(weatherServiceType);
            var weatherService = genericMethod.Invoke(null, null);
            if (weatherService == null) return "";

            var getWeatherText = weatherServiceType.GetMethod("GetWeatherTextByCode", BindingFlags.Public | BindingFlags.Instance);
            if (getWeatherText == null) return "";

            return getWeatherText.Invoke(weatherService, new object[] { code })?.ToString() ?? "";
        }
        catch { return ""; }
    }
}
