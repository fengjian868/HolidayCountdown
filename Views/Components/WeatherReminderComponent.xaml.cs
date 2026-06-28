using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;

namespace HolidayCountdown.Views.Components;

[ComponentInfo(
    "D4E5F6A7-B8C9-0123-DEF1-2345678901AB",
    "天气变化提醒",
    "fluent(\uE753)",
    "根据未来数日天气生成降温、升温、降水、日落、雷电等提醒"
)]
public class WeatherReminderComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private TextBlock _txt = null!;

    public WeatherReminderComponent()
    {
        _txt = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = 12,
            TextWrapping = TextWrapping.NoWrap,
            Opacity = 0.9
        };
        Content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Children = { _txt }
        };

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
        _timer.Tick += (s, e) => Update();
        _timer.Start();

        Dispatcher.UIThread.Post(Update);
    }

    void Update()
    {
        try
        {
            var reminders = BuildReminders();
            if (reminders.Count == 0)
            {
                _txt.Text = "";
                return;
            }

            _txt.Text = string.Join("  ·  ", reminders.Take(3));
        }
        catch
        {
            _txt.Text = "";
        }
    }

    List<string> BuildReminders()
    {
        var list = new List<string>();
        var data = GetWeatherData();
        if (data == null) return list;

        // 1. 温度趋势（今明后对比）
        var dailyTemps = GetDailyTemps(data);
        if (dailyTemps.Count >= 3)
        {
            var todayHigh = dailyTemps[0].High;
            var dayAfterHigh = dailyTemps[2].High;
            var diff = dayAfterHigh - todayHigh;
            if (diff <= -5)
                list.Add($"🧣 后天降温约{Math.Abs(diff)}°C");
            else if (diff >= 5)
                list.Add($"🌡️ 后天升温约{diff}°C");
        }
        if (dailyTemps.Count >= 7)
        {
            var todayHigh = dailyTemps[0].High;
            var nextWeekHigh = dailyTemps[6].High;
            var diff = nextWeekHigh - todayHigh;
            if (diff >= 5)
                list.Add($"📈 下周升温约{diff}°C");
            else if (diff <= -5)
                list.Add($"📉 下周降温约{Math.Abs(diff)}°C");
        }

        // 2. 未来几小时降水
        var rainHour = GetNextRainHour(data);
        if (rainHour.HasValue)
        {
            var when = rainHour.Value == 0 ? "未来1小时内" : $"约{rainHour.Value}小时后";
            list.Add($"🌧️ {when}降水增多");
        }

        // 3. 日落时间
        var sunset = GetTodaySunset(data);
        if (!string.IsNullOrEmpty(sunset))
            list.Add($"🌇 日落 {sunset}");

        // 4. 雷电预警
        var thunderAlert = GetThunderAlert(data);
        if (!string.IsNullOrEmpty(thunderAlert))
            list.Add($"⚡ {thunderAlert}");

        return list;
    }

    object? GetWeatherData()
    {
        try
        {
            var settings = GetSettingsServiceSettings();
            if (settings == null) return null;
            return GetPropertyValue(settings, "LastWeatherInfo");
        }
        catch { return null; }
    }

    List<(int High, int Low)> GetDailyTemps(object data)
    {
        var result = new List<(int, int)>();
        try
        {
            var forecastDaily = GetPropertyValue(data, "ForecastDaily");
            if (forecastDaily == null) return result;

            var temperature = GetPropertyValue(forecastDaily, "Temperature");
            if (temperature == null) return result;

            var value = GetPropertyValue(temperature, "Value");
            if (value is not IList list) return result;

            foreach (var item in list)
            {
                var from = GetPropertyValue(item, "From")?.ToString();
                var to = GetPropertyValue(item, "To")?.ToString();
                int low = 0, high = 0;
                if (from != null) int.TryParse(from, out low);
                if (to != null) int.TryParse(to, out high);
                result.Add((high, low));
            }
        }
        catch { }
        return result;
    }

    int? GetNextRainHour(object data)
    {
        try
        {
            var forecastHourly = GetPropertyValue(data, "ForecastHourly");
            if (forecastHourly == null) return null;

            var weather = GetPropertyValue(forecastHourly, "Weather");
            if (weather == null) return null;

            var value = GetPropertyValue(weather, "Value");
            if (value is not IList list) return null;

            for (int i = 0; i < list.Count && i < 24; i++)
            {
                var codeObj = list[i]?.ToString();
                if (int.TryParse(codeObj, out var code))
                {
                    // 小米天气代码：1-3 晴/多云，4-7 阴，8+ 为雨/雪等降水
                    if (code >= 8 && code <= 19) return i;
                }
            }
        }
        catch { }
        return null;
    }

    string? GetTodaySunset(object data)
    {
        try
        {
            var forecastDaily = GetPropertyValue(data, "ForecastDaily");
            if (forecastDaily == null) return null;

            var sunRiseSet = GetPropertyValue(forecastDaily, "SunRiseSet");
            if (sunRiseSet == null) return null;

            var value = GetPropertyValue(sunRiseSet, "Value");
            if (value is not IList list || list.Count == 0) return null;

            var item = list[0];
            var to = GetPropertyValue(item, "To")?.ToString();
            if (string.IsNullOrEmpty(to)) return null;

            // to 可能是 "18:47" 或完整时间
            if (to.Length > 5) to = to[..5];
            return to;
        }
        catch { return null; }
    }

    string? GetThunderAlert(object data)
    {
        try
        {
            var alerts = GetPropertyValue(data, "Alerts");
            if (alerts == null) return null;

            var countProp = alerts.GetType().GetProperty("Count");
            var count = (int?)countProp?.GetValue(alerts) ?? 0;
            if (count == 0) return null;

            var indexer = alerts.GetType().GetProperties().FirstOrDefault(p => p.GetIndexParameters().Length == 1);
            if (indexer == null) return null;

            for (int i = 0; i < count; i++)
            {
                var alert = indexer.GetValue(alerts, new object[] { i });
                if (alert == null) continue;
                var title = GetPropertyValue(alert, "Title")?.ToString() ?? "";
                if (title.Contains("雷") || title.Contains("电") || title.Contains("雷雨") || title.Contains("雷阵雨"))
                    return title;
            }
        }
        catch { }
        return null;
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
}
