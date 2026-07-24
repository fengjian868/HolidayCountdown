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
        if (string.IsNullOrEmpty(context.WeatherCode) && context.HourlyWeatherTexts.Count == 0)
            return null;

        // 当前已有降水则不提醒。
        // 旧实现用 IsPrecipitationText(WeatherText) 早出，但 BuildContext 之前根本没填充 WeatherText，
        // 导致"当前正在下雨"时仍可能误报"未来1小时内开始降雨"。
        // 改用代码 + 文本双路校验：当前 code 在降水区间或当前文本含"雨/雪/冰雹"就退出。
        if (IsCurrentRaining(context)) return null;

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

    static bool IsCurrentRaining(WeatherReminderContext context)
    {
        if (int.TryParse(context.WeatherCode, out var code) && WeatherDataHelper.IsPrecipitationCode(code))
            return true;
        if (!string.IsNullOrEmpty(context.WeatherText) && WeatherDataHelper.IsPrecipitationText(context.WeatherText))
            return true;
        return false;
    }
}
