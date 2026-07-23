using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.Components;

[ComponentInfo(
    "F6A7B8C9-D0E1-2345-F012-123456789015",
    "寒暑假倒计时",
    "fluent(\uE8F3)",
    "显示距离寒暑假的剩余周数和天数"
)]
public partial class VacationCountdownComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private HolidayService? _svc;

    public VacationCountdownComponent()
    {
        InitializeComponent();
        // Main 是 axaml 中的纵向容器，Update() 会清空它。
        // 用横向外层包裹：Main 内容 + 设置入口（设置入口不被清空）
        var wrapper = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        // 先把 wrapper 设为 Content，使 Main 从 UserControl 分离，再安全挂到 wrapper 下
        Content = wrapper;
        wrapper.Children.Add(Main);
        wrapper.Children.Add(ComponentSettingsOpener.CreateSettingsEntry("vacation", "寒暑假倒计时设置"));
        _timer = new DispatcherTimer { Interval = TimeSpan.FromHours(1) };
        _timer.Tick += (s, e) => Update();
        _timer.Start();
        Dispatcher.UIThread.Post(() =>
        {
            _svc = new HolidayService();
            HolidayService.SettingsChanged += OnSettingsChanged;
            Update();
        });
    }

    void OnSettingsChanged()
    {
        _svc?.LoadSettings();
        Dispatcher.UIThread.Post(Update);
    }

    static TextBlock ThemedText(string text, double fontSize = 13, FontWeight weight = FontWeight.Normal,
        HorizontalAlignment hAlign = HorizontalAlignment.Center, VerticalAlignment vAlign = VerticalAlignment.Center)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = weight,
            HorizontalAlignment = hAlign,
            VerticalAlignment = vAlign
        };
        tb[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
        return tb;
    }

    void Update()
    {
        Main.Children.Clear();
        if (_svc == null) return;
        var now = DateTime.Now;
        var s = _svc.Settings;
        var targets = new[] { ("暑假", s.SummerStart, s.SummerEnd), ("寒假", s.WinterStart, s.WinterEnd) };

        // 找出最近的一个假期
        var nearest = targets
            .Select(t =>
            {
                var (name, start, end) = t;
                if (now.Date < start.Date)
                    return new { Name = name, Start = start, End = end, Days = (start.Date - now.Date).Days, IsActive = false };
                else if (now.Date >= start.Date && now.Date <= end.Date)
                    return new { Name = name, Start = start, End = end, Days = (end.Date - now.Date).Days, IsActive = true };
                else
                    return null;
            })
            .Where(x => x != null)
            .OrderBy(x => x!.Days)
            .FirstOrDefault();

        if (nearest != null)
        {
            var weeks = nearest.Days / 7;
            var days = nearest.Days % 7;
            if (nearest.IsActive)
            {
                Main.Children.Add(ThemedText($"{nearest.Name}进行中", weight: FontWeight.SemiBold));
                Main.Children.Add(ThemedText($"剩余 {weeks} 周 {days} 天"));
            }
            else
            {
                var row = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 4,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                row.Children.Add(ThemedText($"距离{nearest.Name}还有", weight: FontWeight.SemiBold, hAlign: HorizontalAlignment.Left));
                row.Children.Add(ThemedText($"{weeks} 周 {days} 天", hAlign: HorizontalAlignment.Left));
                Main.Children.Add(row);
            }
        }
        else
        {
            Main.Children.Add(ThemedText("暂无寒暑假安排"));
        }
    }
}
