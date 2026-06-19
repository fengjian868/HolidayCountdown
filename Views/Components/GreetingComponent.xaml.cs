using System;
using System.Linq;
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
    "B2C3D4E5-F6A7-8901-BCDE-F23456789013",
    "问候语",
    "fluent(\uE8BD)",
    "显示时段问候语、放学提醒、特殊日期问候等"
)]
public class GreetingComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private TextBlock _txt = null!;
    private HolidayService? _svc;

    public GreetingComponent()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        _txt = new TextBlock { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Opacity = 0.9, Foreground = Brushes.Black };
        panel.Children.Add(_txt);
        Content = panel;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
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
        if (_svc == null) { _txt.Text = ""; return; }

        try
        {
            var now = DateTime.Now;
            var dow = (int)now.DayOfWeek; if (dow == 0) dow = 7;
            var hour = now.Hour;
            var minute = now.Minute;

            // 每天自动刷新问候语（如果开启且今天还没刷新过）
            if (_svc.Settings.AutoRefreshGreetings)
            {
                var today = now.Date;
                if (_svc.Settings.LastGreetingRefreshDate != today)
                {
                    RefreshDailyGreetings();
                    _svc.Settings.LastGreetingRefreshDate = today;
                    _svc.SaveSettings();
                }
            }

            // 1. 放学提醒
            var schoolEnd = new DateTime(now.Year, now.Month, now.Day, _svc.Settings.SchoolEndHour, _svc.Settings.SchoolEndMinute, 0);
            var reminderStart = schoolEnd.AddMinutes(-_svc.Settings.SchoolEndReminderMinutes);
            if (now >= reminderStart && now < schoolEnd)
            {
                _txt.Text = _svc.Settings.BeforeSchoolEndText;
                return;
            }
            if (now >= schoolEnd)
            {
                _txt.Text = _svc.Settings.AfterSchoolEndText;
                return;
            }

            // 2. 周日晚上晚修提醒
            if (dow == 7 && _svc.Settings.ShowSundayEveningStudy)
            {
                if (hour >= 18)
                {
                    _txt.Text = _svc.Settings.SundayEveningStudyText;
                    return;
                }
            }

            // 4. 特殊日期问候
            foreach (var special in _svc.Settings.SpecialDateGreetings.OrderBy(x => x.StartHour * 60 + x.StartMinute))
            {
                if (!special.Enabled) continue;
                if (special.DayOfWeek == dow && IsInTimeRange(special.StartHour, special.StartMinute, special.EndHour, special.EndMinute, hour, minute))
                {
                    if (!string.IsNullOrEmpty(special.Text))
                    {
                        _txt.Text = special.Text;
                        return;
                    }
                    // 如果特殊日期文本为空，使用本地数据库按标签获取
                    if (!string.IsNullOrEmpty(special.Tag))
                    {
                        var tagText = LocalGreetingDB.GetDaily(special.Tag, LocalGreetingDB.WeeklyReminders);
                        if (!string.IsNullOrEmpty(tagText))
                        {
                            _txt.Text = tagText;
                            return;
                        }
                    }
                }
            }

            // 5. 时段问候语：按开始时间排序，支持跨天，取第一个匹配
            foreach (var slot in _svc.Settings.TimeSlotGreetings.OrderBy(x => x.StartHour * 60 + x.StartMinute))
            {
                if (IsInTimeRange(slot.StartHour, slot.StartMinute, slot.EndHour, slot.EndMinute, hour, minute))
                {
                    if (!string.IsNullOrEmpty(slot.Text))
                    {
                        _txt.Text = slot.Text;
                        return;
                    }
                    // 如果时段文本为空，使用本地数据库按标签获取
                    if (!string.IsNullOrEmpty(slot.Tag))
                    {
                        var tagText = LocalGreetingDB.GetDaily(slot.Tag, LocalGreetingDB.TimeSlotGreetings);
                        if (!string.IsNullOrEmpty(tagText))
                        {
                            _txt.Text = tagText;
                            return;
                        }
                    }
                }
            }

            _txt.Text = "";
        }
        catch { _txt.Text = ""; }
    }

    void RefreshDailyGreetings()
    {
        if (_svc == null) return;
        try
        {
            // 刷新时段问候语
            foreach (var slot in _svc.Settings.TimeSlotGreetings)
            {
                if (string.IsNullOrEmpty(slot.Text) && !string.IsNullOrEmpty(slot.Tag))
                {
                    var tagText = LocalGreetingDB.GetDaily(slot.Tag, LocalGreetingDB.TimeSlotGreetings);
                    if (!string.IsNullOrEmpty(tagText)) slot.Text = tagText;
                }
            }
            // 刷新特殊日期问候语
            foreach (var special in _svc.Settings.SpecialDateGreetings)
            {
                if (string.IsNullOrEmpty(special.Text) && !string.IsNullOrEmpty(special.Tag))
                {
                    var tagText = LocalGreetingDB.GetDaily(special.Tag, LocalGreetingDB.WeeklyReminders);
                    if (!string.IsNullOrEmpty(tagText)) special.Text = tagText;
                }
            }
        }
        catch { }
    }

    /// <summary>
    /// 判断当前时间是否落在 [start, end) 区间内；结束时间小于等于开始时间时按跨天处理。
    /// </summary>
    static bool IsInTimeRange(int startHour, int startMinute, int endHour, int endMinute, int nowHour, int nowMinute)
    {
        var start = startHour * 60 + startMinute;
        var end = endHour * 60 + endMinute;
        var now = nowHour * 60 + nowMinute;
        if (end <= start) // 跨天，例如 18:00 ~ 05:00
            return now >= start || now < end;
        return now >= start && now < end;
    }
}
