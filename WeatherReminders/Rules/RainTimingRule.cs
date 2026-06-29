namespace HolidayCountdown.WeatherReminders.Rules;

/// <summary>
/// 降雨时间提醒：当前下雨则提示多久停雨，当前无雨则提示多久后下雨。
/// </summary>
public class RainTimingRule : IWeatherReminderRule
{
    public string Id => "rain-timing";
    public string Name => "降雨时间";
    public string DefaultIcon => "🌧️";
    public bool EnabledByDefault => true;
    public int Priority => 8;

    public WeatherReminderResult? Evaluate(WeatherReminderContext context)
    {
        if (context.WeatherInfo == null) return null;

        var hourlyCodes = WeatherDataHelper.GetHourlyWeatherCodes(context.WeatherInfo, 24);
        var hourlyTexts = WeatherDataHelper.GetHourlyWeatherTexts(context.WeatherInfo, 24);
        var maxHours = Math.Max(hourlyCodes.Count, hourlyTexts.Count);
        if (maxHours == 0) return null;

        bool IsRainingNow()
        {
            if (!string.IsNullOrEmpty(context.WeatherText) && IsRainingText(context.WeatherText)) return true;
            if (hourlyCodes.Count > 0 && IsRainingCode(hourlyCodes[0])) return true;
            if (hourlyTexts.Count > 0 && IsRainingText(hourlyTexts[0])) return true;
            return false;
        }

        if (IsRainingNow())
        {
            // 找未来首次停雨的时刻
            for (int i = 1; i < maxHours; i++)
            {
                var code = i < hourlyCodes.Count ? hourlyCodes[i] : (int?)null;
                var text = i < hourlyTexts.Count ? hourlyTexts[i] : null;
                if (!IsRainingCode(code) && !IsRainingText(text))
                {
                    var when = i == 1 ? "1小时后停雨" : $"{i}小时后停雨";
                    return new WeatherReminderResult
                    {
                        RuleId = Id,
                        Icon = "🌂",
                        Text = when,
                        Priority = Priority,
                        Category = "降雨"
                    };
                }
            }
            return new WeatherReminderResult
            {
                RuleId = Id,
                Icon = DefaultIcon,
                Text = "将持续降雨",
                Priority = Priority,
                Category = "降雨"
            };
        }
        else
        {
            // 找未来首次降雨的时刻
            for (int i = 0; i < maxHours; i++)
            {
                var code = i < hourlyCodes.Count ? hourlyCodes[i] : (int?)null;
                var text = i < hourlyTexts.Count ? hourlyTexts[i] : null;
                if (IsRainingCode(code) || IsRainingText(text))
                {
                    var when = i == 0 ? "即将下雨" : $"{i}小时后下雨";
                    return new WeatherReminderResult
                    {
                        RuleId = Id,
                        Icon = "🌂",
                        Text = when,
                        Priority = Priority,
                        Category = "降雨"
                    };
                }
            }
        }

        return null;
    }

    static bool IsRainingText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        return text.Contains("雨") || text.Contains("雪") || text.Contains("冰雹");
    }

    static bool IsRainingCode(int? code)
    {
        return code.HasValue && code.Value >= 8 && code.Value <= 19;
    }
}
