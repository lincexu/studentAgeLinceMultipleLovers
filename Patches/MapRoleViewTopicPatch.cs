using HarmonyLib;
using System;
using System.Collections.Generic;
using Sdk;
using TheEntity;
using View.Main;

namespace LinceMultipleLovers.Patches
{
    /// <summary>
    /// MapRoleView话题补丁 - 修复恋爱话题使用正确的NPC ID
    /// 策略：在话题交互时直接修改loverId为当前NPC
    /// </summary>
    public static class MapRoleViewTopicPatch
    {
        /// <summary>
        /// 临时存储当前正在交互的NPC ID
        /// </summary>
        public static int CurrentNpcId { get; set; } = 0;

        /// <summary>
        /// 修改OnCellRender - 当渲染308按钮时记录当前NPC并修改loverId
        /// </summary>
        [HarmonyPatch(typeof(MapRoleView), "OnCellRender")]
        [HarmonyPostfix]
        public static void OnCellRender_Postfix(MapRoleView __instance, UICell _cell)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return;

            // 获取按钮ID
            int btnId = (int)_cell.data;
            
            // 调试：记录所有按钮ID
            if (ModConfig.DebugMode.Value)
            {
                LinceMultipleLoversPlugin.Log.LogInfo($"[MapRoleViewTopic] OnCellRender 按钮ID: {btnId}");
            }
            
            // 只处理308按钮（恋爱话题）
            if (btnId != 308)
                return;

            // 获取当前NPC
            var npcField = typeof(MapRoleView).GetField("npc", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var npc = npcField?.GetValue(__instance) as Role;
            
            if (ModConfig.DebugMode.Value)
            {
                LinceMultipleLoversPlugin.Log.LogInfo($"[MapRoleViewTopic] NPC: {npc?.Name ?? "null"}({npc?.id ?? 0}), 是否恋人: {LoverIdInterceptor.IsLover(npc?.id ?? 0)}");
            }
            
            if (npc != null && LoverIdInterceptor.IsLover(npc.id))
            {
                // 记录当前NPC ID
                CurrentNpcId = npc.id;
                
                // 直接修改loverId为当前NPC
                var loveData = Singleton<RoleMgr>.Ins.GetLoveData();
                if (loveData != null && loveData.loverId != npc.id)
                {
                    int originalLoverId = loveData.loverId;
                    loveData.loverId = npc.id;
                    
                    // 记录为最近交互的恋人
                    LastInteractedLover.SetLastLover(npc.id);
                    
                    if (ModConfig.DebugMode.Value)
                    {
                        LinceMultipleLoversPlugin.Log.LogInfo($"[MapRoleViewTopic] 渲染308按钮，修改loverId: {originalLoverId} -> {npc.id} ({npc.Name})");
                    }
                }
                else if (loveData != null && loveData.loverId == npc.id)
                {
                    if (ModConfig.DebugMode.Value)
                    {
                        LinceMultipleLoversPlugin.Log.LogInfo($"[MapRoleViewTopic] loverId已经是{npc.id}，无需修改");
                    }
                }
            }
        }

        /// <summary>
        /// 修改OnCellCreate - 当创建308按钮时记录当前NPC
        /// </summary>
        [HarmonyPatch(typeof(MapRoleView), "OnCellCreate")]
        [HarmonyPostfix]
        public static void OnCellCreate_Postfix(MapRoleView __instance, UICell _cell)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return;

            // 获取按钮ID
            int btnId = (int)_cell.data;
            
            // 调试：记录所有按钮ID
            if (ModConfig.DebugMode.Value)
            {
                LinceMultipleLoversPlugin.Log.LogInfo($"[MapRoleViewTopic] OnCellCreate 按钮ID: {btnId}");
            }
            
            // 只处理308按钮（恋爱话题）
            if (btnId != 308)
                return;

            // 获取当前NPC
            var npcField = typeof(MapRoleView).GetField("npc", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var npc = npcField?.GetValue(__instance) as Role;
            
            if (ModConfig.DebugMode.Value)
            {
                LinceMultipleLoversPlugin.Log.LogInfo($"[MapRoleViewTopic] NPC: {npc?.Name ?? "null"}({npc?.id ?? 0}), 是否恋人: {LoverIdInterceptor.IsLover(npc?.id ?? 0)}");
            }
            
            if (npc != null && LoverIdInterceptor.IsLover(npc.id))
            {
                // 记录当前NPC ID
                CurrentNpcId = npc.id;
                
                // 直接修改loverId为当前NPC
                var loveData = Singleton<RoleMgr>.Ins.GetLoveData();
                if (loveData != null && loveData.loverId != npc.id)
                {
                    int originalLoverId = loveData.loverId;
                    loveData.loverId = npc.id;
                    
                    // 记录为最近交互的恋人
                    LastInteractedLover.SetLastLover(npc.id);
                    
                    if (ModConfig.DebugMode.Value)
                    {
                        LinceMultipleLoversPlugin.Log.LogInfo($"[MapRoleViewTopic] 创建308按钮，修改loverId: {originalLoverId} -> {npc.id} ({npc.Name})");
                    }
                }
                else if (loveData != null && loveData.loverId == npc.id)
                {
                    if (ModConfig.DebugMode.Value)
                    {
                        LinceMultipleLoversPlugin.Log.LogInfo($"[MapRoleViewTopic] loverId已经是{npc.id}，无需修改");
                    }
                }
            }
        }
    }
}
