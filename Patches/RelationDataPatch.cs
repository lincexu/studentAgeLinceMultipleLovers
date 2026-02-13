using HarmonyLib;
using Config;
using Sdk;
using TheEntity;

namespace LinceMultipleLovers.Patches
{
    /// <summary>
    /// RelationData补丁 - 修复告白成为恋人后社交容量重复计算问题
    /// 问题：ChangeRelation中520关系直接返回，没有从旧关系移除
    /// </summary>
    public static class RelationDataPatch
    {
        /// <summary>
        /// 修改ChangeRelation方法 - 处理恋人关系时也执行从旧关系移除
        /// </summary>
        [HarmonyPatch(typeof(RelationData), nameof(RelationData.ChangeRelation))]
        [HarmonyPrefix]
        public static bool ChangeRelation_Prefix(ref bool __result, RelationData __instance, int _roleId, int _relationId, string _tag = null, bool _focusBefore = false)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return true; // 执行原方法

            // 只处理恋人关系（520）的情况
            if (_relationId != 520)
                return true; // 执行原方法

            Role role = Singleton<RoleMgr>.Ins.GetRole(_roleId);
            if (role == null)
            {
                __result = false;
                return false;
            }

            // 获取原关系
            int originalRelation = role.Relation;
            
            // 如果原关系就是恋人，不需要处理
            if (originalRelation == 520)
            {
                __result = false;
                return false;
            }

            // 关键修复：从原关系列表中移除
            if (__instance.GetAllRelationShip().ContainsKey(originalRelation))
            {
                __instance.GetAllRelationShip()[originalRelation].Remove(_roleId);
                
                if (ModConfig.DebugMode.Value)
                {
                    LinceMultipleLoversPlugin.Log.LogInfo($"[ChangeRelation] 从关系 {originalRelation} 移除 {_roleId} ({role.Name})");
                }
            }

            // 设置新关系
            role.Relation = 520;
            
            // 添加到恋人列表
            __instance.AddRelation(520, _roleId, false);
            
            // 调用SetLover
            Singleton<RoleMgr>.Ins.GetLoveData().SetLover(_roleId);
            
            // 刷新社交容量
            __instance.RefreshSocialCapacity();
            
            // 记录珍贵回忆
            Singleton<RecordMgr>.Ins.AddRecord(4, _tag, new float[]
            {
                (float)_roleId,
                (float)520
            });
            
            // 显示提示
            ToastHelper.Toast<string>(116, new string[]
            {
                role.Name,
                Cfg.RelationCfgMap[520].name
            });

            if (ModConfig.DebugMode.Value)
            {
                LinceMultipleLoversPlugin.Log.LogInfo($"[ChangeRelation] {_roleId} ({role.Name}) 成为恋人，从关系 {originalRelation} 转移");
            }

            __result = true;
            return false; // 跳过原方法
        }
    }
}
