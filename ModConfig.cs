using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinceMultipleLovers
{
    public static class ModConfig
    {
        // 配置项
        public static ConfigEntry<bool> EnableMultipleLovers { get; private set; } = null!;
        public static ConfigEntry<bool> BypassSingleCheck { get; private set; } = null!;
        public static ConfigEntry<bool> AlwaysSingleCheck { get; private set; } = null!;
        public static ConfigEntry<bool> AllowLoveActivity { get; private set; } = null!;
        public static ConfigEntry<bool> ForceNoLoveHistory { get; private set; } = null!;
        public static ConfigEntry<bool> LockLoverId { get; private set; } = null!;
        public static ConfigEntry<bool> DebugMode { get; private set; } = null!;
        public static ConfigEntry<string> FeedbackInfo { get; private set; } = null!;

        public static void Init(ConfigFile config)
        {
            EnableMultipleLovers = config.Bind(
                "通用设置",
                "启用多恋人功能",
                true,
                "启用多恋人功能，允许同时拥有多个恋人"
            );

            BypassSingleCheck = config.Bind(
                "通用设置",
                "绕过单身检查",
                true,
                "允许在已有恋人的情况下继续告白"
            );

            AlwaysSingleCheck = config.Bind(
                "通用设置",
                "主角始终判定为单身",
                false,
                "主角单身判定始终为真（用于调试或特殊玩法）"
            );

            AllowLoveActivity = config.Bind(
                "通用设置",
                "允许恋爱活动",
                true,
                "即使开启强制单身，也允许恋爱活动触发（用于任务推进）"
            );

            ForceNoLoveHistory = config.Bind(
                "通用设置",
                "强制无恋爱经历",
                false,
                "开启时，主角始终被判定为无恋爱经历（condition [11,3] 始终返回true）。关闭时正常验证historyLover"
            );

            LockLoverId = config.Bind(
                "通用设置",
                "锁定loverId",
                false,
                "开启后 loverId 不再自动切换（持久配置，区别于控制台 LINCE LOVERID LOCK 的运行时锁定）"
            );

            DebugMode = config.Bind(
                "调试设置",
                "启用调试日志",
                false,
                "启用调试日志输出"
            );

            // 反馈说明（只读）
            FeedbackInfo = config.Bind(
                "关于",
                "反馈说明",
                "当前Mod仍处于测试版本，如遇Bug请联系: lincexu@qq.com",
                "【重要】当前Mod仍处于测试版本，如遇Bug请联系: lincexu@qq.com"
            );

            // 监听配置变更事件
            EnableMultipleLovers.SettingChanged += OnSettingChanged;
            BypassSingleCheck.SettingChanged += OnSettingChanged;
            AlwaysSingleCheck.SettingChanged += OnSettingChanged;
            AllowLoveActivity.SettingChanged += OnSettingChanged;
            ForceNoLoveHistory.SettingChanged += OnSettingChanged;
            LockLoverId.SettingChanged += OnSettingChanged;
            DebugMode.SettingChanged += OnSettingChanged;

            LinceMultipleLoversPlugin.Log.LogInfo("配置初始化完成");
        }

        /// <summary>
        /// 配置变更事件处理
        /// </summary>
        private static void OnSettingChanged(object sender, EventArgs e)
        {
            LinceMultipleLoversPlugin.Log.LogInfo("配置项已变更");
        }
    }
}
