using HarmonyLib;
using Sdk;
using TheEntity;
using View.TheAction;

namespace LinceMultipleLovers.Patches
{
    /// <summary>
    /// QuickSocialView补丁 - 修复社交容量面板显示所有恋人
    /// 策略：在渲染每个角色前临时修改loverId
    /// </summary>
    public static class QuickSocialViewPatch
    {
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
