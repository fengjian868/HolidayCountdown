using System;
using System.Collections.Generic;
using System.Linq;

namespace HolidayCountdown.Models;

/// <summary>
/// 本地问候语数据库，按标签分类，每天根据标签随机刷新
/// </summary>
public static class LocalGreetingDB
{
    /// <summary>
    /// 时段问候语 - 按标签分类
    /// </summary>
    public static readonly Dictionary<string, List<string>> TimeSlotGreetings = new()
    {
        ["早晨"] = new()
        {
            "早啊，今天也要加油 💪",
            "新的一天，从微笑开始 😊",
            "早安！愿今天一切顺利 🌅",
            "起床啦，太阳晒屁股了 ☀️",
            "早上好！今天也要元气满满 ✨",
            "清晨的阳光，是最好的鼓励 🌄",
            "又是崭新的一天，冲鸭 🦆",
            "早安，今天的目标是比昨天更好 📈",
            "新的一天，新的可能 🌟",
            "早起鸟儿有虫吃 🐦",
            "晨光正好，不负韶华 🌿",
            "早安！今天也要做最棒的自己 🏆"
        },
        ["上午"] = new()
        {
            "上午好，距离午休还有几节课",
            "上午时光，效率最高 📚",
            "专注当下，上午加油 💪",
            "上午好！保持专注 🎯",
            "上午的课要认真听哦 📖",
            "上午好，今天进度不错 👍",
            "上午是黄金时间，别浪费 ⏰",
            "上午好！一鼓作气冲过去 🚀",
            "上午的时光最适合学习 📝",
            "上午好，保持节奏 💯"
        },
        ["中午"] = new()
        {
            "吃饭时间到！🍚",
            "午休时间，好好休息 😴",
            "中午了，记得吃饭 🍜",
            "午餐时间，补充能量 🍱",
            "中午好！吃饱了才有力气 💪",
            "午间小憩，下午更有精神 🛋️",
            "干饭时间！🍚🍚🍚",
            "中午记得午睡一会儿 😌",
            "午餐要吃好，下午才不困 🥗",
            "中午好，适当休息一下 ☕"
        },
        ["下午"] = new()
        {
            "下午容易犯困，坚持住 😪",
            "下午好！来杯茶提提神 🍵",
            "下午的课也要认真哦 📖",
            "下午好，再坚持一下 💪",
            "午后时光，别打瞌睡 😴",
            "下午好！距离放学又近了一步 🏃",
            "下午容易走神，集中注意力 🎯",
            "下午好，喝口水活动活动 💧",
            "下午的阳光，暖洋洋的 🌤️",
            "下午好！坚持就是胜利 ✊"
        },
        ["傍晚"] = new()
        {
            "再坚持一下就能run了！",
            "傍晚了，快放学了 🌆",
            "日落时分，一天快结束了 🌇",
            "傍晚好！辛苦了一天 🌙",
            "黄昏时刻，回家路上注意安全 🚶",
            "傍晚了，今天辛苦了 💪",
            "夕阳西下，又是充实的一天 🌅",
            "傍晚好！今天的你很棒 ⭐",
            "天快黑了，注意安全回家 🏠",
            "傍晚好，放松一下吧 🎵"
        },
        ["夜晚"] = new()
        {
            "夜猫子模式启动 🦉",
            "晚上好，记得早点休息 🌙",
            "夜深了，别太晚睡 😴",
            "晚上好！今天辛苦了 💫",
            "夜晚是思考的好时光 🤔",
            "晚安前再看看明天的课表 📋",
            "晚上好，适当放松 🎮",
            "夜色已深，早睡早起身体好 💤",
            "晚上好！明天继续加油 💪",
            "夜深了，放下手机休息吧 📱❌",
            "夜晚的宁静，适合反思 🌃",
            "晚安，明天见 🌙✨"
        }
    };

    /// <summary>
    /// 每周提醒 - 按标签分类
    /// </summary>
    public static readonly Dictionary<string, List<string>> WeeklyReminders = new()
    {
        ["周一"] = new()
        {
            "本周还有 5 天到周末 😭",
            "周一综合征，挺住 💪",
            "新的一周，新的开始 🌅",
            "周一加油，万事开头难 🚀",
            "周一了，调整状态冲 🏃",
            "周一不哭，周末会来的 😊"
        },
        ["周二"] = new()
        {
            "周二了，习惯节奏了吧 💪",
            "周二，离周末又近了一天 📅",
            "周二加油，最长的周二也能过 🏔️",
            "周二好！保持节奏 🎵"
        },
        ["周三"] = new()
        {
            "周三了，过半了！📈",
            "周三，一周的转折点 🔄",
            "熬过周三就是下半场了 ⚽",
            "周三好！曙光就在前方 🌅",
            "周三了，周末在向你招手 👋"
        },
        ["周四"] = new()
        {
            "周四了，胜利在望 🏆",
            "周四！明天就是周五了 🎉",
            "周四加油，再撑一天 💪",
            "周四好！快到终点了 🏁"
        },
        ["周五"] = new()
        {
            "周五周五，敲锣打鼓 🥁",
            "周五了！胜利就在眼前 🎊",
            "周五快乐！再坚持半天 🌈",
            "周五！周末我来啦 🏃💨",
            "周五好！今天效率最高 💯",
            "周五了，今天的心情格外好 😄"
        },
        ["周末"] = new()
        {
            "享受假期吧 🎉",
            "周末愉快！好好休息 😌",
            "周末到了，放松一下 🎮",
            "周末好！睡个懒觉 😴",
            "周末快乐！今天想做什么 🤔",
            "周末模式已开启 🔋"
        }
    };

    /// <summary>
    /// 温度问候语 - 按温度区间分类
    /// </summary>
    public static readonly List<TempGreeting> DefaultTempGreetings = new()
    {
        new() { MinTemp = 35, MaxTemp = 999, Text = "高温预警，注意防暑 🌡️" },
        new() { MinTemp = 30, MaxTemp = 35, Text = "很热，穿短袖注意防晒 ☀️" },
        new() { MinTemp = 25, MaxTemp = 30, Text = "较热，短袖即可 👕" },
        new() { MinTemp = 20, MaxTemp = 25, Text = "舒适，薄长袖或短袖 🍃" },
        new() { MinTemp = 15, MaxTemp = 20, Text = "微凉，建议穿外套 🧥" },
        new() { MinTemp = 10, MaxTemp = 15, Text = "较冷，穿厚外套 🧣" },
        new() { MinTemp = 5, MaxTemp = 10, Text = "冷，穿羽绒服或棉衣 ❄️" },
        new() { MinTemp = 0, MaxTemp = 5, Text = "很冷，注意保暖 🥶" },
        new() { MinTemp = -999, MaxTemp = 0, Text = "严寒，多穿点别冻着 🧊" }
    };

    static readonly Random _rng = new();

    /// <summary>
    /// 根据标签获取今日问候语（同一天同一标签返回同一条）
    /// </summary>
    public static string GetDaily(string tag, Dictionary<string, List<string>> db)
    {
        if (!db.TryGetValue(tag, out var list) || list.Count == 0) return "";
        // 用日期作为种子，保证同一天同一标签返回同一条
        var seed = DateTime.Now.Year * 10000 + DateTime.Now.Month * 100 + DateTime.Now.Day + tag.GetHashCode();
        var rng = new Random(seed);
        return list[rng.Next(list.Count)];
    }

    /// <summary>
    /// 根据星期几获取每周提醒标签
    /// </summary>
    public static string GetDayOfWeekTag(DayOfWeek dow) => dow switch
    {
        DayOfWeek.Monday => "周一",
        DayOfWeek.Tuesday => "周二",
        DayOfWeek.Wednesday => "周三",
        DayOfWeek.Thursday => "周四",
        DayOfWeek.Friday => "周五",
        _ => "周末"
    };

    /// <summary>
    /// 根据小时获取时段标签
    /// </summary>
    public static string GetTimeSlotTag(int hour) => hour switch
    {
        >= 5 and < 8 => "早晨",
        >= 8 and < 12 => "上午",
        >= 12 and < 14 => "中午",
        >= 14 and < 17 => "下午",
        >= 17 and < 19 => "傍晚",
        _ => "夜晚"
    };
}
