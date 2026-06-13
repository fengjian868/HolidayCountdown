using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Models;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.SettingsPages;

[SettingsPageInfo("holidaycountdown.weather", "天气问候设置", "\uE753", "\uE753")]
public class WeatherSettingsPage : SettingsPageBase
{
    private readonly HolidayService _svc;
    public WeatherSettingsPage() { _svc = new HolidayService(); Content = Build(); }

    Control Build()
    {
        var s = new StackPanel { Spacing = 0, Margin = new Thickness(20, 16) };
        s.Children.Add(SettingsUI.PageHeader("🌤️ 天气问候设置"));

        // 开关
        var togglePanel = new StackPanel { Spacing = 0 };
        togglePanel.Children.Add(SettingsUI.SettingItem("启用天气问候", "根据ClassIsland天气显示问候语",
            SettingsUI.Toggle(_svc.Settings.WeatherGreetingEnabled, v => _svc.Settings.WeatherGreetingEnabled = v)));
        s.Children.Add(SettingsUI.Expander("开关", "天气问候组件总开关", togglePanel));

        // 排版设置
        var layoutPanel = new StackPanel { Spacing = 0 };
        var presets = new[] { "仅问候", "图标+问候", "温度+问候", "完整信息" };
        var presetCombo = new ComboBox { Width = 120, HorizontalAlignment = HorizontalAlignment.Right };
        foreach (var p in presets) presetCombo.Items.Add(p);

        var currentTemplate = _svc.Settings.WeatherTemplate ?? "{greeting}";
        presetCombo.SelectedIndex = currentTemplate switch
        {
            "{greeting}" => 0,
            "{icon} {greeting}" => 1,
            "{temp} {greeting}" => 2,
            "{icon} {temp} {greeting} {warning}" => 3,
            _ => -1
        };
        presetCombo.SelectionChanged += (a, b) =>
        {
            _svc.Settings.WeatherTemplate = presetCombo.SelectedIndex switch
            {
                0 => "{greeting}",
                1 => "{icon} {greeting}",
                2 => "{temp} {greeting}",
                3 => "{icon} {temp} {greeting} {warning}",
                _ => _svc.Settings.WeatherTemplate
            };
        };

        layoutPanel.Children.Add(SettingsUI.SettingItem("预设模板", "快速选择排版样式", presetCombo));
        layoutPanel.Children.Add(SettingsUI.Separator());
        layoutPanel.Children.Add(SettingsUI.SettingItem("自定义模板", null,
            SettingsUI.Text(_svc.Settings.WeatherTemplate ?? "{greeting}", 280, v => _svc.Settings.WeatherTemplate = v)));
        layoutPanel.Children.Add(SettingsUI.Separator());
        layoutPanel.Children.Add(SettingsUI.SettingItem("显示天气图标", "在模板中使用 {icon}",
            SettingsUI.Toggle(_svc.Settings.WeatherShowIcon, v => _svc.Settings.WeatherShowIcon = v)));
        layoutPanel.Children.Add(SettingsUI.Separator());
        layoutPanel.Children.Add(SettingsUI.SettingItem("显示温度", "在模板中使用 {temp}",
            SettingsUI.Toggle(_svc.Settings.WeatherShowTemp, v => _svc.Settings.WeatherShowTemp = v)));
        layoutPanel.Children.Add(SettingsUI.Separator());
        layoutPanel.Children.Add(SettingsUI.Info("可用变量: {greeting} 问候语 | {temp} 温度 | {weather} 天气 | {warning} 预警 | {icon} 天气图标"));
        s.Children.Add(SettingsUI.Expander("排版", "自定义天气问候的显示格式", layoutPanel));

        // 温度提醒
        s.Children.Add(SettingsUI.Expander("温度提醒", "自定义各温度区间的穿衣提醒文案", BuildTempPanel()));

        // 天气关键词问候
        s.Children.Add(SettingsUI.Expander("天气关键词", "根据天气关键词匹配显示文案", BuildGreetingPanel()));

        s.Children.Add(SettingsUI.Info("天气数据来自ClassIsland内置天气服务，插件会自动读取当前天气并匹配对应的问候语。"));
        s.Children.Add(SettingsUI.SaveButton(() => _svc.SaveSettings()));
        return new ScrollViewer { Content = s };
    }

    StackPanel BuildTempPanel()
    {
        var panel = new StackPanel { Spacing = 8 };
        var listPanel = new StackPanel { Spacing = 0 };

        void RefreshList()
        {
            listPanel.Children.Clear();
            var items = _svc.Settings.TempGreetings.OrderBy(g => g.MinTemp).ToList();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(16, 8, 16, 8) };

                var minBox = new TextBox { Text = item.MinTemp.ToString(), Width = 45 };
                minBox.LostFocus += (a, b) =>
                {
                    if (int.TryParse(minBox.Text, out var v)) item.MinTemp = v;
                    else minBox.Text = item.MinTemp.ToString();
                };

                var maxBox = new TextBox { Text = item.MaxTemp == 999 ? "" : item.MaxTemp.ToString(), Width = 45, Watermark = "∞" };
                maxBox.LostFocus += (a, b) =>
                {
                    if (string.IsNullOrEmpty(maxBox.Text)) item.MaxTemp = 999;
                    else if (int.TryParse(maxBox.Text, out var v)) item.MaxTemp = v;
                    else maxBox.Text = item.MaxTemp == 999 ? "" : item.MaxTemp.ToString();
                };

                var textBox = SettingsUI.Text(item.Text, 200, v => item.Text = v);
                var delBtn = new Button { Content = "🗑️", Padding = new Thickness(4, 2) };
                delBtn.Click += (a, e) => { _svc.Settings.TempGreetings.Remove(item); RefreshList(); };

                row.Children.Add(new TextBlock { Text = "≥", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                row.Children.Add(minBox);
                row.Children.Add(new TextBlock { Text = "°C", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                row.Children.Add(new TextBlock { Text = "~", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                row.Children.Add(maxBox);
                row.Children.Add(new TextBlock { Text = "°C", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                row.Children.Add(textBox);
                row.Children.Add(delBtn);
                listPanel.Children.Add(row);
                if (i < items.Count - 1)
                    listPanel.Children.Add(SettingsUI.Separator());
            }
        }

        RefreshList();
        panel.Children.Add(listPanel);

        var addBtn = new Button { Content = "+ 添加温度区间", Padding = new Thickness(12, 4), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(16, 4, 16, 8) };
        addBtn.Click += (a, e) =>
        {
            _svc.Settings.TempGreetings.Add(new TempGreeting { MinTemp = 0, MaxTemp = 999, Text = "" });
            RefreshList();
        };
        panel.Children.Add(addBtn);

        var resetBtn = new Button { Content = "恢复默认", Padding = new Thickness(12, 4), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(16, 0, 16, 8) };
        resetBtn.Click += (a, e) =>
        {
            _svc.Settings.TempGreetings.Clear();
            foreach (var g in LocalGreetingDB.DefaultTempGreetings)
                _svc.Settings.TempGreetings.Add(new TempGreeting { MinTemp = g.MinTemp, MaxTemp = g.MaxTemp, Text = g.Text });
            RefreshList();
        };
        panel.Children.Add(resetBtn);

        return panel;
    }

    StackPanel BuildGreetingPanel()
    {
        var panel = new StackPanel { Spacing = 8 };
        var listPanel = new StackPanel { Spacing = 0 };

        void RefreshList()
        {
            listPanel.Children.Clear();
            var items = _svc.Settings.WeatherGreetings.ToList();
            for (int i = 0; i < items.Count; i++)
            {
                var kv = items[i];
                var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(16, 8, 16, 8) };
                var keyBox = new TextBox { Text = kv.Key, Width = 80, IsReadOnly = kv.Key == "默认" };
                keyBox.TextChanged += (a, b) =>
                {
                    if (kv.Key == "默认") return;
                    var newKey = keyBox.Text ?? "";
                    if (newKey != kv.Key && !string.IsNullOrEmpty(newKey) && !_svc.Settings.WeatherGreetings.ContainsKey(newKey))
                    {
                        _svc.Settings.WeatherGreetings.Remove(kv.Key);
                        _svc.Settings.WeatherGreetings[newKey] = kv.Value;
                    }
                };
                var textBox = SettingsUI.Text(kv.Value, 220, v => _svc.Settings.WeatherGreetings[kv.Key] = v);
                var delBtn = new Button { Content = "🗑️", Padding = new Thickness(4, 2), IsVisible = kv.Key != "默认" };
                delBtn.Click += (a, e) => { _svc.Settings.WeatherGreetings.Remove(kv.Key); RefreshList(); };

                row.Children.Add(new TextBlock { Text = "关键词", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                row.Children.Add(keyBox);
                row.Children.Add(new TextBlock { Text = "文案", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                row.Children.Add(textBox);
                row.Children.Add(delBtn);
                listPanel.Children.Add(row);
                if (i < items.Count - 1)
                    listPanel.Children.Add(SettingsUI.Separator());
            }
        }

        RefreshList();
        panel.Children.Add(listPanel);

        var addBtn = new Button { Content = "+ 添加天气问候", Padding = new Thickness(12, 4), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(16, 4, 16, 8) };
        addBtn.Click += (a, e) =>
        {
            _svc.Settings.WeatherGreetings["新天气"] = "";
            RefreshList();
        };
        panel.Children.Add(addBtn);

        panel.Children.Add(SettingsUI.Info("说明：当天气文本包含对应关键词时，显示该文案。{weather} 会被替换为实际天气名称。"));

        return panel;
    }
}
