using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using HolidayCountdown.Services;

namespace HolidayCountdown.Automation.Actions;

/// <summary>
/// 刷新插件中所有天气相关文案（天气关键词问候、温度区间问候）。
/// </summary>
[ActionInfo(
    "holidaycountdown.action.refreshWeatherText",
    "刷新天气文案",
    "\ue895",
    defaultGroupToMenu: "HolidayCountdown"
)]
public class RefreshWeatherTextAction : ActionBase
{
    protected override async Task OnInvoke()
    {
        await base.OnInvoke();
        try
        {
            var svc = new HolidayService();
            svc.RefreshAllWeatherGreetings();
            svc.RefreshAllTempGreetings();
        }
        catch
        {
            // 忽略刷新失败
        }
    }
}
