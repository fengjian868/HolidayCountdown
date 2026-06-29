using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;

namespace HolidayCountdown.Automation.Actions;

/// <summary>
/// 立即刷新 ClassIsland 天气数据。
/// </summary>
[ActionInfo(
    "holidaycountdown.action.refreshWeather",
    "刷新天气",
    "fluent(\ue753)",
    defaultGroupToMenu: "HolidayCountdown"
)]
public class RefreshWeatherAction : ActionBase
{
    protected override async Task OnInvoke()
    {
        await base.OnInvoke();
        try
        {
            var weatherService = IAppHost.TryGetService<IWeatherService>();
            if (weatherService != null)
                await weatherService.QueryWeatherAsync();
        }
        catch
        {
            // 忽略刷新失败
        }
    }
}
