namespace HolidayCountdown.WeatherReminders.Rules;

/// <summary>
/// 高温防暑提醒：当前温度 ≥ 35°C。
/// </summary>
public class HeatRule : IWeatherReminderRule
{
    public string Id => "heat";
    public string Name => "高温防暑";
    public string DefaultIcon => "🌡️";
    public bool EnabledByDefault => true;
    public int Priority => 30;

    public WeatherReminderResult? Evaluate(WeatherReminderContext context)
    {
        if (context.CurrentTemp >= 35)
        {
            return new WeatherReminderResult
            {
                RuleId = Id,
                Icon = DefaultIcon,
                Text = $"高温{context.CurrentTemp.Value:0}°C，注意防暑",
                Priority = Priority,
                Category = "温度"
            };
        }

        return null;
    }
}
