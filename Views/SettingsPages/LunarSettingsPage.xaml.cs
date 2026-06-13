using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.SettingsPages;

[SettingsPageInfo("holidaycountdown.lunar", "农历设置", "\uE787", "\uE787")]
public class LunarSettingsPage : SettingsPageBase
{
    private readonly HolidayService _svc;
    public LunarSettingsPage() { _svc = new HolidayService(); Content = Build(); }

    Control Build()
    {
        var s = new StackPanel { Spacing = 14, Margin = new Thickness(24, 16) };
        s.Children.Add(Header("🌙 农历日期设置"));
        s.Children.Add(Expander("显示", new StackPanel { Spacing = 10 }.Also(p =>
        {
            p.Children.Add(Row("显示农历", "", Toggle(_svc.Settings.ShowLunarDate, v => _svc.Settings.ShowLunarDate = v)));
            p.Children.Add(Row("自动网络刷新", "有网络时自动获取最新农历", Toggle(_svc.Settings.LunarAutoRefresh, v => _svc.Settings.LunarAutoRefresh = v)));
        })));
        s.Children.Add(Expander("显示格式", new StackPanel { Spacing = 10 }.Also(p =>
        {
            // 提供几个预设格式，用户可以直接选择
            var presets = new[]
            {
                ("完整", "{gzYear} {IMonthCn}{IDayCn} {Animal}"),
                ("简洁", "{IMonthCn}{IDayCn}"),
                ("含生肖", "{IMonthCn}{IDayCn} {Animal}"),
                ("含节气", "{IMonthCn}{IDayCn} {Term}"),
            };
            var presetCombo = new ComboBox { Width = 120 };
            foreach (var (name, _) in presets) presetCombo.Items.Add(name);
            
            // 根据当前模板设置选中项
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
            
            p.Children.Add(new TextBlock { Text = "选择格式:", FontWeight = FontWeight.SemiBold });
            p.Children.Add(presetCombo);
            
            // 自定义模板输入
            p.Children.Add(new TextBlock { Text = "或自定义:", FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 8, 0, 0) });
            var templateBox = new TextBox { Text = _svc.Settings.LunarDateTemplate, Width = 300 };
            templateBox.TextChanged += (a, b) => _svc.Settings.LunarDateTemplate = templateBox.Text ?? "";
            p.Children.Add(templateBox);
            
            p.Children.Add(new TextBlock { Text = "可用变量: {gzYear} 干支年 | {IMonthCn} 农历月 | {IDayCn} 农历日 | {Animal} 生肖 | {Term} 节气", Opacity = 0.5, FontSize = 11, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        })));
        s.Children.Add(new TextBlock { Text = "示例: 癸卯年 九月初八 兔", Opacity = 0.5, FontSize = 11 });
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
    static TextBox Tx(string v, int w, Action<string> cb) { var t = new TextBox { Text = v, Width = w }; t.TextChanged += (a, b) => cb(t.Text ?? ""); return t; }
    Button SaveBtn() { var b = new Button { Content = "💾 保存", Padding = new Thickness(20, 8) }; b.Click += (a, e) => { _svc.SaveSettings(); b.Content = "✅ 已保存"; }; return b; }
}
