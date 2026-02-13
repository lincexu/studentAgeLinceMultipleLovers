using HarmonyLib;
using System.Collections.Generic;
using Sdk;
using TheEntity;
using View.TheAction;

namespace LinceMultipleLovers.Patches
{
    /// <summary>
    /// QuickSocialView补丁 - 修复社交容量面板显示所有恋人
    /// 策略：在OnOpen时将恋人添加到列表中
    /// </summary>
    public static class QuickSocialViewPatch
    {
        /// <summary>
        /// 修改OnOpen方法 - 将恋人添加到社交容量列表
        /// </summary>
        [HarmonyPatch(typeof(QuickSocialView), "OnOpen")]
        [HarmonyPostfix]
        public static void OnOpen_Postfix(QuickSocialView __instance)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return;

            // 获取所有恋人
            var lovers = LoverIdInterceptor.GetAllLoverIds();
            if (lovers.Count == 0)
                return;

            // 使用反射获取itemgroup_data
            var itemGroupField = typeof(QuickSocialView).GetField("itemgroup_data", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var itemGroup = itemGroupField?.GetValue(__instance);
            
            if (itemGroup == null)
                return;

            // 获取当前列表
            var getDatasMethod = itemGroup.GetType().GetMethod("GetDatas");
            var currentList = getDatasMethod?.Invoke(itemGroup, null) as List<int>;
            
            if (currentList == null)
                return;

            // 将不在列表中的恋人添加进去
            bool added = false;
            foreach (int loverId in lovers)
            {
                if (!currentList.Contains(loverId))
                {
                    currentList.Add(loverId);
                    added = true;
                    
                    if (ModConfig.DebugMode.Value)
                    {
                        var role = Singleton<RoleMgr>.Ins.GetRole(loverId);
                        LinceMultipleLoversPlugin.Log.LogInfo($"[QuickSocialView] 添加恋人到列表: {loverId} ({role?.Name})");
                    }
                }
            }

            // 如果有添加，刷新列表
            if (added)
            {
                var setDatasMethod = itemGroup.GetType().GetMethod("SetDatas");
                setDatasMethod?.Invoke(itemGroup, new object[] { currentList, null });
                
                // 刷新显示
                var refreshMethod = typeof(QuickSocialView).GetMethod("Refresh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                refreshMethod?.Invoke(__instance, null);
            }
        }

        /// <summary>
        /// 修改OnRenderSocial方法 - 临时修改loverId为当前角色
        /// </summary>
        [HarmonyPatch(typeof(QuickSocialView), "OnRenderSocial")]
        [HarmonyPrefix]
        public static void OnRenderSocial_Prefix(QuickSocialView __instance, UICell _cell)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return;

            // 获取当前NPC ID
            int num = (int)_cell.data;
            
            // 如果这个NPC是恋人之一，临时修改loverId
            if (LoverIdInterceptor.IsLover(num))
            {
                var loveData = Singleton<RoleMgr>.Ins.GetLoveData();
                if (loveData != null && loveData.loverId != num)
                {
                    loveData.loverId = num;
                    
                    if (ModConfig.DebugMode.Value)
                    {
                        var role = Singleton<RoleMgr>.Ins.GetRole(num);
                        LinceMultipleLoversPlugin.Log.LogInfo($"[QuickSocialView] 临时修改loverId: {num} ({role?.Name})");
                    }
                }
            }
        }
    }
}
