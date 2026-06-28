using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Models.ComponentSettings;

namespace HolidayCountdown.Views.ComponentSettings;

public class VacationSettingsControl : ComponentBase<VacationSettings>
{
    private ToggleSwitch? _showToggle;

    public VacationSettingsControl()
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

        _showToggle = new ToggleSwitch
        {
            IsChecked = Settings?.ShowCountdown ?? true,
            OnContent = "",
            OffContent = ""
        };
        _showToggle.IsCheckedChanged += (s, ev) =>
        {
            if (Settings != null) Settings.ShowCountdown = _showToggle.IsChecked == true;
        };
        panel.Children.Add(CreateRow("显示寒暑假倒计时", "关闭后组件将不显示任何内容", _showToggle));
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
