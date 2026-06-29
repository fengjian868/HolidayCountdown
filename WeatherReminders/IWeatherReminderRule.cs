using System.Collections.Generic;

namespace HolidayCountdown.WeatherReminders;

/// <summary>
/// 天气变化提醒规则接口。
/// </summary>
public interface IWeatherReminderRule
{
    /// <summary>唯一标识，用于持久化启用状态。</summary>
    string Id { get; }

    /// <summary>显示名称。</summary>
    string Name { get; }

    /// <summary>默认图标。</summary>
    string DefaultIcon { get; }

    /// <summary>是否默认启用。</summary>
    bool EnabledByDefault { get; }

    /// <summary>优先级，数字越小越优先。</summary>
    int Priority { get; }

    /// <summary>
    /// 根据上下文评估是否生成提醒。
    /// </summary>
    WeatherReminderResult? Evaluate(WeatherReminderContext context);
}
