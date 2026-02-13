using HarmonyLib;
using Config;
using Sdk;
using TheEntity;

namespace LinceMultipleLovers.Patches
{
    /// <summary>
    /// RelationData补丁 - 修复告白成为恋人后社交容量重复计算问题
    /// 问题：ChangeRelation中520关系直接返回，没有执行从旧关系移除和刷新社交容量
    /// </summary>
    public static class RelationDataPatch
    {
        /// <summary>
        /// 修改ChangeRelation方法 - 让520关系也能执行完整的流程
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

            // 关键修复：执行完整的流程，不只是调用SetLover
            
            // 1. 从原关系列表中移除（这是原方法跳过的）
            if (__instance.GetAllRelationShip().ContainsKey(originalRelation))
            {
                __instance.GetAllRelationShip()[originalRelation].Remove(_roleId);
            }

            // 2. 设置新关系
            role.Relation = 520;
            
            // 3. 添加到恋人列表
            __instance.AddRelation(520, _roleId, false);
            
            // 4. 调用SetLover（添加多恋人支持）
            Singleton<RoleMgr>.Ins.GetLoveData().SetLover(_roleId);
            
            // 5. 刷新社交容量（这是原方法跳过的）
            __instance.RefreshSocialCapacity();
            
            // 6. 记录珍贵回忆
            Singleton<RecordMgr>.Ins.AddRecord(4, _tag, new float[]
            {
                (float)_roleId,
                (float)520
            });
            
            // 7. 显示提示
            ToastHelper.Toast<string>(116, new string[]
            {
                role.Name,
                Cfg.RelationCfgMap[520].name
            });

            if (ModConfig.DebugMode.Value)
            {
                LinceMultipleLoversPlugin.Log.LogInfo($"[ChangeRelation] {_roleId} ({role.Name}) 成为恋人，从关系 {originalRelation} 转移，社交容量已刷新");
            }

            __result = true;
            return false; // 跳过原方法
        }
    }
}
