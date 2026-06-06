using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Models;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.SettingsPages;

[SettingsPageInfo("holidaycountdown.greeting", "问候语设置", "\uE9D2", "\uE9D2")]
public class GreetingSettingsPage : SettingsPageBase
{
    private readonly HolidayService _svc;
    public GreetingSettingsPage() { _svc = new HolidayService(); Content = Build(); }

    Control Build()
    {
        var s = new StackPanel { Spacing = 14, Margin = new Thickness(24, 16) };
        s.Children.Add(Header("💬 问候语设置"));
        s.Children.Add(Expander("开关", new StackPanel { Spacing = 10 }.Also(p =>
        {
            p.Children.Add(Row("启用问候语", "", Toggle(_svc.Settings.ShowGreeting, v => _svc.Settings.ShowGreeting = v)));
            p.Children.Add(Row("合并到节假日组件", "问候语显示在节假日下方", Toggle(_svc.Settings.MergeGreeting, v => _svc.Settings.MergeGreeting = v)));
            p.Children.Add(Row("联网刷新问候语", "每5分钟从网络获取新文案", Toggle(_svc.Settings.GreetingOnline, v => _svc.Settings.GreetingOnline = v)));
            p.Children.Add(Row("周日晚修提醒", "周日17-21点显示晚修提示", Toggle(_svc.Settings.ShowSundayEveningStudy, v => _svc.Settings.ShowSundayEveningStudy = v)));
        })));
        s.Children.Add(Expander("放学", new StackPanel { Spacing = 10 }.Also(p =>
        {
            p.Children.Add(Row("放学时间", "时:分", new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 }.Also(h => { h.Children.Add(Tx(_svc.Settings.SchoolEndHour.ToString("D2"), 40, v => _svc.Settings.SchoolEndHour = Math.Max(0, Math.Min(23, int.Parse(v))))); h.Children.Add(new TextBlock { Text = ":", VerticalAlignment = VerticalAlignment.Center }); h.Children.Add(Tx(_svc.Settings.SchoolEndMinute.ToString("D2"), 40, v => _svc.Settings.SchoolEndMinute = Math.Max(0, Math.Min(59, int.Parse(v))))); })));
            p.Children.Add(Row("提前提醒分钟", "放学前多少分钟切换提醒", Num(_svc.Settings.SchoolEndReminderMinutes, 1, 60, v => _svc.Settings.SchoolEndReminderMinutes = v)));
            p.Children.Add(Row("放学前文案", "", Tx(_svc.Settings.BeforeSchoolEndText, 200, v => _svc.Settings.BeforeSchoolEndText = v)));
            p.Children.Add(Row("放学后文案", "", Tx(_svc.Settings.AfterSchoolEndText, 200, v => _svc.Settings.AfterSchoolEndText = v)));
            p.Children.Add(Row("晚修文案", "", Tx(_svc.Settings.SundayEveningStudyText, 200, v => _svc.Settings.SundayEveningStudyText = v)));
        })));
        s.Children.Add(Expander("时段文案", BuildTimeSlotPanel()));
        s.Children.Add(Expander("特殊日期", BuildSpecialDatePanel()));
        s.Children.Add(SaveBtn());
        return new ScrollViewer { Content = s };
    }

    StackPanel BuildTimeSlotPanel()
    {
        var panel = new StackPanel { Spacing = 8 };
        var listPanel = new StackPanel { Spacing = 6 };

        void RefreshList()
        {
            listPanel.Children.Clear();
            foreach (var slot in _svc.Settings.TimeSlotGreetings.OrderBy(x => x.StartHour * 60 + x.StartMinute))
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
                var startBox = Tx($"{slot.StartHour:D2}:{slot.StartMinute:D2}", 50, v =>
                {
                    if (TimeSpan.TryParse(v, out var ts)) { slot.StartHour = ts.Hours; slot.StartMinute = ts.Minutes; }
                });
                var endBox = Tx($"{slot.EndHour:D2}:{slot.EndMinute:D2}", 50, v =>
                {
                    if (TimeSpan.TryParse(v, out var ts)) { slot.EndHour = ts.Hours; slot.EndMinute = ts.Minutes; }
                });
                var textBox = Tx(slot.Text, 200, v => slot.Text = v);
                var delBtn = new Button { Content = "🗑️", Padding = new Thickness(4, 2) };
                delBtn.Click += (a, e) => { _svc.Settings.TimeSlotGreetings.Remove(slot); RefreshList(); };

                row.Children.Add(new TextBlock { Text = "从", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                row.Children.Add(startBox);
                row.Children.Add(new TextBlock { Text = "到", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                row.Children.Add(endBox);
                row.Children.Add(textBox);
                row.Children.Add(delBtn);
                listPanel.Children.Add(row);
            }
        }

        RefreshList();
        panel.Children.Add(listPanel);

        var addBtn = new Button { Content = "+ 添加时段", Padding = new Thickness(12, 4), HorizontalAlignment = HorizontalAlignment.Left };
        addBtn.Click += (a, e) =>
        {
            _svc.Settings.TimeSlotGreetings.Add(new TimeSlotGreeting { StartHour = 8, StartMinute = 0, EndHour = 12, EndMinute = 0, Text = "" });
            RefreshList();
        };
        panel.Children.Add(addBtn);

        return panel;
    }

    StackPanel BuildSpecialDatePanel()
    {
        var panel = new StackPanel { Spacing = 8 };
        var listPanel = new StackPanel { Spacing = 6 };

        void RefreshList()
        {
            listPanel.Children.Clear();
            foreach (var item in _svc.Settings.SpecialDateGreetings)
            {
                var row = new StackPanel { Spacing = 4 };
                
                // 第一行：名称 + 星期 + 启用开关 + 删除
                var headerRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
                var nameBox = Tx(item.Name, 80, v => item.Name = v);
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
                
                // 第二行：时段
                var timeRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
                var startBox = Tx($"{item.StartHour:D2}:{item.StartMinute:D2}", 50, v =>
                {
                    if (TimeSpan.TryParse(v, out var ts)) { item.StartHour = ts.Hours; item.StartMinute = ts.Minutes; }
                });
                var endBox = Tx($"{item.EndHour:D2}:{item.EndMinute:D2}", 50, v =>
                {
                    if (TimeSpan.TryParse(v, out var ts)) { item.EndHour = ts.Hours; item.EndMinute = ts.Minutes; }
                });
                var textBox = Tx(item.Text, 200, v => item.Text = v);
                
                timeRow.Children.Add(new TextBlock { Text = "从", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                timeRow.Children.Add(startBox);
                timeRow.Children.Add(new TextBlock { Text = "到", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                timeRow.Children.Add(endBox);
                timeRow.Children.Add(textBox);
                row.Children.Add(timeRow);
                
                listPanel.Children.Add(row);
            }
        }

        RefreshList();
        panel.Children.Add(listPanel);

        var addBtn = new Button { Content = "+ 添加特殊日期", Padding = new Thickness(12, 4), HorizontalAlignment = HorizontalAlignment.Left };
        addBtn.Click += (a, e) =>
        {
            _svc.Settings.SpecialDateGreetings.Add(new SpecialDateGreeting { Name = "新日期", DayOfWeek = 1, StartHour = 0, StartMinute = 0, EndHour = 23, EndMinute = 59, Text = "" });
            RefreshList();
        };
        panel.Children.Add(addBtn);

        return panel;
    }

    static TextBlock Header(string t) => new() { Text = t, FontSize = 22, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 0, 0, 8) };
    static Border Expander(string title, Control content)
    {
        var header = new Button { Content = $"▶ {title}", HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(12, 8) };
        var panel = new StackPanel { Spacing = 10, IsVisible = false };
        panel.Children.Add(content);
        var border = new Border { Background = new SolidColorBrush(Color.Parse("#0DFFFFFF")), CornerRadius = new CornerRadius(12), Padding = new Thickness(16), BorderBrush = new SolidColorBrush(Color.Parse("#1AFFFFFF")), BorderThickness = new Thickness(1), Margin = new Thickness(0, 4) };
        var container = new StackPanel { Spacing = 4 };
        container.Children.Add(header);
        container.Children.Add(panel);
        border.Child = container;
        header.Click += (a, e) =>
        {
            panel.IsVisible = !panel.IsVisible;
            header.Content = panel.IsVisible ? $"▼ {title}" : $"▶ {title}";
        };
        return border;
    }
    static Control Row(string l, string d, Control c) { var g = new Grid { ColumnDefinitions = new ColumnDefinitions("120 *") }; var left = new StackPanel { VerticalAlignment = VerticalAlignment.Center }; left.Children.Add(new TextBlock { Text = l, FontWeight = FontWeight.SemiBold }); if (!string.IsNullOrEmpty(d)) left.Children.Add(new TextBlock { Text = d, Opacity = 0.5, FontSize = 11 }); Grid.SetColumn(left, 0); Grid.SetColumn(c, 1); c.VerticalAlignment = VerticalAlignment.Center; g.Children.Add(left); g.Children.Add(c); return g; }
    static ToggleSwitch Toggle(bool v, Action<bool> cb) { var t = new ToggleSwitch { IsChecked = v, OnContent = "开", OffContent = "关" }; t.IsCheckedChanged += (a, b) => { cb(t.IsChecked == true); }; return t; }
    static TextBox Num(int v, int min, int max, Action<int> cb) { var t = new TextBox { Text = v.ToString(), Width = 60 }; t.LostFocus += (a, b) => { if (int.TryParse(t.Text, out var n)) { n = Math.Max(min, Math.Min(max, n)); t.Text = n.ToString(); cb(n); } }; return t; }
    static TextBox Tx(string v, int w, Action<string> cb) { var t = new TextBox { Text = v, Width = w }; t.TextChanged += (a, b) => cb(t.Text ?? ""); return t; }
    Button SaveBtn() { var b = new Button { Content = "💾 保存", Padding = new Thickness(20, 8) }; b.Click += (a, e) => { _svc.SaveSettings(); b.Content = "✅ 已保存"; }; return b; }
}
