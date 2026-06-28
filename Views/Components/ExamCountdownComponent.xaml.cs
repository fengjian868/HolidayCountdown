using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using HolidayCountdown.Models;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.Components;

[ComponentInfo(
    "F1A2B3C4-D5E6-7890-1234-567890ABCDEF",
    "大考倒计时",
    "fluent(\uE921)",
    "显示中考/高考倒计时，内置全国各地考试时间，每年自动刷新"
)]
public class ExamCountdownComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private TextBlock _txt = null!;
    private Arc _ringTrack = null!;
    private Arc _ringProgress = null!;
    private HolidayService _svc = new();

    public ExamCountdownComponent()
    {
        _ringTrack = new Arc
        {
            Width = 12,
            Height = 12,
            StartAngle = 0,
            SweepAngle = 360,
            StrokeThickness = 2,
            StrokeLineCap = PenLineCap.Round,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        _ringProgress = new Arc
        {
            Width = 12,
            Height = 12,
            StartAngle = -90,
            StrokeThickness = 2,
            StrokeLineCap = PenLineCap.Round,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var ringGrid = new Grid { Width = 12, Height = 12, Margin = new Thickness(0, 0, 6, 0) };
        ringGrid.Children.Add(_ringTrack);
        ringGrid.Children.Add(_ringProgress);

        _txt = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = 13,
            FontWeight = FontWeight.SemiBold
        };

        // 直接圆环 + 文字，无背景块
        Content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Children = { ringGrid, _txt }
        };

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _timer.Tick += (s, e) => Update();
        _timer.Start();

        Dispatcher.UIThread.Post(Update);
    }

    void Update()
    {
        try
        {
            var (examName, examDate) = GetNextExamDate();
            var today = DateTime.Now.Date;
            var days = (examDate.Date - today).Days;

            string text;
            if (days <= 0)
                text = _svc.Settings.ExamCountdownTodayText;
            else
                text = _svc.Settings.ExamCountdownCustomText;

            text = text
                .Replace("{exam}", examName)
                .Replace("{days}", Math.Max(0, days).ToString())
                .Replace("{date}", examDate.ToString("M月d日", CultureInfo.CurrentCulture));
            text = Regex.Replace(text, @"\s+", " ").Trim();

            _txt.Text = text;
            var fontSize = _svc.Settings.ExamCountdownFontSize > 0
                ? _svc.Settings.ExamCountdownFontSize
                : (int)GetClassIslandFontSize();
            _txt.FontSize = fontSize;

            if (Color.TryParse(_svc.Settings.ExamCountdownTextColor, out var fg))
                _txt.Foreground = new SolidColorBrush(fg);

            var ringVisible = _svc.Settings.ExamCountdownShowRing;
            _ringTrack.IsVisible = ringVisible;
            _ringProgress.IsVisible = ringVisible;
            if (ringVisible && Color.TryParse(_svc.Settings.ExamCountdownRingColor, out var ringColor))
            {
                _ringTrack.Stroke = new SolidColorBrush(ringColor) { Opacity = 0.25 };
                _ringProgress.Stroke = new SolidColorBrush(ringColor);
                var progress = ComputeRingProgress(examDate);
                _ringProgress.SweepAngle = Math.Max(0, Math.Min(360, progress * 360));
            }

        }
        catch
        {
            _txt.Text = "大考倒计时";
        }
    }

    (string examName, DateTime examDate) GetNextExamDate()
    {
        var now = DateTime.Now;
        var examName = _svc.Settings.ExamType == 1 ? "中考" : "高考";

        DateTime GetDateForYear(int year)
        {
            var custom = _svc.Settings.ExamCountdownCustomDate;
            if (!string.IsNullOrWhiteSpace(custom) &&
                DateTime.TryParseExact(year + "-" + custom, "yyyy-M-d", CultureInfo.InvariantCulture, DateTimeStyles.None, out var cd))
                return cd;
            return ExamDateData.GetExamDate(year, _svc.Settings.ExamType, _svc.Settings.ExamCity);
        }

        var current = GetDateForYear(now.Year);
        if (current.Date < now.Date)
        {
            if (_svc.Settings.ExamCountdownRepeatYearly)
                current = GetDateForYear(now.Year + 1);
        }

        return (examName, current);
    }

    double ComputeRingProgress(DateTime examDate)
    {
        var now = DateTime.Now;
        var start = ParseRingStartDate(examDate.Year);
        if (start > examDate) start = start.AddYears(-1);
        if (now <= start) return 0;
        var total = (examDate - start).TotalDays;
        var passed = (now - start).TotalDays;
        if (total <= 0) return 1;
        return Math.Min(1, passed / total);
    }

    DateTime ParseRingStartDate(int year)
    {
        var input = _svc.Settings.ExamCountdownRingStartDate ?? "08-01";
        var parts = input.Split('-', '/', '.');
        int month = 8, day = 1;
        if (parts.Length >= 2 &&
            int.TryParse(parts[0], out var m) &&
            int.TryParse(parts[1], out var d))
        {
            month = m;
            day = d;
        }
        try { return new DateTime(year, month, day); }
        catch { return new DateTime(year, 8, 1); }
    }

    double GetClassIslandFontSize()
    {
        try
        {
            var settings = GetSettingsServiceSettings();
            if (settings == null) return 14;
            var value = GetPropertyValue(settings, "MainWindowBodyFontSize");
            if (value is double d) return d;
            if (value is float f) return f;
            if (value != null && double.TryParse(value.ToString(), out var parsed)) return parsed;
        }
        catch { }
        return 14;
    }

    object? GetSettingsServiceSettings()
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

            var settingsServiceType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == "SettingsService");

            if (settingsServiceType == null) return null;

            var genericMethod = tryGetService.MakeGenericMethod(settingsServiceType);
            var settingsService = genericMethod.Invoke(null, null);
            if (settingsService == null) return null;

            var settingsProp = settingsServiceType.GetProperty("Settings", BindingFlags.Public | BindingFlags.Instance);
            return settingsProp?.GetValue(settingsService);
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
}
