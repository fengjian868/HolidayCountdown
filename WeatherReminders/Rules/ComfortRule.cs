namespace HolidayCountdown.WeatherReminders.Rules;

/// <summary>
/// 舒适度提醒：根据温度判断天气舒适度。
/// </summary>
public class ComfortRule : IWeatherReminderRule
{
    public string Id => "comfort";
    public string Name => "舒适度提醒";
    public string DefaultIcon => "😊";
    public bool EnabledByDefault => false;
    public int Priority => 32;

    public WeatherReminderResult? Evaluate(WeatherReminderContext context)
    {
        var temp = WeatherDataHelper.GetCurrentTemp(context.WeatherInfo);
        if (!temp.HasValue) return null;

        // 18~26°C 为舒适区间
        if (temp.Value >= 18 && temp.Value <= 26)
        {
            return new WeatherReminderResult
            {
                RuleId = Id,
                Icon = DefaultIcon,
                Text = "天气舒适，适合户外活动 😊",
                Priority = Priority,
                Category = "舒适度"
            };
        }

        return new WeatherReminderResult
        {
            RuleId = Id,
            Icon = DefaultIcon,
            Text = "天气不适，注意防护 😊",
            Priority = Priority,
            Category = "舒适度"
        };
    }
}
