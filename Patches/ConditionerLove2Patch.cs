using HarmonyLib;
using Condition;
using Config;
using Sdk;

namespace LinceMultipleLovers.Patches
{
    /// <summary>
    /// ConditionerLove2补丁 - 修复恋人条件判断，支持多恋人
    /// 原逻辑只检查loverId，需要扩展为检查所有恋人
    /// 
    /// 优化内容：
    /// 1. 支持多恋人条件判断（subType=1,2,22,-22）
    /// 2. 实现AllowLoveActivity配置对type=3行动的特殊处理
    ///    - 当AllowLoveActivity=true时，type=3行动的单身验证使用原版逻辑
    ///    - 其他情况遵循AlwaysSingleCheck的强制单身规则
    /// </summary>
    public static class ConditionerLove2Patch
    {
        /// <summary>
        /// 修改OnIsMatch方法 - 支持多恋人条件判断
        /// [52, 1, 1] - 已脱单（有恋人）
        /// [52, 1, -1] - 单身（无恋人）
        /// [52, 2, 1, X] - X是主角的恋人（只要X在恋人列表中即成功）
        /// [52, 2, -1, X] - X不是主角的恋人
        /// [52, 22] - 有同性别恋人
        /// [52, -22] - 没有同性别恋人
        /// </summary>
        [HarmonyPatch(typeof(ConditionerLove2), "OnIsMatch")]
        [HarmonyPrefix]
        public static bool OnIsMatch_Prefix(ConditionerLove2 __instance, ref bool __result)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return true; // 执行原方法

            // 获取所有恋人ID
            var loverIds = LoverIdInterceptor.GetAllLoverIds();
            var currentLoverId = Singleton<RoleMgr>.Ins.GetLoveData().loverId;
            
            // 使用反射获取subType, value, npcId
            var subTypeField = typeof(ConditionerLove2).GetField("subType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var valueField = typeof(ConditionerLove2).GetField("value", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var npcIdField = typeof(ConditionerLove2).GetField("npcId", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            int subType = (int)(subTypeField?.GetValue(__instance) ?? 0);
            int value = (int)(valueField?.GetValue(__instance) ?? 0);
            int npcId = (int)(npcIdField?.GetValue(__instance) ?? 0);

            // subType = 1: 脱单/单身检查
            if (subType == 1)
            {
                // 检查是否应该使用原版单身验证逻辑
                // 关键优化：AllowLoveActivity=true 且当前是type=3行动解锁检查时使用原版逻辑
                bool useOriginalLogic = ShouldUseOriginalSingleCheck();
                
                if (value == 1)
                {
                    // [52, 1, 1] - 已脱单（有恋人）
                    __result = loverIds.Count > 0;
                }
                else
                {
                    // [52, 1, -1或其他] - 单身（无恋人）
                    __result = loverIds.Count == 0;
                }
                
                // 如果启用了"始终单身"选项且不使用原版逻辑，则强制返回单身状态
                if (ModConfig.AlwaysSingleCheck.Value && !useOriginalLogic)
                {
                    if (value == 1)
                    {
                        // [52, 1, 1] - 已脱单，但强制返回false
                        __result = false;
                    }
                    else
                    {
                        // [52, 1, -1或其他] - 单身，强制返回true
                        __result = true;
                    }
                    
                    if (ModConfig.DebugMode.Value)
                    {
                        LinceMultipleLoversPlugin.Log.LogInfo($"[ConditionerLove2] 强制单身模式: value={value}, 结果={__result}, 上下文={GetContextInfo()}");
                    }
                }
                else if (ModConfig.DebugMode.Value)
                {
                    string mode = useOriginalLogic ? "原版逻辑" : "正常模式";
                    LinceMultipleLoversPlugin.Log.LogInfo($"[ConditionerLove2] 脱单检查({mode}): value={value}, 恋人数量={loverIds.Count}, 结果={__result}, 上下文={GetContextInfo()}");
                }
                
                return false; // 跳过原方法
            }
            
            // subType = 2: 特定NPC恋人检查
            if (subType == 2)
            {
                // 关键修改：检查npcId是否在恋人列表中，而不是只检查currentLoverId
                bool isLover = loverIds.Contains(npcId);
                
                if (value == 1)
                {
                    // [52, 2, 1, X] - X是主角的恋人
                    __result = isLover;
                }
                else
                {
                    // [52, 2, -1或其他, X] - X不是主角的恋人
                    __result = !isLover;
                }
                
                if (ModConfig.DebugMode.Value)
                {
                    LinceMultipleLoversPlugin.Log.LogInfo($"[ConditionerLove2] 特定NPC检查: npcId={npcId}, value={value}, 恋人列表=[{string.Join(", ", loverIds)}], 结果={__result}");
                }
                
                return false; // 跳过原方法
            }
            
            // subType = 22 / -22: 同性别/不同性别恋人检查
            if (subType == 22 || subType == -22)
            {
                // 检查是否有任何恋人
                if (loverIds.Count == 0)
                {
                    // 没有恋人
                    __result = (subType != 22); // 22返回false，-22返回true
                }
                else
                {
                    // 有恋人，检查所有恋人的性别
                    bool hasSameGenderLover = false;
                    int playerGender = (int)Singleton<RoleMgr>.Ins.GetRole().Sex;
                    
                    foreach (int loverId in loverIds)
                    {
                        if (Cfg.PersonCfgMap.TryGetValue(loverId, out var personCfg))
                        {
                            if (personCfg.gender == playerGender)
                            {
                                hasSameGenderLover = true;
                                break;
                            }
                        }
                    }
                    
                    if (subType == 22)
                    {
                        // [52, 22] - 有同性别恋人
                        __result = hasSameGenderLover;
                    }
                    else
                    {
                        // [52, -22] - 没有同性别恋人（即所有恋人都是不同性别）
                        __result = !hasSameGenderLover;
                    }
                }
                
                if (ModConfig.DebugMode.Value)
                {
                    LinceMultipleLoversPlugin.Log.LogInfo($"[ConditionerLove2] 性别检查: subType={subType}, 恋人数量={loverIds.Count}, 结果={__result}");
                }
                
                return false; // 跳过原方法
            }

            // 其他情况，执行原方法
            return true;
        }

        /// <summary>
        /// 判断是否应该使用原版单身检查逻辑
        /// 核心逻辑：
        /// 1. 如果未启用多恋人功能 → 使用原版逻辑
        /// 2. 如果未启用强制单身 → 使用原版逻辑
        /// 3. 如果AllowLoveActivity=true 且当前是type=3行动解锁检查 → 使用原版逻辑
        /// 4. 其他情况 → 使用强制单身逻辑
        /// </summary>
        private static bool ShouldUseOriginalSingleCheck()
        {
            // 使用ActionUnlockContext的决策逻辑
            return ActionUnlockContext.ShouldUseOriginalSingleCheck();
        }

        /// <summary>
        /// 获取当前上下文信息（用于调试）
        /// </summary>
        private static string GetContextInfo()
        {
            if (ActionUnlockContext.IsInUnlockContext)
            {
                return $"ActionUnlock(actionId={ActionUnlockContext.CurrentActionId}, type={ActionUnlockContext.CurrentActionType})";
            }
            return "General";
        }
        
        /// <summary>
        /// 修改OnGetProgress方法 - 支持多恋人进度显示
        /// </summary>
        [HarmonyPatch(typeof(ConditionerLove2), "OnGetProgress")]
        [HarmonyPrefix]
        public static bool OnGetProgress_Prefix(ConditionerLove2 __instance, ref (float, float) __result)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return true; // 执行原方法

            // 获取所有恋人ID
            var loverIds = LoverIdInterceptor.GetAllLoverIds();
            
            // 使用反射获取subType, value, npcId
            var subTypeField = typeof(ConditionerLove2).GetField("subType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var valueField = typeof(ConditionerLove2).GetField("value", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var npcIdField = typeof(ConditionerLove2).GetField("npcId", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            int subType = (int)(subTypeField?.GetValue(__instance) ?? 0);
            int value = (int)(valueField?.GetValue(__instance) ?? 0);
            int npcId = (int)(npcIdField?.GetValue(__instance) ?? 0);

            // subType = 1: 脱单/单身进度
            if (subType == 1)
            {
                if (value == 1)
                {
                    // 已脱单进度
                    __result = (loverIds.Count > 0) ? (1f, 1f) : (0f, 1f);
                }
                else
                {
                    // 单身进度
                    __result = (loverIds.Count == 0) ? (1f, 1f) : (0f, 1f);
                }
                return false; // 跳过原方法
            }
            
            // subType = 2: 特定NPC进度
            if (subType == 2)
            {
                bool isLover = loverIds.Contains(npcId);
                
                if (value == 1)
                {
                    // X是恋人进度
                    __result = isLover ? (1f, 1f) : (0f, 1f);
                }
                else
                {
                    // X不是恋人进度
                    __result = !isLover ? (1f, 1f) : (0f, 1f);
                }
                return false; // 跳过原方法
            }
            
            // subType = 22 / -22: 同性别/不同性别进度
            if (subType == 22 || subType == -22)
            {
                if (loverIds.Count == 0)
                {
                    __result = (subType != 22) ? (1f, 1f) : (0f, 1f);
                }
                else
                {
                    bool hasSameGenderLover = false;
                    int playerGender = (int)Singleton<RoleMgr>.Ins.GetRole().Sex;
                    
                    foreach (int loverId in loverIds)
                    {
                        if (Cfg.PersonCfgMap.TryGetValue(loverId, out var personCfg))
                        {
                            if (personCfg.gender == playerGender)
                            {
                                hasSameGenderLover = true;
                                break;
                            }
                        }
                    }
                    
                    if (subType == 22)
                    {
                        __result = hasSameGenderLover ? (1f, 1f) : (0f, 1f);
                    }
                    else
                    {
                        __result = !hasSameGenderLover ? (1f, 1f) : (0f, 1f);
                    }
                }
                return false; // 跳过原方法
            }

            // 其他情况，执行原方法
            return true;
        }
    }
}
