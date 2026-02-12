using HarmonyLib;
using System.Collections.Generic;
using Sdk;
using TheEntity;

namespace LinceMultipleLovers.Patches
{
    /// <summary>
    /// LoveData社交话题补丁 - 为每个角色单独计算话题次数
    /// 原逻辑: socialTopicCntThisRound 是全局的，所有角色共享
    /// 修改后: 每个角色有自己的话题计数
    /// </summary>
    public static class LoveDataSocialTopicPatch
    {
        /// <summary>
        /// 每角色话题计数器: NPC ID -> 本回合已话题次数
        /// </summary>
        private static Dictionary<int, int> npcSocialTopicCount = new Dictionary<int, int>();
        
        /// <summary>
        /// 每角色话题列表: NPC ID -> 话题列表
        /// </summary>
        private static Dictionary<int, List<int>> npcTopicsThisRound = new Dictionary<int, List<int>>();

        /// <summary>
        /// 获取指定NPC的话题计数
        /// </summary>
        public static int GetNpcTopicCount(int npcId)
        {
            if (npcId <= 0) return 0;
            return npcSocialTopicCount.TryGetValue(npcId, out int count) ? count : 0;
        }

        /// <summary>
        /// 增加指定NPC的话题计数
        /// </summary>
        public static void IncrementNpcTopicCount(int npcId)
        {
            if (npcId <= 0) return;
            
            if (!npcSocialTopicCount.ContainsKey(npcId))
            {
                npcSocialTopicCount[npcId] = 0;
            }
            npcSocialTopicCount[npcId]++;
            
            if (ModConfig.DebugMode.Value)
            {
                LinceMultipleLoversPlugin.Log.LogInfo($"[SocialTopic] NPC {npcId} 话题计数: {npcSocialTopicCount[npcId]}");
            }
        }

        /// <summary>
        /// 获取指定NPC的话题列表
        /// </summary>
        public static List<int> GetNpcTopics(int npcId)
        {
            if (npcId <= 0) return null;
            
            if (!npcTopicsThisRound.ContainsKey(npcId) || npcTopicsThisRound[npcId] == null)
            {
                // 为该NPC生成话题
                npcTopicsThisRound[npcId] = Singleton<CommonEvtMgr>.Ins.GetEnableEvtIds(22, npcId, 0, 3);
                
                if (ModConfig.DebugMode.Value)
                {
                    LinceMultipleLoversPlugin.Log.LogInfo($"[SocialTopic] 为NPC {npcId} 生成话题: {(npcTopicsThisRound[npcId] != null ? string.Join(", ", npcTopicsThisRound[npcId]) : "null")}");
                }
            }
            
            return npcTopicsThisRound[npcId];
        }

        /// <summary>
        /// 新回合时重置所有计数器
        /// </summary>
        public static void ResetForNewRound()
        {
            npcSocialTopicCount.Clear();
            npcTopicsThisRound.Clear();
            
            if (ModConfig.DebugMode.Value)
            {
                LinceMultipleLoversPlugin.Log.LogInfo("[SocialTopic] 新回合，重置所有角色话题计数");
            }
        }

        /// <summary>
        /// 修改CanSocialTopic方法 - 检查当前NPC是否可以话题
        /// 原逻辑: return this.socialTopicCntThisRound == 0;
        /// </summary>
        [HarmonyPatch(typeof(LoveData), nameof(LoveData.CanSocialTopic))]
        [HarmonyPrefix]
        public static bool CanSocialTopic_Prefix(ref bool __result, LoveData __instance)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return true; // 执行原方法

            // 获取当前正在交互的NPC
            int npcId = MapRoleViewTopicPatch.CurrentNpcId;
            
            // 如果没有记录当前NPC，使用主恋人
            if (npcId <= 0)
            {
                npcId = LoverIdInterceptor.GetPrimaryLoverId();
            }

            // 检查该NPC是否还有话题次数
            int count = GetNpcTopicCount(npcId);
            __result = count == 0;

            if (ModConfig.DebugMode.Value)
            {
                LinceMultipleLoversPlugin.Log.LogInfo($"[CanSocialTopic] NPC {npcId}: {__result} (已话题 {count} 次)");
            }

            return false; // 跳过原方法
        }

        /// <summary>
        /// 修改SocialTopic方法 - 记录每个NPC的话题次数
        /// 原逻辑: if (this.socialTopicCntThisRound > 0) return false;
        /// </summary>
        [HarmonyPatch(typeof(LoveData), nameof(LoveData.SocialTopic))]
        [HarmonyPrefix]
        public static bool SocialTopic_Prefix(ref bool __result, LoveData __instance, int _evtId, int _bgId)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return true; // 执行原方法

            // 获取当前正在交互的NPC
            int npcId = MapRoleViewTopicPatch.CurrentNpcId;
            
            // 如果没有记录当前NPC，尝试从事件ID推断
            if (npcId <= 0)
            {
                npcId = LoverIdInterceptor.GetPrimaryLoverId();
            }

            // 检查该NPC是否还可以话题
            if (GetNpcTopicCount(npcId) > 0)
            {
                __result = false;
                
                if (ModConfig.DebugMode.Value)
                {
                    LinceMultipleLoversPlugin.Log.LogInfo($"[SocialTopic] NPC {npcId} 本回合已话题过，无法再次话题");
                }
                
                return false;
            }

            // 检查心情值
            Role role = Singleton<RoleMgr>.Ins.GetRole();
            if (role.GetAttr(11, false) < 1f)
            {
                __result = false;
                return false;
            }

            // 消耗心情值
            role.UpdateAttr(11, -1f, 1f, DescCtrl.GetTxt(10002), 2);
            
            // 增加该NPC的话题计数
            IncrementNpcTopicCount(npcId);
            
            // 显示事件
            Singleton<CommonEvtMgr>.Ins.ShowEvent(_evtId, 1f, null, _bgId, true, null);
            
            __result = true;
            
            if (ModConfig.DebugMode.Value)
            {
                LinceMultipleLoversPlugin.Log.LogInfo($"[SocialTopic] NPC {npcId} 话题成功，事件ID: {_evtId}");
            }

            return false; // 跳过原方法
        }

        /// <summary>
        /// 修改GetTopics方法 - 获取当前NPC的话题
        /// </summary>
        [HarmonyPatch(typeof(LoveData), nameof(LoveData.GetTopics))]
        [HarmonyPrefix]
        public static bool GetTopics_Prefix(ref List<int> __result, LoveData __instance)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return true; // 执行原方法

            // 使用拦截器检查是否有恋人
            if (!LoverIdInterceptor.HasLover())
            {
                __result = null;
                return false;
            }

            // 获取当前正在交互的NPC
            int npcId = MapRoleViewTopicPatch.CurrentNpcId;
            
            // 如果没有记录或记录的不是恋人，使用主恋人
            if (npcId <= 0 || !LoverIdInterceptor.IsLover(npcId))
            {
                npcId = LoverIdInterceptor.GetPrimaryLoverId();
            }

            if (npcId <= 0)
            {
                __result = null;
                return false;
            }

            // 获取该NPC的话题列表
            __result = GetNpcTopics(npcId);

            if (ModConfig.DebugMode.Value)
            {
                LinceMultipleLoversPlugin.Log.LogInfo($"[GetTopics] 为NPC {npcId} 获取话题: {(__result != null ? string.Join(", ", __result) : "null")}");
            }

            return false; // 跳过原方法
        }

        /// <summary>
        /// 修改NewRound方法 - 新回合时重置计数器
        /// </summary>
        [HarmonyPatch(typeof(LoveData), nameof(LoveData.NewRound))]
        [HarmonyPostfix]
        public static void NewRound_Postfix(LoveData __instance)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return;

            ResetForNewRound();
        }
    }
}
