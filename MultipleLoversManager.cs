using System;
using System.Collections.Generic;
using System.Linq;
using Sdk;
using TheEntity;

namespace LinceMultipleLovers
{
    /// <summary>
    /// 多恋人管理器 - 使用游戏原生的historyLoverIds存档
    /// </summary>
    public class MultipleLoversManager
    {
        /// <summary>
        /// 从游戏的LoveData中获取historyLoverIds
        /// </summary>
        private List<int> GetHistoryLoverIds()
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
        /// 添加恋人 - 直接修改游戏的historyLoverIds
        /// </summary>
        public void AddLover(int npcId)
        {
            if (npcId <= 0) return;
            
            var historyIds = GetHistoryLoverIds();
            
            if (!historyIds.Contains(npcId))
            {
                historyIds.Add(npcId);
                LinceMultipleLoversPlugin.Log.LogInfo($"添加恋人到存档: NPC ID {npcId}，当前列表: {string.Join(", ", historyIds)}");
            }
            else
            {
                LinceMultipleLoversPlugin.Log.LogInfo($"NPC {npcId} 已在恋人列表中");
            }
        }

        /// <summary>
        /// 移除恋人
        /// </summary>
        public void RemoveLover(int npcId)
        {
            if (npcId <= 0) return;
            
            var historyIds = GetHistoryLoverIds();
            
            if (historyIds.Contains(npcId))
            {
                historyIds.Remove(npcId);
                LinceMultipleLoversPlugin.Log.LogInfo($"从存档移除恋人: NPC ID {npcId}，当前列表: {string.Join(", ", historyIds)}");
            }
        }

        /// <summary>
        /// 检查是否为恋人
        /// </summary>
        public bool IsLover(int npcId)
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
        /// 获取所有恋人ID列表
        /// </summary>
        public List<int> GetAllLoverIds()
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
        /// 获取主恋人ID（当前的loverId）
        /// </summary>
        public int GetPrimaryLoverId()
        {
            var loveData = Singleton<RoleMgr>.Ins?.GetLoveData();
            return loveData?.loverId ?? 0;
        }

        /// <summary>
        /// 检查是否有任何恋人
        /// </summary>
        public bool HasAnyLover()
        {
            return GetAllLoverIds().Count > 0;
        }

        /// <summary>
        /// 获取恋人数量
        /// </summary>
        public int GetLoverCount()
        {
            return GetAllLoverIds().Count;
        }

        /// <summary>
        /// 同步原版数据到管理器
        /// 当游戏加载存档时调用
        /// </summary>
        public void SyncFromOriginalData()
        {
            var loveData = Singleton<RoleMgr>.Ins?.GetLoveData();
            if (loveData == null) return;
            
            // 确保historyLoverIds存在
            if (loveData.historyLoverIds == null)
            {
                loveData.historyLoverIds = new List<int>();
            }
            
            // 如果当前有loverId但不在historyLoverIds中，添加进去
            if (loveData.loverId > 0 && !loveData.historyLoverIds.Contains(loveData.loverId))
            {
                loveData.historyLoverIds.Add(loveData.loverId);
                LinceMultipleLoversPlugin.Log.LogInfo($"同步: 将当前恋人 {loveData.loverId} 添加到历史列表");
            }
            
            if (ModConfig.DebugMode.Value)
            {
                LogDebugInfo();
            }
        }

        /// <summary>
        /// 打印调试信息
        /// </summary>
        public void LogDebugInfo()
        {
            if (!ModConfig.DebugMode.Value) return;
            
            var loveData = Singleton<RoleMgr>.Ins?.GetLoveData();
            var historyIds = GetHistoryLoverIds();
            
            LinceMultipleLoversPlugin.Log.LogInfo("=== 多恋人Mod调试信息 ===");
            LinceMultipleLoversPlugin.Log.LogInfo($"原版loverId: {loveData?.loverId ?? 0}");
            LinceMultipleLoversPlugin.Log.LogInfo($"历史恋人列表: {string.Join(", ", historyIds)}");
            LinceMultipleLoversPlugin.Log.LogInfo($"总恋人数量: {GetLoverCount()}");
            LinceMultipleLoversPlugin.Log.LogInfo("========================");
        }

        /// <summary>
        /// 强制将指定NPC设为恋人（用于调试）
        /// </summary>
        public void ForceSetLover(int npcId)
        {
            if (npcId <= 0) return;
            
            LinceMultipleLoversPlugin.Log.LogInfo($"[强制设置恋人] NPC ID: {npcId}");
            
            // 添加到historyLoverIds
            AddLover(npcId);
            
            // 同时修改原版loverId（临时）
            var loveData = Singleton<RoleMgr>.Ins?.GetLoveData();
            if (loveData != null)
            {
                loveData.loverId = npcId;
                LinceMultipleLoversPlugin.Log.LogInfo($"[强制设置恋人] 已修改原版loverId为: {npcId}");
            }
            
            LogDebugInfo();
        }

        /// <summary>
        /// 详细检查指定NPC的恋人状态
        /// </summary>
        public void CheckNpcLoverStatus(int npcId)
        {
            LinceMultipleLoversPlugin.Log.LogInfo($"=== 检查NPC {npcId} 的恋人状态 ===");
            
            var loveData = Singleton<RoleMgr>.Ins?.GetLoveData();
            var historyIds = GetHistoryLoverIds();
            
            // 检查原版loverId
            bool isOriginalLover = loveData?.loverId == npcId;
            LinceMultipleLoversPlugin.Log.LogInfo($"原版loverId匹配: {isOriginalLover} (原版: {loveData?.loverId ?? 0})");
            
            // 检查historyLoverIds
            bool inHistoryList = historyIds.Contains(npcId);
            LinceMultipleLoversPlugin.Log.LogInfo($"在历史列表中: {inHistoryList}");
            
            // 最终判断
            bool finalIsLover = IsLover(npcId);
            LinceMultipleLoversPlugin.Log.LogInfo($"最终IsLover结果: {finalIsLover}");
            LinceMultipleLoversPlugin.Log.LogInfo("=====================================");
        }

        /// <summary>
        /// 从配置加载恋人（用于兼容旧配置）
        /// </summary>
        public void LoadConfiguredLovers()
        {
            // 不再从配置文件加载，而是从存档的historyLoverIds自动加载
            // 这个方法保留用于兼容性
            SyncFromOriginalData();
        }
    }
}
