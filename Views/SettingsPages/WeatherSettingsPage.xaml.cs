using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.SettingsPages;

[SettingsPageInfo("holidaycountdown.weather", "天气问候设置", "\uE753", "\uE753")]
public class WeatherSettingsPage : SettingsPageBase
{
    private readonly HolidayService _svc;
    public WeatherSettingsPage() { _svc = new HolidayService(); Content = Build(); }

    Control Build()
    {
        var s = new StackPanel { Spacing = 14, Margin = new Thickness(24, 16) };
        s.Children.Add(Header("🌤️ 天气问候设置"));
        s.Children.Add(Expander("开关", new StackPanel { Spacing = 10 }.Also(p =>
        {
            p.Children.Add(Row("启用天气问候", "根据ClassIsland天气显示问候语", Toggle(_svc.Settings.WeatherGreetingEnabled, v => _svc.Settings.WeatherGreetingEnabled = v)));
        })));
        s.Children.Add(Expander("问候语文案", BuildGreetingPanel()));
        s.Children.Add(new TextBlock { Text = "天气数据来自ClassIsland内置天气服务，插件会自动读取当前天气并匹配对应的问候语。", Opacity = 0.5, FontSize = 11, TextWrapping = TextWrapping.Wrap });
        s.Children.Add(SaveBtn());
        return new ScrollViewer { Content = s };
    }

    StackPanel BuildGreetingPanel()
    {
        var panel = new StackPanel { Spacing = 8 };
        var listPanel = new StackPanel { Spacing = 6 };

        void RefreshList()
        {
            listPanel.Children.Clear();
            foreach (var kv in _svc.Settings.WeatherGreetings)
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
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
                var textBox = new TextBox { Text = kv.Value, Width = 250 };
                textBox.TextChanged += (a, b) => _svc.Settings.WeatherGreetings[kv.Key] = textBox.Text ?? "";
                var delBtn = new Button { Content = "🗑️", Padding = new Thickness(4, 2), IsVisible = kv.Key != "默认" };
                delBtn.Click += (a, e) => { _svc.Settings.WeatherGreetings.Remove(kv.Key); RefreshList(); };

                row.Children.Add(new TextBlock { Text = "关键词", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                row.Children.Add(keyBox);
                row.Children.Add(new TextBlock { Text = "文案", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                row.Children.Add(textBox);
                row.Children.Add(delBtn);
                listPanel.Children.Add(row);
            }
        }

        RefreshList();
        panel.Children.Add(listPanel);

        var addBtn = new Button { Content = "+ 添加天气问候", Padding = new Thickness(12, 4), HorizontalAlignment = HorizontalAlignment.Left };
        addBtn.Click += (a, e) =>
        {
            _svc.Settings.WeatherGreetings["新天气"] = "";
            RefreshList();
        };
        panel.Children.Add(addBtn);

        panel.Children.Add(new TextBlock { Text = "说明：当天气文本包含对应关键词时，显示该文案。{weather} 会被替换为实际天气名称。", Opacity = 0.5, FontSize = 11, TextWrapping = TextWrapping.Wrap });

        return panel;
    }

    static TextBlock Header(string t) => new() { Text = t, FontSize = 22, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 0, 0, 8) };
    static Border Expander(string title, Control content)
    {
        var header = new Button { Content = $"▶ {title}", HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(12, 8) };
        var panel = new StackPanel { Spacing = 10, IsVisible = false };
        panel.Children.Add(content);
        var border = new Border { Background = new SolidColorBrush(Color.Parse("#0DFFFFFF")), CornerRadius = new CornerRadius(12), Padding = new Thickness(16), BorderBrush = new SolidColorBrush(Color.Parse("#1AFFFFFF")), BorderThickness = new Thickness(1), Margin = new Thickness(0, 4) };
        var container = new StackPanel { Spacing = 4 };
        container.Children.Add(header);
        container.Children.Add(panel);
        border.Child = container;
        header.Click += (a, e) =>
        {
            panel.IsVisible = !panel.IsVisible;
            header.Content = panel.IsVisible ? $"▼ {title}" : $"▶ {title}";
        };
        return border;
    }
    static Control Row(string l, string d, Control c) { var g = new Grid { ColumnDefinitions = new ColumnDefinitions("120 *") }; var left = new StackPanel { VerticalAlignment = VerticalAlignment.Center }; left.Children.Add(new TextBlock { Text = l, FontWeight = FontWeight.SemiBold }); if (!string.IsNullOrEmpty(d)) left.Children.Add(new TextBlock { Text = d, Opacity = 0.5, FontSize = 11 }); Grid.SetColumn(left, 0); Grid.SetColumn(c, 1); c.VerticalAlignment = VerticalAlignment.Center; g.Children.Add(left); g.Children.Add(c); return g; }
    static ToggleSwitch Toggle(bool v, Action<bool> cb) { var t = new ToggleSwitch { IsChecked = v, OnContent = "开", OffContent = "关" }; t.IsCheckedChanged += (a, b) => cb(t.IsChecked == true); return t; }
    Button SaveBtn() { var b = new Button { Content = "💾 保存", Padding = new Thickness(20, 8) }; b.Click += (a, e) => { _svc.SaveSettings(); b.Content = "✅ 已保存"; }; return b; }
}
