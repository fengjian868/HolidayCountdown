# 天气组件与天气变化提醒设计方案

## 背景与目标

当前插件已包含智能天气、天气问候、天气变化提醒三个天气相关组件，但存在以下问题：

- 智能天气组件默认模板把温度放在图标左侧，与 ClassIsland 自带天气样式不一致。
- 天气图标大小固定，无法跟随 ClassIsland 主体字体缩放。
- 天气问候组件刷新间隔固定为 1 分钟，用户希望可调且默认 10 分钟。
- 天气变化提醒组件处于测试版，当前提示语无显示内容，需要修复并扩展为完整的规则引擎式提醒系统。
- 设置页 Tab 栏在增加「天气提醒」配置页后可能溢出，需要支持两排显示。

本次设计目标：

1. 调整智能天气组件图标顺序与大小，使其与 ClassIsland 样式一致。
2. 为天气问候组件增加刷新间隔设置。
3. 用规则引擎重构天气变化提醒测试版，支持 27 种提醒类型并可持续显示。
4. 改造设置页 Tab 栏，溢出时自动改为两排。

## 方案选择

采用**规则引擎式方案（方案 B）**：

- 每种天气提醒类型实现为一个独立规则类，统一实现 `IWeatherReminderRule`。
- 新增 `WeatherReminderEvaluator` 负责读取启用规则、执行评估、排序并返回结果。
- `WeatherReminderComponent` 只负责渲染，逻辑与数据解耦。

优点：新增提醒类型只需新增规则文件，不影响主组件；便于单元测试和后期维护。缺点：初期文件数略多，但长期收益更大。

## 组件与文件架构

```
HolidayCountdown/
├── WeatherReminders/
│   ├── IWeatherReminderRule.cs
│   ├── WeatherReminderContext.cs
│   ├── WeatherReminderResult.cs
│   ├── WeatherReminderEvaluator.cs
│   └── Rules/
│       ├── LightningNearbyRule.cs
│       ├── RainSoonRule.cs
│       ├── RainStopRule.cs
│       ├── RainIncreaseRule.cs
│       ├── TempDropRule.cs
│       ├── TempRiseRule.cs
│       ├── StrongWindRule.cs
│       ├── AirQualityRule.cs
│       ├── SnowRule.cs
│       ├── UVRRule.cs
│       ├── HumidityRule.cs
│       ├── SunsetRule.cs
│       ├── ThunderOngoingRule.cs
│       ├── FrostRule.cs
│       ├── TyphoonRule.cs
│       ├── SandstormRule.cs
│       ├── TempDiffRule.cs
│       ├── RainbowRule.cs
│       ├── HeatRule.cs
│       ├── FreezeRule.cs
│       ├── ThunderWindRule.cs
│       ├── HailRule.cs
│       ├── HeavyRainRule.cs
│       ├── GustWindRule.cs
│       ├── DustRule.cs
│       ├── StuffyRule.cs
│       └── FeelsLikeRule.cs
├── Models/
│   └── PluginSettings.cs
├── Views/
│   ├── Components/
│   │   ├── SmartWeatherComponent.xaml.cs
│   │   ├── WeatherGreetingComponent.xaml.cs
│   │   └── WeatherReminderComponent.xaml.cs
│   └── SettingsPages/
│       └── UnifiedSettingsPage.xaml.cs
└── Services/
    └── HolidayService.cs
```

## 天气提醒规则系统

### IWeatherReminderRule 接口

```csharp
public interface IWeatherReminderRule
{
    string Id { get; }
    string Name { get; }
    string DefaultIcon { get; }
    bool EnabledByDefault { get; }
    int Priority { get; }  // 数字越小优先级越高
    WeatherReminderResult? Evaluate(WeatherReminderContext context);
}
```

### WeatherReminderContext 上下文

包含评估所需的全部数据：

- `double? CurrentTemp`：当前温度
- `string? WeatherCode`：天气代码
- `string? WeatherText`：天气文本
- `IList? HourlyForecasts`：未来 24 小时预报
- `IList? DailyForecasts`：未来 7 天预报
- `IList? Alerts`：预警列表
- `DateTime? UpdateTime`：天气更新时间
- `IReadOnlyList<WeatherReminderResult>? LastResults`：上次评估结果
- `DateTime Now`：当前时间

### WeatherReminderResult 结果

```csharp
public class WeatherReminderResult
{
    public string RuleId { get; set; } = "";
    public string Text { get; set; } = "";
    public string Icon { get; set; } = "";
    public int Priority { get; set; }
    public string Category { get; set; } = "";
}
```

### WeatherReminderEvaluator 评估器

职责：

1. 从 `PluginSettings.EnabledWeatherReminderRuleIds` 读取启用的规则 ID。
2. 遍历所有启用的规则，调用 `Evaluate(context)`。
3. 过滤 `null` 结果。
4. 按 `Priority` 升序排序。
5. 取前 `WeatherReminderMaxDisplayCount` 条（默认 3）。
6. 与 `context.LastResults` 对比，若发生变化且 `ShowImmediatelyOnChange` 为 true，则触发即时刷新。

错误处理：每个规则内部独立 `try-catch`，单条规则异常不影响其他规则。

### 27 种提醒规则清单

| 序号 | 规则 ID | 名称 | 默认启用 | 说明 |
|------|---------|------|----------|------|
| 1 | lightning-nearby | 附近有闪电 | 是 | 预警标题含「雷」「电」或小时预报未来 2 小时内有雷 |
| 2 | rain-soon | 即将降雨 | 是 | 当前无雨，未来 2 小时内天气代码进入降水区间（8~19） |
| 3 | rain-stop | 即将雨停 | 是 | 当前有雨，未来 2 小时内天气代码离开降水区间 |
| 4 | rain-increase | 降雨增多 | 是 | 未来 3 小时内降水天气代码从较小（如小雨）升到较大（如大雨） |
| 5 | temp-drop | 气温骤降 | 是 | 未来 1~2 天最高气温下降 ≥ 5°C |
| 6 | temp-rise | 气温骤升 | 是 | 未来 1~2 天最高气温上升 ≥ 5°C |
| 7 | strong-wind | 强风提醒 | 是 | 大风预警，或小时预报风速 ≥ 6 级（约 10.8 m/s） |
| 8 | air-quality | 空气质量转差 | 否 | 预警标题含「霾」「沙尘」或空气质量相关关键字 |
| 9 | snow | 降雪提醒 | 是 | 未来 6 小时或当天天气代码进入雪天区间 |
| 10 | uv | 紫外线较强 | 否 | 晴天（天气代码 1~3）且为白天 10:00~16:00 |
| 11 | humidity | 湿度较高 | 否 | 天气文本含「潮湿」「闷热」或温度 ≥ 28°C 且天气代码为雨天/阴天 |
| 12 | sunset | 日落时间 | 否 | 当日日落前 1 小时内显示，文本为「日落 HH:mm」 |
| 13 | thunder-ongoing | 雷电持续中 | 是 | 当前天气文本含「雷」「电」且小时预报未来 2 小时持续 |
| 14 | frost | 霜冻/道路结冰 | 是 | 当前温度 ≤ 0°C 或预警标题含「霜冻」「结冰」 |
| 15 | typhoon | 台风影响 | 是 | 预警标题含「台风」 |
| 16 | sandstorm | 沙尘暴提醒 | 是 | 预警标题含「沙尘暴」 |
| 17 | temp-diff | 昼夜温差大 | 否 | 当日昼夜温差 ≥ 10°C |
| 18 | rainbow | 雨后彩虹 | 否 | 当前雨停且未来 2 小时内转晴（多云/晴） |
| 19 | heat | 高温防暑 | 是 | 当前温度 ≥ 35°C |
| 20 | freeze | 低温防冻 | 是 | 当前温度 ≤ 0°C |
| 21 | thunder-wind | 雷雨大风 | 是 | 同时满足雷电和大风条件 |
| 22 | hail | 冰雹提醒 | 是 | 预警标题含「冰雹」或天气文本含「冰雹」 |
| 23 | heavy-rain | 暴雨内涝 | 是 | 暴雨预警或天气文本含「暴雨」「大暴雨」 |
| 24 | gust-wind | 阵风较强 | 是 | 阵风预警或小时预报阵风 ≥ 8 级（约 17.2 m/s） |
| 25 | dust | 大风扬尘 | 是 | 大风预警且天气文本含「沙」「尘」 |
| 26 | stuffy | 闷热转雨 | 是 | 当前闷热且未来 3 小时内转雨 |
| 27 | feels-like | 体感温度偏离 | 否 | 体感温度与实际温度差值 ≥ 5°C（数据存在时） |

## 现有组件改造

### SmartWeatherComponent

- 默认模板从 `{A} {B} {C} {D}` 改为 `{B} {A} {C} {D}`，使天气图标位于温度左侧。
- `Badge` 方法中所有文本（包括 `{A}` 温度、`{B}` 天气图标、`{D}` 提醒等）的 `FontSize` 不再写死 13，统一改为跟随 ClassIsland 主体字体大小。
- 天气图标 `{B}` 保持彩色渲染，不额外添加背景。
- 若 `SmartWeatherComponent` 尚未实现字体大小读取，则复用 `WeatherGreetingComponent.GetClassIslandFontSize()` 中的反射逻辑，或将其提取为公共辅助方法。

### WeatherGreetingComponent

- 新增设置项 `WeatherGreetingRefreshMinutes`（int），默认 10。
- 设置页提供下拉选项：5 / 10 / 15 / 30 分钟。
- 组件初始化及设置变更时，更新 `_timer.Interval`。
- 保留现有模板解析和动态主题色绑定。

### WeatherReminderComponent

- 移除现有 `BuildReminders` 中的硬编码逻辑。
- 引入 `WeatherReminderEvaluator`。
- 使用 `DispatcherTimer`，间隔由 `WeatherReminderRefreshMinutes` 控制（默认 10 分钟）。
- 每次 Tick：
  1. 从 ClassIsland `LastWeatherInfo` 构建 `WeatherReminderContext`。
  2. 调用 `WeatherReminderEvaluator.Evaluate`。
  3. 按优先级排序并取前 N 条渲染。
  4. 若结果与上次不同且启用即时刷新，则立即更新界面。
- 修复当前无内容问题：
  - 若评估结果为空，清空 `_txt.Text`。
  - 若天气数据不可用，显示「天气数据不可用」。
  - 捕获所有异常，避免组件崩溃导致空白。

## 设置页 Tab 栏改造

### 新增「天气提醒」Tab

在 `UnifiedSettingsPage._tabs` 数组中新增一项：

```csharp
("\uE753", "天气提醒", BuildWeatherReminderPanel)
```

位置放在「天气」Tab 之后、「课表」之前。

### 天气提醒设置内容

`BuildWeatherReminderPanel` 提供：

- 启用天气变化提醒测试版（ToggleSwitch）
- 刷新间隔（ComboBox：5/10/15/30 分钟，默认 10）
- 最多显示条数（NumberBox，默认 3，范围 1~5）
- 变化时立即刷新（ToggleSwitch，默认 true）
- 规则列表：每个规则一个 ToggleSwitch，显示图标 + 名称，默认按 `EnabledByDefault`

### Tab 栏两排显示

实现方式：

1. 将 `tabBar` 从 `StackPanel` 改为 `Grid`，内部包含两个 `StackPanel`（`Row0`、`Row1`），每排方向均为水平。
2. 所有 Tab 按钮先放入 `Row0`。
3. 监听 `SizeChanged` 事件，计算 `Row0` 中所有 Tab 按钮的期望总宽度。
4. 若总宽度大于 `tabBar.ActualWidth - 边距`，从 `Row0` 末尾依次移动按钮到 `Row1`，直到 `Row0` 不溢出。
5. 当前选中 Tab 的高亮样式保持不变。
6. 窗口大小变化时重新计算；窗口变宽时可将 `Row1` 的按钮移回 `Row0`。

**简化回退**：若 Avalonia 中动态测量与移动实现复杂，第一版可按 Tab 总数硬拆分（例如超过 7 个时后一半放第二排），后续再优化为按宽度自适应。

## 数据流

1. **ClassIsland 天气服务**提供 `LastWeatherInfo`。
2. `WeatherReminderComponent` 或 `SmartWeatherComponent` 通过反射读取该数据。
3. `WeatherReminderEvaluator` 构建 `WeatherReminderContext` 并执行规则。
4. 规则根据上下文返回 `WeatherReminderResult`。
5. 评估器排序、截断并返回最终结果列表。
6. 组件渲染结果，若发生变化则即时刷新。

## 新增配置项

在 `PluginSettings.cs` 中新增：

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `WeatherGreetingRefreshMinutes` | int | 10 | 天气问候刷新间隔 |
| `WeatherReminderEnabled` | bool | false | 启用天气变化提醒测试版 |
| `WeatherReminderRefreshMinutes` | int | 10 | 天气提醒刷新间隔 |
| `WeatherReminderMaxDisplayCount` | int | 3 | 最多同时显示条数 |
| `WeatherReminderShowImmediatelyOnChange` | bool | true | 天气变化时立即刷新 |
| `EnabledWeatherReminderRuleIds` | `List<string>` | 按各规则 `EnabledByDefault` | 启用的规则 ID 列表 |

`SmartWeatherTemplate` 默认值从 `{A} {B} {C} {D}` 改为 `{B} {A} {C} {D}`。

## 错误处理

- 天气数据不可用时，所有组件显示友好的占位文本而不是空白。
- 单个规则异常被捕获，不影响其他规则。
- 反射读取 ClassIsland 服务失败时，组件静默降级。

## 测试思路

- 使用不同天气数据验证 27 条规则中主要规则的触发条件（降雨、雷电、温度升降）。
- 验证 SmartWeather 默认模板下图标位于温度左侧。
- 验证图标大小随 ClassIsland 主体字体变化。
- 验证天气问候和天气提醒的刷新间隔设置生效。
- 验证设置页 Tab 栏在窗口缩小时正确变为两排。
- 验证规则启用/禁用设置保存后实时生效。

## 风险与回退

- ClassIsland 天气数据结构变化可能影响规则判断，需在规则内部做好 null 和类型检查。
- 27 条规则同时运行可能增加评估时间，但实际数据量小，风险可控。
- 若规则引擎实现超预期复杂，可回退为集中式方案 A，但当前设计已尽量保持简单。
