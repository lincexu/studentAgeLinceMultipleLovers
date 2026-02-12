# 游戏源代码分析记录

> 记录从dnSpy分析《学生时代》(StudentAge)游戏代码的关键发现
> 用于多恋人MOD开发参考

---

## 📌 核心发现

### 1. 恋人数据获取方式

**发现位置**: `PaintView` 类 (`View.Love` 命名空间)

**关键代码**:
```csharp
// PaintView.cs 第 ~120 行
if (this.npcId == 0)
{
    this.npcId = Singleton<RoleMgr>.Ins.GetLoveData().loverId;
}
```

**结论**:
- 游戏使用 `Singleton<RoleMgr>.Ins.GetLoveData()` 获取恋人数据
- `LoveData` 类包含公共字段 `loverId` (int类型)
- NPC ID 为整数类型

---

## 🏗️ 类结构分析

### RoleMgr 类

**命名空间**: (推测为全局或 `Sdk`)

**访问方式**:
```csharp
Singleton<RoleMgr>.Ins
```

**关键方法**:
| 方法名 | 返回类型 | 说明 |
|--------|----------|------|
| `GetLoveData()` | `LoveData` | 获取恋人数据对象 |
| `GetRoleName(int, PersonNameDefine, null)` | `string` | 获取角色名称（静态） |
| `GetRole()` | `Role` | 获取当前角色（从MottoView发现） |
| `GetPersonalityFuncData(bool)` | `PersonalityFuncData` | 获取个性功能数据 |
| `HasEnoughCost(int, int, bool)` | `bool` | 检查是否有足够消耗 |

**静态方法** (从PaintView发现):
```csharp
// PaintView.cs 中的使用
RoleMgr.GetRoleName(this.npcId, PersonNameDefine.Full, null)
```
- 这是一个**静态方法**，不是实例方法
- 参数1: `int npcId` - NPC ID
- 参数2: `PersonNameDefine` 枚举 - 名称类型（Full表示全名）
- 参数3: `object` - 额外参数（可为null）
- 返回: `string` 角色名称

**实例方法** (从MottoView发现):
```csharp
// MottoView.cs 中的使用
Singleton<RoleMgr>.Ins.GetPersonalityFuncData(true)
Singleton<RoleMgr>.Ins.GetRole().IsUnlock(9021)
Singleton<RoleMgr>.Ins.HasEnoughCost(0, mottoLVCfg.cost, false)
```

**推测字段**:
- 可能包含 `loverId` 或相关恋人管理字段
- 可能包含角色数据、个性数据等

---

### LoveData 类

**继承**: `BaseData`

**关键字段**:
| 字段名 | 类型 | 说明 |
|--------|------|------|
| `loverId` | `int` | 当前恋人NPC ID |
| `breakfastId` | `int` | 早餐ID |
| `loveDate` | `int` | 开始恋爱的日期 |
| `fix` | `int` | 修复标记 |
| `historyLoverIds` | `List<int>` | 历史恋人ID列表 |
| `browserRibbonCntThisRound` | `int` | 本回合浏览器丝带数量 |
| `topicsThisRound` | `List<int>` | 本回合话题列表 |
| `socialTopicCntThisRound` | `int` | 本回合社交话题数量 |
| `hasGreeting` | `bool` | 是否已打招呼 |

**关键方法**:
| 方法名 | 参数 | 返回类型 | 说明 |
|--------|------|----------|------|
| `SetLover(int _roleId)` | `_roleId`: NPC ID | `void` | 设置/更换恋人 |
| `CanVinidcate(Role _npc)` | `_npc`: NPC角色 | `(VindicateResult, int)` | 检查是否可以表白 |
| `GetVindicateCost(Role _npc)` | `_npc`: NPC角色 | `(float, float)` | 获取表白消耗 |
| `GetVindicateSuccessRate(Role _npc)` | `_npc`: NPC角色 | `(float, float, float, float)` | 获取表白成功率 |
| `GetNewLovePaint(int npcId)` | `npcId`: NPC ID | `ValueTuple<string, string, string, List<int>>` | 获取新的恋人绘画 |
| `GetLoveBreakfast()` | 无 | `void` | 获取恋人早餐数据 |
| `NewRound()` | 无 | `void` | 新回合开始时的处理 |
| `CheckLoveBreakfast(bool)` | `bool` | `void` | 检查恋人早餐 |
| `GetLoveYear()` | 无 | `int` | 获取恋爱年数 |

**CanVinidcate 方法详细逻辑**:
```csharp
public ValueTuple<VindicateResult, int> CanVinidcate(Role _npc)
{
    // 检查是否已有恋人（原版限制）
    if (this.loverId > 0)
    {
        return (VindicateResult.NeedSingle, 0);
    }
    
    // 检查年级（需要>6年级）
    if (Singleton<RoleMgr>.Ins.GetRole().Grade <= 6)
    {
        return (VindicateResult.NeedOlder, 0);
    }
    
    // 检查关系等级（需要>=4级，即挚友）
    if (_npc.Relation < 4)
    {
        return (VindicateResult.NeedCloseFriend, 0);
    }
    
    // 检查成功率
    if (this.GetVindicateSuccessRate(_npc).Item1 < RoleMgr.GetConstValue(3405))
    {
        return (VindicateResult.LowSuccessRate, 0);
    }
    
    // 检查心情和信任值
    var cost = this.GetVindicateCost(_npc);
    if (!Singleton<RoleMgr>.Ins.HasEnoughMood(cost.Item1) || 
        !Singleton<RoleMgr>.Ins.HasEnoughTrust(cost.Item2))
    {
        return (VindicateResult.NeedMoodOrTrust, 0);
    }
    
    // 检查是否有可用事件
    int enableEvtId = Singleton<CommonEvtMgr>.Ins.GetEnableEvtId(520, _npc.id, -1, false);
    if (enableEvtId <= 0)
    {
        return (VindicateResult.NoEvt, 0);
    }
    
    // 可以表白
    return (VindicateResult.Success, enableEvtId);
}
```

**VindicateResult 枚举**:
```csharp
enum VindicateResult
{
    Success,           // 可以表白
    NeedSingle,        // 需要单身（已有恋人）
    NeedOlder,         // 需要更高年级
    NeedCloseFriend,   // 需要挚友关系
    LowSuccessRate,    // 成功率太低
    NeedMoodOrTrust,   // 需要心情或信任值
    NoEvt              // 没有可用事件
}
```

**GetVindicateSuccessRate 方法**:
```csharp
public ValueTuple<float, float, float, float> GetVindicateSuccessRate(Role _npc)
{
    float baseRate = _npc.GetAttr(9009, false);
    
    if (!Cfg.LoveVindicateRateCfgMap.ContainsKey(_npc.id))
    {
        return (Mathf.Clamp(baseRate, 0f, 1f), 0f, 0f, baseRate);
    }
    
    Role role = Singleton<RoleMgr>.Ins.GetRole();
    LoveVindicateRateCfg cfg = Cfg.LoveVindicateRateCfgMap[_npc.id];
    
    // 计算专注度加成
    float focusAttr = (_npc.FocusId > 0) ? role.GetAttr(_npc.FocusId, false) : 0f;
    float focusBonus = cfg.attrParms[0] * Mathf.Log10(cfg.attrParms[1] * focusAttr + cfg.attrParms[3]) 
                     / Mathf.Log10(cfg.attrParms[2] + cfg.attrParms[4]);
    
    // 计算好感度加成
    float favor = _npc.GetFavor(false);
    float favorBonus = cfg.favorParms[0] * Mathf.Log10(cfg.favorParms[1] * favor + cfg.favorParms[3]) 
                     / Mathf.Log10(cfg.favorParms[2] + cfg.favorParms[4]);
    
    return (Mathf.Clamp(focusBonus + favorBonus + baseRate, 0f, 0.9f), focusBonus, favorBonus, baseRate);
}
```

**SetLover 方法详细逻辑**:
```csharp
public void SetLover(int _roleId)
{
    int num = this.loverId;  // 保存原恋人ID
    this.loverId = _roleId;   // 设置新恋人ID
    this.breakfastId = 0;     // 重置早餐ID
    this.fix = 1;             // 设置修复标记
    
    // 更新角色属性
    Singleton<RoleMgr>.Ins.GetRole().SetAttr(520, 5f, 0f);
    
    if (_roleId > 0)
    {
        // 添加恋人
        this.loveDate = Singleton<RoundMgr>.Ins.Now();  // 记录恋爱日期
        
        // 添加到历史记录
        if (this.historyLoverIds == null)
            this.historyLoverIds = new List<int>();
        if (!this.historyLoverIds.Contains(_roleId))
            this.historyLoverIds.Add(_roleId);
        
        // 开启功能
        Singleton<FuncMgr>.Ins.OpenFunc(20, true);
        
        // 添加记录
        Singleton<RecordMgr>.Ins.AddRecord(4, null, new float[] { _roleId, 520f });
        
        // 显示提示
        ToastHelper.Toast<string>(116, new string[] { 
            RoleMgr.GetRoleName(_roleId, PersonNameDefine.Full, null),
            Cfg.RelationCfgMap[520].name 
        });
    }
    else if (num > 0)
    {
        // 分手/清除恋人
        Singleton<FuncMgr>.Ins.CloseFunc(20);
        ToastHelper.Toast<string>(861, new string[] { 
            RoleMgr.GetRoleName(num, PersonNameDefine.Full, null) 
        });
    }
    
    // 检查成就
    Singleton<GlobalMgr>.Ins.CheckAchievement(52, 0);
    
    // 发送事件
    EventMgr.Send(1601);
}
```

**NewRound 方法逻辑**:
```csharp
public override void NewRound()
{
    // 重置每回合数据
    this.browserRibbonCntThisRound = (int)RoleMgr.GetConstValue(3401);
    this.topicsThisRound?.Clear();
    this.socialTopicCntThisRound = 0;
    this.hasGreeting = false;
    this.breakfastId = 0;
    
    if (this.loverId == 0)
        return;
    
    // 检查恋人早餐
    this.CheckLoveBreakfast(false);
    
    // 更新角色属性（消耗）
    Singleton<RoleMgr>.Ins.GetRole().UpdateAttr(520, -(float)this.GetLoveYear(), 1f, null, 2);
    
    // 检查恋人生日
    Role role = Singleton<RoleMgr>.Ins.GetRole(this.loverId);
    if (role != null && role.IsBirthday())
    {
        Singleton<TipsMgr>.Ins.AddNotifyTxt(
            DescCtrl.GetTxt<string>(1098, new string[] { role.Name }), 
            0, null
        );
    }
}
```

---

### PersonalityFuncData 类 (从MottoView发现)

**获取方式**:
```csharp
Singleton<RoleMgr>.Ins.GetPersonalityFuncData(true)
```

**关键字段**:
| 字段名 | 类型 | 说明 |
|--------|------|------|
| `mottoId` | `int` | 当前座右铭ID |
| `mottos` | `List<int>` | 座右铭列表 |
| `mottoNames` | `Dictionary<int, string>` | 座右铭自定义名称 |
| `mottoRestRound` | `int` | 座右铭剩余回合 |
| `openMottoViewThisRound` | `bool` | 本回合是否打开过座右铭界面 |

**关键方法**:
| 方法名 | 说明 |
|--------|------|
| `UpdateMotto(int id, string name)` | 更新座右铭 |
| `GetMottoLv(int id)` | 获取座右铭等级 |

**使用示例**:
```csharp
// 获取数据
var pData = Singleton<RoleMgr>.Ins.GetPersonalityFuncData(true);

// 检查当前座右铭
if (pData.mottoId == 0)
{
    pData.openMottoViewThisRound = true;
    EventMgr.Send(105);
}

// 更新座右铭
pData.UpdateMotto(id, cell.inputex_name.text);

// 获取座右铭等级
int mottoLv = pData.GetMottoLv(num);
```

---

### LoveBreakfastView 类

**命名空间**: `View.Love`

**功能**: 恋人早餐界面

**关键发现**:
```csharp
int breakfastId = Singleton<RoleMgr>.Ins.GetLoveData().breakfastId;
LoveBreakfastCfg loveBreakfastCfg = Cfg.LoveBreakfastCfgMap[breakfastId];
```

**说明**: `LoveData` 还包含 `breakfastId` 字段

---

### PaintView 类

**命名空间**: `View.Love`

**功能**: 恋人绘画界面

**关键代码片段**:

```csharp
public class PaintView : PaintUI
{
    private int npcId;
    private MiniGameFromType type;
    
    public override void OnOpen()
    {
        // ... 参数处理 ...
        
        if (this.type == MiniGameFromType.Love)
        {
            if (this.npcId == 0)
            {
                // 关键：获取当前恋人ID
                this.npcId = Singleton<RoleMgr>.Ins.GetLoveData().loverId;
            }
            // 获取绘画数据
            this.paint = Singleton<RoleMgr>.Ins.GetLoveData().GetNewLovePaint(this.npcId);
        }
    }
}
```

---

### MottoView 类

**命名空间**: `View.Role`

**功能**: 座右铭界面

**关键发现**:
```csharp
// 获取角色和个性数据
this.role = Singleton<RoleMgr>.Ins.GetRole();
this.pData = Singleton<RoleMgr>.Ins.GetPersonalityFuncData(true);

// 使用 PersonalityFuncData
if (pData.mottoId == 0)
{
    pData.openMottoViewThisRound = true;
    EventMgr.Send(105);
}

// 更新座右铭
Singleton<RoleMgr>.Ins.GetPersonalityFuncData(true).UpdateMotto(id, cell.inputex_name.text);
```

---

### RelationData 类

**继承**: `BaseData, IRedpoint`

**核心字段**:
| 字段名 | 类型 | 说明 |
|--------|------|------|
| `relationDict` | `Dictionary<int, List<int>>` | 关系字典：关系ID -> 角色ID列表 |

**核心方法**:
| 方法名 | 参数 | 返回 | 说明 |
|--------|------|------|------|
| `ChangeRelation` | `_roleId, _relationId, _tag, _focusBefore` | `bool` | 改变关系（520=恋人） |
| `GetRelationship` | `_relationId` | `List<int>` | 获取指定关系的角色列表 |
| `GetAllRelationShip` | 无 | `Dictionary<int, List<int>>` | 获取所有关系 |
| `InRelationList` | `_roleId, _relationType` | `bool` | 检查角色是否在关系中 |
| `GetRelationCnt` | `_relationId, _contain` | `int` | 获取关系数量 |
| `HasRelation` | 无 | `bool` | 检查是否有任何关系 |
| `GetSexRelationCnt` | `_relationId, _sex` | `int` | 按性别统计关系 |
| `GetFavorMax` | 无 | `(int, float)` | 获取最高好感度角色 |
| `GetOtherRelation` | `_type` | `List<int>` | 获取其他关系 |
| `GetOtherRelationCnt` | `_type` | `int` | 获取其他关系数量 |
| `AddRelation` | `_relationId, _roleId, _sendEvt` | `void` | 添加关系 |
| `IsMatchRelationType` | `_role, _relationType` | `bool` | **检查是否匹配关系类型（关键！）** |
| `GetRelationType` | `_role` | `List<int>` | 获取角色所有关系类型 |
| `IsNpcFavorLowerThan` | `_v` | `bool` | 检查是否有NPC好感度低于指定值 |
| `GetRelationAchReward` | `_relation` | `float` | 获取关系成就奖励 |
| `GetAllSocialNpcs` | `_includeDLC` | `List<int>` | 获取所有可社交NPC |
| `IsNpcAppearInThisGame` | `_id, _includeDLC` | `bool` | 检查NPC是否在本局游戏出现 |
| `GetUnknownFriendId` | 无 | `int` | 获取未知朋友ID |
| `ReFocusNpc` | `_id` | `void` | 重新关注NPC |
| `MakeAcquaintances` | `_id` | `void` | 与NPC结识 |
| `CanFocusNPC` | 无 | `bool` | 检查是否可以关注NPC |
| `ShowFocusNPCView` | 无 | `void` | 显示关注NPC界面 |
| `GetSearchFriendNeedEQ` | 无 | `float` | 获取交友所需情商 |
| `NPCLeave` | `_npcId, _forever, _subType` | `void` | NPC离开 |
| `NPCBack` | `_npcId, _mapId` | `void` | NPC返回 |
| `UnFocus` | `_npcId` | `void` | 取消关注NPC |
| `CanUnFocus` | `_npcId` | `bool` | 检查是否可以取消关注 |
| `RefreshSocialCapacity` | 无 | `void` | 刷新社交容量 |
| `GetSocialCapacity` | 无 | `int` | 获取社交容量 |
| `CheckSocialEvtRedpoint` | 无 | `void` | 检查社交事件红点 |
| `CheckNewFriendRedpoint` | 无 | `void` | 检查新朋友红点 |
| `CheckRedpoint` | `_type, _id` | `void` | 检查红点 |
| `IsNpcRedpointShow` | `_id` | `bool` | 检查NPC红点是否显示 |
| `HasNpcEvtRedpoint` | `_evtId` | `bool` | 检查是否有NPC事件红点 |
| `RemoveNpcRedpoint` | `_evtId` | `void` | 移除NPC红点 |
| `AddNpcRedpoint` | `_evtId` | `void` | 添加NPC红点 |
| `NewRound` | 无 | `void` | 新回合处理 |
| `GetOrderRelationCfgs` | 无 | `List<RelationCfg>` | 获取排序后的关系配置 |
| `CheckOldSave` | 无 | `void` | 检查旧存档 |

**核心字段**:
| 字段名 | 类型 | 说明 |
|--------|------|------|
| `relationDict` | `Dictionary<int, List<int>>` | 关系字典（关系ID -> 角色ID列表） |
| `lastInviteRound` | `int` | 上次邀请回合 |
| `searchFriendCnt` | `int` | 交友计数 |
| `unFocusCnt` | `int` | 取消关注计数 |
| `rewardIds` | `Dictionary<int, int>` | 奖励ID字典 |
| `showSocialEvtRedpointInThisRounds` | `List<int>` | 本回合显示社交事件红点的NPC |
| `showFocusNpcViewInThisRound` | `bool` | 本回合是否显示关注NPC界面 |
| `orderRelationCfgs` | `List<RelationCfg>` | 排序后的关系配置 |
| `enableFocusCnt` | `int` | 可关注数量（默认4） |
| `specialMakeAcquinceMode` | `int` | 特殊结识模式 |
| `npcEvtRedpoints` | `List<int>` | NPC事件红点列表 |
| `jumpFocusViewThisRound` | `bool` | 本回合是否跳过关注界面 |
| `socialCapacity` | `int` | 社交容量 |
| `setSocialCapacityDirty` | `bool` | 社交容量是否需要刷新 |
| `energyMaxDownByNegativeSocialCapacityEffectUid` | `ulong` | 负社交容量影响效果UID |
| `socialCntAddByDownFavorNpcUid` | `ulong` | 降低好感度NPC增加社交次数效果UID |

**关键发现 - ChangeRelation 方法**:
```csharp
public bool ChangeRelation(int _roleId, int _relationId, string _tag, bool _focusBefore)
{
    // 关系ID 520 表示恋人
    if (_relationId == 520)
    {
        Singleton<RoleMgr>.Ins.GetLoveData().SetLover(_roleId);
        return true;
    }
    // ... 其他关系处理
}
```

**关键发现 - IsMatchRelationType 方法（重要！）**:
```csharp
public bool IsMatchRelationType(Role _role, int _relationType)
{
    if (_relationType == 520)  // 恋人关系检查
    {
        // ⚠️ 问题：只检查当前恋人！
        return Singleton<RoleMgr>.Ins.GetLoveData().loverId == _role.id;
    }
    // ... 其他关系检查
}
```

**关系ID定义**:
| 关系ID | 说明 |
|--------|------|
| `520` | 恋人（特殊） |
| `1-6` | 普通关系（朋友、挚友等） |
| `-1` | 陌生人/解除关系 |
| `0` | 初始状态 |
| `21` | 最高好感度 |
| `22` | 最低好感度 |
| `23` | 异性最高好感度 |
| `-11` | 同性 |
| `-12` | 异性 |

---

## 🔍 配置类分析

### Cfg 类

**静态配置访问器**:
| 配置名 | 类型 | 说明 |
|--------|------|------|
| `LoveBreakfastCfgMap` | `Dictionary<int, LoveBreakfastCfg>` | 早餐配置 |
| `LoveDrawCfgMap` | `Dictionary<int, LoveDrawCfg>` | 绘画配置 |
| `ItemCfgMap` | `Dictionary<int, ItemCfg>` | 物品配置 |
| `MottoCfgMap` | `Dictionary<int, MottoCfg>` | 座右铭配置 |
| `MottoLVCfgMap` | `Dictionary<int, MottoLVCfg>` | 座右铭等级配置 |

---

## 🎮 游戏系统枚举

### MiniGameFromType

```csharp
enum MiniGameFromType
{
    Love,    // 恋人相关
    Option,  // 选项相关
    // ... 其他类型
}
```

---

## 📝 关键方法签名

### 社交/恋人相关方法（推测）

基于代码模式推测可能存在的方法：

```csharp
// RoleMgr 类 - 已确认的方法
public Role GetRole()                                    // 获取主角
public Role GetRole(int _roleId)                         // 获取指定角色
public static string GetRoleName(int _roleId, PersonNameDefine _type, Dictionary<int, PersonCfg> _personCfgMap)  // 获取角色名称
public Dictionary<int, Role> GetRoleDict()               // 获取角色字典

// 数据获取方法
public RelationData GetRelationData(bool _nullAndCreate = true)   // 获取关系数据
public ActionData GetActionData()                        // 获取行动数据
public StudyData GetStudyData(bool nullAndCreate = true) // 获取学习数据
public IntentData GetIntentData()                        // 获取意图数据
public ValueviewData GetValueviewData()                  // 获取价值观数据
public SkillData GetSkillData()                          // 获取技能数据
public WritingData GetWritingData()                      // 获取写作数据
public DIYData GetDIYData()                              // 获取DIY数据
public EGameData GetEGameData()                          // 获取游戏数据
public NegotiationData GetNegotiationData(bool _nullAndCreate = true)  // 获取谈判数据
public SportData GetSportData()                          // 获取运动数据
public AchievementData GetAchievementData()              // 获取成就数据
public DivinationData GetDivinationData()                // 获取占卜数据
public DNDData GetDNDData(bool _nullAndCreate = true)    // 获取DND数据
public KZoneData GetKZoneData()                          // 获取KZone数据
public TelephoneData GetTelephoneData()                  // 获取电话数据
public PartyData GetPartyData()                          // 获取聚会数据
public LoveData GetLoveData()                            // **获取恋人数据（核心！）**
public RenshengguanData GetRenshengguanData()            // 获取人生观数据
public NeedsData GetNeedsData()                          // 获取需求数据
public TripData GetTripData()                            // 获取旅行数据
public NpcSpecialData GetNpcSpecialData()                // 获取NPC特殊数据

// 成本检查方法
public bool HasEnoughEnergy(float _cost)                 // 检查精力
public bool HasEnoughMoney(float _cost)                  // 检查金钱
public bool HasEnoughTrust(float _cost)                  // 检查信任
public bool HasEnoughMood(float _cost)                   // 检查心情
public bool HasEnoughMotivation(float _cost)             // 检查动力
public bool HasEnoughCost(int _id, float _cost, bool _isBigger = false)  // 通用成本检查

// 好感度方法
public void AddRoleFavor(int _roleId, float _favor, float _efficiency = 1f)  // 增加好感度
public float GetFavor(int _roleId)                       // 获取好感度

// LoveData 类
public int loverId;  // 字段
public int breakfastId;  // 字段
public ValueTuple<string, string, string, List<int>> GetNewLovePaint(int npcId)
public void GetLoveBreakfast()

// PersonalityFuncData 类
public int mottoId;
public List<int> mottos;
public Dictionary<int, string> mottoNames;
public int mottoRestRound;
public bool openMottoViewThisRound;
public void UpdateMotto(int id, string name)
public int GetMottoLv(int id)

// 可能的验证方法（待确认）
public bool IsLover(int npcId)
public bool HaveLover()
public void SetLover(int npcId)
```

---

### RoleMgr 类 - GetLoveData 方法（核心！）

```csharp
public LoveData GetLoveData()
{
    if (this.model == null)
    {
        return null;
    }
    LoveData result;
    if ((result = this.model.loveData) == null)
    {
        result = (this.model.loveData = new LoveData());
    }
    return result;
}
```

**关键信息**：
- 从 `this.model.loveData` 获取恋人数据
- 如果为null，创建新的 `LoveData()`
- 这是MOD需要拦截的核心方法！

---

## 🎯 MOD补丁策略

### 已实现的补丁

1. **LoveDataPatch.cs**
   - 拦截 `GetLoveData()` 方法
   - 修改返回对象的 `loverId` 字段
   - 管理多恋人ID列表

2. **RoleMgrLovePatch.cs**
   - 自动扫描 `RoleMgr` 类
   - 查找恋人相关方法

3. **SocialValidationPatch.cs**
   - 扫描社交相关类
   - 拦截验证逻辑

### 已确认的信息

- [x] `LoveData` 类完整字段和方法（从源代码获取）
- [x] `RelationData` 类完整字段和方法（从源代码获取）
- [x] 表白方法：`CanVinidcate`, `Vindicate`
- [x] 社交验证方法：`CheckGreeting`, `GetTopics`
- [x] 关键问题：`IsMatchRelationType` 只检查当前恋人

### 已确认的信息（更新）

- [x] `LoveData` 类完整字段和方法（从源代码获取）
- [x] `RelationData` 类完整字段和方法（从源代码获取）
- [x] `RoleMgr` 类核心方法（从源代码获取）
- [x] `RoleMgr.GetLoveData()` 方法（**核心！**）
- [x] 表白方法：`CanVinidcate`, `Vindicate`
- [x] 社交验证方法：`CheckGreeting`, `GetTopics`
- [x] 关键问题：`IsMatchRelationType` 只检查当前恋人

### 待确认的信息

- [ ] `RoleMgr` 类的完整字段列表（部分已确认）
- [ ] 恋人关系存储的持久化机制

---

## 🔧 技术细节

### Unity相关

- 游戏使用 Unity 引擎
- UI系统：自定义UI框架 (GenUI)
- 动画：DOTween
- 异步：UniTasks

### 关键命名空间

```csharp
using Sdk;           // SDK相关，包含Singleton
using Config;        // 配置相关
using GenUI.Love;    // 恋人UI相关
using View.Love;     // 恋人视图
using View.Role;     // 角色视图
```

### 单例模式

```csharp
// 游戏使用的单例访问方式
Singleton<RoleMgr>.Ins
Singleton<CommonEvtMgr>.Ins
```

### 事件系统

```csharp
// 从MottoView发现
EventMgr.Send(105);  // 发送事件
base.UpdateListener(2801, new EventCallback(this.Refresh));  // 监听事件
```

---

## 📚 参考文件

### 已完整分析的代码文件

- [x] `LoveData.cs` - 恋人数据类（**完整**）
- [x] `RelationData.cs` - 关系数据类（**完整**）
- [x] `LoveBreakfastView.cs` - 恋人早餐界面
- [x] `PaintView.cs` - 恋人绘画界面（完整）
- [x] `MottoView.cs` - 座右铭界面（完整）

### 待分析的代码文件

- [ ] `RoleMgr.cs` - 角色管理器（核心）
- [ ] 社交系统相关类
- [ ] 表白/剧情事件相关类

---

## 🐛 已知问题/注意事项

1. **存档兼容性**: MOD修改的是运行时数据，存档中可能只存储单个恋人ID
2. **剧情事件**: 某些剧情可能硬编码只处理单个恋人
3. **UI显示**: 游戏UI可能未设计多恋人显示

---

## 📝 更新日志

### 2024-XX-XX (最新)
- 完整分析 `RelationData` 类
- 发现 `ChangeRelation` 方法是设置恋人的核心
- 发现 `IsMatchRelationType` 只检查当前恋人（关键问题！）
- 发现关系ID定义：520=恋人, 1-6=普通关系, 21-23=特殊关系
- 记录所有 `RelationData` 方法：AddRelation, GetRelationship, InRelationList 等

### 2024-XX-XX
- 完整分析 `LoveData` 类
- 发现 `SetLover`, `CanVinidcate`, `Vindicate` 等核心方法
- 发现 `CheckGreeting`, `GetTopics`, `CanGiveBirthdayGift` 等社交方法
- 记录完整字段列表：loverId, historyLoverIds, loveDate 等

### 2024-XX-XX
- 分析 `MottoView` 类
- 发现 `PersonalityFuncData` 类完整结构
- 发现 `RoleMgr.GetRole()` 和 `RoleMgr.HasEnoughCost()` 方法
- 发现事件系统 `EventMgr.Send()`

### 2024-XX-XX
- 从PaintView发现 `RoleMgr.GetRoleName()` 是**静态方法**
- 确认方法签名: `RoleMgr.GetRoleName(int npcId, PersonNameDefine nameType, object param)`
- 发现 `PersonNameDefine.Full` 枚举值

### 2024-XX-XX
- 初始分析
- 发现 `LoveData.loverId` 字段
- 确认 `Singleton<RoleMgr>.Ins.GetLoveData()` 访问模式

---

## 🔗 相关资源

- 游戏路径: `e:\steam\steamapps\common\StudentAge`
- 主程序集: `StudentAge_Data/Managed/Assembly-CSharp.dll`
- MOD路径: `BepInEx/plugins/MultiLoverMod/`

---

*此文档用于MOD开发参考，基于dnSpy反编译分析*
