using System;

namespace HolidayCountdown.Models;

public class Holiday
{
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int DaysOff { get; set; } = 1;
    public bool IsWorkday { get; set; } = false;
}