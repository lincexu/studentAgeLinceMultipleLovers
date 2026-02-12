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
        public static ConfigEntry<bool> DebugMode { get; private set; } = null!;

        public static void Init(ConfigFile config)
        {
            EnableMultipleLovers = config.Bind(
                "General",
                "EnableMultipleLovers",
                true,
                "启用多恋人功能 (Enable Multiple Lovers Feature)"
            );

            BypassSingleCheck = config.Bind(
                "General",
                "BypassSingleCheck",
                true,
                "绕过单身检查，允许在已有恋人的情况下表白 (Bypass single check when vindicating)"
            );

            DebugMode = config.Bind(
                "Debug",
                "DebugMode",
                false,
                "启用调试日志 (Enable debug logging)"
            );

            // 监听配置变更事件
            EnableMultipleLovers.SettingChanged += OnSettingChanged;
            BypassSingleCheck.SettingChanged += OnSettingChanged;
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
