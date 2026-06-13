using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace HolidayCountdown.Models;

public class SolarTerm
{
    public string Name { get; set; } = "";
    public DateTime Date { get; set; }
    public string CustomColor { get; set; } = "";
}

public static class SolarTermData
{
    private static List<SolarTerm>? _cached;
    private static DateTime _lastNet = DateTime.MinValue;
    private static readonly object _lock = new();

    static readonly Dictionary<int, string[]> Raw = new()
    {
        [2025] = new[]{"01-05","01-20","02-03","02-18","03-05","03-20","04-04","04-20","05-05","05-21","06-05","06-21","07-07","07-22","08-07","08-23","09-07","09-23","10-08","10-23","11-07","11-22","12-07","12-21"},
        [2026] = new[]{"01-05","01-20","02-04","02-18","03-05","03-20","04-05","04-20","05-05","05-21","06-05","06-21","07-07","07-23","08-07","08-23","09-07","09-23","10-08","10-23","11-07","11-22","12-07","12-22"},
        [2027] = new[]{"01-05","01-20","02-04","02-18","03-05","03-21","04-05","04-20","05-05","05-21","06-06","06-21","07-07","07-23","08-07","08-23","09-08","09-23","10-08","10-23","11-07","11-22","12-07","12-22"},
        [2028] = new[]{"01-05","01-20","02-04","02-19","03-05","03-20","04-04","04-19","05-05","05-20","06-05","06-21","07-06","07-22","08-07","08-22","09-07","09-22","10-08","10-23","11-07","11-21","12-06","12-21"},
        [2029] = new[]{"01-05","01-20","02-03","02-18","03-05","03-20","04-04","04-19","05-05","05-21","06-05","06-21","07-07","07-22","08-07","08-23","09-07","09-23","10-08","10-23","11-07","11-22","12-07","12-21"},
        [2030] = new[]{"01-05","01-20","02-04","02-18","03-05","03-20","04-05","04-20","05-05","05-21","06-05","06-21","07-07","07-23","08-07","08-23","09-07","09-23","10-08","10-23","11-07","11-22","12-07","12-22"}
    };
    static readonly string[] Names = {"小寒","大寒","立春","雨水","惊蛰","春分","清明","谷雨","立夏","小满","芒种","夏至","小暑","大暑","立秋","处暑","白露","秋分","寒露","霜降","立冬","小雪","大雪","冬至"};

    public static List<SolarTerm> GetAll()
    {
        if (_cached != null) return _cached;
        var list = new List<SolarTerm>();
        foreach (var kv in Raw)
            for (int i = 0; i < 24; i++)
                if (DateTime.TryParseExact($"{kv.Key}-{kv.Value[i]}", "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var d))
                    list.Add(new SolarTerm { Name = Names[i], Date = d });
        _cached = list.OrderBy(t => t.Date).ToList();
        return _cached;
    }

    public static SolarTerm? GetNext() => GetAll().FirstOrDefault(t => t.Date >= DateTime.Now.Date);
    public static SolarTerm? GetPrev() => GetAll().LastOrDefault(t => t.Date < DateTime.Now.Date);

    public static async Task TryRefreshAsync()
    {
        lock (_lock) { if ((DateTime.Now - _lastNet).TotalHours < 12) return; }
        try
        {
            using var c = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var r = await c.GetStringAsync($"https://api.aa1.cn/api/perpetual-calendar?date={DateTime.Now:yyyy-MM-dd}");
            using var doc = JsonDocument.Parse(r);
            var root = doc.RootElement;
            if (root.TryGetProperty("code", out var cp) && cp.GetInt32() == 200 && root.TryGetProperty("data", out var dp) && dp.TryGetProperty("traditionalChineseInfo", out var tcp))
            {
                if (tcp.TryGetProperty("nextJieQiName", out var n) && tcp.TryGetProperty("nextJieQiTime", out var t))
                {
                    var name = n.GetString(); var ts = t.GetString();
                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(ts) && DateTime.TryParseExact(ts, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out var d))
                    {
                        var all = GetAll(); var ex = all.FirstOrDefault(x => x.Name == name && x.Date.Year == d.Year);
                        if (ex != null) { ex.Date = d; lock (_lock) { _lastNet = DateTime.Now; } }
                    }
                }
            }
        }
        catch { }
    }
}
