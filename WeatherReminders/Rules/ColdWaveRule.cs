namespace HolidayCountdown.WeatherReminders.Rules;

/// <summary>
/// 寒潮提醒：今日到明日气温骤降 8°C 以上。
/// </summary>
public class ColdWaveRule : IWeatherReminderRule
{
    public string Id => "cold-wave";
    public string Name => "寒潮提醒";
    public string DefaultIcon => "🥶";
    public bool EnabledByDefault => true;
    public int Priority => 12;

    public WeatherReminderResult? Evaluate(WeatherReminderContext context)
    {
        var dailyTemps = WeatherDataHelper.GetDailyTemps(context.WeatherInfo, 2);
        if (dailyTemps.Count < 2) return null;

        // 比较今天和明天的高温，判断是否骤降
        var todayHigh = dailyTemps[0].High;
        var tomorrowHigh = dailyTemps[1].High;
        if (todayHigh - tomorrowHigh >= 8)
        {
            return new WeatherReminderResult
            {
                RuleId = Id,
                Icon = DefaultIcon,
                Text = "寒潮来袭，气温骤降，注意保暖 🥶",
                Priority = Priority,
                Category = "温度"
            };
        }

        return null;
    }
}
