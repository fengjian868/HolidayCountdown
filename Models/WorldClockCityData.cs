using System;
using System.Collections.Generic;
using System.Linq;

namespace HolidayCountdown.Models;

/// <summary>
/// 世界时钟城市与时区映射数据。
/// 包含常见城市的时区ID，用户选择城市后自动匹配时区。
/// </summary>
public static class WorldClockCityData
{
    /// <summary>
    /// 获取所有支持的城市列表
    /// </summary>
    public static IReadOnlyCollection<string> SupportedCities => CityTimeZoneMap.Keys.OrderBy(x => x).ToList();

    /// <summary>
    /// 根据城市名获取时区ID
    /// </summary>
    public static string GetTimeZoneId(string city)
    {
        var key = city.Trim();
        if (CityTimeZoneMap.TryGetValue(key, out var tz))
            return tz;
        // 默认返回中国标准时间
        return "China Standard Time";
    }

    /// <summary>
    /// 帎市与时区映射表
    /// 时区ID 参考：https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/default-time-zones
    /// </summary>
    static readonly Dictionary<string, string> CityTimeZoneMap = new()
    {
        // 中国
        ["北京"] = "China Standard Time",
        ["上海"] = "China Standard Time",
        ["广州"] = "China Standard Time",
        ["深圳"] = "China Standard Time",
        ["杭州"] = "China Standard Time",
        ["南京"] = "China Standard Time",
        ["成都"] = "China Standard Time",
        ["武汉"] = "China Standard Time",
        ["西安"] = "China Standard Time",
        ["重庆"] = "China Standard Time",
        ["天津"] = "China Standard Time",
        ["苏州"] = "China Standard Time",
        ["厦门"] = "China Standard Time",
        ["青岛"] = "China Standard Time",
        ["大连"] = "China Standard Time",
        ["长沙"] = "China Standard Time",
        ["郑州"] = "China Standard Time",
        ["沈阳"] = "China Standard Time",
        ["哈尔滨"] = "China Standard Time",
        ["昆明"] = "China Standard Time",
        ["贵阳"] = "China Standard Time",
        ["南宁"] = "China Standard Time",
        ["海口"] = "China Standard Time",
        ["三亚"] = "China Standard Time",
        ["福州"] = "China Standard Time",
        ["南昌"] = "China Standard Time",
        ["合肥"] = "China Standard Time",
        ["石家庄"] = "China Standard Time",
        ["太原"] = "China Standard Time",
        ["兰州"] = "China Standard Time",
        ["乌鲁木齐"] = "China Standard Time",
        ["拉萨"] = "China Standard Time",
        ["呼和浩特"] = "China Standard Time",
        ["银川"] = "China Standard Time",
        ["西宁"] = "China Standard Time",

        // 日本
        ["东京"] = "Tokyo Standard Time",
        ["大阪"] = "Tokyo Standard Time",
        ["京都"] = "Tokyo Standard Time",
        ["横滨"] = "Tokyo Standard Time",
        ["名古屋"] = "Tokyo Standard Time",
        ["札幌"] = "Tokyo Standard Time",
        ["福冈"] = "Tokyo Standard Time",

        // 韩国
        ["首尔"] = "Korea Standard Time",
        ["釜山"] = "Korea Standard Time",
        ["仁川"] = "Korea Standard Time",

        // 东南亚
        ["新加坡"] = "Singapore Standard Time",
        ["曼谷"] = "SE Asia Standard Time",
        ["吉隆坡"] = "Singapore Standard Time",
        ["雅加达"] = "SE Asia Standard Time",
        ["马尼拉"] = "Singapore Standard Time",
        ["胡志明市"] = "SE Asia Standard Time",
        ["河内"] = "SE Asia Standard Time",

        // 印度
        ["新德里"] = "India Standard Time",
        ["孟买"] = "India Standard Time",
        ["班加罗尔"] = "India Standard Time",
        ["加尔各答"] = "India Standard Time",

        // 中东
        ["迪拜"] = "Arabian Standard Time",
        ["阿布扎比"] = "Arabian Standard Time",
        ["多哈"] = "Arabian Standard Time",
        ["利雅得"] = "Arab Standard Time",
        ["耶路撒冷"] = "Israel Standard Time",
        ["伊斯坦布尔"] = "Turkey Standard Time",

        // 欧洲
        ["伦敦"] = "GMT Standard Time",
        ["巴黎"] = "Romance Standard Time",
        ["柏林"] = "Central European Standard Time",
        ["罗马"] = "Central European Standard Time",
        ["马德里"] = "Romance Standard Time",
        ["阿姆斯特丹"] = "Central European Standard Time",
        ["布鲁塞尔"] = "Central European Standard Time",
        ["维也纳"] = "Central European Standard Time",
        ["布拉格"] = "Central European Standard Time",
        ["华沙"] = "Central European Standard Time",
        ["斯德哥尔摩"] = "Central European Standard Time",
        ["奥斯陆"] = "Central European Standard Time",
        ["哥本哈根"] = "Central European Standard Time",
        ["赫尔辛基"] = "FLE Standard Time",
        ["莫斯科"] = "Russian Standard Time",
        ["圣彼得堡"] = "Russian Standard Time",
        ["雅典"] = "GTB Standard Time",
        ["布达佩斯"] = "Central European Standard Time",
        ["里斯本"] = "GMT Standard Time",
        ["日内瓦"] = "Central European Standard Time",
        ["苏黎世"] = "Central European Standard Time",
        ["慕尼黑"] = "Central European Standard Time",
        ["法兰克福"] = "Central European Standard Time",
        ["巴塞罗那"] = "Romance Standard Time",
        ["米兰"] = "Central European Standard Time",
        ["都柏林"] = "GMT Standard Time",
        ["爱丁堡"] = "GMT Standard Time",

        // 非洲
        ["开罗"] = "Egypt Standard Time",
        ["约翰内斯堡"] = "South Africa Standard Time",
        ["开普敦"] = "South Africa Standard Time",
        ["拉各斯"] = "W. Central Africa Standard Time",
        ["内罗毕"] = "E. Africa Standard Time",

        // 澳大利亚/大洋洲
        ["悉尼"] = "AUS Eastern Standard Time",
        ["墨尔本"] = "AUS Eastern Standard Time",
        ["布里斯班"] = "E. Australia Standard Time",
        ["珀斯"] = "W. Australia Standard Time",
        ["阿德莱德"] = "Cen. Australia Standard Time",
        ["堪培拉"] = "AUS Eastern Standard Time",
        ["奥克兰"] = "New Zealand Standard Time",
        ["惠灵顿"] = "New Zealand Standard Time",
        ["斐济"] = "Fiji Standard Time",

        // 北美洲
        ["纽约"] = "Eastern Standard Time",
        ["洛杉矶"] = "Pacific Standard Time",
        ["芝加哥"] = "Central Standard Time",
        ["休斯顿"] = "Central Standard Time",
        ["迈阿密"] = "Eastern Standard Time",
        ["西雅图"] = "Pacific Standard Time",
        ["波士顿"] = "Eastern Standard Time",
        ["华盛顿"] = "Eastern Standard Time",
        ["旧金山"] = "Pacific Standard Time",
        ["拉斯维加斯"] = "Pacific Standard Time",
        ["达拉斯"] = "Central Standard Time",
        ["亚特兰大"] = "Eastern Standard Time",
        ["底特律"] = "Eastern Standard Time",
        ["多伦多"] = "Eastern Standard Time",
        ["温哥华"] = "Pacific Standard Time",
        ["蒙特利尔"] = "Eastern Standard Time",
        ["卡尔加里"] = "Mountain Standard Time",
        ["渥太华"] = "Eastern Standard Time",
        ["墨西哥城"] = "Central Standard Time",
        ["墨西哥"] = "Central Standard Time",

        // 南美洲
        ["里约热内卢"] = "E. South America Standard Time",
        ["圣保罗"] = "E. South America Standard Time",
        ["布宜诺斯艾利斯"] = "Argentina Standard Time",
        ["利马"] = "SA Pacific Standard Time",
        ["圣地亚哥"] = "SA Pacific Standard Time",
        ["波哥大"] = "SA Pacific Standard Time",
        ["加拉加斯"] = "SA Western Standard Time",

        // 夏威夷
        ["夏威夷"] = "Hawaiian Standard Time",
        ["檀香山"] = "Hawaiian Standard Time",

        // 阿拉斯加
        ["阿拉斯加"] = "Alaskan Standard Time",
    };
}