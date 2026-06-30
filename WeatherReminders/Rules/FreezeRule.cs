namespace HolidayCountdown.WeatherReminders.Rules;

/// <summary>
/// 冰冻提醒：气温 ≤ 0°C 或出现冻雨/冰。
/// </summary>
public class FreezeRule : IWeatherReminderRule
{
    public string Id => "freeze";
    public string Name => "冰冻提醒";
    public string DefaultIcon => "🧊";
    public bool EnabledByDefault => true;
    public int Priority => 13;

    public WeatherReminderResult? Evaluate(WeatherReminderContext context)
    {
        // 冻雨天气
        if (!string.IsNullOrEmpty(context.WeatherText) && context.WeatherText.Contains("冻雨"))
        {
            return new WeatherReminderResult
            {
                RuleId = Id,
                Icon = DefaultIcon,
                Text = "冻雨天气，避免外出 🧊",
                Priority = Priority,
                Category = "冰冻"
            };
        }

        // 天气文本包含"冰"
        if (!string.IsNullOrEmpty(context.WeatherText) && context.WeatherText.Contains("冰"))
        {
            return new WeatherReminderResult
            {
                RuleId = Id,
                Icon = DefaultIcon,
                Text = "路面可能结冰，注意防滑 🧊",
                Priority = Priority,
                Category = "冰冻"
            };
        }

        // 气温 ≤ 0°C，提示路面可能结冰
        var temp = WeatherDataHelper.GetCurrentTemp(context.WeatherInfo);
        if (temp.HasValue && temp.Value <= 0)
        {
            return new WeatherReminderResult
            {
                RuleId = Id,
                Icon = DefaultIcon,
                Text = "路面可能结冰，注意防滑 🧊",
                Priority = Priority,
                Category = "冰冻"
            };
        }

        return null;
    }
}
