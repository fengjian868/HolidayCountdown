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
        services.AddComponent<Views.Components.StudyTimeComponent>();
        services.AddComponent<Views.Components.WeatherGreetingComponent>();
        services.AddComponent<Views.Components.ClassScheduleComponent>();

        // 实验性功能（需要在关于页开启后重启生效）
        var expFile = System.IO.Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "ClassIsland", "Plugins", "HolidayCountdown", "experimental_enabled");
        if (System.IO.File.Exists(expFile))
        {
            services.AddComponent<Views.Components.ExamCountdownComponent>();
            services.AddComponent<Views.Components.WorldClockComponent>();
            services.AddComponent<Views.Components.WeatherReminderComponent>();

            services.AddAction<Automation.Actions.OpenUsbDriveAction>();
            services.AddAction<Automation.Actions.RefreshWeatherAction>();
            services.AddAction<Automation.Actions.RefreshWeatherTextAction>();
        }

        services.AddSettingsPage<Views.SettingsPages.UnifiedSettingsPage>();
    }
}
