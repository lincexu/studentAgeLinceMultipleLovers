# LinceMultipleLovers 插件兼容性说明

## 插件概述

本插件实现了《学生时代》游戏的多恋人系统，允许玩家同时拥有多个恋人。

## 核心修改

### 1. 数据存储扩展

**修改文件**: `LoveData` 类 (通过 Harmony 补丁)

**原逻辑**:
```csharp
public class LoveData
{
    public int loverId;  // 只能存储一个恋人ID
}
```

**新逻辑**:
- 使用 `historyLoverIds` 列表存储所有恋人ID
- 通过 `LoverIdInterceptor` 类管理多恋人数据
- 兼容原版存档格式

### 2. 关键类修改清单

| 类名 | 修改方式 | 修改内容 |
|------|----------|----------|
| `LoveData` | Harmony Postfix | 同步数据到 Mod 管理器 |
| `Role` | Harmony Postfix | 修改 `GetRelationForUI` 方法，支持多恋人显示 |
| `MapRoleView` | Harmony Prefix | 修改 `RefreshData`，根据当前NPC动态修改 `loverId` |
| `MapRoleView` | Harmony Postfix | 修改 `OnCellRender`/`OnCellCreate`，记录当前交互NPC |
| `QuickSocialView` | Harmony Prefix | 修改 `OnRenderSocial`，社交容量面板显示所有恋人 |
| `ConditionerLove` | Harmony Prefix | 修改 `OnIsMatch`，条件判断支持多恋人 |
| `ConditionerLove2` | Harmony Prefix | 修改 `OnIsMatch`/`OnGetProgress`，多恋人条件判断 + 强制单身 |
| `ActionData` | Harmony Prefix | 修改 `HelpLoveAction`，恋爱活动使用正确的恋人 |
| `BadmintonMiniGameView` | Harmony Postfix | 记录小游戏对手信息 |
| `RelationData` | Harmony Prefix/Postfix | 社交容量、关系判断多恋人适配 |
| `ShareView` | Harmony Prefix | 毕业分享页面多恋人切换 |
| `DebugMgr` | Harmony Prefix | LINCE控制台命令拦截 |
| `DebugView` | Harmony Postfix/Prefix | 控制台历史记录浏览 |
| `IncreaserOther` | Harmony Prefix | 恋人相关增益多恋人适配 (3003/3910/3913/9) |
| `RoleMgr` | Harmony Prefix | 好友特性倍率(10001)亲密度加成多恋人适配 |
| `CommonEvtMgr` | Harmony Prefix | 自定义条件5207(恋人数量) + 自定义效果5217(批量好感) |

### 3. 方法修改详情

#### 3.1 LoveDataPatch

**方法**: `SetLover_Postfix`
- **触发时机**: 设置恋人时
- **功能**: 将恋人ID添加到 `historyLoverIds` 列表

**方法**: `BreakLover_Postfix`
- **触发时机**: 分手时
- **功能**: 从 `historyLoverIds` 列表移除恋人ID

**方法**: `GetTopics_Prefix`
- **原逻辑**: `if (this.loverId == 0) return null;`
- **新逻辑**: 检查 `historyLoverIds` 列表，使用当前交互NPC获取话题

#### 3.2 RolePatch

**方法**: `GetRelationForUI_Postfix`
- **原逻辑**: 只检查 `loverId == this.id`
- **新逻辑**: 检查 `historyLoverIds` 列表

#### 3.3 MapRoleViewPatch

**方法**: `RefreshData_Prefix`
- **功能**: 根据当前NPC动态修改 `loverId`
- **注意**: 不再恢复 `loverId`，保持为最后交互的恋人

#### 3.4 MapRoleViewTopicPatch

**方法**: `OnCellRender_Postfix` / `OnCellCreate_Postfix`
- **功能**: 
  1. 记录当前交互NPC (`CurrentNpcId`)
  2. 修改 `loverId` 为当前NPC
  3. 记录最近交互恋人 (`LastInteractedLover`)

#### 3.5 QuickSocialViewPatch

**方法**: `OnRenderSocial_Prefix`
- **功能**: 渲染每个角色前，如果是恋人则临时修改 `loverId`

#### 3.6 ConditionerLovePatch

**方法**: `OnIsMatch_Prefix`
- **条件格式**: `52,2,1,X` (X是NPC ID)
- **原逻辑**: `return loverId == X;`
- **新逻辑**: `return IsLover(X);` (检查 `historyLoverIds`)

#### 3.7 ActionDataPatch

**方法**: `HelpLoveAction_Prefix`
- **功能**: 记录当前 `loverId` 用于调试

#### 3.8 MiniGamePatch

**方法**: `BadmintonOnOpen_Postfix`
- **功能**: 记录小游戏对手信息

#### 3.9 LoveDataSocialTopicPatch

**方法**: `CanSocialTopic_Prefix`
- **功能**: 每角色独立计算话题次数

**方法**: `SocialTopic_Prefix`
- **功能**: 记录每角色话题次数

**方法**: `GetTopics_Prefix`
- **功能**: 使用 `CurrentNpcId` 获取话题

**方法**: `NewRound_Postfix`
- **功能**: 新回合重置所有角色话题计数

#### 3.10 IncreaserOtherPatch

**方法**: `OnGetSelfValue_Prefix`
- **功能**: 拦截 `IncreaserOther.OnGetSelfValue`，对loverId相关的otherAttrId进行多恋人适配
- **涉及 otherAttrId**:
  - `3003`: 检查所有恋人的最高属性是否匹配id
  - `3910`: 有恋人返回0，无恋人返回value
  - `3913`: 有恋人返回0，无恋人返回记忆数*value

**方法**: `OnRun_Prefix`
- **功能**: 拦截 `IncreaserOther.OnRun`，对otherAttrId=9的有/无恋人分支进行多恋人适配

#### 3.11 RoleMgrPatch

**方法**: `GetRateType_Prefix`
- **功能**: 拦截 `RoleMgr.GetRateType` case 10001（好友特性倍率）
- **原逻辑**: `if (role2.id == loverId)` 才加入亲密度加成(3002)
- **新逻辑**: `if (allLoverIds.Contains(role2.id))` 任一恋人均可获得加成

#### 3.12 CustomConditionPatch

**方法**: `GenConditioner_Prefix`
- **功能**: 拦截 `CommonEvtMgr.GenConditioner`，注入自定义条件类型5207
- **格式**: `[5207, subType, X]`
  - `subType=1`: 恋人总数 ≥ X 返回true
  - `subType=-1`: 恋人总数 ≤ X 返回true
- **实现类**: `CustomConditionerLoverCount` 继承 `Conditioner`

#### 3.13 CustomEffectPatch

**方法**: `GenEffector_Prefix`
- **功能**: 拦截 `CommonEvtMgr.GenEffector`，注入自定义效果类型5217
- **格式**: `[5217, 1, X]` 或 `[5217, 1, X, Y]`
  - 无Y: 除当前loverId外，所有恋人好感 +X
  - 有Y: 除角色id=Y外，所有恋人好感 +X
- **实现类**: `CustomEffectorAllLoversFavor` 继承 `Effector`
- **特性**: 手动复现UpdateFavor逻辑，合并为一条Toast显示

### 4. 新增类

#### 4.1 LoverIdInterceptor

**功能**: 管理多恋人数据

**关键方法**:
- `GetAllLoverIds()`: 获取所有恋人ID列表
- `IsLover(int npcId)`: 检查NPC是否是恋人
- `HasLover()`: 检查是否有任何恋人
- `GetPrimaryLoverId()`: 获取主恋人ID（列表第一个）
- `LoverIdLocked`: loverId锁定标志，锁定时阻止自动切换

#### 4.2 LastInteractedLover

**功能**: 记录最近交互的恋人

**关键属性**:
- `LastLoverId`: 最近交互的恋人ID
- `LastInteractionTime`: 最后交互时间

#### 4.3 LoversManager

**功能**: 管理恋人数据持久化

**关键方法**:
- `SaveLoverIds()`: 保存到存档
- `LoadLoverIds()`: 从存档加载
- `SyncFromOriginalData()`: 同步原版数据

### 5. 配置选项

**配置文件**: `BepInEx/config/LinceMultipleLovers.cfg`

```ini
[General]
EnableMultipleLovers = true  # 启用多恋人系统
DebugMode = false            # 调试模式（输出详细日志）
```

### 6. 控制台命令

**命令前缀**: `LINCE`

| 命令 | 功能 |
|------|------|
| `LINCE HELP` | 显示所有可用命令 |
| `LINCE CLEAR` | 清空控制台记录 |
| `LINCE LOVER <npcId>` | 将指定NPC设为恋人 |
| `LINCE BREAK <npcId>` | 与指定NPC分手 |
| `LINCE LOVERID <角色ID>` | 切换当前活跃恋人 |
| `LINCE LOVERID LOCK` | 锁定当前loverId，阻止自动切换 |
| `LINCE LOVERID UNLOCK` | 解除loverId锁定 |
| `LINCE EFFECT <type,sub,...>` | 手动执行effect指令 |
| `LINCE ADDFOLLOW <数量>` | 增加关注上限 |
| `LINCE NPC` | 显示所有角色ID和名称 |
| `LINCE NPC ID <id>` | 查询指定ID的角色名称 |
| `LINCE NPC NAME <名字>` | 模糊搜索角色名称 |
| `LINCE RESOCIAL` | 刷新所有角色社交事件 |

### 7. 与其他Mod的兼容性

#### 7.1 兼容的Mod类型

- **UI Mod**: 本插件修改UI显示，但使用Harmony补丁，通常兼容
- **数据Mod**: 如果也修改 `LoveData`，需要确保加载顺序
- **功能Mod**: 一般兼容

#### 7.2 潜在的冲突

| 冲突类型 | 说明 | 解决方案 |
|----------|------|----------|
| 重复修改 `loverId` | 其他Mod也修改 `loverId` | 确保本插件最后加载 |
| 直接读取 `loverId` | 其他Mod直接比较 `loverId` | 需要该Mod也支持多恋人 |
| 关系数据 Mod | 修改 RelationData | 本插件也修改了 RelationData，需测试兼容性 |
| 条件/效果 Mod | 修改 CommonEvtMgr | 本插件拦截了GenConditioner和GenEffector，仅处理自定义类型(5207/5217)，不影响其他类型 |

#### 7.3 推荐的加载顺序

1. 其他功能Mod
2. 本插件 (LinceMultipleLovers)

### 8. 技术细节

#### 8.1 Harmony补丁优先级

所有补丁使用默认优先级，除非特别说明。

#### 8.2 数据存储

- **原版数据**: `LoveData.loverId` (保持兼容性)
- **Mod数据**: `LoveData.historyLoverIds` (扩展列表)
- **存档位置**: 与原版存档一起保存

### 9. 常见问题

#### Q: 安装后存档会损坏吗？
A: 不会。本插件兼容原版存档格式。

#### Q: 卸载后存档还能用吗？
A: 可以。卸载后只保留 `loverId` 的第一个恋人。

#### Q: 与其他修改恋人的Mod冲突吗？
A: 可能冲突。建议测试或联系作者。

### 10. 联系方式

如有兼容性问题，请提供：
1. 冲突的Mod名称
2. BepInEx日志 (`BepInEx/LogOutput.log`)
3. 复现步骤

---
