using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.VisualTree;
using HolidayCountdown.Views.SettingsPages;

namespace HolidayCountdown.Views.Components;

// 组件设置弹窗辅助：在组件 UI 上提供「⚙ 设置」入口，
// 点击后弹出该组件对应的独立设置面板（复用 UnifiedSettingsPage 的 standalone 模式）。
// 设置仍读写全局 PluginSettings，不会丢失用户现有配置。
internal static class ComponentSettingsOpener
{
    // 创建一个紧凑的 ⚙ 文本入口，添加到组件的横向布局中
    public static TextBlock CreateSettingsEntry(string key, string title)
    {
        var tb = new TextBlock
        {
            Text = "\u2699", // ⚙
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center,
            Opacity = 0.5,
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        tb[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
        tb.PointerReleased += (s, e) => Open(tb, key, title);
        return tb;
    }

    // 弹出独立窗口显示指定组件的设置面板
    public static void Open(Visual anchor, string key, string title)
    {
        var page = new UnifiedSettingsPage(standalone: true);
        var content = page.GetStandalonePanel(key);
        var win = new Window
        {
            Title = title,
            Width = 500,
            Height = 620,
            Content = content,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = true
        };
        var owner = anchor.VisualRoot as Window;
        if (owner != null) win.ShowDialog(owner);
        else win.Show();
    }
}
