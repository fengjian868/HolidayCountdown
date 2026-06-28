using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Models.ComponentSettings;

namespace HolidayCountdown.Views.ComponentSettings;

public class ExamCountdownSettingsControl : ComponentBase<ExamCountdownSettings>
{
    public ExamCountdownSettingsControl()
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

        var typeCombo = new ComboBox { Width = 100 };
        typeCombo.Items.Add("高考");
        typeCombo.Items.Add("中考");
        typeCombo.SelectedIndex = Settings?.ExamType ?? 0;
        typeCombo.SelectionChanged += (s, ev) =>
        {
            if (Settings != null) Settings.ExamType = typeCombo.SelectedIndex;
        };
        panel.Children.Add(CreateRow("考试类型", "选择倒计时对应的考试", typeCombo));

        var cityBox = new TextBox { Text = Settings?.City ?? "北京", Width = 120 };
        cityBox.TextChanged += (s, ev) =>
        {
            if (Settings != null) Settings.City = cityBox.Text ?? "北京";
        };
        panel.Children.Add(CreateRow("城市", "精确到城市，用于匹配内置考试时间", cityBox));

        var repeatToggle = new ToggleSwitch { IsChecked = Settings?.RepeatYearly ?? true, OnContent = "", OffContent = "" };
        repeatToggle.IsCheckedChanged += (s, ev) =>
        {
            if (Settings != null) Settings.RepeatYearly = repeatToggle.IsChecked == true;
        };
        panel.Children.Add(CreateRow("每年重复", "考试结束后自动进入下一年周期", repeatToggle));

        var customDateBox = new TextBox { Text = Settings?.CustomDate ?? "", Width = 120, Watermark = "MM-dd" };
        customDateBox.TextChanged += (s, ev) =>
        {
            if (Settings != null) Settings.CustomDate = string.IsNullOrWhiteSpace(customDateBox.Text) ? null : customDateBox.Text;
        };
        panel.Children.Add(CreateRow("自定义日期", "覆盖内置日期，格式 MM-dd（可选）", customDateBox));

        var customTextBox = new TextBox { Text = Settings?.CustomText ?? "{exam}还有{days}天", Width = 220 };
        customTextBox.TextChanged += (s, ev) =>
        {
            if (Settings != null) Settings.CustomText = customTextBox.Text ?? "{exam}还有{days}天";
        };
        panel.Children.Add(CreateRow("显示文案", "变量：{exam} {days} {date}", customTextBox));

        var todayTextBox = new TextBox { Text = Settings?.TodayText ?? "今天就是{exam}，加油！", Width = 220 };
        todayTextBox.TextChanged += (s, ev) =>
        {
            if (Settings != null) Settings.TodayText = todayTextBox.Text ?? "今天就是{exam}，加油！";
        };
        panel.Children.Add(CreateRow("当天文案", "考试当天显示的文案", todayTextBox));

        var showBgToggle = new ToggleSwitch { IsChecked = Settings?.ShowBackground ?? true, OnContent = "", OffContent = "" };
        showBgToggle.IsCheckedChanged += (s, ev) =>
        {
            if (Settings != null) Settings.ShowBackground = showBgToggle.IsChecked == true;
        };
        panel.Children.Add(CreateRow("显示背景", "在组件后方显示色块", showBgToggle));

        var textColorPicker = new ColorPicker { Width = 40, Height = 28, Color = TryParseColor(Settings?.TextColor ?? "#FF2196F3") };
        textColorPicker.ColorChanged += (s, ev) =>
        {
            if (Settings != null) Settings.TextColor = textColorPicker.Color.ToString();
        };
        panel.Children.Add(CreateRow("文字颜色", null, textColorPicker));

        var bgColorPicker = new ColorPicker { Width = 40, Height = 28, Color = TryParseColor(Settings?.BackgroundColor ?? "#202196F3") };
        bgColorPicker.ColorChanged += (s, ev) =>
        {
            if (Settings != null) Settings.BackgroundColor = bgColorPicker.Color.ToString();
        };
        panel.Children.Add(CreateRow("背景颜色", null, bgColorPicker));
    }

    static Control CreateRow(string title, string? desc, Control control)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("* Auto") };
        var left = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        left.Children.Add(new TextBlock { Text = title, FontWeight = FontWeight.SemiBold, FontSize = 13 });
        if (!string.IsNullOrEmpty(desc))
            left.Children.Add(new TextBlock { Text = desc, FontSize = 11, Opacity = 0.5 });
        Grid.SetColumn(left, 0);
        control.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(control, 1);
        grid.Children.Add(left);
        grid.Children.Add(control);
        return grid;
    }

    static Color TryParseColor(string hex)
    {
        try { return Color.Parse(hex); }
        catch { return Color.Parse("#2196F3"); }
    }
}
