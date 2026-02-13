using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Config;
using Sdk;
using TheEntity;

namespace LinceMultipleLovers.Patches
{
    /// <summary>
    /// RelationData补丁 - 修复社交容量计算
    /// 恋人关系不应该再计算之前的关系容量
    /// </summary>
    public static class RelationDataPatch
    {
        /// <summary>
        /// 修改RefreshSocialCapacity方法 - 恋人只计算恋人容量，不计算之前的关系容量
        /// </summary>
        [HarmonyPatch(typeof(RelationData), "RefreshSocialCapacity")]
        [HarmonyPrefix]
        public static bool RefreshSocialCapacity_Prefix(RelationData __instance)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return true; // 执行原方法

            int num = 0;
            
            // 获取所有恋人ID
            var loverIds = LoverIdInterceptor.GetAllLoverIds();
            
            foreach (var keyValuePair in Cfg.RelationCfgMap)
            {
                if (keyValuePair.Value.socialCapacity != 0)
                {
                    List<int> relationship = __instance.GetRelationship(keyValuePair.Key);
                    if (relationship != null)
                    {
                        int count = relationship.Count;
                        
                        // 关键修改：如果是恋人关系(520)，检查这些NPC是否也在其他关系中
                        // 如果是其他关系，检查该NPC是否是恋人，如果是则不计算
                        if (keyValuePair.Key != 520) // 非恋人关系
                        {
                            // 过滤掉已经是恋人的NPC
                            count = relationship.Count(npcId => !loverIds.Contains(npcId));
                        }
                        
                        num += count * keyValuePair.Value.socialCapacity;
                    }
                }
            }
            
            int num2 = 5;
            int num3 = UnityEngine.Mathf.Max(0, Singleton<RoleMgr>.Ins.GetRole().GetAttrRank(2).Item1 - 5) * (int)RoleMgr.GetConstValue(19);
            int num4 = (int)Singleton<RoleMgr>.Ins.GetRole().IncCtrl.GetValue(RoleIncType.OtherAttrInc, 400);
            
            var socialCapacityField = typeof(RelationData).GetField("socialCapacity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var setSocialCapacityDirtyField = typeof(RelationData).GetField("setSocialCapacityDirty", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (socialCapacityField != null)
            {
                socialCapacityField.SetValue(__instance, num2 + num3 + num4 - num);
            }
            
            if (setSocialCapacityDirtyField != null)
            {
                setSocialCapacityDirtyField.SetValue(__instance, false);
            }

            if (ModConfig.DebugMode.Value)
            {
                int totalCapacity = num2 + num3 + num4 - num;
                LinceMultipleLoversPlugin.Log.LogInfo($"[RefreshSocialCapacity] 总容量: {totalCapacity} = 基础{num2} + 情商{num3} + 额外{num4} - 关系消耗{num}");
            }

            return false; // 跳过原方法
        }
    }
}
