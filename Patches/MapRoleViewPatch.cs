using HarmonyLib;
using System;
using System.Collections.Generic;
using Sdk;
using TheEntity;
using View.Main;

namespace LinceMultipleLovers.Patches
{
    /// <summary>
    /// MapRoleView类补丁 - 修复社交页面恋人显示
    /// 策略：直接修改loverId为当前交互的NPC，不再恢复
    /// </summary>
    public static class MapRoleViewPatch
    {
        /// <summary>
        /// 修改RefreshData方法 - 让多恋人也能显示恋人专属功能
        /// 直接修改loverId为当前NPC，不再恢复
        /// </summary>
        [HarmonyPatch(typeof(MapRoleView), "RefreshData")]
        [HarmonyPrefix]
        public static void RefreshData_Prefix(MapRoleView __instance)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return;

            // 获取当前NPC ID
            var npcField = typeof(MapRoleView).GetField("npc", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var npc = npcField?.GetValue(__instance) as Role;
            
            if (npc == null)
                return;

            // 检查这个NPC是否是Mod管理的恋人
            if (LoverIdInterceptor.IsLover(npc.id))
            {
                var loveData = Singleton<RoleMgr>.Ins.GetLoveData();
                int originalLoverId = loveData.loverId;
                
                // 直接修改loverId为这个NPC，不再恢复
                if (originalLoverId != npc.id)
                {
                    loveData.loverId = npc.id;
                    
                    // 记录为最近交互的恋人
                    LastInteractedLover.SetLastLover(npc.id);
                    
                    if (ModConfig.DebugMode.Value)
                    {
                        LinceMultipleLoversPlugin.Log.LogInfo($"[MapRoleView] 修改loverId: {originalLoverId} -> {npc.id} (NPC: {npc.Name})");
                    }
                }
            }
        }
    }
}
