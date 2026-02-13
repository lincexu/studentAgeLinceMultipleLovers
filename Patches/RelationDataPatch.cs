using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Sdk;
using TheEntity;
using Config;

namespace LinceMultipleLovers.Patches
{
    /// <summary>
    /// RelationData类补丁 - 修改关系验证逻辑
    /// 所有涉及loverId的验证都被拦截并替换为LoverIdInterceptor的调用
    /// </summary>
    public static class RelationDataPatch
    {
        /// <summary>
        /// 修改IsMatchRelationType方法 - 关键！修改恋人关系验证
        /// 原逻辑: if (_relationType == 520) return Singleton<RoleMgr>.Ins.GetLoveData().loverId == _role.id;
        /// </summary>
        [HarmonyPatch(typeof(RelationData), nameof(RelationData.IsMatchRelationType), new[] { typeof(Role), typeof(int) })]
        [HarmonyPrefix]
        public static bool IsMatchRelationType_Prefix(ref bool __result, RelationData __instance, Role _role, int _relationType)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return true; // 执行原方法

            if (!Cfg.OtherRelationCfgMap.ContainsKey(_relationType))
            {
                __result = false;
                return false;
            }

            // 关键修改：检查是否为恋人关系(520)
            if (_relationType == 520)
            {
                // 使用统一的拦截器进行验证
                __result = LoverIdInterceptor.IsLover(_role.id);
                
                if (ModConfig.DebugMode.Value)
                {
                    LinceMultipleLoversPlugin.Log.LogInfo($"[IsMatchRelationType] NPC {_role.Name}({_role.id}) 恋人验证: {__result}");
                }
                
                return false; // 跳过原方法
            }

            // 其他关系类型保持原逻辑
            if (_relationType == -1)
            {
                __result = true;
                return false;
            }
            if (_relationType == -11)
            {
                __result = _role.Gender == Singleton<RoleMgr>.Ins.GetRole().Gender;
                return false;
            }
            if (_relationType == -12)
            {
                __result = _role.Gender != Singleton<RoleMgr>.Ins.GetRole().Gender;
                return false;
            }
            if (_relationType == 21)
            {
                List<int> otherRelation = __instance.GetOtherRelation(_relationType);
                if (otherRelation.Count > 0)
                {
                    float maxFavor = otherRelation.Max(delegate(int _id)
                    {
                        Role role = Singleton<RoleMgr>.Ins.GetRole(_id);
                        if (role != null)
                        {
                            return role.Favor;
                        }
                        return 0f;
                    });
                    __result = _role.Favor == maxFavor;
                }
                else
                {
                    __result = false;
                }
                return false;
            }
            else if (_relationType == 22)
            {
                List<int> otherRelation2 = __instance.GetOtherRelation(_relationType);
                if (otherRelation2.Count > 0)
                {
                    float minFavor = otherRelation2.Min(delegate(int _id)
                    {
                        Role role = Singleton<RoleMgr>.Ins.GetRole(_id);
                        if (role != null)
                        {
                            return role.Favor;
                        }
                        return 0f;
                    });
                    __result = _role.Favor == minFavor;
                }
                else
                {
                    __result = false;
                }
                return false;
            }
            else
            {
                if (_relationType != 23)
                {
                    OtherRelationCfg otherRelationCfg = Cfg.OtherRelationCfgMap[_relationType];
                    __result = otherRelationCfg.groups[0] == -1 || otherRelationCfg.groups.Contains(_role.Relation);
                    return false;
                }
                List<int> otherRelation3 = __instance.GetOtherRelation(-12);
                if (otherRelation3.Count > 0)
                {
                    float maxFavor = otherRelation3.Max(delegate(int _id)
                    {
                        Role role = Singleton<RoleMgr>.Ins.GetRole(_id);
                        if (role != null)
                        {
                            return role.Favor;
                        }
                        return 0f;
                    });
                    __result = _role.Favor == maxFavor;
                }
                else
                {
                    __result = false;
                }
                return false;
            }
        }

        /// <summary>
        /// 修改IsMatchRelationType方法(带_roleId参数的版本)
        /// </summary>
        [HarmonyPatch(typeof(RelationData), nameof(RelationData.IsMatchRelationType), new[] { typeof(int), typeof(int) })]
        [HarmonyPrefix]
        public static bool IsMatchRelationTypeById_Prefix(ref bool __result, RelationData __instance, int _roleId, int _relationType)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return true; // 执行原方法

            Role role = Singleton<RoleMgr>.Ins.GetRole(_roleId);
            if (role == null)
            {
                __result = false;
                return false;
            }

            // 调用另一个补丁处理
            __result = __instance.IsMatchRelationType(role, _relationType);
            return false;
        }

        /// <summary>
        /// 修改ChangeRelation方法 - 处理恋人关系变更
        /// </summary>
        [HarmonyPatch(typeof(RelationData), nameof(RelationData.ChangeRelation))]
        [HarmonyPostfix]
        public static void ChangeRelation_Postfix(RelationData __instance, int _roleId, int _relationId, string _tag, bool _focusBefore, bool __result)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return;

            // 如果成功建立恋人关系
            if (__result && _relationId == 520)
            {
                LinceMultipleLoversPlugin.LoversManager.AddLover(_roleId);
                
                if (ModConfig.DebugMode.Value)
                {
                    LinceMultipleLoversPlugin.Log.LogInfo($"[ChangeRelation] 通过ChangeRelation添加恋人: {_roleId}");
                }
            }
        }

        /// <summary>
        /// 修改GetRelationship方法 - 返回所有恋人
        /// 原逻辑: 只返回loverId匹配的角色
        /// </summary>
        [HarmonyPatch(typeof(RelationData), nameof(RelationData.GetRelationship))]
        [HarmonyPostfix]
        public static void GetRelationship_Postfix(RelationData __instance, int _relationId, ref List<int> __result)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return;

            // 如果是查询恋人关系(520)
            if (_relationId == 520)
            {
                var lovers = LoverIdInterceptor.GetAllLoverIds();
                if (lovers.Count > 0)
                {
                    __result = lovers;
                    
                    if (ModConfig.DebugMode.Value)
                    {
                        LinceMultipleLoversPlugin.Log.LogInfo($"[GetRelationship] 返回恋人列表: {string.Join(", ", lovers)}");
                    }
                }
            }
        }
    }
}
