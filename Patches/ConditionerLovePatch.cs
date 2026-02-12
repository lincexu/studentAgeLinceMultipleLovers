using HarmonyLib;
using Condition;
using Sdk;

namespace LinceMultipleLovers.Patches
{
    /// <summary>
    /// ConditionerLove补丁 - 修改恋人条件判断
    /// 原逻辑: return Singleton<RoleMgr>.Ins.GetLoveData().loverId == this.childType;
    /// 修改后: 检查historyLoverIds列表
    /// </summary>
    public static class ConditionerLovePatch
    {
        /// <summary>
        /// 修改OnIsMatch方法 - 支持多恋人判断
        /// 条件格式: 52,2,1,X - 检查X是否是恋人之一
        /// </summary>
        [HarmonyPatch(typeof(ConditionerLove), "OnIsMatch")]
        [HarmonyPrefix]
        public static bool OnIsMatch_Prefix(ref bool __result, ConditionerLove __instance)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return true; // 执行原方法

            // 使用反射获取subType和childType
            var subTypeField = typeof(ConditionerLove).GetField("subType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var childTypeField = typeof(ConditionerLove).GetField("childType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            int subType = (int)(subTypeField?.GetValue(__instance) ?? 0);
            int childType = (int)(childTypeField?.GetValue(__instance) ?? 0);

            if (subType == 1)
            {
                if (childType == 0)
                {
                    // 检查是否有任何恋人
                    __result = LoverIdInterceptor.HasLover();
                    
                    if (ModConfig.DebugMode.Value)
                    {
                        LinceMultipleLoversPlugin.Log.LogInfo($"[ConditionerLove] 检查是否有恋人: {__result}");
                    }
                    
                    return false; // 跳过原方法
                }
                
                // 检查特定NPC是否是恋人之一
                __result = LoverIdInterceptor.IsLover(childType);
                
                if (ModConfig.DebugMode.Value)
                {
                    LinceMultipleLoversPlugin.Log.LogInfo($"[ConditionerLove] 检查NPC {childType} 是否是恋人: {__result}");
                }
                
                return false; // 跳过原方法
            }
            else if (subType == 3)
            {
                // 检查是否单身（没有恋人）
                __result = !LoverIdInterceptor.HasLover();
                
                if (ModConfig.DebugMode.Value)
                {
                    LinceMultipleLoversPlugin.Log.LogInfo($"[ConditionerLove] 检查是否单身: {__result}");
                }
                
                return false; // 跳过原方法
            }

            return true; // 执行原方法
        }
    }
}
