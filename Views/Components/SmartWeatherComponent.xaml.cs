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
    "使用和风天气API，1分钟刷新，含彩色图标与预警"
)]
public class SmartWeatherComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private StackPanel _root = null!;
    private HolidayService? _svc;
    private QWeatherService? _qweather;
    private string? _lastLocationId;
    private string? _lastCityName;

    // 缓存天气数据，避免每秒 tick 都请求 API
    private QWeatherService.QWeatherNowResponse? _cachedNow;
    private QWeatherService.QWeatherWarningResponse? _cachedWarnings;
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
        _lastLocationId = null;
        Dispatcher.UIThread.Post(FetchAndUpdate);
    }

    async void FetchAndUpdate()
    {
        if (_svc == null) return;

        try
        {
            var apiKey = _svc.Settings.QWeatherApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                Dispatcher.UIThread.Post(() => RenderNoKey());
                return;
            }

            // 初始化 QWeatherService
            if (_qweather == null || _qweather.ApiKey != apiKey)
            {
                _qweather = new QWeatherService { ApiKey = apiKey };
                _lastLocationId = null;
            }

            // 读取 ClassIsland 设置的城市
            var cityName = GetClassIslandCityName();
            if (string.IsNullOrEmpty(cityName))
            {
                Dispatcher.UIThread.Post(() => RenderNoCity());
                return;
            }

            // 城市变了，重新查找 LocationId
            if (cityName != _lastCityName)
            {
                _lastCityName = cityName;
                _lastLocationId = await _qweather.GetCityLocationId(cityName);
            }

            if (string.IsNullOrEmpty(_lastLocationId))
            {
                Dispatcher.UIThread.Post(() => RenderNoCity());
                return;
            }

            // 并行请求实时天气和预警
            var nowTask = _qweather.GetWeatherNowAsync(_lastLocationId);
            var warningTask = _qweather.GetWarningsAsync(_lastLocationId);
            await Task.WhenAll(nowTask, warningTask);

            _cachedNow = nowTask.Result;
            _cachedWarnings = warningTask.Result;
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
        string? weatherText = null;
        string? weatherCode = null;
        DateTime? updateTime = null;

        if (_cachedNow?.Now != null)
        {
            var now = _cachedNow.Now;
            if (double.TryParse(now.Temp, out var t)) temp = t;
            weatherText = now.Text;
            weatherCode = now.Icon;
            if (DateTime.TryParse(_cachedNow.UpdateTime, out var ut)) updateTime = ut;
        }

        // 预警
        var warnings = Array.Empty<string>();
        if (_cachedWarnings?.Warning != null && _cachedWarnings.Warning.Count > 0)
        {
            warnings = _cachedWarnings.Warning
                .Where(w => !string.IsNullOrEmpty(w.Title))
                .Select(w => w.Title!)
                .ToArray();
        }

        return new WeatherData(temp, weatherCode, weatherText, warnings, updateTime);
    }

    void RenderNoKey()
    {
        _root.Children.Clear();
        var tb = new TextBlock
        {
            Text = "请设置和风天气API Key",
            VerticalAlignment = VerticalAlignment.Center,
            Opacity = 0.8
        };
        tb[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
        _root.Children.Add(tb);
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
            return greet;
        }

        return "";
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
        if (string.IsNullOrEmpty(weatherText) && !string.IsNullOrEmpty(weatherCode))
            weatherText = GetWeatherTextByIconCode(weatherCode);
        if (string.IsNullOrEmpty(weatherText))
            return ("🌤️", new SolidColorBrush(Color.Parse("#FFD54F")));

        var t = weatherText;

        if (t.Contains("雷阵雨")) return ("⛈️", new SolidColorBrush(Color.Parse("#5C6BC0")));
        if (t.Contains("雨")) return ("🌧️", new SolidColorBrush(Color.Parse("#2196F3")));
        if (t.Contains("晴") || t.Contains("高温")) return ("☀️", new SolidColorBrush(Color.Parse("#FFA500")));
        if (t.Contains("多云")) return ("⛅", new SolidColorBrush(Color.Parse("#FFD700")));
        if (t.Contains("阴")) return ("☁️", new SolidColorBrush(Color.Parse("#90A4AE")));
        if (t.Contains("雪") || t.Contains("冰雹")) return ("❄️", new SolidColorBrush(Color.Parse("#81D4FA")));
        if (t.Contains("雾") || t.Contains("霾")) return ("🌫️", new SolidColorBrush(Color.Parse("#B0BEC5")));
        if (t.Contains("风") || t.Contains("沙尘")) return ("🍃", new SolidColorBrush(Color.Parse("#8D6E63")));

        return ("🌤️", new SolidColorBrush(Color.Parse("#FFD54F")));
    }

    /// <summary>
    /// 和风天气图标代码映射为中文天气文本
    /// </summary>
    string GetWeatherTextByIconCode(string iconCode)
    {
        if (string.IsNullOrEmpty(iconCode)) return "";
        return iconCode switch
        {
            "100" => "晴", "101" => "多云", "102" => "少云", "103" => "晴间多云",
            "104" => "阴", "150" => "晴",
            "300" => "阵雨", "301" => "强阵雨", "302" => "雷阵雨", "303" => "强雷阵雨",
            "304" => "雷阵雨伴有冰雹", "305" => "小雨", "306" => "中雨", "307" => "大雨",
            "308" => "极端降雨", "309" => "毛毛雨/细雨", "310" => "暴雨", "311" => "大暴雨",
            "312" => "特大暴雨", "313" => "冻雨", "314" => "小到中雨", "315" => "中到大雨",
            "316" => "大到暴雨", "317" => "暴雨到大暴雨", "318" => "大暴雨到特大暴雨",
            "399" => "雨",
            "400" => "小雪", "401" => "中雪", "402" => "大雪", "403" => "暴雪",
            "404" => "雨夹雪", "405" => "雨雪天气", "406" => "阵雨夹雪", "407" => "阵雪",
            "408" => "小到中雪", "409" => "中到大雪", "410" => "大到暴雪", "499" => "雪",
            "500" => "薄雾", "501" => "雾", "502" => "霾", "503" => "扬沙",
            "504" => "浮尘", "507" => "沙尘暴", "508" => "强沙尘暴",
            "509" => "浓雾", "510" => "强浓雾", "514" => "大雾", "515" => "特强浓雾",
            "900" => "热", "901" => "冷", "999" => "未知",
            _ => ""
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
