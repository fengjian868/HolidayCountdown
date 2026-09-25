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

        // 按优先级排序：灾害预警 > 降雪/降雨 > 今天日出日落 > 未来天气
        // 使用 Category 字段映射到显示优先级
        if (results.Count > 0)
        {
            // 按显示优先级排序：数字小的排前面
            var ordered = results.OrderBy(r => GetDisplayPriority(r)).ThenBy(r => r.Priority).ToList();
            return ordered;
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

    /// <summary>
    /// 根据规则分类映射到显示优先级（数字越小越优先）：
    /// 0=灾害预警, 1=降雪/降雨, 2=日出日落, 3=未来天气, 4=其他
    /// </summary>
    static int GetDisplayPriority(WeatherReminderResult r)
    {
        if (string.IsNullOrEmpty(r.Category)) return 4;
        var cat = r.Category;
        if (cat.Contains("预警") || cat.Contains("灾害") || cat.Contains("雷电") || cat.Contains("台风")) return 0;
        if (cat.Contains("雪") || cat.Contains("雨")) return 1;
        if (cat.Contains("日出") || cat.Contains("日落")) return 2;
        if (cat.Contains("未来") || cat.Contains("预报")) return 3;
        return 4;
    }
}
