using Config;
using Effect;
using LinceMultipleLovers.Patches;
using Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheEntity;
using UnityEngine;

namespace LinceMultipleLovers.Patches
{
    /// <summary>
    /// 自定义效果补丁
    /// 
    /// 通过Prefix拦截GenEffector工厂方法，注入自定义效果类型5217/5001。
    /// 
    /// 支持的格式:
    ///   [5001, 1, X]       — 当前年级设置为X
    ///   [5001, 2, X, Y]    — 当前年月设置为X年Y月
    ///   [5217, 0, X]       — 所有恋人好感 +X（不排除任何人）
    ///   [5217, 1, X]       — 除当前loverId外，所有恋人好感 +X
    ///   [5217, 1, X, Y]    — 除角色Y外，所有恋人好感 +X
    ///   [5217, 6, 1, X]    — 与角色X分手（等同于 LINCE BREAK X）
    ///   [5217, 7, X, Y]    — 角色Y的恋人融洽度设为X（对照原版 52,7,X）
    /// </summary>
    public static class CustomEffectPatch
    {
        public static bool GenEffector_Prefix(List<float> _effect, ref Effector _effector, ref Effector __result, int _toRoleId, int _fromRoleId)
        {
            if (_effect == null || _effect.Count < 3)
                return true;

            int type = (int)_effect[0];
            if (type != 5217 && type != 5001)
                return true; // 非自定义类型，走原版逻辑

            try
            {
                int subType = (int)_effect[1];
                Effector newEffector;

                if (type == 5001)
                {
                    switch (subType)
                    {
                        case 1:
                        case 2:
                            newEffector = new CustomEffectorSetGradeTime(_effector, _effect);
                            break;
                        default:
                            LinceMultipleLoversPlugin.Log.LogWarning(
                                $"[CustomEffect 5001] 未知subType={subType}，跳过");
                            return true;
                    }
                }
                else
                {
                    switch (subType)
                    {
                        case 0:
                        case 1:
                            newEffector = new CustomEffectorAllLoversFavor(_effector, _effect);
                            break;
                        case 6:
                            newEffector = new CustomEffectorBreakUp(_effector, _effect);
                            break;
                        case 7:
                            newEffector = new CustomEffectorLoverFix(_effector, _effect);
                            break;
                        default:
                            LinceMultipleLoversPlugin.Log.LogWarning(
                                $"[CustomEffect 5217] 未知subType={subType}，跳过");
                            return true;
                    }
                }

                newEffector.toRoleId = _toRoleId;
                newEffector.fromRoleId = _fromRoleId;
                _effector = newEffector;
                __result = newEffector;

                if (ModConfig.DebugMode.Value)
                {
                    LinceMultipleLoversPlugin.Log.LogInfo(
                        $"[CustomEffect {type}] 创建效果: subType={subType}, 类型={newEffector.GetType().Name}");
                }

                return false; // 跳过原版GenEffector
            }
            catch (Exception ex)
            {
                LinceMultipleLoversPlugin.Log.LogError($"[CustomEffect] 创建失败: {ex}");
                return true; // 出错时走原版（会输出"无此效果"）
            }
        }
    }

    /// <summary>
    /// 时间/年级设置效果 (类型5001)
    ///
    /// 格式:
    ///   [5001, 1, X]    -> 当前年级设置为X（对齐控制台1202逻辑）
    ///   [5001, 2, X, Y] -> 当前年月设置为X年Y月（对齐控制台1402逻辑）
    /// </summary>
    public class CustomEffectorSetGradeTime : Effector
    {
        private int subType;
        private int yearOrGrade;
        private int month;

        public CustomEffectorSetGradeTime() { }

        public CustomEffectorSetGradeTime(Effector _effector, List<float> _effect)
            : base(_effector, _effect)
        {
            this.subType = (int)_effect[1];
            this.yearOrGrade = _effect.Count >= 3 ? (int)_effect[2] : 0;
            this.month = _effect.Count >= 4 ? (int)_effect[3] : 0;
        }

        public override void OnRun(float _rate = 1f, bool _toast = false)
        {
            if (this.subType == 1)
            {
                if (this.yearOrGrade <= 0)
                {
                    LinceMultipleLoversPlugin.Log.LogWarning(
                        $"[Effect 5001.1] 参数无效: grade={this.yearOrGrade}");
                    return;
                }

                Role role = Singleton<RoleMgr>.Ins?.GetRole();
                if (role == null)
                {
                    LinceMultipleLoversPlugin.Log.LogWarning("[Effect 5001.1] 主角Role为空，跳过");
                    return;
                }

                role.SetGrade(this.yearOrGrade);
                if (ModConfig.DebugMode.Value)
                {
                    LinceMultipleLoversPlugin.Log.LogInfo(
                        $"[Effect 5001.1] 当前年级设置为: {this.yearOrGrade}");
                }
                return;
            }

            if (this.subType == 2)
            {
                if (this.yearOrGrade <= 0 || this.month <= 0)
                {
                    LinceMultipleLoversPlugin.Log.LogWarning(
                        $"[Effect 5001.2] 参数无效: year={this.yearOrGrade}, month={this.month}");
                    return;
                }

                if (Singleton<RoundMgr>.Ins == null)
                {
                    LinceMultipleLoversPlugin.Log.LogWarning("[Effect 5001.2] RoundMgr为空，跳过");
                    return;
                }

                Singleton<RoundMgr>.Ins.SetTime(this.yearOrGrade, this.month);
                if (ModConfig.DebugMode.Value)
                {
                    LinceMultipleLoversPlugin.Log.LogInfo(
                        $"[Effect 5001.2] 当前年月设置为: {this.yearOrGrade}年{this.month}月");
                }
                return;
            }

            LinceMultipleLoversPlugin.Log.LogWarning($"[Effect 5001] 未知subType={this.subType}，跳过执行");
        }

        public override string OnToString(float _rate = 1f, int _type = 0)
        {
            if (this.subType == 1)
            {
                return $"当前年级设置为{this.yearOrGrade}";
            }

            if (this.subType == 2)
            {
                return $"当前年月设置为{this.yearOrGrade}年{this.month}月";
            }

            return null;
        }
    }

    /// <summary>
    /// 自定义效果器：全恋人好感变更 (类型5217)
    /// 
    /// 格式: [5217, subType, X] 或 [5217, subType, X, Y]
    ///   subType=1: 对所有恋人列表中排除指定角色后的恋人增加X好感度
    ///   无Y参数时排除当前loverId，有Y参数时排除角色id=Y
    ///   （手动复现UpdateFavor逻辑，合并为一条Toast显示）
    /// 
    /// 使用场景示例：
    ///   [5217, 1, 5]      → 除当前恋人外，所有其他恋人好感+5
    ///   [5217, 1, -10]    → 除当前恋人外，所有其他恋人好感-10
    ///   [5217, 1, 5, 102] → 除角色102外，所有其他恋人好感+5
    /// </summary>
    public class CustomEffectorAllLoversFavor : Effector
    {
        private int subType;
        private float value;
        private int excludeRoleId; // 0表示使用当前loverId，>0表示排除指定角色id

        // Favor属性有private set，需要反射写入
        private static readonly PropertyInfo FavorProperty =
            typeof(Role).GetProperty("Favor", BindingFlags.Public | BindingFlags.Instance);

        public CustomEffectorAllLoversFavor() { }

        public CustomEffectorAllLoversFavor(Effector _effector, List<float> _effect)
            : base(_effector, _effect)
        {
            this.subType = (int)_effect[1];
            this.value = _effect[2];
            this.excludeRoleId = _effect.Count >= 4 ? (int)_effect[3] : 0;
        }

        public override void OnRun(float _rate = 1f, bool _toast = false)
        {
            if (this.subType != 0 && this.subType != 1)
            {
                LinceMultipleLoversPlugin.Log.LogWarning(
                    $"[CustomEffect 5217] 未知subType={this.subType}，跳过执行");
                return;
            }

            var loveData = Singleton<RoleMgr>.Ins?.GetLoveData();
            if (loveData == null)
            {
                LinceMultipleLoversPlugin.Log.LogWarning("[CustomEffect 5217] LoveData为null，跳过执行");
                return;
            }

            int currentLoverId = loveData.loverId;
            // subType=0时不排除任何人; subType=1时有Y参数排除Y，否则排除当前loverId
            int skipId = this.subType == 0 ? -1 : (this.excludeRoleId > 0 ? this.excludeRoleId : currentLoverId);
            var allLoverIds = LoverIdInterceptor.GetAllLoverIds();
            float favorChange = this.value * _rate;

            // 收集所有受影响恋人的名字和实际好感变化，手动更新好感避免逐个弹Toast
            var affectedNames = new List<string>();
            float totalRealChange = 0f;

            foreach (int loverId in allLoverIds)
            {
                if (loverId <= 0 || loverId == skipId)
                    continue;

                Role role = Singleton<RoleMgr>.Ins.GetRole(loverId);
                if (role == null || role.IsMainRole)
                    continue;

                // 未认识且未关注的角色不加好感（与原版UpdateFavor一致）
                if (role.Relation == 0 && role.unFocusCnt == 0)
                    continue;

                // 手动计算并更新好感（复现UpdateFavor逻辑，但不触发Toast）
                float realAddFavor = role.GetRealAddFavor(favorChange, 1f);
                float newFavor = Mathf.Max(role.Favor + realAddFavor, -999f);
                FavorProperty.SetValue(role, newFavor);

                // 触发原版的相关事件（与UpdateFavor一致）
                EventMgr.Send(1301);
                if (Singleton<FuncMgr>.Ins.IsFuncOpened(401))
                {
                    Singleton<RoleMgr>.Ins.GetKZoneData().AddVisit(loverId);
                }
                Singleton<GlobalMgr>.Ins.CheckAchievement(999, 0);
                role.CheckTaskEvt();
                Singleton<RecordMgr>.Ins.AddRecord(3, null, new float[]
                {
                    (float)loverId,
                    realAddFavor
                });

                affectedNames.Add(role.Name);
                totalRealChange = realAddFavor; // 同批次变化值相同，取最后一个即可

                if (ModConfig.DebugMode.Value)
                {
                    LinceMultipleLoversPlugin.Log.LogInfo(
                        $"[CustomEffect 5217] 恋人{loverId}({role.Name})好感度变更: {realAddFavor:+0.#;-0.#}");
                }
            }

            // 合并为一条Toast显示
            if (affectedNames.Count > 0)
            {
                string namesStr = string.Join("、", affectedNames);
                string changeStr = HtmlTxtUtil.ToStr(totalRealChange, "{0:0.#}", 0, false);
                string toastText = DescCtrl.GetTxt<string>(117, new string[]
                {
                    namesStr,
                    changeStr
                });
                // 使用第一个受影响恋人的头像
                int firstId = allLoverIds.First(id => id > 0 && id != skipId);
                ToastHelper.Toast(toastText, Cfg.PersonCfgMap[firstId].GetComicIcon(true, false), ToastUIType.Role);
            }

            if (ModConfig.DebugMode.Value)
            {
                LinceMultipleLoversPlugin.Log.LogInfo(
                    $"[CustomEffect 5217] 执行完成: 排除id={skipId}, " +
                    $"恋人总数={allLoverIds.Count}, 受影响={affectedNames.Count}, 好感变更={favorChange:+0.#;-0.#}");
            }
        }

        public override string OnToString(float _rate = 1f, int _type = 0)
        {
            if (this.subType == 0 || this.subType == 1)
            {
                float displayValue = this.value * _rate;
                string sign = displayValue >= 0 ? "+" : "";
                if (this.subType == 0)
                    return $"所有恋人好感{sign}{displayValue:0.#}";
                if (this.excludeRoleId > 0)
                    return $"其他恋人(除{CustomConditionerLoverCount.GetName(this.excludeRoleId)})好感{sign}{displayValue:0.#}";
                return $"其他恋人好感{sign}{displayValue:0.#}";
            }
            return null;
        }
    }

    // =========================================================================
    //  subType = 6 : 分手（等同于 LINCE BREAK）
    // =========================================================================

    /// <summary>
    /// 分手效果
    ///   [5217, 6, 1, X] → 与角色X分手，关系变为熟人，等同于 LINCE BREAK X
    /// </summary>
    public class CustomEffectorBreakUp : Effector
    {
        private int childType;
        private int targetRoleId;

        public CustomEffectorBreakUp() { }

        public CustomEffectorBreakUp(Effector _effector, List<float> _effect)
            : base(_effector, _effect)
        {
            this.childType = (int)_effect[2];
            this.targetRoleId = _effect.Count >= 4 ? (int)_effect[3] : 0;
        }

        public override void OnRun(float _rate = 1f, bool _toast = false)
        {
            if (this.childType != 1 || this.targetRoleId <= 0)
            {
                LinceMultipleLoversPlugin.Log.LogWarning(
                    $"[Effect 5217.6] 参数无效: childType={this.childType}, targetRoleId={this.targetRoleId}");
                return;
            }

            int npcId = this.targetRoleId;

            if (Singleton<RoleMgr>.Ins == null)
            {
                LinceMultipleLoversPlugin.Log.LogWarning("[Effect 5217.6] RoleMgr为null，跳过");
                return;
            }

            Role npc = Singleton<RoleMgr>.Ins.GetRole(npcId);
            if (npc == null)
            {
                LinceMultipleLoversPlugin.Log.LogWarning($"[Effect 5217.6] 找不到角色 ID: {npcId}");
                return;
            }

            if (!LoverIdInterceptor.IsLover(npcId))
            {
                if (ModConfig.DebugMode.Value)
                    LinceMultipleLoversPlugin.Log.LogInfo($"[Effect 5217.6] 角色{npc.Name}({npcId})不是恋人，跳过");
                return;
            }

            var loveData = Singleton<RoleMgr>.Ins.GetLoveData();

            // 1. 从 historyLoverIds 中移除
            if (loveData.historyLoverIds != null)
            {
                loveData.historyLoverIds.Remove(npcId);
            }

            // 2. 从 MultipleLoversManager 中移除
            LinceMultipleLoversPlugin.LoversManager.RemoveLover(npcId);

            // 3. 如果当前 loverId 是该角色，切换到其他恋人或清空
            if (loveData.loverId == npcId)
            {
                var remainingLovers = LoverIdInterceptor.GetAllLoverIds();
                if (remainingLovers.Count > 0)
                {
                    loveData.loverId = remainingLovers[0];
                    if (ModConfig.DebugMode.Value)
                        LinceMultipleLoversPlugin.Log.LogInfo(
                            $"[Effect 5217.6] loverId切换为: {remainingLovers[0]}");
                }
                else
                {
                    loveData.loverId = 0;
                }
            }

            // 4. 从恋人关系字典中移除(520)，关系变为熟人(1)
            var relationData = Singleton<RoleMgr>.Ins.GetRelationData(true);
            var relationDictField = typeof(RelationData).GetField("relationDict",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var relationDict = relationDictField?.GetValue(relationData) as Dictionary<int, List<int>>;

            if (relationDict != null && relationDict.ContainsKey(520))
            {
                relationDict[520].Remove(npcId);
            }

            npc.Relation = 1;

            var addRelationMethod = typeof(RelationData).GetMethod("AddRelation",
                BindingFlags.NonPublic | BindingFlags.Instance);
            addRelationMethod?.Invoke(relationData, new object[] { 1, npcId, false });

            relationData.RefreshSocialCapacity();

            LinceMultipleLoversPlugin.Log.LogInfo(
                $"[Effect 5217.6] 已与 {npc.Name}({npcId}) 分手，关系变为熟人");
        }

        public override string OnToString(float _rate = 1f, int _type = 0)
        {
            return $"与{CustomConditionerLoverCount.GetName(this.targetRoleId)}分手";
        }
    }

    // =========================================================================
    //  subType = 7 : 恋人融洽度设置
    // =========================================================================

    /// <summary>
    /// 恋人融洽度设置
    ///   [5217, 7, X, Y] → 将恋人融洽度(loveData.fix)设为X
    ///                     Y为目标恋人角色ID（须为恋人，否则跳过）
    ///   对照原版: [52, 7, X] 直接设置 loveData.fix = X
    /// </summary>
    public class CustomEffectorLoverFix : Effector
    {
        private int fixValue;
        private int targetRoleId;

        public CustomEffectorLoverFix() { }

        public CustomEffectorLoverFix(Effector _effector, List<float> _effect)
            : base(_effector, _effect)
        {
            this.fixValue = (int)_effect[2];
            this.targetRoleId = _effect.Count >= 4 ? (int)_effect[3] : 0;
        }

        public override void OnRun(float _rate = 1f, bool _toast = false)
        {
            if (Singleton<RoleMgr>.Ins == null)
            {
                LinceMultipleLoversPlugin.Log.LogWarning("[Effect 5217.7] RoleMgr为null，跳过");
                return;
            }

            var loveData = Singleton<RoleMgr>.Ins.GetLoveData();
            if (loveData == null)
            {
                LinceMultipleLoversPlugin.Log.LogWarning("[Effect 5217.7] LoveData为null，跳过");
                return;
            }

            // 如果指定了目标角色，需验证其为恋人
            if (this.targetRoleId > 0 && !LoverIdInterceptor.IsLover(this.targetRoleId))
            {
                if (ModConfig.DebugMode.Value)
                    LinceMultipleLoversPlugin.Log.LogInfo(
                        $"[Effect 5217.7] 角色{this.targetRoleId}不是恋人，跳过融洽度设置");
                return;
            }

            int oldFix = loveData.fix;
            loveData.fix = this.fixValue;

            if (ModConfig.DebugMode.Value)
            {
                string targetStr = this.targetRoleId > 0
                    ? $"(目标恋人:{this.targetRoleId})"
                    : "(全局)";
                LinceMultipleLoversPlugin.Log.LogInfo(
                    $"[Effect 5217.7] 恋人融洽度 {oldFix} → {this.fixValue} {targetStr}");
            }
        }

        public override string OnToString(float _rate = 1f, int _type = 0)
        {
            if (this.targetRoleId > 0)
                return $"{CustomConditionerLoverCount.GetName(this.targetRoleId)}恋人融洽度设为{this.fixValue}";
            return $"恋人融洽度设为{this.fixValue}";
        }
    }
}
