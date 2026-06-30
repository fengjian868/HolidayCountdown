using System;
using System.Collections.Generic;

namespace HolidayCountdown.Models;

public class PluginSettings
{
    // 全局
    public int Version { get; set; } = 123;
    public bool ExperimentalFeaturesEnabled { get; set; } = false;

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
    public bool AutoRefreshGreetings { get; set; } = true;
    public DateTime? LastGreetingRefreshDate { get; set; }
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
    public DateTime SummerStart { get; set; } = new DateTime(2026, 7, 1);
    public DateTime SummerEnd { get; set; } = new DateTime(2026, 8, 31);
    public DateTime WinterStart { get; set; } = new DateTime(2027, 1, 15);
    public DateTime WinterEnd { get; set; } = new DateTime(2027, 2, 13);
    public bool ShowVacationCountdown { get; set; } = true;

    // 节气颜色
    public Dictionary<string, string> TermColors { get; set; } = new();

    // 24节气
    public bool SolarTermShowProgressRing { get; set; } = true;

    // 天气问候
    public bool WeatherGreetingEnabled { get; set; } = true;
    public bool WeatherWarningOverride { get; set; } = true;
    public string? WeatherTemplate { get; set; } = "{icon}{temp} {greeting}";
    public bool WeatherShowTemp { get; set; } = true;
    public List<TempGreeting> TempGreetings { get; set; } = new();
    public List<WeatherGreetingItem> WeatherGreetingItems { get; set; } = new()
    {
        new WeatherGreetingItem { Keyword = "雨", Text = "记得带伞 ☔", Tag = "雨天" },
        new WeatherGreetingItem { Keyword = "小雨", Text = "毛毛雨，带把伞吧 🌧️", Tag = "雨天" },
        new WeatherGreetingItem { Keyword = "中雨", Text = "雨势渐大，注意安全 🌧️", Tag = "雨天" },
        new WeatherGreetingItem { Keyword = "大雨", Text = "大雨倾盆，别淋湿了 🌧️", Tag = "雨天" },
        new WeatherGreetingItem { Keyword = "暴雨", Text = "暴雨来袭，尽量别出门 ⛈️", Tag = "雨天" },
        new WeatherGreetingItem { Keyword = "阵雨", Text = "阵雨突袭，带伞防身 🌦️", Tag = "雨天" },
        new WeatherGreetingItem { Keyword = "雪", Text = "注意保暖 ❄️", Tag = "寒冷" },
        new WeatherGreetingItem { Keyword = "小雪", Text = "飘雪了，多穿点 ❄️", Tag = "寒冷" },
        new WeatherGreetingItem { Keyword = "大雪", Text = "大雪纷飞，注意防滑 ❄️", Tag = "寒冷" },
        new WeatherGreetingItem { Keyword = "晴", Text = "注意防暑 ☀️", Tag = "高温" },
        new WeatherGreetingItem { Keyword = "高温", Text = "注意防暑降温 🌡️", Tag = "高温" },
        new WeatherGreetingItem { Keyword = "阴", Text = "适合学习 📖", Tag = "舒适" },
        new WeatherGreetingItem { Keyword = "雾", Text = "注意安全 🌫️", Tag = "恶劣天气" },
        new WeatherGreetingItem { Keyword = "霾", Text = "减少外出 😷", Tag = "恶劣天气" },
        new WeatherGreetingItem { Keyword = "风", Text = "注意安全 🍃", Tag = "大风" },
        new WeatherGreetingItem { Keyword = "大风", Text = "风大，远离广告牌 🍃", Tag = "大风" },
        new WeatherGreetingItem { Keyword = "雷", Text = "注意安全 ⚡", Tag = "雷电" },
        new WeatherGreetingItem { Keyword = "雷阵雨", Text = "雷电交加，注意安全 ⛈️", Tag = "雷电" },
        new WeatherGreetingItem { Keyword = "云", Text = "舒适宜人 ⛅", Tag = "舒适" },
        new WeatherGreetingItem { Keyword = "多云", Text = "多云转晴，心情不错 ⛅", Tag = "舒适" },
        new WeatherGreetingItem { Keyword = "冰雹", Text = "冰雹来了，躲好别出门 🧊", Tag = "恶劣天气" },
        new WeatherGreetingItem { Keyword = "沙尘", Text = "沙尘天气，关好窗户 😷", Tag = "恶劣天气" },
        new WeatherGreetingItem { Keyword = "默认", Text = "{weather}", Tag = "默认" }
    };

    // 智能天气
    public string SmartWeatherTemplate { get; set; } = "{B} {A} {C} {D}";
    public bool SmartWeatherShowA { get; set; } = true;
    public bool SmartWeatherShowB { get; set; } = true;
    public bool SmartWeatherShowC { get; set; } = true;
    public bool SmartWeatherShowD { get; set; } = true;
    public bool SmartWeatherShowE { get; set; } = false;
    public bool SmartWeatherWarningOverride { get; set; } = true;
    public bool SmartWeatherTempColorEnabled { get; set; } = true;

    // 天气问候
    public bool WeatherShowIcon { get; set; } = true;
    public int WeatherGreetingRefreshMinutes { get; set; } = 10;

    // 天气变化提醒（测试版）
    public bool WeatherReminderEnabled { get; set; } = true;
    public int WeatherReminderRefreshMinutes { get; set; } = 5;
    public bool WeatherReminderShowImmediatelyOnChange { get; set; } = true;
    public List<string> EnabledWeatherReminderRuleIds { get; set; } = new();
    public int WeatherReminderRandomMinSeconds { get; set; } = 30;
    public int WeatherReminderRandomMaxSeconds { get; set; } = 60;

    // 课程表联动
    public bool ClassScheduleEnabled { get; set; } = true;
    public bool ClassScheduleShowIcon { get; set; } = true;
    public bool ClassScheduleShowSubject { get; set; } = true;
    public int PreClassMinutes { get; set; } = 5;
    public bool BreakWarningEnabled { get; set; } = true;
    public int BreakWarningMinutes { get; set; } = 3;
    public string BreakWarningColor { get; set; } = "#FFE53935";
    public List<NoClassTimeSlot> NoClassTimeSlots { get; set; } = new();
    public string NoClassMorningText { get; set; } = "上午好，暂无课程";
    public string NoClassNoonText { get; set; } = "中午好，暂无课程";
    public string NoClassAfternoonText { get; set; } = "下午好，暂无课程";
    public string NoClassEveningText { get; set; } = "晚上好，暂无课程";
    public string ClassScheduleTemplate { get; set; } = "当前:{curIcon}{curSubject} 下节:{nextIcon}{nextSubject} 本节还剩{curRemain}";
    public string ClassScheduleOnClassTemplate { get; set; } = "当前:{curIcon}{curSubject} 下节:{nextIcon}{nextSubject} 本节还剩{curRemain}";
    public string ClassScheduleBreakTemplate { get; set; } = "{breakIcon}课间休息还有{breakRemain} → 下节课是:{nextIcon}{nextSubject}";
    public string ClassSchedulePrepareTemplate { get; set; } = "{prepIcon}准备上课 → 下节课是:{nextIcon}{nextSubject} {prepRemain}";
    public string ClassScheduleAfterSchoolTemplate { get; set; } = "{afterIcon}放学了";
    public string ClassScheduleNoClassTemplate { get; set; } = "{noClassIcon}{text}";

    // 课程联动问候语
    public bool ClassGreetingEnabled { get; set; } = false;
    public string ClassGreetingOnClassTemplate { get; set; } = "正在上{A}，加油 📖";
    public string ClassGreetingBreakTemplate { get; set; } = "课间休息，下节{B} ☕";
    public string ClassGreetingPrepareTemplate { get; set; } = "准备上{B}，拿好课本 🔔";
    public string ClassGreetingAfterSchoolTemplate { get; set; } = "放学啦，今天辛苦了 🏠";
    public string ClassGreetingNoClassTemplate { get; set; } = "暂无课程，好好休息 📅";

    // 学习时长统计
    public bool StudyTimeEnabled { get; set; } = true;
    public bool StudyTimeShowIcon { get; set; } = true;
    public bool StudyTimeCountClassTimeOnly { get; set; } = false;
    public bool StudyTimeWeeklyReset { get; set; } = false;

    // 大考倒计时
    public int ExamType { get; set; } = 0; // 0 高考, 1 中考
    public string ExamCity { get; set; } = "北京";
    public string ExamCountdownTextColor { get; set; } = "#FF2196F3";
    public string ExamCountdownRingColor { get; set; } = "#FFFF5252";
    public bool ExamCountdownShowRing { get; set; } = true;
    public DateTime ExamCountdownRingStartDate { get; set; } = new DateTime(2025, 6, 9);
    public bool ExamCountdownShowBackground { get; set; } = true;
    public string ExamCountdownBackgroundColor { get; set; } = "#502196F3";
    public int ExamCountdownFontSize { get; set; } = 0;
    public string? ExamCountdownCustomDate { get; set; }
    public string ExamCountdownCustomText { get; set; } = "距离{A}还有{B}天";
    public string ExamCountdownTodayText { get; set; } = "今天就是{A}，加油！";
    public bool ExamCountdownRepeatYearly { get; set; } = true;

    // 世界时钟
    public bool WorldClockShowSeconds { get; set; } = false;
    public bool WorldClockShowDate { get; set; } = false;
    public string WorldClockTextColor { get; set; } = "#FFFFFFFF";
    public List<WorldClockCity> WorldClockCities { get; set; } = new()
    {
        new WorldClockCity { Name = "北京", TimeZoneId = "China Standard Time" }
    };
}

public class CustomHoliday
{
    public string Name { get; set; } = "";
    public DateTime Date { get; set; }
    public bool RepeatYearly { get; set; } = false;
}

public class NoClassTimeSlot
{
    public string Name { get; set; } = "";
    public int StartHour { get; set; }
    public int StartMinute { get; set; }
    public int EndHour { get; set; }
    public int EndMinute { get; set; }
    public string Text { get; set; } = "";
}

public class TimeSlotGreeting
{
    public int StartHour { get; set; }
    public int StartMinute { get; set; }
    public int EndHour { get; set; }
    public int EndMinute { get; set; }
    public string Text { get; set; } = "";
    public string Tag { get; set; } = "";
}

public class SpecialDateGreeting
{
    public string Name { get; set; } = "";
    public int DayOfWeek { get; set; } = 1;
    public int StartHour { get; set; } = 0;
    public int StartMinute { get; set; } = 0;
    public int EndHour { get; set; } = 23;
    public int EndMinute { get; set; } = 59;
    public string Text { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public string Tag { get; set; } = "";
}

public class WeatherGreetingItem
{
    public string Keyword { get; set; } = "";
    public string Text { get; set; } = "";
    public string Tag { get; set; } = "";
}

public class WorldClockCity
{
    public string Name { get; set; } = "";
    public string TimeZoneId { get; set; } = "";
}
