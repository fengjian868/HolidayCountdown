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

    /// <summary>未来 24 小时预报集合（通常为完整 WeatherInfo 对象，由规则内部反射读取）。</summary>
    public object? HourlyForecasts { get; set; }

    /// <summary>未来 7 天预报集合（通常为完整 WeatherInfo 对象，由规则内部反射读取）。</summary>
    public object? DailyForecasts { get; set; }

    /// <summary>预警列表。</summary>
    public object? Alerts { get; set; }

    /// <summary>天气数据更新时间。</summary>
    public DateTime? UpdateTime { get; set; }

    /// <summary>当前时间。</summary>
    public DateTime Now { get; set; } = DateTime.Now;

    /// <summary>上次评估结果，用于变化检测。</summary>
    public IReadOnlyList<WeatherReminderResult>? LastResults { get; set; }
}
