using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
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
        return ComponentSettingRow.Create(title, desc, control);
    }
}
