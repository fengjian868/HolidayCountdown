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
    "💬",
    "显示时段问候语、放学提醒、每周提醒等"
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

            // 2. 每周提醒
            if (_svc.Settings.WeeklyReminderEnabled)
            {
                var reminderDay = _svc.Settings.WeeklyReminderDay;
                var startHour = _svc.Settings.WeeklyReminderStartHour;
                var endHour = _svc.Settings.WeeklyReminderEndHour;
                if (dow == reminderDay && hour >= startHour && hour <= endHour)
                {
                    var dayName = GetDayName(dow);
                    var weeklyText = LocalGreetingDB.GetDaily(dayName, LocalGreetingDB.WeeklyReminders);
                    if (!string.IsNullOrEmpty(weeklyText))
                    {
                        _txt.Text = weeklyText;
                        return;
                    }
                }
            }

            // 3. 周日晚上晚修提醒
            if (dow == 7 && _svc.Settings.ShowSundayEveningStudy)
            {
                if (hour >= 18)
                {
                    _txt.Text = _svc.Settings.SundayEveningStudyText;
                    return;
                }
            }

            // 4. 特殊日期问候
            foreach (var special in _svc.Settings.SpecialDateGreetings)
            {
                if (!special.Enabled) continue;
                if (special.DayOfWeek == dow &&
                    hour >= special.StartHour && (hour > special.StartHour || minute >= special.StartMinute) &&
                    hour <= special.EndHour && (hour < special.EndHour || minute <= special.EndMinute))
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

            // 5. 时段问候语
            foreach (var slot in _svc.Settings.TimeSlotGreetings.OrderBy(x => x.StartHour * 60 + x.StartMinute))
            {
                var startMin = slot.StartHour * 60 + slot.StartMinute;
                var endMin = slot.EndHour * 60 + slot.EndMinute;
                var nowMin = hour * 60 + minute;
                if (nowMin >= startMin && nowMin < endMin)
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

    string GetDayName(int dow)
    {
        return dow switch
        {
            1 => "周一",
            2 => "周二",
            3 => "周三",
            4 => "周四",
            5 => "周五",
            6 => "周六",
            7 => "周日",
            _ => "周一"
        };
    }
}
