using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.SettingsPages;

[SettingsPageInfo("holidaycountdown.lunar", "农历设置", "\uE787", "\uE787")]
public class LunarSettingsPage : SettingsPageBase
{
    private readonly HolidayService _svc;
    public LunarSettingsPage() { _svc = new HolidayService(); Content = Build(); }

    Control Build()
    {
        var s = new StackPanel { Spacing = 0, Margin = new Thickness(20, 16) };
        s.Children.Add(SettingsUI.PageHeader("🌙 农历日期设置"));

        // 显示
        var displayPanel = new StackPanel { Spacing = 0 };
        displayPanel.Children.Add(SettingsUI.SettingItem("显示农历", null,
            SettingsUI.Toggle(_svc.Settings.ShowLunarDate, v => _svc.Settings.ShowLunarDate = v)));
        displayPanel.Children.Add(SettingsUI.Separator());
        displayPanel.Children.Add(SettingsUI.SettingItem("自动网络刷新", "有网络时自动获取最新农历",
            SettingsUI.Toggle(_svc.Settings.LunarAutoRefresh, v => _svc.Settings.LunarAutoRefresh = v)));
        s.Children.Add(SettingsUI.Expander("显示", "农历组件基础设置", displayPanel));

        // 显示格式
        var formatPanel = new StackPanel { Spacing = 0 };
        var presets = new[]
        {
            ("完整", "{gzYear} {IMonthCn}{IDayCn} {Animal}"),
            ("简洁", "{IMonthCn}{IDayCn}"),
            ("含生肖", "{IMonthCn}{IDayCn} {Animal}"),
            ("含节气", "{IMonthCn}{IDayCn} {Term}"),
        };
        var presetCombo = new ComboBox { Width = 120, HorizontalAlignment = HorizontalAlignment.Right };
        foreach (var (name, _) in presets) presetCombo.Items.Add(name);

        var currentTemplate = _svc.Settings.LunarDateTemplate ?? "{gzYear} {IMonthCn}{IDayCn} {Animal}";
        int selectedIndex = 0;
        for (int i = 0; i < presets.Length; i++)
        {
            if (presets[i].Item2 == currentTemplate) { selectedIndex = i; break; }
        }
        presetCombo.SelectedIndex = selectedIndex;
        presetCombo.SelectionChanged += (a, b) =>
        {
            if (presetCombo.SelectedIndex >= 0 && presetCombo.SelectedIndex < presets.Length)
                _svc.Settings.LunarDateTemplate = presets[presetCombo.SelectedIndex].Item2;
        };

        formatPanel.Children.Add(SettingsUI.SettingItem("选择格式", "快速选择预设模板", presetCombo));
        formatPanel.Children.Add(SettingsUI.Separator());

        var templateBox = SettingsUI.Text(_svc.Settings.LunarDateTemplate ?? "", 280, v => _svc.Settings.LunarDateTemplate = v);
        formatPanel.Children.Add(SettingsUI.SettingItem("自定义模板", null, templateBox));
        formatPanel.Children.Add(SettingsUI.Separator());
        formatPanel.Children.Add(SettingsUI.Info("可用变量: {gzYear} 干支年 | {IMonthCn} 农历月 | {IDayCn} 农历日 | {Animal} 生肖 | {Term} 节气"));
        s.Children.Add(SettingsUI.Expander("显示格式", "农历日期显示模板", formatPanel));

        s.Children.Add(SettingsUI.Info("示例: 癸卯年 九月初八 兔"));
        s.Children.Add(SettingsUI.SaveButton(() => _svc.SaveSettings()));
        return new ScrollViewer { Content = s };
    }
}
