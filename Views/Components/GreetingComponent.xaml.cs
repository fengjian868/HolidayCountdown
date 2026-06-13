using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.Components;

[ComponentInfo(
    "B2C3D4E5-F6A7-8901-BCDE-F12345678901",
    "时段问候语",
    "\uE9D2",
    "根据时间显示早中晚问候、放学提醒和周日晚修提示"
)]
public class GreetingComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private TextBlock _txt = null!;
    private HolidayService? _svc;

    public GreetingComponent()
    {
        var panel = new Grid { ColumnDefinitions = new ColumnDefinitions("*"), VerticalAlignment = VerticalAlignment.Center };
        _txt = new TextBlock { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Opacity = 0.9 };
        Grid.SetColumn(_txt, 0); panel.Children.Add(_txt); Content = panel;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) }; _timer.Tick += (s, e) => Update(); _timer.Start();
        // 联网刷新问候语，每5分钟一次
        var refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
        refreshTimer.Tick += async (s, e) =>
        {
            if (_svc != null && _svc.Settings.GreetingOnline)
                await _svc.RefreshGreetingsAsync();
        };
        refreshTimer.Start();
        Dispatcher.UIThread.Post(() => { _svc = new HolidayService(); HolidayService.SettingsChanged += OnSettingsChanged; Update(); });
    }

    void OnSettingsChanged()
    {
        _svc?.LoadSettings();
        Dispatcher.UIThread.Post(Update);
    }

    void Update()
    {
        if (_svc == null || !_svc.Settings.ShowGreeting) { _txt.Text = ""; return; }
        var now = DateTime.Now; var s = _svc.Settings; var ct = now.TimeOfDay;

        // 1. 特殊日期问候（最高优先级）
        var special = s.SpecialDateGreetings.FirstOrDefault(sg =>
        {
            if (!sg.Enabled) return false;
            if ((int)now.DayOfWeek == 0 ? sg.DayOfWeek != 7 : (int)now.DayOfWeek != sg.DayOfWeek) return false;
            var start = new TimeSpan(sg.StartHour, sg.StartMinute, 0);
            var end = new TimeSpan(sg.EndHour, sg.EndMinute, 0);
            return ct >= start && ct < end;
        });
        if (special != null) { _txt.Text = special.Text; return; }

        // 2. 周日晚修提醒
        if (s.ShowSundayEveningStudy && now.DayOfWeek == DayOfWeek.Sunday && now.Hour >= 17 && now.Hour <= 21) { _txt.Text = s.SundayEveningStudyText; return; }

        // 3. 放学提醒
        var se = new TimeSpan(s.SchoolEndHour, s.SchoolEndMinute, 0); var rb = se - TimeSpan.FromMinutes(s.SchoolEndReminderMinutes);
        if (ct >= se) { _txt.Text = s.AfterSchoolEndText; return; }
        if (ct >= rb) { _txt.Text = $"{s.BeforeSchoolEndText}（还有{(int)(se - ct).TotalMinutes}分钟）"; return; }

        // 4. 时段问候（最低优先级）
        var slot = s.TimeSlotGreetings.FirstOrDefault(ts =>
        {
            var start = new TimeSpan(ts.StartHour, ts.StartMinute, 0);
            var end = new TimeSpan(ts.EndHour, ts.EndMinute, 0);
            return ct >= start && ct < end;
        });
        if (slot != null) { _txt.Text = slot.Text; return; }

        _txt.Text = "";
    }
}
