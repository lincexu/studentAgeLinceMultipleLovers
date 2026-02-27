using Config;
using Sdk;

namespace LinceMultipleLovers
{
    /// <summary>
    /// 行动解锁上下文追踪器
    /// 用于在条件检查期间追踪当前正在检查的行动ID和类型
    /// 解决AllowLoveActivity配置对type=3行动的特殊处理需求
    /// </summary>
    public static class ActionUnlockContext
    {
        // 当前正在检查的行动ID
        private static int _currentActionId = -1;
        
        // 当前正在检查的行动类型
        private static int _currentActionType = -1;
        
        // 是否处于行动解锁检查上下文
        private static bool _isInUnlockContext = false;

        /// <summary>
        /// 获取当前行动ID
        /// </summary>
        public static int CurrentActionId => _currentActionId;

        /// <summary>
        /// 获取当前行动类型
        /// </summary>
        public static int CurrentActionType => _currentActionType;

        /// <summary>
        /// 是否处于行动解锁检查上下文
        /// </summary>
        public static bool IsInUnlockContext => _isInUnlockContext;

        /// <summary>
        /// 设置当前行动解锁检查上下文
        /// 在检查行动unlock条件前调用
        /// </summary>
        /// <param name="actionId">行动ID</param>
        public static void SetContext(int actionId)
        {
            _currentActionId = actionId;
            _isInUnlockContext = true;
            
            // 获取行动类型
            if (Cfg.ActionCfgMap.TryGetValue(actionId, out var actionCfg))
            {
                _currentActionType = actionCfg.type;
            }
            else
            {
                _currentActionType = -1;
            }
            
            if (ModConfig.DebugMode.Value)
            {
                LinceMultipleLoversPlugin.Log.LogInfo($"[ActionUnlockContext] 设置上下文: actionId={actionId}, type={_currentActionType}");
            }
        }

        /// <summary>
        /// 清除行动解锁检查上下文
        /// 在检查完成后调用
        /// </summary>
        public static void ClearContext()
        {
            if (ModConfig.DebugMode.Value && _isInUnlockContext)
            {
                LinceMultipleLoversPlugin.Log.LogInfo($"[ActionUnlockContext] 清除上下文: actionId={_currentActionId}, type={_currentActionType}");
            }
            
            _currentActionId = -1;
            _currentActionType = -1;
            _isInUnlockContext = false;
        }

        /// <summary>
        /// 检查当前是否是type=3（恋爱）行动的解锁检查
        /// </summary>
        public static bool IsLoveActionUnlockCheck()
        {
            return _isInUnlockContext && _currentActionType == 3;
        }

        /// <summary>
        /// 根据配置决定是否使用原版单身检查逻辑
        /// 核心逻辑：
        /// - AllowLoveActivity=true + 当前是type=3行动解锁检查 → 使用原版逻辑（返回true）
        /// - 其他情况 → 使用强制单身逻辑（返回false）
        /// </summary>
        public static bool ShouldUseOriginalSingleCheck()
        {
            // 只有在启用多恋人功能且启用了允许恋爱活动的情况下才需要特殊处理
            if (!ModConfig.EnableMultipleLovers.Value)
                return true; // 未启用多恋人，使用原版逻辑
            
            if (!ModConfig.AlwaysSingleCheck.Value)
                return true; // 未启用强制单身，使用原版逻辑
            
            // 关键逻辑：AllowLoveActivity=true 且当前是type=3行动的解锁检查
            if (ModConfig.AllowLoveActivity.Value && IsLoveActionUnlockCheck())
            {
                if (ModConfig.DebugMode.Value)
                {
                    LinceMultipleLoversPlugin.Log.LogInfo($"[ActionUnlockContext] type=3恋爱行动解锁检查，使用原版单身验证逻辑 (actionId={_currentActionId})");
                }
                return true; // 使用原版逻辑
            }
            
            // 其他情况使用强制单身逻辑
            if (ModConfig.DebugMode.Value)
            {
                LinceMultipleLoversPlugin.Log.LogInfo($"[ActionUnlockContext] 非type=3行动或AllowLoveActivity=false，使用强制单身逻辑 (actionId={_currentActionId}, type={_currentActionType})");
            }
            return false; // 使用强制单身逻辑
        }
    }
}
