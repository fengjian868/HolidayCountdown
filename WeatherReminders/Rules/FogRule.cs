namespace HolidayCountdown.WeatherReminders.Rules;

/// <summary>
/// 大雾提醒：天气文本包含雾或霾。
/// </summary>
public class FogRule : IWeatherReminderRule
{
    public string Id => "fog";
    public string Name => "大雾提醒";
    public string DefaultIcon => "🌫️";
    public bool EnabledByDefault => true;
    public int Priority => 15;

    public WeatherReminderResult? Evaluate(WeatherReminderContext context)
    {
        if (string.IsNullOrEmpty(context.WeatherText)) return null;

        // 霾天气
        if (context.WeatherText.Contains("霾"))
        {
            return new WeatherReminderResult
            {
                RuleId = Id,
                Icon = DefaultIcon,
                Text = "霾天气，减少外出 😷",
                Priority = Priority,
                Category = "能见度"
            };
        }

        // 大雾天气
        if (context.WeatherText.Contains("雾"))
        {
            return new WeatherReminderResult
            {
                RuleId = Id,
                Icon = DefaultIcon,
                Text = "大雾天气，注意出行安全 🌫️",
                Priority = Priority,
                Category = "能见度"
            };
        }

        return null;
    }
}
