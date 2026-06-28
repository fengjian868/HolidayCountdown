using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Models.ComponentSettings;

namespace HolidayCountdown.Views.ComponentSettings;

public class StudyTimeSettingsControl : ComponentBase<StudyTimeSettings>
{
    private ToggleSwitch? _enabledToggle;
    private ToggleSwitch? _iconToggle;
    private ToggleSwitch? _classOnlyToggle;
    private ToggleSwitch? _weeklyToggle;

    public StudyTimeSettingsControl()
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

        _enabledToggle = new ToggleSwitch { IsChecked = Settings?.Enabled ?? true, OnContent = "", OffContent = "" };
        _enabledToggle.IsCheckedChanged += (s, ev) => { if (Settings != null) Settings.Enabled = _enabledToggle.IsChecked == true; };
        panel.Children.Add(CreateRow("启用统计", "关闭后停止记录并清空显示", _enabledToggle));

        _iconToggle = new ToggleSwitch { IsChecked = Settings?.ShowIcon ?? true, OnContent = "", OffContent = "" };
        _iconToggle.IsCheckedChanged += (s, ev) => { if (Settings != null) Settings.ShowIcon = _iconToggle.IsChecked == true; };
        panel.Children.Add(CreateRow("显示图标", "", _iconToggle));

        _classOnlyToggle = new ToggleSwitch { IsChecked = Settings?.CountClassTimeOnly ?? false, OnContent = "", OffContent = "" };
        _classOnlyToggle.IsCheckedChanged += (s, ev) => { if (Settings != null) Settings.CountClassTimeOnly = _classOnlyToggle.IsChecked == true; };
        panel.Children.Add(CreateRow("仅统计上课时间", "只在上课/自习状态时累加时长", _classOnlyToggle));

        _weeklyToggle = new ToggleSwitch { IsChecked = Settings?.WeeklyReset ?? false, OnContent = "", OffContent = "" };
        _weeklyToggle.IsCheckedChanged += (s, ev) => { if (Settings != null) Settings.WeeklyReset = _weeklyToggle.IsChecked == true; };
        panel.Children.Add(CreateRow("每周重置", "按 ISO 周次统计本周学习时长", _weeklyToggle));
    }

    static Control CreateRow(string title, string desc, Control control)
    {
        return ComponentSettingRow.Create(title, desc, control);
    }
}
