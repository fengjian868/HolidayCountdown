using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.SettingsPages;

[SettingsPageInfo("holidaycountdown.settings", "节假日倒计时", "\uE8F5", "\uE8F5")]
public class UnifiedSettingsPage : SettingsPageBase
{
    private readonly HolidayService _svc;
    private StackPanel _contentPanel = null!;
    private ScrollViewer _scrollViewer = null!;

    // Tab definitions
    private readonly (string Icon, string Label, Func<Control> Build)[] _tabs;

    public UnifiedSettingsPage()
    {
        _svc = new HolidayService();
        _tabs = new (string, string, Func<Control>)[]
        {
            ("\uE8F5", "节假日", BuildHolidayPanel),
            ("\uE9D2", "问候语", BuildGreetingPanel),
            ("\uE9CA", "24节气", BuildSolarTermPanel),
            ("\uE787", "农历", BuildLunarPanel),
            ("\uE915", "自定义", BuildCustomHolidayPanel),
            ("\uE7BE", "寒暑假", BuildVacationPanel),
            ("\uE753", "天气", BuildWeatherPanel),
            ("\uE946", "关于", BuildAboutPanel),
        };
        Content = Build();
    }

    Control Build()
    {
        var root = new StackPanel { Spacing = 0 };

        // Top tab bar
        var tabBar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            Margin = new Thickness(16, 12, 16, 8),
            HorizontalAlignment = HorizontalAlignment.Left
        };

        for (int i = 0; i < _tabs.Length; i++)
        {
            var idx = i;
            var tab = _tabs[i];
            var btn = new Button
            {
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 4,
                    Children =
                    {
                        new TextBlock { Text = tab.Icon, FontSize = 14, VerticalAlignment = VerticalAlignment.Center },
                        new TextBlock { Text = tab.Label, FontSize = 13, VerticalAlignment = VerticalAlignment.Center }
                    }
                },
                Padding = new Thickness(10, 6),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(6),
                Tag = idx
            };
            btn.Click += (s, e) => SwitchTab(idx);
            tabBar.Children.Add(btn);
        }

        // Separator under tabs
        var sep = new Border
        {
            Height = 1,
            Background = new SolidColorBrush(SettingsUI.SeparatorColor),
            Margin = new Thickness(16, 0, 16, 8)
        };

        // Content area
        _contentPanel = new StackPanel { Spacing = 0, Margin = new Thickness(20, 8, 20, 16) };
        _scrollViewer = new ScrollViewer { Content = _contentPanel };

        root.Children.Add(tabBar);
        root.Children.Add(sep);
        root.Children.Add(_scrollViewer);

        // Select first tab by default
        SwitchTab(0);
        return root;
    }

    void SwitchTab(int index)
    {
        // Update button styles
        var tabBar = (Content as StackPanel)?.Children[0] as StackPanel;
        if (tabBar != null)
        {
            foreach (var child in tabBar.Children)
            {
                if (child is Button btn && btn.Tag is int idx)
                {
                    bool active = idx == index;
                    btn.Background = active
                        ? new SolidColorBrush(SettingsUI.AccentColor)
                        : Brushes.Transparent;
                    btn.Opacity = active ? 1.0 : 0.7;
                }
            }
        }

        // Switch content
        _contentPanel.Children.Clear();
        if (index >= 0 && index < _tabs.Length)
        {
            var control = _tabs[index].Build();
            _contentPanel.Children.Add(control);
        }
    }

    // ===== Tab Builders =====

    Control BuildHolidayPanel()
    {
        var s = new StackPanel { Spacing = 0 };
        s.Children.Add(SettingsUI.PageHeader("📅 节假日倒计时设置"));

        var displayPanel = new StackPanel { Spacing = 0 };
        displayPanel.Children.Add(SettingsUI.SettingItem("显示数量", "同时显示多少个节日",
            SettingsUI.Combo(new[] { "1", "3", "5" }, _svc.Settings.DisplayCount == 1 ? 0 : _svc.Settings.DisplayCount == 3 ? 1 : 2,
                v => _svc.Settings.DisplayCount = v == 0 ? 1 : v == 1 ? 3 : 5)));
        displayPanel.Children.Add(SettingsUI.Separator());
        displayPanel.Children.Add(SettingsUI.SettingItem("显示放假天数", "如：春节（放7天）",
            SettingsUI.Toggle(_svc.Settings.ShowDaysOff, v => _svc.Settings.ShowDaysOff = v)));
        displayPanel.Children.Add(SettingsUI.Separator());
        displayPanel.Children.Add(SettingsUI.SettingItem("显示小时数", "节日当天显示剩余小时",
            SettingsUI.Toggle(_svc.Settings.ShowHours, v => _svc.Settings.ShowHours = v)));
        displayPanel.Children.Add(SettingsUI.Separator());
        displayPanel.Children.Add(SettingsUI.SettingItem("显示进度环", "首个节日显示弧形进度",
            SettingsUI.Toggle(_svc.Settings.ShowProgressRing, v => _svc.Settings.ShowProgressRing = v)));
        displayPanel.Children.Add(SettingsUI.Separator());
        displayPanel.Children.Add(SettingsUI.SettingItem("自动播放下一个", "节日过后自动显示下一个",
            SettingsUI.Toggle(_svc.Settings.AutoNextHoliday, v => _svc.Settings.AutoNextHoliday = v)));
        displayPanel.Children.Add(SettingsUI.Separator());
        displayPanel.Children.Add(SettingsUI.SettingItem("显示假期占比", "当年剩余假期百分比",
            SettingsUI.Toggle(_svc.Settings.ShowYearRatio, v => _svc.Settings.ShowYearRatio = v)));
        displayPanel.Children.Add(SettingsUI.Separator());
        displayPanel.Children.Add(SettingsUI.SettingItem("周末倒计时", "列表中显示周六周日",
            SettingsUI.Toggle(_svc.Settings.ShowWeekendCountdown, v => _svc.Settings.ShowWeekendCountdown = v)));
        s.Children.Add(SettingsUI.Expander("显示", "节假日组件的显示选项", displayPanel));

        var workdayPanel = new StackPanel { Spacing = 0 };
        workdayPanel.Children.Add(SettingsUI.SettingItem("调休提醒", "周末调休上课提前提醒",
            SettingsUI.Toggle(_svc.Settings.ShowWorkdayReminder, v => _svc.Settings.ShowWorkdayReminder = v)));
        workdayPanel.Children.Add(SettingsUI.Separator());
        workdayPanel.Children.Add(SettingsUI.SettingItem("提前提醒天数", "调休提醒提前多少天显示",
            SettingsUI.Number(_svc.Settings.WorkdayReminderDays, 1, 30, v => _svc.Settings.WorkdayReminderDays = v)));
        s.Children.Add(SettingsUI.Expander("调休", "调休上课提醒设置", workdayPanel));

        var colorPanel = new StackPanel { Spacing = 0 };
        colorPanel.Children.Add(SettingsUI.SettingItem("自动节日颜色", "根据节日自动匹配颜色",
            SettingsUI.Toggle(_svc.Settings.AutoHolidayColor, v => _svc.Settings.AutoHolidayColor = v)));
        colorPanel.Children.Add(SettingsUI.Separator());
        foreach (var kv in _svc.Settings.HolidayColors.ToList())
        {
            var key = kv.Key;
            colorPanel.Children.Add(SettingsUI.SettingItem(key, null,
                SettingsUI.ColorPicker(kv.Value, c => _svc.Settings.HolidayColors[key] = c)));
            if (key != _svc.Settings.HolidayColors.Keys.Last())
                colorPanel.Children.Add(SettingsUI.Separator());
        }
        s.Children.Add(SettingsUI.Expander("颜色", "节日颜色自定义", colorPanel));

        var switchPanel = new StackPanel { Spacing = 0 };
        var allHolidays = new[] { "元旦", "春节", "清明节", "劳动节", "端午节", "中秋节", "国庆节" };
        foreach (var name in allHolidays)
        {
            var disabled = _svc.Settings.DisabledHolidays.Contains(name);
            var chk = new CheckBox { Content = name, IsChecked = !disabled };
            chk.IsCheckedChanged += (a, b) =>
            {
                if (chk.IsChecked == true) _svc.Settings.DisabledHolidays.Remove(name);
                else if (!_svc.Settings.DisabledHolidays.Contains(name)) _svc.Settings.DisabledHolidays.Add(name);
                _svc.SaveSettings();
            };
            switchPanel.Children.Add(SettingsUI.SettingItem(name, null, chk));
            if (name != allHolidays.Last())
                switchPanel.Children.Add(SettingsUI.Separator());
        }
        s.Children.Add(SettingsUI.Expander("节日开关", "选择要显示的节假日", switchPanel));

        s.Children.Add(SettingsUI.SaveButton(() => _svc.SaveSettings()));
        return s;
    }

    Control BuildGreetingPanel()
    {
        var s = new StackPanel { Spacing = 0 };
        s.Children.Add(SettingsUI.PageHeader("💬 问候语设置"));

        var togglePanel = new StackPanel { Spacing = 0 };
        togglePanel.Children.Add(SettingsUI.SettingItem("启用问候语", null,
            SettingsUI.Toggle(_svc.Settings.ShowGreeting, v => _svc.Settings.ShowGreeting = v)));
        togglePanel.Children.Add(SettingsUI.Separator());
        togglePanel.Children.Add(SettingsUI.SettingItem("合并到节假日组件", "问候语显示在节假日下方",
            SettingsUI.Toggle(_svc.Settings.MergeGreeting, v => _svc.Settings.MergeGreeting = v)));
        s.Children.Add(SettingsUI.Expander("开关", "问候语基础设置", togglePanel));

        var weeklyPanel = new StackPanel { Spacing = 0 };
        weeklyPanel.Children.Add(SettingsUI.SettingItem("启用每周提醒", null,
            SettingsUI.Toggle(_svc.Settings.WeeklyReminderEnabled, v => _svc.Settings.WeeklyReminderEnabled = v)));
        weeklyPanel.Children.Add(SettingsUI.Separator());
        var dayCombo = new ComboBox { Width = 80, HorizontalAlignment = HorizontalAlignment.Right };
        var days = new[] { "周一", "周二", "周三", "周四", "周五", "周六", "周日" };
        foreach (var d in days) dayCombo.Items.Add(d);
        dayCombo.SelectedIndex = _svc.Settings.WeeklyReminderDay == 7 ? 6 : _svc.Settings.WeeklyReminderDay - 1;
        dayCombo.SelectionChanged += (a, b) => _svc.Settings.WeeklyReminderDay = dayCombo.SelectedIndex == 6 ? 7 : dayCombo.SelectedIndex + 1;
        weeklyPanel.Children.Add(SettingsUI.SettingItem("提醒日期", "每周哪天显示提醒", dayCombo));
        weeklyPanel.Children.Add(SettingsUI.Separator());
        weeklyPanel.Children.Add(SettingsUI.SettingItem("开始时间（时）", "提醒开始显示的小时",
            SettingsUI.Number(_svc.Settings.WeeklyReminderStartHour, 0, 23, v => _svc.Settings.WeeklyReminderStartHour = v)));
        weeklyPanel.Children.Add(SettingsUI.Separator());
        weeklyPanel.Children.Add(SettingsUI.SettingItem("结束时间（时）", "提醒结束显示的小时",
            SettingsUI.Number(_svc.Settings.WeeklyReminderEndHour, 0, 23, v => _svc.Settings.WeeklyReminderEndHour = v)));
        weeklyPanel.Children.Add(SettingsUI.Separator());
        weeklyPanel.Children.Add(SettingsUI.Info("内置提醒按标签分类，每天自动从本地随机刷新一条。标签：周一/周二/周三/周四/周五/周末"));
        s.Children.Add(SettingsUI.Expander("每周提醒", "自定义每周提醒的日期和时段", weeklyPanel));

        var schoolPanel = new StackPanel { Spacing = 0 };
        schoolPanel.Children.Add(SettingsUI.SettingItem("放学时间", "时:分",
            new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, HorizontalAlignment = HorizontalAlignment.Right }.Also(h =>
            {
                h.Children.Add(SettingsUI.Text(_svc.Settings.SchoolEndHour.ToString("D2"), 40, v =>
                {
                    if (int.TryParse(v, out var hval)) _svc.Settings.SchoolEndHour = Math.Max(0, Math.Min(23, hval));
                }));
                h.Children.Add(new TextBlock { Text = ":", VerticalAlignment = VerticalAlignment.Center });
                h.Children.Add(SettingsUI.Text(_svc.Settings.SchoolEndMinute.ToString("D2"), 40, v =>
                {
                    if (int.TryParse(v, out var mval)) _svc.Settings.SchoolEndMinute = Math.Max(0, Math.Min(59, mval));
                }));
            })));
        schoolPanel.Children.Add(SettingsUI.Separator());
        schoolPanel.Children.Add(SettingsUI.SettingItem("提前提醒分钟", "放学前多少分钟切换提醒",
            SettingsUI.Number(_svc.Settings.SchoolEndReminderMinutes, 1, 60, v => _svc.Settings.SchoolEndReminderMinutes = v)));
        schoolPanel.Children.Add(SettingsUI.Separator());
        schoolPanel.Children.Add(SettingsUI.SettingItem("放学前文案", null,
            SettingsUI.Text(_svc.Settings.BeforeSchoolEndText, 200, v => _svc.Settings.BeforeSchoolEndText = v)));
        schoolPanel.Children.Add(SettingsUI.Separator());
        schoolPanel.Children.Add(SettingsUI.SettingItem("放学后文案", null,
            SettingsUI.Text(_svc.Settings.AfterSchoolEndText, 200, v => _svc.Settings.AfterSchoolEndText = v)));
        s.Children.Add(SettingsUI.Expander("放学", "放学提醒设置", schoolPanel));

        s.Children.Add(SettingsUI.Expander("时段文案", "自定义多个时间段的问候语", BuildTimeSlotPanel()));
        s.Children.Add(SettingsUI.Expander("特殊日期", "设置特定星期几的问候语", BuildSpecialDatePanel()));

        s.Children.Add(SettingsUI.SaveButton(() => _svc.SaveSettings()));
        return s;
    }

    StackPanel BuildTimeSlotPanel()
    {
        var panel = new StackPanel { Spacing = 8 };
        var listPanel = new StackPanel { Spacing = 0 };

        void RefreshList()
        {
            listPanel.Children.Clear();
            var slots = _svc.Settings.TimeSlotGreetings.OrderBy(x => x.StartHour * 60 + x.StartMinute).ToList();
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(16, 8, 16, 8) };
                var startBox = SettingsUI.Text($"{slot.StartHour:D2}:{slot.StartMinute:D2}", 50, v =>
                {
                    if (TimeSpan.TryParse(v, out var ts)) { slot.StartHour = ts.Hours; slot.StartMinute = ts.Minutes; }
                });
                var endBox = SettingsUI.Text($"{slot.EndHour:D2}:{slot.EndMinute:D2}", 50, v =>
                {
                    if (TimeSpan.TryParse(v, out var ts)) { slot.EndHour = ts.Hours; slot.EndMinute = ts.Minutes; }
                });
                var textBox = SettingsUI.Text(slot.Text, 180, v => slot.Text = v);
                var delBtn = new Button { Content = "🗑️", Padding = new Thickness(4, 2) };
                delBtn.Click += (a, e) => { _svc.Settings.TimeSlotGreetings.Remove(slot); RefreshList(); };

                row.Children.Add(new TextBlock { Text = "从", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                row.Children.Add(startBox);
                row.Children.Add(new TextBlock { Text = "到", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                row.Children.Add(endBox);
                row.Children.Add(textBox);
                row.Children.Add(delBtn);
                listPanel.Children.Add(row);
                if (i < slots.Count - 1)
                    listPanel.Children.Add(SettingsUI.Separator());
            }
        }

        RefreshList();
        panel.Children.Add(listPanel);

        var addBtn = new Button { Content = "+ 添加时段", Padding = new Thickness(12, 4), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(16, 4, 16, 8) };
        addBtn.Click += (a, e) =>
        {
            _svc.Settings.TimeSlotGreetings.Add(new Models.TimeSlotGreeting { StartHour = 8, StartMinute = 0, EndHour = 12, EndMinute = 0, Text = "" });
            RefreshList();
        };
        panel.Children.Add(addBtn);

        panel.Children.Add(SettingsUI.Info("留空的时段会自动使用本地数据库按标签（早晨/上午/中午/下午/傍晚/夜晚）每天刷新一条问候语"));

        return panel;
    }

    StackPanel BuildSpecialDatePanel()
    {
        var panel = new StackPanel { Spacing = 8 };
        var listPanel = new StackPanel { Spacing = 0 };

        void RefreshList()
        {
            listPanel.Children.Clear();
            var items = _svc.Settings.SpecialDateGreetings.ToList();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var row = new StackPanel { Spacing = 4, Margin = new Thickness(16, 8, 16, 8) };

                var headerRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
                var nameBox = SettingsUI.Text(item.Name, 80, v => item.Name = v);
                var dayCombo = new ComboBox { Width = 70 };
                var days = new[] { "周一", "周二", "周三", "周四", "周五", "周六", "周日" };
                foreach (var d in days) dayCombo.Items.Add(d);
                dayCombo.SelectedIndex = Math.Max(0, Math.Min(6, item.DayOfWeek - 1));
                dayCombo.SelectionChanged += (a, b) => item.DayOfWeek = dayCombo.SelectedIndex + 1;
                var enabledChk = new CheckBox { Content = "启用", IsChecked = item.Enabled };
                enabledChk.IsCheckedChanged += (a, b) => item.Enabled = enabledChk.IsChecked == true;
                var delBtn = new Button { Content = "🗑️", Padding = new Thickness(4, 2) };
                delBtn.Click += (a, e) => { _svc.Settings.SpecialDateGreetings.Remove(item); RefreshList(); };

                headerRow.Children.Add(nameBox);
                headerRow.Children.Add(dayCombo);
                headerRow.Children.Add(enabledChk);
                headerRow.Children.Add(delBtn);
                row.Children.Add(headerRow);

                var timeRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
                var startBox = SettingsUI.Text($"{item.StartHour:D2}:{item.StartMinute:D2}", 50, v =>
                {
                    if (TimeSpan.TryParse(v, out var ts)) { item.StartHour = ts.Hours; item.StartMinute = ts.Minutes; }
                });
                var endBox = SettingsUI.Text($"{item.EndHour:D2}:{item.EndMinute:D2}", 50, v =>
                {
                    if (TimeSpan.TryParse(v, out var ts)) { item.EndHour = ts.Hours; item.EndMinute = ts.Minutes; }
                });
                var textBox = SettingsUI.Text(item.Text, 180, v => item.Text = v);

                timeRow.Children.Add(new TextBlock { Text = "从", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                timeRow.Children.Add(startBox);
                timeRow.Children.Add(new TextBlock { Text = "到", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                timeRow.Children.Add(endBox);
                timeRow.Children.Add(textBox);
                row.Children.Add(timeRow);

                listPanel.Children.Add(row);
                if (i < items.Count - 1)
                    listPanel.Children.Add(SettingsUI.Separator());
            }
        }

        RefreshList();
        panel.Children.Add(listPanel);

        var addBtn = new Button { Content = "+ 添加特殊日期", Padding = new Thickness(12, 4), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(16, 4, 16, 8) };
        addBtn.Click += (a, e) =>
        {
            _svc.Settings.SpecialDateGreetings.Add(new Models.SpecialDateGreeting { Name = "新日期", DayOfWeek = 1, StartHour = 0, StartMinute = 0, EndHour = 23, EndMinute = 59, Text = "" });
            RefreshList();
        };
        panel.Children.Add(addBtn);

        return panel;
    }

    Control BuildSolarTermPanel()
    {
        var s = new StackPanel { Spacing = 0 };
        s.Children.Add(SettingsUI.PageHeader("🌿 24节气设置"));

        var displayPanel = new StackPanel { Spacing = 0 };
        displayPanel.Children.Add(SettingsUI.SettingItem("显示进度环", "弧形进度环显示节气进度",
            SettingsUI.Toggle(_svc.Settings.SolarTermShowProgressRing, v => _svc.Settings.SolarTermShowProgressRing = v)));
        s.Children.Add(SettingsUI.Expander("显示", "节气组件显示选项", displayPanel));

        var colorPanel = new StackPanel { Spacing = 0 };
        var colors = _svc.Settings.TermColors.OrderBy(x => x.Key).ToList();
        for (int i = 0; i < colors.Count; i++)
        {
            var kv = colors[i];
            var key = kv.Key;
            colorPanel.Children.Add(SettingsUI.SettingItem(key, null,
                SettingsUI.ColorPicker(kv.Value, c => _svc.Settings.TermColors[key] = c)));
            if (i < colors.Count - 1)
                colorPanel.Children.Add(SettingsUI.Separator());
        }
        s.Children.Add(SettingsUI.Expander("颜色", "各节气显示颜色自定义", colorPanel));

        s.Children.Add(SettingsUI.SaveButton(() => _svc.SaveSettings()));
        return s;
    }

    Control BuildLunarPanel()
    {
        var s = new StackPanel { Spacing = 0 };
        s.Children.Add(SettingsUI.PageHeader("🌙 农历日期设置"));

        var displayPanel = new StackPanel { Spacing = 0 };
        displayPanel.Children.Add(SettingsUI.SettingItem("显示农历", null,
            SettingsUI.Toggle(_svc.Settings.ShowLunarDate, v => _svc.Settings.ShowLunarDate = v)));
        displayPanel.Children.Add(SettingsUI.Separator());
        displayPanel.Children.Add(SettingsUI.SettingItem("自动网络刷新", "有网络时自动获取最新农历",
            SettingsUI.Toggle(_svc.Settings.LunarAutoRefresh, v => _svc.Settings.LunarAutoRefresh = v)));
        s.Children.Add(SettingsUI.Expander("显示", "农历组件基础设置", displayPanel));

        var formatPanel = new StackPanel { Spacing = 0 };
        var presets = new[]
        {
            ("完整", "{gzYear} {IMonthCn}{IDayCn} {Animal}"),
            ("简洁", "{IMonthCn}{IDayCn}"),
            ("含生肖", "{IMonthCn}{IDayCn} {Animal}"),
            ("含节气", "{IMonthCn}{IDayCn} {Term}"),
        };
        var presetCombo = new ComboBox { Width = 120, HorizontalAlignment = HorizontalAlignment.Right };
        foreach (var (name, _) in presets) presetCombo.Items.Add(name);

        var currentTemplate = _svc.Settings.LunarDateTemplate ?? "{gzYear} {IMonthCn}{IDayCn} {Animal}";
        int selectedIndex = 0;
        for (int i = 0; i < presets.Length; i++)
        {
            if (presets[i].Item2 == currentTemplate) { selectedIndex = i; break; }
        }
        presetCombo.SelectedIndex = selectedIndex;
        presetCombo.SelectionChanged += (a, b) =>
        {
            if (presetCombo.SelectedIndex >= 0 && presetCombo.SelectedIndex < presets.Length)
                _svc.Settings.LunarDateTemplate = presets[presetCombo.SelectedIndex].Item2;
        };

        formatPanel.Children.Add(SettingsUI.SettingItem("选择格式", "快速选择预设模板", presetCombo));
        formatPanel.Children.Add(SettingsUI.Separator());

        var templateBox = SettingsUI.Text(_svc.Settings.LunarDateTemplate ?? "", 280, v => _svc.Settings.LunarDateTemplate = v);
        formatPanel.Children.Add(SettingsUI.SettingItem("自定义模板", null, templateBox));
        formatPanel.Children.Add(SettingsUI.Separator());
        formatPanel.Children.Add(SettingsUI.Info("可用变量: {gzYear} 干支年 | {IMonthCn} 农历月 | {IDayCn} 农历日 | {Animal} 生肖 | {Term} 节气"));
        s.Children.Add(SettingsUI.Expander("显示格式", "农历日期显示模板", formatPanel));

        s.Children.Add(SettingsUI.Info("示例: 癸卯年 九月初八 兔"));
        s.Children.Add(SettingsUI.SaveButton(() => _svc.SaveSettings()));
        return s;
    }

    Control BuildCustomHolidayPanel()
    {
        var s = new StackPanel { Spacing = 0 };
        s.Children.Add(SettingsUI.PageHeader("🎂 自定义节日设置"));

        var displayPanel = new StackPanel { Spacing = 0 };
        displayPanel.Children.Add(SettingsUI.SettingItem("显示数量", "同时显示多少个自定义节日",
            SettingsUI.Combo(new[] { "1", "2", "3", "5" },
                _svc.Settings.CustomHolidayDisplayCount == 1 ? 0 :
                _svc.Settings.CustomHolidayDisplayCount == 2 ? 1 :
                _svc.Settings.CustomHolidayDisplayCount == 3 ? 2 : 3,
                v => _svc.Settings.CustomHolidayDisplayCount = v == 0 ? 1 : v == 1 ? 2 : v == 2 ? 3 : 5)));
        displayPanel.Children.Add(SettingsUI.Separator());
        displayPanel.Children.Add(SettingsUI.SettingItem("显示图标", null,
            SettingsUI.Toggle(_svc.Settings.CustomHolidayShowIcon, v => _svc.Settings.CustomHolidayShowIcon = v)));
        displayPanel.Children.Add(SettingsUI.Separator());
        displayPanel.Children.Add(SettingsUI.SettingItem("显示天数", null,
            SettingsUI.Toggle(_svc.Settings.CustomHolidayShowDays, v => _svc.Settings.CustomHolidayShowDays = v)));
        s.Children.Add(SettingsUI.Expander("组件显示", "自定义节日组件显示选项", displayPanel));

        s.Children.Add(SettingsUI.Expander("节日列表", "添加和管理你的自定义节日", BuildCustomHolidayList()));

        s.Children.Add(SettingsUI.SaveButton(() => _svc.SaveSettings()));
        return s;
    }

    Control BuildCustomHolidayList()
    {
        var p = new StackPanel { Spacing = 0 };
        var holidays = _svc.Settings.CustomHolidays.ToList();
        for (int i = 0; i < holidays.Count; i++)
        {
            p.Children.Add(MakeCustomHolidayItem(holidays[i]));
            if (i < holidays.Count - 1)
                p.Children.Add(SettingsUI.Separator());
        }
        var btn = new Button { Content = "➕ 添加", Padding = new Thickness(12, 6), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(16, 8, 16, 8) };
        btn.Click += (a, e) =>
        {
            var h = new Models.CustomHoliday { Name = "新节日", Date = DateTime.Now.AddDays(1) };
            _svc.Settings.CustomHolidays.Add(h);
            SwitchTab(3); // Refresh by reselecting current tab
        };
        p.Children.Add(btn);
        return p;
    }

    Control MakeCustomHolidayItem(Models.CustomHoliday h)
    {
        var g = new Grid { ColumnDefinitions = new ColumnDefinitions("120 100 80 Auto Auto"), Margin = new Thickness(16, 8, 16, 8) };
        var n = new TextBox { Text = h.Name, Margin = new Thickness(0, 0, 8, 0) };
        n.TextChanged += (a, b) => h.Name = n.Text ?? "";
        Grid.SetColumn(n, 0);

        var dateText = new TextBlock { Text = $"{h.Date.Month}月{h.Date.Day}日", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(dateText, 1);

        var d = new DatePicker { SelectedDate = h.Date, Margin = new Thickness(0, 0, 8, 0) };
        d.SelectedDateChanged += (a, b) =>
        {
            if (d.SelectedDate.HasValue)
            {
                h.Date = d.SelectedDate.Value.DateTime;
                dateText.Text = $"{h.Date.Month}月{h.Date.Day}日";
            }
        };
        Grid.SetColumn(d, 2);

        var r = new CheckBox { Content = "每年", IsChecked = h.RepeatYearly, VerticalAlignment = VerticalAlignment.Center };
        r.IsCheckedChanged += (a, b) => h.RepeatYearly = r.IsChecked == true;
        Grid.SetColumn(r, 3);

        var del = new Button { Content = "删除", Width = 50 };
        del.Click += (a, b) => { _svc.Settings.CustomHolidays.Remove(h); SwitchTab(3); };
        Grid.SetColumn(del, 4);

        g.Children.Add(n); g.Children.Add(dateText); g.Children.Add(d); g.Children.Add(r); g.Children.Add(del);
        return g;
    }

    Control BuildVacationPanel()
    {
        var s = new StackPanel { Spacing = 0 };
        s.Children.Add(SettingsUI.PageHeader("🏖️ 寒暑假设置"));

        var togglePanel = new StackPanel { Spacing = 0 };
        togglePanel.Children.Add(SettingsUI.SettingItem("显示寒暑假倒计时", null,
            SettingsUI.Toggle(_svc.Settings.ShowVacationCountdown, v => _svc.Settings.ShowVacationCountdown = v)));
        s.Children.Add(SettingsUI.Expander("开关", "寒暑假组件总开关", togglePanel));

        var summerPanel = new StackPanel { Spacing = 0 };
        summerPanel.Children.Add(SettingsUI.SettingItem("开始日期", null,
            SettingsUI.Date(_svc.Settings.SummerStart, v => _svc.Settings.SummerStart = v)));
        summerPanel.Children.Add(SettingsUI.Separator());
        summerPanel.Children.Add(SettingsUI.SettingItem("结束日期", null,
            SettingsUI.Date(_svc.Settings.SummerEnd, v => _svc.Settings.SummerEnd = v)));
        s.Children.Add(SettingsUI.Expander("暑假", "暑假时间安排", summerPanel));

        var winterPanel = new StackPanel { Spacing = 0 };
        winterPanel.Children.Add(SettingsUI.SettingItem("开始日期", null,
            SettingsUI.Date(_svc.Settings.WinterStart, v => _svc.Settings.WinterStart = v)));
        winterPanel.Children.Add(SettingsUI.Separator());
        winterPanel.Children.Add(SettingsUI.SettingItem("结束日期", null,
            SettingsUI.Date(_svc.Settings.WinterEnd, v => _svc.Settings.WinterEnd = v)));
        s.Children.Add(SettingsUI.Expander("寒假", "寒假时间安排", winterPanel));

        s.Children.Add(SettingsUI.SaveButton(() => _svc.SaveSettings()));
        return s;
    }

    Control BuildWeatherPanel()
    {
        var s = new StackPanel { Spacing = 0 };
        s.Children.Add(SettingsUI.PageHeader("🌤️ 天气问候设置"));

        var togglePanel = new StackPanel { Spacing = 0 };
        togglePanel.Children.Add(SettingsUI.SettingItem("启用天气问候", "根据ClassIsland天气显示问候语",
            SettingsUI.Toggle(_svc.Settings.WeatherGreetingEnabled, v => _svc.Settings.WeatherGreetingEnabled = v)));
        s.Children.Add(SettingsUI.Expander("开关", "天气问候组件总开关", togglePanel));

        var layoutPanel = new StackPanel { Spacing = 0 };
        var presets = new[] { "仅问候", "图标+问候", "温度+问候", "完整信息" };
        var presetCombo = new ComboBox { Width = 120, HorizontalAlignment = HorizontalAlignment.Right };
        foreach (var p in presets) presetCombo.Items.Add(p);

        var currentTemplate = _svc.Settings.WeatherTemplate ?? "{greeting}";
        presetCombo.SelectedIndex = currentTemplate switch
        {
            "{greeting}" => 0,
            "{icon} {greeting}" => 1,
            "{temp} {greeting}" => 2,
            "{icon} {temp} {greeting} {warning}" => 3,
            _ => -1
        };
        presetCombo.SelectionChanged += (a, b) =>
        {
            _svc.Settings.WeatherTemplate = presetCombo.SelectedIndex switch
            {
                0 => "{greeting}",
                1 => "{icon} {greeting}",
                2 => "{temp} {greeting}",
                3 => "{icon} {temp} {greeting} {warning}",
                _ => _svc.Settings.WeatherTemplate
            };
        };

        layoutPanel.Children.Add(SettingsUI.SettingItem("预设模板", "快速选择排版样式", presetCombo));
        layoutPanel.Children.Add(SettingsUI.Separator());
        layoutPanel.Children.Add(SettingsUI.SettingItem("自定义模板", null,
            SettingsUI.Text(_svc.Settings.WeatherTemplate ?? "{greeting}", 280, v => _svc.Settings.WeatherTemplate = v)));
        layoutPanel.Children.Add(SettingsUI.Separator());
        layoutPanel.Children.Add(SettingsUI.SettingItem("显示天气图标", "在模板中使用 {icon}",
            SettingsUI.Toggle(_svc.Settings.WeatherShowIcon, v => _svc.Settings.WeatherShowIcon = v)));
        layoutPanel.Children.Add(SettingsUI.Separator());
        layoutPanel.Children.Add(SettingsUI.SettingItem("显示温度", "在模板中使用 {temp}",
            SettingsUI.Toggle(_svc.Settings.WeatherShowTemp, v => _svc.Settings.WeatherShowTemp = v)));
        layoutPanel.Children.Add(SettingsUI.Separator());
        layoutPanel.Children.Add(SettingsUI.Info("可用变量: {greeting} 问候语 | {temp} 温度 | {weather} 天气 | {warning} 预警 | {icon} 天气图标"));
        s.Children.Add(SettingsUI.Expander("排版", "自定义天气问候的显示格式", layoutPanel));

        s.Children.Add(SettingsUI.Expander("温度提醒", "自定义各温度区间的穿衣提醒文案", BuildTempPanel()));
        s.Children.Add(SettingsUI.Expander("天气关键词", "根据天气关键词匹配显示文案", BuildWeatherGreetingPanel()));

        s.Children.Add(SettingsUI.Info("天气数据来自ClassIsland内置天气服务，插件会自动读取当前天气并匹配对应的问候语。"));
        s.Children.Add(SettingsUI.SaveButton(() => _svc.SaveSettings()));
        return s;
    }

    StackPanel BuildTempPanel()
    {
        var panel = new StackPanel { Spacing = 8 };
        var listPanel = new StackPanel { Spacing = 0 };

        void RefreshList()
        {
            listPanel.Children.Clear();
            var items = _svc.Settings.TempGreetings.OrderBy(g => g.MinTemp).ToList();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(16, 8, 16, 8) };

                var minBox = new TextBox { Text = item.MinTemp.ToString(), Width = 45 };
                minBox.LostFocus += (a, b) =>
                {
                    if (int.TryParse(minBox.Text, out var v)) item.MinTemp = v;
                    else minBox.Text = item.MinTemp.ToString();
                };

                var maxBox = new TextBox { Text = item.MaxTemp == 999 ? "" : item.MaxTemp.ToString(), Width = 45, Watermark = "∞" };
                maxBox.LostFocus += (a, b) =>
                {
                    if (string.IsNullOrEmpty(maxBox.Text)) item.MaxTemp = 999;
                    else if (int.TryParse(maxBox.Text, out var v)) item.MaxTemp = v;
                    else maxBox.Text = item.MaxTemp == 999 ? "" : item.MaxTemp.ToString();
                };

                var textBox = SettingsUI.Text(item.Text, 200, v => item.Text = v);
                var delBtn = new Button { Content = "🗑️", Padding = new Thickness(4, 2) };
                delBtn.Click += (a, e) => { _svc.Settings.TempGreetings.Remove(item); RefreshList(); };

                row.Children.Add(new TextBlock { Text = "≥", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                row.Children.Add(minBox);
                row.Children.Add(new TextBlock { Text = "°C", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                row.Children.Add(new TextBlock { Text = "~", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                row.Children.Add(maxBox);
                row.Children.Add(new TextBlock { Text = "°C", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                row.Children.Add(textBox);
                row.Children.Add(delBtn);
                listPanel.Children.Add(row);
                if (i < items.Count - 1)
                    listPanel.Children.Add(SettingsUI.Separator());
            }
        }

        RefreshList();
        panel.Children.Add(listPanel);

        var addBtn = new Button { Content = "+ 添加温度区间", Padding = new Thickness(12, 4), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(16, 4, 16, 8) };
        addBtn.Click += (a, e) =>
        {
            _svc.Settings.TempGreetings.Add(new Models.TempGreeting { MinTemp = 0, MaxTemp = 999, Text = "" });
            RefreshList();
        };
        panel.Children.Add(addBtn);

        var resetBtn = new Button { Content = "恢复默认", Padding = new Thickness(12, 4), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(16, 0, 16, 8) };
        resetBtn.Click += (a, e) =>
        {
            _svc.Settings.TempGreetings.Clear();
            foreach (var g in Models.LocalGreetingDB.DefaultTempGreetings)
                _svc.Settings.TempGreetings.Add(new Models.TempGreeting { MinTemp = g.MinTemp, MaxTemp = g.MaxTemp, Text = g.Text });
            RefreshList();
        };
        panel.Children.Add(resetBtn);

        return panel;
    }

    StackPanel BuildWeatherGreetingPanel()
    {
        var panel = new StackPanel { Spacing = 8 };
        var listPanel = new StackPanel { Spacing = 0 };

        void RefreshList()
        {
            listPanel.Children.Clear();
            var items = _svc.Settings.WeatherGreetings.ToList();
            for (int i = 0; i < items.Count; i++)
            {
                var kv = items[i];
                var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(16, 8, 16, 8) };
                var keyBox = new TextBox { Text = kv.Key, Width = 80, IsReadOnly = kv.Key == "默认" };
                keyBox.TextChanged += (a, b) =>
                {
                    if (kv.Key == "默认") return;
                    var newKey = keyBox.Text ?? "";
                    if (newKey != kv.Key && !string.IsNullOrEmpty(newKey) && !_svc.Settings.WeatherGreetings.ContainsKey(newKey))
                    {
                        _svc.Settings.WeatherGreetings.Remove(kv.Key);
                        _svc.Settings.WeatherGreetings[newKey] = kv.Value;
                    }
                };
                var textBox = SettingsUI.Text(kv.Value, 220, v => _svc.Settings.WeatherGreetings[kv.Key] = v);
                var delBtn = new Button { Content = "🗑️", Padding = new Thickness(4, 2), IsVisible = kv.Key != "默认" };
                delBtn.Click += (a, e) => { _svc.Settings.WeatherGreetings.Remove(kv.Key); RefreshList(); };

                row.Children.Add(new TextBlock { Text = "关键词", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                row.Children.Add(keyBox);
                row.Children.Add(new TextBlock { Text = "文案", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                row.Children.Add(textBox);
                row.Children.Add(delBtn);
                listPanel.Children.Add(row);
                if (i < items.Count - 1)
                    listPanel.Children.Add(SettingsUI.Separator());
            }
        }

        RefreshList();
        panel.Children.Add(listPanel);

        var addBtn = new Button { Content = "+ 添加天气问候", Padding = new Thickness(12, 4), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(16, 4, 16, 8) };
        addBtn.Click += (a, e) =>
        {
            _svc.Settings.WeatherGreetings["新天气"] = "";
            RefreshList();
        };
        panel.Children.Add(addBtn);

        panel.Children.Add(SettingsUI.Info("说明：当天气文本包含对应关键词时，显示该文案。{weather} 会被替换为实际天气名称。"));

        return panel;
    }

    Control BuildAboutPanel()
    {
        var s = new StackPanel { Spacing = 0 };
        s.Children.Add(SettingsUI.PageHeader("ℹ️ 关于"));

        var infoPanel = new StackPanel { Spacing = 8, Margin = new Thickness(16, 12, 16, 12) };
        infoPanel.Children.Add(new TextBlock
        {
            Text = "节假日倒计时",
            FontSize = 28,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(SettingsUI.AccentColor)
        });
        infoPanel.Children.Add(new TextBlock { Text = "版本: v1.2.0 (正式版)", FontSize = 14, Opacity = 0.7 });
        infoPanel.Children.Add(new TextBlock { Text = "作者: fengjian868", FontSize = 14, Opacity = 0.7 });
        infoPanel.Children.Add(new TextBlock { Text = "GitHub: https://github.com/fengjian868/HolidayCountdown", FontSize = 12, Opacity = 0.5 });
        s.Children.Add(SettingsUI.Card(infoPanel));

        var featurePanel = new StackPanel { Spacing = 6, Margin = new Thickness(16, 12, 16, 12) };
        featurePanel.Children.Add(new TextBlock { Text = "- 节假日倒计时（调休提醒、进度环、放假天数）", FontSize = 12, Opacity = 0.8 });
        featurePanel.Children.Add(new TextBlock { Text = "- 24节气倒计时（网络自动刷新）", FontSize = 12, Opacity = 0.8 });
        featurePanel.Children.Add(new TextBlock { Text = "- 农历日期显示（自定义模板）", FontSize = 12, Opacity = 0.8 });
        featurePanel.Children.Add(new TextBlock { Text = "- 自定义节日倒计时", FontSize = 12, Opacity = 0.8 });
        featurePanel.Children.Add(new TextBlock { Text = "- 寒暑假倒计时（周+天）", FontSize = 12, Opacity = 0.8 });
        featurePanel.Children.Add(new TextBlock { Text = "- 时段问候语（早中晚+放学+晚修）", FontSize = 12, Opacity = 0.8 });
        featurePanel.Children.Add(new TextBlock { Text = "- 天气问候（根据温度提醒穿衣）", FontSize = 12, Opacity = 0.8 });
        featurePanel.Children.Add(new TextBlock { Text = "- 课程表联动（当前课程/课间倒计时）", FontSize = 12, Opacity = 0.8 });
        featurePanel.Children.Add(new TextBlock { Text = "- 学习时长统计（今日学习时长）", FontSize = 12, Opacity = 0.8 });
        s.Children.Add(SettingsUI.Expander("功能模块", "插件支持的所有功能", featurePanel, expanded: true));

        s.Children.Add(new TextBlock { Text = "Made with love for ClassIsland", FontSize = 12, Opacity = 0.5, Margin = new Thickness(0, 8, 0, 0) });
        return s;
    }
}


