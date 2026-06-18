using System;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using HolidayCountdown.Models;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.Components;

[ComponentInfo(
    "D4E5F6A7-B8C9-0123-DEF0-123456789013",
    "农历日期",
    "\uE8C0",
    "显示当前农历日期，支持自定义模板，有网络时自动刷新"
)]
public class LunarDateComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private TextBlock _txt = null!;
    private HolidayService? _svc;

    public LunarDateComponent()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        _txt = new TextBlock { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Opacity = 0.85, Foreground = Brushes.Black };
        panel.Children.Add(new TextBlock { Text = "\u2630", FontSize = 14, VerticalAlignment = VerticalAlignment.Center, Opacity = 0.7 });
        panel.Children.Add(_txt);
        Content = panel;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(30) }; _timer.Tick += (s, e) => _ = RefreshAsync(); _timer.Start();
        Dispatcher.UIThread.Post(() => { _svc = new HolidayService(); HolidayService.SettingsChanged += OnSettingsChanged; _ = RefreshAsync(); });
    }

    void OnSettingsChanged()
    {
        _svc?.LoadSettings();
        Dispatcher.UIThread.Post(() => _ = RefreshAsync());
    }

    async Task RefreshAsync()
    {
        if (_svc == null) { UpdateText(""); return; }
        try
        {
            // 使用 HolidayService 的月度缓存，每日自动刷新，避免每次联网
            var info = await _svc.GetLunarAsync();
            if (info != null) { UpdateText(Format(info)); return; }
        }
        catch { }
        UpdateText("农历获取失败");
    }

    string GetStr(JsonElement e, string p) => e.TryGetProperty(p, out var v) ? (v.GetString() ?? "") : "";
    string Format(LunarInfo i)
    {
        var t = _svc?.Settings.LunarDateTemplate ?? "{gzYear} {IMonthCn}{IDayCn} {Animal}";
        var result = t.Replace("{gzYear}", i.gzYear).Replace("{IMonthCn}", i.IMonthCn).Replace("{IDayCn}", i.IDayCn).Replace("{Animal}", i.Animal).Replace("{Term}", string.IsNullOrEmpty(i.Term) ? "" : $" · {i.Term}").Replace("{lunarDate}", i.lunarDate);
        // 清理多余空格，让排版更紧凑
        while (result.Contains("  ")) result = result.Replace("  ", " ");
        return result.Trim();
    }
    void UpdateText(string t) => Dispatcher.UIThread.Post(() => _txt.Text = t);
}
