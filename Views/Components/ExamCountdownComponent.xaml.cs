using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using HolidayCountdown.Models;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.Components;

[ComponentInfo(
    "F1A2B3C4-D5E6-7890-1234-567890ABCDEF",
    "大考倒计时[测试版]",
    "\uEDF3",
    "显示中考/高考倒计时，内置全国各地考试时间，每年自动刷新"
)]
public class ExamCountdownComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private TextBlock _txt = null!;
    private Grid _ringGrid = null!;
    private HolidayService _svc = new();

    public ExamCountdownComponent()
    {
        const double ringSize = 32;

        _ringGrid = new Grid { Width = ringSize, Height = ringSize, Margin = new Thickness(0, 0, 8, 0) };

        _txt = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = 13,
            FontWeight = FontWeight.SemiBold
        };

        Content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Children = { _ringGrid, _txt }
        };

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _timer.Tick += (s, e) => Update();
        _timer.Start();

        HolidayService.SettingsChanged += OnSettingsChanged;
        Update();
    }

    void OnSettingsChanged()
    {
        _svc.LoadSettings();
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
                .Replace("{A}", examName)
                .Replace("{B}", Math.Max(0, days).ToString())
                .Replace("{C}", examDate.ToString("M月d日", CultureInfo.CurrentCulture))
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
            {
                // 黑白/灰度颜色跟随主题，带颜色则保持用户设置
                if (fg.R == fg.G && fg.G == fg.B)
                    _txt[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
                else
                    _txt.Foreground = new SolidColorBrush(fg);
            }

            // 圆环显示：每次Update重新创建Arc确保渲染正确
            var ringVisible = _svc.Settings.ExamCountdownShowRing;
            _ringGrid.IsVisible = ringVisible;
            _ringGrid.Children.Clear();
            if (ringVisible)
            {
                Color ringColor = Color.TryParse(_svc.Settings.ExamCountdownRingColor, out var rc) ? rc : Color.Parse("#FFFF5252");
                var progress = ComputeRingProgress(examDate);
                var sweepAngle = Math.Max(0, Math.Min(360, progress * 360));

                _ringGrid.Children.Add(new Arc
                {
                    Width = 28, Height = 28,
                    StartAngle = -90, SweepAngle = 360,
                    Stroke = new SolidColorBrush(ringColor) { Opacity = 0.2 },
                    StrokeThickness = 2.5,
                    StrokeLineCap = PenLineCap.Round,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });
                _ringGrid.Children.Add(new Arc
                {
                    Width = 28, Height = 28,
                    StartAngle = -90, SweepAngle = sweepAngle,
                    Stroke = new SolidColorBrush(ringColor),
                    StrokeThickness = 2.5,
                    StrokeLineCap = PenLineCap.Round,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });
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
        // 圆环开始日期：上次考试结束日（默认6月9日）
        // 从设置的开始日期的月/日取，应用到examDate所在年份
        var start = ParseRingStartDate(examDate.Year);
        // 如果开始日期 >= 考试日期，说明开始日期在上一年
        if (start >= examDate) start = start.AddYears(-1);
        // 如果当前时间在开始日期之前，也往前推一年
        if (now < start) start = start.AddYears(-1);

        var total = (examDate - start).TotalDays;
        var passed = (now - start).TotalDays;
        if (total <= 0) return 1;
        return Math.Min(1, Math.Max(0, passed / total));
    }

    DateTime ParseRingStartDate(int year)
    {
        var start = _svc.Settings.ExamCountdownRingStartDate;
        try { return new DateTime(year, start.Month, start.Day); }
        catch { return new DateTime(year, 6, 9); }
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
