using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.Components;

[ComponentInfo(
    "A1B2C3D4-E5F6-7890-ABCD-EF1234567890",
    "节假日倒计时",
    "\uE8F5",
    "显示距离最近节假日的倒计时天数，支持网络同步"
)]
public class HolidayCountdownComponent : ComponentBase
{
    private readonly HolidayService _holidayService;
    private readonly DispatcherTimer _timer;
    private readonly TextBlock _iconText;
    private readonly TextBlock _holidayNameText;
    private readonly TextBlock _countdownText;
    private readonly TextBlock _sourceText;

    public HolidayCountdownComponent()
    {
        _holidayService = new HolidayService();

        var stack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _iconText = new TextBlock
        {
            FontFamily = new FontFamily("Segoe UI Symbol"),
            FontSize = 18,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0),
            Text = "\uE8F5"
        };

        _holidayNameText = new TextBlock
        {
            FontSize = 14,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };

        var separator = new TextBlock
        {
            Text = " · ",
            FontSize = 13,
            Opacity = 0.6,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 4, 0)
        };

        _countdownText = new TextBlock
        {
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center
        };

        _sourceText = new TextBlock
        {
            FontSize = 10,
            Opacity = 0.4,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(6, 0, 0, 0)
        };

        stack.Children.Add(_iconText);
        stack.Children.Add(_holidayNameText);
        stack.Children.Add(separator);
        stack.Children.Add(_countdownText);
        stack.Children.Add(_sourceText);

        Content = stack;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _timer.Tick += (s, e) => UpdateDisplay();
        _timer.Start();
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        var nextHoliday = _holidayService.GetNextHoliday();
        if (nextHoliday == null)
        {
            _holidayNameText.Text = "暂无节假日";
            _countdownText.Text = "";
            _sourceText.Text = "";
            return;
        }

        var days = (int)(nextHoliday.Date.Date - DateTime.Now.Date).TotalDays;
        _holidayNameText.Text = nextHoliday.Name;

        if (days > 0)
        {
            _countdownText.Text = $"还有 {days} 天";
            _iconText.Text = "\uE8F5";
        }
        else if (days == 0)
        {
            _countdownText.Text = "就是今天！";
            _iconText.Text = "\uE8E8";
            _holidayNameText.FontWeight = FontWeight.Bold;
        }
        else
        {
            _countdownText.Text = "已结束";
        }

        _sourceText.Text = _holidayService.IsNetworkLoaded ? "(网络)" : "(本地)";
    }
}