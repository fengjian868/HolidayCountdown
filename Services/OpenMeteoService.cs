using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HolidayCountdown.Services;

/// <summary>
/// Open-Meteo 天气服务（无需 API Key）
/// </summary>
public class OpenMeteoService
{
    private static readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    static OpenMeteoService()
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    /// <summary>
    /// 根据城市名称进行地理编码查询
    /// </summary>
    public async Task<GeoResult?> GeocodeAsync(string cityName)
    {
        if (string.IsNullOrWhiteSpace(cityName)) return null;

        var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(cityName)}&count=1&language=zh&format=json";
        var response = await GetAsync<GeoResponse>(url);
        return response?.Results?.FirstOrDefault();
    }

    /// <summary>
    /// 获取指定经纬度的天气预报
    /// </summary>
    public async Task<WeatherResponse?> GetWeatherAsync(double latitude, double longitude)
    {
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,relative_humidity_2m,weather_code,is_day,precipitation,rain,showers,snowfall,cloud_cover&hourly=weather_code,precipitation_probability,precipitation,rain,showers,snowfall&timezone=auto&forecast_days=2";
        return await GetAsync<WeatherResponse>(url);
    }

    /// <summary>
    /// 判断 WMO 天气代码是否代表降水
    /// 51-67：毛毛雨/雨，71-77：雪，80-82：阵雨，95-99：雷暴
    /// </summary>
    public static bool IsRainingCode(int code)
    {
        return code is >= 51 and <= 67
            or >= 71 and <= 77
            or >= 80 and <= 82
            or >= 95 and <= 99;
    }

    /// <summary>
    /// 根据逐小时数据分析当前降水状态及下一次状态变化
    /// </summary>
    /// <returns>
    /// isRainingNow：当前是否正在降水；
    /// minutesUntilChange：距离状态变化还有多少分钟；
    /// changeType：变化类型描述（"开始降水" 或 "停止降水"）
    /// </returns>
    public static (bool isRainingNow, int? minutesUntilChange, string changeType) GetRainInfo(List<HourlyData> hourly)
    {
        if (hourly == null || hourly.Count == 0)
            return (false, null, string.Empty);

        var now = DateTime.Now;
        var currentHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);

        // 找到当前小时对应的索引
        var currentIndex = hourly.FindIndex(h =>
        {
            if (string.IsNullOrEmpty(h.Time)) return false;
            return DateTime.TryParse(h.Time, out var t) &&
                   t.Year == currentHour.Year &&
                   t.Month == currentHour.Month &&
                   t.Day == currentHour.Day &&
                   t.Hour == currentHour.Hour;
        });

        if (currentIndex < 0)
            return (false, null, string.Empty);

        var currentCode = hourly[currentIndex].WeatherCode ?? -1;
        var isRainingNow = IsRainingCode(currentCode);

        // 向后查找第一个状态不同的小时
        int? changeIndex = null;
        for (int i = currentIndex + 1; i < hourly.Count; i++)
        {
            var code = hourly[i].WeatherCode ?? -1;
            var raining = IsRainingCode(code);
            if (raining != isRainingNow)
            {
                changeIndex = i;
                break;
            }
        }

        if (changeIndex == null)
            return (isRainingNow, null, isRainingNow ? "停止降水" : "开始降水");

        var hourDiff = changeIndex.Value - currentIndex;
        var minutesUntilChange = hourDiff * 60 - now.Minute;
        if (minutesUntilChange < 0) minutesUntilChange = 0;

        var changeType = isRainingNow ? "停止降水" : "开始降水";
        return (isRainingNow, minutesUntilChange, changeType);
    }

    private async Task<T?> GetAsync<T>(string url) where T : class
    {
        try
        {
            var json = await _httpClient.GetStringAsync(url);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    // ===== 响应模型 =====

    /// <summary>
    /// 地理编码响应
    /// </summary>
    public class GeoResponse
    {
        [JsonPropertyName("results")]
        public List<GeoResult>? Results { get; set; }
    }

    /// <summary>
    /// 地理编码结果
    /// </summary>
    public class GeoResult
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("admin1")]
        public string? Admin1 { get; set; }
    }

    /// <summary>
    /// 天气预报响应
    /// </summary>
    public class WeatherResponse
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("current")]
        public CurrentData? Current { get; set; }

        [JsonPropertyName("hourly")]
        public HourlyCollection? Hourly { get; set; }
    }

    /// <summary>
    /// 当前天气数据
    /// </summary>
    public class CurrentData
    {
        [JsonPropertyName("time")]
        public string? Time { get; set; }

        [JsonPropertyName("temperature_2m")]
        public double? Temperature2m { get; set; }

        [JsonPropertyName("relative_humidity_2m")]
        public double? RelativeHumidity2m { get; set; }

        [JsonPropertyName("weather_code")]
        public int? WeatherCode { get; set; }

        [JsonPropertyName("is_day")]
        public int? IsDay { get; set; }

        [JsonPropertyName("precipitation")]
        public double? Precipitation { get; set; }

        [JsonPropertyName("rain")]
        public double? Rain { get; set; }

        [JsonPropertyName("showers")]
        public double? Showers { get; set; }

        [JsonPropertyName("snowfall")]
        public double? Snowfall { get; set; }

        [JsonPropertyName("cloud_cover")]
        public double? CloudCover { get; set; }
    }

    /// <summary>
    /// 逐小时天气数据集合
    /// </summary>
    public class HourlyCollection
    {
        [JsonPropertyName("time")]
        public List<string>? Time { get; set; }

        [JsonPropertyName("weather_code")]
        public List<int?>? WeatherCode { get; set; }

        [JsonPropertyName("precipitation_probability")]
        public List<int?>? PrecipitationProbability { get; set; }

        [JsonPropertyName("precipitation")]
        public List<double?>? Precipitation { get; set; }

        [JsonPropertyName("rain")]
        public List<double?>? Rain { get; set; }

        [JsonPropertyName("showers")]
        public List<double?>? Showers { get; set; }

        [JsonPropertyName("snowfall")]
        public List<double?>? Snowfall { get; set; }

        /// <summary>
        /// 将平行数组转换为逐小时数据列表
        /// </summary>
        public List<HourlyData> ToHourlyDataList()
        {
            var result = new List<HourlyData>();
            if (Time == null) return result;

            var count = Time.Count;
            for (int i = 0; i < count; i++)
            {
                result.Add(new HourlyData
                {
                    Time = i < Time.Count ? Time[i] : null,
                    WeatherCode = WeatherCode != null && i < WeatherCode.Count ? WeatherCode[i] : null,
                    PrecipitationProbability = PrecipitationProbability != null && i < PrecipitationProbability.Count ? PrecipitationProbability[i] : null,
                    Precipitation = Precipitation != null && i < Precipitation.Count ? Precipitation[i] : null,
                    Rain = Rain != null && i < Rain.Count ? Rain[i] : null,
                    Showers = Showers != null && i < Showers.Count ? Showers[i] : null,
                    Snowfall = Snowfall != null && i < Snowfall.Count ? Snowfall[i] : null
                });
            }

            return result;
        }
    }

    /// <summary>
    /// 单小时天气数据
    /// </summary>
    public class HourlyData
    {
        public string? Time { get; set; }
        public int? WeatherCode { get; set; }
        public int? PrecipitationProbability { get; set; }
        public double? Precipitation { get; set; }
        public double? Rain { get; set; }
        public double? Showers { get; set; }
        public double? Snowfall { get; set; }
    }
}
