using System;
using System.Collections.Generic;

namespace HolidayCountdown.Models;

public class PluginSettings
{
    // 全局
    public int Version { get; set; } = 120;

    // 节假日组件
    public int DisplayCount { get; set; } = 3;
    public bool ShowWorkdayReminder { get; set; } = true;
    public int WorkdayReminderDays { get; set; } = 7;
    public bool ShowWeekendCountdown { get; set; } = false;
    public bool ShowDaysOff { get; set; } = false;
    public bool ShowHours { get; set; } = false;
    public bool ShowProgressRing { get; set; } = true;
    public bool AutoHolidayColor { get; set; } = true;
    public bool AutoNextHoliday { get; set; } = true;
    public bool MergeGreeting { get; set; } = false;
    public bool ShowYearRatio { get; set; } = true;
    public Dictionary<string, string> HolidayColors { get; set; } = new();
    public List<string> DisabledHolidays { get; set; } = new();

    // 问候语
    public bool ShowGreeting { get; set; } = true;
    public List<TimeSlotGreeting> TimeSlotGreetings { get; set; } = new();
    public List<SpecialDateGreeting> SpecialDateGreetings { get; set; } = new();
    public Dictionary<string, string> SpecialGreetings { get; set; } = new();
    public int SchoolEndHour { get; set; } = 17;
    public int SchoolEndMinute { get; set; } = 30;
    public int SchoolEndReminderMinutes { get; set; } = 5;
    public string BeforeSchoolEndText { get; set; } = "再坚持一下就能run了！";
    public string AfterSchoolEndText { get; set; } = "run！";
    public bool ShowSundayEveningStudy { get; set; } = true;
    public string SundayEveningStudyText { get; set; } = "今晚有晚修，记得按时到教室！";
    // 每周提醒：自定义日期（1=周一, 7=周日）
    public int WeeklyReminderDay { get; set; } = 7;
    public int WeeklyReminderStartHour { get; set; } = 17;
    public int WeeklyReminderEndHour { get; set; } = 21;
    public bool WeeklyReminderEnabled { get; set; } = true;

    // 农历
    public bool ShowLunarDate { get; set; } = true;
    public string LunarDateTemplate { get; set; } = "{gzYear} {IMonthCn}{IDayCn} {Animal}";
    public bool LunarAutoRefresh { get; set; } = true;

    // 自定义节日组件
    public int CustomHolidayDisplayCount { get; set; } = 3;
    public bool CustomHolidayShowIcon { get; set; } = true;
    public bool CustomHolidayShowDays { get; set; } = true;
    public List<CustomHoliday> CustomHolidays { get; set; } = new();

    // 寒暑假
    public DateTime SummerStart { get; set; } = new DateTime(2025, 7, 1);
    public DateTime SummerEnd { get; set; } = new DateTime(2025, 8, 31);
    public DateTime WinterStart { get; set; } = new DateTime(2026, 1, 15);
    public DateTime WinterEnd { get; set; } = new DateTime(2026, 2, 13);
    public bool ShowVacationCountdown { get; set; } = true;

    // 节气
    public bool SolarTermShowProgressRing { get; set; } = true;
    public Dictionary<string, string> TermColors { get; set; } = new();

    // 天气问候
    public bool WeatherGreetingEnabled { get; set; } = true;
    public string WeatherTemplate { get; set; } = "{greeting}";
    public bool WeatherShowIcon { get; set; } = true;
    public bool WeatherShowTemp { get; set; } = true;
    public List<TempGreeting> TempGreetings { get; set; } = new();
    public Dictionary<string, string> WeatherGreetings { get; set; } = new()
    {
        ["雨"] = "记得带伞 ☔",
        ["小雨"] = "毛毛雨，带把伞吧 🌧️",
        ["中雨"] = "雨势渐大，注意安全 🌧️",
        ["大雨"] = "大雨倾盆，别淋湿了 🌧️",
        ["暴雨"] = "暴雨来袭，尽量别出门 ⛈️",
        ["雪"] = "注意保暖 ❄️",
        ["小雪"] = "飘雪了，多穿点 ❄️",
        ["大雪"] = "大雪纷飞，注意防滑 ❄️",
        ["晴"] = "注意防暑 ☀️",
        ["高温"] = "注意防暑降温 🌡️",
        ["阴"] = "适合学习 📖",
        ["雾"] = "注意安全 🌫️",
        ["霾"] = "减少外出 😷",
        ["风"] = "注意安全 🍃",
        ["大风"] = "风大，远离广告牌 🍃",
        ["雷"] = "注意安全 ⚡",
        ["雷阵雨"] = "雷电交加，注意安全 ⛈️",
        ["云"] = "舒适宜人 ⛅",
        ["多云"] = "多云转晴，心情不错 ⛅",
        ["阵雨"] = "阵雨突袭，带伞防身 🌦️",
        ["冰雹"] = "冰雹来了，躲好别出门 🧊",
        ["沙尘"] = "沙尘天气，关好窗户 😷",
        ["默认"] = "{weather}"
    };
}

public class CustomHoliday
{
    public string Name { get; set; } = "";
    public DateTime Date { get; set; }
    public bool RepeatYearly { get; set; } = false;
}

public class TimeSlotGreeting
{
    public int StartHour { get; set; }
    public int StartMinute { get; set; }
    public int EndHour { get; set; }
    public int EndMinute { get; set; }
    public string Text { get; set; } = "";
}

public class SpecialDateGreeting
{
    public string Name { get; set; } = "";
    public int DayOfWeek { get; set; } = 1; // 1=周一, 7=周日
    public int StartHour { get; set; } = 0;
    public int StartMinute { get; set; } = 0;
    public int EndHour { get; set; } = 23;
    public int EndMinute { get; set; } = 59;
    public string Text { get; set; } = "";
    public bool Enabled { get; set; } = true;
}

public class TempGreeting
{
    public int MinTemp { get; set; }
    public int MaxTemp { get; set; }
    public string Text { get; set; } = "";
}
