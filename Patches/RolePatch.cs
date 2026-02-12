using HarmonyLib;
using Sdk;
using TheEntity;

namespace LinceMultipleLovers.Patches
{
    /// <summary>
    /// Role类补丁 - 修复GetRelationForUI方法
    /// 这是显示角色关系图标的关键方法
    /// </summary>
    public static class RolePatch
    {
        /// <summary>
        /// 修改GetRelationForUI方法 - 显示正确的恋人关系
        /// 原逻辑: if (Singleton<RoleMgr>.Ins.GetLoveData().loverId == this.id) return 520;
        /// </summary>
        [HarmonyPatch(typeof(Role), nameof(Role.GetRelationForUI))]
        [HarmonyPrefix]
        public static bool GetRelationForUI_Prefix(ref int __result, Role __instance)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return true; // 执行原方法

            // 使用拦截器检查是否为恋人
            if (LoverIdInterceptor.IsLover(__instance.id))
            {
                __result = 520; // 返回恋人关系ID
                
                if (ModConfig.DebugMode.Value)
                {
                    LinceMultipleLoversPlugin.Log.LogInfo($"[GetRelationForUI] NPC {__instance.Name}({__instance.id}) 显示为恋人关系");
                }
                
                return false; // 跳过原方法
            }

            // 不是恋人，返回普通关系
            __result = __instance.Relation;
            return false; // 跳过原方法
        }
    }
}
