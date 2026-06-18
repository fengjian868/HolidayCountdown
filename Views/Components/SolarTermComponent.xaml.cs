using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using Path = System.IO.Path;
using ClassIsland.Core.Attributes;
using HolidayCountdown.Models;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.Components;

[ComponentInfo(
    "C3D4E5F6-A7B8-9012-CDEF-345678901234",
    "24节气",
    "🌿",
    "显示当前24节气倒计时"
)]
public class SolarTermComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private StackPanel _panel = null!;
    private HolidayService? _svc;
    private SolarTermInfo? _currentTerm;

    public SolarTermComponent()
    {
        _panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        Content = _panel;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(10) };
        _timer.Tick += (s, e) => Update();
        _timer.Start();
        Dispatcher.UIThread.Post(async () =>
        {
            _svc = new HolidayService();
            HolidayService.SettingsChanged += OnSettingsChanged;
            await LoadTermAsync();
            Update();
        });
    }

    void OnSettingsChanged()
    {
        _svc?.LoadSettings();
        Dispatcher.UIThread.Post(Update);
    }

    async Task LoadTermAsync()
    {
        var now = DateTime.Now;
        var cachePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ClassIsland", "Plugins", "HolidayCountdown", "solar_term_cache.json");

        try
        {
            if (File.Exists(cachePath))
            {
                var json = File.ReadAllText(cachePath);
                var cached = JsonSerializer.Deserialize<SolarTermInfo>(json);
                if (cached != null && cached.NextDate.Date >= now.Date)
                {
                    _currentTerm = cached;
                    return;
                }
            }
        }
        catch { }

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var r = await client.GetStringAsync($"https://api.mu-jie.cc/lunar?date={now:yyyy-MM-dd}");
            using var doc = JsonDocument.Parse(r);
            var root = doc.RootElement;
            if (root.TryGetProperty("code", out var cp) && cp.GetInt32() == 200 && root.TryGetProperty("data", out var dp))
            {
                var term = GetStr(dp, "Term");
                if (!string.IsNullOrEmpty(term))
                {
                    var nextTerm = CalculateNextTerm(now);
                    _currentTerm = new SolarTermInfo
                    {
                        Name = term,
                        Date = nextTerm?.Date ?? now,
                        NextDate = nextTerm?.Date ?? now.AddDays(15),
                        NextName = nextTerm?.Name ?? GetNextTermName(term)
                    };
                    try { File.WriteAllText(cachePath, JsonSerializer.Serialize(_currentTerm)); }
                    catch { }
                    return;
                }
            }
        }
        catch { }

        // 网络失败时使用本地计算下一个节气
        var localNext = CalculateNextTerm(now);
        if (localNext.HasValue)
        {
            _currentTerm = new SolarTermInfo
            {
                Name = localNext.Value.Name,
                Date = localNext.Value.Date,
                NextDate = localNext.Value.Date,
                NextName = localNext.Value.Name
            };
        }
    }

    // 2024-2026年24节气真实日期表（CET+8）
    static readonly Dictionary<int, (string name, DateTime date)[]> TermDates = new()
    {
        [2024] = new[]
        {
            ("小寒", new DateTime(2024, 1, 6)), ("大寒", new DateTime(2024, 1, 20)),
            ("立春", new DateTime(2024, 2, 4)), ("雨水", new DateTime(2024, 2, 19)),
            ("惊蛰", new DateTime(2024, 3, 5)), ("春分", new DateTime(2024, 3, 20)),
            ("清明", new DateTime(2024, 4, 4)), ("谷雨", new DateTime(2024, 4, 19)),
            ("立夏", new DateTime(2024, 5, 5)), ("小满", new DateTime(2024, 5, 20)),
            ("芒种", new DateTime(2024, 6, 5)), ("夏至", new DateTime(2024, 6, 21)),
            ("小暑", new DateTime(2024, 7, 6)), ("大暑", new DateTime(2024, 7, 22)),
            ("立秋", new DateTime(2024, 8, 7)), ("处暑", new DateTime(2024, 8, 22)),
            ("白露", new DateTime(2024, 9, 7)), ("秋分", new DateTime(2024, 9, 22)),
            ("寒露", new DateTime(2024, 10, 8)), ("霜降", new DateTime(2024, 10, 23)),
            ("立冬", new DateTime(2024, 11, 7)), ("小雪", new DateTime(2024, 11, 22)),
            ("大雪", new DateTime(2024, 12, 6)), ("冬至", new DateTime(2024, 12, 21))
        },
        [2025] = new[]
        {
            ("小寒", new DateTime(2025, 1, 5)), ("大寒", new DateTime(2025, 1, 20)),
            ("立春", new DateTime(2025, 2, 3)), ("雨水", new DateTime(2025, 2, 18)),
            ("惊蛰", new DateTime(2025, 3, 5)), ("春分", new DateTime(2025, 3, 20)),
            ("清明", new DateTime(2025, 4, 4)), ("谷雨", new DateTime(2025, 4, 20)),
            ("立夏", new DateTime(2025, 5, 5)), ("小满", new DateTime(2025, 5, 21)),
            ("芒种", new DateTime(2025, 6, 5)), ("夏至", new DateTime(2025, 6, 21)),
            ("小暑", new DateTime(2025, 7, 7)), ("大暑", new DateTime(2025, 7, 22)),
            ("立秋", new DateTime(2025, 8, 7)), ("处暑", new DateTime(2025, 8, 23)),
            ("白露", new DateTime(2025, 9, 7)), ("秋分", new DateTime(2025, 9, 23)),
            ("寒露", new DateTime(2025, 10, 8)), ("霜降", new DateTime(2025, 10, 23)),
            ("立冬", new DateTime(2025, 11, 7)), ("小雪", new DateTime(2025, 11, 22)),
            ("大雪", new DateTime(2025, 12, 7)), ("冬至", new DateTime(2025, 12, 21))
        },
        [2026] = new[]
        {
            ("小寒", new DateTime(2026, 1, 5)), ("大寒", new DateTime(2026, 1, 20)),
            ("立春", new DateTime(2026, 2, 4)), ("雨水", new DateTime(2026, 2, 18)),
            ("惊蛰", new DateTime(2026, 3, 5)), ("春分", new DateTime(2026, 3, 20)),
            ("清明", new DateTime(2026, 4, 5)), ("谷雨", new DateTime(2026, 4, 20)),
            ("立夏", new DateTime(2026, 5, 5)), ("小满", new DateTime(2026, 5, 21)),
            ("芒种", new DateTime(2026, 6, 5)), ("夏至", new DateTime(2026, 6, 21)),
            ("小暑", new DateTime(2026, 7, 7)), ("大暑", new DateTime(2026, 7, 23)),
            ("立秋", new DateTime(2026, 8, 7)), ("处暑", new DateTime(2026, 8, 23)),
            ("白露", new DateTime(2026, 9, 7)), ("秋分", new DateTime(2026, 9, 23)),
            ("寒露", new DateTime(2026, 10, 8)), ("霜降", new DateTime(2026, 10, 23)),
            ("立冬", new DateTime(2026, 11, 7)), ("小雪", new DateTime(2026, 11, 22)),
            ("大雪", new DateTime(2026, 12, 7)), ("冬至", new DateTime(2026, 12, 22))
        }
    };

    SolarTermInfo? CalculateLocalTerm(DateTime date)
    {
        var year = date.Year;
        if (!TermDates.TryGetValue(year, out var terms))
        {
            // 如果年份不在表中，使用2026年的数据推算（节气日期每年变化很小）
            terms = TermDates[2026];
        }

        for (int i = 0; i < terms.Length; i++)
        {
            var (name, termDate) = terms[i];
            var nextTerm = terms[(i + 1) % terms.Length];
            var nextDate = i < terms.Length - 1 ? nextTerm.date : new DateTime(year + 1, 1, 5);

            if (date.Date >= termDate.Date && date.Date < nextDate.Date)
            {
                return new SolarTermInfo
                {
                    Name = name,
                    Date = termDate,
                    NextDate = nextDate,
                    NextName = nextTerm.name
                };
            }
        }

        // 如果在年初小寒之前
        if (date.Date < terms[0].date.Date)
        {
            var prevYearTerms = TermDates.TryGetValue(year - 1, out var pt) ? pt : TermDates[2026];
            var lastTerm = prevYearTerms[^1];
            return new SolarTermInfo
            {
                Name = lastTerm.name,
                Date = lastTerm.date,
                NextDate = terms[0].date,
                NextName = terms[0].name
            };
        }

        return null;
    }

    // 计算下一个即将到来的节气
    (string Name, DateTime Date, DateTime PrevDate)? CalculateNextTerm(DateTime date)
    {
        var year = date.Year;
        if (!TermDates.TryGetValue(year, out var terms))
        {
            terms = TermDates[2026];
        }

        for (int i = 0; i < terms.Length; i++)
        {
            var (name, termDate) = terms[i];
            if (date.Date < termDate.Date)
            {
                var prevDate = i > 0 ? terms[i - 1].date : (TermDates.TryGetValue(year - 1, out var pt) ? pt[^1].date : new DateTime(year, 1, 1));
                return (name, termDate, prevDate);
            }
        }

        // 如果今年所有节气都过了，返回明年第一个节气
        var nextYearFirst = TermDates.TryGetValue(year + 1, out var nt) ? nt[0] : (TermDates.TryGetValue(year, out var ct) ? ct[0] : terms[0]);
        return (nextYearFirst.name, nextYearFirst.date, terms[^1].date);
    }

    void Update()
    {
        if (_svc == null) { _panel.Children.Clear(); return; }
        try
        {
            var now = DateTime.Now;
            // 每次更新都用真实日期表重新计算，显示下一个节气还有几天
            var nextTerm = CalculateNextTerm(now);
            if (nextTerm == null) { _panel.Children.Clear(); return; }

            var term = nextTerm.Value;
            var days = (term.Date.Date - now.Date).Days;
            var color = _svc.GetTermColor(term.Name);

            _panel.Children.Clear();

            var showProgress = _svc.Settings.SolarTermShowProgressRing;

            if (showProgress && days <= 15 && days >= 0)
            {
                var totalDays = Math.Max(1, (term.Date.Date - term.PrevDate.Date).Days);
                var progress = 1.0 - (double)days / totalDays;
                _panel.Children.Add(CreateArcRing(days, progress, color));
            }

            // 恢复 1.2.0.2 版本的节气叶子图标
            _panel.Children.Add(new TextBlock { Text = "🌿", FontSize = 13, VerticalAlignment = VerticalAlignment.Center });

            var nameBlock = new TextBlock
            {
                Text = term.Name,
                Foreground = new SolidColorBrush(color),
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            _panel.Children.Add(nameBlock);

            var daysBlock = new TextBlock
            {
                Text = days == 0 ? "今天" : $"还有{days}天",
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = 0.8
            };
            _panel.Children.Add(daysBlock);

            if (days == 0)
                _panel.Children.Add(new TextBlock { Text = "✨", FontSize = 12, VerticalAlignment = VerticalAlignment.Center });
        }
        catch { _panel.Children.Clear(); }
    }

    Control CreateArcRing(int days, double progress, Color color)
    {
        var container = new Border
        {
            Width = 32,
            Height = 32,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Background = Brushes.Transparent
        };

        var inner = new Grid { Width = 28, Height = 28, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        inner.Children.Add(new Arc { Width = 28, Height = 28, StartAngle = -90, SweepAngle = 360, Stroke = new SolidColorBrush(Color.Parse("#20FFFFFF")), StrokeThickness = 2.5, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center });
        inner.Children.Add(new Arc { Width = 28, Height = 28, StartAngle = -90, SweepAngle = progress * 360, Stroke = new SolidColorBrush(color), StrokeThickness = 2.5, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center });
        inner.Children.Add(new TextBlock { Text = days.ToString(), FontSize = 9, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(color), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center });
        container.Child = inner;
        return container;
    }

    string GetNextTermName(string current)
    {
        var terms = new[] { "立春", "雨水", "惊蛰", "春分", "清明", "谷雨", "立夏", "小满", "芒种", "夏至", "小暑", "大暑", "立秋", "处暑", "白露", "秋分", "寒露", "霜降", "立冬", "小雪", "大雪", "冬至", "小寒", "大寒" };
        var idx = Array.IndexOf(terms, current);
        return idx >= 0 ? terms[(idx + 1) % terms.Length] : "";
    }

    string GetStr(JsonElement e, string p) => e.TryGetProperty(p, out var v) ? (v.GetString() ?? "") : "";
}

public class SolarTermInfo
{
    public string Name { get; set; } = "";
    public DateTime Date { get; set; }
    public DateTime NextDate { get; set; }
    public string NextName { get; set; } = "";
}
