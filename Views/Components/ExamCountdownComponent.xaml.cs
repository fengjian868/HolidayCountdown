using System;
using System.Globalization;
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
using HolidayCountdown.Models.ComponentSettings;

namespace HolidayCountdown.Views.Components;

[ComponentInfo(
    "F1A2B3C4-D5E6-7890-1234-567890ABCDEF",
    "大考倒计时",
    "fluent(\uE921)",
    "显示中考/高考倒计时，内置全国各地考试时间，每年自动刷新"
)]
public class ExamCountdownComponent : ComponentBase<ExamCountdownSettings>
{
    private DispatcherTimer _timer = null!;
    private TextBlock _txt = null!;
    private Ellipse _dot = null!;
    private Border? _bg;

    public ExamCountdownComponent()
    {
        _dot = new Ellipse
        {
            Width = 6,
            Height = 6,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0)
        };

        _txt = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = 13,
            FontWeight = FontWeight.SemiBold
        };

        var inner = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Children = { _dot, _txt }
        };

        _bg = new Border
        {
            CornerRadius = new CornerRadius(999),
            Padding = new Thickness(10, 4),
            Child = inner
        };

        Content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Children = { _bg }
        };

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _timer.Tick += (s, e) => Update();
        _timer.Start();

        Dispatcher.UIThread.Post(Update);
    }

    void Update()
    {
        if (Settings == null) return;

        try
        {
            var (examName, examDate) = GetNextExamDate();
            var today = DateTime.Now.Date;
            var days = (examDate.Date - today).Days;

            string text;
            if (days <= 0)
                text = Settings.TodayText;
            else
                text = Settings.CustomText;

            text = text
                .Replace("{exam}", examName)
                .Replace("{days}", Math.Max(0, days).ToString())
                .Replace("{date}", examDate.ToString("M月d日", CultureInfo.CurrentCulture));
            text = Regex.Replace(text, @"\s+", " ").Trim();

            _txt.Text = text;

            if (Color.TryParse(Settings.TextColor, out var fg))
                _txt.Foreground = new SolidColorBrush(fg);

            _dot.IsVisible = Settings.ShowDot;
            if (Settings.ShowDot && Color.TryParse(Settings.DotColor, out var dotColor))
                _dot.Fill = new SolidColorBrush(dotColor);

            if (Settings.ShowBackground && _bg != null)
            {
                _bg.Background = Color.TryParse(Settings.BackgroundColor, out var bg)
                    ? new SolidColorBrush(bg)
                    : new SolidColorBrush(Color.Parse("#202196F3"));
                _bg.BorderThickness = new Thickness(0);
            }
            else if (_bg != null)
            {
                _bg.Background = Brushes.Transparent;
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
        var examName = Settings!.ExamType == 1 ? "中考" : "高考";

        DateTime GetDateForYear(int year)
        {
            if (!string.IsNullOrWhiteSpace(Settings.CustomDate) &&
                DateTime.TryParseExact(year + "-" + Settings.CustomDate, "yyyy-M-d", CultureInfo.InvariantCulture, DateTimeStyles.None, out var cd))
                return cd;
            return ExamDateData.GetExamDate(year, Settings.ExamType, Settings.City);
        }

        var current = GetDateForYear(now.Year);
        if (current.Date < now.Date)
        {
            if (Settings.RepeatYearly)
                current = GetDateForYear(now.Year + 1);
        }

        return (examName, current);
    }
}
