using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Models.ComponentSettings;

namespace HolidayCountdown.Views.ComponentSettings;

public class SolarTermSettingsControl : ComponentBase<SolarTermSettings>
{
    private ToggleSwitch? _progressToggle;

    public SolarTermSettingsControl()
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

        _progressToggle = new ToggleSwitch
        {
            IsChecked = Settings?.ShowProgressRing ?? true,
            OnContent = "",
            OffContent = ""
        };
        _progressToggle.IsCheckedChanged += (s, ev) =>
        {
            if (Settings != null) Settings.ShowProgressRing = _progressToggle.IsChecked == true;
        };

        panel.Children.Add(CreateRow("显示进度环", "距离节气15天内显示弧形进度环", _progressToggle));
    }

    static Control CreateRow(string title, string desc, Control control)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("* Auto") };
        var left = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        left.Children.Add(new TextBlock { Text = title, FontWeight = FontWeight.SemiBold, FontSize = 13 });
        left.Children.Add(new TextBlock { Text = desc, FontSize = 11, Opacity = 0.5 });
        Grid.SetColumn(left, 0);
        control.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(control, 1);
        grid.Children.Add(left);
        grid.Children.Add(control);
        return grid;
    }
}
