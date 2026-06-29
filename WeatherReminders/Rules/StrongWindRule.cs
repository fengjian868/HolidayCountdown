namespace HolidayCountdown.WeatherReminders.Rules;

/// <summary>
/// 强风提醒：大风预警或小时级风速较高。
/// </summary>
public class StrongWindRule : IWeatherReminderRule
{
    public string Id => "strong-wind";
    public string Name => "强风提醒";
    public string DefaultIcon => "🍃";
    public bool EnabledByDefault => true;
    public int Priority => 15;

    public WeatherReminderResult? Evaluate(WeatherReminderContext context)
    {
        // 根据预警判断
        if (WeatherDataHelper.AlertContainsAny(context.Alerts, "大风", "阵风", "台风"))
        {
            return new WeatherReminderResult
            {
                RuleId = Id,
                Icon = DefaultIcon,
                Text = "大风提醒，注意安全",
                Priority = Priority,
                Category = "风"
            };
        }

        // 根据天气文本判断
        if (!string.IsNullOrEmpty(context.WeatherText) &&
            (context.WeatherText.Contains("大风") || context.WeatherText.Contains("阵风") || context.WeatherText.Contains("台风")))
        {
            return new WeatherReminderResult
            {
                RuleId = Id,
                Icon = DefaultIcon,
                Text = "大风提醒，注意安全",
                Priority = Priority,
                Category = "风"
            };
        }

        return null;
    }
}
