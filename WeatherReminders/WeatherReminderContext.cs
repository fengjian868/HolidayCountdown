using System;
using System.Collections;
using System.Collections.Generic;

namespace HolidayCountdown.WeatherReminders;

/// <summary>
/// 天气提醒规则评估上下文。
/// </summary>
public class WeatherReminderContext
{
    /// <summary>当前温度。</summary>
    public double? CurrentTemp { get; set; }

    /// <summary>当前天气代码。</summary>
    public string? WeatherCode { get; set; }

    /// <summary>当前天气文本。</summary>
    public string? WeatherText { get; set; }

    /// <summary>
    /// 未来 24 小时逐小时天气文本（已通过 IWeatherService.GetWeatherTextByCode 解析）。
    /// 与 <see cref="WeatherInfo"/> 中的 ForecastHourly.Weather（代码列表）一一对应。
    /// </summary>
    public IReadOnlyList<string> HourlyWeatherTexts { get; set; } = Array.Empty<string>();

    /// <summary>完整天气信息对象，供规则内部反射读取 ForecastHourly / ForecastDaily。</summary>
    public object? WeatherInfo { get; set; }

    /// <summary>预警列表。</summary>
    public IList? Alerts { get; set; }

    /// <summary>天气数据更新时间。</summary>
    public DateTime? UpdateTime { get; set; }

    /// <summary>当前时间。</summary>
    public DateTime Now { get; set; } = DateTime.Now;

    /// <summary>上次评估结果，用于变化检测。</summary>
    public IReadOnlyList<WeatherReminderResult>? LastResults { get; set; }
}
