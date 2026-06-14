using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using HolidayCountdown.Models;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.Components;

[ComponentInfo(
    "C3D4E5F6-A7B8-9012-CDEF-345678901234",
    "24节气",
    "\uE9CA",
    "显示当前24节气倒计时"
)]
public class SolarTermComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private TextBlock _txt = null!;
    private HolidayService? _svc;
    private SolarTermInfo? _currentTerm;

    public SolarTermComponent()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        _txt = new TextBlock { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Opacity = 0.9 };
        panel.Children.Add(_txt);
        Content = panel;
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
                    _currentTerm = new SolarTermInfo
                    {
                        Name = term,
                        Date = now,
                        NextDate = now.AddDays(15),
                        NextName = GetNextTermName(term)
                    };
                    try { File.WriteAllText(cachePath, JsonSerializer.Serialize(_currentTerm)); }
                    catch { }
                    return;
                }
            }
        }
        catch { }

        // 网络失败时使用本地计算
        _currentTerm = CalculateLocalTerm(now);
    }

    SolarTermInfo? CalculateLocalTerm(DateTime date)
    {
        var terms = new[] { "小寒", "大寒", "立春", "雨水", "惊蛰", "春分", "清明", "谷雨", "立夏", "小满", "芒种", "夏至", "小暑", "大暑", "立秋", "处暑", "白露", "秋分", "寒露", "霜降", "立冬", "小雪", "大雪", "冬至" };
        var year = date.Year;
        var baseDate = new DateTime(year, 1, 6); // 小寒约1月6日
        var dayOfYear = (date - new DateTime(year, 1, 1)).TotalDays;

        for (int i = 0; i < terms.Length; i++)
        {
            var termDate = baseDate.AddDays(i * 15.2);
            var nextTermDate = baseDate.AddDays((i + 1) * 15.2);
            if (dayOfYear >= (termDate - new DateTime(year, 1, 1)).TotalDays && dayOfYear < (nextTermDate - new DateTime(year, 1, 1)).TotalDays)
            {
                return new SolarTermInfo
                {
                    Name = terms[i],
                    Date = termDate,
                    NextDate = nextTermDate,
                    NextName = terms[(i + 1) % terms.Length]
                };
            }
        }

        // 如果在一年的末尾，返回冬至
        return new SolarTermInfo
        {
            Name = "冬至",
            Date = baseDate.AddDays(23 * 15.2),
            NextDate = new DateTime(year + 1, 1, 6),
            NextName = "小寒"
        };
    }

    void Update()
    {
        if (_svc == null || _currentTerm == null) { _txt.Text = ""; return; }
        try
        {
            var now = DateTime.Now;
            var days = (_currentTerm.NextDate.Date - now.Date).Days;
            var color = _svc.GetTermColor(_currentTerm.Name);

            var showProgress = _svc.Settings.SolarTermShowProgressRing;

            if (showProgress && days <= 15 && days >= 0)
            {
                var progress = 1.0 - (double)days / 15.0;
                var arc = GetArcText(progress);
                _txt.Text = $"🌿 {_currentTerm.Name} {arc}";
                _txt.Foreground = new SolidColorBrush(color);
            }
            else
            {
                _txt.Text = $"🌿 {_currentTerm.Name}";
                _txt.Foreground = new SolidColorBrush(color);
            }
        }
        catch { _txt.Text = ""; }
    }

    string GetArcText(double progress)
    {
        var chars = new[] { "○", "◔", "◑", "◕", "●" };
        var idx = Math.Min((int)(progress * chars.Length), chars.Length - 1);
        return chars[idx];
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
