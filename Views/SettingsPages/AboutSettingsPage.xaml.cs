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
        var s = new StackPanel { Spacing = 0, Margin = new Thickness(20, 16) };
        s.Children.Add(SettingsUI.PageHeader("ℹ️ 关于"));

        // 插件信息
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

        // 功能模块
        var featurePanel = new StackPanel { Spacing = 6, Margin = new Thickness(16, 12, 16, 12) };
        featurePanel.Children.Add(new TextBlock { Text = "- 节假日倒计时（调休提醒、进度环、放假天数）", FontSize = 12, Opacity = 0.8 });
        featurePanel.Children.Add(new TextBlock { Text = "- 24节气倒计时（网络自动刷新）", FontSize = 12, Opacity = 0.8 });
        featurePanel.Children.Add(new TextBlock { Text = "- 农历日期显示（自定义模板）", FontSize = 12, Opacity = 0.8 });
        featurePanel.Children.Add(new TextBlock { Text = "- 自定义节日倒计时", FontSize = 12, Opacity = 0.8 });
        featurePanel.Children.Add(new TextBlock { Text = "- 寒暑假倒计时（周+天）", FontSize = 12, Opacity = 0.8 });
        featurePanel.Children.Add(new TextBlock { Text = "- 时段问候语（早中晚+放学+晚修）", FontSize = 12, Opacity = 0.8 });
        featurePanel.Children.Add(new TextBlock { Text = "- 天气问候（根据温度提醒穿衣）", FontSize = 12, Opacity = 0.8 });
        s.Children.Add(SettingsUI.Expander("功能模块", "插件支持的所有功能", featurePanel, expanded: true));

        s.Children.Add(new TextBlock { Text = "Made with love for ClassIsland", FontSize = 12, Opacity = 0.5, Margin = new Thickness(0, 8, 0, 0) });
        return new ScrollViewer { Content = s };
    }
}
