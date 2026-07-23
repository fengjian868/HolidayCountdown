using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Models;
using HolidayCountdown.Services;
using HolidayCountdown.WeatherReminders;

namespace HolidayCountdown.Views.SettingsPages;

[SettingsPageInfo("holidaycountdown.settings", "节假日倒计时设置", "\uE3A2", "\uE3A2")]
public class UnifiedSettingsPage : SettingsPageBase
{
    private readonly HolidayService _svc;
    private readonly List<Border> _tabButtons = new();
    private readonly DispatcherTimer _saveTimer;
    private StackPanel _contentPanel = null!;
    private ScrollViewer _scrollViewer = null!;
    private Grid _tabBarGrid = null!;
    private StackPanel _tabRow0 = null!;
    private StackPanel _tabRow1 = null!;

    private readonly (string Icon, string Label, Func<Control> Build)[] _tabs;
    private int _currentIndex = -1;

    // 性能优化：缓存已构建的 tab 面板，切换 tab 时复用而非每次重建
    private readonly Dictionary<int, Control> _builtPanels = new();
    // RebalanceTabRows 防抖，避免 SizeChanged 频繁触发布局重算导致卡顿
    private readonly DispatcherTimer _rebalanceTimer;

    /// <summary>
    /// 将文本前景色绑定到主题资源，自动适配明暗主题
    /// </summary>
    static void BindThemeForeground(TextBlock textBlock)
    {
        textBlock[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
    }

    public UnifiedSettingsPage()
    {
        _svc = new HolidayService();
        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _saveTimer.Tick += (s, e) => { _saveTimer.Stop(); _svc.SaveSettings(); };
        _rebalanceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _rebalanceTimer.Tick += (s, e) => { _rebalanceTimer.Stop(); RebalanceTabRowsCore(); };

        var expEnabled = _svc.Settings.ExperimentalFeaturesEnabled;
        var tabList = new List<(string, string, Func<Control>)>
        {
            ("\uE946", "关于", BuildAboutPanel),
            ("\uE8F5", "节假日", BuildHolidayPanel),
            ("\uE8BD", "问候语", BuildGreetingPanel),
            ("\uE9CA", "24节气", BuildSolarTermPanel),
            ("\uE8C0", "农历", BuildLunarPanel),
            ("\uE70F", "自定义", BuildCustomHolidayPanel),
            ("\uE8F3", "寒暑假", BuildVacationPanel),
            ("\uE753", "天气", BuildWeatherPanel),
            ("\uE9D1", "学习", BuildStudyTimePanel),
        };

        // 实验性功能 Tab
        if (expEnabled)
        {
            tabList.Add(("\uE7BE", "课表", BuildClassSchedulePanel));
            // 天气提醒已融合进「天气」Tab，不再单独显示
            tabList.Add(("\uE921", "大考", BuildExamCountdownPanel));
            tabList.Add(("\uE823", "时钟", BuildWorldClockPanel));
        }

        _tabs = tabList.ToArray();
        Content = Build();
    }

    Control Build()
    {
        var root = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto Auto *")
        };

        _tabBarGrid = new Grid
        {
            Margin = new Thickness(16, 12, 16, 8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };
        _tabBarGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        _tabBarGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        // 顶部导航栏左侧显示设置页图标
        var navIconBlock = new TextBlock
        {
            Text = "\uE3A2",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 22,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0)
        };
        BindThemeForeground(navIconBlock);

        _tabRow0 = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center
        };
        _tabRow0.Children.Add(navIconBlock);

        _tabRow1 = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 0)
        };

        for (int i = 0; i < _tabs.Length; i++)
        {
            var idx = i;
            var tab = _tabs[i];
            var tabIconBlock = new TextBlock { Text = tab.Icon, FontSize = 14, VerticalAlignment = VerticalAlignment.Center };
            BindThemeForeground(tabIconBlock);
            var tabLabelBlock = new TextBlock { Text = tab.Label, FontSize = 13, VerticalAlignment = VerticalAlignment.Center };
            BindThemeForeground(tabLabelBlock);
            var content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                Children =
                {
                    tabIconBlock,
                    tabLabelBlock
                }
            };
            var btn = new Border
            {
                Child = content,
                Padding = new Thickness(10, 6),
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(6),
                Cursor = new Cursor(StandardCursorType.Hand),
                Tag = idx
            };
            btn.PointerPressed += (s, e) => SwitchTab(idx);
            _tabButtons.Add(btn);
            _tabRow0.Children.Add(btn);
        }

        Grid.SetRow(_tabRow0, 0);
        Grid.SetRow(_tabRow1, 1);
        _tabBarGrid.Children.Add(_tabRow0);
        _tabBarGrid.Children.Add(_tabRow1);
        Grid.SetRow(_tabBarGrid, 0);
        root.Children.Add(_tabBarGrid);

        var sep = new Border
        {
            Height = 1,
            Background = new SolidColorBrush(Color.Parse("#20FFFFFF")),
            Margin = new Thickness(16, 0, 16, 8)
        };
        Grid.SetRow(sep, 1);
        root.Children.Add(sep);

        _contentPanel = new StackPanel { Spacing = 0, Margin = new Thickness(20, 8, 20, 16) };
        _scrollViewer = new ScrollViewer
        {
            Content = _contentPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        Grid.SetRow(_scrollViewer, 2);
        root.Children.Add(_scrollViewer);

        _tabBarGrid.SizeChanged += (s, e) => RebalanceTabRows();
        SwitchTab(0);
        return root;
    }

    // 防抖入口：SizeChanged 频繁触发时合并为一次重算，避免卡顿
    void RebalanceTabRows()
    {
        _rebalanceTimer.Stop();
        _rebalanceTimer.Start();
    }

    void RebalanceTabRowsCore()
    {
        if (_tabBarGrid == null || _tabRow0 == null || _tabRow1 == null) return;

        var availableWidth = _tabBarGrid.Bounds.Width;
        if (availableWidth <= 0 || _tabRow0.Children.Count == 0) return;

        // 先把所有按钮收回第一行（保留第一个导航图标不移动）
        while (_tabRow1.Children.Count > 0)
        {
            var btn = _tabRow1.Children[0];
            _tabRow1.Children.RemoveAt(0);
            _tabRow0.Children.Add(btn);
        }

        // 计算第一行期望总宽度
        double totalWidth = 0;
        foreach (var child in _tabRow0.Children)
        {
            if (child is Control c) totalWidth += c.Bounds.Width;
        }

        // 若溢出，从末尾依次移到第二行，直到不溢出
        // 注意：第一个导航图标保留在第一行
        while (totalWidth > availableWidth && _tabRow0.Children.Count > 2)
        {
            var last = _tabRow0.Children[_tabRow0.Children.Count - 1];
            _tabRow0.Children.RemoveAt(_tabRow0.Children.Count - 1);
            _tabRow1.Children.Insert(0, last);

            totalWidth = 0;
            foreach (var child in _tabRow0.Children)
            {
                if (child is Control c) totalWidth += c.Bounds.Width;
            }
        }
    }

    void SwitchTab(int index)
    {
        if (index < 0 || index >= _tabs.Length) index = 0;
        _currentIndex = index;
        foreach (var btn in _tabButtons)
        {
            bool active = btn.Tag is int idx && idx == index;
            btn.Background = active
                ? new SolidColorBrush(Color.Parse("#FF2196F3"))
                : Brushes.Transparent;
            btn.Opacity = active ? 1.0 : 0.7;
        }

        _contentPanel.Children.Clear();
        if (index >= 0 && index < _tabs.Length)
        {
            // 性能优化：优先复用已缓存的面板，避免每次切换都重建大量控件
            if (!_builtPanels.TryGetValue(index, out var control))
            {
                control = _tabs[index].Build();
                _builtPanels[index] = control;
            }
            _contentPanel.Children.Add(control);
        }
    }

    // 清除当前 tab 的缓存并立即重建（用于列表增删后刷新当前面板）
    void RefreshCurrentTab()
    {
        if (_currentIndex >= 0) _builtPanels.Remove(_currentIndex);
        SwitchTab(_currentIndex);
    }

    // ===== Tab Builders =====

    Control BuildClassSchedulePanel()
    {
        var s = new StackPanel { Spacing = 0 };
        s.Children.Add(PageHeader("\uE7BE 课程表联动设置"));

        var schedulePanel = new StackPanel { Spacing = 0 };
        schedulePanel.Children.Add(SettingItem("启用课程表联动", "关闭后组件不显示任何内容",
            Toggle(_svc.Settings.ClassScheduleEnabled, v => { _svc.Settings.ClassScheduleEnabled = v; AutoSave(); })));
        schedulePanel.Children.Add(Separator());
        schedulePanel.Children.Add(SettingItem("显示图标", "在课程表信息前显示学科图标",
            Toggle(_svc.Settings.ClassScheduleShowIcon, v => { _svc.Settings.ClassScheduleShowIcon = v; AutoSave(); })));
        schedulePanel.Children.Add(Separator());
        schedulePanel.Children.Add(SettingItem("显示学科名", "是否显示课程名称",
            Toggle(_svc.Settings.ClassScheduleShowSubject, v => { _svc.Settings.ClassScheduleShowSubject = v; AutoSave(); })));
        s.Children.Add(Expander("基础设置", "课程表联动组件基础设置", schedulePanel));

        var noClassPanel = new StackPanel { Spacing = 0 };
        var noClassListPanel = new StackPanel { Spacing = 0 };

        void RefreshNoClassList()
        {
            noClassListPanel.Children.Clear();
            var slots = _svc.Settings.NoClassTimeSlots.OrderBy(x => x.StartHour * 60 + x.StartMinute).ToList();
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(16, 8, 16, 8) };
                var nameBox = Text(slot.Name, 70, v => { slot.Name = v; AutoSave(); });
                var startBox = Text($"{slot.StartHour:D2}:{slot.StartMinute:D2}", 50, v =>
                {
                    if (TimeSpan.TryParse(v, out var ts)) { slot.StartHour = ts.Hours; slot.StartMinute = ts.Minutes; AutoSave(); }
                });
                var endBox = Text($"{slot.EndHour:D2}:{slot.EndMinute:D2}", 50, v =>
                {
                    if (TimeSpan.TryParse(v, out var ts)) { slot.EndHour = ts.Hours; slot.EndMinute = ts.Minutes; AutoSave(); }
                });
                var textBox = Text(slot.Text, 180, v => { slot.Text = v; AutoSave(); });
                var delBtn = new Button { Content = "删除", Padding = new Thickness(6, 2), Foreground = new SolidColorBrush(Color.Parse("#FFE53935")) };
                delBtn.Click += (a, e) => { _svc.Settings.NoClassTimeSlots.Remove(slot); AutoSave(); RefreshNoClassList(); };

                var nameLabelBlock = new TextBlock { Text = "名称", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 };
                BindThemeForeground(nameLabelBlock);
                row.Children.Add(nameLabelBlock);
                row.Children.Add(nameBox);
                var fromBlock = new TextBlock { Text = "从", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 };
                BindThemeForeground(fromBlock);
                row.Children.Add(fromBlock);
                row.Children.Add(startBox);
                var toBlock = new TextBlock { Text = "到", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 };
                BindThemeForeground(toBlock);
                row.Children.Add(toBlock);
                row.Children.Add(endBox);
                var textLabelBlock = new TextBlock { Text = "文案", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 };
                BindThemeForeground(textLabelBlock);
                row.Children.Add(textLabelBlock);
                row.Children.Add(textBox);
                row.Children.Add(delBtn);
                noClassListPanel.Children.Add(row);
                if (i < slots.Count - 1)
                    noClassListPanel.Children.Add(Separator());
            }
        }

        RefreshNoClassList();
        noClassPanel.Children.Add(noClassListPanel);

        var addNoClassBtn = new Button { Content = "+ 添加时段", Padding = new Thickness(12, 4), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(16, 4, 16, 8) };
        addNoClassBtn.Click += (a, e) =>
        {
            _svc.Settings.NoClassTimeSlots.Add(new Models.NoClassTimeSlot { Name = "新时段", StartHour = 0, StartMinute = 0, EndHour = 23, EndMinute = 59, Text = "" });
            AutoSave();
            RefreshNoClassList();
        };
        noClassPanel.Children.Add(addNoClassBtn);
        noClassPanel.Children.Add(Info("按当前时间匹配第一个满足条件的时段，可自由添加、删除、修改时段和文案"));
        s.Children.Add(Expander("无课程文案", "无课程时按时段显示的内容", noClassPanel));

        var preClassPanel = new StackPanel { Spacing = 0 };
        preClassPanel.Children.Add(SettingItem("课前提示分钟", "上课前多少分钟开始显示准备上课（当前由课间警示分钟数控制）",
            Number(_svc.Settings.PreClassMinutes, 0, 60, v => { _svc.Settings.PreClassMinutes = v; AutoSave(); })));
        s.Children.Add(Expander("课前提示", "上课前提前显示下节课信息", preClassPanel));

        var warningPanel = new StackPanel { Spacing = 0 };
        warningPanel.Children.Add(SettingItem("启用课间警示", "课间剩余时间较少时变红提醒",
            Toggle(_svc.Settings.BreakWarningEnabled, v => { _svc.Settings.BreakWarningEnabled = v; AutoSave(); })));
        warningPanel.Children.Add(Separator());
        warningPanel.Children.Add(SettingItem("警示分钟", "剩余多少分钟时触发警示",
            Number(_svc.Settings.BreakWarningMinutes, 0, 30, v => { _svc.Settings.BreakWarningMinutes = v; AutoSave(); })));
        warningPanel.Children.Add(Separator());
        warningPanel.Children.Add(SettingItem("警示颜色", null,
            ColorPicker(_svc.Settings.BreakWarningColor, c => { _svc.Settings.BreakWarningColor = c; AutoSave(); })));
        s.Children.Add(Expander("课间警示", "课间剩余时间较少时高亮显示", warningPanel));

        var templatePanel = new StackPanel { Spacing = 0 };
        templatePanel.Children.Add(SettingItem("上课模板", "{A}=当前学科图标 {B}=当前学科名 {C}=距下课剩余时间 {D}=下节课图标 {E}=下节课名 (建议加 {C} 显示倒计时)",
            Text(_svc.Settings.ClassScheduleOnClassTemplate, 320, v => { _svc.Settings.ClassScheduleOnClassTemplate = v; AutoSave(); })));
        templatePanel.Children.Add(Separator());
        templatePanel.Children.Add(SettingItem("课间模板", "{F}=课间图标 {G}=课间剩余时间 {D}=下节课图标 {E}=下节课名",
            Text(_svc.Settings.ClassScheduleBreakTemplate, 320, v => { _svc.Settings.ClassScheduleBreakTemplate = v; AutoSave(); })));
        templatePanel.Children.Add(Separator());
        templatePanel.Children.Add(SettingItem("准备上课模板", "{H}=准备上课图标 {D}=下节课图标 {E}=下节课名 {I}=距上课剩余时间",
            Text(_svc.Settings.ClassSchedulePrepareTemplate, 320, v => { _svc.Settings.ClassSchedulePrepareTemplate = v; AutoSave(); })));
        templatePanel.Children.Add(Separator());
        templatePanel.Children.Add(SettingItem("放学模板", "{J}=放学图标",
            Text(_svc.Settings.ClassScheduleAfterSchoolTemplate, 320, v => { _svc.Settings.ClassScheduleAfterSchoolTemplate = v; AutoSave(); })));
        templatePanel.Children.Add(Separator());
        templatePanel.Children.Add(SettingItem("无课程模板", "{K}=无课程图标 {M}=无课程文案",
            Text(_svc.Settings.ClassScheduleNoClassTemplate, 320, v => { _svc.Settings.ClassScheduleNoClassTemplate = v; AutoSave(); })));
        s.Children.Add(Expander("显示模板", "自定义各类状态的显示格式", templatePanel));

        return s;
    }

    Control BuildStudyTimePanel()
    {
        var s = new StackPanel { Spacing = 0 };
        s.Children.Add(PageHeader("\uE9D1 学习时长统计设置"));

        var studyPanel = new StackPanel { Spacing = 0 };
        var modeCombo = new ComboBox { Width = 160, HorizontalAlignment = HorizontalAlignment.Right };
        var modes = new[] { "关闭", "统计总运行时长", "仅统计上课时间" };
        foreach (var m in modes) modeCombo.Items.Add(m);
        modeCombo.SelectedIndex = !_svc.Settings.StudyTimeEnabled ? 0 : (_svc.Settings.StudyTimeCountClassTimeOnly ? 2 : 1);
        modeCombo.SelectionChanged += (a, b) =>
        {
            switch (modeCombo.SelectedIndex)
            {
                case 0: _svc.Settings.StudyTimeEnabled = false; break;
                case 1: _svc.Settings.StudyTimeEnabled = true; _svc.Settings.StudyTimeCountClassTimeOnly = false; break;
                case 2: _svc.Settings.StudyTimeEnabled = true; _svc.Settings.StudyTimeCountClassTimeOnly = true; break;
            }
            AutoSave();
        };
        studyPanel.Children.Add(SettingItem("统计模式", "选择学习时长的统计方式", modeCombo));
        studyPanel.Children.Add(Separator());
        studyPanel.Children.Add(SettingItem("显示图标", "在组件中显示图标",
            Toggle(_svc.Settings.StudyTimeShowIcon, v => { _svc.Settings.StudyTimeShowIcon = v; AutoSave(); })));
        studyPanel.Children.Add(Separator());
        studyPanel.Children.Add(SettingItem("每周重置", "按 ISO 周次统计本周学习时长",
            Toggle(_svc.Settings.StudyTimeWeeklyReset, v => { _svc.Settings.StudyTimeWeeklyReset = v; AutoSave(); })));
        studyPanel.Children.Add(Separator());
        var resetStudyBtn = new Button { Content = "🔄 重置当前统计", Padding = new Thickness(12, 4), HorizontalAlignment = HorizontalAlignment.Left };
        resetStudyBtn.Click += (a, e) =>
        {
            var dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClassIsland", "Plugins", "HolidayCountdown");
            var studyTimePath = Path.Combine(dataDir, "study_time.json");
            try
            {
                if (File.Exists(studyTimePath))
                {
                    var json = File.ReadAllText(studyTimePath);
                    var data = JsonSerializer.Deserialize<Dictionary<string, double>>(json) ?? new Dictionary<string, double>();
                    var key = DateTime.Now.ToString("yyyy-MM-dd");
                    data[key] = 0;
                    File.WriteAllText(studyTimePath, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
                }
            }
            catch { }
        };
        studyPanel.Children.Add(SettingItem("重置今日时长", "将今日学习时长清零", resetStudyBtn));
        s.Children.Add(Expander("基础设置", "学习时长统计组件基础设置", studyPanel));

        return s;
    }

    Control BuildExamCountdownPanel()
    {
        var s = new StackPanel { Spacing = 0 };
        s.Children.Add(PageHeader("🎓 大考倒计时设置"));

        var basicPanel = new StackPanel { Spacing = 0 };
        var typeCombo = new ComboBox { Width = 120, HorizontalAlignment = HorizontalAlignment.Right };
        typeCombo.Items.Add("高考");
        typeCombo.Items.Add("中考");
        typeCombo.SelectedIndex = _svc.Settings.ExamType == 1 ? 1 : 0;

        // 当前考试日期显示（实时同步）
        var examDateText = new TextBlock
        {
            Text = GetCurrentExamDateDisplay(),
            FontSize = 12,
            Opacity = 0.7
        };
        BindThemeForeground(examDateText);

        void RefreshExamDateDisplay()
        {
            examDateText.Text = GetCurrentExamDateDisplay();
        }

        typeCombo.SelectionChanged += (a, b) =>
        {
            _svc.Settings.ExamType = typeCombo.SelectedIndex;
            AutoSave();
            RefreshExamDateDisplay();
        };

        basicPanel.Children.Add(SettingItem("考试类型", "选择中考或高考", typeCombo));
        basicPanel.Children.Add(Separator());

        var cityPanel = SearchableCityPicker(
            ExamDateData.SupportedCities.ToList(),
            _svc.Settings.ExamCity,
            "搜索城市...",
            160,
            selectedCity =>
            {
                _svc.Settings.ExamCity = selectedCity;
                AutoSave();
                RefreshExamDateDisplay();
            });
        basicPanel.Children.Add(SettingItem("城市", "搜索并选择城市（如北京、上海、广州）", cityPanel));
        basicPanel.Children.Add(Separator());

        // 当前考试日期
        basicPanel.Children.Add(SettingItem("当前考试日期", "根据城市和考试类型自动计算", examDateText));
        basicPanel.Children.Add(Separator());

        // 自定义日期：改为日期选择器
        var customDatePicker = new DatePicker
        {
            Width = 150,
            HorizontalAlignment = HorizontalAlignment.Right,
            SelectedDate = !string.IsNullOrWhiteSpace(_svc.Settings.ExamCountdownCustomDate)
                ? ParseCustomDate(_svc.Settings.ExamCountdownCustomDate)
                : null
        };
        customDatePicker.SelectedDateChanged += (a, b) =>
        {
            if (customDatePicker.SelectedDate.HasValue)
            {
                var d = customDatePicker.SelectedDate.Value;
                _svc.Settings.ExamCountdownCustomDate = $"{d.Month}-{d.Day}";
            }
            else
            {
                _svc.Settings.ExamCountdownCustomDate = null;
            }
            AutoSave();
            RefreshExamDateDisplay();
        };
        basicPanel.Children.Add(SettingItem("自定义日期", "选择日期覆盖内置数据，清空则恢复默认", customDatePicker));
        basicPanel.Children.Add(Separator());
        basicPanel.Children.Add(SettingItem("每年重复", "考试过后自动显示下一年倒计时",
            Toggle(_svc.Settings.ExamCountdownRepeatYearly, v => { _svc.Settings.ExamCountdownRepeatYearly = v; AutoSave(); })));
        s.Children.Add(Expander("考试", "考试类型与城市", basicPanel));

        var stylePanel = new StackPanel { Spacing = 0 };
        stylePanel.Children.Add(SettingItem("显示圆环", "在文案左侧显示进度圆环",
            Toggle(_svc.Settings.ExamCountdownShowRing, v => { _svc.Settings.ExamCountdownShowRing = v; AutoSave(); })));
        stylePanel.Children.Add(Separator());
        stylePanel.Children.Add(SettingItem("圆环颜色", null,
            ColorPicker(_svc.Settings.ExamCountdownRingColor, c => { _svc.Settings.ExamCountdownRingColor = c; AutoSave(); })));
        stylePanel.Children.Add(Separator());
        stylePanel.Children.Add(SettingItem("圆环开始日期", "圆环进度从这一天开始计算，每年自动循环",
            Date(_svc.Settings.ExamCountdownRingStartDate, d => { _svc.Settings.ExamCountdownRingStartDate = d; AutoSave(); })));
        stylePanel.Children.Add(Separator());
        stylePanel.Children.Add(SettingItem("文字颜色", null,
            ColorPicker(_svc.Settings.ExamCountdownTextColor, c => { _svc.Settings.ExamCountdownTextColor = c; AutoSave(); })));
        stylePanel.Children.Add(Separator());
        stylePanel.Children.Add(SettingItem("字体大小", "0 表示跟随 ClassIsland 主体字体大小",
            Number(_svc.Settings.ExamCountdownFontSize, 0, 72, v => { _svc.Settings.ExamCountdownFontSize = v; AutoSave(); })));
        s.Children.Add(Expander("样式", "圆环、颜色与字体", stylePanel));

        var textPanel = new StackPanel { Spacing = 0 };
        textPanel.Children.Add(SettingItem("倒计时文案", "变量：{A}=考试名 {B}=剩余天数 {C}=考试日期（兼容：{exam} {days} {date}）",
            Text(_svc.Settings.ExamCountdownCustomText, 260, v => { _svc.Settings.ExamCountdownCustomText = v; AutoSave(); })));
        textPanel.Children.Add(Separator());
        textPanel.Children.Add(SettingItem("当天文案", "变量：{A}=考试名 {C}=考试日期（兼容：{exam} {date}）",
            Text(_svc.Settings.ExamCountdownTodayText, 260, v => { _svc.Settings.ExamCountdownTodayText = v; AutoSave(); })));
        s.Children.Add(Expander("文案", "自定义显示文字", textPanel));

        return s;
    }

    string GetCurrentExamDateDisplay()
    {
        var year = DateTime.Now.Year;
        var examType = _svc.Settings.ExamType;
        var city = _svc.Settings.ExamCity;
        var examName = examType == 1 ? "中考" : "高考";

        DateTime examDate;
        var custom = _svc.Settings.ExamCountdownCustomDate;
        if (!string.IsNullOrWhiteSpace(custom) &&
            DateTime.TryParseExact(year + "-" + custom, "yyyy-M-d", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var cd))
        {
            examDate = cd;
        }
        else
        {
            examDate = ExamDateData.GetExamDate(year, examType, city);
        }

        if (examDate.Date < DateTime.Now.Date && _svc.Settings.ExamCountdownRepeatYearly)
        {
            if (!string.IsNullOrWhiteSpace(custom) &&
                DateTime.TryParseExact((year + 1) + "-" + custom, "yyyy-M-d", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var cd2))
            {
                examDate = cd2;
            }
            else
            {
                examDate = ExamDateData.GetExamDate(year + 1, examType, city);
            }
        }

        var days = (examDate.Date - DateTime.Now.Date).Days;
        var daysText = days <= 0 ? "今天" : $"还有{days}天";
        return $"{examName} {examDate:yyyy年M月d日}（{daysText}）";
    }

    static DateTimeOffset? ParseCustomDate(string? customDate)
    {
        if (string.IsNullOrWhiteSpace(customDate)) return null;
        var year = DateTime.Now.Year;
        if (DateTime.TryParseExact(year + "-" + customDate, "yyyy-M-d", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d))
            return new DateTimeOffset(d);
        return null;
    }

    Control BuildWorldClockPanel()
    {
        var s = new StackPanel { Spacing = 0 };
        s.Children.Add(PageHeader("🌍 世界时钟设置"));

        var displayPanel = new StackPanel { Spacing = 0 };
        displayPanel.Children.Add(SettingItem("显示秒", "时间显示到秒",
            Toggle(_svc.Settings.WorldClockShowSeconds, v => { _svc.Settings.WorldClockShowSeconds = v; AutoSave(); })));
        displayPanel.Children.Add(Separator());
        displayPanel.Children.Add(SettingItem("显示日期", "在每个城市下方显示日期",
            Toggle(_svc.Settings.WorldClockShowDate, v => { _svc.Settings.WorldClockShowDate = v; AutoSave(); })));
        displayPanel.Children.Add(Separator());
        displayPanel.Children.Add(SettingItem("文字颜色", null,
            ColorPicker(_svc.Settings.WorldClockTextColor, c => { _svc.Settings.WorldClockTextColor = c; AutoSave(); })));
        s.Children.Add(Expander("显示", "世界时钟显示选项", displayPanel));

        var listPanel = new StackPanel { Spacing = 0 };
        var citiesPanel = new StackPanel { Spacing = 0 };

        void RefreshCities()
        {
            citiesPanel.Children.Clear();
            var cities = _svc.Settings.WorldClockCities.ToList();
            for (int i = 0; i < cities.Count; i++)
            {
                var city = cities[i];
                var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(16, 8, 16, 8) };

                var citySelectPanel = SearchableCityPicker(
                    WorldClockCityData.SupportedCities.ToList(),
                    city.Name,
                    "搜索...",
                    90,
                    selectedCity =>
                    {
                        city.Name = selectedCity;
                        city.TimeZoneId = WorldClockCityData.GetTimeZoneId(selectedCity);
                        AutoSave();
                    });

                // 时区显示（只读，根据城市自动匹配）
                var tzText = new TextBlock
                {
                    Text = city.TimeZoneId,
                    Width = 180,
                    VerticalAlignment = VerticalAlignment.Center,
                    Opacity = 0.7,
                    FontSize = 11
                };
                BindThemeForeground(tzText);

                var delBtn = new Button { Content = "删除", Padding = new Thickness(6, 2), Foreground = new SolidColorBrush(Color.Parse("#FFE53935")) };
                delBtn.Click += (a, e) => { _svc.Settings.WorldClockCities.Remove(city); AutoSave(); RefreshCities(); };

                var nameLabel = new TextBlock { Text = "城市", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 };
                BindThemeForeground(nameLabel);
                var tzLabel = new TextBlock { Text = "时区（自动）", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 };
                BindThemeForeground(tzLabel);
                row.Children.Add(nameLabel);
                row.Children.Add(citySelectPanel);
                row.Children.Add(tzLabel);
                row.Children.Add(tzText);
                row.Children.Add(delBtn);
                citiesPanel.Children.Add(row);
                if (i < cities.Count - 1) citiesPanel.Children.Add(Separator());
            }
        }

        RefreshCities();
        listPanel.Children.Add(citiesPanel);

        var addBtn = new Button { Content = "+ 添加城市", Padding = new Thickness(12, 4), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(16, 4, 16, 8) };
        addBtn.Click += (a, e) =>
        {
            if (_svc.Settings.WorldClockCities.Count < 5)
            {
                _svc.Settings.WorldClockCities.Add(new WorldClockCity { Name = "北京", TimeZoneId = "China Standard Time" });
                AutoSave();
                RefreshCities();
            }
        };
        listPanel.Children.Add(addBtn);
        listPanel.Children.Add(Info("最多 5 个城市，选择城市后时区自动匹配"));
        s.Children.Add(Expander("城市", "管理显示的城市与时区", listPanel));

        return s;
    }

    Control BuildHolidayPanel()
    {
        var s = new StackPanel { Spacing = 0 };
        s.Children.Add(PageHeader("📅 节假日倒计时设置"));

        var displayPanel = new StackPanel { Spacing = 0 };
        displayPanel.Children.Add(SettingItem("显示数量", "同时显示多少个节日",
            Combo(new[] { "1", "3", "5" }, _svc.Settings.DisplayCount == 1 ? 0 : _svc.Settings.DisplayCount == 3 ? 1 : 2,
                v => { _svc.Settings.DisplayCount = v == 0 ? 1 : v == 1 ? 3 : 5; AutoSave(); })));
        displayPanel.Children.Add(Separator());
        displayPanel.Children.Add(SettingItem("显示放假天数", "如：春节（放7天）",
            Toggle(_svc.Settings.ShowDaysOff, v => { _svc.Settings.ShowDaysOff = v; AutoSave(); })));
        displayPanel.Children.Add(Separator());
        displayPanel.Children.Add(SettingItem("显示小时数", "节日当天显示剩余小时",
            Toggle(_svc.Settings.ShowHours, v => { _svc.Settings.ShowHours = v; AutoSave(); })));
        displayPanel.Children.Add(Separator());
        displayPanel.Children.Add(SettingItem("显示进度环", "首个节日显示弧形进度",
            Toggle(_svc.Settings.ShowProgressRing, v => { _svc.Settings.ShowProgressRing = v; AutoSave(); })));
        displayPanel.Children.Add(Separator());
        displayPanel.Children.Add(SettingItem("自动播放下一个", "节日过后自动显示下一个",
            Toggle(_svc.Settings.AutoNextHoliday, v => { _svc.Settings.AutoNextHoliday = v; AutoSave(); })));
        displayPanel.Children.Add(Separator());
        displayPanel.Children.Add(SettingItem("显示假期占比", "当年剩余假期百分比",
            Toggle(_svc.Settings.ShowYearRatio, v => { _svc.Settings.ShowYearRatio = v; AutoSave(); })));
        displayPanel.Children.Add(Separator());
        displayPanel.Children.Add(SettingItem("周末倒计时", "列表中显示周六周日",
            Toggle(_svc.Settings.ShowWeekendCountdown, v => { _svc.Settings.ShowWeekendCountdown = v; AutoSave(); })));
        s.Children.Add(Expander("显示", "节假日组件的显示选项", displayPanel));

        var workdayPanel = new StackPanel { Spacing = 0 };
        workdayPanel.Children.Add(SettingItem("调休提醒", "周末调休上课提前提醒",
            Toggle(_svc.Settings.ShowWorkdayReminder, v => { _svc.Settings.ShowWorkdayReminder = v; AutoSave(); })));
        workdayPanel.Children.Add(Separator());
        workdayPanel.Children.Add(SettingItem("提前提醒天数", "调休提醒提前多少天显示",
            Number(_svc.Settings.WorkdayReminderDays, 1, 30, v => { _svc.Settings.WorkdayReminderDays = v; AutoSave(); })));
        s.Children.Add(Expander("调休", "调休上课提醒设置", workdayPanel));

        var colorPanel = new StackPanel { Spacing = 0 };
        colorPanel.Children.Add(SettingItem("自动节日颜色", "根据节日自动匹配颜色",
            Toggle(_svc.Settings.AutoHolidayColor, v => { _svc.Settings.AutoHolidayColor = v; AutoSave(); })));
        colorPanel.Children.Add(Separator());
        foreach (var kv in _svc.Settings.HolidayColors.ToList())
        {
            var key = kv.Key;
            colorPanel.Children.Add(SettingItem(key, null,
                ColorPicker(kv.Value, c => { _svc.Settings.HolidayColors[key] = c; AutoSave(); })));
            if (key != _svc.Settings.HolidayColors.Keys.Last())
                colorPanel.Children.Add(Separator());
        }
        s.Children.Add(Expander("颜色", "节日颜色自定义", colorPanel));

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
                AutoSave();
            };
            switchPanel.Children.Add(SettingItem(name, null, chk));
            if (name != allHolidays.Last())
                switchPanel.Children.Add(Separator());
        }
        s.Children.Add(Expander("节日开关", "选择要显示的节假日", switchPanel));

        return s;
    }

    Control BuildGreetingPanel()
    {
        var s = new StackPanel { Spacing = 0 };
        s.Children.Add(PageHeader("💬 问候语设置"));

        var togglePanel = new StackPanel { Spacing = 0 };
        togglePanel.Children.Add(SettingItem("每天自动刷新问候语", "开启后每天自动从本地数据库随机刷新一条问候语",
            Toggle(_svc.Settings.AutoRefreshGreetings, v => { _svc.Settings.AutoRefreshGreetings = v; AutoSave(); })));
        togglePanel.Children.Add(Separator());
        var todayStatus = new TextBlock { Text = $"今天：{(_svc.IsTodayWorkday() ? "调休上班" : "正常")} / {(_svc.IsTodaySolarTerm() ? $"24节气-{_svc.GetTodaySolarTermName()}" : "非节气")}", Opacity = 0.7, FontSize = 12 };
        BindThemeForeground(todayStatus);
        togglePanel.Children.Add(SettingItem("今日状态", null, todayStatus));
        s.Children.Add(Expander("开关", "问候语基础设置", togglePanel));

        var schoolPanel = new StackPanel { Spacing = 0 };
        schoolPanel.Children.Add(SettingItem("放学时间", "时:分",
            new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, HorizontalAlignment = HorizontalAlignment.Right }.Also(h =>
            {
                h.Children.Add(Text(_svc.Settings.SchoolEndHour.ToString("D2"), 40, v =>
                {
                    if (int.TryParse(v, out var hval)) { _svc.Settings.SchoolEndHour = Math.Max(0, Math.Min(23, hval)); AutoSave(); }
                }));
                var colonBlock = new TextBlock { Text = ":", VerticalAlignment = VerticalAlignment.Center };
                BindThemeForeground(colonBlock);
                h.Children.Add(colonBlock);
                h.Children.Add(Text(_svc.Settings.SchoolEndMinute.ToString("D2"), 40, v =>
                {
                    if (int.TryParse(v, out var mval)) { _svc.Settings.SchoolEndMinute = Math.Max(0, Math.Min(59, mval)); AutoSave(); }
                }));
            })));
        schoolPanel.Children.Add(Separator());
        schoolPanel.Children.Add(SettingItem("提前提醒分钟", "放学前多少分钟切换提醒",
            Number(_svc.Settings.SchoolEndReminderMinutes, 1, 60, v => { _svc.Settings.SchoolEndReminderMinutes = v; AutoSave(); })));
        schoolPanel.Children.Add(Separator());
        schoolPanel.Children.Add(SettingItem("放学前文案", null,
            Text(_svc.Settings.BeforeSchoolEndText, 200, v => { _svc.Settings.BeforeSchoolEndText = v; AutoSave(); })));
        schoolPanel.Children.Add(Separator());
        schoolPanel.Children.Add(SettingItem("放学后文案", null,
            Text(_svc.Settings.AfterSchoolEndText, 200, v => { _svc.Settings.AfterSchoolEndText = v; AutoSave(); })));
        s.Children.Add(Expander("放学", "放学提醒设置", schoolPanel));

        s.Children.Add(Expander("课程联动", "根据课程表/临时课程显示问候语", BuildClassGreetingPanel()));
        s.Children.Add(Expander("时段文案", "自定义多个时间段的问候语", BuildTimeSlotPanel()));
        s.Children.Add(Expander("特殊日期", "设置特定星期几的问候语", BuildSpecialDatePanel()));

        return s;
    }

    StackPanel BuildClassGreetingPanel()
    {
        var panel = new StackPanel { Spacing = 0 };
        panel.Children.Add(SettingItem("启用课程联动问候语", "根据ClassIsland课程表（含临时课程）显示对应问候",
            Toggle(_svc.Settings.ClassGreetingEnabled, v => { _svc.Settings.ClassGreetingEnabled = v; AutoSave(); })));
        panel.Children.Add(Separator());
        panel.Children.Add(SettingItem("上课模板", "{A}=学科名 {C}=状态（兼容: {subject} {state}）",
            Text(_svc.Settings.ClassGreetingOnClassTemplate, 260, v => { _svc.Settings.ClassGreetingOnClassTemplate = v; AutoSave(); })));
        panel.Children.Add(Separator());
        panel.Children.Add(SettingItem("课间模板", "{B}=下节课名 {C}=状态（兼容: {next} {state}）",
            Text(_svc.Settings.ClassGreetingBreakTemplate, 260, v => { _svc.Settings.ClassGreetingBreakTemplate = v; AutoSave(); })));
        panel.Children.Add(Separator());
        panel.Children.Add(SettingItem("准备上课模板", "{B}=下节课名 {C}=状态（兼容: {next} {state}）",
            Text(_svc.Settings.ClassGreetingPrepareTemplate, 260, v => { _svc.Settings.ClassGreetingPrepareTemplate = v; AutoSave(); })));
        panel.Children.Add(Separator());
        panel.Children.Add(SettingItem("放学模板", "{C}=状态（兼容: {state}）",
            Text(_svc.Settings.ClassGreetingAfterSchoolTemplate, 260, v => { _svc.Settings.ClassGreetingAfterSchoolTemplate = v; AutoSave(); })));
        panel.Children.Add(Separator());
        panel.Children.Add(SettingItem("无课程模板", "{C}=状态（兼容: {state}）",
            Text(_svc.Settings.ClassGreetingNoClassTemplate, 260, v => { _svc.Settings.ClassGreetingNoClassTemplate = v; AutoSave(); })));
        return panel;
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
                var startBox = Text($"{slot.StartHour:D2}:{slot.StartMinute:D2}", 50, v =>
                {
                    if (TimeSpan.TryParse(v, out var ts)) { slot.StartHour = ts.Hours; slot.StartMinute = ts.Minutes; AutoSave(); }
                });
                var endBox = Text($"{slot.EndHour:D2}:{slot.EndMinute:D2}", 50, v =>
                {
                    if (TimeSpan.TryParse(v, out var ts)) { slot.EndHour = ts.Hours; slot.EndMinute = ts.Minutes; AutoSave(); }
                });
                var tags = new[] { "早晨", "上午", "中午", "下午", "傍晚", "晚上" };
                var tagCombo = new ComboBox { Width = 60 };
                foreach (var t in tags) tagCombo.Items.Add(t);
                tagCombo.SelectedItem = tags.Contains(slot.Tag) ? slot.Tag : GetTimeSlotTag(slot.StartHour);
                tagCombo.SelectionChanged += (a, b) => { slot.Tag = tagCombo.SelectedItem?.ToString() ?? GetTimeSlotTag(slot.StartHour); AutoSave(); };
                var textBox = Text(slot.Text, 140, v => { slot.Text = v; AutoSave(); });
                var refreshBtn = new Button { Content = "刷新", Padding = new Thickness(6, 2), Foreground = new SolidColorBrush(Color.Parse("#FF2196F3")) };
                refreshBtn.Click += (a, e) =>
                {
                    var tag = string.IsNullOrEmpty(slot.Tag) ? GetTimeSlotTag(slot.StartHour) : slot.Tag;
                    var text = LocalGreetingDB.GetRandom(tag, LocalGreetingDB.TimeSlotGreetings);
                    if (!string.IsNullOrEmpty(text)) slot.Text = text;
                    AutoSave();
                    RefreshList();
                };
                var delBtn = new Button { Content = "删除", Padding = new Thickness(6, 2), Foreground = new SolidColorBrush(Color.Parse("#FFE53935")) };
                delBtn.Click += (a, e) => { _svc.Settings.TimeSlotGreetings.Remove(slot); AutoSave(); RefreshList(); };

                var fromBlock = new TextBlock { Text = "从", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 };
                BindThemeForeground(fromBlock);
                row.Children.Add(fromBlock);
                row.Children.Add(startBox);
                var toBlock = new TextBlock { Text = "到", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 };
                BindThemeForeground(toBlock);
                row.Children.Add(toBlock);
                row.Children.Add(endBox);
                var tagLabelBlock = new TextBlock { Text = "标签", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 };
                BindThemeForeground(tagLabelBlock);
                row.Children.Add(tagLabelBlock);
                row.Children.Add(tagCombo);
                row.Children.Add(textBox);
                row.Children.Add(refreshBtn);
                row.Children.Add(delBtn);
                listPanel.Children.Add(row);
                if (i < slots.Count - 1)
                    listPanel.Children.Add(Separator());
            }
        }

        RefreshList();
        panel.Children.Add(listPanel);

        var addBtn = new Button { Content = "+ 添加时段", Padding = new Thickness(12, 4), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(16, 4, 16, 8) };
        addBtn.Click += (a, e) =>
        {
            _svc.Settings.TimeSlotGreetings.Add(new Models.TimeSlotGreeting { StartHour = 8, StartMinute = 0, EndHour = 12, EndMinute = 0, Text = "" });
            AutoSave();
            RefreshList();
        };
        panel.Children.Add(addBtn);

        var refreshAllBtn = new Button { Content = "🔄 一键刷新全部文案", Padding = new Thickness(12, 4), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(16, 0, 16, 8) };
        refreshAllBtn.Click += async (a, e) =>
        {
            _svc.RefreshAllGreetings();
            refreshAllBtn.Content = "✅ 已刷新";
            RefreshList();
            await Task.Delay(1500);
            refreshAllBtn.Content = "🔄 一键刷新全部文案";
        };
        panel.Children.Add(refreshAllBtn);

        panel.Children.Add(Info("留空的时段会自动使用本地数据库按标签（早晨/上午/中午/下午/傍晚/晚上）每天刷新一条问候语"));

        return panel;
    }

    static string GetTimeSlotTag(int hour)
    {
        return hour switch
        {
            >= 5 and < 8 => "早晨",
            >= 8 and < 12 => "上午",
            >= 12 and < 14 => "中午",
            >= 14 and < 17 => "下午",
            >= 17 and < 19 => "傍晚",
            _ => "晚上"
        };
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
                var nameBox = Text(item.Name, 80, v => { item.Name = v; AutoSave(); });
                var dayCombo = new ComboBox { Width = 70 };
                var days = new[] { "周一", "周二", "周三", "周四", "周五", "周六", "周日" };
                foreach (var d in days) dayCombo.Items.Add(d);
                dayCombo.SelectedIndex = Math.Max(0, Math.Min(6, item.DayOfWeek - 1));
                dayCombo.SelectionChanged += (a, b) => { item.DayOfWeek = dayCombo.SelectedIndex + 1; AutoSave(); };
                var enabledChk = new CheckBox { Content = "启用", IsChecked = item.Enabled };
                enabledChk.IsCheckedChanged += (a, b) => { item.Enabled = enabledChk.IsChecked == true; AutoSave(); };
                var tags = new[] { "周一", "周二", "周三", "周四", "周五", "周六", "周日", "周末" };
                var tagCombo = new ComboBox { Width = 60 };
                foreach (var t in tags) tagCombo.Items.Add(t);
                tagCombo.SelectedItem = tags.Contains(item.Tag) ? item.Tag : (item.DayOfWeek == 6 || item.DayOfWeek == 7 ? "周末" : $"周{new[] { "一", "二", "三", "四", "五", "六", "日" }[item.DayOfWeek - 1]}");
                tagCombo.SelectionChanged += (a, b) => { item.Tag = tagCombo.SelectedItem?.ToString() ?? ""; AutoSave(); };
                var refreshBtn = new Button { Content = "刷新", Padding = new Thickness(6, 2), Foreground = new SolidColorBrush(Color.Parse("#FF2196F3")) };
                refreshBtn.Click += (a, e) =>
                {
                    var tag = string.IsNullOrEmpty(item.Tag) ? (item.DayOfWeek == 6 || item.DayOfWeek == 7 ? "周末" : $"周{new[] { "一", "二", "三", "四", "五", "六", "日" }[item.DayOfWeek - 1]}") : item.Tag;
                    var text = LocalGreetingDB.GetRandom(tag, LocalGreetingDB.WeeklyReminders);
                    if (!string.IsNullOrEmpty(text)) item.Text = text;
                    AutoSave();
                    RefreshList();
                };
                var delBtn = new Button { Content = "删除", Padding = new Thickness(6, 2), Foreground = new SolidColorBrush(Color.Parse("#FFE53935")) };
                delBtn.Click += (a, e) => { _svc.Settings.SpecialDateGreetings.Remove(item); AutoSave(); RefreshList(); };

                headerRow.Children.Add(nameBox);
                headerRow.Children.Add(dayCombo);
                headerRow.Children.Add(tagCombo);
                headerRow.Children.Add(enabledChk);
                headerRow.Children.Add(refreshBtn);
                headerRow.Children.Add(delBtn);
                row.Children.Add(headerRow);

                var timeRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
                var startBox = Text($"{item.StartHour:D2}:{item.StartMinute:D2}", 50, v =>
                {
                    if (TimeSpan.TryParse(v, out var ts)) { item.StartHour = ts.Hours; item.StartMinute = ts.Minutes; AutoSave(); }
                });
                var endBox = Text($"{item.EndHour:D2}:{item.EndMinute:D2}", 50, v =>
                {
                    if (TimeSpan.TryParse(v, out var ts)) { item.EndHour = ts.Hours; item.EndMinute = ts.Minutes; AutoSave(); }
                });
                var textBox = Text(item.Text, 160, v => { item.Text = v; AutoSave(); });

                var fromBlock = new TextBlock { Text = "从", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 };
                BindThemeForeground(fromBlock);
                timeRow.Children.Add(fromBlock);
                timeRow.Children.Add(startBox);
                var toBlock = new TextBlock { Text = "到", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 };
                BindThemeForeground(toBlock);
                timeRow.Children.Add(toBlock);
                timeRow.Children.Add(endBox);
                timeRow.Children.Add(textBox);
                row.Children.Add(timeRow);

                listPanel.Children.Add(row);
                if (i < items.Count - 1)
                    listPanel.Children.Add(Separator());
            }
        }

        RefreshList();
        panel.Children.Add(listPanel);

        var addBtn = new Button { Content = "+ 添加特殊日期", Padding = new Thickness(12, 4), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(16, 4, 16, 8) };
        addBtn.Click += (a, e) =>
        {
            _svc.Settings.SpecialDateGreetings.Add(new Models.SpecialDateGreeting { Name = "新日期", DayOfWeek = 1, StartHour = 0, StartMinute = 0, EndHour = 23, EndMinute = 59, Text = "" });
            AutoSave();
            RefreshList();
        };
        panel.Children.Add(addBtn);

        return panel;
    }

    Control BuildSolarTermPanel()
    {
        var s = new StackPanel { Spacing = 0 };
        s.Children.Add(PageHeader("🌿 24节气设置"));

        var displayPanel = new StackPanel { Spacing = 0 };
        displayPanel.Children.Add(SettingItem("显示进度环", "节气倒计时显示弧形进度",
            Toggle(_svc.Settings.SolarTermShowProgressRing, v => { _svc.Settings.SolarTermShowProgressRing = v; AutoSave(); })));
        s.Children.Add(Expander("显示", "节气组件显示选项", displayPanel));

        var colorPanel = new StackPanel { Spacing = 0 };
        var colors = _svc.Settings.TermColors.OrderBy(x => x.Key).ToList();
        for (int i = 0; i < colors.Count; i++)
        {
            var kv = colors[i];
            var key = kv.Key;
            colorPanel.Children.Add(SettingItem(key, null,
                ColorPicker(kv.Value, c => { _svc.Settings.TermColors[key] = c; AutoSave(); })));
            if (i < colors.Count - 1)
                colorPanel.Children.Add(Separator());
        }
        s.Children.Add(Expander("颜色", "各节气显示颜色自定义", colorPanel));

        return s;
    }

    Control BuildLunarPanel()
    {
        var s = new StackPanel { Spacing = 0 };
        s.Children.Add(PageHeader("\uE8C0 农历日期设置"));

        var displayPanel = new StackPanel { Spacing = 0 };
        displayPanel.Children.Add(SettingItem("自动刷新", "每天自动重新计算农历",
            Toggle(_svc.Settings.LunarAutoRefresh, v => { _svc.Settings.LunarAutoRefresh = v; AutoSave(); })));
        displayPanel.Children.Add(Separator());
        displayPanel.Children.Add(SettingItem("显示模板", "可用变量: {A} 干支年 | {B} 农历月 | {C} 农历日 | {D} 生肖 | {E} 节气 | {F} 农历日期（兼容旧变量: {gzYear} {IMonthCn} {IDayCn} {Animal} {Term} {lunarDate}）",
            Text(_svc.Settings.LunarDateTemplate, 280, v => { _svc.Settings.LunarDateTemplate = v; AutoSave(); })));
        s.Children.Add(Expander("显示", "农历组件显示选项", displayPanel));

        s.Children.Add(Info("示例: 癸卯年 九月初八 兔"));
        return s;
    }

    Control BuildCustomHolidayPanel()
    {
        var s = new StackPanel { Spacing = 0 };
        s.Children.Add(PageHeader("🎂 自定义节日设置"));

        var displayPanel = new StackPanel { Spacing = 0 };
        displayPanel.Children.Add(SettingItem("显示数量", "同时显示多少个自定义节日",
            Number(_svc.Settings.CustomHolidayDisplayCount, 1, 10, v => { _svc.Settings.CustomHolidayDisplayCount = v; AutoSave(); })));
        displayPanel.Children.Add(Separator());
        displayPanel.Children.Add(SettingItem("显示图标", "在节日前显示图标",
            Toggle(_svc.Settings.CustomHolidayShowIcon, v => { _svc.Settings.CustomHolidayShowIcon = v; AutoSave(); })));
        displayPanel.Children.Add(Separator());
        displayPanel.Children.Add(SettingItem("显示天数", "显示距离节日还有多少天",
            Toggle(_svc.Settings.CustomHolidayShowDays, v => { _svc.Settings.CustomHolidayShowDays = v; AutoSave(); })));
        s.Children.Add(Expander("组件显示", "自定义节日组件显示选项", displayPanel));

        s.Children.Add(Expander("节日列表", "添加和管理你的自定义节日", BuildCustomHolidayList()));

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
                p.Children.Add(Separator());
        }
        var btn = new Button { Content = "➕ 添加", Padding = new Thickness(12, 6), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(16, 8, 16, 8) };
        btn.Click += (a, e) =>
        {
            var h = new Models.CustomHoliday { Name = "新节日", Date = DateTime.Now.AddDays(1) };
            _svc.Settings.CustomHolidays.Add(h);
            AutoSave();
            RefreshCurrentTab();
        };
        p.Children.Add(btn);
        return p;
    }

    Control MakeCustomHolidayItem(Models.CustomHoliday h)
    {
        var g = new Grid { ColumnDefinitions = new ColumnDefinitions("120 100 120 Auto Auto"), Margin = new Thickness(16, 8, 16, 8) };
        var n = new TextBox { Text = h.Name, Margin = new Thickness(0, 0, 8, 0) };
        n.TextChanged += (a, b) => { h.Name = n.Text ?? ""; AutoSave(); };
        Grid.SetColumn(n, 0);

        var dateText = new TextBlock { Text = $"{h.Date.Month}月{h.Date.Day}日", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        BindThemeForeground(dateText);
        Grid.SetColumn(dateText, 1);

        var d = new DatePicker { SelectedDate = h.Date, Margin = new Thickness(0, 0, 8, 0), Width = 120 };
        d.SelectedDateChanged += (a, b) =>
        {
            if (d.SelectedDate.HasValue)
            {
                h.Date = d.SelectedDate.Value.DateTime;
                dateText.Text = $"{h.Date.Month}月{h.Date.Day}日";
                AutoSave();
            }
        };
        Grid.SetColumn(d, 2);

        var r = new CheckBox { Content = "每年", IsChecked = h.RepeatYearly, VerticalAlignment = VerticalAlignment.Center };
        r.IsCheckedChanged += (a, b) => { h.RepeatYearly = r.IsChecked == true; AutoSave(); };
        Grid.SetColumn(r, 3);

        var del = new Button { Content = "删除", Width = 50 };
        del.Click += (a, b) => { _svc.Settings.CustomHolidays.Remove(h); AutoSave(); RefreshCurrentTab(); };
        Grid.SetColumn(del, 4);

        g.Children.Add(n); g.Children.Add(dateText); g.Children.Add(d); g.Children.Add(r); g.Children.Add(del);
        return g;
    }

    Control BuildVacationPanel()
    {
        var s = new StackPanel { Spacing = 0 };
        s.Children.Add(PageHeader("🏖️ 寒暑假设置"));

        var displayPanel = new StackPanel { Spacing = 0 };
        displayPanel.Children.Add(SettingItem("启用寒暑假倒计时", "关闭后不显示任何内容",
            Toggle(_svc.Settings.ShowVacationCountdown, v => { _svc.Settings.ShowVacationCountdown = v; AutoSave(); })));
        s.Children.Add(Expander("开关", "寒暑假倒计时总开关", displayPanel));

        var summerPanel = new StackPanel { Spacing = 0 };
        summerPanel.Children.Add(SettingItem("开始日期", null,
            Date(_svc.Settings.SummerStart, v => { _svc.Settings.SummerStart = v; AutoSave(); })));
        summerPanel.Children.Add(Separator());
        summerPanel.Children.Add(SettingItem("结束日期", null,
            Date(_svc.Settings.SummerEnd, v => { _svc.Settings.SummerEnd = v; AutoSave(); })));
        s.Children.Add(Expander("暑假", "暑假时间安排", summerPanel));

        var winterPanel = new StackPanel { Spacing = 0 };
        winterPanel.Children.Add(SettingItem("开始日期", null,
            Date(_svc.Settings.WinterStart, v => { _svc.Settings.WinterStart = v; AutoSave(); })));
        winterPanel.Children.Add(Separator());
        winterPanel.Children.Add(SettingItem("结束日期", null,
            Date(_svc.Settings.WinterEnd, v => { _svc.Settings.WinterEnd = v; AutoSave(); })));
        s.Children.Add(Expander("寒假", "寒假时间安排", winterPanel));

        return s;
    }

    Control BuildWeatherPanel()
    {
        var s = new StackPanel { Spacing = 0 };
        s.Children.Add(PageHeader("🌤️ 天气问候设置"));

        var layoutPanel = new StackPanel { Spacing = 0 };
        var presets = new[] { "仅问候", "图标+问候", "图标+天气+温度+问候", "图标+温度", "完整信息" };
        var presetCombo = new ComboBox { Width = 120, HorizontalAlignment = HorizontalAlignment.Right };
        foreach (var p in presets) presetCombo.Items.Add(p);

        var currentTemplate = _svc.Settings.WeatherTemplate ?? "{icon} {weather} {temp} {greeting}";
        presetCombo.SelectedIndex = currentTemplate switch
        {
            "{D}" => 0,
            "{A} {D}" => 1,
            "{A}{B} {C} {D}" => 2,
            "{A}{C}" => 3,
            "{A}{B} {C} {D} {E}" => 4,
            "{icon} {weather} {temp} {greeting}" => 2,
            _ => -1
        };

        // 自定义模板 TextBox 需要在预设切换时同步更新
        var templateTextBox = new TextBox { Text = currentTemplate, Width = 280 };
        templateTextBox.TextChanged += (s, e) =>
        {
            _svc.Settings.WeatherTemplate = templateTextBox.Text ?? "";
            AutoSave();
        };

        presetCombo.SelectionChanged += (a, b) =>
        {
            var newTemplate = presetCombo.SelectedIndex switch
            {
                0 => "{D}",
                1 => "{A} {D}",
                2 => "{A}{B} {C} {D}",
                3 => "{A}{C}",
                4 => "{A}{B} {C} {D} {E}",
                _ => _svc.Settings.WeatherTemplate ?? "{icon} {weather} {temp} {greeting}"
            };
            _svc.Settings.WeatherTemplate = newTemplate;
            // 同步到自定义模板输入框
            templateTextBox.Text = newTemplate;
            AutoSave();
        };

        layoutPanel.Children.Add(SettingItem("预设模板", "快速选择排版样式", presetCombo));
        layoutPanel.Children.Add(Separator());
        layoutPanel.Children.Add(SettingItem("自定义模板", null, templateTextBox));
        layoutPanel.Children.Add(Separator());
        layoutPanel.Children.Add(SettingItem("显示天气图标", "在模板中使用 {icon}",
            Toggle(_svc.Settings.WeatherShowIcon, v => { _svc.Settings.WeatherShowIcon = v; AutoSave(); })));
        layoutPanel.Children.Add(Separator());
        layoutPanel.Children.Add(SettingItem("显示温度", "在模板中使用 {temp}",
            Toggle(_svc.Settings.WeatherShowTemp, v => { _svc.Settings.WeatherShowTemp = v; AutoSave(); })));
        layoutPanel.Children.Add(Separator());
        layoutPanel.Children.Add(SettingItem("温度按冷暖变色", "根据温度自动调整温度文本颜色",
            Toggle(_svc.Settings.SmartWeatherTempColorEnabled, v => { _svc.Settings.SmartWeatherTempColorEnabled = v; AutoSave(); })));
        layoutPanel.Children.Add(Separator());
        layoutPanel.Children.Add(SettingItem("预警置顶", "有预警时优先完整显示预警信息",
            Toggle(_svc.Settings.SmartWeatherWarningOverride, v => { _svc.Settings.SmartWeatherWarningOverride = v; AutoSave(); })));
        layoutPanel.Children.Add(Separator());
        layoutPanel.Children.Add(Info("短变量: {A}=天气图标 {B}=天气文字 {C}=温度 {D}=穿衣问候 {E}=预警文字 {F}=降雨/停雨提示 {G}=问候+下雨提醒 {H}=更新状态 {I}=彩色图标\n兼容旧变量: {icon} {weather} {temp} {greeting} {warning} {rain}"));
        s.Children.Add(Expander("排版", "自定义天气问候的显示格式", layoutPanel));

        s.Children.Add(Expander("温度提醒", "自定义各温度区间的穿衣提醒文案", BuildTempPanel()));
        s.Children.Add(Expander("天气关键词", "根据天气关键词匹配显示文案", BuildWeatherGreetingPanel()));

        // 天气提醒设置融合至此
        s.Children.Add(Expander("天气变化提醒[测试版]", "天气变化提醒设置", BuildWeatherReminderPanel()));

        s.Children.Add(Info("天气数据来自ClassIsland内置天气服务，每分钟自动刷新一次。"));
        return s;
    }

    Control BuildWeatherReminderPanel()
    {
        var s = new StackPanel { Spacing = 0 };

        var basicPanel = new StackPanel { Spacing = 0 };
        basicPanel.Children.Add(SettingItem("启用天气变化提醒", "关闭后组件不显示任何内容",
            Toggle(_svc.Settings.WeatherReminderEnabled, v => { _svc.Settings.WeatherReminderEnabled = v; AutoSave(); })));
        basicPanel.Children.Add(Separator());

        // 刷新间隔：Slider 横条拖动 1-10 分钟
        var refreshSlider = new Slider
        {
            Minimum = 1,
            Maximum = 10,
            Value = Math.Max(1, Math.Min(10, _svc.Settings.WeatherReminderRefreshMinutes)),
            Width = 180,
            HorizontalAlignment = HorizontalAlignment.Right,
            TickFrequency = 1,
            IsSnapToTickEnabled = true
        };
        var refreshValueText = new TextBlock
        {
            Text = $"{_svc.Settings.WeatherReminderRefreshMinutes}分钟",
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 12,
            Margin = new Thickness(8, 0, 0, 0),
            Width = 50,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        BindThemeForeground(refreshValueText);
        var refreshPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 4,
            Children = { refreshSlider, refreshValueText }
        };
        refreshSlider.ValueChanged += (a, b) =>
        {
            var val = (int)Math.Round(refreshSlider.Value);
            _svc.Settings.WeatherReminderRefreshMinutes = val;
            refreshValueText.Text = $"{val}分钟";
            AutoSave();
        };

        basicPanel.Children.Add(SettingItem("刷新间隔", "拖动设置多久评估一次天气变化（1-10分钟）", refreshPanel));
        basicPanel.Children.Add(Separator());

        // 随机刷新区间输入框（默认30-60分钟）
        var randomMinBox = new TextBox
        {
            Text = _svc.Settings.WeatherReminderRandomMinMinutes.ToString(),
            Width = 60,
            HorizontalAlignment = HorizontalAlignment.Right,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        var randomMaxBox = new TextBox
        {
            Text = _svc.Settings.WeatherReminderRandomMaxMinutes.ToString(),
            Width = 60,
            HorizontalAlignment = HorizontalAlignment.Right,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        var dashBlock = new TextBlock { Text = "-", VerticalAlignment = VerticalAlignment.Center, FontSize = 14 };
        BindThemeForeground(dashBlock);
        var minBlock = new TextBlock { Text = "分钟", VerticalAlignment = VerticalAlignment.Center, FontSize = 12, Opacity = 0.7 };
        BindThemeForeground(minBlock);
        var maxBlock = new TextBlock { Text = "分钟", VerticalAlignment = VerticalAlignment.Center, FontSize = 12, Opacity = 0.7 };
        BindThemeForeground(maxBlock);
        var randomPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 4,
            Children = { randomMinBox, minBlock, dashBlock, randomMaxBox, maxBlock }
        };
        randomMinBox.LostFocus += (a, b) =>
        {
            if (int.TryParse(randomMinBox.Text, out var v) && v >= 1)
                _svc.Settings.WeatherReminderRandomMinMinutes = v;
            else
                randomMinBox.Text = _svc.Settings.WeatherReminderRandomMinMinutes.ToString();
            AutoSave();
        };
        randomMaxBox.LostFocus += (a, b) =>
        {
            if (int.TryParse(randomMaxBox.Text, out var v) && v >= 1)
                _svc.Settings.WeatherReminderRandomMaxMinutes = v;
            else
                randomMaxBox.Text = _svc.Settings.WeatherReminderRandomMaxMinutes.ToString();
            AutoSave();
        };

        basicPanel.Children.Add(SettingItem("随机刷新区间", "在此区间内随机选择时间刷新一条变化提醒", randomPanel));
        basicPanel.Children.Add(Separator());
        basicPanel.Children.Add(SettingItem("变化时立即刷新", "检测到天气变化时立即更新显示",
            Toggle(_svc.Settings.WeatherReminderShowImmediatelyOnChange, v => { _svc.Settings.WeatherReminderShowImmediatelyOnChange = v; AutoSave(); })));
        s.Children.Add(Expander("基础设置", "天气变化提醒刷新策略", basicPanel));

        // 提醒规则：三列布局 + 分割线
        var rulePanel = new StackPanel { Spacing = 0 };
        var evaluator = new WeatherReminderEvaluator(_svc);
        var allRules = evaluator.GetAllRules();
        var enabledIds = _svc.Settings.EnabledWeatherReminderRuleIds;

        // 将规则分为三组，每组放在一列
        var totalRules = allRules.Count;
        var colSize = (int)Math.Ceiling(totalRules / 3.0);

        var columnsWrap = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(8, 4, 8, 4)
        };

        for (int col = 0; col < 3; col++)
        {
            var colPanel = new StackPanel
            {
                Width = 220,
                Spacing = 2,
                Margin = new Thickness(col > 0 ? 12 : 0, 0, 0, 0)
            };

            // 在第二列和第三列前加分割线
            if (col > 0)
            {
                var vLine = new Border
                {
                    Width = 1,
                    Background = new SolidColorBrush(Color.Parse("#30FFFFFF")),
                    Margin = new Thickness(-6, 0, 0, 0),
                    Height = colSize * 36
                };
            }

            for (int i = col * colSize; i < Math.Min((col + 1) * colSize, totalRules); i++)
            {
                var rule = allRules[i];
                var ruleId = rule.Id;
                var isEnabled = enabledIds.Contains(ruleId) || (enabledIds.Count == 0 && rule.EnabledByDefault);
                var chk = new CheckBox
                {
                    Content = $"{rule.DefaultIcon} {rule.Name}",
                    IsChecked = isEnabled
                };
                chk.IsCheckedChanged += (a, b) =>
                {
                    if (chk.IsChecked == true)
                    {
                        if (!enabledIds.Contains(ruleId)) enabledIds.Add(ruleId);
                    }
                    else
                    {
                        enabledIds.Remove(ruleId);
                    }
                    AutoSave();
                };
                colPanel.Children.Add(chk);
            }

            columnsWrap.Children.Add(colPanel);

            // 在第一列和第二列之间加垂直分割线
            if (col < 2)
            {
                var vSep = new Border
                {
                    Width = 1,
                    Height = colSize * 36,
                    Background = new SolidColorBrush(Color.Parse("#30FFFFFF")),
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Margin = new Thickness(0, 4, 0, 4)
                };
                columnsWrap.Children.Add(vSep);
            }
        }

        rulePanel.Children.Add(columnsWrap);
        s.Children.Add(Expander("提醒规则", "勾选要启用的天气变化提醒类型", rulePanel));

        s.Children.Add(Info("天气变化提醒为测试版功能，当前内置 " + totalRules + " 条规则。随机刷新区间控制每次刷新提醒时的随机延迟秒数。"));
        return s;
    }

    StackPanel BuildTempPanel()
    {
        // 打开面板时先对齐一次，确保展示无重叠区间
        _svc.AlignTempGreetings();

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

                // 下限由上一区间上限自动推导，只读
                var minBox = new TextBox { Text = item.MinTemp.ToString(), Width = 45, IsReadOnly = true, Opacity = 0.6 };

                var maxBox = new TextBox { Text = item.MaxTemp == 999 ? "" : item.MaxTemp.ToString(), Width = 45, Watermark = "∞" };
                maxBox.LostFocus += (a, b) =>
                {
                    if (string.IsNullOrEmpty(maxBox.Text)) item.MaxTemp = 999;
                    else if (int.TryParse(maxBox.Text, out var v)) item.MaxTemp = v;
                    else maxBox.Text = item.MaxTemp == 999 ? "" : item.MaxTemp.ToString();
                    _svc.AlignTempGreetings();
                    AutoSave();
                    RefreshList();
                };

                var tags = new[] { "极寒", "寒冷", "偏冷", "凉", "微凉", "舒适", "偏热", "炎热", "极热" };
                var tagCombo = new ComboBox { Width = 70 };
                foreach (var t in tags) tagCombo.Items.Add(t);
                tagCombo.SelectedItem = tags.Contains(item.Tag) ? item.Tag : "舒适";
                tagCombo.SelectionChanged += (a, b) => { item.Tag = tagCombo.SelectedItem?.ToString() ?? "舒适"; AutoSave(); };
                var textBox = Text(item.Text, 160, v => { item.Text = v; AutoSave(); });
                var refreshBtn = new Button { Content = "刷新", Padding = new Thickness(6, 2), Foreground = new SolidColorBrush(Color.Parse("#FF2196F3")) };
                refreshBtn.Click += (a, e) =>
                {
                    var tag = string.IsNullOrEmpty(item.Tag) ? "舒适" : item.Tag;
                    var text = LocalGreetingDB.GetRandom(tag, LocalGreetingDB.WeatherGreetings);
                    if (!string.IsNullOrEmpty(text)) item.Text = text;
                    AutoSave();
                    RefreshList();
                };
                var delBtn = new Button { Content = "删除", Padding = new Thickness(6, 2), Foreground = new SolidColorBrush(Color.Parse("#FFE53935")) };
                delBtn.Click += (a, e) => { _svc.Settings.TempGreetings.Remove(item); _svc.AlignTempGreetings(); AutoSave(); RefreshList(); };

                var geBlock = new TextBlock { Text = "≥", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 };
                BindThemeForeground(geBlock);
                row.Children.Add(geBlock);
                row.Children.Add(minBox);
                var c1Block = new TextBlock { Text = "°C", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 };
                BindThemeForeground(c1Block);
                row.Children.Add(c1Block);
                var tildeBlock = new TextBlock { Text = "~", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 };
                BindThemeForeground(tildeBlock);
                row.Children.Add(tildeBlock);
                row.Children.Add(maxBox);
                var c2Block = new TextBlock { Text = "°C", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 };
                BindThemeForeground(c2Block);
                row.Children.Add(c2Block);
                row.Children.Add(tagCombo);
                row.Children.Add(textBox);
                row.Children.Add(refreshBtn);
                row.Children.Add(delBtn);
                listPanel.Children.Add(row);
                if (i < items.Count - 1)
                    listPanel.Children.Add(Separator());
            }
        }

        RefreshList();
        panel.Children.Add(listPanel);

        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(16, 4, 16, 8) };
        var addBtn = new Button { Content = "+ 添加温度区间", Padding = new Thickness(12, 4), HorizontalAlignment = HorizontalAlignment.Left };
        addBtn.Click += (a, e) =>
        {
            var last = _svc.Settings.TempGreetings.OrderBy(g => g.MinTemp).LastOrDefault();
            var start = last != null ? last.MaxTemp + 1 : 0;
            _svc.Settings.TempGreetings.Add(new Models.TempGreeting { MinTemp = start, MaxTemp = 999, Text = "", Tag = "" });
            _svc.AlignTempGreetings();
            AutoSave();
            RefreshList();
        };
        btnRow.Children.Add(addBtn);

        var resetBtn = new Button { Content = "🔄 恢复默认", Padding = new Thickness(12, 4), HorizontalAlignment = HorizontalAlignment.Left };
        resetBtn.Click += (a, e) =>
        {
            _svc.Settings.TempGreetings.Clear();
            foreach (var g in Models.LocalGreetingDB.DefaultTempGreetings)
                _svc.Settings.TempGreetings.Add(new Models.TempGreeting { MinTemp = g.MinTemp, MaxTemp = g.MaxTemp, Text = g.Text, Tag = g.Tag });
            _svc.AlignTempGreetings();
            AutoSave();
            RefreshList();
        };
        btnRow.Children.Add(resetBtn);

        var refreshAllTempBtn = new Button { Content = "🔄 刷新全部温度提醒", Padding = new Thickness(12, 4), HorizontalAlignment = HorizontalAlignment.Left };
        refreshAllTempBtn.Click += (a, e) =>
        {
            _svc.RefreshAllTempGreetings();
            RefreshList();
        };
        btnRow.Children.Add(refreshAllTempBtn);

        panel.Children.Add(btnRow);

        return panel;
    }

    StackPanel BuildWeatherGreetingPanel()
    {
        var panel = new StackPanel { Spacing = 8 };
        var listPanel = new StackPanel { Spacing = 0 };

        void RefreshList()
        {
            listPanel.Children.Clear();
            var items = _svc.Settings.WeatherGreetingItems.ToList();
            for (int i = 0; i < items.Count; i++)
            {
                var kv = items[i];
                var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(16, 8, 16, 8) };
                var keyBox = new TextBox { Text = kv.Keyword, Width = 80, IsReadOnly = kv.Keyword == "默认" };
                keyBox.TextChanged += (a, b) =>
                {
                    if (kv.Keyword == "默认") return;
                    var newKey = keyBox.Text ?? "";
                    if (newKey != kv.Keyword && !string.IsNullOrEmpty(newKey) && !_svc.Settings.WeatherGreetingItems.Any(x => x.Keyword == newKey))
                    {
                        kv.Keyword = newKey;
                        AutoSave();
                    }
                };
                var weatherTags = new[] { "雨天", "寒冷", "高温", "舒适", "恶劣天气", "大风", "雷电", "默认" };
                var tagCombo = new ComboBox { Width = 70 };
                foreach (var t in weatherTags) tagCombo.Items.Add(t);
                tagCombo.SelectedItem = weatherTags.Contains(kv.Tag) ? kv.Tag : "默认";
                tagCombo.SelectionChanged += (a, b) => { kv.Tag = tagCombo.SelectedItem?.ToString() ?? "默认"; AutoSave(); };
                var textBox = Text(kv.Text, 160, v => { kv.Text = v; AutoSave(); });
                var refreshBtn = new Button { Content = "刷新", Padding = new Thickness(6, 2), Foreground = new SolidColorBrush(Color.Parse("#FF2196F3")) };
                refreshBtn.Click += (a, e) =>
                {
                    var tag = string.IsNullOrEmpty(kv.Tag) ? "默认" : kv.Tag;
                    var text = LocalGreetingDB.GetRandom(tag, LocalGreetingDB.WeatherGreetings);
                    if (!string.IsNullOrEmpty(text)) kv.Text = text;
                    AutoSave();
                    RefreshList();
                };
                var delBtn = new Button { Content = "删除", Padding = new Thickness(6, 2), Foreground = new SolidColorBrush(Color.Parse("#FFE53935")), IsVisible = kv.Keyword != "默认" };
                delBtn.Click += (a, e) => { _svc.Settings.WeatherGreetingItems.Remove(kv); AutoSave(); RefreshList(); };

                var kwBlock = new TextBlock { Text = "关键词", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 };
                BindThemeForeground(kwBlock);
                row.Children.Add(kwBlock);
                row.Children.Add(keyBox);
                var tagBlock = new TextBlock { Text = "标签", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 };
                BindThemeForeground(tagBlock);
                row.Children.Add(tagBlock);
                row.Children.Add(tagCombo);
                var textBlock = new TextBlock { Text = "文案", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 };
                BindThemeForeground(textBlock);
                row.Children.Add(textBlock);
                row.Children.Add(textBox);
                row.Children.Add(refreshBtn);
                row.Children.Add(delBtn);
                listPanel.Children.Add(row);
                if (i < items.Count - 1)
                    listPanel.Children.Add(Separator());
            }
        }

        RefreshList();
        panel.Children.Add(listPanel);

        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(16, 4, 16, 8) };
        var addBtn = new Button { Content = "+ 添加天气问候", Padding = new Thickness(12, 4), HorizontalAlignment = HorizontalAlignment.Left };
        addBtn.Click += (a, e) =>
        {
            _svc.Settings.WeatherGreetingItems.Add(new Models.WeatherGreetingItem { Keyword = "新天气", Text = "" });
            AutoSave();
            RefreshList();
        };
        btnRow.Children.Add(addBtn);

        var refreshAllWeatherBtn = new Button { Content = "🔄 刷新全部天气问候", Padding = new Thickness(12, 4), HorizontalAlignment = HorizontalAlignment.Left };
        refreshAllWeatherBtn.Click += (a, e) =>
        {
            _svc.RefreshAllWeatherGreetings();
            RefreshList();
        };
        btnRow.Children.Add(refreshAllWeatherBtn);
        panel.Children.Add(btnRow);

        panel.Children.Add(Info("说明：当天气文本包含对应关键词时，显示该文案。{weather} 会被替换为实际天气名称。"));

        return panel;
    }

    Control BuildAboutPanel()
    {
        var s = new StackPanel { Spacing = 0 };
        s.Children.Add(PageHeader("ℹ️ 关于"));

        var infoPanel = new StackPanel { Spacing = 8, Margin = new Thickness(16, 12, 16, 12) };
        infoPanel.Children.Add(new TextBlock
        {
            Text = "节假日倒计时",
            FontSize = 28,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.Parse("#FF2196F3"))
        });
        var versionBlock = new TextBlock { Text = "版本: v1.3.0.1", FontSize = 14, Opacity = 0.7 };
        BindThemeForeground(versionBlock);
        infoPanel.Children.Add(versionBlock);
        var authorBlock = new TextBlock { Text = "作者: fengjian868", FontSize = 14, Opacity = 0.7 };
        BindThemeForeground(authorBlock);
        infoPanel.Children.Add(authorBlock);
        var githubBlock = new TextBlock { Text = "GitHub: https://github.com/fengjian868/HolidayCountdown", FontSize = 12, Opacity = 0.5 };
        BindThemeForeground(githubBlock);
        infoPanel.Children.Add(githubBlock);
        var repoBtn = new Button
        {
            Content = "打开插件仓库",
            HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(10, 4),
            Margin = new Thickness(0, 6, 0, 0)
        };
        repoBtn.Click += (a, e) =>
        {
            try { Process.Start(new ProcessStartInfo("https://github.com/fengjian868/HolidayCountdown") { UseShellExecute = true }); }
            catch { }
        };
        infoPanel.Children.Add(repoBtn);
        s.Children.Add(Card(infoPanel));

        var changelogPanel = new StackPanel { Spacing = 6, Margin = new Thickness(16, 12, 16, 12) };
        var changelogTitle = new TextBlock { Text = "v1.3.0.1 更新日志", FontSize = 16, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 0, 0, 8) };
        BindThemeForeground(changelogTitle);
        changelogPanel.Children.Add(changelogTitle);
        var changelogItems = new[]
        {
            "— 新增功能 —",
            "1. 增加了大考倒计时，显示距离下次高考/中考的倒计时[实验中请手动开启]",
            "2. 增加了比ci更好的天气组件\"智能天气\"，天气图标有颜色内置了有我组件自带的天气提醒，城市是根据ci来读取的[实验中请手动开启]",
            "3. 增加了世界时钟，可查看每个城市的时间[实验中请手动开启]",
            "4. 增加了自动化行动\"打开U盘\"\"刷新天气\"\"刷新天气文案\"[实验中请手动开启]",
            "5. 增加了天气变化提醒组件，模仿windows小组件天气的变化提示例如\"附近有闪电\"\"几点后会下雨\"[实验中请手动开启]",
            "— 修复功能 —",
            "1. 修复了课表联动的部分变量不显示问题",
            "2. 修复了切换不同颜色主题不会根据主题变化颜色",
            "3. 修复了设置部分内容点击不了的问题",
            "— 优化 —",
            "1. 设置的部分变量显示",
            "2. 部分图标显示",
            "",
            "*新增功能会在后续版本逐渐修改优化并上线"
        };
        foreach (var item in changelogItems)
        {
            var itemBlock = new TextBlock { Text = item, FontSize = 12, Opacity = 0.8 };
            BindThemeForeground(itemBlock);
            changelogPanel.Children.Add(itemBlock);
        }
        s.Children.Add(Expander("更新日志", "v1.3.0.1 更新内容", changelogPanel, expanded: true));

        var featurePanel = new StackPanel { Spacing = 6, Margin = new Thickness(16, 12, 16, 12) };
        var featureItems = new[]
        {
            "- 节假日倒计时（调休提醒、进度环、放假天数）",
            "- 24节气倒计时（网络自动刷新）",
            "- 农历日期显示（自定义模板）",
            "- 自定义节日倒计时",
            "- 寒暑假倒计时（周+天）",
            "- 时段问候语（早中晚+放学+晚修）",
            "- 天气问候（根据温度提醒穿衣）",
            "- 课程表联动（当前课程/课间倒计时）",
            "- 学习时长统计（今日学习时长）"
        };
        foreach (var item in featureItems)
        {
            var itemBlock = new TextBlock { Text = item, FontSize = 12, Opacity = 0.8 };
            BindThemeForeground(itemBlock);
            featurePanel.Children.Add(itemBlock);
        }
        s.Children.Add(Expander("功能模块", "插件支持的所有功能", featurePanel, expanded: true));

        // 实验性功能开关
        var expPanel = new StackPanel { Spacing = 8, Margin = new Thickness(16, 12, 16, 12) };
        var expDesc = new TextBlock
        {
            Text = "实验性功能包含：智能天气、大考倒计时、世界时钟、天气变化提醒、自动化行动（打开U盘/刷新天气/刷新天气文案）。\n这些功能仍在开发中，可能不稳定。开启后需重启 ClassIsland 才能生效。",
            FontSize = 12,
            Opacity = 0.7
        };
        BindThemeForeground(expDesc);
        expPanel.Children.Add(expDesc);

        var expEnabled = _svc.Settings.ExperimentalFeaturesEnabled;
        var expToggle = new ToggleSwitch { IsChecked = expEnabled };
        expToggle.IsCheckedChanged += (a, b) =>
        {
            var enable = expToggle.IsChecked == true;
            _svc.Settings.ExperimentalFeaturesEnabled = enable;
            AutoSave();

            // 写入/删除标记文件
            var expFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ClassIsland", "Plugins", "HolidayCountdown", "experimental_enabled");
            try
            {
                if (enable)
                {
                    var dir = Path.GetDirectoryName(expFile);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
                    File.WriteAllText(expFile, "1");
                }
                else
                {
                    if (File.Exists(expFile)) File.Delete(expFile);
                }
            }
            catch { }

            // 提示需要重启
            try
            {
                var appHostType = Type.GetType("ClassIsland.Shared.IAppHost, ClassIsland.Shared")
                    ?? Type.GetType("ClassIsland.Shared.IAppHost, ClassIsland.Core")
                    ?? AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(asm => asm.GetTypes())
                        .FirstOrDefault(t => t.Name == "IAppHost");
                if (appHostType != null)
                {
                    var tryGetService = appHostType.GetMethod("TryGetService", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (tryGetService != null && tryGetService.IsGenericMethodDefinition)
                    {
                        var notifType = Type.GetType("ClassIsland.Core.Abstractions.Services.INotificationHostService, ClassIsland.Core")
                            ?? AppDomain.CurrentDomain.GetAssemblies()
                                .SelectMany(asm => asm.GetTypes())
                                .FirstOrDefault(t => t.Name == "INotificationHostService");
                        if (notifType != null)
                        {
                            var genericMethod = tryGetService.MakeGenericMethod(notifType);
                            var notifService = genericMethod.Invoke(null, null);
                            if (notifService != null)
                            {
                                var showMethod = notifService.GetType().GetMethod("ShowNotification", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                                if (showMethod != null)
                                {
                                    // 尝试调用重启提示
                                    showMethod.Invoke(notifService, new object[] { "实验性功能设置已更改", "请重启 ClassIsland 以使更改生效。", 5000 });
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        };
        expPanel.Children.Add(SettingItem("开启实验性功能", "需重启 ClassIsland 后生效", expToggle));

        s.Children.Add(Expander("实验性功能", "测试版功能，默认关闭", expPanel));

        var footerBlock = new TextBlock { Text = "Made with love for ClassIsland", FontSize = 12, Opacity = 0.5, Margin = new Thickness(0, 8, 0, 0) };
        BindThemeForeground(footerBlock);
        s.Children.Add(footerBlock);
        return s;
    }

    // ===== UI Helpers =====

    static TextBlock PageHeader(string text)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = 22,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(16, 16, 16, 12)
        };
        BindThemeForeground(tb);
        return tb;
    }

    static Border Card(Control content) => new Border
    {
        Background = new SolidColorBrush(Color.Parse("#15FFFFFF")),
        CornerRadius = new CornerRadius(8),
        Padding = new Thickness(16),
        Margin = new Thickness(16, 8, 16, 8),
        Child = content
    };

    static Expander Expander(string header, string? desc, Control content, bool expanded = false)
    {
        var headerPanel = new StackPanel { Spacing = 2 };
        var headerBlock = new TextBlock { Text = header, FontSize = 15, FontWeight = FontWeight.SemiBold };
        BindThemeForeground(headerBlock);
        headerPanel.Children.Add(headerBlock);
        if (!string.IsNullOrEmpty(desc))
        {
            var descBlock = new TextBlock { Text = desc, FontSize = 11, Opacity = 0.6 };
            BindThemeForeground(descBlock);
            headerPanel.Children.Add(descBlock);
        }

        var expander = new Expander
        {
            Header = headerPanel,
            Content = content,
            IsExpanded = expanded,
            Margin = new Thickness(16, 4, 16, 4),
            CornerRadius = new CornerRadius(8)
        };
        return expander;
    }

    static Grid SettingItem(string title, string? desc, Control control)
    {
        var g = new Grid { ColumnDefinitions = new ColumnDefinitions("* Auto"), Margin = new Thickness(16, 10, 16, 10) };
        var left = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        var titleBlock = new TextBlock { Text = title, FontSize = 14, VerticalAlignment = VerticalAlignment.Center };
        BindThemeForeground(titleBlock);
        left.Children.Add(titleBlock);
        if (!string.IsNullOrEmpty(desc))
        {
            var descBlock = new TextBlock { Text = desc, FontSize = 11, Opacity = 0.6, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
            BindThemeForeground(descBlock);
            left.Children.Add(descBlock);
        }
        Grid.SetColumn(left, 0);
        Grid.SetColumn(control, 1);
        control.VerticalAlignment = VerticalAlignment.Center;
        g.Children.Add(left);
        g.Children.Add(control);
        return g;
    }

    static Border Separator() => new Border
    {
        Height = 1,
        Background = new SolidColorBrush(Color.Parse("#15FFFFFF")),
        Margin = new Thickness(16, 0, 16, 0)
    };

    void AutoSave()
    {
        // 立即保存，确保组件能立即获取最新设置
        _saveTimer.Stop();
        _svc.SaveSettings();
    }

    static ToggleSwitch Toggle(bool value, Action<bool> onChanged)
    {
        var t = new ToggleSwitch { IsChecked = value };
        t.IsCheckedChanged += (s, e) => onChanged(t.IsChecked == true);
        return t;
    }

    static NumericUpDown Number(int value, int min, int max, Action<int> onChanged)
    {
        var n = new NumericUpDown
        {
            Value = value,
            Minimum = min,
            Maximum = max,
            Increment = 1,
            Width = 100,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        n.ValueChanged += (s, e) =>
        {
            var v = (int)Math.Max(min, Math.Min(max, n.Value ?? value));
            onChanged(v);
        };
        return n;
    }

    static TextBox Text(string value, int width, Action<string> onChanged)
    {
        var t = new TextBox { Text = value, Width = width };
        t.TextChanged += (s, e) => onChanged(t.Text ?? "");
        return t;
    }

    static ComboBox Combo(string[] items, int selected, Action<int> onChanged)
    {
        var c = new ComboBox { Width = 80 };
        foreach (var item in items) c.Items.Add(item);
        c.SelectedIndex = selected;
        c.SelectionChanged += (s, e) => onChanged(c.SelectedIndex);
        return c;
    }

    static DatePicker Date(DateTime value, Action<DateTime> onChanged)
    {
        var d = new DatePicker { SelectedDate = value };
        d.SelectedDateChanged += (s, e) => { if (d.SelectedDate.HasValue) onChanged(d.SelectedDate.Value.DateTime); };
        return d;
    }

    static Control SearchableCityPicker(IReadOnlyList<string> allItems, string selectedItem, string watermark, int width, Action<string> onSelected)
    {
        var filtered = new ObservableCollection<string>(allItems);
        var textBox = new TextBox { Text = selectedItem, Width = width, Watermark = watermark };
        var listBox = new ListBox
        {
            Width = width,
            MaxHeight = 150,
            ItemsSource = filtered,
            IsVisible = false
        };
        var suppressShow = false;

        void Filter()
        {
            var search = textBox.Text ?? "";
            filtered.Clear();
            foreach (var city in allItems.Where(c => c.Contains(search, StringComparison.OrdinalIgnoreCase)))
                filtered.Add(city);
        }

        textBox.GotFocus += (s, e) =>
        {
            if (suppressShow) return;
            Filter();
            listBox.IsVisible = filtered.Count > 0;
        };
        textBox.TextChanged += (s, e) =>
        {
            if (suppressShow) return;
            Filter();
            listBox.IsVisible = filtered.Count > 0;
        };
        textBox.LostFocus += (s, e) =>
        {
            // 稍等一帧再隐藏，避免点击列表项时列表提前消失导致选不中
            Dispatcher.UIThread.Post(() =>
            {
                if (!suppressShow) listBox.IsVisible = false;
            }, DispatcherPriority.Background);
        };

        listBox.LostFocus += (s, e) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!suppressShow) listBox.IsVisible = false;
            }, DispatcherPriority.Background);
        };

        listBox.SelectionChanged += (s, e) =>
        {
            if (listBox.SelectedItem is string city)
            {
                suppressShow = true;
                textBox.Text = city;
                onSelected(city);
                listBox.IsVisible = false;
                listBox.SelectedItem = null;
                Dispatcher.UIThread.Post(() => suppressShow = false, DispatcherPriority.Background);
            }
        };

        return new StackPanel { Spacing = 4, HorizontalAlignment = HorizontalAlignment.Right, Children = { textBox, listBox } };
    }

    static ColorPicker ColorPicker(string color, Action<string> onChanged)
    {
        var c = new ColorPicker { Color = Color.Parse(color), Width = 40, Height = 24 };
        c.ColorChanged += (s, e) => onChanged(c.Color.ToString());
        return c;
    }

    static TextBlock Info(string text)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = 11,
            Opacity = 0.6,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Margin = new Thickness(16, 4, 16, 8)
        };
        BindThemeForeground(tb);
        return tb;
    }
}

public static class PanelExt
{
    public static T Also<T>(this T t, Action<T> a) { a(t); return t; }
}
