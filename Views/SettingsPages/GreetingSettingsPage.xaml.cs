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

[SettingsPageInfo("holidaycountdown.greeting", "问候语设置", "\uE9D2", "\uE9D2")]
public class GreetingSettingsPage : SettingsPageBase
{
    private readonly HolidayService _svc;
    public GreetingSettingsPage() { _svc = new HolidayService(); Content = Build(); }

    Control Build()
    {
        var s = new StackPanel { Spacing = 14, Margin = new Thickness(24, 16) };
        s.Children.Add(Header("💬 问候语设置"));
        s.Children.Add(Expander("开关", new StackPanel { Spacing = 10 }.Also(p =>
        {
            p.Children.Add(Row("启用问候语", "", Toggle(_svc.Settings.ShowGreeting, v => _svc.Settings.ShowGreeting = v)));
            p.Children.Add(Row("合并到节假日组件", "问候语显示在节假日下方", Toggle(_svc.Settings.MergeGreeting, v => _svc.Settings.MergeGreeting = v)));
            p.Children.Add(Row("联网刷新问候语", "每天自动从网络获取新文案", Toggle(_svc.Settings.GreetingOnline, v => _svc.Settings.GreetingOnline = v)));
            p.Children.Add(Row("周日晚修提醒", "周日17-21点显示晚修提示", Toggle(_svc.Settings.ShowSundayEveningStudy, v => _svc.Settings.ShowSundayEveningStudy = v)));
        })));
        s.Children.Add(Expander("放学", new StackPanel { Spacing = 10 }.Also(p =>
        {
            p.Children.Add(Row("放学时间", "时:分", new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 }.Also(h => { h.Children.Add(Tx(_svc.Settings.SchoolEndHour.ToString("D2"), 40, v => _svc.Settings.SchoolEndHour = Math.Max(0, Math.Min(23, int.Parse(v))))); h.Children.Add(new TextBlock { Text = ":", VerticalAlignment = VerticalAlignment.Center }); h.Children.Add(Tx(_svc.Settings.SchoolEndMinute.ToString("D2"), 40, v => _svc.Settings.SchoolEndMinute = Math.Max(0, Math.Min(59, int.Parse(v))))); })));
            p.Children.Add(Row("提前提醒分钟", "放学前多少分钟切换提醒", Num(_svc.Settings.SchoolEndReminderMinutes, 1, 60, v => _svc.Settings.SchoolEndReminderMinutes = v)));
            p.Children.Add(Row("放学前文案", "", Tx(_svc.Settings.BeforeSchoolEndText, 200, v => _svc.Settings.BeforeSchoolEndText = v)));
            p.Children.Add(Row("放学后文案", "", Tx(_svc.Settings.AfterSchoolEndText, 200, v => _svc.Settings.AfterSchoolEndText = v)));
            p.Children.Add(Row("晚修文案", "", Tx(_svc.Settings.SundayEveningStudyText, 200, v => _svc.Settings.SundayEveningStudyText = v)));
        })));
        s.Children.Add(Expander("时段文案", BuildTimeSlotPanel()));
        s.Children.Add(Expander("特殊日期", BuildSpecialDatePanel()));
        s.Children.Add(SaveBtn());
        return new ScrollViewer { Content = s };
    }

    StackPanel BuildTimeSlotPanel()
    {
        var panel = new StackPanel { Spacing = 8 };
        var listPanel = new StackPanel { Spacing = 6 };

        void RefreshList()
        {
            listPanel.Children.Clear();
            foreach (var slot in _svc.Settings.TimeSlotGreetings.OrderBy(x => x.StartHour * 60 + x.StartMinute))
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
                var startBox = Tx($"{slot.StartHour:D2}:{slot.StartMinute:D2}", 50, v =>
                {
                    if (TimeSpan.TryParse(v, out var ts)) { slot.StartHour = ts.Hours; slot.StartMinute = ts.Minutes; }
                });
                var endBox = Tx($"{slot.EndHour:D2}:{slot.EndMinute:D2}", 50, v =>
                {
                    if (TimeSpan.TryParse(v, out var ts)) { slot.EndHour = ts.Hours; slot.EndMinute = ts.Minutes; }
                });
                var textBox = Tx(slot.Text, 200, v => slot.Text = v);
                // 标签选择
                var tagCombo = new ComboBox { Width = 60 };
                var tags = new[] { "早晨", "上午", "中午", "下午", "傍晚", "晚上", "深夜" };
                foreach (var t in tags) tagCombo.Items.Add(t);
                tagCombo.SelectedIndex = Math.Max(0, Array.IndexOf(tags, slot.Tag));
                if (tagCombo.SelectedIndex < 0) tagCombo.SelectedIndex = 0;
                tagCombo.SelectionChanged += (a, b) => slot.Tag = tags[tagCombo.SelectedIndex];
                // 刷新同类按钮
                var refreshBtn = new Button { Content = "🔄", Padding = new Thickness(4, 2) };
                ToolTip.SetTip(refreshBtn, "刷新同类问候语");
                refreshBtn.Click += (a, e) =>
                {
                    refreshBtn.Content = "⏳";
                    var currentTag = slot.Tag;
                    if (string.IsNullOrEmpty(currentTag)) currentTag = tags[tagCombo.SelectedIndex];
                    RefreshByTag(currentTag);
                    RefreshList();
                    refreshBtn.Content = "🔄";
                };
                var delBtn = new Button { Content = "🗑️", Padding = new Thickness(4, 2) };
                delBtn.Click += (a, e) => { _svc.Settings.TimeSlotGreetings.Remove(slot); RefreshList(); };

                row.Children.Add(new TextBlock { Text = "从", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                row.Children.Add(startBox);
                row.Children.Add(new TextBlock { Text = "到", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                row.Children.Add(endBox);
                row.Children.Add(textBox);
                row.Children.Add(tagCombo);
                row.Children.Add(refreshBtn);
                row.Children.Add(delBtn);
                listPanel.Children.Add(row);
            }
        }

        RefreshList();
        panel.Children.Add(listPanel);

        var addBtn = new Button { Content = "+ 添加时段", Padding = new Thickness(12, 4), HorizontalAlignment = HorizontalAlignment.Left };
        addBtn.Click += (a, e) =>
        {
            _svc.Settings.TimeSlotGreetings.Add(new TimeSlotGreeting { StartHour = 8, StartMinute = 0, EndHour = 12, EndMinute = 0, Text = "" });
            RefreshList();
        };
        panel.Children.Add(addBtn);

        return panel;
    }

    StackPanel BuildSpecialDatePanel()
    {
        var panel = new StackPanel { Spacing = 8 };
        var listPanel = new StackPanel { Spacing = 6 };

        void RefreshList()
        {
            listPanel.Children.Clear();
            foreach (var item in _svc.Settings.SpecialDateGreetings)
            {
                var row = new StackPanel { Spacing = 4 };
                
                // 第一行：名称 + 星期 + 标签 + 启用开关 + 删除
                var headerRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
                var nameBox = Tx(item.Name, 80, v => item.Name = v);
                var dayCombo = new ComboBox { Width = 70 };
                var days = new[] { "周一", "周二", "周三", "周四", "周五", "周六", "周日" };
                foreach (var d in days) dayCombo.Items.Add(d);
                dayCombo.SelectedIndex = Math.Max(0, Math.Min(6, item.DayOfWeek - 1));
                dayCombo.SelectionChanged += (a, b) => item.DayOfWeek = dayCombo.SelectedIndex + 1;
                // 标签选择
                var tagCombo = new ComboBox { Width = 60 };
                foreach (var d in days) tagCombo.Items.Add(d);
                tagCombo.SelectedIndex = Math.Max(0, Array.IndexOf(days, item.Tag));
                if (tagCombo.SelectedIndex < 0) tagCombo.SelectedIndex = dayCombo.SelectedIndex;
                tagCombo.SelectionChanged += (a, b) => item.Tag = days[tagCombo.SelectedIndex];
                var enabledChk = new CheckBox { Content = "启用", IsChecked = item.Enabled };
                enabledChk.IsCheckedChanged += (a, b) => item.Enabled = enabledChk.IsChecked == true;
                var delBtn = new Button { Content = "🗑️", Padding = new Thickness(4, 2) };
                delBtn.Click += (a, e) => { _svc.Settings.SpecialDateGreetings.Remove(item); RefreshList(); };
                
                headerRow.Children.Add(nameBox);
                headerRow.Children.Add(dayCombo);
                headerRow.Children.Add(tagCombo);
                headerRow.Children.Add(enabledChk);
                headerRow.Children.Add(delBtn);
                row.Children.Add(headerRow);
                
                // 第二行：时段 + 刷新同类按钮
                var timeRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
                var startBox = Tx($"{item.StartHour:D2}:{item.StartMinute:D2}", 50, v =>
                {
                    if (TimeSpan.TryParse(v, out var ts)) { item.StartHour = ts.Hours; item.StartMinute = ts.Minutes; }
                });
                var endBox = Tx($"{item.EndHour:D2}:{item.EndMinute:D2}", 50, v =>
                {
                    if (TimeSpan.TryParse(v, out var ts)) { item.EndHour = ts.Hours; item.EndMinute = ts.Minutes; }
                });
                var textBox = Tx(item.Text, 200, v => item.Text = v);
                var refreshBtn = new Button { Content = "🔄", Padding = new Thickness(4, 2) };
                ToolTip.SetTip(refreshBtn, "刷新同类问候语");
                refreshBtn.Click += (a, e) =>
                {
                    refreshBtn.Content = "⏳";
                    var currentTag = item.Tag;
                    if (string.IsNullOrEmpty(currentTag)) currentTag = days[tagCombo.SelectedIndex];
                    RefreshSpecialByTag(currentTag);
                    RefreshList();
                    refreshBtn.Content = "🔄";
                };
                
                timeRow.Children.Add(new TextBlock { Text = "从", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                timeRow.Children.Add(startBox);
                timeRow.Children.Add(new TextBlock { Text = "到", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                timeRow.Children.Add(endBox);
                timeRow.Children.Add(textBox);
                timeRow.Children.Add(refreshBtn);
                row.Children.Add(timeRow);
                
                listPanel.Children.Add(row);
            }
        }

        RefreshList();
        panel.Children.Add(listPanel);

        var addBtn = new Button { Content = "+ 添加特殊日期", Padding = new Thickness(12, 4), HorizontalAlignment = HorizontalAlignment.Left };
        addBtn.Click += (a, e) =>
        {
            _svc.Settings.SpecialDateGreetings.Add(new SpecialDateGreeting { Name = "新日期", DayOfWeek = 1, StartHour = 0, StartMinute = 0, EndHour = 23, EndMinute = 59, Text = "" });
            RefreshList();
        };
        panel.Children.Add(addBtn);

        return panel;
    }

    /// <summary>
    /// 刷新指定标签的所有时段问候语，从预设库中随机选取
    /// </summary>
    void RefreshByTag(string tag)
    {
        var pools = new System.Collections.Generic.Dictionary<string, string[]>
        {
            ["早晨"] = new[] { "早啊，今天也要加油 💪", "新的一天开始了，元气满满 ☀️", "早安，记得吃早餐 🍞", "清晨的第一缕阳光，送给你 🌅", "早起的人儿有书读 📚" },
            ["上午"] = new[] { "上午好，保持专注 📖", "趁早上头脑清醒，多学一点 🧠", "上午是黄金时间，别浪费 ⏰", "加油，离午休不远了 🍱", "保持状态，继续冲 💨" },
            ["中午"] = new[] { "吃饭时间到！🍚", "午饭吃什么？🤔", "吃饱了才有力气学习 🍜", "午休一下，下午更有精神 😴", "记得细嚼慢咽哦 🥢" },
            ["下午"] = new[] { "下午容易犯困，坚持住 😪", "来杯咖啡提提神 ☕", "下午也是学习的好时光 📚", "再坚持一下，快放学了 🎒", "打起精神，别走神 💪" },
            ["傍晚"] = new[] { "即将吃晚饭！！🍽️", "一天快结束了，辛苦了 🌆", "夕阳无限好，只是近黄昏 🌇", "晚饭吃什么好呢？🤤", "放松一下，准备晚餐 🍳" },
            ["晚上"] = new[] { "再坚持一下就放学了！🎉", "晚上是复习的好时间 📖", "别熬夜，注意作息 🌙", "今天的任务完成了吗？✅", "晚安前的最后冲刺 🏃" },
            ["深夜"] = new[] { "该睡觉了，别熬太晚 🌙", "熬夜伤身，早点休息 😴", "深夜了，明天再继续吧 🛏️", "晚安，好梦 💤", "身体是革命的本钱 🌟" }
        };

        if (!pools.TryGetValue(tag, out var pool)) return;

        var sameTagSlots = _svc.Settings.TimeSlotGreetings.Where(s => s.Tag == tag).ToList();
        var rnd = new Random();
        foreach (var slot in sameTagSlots)
        {
            var text = pool[rnd.Next(pool.Length)];
            if (!string.IsNullOrEmpty(text)) slot.Text = text;
        }
        _svc.SaveSettings();
    }

    /// <summary>
    /// 刷新指定标签的所有特殊日期问候语，从预设库中随机选取
    /// </summary>
    void RefreshSpecialByTag(string tag)
    {
        var pools = new System.Collections.Generic.Dictionary<string, string[]>
        {
            ["周一"] = new[] { "周一了，新的一周开始 💪", "星期一，打起精神来 📅", "周一综合症？坚持一下 😅", "新的一周，新的目标 🎯", "周一加油，冲鸭 🚀" },
            ["周二"] = new[] { "周二，渐入佳境 📈", "星期二，状态不错 😊", "周二也要努力呀 💪", "熬过周一，周二轻松点 🎈", "周二快乐，保持节奏 🎵" },
            ["周三"] = new[] { "周三，小周末来了 🎉", "星期三，过半了！📊", "周三加油，胜利在望 🏁", "小周末快乐，放松一下 😌", "周三了，再坚持两天 ✌️" },
            ["周四"] = new[] { "周四，黎明前的黑暗 🌑", "星期四，快解放了 🎊", "周四坚持住，周五在招手 👋", "明天就是周五了！🎈", "周四不松懈，继续加油 🔥" },
            ["周五"] = new[] { "周五了，周末在望！🎉", "星期五，心情飞扬 💃", "最后一天，冲鸭 🚀", "周五快乐，准备迎接周末 🏖️", "坚持到今天，你真棒 👍" },
            ["周六"] = new[] { "周六快乐！🎊", "周末到了，好好休息 🛋️", "星期六，睡到自然醒 😴", "周末愉快，做自己喜欢的事 ❤️", "周六不学习，放松一下 🎮" },
            ["周日"] = new[] { "周日，享受最后假期 🌴", "星期天，准备迎接新周 📅", "今晚有晚修，记得按时到教室 ⏰", "周日了，调整状态 💪", "周末余额不足，且行且珍惜 ⏳" }
        };

        if (!pools.TryGetValue(tag, out var pool)) return;

        var sameTagItems = _svc.Settings.SpecialDateGreetings.Where(s => s.Tag == tag).ToList();
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
    static ToggleSwitch Toggle(bool v, Action<bool> cb) { var t = new ToggleSwitch { IsChecked = v, OnContent = "开", OffContent = "关" }; t.IsCheckedChanged += (a, b) => { cb(t.IsChecked == true); }; return t; }
    static TextBox Num(int v, int min, int max, Action<int> cb) { var t = new TextBox { Text = v.ToString(), Width = 60 }; t.LostFocus += (a, b) => { if (int.TryParse(t.Text, out var n)) { n = Math.Max(min, Math.Min(max, n)); t.Text = n.ToString(); cb(n); } }; return t; }
    static TextBox Tx(string v, int w, Action<string> cb) { var t = new TextBox { Text = v, Width = w }; t.TextChanged += (a, b) => cb(t.Text ?? ""); return t; }
    Button SaveBtn() { var b = new Button { Content = "💾 保存", Padding = new Thickness(20, 8) }; b.Click += (a, e) => { _svc.SaveSettings(); b.Content = "✅ 已保存"; }; return b; }
}
