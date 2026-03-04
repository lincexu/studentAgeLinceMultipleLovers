# LinceMultipleLovers - 学生时代多恋人Mod

本文档由ai生成有改动。

## 简介

LinceMultipleLovers 是《学生时代》游戏的多恋人系统Mod，允许玩家在游戏中同时拥有多个恋人，打破了原版只能有一个恋人的限制。

## 功能特性

- ✅ **多恋人系统** - 可同时与多个角色建立恋爱关系
- ✅ **独立话题系统** - 每个恋人每回合都有独立的话题机会
- ✅ **恋爱活动** - 可与任意恋人进行看电影、打羽毛球等恋爱活动
- ✅ **条件判断** - 事件条件正确识别所有恋人（ConditionerLove / ConditionerLove2）
- ✅ **社交面板** - 社交容量面板正确显示所有恋人关系
- ✅ **毕业分享页面** - 恋人栏支持左右切换按钮浏览多个恋人
- ✅ **强制单身判定** - 可配置主角始终判定为单身（用于触发单身相关事件）
- ✅ **强制无恋爱经历** - 可配置始终判定无恋爱经历（condition [11,3] 强制返回true）
- ✅ **控制台命令** - 支持LINCE系列调试命令（添加/移除恋人、设置loverId、执行effect等）
- ✅ **恋人特性适配** - IncreaserOther中loverId相关效果遍历所有恋人生效
- ✅ **好友特性倍率适配** - 陪伴人生观「亲密度→特性加成」对所有恋人生效
- ✅ **loverId锁定** - 可锁定当前活跃恋人，阻止自动切换
- ✅ **自定义条件5207** - 恋人数量/身份/顺序条件判断，可用于事件配置
- ✅ **自定义效果5217** - 批量好感变更、分手、恋人融洽度设置等多用途效果
- ✅ **存档兼容** - 完全兼容原版存档格式

## 安装方法

### 前置需求

- BepInEx 5.4.21 或更高版本
- 《学生时代》游戏本体

### 安装步骤

1. **安装BepInEx**
   - 下载并安装 [BepInEx](https://github.com/BepInEx/BepInEx/releases)
   - 确保BepInEx正确加载

2. **安装本Mod**
   - 下载最新版本的 `LinceMultipleLovers.dll`
   - 将DLL文件放入 `BepInEx/plugins/` 文件夹

3. **启动游戏**
   - 启动游戏，Mod会自动加载
   - F1查看BepInEx控制台确认Mod加载成功

## 配置说明

配置文件位置：`BepInEx/config/lince.multiplelovers.cfg`

| 分类 | 配置项 | 默认值 | 说明 |
|------|--------|--------|------|
| 通用设置 | 启用多恋人功能 | 开 | 允许同时拥有多个恋人 |
| 通用设置 | 绕过单身检查 | 开 | 允许在已有恋人的情况下继续告白 |
| 通用设置 | 主角始终判定为单身 | 关 | 强制单身判定（用于调试或特殊玩法） |
| 通用设置 | 允许恋爱活动 | 开 | 即使开启强制单身，也允许恋爱活动触发（用于任务推进） |
| 通用设置 | 强制无恋爱经历 | 关 | 开启时condition [11,3]始终返回true，关闭时正常验证historyLover |
| 调试设置 | 启用调试日志 | 关 | 启用调试日志输出 |

> 💡 可在游戏中按 **F1** 打开BepInEx配置管理器实时修改（需安装ConfigurationManager插件）

## 控制台命令

在游戏中按 **`↑↓↑↓←→←→BABA`** 打开控制台，输入以下命令：

| 命令 | 功能 |
|------|------|
| `LINCE HELP` | 显示所有可用命令 |
| `LINCE CLEAR` | 清空控制台记录 |
| `LINCE LOVER <npcId>` | 使用游戏原生效果将指定NPC设为恋人（等效于effect [20,2,npcId,520]） |
| `LINCE BREAK <npcId>` | 与指定NPC分手，关系变为相识 |
| `LINCE LOVERID <角色ID>` | 切换当前活跃恋人为指定角色 |
| `LINCE LOVERID LOCK` | 锁定当前loverId，阻止自动切换 |
| `LINCE LOVERID UNLOCK` | 解除loverId锁定 |
| `LINCE EFFECT <type,sub,...>` | 手动执行effect指令（如: `LINCE EFFECT 60,2,3001`） |
| `LINCE ADDFOLLOW <数量>` | 增加关注上限（等效effect [20,92,X]） |
| `LINCE NPC` | 显示所有角色ID和名称 |
| `LINCE NPC ID <id>` | 查询指定ID的角色名称 |
| `LINCE NPC NAME <名字>` | 模糊搜索角色名称，显示匹配的角色ID |
| `LINCE RESOCIAL` | 刷新所有角色社交事件（重新检测可触发事件） |

### 自定义条件与效果

本Mod注入了自定义条件/效果类型，为mod提供接口：

| 类型 | 格式 | 说明 |
|------|------|------|
| 条件 5207 | `5207, 1, X` | 恋人总数 ≥ X 时满足条件 |
| 条件 5207 | `5207, -1, X` | 恋人总数 ≤ X 时满足条件 |
| 条件 5207 | `5207, 2, 1, id1, id2, …` | 所有指定角色均为恋人 |
| 条件 5207 | `5207, 2, -1, id1, id2, …` | 所有指定角色均不是恋人 |
| 条件 5207 | `5207, 3, 1, X, Y` | 角色Y是第X个恋人（按顺序，1-based） |
| 条件 5207 | `5207, 3, 2, Y` | 角色Y是最后一个恋人 |
| 条件 5207 | `5207, 3, 3, X, Y` | 角色X先于角色Y成为恋人 |
| 效果 5217 | `5217, 1, X` | 除当前loverId外，所有恋人好感 +X |
| 效果 5217 | `5217, 1, X, Y` | 除角色id=Y外，所有恋人好感 +X |
| 效果 5217 | `5217, 6, 1, X` | 与角色X分手（等同 LINCE BREAK X） |
| 效果 5217 | `5217, 7, X, Y` | 角色Y的恋人融洽度设为X（对照原版 52,7,X） |

> 命令不区分大小写。NPC ID可通过 `LINCE NPC` 命令查询。命令系统采用注册表模式，`LINCE HELP`命令自动显示所有已注册命令。
>
> 注意恋人融洽度可能后续游戏会更新会失效，请慎重使用

## 技术细节

### 核心实现

本Mod使用 Harmony 框架对游戏进行运行时补丁，主要修改以下类：

| 补丁类 | 目标类 | 功能 |
|--------|--------|------|
| LoveDataPatch | LoveData | 扩展恋人数据存储，支持多恋人 |
| RolePatch | Role | 修改关系判断逻辑 |
| MapRoleViewPatch | MapRoleView | 修复社交页面显示 |
| MapRoleViewTopicPatch | MapRoleView | 多恋人独立话题支持 |
| ConditionerLovePatch | ConditionerLove | 多恋人条件判断 + 强制无恋爱经历 |
| ConditionerLove2Patch | ConditionerLove2 | 多恋人条件判断 + 强制单身 |
| ActionDataPatch | ActionData | 修复恋爱活动解锁 |
| ActionUnlockPatch | ActionData | 行动解锁上下文管理 |
| MiniGamePatch | 各小游戏类 | 羽毛球、画画等小游戏支持 |
| QuickSocialViewPatch | QuickSocialView | 社交容量面板修复 |
| RelationDataPatch | RelationData | 关系数据扩展 |
| ShareViewPatch | ShareView | 毕业分享页面多恋人切换 |
| ConsoleCommandPatch | DebugMgr | LINCE控制台命令 |
| ConsoleHistoryPatch | DebugMgr | 控制台历史记录 |
| IncreaserOtherPatch | IncreaserOther | 恋人相关增益多恋人适配（3003/3910/3913/9） |
| RoleMgrPatch | RoleMgr | 好友特性倍率(10001)亲密度加成多恋人适配 |
| CustomConditionPatch | CommonEvtMgr | 自定义条件5207：恋人数量/身份/顺序判断 |
| CustomEffectPatch | CommonEvtMgr | 自定义效果5217：好感/分手/融洽度等多用途效果 |
| LoveDataSocialTopicPatch | LoveData | 恋人话题系统 |

### 数据存储

- 使用原版 `historyLoverIds` 列表存储所有恋人ID
- 兼容原版 `loverId` 字段
- 存档数据与原版存档一起保存

## 兼容性

### 兼容的Mod

- UI类Mod（一般兼容）
- 功能扩展类Mod（一般兼容）

### 已知冲突

- 其他修改恋人系统的Mod可能会冲突
- 建议将本Mod放在加载顺序最后

详细兼容性说明请查看 [COMPATIBILITY.md](COMPATIBILITY.md)

## 构建方法

### 环境需求

- .NET Framework 4.7.2 或 .NET 6.0

### github构建步骤

1. 克隆仓库
```bash
git clone https://github.com/lincexu/studentAgeLinceMultipleLovers.git
```

2. 打开项目
```bash
cd studentAgeLinceMultipleLovers
```

3. 构建项目
```bash
dotnet build
```

4. 输出文件位于 `bin/Debug/net472/LinceMultipleLovers.dll` 

## 项目结构

```
linceMultipleLovers/
├── LinceMultipleLoversPlugin.cs    # 主插件类，Harmony补丁注册
├── LoverIdInterceptor.cs           # 恋人ID管理
├── LastInteractedLover.cs          # 最近交互记录
├── ActionUnlockContext.cs          # 行动解锁上下文管理
├── ModConfig.cs                    # 配置管理
├── DebugCommands.cs                # 调试命令
├── Patches/                        # Harmony补丁
│   ├── LoveDataPatch.cs            # 恋人数据扩展
│   ├── RolePatch.cs                # 角色关系判断
│   ├── MapRoleViewPatch.cs         # 社交页面修复
│   ├── MapRoleViewTopicPatch.cs    # 话题系统支持
│   ├── QuickSocialViewPatch.cs     # 社交容量面板
│   ├── ConditionerLovePatch.cs     # 恋爱条件判断 + 强制无恋爱经历
│   ├── ConditionerLove2Patch.cs    # 恋爱条件判断2 + 强制单身
│   ├── ActionDataPatch.cs          # 恋爱活动修复
│   ├── ActionUnlockPatch.cs        # 行动解锁补丁
│   ├── MiniGamePatch.cs            # 小游戏支持
│   ├── RelationDataPatch.cs        # 关系数据扩展
│   ├── ShareViewPatch.cs           # 毕业分享页面
│   ├── ConsoleCommandPatch.cs      # 控制台命令
│   ├── ConsoleHistoryPatch.cs      # 控制台历史记录
│   ├── IncreaserOtherPatch.cs      # 恋人增益多恋人适配
│   ├── RoleMgrPatch.cs             # 好友特性倍率多恋人适配
│   ├── CustomConditionPatch.cs     # 自定义条件5207（恋人数量/身份/顺序判断）
│   ├── CustomEffectPatch.cs        # 自定义效果5217（好感/分手/融洽度）
│   └── LoveDataSocialTopicPatch.cs # 话题数据管理
├── COMPATIBILITY.md                # 兼容性文档
├── README.md                       # 本文件
└── LinceMultipleLovers.csproj      # 项目文件
```

## 常见问题

### Q: 安装后存档会损坏吗？
A: 目前未知。本Mod完全兼容原版存档格式，但目前处于早期开发阶段，可能有损坏的风险，建议新游戏或备份存档。

### Q: 卸载Mod后存档还能用吗？
A: 理论可以。卸载后只保留 `loverId` 的第一个恋人，其他恋人关系会消失。

### Q: 如何添加新的恋人？
A: 正常进行游戏，告白成功后自动添加到恋人列表。

### Q: 为什么话题按钮有时不显示？
A: 每个恋人每回合只能话题一次（已修改官方底层代码），请检查是否已话题过。

## 更新日志

### v0.1.3.3 (2026-03-04)
- 新增自定义条件5207：恋人数量判断（`5207,1,X` / `5207,-1,X`）、批量身份检查（`5207,2,1,id...` / `5207,2,-1,id...`）、恋人顺序/位置判断（`5207,3,1,X,Y` / `5207,3,2,Y` / `5207,3,3,X,Y`）
- 新增自定义效果5217：批量好感变更（`5217,1,X`）、分手（`5217,6,1,X`）、恋人融洽度设置（`5217,7,X,Y`）

- 新增 IncreaserOtherPatch：otherAttrId 3003（恋人最高属性匹配）、3910/3913（单身判定）、9（有无恋人分支）遍历所有恋人生效
- 新增 RoleMgrPatch：GetRateType case 10001（好友特性倍率）的亲密度加成(3002)对所有恋人生效（陪伴人生观第5级效果适配）
- 新增 LINCE EFFECT 命令：手动输入effect参数直接执行（如 `LINCE EFFECT 60,2,3001`）
- 优化 LINCE LOVERID 命令：新增 LOCK/UNLOCK 子命令，可锁定当前loverId阻止自动切换
- loverId锁定时 MapRoleViewPatch、MapRoleViewTopicPatch、QuickSocialViewPatch 不再自动切换
- LoverIdInterceptor 新增 LoverIdLocked 属性

### v0.1.3 (2026-02-28)
- 新增「强制无恋爱经历」配置项（condition [11,3] 始终返回true）
- 修复 ConditionerLovePatch 反射 bug（subType/childType 为 public 字段，反射用了 NonPublic 标志导致始终为0）
- 修复毕业分享页面至交栏显示为空的问题（ShareUI 公有字段改为直接访问）
- 修复分享页面刷新按钮与名字文字重叠（按钮位置调整）
- 控制台命令系统重构为注册表模式，新增命令自动出现在HELP列表中
- 新增 LINCE HELP 命令（自动列出所有已注册命令）
- 新增 LINCE CLEAR 命令（清空控制台记录）
- 新增 LINCE LOVERID 命令（直接设置loverId）
- 新增 LINCE ADDFOLLOW 命令（增加关注上限，等效effect [20,92,X]）
- 新增 LINCE NPC 命令（查询角色ID/名称，支持模糊搜索）
- 新增 LINCE RESOCIAL 命令（刷新所有角色社交事件）
- LINCE LOVER 命令改用游戏原生 ChangeRelation(npcId, 520) 逻辑

### v0.1.2 (2026-02-20)
- 新增「主角始终判定为单身」配置项
- 新增「允许恋爱活动」配置项（强制单身下仍可触发恋爱活动）
- 新增 ConditionerLove2Patch 支持多恋人条件判断
- 新增 ActionUnlockPatch 行动解锁上下文管理
- 新增 RelationDataPatch 关系数据扩展
- 新增 ShareViewPatch 毕业分享页面多恋人切换
- 新增 ConsoleCommandPatch 控制台命令（LINCE LOVER / BREAK）

### v0.1.0 (2026-02-12)
- 初始版本发布
- 实现多恋人核心功能
- 支持话题、活动、小游戏

## 贡献指南

欢迎提交Issue和Pull Request！

## 许可证

本项目采用 MIT 许可证 

## 致谢

- [BepInEx](https://github.com/BepInEx/BepInEx) - 插件框架
- [Harmony](https://github.com/pardeike/Harmony) - 补丁框架

## 联系方式

如有问题或建议，请通过以下方式联系：

- GitHub Issues: https://github.com/lincexu/studentAgeLinceMultipleLovers/issues
- b站: UID:491053555
- 黑盒: UID:47583706
- 邮箱: lincexu@qq.com、lincexumen@gmail.com

---

**注意**: 

（1）本mod基于BepInEx：
是对游戏代码打补丁，并非官方支持的mod创意工坊（官方不支持多个恋人），加上早期开发测试，有一定坏档风险。
仅欢迎有意愿和具备一定基础电脑常识的玩家尝试。（至少报错的时候能找得到日志文件，存档发给我）

（2）免责申明:
所有涉及的代码变更均为研究学习和同人改动，与游戏官方无任何关联。不反映、不倡导、不影射现实生活中的任何人际关系、两性观念或社会制度。Mod完全免费开源，相关代码已上传github。

（3）由于我自从游戏正式版上线以来一直都在做mod，还没有自己玩过一次，所以一直用的老版本存档测试，没有对新dlc角色测试，此外由于原版所有代码基于一个恋人，因此会有很多隐性bug未被注意。
