using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using ClassIsland.Core.Abstractions.Controls;

namespace HolidayCountdown.Automation.Actions;

public class RandomDelayActionSettingsControl : ActionSettingsControlBase<RandomDelaySettings>
{
    private NumericUpDown? _minInput;
    private NumericUpDown? _maxInput;

    public RandomDelayActionSettingsControl()
    {
        var panel = new StackPanel
        {
            Spacing = 8,
            Margin = new Thickness(16),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var title = new TextBlock { Text = "区间随机等待设置", FontWeight = FontWeight.Bold, FontSize = 14 };
        panel.Children.Add(title);

        var minRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        minRow.Children.Add(new TextBlock { Text = "最小等待（秒）:", VerticalAlignment = VerticalAlignment.Center });
        _minInput = new NumericUpDown
        {
            Minimum = 0, Maximum = 3600, Value = 1,
            Width = 120, FormatString = "0"
        };
        minRow.Children.Add(_minInput);
        panel.Children.Add(minRow);

        var maxRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        maxRow.Children.Add(new TextBlock { Text = "最大等待（秒）:", VerticalAlignment = VerticalAlignment.Center });
        _maxInput = new NumericUpDown
        {
            Minimum = 0, Maximum = 3600, Value = 10,
            Width = 120, FormatString = "0"
        };
        maxRow.Children.Add(_maxInput);
        panel.Children.Add(maxRow);

        Content = panel;

        _minInput.ValueChanged += (s, e) => SaveSettings();
        _maxInput.ValueChanged += (s, e) => SaveSettings();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        LoadSettings();
    }

    void LoadSettings()
    {
        try
        {
            if (Settings == null) return;
            _minInput!.Value = Settings.MinSeconds;
            _maxInput!.Value = Settings.MaxSeconds;
        }
        catch { }
    }

    void SaveSettings()
    {
        try
        {
            if (Settings == null) return;
            Settings.MinSeconds = (int)(_minInput?.Value ?? 1);
            Settings.MaxSeconds = (int)(_maxInput?.Value ?? 10);
        }
        catch { }
    }
}
