using System;
using System.Linq;
using System.Threading.Tasks;
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
        var s = new StackPanel { Spacing = 14, Margin = new Thickness(24, 16) };
        s.Children.Add(Header("🌤️ 天气问候设置"));
        s.Children.Add(Expander("开关", new StackPanel { Spacing = 10 }.Also(p =>
        {
            p.Children.Add(Row("启用天气问候", "根据ClassIsland天气显示问候语", Toggle(_svc.Settings.WeatherGreetingEnabled, v => _svc.Settings.WeatherGreetingEnabled = v)));
            p.Children.Add(Row("预警覆盖提醒", "有预警时只显示预警信息", Toggle(_svc.Settings.WeatherWarningOverride, v => _svc.Settings.WeatherWarningOverride = v)));
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
            foreach (var item in _svc.Settings.WeatherGreetingItems)
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
                var keyBox = new TextBox { Text = item.Keyword, Width = 80, IsReadOnly = item.Keyword == "默认" };
                keyBox.TextChanged += (a, b) =>
                {
                    if (item.Keyword == "默认") return;
                    item.Keyword = keyBox.Text ?? "";
                };
                var textBox = new TextBox { Text = item.Text, Width = 200 };
                textBox.TextChanged += (a, b) => item.Text = textBox.Text ?? "";
                // 标签选择
                var tagCombo = new ComboBox { Width = 70 };
                var tags = new[] { "雨天", "寒冷", "高温", "舒适", "恶劣天气", "大风", "雷电", "默认" };
                foreach (var t in tags) tagCombo.Items.Add(t);
                tagCombo.SelectedIndex = Math.Max(0, Array.IndexOf(tags, item.Tag));
                if (tagCombo.SelectedIndex < 0) tagCombo.SelectedIndex = 0;
                tagCombo.SelectionChanged += (a, b) => item.Tag = tags[tagCombo.SelectedIndex];
                // 刷新同类按钮
                var refreshBtn = new Button { Content = "🔄", Padding = new Thickness(4, 2) };
                ToolTip.SetTip(refreshBtn, "刷新同类问候语");
                refreshBtn.Click += (a, e) =>
                {
                    refreshBtn.Content = "⏳";
                    var currentTag = item.Tag;
                    if (string.IsNullOrEmpty(currentTag)) currentTag = tags[tagCombo.SelectedIndex];
                    RefreshWeatherByTag(currentTag);
                    RefreshList();
                    refreshBtn.Content = "🔄";
                };
                var delBtn = new Button { Content = "🗑️", Padding = new Thickness(4, 2), IsVisible = item.Keyword != "默认" };
                delBtn.Click += (a, e) => { _svc.Settings.WeatherGreetingItems.Remove(item); RefreshList(); };

                row.Children.Add(new TextBlock { Text = "关键词", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                row.Children.Add(keyBox);
                row.Children.Add(new TextBlock { Text = "文案", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                row.Children.Add(textBox);
                row.Children.Add(tagCombo);
                row.Children.Add(refreshBtn);
                row.Children.Add(delBtn);
                listPanel.Children.Add(row);
            }
        }

        RefreshList();
        panel.Children.Add(listPanel);

        var addBtn = new Button { Content = "+ 添加天气问候", Padding = new Thickness(12, 4), HorizontalAlignment = HorizontalAlignment.Left };
        addBtn.Click += (a, e) =>
        {
            _svc.Settings.WeatherGreetingItems.Add(new WeatherGreetingItem { Keyword = "新天气", Text = "", Tag = "舒适" });
            RefreshList();
        };
        panel.Children.Add(addBtn);

        panel.Children.Add(new TextBlock { Text = "说明：当天气文本包含对应关键词时，显示该文案。{weather} 会被替换为实际天气名称。", Opacity = 0.5, FontSize = 11, TextWrapping = TextWrapping.Wrap });

        return panel;
    }

    /// <summary>
    /// 刷新指定标签的所有天气问候语，从该类型预设库中随机选取
    /// </summary>
    void RefreshWeatherByTag(string tag)
    {
        var pools = new System.Collections.Generic.Dictionary<string, string[]>
        {
            ["雨天"] = new[] { "记得带伞 ☔", "雨声潺潺，适合静思 🌧️", "雨天路滑，小心行走 🌧️", "听着雨声，心情也温柔了 🌧️", "出门别忘了伞哦 ☂️" },
            ["寒冷"] = new[] { "多穿点，别感冒了 ❄️", "天冷了，喝杯热饮暖暖身 ☕", "寒风凛冽，注意保暖 🧣", "雪花飘飘，注意防滑 ❄️", "天冷加衣，照顾好自己 🧥" },
            ["高温"] = new[] { "注意防暑，多喝水 🌡️", "烈日当空，避免暴晒 ☀️", "天气炎热，吃点清凉的 🍉", "高温难耐，注意午休 😴", "防晒补水，保持清爽 💧" },
            ["舒适"] = new[] { "天气不错，心情也好 😊", "舒适宜人，适合学习 📖", "微风拂面，神清气爽 🍃", "好天气，出去走走吧 🚶", "云淡风轻，一切刚好 ⛅" },
            ["恶劣天气"] = new[] { "天气不好，减少外出 😷", "注意安全，保护好自己 ⚠️", "恶劣天气，关好门窗 🏠", "能见度低，出行小心 🌫️", "宅家也不错，安全第一 🛋️" },
            ["大风"] = new[] { "风大，注意防风 💨", "远离广告牌和临时搭建物 🚧", "大风天，关好窗户 🪟", "注意防风，别被吹跑了 😄", "风大 dust多，戴口罩 😷" },
            ["雷电"] = new[] { "雷电天气，待在室内 ⚡", "关好电器，防雷击 🔌", "雷雨交加，注意安全 ⛈️", "别在树下避雨 🌳", "等雷停了再出门吧 🏠" }
        };

        if (!pools.TryGetValue(tag, out var pool)) return;

        var sameTagItems = _svc.Settings.WeatherGreetingItems.Where(i => i.Tag == tag && i.Keyword != "默认").ToList();
        var rnd = new Random();
        foreach (var item in sameTagItems)
        {
            var text = pool[rnd.Next(pool.Length)];
            if (!string.IsNullOrEmpty(text)) item.Text = text;
        }
        _svc.SaveSettings();
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
