namespace HolidayCountdown.WeatherReminders.Rules;

/// <summary>
/// 气温骤降提醒：未来 1~2 天最高气温下降 ≥ 5°C。
/// </summary>
public class TempDropRule : IWeatherReminderRule
{
    public string Id => "temp-drop";
    public string Name => "气温骤降";
    public string DefaultIcon => "📉";
    public bool EnabledByDefault => true;
    public int Priority => 20;

    public WeatherReminderResult? Evaluate(WeatherReminderContext context)
    {
        var daily = WeatherDataHelper.GetDailyTemps(context.WeatherData, 3);
        if (daily.Count < 3) return null;

        var todayHigh = daily[0].High;
        var dayAfterHigh = daily[2].High;
        var diff = dayAfterHigh - todayHigh;

        if (diff <= -5)
        {
            return new WeatherReminderResult
            {
                RuleId = Id,
                Icon = DefaultIcon,
                Text = $"后天降温约{System.Math.Abs(diff)}°C",
                Priority = Priority,
                Category = "温度"
            };
        }

        return null;
    }
}
