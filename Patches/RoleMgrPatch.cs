using HarmonyLib;
using Sdk;
using System;
using System.Collections.Generic;
using TheEntity;

namespace LinceMultipleLovers.Patches
{
    /// <summary>
    /// RoleMgr.GetRateType补丁 - 多恋人适配
    /// 
    /// 原版case 10001（RateTypeTrait，好友特性倍率）中：
    ///   if (role2.id == this.GetLoveData().loverId)
    ///       num3 = role.IncCtrl.GetValue(RoleIncType.OtherAttrMul, 3002);
    /// 
    /// 亲密度加成(3002)只对当前loverId生效，其他恋人的好友特性不会获得该加成。
    /// 
    /// 补丁改为：只要role2是任一恋人，即可获得3002亲密度加成。
    /// 这使得"陪伴人生观第5级 - 每1亲密度增加恋人1%特性效果"对所有恋人生效。
    /// </summary>
    public static class RoleMgrPatch
    {
        /// <summary>
        /// 拦截GetRateType，对case 10001进行多恋人适配
        /// 
        /// 原版倍率公式: 特性倍率 = 1 + 好感度加成(213) + 好感×好感倍率(221) + 亲密度加成(3002)
        /// 原版问题: 3002只在role2.id == loverId时才加入计算
        /// 修复后: 3002在role2.id为任一恋人时都加入计算
        /// </summary>
        public static bool GetRateType_Prefix(
            RoleMgr __instance,
            ref ValueTuple<float, float> __result,
            int _rateType, int _fromRoleId, int _toRoleId, float _v)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return true;

            // 只拦截case 10001 (RateTypeTrait - 好友特性倍率)
            if (_rateType != 10001)
                return true;

            // 复现原版case 10001逻辑，将loverId检查改为遍历所有恋人
            Role role = Singleton<RoleMgr>.Ins.GetRole();
            int traitNeedRelation = Singleton<RoleMgr>.Ins.GetTraitNeedRelation();
            Role role2 = Singleton<RoleMgr>.Ins.GetRole(_fromRoleId);

            float item;
            float num;

            if (role2 != null && role2.Relation >= traitNeedRelation)
            {
                float value = role.IncCtrl.GetValue(RoleIncType.OtherAttrMul, 213);
                float num2 = role2.Favor * role.IncCtrl.GetValue(RoleIncType.OtherAttrMul, 221);
                float num3 = 0f;

                // 多恋人适配：检查role2是否为任一恋人
                List<int> allLoverIds = LoverIdInterceptor.GetAllLoverIds();
                if (allLoverIds.Contains(role2.id))
                {
                    num3 = role.IncCtrl.GetValue(RoleIncType.OtherAttrMul, 3002);
                }

                num = 1f + value + num2 + num3;
            }
            else
            {
                num = 0f;
            }

            item = _v * num;
            __result = new ValueTuple<float, float>(item, num);
            return false; // 跳过原版
        }
    }
}
