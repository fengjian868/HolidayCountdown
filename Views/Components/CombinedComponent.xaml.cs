using System;
using System.Linq;
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
    "F1E2D3C4-B5A6-7890-1234-567890ABCDEF",
    "节假日+问候语",
    "\uE8F5",
    "合并显示节假日倒计时和时段问候语，可配置是否分开"
)]
public class CombinedComponent : ComponentBase
{
    private HolidayService _svc = null!;
    private DispatcherTimer _timer = null!;
    private StackPanel _main = null!;

    public CombinedComponent()
    {
        _main = new StackPanel { Orientation = Orientation.Vertical, Spacing = 2, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        Content = _main;
        Dispatcher.UIThread.Post(() =>
        {
            _svc = new HolidayService();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _timer.Tick += (s, e) => Update();
            _timer.Start();
            // 订阅设置变更事件，保存后立即刷新
            HolidayService.SettingsChanged += OnSettingsChanged;
            Update();
        });
    }

    void OnSettingsChanged()
    {
        _svc?.LoadSettings();
        Dispatcher.UIThread.Post(Update);
    }

    void Update()
    {
        _main.Children.Clear();
        if (_svc == null) return;

        // 问候语行
        var greet = GetGreetingText();
        if (!string.IsNullOrEmpty(greet))
        {
            _main.Children.Add(new TextBlock
            {
                Text = greet,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = 0.9
            });
        }

        // 调休提醒
        var wr = _svc.GetNextWorkdayReminder();
        if (wr != null)
        {
            var rd = (int)(wr.Date.Date - DateTime.Now.Date).TotalDays;
            if (rd <= _svc.Settings.WorkdayReminderDays)
            {
                _main.Children.Add(new TextBlock
                {
                    Text = rd == 0 ? "⚠️ 明天调休上课" : $"⚠️ {rd}天后调休上课",
                    Foreground = new SolidColorBrush(Colors.Orange),
                    FontWeight = FontWeight.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 11
                });
            }
        }

        // 节假日横向排列（只显示官方节假日，不包含自定义节日）
        var hs = _svc.GetNextHolidays(_svc.Settings.DisplayCount);
        if (hs.Count > 0)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            for (int i = 0; i < hs.Count; i++)
            {
                var h = hs[i];
                var days = (int)(h.Date.Date - DateTime.Now.Date).TotalDays;
                var color = _svc.Settings.AutoHolidayColor ? _svc.GetHolidayColor(h.Name) : Color.Parse("#2196F3");

                var item = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };

                if (_svc.Settings.ShowProgressRing && i == 0)
                {
                    var prev = _svc.GetPrevHoliday();
                    item.Children.Add(CreateArcRing(days, prev, h, color));
                }
                else
                {
                    item.Children.Add(new TextBlock { Text = h.IsCustom ? "🎂" : "📅", VerticalAlignment = VerticalAlignment.Center, FontSize = 13 });
                }

                // 文字信息垂直排列在进度环右侧
                var textCol = new StackPanel { Orientation = Orientation.Vertical, Spacing = 0, VerticalAlignment = VerticalAlignment.Center };
                textCol.Children.Add(new TextBlock { Text = h.Name, Foreground = new SolidColorBrush(color), FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center, FontSize = 12 });
                var daysText = days == 0 ? "就是今天！" : $"还有 {days} 天";
                if (_svc.Settings.ShowDaysOff && h.DaysOff > 1 && days >= 0)
                    daysText += $"（放{h.DaysOff}天）";
                textCol.Children.Add(new TextBlock { Text = daysText, VerticalAlignment = VerticalAlignment.Center, Opacity = 0.8, FontSize = 11 });
                item.Children.Add(textCol);

                row.Children.Add(item);
            }
            _main.Children.Add(row);
        }
        else
        {
            _main.Children.Add(new TextBlock { Text = "暂无节假日", HorizontalAlignment = HorizontalAlignment.Center, Opacity = 0.5 });
        }

        // 放假占比
        if (_svc.Settings.ShowYearRatio)
        {
            var ratio = _svc.GetYearRatio();
            _main.Children.Add(new TextBlock
            {
                Text = $"当年假期剩余 {ratio:P0}",
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 10,
                Opacity = 0.6
            });
        }
    }

    string GetGreetingText()
    {
        if (!_svc.Settings.ShowGreeting) return "";
        var now = DateTime.Now;
        var s = _svc.Settings;
        var ct = now.TimeOfDay;

        // 1. 特殊日期问候（最高优先级）
        var special = s.SpecialDateGreetings.FirstOrDefault(sg =>
        {
            if (!sg.Enabled) return false;
            if ((int)now.DayOfWeek == 0 ? sg.DayOfWeek != 7 : (int)now.DayOfWeek != sg.DayOfWeek) return false;
            var start = new TimeSpan(sg.StartHour, sg.StartMinute, 0);
            var end = new TimeSpan(sg.EndHour, sg.EndMinute, 0);
            return ct >= start && ct < end;
        });
        if (special != null) return special.Text;

        // 2. 周日晚修提醒
        if (s.ShowSundayEveningStudy && now.DayOfWeek == DayOfWeek.Sunday && now.Hour >= 17 && now.Hour <= 21)
            return s.SundayEveningStudyText;

        // 3. 放学提醒
        var se = new TimeSpan(s.SchoolEndHour, s.SchoolEndMinute, 0);
        var rb = se - TimeSpan.FromMinutes(s.SchoolEndReminderMinutes);
        if (ct >= se) return s.AfterSchoolEndText;
        if (ct >= rb) return $"{s.BeforeSchoolEndText}（还有{(int)(se - ct).TotalMinutes}分钟）";

        // 4. 时段问候（最低优先级）
        var slot = s.TimeSlotGreetings.FirstOrDefault(ts =>
        {
            var start = new TimeSpan(ts.StartHour, ts.StartMinute, 0);
            var end = new TimeSpan(ts.EndHour, ts.EndMinute, 0);
            return ct >= start && ct < end;
        });
        if (slot != null) return slot.Text;

        return "";
    }

    Control CreateArcRing(int days, Holiday? prev, Holiday next, Color color)
    {
        // 固定大小的进度环容器，避免穿模
        var container = new Border
        {
            Width = 40,
            Height = 40,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Background = Brushes.Transparent
        };

        var inner = new Grid { Width = 36, Height = 36, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };

        // 背景弧
        inner.Children.Add(new Arc
        {
            Width = 36, Height = 36,
            StartAngle = -90, SweepAngle = 360,
            Stroke = new SolidColorBrush(Color.Parse("#20FFFFFF")),
            StrokeThickness = 3,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        // 进度弧
        double progress = 0;
        if (prev != null)
        {
            var total = (next.Date - prev.Date).TotalDays;
            var passed = (DateTime.Now - prev.Date).TotalDays;
            progress = Math.Max(0, Math.Min(1, passed / total));
        }
        else progress = Math.Max(0, Math.Min(1, 1 - days / 30.0));

        inner.Children.Add(new Arc
        {
            Width = 36, Height = 36,
            StartAngle = -90, SweepAngle = progress * 360,
            Stroke = new SolidColorBrush(color),
            StrokeThickness = 3,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        // 圈内显示节假日的日期（几号）
        inner.Children.Add(new TextBlock
        {
            Text = next.Date.Day.ToString(),
            FontSize = 10, FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(color),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        container.Child = inner;
        return container;
    }
}
