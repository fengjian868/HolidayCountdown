using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Extensions.Registry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HolidayCountdown;

[PluginEntrance]
public class Plugin : PluginBase
{
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        services.AddComponent<Views.Components.HolidayCountdownComponent>();
        services.AddComponent<Views.Components.GreetingComponent>();
        services.AddComponent<Views.Components.SolarTermComponent>();
        services.AddComponent<Views.Components.LunarDateComponent>();
        services.AddComponent<Views.Components.CustomHolidayComponent>();
        services.AddComponent<Views.Components.VacationCountdownComponent>();
        services.AddComponent<Views.Components.WeatherGreetingComponent>();
        services.AddComponent<Views.Components.ClassScheduleComponent>();
        services.AddComponent<Views.Components.StudyTimeComponent>();

        services.AddSettingsPage<Views.SettingsPages.UnifiedSettingsPage>();
    }
}
