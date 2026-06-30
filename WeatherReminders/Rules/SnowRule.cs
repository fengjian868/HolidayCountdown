using System.Linq;

namespace HolidayCountdown.WeatherReminders.Rules;

/// <summary>
/// 降雪提醒：当前或未来天气包含降雪。
/// </summary>
public class SnowRule : IWeatherReminderRule
{
    public string Id => "snow";
    public string Name => "降雪提醒";
    public string DefaultIcon => "❄️";
    public bool EnabledByDefault => true;
    public int Priority => 10;

    public WeatherReminderResult? Evaluate(WeatherReminderContext context)
    {
        // 当前天气文本包含"雪"
        if (!string.IsNullOrEmpty(context.WeatherText) && context.WeatherText.Contains("雪"))
        {
            return new WeatherReminderResult
            {
                RuleId = Id,
                Icon = DefaultIcon,
                Text = "正在下雪，注意防滑 ❄️",
                Priority = Priority,
                Category = "降水"
            };
        }

        // 检查逐小时天气代码是否包含降雪（小米天气代码 13~17 为降雪区间）
        var hourlyCodes = WeatherDataHelper.GetHourlyWeatherCodes(context.WeatherInfo, 24);
        if (hourlyCodes.Any(c => c >= 13 && c <= 17))
        {
            return new WeatherReminderResult
            {
                RuleId = Id,
                Icon = DefaultIcon,
                Text = "预计降雪，注意保暖 ❄️",
                Priority = Priority,
                Category = "降水"
            };
        }

        return null;
    }
}
