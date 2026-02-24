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
            if (!ModConfig.EnableMultipleLovers.Value)
                return true; // 执行原方法

            // 获取所有恋人ID
            var loverIds = LoverIdInterceptor.GetAllLoverIds();
            
            // 获取subType和childType
            var subTypeField = typeof(Conditioner).GetField("subType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var childTypeField = typeof(ConditionerLove).GetField("childType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            int subType = (int)(subTypeField?.GetValue(__instance) ?? 0);
            int childType = (int)(childTypeField?.GetValue(__instance) ?? 0);
            
            if (ModConfig.DebugMode.Value)
            {
                LinceMultipleLoversPlugin.Log.LogInfo($"[ConditionerLove] OnIsMatch被调用: subType={subType}, childType={childType}, 恋人列表=[{string.Join(", ", loverIds)}]");
            }
            
            // subType = 1: 恋人相关检查
            if (subType == 1)
            {
                if (childType == 0)
                {
                    // [52, 1, 0] - 检查是否有任何恋人（已脱单）
                    __result = loverIds.Count > 0;
                }
                else
                {
                    // [52, 1, X] - 检查是否和指定NPC(X)是恋人
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
