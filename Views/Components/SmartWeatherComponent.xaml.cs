using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.Components;

[ComponentInfo(
    "A1B2C3D4-E5F6-7890-1234-567890ABCDEF",
    "智能天气[测试版]",
    "\uE4FB",
    "比原版更美观的天气组件，含彩色图标与预警"
)]
public class SmartWeatherComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private StackPanel _root = null!;
    private HolidayService? _svc;

    public SmartWeatherComponent()
    {
        _root = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        Content = _root;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _timer.Tick += (s, e) => Update();
        _timer.Start();

        Dispatcher.UIThread.Post(() =>
        {
            _svc = new HolidayService();
            HolidayService.SettingsChanged += OnSettingsChanged;
            Update();
        });
    }

    void OnSettingsChanged()
    {
        _svc?.LoadSettings();
        Dispatcher.UIThread.Post(Update);
    }

    void Update()
    {
        _root.Children.Clear();
        if (_svc == null) return;

        try
        {
            var data = GetWeatherData();
            var vars = BuildVariables(data);
            Render(vars);
        }
        catch
        {
            _root.Children.Add(Badge("天气数据不可用", null, null));
        }
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
    /// 集中生成所有模板变量 A/B/C/D/E/F
    /// </summary>
    SmartWeatherVariables BuildVariables(WeatherData data)
    {
        var vars = new SmartWeatherVariables();

        // A: 温度
        vars.A = data.Temp.HasValue ? $"{data.Temp.Value}°C" : "";
        vars.AColor = _svc?.Settings.SmartWeatherTempColorEnabled == true
            ? GetTempColor(data.Temp)
            : null;

        // B: 天气图标
        var (icon, iconColor) = GetWeatherIconAndColor(data.WeatherText, data.WeatherCode);
        vars.B = icon;
        vars.BColor = iconColor;

        // F: 天气状况文本（晴/雨/阴等）
        var weatherText = data.WeatherText;
        if (string.IsNullOrEmpty(weatherText))
            weatherText = GetWeatherTextByCode(data.WeatherCode ?? "");
        vars.F = weatherText ?? "";

        // C: 预警信息（支持同一条标题中包含多个类型，如“雷雨大风”）
        vars.C = data.Warnings.Length > 0
            ? string.Join(" ", data.Warnings.Select(GetWarningBadgeText))
            : "";
        vars.CWarnings = data.Warnings
            .SelectMany(ParseWarnings)
            .GroupBy(w => w.Type)
            .Select(g => g.First())
            .ToList();

        // D: 穿衣/出行提醒
        vars.D = GetReminder(data);

        // E: 更新时间/状态
        vars.E = GetStaleWarning(data.UpdateTime);

        return vars;
    }

    /// <summary>
    /// 按模板与用户开关渲染徽章
    /// </summary>
    void Render(SmartWeatherVariables vars)
    {
        var template = _svc?.Settings.SmartWeatherTemplate ?? "{B} {F} {A} {D}";
        var showMap = new Dictionary<string, bool>
        {
            ["A"] = _svc?.Settings.SmartWeatherShowA ?? true,
            ["B"] = _svc?.Settings.SmartWeatherShowB ?? true,
            ["C"] = _svc?.Settings.SmartWeatherShowC ?? true,
            ["D"] = _svc?.Settings.SmartWeatherShowD ?? true,
            ["E"] = _svc?.Settings.SmartWeatherShowE ?? false,
            ["F"] = _svc?.Settings.SmartWeatherShowF ?? true,
        };

        // 预警置顶：有预警且开启显示{C}时，把 {C} 放在最前面完整显示
        if (vars.CWarnings.Count > 0 && (_svc?.Settings.SmartWeatherShowC ?? true) && (_svc?.Settings.SmartWeatherWarningOverride ?? true))
        {
            foreach (var w in vars.CWarnings)
                _root.Children.Add(WarningBadge(w));
        }

        // 按模板顺序渲染其余变量
        var matches = Regex.Matches(template, @"\{([A-F])\}");
        foreach (Match m in matches)
        {
            var key = m.Groups[1].Value;
            if (!showMap.GetValueOrDefault(key, true)) continue;
            if (key == "C" && vars.CWarnings.Count > 0 && (_svc?.Settings.SmartWeatherShowC ?? true) && (_svc?.Settings.SmartWeatherWarningOverride ?? true)) continue;

            var control = key switch
            {
                "A" => Badge(vars.A, vars.AColor, null),
                "B" => Badge(vars.B, vars.BColor, null),
                "C" => WarningList(vars.CWarnings),
                "D" => Badge(vars.D, null, null),
                "E" => Badge(vars.E, null, Brushes.Gray),
                "F" => Badge(vars.F, null, null),
                _ => null
            };
            if (control != null) _root.Children.Add(control);
        }

        if (_root.Children.Count == 0)
            _root.Children.Add(Badge("无天气信息", null, null));
    }

    /// <summary>
    /// 普通文本徽章
    /// </summary>
    Control Badge(string text, IBrush? foreground, IBrush? background)
    {
        var baseFontSize = GetClassIslandFontSize();
        var tb = new TextBlock
        {
            Text = text,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = baseFontSize
        };
        if (foreground != null) tb.Foreground = foreground;

        if (background == null) return tb;

        return new Border
        {
            Background = background,
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(4, 1),
            Child = tb
        };
    }

    /// <summary>
    /// 预警徽章列表
    /// </summary>
    Control WarningList(List<WarningInfo> warnings)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center
        };
        foreach (var w in warnings)
            panel.Children.Add(WarningBadge(w));
        return panel;
    }

    /// <summary>
    /// 单个预警徽章
    /// </summary>
    Control WarningBadge(WarningInfo w)
    {
        var (bg, fg) = GetWarningColors(w.Level);
        var text = $"{w.Icon} {w.Type} {w.LevelText}";
        var baseFontSize = GetClassIslandFontSize();
        var tb = new TextBlock
        {
            Text = text,
            FontSize = baseFontSize - 2,
            Foreground = fg,
            VerticalAlignment = VerticalAlignment.Center
        };
        return new Border
        {
            Background = bg,
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(5, 2),
            Child = tb
        };
    }

    string GetReminder(WeatherData data)
    {
        // 优先根据温度
        if (data.Temp.HasValue && _svc?.Settings.TempGreetings.Count > 0)
        {
            var match = _svc.Settings.TempGreetings
                .FirstOrDefault(x => data.Temp.Value >= x.MinTemp && data.Temp.Value <= x.MaxTemp);
            if (match != null && !string.IsNullOrEmpty(match.Text)) return match.Text;
        }

        if (data.Temp.HasValue)
        {
            var t = data.Temp.Value;
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

        // 回退到天气关键词
        if (!string.IsNullOrEmpty(data.WeatherText))
        {
            var text = GetWeatherTextByCode(data.WeatherCode ?? "");
            if (string.IsNullOrEmpty(text)) text = data.WeatherText;
            var match = _svc?.Settings.WeatherGreetingItems
                .Where(i => i.Keyword != "默认" && text.Contains(i.Keyword))
                .OrderByDescending(i => i.Keyword.Length)
                .FirstOrDefault();
            var greet = match?.Text ?? "";
            if (string.IsNullOrEmpty(greet))
            {
                var def = _svc?.Settings.WeatherGreetingItems.FirstOrDefault(i => i.Keyword == "默认");
                if (def != null) greet = def.Text.Replace("{weather}", text);
            }
            return greet;
        }

        return "";
    }

    #region Weather Data Reading

    WeatherData GetWeatherData()
    {
        var settings = GetSettingsServiceSettings();
        if (settings == null) return new WeatherData();

        var lastWeatherInfo = GetPropertyValue(settings, "LastWeatherInfo");
        if (lastWeatherInfo == null) return new WeatherData();

        double? temp = null;
        string? weatherCode = null;
        string? weatherText = null;

        var current = GetPropertyValue(lastWeatherInfo, "Current");
        if (current != null)
        {
            // CurrentWeather.Temperature 是 ValueUnitPair,Value 是 string
            var temperature = GetPropertyValue(current, "Temperature");
            if (temperature != null)
            {
                var tempValue = GetPropertyValue(temperature, "Value")?.ToString();
                if (double.TryParse(tempValue, out var t)) temp = t;
            }
            // CurrentWeather 只有 Weather 字段(天气代码 string),没有 WeatherText/Description/Text
            // 文字由 IWeatherService.GetWeatherTextByCode(code) 提供
            weatherCode = GetPropertyValue(current, "Weather")?.ToString();
        }

        var warnings = GetAllAlertTitles(lastWeatherInfo);
        // WeatherInfo 仅提供解析属性 UpdateTime (由 UpdateTimeUnix 转换),无其他时间字段
        var updateTime = GetDateTimeProperty(lastWeatherInfo, "UpdateTime");

        return new WeatherData(temp, weatherCode, weatherText, warnings, updateTime);
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

    #endregion

    #region Weather Icon & Color

    (string icon, IBrush color) GetWeatherIconAndColor(string? weatherText, string? weatherCode)
    {
        if (string.IsNullOrEmpty(weatherText))
            weatherText = GetWeatherTextByCode(weatherCode ?? "");
        if (string.IsNullOrEmpty(weatherText))
            return ("🌤️", new SolidColorBrush(Color.Parse("#FFD54F")));

        var t = weatherText;

        // 下雨统一使用雨滴风格
        if (t.Contains("雷阵雨")) return ("⛈️", new SolidColorBrush(Color.Parse("#5C6BC0")));
        if (t.Contains("雨")) return ("🌧️", new SolidColorBrush(Color.Parse("#2196F3")));

        if (t.Contains("高温")) return ("🥵", new SolidColorBrush(Color.Parse("#F44336")));
        if (t.Contains("晴")) return ("☀️", new SolidColorBrush(Color.Parse("#FFA500")));
        if (t.Contains("多云")) return ("⛅", new SolidColorBrush(Color.Parse("#FFD700")));
        if (t.Contains("阴")) return ("☁️", new SolidColorBrush(Color.Parse("#90A4AE")));
        if (t.Contains("雪") || t.Contains("冰雹")) return ("❄️", new SolidColorBrush(Color.Parse("#81D4FA")));
        if (t.Contains("雾") || t.Contains("霾")) return ("🌫️", new SolidColorBrush(Color.Parse("#B0BEC5")));
        if (t.Contains("风") || t.Contains("沙尘")) return ("🍃", new SolidColorBrush(Color.Parse("#8D6E63")));

        return ("🌤️", new SolidColorBrush(Color.Parse("#FFD54F")));
    }

    IBrush? GetTempColor(double? temp)
    {
        if (!temp.HasValue) return null;
        var color = temp.Value switch
        {
            >= 35 => "#F44336",
            >= 25 => "#FF9800",
            >= 15 => "#4CAF50",
            >= 5 => "#2196F3",
            _ => "#3F51B5"
        };
        return new SolidColorBrush(Color.Parse(color));
    }

    #endregion

    #region Warning Parsing

    IEnumerable<WarningInfo> ParseWarnings(string title)
    {
        foreach (var type in GetWarningTypes(title))
        {
            var level = GetWarningLevel(title);
            var icon = GetWarningIcon(type);
            yield return new WarningInfo(type, level, icon);
        }
    }

    List<string> GetWarningTypes(string title)
    {
        var types = new[] { "道路结冰", "高温", "暴雨", "雷暴", "雷雨", "大风", "雷电", "冰雹", "暴雪", "寒潮", "大雾", "沙尘", "台风", "霜冻", "干旱", "霾" };
        return types.Where(t => title.Contains(t)).ToList();
    }

    string GetWarningLevel(string title)
    {
        if (title.Contains("红色")) return "红色";
        if (title.Contains("橙色")) return "橙色";
        if (title.Contains("黄色")) return "黄色";
        if (title.Contains("蓝色")) return "蓝色";
        return "";
    }

    string GetWarningIcon(string type)
    {
        return type switch
        {
            "高温" => "\uD83C\uDF21️",
            "暴雨" => "\uD83C\uDF27️",
            "大风" => "\uD83D\uDCA8",
            "雷电" or "雷雨" or "雷暴" => "\u26A1",
            "冰雹" => "\uD83C\uDF28️",
            "暴雪" => "\uD83C\uDF28️",
            "寒潮" => "\uD83E\uDDE3",
            "大雾" => "\uD83C\uDF2B️",
            "沙尘" => "\uD83D\uDE37",
            "台风" => "\uD83C\uDF00",
            "霜冻" => "\u2744️",
            "道路结冰" => "\uD83D\uDEA8",
            "干旱" => "\uD83D\uDCA7",
            "霾" => "\uD83D\uDE37",
            _ => "\u26A0️"
        };
    }

    string GetWarningBadgeText(string title)
    {
        var infos = ParseWarnings(title).ToList();
        if (infos.Count == 0) return "";
        return string.Join(" ", infos.Select(i => $"{i.Icon} {i.Type} {i.LevelText}"));
    }

    (IBrush bg, IBrush fg) GetWarningColors(string level)
    {
        return level switch
        {
            "红色" => (new SolidColorBrush(Color.Parse("#F44336")), Brushes.White),
            "橙色" => (new SolidColorBrush(Color.Parse("#FF9800")), Brushes.White),
            "黄色" => (new SolidColorBrush(Color.Parse("#FFEB3B")), Brushes.Black),
            "蓝色" => (new SolidColorBrush(Color.Parse("#2196F3")), Brushes.White),
            _ => (new SolidColorBrush(Color.Parse("#9E9E9E")), Brushes.White)
        };
    }

    #endregion

    string GetStaleWarning(DateTime? updateTime)
    {
        if (!updateTime.HasValue) return "(天气未刷新)";
        var elapsed = DateTime.Now - updateTime.Value;
        if (elapsed.TotalMinutes >= 30) return "(天气未刷新)";
        return "";
    }
}

public class SmartWeatherVariables
{
    public string A { get; set; } = "";
    public IBrush? AColor { get; set; }
    public string B { get; set; } = "";
    public IBrush? BColor { get; set; }
    public string C { get; set; } = "";
    public List<WarningInfo> CWarnings { get; set; } = new();
    public string D { get; set; } = "";
    public string E { get; set; } = "";
    public string F { get; set; } = "";
}

public class WarningInfo
{
    public string Type { get; }
    public string Level { get; }
    public string Icon { get; }
    public string LevelText => string.IsNullOrEmpty(Level) ? "预警" : Level;

    public WarningInfo(string type, string level, string icon)
    {
        Type = type;
        Level = level;
        Icon = icon;
    }
}

public class WeatherData
{
    public double? Temp { get; }
    public string? WeatherCode { get; }
    public string? WeatherText { get; }
    public string[] Warnings { get; }
    public DateTime? UpdateTime { get; }

    public WeatherData(double? temp = null, string? weatherCode = null, string? weatherText = null,
        string[]? warnings = null, DateTime? updateTime = null)
    {
        Temp = temp;
        WeatherCode = weatherCode;
        WeatherText = weatherText;
        Warnings = warnings ?? Array.Empty<string>();
        UpdateTime = updateTime;
    }
}
