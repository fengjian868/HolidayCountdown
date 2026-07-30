using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HolidayCountdown.WeatherReminders;

/// <summary>
/// 天气数据解析辅助类，供规则引擎和组件使用。
/// </summary>
public static class WeatherDataHelper
{
    /// <summary>
    /// 从小米天气代码判断是否为降水天气（8~19 为降水区间）。
    /// </summary>
    public static bool IsPrecipitationCode(int code)
    {
        return code >= 8 && code <= 19;
    }

    /// <summary>
    /// 判断天气文本是否包含降水相关关键字。
    /// </summary>
    public static bool IsPrecipitationText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        return text.Contains("雨") || text.Contains("雪") || text.Contains("冰雹");
    }

    /// <summary>
    /// 判断天气文本是否包含雷电关键字。
    /// </summary>
    public static bool IsThunderText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        return text.Contains("雷") || text.Contains("电") || text.Contains("闪");
    }

    /// <summary>
    /// 判断预警标题是否包含指定关键字之一。
    /// </summary>
    public static bool AlertContainsAny(object? alerts, params string[] keywords)
    {
        if (alerts == null || keywords.Length == 0) return false;
        try
        {
            var countProp = alerts.GetType().GetProperty("Count");
            var count = (int?)countProp?.GetValue(alerts) ?? 0;
            if (count == 0) return false;

            var indexer = alerts.GetType().GetProperties().FirstOrDefault(p => p.GetIndexParameters().Length == 1);
            if (indexer == null) return false;

            for (int i = 0; i < count; i++)
            {
                var alert = indexer.GetValue(alerts, new object[] { i });
                var title = GetPropertyValue(alert, "Title")?.ToString() ?? "";
                if (keywords.Any(k => title.Contains(k))) return true;
            }
        }
        catch { }
        return false;
    }

    /// <summary>
    /// 获取未来 N 小时的小时级天气代码列表。
    /// </summary>
    public static List<int> GetHourlyWeatherCodes(object? data, int maxHours = 24)
    {
        var result = new List<int>();
        if (data == null) return result;

        try
        {
            var forecastHourly = GetPropertyValue(data, "ForecastHourly");
            if (forecastHourly == null) return result;

            var weather = GetPropertyValue(forecastHourly, "Weather");
            if (weather == null) return result;

            var value = GetPropertyValue(weather, "Value");
            if (value is not IList list) return result;

            for (int i = 0; i < list.Count && i < maxHours; i++)
            {
                var codeObj = list[i]?.ToString();
                if (int.TryParse(codeObj, out var code)) result.Add(code);
            }
        }
        catch { }

        return result;
    }

    /// <summary>
    /// 获取未来 N 天的高温/低温预报。
    /// </summary>
    public static List<(int High, int Low)> GetDailyTemps(object? data, int maxDays = 7)
    {
        var result = new List<(int, int)>();
        if (data == null) return result;

        try
        {
            var forecastDaily = GetPropertyValue(data, "ForecastDaily");
            if (forecastDaily == null) return result;

            var temperature = GetPropertyValue(forecastDaily, "Temperature");
            if (temperature == null) return result;

            var value = GetPropertyValue(temperature, "Value");
            if (value is not IList list) return result;

            for (int i = 0; i < list.Count && i < maxDays; i++)
            {
                var item = list[i];
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

    /// <summary>
    /// 获取未来 N 小时的小时级天气文本列表。
    /// </summary>
    public static List<string> GetHourlyWeatherTexts(object? data, int maxHours = 24)
    {
        var result = new List<string>();
        if (data == null) return result;

        try
        {
            var forecastHourly = GetPropertyValue(data, "ForecastHourly");
            if (forecastHourly == null) return result;

            var weatherText = GetPropertyValue(forecastHourly, "WeatherText");
            if (weatherText == null) return result;

            var value = GetPropertyValue(weatherText, "Value");
            if (value is not IList list) return result;

            for (int i = 0; i < list.Count && i < maxHours; i++)
            {
                result.Add(list[i]?.ToString() ?? "");
            }
        }
        catch { }

        return result;
    }

    /// <summary>
    /// 获取当前天气信息中的温度。
    /// </summary>
    public static double? GetCurrentTemp(object? data)
    {
        if (data == null) return null;
        try
        {
            var current = GetPropertyValue(data, "Current");
            if (current == null) return null;

            var temperature = GetPropertyValue(current, "Temperature");
            if (temperature == null) return null;

            var tempValue = GetPropertyValue(temperature, "Value")?.ToString();
            if (double.TryParse(tempValue, out var t)) return t;
        }
        catch { }
        return null;
    }

    /// <summary>
    /// 通过反射调用 ClassIsland IWeatherService.GetWeatherTextByCode(code)，
    /// 把小米天气代码解析为"晴/小雨"等可读文本。
    /// </summary>
    /// <remarks>
    /// 旧实现曾尝试直接从 CurrentWeather / ForecastHourly 上读取 WeatherText 字段，
    /// 但这两个类根本不存在该字段——天气仅以 code 提供，文本只能由 IWeatherService 翻译。
    /// 解析失败时返回空字符串，不抛异常，确保规则链不会因解析错误而中断。
    /// </remarks>
    public static string GetWeatherTextByCode(string? code)
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

            var method = weatherServiceType.GetMethod("GetWeatherTextByCode", BindingFlags.Public | BindingFlags.Instance);
            if (method == null) return "";

            return method.Invoke(weatherService, new object[] { code })?.ToString() ?? "";
        }
        catch { return ""; }
    }

    /// <summary>
    /// 通用反射获取属性值。
    /// </summary>
    public static object? GetPropertyValue(object? obj, string propName)
    {
        if (obj == null) return null;
        try
        {
            var prop = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            return prop?.GetValue(obj);
        }
        catch { return null; }
    }
}
