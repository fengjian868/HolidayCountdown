using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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
    "E5F6A7B8-C9D0-1234-EF01-234567890ABC",
    "学习时长统计 [测试版]",
    "\uE917",
    "记录ClassIsland运行时长，显示今日学习时长（测试版，不稳定）"
)]
public class StudyTimeComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private TextBlock _txt = null!;
    private HolidayService? _svc;
    private DateTime _sessionStart;
    private DateTime _lastUpdate;
    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ClassIsland", "Plugins", "HolidayCountdown");
    private static readonly string StudyTimePath = Path.Combine(DataDir, "study_time.json");

    public StudyTimeComponent()
    {
        _sessionStart = DateTime.Now;
        _lastUpdate = DateTime.Now;
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        _txt = new TextBlock { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Opacity = 0.9 };
        panel.Children.Add(_txt);
        Content = panel;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _timer.Tick += (s, e) => Update();
        _timer.Start();
        Dispatcher.UIThread.Post(() => { _svc = new HolidayService(); HolidayService.SettingsChanged += OnSettingsChanged; Update(); });
    }

    void OnSettingsChanged()
    {
        _svc?.LoadSettings();
        Dispatcher.UIThread.Post(Update);
    }

    void Update()
    {
        if (_svc == null || !_svc.Settings.StudyTimeEnabled) { _txt.Text = ""; return; }

        try
        {
            var today = DateTime.Now.Date;
            var data = LoadStudyData();
            var key = today.ToString("yyyy-MM-dd");

            // Only add elapsed time since last update
            var now = DateTime.Now;
            var elapsedMinutes = (now - _lastUpdate).TotalMinutes;
            _lastUpdate = now;

            if (!data.ContainsKey(key)) data[key] = 0;
            data[key] = data[key] + elapsedMinutes;

            // Save updated data
            SaveStudyData(data);

            var totalMinutes = data[key];
            var icon = _svc.Settings.StudyTimeShowIcon ? "📚 " : "";
            _txt.Text = $"{icon}今日已学习 {FormatDuration(totalMinutes)}";
        }
        catch { _txt.Text = ""; }
    }

    string FormatDuration(double totalMinutes)
    {
        var hours = (int)(totalMinutes / 60);
        var mins = (int)(totalMinutes % 60);
        if (hours > 0)
            return $"{hours}小时{mins}分钟";
        return $"{mins}分钟";
    }

    Dictionary<string, double> LoadStudyData()
    {
        try
        {
            if (File.Exists(StudyTimePath))
            {
                var json = File.ReadAllText(StudyTimePath);
                return JsonSerializer.Deserialize<Dictionary<string, double>>(json) ?? new Dictionary<string, double>();
            }
        }
        catch { }
        return new Dictionary<string, double>();
    }

    void SaveStudyData(Dictionary<string, double> data)
    {
        try
        {
            Directory.CreateDirectory(DataDir);
            // Keep only last 30 days
            var cutoff = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");
            var keysToRemove = data.Keys.Where(k => string.Compare(k, cutoff) < 0).ToList();
            foreach (var k in keysToRemove) data.Remove(k);

            var opt = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(StudyTimePath, JsonSerializer.Serialize(data, opt));
        }
        catch { }
    }
}
