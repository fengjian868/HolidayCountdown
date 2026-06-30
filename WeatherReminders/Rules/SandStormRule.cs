namespace HolidayCountdown.WeatherReminders.Rules;

/// <summary>
/// 沙尘提醒：天气文本包含沙尘、扬沙或浮尘。
/// </summary>
public class SandStormRule : IWeatherReminderRule
{
    public string Id => "sandstorm";
    public string Name => "沙尘提醒";
    public string DefaultIcon => "💨";
    public bool EnabledByDefault => true;
    public int Priority => 14;

    public WeatherReminderResult? Evaluate(WeatherReminderContext context)
    {
        if (string.IsNullOrEmpty(context.WeatherText)) return null;

        if (context.WeatherText.Contains("沙尘") ||
            context.WeatherText.Contains("扬沙") ||
            context.WeatherText.Contains("浮尘"))
        {
            return new WeatherReminderResult
            {
                RuleId = Id,
                Icon = DefaultIcon,
                Text = "沙尘天气，关好门窗 💨",
                Priority = Priority,
                Category = "沙尘"
            };
        }

        return null;
    }
}
