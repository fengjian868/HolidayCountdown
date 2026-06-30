namespace HolidayCountdown.WeatherReminders.Rules;

/// <summary>
/// 附近有闪电提醒：基于预警或小时预报未来 2 小时内有雷。
/// </summary>
public class LightningNearbyRule : IWeatherReminderRule
{
    public string Id => "lightning-nearby";
    public string Name => "附近有闪电";
    public string DefaultIcon => "⚡";
    public bool EnabledByDefault => true;
    public int Priority => 5;

    public WeatherReminderResult? Evaluate(WeatherReminderContext context)
    {
        // 优先根据预警判断
        if (WeatherDataHelper.AlertContainsAny(context.Alerts, "雷", "电", "雷雨", "雷阵雨"))
        {
            return new WeatherReminderResult
            {
                RuleId = Id,
                Icon = DefaultIcon,
                Text = "附近有闪电",
                Priority = Priority,
                Category = "雷电"
            };
        }

        // 根据小时预报判断未来 2 小时是否有雷阵雨天气代码（通常 10~12 为雷暴/雷阵雨）
        var hourly = WeatherDataHelper.GetHourlyWeatherCodes(context.WeatherInfo, 24);
        for (int i = 0; i < hourly.Count && i < 2; i++)
        {
            if (hourly[i] is >= 10 and <= 12)
            {
                return new WeatherReminderResult
                {
                    RuleId = Id,
                    Icon = DefaultIcon,
                    Text = i == 0 ? "附近有闪电" : $"约{i}小时后有闪电",
                    Priority = Priority,
                    Category = "雷电"
                };
            }
        }

        return null;
    }
}
