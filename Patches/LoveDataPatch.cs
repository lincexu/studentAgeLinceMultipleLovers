using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Sdk;
using TheEntity;
using Config;
using UnityEngine;

namespace LinceMultipleLovers.Patches
{
    /// <summary>
    /// LoveData类补丁 - 修改恋人相关的核心逻辑
    /// 所有涉及loverId的验证都被拦截并替换为LoverIdInterceptor的调用
    /// </summary>
    public static class LoveDataPatch
    {
        /// <summary>
        /// 修改CanShowVindicateBtn方法 - 允许在已有恋人的情况下显示表白按钮
        /// 原逻辑: if (this.loverId > 0) return false;
        /// </summary>
        [HarmonyPatch(typeof(LoveData), nameof(LoveData.CanShowVindicateBtn))]
        [HarmonyPrefix]
        public static bool CanShowVindicateBtn_Prefix(ref bool __result, LoveData __instance, Role _npc)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return true; // 执行原方法

            // 使用拦截器的单身检查
            if (!LoverIdInterceptor.CanVindicateCheck())
            {
                __result = false;
                return false;
            }

            // 跳过原版对loverId的检查，其他检查保留
            if (_npc.id == 3) // 排除特定NPC
            {
                __result = false;
                return false;
            }
            if (_npc.Relation < 2)
            {
                __result = false;
                return false;
            }

            Role role = Singleton<RoleMgr>.Ins.GetRole();
            if (_npc.Sex == role.Sex)
            {
                if (_npc.id == 104)
                {
                    if (!Singleton<RoleMgr>.Ins.GetRole().IsUnlock(9033))
                    {
                        __result = false;
                        return false;
                    }
                }
                else if (_npc.id == 101)
                {
                    if (!Singleton<RoleMgr>.Ins.GetRole().IsUnlock(9036))
                    {
                        __result = false;
                        return false;
                    }
                }
                else
                {
                    if (_npc.id != 102)
                    {
                        __result = false;
                        return false;
                    }
                    if (!Singleton<RoleMgr>.Ins.GetRole().IsUnlock(9035))
                    {
                        __result = false;
                        return false;
                    }
                }
            }

            __result = _npc.GetUnlockValue(9030, false) != -1f;
            
            if (ModConfig.DebugMode.Value)
            {
                LinceMultipleLoversPlugin.Log.LogInfo($"[CanShowVindicateBtn] NPC {_npc.Name}({_npc.id}): {__result}");
            }
            
            return false; // 跳过原方法
        }

        /// <summary>
        /// 修改CanVinidcate方法 - 允许在已有恋人的情况下表白
        /// 原逻辑: if (this.loverId > 0) return NeedSingle;
        /// </summary>
        [HarmonyPatch(typeof(LoveData), nameof(LoveData.CanVinidcate))]
        [HarmonyPrefix]
        public static bool CanVinidcate_Prefix(ref ValueTuple<VindicateResult, int> __result, LoveData __instance, Role _npc)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return true; // 执行原方法

            // 使用拦截器的单身检查
            if (!LoverIdInterceptor.CanVindicateCheck())
            {
                __result = new ValueTuple<VindicateResult, int>(VindicateResult.NeedSingle, 0);
                return false;
            }

            // 跳过原版对loverId>0的检查，其他检查保留
            if (Singleton<RoleMgr>.Ins.GetRole().Grade <= 6)
            {
                __result = new ValueTuple<VindicateResult, int>(VindicateResult.NeedOlder, 0);
                return false;
            }
            if (_npc.Relation < 4)
            {
                __result = new ValueTuple<VindicateResult, int>(VindicateResult.NeedCloseFriend, 0);
                return false;
            }
            if (__instance.GetVindicateSuccessRate(_npc).Item1 < RoleMgr.GetConstValue(3405))
            {
                __result = new ValueTuple<VindicateResult, int>(VindicateResult.LowSuccessRate, 0);
                return false;
            }
            ValueTuple<float, float> vindicateCost = __instance.GetVindicateCost(_npc);
            if (!Singleton<RoleMgr>.Ins.HasEnoughMood(vindicateCost.Item1) || !Singleton<RoleMgr>.Ins.HasEnoughTrust(vindicateCost.Item2))
            {
                __result = new ValueTuple<VindicateResult, int>(VindicateResult.NeedMoodOrTrust, 0);
                return false;
            }
            int enableEvtId = Singleton<CommonEvtMgr>.Ins.GetEnableEvtId(520, _npc.id, -1, false);
            if (enableEvtId <= 0)
            {
                __result = new ValueTuple<VindicateResult, int>(VindicateResult.NoEvt, 0);
                return false;
            }

            __result = new ValueTuple<VindicateResult, int>(VindicateResult.Success, enableEvtId);
            
            if (ModConfig.DebugMode.Value)
            {
                LinceMultipleLoversPlugin.Log.LogInfo($"[CanVinidcate] NPC {_npc.Name}({_npc.id}): Success");
            }
            
            return false; // 跳过原方法
        }

        /// <summary>
        /// 修改SetLover方法 - 添加新恋人时保留现有恋人
        /// </summary>
        [HarmonyPatch(typeof(LoveData), nameof(LoveData.SetLover))]
        [HarmonyPrefix]
        public static void SetLover_Prefix(LoveData __instance, int _roleId, out int __state)
        {
            __state = __instance.loverId; // 保存原恋人ID
            
            if (ModConfig.DebugMode.Value)
            {
                LinceMultipleLoversPlugin.Log.LogInfo($"[SetLover] 准备设置恋人: {_roleId}, 原恋人: {__state}");
            }
        }

        [HarmonyPatch(typeof(LoveData), nameof(LoveData.SetLover))]
        [HarmonyPostfix]
        public static void SetLover_Postfix(LoveData __instance, int _roleId, int __state)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return;

            if (_roleId > 0)
            {
                // 添加到Mod的多恋人列表
                LinceMultipleLoversPlugin.LoversManager.AddLover(_roleId);

                // 如果之前有恋人，保留原恋人
                if (__state > 0 && __state != _roleId)
                {
                    LinceMultipleLoversPlugin.LoversManager.AddLover(__state);
                    
                    // 恢复loverId为之前的值，保持原版存档兼容性
                    __instance.loverId = __state;
                    
                    if (ModConfig.DebugMode.Value)
                    {
                        LinceMultipleLoversPlugin.Log.LogInfo($"[SetLover] 添加新恋人 {_roleId}，保留原恋人 {__state}，恢复loverId={__state}");
                    }
                }
                else
                {
                    if (ModConfig.DebugMode.Value)
                    {
                        LinceMultipleLoversPlugin.Log.LogInfo($"[SetLover] 设置第一个恋人 {_roleId}");
                    }
                }
            }
            else if (__state > 0)
            {
                // 分手/清除恋人
                LinceMultipleLoversPlugin.LoversManager.RemoveLover(__state);
                
                if (ModConfig.DebugMode.Value)
                {
                    LinceMultipleLoversPlugin.Log.LogInfo($"[SetLover] 分手，移除恋人 {__state}");
                }
            }
        }

        /// <summary>
        /// 修改CheckGreeting方法 - 检查多个恋人的问候事件
        /// 原逻辑: if (this.loverId == 0) return -1;
        /// </summary>
        [HarmonyPatch(typeof(LoveData), nameof(LoveData.CheckGreeting))]
        [HarmonyPrefix]
        public static bool CheckGreeting_Prefix(ref int __result, LoveData __instance, int _mapId)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return true; // 执行原方法

            // 使用拦截器检查是否有恋人
            if (!LoverIdInterceptor.HasLover())
            {
                __result = -1;
                return false;
            }

            if (__instance.hasGreeting)
            {
                __result = -1;
                return false;
            }

            // 检查是否有任何恋人在当前地图
            var lovers = LoverIdInterceptor.GetAllLoverIds();
            foreach (var loverId in lovers)
            {
                if (Singleton<FuncMgr>.Ins.GetMapData().GetMapByNpc(loverId) == _mapId)
                {
                    int evtId = Singleton<CommonEvtMgr>.Ins.GetEnableEvtId(21, loverId, _mapId, false);
                    if (evtId > 0)
                    {
                        __result = evtId;
                        if (ModConfig.DebugMode.Value)
                        {
                            LinceMultipleLoversPlugin.Log.LogInfo($"[CheckGreeting] 找到恋人 {loverId} 的问候事件 {evtId}");
                        }
                        return false;
                    }
                }
            }

            __result = -1;
            return false;
        }

        /// <summary>
        /// 修改CanGiveBirthdayGift方法 - 检查多个恋人的生日
        /// 原逻辑: return this.loverId != 0 && Singleton<RoleMgr>.Ins.GetRole(this.loverId).IsBirthday();
        /// </summary>
        [HarmonyPatch(typeof(LoveData), nameof(LoveData.CanGiveBirthdayGift))]
        [HarmonyPrefix]
        public static bool CanGiveBirthdayGift_Prefix(ref bool __result, LoveData __instance)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return true; // 执行原方法

            // 检查所有恋人的生日
            var lovers = LoverIdInterceptor.GetAllLoverIds();
            foreach (var loverId in lovers)
            {
                Role role = Singleton<RoleMgr>.Ins.GetRole(loverId);
                if (role != null && role.IsBirthday())
                {
                    __result = true;
                    if (ModConfig.DebugMode.Value)
                    {
                        LinceMultipleLoversPlugin.Log.LogInfo($"[CanGiveBirthdayGift] 恋人 {loverId}({role.Name}) 生日");
                    }
                    return false;
                }
            }

            __result = false;
            return false;
        }

        /// <summary>
        /// 修改OnAttrLoveChange方法 - 处理多个恋人的功能开启
        /// 原逻辑: if (this.loverId > 0 && ...) Singleton<FuncMgr>.Ins.OpenFunc(...)
        /// </summary>
        [HarmonyPatch(typeof(LoveData), nameof(LoveData.OnAttrLoveChange))]
        [HarmonyPrefix]
        public static bool OnAttrLoveChange_Prefix(LoveData __instance, float _v)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return true; // 执行原方法

            // 只要有任何恋人就开启功能
            bool hasLover = LoverIdInterceptor.HasLover();
            
            foreach (var keyValuePair in Cfg.LoveActionCfgMap)
            {
                if (hasLover && (float)keyValuePair.Key <= _v)
                {
                    Singleton<FuncMgr>.Ins.OpenFunc(keyValuePair.Value.funcId, true);
                }
                else
                {
                    Singleton<FuncMgr>.Ins.CloseFunc(keyValuePair.Value.funcId);
                }
            }

            return false; // 跳过原方法
        }

        /// <summary>
        /// 修改GetLoveYear方法 - 获取与主恋人的恋爱年数
        /// 原逻辑: if (this.loverId <= 0) return 0;
        /// </summary>
        [HarmonyPatch(typeof(LoveData), nameof(LoveData.GetLoveYear))]
        [HarmonyPrefix]
        public static bool GetLoveYear_Prefix(ref int __result, LoveData __instance)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return true; // 执行原方法

            int primaryLover = LoverIdInterceptor.GetPrimaryLoverId();
            if (primaryLover <= 0)
            {
                __result = 0;
                return false;
            }

            // 使用主恋人计算
            int year = Singleton<RoundMgr>.Ins.GetYear();
            var loveDate = __instance.loveDate;
            if (Singleton<RoundMgr>.Ins.GetMonth()[0] >= loveDate.Item2)
            {
                __result = year - loveDate.Item1;
            }
            else
            {
                __result = year - loveDate.Item1 - 1;
            }

            return false;
        }
    }
}
