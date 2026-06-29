using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
    "A1B2C3D4-E5F6-7890-1234-567890ABCDEF",
    "智能天气[测试版]",
    "fluent(\uE753)",
    "使用免费Open-Meteo API，1分钟刷新，含彩色图标、预警、下雨/停雨倒计时"
)]
public class SmartWeatherComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private StackPanel _root = null!;
    private HolidayService? _svc;
    private OpenMeteoService? _openMeteo;
    private string? _lastCityName;
    private double? _lastLat;
    private double? _lastLon;

    // 缓存天气数据，避免每秒 tick 都请求 API
    private OpenMeteoService.WeatherResponse? _cachedWeather;
    private string[] _cachedClWarnings = Array.Empty<string>();
    private DateTime _lastFetchTime = DateTime.MinValue;

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
        _timer.Tick += (s, e) => FetchAndUpdate();
        _timer.Start();

        Dispatcher.UIThread.Post(() =>
        {
            _svc = new HolidayService();
            HolidayService.SettingsChanged += OnSettingsChanged;
            FetchAndUpdate();
        });
    }

    void OnSettingsChanged()
    {
        _svc?.LoadSettings();
        // 城市可能变了，清空缓存
        _lastCityName = null;
        _lastLat = null;
        _lastLon = null;
        Dispatcher.UIThread.Post(FetchAndUpdate);
    }

    async void FetchAndUpdate()
    {
        if (_svc == null) return;

        try
        {
            if (_openMeteo == null)
                _openMeteo = new OpenMeteoService();

            // 读取 ClassIsland 设置的城市
            var cityName = GetClassIslandCityName();
            if (string.IsNullOrEmpty(cityName))
            {
                Dispatcher.UIThread.Post(() => RenderNoCity());
                return;
            }

            // 城市变了，重新地理编码
            if (cityName != _lastCityName || _lastLat == null || _lastLon == null)
            {
                _lastCityName = cityName;
                var geo = await _openMeteo.GeocodeAsync(cityName);
                if (geo == null)
                {
                    Dispatcher.UIThread.Post(() => RenderNoCity());
                    return;
                }
                _lastLat = geo.Latitude;
                _lastLon = geo.Longitude;
            }

            // 并行请求 Open-Meteo 天气和 ClassIsland 预警（Open-Meteo 没有中国预警数据）
            var weatherTask = _openMeteo.GetWeatherAsync(_lastLat.Value, _lastLon.Value);
            var clWarningTask = Task.Run(() => GetClassIslandWarnings());
            await Task.WhenAll(weatherTask, clWarningTask);

            _cachedWeather = weatherTask.Result;
            _cachedClWarnings = clWarningTask.Result;
            _lastFetchTime = DateTime.Now;

            Dispatcher.UIThread.Post(RenderFromCache);
        }
        catch
        {
            Dispatcher.UIThread.Post(() => RenderError());
        }
    }

    void RenderFromCache()
    {
        _root.Children.Clear();
        if (_svc == null) return;

        try
        {
            var data = BuildWeatherData();
            var vars = BuildVariables(data);
            Render(vars);
        }
        catch
        {
            _root.Children.Add(Badge("天气数据不可用", null, null));
        }
    }

    WeatherData BuildWeatherData()
    {
        double? temp = null;
        double? humidity = null;
        string? weatherText = null;
        string? weatherCode = null;
        DateTime? updateTime = null;

        if (_cachedWeather?.Current != null)
        {
            var current = _cachedWeather.Current;
            temp = current.Temperature2m;
            humidity = current.RelativeHumidity2m;
            weatherCode = current.WeatherCode?.ToString();
            weatherText = GetWeatherTextByWmoCode(current.WeatherCode ?? -1);
            if (DateTime.TryParse(current.Time, out var ut)) updateTime = ut;
        }

        // 下雨/停雨倒计时
        string? rainInfo = null;
        if (_cachedWeather?.Hourly != null)
        {
            var hourly = _cachedWeather.Hourly.ToHourlyDataList();
            var (isRainingNow, minutes, changeType) = OpenMeteoService.GetRainInfo(hourly);
            if (minutes.HasValue && minutes.Value > 0)
            {
                if (isRainingNow)
                    rainInfo = $"{minutes}分钟后停雨";
                else
                    rainInfo = $"{minutes}分钟后下雨";
            }
            else if (isRainingNow)
            {
                rainInfo = "正在下雨";
            }
        }

        return new WeatherData(temp, weatherCode, weatherText, _cachedClWarnings, updateTime, humidity, rainInfo);
    }

    void RenderNoCity()
    {
        _root.Children.Clear();
        var tb = new TextBlock
        {
            Text = "未获取到城市信息",
            VerticalAlignment = VerticalAlignment.Center,
            Opacity = 0.8
        };
        tb[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
        _root.Children.Add(tb);
    }

    void RenderError()
    {
        _root.Children.Clear();
        _root.Children.Add(Badge("天气数据不可用", null, null));
    }

    /// <summary>
    /// 从 ClassIsland 设置中读取城市名称
    /// </summary>
    string? GetClassIslandCityName()
    {
        try
        {
            var settings = GetSettingsServiceSettings();
            if (settings == null) return null;

            // 优先读取 WeatherCity
            var city = GetPropertyValue(settings, "WeatherCity")?.ToString();
            if (!string.IsNullOrEmpty(city)) return city;

            // 尝试读取 LastWeatherInfo 中的城市信息
            var lastWeatherInfo = GetPropertyValue(settings, "LastWeatherInfo");
            if (lastWeatherInfo != null)
            {
                var cityName = GetPropertyValue(lastWeatherInfo, "CityName")?.ToString();
                if (!string.IsNullOrEmpty(cityName)) return cityName;

                var location = GetPropertyValue(lastWeatherInfo, "Location");
                if (location != null)
                {
                    var name = GetPropertyValue(location, "Name")?.ToString();
                    if (!string.IsNullOrEmpty(name)) return name;

                    var adm2 = GetPropertyValue(location, "Adm2")?.ToString();
                    if (!string.IsNullOrEmpty(adm2)) return adm2;
                }
            }

            return null;
        }
        catch { return null; }
    }

    /// <summary>
    /// 从 ClassIsland 读取天气预警信息（Open-Meteo 没有中国预警）
    /// </summary>
    string[] GetClassIslandWarnings()
    {
        try
        {
            var settings = GetSettingsServiceSettings();
            if (settings == null) return Array.Empty<string>();

            var lastWeatherInfo = GetPropertyValue(settings, "LastWeatherInfo");
            if (lastWeatherInfo == null) return Array.Empty<string>();

            var alerts = GetPropertyValue(lastWeatherInfo, "Alerts") as System.Collections.IEnumerable;
            if (alerts == null) return Array.Empty<string>();

            var result = new List<string>();
            foreach (var alert in alerts)
            {
                var title = GetPropertyValue(alert, "Title")?.ToString()
                         ?? GetPropertyValue(alert, "TypeName")?.ToString()
                         ?? GetPropertyValue(alert, "Type")?.ToString();
                if (!string.IsNullOrEmpty(title))
                    result.Add(title);
            }
            return result.ToArray();
        }
        catch { return Array.Empty<string>(); }
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
    /// 集中生成所有模板变量 A/B/C/D/E
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

        // C: 预警信息
        vars.C = data.Warnings.Length > 0
            ? string.Join(" ", data.Warnings.Select(GetWarningBadgeText))
            : "";
        vars.CWarnings = data.Warnings
            .SelectMany(ParseWarnings)
            .GroupBy(w => w.Type)
            .Select(g => g.First())
            .ToList();

        // D: 穿衣/出行/下雨提醒
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
        var template = _svc?.Settings.SmartWeatherTemplate ?? "{B} {A} {C} {D}";
        var showMap = new Dictionary<string, bool>
        {
            ["A"] = _svc?.Settings.SmartWeatherShowA ?? true,
            ["B"] = _svc?.Settings.SmartWeatherShowB ?? true,
            ["C"] = _svc?.Settings.SmartWeatherShowC ?? true,
            ["D"] = _svc?.Settings.SmartWeatherShowD ?? true,
            ["E"] = _svc?.Settings.SmartWeatherShowE ?? false,
        };

        // 预警置顶
        if (vars.CWarnings.Count > 0 && (_svc?.Settings.SmartWeatherShowC ?? true) && (_svc?.Settings.SmartWeatherWarningOverride ?? true))
        {
            foreach (var w in vars.CWarnings)
                _root.Children.Add(WarningBadge(w));
        }

        // 按模板顺序渲染
        var matches = Regex.Matches(template, @"\{([A-E])\}");
        foreach (Match m in matches)
        {
            var key = m.Groups[1].Value;
            if (!showMap.GetValueOrDefault(key, true)) continue;
            if (key == "C" && vars.CWarnings.Count > 0 && (_svc?.Settings.SmartWeatherShowC ?? true) && (_svc?.Settings.SmartWeatherWarningOverride ?? true)) continue;

            var control = key switch
            {
                "A" => Badge(vars.A, vars.AColor, null),
                "B" => Badge(vars.B, vars.BColor, null, isWeatherIcon: true),
                "C" => WarningList(vars.CWarnings),
                "D" => Badge(vars.D, null, null),
                "E" => Badge(vars.E, null, Brushes.Gray),
                _ => null
            };
            if (control != null) _root.Children.Add(control);
        }

        if (_root.Children.Count == 0)
            _root.Children.Add(Badge("无天气信息", null, null));
    }

    Control Badge(string text, IBrush? foreground, IBrush? background, bool isWeatherIcon = false)
    {
        var baseFontSize = GetClassIslandFontSize();
        var tb = new TextBlock
        {
            Text = text,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = isWeatherIcon ? baseFontSize + 2 : baseFontSize
        };
        if (foreground != null) tb.Foreground = foreground;
        else tb[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
        if (isWeatherIcon)
        {
            tb.FontFamily = new FontFamily("Segoe UI Emoji,Noto Color Emoji,Apple Color Emoji");
        }

        if (background == null) return tb;

        return new Border
        {
            Background = background,
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(4, 1),
            Child = tb
        };
    }

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
        var parts = new List<string>();

        // 下雨/停雨倒计时
        if (!string.IsNullOrEmpty(data.RainInfo))
            parts.Add(data.RainInfo);

        // 自定义温度问候
        if (data.Temp.HasValue && _svc?.Settings.TempGreetings.Count > 0)
        {
            var match = _svc.Settings.TempGreetings
                .FirstOrDefault(x => data.Temp.Value >= x.MinTemp && data.Temp.Value <= x.MaxTemp);
            if (match != null && !string.IsNullOrEmpty(match.Text))
                parts.Add(match.Text);
        }
        else if (data.Temp.HasValue)
        {
            var t = data.Temp.Value;
            var text = t switch
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
            parts.Add(text);
        }

        // 天气关键字问候
        if (!string.IsNullOrEmpty(data.WeatherText))
        {
            var match = _svc?.Settings.WeatherGreetingItems
                .Where(i => i.Keyword != "默认" && data.WeatherText.Contains(i.Keyword))
                .OrderByDescending(i => i.Keyword.Length)
                .FirstOrDefault();
            var greet = match?.Text ?? "";
            if (string.IsNullOrEmpty(greet))
            {
                var def = _svc?.Settings.WeatherGreetingItems.FirstOrDefault(i => i.Keyword == "默认");
                if (def != null) greet = def.Text.Replace("{weather}", data.WeatherText);
            }
            if (!string.IsNullOrEmpty(greet))
                parts.Add(greet);
        }

        return string.Join("，", parts);
    }

    #region ClassIsland Reflection

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

    #endregion

    #region Weather Icon & Color

    (string icon, IBrush color) GetWeatherIconAndColor(string? weatherText, string? weatherCode)
    {
        if (!string.IsNullOrEmpty(weatherCode) && int.TryParse(weatherCode, out var code))
            weatherText = GetWeatherTextByWmoCode(code);
        if (string.IsNullOrEmpty(weatherText))
            return ("🌤️", new SolidColorBrush(Color.Parse("#FFD54F")));

        var t = weatherText;

        if (t.Contains("雷")) return ("⛈️", new SolidColorBrush(Color.Parse("#5C6BC0")));
        if (t.Contains("雨") || t.Contains("阵雨")) return ("🌧️", new SolidColorBrush(Color.Parse("#2196F3")));
        if (t.Contains("晴") || t.Contains("高温")) return ("☀️", new SolidColorBrush(Color.Parse("#FFA500")));
        if (t.Contains("多云")) return ("⛅", new SolidColorBrush(Color.Parse("#FFD700")));
        if (t.Contains("阴")) return ("☁️", new SolidColorBrush(Color.Parse("#90A4AE")));
        if (t.Contains("雪") || t.Contains("冰雹")) return ("❄️", new SolidColorBrush(Color.Parse("#81D4FA")));
        if (t.Contains("雾") || t.Contains("霾")) return ("🌫️", new SolidColorBrush(Color.Parse("#B0BEC5")));
        if (t.Contains("风") || t.Contains("沙尘")) return ("🍃", new SolidColorBrush(Color.Parse("#8D6E63")));

        return ("🌤️", new SolidColorBrush(Color.Parse("#FFD54F")));
    }

    /// <summary>
    /// WMO 天气代码映射为中文天气文本
    /// </summary>
    string GetWeatherTextByWmoCode(int code)
    {
        return code switch
        {
            0 => "晴",
            1 => "主要晴朗",
            2 => "多云",
            3 => "阴",
            45 => "雾",
            48 => "雾凇",
            51 => "毛毛雨",
            53 => "中度毛毛雨",
            55 => "强毛毛雨",
            56 => "冻毛毛雨",
            57 => "强冻毛毛雨",
            61 => "小雨",
            63 => "中雨",
            65 => "大雨",
            66 => "冻雨",
            67 => "强冻雨",
            71 => "小雪",
            73 => "中雪",
            75 => "大雪",
            77 => "雪粒",
            80 => "阵雨",
            81 => "强阵雨",
            82 => "暴雨",
            85 => "阵雪",
            86 => "强阵雪",
            95 => "雷雨",
            96 => "雷雨伴小冰雹",
            99 => "雷雨伴大冰雹",
            _ => "未知"
        };
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
    public double? Humidity { get; }
    public string? RainInfo { get; }

    public WeatherData(double? temp = null, string? weatherCode = null, string? weatherText = null,
        string[]? warnings = null, DateTime? updateTime = null, double? humidity = null, string? rainInfo = null)
    {
        Temp = temp;
        WeatherCode = weatherCode;
        WeatherText = weatherText;
        Warnings = warnings ?? Array.Empty<string>();
        UpdateTime = updateTime;
        Humidity = humidity;
        RainInfo = rainInfo;
    }
}
