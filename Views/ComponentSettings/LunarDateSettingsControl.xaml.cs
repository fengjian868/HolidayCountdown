using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Models.ComponentSettings;

namespace HolidayCountdown.Views.ComponentSettings;

public class LunarDateSettingsControl : ComponentBase<LunarDateSettings>
{
    private TextBox? _templateBox;
    private ToggleSwitch? _autoRefreshToggle;

    public LunarDateSettingsControl()
    {
        Content = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            Margin = new Thickness(0, 8, 0, 0)
        };
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (Content is not StackPanel panel) return;
        panel.Children.Clear();

        _templateBox = new TextBox
        {
            Text = Settings?.Template ?? "{gzYear} {IMonthCn}{IDayCn} {Animal}",
            Width = 240,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        _templateBox.TextChanged += (s, ev) =>
        {
            if (Settings != null) Settings.Template = _templateBox.Text ?? "";
        };
        panel.Children.Add(CreateRow("显示模板", "{gzYear} 干支年 | {IMonthCn} 农历月 | {IDayCn} 农历日 | {Animal} 生肖 | {Term} 节气", _templateBox));

        _autoRefreshToggle = new ToggleSwitch
        {
            IsChecked = Settings?.AutoRefresh ?? true,
            OnContent = "",
            OffContent = ""
        };
        _autoRefreshToggle.IsCheckedChanged += (s, ev) =>
        {
            if (Settings != null) Settings.AutoRefresh = _autoRefreshToggle.IsChecked == true;
        };
        panel.Children.Add(CreateRow("自动刷新", "联网获取当月农历数据", _autoRefreshToggle));
    }

    static Control CreateRow(string title, string desc, Control control)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("* Auto") };
        var left = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        left.Children.Add(new TextBlock { Text = title, FontWeight = FontWeight.SemiBold, FontSize = 13 });
        left.Children.Add(new TextBlock { Text = desc, FontSize = 11, Opacity = 0.5, TextWrapping = TextWrapping.Wrap, MaxWidth = 260 });
        Grid.SetColumn(left, 0);
        control.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(control, 1);
        grid.Children.Add(left);
        grid.Children.Add(control);
        return grid;
    }
}
