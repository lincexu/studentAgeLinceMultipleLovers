using System;
using System.Collections.Generic;
using Sdk;
using TheEntity;

namespace LinceMultipleLovers
{
    /// <summary>
    /// 最近交互的恋人管理器
    /// 用于记录玩家最近与之交互的恋人，以便在活动（看电影、友谊赛等）中邀请
    /// </summary>
    public static class LastInteractedLover
    {
        /// <summary>
        /// 最近交互的恋人ID
        /// </summary>
        public static int LastLoverId { get; private set; } = 0;

        /// <summary>
        /// 最近交互的时间戳
        /// </summary>
        public static DateTime LastInteractionTime { get; private set; } = DateTime.MinValue;

        /// <summary>
        /// 设置最近交互的恋人
        /// </summary>
        public static void SetLastLover(int npcId)
        {
            if (npcId <= 0) return;
            
            LastLoverId = npcId;
            LastInteractionTime = DateTime.Now;
            
            if (ModConfig.DebugMode.Value)
            {
                var role = Singleton<RoleMgr>.Ins.GetRole(npcId);
                LinceMultipleLoversPlugin.Log.LogInfo($"[LastInteractedLover] 设置最近交互恋人: {role?.Name ?? "Unknown"}({npcId})");
            }
        }

        /// <summary>
        /// 获取最近交互的恋人ID
        /// 如果最近交互的恋人无效，则返回主恋人
        /// </summary>
        public static int GetLastLoverId()
        {
            // 检查最近交互的恋人是否仍然有效（是恋人关系）
            if (LastLoverId > 0 && LoverIdInterceptor.IsLover(LastLoverId))
            {
                return LastLoverId;
            }
            
            // 回退到主恋人
            return LoverIdInterceptor.GetPrimaryLoverId();
        }

        /// <summary>
        /// 清除记录
        /// </summary>
        public static void Clear()
        {
            LastLoverId = 0;
            LastInteractionTime = DateTime.MinValue;
            
            if (ModConfig.DebugMode.Value)
            {
                LinceMultipleLoversPlugin.Log.LogInfo("[LastInteractedLover] 清除记录");
            }
        }

        /// <summary>
        /// 检查是否有有效的最近交互记录
        /// </summary>
        public static bool HasValidLastLover()
        {
            return LastLoverId > 0 && LoverIdInterceptor.IsLover(LastLoverId);
        }
    }
}
