using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using HolidayCountdown.Models;
using Avalonia.Media;

namespace HolidayCountdown.Services;

public class HolidayService
{
    private readonly List<Holiday> _builtIn;
    private List<Holiday> _holidays = new();
    private readonly string _cachePath, _settingsPath, _lunarCachePath, _lunarYearCachePath, _lunarMonthCachePath;
    private static bool _netTried;
    private static readonly object _netLock = new();
    private static bool _netLoaded;

    public PluginSettings Settings { get; set; } = new();

    /// <summary>
    /// 设置变更事件，保存设置后触发，通知所有组件刷新
    /// </summary>
    public static event Action? SettingsChanged;

    public HolidayService()
    {
        _builtIn = LoadBuiltIn();
        _holidays = new List<Holiday>(_builtIn);
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClassIsland", "Plugins", "HolidayCountdown");
        Directory.CreateDirectory(dir);
        _cachePath = Path.Combine(dir, "holiday_cache.json");
        _settingsPath = Path.Combine(dir, "settings.json");
        _lunarCachePath = Path.Combine(dir, "lunar_cache.json");
        _lunarYearCachePath = Path.Combine(dir, "lunar_year_cache.json");
        _lunarMonthCachePath = Path.Combine(dir, "lunar_month_cache.json");
        LoadSettings();
        if (CacheValid()) { var c = LoadCache(); if (c.Count > 0) _holidays = c; }
        if (!_netTried)
        {
            lock (_netLock)
            {
                if (!_netTried) { _netTried = true; _ = Task.Run(async () => await RefreshNetAsync()); }
            }
        }
    }

    public void LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var loaded = JsonSerializer.Deserialize<PluginSettings>(json);
                if (loaded != null) Settings = loaded;
            }
        }
        catch { }
        InitDefaults();
    }

    void InitDefaults()
    {
        if (Settings.TimeSlotGreetings.Count == 0)
        {
            Settings.TimeSlotGreetings.Add(new TimeSlotGreeting { StartHour = 5, StartMinute = 0, EndHour = 8, EndMinute = 0, Text = "", Tag = "早晨" });
            Settings.TimeSlotGreetings.Add(new TimeSlotGreeting { StartHour = 8, StartMinute = 0, EndHour = 12, EndMinute = 0, Text = "", Tag = "上午" });
            Settings.TimeSlotGreetings.Add(new TimeSlotGreeting { StartHour = 12, StartMinute = 0, EndHour = 14, EndMinute = 0, Text = "", Tag = "中午" });
            Settings.TimeSlotGreetings.Add(new TimeSlotGreeting { StartHour = 14, StartMinute = 0, EndHour = 17, EndMinute = 0, Text = "", Tag = "下午" });
            Settings.TimeSlotGreetings.Add(new TimeSlotGreeting { StartHour = 17, StartMinute = 0, EndHour = 19, EndMinute = 0, Text = "", Tag = "傍晚" });
            Settings.TimeSlotGreetings.Add(new TimeSlotGreeting { StartHour = 19, StartMinute = 0, EndHour = 23, EndMinute = 59, Text = "", Tag = "晚上" });
        }
        if (Settings.SpecialDateGreetings.Count == 0)
        {
            Settings.SpecialDateGreetings.Add(new SpecialDateGreeting { Name = "周一早晨", DayOfWeek = 1, StartHour = 0, StartMinute = 0, EndHour = 12, EndMinute = 0, Text = "", Tag = "周一" });
            Settings.SpecialDateGreetings.Add(new SpecialDateGreeting { Name = "周三", DayOfWeek = 3, StartHour = 0, StartMinute = 0, EndHour = 23, EndMinute = 59, Text = "", Tag = "周三" });
            Settings.SpecialDateGreetings.Add(new SpecialDateGreeting { Name = "周五下午", DayOfWeek = 5, StartHour = 12, StartMinute = 0, EndHour = 23, EndMinute = 59, Text = "", Tag = "周五" });
            Settings.SpecialDateGreetings.Add(new SpecialDateGreeting { Name = "周末", DayOfWeek = 6, StartHour = 0, StartMinute = 0, EndHour = 23, EndMinute = 59, Text = "", Tag = "周六" });
            Settings.SpecialDateGreetings.Add(new SpecialDateGreeting { Name = "周末", DayOfWeek = 7, StartHour = 0, StartMinute = 0, EndHour = 23, EndMinute = 59, Text = "", Tag = "周日" });
        }
        if (Settings.HolidayColors.Count == 0)
        {
            Settings.HolidayColors["春节"] = "#FFD700";
            Settings.HolidayColors["清明"] = "#4CAF50";
            Settings.HolidayColors["端午"] = "#2E7D32";
            Settings.HolidayColors["中秋"] = "#FF9800";
            Settings.HolidayColors["国庆"] = "#F44336";
            Settings.HolidayColors["元旦"] = "#9C27B0";
            Settings.HolidayColors["劳动"] = "#E91E63";
        }
        if (Settings.TermColors.Count == 0)
        {
            Settings.TermColors["立春"] = "#4CAF50"; Settings.TermColors["雨水"] = "#4CAF50"; Settings.TermColors["惊蛰"] = "#4CAF50";
            Settings.TermColors["春分"] = "#66BB6A"; Settings.TermColors["清明"] = "#66BB6A"; Settings.TermColors["谷雨"] = "#66BB6A";
            Settings.TermColors["立夏"] = "#FF9800"; Settings.TermColors["小满"] = "#FF9800"; Settings.TermColors["芒种"] = "#FF9800";
            Settings.TermColors["夏至"] = "#F44336"; Settings.TermColors["小暑"] = "#F44336"; Settings.TermColors["大暑"] = "#F44336";
            Settings.TermColors["立秋"] = "#FF5722"; Settings.TermColors["处暑"] = "#FF5722"; Settings.TermColors["白露"] = "#FF5722";
            Settings.TermColors["秋分"] = "#795548"; Settings.TermColors["寒露"] = "#795548"; Settings.TermColors["霜降"] = "#795548";
            Settings.TermColors["立冬"] = "#607D8B"; Settings.TermColors["小雪"] = "#607D8B"; Settings.TermColors["大雪"] = "#607D8B";
            Settings.TermColors["冬至"] = "#2196F3"; Settings.TermColors["小寒"] = "#2196F3"; Settings.TermColors["大寒"] = "#2196F3";
        }
        if (Settings.TempGreetings.Count == 0)
        {
            foreach (var g in LocalGreetingDB.DefaultTempGreetings)
                Settings.TempGreetings.Add(new TempGreeting { MinTemp = g.MinTemp, MaxTemp = g.MaxTemp, Text = g.Text, Tag = g.Tag });
        }
        if (Settings.NoClassTimeSlots.Count == 0)
        {
            Settings.NoClassTimeSlots.Add(new NoClassTimeSlot { Name = "上午", StartHour = 5, StartMinute = 0, EndHour = 11, EndMinute = 0, Text = Settings.NoClassMorningText });
            Settings.NoClassTimeSlots.Add(new NoClassTimeSlot { Name = "中午", StartHour = 11, StartMinute = 0, EndHour = 13, EndMinute = 0, Text = Settings.NoClassNoonText });
            Settings.NoClassTimeSlots.Add(new NoClassTimeSlot { Name = "下午", StartHour = 13, StartMinute = 0, EndHour = 18, EndMinute = 0, Text = Settings.NoClassAfternoonText });
            Settings.NoClassTimeSlots.Add(new NoClassTimeSlot { Name = "晚间", StartHour = 18, StartMinute = 0, EndHour = 5, EndMinute = 0, Text = Settings.NoClassEveningText });
        }
    }

    public void SaveSettings()
    {
        try
        {
            var opt = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(Settings, opt));
        }
        catch { }
        SettingsChanged?.Invoke();
    }

    /// <summary>
    /// 自动对齐温度区间：排序后首项固定 -999，后续每项 Min = 上一项 Max + 1，避免重叠
    /// </summary>
    public void AlignTempGreetings()
    {
        var items = Settings.TempGreetings.OrderBy(g => g.MinTemp).ToList();
        if (items.Count == 0) return;

        // 第一项固定为 -999 ~ Max
        items[0].MinTemp = -999;
        if (items[0].MaxTemp <= -999) items[0].MaxTemp = 0;

        for (int i = 1; i < items.Count; i++)
        {
            items[i].MinTemp = items[i - 1].MaxTemp + 1;
            // 如果上限不合法，给默认跨度；最后一项若原先是 999 则保持无界
            if (items[i].MaxTemp <= items[i].MinTemp)
            {
                if (i == items.Count - 1)
                    items[i].MaxTemp = 999;
                else
                    items[i].MaxTemp = items[i].MinTemp + 5;
            }
        }
    }

    bool CacheValid() => File.Exists(_cachePath) && (DateTime.Now - new FileInfo(_cachePath).LastWriteTime).TotalDays < 7;

    async Task RefreshNetAsync()
    {
        try
        {
            using var c = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            var y = DateTime.Now.Year;
            var r = await c.GetStringAsync($"https://timor.tech/api/holiday/year/{y}");
            var list = ParseTimor(r);
            if (list.Count > 0) { _holidays = list; SaveCache(list); _netLoaded = true; }
        }
        catch { }
    }

    List<Holiday> ParseTimor(string json)
    {
        var list = new List<Holiday>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.GetProperty("code").GetInt32() != 0) return list;
            var ho = root.GetProperty("holiday");
            foreach (var p in ho.EnumerateObject())
            {
                var item = p.Value;
                var name = item.GetProperty("name").GetString() ?? "";
                var ds = item.GetProperty("date").GetString() ?? "";
                var isH = item.GetProperty("holiday").GetBoolean();
                if (DateTime.TryParseExact(ds, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var d))
                    list.Add(new Holiday { Name = StripYearPrefix(name), Date = d, IsWorkday = !isH, DaysOff = isH ? 1 : 0 });
            }
        }
        catch { }
        return list.GroupBy(h => h.Name).Select(g => g.OrderBy(h => h.Date).First()).OrderBy(h => h.Date).ToList();
    }

    void SaveCache(List<Holiday> h)
    {
        try { File.WriteAllText(_cachePath, JsonSerializer.Serialize(h, new JsonSerializerOptions { WriteIndented = true })); }
        catch { }
    }

    List<Holiday> LoadCache()
    {
        try
        {
            var json = File.ReadAllText(_cachePath);
            var list = JsonSerializer.Deserialize<List<Holiday>>(json) ?? new List<Holiday>();
            // 兼容旧缓存：去除节日名称开头的"XXXX年"前缀（如"2026年元旦"→"元旦"）
            foreach (var h in list) h.Name = StripYearPrefix(h.Name);
            return list;
        }
        catch { return new List<Holiday>(); }
    }

    // 去除节日名称开头的"XXXX年"前缀
    static string StripYearPrefix(string name)
    {
        if (string.IsNullOrEmpty(name) || name.Length < 5) return name;
        // 格式：4位数字+"年"+剩余，如"2026年元旦"
        if (name.Length >= 5 && name[4] == '年' && int.TryParse(name.Substring(0, 4), out _))
            return name.Substring(5);
        return name;
    }

    List<Holiday> LoadBuiltIn()
    {
        var list = new List<Holiday>();
        var cy = DateTime.Now.Year;
        for (int y = cy; y <= cy + 2; y++) list.AddRange(GetYear(y));
        return list;
    }

    List<Holiday> GetYear(int y)
    {
        var list = new List<Holiday>();
        list.Add(new Holiday { Name = "元旦", Date = new DateTime(y, 1, 1), DaysOff = 1 });
        var sd = new Dictionary<int, DateTime> { [2025] = new(2025, 1, 29), [2026] = new(2026, 2, 17), [2027] = new(2027, 2, 6), [2028] = new(2028, 1, 26), [2029] = new(2029, 2, 13), [2030] = new(2030, 2, 3) };
        if (sd.TryGetValue(y, out var d)) list.Add(new Holiday { Name = "春节", Date = d, DaysOff = 7 });
        list.Add(new Holiday { Name = "清明节", Date = new DateTime(y, 4, 4), DaysOff = 3 });
        list.Add(new Holiday { Name = "劳动节", Date = new DateTime(y, 5, 1), DaysOff = 5 });
        var dd = new Dictionary<int, DateTime> { [2025] = new(2025, 5, 31), [2026] = new(2026, 6, 19), [2027] = new(2027, 6, 9), [2028] = new(2028, 5, 28), [2029] = new(2029, 6, 16), [2030] = new(2030, 6, 5) };
        if (dd.TryGetValue(y, out var d2)) list.Add(new Holiday { Name = "端午节", Date = d2, DaysOff = 3 });
        var md = new Dictionary<int, DateTime> { [2025] = new(2025, 10, 6), [2026] = new(2026, 9, 25), [2027] = new(2027, 9, 15), [2028] = new(2028, 10, 3), [2029] = new(2029, 9, 22), [2030] = new(2030, 10, 12) };
        if (md.TryGetValue(y, out var d3)) list.Add(new Holiday { Name = "中秋节", Date = d3, DaysOff = 3 });
        list.Add(new Holiday { Name = "国庆节", Date = new DateTime(y, 10, 1), DaysOff = 7 });
        return list;
    }

    public List<Holiday> GetNextHolidays(int count)
    {
        var now = DateTime.Now;
        var all = new List<Holiday>(_holidays);
        if (Settings.ShowWeekendCountdown)
        {
            var sat = NextWeekend(DayOfWeek.Saturday); var sun = NextWeekend(DayOfWeek.Sunday);
            if (sat >= now.Date) all.Add(new Holiday { Name = "周六", Date = sat, IsCustom = true });
            if (sun >= now.Date) all.Add(new Holiday { Name = "周日", Date = sun, IsCustom = true });
        }
        return all.Where(h => h.Date.Date >= now.Date && !h.IsWorkday && h.IsEnabled && !Settings.DisabledHolidays.Contains(h.Name))
                  .OrderBy(h => h.Date).Take(count).ToList();
    }

    public List<Holiday> GetNextCustomHolidays(int count)
    {
        var now = DateTime.Now;
        var all = new List<Holiday>();
        foreach (var ch in Settings.CustomHolidays)
        {
            var d = ch.Date;
            if (ch.RepeatYearly && d.Year < now.Year) d = new DateTime(now.Year, ch.Date.Month, ch.Date.Day);
            if (ch.RepeatYearly && ch.Date.Month == 2 && ch.Date.Day == 29 && !DateTime.IsLeapYear(now.Year)) d = new DateTime(now.Year, 2, 28);
            if (d.Date >= now.Date) all.Add(new Holiday { Name = ch.Name, Date = d, IsCustom = true });
        }
        return all.Where(h => h.Date.Date >= now.Date && !h.IsWorkday && h.IsEnabled)
                  .OrderBy(h => h.Date).Take(count).ToList();
    }

    DateTime NextWeekend(DayOfWeek dw)
    {
        var n = DateTime.Now; int d = ((int)dw - (int)n.DayOfWeek + 7) % 7; if (d == 0) d = 7; return n.AddDays(d).Date;
    }

    public Holiday? GetNextWorkdayReminder()
    {
        if (!Settings.ShowWorkdayReminder) return null;
        return _holidays.Where(h => h.Date.Date >= DateTime.Now.Date && h.IsWorkday).OrderBy(h => h.Date).FirstOrDefault();
    }

    public Holiday? GetPrevHoliday()
    {
        return _holidays.Where(h => h.Date.Date < DateTime.Now.Date && !h.IsWorkday).OrderByDescending(h => h.Date).FirstOrDefault();
    }

    public double GetYearRatio()
    {
        var y = DateTime.Now.Year;
        var allHolidays = LoadBuiltIn().Where(h => h.Date.Year == y && !h.IsWorkday).ToList();
        var custom = Settings.CustomHolidays.Where(ch =>
        {
            var d = ch.Date; if (ch.RepeatYearly) d = new DateTime(y, ch.Date.Month, ch.Date.Day);
            return d.Year == y;
        }).Select(ch => new Holiday { Date = ch.Date, DaysOff = 1 }).ToList();
        allHolidays.AddRange(custom);
        var totalHolidayDays = allHolidays.Sum(h => h.DaysOff);
        var remaining = allHolidays.Where(h => h.Date >= DateTime.Now.Date).Sum(h => h.DaysOff);
        return totalHolidayDays > 0 ? (double)remaining / totalHolidayDays : 0;
    }

    // ===== 农历年度缓存 =====

    public async Task<LunarInfo?> GetLunarAsync(bool autoRefresh = true)
    {
        var today = DateTime.Now.Date;
        var monthKey = today.ToString("yyyy-MM");

        // 优先使用本月本地缓存
        var monthCache = LoadLunarMonthCache(monthKey);
        if (monthCache != null)
        {
            var info = monthCache.Data.FirstOrDefault(x => x.Date == today);
            if (info != null) return info;
        }

        if (!autoRefresh) return null;

        // 每日自动刷新一次当月数据
        await RefreshLunarMonthAsync(today.Year, today.Month);
        monthCache = LoadLunarMonthCache(monthKey);
        return monthCache?.Data.FirstOrDefault(x => x.Date == today);
    }

    LunarMonthCache? LoadLunarMonthCache(string month)
    {
        try
        {
            if (File.Exists(_lunarMonthCachePath))
            {
                var json = File.ReadAllText(_lunarMonthCachePath);
                var cache = JsonSerializer.Deserialize<LunarMonthCache>(json);
                if (cache != null && cache.Month == month && cache.Data.Count > 0 && cache.LastRefresh.Date == DateTime.Now.Date)
                    return cache;
            }
        }
        catch { }
        return null;
    }

    async Task RefreshLunarMonthAsync(int year, int month)
    {
        try
        {
            var list = new List<LunarInfo>();
            var daysInMonth = DateTime.DaysInMonth(year, month);
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(year, month, day);
                try
                {
                    var r = await client.GetStringAsync($"https://api.mu-jie.cc/lunar?date={date:yyyy-MM-dd}");
                    using var doc = JsonDocument.Parse(r);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("code", out var cp) && cp.GetInt32() == 200 && root.TryGetProperty("data", out var dp))
                    {
                        list.Add(new LunarInfo
                        {
                            Date = date,
                            gzYear = GetStr(dp, "gzYear") + "年",
                            IMonthCn = GetStr(dp, "IMonthCn"),
                            IDayCn = GetStr(dp, "IDayCn"),
                            Animal = GetStr(dp, "Animal"),
                            Term = GetStr(dp, "Term"),
                            lunarDate = GetStr(dp, "lunarDate")
                        });
                    }
                }
                catch { }
            }

            if (list.Count > 0)
            {
                SaveLunarMonthCache(new LunarMonthCache
                {
                    Month = $"{year:0000}-{month:00}",
                    LastRefresh = DateTime.Now,
                    Data = list
                });
            }
        }
        catch { }
    }

    void SaveLunarMonthCache(LunarMonthCache cache)
    {
        try
        {
            File.WriteAllText(_lunarMonthCachePath, JsonSerializer.Serialize(cache, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    List<LunarInfo>? LoadLunarYearCache(int year)
    {
        try
        {
            if (File.Exists(_lunarYearCachePath))
            {
                var json = File.ReadAllText(_lunarYearCachePath);
                var cache = JsonSerializer.Deserialize<LunarYearCache>(json);
                if (cache != null && cache.Year == year && cache.Data.Count > 0)
                    return cache.Data;
            }
        }
        catch { }
        return null;
    }

    async Task RefreshLunarYearAsync(int year)
    {
        try
        {
            var list = new List<LunarInfo>();
            var totalDays = DateTime.IsLeapYear(year) ? 366 : 365;
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

            for (int i = 0; i < totalDays; i++)
            {
                var date = new DateTime(year, 1, 1).AddDays(i);
                try
                {
                    var r = await client.GetStringAsync($"https://api.mu-jie.cc/lunar?date={date:yyyy-MM-dd}");
                    using var doc = JsonDocument.Parse(r);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("code", out var cp) && cp.GetInt32() == 200 && root.TryGetProperty("data", out var dp))
                    {
                        list.Add(new LunarInfo
                        {
                            Date = date,
                            gzYear = GetStr(dp, "gzYear") + "年",
                            IMonthCn = GetStr(dp, "IMonthCn"),
                            IDayCn = GetStr(dp, "IDayCn"),
                            Animal = GetStr(dp, "Animal"),
                            Term = GetStr(dp, "Term"),
                            lunarDate = GetStr(dp, "lunarDate")
                        });
                    }
                }
                catch { }

                // 每30天保存一次中间结果，避免全部失败
                if (i % 30 == 0 && list.Count > 0)
                {
                    SaveLunarYearCache(year, list);
                }
            }

            if (list.Count > 0)
                SaveLunarYearCache(year, list);
        }
        catch { }
    }

    void SaveLunarYearCache(int year, List<LunarInfo> data)
    {
        try
        {
            var cache = new LunarYearCache { Year = year, Data = data };
            File.WriteAllText(_lunarYearCachePath, JsonSerializer.Serialize(cache, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    string GetStr(JsonElement e, string p) => e.TryGetProperty(p, out var v) ? (v.GetString() ?? "") : "";

    public Color ParseColor(string hex)
    {
        try { return Color.Parse(hex); }
        catch { return Color.Parse("#2196F3"); }
    }

    public Color GetHolidayColor(string name)
    {
        if (!string.IsNullOrEmpty(Settings.HolidayColors.GetValueOrDefault(name))) return ParseColor(Settings.HolidayColors[name]);
        if (name.Contains("春节")) return Colors.Gold;
        if (name.Contains("清明")) return Color.Parse("#4CAF50");
        if (name.Contains("端午")) return Color.Parse("#2E7D32");
        if (name.Contains("中秋")) return Colors.Orange;
        if (name.Contains("国庆")) return Colors.Red;
        if (name.Contains("元旦")) return Color.Parse("#9C27B0");
        if (name.Contains("劳动")) return Color.Parse("#E91E63");
        return Color.Parse("#2196F3");
    }

    public Color GetTermColor(string name)
    {
        if (!string.IsNullOrEmpty(Settings.TermColors.GetValueOrDefault(name))) return ParseColor(Settings.TermColors[name]);
        return name switch
        {
            "立春" or "雨水" or "惊蛰" => Color.Parse("#4CAF50"),
            "春分" or "清明" or "谷雨" => Color.Parse("#66BB6A"),
            "立夏" or "小满" or "芒种" => Color.Parse("#FF9800"),
            "夏至" or "小暑" or "大暑" => Color.Parse("#F44336"),
            "立秋" or "处暑" or "白露" => Color.Parse("#FF5722"),
            "秋分" or "寒露" or "霜降" => Color.Parse("#795548"),
            "立冬" or "小雪" or "大雪" => Color.Parse("#607D8B"),
            "冬至" or "小寒" or "大寒" => Color.Parse("#2196F3"),
            _ => Color.Parse("#9C27B0")
        };
    }

    public bool IsNetLoaded => _netLoaded;

    /// <summary>
    /// 判断今天是否为调休上班日
    /// </summary>
    public bool IsTodayWorkday()
    {
        return _holidays.Any(h => h.Date.Date == DateTime.Now.Date && h.IsWorkday);
    }

    /// <summary>
    /// 判断今天是否为24节气日
    /// </summary>
    public bool IsTodaySolarTerm()
    {
        var today = DateTime.Now.Date;
        var yearCache = LoadLunarYearCache(today.Year);
        if (yearCache != null)
        {
            var info = yearCache.FirstOrDefault(x => x.Date == today);
            return info != null && !string.IsNullOrEmpty(info.Term);
        }
        return false;
    }

    /// <summary>
    /// 获取今天节气名称，无则返回空字符串
    /// </summary>
    public string GetTodaySolarTermName()
    {
        var today = DateTime.Now.Date;
        var yearCache = LoadLunarYearCache(today.Year);
        if (yearCache != null)
        {
            var info = yearCache.FirstOrDefault(x => x.Date == today);
            return info?.Term ?? "";
        }
        return "";
    }

    /// <summary>
    /// 一键刷新全部文案问候语（手动触发，真正随机）
    /// </summary>
    public void RefreshAllGreetings()
    {
        // 重置每日刷新标记，让问候语组件重新随机
        Settings.LastGreetingRefreshDate = null;

        // 刷新时段问候语
        foreach (var slot in Settings.TimeSlotGreetings)
        {
            if (!string.IsNullOrEmpty(slot.Tag))
            {
                var tagText = LocalGreetingDB.GetRandom(slot.Tag, LocalGreetingDB.TimeSlotGreetings);
                if (!string.IsNullOrEmpty(tagText)) slot.Text = tagText;
            }
        }

        // 刷新特殊日期问候语
        foreach (var special in Settings.SpecialDateGreetings)
        {
            if (!string.IsNullOrEmpty(special.Tag))
            {
                var tagText = LocalGreetingDB.GetRandom(special.Tag, LocalGreetingDB.WeeklyReminders);
                if (!string.IsNullOrEmpty(tagText)) special.Text = tagText;
            }
        }

        SaveSettings();
    }

    /// <summary>
    /// 一键刷新全部天气关键词问候语
    /// </summary>
    public void RefreshAllWeatherGreetings()
    {
        foreach (var item in Settings.WeatherGreetingItems)
        {
            if (item.Keyword == "默认") continue;
            var tag = string.IsNullOrEmpty(item.Tag) ? "默认" : item.Tag;
            var text = LocalGreetingDB.GetRandom(tag, LocalGreetingDB.WeatherGreetings);
            if (!string.IsNullOrEmpty(text)) item.Text = text;
        }
        SaveSettings();
    }

    /// <summary>
    /// 一键刷新全部温度区间问候语
    /// </summary>
    public void RefreshAllTempGreetings()
    {
        foreach (var item in Settings.TempGreetings)
        {
            var tag = string.IsNullOrEmpty(item.Tag) ? "舒适" : item.Tag;
            var text = LocalGreetingDB.GetRandom(tag, LocalGreetingDB.WeatherGreetings);
            if (!string.IsNullOrEmpty(text)) item.Text = text;
        }
        SaveSettings();
    }
}

public class LunarYearCache
{
    public int Year { get; set; }
    public List<LunarInfo> Data { get; set; } = new();
}
