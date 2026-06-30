using System.Linq;

namespace HolidayCountdown.WeatherReminders.Rules;

/// <summary>
/// 带伞提醒：未来 6 小时内可能降雨。
/// </summary>
public class UmbrellaRule : IWeatherReminderRule
{
    public string Id => "umbrella";
    public string Name => "带伞提醒";
    public string DefaultIcon => "☂️";
    public bool EnabledByDefault => true;
    public int Priority => 9;

    public WeatherReminderResult? Evaluate(WeatherReminderContext context)
    {
        // 检查未来 6 小时的天气代码是否包含降水
        var hourlyCodes = WeatherDataHelper.GetHourlyWeatherCodes(context.WeatherInfo, 6);
        bool rainInCodes = hourlyCodes.Any(c => WeatherDataHelper.IsPrecipitationCode(c));

        // 检查未来 6 小时的天气文本是否包含降水
        var hourlyTexts = WeatherDataHelper.GetHourlyWeatherTexts(context.WeatherInfo, 6);
        bool rainInTexts = hourlyTexts.Any(t => WeatherDataHelper.IsPrecipitationText(t));

        if (rainInCodes || rainInTexts)
        {
            return new WeatherReminderResult
            {
                RuleId = Id,
                Icon = DefaultIcon,
                Text = "未来几小时可能降雨，记得带伞 ☂️",
                Priority = Priority,
                Category = "降雨"
            };
        }

        return null;
    }
}
