using HarmonyLib;
using Increase;
using Sdk;
using System.Collections.Generic;

namespace LinceMultipleLovers.Patches
{
    /// <summary>
    /// IncreaserOther补丁 - 多恋人适配
    /// 
    /// 原版中以下otherAttrId直接使用loverId，需要改为遍历所有恋人：
    /// 
    /// [3003] OnGetSelfValue: 检查恋人最高属性是否匹配id → 改为检查所有恋人
    /// [3910] OnGetSelfValue: 单身返回value，有恋人返回0 → 改为检查所有恋人
    /// [3913] OnGetSelfValue: 单身时按记忆数*value，有恋人返回0 → 改为检查所有恋人
    /// [9]    OnRun: 有恋人用value，无恋人用value2 → 改为检查所有恋人
    /// </summary>
    public static class IncreaserOtherPatch
    {
        /// <summary>
        /// 拦截OnGetSelfValue，对loverId相关的otherAttrId进行多恋人适配
        /// </summary>
        [HarmonyPatch(typeof(IncreaserOther), "OnGetSelfValue")]
        [HarmonyPrefix]
        public static bool OnGetSelfValue_Prefix(IncreaserOther __instance, ref float __result)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return true;

            switch (__instance.otherAttrId)
            {
                case 3003:
                    __result = Calculate3003(__instance);
                    return false;

                case 3910:
                    __result = Calculate3910(__instance);
                    return false;

                case 3913:
                    __result = Calculate3913(__instance);
                    return false;

                default:
                    return true; // 其他情况走原版逻辑
            }
        }

        /// <summary>
        /// [3003] 原版：检查loverId的最高属性==id时返回value
        /// 多恋人：遍历所有恋人，任一恋人的最高属性匹配即返回value
        /// </summary>
        private static float Calculate3003(IncreaserOther inc)
        {
            var loverIds = LoverIdInterceptor.GetAllLoverIds();
            if (loverIds.Count == 0)
                return 0f;

            foreach (int loverId in loverIds)
            {
                if (loverId <= 0) continue;
                var loverRole = Singleton<RoleMgr>.Ins.GetRole(loverId);
                if (loverRole != null && loverRole.GetMaxAttr(false).Item1 == inc.id)
                {
                    if (ModConfig.DebugMode.Value)
                    {
                        LinceMultipleLoversPlugin.Log.LogInfo(
                            $"[IncreaserOther 3003] 恋人{loverId}最高属性匹配id={inc.id}，返回value={inc.value}");
                    }
                    return inc.value;
                }
            }

            return 0f;
        }

        /// <summary>
        /// [3910] 原版：loverId<=0（单身）时返回value，否则返回0
        /// 多恋人：任何恋人存在时返回0，无恋人时返回value
        /// </summary>
        private static float Calculate3910(IncreaserOther inc)
        {
            var loverIds = LoverIdInterceptor.GetAllLoverIds();
            if (loverIds.Count == 0)
                return inc.value;

            return 0f;
        }

        /// <summary>
        /// [3913] 原版：loverId<=0（单身）时返回记忆数*value，否则返回0
        /// 多恋人：任何恋人存在时返回0，无恋人时返回记忆数*value
        /// </summary>
        private static float Calculate3913(IncreaserOther inc)
        {
            var loverIds = LoverIdInterceptor.GetAllLoverIds();
            if (loverIds.Count == 0)
            {
                return (float)Singleton<RoleMgr>.Ins.GetRenshengguanData().GetMemoryCnt(inc.id) * inc.value;
            }

            return 0f;
        }

        /// <summary>
        /// 拦截OnRun，对otherAttrId=9的loverId判断进行多恋人适配
        /// [9] 原版：有恋人用value，无恋人用value2
        /// 多恋人：检查所有恋人是否存在
        /// </summary>
        [HarmonyPatch(typeof(IncreaserOther), "OnRun")]
        [HarmonyPrefix]
        public static bool OnRun_Prefix(IncreaserOther __instance, float _rate)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return true;

            // 只拦截otherAttrId=9的情况
            if (__instance.otherAttrId != 9)
                return true;

            if (_rate <= 0f)
                return false; // 与原版一致：rate<=0时不执行

            var role = Singleton<RoleMgr>.Ins.GetRole();
            // ids字段是IncreaserOther的public字段
            if (__instance.ids == null || __instance.ids.Count == 0)
                return false;

            int attrId = __instance.ids[UnityEngine.Random.Range(0, __instance.ids.Count)];

            // 多恋人适配：检查所有恋人而非仅loverId
            var loverIds = LoverIdInterceptor.GetAllLoverIds();
            bool hasLover = loverIds.Count > 0;

            if (hasLover)
            {
                role.UpdateAttr2(attrId, _rate * __instance.value, 0f, null);
            }
            else
            {
                role.UpdateAttr2(attrId, _rate * __instance.value2, 0f, null);
            }

            return false; // 跳过原方法
        }
    }
}
