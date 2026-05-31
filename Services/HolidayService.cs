using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using HolidayCountdown.Models;

namespace HolidayCountdown.Services;

public class HolidayService
{
    private readonly List<Holiday> _builtInHolidays;
    private List<Holiday> _holidays = new();
    private readonly string _cachePath;
    private static bool _networkTried;
    private static readonly object _networkLock = new();
    private static bool _networkLoaded;

    public HolidayService()
    {
        _builtInHolidays = LoadBuiltInHolidays();
        _holidays = new List<Holiday>(_builtInHolidays);

        var cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ClassIsland", "Plugins", "HolidayCountdown"
        );
        Directory.CreateDirectory(cacheDir);
        _cachePath = Path.Combine(cacheDir, "holiday_cache.json");

        if (CacheIsValid())
        {
            var cached = LoadFromCache();
            if (cached.Count > 0) _holidays = cached;
        }

        if (!_networkTried)
        {
            lock (_networkLock)
            {
                if (!_networkTried)
                {
                    _networkTried = true;
                    _ = Task.Run(async () => await RefreshFromNetworkAsync());
                }
            }
        }
    }

    private bool CacheIsValid()
    {
        if (!File.Exists(_cachePath)) return false;
        return (DateTime.Now - new FileInfo(_cachePath).LastWriteTime).TotalDays < 7;
    }

    private async Task RefreshFromNetworkAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            var year = DateTime.Now.Year;
            var response = await client.GetStringAsync($"https://timor.tech/api/holiday/year/{year}");

            var holidays = ParseTimorTechResponse(response);
            if (holidays.Count > 0)
            {
                _holidays = holidays;
                SaveToCache(holidays);
                _networkLoaded = true;
            }
        }
        catch { }
    }

    private List<Holiday> ParseTimorTechResponse(string json)
    {
        var list = new List<Holiday>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.GetProperty("code").GetInt32() != 0) return list;

            var holidayObj = root.GetProperty("holiday");
            foreach (var prop in holidayObj.EnumerateObject())
            {
                var item = prop.Value;
                var name = item.GetProperty("name").GetString() ?? "";
                var dateStr = item.GetProperty("date").GetString() ?? "";
                var isHoliday = item.GetProperty("holiday").GetBoolean();

                if (DateTime.TryParseExact(dateStr, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var date))
                {
                    list.Add(new Holiday
                    {
                        Name = name,
                        Date = date,
                        IsWorkday = !isHoliday,
                        DaysOff = isHoliday ? 1 : 0
                    });
                }
            }
        }
        catch { }
        return list.OrderBy(h => h.Date).ToList();
    }

    private void SaveToCache(List<Holiday> holidays)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(holidays, options);
            File.WriteAllText(_cachePath, json);
        }
        catch { }
    }

    private List<Holiday> LoadFromCache()
    {
        try
        {
            var json = File.ReadAllText(_cachePath);
            return JsonSerializer.Deserialize<List<Holiday>>(json) ?? new List<Holiday>();
        }
        catch { return new List<Holiday>(); }
    }

    private List<Holiday> LoadBuiltInHolidays()
    {
        var list = new List<Holiday>();
        var currentYear = DateTime.Now.Year;
        for (int year = currentYear; year <= currentYear + 2; year++)
            list.AddRange(GetYearHolidays(year));
        return list;
    }

    private List<Holiday> GetYearHolidays(int year)
    {
        var list = new List<Holiday>();
        list.Add(new Holiday { Name = $"{year}年元旦", Date = new DateTime(year, 1, 1), DaysOff = 1 });

        var springDates = new Dictionary<int, DateTime>
        {
            [2025] = new DateTime(2025, 1, 29), [2026] = new DateTime(2026, 2, 17),
            [2027] = new DateTime(2027, 2, 6), [2028] = new DateTime(2028, 1, 26),
        };
        if (springDates.TryGetValue(year, out var sd))
            list.Add(new Holiday { Name = $"{year}年春节", Date = sd, DaysOff = 7 });

        list.Add(new Holiday { Name = $"{year}年清明节", Date = new DateTime(year, 4, 4), DaysOff = 3 });
        list.Add(new Holiday { Name = $"{year}年劳动节", Date = new DateTime(year, 5, 1), DaysOff = 5 });

        var dragonDates = new Dictionary<int, DateTime>
        {
            [2025] = new DateTime(2025, 5, 31), [2026] = new DateTime(2026, 6, 19),
            [2027] = new DateTime(2027, 6, 9), [2028] = new DateTime(2028, 5, 28),
        };
        if (dragonDates.TryGetValue(year, out var dd))
            list.Add(new Holiday { Name = $"{year}年端午节", Date = dd, DaysOff = 3 });

        var midAutumnDates = new Dictionary<int, DateTime>
        {
            [2025] = new DateTime(2025, 10, 6), [2026] = new DateTime(2026, 9, 25),
            [2027] = new DateTime(2027, 9, 15), [2028] = new DateTime(2028, 10, 3),
        };
        if (midAutumnDates.TryGetValue(year, out var md))
            list.Add(new Holiday { Name = $"{year}年中秋节", Date = md, DaysOff = 3 });

        list.Add(new Holiday { Name = $"{year}年国庆节", Date = new DateTime(year, 10, 1), DaysOff = 7 });
        return list;
    }

    public Holiday? GetNextHoliday()
    {
        var now = DateTime.Now;
        return _holidays
            .Where(h => h.Date.Date >= now.Date && !h.IsWorkday)
            .OrderBy(h => h.Date)
            .FirstOrDefault();
    }

    public bool IsNetworkLoaded => _networkLoaded;
}