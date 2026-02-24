# LinceMultipleLovers 多恋人Mod 项目总结

## 项目概述

**项目名称**: LinceMultipleLovers  
**当前版本**: v0.1.2  
**目标游戏**: 《学生时代》(StudentAge)  
**框架**: BepInEx 5.x + HarmonyLib  
**开发语言**: C# (.NET Framework 4.7.2)

## 功能实现

### 核心功能
1. **多恋人支持**: 允许主角同时拥有多个恋人
2. **绕过单身检查**: 允许在已有恋人的情况下继续告白
3. **社交容量修复**: 正确计算多恋人的社交容量占用
4. **条件判定兼容**: 修复所有恋人相关的条件判定

### 配置选项 (BepInEx F1页面)

| 分类 | 配置项 | 默认值 | 说明 |
|------|--------|--------|------|
| 通用设置 | 启用多恋人功能 | 开 | 启用多恋人功能，允许同时拥有多个恋人 |
| 通用设置 | 绕过单身检查 | 开 | 允许在已有恋人的情况下继续告白 |
| 通用设置 | 主角始终判定为单身 | 关 | 主角单身判定始终为真（用于调试或特殊玩法） |
| 通用设置 | 允许恋爱活动 | 开 | 即使开启强制单身，也允许恋爱活动触发（用于任务推进） |
| 调试设置 | 启用调试日志 | 关 | 启用调试日志输出 |
| 关于 | 反馈说明 | - | 当前Mod仍处于测试版本，如遇Bug请联系: lincexu@qq.com |

## 文件结构

```
linceMultipleLovers/
├── LinceMultipleLoversPlugin.cs    # 主插件类，Harmony补丁入口
├── ModConfig.cs                     # BepInEx配置管理
├── MultipleLoversManager.cs         # 多恋人数据管理核心
├── LoverIdInterceptor.cs           # 恋人ID拦截器
├── DebugCommands.cs                # 调试快捷键(F1-F4)
├── Patches/
│   ├── LoveDataPatch.cs            # LoveData.SetLover补丁
│   ├── LoveDataSocialTopicPatch.cs # 社交话题补丁
│   ├── RelationDataPatch.cs        # 关系数据补丁
│   ├── QuickSocialViewPatch.cs     # 社交面板UI补丁
│   ├── ConditionerLovePatch.cs     # 恋爱条件判定补丁(类型14)
│   ├── ConditionerLove2Patch.cs    # 恋爱条件判定补丁(类型27)
│   └── EffectorChangeRelationPatch.cs # 关系变更效果补丁
├── LinceMultipleLovers.csproj      # 项目文件
└── PROJECT_SUMMARY.md              # 本文件
```

## 关键代码逻辑

### 1. 恋人列表存储机制
- **存储位置**: `LoveData.historyLoverIds` (原版字段)
- **访问方式**: `Singleton<RoleMgr>.Ins.GetLoveData().historyLoverIds`
- **存档兼容**: 使用原版字段，无需额外存档逻辑，卸载Mod后存档仍可用

### 2. SetLover补丁 (LoveDataPatch.cs)
```csharp
// 关键逻辑：不恢复loverId，保持为新恋人
// 这样珍贵回忆等系统能正确响应
// 恋人列表通过historyLoverIds维护
```

### 3. 条件判定补丁
- **ConditionerLove (类型14)**: 支持 `[14, 1, X]` 和 `[14, 3]`
- **ConditionerLove2 (类型27)**: 支持 `[52, 1, X]`, `[52, 2, X, Y]`, `[52, 22/-22]`

### 4. 社交容量计算 (QuickSocialViewPatch.cs)
- 过滤掉已是恋人的角色的其他关系占用
- 正确计算所有恋人的容量占用

## 游戏原版机制分析

### 恋人ID修改途径
1. **告白成功**: `RelationData.ChangeRelation(npcId, 520)` → `SetLover(npcId)`
2. **分手事件**: `EffectorLove.OnRun()` → `SetLover(0)`
3. **Effect接口**: `[20, 2, npcId, 520]` → `ChangeRelation` → `SetLover(npcId)`

### 条件类型编号
| 类型 | 类名 | 说明 |
|------|------|------|
| 11 | ConditionerRelation | 关系判定 |
| 14 | ConditionerLove | 恋爱判定(旧版) |
| 27 | ConditionerLove2 | 恋爱判定(新版) |

### 存档数据流
```
存档文件 ↔ RoleModel ↔ LoveData ↔ historyLoverIds
```

## 已知问题与限制

### 已解决
- [x] 社交容量计算错误
- [x] 条件判定不支持多恋人
- [x] 强制单身模式下无法触发恋爱活动

### 待实现
- [ ] `/multilover` 控制台命令（文档中提到但未实现）
- [ ] 多恋人UI显示优化

### 设计限制
- `loverId` 只能指向一个恋人（最后设置的）
- 地图问候按钮只显示给当前 `loverId`
- 恋人专属事件只触发给当前 `loverId`

## 调试功能

### 快捷键
- **F1**: 显示帮助
- **F2**: 显示当前恋人状态
- **F3**: 强制设置当前交互NPC为恋人
- **F4**: 检查配置中的恋人ID

### 日志输出
启用调试日志后，可在BepInEx控制台看到详细的条件判定信息。

## 编译与安装

### 编译命令
```bash
cd E:\steam\steamapps\common\StudentAge\linceMultipleLovers
dotnet build
```

### 安装位置
```
E:\steam\steamapps\common\StudentAge\BepInEx\plugins\LinceMultipleLovers\LinceMultipleLovers.dll
```

### 复制命令
```powershell
Copy-Item -Path "bin\Debug\net472\LinceMultipleLovers.dll" -Destination "E:\steam\steamapps\common\StudentAge\BepInEx\plugins\LinceMultipleLovers\LinceMultipleLovers.dll" -Force
```

## 依赖项

- BepInEx 5.x
- HarmonyLib
- 游戏程序集: `Assembly-CSharp.dll`
- 依赖库: `MessagePack`, `UnityEngine.dll`

## 联系方式

**作者**: Lince  
**邮箱**: lincexu@qq.com  
**版本**: v0.1.2 (测试版)

---

*此文档用于项目上下文传递，请在继续开发时保持更新。*
