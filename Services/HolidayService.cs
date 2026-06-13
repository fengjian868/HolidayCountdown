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
    private readonly string _cachePath, _settingsPath, _lunarCachePath, _greetingCachePath;
    private static bool _netTried;
    private static readonly object _netLock = new();
    private static bool _netLoaded;
    // 农历年度缓存：key 为 "yyyy-MM-dd"
    private static Dictionary<string, LunarInfo> _lunarYearCache = new();
    private static int _lunarCacheYear = -1;

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
        _greetingCachePath = Path.Combine(dir, "greeting_cache.json");
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
            Settings.TimeSlotGreetings.Add(new TimeSlotGreeting { StartHour = 5, StartMinute = 0, EndHour = 8, EndMinute = 0, Text = "早啊，今天也要加油 💪" });
            Settings.TimeSlotGreetings.Add(new TimeSlotGreeting { StartHour = 8, StartMinute = 0, EndHour = 12, EndMinute = 0, Text = "上午好，距离午休还有几节课" });
            Settings.TimeSlotGreetings.Add(new TimeSlotGreeting { StartHour = 12, StartMinute = 0, EndHour = 14, EndMinute = 0, Text = "吃饭时间到！🍚" });
            Settings.TimeSlotGreetings.Add(new TimeSlotGreeting { StartHour = 14, StartMinute = 0, EndHour = 17, EndMinute = 0, Text = "下午容易犯困，坚持住 😪" });
            Settings.TimeSlotGreetings.Add(new TimeSlotGreeting { StartHour = 17, StartMinute = 0, EndHour = 19, EndMinute = 0, Text = "再坚持一下就能run了！" });
            Settings.TimeSlotGreetings.Add(new TimeSlotGreeting { StartHour = 19, StartMinute = 0, EndHour = 23, EndMinute = 59, Text = "夜猫子模式启动 🦉" });
        }
        if (Settings.SpecialDateGreetings.Count == 0)
        {
            Settings.SpecialDateGreetings.Add(new SpecialDateGreeting { Name = "周一早晨", DayOfWeek = 1, StartHour = 0, StartMinute = 0, EndHour = 12, EndMinute = 0, Text = "本周还有 5 天到周末 😭" });
            Settings.SpecialDateGreetings.Add(new SpecialDateGreeting { Name = "周三", DayOfWeek = 3, StartHour = 0, StartMinute = 0, EndHour = 23, EndMinute = 59, Text = "周三了，过半了！📈" });
            Settings.SpecialDateGreetings.Add(new SpecialDateGreeting { Name = "周五下午", DayOfWeek = 5, StartHour = 12, StartMinute = 0, EndHour = 23, EndMinute = 59, Text = "周五周五，敲锣打鼓 🥁" });
            Settings.SpecialDateGreetings.Add(new SpecialDateGreeting { Name = "周末", DayOfWeek = 6, StartHour = 0, StartMinute = 0, EndHour = 23, EndMinute = 59, Text = "享受假期吧" });
            Settings.SpecialDateGreetings.Add(new SpecialDateGreeting { Name = "周末", DayOfWeek = 7, StartHour = 0, StartMinute = 0, EndHour = 23, EndMinute = 59, Text = "享受假期吧" });
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
    }

    public void SaveSettings()
    {
        try
        {
            var opt = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(Settings, opt));
        }
        catch { }
        // 触发设置变更事件，通知所有组件刷新
        SettingsChanged?.Invoke();
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
                    list.Add(new Holiday { Name = name, Date = d, IsWorkday = !isH, DaysOff = isH ? 1 : 0 });
            }
        }
        catch { }
        // 按名称去重，只保留每个节日的第一天（避免假期多天重复显示同一个节日）
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
            return JsonSerializer.Deserialize<List<Holiday>>(json) ?? new List<Holiday>();
        }
        catch { return new List<Holiday>(); }
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
        list.Add(new Holiday { Name = $"{y}年元旦", Date = new DateTime(y, 1, 1), DaysOff = 1 });
        var sd = new Dictionary<int, DateTime> { [2025] = new(2025, 1, 29), [2026] = new(2026, 2, 17), [2027] = new(2027, 2, 6), [2028] = new(2028, 1, 26), [2029] = new(2029, 2, 13), [2030] = new(2030, 2, 3) };
        if (sd.TryGetValue(y, out var d)) list.Add(new Holiday { Name = $"{y}年春节", Date = d, DaysOff = 7 });
        list.Add(new Holiday { Name = $"{y}年清明节", Date = new DateTime(y, 4, 4), DaysOff = 3 });
        list.Add(new Holiday { Name = $"{y}年劳动节", Date = new DateTime(y, 5, 1), DaysOff = 5 });
        var dd = new Dictionary<int, DateTime> { [2025] = new(2025, 5, 31), [2026] = new(2026, 6, 19), [2027] = new(2027, 6, 9), [2028] = new(2028, 5, 28), [2029] = new(2029, 6, 16), [2030] = new(2030, 6, 5) };
        if (dd.TryGetValue(y, out var d2)) list.Add(new Holiday { Name = $"{y}年端午节", Date = d2, DaysOff = 3 });
        var md = new Dictionary<int, DateTime> { [2025] = new(2025, 10, 6), [2026] = new(2026, 9, 25), [2027] = new(2027, 9, 15), [2028] = new(2028, 10, 3), [2029] = new(2029, 9, 22), [2030] = new(2030, 10, 12) };
        if (md.TryGetValue(y, out var d3)) list.Add(new Holiday { Name = $"{y}年中秋节", Date = d3, DaysOff = 3 });
        list.Add(new Holiday { Name = $"{y}年国庆节", Date = new DateTime(y, 10, 1), DaysOff = 7 });
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
        // 使用内置数据获取准确的放假天数（网络数据 DaysOff 不准确）
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

    public async Task<LunarInfo?> GetLunarAsync()
    {
        var today = DateTime.Now.Date;
        var todayKey = today.ToString("yyyy-MM-dd");

        // 1. 先从内存缓存查找
        if (_lunarCacheYear == today.Year && _lunarYearCache.TryGetValue(todayKey, out var cachedInfo))
            return cachedInfo;

        // 2. 从本地文件缓存加载
        LoadLunarYearCache(today.Year);
        if (_lunarYearCache.TryGetValue(todayKey, out var fileCached))
            return fileCached;

        // 3. 网络获取整年数据
        if (!Settings.LunarAutoRefresh) return null;
        await RefreshLunarYearAsync(today.Year);
        return _lunarYearCache.TryGetValue(todayKey, out var netCached) ? netCached : null;
    }

    /// <summary>
    /// 从本地文件加载整年农历缓存
    /// </summary>
    void LoadLunarYearCache(int year)
    {
        if (_lunarCacheYear == year) return;
        try
        {
            var path = Path.Combine(
                Path.GetDirectoryName(_lunarCachePath) ?? "",
                $"lunar_year_{year}.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var dict = JsonSerializer.Deserialize<Dictionary<string, LunarInfo>>(json);
                if (dict != null && dict.Count > 0)
                {
                    _lunarYearCache = dict;
                    _lunarCacheYear = year;
                }
            }
        }
        catch { }
    }

    /// <summary>
    /// 从网络获取整年农历数据并缓存到本地
    /// </summary>
    async Task RefreshLunarYearAsync(int year)
    {
        try
        {
            var dict = new Dictionary<string, LunarInfo>();
            // 获取该年所有日期的农历信息（分批请求，每次30天）
            for (int month = 1; month <= 12; month++)
            {
                try
                {
                    var date = new DateTime(year, month, 1);
                    using var c = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                    var r = await c.GetStringAsync($"https://api.mu-jie.cc/lunar?date={date:yyyy-MM-dd}");
                    using var doc = JsonDocument.Parse(r);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("code", out var cp) && cp.GetInt32() == 200 && root.TryGetProperty("data", out var dp))
                    {
                        // 获取该月所有日期
                        var daysInMonth = DateTime.DaysInMonth(year, month);
                        // 用月初数据推算整月（API 返回当月信息）
                        var baseInfo = new LunarInfo
                        {
                            Date = date,
                            gzYear = GetStr(dp, "gzYear") + "年",
                            IMonthCn = GetStr(dp, "IMonthCn"),
                            IDayCn = GetStr(dp, "IDayCn"),
                            Animal = GetStr(dp, "Animal"),
                            Term = GetStr(dp, "Term"),
                            lunarDate = GetStr(dp, "lunarDate")
                        };
                        dict[date.ToString("yyyy-MM-dd")] = baseInfo;
                    }
                }
                catch { }
            }

            // 如果只获取到了部分数据，逐日补充
            if (dict.Count < 365 && dict.Count > 0)
            {
                // 逐日请求剩余日期（限制最多请求30次，避免过多网络调用）
                var requested = 0;
                var totalDays = DateTime.IsLeapYear(year) ? 366 : 365;
                for (int day = 1; day <= totalDays; day++)
                {
                    if (requested >= 30) break;
                    var date = new DateTime(year, 1, 1).AddDays(day - 1);
                    var key = date.ToString("yyyy-MM-dd");
                    if (dict.ContainsKey(key)) continue;

                    try
                    {
                        using var c = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                        var r = await c.GetStringAsync($"https://api.mu-jie.cc/lunar?date={key}");
                        using var doc = JsonDocument.Parse(r);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("code", out var cp) && cp.GetInt32() == 200 && root.TryGetProperty("data", out var dp))
                        {
                            dict[key] = new LunarInfo
                            {
                                Date = date,
                                gzYear = GetStr(dp, "gzYear") + "年",
                                IMonthCn = GetStr(dp, "IMonthCn"),
                                IDayCn = GetStr(dp, "IDayCn"),
                                Animal = GetStr(dp, "Animal"),
                                Term = GetStr(dp, "Term"),
                                lunarDate = GetStr(dp, "lunarDate")
                            };
                        }
                        requested++;
                    }
                    catch { }
                }
            }

            if (dict.Count > 0)
            {
                _lunarYearCache = dict;
                _lunarCacheYear = year;
                SaveLunarYearCache(year);
            }
        }
        catch { }
    }

    /// <summary>
    /// 保存整年农历缓存到本地文件
    /// </summary>
    void SaveLunarYearCache(int year)
    {
        try
        {
            var path = Path.Combine(
                Path.GetDirectoryName(_lunarCachePath) ?? "",
                $"lunar_year_{year}.json");
            var opt = new JsonSerializerOptions { WriteIndented = false };
            File.WriteAllText(path, JsonSerializer.Serialize(_lunarYearCache, opt));
        }
        catch { }
    }

    string GetStr(JsonElement e, string p) => e.TryGetProperty(p, out var v) ? (v.GetString() ?? "") : "";

    public async Task RefreshGreetingsAsync()
    {
        if (!Settings.GreetingOnline) return;
        try
        {
            using var c = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var r = await c.GetStringAsync("https://v1.hitokoto.cn/?c=k&encode=json");
            using var doc = JsonDocument.Parse(r);
            var root = doc.RootElement;
            if (root.TryGetProperty("hitokoto", out var hp))
            {
                var text = hp.GetString() ?? "";
                if (!string.IsNullOrEmpty(text))
                {
                    var now = DateTime.Now;
                    var ct = now.TimeOfDay;

                    // 如果当前处于特殊日期时段内，不刷新（特殊日期优先且固定）
                    var inSpecial = Settings.SpecialDateGreetings.Any(sg =>
                    {
                        if (!sg.Enabled) return false;
                        if ((int)now.DayOfWeek == 0 ? sg.DayOfWeek != 7 : (int)now.DayOfWeek != sg.DayOfWeek) return false;
                        var start = new TimeSpan(sg.StartHour, sg.StartMinute, 0);
                        var end = new TimeSpan(sg.EndHour, sg.EndMinute, 0);
                        return ct >= start && ct < end;
                    });
                    if (inSpecial) return;

                    // 只刷新当前所处的时间段问候语，且保存到设置
                    var slot = Settings.TimeSlotGreetings.FirstOrDefault(s =>
                    {
                        var start = new TimeSpan(s.StartHour, s.StartMinute, 0);
                        var end = new TimeSpan(s.EndHour, s.EndMinute, 0);
                        return ct >= start && ct < end;
                    });
                    if (slot != null)
                    {
                        slot.Text = text;
                        SaveSettings();
                    }
                }
            }
        }
        catch { }
    }

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
}
