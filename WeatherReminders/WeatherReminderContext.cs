using System;
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

    /// <summary>完整天气数据对象（含 ForecastHourly / ForecastDaily 等），供 WeatherDataHelper 反射读取。</summary>
    public object? WeatherData { get; set; }

    /// <summary>预警列表原始对象。</summary>
    public object? Alerts { get; set; }

    /// <summary>天气数据更新时间。</summary>
    public DateTime? UpdateTime { get; set; }

    /// <summary>当前时间。</summary>
    public DateTime Now { get; set; } = DateTime.Now;

    /// <summary>上次评估结果，用于变化检测。</summary>
    public IReadOnlyList<WeatherReminderResult>? LastResults { get; set; }
}
