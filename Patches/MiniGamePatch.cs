using HarmonyLib;
using System;
using System.Reflection;
using Config;
using MiniGame.Badminton;
using Sdk;
using TheEntity;

namespace LinceMultipleLovers.Patches
{
    /// <summary>
    /// 小游戏补丁 - loverId已经通过MapRoleViewTopicPatch修改
    /// 这个补丁保留用于兼容性和日志记录
    /// </summary>
    public static class MiniGamePatch
    {
        /// <summary>
        /// 记录BadmintonMiniGameView打开日志
        /// </summary>
        [HarmonyPatch(typeof(BadmintonMiniGameView), "OnOpen")]
        [HarmonyPostfix]
        public static void BadmintonOnOpen_Postfix(BadmintonMiniGameView __instance)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return;

            // 获取enermyNpcId字段
            var enermyNpcIdField = typeof(BadmintonMiniGameView).GetField("enermyNpcId", BindingFlags.NonPublic | BindingFlags.Instance);
            if (enermyNpcIdField != null)
            {
                int enermyNpcId = (int)(enermyNpcIdField.GetValue(__instance) ?? 0);
                var role = Singleton<RoleMgr>.Ins.GetRole(enermyNpcId);
                
                if (ModConfig.DebugMode.Value)
                {
                    LinceMultipleLoversPlugin.Log.LogInfo($"[BadmintonMiniGame] 对手: {role?.Name ?? "Unknown"}({enermyNpcId})");
                }
            }
        }
    }
}
