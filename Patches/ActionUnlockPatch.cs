using HarmonyLib;
using Config;
using Sdk;
using System;
using System.Collections.Generic;

namespace LinceMultipleLovers.Patches
{
    /// <summary>
    /// ActionUnlock补丁 - 在行动解锁条件检查前设置上下文
    /// 用于支持AllowLoveActivity配置对type=3行动的特殊处理
    /// 
    /// 实现原理：
    /// 1. 拦截所有调用CommonEvtMgr.IsMatchCondition(List&lt;List&lt;double&gt;&gt;)的地方
    /// 2. 在调用前检查是否来自行动解锁检查（通过检查调用方是否在ActionView/MapView/MapSceneView/QuickActionView中）
    /// 3. 如果是行动解锁检查，遍历所有行动找到匹配当前条件的行动ID
    /// 4. 设置上下文供ConditionerLove2Patch使用
    /// </summary>
    public static class ActionUnlockPatch
    {
        // 标记是否处于行动解锁检查上下文
        private static bool _isProcessingActionUnlock = false;
        
        // 当前处理的行动ID
        private static int _currentActionId = -1;

        /// <summary>
        /// 检查是否处于行动解锁处理上下文
        /// </summary>
        public static bool IsProcessingActionUnlock => _isProcessingActionUnlock;

        /// <summary>
        /// 获取当前处理的行动ID
        /// </summary>
        public static int CurrentActionId => _currentActionId;

        /// <summary>
        /// 设置行动解锁处理上下文
        /// </summary>
        public static void SetProcessingContext(int actionId)
        {
            _isProcessingActionUnlock = true;
            _currentActionId = actionId;
        }

        /// <summary>
        /// 清除行动解锁处理上下文
        /// </summary>
        public static void ClearProcessingContext()
        {
            _isProcessingActionUnlock = false;
            _currentActionId = -1;
        }

        /// <summary>
        /// 尝试从条件反查行动ID
        /// 通过遍历ActionCfgMap找到unlock条件匹配的行动
        /// </summary>
        public static bool TryFindActionByCondition(List<List<double>> condition, out int actionId, out ActionCfg actionCfg)
        {
            actionId = -1;
            actionCfg = null;

            if (condition == null || condition.Count == 0)
                return false;

            // 遍历所有行动配置
            foreach (var kvp in Cfg.ActionCfgMap)
            {
                var cfg = kvp.Value;
                if (cfg.unlock != null && ConditionsEqual(cfg.unlock, condition))
                {
                    actionId = kvp.Key;
                    actionCfg = cfg;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 比较两个条件列表是否相等
        /// </summary>
        private static bool ConditionsEqual(List<List<double>> a, List<List<double>> b)
        {
            if (a == null || b == null)
                return a == b;

            if (a.Count != b.Count)
                return false;

            for (int i = 0; i < a.Count; i++)
            {
                if (a[i] == null || b[i] == null)
                {
                    if (a[i] != b[i])
                        return false;
                    continue;
                }

                if (a[i].Count != b[i].Count)
                    return false;

                for (int j = 0; j < a[i].Count; j++)
                {
                    // 使用近似比较浮点数
                    if (Math.Abs(a[i][j] - b[i][j]) > 0.0001)
                        return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// CommonEvtMgr.IsMatchCondition补丁 - 双层条件列表版本
    /// 这是行动unlock条件检查的主要入口
    /// </summary>
    [HarmonyPatch(typeof(CommonEvtMgr), nameof(CommonEvtMgr.IsMatchCondition), new Type[] { typeof(List<List<double>>), typeof(bool) })]
    public static class CommonEvtMgrIsMatchConditionPatch
    {
        [HarmonyPrefix]
        public static void Prefix(List<List<double>> _conditions)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return;

            // 检查调用栈是否来自行动解锁检查
            if (IsFromActionUnlockCheck())
            {
                // 尝试找到匹配的行动
                if (ActionUnlockPatch.TryFindActionByCondition(_conditions, out int actionId, out var actionCfg))
                {
                    ActionUnlockPatch.SetProcessingContext(actionId);
                    ActionUnlockContext.SetContext(actionId);

                    if (ModConfig.DebugMode.Value)
                    {
                        LinceMultipleLoversPlugin.Log.LogInfo($"[ActionUnlockPatch] 检测到行动解锁检查: actionId={actionId}, type={actionCfg?.type}, name={actionCfg?.name}");
                    }
                }
            }
        }

        [HarmonyPostfix]
        public static void Postfix()
        {
            if (ActionUnlockPatch.IsProcessingActionUnlock)
            {
                ActionUnlockPatch.ClearProcessingContext();
                ActionUnlockContext.ClearContext();
            }
        }

        /// <summary>
        /// 检查调用栈是否来自行动解锁检查
        /// </summary>
        private static bool IsFromActionUnlockCheck()
        {
            var stackTrace = new System.Diagnostics.StackTrace();
            var frames = stackTrace.GetFrames();

            if (frames == null)
                return false;

            foreach (var frame in frames)
            {
                var method = frame.GetMethod();
                var declaringType = method.DeclaringType;

                if (declaringType == null)
                    continue;

                string typeName = declaringType.Name;

                // 检查是否来自行动相关的视图类
                if (typeName == "ActionView" ||
                    typeName == "MapView" ||
                    typeName == "MapSceneView" ||
                    typeName == "QuickActionView")
                {
                    return true;
                }
            }

            return false;
        }
    }
}
