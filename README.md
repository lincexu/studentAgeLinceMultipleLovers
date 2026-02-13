# LinceMultipleLovers - 学生时代多恋人Mod

本文档由ai生成有改动。

## 简介

LinceMultipleLovers 是《学生时代》游戏的多恋人系统Mod，允许玩家在游戏中同时拥有多个恋人，打破了原版只能有一个恋人的限制。

## 功能特性

- ✅ **多恋人系统** - 可同时与多个角色建立恋爱关系
- ✅ **独立话题系统** - 每个恋人每回合都有独立的话题机会
- ✅ **恋爱活动** - 可与任意恋人进行看电影、打羽毛球等恋爱活动
- ✅ **小游戏支持** - 羽毛球、画画等小游戏支持多恋人
- ✅ **条件判断** - 事件条件正确识别所有恋人
- ✅ **社交面板** - 社交容量面板正确显示所有恋人关系
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

配置文件位置：`BepInEx/config/LinceMultipleLovers.cfg`

```ini
[General]
## 启用多恋人系统
# 设置类型: Boolean
# 默认值: true
EnableMultipleLovers = true

## 调试模式（输出详细日志）
# 设置类型: Boolean
# 默认值: false
DebugMode = false
```

## 调试命令（暂不支持）

在游戏中启动 `控制台` 打开聊天框，输入以下命令：

| 命令 | 功能 |
|------|------|
| `/multilover list` | 列出所有恋人 |
| `/multilover add <npcId>` | 添加指定NPC为恋人 |
| `/multilover remove <npcId>` | 移除指定恋人 |
| `/multilover clear` | 清除所有恋人 |
| `/multilover sync` | 同步数据 |

## 技术细节

### 核心实现

本Mod使用 Harmony 框架对游戏进行补丁，主要修改以下类：

- `LoveData` - 扩展恋人数据存储
- `Role` - 修改关系判断逻辑
- `MapRoleView` - 修复社交页面显示
- `ConditionerLove` - 支持多恋人条件判断
- `ActionData` - 修复恋爱活动
- `QuickSocialView` - 修复社交容量面板

### 数据存储

- 使用 `historyLoverIds` 列表存储所有恋人ID
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
├── LinceMultipleLoversPlugin.cs    # 主插件类
├── LoverIdInterceptor.cs           # 恋人ID管理
├── LastInteractedLover.cs          # 最近交互记录
├── ModConfig.cs                    # 配置管理
├── DebugCommands.cs                # 调试命令
├── Patches/                        # Harmony补丁
│   ├── LoveDataPatch.cs
│   ├── RolePatch.cs
│   ├── MapRoleViewPatch.cs
│   ├── MapRoleViewTopicPatch.cs
│   ├── QuickSocialViewPatch.cs
│   ├── ConditionerLovePatch.cs
│   ├── ActionDataPatch.cs
│   ├── MiniGamePatch.cs
│   └── LoveDataSocialTopicPatch.cs
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

### v0.1.1 (2026-02-14)
- 修复告白按钮在成为恋人后仍然显示的问题
- 修复珍贵回忆延迟触发的问题
- 修复社交容量重复计算恋人的问题
- 优化多恋人系统的稳定性

### v0.1.0 (2026-02-12)
- 初始版本发布
- 实现多恋人核心功能
- 支持话题、活动、小游戏
- 添加调试命令

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

（3）由于我自从游戏正式版上线以来一直都在做mod，还没有自己玩过一次，所以一直用的老版本存档测试，所以没有对新dlc角色测试。
