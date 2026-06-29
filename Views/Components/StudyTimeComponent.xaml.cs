using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
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
    "学习时长统计",
    "fluent(\uE9D1)",
    "记录ClassIsland运行时长，显示今日学习时长"
)]
public class StudyTimeComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private TextBlock _txt = null!;
    private HolidayService? _svc;
    private static readonly DateTime _sessionStart = DateTime.Now;
    private static DateTime _lastUpdate = DateTime.Now;
    private static readonly object _saveLock = new();
    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ClassIsland", "Plugins", "HolidayCountdown");
    private static readonly string StudyTimePath = Path.Combine(DataDir, "study_time.json");

    public StudyTimeComponent()
    {
        // 多个组件实例共享同一会话计时，避免重复统计或统计丢失
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
            var now = DateTime.Now;
            var elapsedMinutes = (now - _lastUpdate).TotalMinutes;
            _lastUpdate = now;

            // 若只统计已上课时长，则仅在上课状态时累加
            if (_svc.Settings.StudyTimeCountClassTimeOnly)
            {
                var state = GetCurrentState();
                if (state != 1) elapsedMinutes = 0;
            }

            var key = GetCurrentKey();

            lock (_saveLock)
            {
                var data = LoadStudyData();

                if (!data.ContainsKey(key)) data[key] = 0;
                data[key] = data[key] + elapsedMinutes;

                SaveStudyData(data);

                var totalMinutes = data[key];
                var icon = _svc.Settings.StudyTimeShowIcon ? "📚 " : "";
                var timeScope = _svc.Settings.StudyTimeWeeklyReset ? "本周" : "今日";
                var action = _svc.Settings.StudyTimeCountClassTimeOnly ? "已上课" : "已学习";
                _txt.Text = $"{icon}{timeScope}{action} {FormatDuration(totalMinutes)}";
            }
        }
        catch { _txt.Text = ""; }
    }

    string GetCurrentKey()
    {
        if (_svc?.Settings.StudyTimeWeeklyReset == true)
        {
            var now = DateTime.Now;
            return $"{now.Year}-W{ISOWeek.GetWeekOfYear(now)}";
        }
        return DateTime.Now.ToString("yyyy-MM-dd");
    }

    int GetCurrentState()
    {
        try
        {
            var svc = GetLessonsService();
            if (svc == null) return 0;
            var stateObj = GetPropertyValue(svc, "CurrentState");
            return stateObj as int? ?? 0;
        }
        catch { return 0; }
    }

    object? GetLessonsService()
    {
        try
        {
            var appHostType = Type.GetType("ClassIsland.Shared.IAppHost, ClassIsland.Shared")
                ?? Type.GetType("ClassIsland.Shared.IAppHost, ClassIsland.Core")
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == "IAppHost");

            if (appHostType == null) return null;

            var tryGetService = appHostType.GetMethod("TryGetService", BindingFlags.Public | BindingFlags.Static);
            if (tryGetService == null || !tryGetService.IsGenericMethodDefinition) return null;

            var lessonsServiceType = Type.GetType("ClassIsland.Core.Abstractions.Services.ILessonsService, ClassIsland.Core")
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == "ILessonsService" || t.Name == "LessonsService");

            if (lessonsServiceType == null) return null;

            var genericMethod = tryGetService.MakeGenericMethod(lessonsServiceType);
            return genericMethod.Invoke(null, null);
        }
        catch { return null; }
    }

    object? GetPropertyValue(object obj, string propName)
    {
        try
        {
            var prop = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            return prop?.GetValue(obj);
        }
        catch { return null; }
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
