namespace HolidayCountdown.Models.ComponentSettings;

public class ExamCountdownSettings
{
    /// <summary>
    /// 0=高考，1=中考
    /// </summary>
    public int ExamType { get; set; } = 0;

    /// <summary>
    /// 城市名称，用于匹配内置考试时间
    /// </summary>
    public string City { get; set; } = "北京";

    /// <summary>
    /// 是否每年重复（自动进入下一年考试周期）
    /// </summary>
    public bool RepeatYearly { get; set; } = true;

    /// <summary>
    /// 前景文字颜色
    /// </summary>
    public string TextColor { get; set; } = "#FF2196F3";

    /// <summary>
    /// 倒计时圆环颜色
    /// </summary>
    public string RingColor { get; set; } = "#FFFF5252";

    /// <summary>
    /// 是否显示倒计时圆环
    /// </summary>
    public bool ShowRing { get; set; } = true;

    /// <summary>
    /// 圆环开始日期（MM-dd），默认8月1日
    /// </summary>
    public string RingStartDate { get; set; } = "08-01";

    /// <summary>
    /// 是否显示背景色块
    /// </summary>
    public bool ShowBackground { get; set; } = true;

    /// <summary>
    /// 背景色
    /// </summary>
    public string BackgroundColor { get; set; } = "#202196F3";

    /// <summary>
    /// 自定义考试日期（留空则使用内置数据）
    /// </summary>
    public string? CustomDate { get; set; }

    /// <summary>
    /// 自定义显示文案
    /// </summary>
    public string CustomText { get; set; } = "距离{exam}还有{days}天";

    /// <summary>
    /// 考试当天显示文案
    /// </summary>
    public string TodayText { get; set; } = "今天就是{exam}，加油！";
}
