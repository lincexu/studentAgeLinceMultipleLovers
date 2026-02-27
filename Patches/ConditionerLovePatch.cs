using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Condition;
using Sdk;

namespace LinceMultipleLovers.Patches
{
    /// <summary>
    /// ConditionerLove补丁 - 修复结局CG触发问题
    /// 让条件判断检查所有恋人，而不仅仅是当前loverId
    /// </summary>
    [HarmonyPatch(typeof(ConditionerLove), nameof(ConditionerLove.OnIsMatch))]
    public static class ConditionerLovePatch
    {
        public static bool OnIsMatch_Prefix(ConditionerLove __instance, ref bool __result)
        {
            // 直接访问public字段
            int subType = __instance.subType;
            int childType = __instance.childType;

            // subType = 3: 无恋爱经历检查 [11, 3]（独立于多恋人功能）
            // 原版逻辑: historyLoverIds.IsEmpty() && loverId <= 0
            if (subType == 3)
            {
                if (ModConfig.ForceNoLoveHistory.Value)
                {
                    // 强制无恋爱经历模式：始终返回true
                    __result = true;
                    
                    if (ModConfig.DebugMode.Value)
                    {
                        LoveData loveData = Singleton<RoleMgr>.Ins.GetLoveData();
                        LinceMultipleLoversPlugin.Log.LogInfo($"[ConditionerLove] 强制无恋爱经历: historyLoverIds数量={loveData.historyLoverIds?.Count ?? 0}, loverId={loveData.loverId}, 强制结果=true");
                    }
                    
                    return false; // 跳过原方法
                }
                
                // 未开启强制选项时，执行原方法
                return true;
            }

            // 以下功能需要多恋人功能启用
            if (!ModConfig.EnableMultipleLovers.Value)
                return true; // 执行原方法

            // 获取所有恋人ID
            var loverIds = LoverIdInterceptor.GetAllLoverIds();
            
            if (ModConfig.DebugMode.Value)
            {
                LinceMultipleLoversPlugin.Log.LogInfo($"[ConditionerLove] OnIsMatch被调用: subType={subType}, childType={childType}, 恋人列表=[{string.Join(", ", loverIds)}]");
            }
            
            // subType = 1: 恋人相关检查
            if (subType == 1)
            {
                if (childType == 0)
                {
                    // [11, 1, 0] - 检查是否有任何恋人（已脱单）
                    __result = loverIds.Count > 0;
                }
                else
                {
                    // [11, 1, X] - 检查是否和指定NPC(X)是恋人
                    __result = loverIds.Contains(childType);
                }
                
                if (ModConfig.DebugMode.Value)
                {
                    LinceMultipleLoversPlugin.Log.LogInfo($"[ConditionerLove] 条件检查: subType={subType}, childType={childType}, 恋人列表=[{string.Join(", ", loverIds)}], 结果={__result}");
                }
                
                return false; // 跳过原方法
            }
            
            return true; // 执行原方法（其他subType）
        }
    }
}
