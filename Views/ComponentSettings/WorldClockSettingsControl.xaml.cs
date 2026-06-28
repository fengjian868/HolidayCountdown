using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Models.ComponentSettings;

namespace HolidayCountdown.Views.ComponentSettings;

public class WorldClockSettingsControl : ComponentBase<WorldClockSettings>
{
    public WorldClockSettingsControl()
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

        var showSecondsToggle = new ToggleSwitch { IsChecked = Settings?.ShowSeconds ?? false, OnContent = "", OffContent = "" };
        showSecondsToggle.IsCheckedChanged += (s, ev) =>
        {
            if (Settings != null) Settings.ShowSeconds = showSecondsToggle.IsChecked == true;
        };
        panel.Children.Add(CreateRow("显示秒数", null, showSecondsToggle));

        var showDateToggle = new ToggleSwitch { IsChecked = Settings?.ShowDate ?? false, OnContent = "", OffContent = "" };
        showDateToggle.IsCheckedChanged += (s, ev) =>
        {
            if (Settings != null) Settings.ShowDate = showDateToggle.IsChecked == true;
        };
        panel.Children.Add(CreateRow("显示日期", null, showDateToggle));

        var colorPicker = new ColorPicker { Width = 40, Height = 28, Color = TryParseColor(Settings?.TextColor ?? "#FFFFFFFF") };
        colorPicker.ColorChanged += (s, ev) =>
        {
            if (Settings != null) Settings.TextColor = colorPicker.Color.ToString();
        };
        panel.Children.Add(CreateRow("文字颜色", null, colorPicker));

        var listPanel = new StackPanel { Spacing = 0 };

        void RefreshList()
        {
            listPanel.Children.Clear();
            var cities = Settings?.Cities ?? new List<WorldClockCity>();
            for (int i = 0; i < cities.Count; i++)
            {
                var city = cities[i];
                var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 4, 0, 4) };

                var nameBox = new TextBox { Text = city.Name, Width = 80, Watermark = "城市名" };
                nameBox.TextChanged += (s, ev) => { city.Name = nameBox.Text ?? ""; };

                var tzBox = new TextBox { Text = city.TimeZoneId, Width = 180, Watermark = "时区ID" };
                tzBox.TextChanged += (s, ev) => { city.TimeZoneId = tzBox.Text ?? ""; };

                var delBtn = new Button { Content = "删除", Padding = new Thickness(6, 2), Foreground = new SolidColorBrush(Color.Parse("#FFE53935")) };
                delBtn.Click += (s, ev) =>
                {
                    cities.Remove(city);
                    RefreshList();
                };

                row.Children.Add(new TextBlock { Text = $"城市{i + 1}", VerticalAlignment = VerticalAlignment.Center, Width = 45 });
                row.Children.Add(nameBox);
                row.Children.Add(tzBox);
                row.Children.Add(delBtn);
                listPanel.Children.Add(row);
            }
        }

        RefreshList();
        panel.Children.Add(listPanel);

        var addBtn = new Button { Content = "+ 添加城市（最多5个）", Padding = new Thickness(12, 4), HorizontalAlignment = HorizontalAlignment.Left };
        addBtn.Click += (s, ev) =>
        {
            if (Settings != null && Settings.Cities.Count < 5)
            {
                Settings.Cities.Add(new WorldClockCity { Name = "新城市", TimeZoneId = "UTC" });
                RefreshList();
            }
        };
        panel.Children.Add(addBtn);

        panel.Children.Add(new TextBlock
        {
            Text = "常用时区ID：China Standard Time（北京）、Tokyo Standard Time、Pacific Standard Time、Eastern Standard Time、GMT Standard Time、Central European Standard Time",
            FontSize = 10,
            Opacity = 0.5,
            TextWrapping = TextWrapping.Wrap
        });
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
        catch { return Color.Parse("#FFFFFFFF"); }
    }
}
