using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HolidayCountdown.Services;

public class QWeatherService
{
    private static readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly Dictionary<string, string> _locationIdCache = new();

    static QWeatherService()
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

    public string? ApiKey { get; set; }

    public async Task<QWeatherNowResponse?> GetWeatherNowAsync(string locationId)
    {
        return await GetAsync<QWeatherNowResponse>($"https://devapi.qweather.com/v7/weather/now?location={locationId}&key={ApiKey}&lang=zh-cn");
    }

    public async Task<QWeatherHourlyResponse?> GetWeather24hAsync(string locationId)
    {
        return await GetAsync<QWeatherHourlyResponse>($"https://devapi.qweather.com/v7/weather/24h?location={locationId}&key={ApiKey}&lang=zh-cn");
    }

    public async Task<QWeatherDailyResponse?> GetWeather3dAsync(string locationId)
    {
        return await GetAsync<QWeatherDailyResponse>($"https://devapi.qweather.com/v7/weather/3d?location={locationId}&key={ApiKey}&lang=zh-cn");
    }

    public async Task<QWeatherWarningResponse?> GetWarningsAsync(string locationId)
    {
        return await GetAsync<QWeatherWarningResponse>($"https://devapi.qweather.com/v7/warning/now?location={locationId}&key={ApiKey}&lang=zh-cn");
    }

    public async Task<string?> GetCityLocationId(string cityName)
    {
        if (string.IsNullOrEmpty(cityName)) return null;

        if (_locationIdCache.TryGetValue(cityName, out var cachedId))
            return cachedId;

        try
        {
            var url = $"https://geoapi.qweather.com/v2/city/lookup?location={Uri.EscapeDataString(cityName)}&key={ApiKey}&lang=zh-cn";
            var json = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("code", out var codeEl) && codeEl.GetString() == "200")
            {
                if (root.TryGetProperty("location", out var locationArr) && locationArr.GetArrayLength() > 0)
                {
                    var first = locationArr[0];
                    var id = first.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                    if (id != null)
                    {
                        _locationIdCache[cityName] = id;
                        return id;
                    }
                }
            }
        }
        catch { }

        return null;
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

    // ===== Response Models =====

    public class QWeatherNowResponse
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("updateTime")]
        public string? UpdateTime { get; set; }

        [JsonPropertyName("now")]
        public QWeatherNow? Now { get; set; }
    }

    public class QWeatherNow
    {
        [JsonPropertyName("temp")]
        public string? Temp { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("icon")]
        public string? Icon { get; set; }

        [JsonPropertyName("feelsLike")]
        public string? FeelsLike { get; set; }

        [JsonPropertyName("humidity")]
        public string? Humidity { get; set; }

        [JsonPropertyName("windDir")]
        public string? WindDir { get; set; }

        [JsonPropertyName("windSpeed")]
        public string? WindSpeed { get; set; }

        [JsonPropertyName("windScale")]
        public string? WindScale { get; set; }

        [JsonPropertyName("precip")]
        public string? Precip { get; set; }

        [JsonPropertyName("pressure")]
        public string? Pressure { get; set; }

        [JsonPropertyName("vis")]
        public string? Vis { get; set; }

        [JsonPropertyName("cloud")]
        public string? Cloud { get; set; }

        [JsonPropertyName("dew")]
        public string? Dew { get; set; }
    }

    public class QWeatherHourlyResponse
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("updateTime")]
        public string? UpdateTime { get; set; }

        [JsonPropertyName("hourly")]
        public List<QWeatherHourly>? Hourly { get; set; }
    }

    public class QWeatherHourly
    {
        [JsonPropertyName("fxTime")]
        public string? FxTime { get; set; }

        [JsonPropertyName("temp")]
        public string? Temp { get; set; }

        [JsonPropertyName("icon")]
        public string? Icon { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("humidity")]
        public string? Humidity { get; set; }

        [JsonPropertyName("precip")]
        public string? Precip { get; set; }

        [JsonPropertyName("pop")]
        public string? Pop { get; set; }

        [JsonPropertyName("windDir")]
        public string? WindDir { get; set; }

        [JsonPropertyName("windScale")]
        public string? WindScale { get; set; }

        [JsonPropertyName("windSpeed")]
        public string? WindSpeed { get; set; }
    }

    public class QWeatherDailyResponse
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("updateTime")]
        public string? UpdateTime { get; set; }

        [JsonPropertyName("daily")]
        public List<QWeatherDaily>? Daily { get; set; }
    }

    public class QWeatherDaily
    {
        [JsonPropertyName("fxDate")]
        public string? FxDate { get; set; }

        [JsonPropertyName("tempMax")]
        public string? TempMax { get; set; }

        [JsonPropertyName("tempMin")]
        public string? TempMin { get; set; }

        [JsonPropertyName("iconDay")]
        public string? IconDay { get; set; }

        [JsonPropertyName("textDay")]
        public string? TextDay { get; set; }

        [JsonPropertyName("iconNight")]
        public string? IconNight { get; set; }

        [JsonPropertyName("textNight")]
        public string? TextNight { get; set; }

        [JsonPropertyName("humidity")]
        public string? Humidity { get; set; }

        [JsonPropertyName("precip")]
        public string? Precip { get; set; }

        [JsonPropertyName("windDirDay")]
        public string? WindDirDay { get; set; }

        [JsonPropertyName("windScaleDay")]
        public string? WindScaleDay { get; set; }

        [JsonPropertyName("windSpeedDay")]
        public string? WindSpeedDay { get; set; }
    }

    public class QWeatherWarningResponse
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("updateTime")]
        public string? UpdateTime { get; set; }

        [JsonPropertyName("warning")]
        public List<QWeatherWarning>? Warning { get; set; }
    }

    public class QWeatherWarning
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("sender")]
        public string? Sender { get; set; }

        [JsonPropertyName("pubTime")]
        public string? PubTime { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("level")]
        public string? Level { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("typeName")]
        public string? TypeName { get; set; }

        [JsonPropertyName("urgency")]
        public string? Urgency { get; set; }

        [JsonPropertyName("certainty")]
        public string? Certainty { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
