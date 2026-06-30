using System;
using System.Collections.Generic;
using System.Linq;
using HolidayCountdown.Models;
using HolidayCountdown.Services;

namespace HolidayCountdown.WeatherReminders;

/// <summary>
/// 天气提醒规则评估器。
/// </summary>
public class WeatherReminderEvaluator
{
    private readonly List<IWeatherReminderRule> _rules = new();
    private readonly HolidayService _svc;

    public WeatherReminderEvaluator(HolidayService svc)
    {
        _svc = svc;
        RegisterRules();
    }

    /// <summary>
    /// 注册所有内置规则。
    /// </summary>
    void RegisterRules()
    {
        _rules.Add(new Rules.RainTimingRule());
        _rules.Add(new Rules.RainSoonRule());
        _rules.Add(new Rules.UmbrellaRule());
        _rules.Add(new Rules.LightningNearbyRule());
        _rules.Add(new Rules.SnowRule());
        _rules.Add(new Rules.StrongWindRule());
        _rules.Add(new Rules.FogRule());
        _rules.Add(new Rules.ColdWaveRule());
        _rules.Add(new Rules.FreezeRule());
        _rules.Add(new Rules.SandStormRule());
        _rules.Add(new Rules.TempDropRule());
        _rules.Add(new Rules.TempRiseRule());
        _rules.Add(new Rules.HeatRule());
        _rules.Add(new Rules.UVRule());
        _rules.Add(new Rules.HumidityRule());
        _rules.Add(new Rules.DressRule());
        _rules.Add(new Rules.ComfortRule());
    }

    /// <summary>
    /// 评估并返回应显示的天气提醒列表。
    /// </summary>
    public IReadOnlyList<WeatherReminderResult> Evaluate(WeatherReminderContext context)
    {
        var enabledIds = _svc.Settings.EnabledWeatherReminderRuleIds;

        var results = new List<WeatherReminderResult>();

        foreach (var rule in _rules)
        {
            if (!IsRuleEnabled(rule, enabledIds)) continue;

            try
            {
                var result = rule.Evaluate(context);
                if (result != null) results.Add(result);
            }
            catch
            {
                // 单条规则异常不影响其他规则
            }
        }

        // 随机刷新区间：从所有匹配结果中随机选一条
        var minSec = _svc.Settings.WeatherReminderRandomMinSeconds;
        var maxSec = _svc.Settings.WeatherReminderRandomMaxSeconds;
        if (minSec > maxSec) (minSec, maxSec) = (maxSec, minSec);
        if (minSec < 1) minSec = 1;
        if (maxSec < 1) maxSec = 60;

        if (results.Count > 0)
        {
            var random = new Random();
            var index = random.Next(results.Count);
            return new List<WeatherReminderResult> { results[index] };
        }

        return new List<WeatherReminderResult>();
    }

    /// <summary>
    /// 判断规则是否启用。若用户未配置过，按规则默认值处理。
    /// </summary>
    bool IsRuleEnabled(IWeatherReminderRule rule, List<string> enabledIds)
    {
        if (enabledIds == null || enabledIds.Count == 0) return rule.EnabledByDefault;
        return enabledIds.Contains(rule.Id);
    }

    /// <summary>
    /// 对比两次评估结果是否发生变化。
    /// </summary>
    public bool HasChanged(IReadOnlyList<WeatherReminderResult>? previous, IReadOnlyList<WeatherReminderResult> current)
    {
        if (previous == null) return current.Count > 0;
        if (previous.Count != current.Count) return true;

        for (int i = 0; i < previous.Count; i++)
        {
            var a = previous[i];
            var b = current[i];
            if (a.RuleId != b.RuleId || a.Text != b.Text) return true;
        }

        return false;
    }

    /// <summary>
    /// 获取所有已注册规则的元数据，用于设置页展示。
    /// </summary>
    public IReadOnlyList<IWeatherReminderRule> GetAllRules()
    {
        return _rules.OrderBy(r => r.Priority).ThenBy(r => r.Name).ToList();
    }
}
