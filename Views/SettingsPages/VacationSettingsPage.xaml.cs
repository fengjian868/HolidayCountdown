using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.SettingsPages;

[SettingsPageInfo("holidaycountdown.vacation", "寒暑假设置", "\uE7BE", "\uE7BE")]
public class VacationSettingsPage : SettingsPageBase
{
    private readonly HolidayService _svc;
    public VacationSettingsPage() { _svc = new HolidayService(); Content = Build(); }

    Control Build()
    {
        var s = new StackPanel { Spacing = 14, Margin = new Thickness(24, 16) };
        s.Children.Add(Header("🏖️ 寒暑假设置"));
        s.Children.Add(Expander("开关", new StackPanel { Spacing = 10 }.Also(p =>
        {
            p.Children.Add(Row("显示寒暑假倒计时", "", Toggle(_svc.Settings.ShowVacationCountdown, v => _svc.Settings.ShowVacationCountdown = v)));
        })));
        s.Children.Add(Expander("暑假", new StackPanel { Spacing = 10 }.Also(p =>
        {
            p.Children.Add(Row("开始日期", "", Dp(_svc.Settings.SummerStart, v => _svc.Settings.SummerStart = v)));
            p.Children.Add(Row("结束日期", "", Dp(_svc.Settings.SummerEnd, v => _svc.Settings.SummerEnd = v)));
        })));
        s.Children.Add(Expander("寒假", new StackPanel { Spacing = 10 }.Also(p =>
        {
            p.Children.Add(Row("开始日期", "", Dp(_svc.Settings.WinterStart, v => _svc.Settings.WinterStart = v)));
            p.Children.Add(Row("结束日期", "", Dp(_svc.Settings.WinterEnd, v => _svc.Settings.WinterEnd = v)));
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
    static Control Row(string l, string d, Control c) { var g = new Grid { ColumnDefinitions = new ColumnDefinitions("120 *") }; var left = new StackPanel { VerticalAlignment = VerticalAlignment.Center }; left.Children.Add(new TextBlock { Text = l, FontWeight = FontWeight.SemiBold }); if (!string.IsNullOrEmpty(d)) left.Children.Add(new TextBlock { Text = d, Opacity = 0.5, FontSize = 11 }); Grid.SetColumn(left, 0); Grid.SetColumn(c, 1); c.VerticalAlignment = VerticalAlignment.Center; g.Children.Add(left); g.Children.Add(c); return g; }
    static ToggleSwitch Toggle(bool v, Action<bool> cb) { var t = new ToggleSwitch { IsChecked = v, OnContent = "开", OffContent = "关" }; t.IsCheckedChanged += (a, b) => cb(t.IsChecked == true); return t; }
    static DatePicker Dp(DateTime v, Action<DateTime> cb) { var d = new DatePicker { SelectedDate = v }; d.SelectedDateChanged += (a, b) => { if (d.SelectedDate.HasValue) cb(d.SelectedDate.Value.DateTime); }; return d; }
    Button SaveBtn() { var b = new Button { Content = "💾 保存", Padding = new Thickness(20, 8) }; b.Click += (a, e) => { _svc.SaveSettings(); b.Content = "✅ 已保存"; }; return b; }
}
