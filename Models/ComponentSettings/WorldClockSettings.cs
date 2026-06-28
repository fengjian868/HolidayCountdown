using System.Collections.Generic;

namespace HolidayCountdown.Models.ComponentSettings;

public class WorldClockCity
{
    public string Name { get; set; } = "";
    public string TimeZoneId { get; set; } = "";
}

public class WorldClockSettings
{
    public bool ShowSeconds { get; set; } = false;
    public bool ShowDate { get; set; } = false;
    public string TextColor { get; set; } = "#FFFFFFFF";
    public List<WorldClockCity> Cities { get; set; } = new()
    {
        new WorldClockCity { Name = "北京", TimeZoneId = "China Standard Time" }
    };
}
