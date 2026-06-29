using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using HolidayCountdown.Services;
using HolidayCountdown.WeatherReminders;

namespace HolidayCountdown.Views.Components;

[ComponentInfo(
    "D4E5F6A7-B8C9-0123-DEF1-2345678901AB",
    "天气变化提醒[测试版]",
    "fluent(\uE753)",
    "根据未来数日天气生成降温、升温、降水、雷电等提醒"
)]
public class WeatherReminderComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private TextBlock _txt = null!;
    private HolidayService? _svc;
    private WeatherReminderEvaluator? _evaluator;
    private IReadOnlyList<WeatherReminderResult> _lastResults = new List<WeatherReminderResult>();

    public WeatherReminderComponent()
    {
        _txt = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.NoWrap,
            Opacity = 0.9
        };
        Content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Children = { _txt }
        };

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(10) };
        _timer.Tick += (s, e) => Update();
        _timer.Start();

        Dispatcher.UIThread.Post(() =>
        {
            _svc = new HolidayService();
            _evaluator = new WeatherReminderEvaluator(_svc);
            HolidayService.SettingsChanged += OnSettingsChanged;
            UpdateTimerInterval();
            Update();
        });
    }

    void OnSettingsChanged()
    {
        _svc?.LoadSettings();
        Dispatcher.UIThread.Post(() => { UpdateTimerInterval(); Update(); });
    }

    void UpdateTimerInterval()
    {
        if (_svc == null) return;
        var minutes = _svc.Settings.WeatherReminderRefreshMinutes;
        if (minutes < 1) minutes = 10;
        // 启用“变化时立即刷新”后，使用 1 分钟间隔快速响应天气变化
        if (_svc.Settings.WeatherReminderShowImmediatelyOnChange && minutes > 1)
            minutes = 1;
        _timer.Interval = TimeSpan.FromMinutes(minutes);
    }

    double GetClassIslandFontSize()
    {
        try
        {
            var settings = GetSettingsServiceSettings();
            if (settings == null) return 14;
            var value = GetPropertyValue(settings, "MainWindowBodyFontSize");
            if (value is double d) return d;
            if (value is float f) return f;
            if (value != null && double.TryParse(value.ToString(), out var parsed)) return parsed;
        }
        catch { }
        return 14;
    }

    void Update()
    {
        if (_svc == null || _evaluator == null)
        {
            _txt.Text = "";
            return;
        }

        if (!_svc.Settings.WeatherReminderEnabled)
        {
            _txt.Text = "";
            return;
        }

        try
        {
            var context = BuildContext();
            context.LastResults = _lastResults;

            var results = _evaluator.Evaluate(context);

            if (results.Count == 0)
            {
                // 数据未刷新或不可用时给出占位提示，方便排查
                if (context.UpdateTime == null || (DateTime.Now - context.UpdateTime.Value).TotalMinutes >= 30)
                    _txt.Text = "天气未更新";
                else
                    _txt.Text = "暂无天气变化提醒";
                _txt.FontSize = GetClassIslandFontSize();
                _lastResults = results;
                return;
            }

            _txt.Text = string.Join("  ·  ", results.Select(r => $"{r.Icon} {r.Text}"));
            _txt.FontSize = GetClassIslandFontSize();

            _lastResults = results;
        }
        catch
        {
            _txt.Text = "";
        }
    }

    WeatherReminderContext BuildContext()
    {
        var context = new WeatherReminderContext();
        var data = GetWeatherData();
        if (data == null)
        {
            context.WeatherText = "";
            return context;
        }

        context.CurrentTemp = WeatherDataHelper.GetCurrentTemp(data);

        var current = GetPropertyValue(data, "Current");
        if (current != null)
        {
            context.WeatherCode = GetPropertyValue(current, "Weather")?.ToString();
            context.WeatherText = GetPropertyValue(current, "WeatherText")?.ToString()
                ?? GetPropertyValue(current, "WeatherDescription")?.ToString()
                ?? GetPropertyValue(current, "Text")?.ToString();
        }

        // 传完整 WeatherInfo 对象，规则内部通过反射读取 ForecastHourly / ForecastDaily
        context.WeatherInfo = data;
        context.Alerts = GetPropertyValue(data, "Alerts") as IList;
        context.UpdateTime = GetDateTimeProperty(data, "UpdateTime")
            ?? GetDateTimeProperty(data, "FetchTime")
            ?? GetDateTimeProperty(data, "LastUpdateTime")
            ?? GetDateTimeProperty(data, "UpdatedTime");
        context.Now = DateTime.Now;

        return context;
    }

    object? GetWeatherData()
    {
        try
        {
            var settings = GetSettingsServiceSettings();
            if (settings == null) return null;
            return GetPropertyValue(settings, "LastWeatherInfo");
        }
        catch { return null; }
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

    DateTime? GetDateTimeProperty(object obj, string propName)
    {
        try
        {
            var prop = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            if (prop == null) return null;
            var value = prop.GetValue(obj);
            if (value is DateTime dt) return dt;
            if (value is DateTimeOffset dto) return dto.DateTime;
            if (DateTime.TryParse(value?.ToString(), out var parsed)) return parsed;
            return null;
        }
        catch { return null; }
    }
}
