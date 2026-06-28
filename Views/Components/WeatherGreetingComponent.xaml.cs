using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.Components;

[ComponentInfo(
    "B2C3D4E5-F6A7-8901-BCDE-F23456789012",
    "天气问候",
    "fluent(\uE753)",
    "根据ClassIsland天气温度显示穿衣提醒，支持预警提示"
)]
public class WeatherGreetingComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private StackPanel _panel = null!;
    private HolidayService? _svc;
    private string _lastWeatherKey = "";

    public WeatherGreetingComponent()
    {
        _panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 2
        };
        Content = _panel;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) }; _timer.Tick += (s, e) => Update(); _timer.Start();
        Dispatcher.UIThread.Post(() => { _svc = new HolidayService(); HolidayService.SettingsChanged += OnSettingsChanged; Update(); });
    }

    void OnSettingsChanged()
    {
        _svc?.LoadSettings();
        Dispatcher.UIThread.Post(Update);
    }

    void Update()
    {
        if (_svc == null) { _panel.Children.Clear(); return; }

        var (temp, weatherCode, weatherText, warnings, updateTime) = GetWeatherData();

        // 保留 key 用于调试
        _lastWeatherKey = $"{temp}|{weatherCode}|{string.Join(",", warnings)}";

        // 优先根据温度给出穿衣提醒
        var greeting = GetTempGreeting(temp);

        // 如果温度提醒为空，回退到天气关键词匹配
        var actualWeatherText = weatherText;
        if (string.IsNullOrEmpty(greeting) && !string.IsNullOrEmpty(weatherCode))
        {
            actualWeatherText = GetWeatherTextByCode(weatherCode);
            greeting = GetWeatherGreeting(actualWeatherText);
        }
        if (string.IsNullOrEmpty(actualWeatherText)) actualWeatherText = GetWeatherTextByCode(weatherCode ?? "");

        // 预警提醒
        var warning = GetWarningText(warnings);
        if (!string.IsNullOrEmpty(warning) && _svc.Settings.WeatherWarningOverride)
        {
            greeting = warning;
            warning = "";
        }

        var template = _svc.Settings.WeatherTemplate ?? "{greeting}";
        var icon = _svc.Settings.WeatherShowIcon ? GetWeatherIcon(actualWeatherText) : "";
        var stale = GetStaleWarning(updateTime);

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["greeting"] = greeting,
            ["temp"] = (_svc.Settings.WeatherShowTemp && temp.HasValue) ? $"{temp.Value}°C" : "",
            ["weather"] = actualWeatherText,
            ["warning"] = warning,
            ["icon"] = icon
        };

        _panel.Children.Clear();
        var baseFontSize = GetClassIslandFontSize();
        var blocks = ParseTemplate(template, values);
        foreach (var block in blocks)
        {
            var tb = new TextBlock
            {
                Text = block.Text,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = block.IsIcon ? baseFontSize + 2 : baseFontSize,
                Opacity = block.IsIcon ? 1.0 : 0.95
            };
            tb[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
            _panel.Children.Add(tb);
        }

        if (!string.IsNullOrEmpty(stale))
        {
            var staleBlock = new TextBlock
            {
                Text = stale,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = baseFontSize - 2,
                Opacity = 0.6
            };
            staleBlock[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
            _panel.Children.Add(staleBlock);
        }
    }

    List<(string Text, bool IsIcon)> ParseTemplate(string template, Dictionary<string, string> values)
    {
        var list = new List<(string Text, bool IsIcon)>();
        int i = 0;
        while (i < template.Length)
        {
            int open = template.IndexOf('{', i);
            if (open < 0)
            {
                var tail = template[i..];
                if (!string.IsNullOrEmpty(tail)) list.Add((tail, false));
                break;
            }
            if (open > i)
            {
                var literal = template[i..open];
                if (!string.IsNullOrEmpty(literal)) list.Add((literal, false));
            }
            int close = template.IndexOf('}', open);
            if (close < 0)
            {
                var tail = template[open..];
                if (!string.IsNullOrEmpty(tail)) list.Add((tail, false));
                break;
            }
            var key = template[(open + 1)..close];
            var value = values.TryGetValue(key, out var v) ? v : "";
            list.Add((value, key.Equals("icon", StringComparison.OrdinalIgnoreCase)));
            i = close + 1;
        }
        // 合并相邻同类型片段，避免多余空白块造成布局问题
        var merged = new List<(string Text, bool IsIcon)>();
        foreach (var item in list)
        {
            if (string.IsNullOrEmpty(item.Text)) continue;
            if (merged.Count > 0 && merged[^1].IsIcon == item.IsIcon)
                merged[^1] = (merged[^1].Text + item.Text, item.IsIcon);
            else
                merged.Add(item);
        }
        return merged;
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

    /// <summary>
    /// 根据天气文本返回对应图标
    /// </summary>
    string GetWeatherIcon(string? weatherText)
    {
        if (string.IsNullOrEmpty(weatherText)) return "";
        var t = weatherText;
        if (t.Contains("晴")) return "☀️";
        if (t.Contains("多云")) return "⛅";
        if (t.Contains("阴")) return "☁️";
        if (t.Contains("雷阵雨")) return "⛈️";
        if (t.Contains("雷")) return "⚡";
        if (t.Contains("雨")) return "🌧️";
        if (t.Contains("雪")) return "❄️";
        if (t.Contains("雾") || t.Contains("霾")) return "🌫️";
        if (t.Contains("风") || t.Contains("沙")) return "🍃";
        if (t.Contains("冰雹")) return "🧊";
        return "🌤️";
    }

    /// <summary>
    /// 天气数据超过半小时未更新则提示
    /// </summary>
    string GetStaleWarning(DateTime? updateTime)
    {
        if (!updateTime.HasValue) return "";
        var elapsed = DateTime.Now - updateTime.Value;
        if (elapsed.TotalMinutes >= 30) return "(天气未刷新)";
        return "";
    }

    /// <summary>
    /// 根据温度给出穿衣提醒（读取用户设置中的温度区间文案）
    /// </summary>
    string GetTempGreeting(double? temp)
    {
        if (temp == null) return "";
        var t = temp.Value;

        // 优先使用用户设置中的温度区间文案
        var items = _svc?.Settings.TempGreetings;
        if (items != null && items.Count > 0)
        {
            var match = items.FirstOrDefault(x => t >= x.MinTemp && t <= x.MaxTemp);
            if (match != null) return match.Text;
        }

        // 回退到默认硬编码
        return t switch
        {
            >= 35 => "高温预警，注意防暑 \uD83C\uDF21️",
            >= 30 => "很热，穿短袖注意防晒 ☀️",
            >= 25 => "较热，短袖即可 \uD83D\uDC55",
            >= 20 => "舒适，薄长袖或短袖 \uD83C\uDF43",
            >= 15 => "微凉，建议穿外套 \uD83E\uDDE5",
            >= 10 => "较冷，穿厚外套 \uD83E\uDDE3",
            >= 5 => "冷，穿羽绒服或棉衣 ❄️",
            >= 0 => "很冷，注意保暖 \uD83E\uDD76",
            _ => "严寒，多穿点别冻着 \uD83E\uDDCA"
        };
    }

    /// <summary>
    /// 根据天气文本匹配问候语（备用）
    /// </summary>
    string GetWeatherGreeting(string weatherText)
    {
        if (string.IsNullOrEmpty(weatherText)) return "";
        var match = _svc!.Settings.WeatherGreetingItems
            .Where(i => i.Keyword != "默认" && weatherText.Contains(i.Keyword))
            .OrderByDescending(i => i.Keyword.Length)
            .FirstOrDefault();
        var greet = match?.Text ?? "";
        if (string.IsNullOrEmpty(greet))
        {
            var def = _svc.Settings.WeatherGreetingItems.FirstOrDefault(i => i.Keyword == "默认");
            if (def != null) greet = def.Text.Replace("{weather}", weatherText);
        }
        return greet;
    }

    /// <summary>
    /// 根据所有预警类型合并返回一条简短防护提醒（支持同一条标题含多个类型）
    /// </summary>
    string GetWarningText(string[] warnings)
    {
        if (warnings.Length == 0) return "";
        var types = new List<string>();
        foreach (var w in warnings)
            foreach (var type in GetWarningTypes(w))
                if (!string.IsNullOrEmpty(type) && !types.Contains(type))
                    types.Add(type);
        if (types.Count == 0) return "";
        if (types.Count == 1) return GetShortTip(types[0]);
        // 多个预警合并为一条简短提醒
        var typeStr = string.Join("、", types);
        var actions = types.Select(GetShortAction).Distinct();
        return $"⚠️{typeStr}预警，{string.Join("，", actions)}";
    }

    List<string> GetWarningTypes(string w)
    {
        var types = new[] { "道路结冰", "高温", "暴雨", "雷暴", "雷雨", "大风", "雷电", "冰雹", "暴雪", "寒潮", "大雾", "沙尘", "台风", "霜冻", "干旱", "霾" };
        return types.Where(t => w.Contains(t)).ToList();
    }

    string GetShortTip(string type)
    {
        return type switch
        {
            "高温" => "高温预警，注意防暑 \uD83C\uDF21️",
            "暴雨" => "暴雨预警，记得带伞 \uD83C\uDF27️",
            "大风" => "大风预警，注意防风 \uD83D\uDCA8",
            "雷电" or "雷雨" or "雷暴" => "雷电预警，待在室内 \u26A1",
            "冰雹" => "冰雹预警，避免外出 \uD83C\uDF28️",
            "暴雪" => "暴雪预警，注意防滑 \uD83C\uDF28️",
            "寒潮" => "寒潮预警，注意保暖 \uD83E\uDDE3",
            "大雾" => "大雾预警，注意安全 \uD83C\uDF2B️",
            "沙尘" => "沙尘预警，戴口罩 \uD83D\uDE37",
            "台风" => "台风预警，关好门窗 \uD83C\uDF00",
            "霜冻" => "霜冻预警，注意保暖 \u2744️",
            "道路结冰" => "道路结冰，小心行走 \uD83D\uDEA8",
            "干旱" => "干旱预警，节约用水 \uD83D\uDCA7",
            "霾" => "霾预警，戴口罩 \uD83D\uDE37",
            _ => ""
        };
    }

    string GetShortAction(string type)
    {
        return type switch
        {
            "高温" => "注意防暑",
            "暴雨" => "记得带伞",
            "大风" => "注意防风",
            "雷电" or "雷雨" or "雷暴" => "待在室内",
            "冰雹" => "避免外出",
            "暴雪" => "注意防滑",
            "寒潮" => "注意保暖",
            "大雾" => "注意安全",
            "沙尘" => "戴口罩",
            "台风" => "关好门窗",
            "霜冻" => "注意保暖",
            "道路结冰" => "小心行走",
            "干旱" => "节约用水",
            "霾" => "戴口罩",
            _ => "注意防护"
        };
    }

    /// <summary>
    /// 获取天气数据：温度、天气代码、天气文本、预警列表、更新时间
    /// </summary>
    (double? temp, string? weatherCode, string? weatherText, string[] warnings, DateTime? updateTime) GetWeatherData()
    {
        try
        {
            var settings = GetSettingsServiceSettings();
            if (settings == null) return (null, null, null, Array.Empty<string>(), null);

            var lastWeatherInfo = GetPropertyValue(settings, "LastWeatherInfo");
            if (lastWeatherInfo == null) return (null, null, null, Array.Empty<string>(), null);

            // 获取 Current 中的温度、天气代码与文本
            var current = GetPropertyValue(lastWeatherInfo, "Current");
            double? temp = null;
            string? weatherCode = null;
            string? weatherText = null;
            if (current != null)
            {
                var temperature = GetPropertyValue(current, "Temperature");
                if (temperature != null)
                {
                    var tempValue = GetPropertyValue(temperature, "Value")?.ToString();
                    if (double.TryParse(tempValue, out var t)) temp = t;
                }
                weatherCode = GetPropertyValue(current, "Weather")?.ToString();
                weatherText = GetPropertyValue(current, "WeatherText")?.ToString()
                    ?? GetPropertyValue(current, "WeatherDescription")?.ToString()
                    ?? GetPropertyValue(current, "Text")?.ToString();
            }

            // 获取所有预警
            var warnings = GetAllAlertTitles(lastWeatherInfo);

            // 获取天气更新时间
            var updateTime = GetDateTimeProperty(lastWeatherInfo, "UpdateTime")
                ?? GetDateTimeProperty(lastWeatherInfo, "FetchTime")
                ?? GetDateTimeProperty(lastWeatherInfo, "LastUpdateTime")
                ?? GetDateTimeProperty(lastWeatherInfo, "UpdatedTime");

            return (temp, weatherCode, weatherText, warnings, updateTime);
        }
        catch { return (null, null, null, Array.Empty<string>(), null); }
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

    object? GetPropertyValue(object obj, string propName)
    {
        try
        {
            var prop = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            return prop?.GetValue(obj);
        }
        catch { return null; }
    }

    /// <summary>
    /// 获取所有预警标题
    /// </summary>
    string[] GetAllAlertTitles(object lastWeatherInfo)
    {
        try
        {
            var alerts = GetPropertyValue(lastWeatherInfo, "Alerts");
            if (alerts == null) return Array.Empty<string>();

            var countProp = alerts.GetType().GetProperty("Count");
            var count = (int?)countProp?.GetValue(alerts) ?? 0;
            if (count == 0) return Array.Empty<string>();

            var result = new List<string>();
            // 尝试用索引器获取
            var indexer = alerts.GetType().GetProperties()
                .FirstOrDefault(p => p.GetIndexParameters().Length == 1);
            if (indexer != null)
            {
                for (int i = 0; i < count; i++)
                {
                    var alert = indexer.GetValue(alerts, new object[] { i });
                    if (alert != null)
                    {
                        var titleProp = alert.GetType().GetProperty("Title", BindingFlags.Public | BindingFlags.Instance);
                        var title = titleProp?.GetValue(alert)?.ToString();
                        if (!string.IsNullOrEmpty(title)) result.Add(title);
                    }
                }
            }
            return result.ToArray();
        }
        catch { return Array.Empty<string>(); }
    }

    string GetWeatherTextByCode(string code)
    {
        if (string.IsNullOrEmpty(code)) return "";
        try
        {
            var appHostType = Type.GetType("ClassIsland.Shared.IAppHost, ClassIsland.Shared")
                ?? Type.GetType("ClassIsland.Shared.IAppHost, ClassIsland.Core")
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == "IAppHost");

            if (appHostType == null) return "";

            var tryGetService = appHostType.GetMethod("TryGetService", BindingFlags.Public | BindingFlags.Static);
            if (tryGetService == null || !tryGetService.IsGenericMethodDefinition) return "";

            var weatherServiceType = Type.GetType("ClassIsland.Core.Abstractions.Services.IWeatherService, ClassIsland.Core")
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == "IWeatherService");

            if (weatherServiceType == null) return "";

            var genericMethod = tryGetService.MakeGenericMethod(weatherServiceType);
            var weatherService = genericMethod.Invoke(null, null);
            if (weatherService == null) return "";

            var getWeatherText = weatherServiceType.GetMethod("GetWeatherTextByCode", BindingFlags.Public | BindingFlags.Instance);
            if (getWeatherText == null) return "";

            return getWeatherText.Invoke(weatherService, new object[] { code })?.ToString() ?? "";
        }
        catch { return ""; }
    }
}
