using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LinceMultipleLovers
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class LinceMultipleLoversPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log { get; private set; } = null!;
        internal static Harmony Harmony { get; private set; } = null!;
        internal static MultipleLoversManager LoversManager { get; private set; } = null!;

        private void Awake()
        {
            try
            {
                Log = Logger;
                Log.LogInfo($"多恋人Mod v{PluginInfo.PLUGIN_VERSION} 正在加载...");

                // 初始化配置
                ModConfig.Init(Config);

                // 初始化多恋人管理器
                LoversManager = new MultipleLoversManager();

                // 应用Harmony补丁
                Harmony = new Harmony(PluginInfo.PLUGIN_GUID);
                
                // 手动应用所有补丁
                ApplyPatches();

                // 初始化调试命令
                DebugCommands.Init();

                Log.LogInfo("多恋人Mod加载完成!");
                Log.LogInfo("调试快捷键: F1=帮助, F2=状态, F3=强制设置当前NPC为恋人, F4=检查配置");
            }
            catch (Exception ex)
            {
                Log.LogError($"多恋人Mod加载失败: {ex}");
            }
        }

        private void ApplyPatches()
        {
            try
            {
                // 获取LoveData类型
                var loveDataType = typeof(Singleton<RoleMgr>).Assembly.GetType("LoveData");
                if (loveDataType == null)
                {
                    Log.LogError("无法找到LoveData类型");
                    return;
                }
                Log.LogInfo($"找到LoveData类型: {loveDataType.FullName}");

                // 应用LoveDataPatch
                Log.LogInfo("正在应用LoveData补丁...");
                
                // CanShowVindicateBtn
                var canShowMethod = loveDataType.GetMethod("CanShowVindicateBtn");
                if (canShowMethod != null)
                {
                    Harmony.Patch(canShowMethod, 
                        prefix: new HarmonyMethod(typeof(Patches.LoveDataPatch), nameof(Patches.LoveDataPatch.CanShowVindicateBtn_Prefix)));
                    Log.LogInfo("成功应用CanShowVindicateBtn补丁");
                }

                // CanVinidcate
                var canVinidcateMethod = loveDataType.GetMethod("CanVinidcate");
                if (canVinidcateMethod != null)
                {
                    Harmony.Patch(canVinidcateMethod,
                        prefix: new HarmonyMethod(typeof(Patches.LoveDataPatch), nameof(Patches.LoveDataPatch.CanVinidcate_Prefix)));
                    Log.LogInfo("成功应用CanVinidcate补丁");
                }

                // SetLover
                var setLoverMethod = loveDataType.GetMethod("SetLover");
                if (setLoverMethod != null)
                {
                    Harmony.Patch(setLoverMethod,
                        prefix: new HarmonyMethod(typeof(Patches.LoveDataPatch), nameof(Patches.LoveDataPatch.SetLover_Prefix)),
                        postfix: new HarmonyMethod(typeof(Patches.LoveDataPatch), nameof(Patches.LoveDataPatch.SetLover_Postfix)));
                    Log.LogInfo("成功应用SetLover补丁");
                }

                // CheckGreeting
                var checkGreetingMethod = loveDataType.GetMethod("CheckGreeting");
                if (checkGreetingMethod != null)
                {
                    Harmony.Patch(checkGreetingMethod,
                        prefix: new HarmonyMethod(typeof(Patches.LoveDataPatch), nameof(Patches.LoveDataPatch.CheckGreeting_Prefix)));
                    Log.LogInfo("成功应用CheckGreeting补丁");
                }

                // CanGiveBirthdayGift
                var canGiveBirthdayMethod = loveDataType.GetMethod("CanGiveBirthdayGift");
                if (canGiveBirthdayMethod != null)
                {
                    Harmony.Patch(canGiveBirthdayMethod,
                        prefix: new HarmonyMethod(typeof(Patches.LoveDataPatch), nameof(Patches.LoveDataPatch.CanGiveBirthdayGift_Prefix)));
                    Log.LogInfo("成功应用CanGiveBirthdayGift补丁");
                }

                // NewRound - 由LoveDataSocialTopicPatch处理
                var newRoundMethod = loveDataType.GetMethod("NewRound");
                if (newRoundMethod != null)
                {
                    Harmony.Patch(newRoundMethod,
                        postfix: new HarmonyMethod(typeof(Patches.LoveDataSocialTopicPatch), nameof(Patches.LoveDataSocialTopicPatch.NewRound_Postfix)));
                    Log.LogInfo("成功应用NewRound补丁(社交话题)");
                }

                // CanSocialTopic
                var canSocialTopicMethod = loveDataType.GetMethod("CanSocialTopic");
                if (canSocialTopicMethod != null)
                {
                    Harmony.Patch(canSocialTopicMethod,
                        prefix: new HarmonyMethod(typeof(Patches.LoveDataSocialTopicPatch), nameof(Patches.LoveDataSocialTopicPatch.CanSocialTopic_Prefix)));
                    Log.LogInfo("成功应用CanSocialTopic补丁");
                }

                // SocialTopic
                var socialTopicMethod = loveDataType.GetMethod("SocialTopic");
                if (socialTopicMethod != null)
                {
                    Harmony.Patch(socialTopicMethod,
                        prefix: new HarmonyMethod(typeof(Patches.LoveDataSocialTopicPatch), nameof(Patches.LoveDataSocialTopicPatch.SocialTopic_Prefix)));
                    Log.LogInfo("成功应用SocialTopic补丁");
                }

                // GetTopics - 由LoveDataSocialTopicPatch处理
                var getTopicsMethod = loveDataType.GetMethod("GetTopics");
                if (getTopicsMethod != null)
                {
                    Harmony.Patch(getTopicsMethod,
                        prefix: new HarmonyMethod(typeof(Patches.LoveDataSocialTopicPatch), nameof(Patches.LoveDataSocialTopicPatch.GetTopics_Prefix)));
                    Log.LogInfo("成功应用GetTopics补丁(社交话题)");
                }

                // 应用MapRoleViewPatch
                Log.LogInfo("正在应用MapRoleView补丁...");
                var mapRoleViewType = typeof(View.Main.MapRoleView);
                var refreshDataMethod = mapRoleViewType.GetMethod("RefreshData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (refreshDataMethod != null)
                {
                    Harmony.Patch(refreshDataMethod,
                        prefix: new HarmonyMethod(typeof(Patches.MapRoleViewPatch), nameof(Patches.MapRoleViewPatch.RefreshData_Prefix)));
                    Log.LogInfo("成功应用RefreshData补丁");
                }

                // 应用RolePatch
                Log.LogInfo("正在应用Role补丁...");
                var roleType = typeof(TheEntity.Role);
                var getRelationForUIMethod = roleType.GetMethod("GetRelationForUI");
                if (getRelationForUIMethod != null)
                {
                    Harmony.Patch(getRelationForUIMethod,
                        prefix: new HarmonyMethod(typeof(Patches.RolePatch), nameof(Patches.RolePatch.GetRelationForUI_Prefix)));
                    Log.LogInfo("成功应用GetRelationForUI补丁");
                }

                // 应用话题相关补丁
                Log.LogInfo("正在应用话题补丁...");
                Log.LogInfo($"mapRoleViewType = {mapRoleViewType?.FullName ?? "null"}");
                
                // MapRoleView.OnCellRender
                Log.LogInfo("查找OnCellRender方法...");
                var onCellRenderMethod = mapRoleViewType?.GetMethod("OnCellRender", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                Log.LogInfo($"onCellRenderMethod = {onCellRenderMethod?.Name ?? "null"}");
                if (onCellRenderMethod != null)
                {
                    try
                    {
                        Harmony.Patch(onCellRenderMethod,
                            postfix: new HarmonyMethod(typeof(Patches.MapRoleViewTopicPatch), nameof(Patches.MapRoleViewTopicPatch.OnCellRender_Postfix)));
                        Log.LogInfo("成功应用OnCellRender补丁");
                    }
                    catch (Exception ex)
                    {
                        Log.LogError($"应用OnCellRender补丁失败: {ex}");
                    }
                }
                else
                {
                    Log.LogError("找不到OnCellRender方法");
                }
                
                // MapRoleView.OnCellCreate
                Log.LogInfo("查找OnCellCreate方法...");
                var onCellCreateMethod = mapRoleViewType?.GetMethod("OnCellCreate", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                Log.LogInfo($"onCellCreateMethod = {onCellCreateMethod?.Name ?? "null"}");
                if (onCellCreateMethod != null)
                {
                    try
                    {
                        Harmony.Patch(onCellCreateMethod,
                            postfix: new HarmonyMethod(typeof(Patches.MapRoleViewTopicPatch), nameof(Patches.MapRoleViewTopicPatch.OnCellCreate_Postfix)));
                        Log.LogInfo("成功应用OnCellCreate补丁");
                    }
                    catch (Exception ex)
                    {
                        Log.LogError($"应用OnCellCreate补丁失败: {ex}");
                    }
                }
                else
                {
                    Log.LogError("找不到OnCellCreate方法");
                }

                // 应用ActionDataPatch
                Log.LogInfo("正在应用ActionData补丁...");
                var actionDataType = typeof(ActionData);
                var helpLoveActionMethod = actionDataType.GetMethod("HelpLoveAction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (helpLoveActionMethod != null)
                {
                    try
                    {
                        Harmony.Patch(helpLoveActionMethod,
                            prefix: new HarmonyMethod(typeof(Patches.ActionDataPatch), nameof(Patches.ActionDataPatch.HelpLoveAction_Prefix)));
                        Log.LogInfo("成功应用HelpLoveAction补丁");
                    }
                    catch (Exception ex)
                    {
                        Log.LogError($"应用HelpLoveAction补丁失败: {ex}");
                    }
                }
                else
                {
                    Log.LogError("找不到HelpLoveAction方法");
                }

                // 应用MiniGamePatch
                Log.LogInfo("正在应用MiniGame补丁...");
                var badmintonMiniGameViewType = typeof(MiniGame.Badminton.BadmintonMiniGameView);
                var badmintonOnOpenMethod = badmintonMiniGameViewType.GetMethod("OnOpen", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (badmintonOnOpenMethod != null)
                {
                    try
                    {
                        Harmony.Patch(badmintonOnOpenMethod,
                            postfix: new HarmonyMethod(typeof(Patches.MiniGamePatch), nameof(Patches.MiniGamePatch.BadmintonOnOpen_Postfix)));
                        Log.LogInfo("成功应用BadmintonMiniGameView.OnOpen补丁");
                    }
                    catch (Exception ex)
                    {
                        Log.LogError($"应用BadmintonMiniGameView.OnOpen补丁失败: {ex}");
                    }
                }
                else
                {
                    Log.LogError("找不到BadmintonMiniGameView.OnOpen方法");
                }

                // 应用ConditionerLovePatch
                Log.LogInfo("正在应用ConditionerLove补丁...");
                var conditionerLoveType = typeof(Condition.ConditionerLove);
                var onIsMatchMethod = conditionerLoveType.GetMethod("OnIsMatch");
                if (onIsMatchMethod != null)
                {
                    try
                    {
                        Harmony.Patch(onIsMatchMethod,
                            prefix: new HarmonyMethod(typeof(Patches.ConditionerLovePatch), nameof(Patches.ConditionerLovePatch.OnIsMatch_Prefix)));
                        Log.LogInfo("成功应用ConditionerLove.OnIsMatch补丁");
                    }
                    catch (Exception ex)
                    {
                        Log.LogError($"应用ConditionerLove.OnIsMatch补丁失败: {ex}");
                    }
                }
                else
                {
                    Log.LogError("找不到ConditionerLove.OnIsMatch方法");
                }

                // 应用QuickSocialViewPatch
                Log.LogInfo("正在应用QuickSocialView补丁...");
                var quickSocialViewType = typeof(View.TheAction.QuickSocialView);
                var onRenderSocialMethod = quickSocialViewType.GetMethod("OnRenderSocial", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (onRenderSocialMethod != null)
                {
                    try
                    {
                        Harmony.Patch(onRenderSocialMethod,
                            prefix: new HarmonyMethod(typeof(Patches.QuickSocialViewPatch), nameof(Patches.QuickSocialViewPatch.OnRenderSocial_Prefix)));
                        Log.LogInfo("成功应用QuickSocialView.OnRenderSocial补丁");
                    }
                    catch (Exception ex)
                    {
                        Log.LogError($"应用QuickSocialView.OnRenderSocial补丁失败: {ex}");
                    }
                }
                else
                {
                    Log.LogError("找不到QuickSocialView.OnRenderSocial方法");
                }

                // 应用RelationDataPatch
                Log.LogInfo("正在应用RelationData补丁...");
                var relationDataType = typeof(RelationData);
                var changeRelationMethod = relationDataType.GetMethod(nameof(RelationData.ChangeRelation));
                if (changeRelationMethod != null)
                {
                    try
                    {
                        Harmony.Patch(changeRelationMethod,
                            prefix: new HarmonyMethod(typeof(Patches.RelationDataPatch), nameof(Patches.RelationDataPatch.ChangeRelation_Prefix)));
                        Log.LogInfo("成功应用RelationData.ChangeRelation补丁");
                    }
                    catch (Exception ex)
                    {
                        Log.LogError($"应用RelationData.ChangeRelation补丁失败: {ex}");
                    }
                }
                else
                {
                    Log.LogError("找不到RelationData.ChangeRelation方法");
                }

                Log.LogInfo("所有补丁应用完成!");
            }
            catch (Exception ex)
            {
                Log.LogError($"应用补丁时出错: {ex}");
            }
        }

        private void OnDestroy()
        {
            try
            {
                Harmony?.UnpatchSelf();
                Log.LogInfo("多恋人Mod已卸载");
            }
            catch (Exception ex)
            {
                Log.LogError($"卸载Mod时出错: {ex}");
            }
        }
    }

    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "lince.multiplelovers";
        public const string PLUGIN_NAME = "LinceMultipleLovers";
        public const string PLUGIN_VERSION = "0.1.1";
    }
}
