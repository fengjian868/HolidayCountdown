namespace HolidayCountdown.Models.ComponentSettings;

public class LunarDateSettings
{
    public string Template { get; set; } = "{gzYear} {IMonthCn}{IDayCn} {Animal}";
    public bool AutoRefresh { get; set; } = true;
}
