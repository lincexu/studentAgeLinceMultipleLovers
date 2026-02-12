using System;
using System.Collections.Generic;
using System.Linq;
using Sdk;
using TheEntity;

namespace LinceMultipleLovers
{
    /// <summary>
    /// 统一的恋人ID验证拦截器
    /// 使用游戏原生的historyLoverIds存档，不依赖本地配置
    /// </summary>
    public static class LoverIdInterceptor
    {
        /// <summary>
        /// 从游戏的LoveData中获取historyLoverIds
        /// </summary>
        private static List<int> GetHistoryLoverIds()
        {
            var loveData = Singleton<RoleMgr>.Ins?.GetLoveData();
            if (loveData == null)
                return new List<int>();
            
            if (loveData.historyLoverIds == null)
            {
                loveData.historyLoverIds = new List<int>();
            }
            
            return loveData.historyLoverIds;
        }

        /// <summary>
        /// 检查指定NPC是否为恋人（核心验证方法）
        /// 替换所有直接比较 loverId == npc.id 的地方
        /// </summary>
        public static bool IsLover(int npcId)
        {
            if (npcId <= 0) return false;
            
            // 首先检查historyLoverIds（多恋人列表）
            var historyIds = GetHistoryLoverIds();
            if (historyIds.Contains(npcId))
                return true;
            
            // 回退到原版的loverId检查（用于兼容性）
            var loveData = Singleton<RoleMgr>.Ins?.GetLoveData();
            if (loveData != null && loveData.loverId == npcId)
                return true;
            
            return false;
        }

        /// <summary>
        /// 检查是否有任何恋人
        /// 替换所有检查 loverId > 0 或 loverId != 0 的地方
        /// </summary>
        public static bool HasLover()
        {
            // 检查historyLoverIds
            var historyIds = GetHistoryLoverIds();
            if (historyIds.Count > 0)
                return true;
            
            // 回退到原版检查
            var loveData = Singleton<RoleMgr>.Ins?.GetLoveData();
            return loveData != null && loveData.loverId > 0;
        }

        /// <summary>
        /// 获取主恋人ID
        /// 用于需要返回一个特定恋人ID的场景
        /// </summary>
        public static int GetPrimaryLoverId()
        {
            // 优先返回原版的loverId（当前恋人）
            var loveData = Singleton<RoleMgr>.Ins?.GetLoveData();
            if (loveData != null && loveData.loverId > 0)
                return loveData.loverId;
            
            // 如果没有当前恋人，返回historyLoverIds的第一个
            var historyIds = GetHistoryLoverIds();
            if (historyIds.Count > 0)
                return historyIds[0];
            
            return 0;
        }

        /// <summary>
        /// 获取所有恋人ID列表
        /// </summary>
        public static List<int> GetAllLoverIds()
        {
            var result = new List<int>();
            
            // 从historyLoverIds获取
            var historyIds = GetHistoryLoverIds();
            result.AddRange(historyIds);
            
            // 同时包含当前的loverId（如果不在列表中）
            var loveData = Singleton<RoleMgr>.Ins?.GetLoveData();
            if (loveData != null && loveData.loverId > 0 && !result.Contains(loveData.loverId))
            {
                result.Add(loveData.loverId);
            }
            
            return result;
        }

        /// <summary>
        /// 检查是否单身（没有恋人）
        /// 用于CanShowVindicateBtn等方法的单身检查
        /// </summary>
        public static bool IsSingle()
        {
            return !HasLover();
        }

        /// <summary>
        /// 验证表白条件时使用的单身检查
        /// 如果启用了多恋人且允许绕过单身检查，则始终返回true
        /// </summary>
        public static bool CanVindicateCheck()
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return IsSingle();
            
            if (ModConfig.BypassSingleCheck.Value)
                return true; // 允许在有恋人的情况下继续表白
            
            return IsSingle();
        }

        /// <summary>
        /// 添加恋人到historyLoverIds
        /// </summary>
        public static void AddLover(int npcId)
        {
            if (npcId <= 0) return;
            
            var historyIds = GetHistoryLoverIds();
            if (!historyIds.Contains(npcId))
            {
                historyIds.Add(npcId);
                
                if (ModConfig.DebugMode.Value)
                {
                    LinceMultipleLoversPlugin.Log.LogInfo($"[LoverIdInterceptor] 添加恋人 {npcId} 到historyLoverIds");
                }
            }
        }

        /// <summary>
        /// 调试日志输出
        /// </summary>
        public static void LogDebugInfo(string context)
        {
            if (!ModConfig.DebugMode.Value) return;
            
            var loveData = Singleton<RoleMgr>.Ins?.GetLoveData();
            var historyIds = GetHistoryLoverIds();
            
            LinceMultipleLoversPlugin.Log.LogInfo($"[LoverIdInterceptor] {context}");
            LinceMultipleLoversPlugin.Log.LogInfo($"  原版loverId: {loveData?.loverId ?? 0}");
            LinceMultipleLoversPlugin.Log.LogInfo($"  historyLoverIds: {(historyIds.Count > 0 ? string.Join(", ", historyIds) : "empty")}");
            LinceMultipleLoversPlugin.Log.LogInfo($"  HasLover: {HasLover()}, IsSingle: {IsSingle()}");
        }
    }
}
