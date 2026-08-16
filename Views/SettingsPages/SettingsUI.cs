using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;

namespace HolidayCountdown.Views.SettingsPages;

/// <summary>
/// SystemTools 风格的设置页 UI 构建工具
/// </summary>
public static class SettingsUI
{
    // 颜色常量
    public static readonly Avalonia.Media.Color CardBg = Avalonia.Media.Color.Parse("#0DFFFFFF");
    public static readonly Avalonia.Media.Color CardBorder = Avalonia.Media.Color.Parse("#1AFFFFFF");
    public static readonly Avalonia.Media.Color HeaderText = Avalonia.Media.Color.Parse("#FFFFFF");
    public static readonly Avalonia.Media.Color DescText = Avalonia.Media.Color.Parse("#A0A0A0");
    public static readonly Avalonia.Media.Color AccentColor = Avalonia.Media.Color.Parse("#2196F3");
    public static readonly Avalonia.Media.Color SeparatorColor = Avalonia.Media.Color.Parse("#15FFFFFF");

    /// <summary>
    /// 将文本前景色绑定到主题资源，自动适配明暗主题
    /// </summary>
    static void BindThemeForeground(TextBlock textBlock)
    {
        textBlock[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
    }

    /// <summary>
    /// 页面标题
    /// </summary>
    public static TextBlock PageHeader(string text)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = 24,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 16)
        };
        BindThemeForeground(tb);
        return tb;
    }

    /// <summary>
    /// 设置卡片容器（圆角背景）
    /// </summary>
    public static Border Card(Control content) => new()
    {
        Background = new SolidColorBrush(CardBg),
        BorderBrush = new SolidColorBrush(CardBorder),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(8),
        Margin = new Thickness(0, 0, 0, 12),
        Child = content
    };

    /// <summary>
    /// 可折叠设置组（SystemTools 风格）
    /// </summary>
    public static Control Expander(string title, string? description, Control content, bool expanded = false)
    {
        var panel = new StackPanel();

        // 头部按钮（可点击展开/折叠）
        var headerBtn = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(16, 12),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0)
        };

        var headerGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("* Auto") };

        // 左侧：标题+描述
        var leftPanel = new StackPanel { Spacing = 2 };
        var titleBlock = new TextBlock
        {
            Text = title,
            FontWeight = FontWeight.SemiBold,
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center
        };
        BindThemeForeground(titleBlock);
        leftPanel.Children.Add(titleBlock);
        if (!string.IsNullOrEmpty(description))
        {
            var descBlock = new TextBlock
            {
                Text = description,
                FontSize = 12,
                Opacity = 0.6,
                TextWrapping = TextWrapping.Wrap
            };
            BindThemeForeground(descBlock);
            leftPanel.Children.Add(descBlock);
        }
        Grid.SetColumn(leftPanel, 0);

        // 右侧：箭头图标
        var arrow = new Path
        {
            Width = 12,
            Height = 12,
            Stretch = Stretch.Uniform,
            Fill = new SolidColorBrush(DescText),
            Data = Geometry.Parse("M 0 0 L 6 6 L 12 0"),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative)
        };
        var arrowBorder = new Border
        {
            Width = 24,
            Height = 24,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = arrow
        };
        Grid.SetColumn(arrowBorder, 1);

        headerGrid.Children.Add(leftPanel);
        headerGrid.Children.Add(arrowBorder);
        headerBtn.Content = headerGrid;

        // 内容区域
        var contentPanel = new StackPanel
        {
            Spacing = 0,
            IsVisible = expanded,
            Margin = new Thickness(0, 0, 0, 8)
        };

        // 分隔线
        var separator = new Border
        {
            Height = 1,
            Background = new SolidColorBrush(SeparatorColor),
            Margin = new Thickness(16, 0, 16, 8),
            IsVisible = expanded
        };

        contentPanel.Children.Add(separator);
        contentPanel.Children.Add(content);

        panel.Children.Add(headerBtn);
        panel.Children.Add(contentPanel);

        // 点击展开/折叠动画
        headerBtn.Click += (s, e) =>
        {
            var isExpanding = !contentPanel.IsVisible;
            contentPanel.IsVisible = isExpanding;
            separator.IsVisible = isExpanding;

            // 箭头旋转动画
            arrow.RenderTransform = isExpanding
                ? new RotateTransform(180)
                : new RotateTransform(0);
        };

        // 初始状态箭头
        if (expanded)
            arrow.RenderTransform = new RotateTransform(180);

        return Card(panel);
    }

    /// <summary>
    /// 单行设置项（SystemTools SettingsCard 风格）
    /// 左侧标题+描述，右侧控件
    /// </summary>
    public static Control SettingItem(string title, string? description, Control control)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("* Auto") };
        grid.Margin = new Thickness(16, 10, 16, 10);

        var left = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        var titleBlock = new TextBlock
        {
            Text = title,
            FontWeight = FontWeight.SemiBold,
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center
        };
        BindThemeForeground(titleBlock);
        left.Children.Add(titleBlock);
        if (!string.IsNullOrEmpty(description))
        {
            var descBlock = new TextBlock
            {
                Text = description,
                FontSize = 11,
                Opacity = 0.5,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 280
            };
            BindThemeForeground(descBlock);
            left.Children.Add(descBlock);
        }
        Grid.SetColumn(left, 0);

        control.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(control, 1);

        grid.Children.Add(left);
        grid.Children.Add(control);
        return grid;
    }

    /// <summary>
    /// 带图标的单行设置项
    /// </summary>
    public static Control SettingItem(string icon, string title, string? description, Control control)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("36 * Auto") };
        grid.Margin = new Thickness(16, 10, 16, 10);

        // 图标
        var iconBlock = new TextBlock
        {
            Text = icon,
            FontSize = 18,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        BindThemeForeground(iconBlock);
        Grid.SetColumn(iconBlock, 0);

        // 标题+描述
        var left = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        var titleBlock2 = new TextBlock
        {
            Text = title,
            FontWeight = FontWeight.SemiBold,
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center
        };
        BindThemeForeground(titleBlock2);
        left.Children.Add(titleBlock2);
        if (!string.IsNullOrEmpty(description))
        {
            var descBlock2 = new TextBlock
            {
                Text = description,
                FontSize = 11,
                Opacity = 0.5,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 260
            };
            BindThemeForeground(descBlock2);
            left.Children.Add(descBlock2);
        }
        Grid.SetColumn(left, 1);

        control.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(control, 2);

        grid.Children.Add(iconBlock);
        grid.Children.Add(left);
        grid.Children.Add(control);
        return grid;
    }

    /// <summary>
    /// 设置项之间的分隔线
    /// </summary>
    public static Control Separator() => new Border
    {
        Height = 1,
        Background = new SolidColorBrush(SeparatorColor),
        Margin = new Thickness(16, 0, 16, 0)
    };

    /// <summary>
    /// 开关控件（用 CheckBox 代替 ToggleSwitch：ToggleSwitch 在组件设置抽屉中会因
    /// PART_MovingKnobs 模板部件缺失而崩溃，CheckBox 控件模板跨版本稳定）
    /// </summary>
    public static CheckBox Toggle(bool value, Action<bool> onChanged)
    {
        var c = new CheckBox { IsChecked = value };
        c.Checked += (s, e) => onChanged(true);
        c.Unchecked += (s, e) => onChanged(false);
        return c;
    }

    /// <summary>
    /// 数字输入框
    /// </summary>
    public static TextBox Number(int value, int min, int max, Action<int> onChanged)
    {
        var t = new TextBox
        {
            Text = value.ToString(),
            Width = 60,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        t.LostFocus += (s, e) =>
        {
            if (int.TryParse(t.Text, out var n))
            {
                n = Math.Max(min, Math.Min(max, n));
                t.Text = n.ToString();
                onChanged(n);
            }
        };
        return t;
    }

    /// <summary>
    /// 文本输入框
    /// </summary>
    public static TextBox Text(string value, int width, Action<string> onChanged)
    {
        var t = new TextBox
        {
            Text = value,
            Width = width,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        t.TextChanged += (s, e) => onChanged(t.Text ?? "");
        return t;
    }

    /// <summary>
    /// 下拉选择框
    /// </summary>
    public static ComboBox Combo(string[] items, int selectedIndex, Action<int> onChanged)
    {
        var c = new ComboBox
        {
            Width = 100,
            SelectedIndex = selectedIndex,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        foreach (var item in items) c.Items.Add(item);
        c.SelectionChanged += (s, e) => onChanged(c.SelectedIndex);
        return c;
    }

    /// <summary>
    /// 日期选择器
    /// </summary>
    public static DatePicker Date(DateTime value, Action<DateTime> onChanged)
    {
        var d = new DatePicker
        {
            SelectedDate = value,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        d.SelectedDateChanged += (s, e) =>
        {
            if (d.SelectedDate.HasValue) onChanged(d.SelectedDate.Value.DateTime);
        };
        return d;
    }

    /// <summary>
    /// 颜色选择器
    /// </summary>
    public static ColorPicker ColorPicker(string hex, Action<string> onChanged)
    {
        var picker = new ColorPicker
        {
            Width = 40,
            Height = 28,
            Color = TryParseColor(hex),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        picker.ColorChanged += (s, e) => onChanged(picker.Color.ToString());
        return picker;
    }

    /// <summary>
    /// 保存按钮
    /// </summary>
    public static Button SaveButton(Action onSave)
    {
        var b = new Button
        {
            Content = "💾 保存设置",
            Padding = new Thickness(24, 10),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 8, 0, 0)
        };
        b.Click += (s, e) =>
        {
            onSave();
            b.Content = "✅ 已保存";
        };
        return b;
    }

    /// <summary>
    /// 信息提示文本
    /// </summary>
    public static TextBlock Info(string text)
    {
        var tb = new TextBlock
        {
            Text = text,
            Opacity = 0.5,
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(16, 8, 16, 8)
        };
        BindThemeForeground(tb);
        return tb;
    }

    static Avalonia.Media.Color TryParseColor(string hex)
    {
        try { return Avalonia.Media.Color.Parse(hex); }
        catch { return Avalonia.Media.Color.Parse("#2196F3"); }
    }
}
