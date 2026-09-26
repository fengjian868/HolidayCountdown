using System;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;

namespace HolidayCountdown.Automation.Actions;

/// <summary>
/// 在指定区间内随机等待一段时间后继续。
/// </summary>
[ActionInfo(
    "holidaycountdown.action.randomDelay",
    "区间随机等待",
    "\uE9F5",
    defaultGroupToMenu: "HolidayCountdown"
)]
public class RandomDelayAction : ActionBase<RandomDelaySettings>
{
    protected override async Task OnInvoke()
    {
        await base.OnInvoke();
        var min = Math.Max(0, Settings.MinSeconds);
        var max = Math.Max(min, Settings.MaxSeconds);
        var delaySec = new Random().Next(min, max + 1);
        await Task.Delay(TimeSpan.FromSeconds(delaySec));
    }
}

public class RandomDelaySettings
{
    public int MinSeconds { get; set; } = 1;
    public int MaxSeconds { get; set; } = 10;
}
