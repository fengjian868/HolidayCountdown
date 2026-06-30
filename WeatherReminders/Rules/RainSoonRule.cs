namespace HolidayCountdown.WeatherReminders.Rules;

/// <summary>
/// 即将降雨提醒：当前无雨，未来 2 小时内开始降雨。
/// </summary>
public class RainSoonRule : IWeatherReminderRule
{
    public string Id => "rain-soon";
    public string Name => "即将降雨";
    public string DefaultIcon => "🌧️";
    public bool EnabledByDefault => true;
    public int Priority => 10;

    public WeatherReminderResult? Evaluate(WeatherReminderContext context)
    {
        if (string.IsNullOrEmpty(context.WeatherCode) && string.IsNullOrEmpty(context.WeatherText))
            return null;

        // 当前已有降水则不提醒
        if (WeatherDataHelper.IsPrecipitationText(context.WeatherText)) return null;

        var hourly = WeatherDataHelper.GetHourlyWeatherCodes(context.WeatherInfo, 24);
        if (hourly.Count == 0) return null;

        // 找到未来 2 小时内首次出现降水的时刻
        for (int i = 0; i < hourly.Count && i < 2; i++)
        {
            if (WeatherDataHelper.IsPrecipitationCode(hourly[i]))
            {
                var when = i == 0 ? "未来1小时内" : $"约{i}小时后";
                return new WeatherReminderResult
                {
                    RuleId = Id,
                    Icon = DefaultIcon,
                    Text = $"{when}开始降雨",
                    Priority = Priority,
                    Category = "降雨"
                };
            }
        }

        return null;
    }
}
