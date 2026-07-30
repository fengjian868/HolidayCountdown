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
        services.AddComponent<Views.Components.LunarDateComponent, Views.ComponentSettings.LunarDateComponentSettings>();
        services.AddComponent<Views.Components.CustomHolidayComponent, Views.ComponentSettings.CustomHolidayComponentSettings>();
        services.AddComponent<Views.Components.VacationCountdownComponent, Views.ComponentSettings.VacationCountdownComponentSettings>();
        services.AddComponent<Views.Components.StudyTimeComponent, Views.ComponentSettings.StudyTimeComponentSettings>();
        services.AddComponent<Views.Components.WeatherGreetingComponent>();
        services.AddComponent<Views.Components.SmartWeatherComponent>();
        services.AddComponent<Views.Components.ExamCountdownComponent, Views.ComponentSettings.ExamCountdownComponentSettings>();
        services.AddComponent<Views.Components.WorldClockComponent, Views.ComponentSettings.WorldClockComponentSettings>();

        services.AddAction<Automation.Actions.OpenUsbDriveAction>();
        services.AddAction<Automation.Actions.RefreshWeatherAction>();
        services.AddAction<Automation.Actions.RefreshWeatherTextAction>();

        // 测试版功能（需要在关于页开启实验性功能后重启生效）
        var expFile = System.IO.Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "ClassIsland", "Plugins", "HolidayCountdown", "experimental_enabled");
        if (System.IO.File.Exists(expFile))
        {
            services.AddComponent<Views.Components.ClassScheduleComponent>();
            services.AddComponent<Views.Components.WeatherReminderComponent>();
        }

        services.AddSettingsPage<Views.SettingsPages.UnifiedSettingsPage>();
    }
}
