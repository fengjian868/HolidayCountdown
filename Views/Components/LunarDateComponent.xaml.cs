using System;
using System.IO;
using System.Net.Http;
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
    private readonly string _cache;

    public LunarDateComponent()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClassIsland", "Plugins", "HolidayCountdown");
        Directory.CreateDirectory(dir); _cache = Path.Combine(dir, "lunar_cache.json");
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        _txt = new TextBlock { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Opacity = 0.85 };
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
        if (_svc == null || !_svc.Settings.ShowLunarDate) { UpdateText(""); return; }
        try
        {
            if (File.Exists(_cache))
            {
                var c = JsonSerializer.Deserialize<LunarInfo>(File.ReadAllText(_cache));
                if (c != null && c.Date == DateTime.Now.Date) { UpdateText(Format(c)); return; }
            }
        }
        catch { }
        if (!_svc.Settings.LunarAutoRefresh) { UpdateText("农历获取失败"); return; }
        try
        {
            using var cl = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var r = await cl.GetStringAsync($"https://api.mu-jie.cc/lunar?date={DateTime.Now:yyyy-MM-dd}");
            using var doc = JsonDocument.Parse(r);
            var root = doc.RootElement;
            if (root.TryGetProperty("code", out var cp) && cp.GetInt32() == 200 && root.TryGetProperty("data", out var dp))
            {
                var info = new LunarInfo { Date = DateTime.Now.Date, gzYear = GetStr(dp, "gzYear") + "年", IMonthCn = GetStr(dp, "IMonthCn"), IDayCn = GetStr(dp, "IDayCn"), Animal = GetStr(dp, "Animal"), Term = GetStr(dp, "Term"), lunarDate = GetStr(dp, "lunarDate") };
                File.WriteAllText(_cache, JsonSerializer.Serialize(info)); UpdateText(Format(info)); return;
            }
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
