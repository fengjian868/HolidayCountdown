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
        services.AddComponent<Views.Components.SolarTermComponent, Views.ComponentSettings.SolarTermSettingsControl>();
        services.AddComponent<Views.Components.LunarDateComponent, Views.ComponentSettings.LunarDateSettingsControl>();
        services.AddComponent<Views.Components.CustomHolidayComponent, Views.ComponentSettings.CustomHolidaySettingsControl>();
        services.AddComponent<Views.Components.VacationCountdownComponent, Views.ComponentSettings.VacationSettingsControl>();
        services.AddComponent<Views.Components.StudyTimeComponent, Views.ComponentSettings.StudyTimeSettingsControl>();
        services.AddComponent<Views.Components.WeatherGreetingComponent>();
        services.AddComponent<Views.Components.SmartWeatherComponent>();
        services.AddComponent<Views.Components.ClassScheduleComponent>();
        services.AddComponent<Views.Components.ExamCountdownComponent, Views.ComponentSettings.ExamCountdownSettingsControl>();
        services.AddComponent<Views.Components.WorldClockComponent, Views.ComponentSettings.WorldClockSettingsControl>();
        services.AddComponent<Views.Components.WeatherReminderComponent>();

        services.AddAction<Automation.Actions.OpenUsbDriveAction>();
        services.AddAction<Automation.Actions.RefreshWeatherAction>();
        services.AddAction<Automation.Actions.RefreshWeatherTextAction>();

        services.AddSettingsPage<Views.SettingsPages.UnifiedSettingsPage>();
    }
}
