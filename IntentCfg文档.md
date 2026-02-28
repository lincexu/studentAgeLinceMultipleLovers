# IntentCfg.json 属性说明文档

基于《学生时代》游戏官方源代码解析

---

## 一、文件概述

**IntentCfg.json** 是游戏的目标/意图系统配置文件，定义了玩家在游戏中可以接取的各种目标（如学习计划、社交目标等）。

- **加载路径**: `Cfgs/{语言}/IntentCfg.json`
- **存储位置**: `Cfg.IntentCfgMap` (Dictionary<int, IntentCfg>)
- **核心类**: [IntentCfg.cs](file:///e:/steam/steamapps/common/StudentAge/datafanbianyi/Assembly-CSharp/Config/IntentCfg.cs)

---

## 二、核心属性详解

### 2.1 基础信息

| Key | 类型 | 代码特性 | 功能说明 |
|-----|------|----------|----------|
| `id` | int | `[CfgProperty(8000)]` | 目标唯一标识符 |
| `name` | string | `[CfgProperty(8020)]` | 目标显示名称（UI上展示） |
| `desc` | string | `[CfgProperty(8027)]` | 目标描述文本 |
| `group` | int | `[CfgProperty(8013)]` | **目标分组**：<br>• `999` = 默认分组<br>• `-1` = 引导目标<br>• `100` = 新年目标<br>• 相同组的目标**互斥** |

### 2.2 完成机制

| Key | 类型 | 功能说明 |
|-----|------|----------|
| `demand` | `List<List<double>>` | **完成条件**，格式：`[[条件类型, 操作符, 值], ...]` |
| `finishType` | int | **完成类型**：<br>• `0` = 手动完成（玩家点击）<br>• `1` = 自动完成（条件满足自动）<br>• `2` = 不可完成（通过效果移除） |
| `round` | int | **持续回合数**，`0` = 无限期，每回合减1，到0时失败 |
| `targetRound` | int | **目标回合**，要求在该回合前完成 |

### 2.3 奖励与惩罚

| Key | 类型 | 功能说明 |
|-----|------|----------|
| `reward` | `List<List<float>>` | **完成奖励**，格式：`[[效果类型, 子类型, 参数...], ...]` |
| `fail` | `List<List<float>>` | **失败效果**，目标超时或失败时触发 |
| `finishTalk` | `List<int>` | **完成对话ID列表**，完成时依次播放 |
| `failTalk` | `List<int>` | **失败对话ID列表**，失败时依次播放 |

### 2.4 触发与关联

| Key | 类型 | 功能说明 |
|-----|------|----------|
| `condition` | `List<List<double>>` | **触发条件**，满足条件才会出现该目标 |
| `before` | int | **前置目标ID**，必须先完成该目标才能接取 |
| `next` | int | **下一个目标组**，完成后随机添加该组的新目标 |
| `npc` | int | **关联NPC**，对应 PersonCfg 的ID，用于显示头像 |

### 2.5 特殊属性

| Key | 类型 | 功能说明 |
|-----|------|----------|
| `renshengguan` | int | **人生观类型**，关联特定人生观（如学习观、社交观） |
| `tag` | int | **标签**：<br>• `1` = 普通目标<br>• `2` = 新年目标/选择型目标 |
| `weight` | float | **随机权重**，随机选择目标时的概率权重 |

---

## 三、属性详细说明

### 3.1 group（目标分组）

**核心功能**：相同 `group` 值的目标**互斥**，同时只能存在一个。

**特殊分组值**：

| group值 | 含义 | 用途 |
|---------|------|------|
| `999` | 默认分组 | 普通目标 |
| `-1` | 引导目标 | 新手教程目标 |
| `100` | 新年目标 | 每年初选择的目标 |
| `12` | 特殊组 | 特定剧情目标 |

**代码逻辑**（IntentData.cs）：
```csharp
// 添加目标时检查同组互斥
public bool AddIntent(int _id, ...)
{
    IntentCfg intentCfg = Cfg.IntentCfgMap[_id];
    // 检查是否已有同组目标
    if (intentCfg.group > 0 && this.group == intentCfg.group)
        return false; // 同组目标已存在
    // ...
}
```

---

### 3.2 demand（完成条件）

**格式**: `[[条件类型, 操作符, 值, 额外参数], ...]`

**示例**:
```json
"demand": [
    [1.0, 1.0, 80.0],      // 属性1（智力）≥ 80
    [7.0, 2.0, 100.0]      // 属性7（金钱）≤ 100
]
```

**代码解析**（IntentData.cs）：
```csharp
// 检查目标是否完成
public void CheckIntentFinish(IntentSubData _data)
{
    IntentCfg intentCfg = Cfg.IntentCfgMap[_data.id];
    if (intentCfg.demand.NotEmpty())
    {
        // 使用 Conditioner 检查所有 demand 条件
        foreach (var condition in intentCfg.demand)
        {
            if (!Conditioner.IsMatch(condition))
                return; // 条件不满足
        }
        // 所有条件满足，目标完成
        this.FinishIntent(_data);
    }
}
```

---

### 3.3 reward（完成奖励）

**格式**: `[[效果类型, 子类型, 属性ID, 数值], ...]`

**示例**:
```json
"reward": [
    [1.0, 1.0, 1.0, 10.0],     // 智力+10
    [1.0, 1.0, 7.0, 50.0],     // 金钱+50
    [14.0, 1.0, 101.0]         // 特殊效果
]
```

**代码解析**（IntentData.cs）：
```csharp
// 获取目标奖励
public void GetIntentReward(int _id)
{
    IntentCfg intentCfg = Cfg.IntentCfgMap[_id];
    if (intentCfg.reward.NotEmpty())
    {
        foreach (var effect in intentCfg.reward)
        {
            // 执行效果
            Effector.DoEffect(effect);
        }
    }
}
```

---

### 3.4 condition（触发条件）

**格式**: 同 `demand`，用于控制目标何时出现。

**代码解析**（IntentData.cs）：
```csharp
// 检查新目标
public void CheckNewIntent()
{
    foreach (var pair in Cfg.IntentCfgMap)
    {
        IntentCfg intentCfg = pair.Value;
        // 检查触发条件
        if (intentCfg.condition.NotEmpty())
        {
            bool canAdd = true;
            foreach (var cond in intentCfg.condition)
            {
                if (!Conditioner.IsMatch(cond))
                {
                    canAdd = false;
                    break;
                }
            }
            if (canAdd)
                this.AddIntent(intentCfg.id);
        }
    }
}
```

---

### 3.5 round（持续回合）

**机制**：
- 每回合结束时减1
- 到0时目标失败
- 0表示无限期

**代码解析**（IntentData.cs）：
```csharp
public void NewRound()
{
    foreach (var intent in this.intentList)
    {
        IntentCfg intentCfg = Cfg.IntentCfgMap[intent.id];
        if (intentCfg.round > 0)
        {
            intent.restRound--;
            if (intent.restRound <= 0)
            {
                // 回合耗尽，目标失败
                this.GetIntentFail(intent);
            }
        }
    }
}
```

---

## 四、完整配置示例

### 示例1：普通学习目标

```json
{
    "id": 1001,
    "name": "期末冲刺",
    "desc": "期末考试前将智力提升到80",
    "group": 999,
    "demand": [
        [1.0, 1.0, 80.0]       // 智力≥80
    ],
    "condition": [
        [10.0, -30.0]          // 第30回合前
    ],
    "reward": [
        [1.0, 1.0, 7.0, 100.0],    // 金钱+100
        [1.0, 1.0, 11.0, 10.0]     // 心情+10
    ],
    "fail": [
        [1.0, 1.0, 11.0, -20.0]    // 失败：心情-20
    ],
    "finishTalk": [1001, 1002],    // 完成时播放对话
    "failTalk": [1003],             // 失败时播放对话
    "round": 30,                    // 30回合内完成
    "finishType": 0,                // 手动完成
    "npc": 101,                     // 关联NPC（老师）
    "weight": 1.0
}
```

---

### 示例2：新年目标

```json
{
    "id": 2001,
    "name": "读书计划",
    "desc": "今年读完10本书",
    "group": 100,                   // 新年目标分组
    "tag": 2,                       // 新年目标标签
    "demand": [
        [52.0, 1.0, 1001.0, 10.0]  // 完成行动1001达10次
    ],
    "reward": [
        [1.0, 1.0, 1.0, 20.0],     // 智力+20
        [1.0, 1.0, 3.0, 10.0]      // 想象力+10
    ],
    "renshengguan": 1,              // 关联学习观
    "round": 0,                     // 全年有效
    "finishType": 1,                // 自动完成
    "weight": 1.5                   // 较高权重
}
```

---

### 示例3：引导目标

```json
{
    "id": 1,
    "name": "初次上学",
    "desc": "完成第一天的课程",
    "group": -1,                    // 引导目标分组
    "demand": [
        [10.0, 1.0, 2.0]           // 第2回合
    ],
    "condition": [
        [10.0, 1.0, 1.0]           // 第1回合触发
    ],
    "reward": [
        [1.0, 1.0, 7.0, 50.0]      // 金钱+50
    ],
    "next": 10,                     // 完成后添加group=10的目标
    "round": 10,
    "finishType": 1,
    "npc": 1                        // 关联主角
}
```

---

### 示例4：互斥目标组

```json
// 目标A
{
    "id": 3001,
    "name": "理科方向",
    "desc": "专注理科学习",
    "group": 30,                    // 分组30
    "demand": [
        [1.0, 1.0, 90.0]           // 智力≥90
    ],
    "reward": [
        [1.0, 1.0, 1.0, 30.0]
    ],
    "weight": 1.0
}

// 目标B（与A互斥）
{
    "id": 3002,
    "name": "文科方向",
    "desc": "专注文科学习",
    "group": 30,                    // 同分组30，与A互斥
    "demand": [
        [2.0, 1.0, 90.0]           // 记忆力≥90
    ],
    "reward": [
        [1.0, 1.0, 2.0, 30.0]
    ],
    "weight": 1.0
}
```

---

## 五、相关代码文件

| 文件路径 | 功能 |
|----------|------|
| `Config/IntentCfg.cs` | IntentCfg 类定义 |
| `IntentData.cs` | 目标系统核心逻辑 |
| `IntentSubData.cs` | 玩家目标数据 |
| `ConditionerGoal.cs` | 目标条件判断器 |
| `View/TheAction/IntentSelectView.cs` | 新年目标选择界面 |
| `View/TheAction/IntentMiniView.cs` | 迷你目标视图 |
| `View/Common/IntentItem.cs` | 目标列表项 |
| `View/Main/DetailIntentView.cs` | 目标详情界面 |
| `MainDescHelper.cs` | 目标描述帮助类 |

---

## 六、目标系统流程

```
1. 加载配置
   LoadIntentCfgMap() → Cfg.IntentCfgMap

2. 触发目标
   CheckNewIntent() → 检查 condition → AddIntent()

3. 显示目标
   IntentMiniView / IntentItem 读取 name, desc, npc

4. 回合更新
   NewRound() → round-- → CheckIntentFinish()

5. 完成目标
   检查 demand → GetIntentReward() → 播放 finishTalk

6. 目标失败
   round=0 → GetIntentFail() → 播放 failTalk

7. 链式目标
   完成 → 触发 next 组 → 添加新目标
```

---

*文档生成时间: 2026-02-24*  
*基于游戏版本: 学生时代的官方源代码*
