namespace HolidayCountdown.WeatherReminders.Rules;

/// <summary>
/// 湿度提醒：湿度过高或过低时给出建议。
/// </summary>
public class HumidityRule : IWeatherReminderRule
{
    public string Id => "humidity";
    public string Name => "湿度提醒";
    public string DefaultIcon => "💧";
    public bool EnabledByDefault => false;
    public int Priority => 28;

    public WeatherReminderResult? Evaluate(WeatherReminderContext context)
    {
        if (context.WeatherInfo == null) return null;

        // 尝试读取湿度数据
        var current = WeatherDataHelper.GetPropertyValue(context.WeatherInfo, "Current");
        if (current == null) return null;

        // 优先读取 RelativeHumidity，其次读取 Humidity
        var humidityObj = WeatherDataHelper.GetPropertyValue(current, "RelativeHumidity")
                         ?? WeatherDataHelper.GetPropertyValue(current, "Humidity");
        if (humidityObj == null) return null;

        if (!double.TryParse(humidityObj.ToString(), out var humidity)) return null;

        if (humidity >= 80)
        {
            return new WeatherReminderResult
            {
                RuleId = Id,
                Icon = DefaultIcon,
                Text = "湿度较大，注意防潮 💧",
                Priority = Priority,
                Category = "湿度"
            };
        }

        if (humidity <= 30)
        {
            return new WeatherReminderResult
            {
                RuleId = Id,
                Icon = DefaultIcon,
                Text = "空气干燥，注意补水 💧",
                Priority = Priority,
                Category = "湿度"
            };
        }

        return null;
    }
}
