using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Abstractions.Controls;

namespace HolidayCountdown.Views.SettingsPages;

[SettingsPageInfo("holidaycountdown.about", "关于", "\uE946", "\uE946")]
public class AboutSettingsPage : SettingsPageBase
{
    public AboutSettingsPage() { Content = Build(); }

    Control Build()
    {
        var s = new StackPanel { Spacing = 14, Margin = new Thickness(24, 16) };
        s.Children.Add(Header("ℹ️ 关于"));
        s.Children.Add(Expander("插件信息", new StackPanel { Spacing = 10 }.Also(p =>
        {
            p.Children.Add(new TextBlock { Text = "节假日倒计时", FontSize = 28, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#2196F3")) });
            p.Children.Add(new TextBlock { Text = "版本: v1.2.0.2 (正式版)", FontSize = 14, Opacity = 0.7 });
            p.Children.Add(new TextBlock { Text = "作者: fengjian868", FontSize = 14, Opacity = 0.7 });
            p.Children.Add(new TextBlock { Text = "GitHub: https://github.com/fengjian868/HolidayCountdown", FontSize = 12, Opacity = 0.5 });
        })));
        s.Children.Add(Expander("功能模块", new StackPanel { Spacing = 6 }.Also(p =>
        {
            p.Children.Add(new TextBlock { Text = "- 节假日倒计时（调休提醒、进度环、放假天数）", FontSize = 12, Opacity = 0.8 });
            p.Children.Add(new TextBlock { Text = "- 24节气倒计时（网络自动刷新）", FontSize = 12, Opacity = 0.8 });
            p.Children.Add(new TextBlock { Text = "- 农历日期显示（自定义模板）", FontSize = 12, Opacity = 0.8 });
            p.Children.Add(new TextBlock { Text = "- 自定义节日倒计时", FontSize = 12, Opacity = 0.8 });
            p.Children.Add(new TextBlock { Text = "- 寒暑假倒计时（周+天）", FontSize = 12, Opacity = 0.8 });
            p.Children.Add(new TextBlock { Text = "- 时段问候语（早中晚+放学+晚修）", FontSize = 12, Opacity = 0.8 });
            p.Children.Add(new TextBlock { Text = "- 天气问候（根据温度提醒穿衣）", FontSize = 12, Opacity = 0.8 });
        })));
        s.Children.Add(new TextBlock { Text = "Made with love for ClassIsland", FontSize = 12, Opacity = 0.5, Margin = new Thickness(0, 8, 0, 0) });
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
}
