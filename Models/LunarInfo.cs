using System;
using System.Collections.Generic;

namespace HolidayCountdown.Models;

public class LunarInfo
{
    public DateTime Date { get; set; }
    public string gzYear { get; set; } = "";
    public string IMonthCn { get; set; } = "";
    public string IDayCn { get; set; } = "";
    public string Animal { get; set; } = "";
    public string Term { get; set; } = "";
    public string lunarDate { get; set; } = "";
}

/// <summary>
/// 农历月度缓存，缓存一个月的农历数据，每日自动刷新
/// </summary>
public class LunarMonthCache
{
    public string Month { get; set; } = "";
    public DateTime LastRefresh { get; set; }
    public List<LunarInfo> Data { get; set; } = new();
}
