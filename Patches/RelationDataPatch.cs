using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using Increase;
using Sdk;
using TheEntity;

namespace LinceMultipleLovers.Patches
{
    /// <summary>
    /// RelationData补丁 - 修复社交容量计算和关系显示
    /// </summary>
    public static class RelationDataPatch
    {
        /// <summary>
        /// 修改RefreshSocialCapacity方法 - 避免恋人关系重复计算社交容量
        /// 原逻辑：遍历所有关系类型，计算每个关系的社交容量
        /// 问题：恋人可能同时存在于原关系和恋人关系中，导致重复计算
        /// 修改后：如果角色是恋人，只计算恋人关系的容量，不计算原关系
        /// </summary>
        [HarmonyPatch(typeof(RelationData), "RefreshSocialCapacity")]
        [HarmonyPrefix]
        public static bool RefreshSocialCapacity_Prefix(RelationData __instance)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return true; // 执行原方法

            int num = 0; // 关系占用的容量
            
            // 获取所有恋人ID
            var loverIds = LoverIdInterceptor.GetAllLoverIds();
            HashSet<int> loverIdSet = new HashSet<int>(loverIds);

            if (ModConfig.DebugMode.Value)
            {
                LinceMultipleLoversPlugin.Log.LogInfo($"[RefreshSocialCapacity] 恋人列表: {string.Join(", ", loverIds)}");
            }

            // 直接访问relationDict而不是通过GetRelationship（避免Postfix干扰）
            // relationDict是public字段，但需要通过反射获取
            Dictionary<int, List<int>>? relationDict = null;
            try
            {
                var relationDictField = typeof(RelationData).GetField("relationDict", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                relationDict = relationDictField?.GetValue(__instance) as Dictionary<int, List<int>>;
            }
            catch (Exception ex)
            {
                LinceMultipleLoversPlugin.Log.LogError($"[RefreshSocialCapacity] 获取relationDict时出错: {ex.Message}");
            }

            if (relationDict == null)
            {
                LinceMultipleLoversPlugin.Log.LogError("[RefreshSocialCapacity] 无法获取relationDict，执行原方法");
                return true; // 执行原方法
            }

            // 先计算所有非恋人关系的容量
            foreach (KeyValuePair<int, RelationCfg> keyValuePair in Cfg.RelationCfgMap)
            {
                if (keyValuePair.Value.socialCapacity != 0)
                {
                    // 跳过恋人关系(520)，后面统一计算
                    if (keyValuePair.Key == 520)
                        continue;
                    
                    // 直接从relationDict获取，不经过GetRelationship
                    List<int> relationship = null;
                    if (relationDict.ContainsKey(keyValuePair.Key))
                    {
                        relationship = relationDict[keyValuePair.Key];
                    }
                    
                    if (relationship != null && relationship.Count > 0)
                    {
                        if (ModConfig.DebugMode.Value)
                        {
                            LinceMultipleLoversPlugin.Log.LogInfo($"[RefreshSocialCapacity] 关系类型 {keyValuePair.Key}: {string.Join(", ", relationship)} (容量占用: {keyValuePair.Value.socialCapacity})");
                        }
                        
                        foreach (int npcId in relationship)
                        {
                            // 关键修改：如果该NPC是恋人，不计算原关系的容量
                            if (loverIdSet.Contains(npcId))
                            {
                                // 跳过原关系容量，后面统一计算恋人容量
                                if (ModConfig.DebugMode.Value)
                                {
                                    LinceMultipleLoversPlugin.Log.LogInfo($"[RefreshSocialCapacity]   NPC {npcId} 是恋人，跳过关系 {keyValuePair.Key} 的容量 ({keyValuePair.Value.socialCapacity})");
                                }
                            }
                            else
                            {
                                // 非恋人，正常计算容量
                                num += keyValuePair.Value.socialCapacity;
                                
                                if (ModConfig.DebugMode.Value)
                                {
                                    LinceMultipleLoversPlugin.Log.LogInfo($"[RefreshSocialCapacity]   NPC {npcId} 不是恋人，计算关系 {keyValuePair.Key} 容量 +{keyValuePair.Value.socialCapacity}");
                                }
                            }
                        }
                    }
                }
            }
            
            // 统一计算所有恋人的容量（每个恋人占用5点）
            if (loverIds.Count > 0)
            {
                int loverCapacity = Cfg.RelationCfgMap[520].socialCapacity; // 恋人关系容量为5
                num += loverIds.Count * loverCapacity;
                
                if (ModConfig.DebugMode.Value)
                {
                    LinceMultipleLoversPlugin.Log.LogInfo($"[RefreshSocialCapacity] 计算恋人容量: {loverIds.Count}个恋人 × {loverCapacity} = {loverIds.Count * loverCapacity}");
                }
            }

            int num2 = 5; // 基础容量
            int num3 = UnityEngine.Mathf.Max(0, Singleton<RoleMgr>.Ins.GetRole().GetAttrRank(2).Item1 - 5) * (int)RoleMgr.GetConstValue(19); // 情商加成
            int num4 = (int)Singleton<RoleMgr>.Ins.GetRole().IncCtrl.GetValue(RoleIncType.OtherAttrInc, 400); // 额外加成
            
            // 使用反射设置socialCapacity
            var socialCapacityField = typeof(RelationData).GetField("socialCapacity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (socialCapacityField != null)
            {
                int socialCapacity = num2 + num3 + num4 - num;
                socialCapacityField.SetValue(__instance, socialCapacity);
                
                if (ModConfig.DebugMode.Value)
                {
                    LinceMultipleLoversPlugin.Log.LogInfo($"[RefreshSocialCapacity] 总计: 社交容量={socialCapacity} (基础{num2} + 情商{num3} + 额外{num4} - 关系{num})");
                }
            }

            // 设置setSocialCapacityDirty = false
            var setSocialCapacityDirtyField = typeof(RelationData).GetField("setSocialCapacityDirty", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (setSocialCapacityDirtyField != null)
            {
                setSocialCapacityDirtyField.SetValue(__instance, false);
            }

            // 处理负社交容量效果
            var socialCapacityValue = (int)(socialCapacityField?.GetValue(__instance) ?? 0);
            if (socialCapacityValue < 0)
            {
                // 检查是否已添加负容量效果
                var energyMaxDownField = typeof(RelationData).GetField("energyMaxDownByNegativeSocialCapacityEffectUid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                ulong energyMaxDownUid = (ulong)(energyMaxDownField?.GetValue(__instance) ?? 0UL);
                
                if (energyMaxDownUid == 0UL)
                {
                    // 添加负容量效果
                    var increaserOther = new IncreaserOther(null)
                    {
                        otherAttrId = 401,
                        tag = DescCtrl.GetTxt(1226)
                    };
                    energyMaxDownField?.SetValue(__instance, increaserOther.uid);
                    Singleton<RoleMgr>.Ins.GetRole().AddEffect(RoleIncType.AttrEachRoundInc, 11, increaserOther, null);
                    
                    if (ModConfig.DebugMode.Value)
                    {
                        LinceMultipleLoversPlugin.Log.LogInfo("[RefreshSocialCapacity] 添加效果：负社交容量影响社交热情");
                    }
                }
            }

            return false; // 跳过原方法
        }

        /// <summary>
        /// 修改GetSocialCapacity方法 - 确保返回正确的社交容量值
        /// 当setSocialCapacityDirty为true时，原方法可能计算了错误的值
        /// 这里重新计算正确的值
        /// </summary>
        [HarmonyPatch(typeof(RelationData), "GetSocialCapacity")]
        [HarmonyPostfix]
        public static void GetSocialCapacity_Postfix(ref int __result, RelationData __instance)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return;

            // 重新计算正确的社交容量
            int num = 0; // 关系占用的容量
            
            // 获取所有恋人ID
            var loverIds = LoverIdInterceptor.GetAllLoverIds();
            HashSet<int> loverIdSet = new HashSet<int>(loverIds);

            // 直接访问relationDict
            var relationDictField = typeof(RelationData).GetField("relationDict", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var relationDict = relationDictField?.GetValue(__instance) as Dictionary<int, List<int>>;

            if (relationDict == null)
                return; // 无法获取数据，保持原结果

            // 先计算所有非恋人关系的容量
            foreach (KeyValuePair<int, RelationCfg> keyValuePair in Cfg.RelationCfgMap)
            {
                if (keyValuePair.Value.socialCapacity != 0)
                {
                    // 跳过恋人关系(520)，后面统一计算
                    if (keyValuePair.Key == 520)
                        continue;
                    
                    if (relationDict.ContainsKey(keyValuePair.Key))
                    {
                        var relationship = relationDict[keyValuePair.Key];
                        if (relationship != null)
                        {
                            foreach (int npcId in relationship)
                            {
                                // 如果该NPC是恋人，不计算原关系的容量
                                if (!loverIdSet.Contains(npcId))
                                {
                                    // 非恋人，正常计算容量
                                    num += keyValuePair.Value.socialCapacity;
                                }
                            }
                        }
                    }
                }
            }
            
            // 统一计算所有恋人的容量（每个恋人占用5点）
            if (loverIds.Count > 0)
            {
                int loverCapacity = Cfg.RelationCfgMap[520].socialCapacity; // 恋人关系容量为5
                num += loverIds.Count * loverCapacity;
            }

            // 计算正确的社交容量
            int num2 = 5; // 基础容量
            int num3 = UnityEngine.Mathf.Max(0, Singleton<RoleMgr>.Ins.GetRole().GetAttrRank(2).Item1 - 5) * (int)RoleMgr.GetConstValue(19); // 情商加成
            int num4 = (int)Singleton<RoleMgr>.Ins.GetRole().IncCtrl.GetValue(RoleIncType.OtherAttrInc, 400); // 额外加成
            
            int correctCapacity = num2 + num3 + num4 - num;
            
            // 如果原结果不正确，修正它
            if (__result != correctCapacity)
            {
                if (ModConfig.DebugMode.Value)
                {
                    LinceMultipleLoversPlugin.Log.LogInfo($"[GetSocialCapacity] 修正社交容量: {__result} -> {correctCapacity} (关系占用: {num})");
                }
                __result = correctCapacity;
                
                // 同时更新socialCapacity字段，保持一致性
                var socialCapacityField = typeof(RelationData).GetField("socialCapacity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                socialCapacityField?.SetValue(__instance, correctCapacity);
            }
        }

        /// <summary>
        /// 修改GetRelationship方法 - 确保恋人角色在关系列表中正确显示
        /// 1. 如果查询的是恋人关系(520)，返回所有恋人
        /// 2. 如果查询的是非恋人关系，过滤掉已经是恋人的角色
        /// </summary>
        [HarmonyPatch(typeof(RelationData), "GetRelationship")]
        [HarmonyPostfix]
        public static void GetRelationship_Postfix(ref List<int> __result, RelationData __instance, int _relationId)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return;

            // 获取所有恋人
            var loverIds = LoverIdInterceptor.GetAllLoverIds();
            
            if (loverIds.Count == 0)
                return;

            // 如果查询的是恋人关系(520)
            if (_relationId == 520)
            {
                // 如果原结果为null，创建新列表
                if (__result == null)
                {
                    __result = new List<int>(loverIds);
                }
                else
                {
                    // 合并原结果和恋人列表（去重）
                    HashSet<int> combined = new HashSet<int>(__result);
                    foreach (int loverId in loverIds)
                    {
                        combined.Add(loverId);
                    }
                    __result = new List<int>(combined);
                }
                
                if (ModConfig.DebugMode.Value)
                {
                    LinceMultipleLoversPlugin.Log.LogInfo($"[GetRelationship] 查询恋人关系(520)，返回: {string.Join(", ", __result)}");
                }
            }
            else
            {
                // 查询非恋人关系，过滤掉已经是恋人的角色
                if (__result != null && __result.Count > 0)
                {
                    var filteredList = __result.Where(npcId => !loverIds.Contains(npcId)).ToList();
                    
                    if (filteredList.Count != __result.Count)
                    {
                        if (ModConfig.DebugMode.Value)
                        {
                            var removed = __result.Except(filteredList).ToList();
                            LinceMultipleLoversPlugin.Log.LogInfo($"[GetRelationship] 查询关系 {_relationId}，过滤掉恋人: {string.Join(", ", removed)}，返回: {string.Join(", ", filteredList)}");
                        }
                        __result = filteredList;
                    }
                }
            }
        }
    }
}
