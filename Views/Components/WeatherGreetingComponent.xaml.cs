using System;
using System.Collections;
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
using HolidayCountdown.WeatherReminders;

namespace HolidayCountdown.Views.Components;

[ComponentInfo(
    "B2C3D4E5-F6A7-8901-BCDE-F23456789012",
    "天气问候",
    "fluent(\uE753)",
    "读取ClassIsland天气，1分钟刷新，含温度问候、预警、下雨/停雨倒计时"
)]
public class WeatherGreetingComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private StackPanel _panel = null!;
    private HolidayService? _svc;

    public WeatherGreetingComponent()
    {
        _panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 4
        };
        Content = _panel;

        // 每分钟刷新一次：先调用 CL 刷新天气，再读取数据
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _timer.Tick += (s, e) => Dispatcher.UIThread.Post(() => Update());
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

    async void Update()
    {
        if (_svc == null) return;

        // 先调用 ClassIsland 天气服务刷新天气（CL 默认 5 分钟，这里每分钟强制刷新）
        await RefreshClassIslandWeatherAsync();

        _panel.Children.Clear();

        try
        {
            var (temp, weatherCode, weatherText, warnings, updateTime) = GetWeatherData();

            // 天气文本
            var actualWeatherText = weatherText;
            if (string.IsNullOrEmpty(actualWeatherText) && !string.IsNullOrEmpty(weatherCode))
                actualWeatherText = GetWeatherTextByCode(weatherCode);
            if (string.IsNullOrEmpty(actualWeatherText)) actualWeatherText = GetWeatherTextByMiCode(weatherCode ?? "");

            // 下雨/停雨倒计时
            var rainInfo = GetRainInfo();

            // 穿衣/出行提醒
            var greeting = GetTempGreeting(temp);
            if (string.IsNullOrEmpty(greeting) && !string.IsNullOrEmpty(actualWeatherText))
                greeting = GetWeatherGreeting(actualWeatherText);

            // 预警提醒
            var warningText = GetWarningText(warnings);
            if (!string.IsNullOrEmpty(warningText) && _svc.Settings.WeatherWarningOverride)
            {
                greeting = warningText;
                warningText = "";
            }

            // 构建模板变量
            var icon = _svc.Settings.WeatherShowIcon ? GetWeatherIcon(actualWeatherText) : "";
            var (coloredIcon, iconColor) = GetWeatherIconAndColor(actualWeatherText, weatherCode);

            var template = _svc.Settings.WeatherTemplate ?? "{icon} {weather} {temp} {greeting} {rain}";

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // 短变量名
                ["A"] = icon,
                ["B"] = actualWeatherText ?? "",
                ["C"] = (_svc.Settings.WeatherShowTemp && temp.HasValue) ? $"{temp.Value}°C" : "",
                ["D"] = greeting,
                ["E"] = warningText,
                ["F"] = rainInfo ?? "",
                ["G"] = CombineReminder(rainInfo, greeting),
                ["H"] = GetStaleWarning(updateTime),
                ["I"] = coloredIcon,
                // 旧长变量名兼容
                ["icon"] = icon,
                ["weather"] = actualWeatherText ?? "",
                ["temp"] = (_svc.Settings.WeatherShowTemp && temp.HasValue) ? $"{temp.Value}°C" : "",
                ["greeting"] = greeting,
                ["warning"] = warningText,
                ["rain"] = rainInfo ?? "",
            };

            var baseFontSize = GetClassIslandFontSize();

            // 如果有预警且预警置顶，先渲染预警徽章
            if (warnings.Length > 0 && _svc.Settings.SmartWeatherWarningOverride)
            {
                foreach (var w in warnings)
                {
                    foreach (var info in ParseWarnings(w))
                    {
                        _panel.Children.Add(WarningBadge(info, baseFontSize));
                    }
                }
            }

            // 按模板渲染
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
                if (block.IsIcon)
                {
                    tb.FontFamily = new FontFamily("Segoe UI Emoji,Noto Color Emoji,Apple Color Emoji");
                }
                else if (block.Key == "C" && _svc.Settings.SmartWeatherTempColorEnabled)
                {
                    var color = GetTempBrush(temp);
                    if (color != null) tb.Foreground = color;
                    else tb[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
                }
                else if (block.Key == "H" || block.Key == "E" && block.Text.Contains("未刷新"))
                {
                    tb.Foreground = Brushes.Gray;
                }
                else
                {
                    tb[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
                }
                _panel.Children.Add(tb);
            }

            if (_panel.Children.Count == 0)
            {
                var empty = new TextBlock { Text = "天气数据不可用", Opacity = 0.7 };
                empty[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
                _panel.Children.Add(empty);
            }
        }
        catch
        {
            var err = new TextBlock { Text = "天气数据不可用", Opacity = 0.7 };
            err[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorPrimaryBrush");
            _panel.Children.Add(err);
        }
    }

    string CombineReminder(string? rainInfo, string greeting)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(rainInfo)) parts.Add(rainInfo);
        if (!string.IsNullOrEmpty(greeting)) parts.Add(greeting);
        return string.Join("，", parts);
    }

    #region ClassIsland 天气刷新

    async Task RefreshClassIslandWeatherAsync()
    {
        try
        {
            var weatherService = GetClassIslandService("IWeatherService");
            if (weatherService == null) return;

            var queryMethod = weatherService.GetType().GetMethod("QueryWeatherAsync", BindingFlags.Public | BindingFlags.Instance);
            if (queryMethod == null) return;

            var task = queryMethod.Invoke(weatherService, null) as Task;
            if (task != null) await task;
        }
        catch { }
    }

    #endregion

    #region 天气数据获取

    (double? temp, string? weatherCode, string? weatherText, string[] warnings, DateTime? updateTime) GetWeatherData()
    {
        try
        {
            var settings = GetSettingsServiceSettings();
            if (settings == null) return (null, null, null, Array.Empty<string>(), null);

            var lastWeatherInfo = GetPropertyValue(settings, "LastWeatherInfo");
            if (lastWeatherInfo == null) return (null, null, null, Array.Empty<string>(), null);

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

            var warnings = GetAllAlertTitles(lastWeatherInfo);

            var updateTime = GetDateTimeProperty(lastWeatherInfo, "UpdateTime")
                ?? GetDateTimeProperty(lastWeatherInfo, "FetchTime")
                ?? GetDateTimeProperty(lastWeatherInfo, "LastUpdateTime")
                ?? GetDateTimeProperty(lastWeatherInfo, "UpdatedTime");

            return (temp, weatherCode, weatherText, warnings, updateTime);
        }
        catch { return (null, null, null, Array.Empty<string>(), null); }
    }

    /// <summary>
    /// 根据未来逐小时天气预报计算"多久下雨"或"多久停雨"
    /// </summary>
    string? GetRainInfo()
    {
        try
        {
            var settings = GetSettingsServiceSettings();
            if (settings == null) return null;
            var lastWeatherInfo = GetPropertyValue(settings, "LastWeatherInfo");
            if (lastWeatherInfo == null) return null;

            // 优先用天气文本来判断
            var hourlyTexts = WeatherDataHelper.GetHourlyWeatherTexts(lastWeatherInfo, 24);
            if (hourlyTexts.Count > 0)
            {
                return ComputeRainInfoFromTexts(hourlyTexts);
            }

            // 天气文本获取失败时，用天气代码判断
            var hourlyCodes = WeatherDataHelper.GetHourlyWeatherCodes(lastWeatherInfo, 24);
            if (hourlyCodes.Count > 0)
            {
                return ComputeRainInfoFromCodes(hourlyCodes);
            }

            return null;
        }
        catch { return null; }
    }

    string? ComputeRainInfoFromTexts(List<string> hourlyTexts)
    {
        var now = DateTime.Now;
        bool isRainingNow = WeatherDataHelper.IsPrecipitationText(hourlyTexts[0]);

        for (int i = 1; i < hourlyTexts.Count; i++)
        {
            bool raining = WeatherDataHelper.IsPrecipitationText(hourlyTexts[i]);
            if (raining != isRainingNow)
            {
                int minutes = i * 60 - now.Minute;
                if (minutes < 0) minutes = 0;
                return isRainingNow ? $"{minutes}分钟后停雨" : $"{minutes}分钟后下雨";
            }
        }

        return isRainingNow ? "将持续降雨" : null;
    }

    string? ComputeRainInfoFromCodes(List<int> hourlyCodes)
    {
        var now = DateTime.Now;
        bool isRainingNow = hourlyCodes.Count > 0 && WeatherDataHelper.IsPrecipitationCode(hourlyCodes[0]);

        for (int i = 1; i < hourlyCodes.Count; i++)
        {
            bool raining = WeatherDataHelper.IsPrecipitationCode(hourlyCodes[i]);
            if (raining != isRainingNow)
            {
                int minutes = i * 60 - now.Minute;
                if (minutes < 0) minutes = 0;
                return isRainingNow ? $"{minutes}分钟后停雨" : $"{minutes}分钟后下雨";
            }
        }

        return isRainingNow ? "将持续降雨" : null;
    }

    string[] GetAllAlertTitles(object lastWeatherInfo)
    {
        try
        {
            var alerts = GetPropertyValue(lastWeatherInfo, "Alerts");
            if (alerts == null) return Array.Empty<string>();

            var result = new List<string>();

            // 方式1：作为 IEnumerable 遍历（最可靠）
            if (alerts is IEnumerable enumerable and not string)
            {
                foreach (var alert in enumerable)
                {
                    if (alert == null) continue;
                    var title = GetPropertyValue(alert, "Title")?.ToString()
                             ?? GetPropertyValue(alert, "TypeName")?.ToString()
                             ?? GetPropertyValue(alert, "Type")?.ToString()
                             ?? alert.ToString();
                    if (!string.IsNullOrEmpty(title) && title != alert.GetType().Name)
                        result.Add(title);
                }
            }

            // 方式2：通过索引器遍历（备用）
            if (result.Count == 0)
            {
                var countProp = alerts.GetType().GetProperty("Count");
                var count = (int?)countProp?.GetValue(alerts) ?? 0;
                if (count > 0)
                {
                    var indexer = alerts.GetType().GetProperties()
                        .FirstOrDefault(p => p.GetIndexParameters().Length == 1);
                    if (indexer != null)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            var alert = indexer.GetValue(alerts, new object[] { i });
                            if (alert == null) continue;
                            var title = GetPropertyValue(alert, "Title")?.ToString()
                                     ?? GetPropertyValue(alert, "TypeName")?.ToString();
                            if (!string.IsNullOrEmpty(title)) result.Add(title);
                        }
                    }
                }
            }

            return result.ToArray();
        }
        catch { return Array.Empty<string>(); }
    }

    #endregion

    #region 问候语

    string GetTempGreeting(double? temp)
    {
        if (temp == null) return "";
        var t = temp.Value;

        var items = _svc?.Settings.TempGreetings;
        if (items != null && items.Count > 0)
        {
            var match = items.FirstOrDefault(x => t >= x.MinTemp && t <= x.MaxTemp);
            if (match != null) return match.Text;
        }

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

    string GetStaleWarning(DateTime? updateTime)
    {
        if (!updateTime.HasValue) return "";
        var elapsed = DateTime.Now - updateTime.Value;
        if (elapsed.TotalMinutes >= 30) return "(天气未刷新)";
        return "";
    }

    #endregion

    #region 图标与颜色

    string GetWeatherIcon(string? weatherText)
    {
        if (string.IsNullOrEmpty(weatherText)) return "";
        var t = weatherText;
        if (t.Contains("雷阵雨")) return "⛈️";
        if (t.Contains("雷")) return "⚡";
        if (t.Contains("雨")) return "🌧️";
        if (t.Contains("晴")) return "☀️";
        if (t.Contains("多云")) return "⛅";
        if (t.Contains("阴")) return "☁️";
        if (t.Contains("雪")) return "❄️";
        if (t.Contains("雾") || t.Contains("霾")) return "🌫️";
        if (t.Contains("风") || t.Contains("沙")) return "🍃";
        if (t.Contains("冰雹")) return "🧊";
        return "🌤️";
    }

    (string icon, IBrush color) GetWeatherIconAndColor(string? weatherText, string? weatherCode)
    {
        if (string.IsNullOrEmpty(weatherText) && !string.IsNullOrEmpty(weatherCode))
            weatherText = GetWeatherTextByMiCode(weatherCode);
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

    IBrush? GetTempBrush(double? temp)
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

    /// <summary>
    /// 小米天气代码映射为中文天气文本
    /// </summary>
    string GetWeatherTextByMiCode(string? code)
    {
        if (string.IsNullOrEmpty(code) || !int.TryParse(code, out var c)) return "";
        return c switch
        {
            0 => "晴", 1 => "多云", 2 => "阴", 3 => "阵雨", 4 => "雷阵雨",
            5 => "雷阵雨伴有冰雹", 6 => "雨夹雪", 7 => "小雨", 8 => "中雨", 9 => "大雨",
            10 => "暴雨", 11 => "大暴雨", 12 => "特大暴雨", 13 => "阵雪", 14 => "小雪",
            15 => "中雪", 16 => "大雪", 17 => "暴雪", 18 => "雾", 19 => "冻雨",
            20 => "沙尘暴", 53 => "霾", _ => ""
        };
    }

    #endregion

    #region 预警徽章

    IEnumerable<WarningInfo> ParseWarnings(string title)
    {
        foreach (var type in GetWarningTypes(title))
        {
            var level = GetWarningLevel(title);
            var icon = GetWarningIcon(type);
            yield return new WarningInfo(type, level, icon);
        }
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
            "高温" => "\uD83C\uDF21️", "暴雨" => "\uD83C\uDF27️", "大风" => "\uD83D\uDCA8",
            "雷电" or "雷雨" or "雷暴" => "\u26A1", "冰雹" => "\uD83C\uDF28️",
            "暴雪" => "\uD83C\uDF28️", "寒潮" => "\uD83E\uDDE3", "大雾" => "\uD83C\uDF2B️",
            "沙尘" => "\uD83D\uDE37", "台风" => "\uD83C\uDF00", "霜冻" => "\u2744️",
            "道路结冰" => "\uD83D\uDEA8", "干旱" => "\uD83D\uDCA7", "霾" => "\uD83D\uDE37",
            _ => "\u26A0️"
        };
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

    Control WarningBadge(WarningInfo w, double baseFontSize)
    {
        var (bg, fg) = GetWarningColors(w.Level);
        var text = $"{w.Icon} {w.Type} {w.LevelText}";
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

    #endregion

    #region 模板解析

    List<(string Text, bool IsIcon, string Key)> ParseTemplate(string template, Dictionary<string, string> values)
    {
        var list = new List<(string Text, bool IsIcon, string Key)>();
        int i = 0;
        while (i < template.Length)
        {
            int open = template.IndexOf('{', i);
            if (open < 0)
            {
                var tail = template[i..];
                if (!string.IsNullOrEmpty(tail)) list.Add((tail, false, ""));
                break;
            }
            if (open > i)
            {
                var literal = template[i..open];
                if (!string.IsNullOrEmpty(literal)) list.Add((literal, false, ""));
            }
            int close = template.IndexOf('}', open);
            if (close < 0)
            {
                var tail = template[open..];
                if (!string.IsNullOrEmpty(tail)) list.Add((tail, false, ""));
                break;
            }
            var key = template[(open + 1)..close];
            var value = values.TryGetValue(key, out var v) ? v : "";
            var isIcon = key.Equals("A", StringComparison.OrdinalIgnoreCase)
                      || key.Equals("I", StringComparison.OrdinalIgnoreCase)
                      || key.Equals("icon", StringComparison.OrdinalIgnoreCase);
            list.Add((value, isIcon, key));
            i = close + 1;
        }
        var merged = new List<(string Text, bool IsIcon, string Key)>();
        foreach (var item in list)
        {
            if (string.IsNullOrEmpty(item.Text)) continue;
            if (merged.Count > 0 && merged[^1].IsIcon == item.IsIcon && merged[^1].Key == item.Key)
                merged[^1] = (merged[^1].Text + item.Text, item.IsIcon, item.Key);
            else
                merged.Add(item);
        }
        return merged;
    }

    #endregion

    #region ClassIsland 反射

    object? GetSettingsServiceSettings()
    {
        try
        {
            return GetClassIslandService("SettingsService") is { } svc
                ? svc.GetType().GetProperty("Settings", BindingFlags.Public | BindingFlags.Instance)?.GetValue(svc)
                : null;
        }
        catch { return null; }
    }

    object? GetClassIslandService(string typeName)
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

            var serviceType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == typeName);
            if (serviceType == null) return null;

            var genericMethod = tryGetService.MakeGenericMethod(serviceType);
            return genericMethod.Invoke(null, null);
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

    string GetWeatherTextByCode(string code)
    {
        if (string.IsNullOrEmpty(code)) return "";
        try
        {
            var weatherService = GetClassIslandService("IWeatherService");
            if (weatherService == null) return "";
            var getWeatherText = weatherService.GetType().GetMethod("GetWeatherTextByCode", BindingFlags.Public | BindingFlags.Instance);
            if (getWeatherText == null) return "";
            return getWeatherText.Invoke(weatherService, new object[] { code })?.ToString() ?? "";
        }
        catch { return ""; }
    }

    #endregion
}
