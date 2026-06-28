using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;

namespace HolidayCountdown.Views.ComponentSettings;

/// <summary>
/// 组件设置页的统一行样式：左侧标题+描述，右侧控制项，带浅色卡片背景。
/// </summary>
public static class ComponentSettingRow
{
    public static Control Create(string title, string? desc, Control control, double maxDescWidth = 260)
    {
        var left = new StackPanel
        {
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 16, 0)
        };

        var titleBlock = new TextBlock
        {
            Text = title,
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        BindThemeForeground(titleBlock);
        left.Children.Add(titleBlock);

        if (!string.IsNullOrEmpty(desc))
        {
            var descBlock = new TextBlock
            {
                Text = desc,
                FontSize = 11,
                Opacity = 0.6,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = maxDescWidth,
                VerticalAlignment = VerticalAlignment.Center
            };
            BindThemeForeground(descBlock);
            left.Children.Add(descBlock);
        }

        control.VerticalAlignment = VerticalAlignment.Center;
        control.HorizontalAlignment = HorizontalAlignment.Right;

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("* Auto") };
        Grid.SetColumn(left, 0);
        Grid.SetColumn(control, 1);
        grid.Children.Add(left);
        grid.Children.Add(control);

        return new Border
        {
            Child = grid,
            Background = new SolidColorBrush(Color.Parse("#08000000")),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 10),
            Margin = new Thickness(0, 4)
        };
    }

    static void BindThemeForeground(TextBlock textBlock)
    {
        textBlock[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
    }
}
