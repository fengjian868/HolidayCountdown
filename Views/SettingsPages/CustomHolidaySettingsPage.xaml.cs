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

[SettingsPageInfo("holidaycountdown.customholiday", "自定义节日", "\uE915", "\uE915")]
public class CustomHolidaySettingsPage : SettingsPageBase
{
    private readonly HolidayService _svc;
    public CustomHolidaySettingsPage() { _svc = new HolidayService(); Content = Build(); }

    Control Build()
    {
        var s = new StackPanel { Spacing = 14, Margin = new Thickness(24, 16) };
        s.Children.Add(Header("🎂 自定义节日设置"));
        s.Children.Add(Expander("组件显示", new StackPanel { Spacing = 10 }.Also(p =>
        {
            p.Children.Add(Row("显示数量", "", Combo(new[]{"1","2","3","5"}, _svc.Settings.CustomHolidayDisplayCount == 1 ? 0 : _svc.Settings.CustomHolidayDisplayCount == 2 ? 1 : _svc.Settings.CustomHolidayDisplayCount == 3 ? 2 : 3, v => _svc.Settings.CustomHolidayDisplayCount = v == 0 ? 1 : v == 1 ? 2 : v == 2 ? 3 : 5)));
            p.Children.Add(Row("显示图标", "", Toggle(_svc.Settings.CustomHolidayShowIcon, v => _svc.Settings.CustomHolidayShowIcon = v)));
            p.Children.Add(Row("显示天数", "", Toggle(_svc.Settings.CustomHolidayShowDays, v => _svc.Settings.CustomHolidayShowDays = v)));
        })));
        s.Children.Add(Expander("节日列表", BuildList()));
        s.Children.Add(SaveBtn());
        return new ScrollViewer { Content = s };
    }

    Control BuildList()
    {
        var p = new StackPanel { Spacing = 8 };
        foreach (var h in _svc.Settings.CustomHolidays.ToList())
            p.Children.Add(MakeItem(h, p));
        var btn = new Button { Content = "➕ 添加", Padding = new Thickness(12, 6) };
        btn.Click += (a, e) => { var h = new CustomHoliday { Name = "新节日", Date = DateTime.Now.AddDays(1) }; _svc.Settings.CustomHolidays.Add(h); p.Children.Insert(p.Children.Count - 1, MakeItem(h, p)); };
        p.Children.Add(btn);
        return p;
    }

    Control MakeItem(CustomHoliday h, StackPanel parent)
    {
        var g = new Grid { ColumnDefinitions = new ColumnDefinitions("120 100 80 Auto Auto") };
        var n = new TextBox { Text = h.Name, Margin = new Thickness(0, 0, 8, 0) }; n.TextChanged += (a, b) => h.Name = n.Text ?? ""; Grid.SetColumn(n, 0);
        // 显示月-日
        var dateText = new TextBlock { Text = $"{h.Date.Month}月{h.Date.Day}日", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(dateText, 1);
        var d = new DatePicker { SelectedDate = h.Date, Margin = new Thickness(0, 0, 8, 0) };
        d.SelectedDateChanged += (a, b) => { if (d.SelectedDate.HasValue) { h.Date = d.SelectedDate.Value.DateTime; dateText.Text = $"{h.Date.Month}月{h.Date.Day}日"; } };
        Grid.SetColumn(d, 2);
        var r = new CheckBox { Content = "每年", IsChecked = h.RepeatYearly, VerticalAlignment = VerticalAlignment.Center }; r.IsCheckedChanged += (a, b) => h.RepeatYearly = r.IsChecked == true; Grid.SetColumn(r, 3);
        var del = new Button { Content = "删除", Width = 50 }; del.Click += (a, b) => { _svc.Settings.CustomHolidays.Remove(h); parent.Children.Remove(g); }; Grid.SetColumn(del, 4);
        g.Children.Add(n); g.Children.Add(dateText); g.Children.Add(d); g.Children.Add(r); g.Children.Add(del);
        return g;
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
    static ToggleSwitch Toggle(bool v, Action<bool> cb) { var t = new ToggleSwitch { IsChecked = v, OnContent = "开", OffContent = "关" }; t.IsCheckedChanged += (a, b) => cb(t.IsChecked == true); return t; }
    static ComboBox Combo(string[] items, int sel, Action<int> cb) { var c = new ComboBox { Width = 80, SelectedIndex = sel }; foreach (var i in items) c.Items.Add(i); c.SelectionChanged += (a, b) => cb(c.SelectedIndex); return c; }
    Button SaveBtn() { var b = new Button { Content = "💾 保存", Padding = new Thickness(20, 8) }; b.Click += (a, e) => { _svc.SaveSettings(); b.Content = "✅ 已保存"; }; return b; }
}
