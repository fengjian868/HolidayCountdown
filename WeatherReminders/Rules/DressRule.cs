namespace HolidayCountdown.WeatherReminders.Rules;

/// <summary>
/// 穿衣指数：根据当前温度推荐穿衣。
/// </summary>
public class DressRule : IWeatherReminderRule
{
    public string Id => "dress";
    public string Name => "穿衣指数";
    public string DefaultIcon => "👔";
    public bool EnabledByDefault => false;
    public int Priority => 30;

    public WeatherReminderResult? Evaluate(WeatherReminderContext context)
    {
        var temp = WeatherDataHelper.GetCurrentTemp(context.WeatherInfo);
        if (!temp.HasValue) return null;

        string text = temp.Value switch
        {
            >= 30 => "炎热，穿短袖 👔",
            >= 20 => "舒适，薄外套 👔",
            >= 10 => "较冷，厚外套 👔",
            _     => "寒冷，穿棉衣 👔"
        };

        return new WeatherReminderResult
        {
            RuleId = Id,
            Icon = DefaultIcon,
            Text = text,
            Priority = Priority,
            Category = "生活指数"
        };
    }
}
