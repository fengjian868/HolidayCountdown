using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Models.ComponentSettings;

namespace HolidayCountdown.Views.ComponentSettings;

public class CustomHolidaySettingsControl : ComponentBase<CustomHolidaySettings>
{
    private ComboBox? _countCombo;
    private ToggleSwitch? _iconToggle;
    private ToggleSwitch? _daysToggle;

    public CustomHolidaySettingsControl()
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

        _countCombo = new ComboBox { Width = 80, HorizontalAlignment = HorizontalAlignment.Right };
        foreach (var c in new[] { "1", "2", "3", "5" }) _countCombo.Items.Add(c);
        _countCombo.SelectedIndex = (Settings?.DisplayCount ?? 3) switch
        {
            1 => 0,
            2 => 1,
            3 => 2,
            _ => 3
        };
        _countCombo.SelectionChanged += (s, ev) =>
        {
            if (Settings != null) Settings.DisplayCount = _countCombo.SelectedIndex switch
            {
                0 => 1,
                1 => 2,
                2 => 3,
                _ => 5
            };
        };
        panel.Children.Add(CreateRow("显示数量", "同时显示多少个即将到来的自定义节日", _countCombo));

        _iconToggle = new ToggleSwitch { IsChecked = Settings?.ShowIcon ?? true, OnContent = "", OffContent = "" };
        _iconToggle.IsCheckedChanged += (s, ev) => { if (Settings != null) Settings.ShowIcon = _iconToggle.IsChecked == true; };
        panel.Children.Add(CreateRow("显示图标", "", _iconToggle));

        _daysToggle = new ToggleSwitch { IsChecked = Settings?.ShowDays ?? true, OnContent = "", OffContent = "" };
        _daysToggle.IsCheckedChanged += (s, ev) => { if (Settings != null) Settings.ShowDays = _daysToggle.IsChecked == true; };
        panel.Children.Add(CreateRow("显示剩余天数", "", _daysToggle));
    }

    static Control CreateRow(string title, string desc, Control control)
    {
        return ComponentSettingRow.Create(title, desc, control);
    }
}
