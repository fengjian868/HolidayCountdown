using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;

namespace HolidayCountdown.Automation.Triggers;

/// <summary>
/// 当检测到天气预警信号时触发。
/// </summary>
[TriggerInfo(
    "holidaycountdown.trigger.weatherAlert",
    "有天气预警信号时",
    "\uE7BA"
)]
public class WeatherAlertTrigger : TriggerBase
{
    private Timer? _timer;
    private int _lastAlertCount = -1;
    private bool _loaded;

    public override void Loaded()
    {
        if (_loaded) return;
        _loaded = true;
        _timer = new Timer(Check, null, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1));
    }

    public override void UnLoaded()
    {
        if (!_loaded) return;
        _loaded = false;
        _timer?.Dispose();
        _timer = null;
    }

    void Check(object? state)
    {
        try
        {
            var count = GetCurrentAlertCount();
            if (_lastAlertCount < 0)
            {
                _lastAlertCount = count;
                return;
            }
            // 预警数量增加时触发
            if (count > _lastAlertCount)
                Trigger();
            _lastAlertCount = count;
        }
        catch { }
    }

    int GetCurrentAlertCount()
    {
        var settings = GetSettingsServiceSettings();
        if (settings == null) return 0;
        var lastWeatherInfo = GetPropertyValue(settings, "LastWeatherInfo");
        if (lastWeatherInfo == null) return 0;

        var alerts = GetPropertyValue(lastWeatherInfo, "Alerts");
        if (alerts == null) return 0;

        if (alerts is IList list)
            return list.Count;

        var countProp = alerts.GetType().GetProperty("Count");
        return countProp != null ? (int?)countProp.GetValue(alerts) ?? 0 : 0;
    }

    object? GetSettingsServiceSettings()
    {
        try
        {
            var appHostType = Type.GetType("ClassIsland.Shared.IAppHost, ClassIsland.Shared")
                ?? Type.GetType("ClassIsland.Shared.IAppHost, ClassIsland.Core")
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == "IAppHost");
            if (appHostType == null) return null;

            var tryGetService = appHostType.GetMethod("TryGetService", BindingFlags.Public | BindingFlags.Static);
            if (tryGetService == null || !tryGetService.IsGenericMethodDefinition) return null;

            var settingsServiceType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == "SettingsService");
            if (settingsServiceType == null) return null;

            var genericMethod = tryGetService.MakeGenericMethod(settingsServiceType);
            var settingsService = genericMethod.Invoke(null, null);
            if (settingsService == null) return null;

            var settingsProp = settingsServiceType.GetProperty("Settings", BindingFlags.Public | BindingFlags.Instance);
            return settingsProp?.GetValue(settingsService);
        }
        catch { return null; }
    }

    object? GetPropertyValue(object? obj, string propName)
    {
        if (obj == null) return null;
        try
        {
            var prop = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            return prop?.GetValue(obj);
        }
        catch { return null; }
    }
}
