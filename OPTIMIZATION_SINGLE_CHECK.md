# 强制单身机制优化文档

## 优化概述

本次优化针对 `AllowLoveActivity` 配置项的行为进行了精细化调整，确保该配置项仅影响 **type=3（恋爱类型）** 行动的单身验证逻辑，而不影响其他类型的单身判断。

## 问题背景

### 原有问题
- `AllowLoveActivity` 配置原本影响所有 `[52, 1, X]` 类型的条件检查
- 这导致一些非恋爱相关的单身检查也被错误地处理
- 无法精确控制只有恋爱行动（type=3）才使用原版单身验证逻辑

### 优化目标
- 精确区分行动类型
- 仅对 type=3（恋爱）行动的 unlock 条件使用原版单身验证
- 其他类型的行动仍遵循 `AlwaysSingleCheck` 的强制单身规则

## 技术实现

### 1. 新增文件

#### ActionUnlockContext.cs
**功能**: 行动解锁上下文追踪器

**核心方法**:
```csharp
// 设置当前行动解锁检查上下文
public static void SetContext(int actionId)

// 检查当前是否是type=3（恋爱）行动的解锁检查
public static bool IsLoveActionUnlockCheck()

// 根据配置决定是否使用原版单身检查逻辑
public static bool ShouldUseOriginalSingleCheck()
```

**决策逻辑**:
```
使用原版逻辑的情况：
1. 未启用多恋人功能 → 使用原版逻辑
2. 未启用强制单身 → 使用原版逻辑
3. AllowLoveActivity=true 且当前是type=3行动解锁检查 → 使用原版逻辑

使用强制单身逻辑的情况：
- 其他所有情况
```

#### Patches/ActionUnlockPatch.cs
**功能**: 在行动解锁条件检查前设置上下文

**实现原理**:
1. 拦截 `CommonEvtMgr.IsMatchCondition(List<List<double>>, bool)` 调用
2. 检查调用栈是否来自行动解锁检查（ActionView/MapView/MapSceneView/QuickActionView）
3. 通过条件反查找到对应的行动ID
4. 设置上下文供 `ConditionerLove2Patch` 使用

**关键特性**:
- `[HarmonyPatch(typeof(CommonEvtMgr), nameof(CommonEvtMgr.IsMatchCondition), ...)]`
- 自动检测行动解锁检查上下文
- 支持条件反查（通过遍历 ActionCfgMap）

### 2. 修改文件

#### Patches/ConditionerLove2Patch.cs
**修改内容**:
1. 添加 `ShouldUseOriginalSingleCheck()` 方法
2. 修改 `OnIsMatch_Prefix` 中的单身检查逻辑
3. 添加调试上下文信息输出

**核心逻辑变化**:
```csharp
// 检查是否应该使用原版单身验证逻辑
bool useOriginalLogic = ShouldUseOriginalSingleCheck();

// 如果启用了"始终单身"选项且不使用原版逻辑，则强制返回单身状态
if (ModConfig.AlwaysSingleCheck.Value && !useOriginalLogic)
{
    // 强制单身模式
    if (value == 1)
        __result = false;  // [52, 1, 1] - 已脱单，但强制返回false
    else
        __result = true;   // [52, 1, -1] - 单身，强制返回true
}
```

#### LinceMultipleLoversPlugin.cs
**修改内容**:
添加 `CommonEvtMgrIsMatchConditionPatch` 补丁的注册:
```csharp
// 应用ActionUnlockPatch - 使用Harmony自动补丁
Log.LogInfo("正在应用ActionUnlock补丁...");
Harmony.CreateClassProcessor(typeof(Patches.CommonEvtMgrIsMatchConditionPatch)).Patch();
```

## 配置说明

### 配置项关系

| 配置项 | 作用 | 优化后行为 |
|--------|------|-----------|
| `EnableMultipleLovers` | 启用多恋人功能 | 总开关，关闭时所有优化不生效 |
| `AlwaysSingleCheck` | 主角始终判定为单身 | 开启时强制单身，但受AllowLoveActivity影响 |
| `AllowLoveActivity` | 允许恋爱活动 | **仅对type=3行动**使用原版单身验证 |
| `DebugMode` | 启用调试日志 | 输出详细的上下文信息 |

### 场景示例

#### 场景1: AllowLoveActivity=true, 检查type=3行动的unlock
```
行动: 约会 (type=3)
Unlock条件: [52, 1, 1] (需要已脱单)

结果: 使用原版逻辑，根据实际恋人状态返回
- 有恋人 → true (解锁)
- 无恋人 → false (锁定)
```

#### 场景2: AllowLoveActivity=true, 检查type=1行动的unlock
```
行动: 图书馆学习 (type=1)
Unlock条件: [52, 1, -1] (需要单身)

结果: 使用强制单身逻辑
- AlwaysSingleCheck=true → true (解锁)
- AlwaysSingleCheck=false → 根据实际状态
```

#### 场景3: AllowLoveActivity=false
```
任何行动的unlock条件 [52, 1, X]

结果: 全部使用强制单身逻辑
- [52, 1, 1] → false (强制未脱单)
- [52, 1, -1] → true (强制单身)
```

## 调试信息

开启 `DebugMode=true` 后，日志会输出以下信息：

```
[ActionUnlockContext] 设置上下文: actionId=301, type=3
[ActionUnlockPatch] 检测到行动解锁检查: actionId=301, type=3, name=约会
[ConditionerLove2] 脱单检查(原版逻辑): value=1, 恋人数量=2, 结果=True, 上下文=ActionUnlock(actionId=301, type=3)
```

## 验证测试

### 测试用例1: type=3行动的单身验证
1. 设置 `AllowLoveActivity=true`, `AlwaysSingleCheck=true`
2. 拥有一个恋人
3. 检查type=3行动（如约会）的unlock条件 `[52, 1, 1]`
4. **预期结果**: 返回true（使用原版逻辑）

### 测试用例2: type=1行动的单身验证
1. 设置 `AllowLoveActivity=true`, `AlwaysSingleCheck=true`
2. 拥有一个恋人
3. 检查type=1行动（如学习）的unlock条件 `[52, 1, -1]`
4. **预期结果**: 返回true（强制单身）

### 测试用例3: AllowLoveActivity=false
1. 设置 `AllowLoveActivity=false`, `AlwaysSingleCheck=true`
2. 拥有一个恋人
3. 检查任意行动的unlock条件 `[52, 1, 1]`
4. **预期结果**: 返回false（强制单身）

## 代码结构

```
LinceMultipleLovers/
├── ActionUnlockContext.cs          # 新增：上下文追踪器
├── Patches/
│   ├── ActionUnlockPatch.cs        # 新增：行动解锁补丁
│   └── ConditionerLove2Patch.cs    # 修改：优化单身检查逻辑
└── LinceMultipleLoversPlugin.cs    # 修改：注册新补丁
```

## 注意事项

1. **性能影响**: 条件反查会遍历所有ActionCfg，但只在unlock检查时触发，影响较小
2. **兼容性**: 与现有补丁完全兼容，不会破坏原有功能
3. **可维护性**: 模块化设计，便于后续扩展其他行动类型的特殊处理
