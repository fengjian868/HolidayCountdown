namespace HolidayCountdown.WeatherReminders;

/// <summary>
/// 单条天气提醒结果。
/// </summary>
public class WeatherReminderResult
{
    /// <summary>触发规则的 ID。</summary>
    public string RuleId { get; set; } = "";

    /// <summary>提醒文本。</summary>
    public string Text { get; set; } = "";

    /// <summary>提醒图标。</summary>
    public string Icon { get; set; } = "";

    /// <summary>优先级，数字越小越优先。</summary>
    public int Priority { get; set; }

    /// <summary>分类，用于分组或统计。</summary>
    public string Category { get; set; } = "";
}
