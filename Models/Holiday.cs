using System;

namespace HolidayCountdown.Models;

public class Holiday
{
    public string Name { get; set; } = "";
    public DateTime Date { get; set; }
    public int DaysOff { get; set; } = 1;
    public bool IsWorkday { get; set; } = false;
    public bool IsCustom { get; set; } = false;
    public bool IsEnabled { get; set; } = true;
    public string CustomColor { get; set; } = "";
}
