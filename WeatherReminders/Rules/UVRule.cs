namespace HolidayCountdown.WeatherReminders.Rules;

/// <summary>
/// 紫外线提醒：晴天且气温 ≥ 30°C。
/// </summary>
public class UVRule : IWeatherReminderRule
{
    public string Id => "uv";
    public string Name => "紫外线提醒";
    public string DefaultIcon => "☀️";
    public bool EnabledByDefault => false;
    public int Priority => 25;

    public WeatherReminderResult? Evaluate(WeatherReminderContext context)
    {
        if (string.IsNullOrEmpty(context.WeatherText)) return null;

        // 天气文本包含"晴"
        if (!context.WeatherText.Contains("晴")) return null;

        // 气温 ≥ 30°C
        var temp = WeatherDataHelper.GetCurrentTemp(context.WeatherInfo);
        if (!temp.HasValue || temp.Value < 30) return null;

        return new WeatherReminderResult
        {
            RuleId = Id,
            Icon = DefaultIcon,
            Text = "紫外线强烈，注意防晒 ☀️",
            Priority = Priority,
            Category = "紫外线"
        };
    }
}
