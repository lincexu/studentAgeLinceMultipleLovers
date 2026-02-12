using HarmonyLib;
using System;
using System.Collections.Generic;
using Config;
using Effect;
using Sdk;
using TheEntity;

namespace LinceMultipleLovers.Patches
{
    /// <summary>
    /// ActionData类补丁 - 恋爱活动已经通过MapRoleViewTopicPatch修改了loverId
    /// 这个补丁保留用于兼容性
    /// </summary>
    public static class ActionDataPatch
    {
        /// <summary>
        /// 在HelpLoveAction执行前记录日志
        /// </summary>
        [HarmonyPatch(typeof(ActionData), "HelpLoveAction")]
        [HarmonyPrefix]
        public static void HelpLoveAction_Prefix(ActionData __instance, ActionSubData _data, Dictionary<int, float> _costs, int _bgId, int _mapId, bool _quick)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return;

            var loveData = Singleton<RoleMgr>.Ins.GetLoveData();
            var cfg = Cfg.ActionCfgMap[_data.id];
            
            if (ModConfig.DebugMode.Value)
            {
                LinceMultipleLoversPlugin.Log.LogInfo($"[HelpLoveAction] 活动: {cfg.name}, 当前loverId: {loveData?.loverId ?? 0}");
            }
        }
    }
}
