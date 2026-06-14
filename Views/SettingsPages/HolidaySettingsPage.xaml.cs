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

[SettingsPageInfo("holidaycountdown.holiday", "节假日设置", "\uE8F5", "\uE8F5")]
public class HolidaySettingsPage : SettingsPageBase
{
    private readonly HolidayService _svc;
    public HolidaySettingsPage() { _svc = new HolidayService(); Content = Build(); }

    Control Build()
    {
        var s = new StackPanel { Spacing = 14, Margin = new Thickness(24, 16) };
        s.Children.Add(Header("📅 节假日倒计时设置"));
        s.Children.Add(Expander("显示", new StackPanel { Spacing = 10 }.Also(p =>
        {
            p.Children.Add(Row("显示数量", "同时显示多少个节日", Combo(new[]{"1","3","5"}, _svc.Settings.DisplayCount == 1 ? 0 : _svc.Settings.DisplayCount == 3 ? 1 : 2, v => _svc.Settings.DisplayCount = v == 0 ? 1 : v == 1 ? 3 : 5)));
            p.Children.Add(Row("显示放假天数", "如：春节（放7天）", Toggle(_svc.Settings.ShowDaysOff, v => _svc.Settings.ShowDaysOff = v)));
            p.Children.Add(Row("显示小时数", "节日当天显示剩余小时", Toggle(_svc.Settings.ShowHours, v => _svc.Settings.ShowHours = v)));
            p.Children.Add(Row("显示进度环", "首个节日显示弧形进度", Toggle(_svc.Settings.ShowProgressRing, v => _svc.Settings.ShowProgressRing = v)));
            p.Children.Add(Row("自动播放下一个", "节日过后自动显示下一个", Toggle(_svc.Settings.AutoNextHoliday, v => _svc.Settings.AutoNextHoliday = v)));
            p.Children.Add(Row("显示假期占比", "当年剩余假期百分比", Toggle(_svc.Settings.ShowYearRatio, v => _svc.Settings.ShowYearRatio = v)));
            p.Children.Add(Row("周末倒计时", "列表中显示周六周日", Toggle(_svc.Settings.ShowWeekendCountdown, v => _svc.Settings.ShowWeekendCountdown = v)));
        })));
        s.Children.Add(Expander("调休", new StackPanel { Spacing = 10 }.Also(p =>
        {
            p.Children.Add(Row("调休提醒", "周末调休上课提前提醒", Toggle(_svc.Settings.ShowWorkdayReminder, v => _svc.Settings.ShowWorkdayReminder = v)));
            p.Children.Add(Row("提前提醒天数", "调休提醒提前多少天显示", Num(_svc.Settings.WorkdayReminderDays, 1, 30, v => _svc.Settings.WorkdayReminderDays = v)));
        })));
        s.Children.Add(Expander("颜色", new StackPanel { Spacing = 10 }.Also(p =>
        {
            p.Children.Add(Row("自动节日颜色", "根据节日自动匹配颜色", Toggle(_svc.Settings.AutoHolidayColor, v => _svc.Settings.AutoHolidayColor = v)));
            p.Children.Add(new TextBlock { Text = "自定义颜色", FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Color.Parse("#2196F3")) });
            foreach (var kv in _svc.Settings.HolidayColors.ToList())
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                row.Children.Add(new TextBlock { Text = kv.Key, Width = 80, VerticalAlignment = VerticalAlignment.Center });
                var picker = new ColorPicker 
                { 
                    Width = 40, 
                    Height = 28,
                    Color = TryParseColor(kv.Value)
                };
                var key = kv.Key;
                picker.ColorChanged += (a, b) => { _svc.Settings.HolidayColors[key] = picker.Color.ToString(); _svc.SaveSettings(); };
                row.Children.Add(picker);
                p.Children.Add(row);
            }
        })));
        s.Children.Add(Expander("节日开关", new StackPanel { Spacing = 10 }.Also(p =>
        {
            var all = new[]{"元旦","春节","清明节","劳动节","端午节","中秋节","国庆节"};
            foreach (var name in all)
            {
                var disabled = _svc.Settings.DisabledHolidays.Contains(name);
                var chk = new CheckBox { Content = name, IsChecked = !disabled };
                chk.IsCheckedChanged += (a, b) => { if (chk.IsChecked == true) _svc.Settings.DisabledHolidays.Remove(name); else if (!_svc.Settings.DisabledHolidays.Contains(name)) _svc.Settings.DisabledHolidays.Add(name); _svc.SaveSettings(); };
                p.Children.Add(chk);
            }
        })));
        s.Children.Add(SaveBtn());
        return new ScrollViewer { Content = s };
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
    static Control Row(string label, string desc, Control ctrl)
    {
        var g = new Grid { ColumnDefinitions = new ColumnDefinitions("* Auto") };
        var left = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        left.Children.Add(new TextBlock { Text = label, FontWeight = FontWeight.SemiBold });
        left.Children.Add(new TextBlock { Text = desc, Opacity = 0.5, FontSize = 11 });
        Grid.SetColumn(left, 0); Grid.SetColumn(ctrl, 1); ctrl.VerticalAlignment = VerticalAlignment.Center;
        g.Children.Add(left); g.Children.Add(ctrl); return g;
    }
    static ToggleSwitch Toggle(bool v, Action<bool> cb) { var t = new ToggleSwitch { IsChecked = v, OnContent = "开", OffContent = "关" }; t.IsCheckedChanged += (a, b) => { cb(t.IsChecked == true); }; return t; }
    static ComboBox Combo(string[] items, int sel, Action<int> cb) { var c = new ComboBox { Width = 80, SelectedIndex = sel }; foreach (var i in items) c.Items.Add(i); c.SelectionChanged += (a, b) => cb(c.SelectedIndex); return c; }
    static TextBox Num(int v, int min, int max, Action<int> cb) { var t = new TextBox { Text = v.ToString(), Width = 60 }; t.LostFocus += (a, b) => { if (int.TryParse(t.Text, out var n)) { n = Math.Max(min, Math.Min(max, n)); t.Text = n.ToString(); cb(n); } }; return t; }
    static Avalonia.Media.Color TryParseColor(string hex)
    {
        try { return Avalonia.Media.Color.Parse(hex); }
        catch { return Avalonia.Media.Color.Parse("#2196F3"); }
    }
    Button SaveBtn() { var b = new Button { Content = "💾 保存", Padding = new Thickness(20, 8) }; b.Click += (a, e) => { _svc.SaveSettings(); b.Content = "✅ 已保存"; }; return b; }
}

public static class PanelExt { public static T Also<T>(this T t, Action<T> a) { a(t); return t; } }
