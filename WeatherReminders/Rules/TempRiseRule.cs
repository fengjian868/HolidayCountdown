namespace HolidayCountdown.WeatherReminders.Rules;

/// <summary>
/// 气温骤升提醒：未来 1~2 天最高气温上升 ≥ 5°C。
/// </summary>
public class TempRiseRule : IWeatherReminderRule
{
    public string Id => "temp-rise";
    public string Name => "气温骤升";
    public string DefaultIcon => "📈";
    public bool EnabledByDefault => true;
    public int Priority => 20;

    public WeatherReminderResult? Evaluate(WeatherReminderContext context)
    {
        var daily = WeatherDataHelper.GetDailyTemps(context.DailyForecasts, 3);
        if (daily.Count < 3) return null;

        var todayHigh = daily[0].High;
        var dayAfterHigh = daily[2].High;
        var diff = dayAfterHigh - todayHigh;

        if (diff >= 5)
        {
            return new WeatherReminderResult
            {
                RuleId = Id,
                Icon = DefaultIcon,
                Text = $"后天升温约{diff}°C",
                Priority = Priority,
                Category = "温度"
            };
        }

        return null;
    }
}
