using HarmonyLib;
using System;
using System.Collections.Generic;
using Sdk;
using TheEntity;

namespace LinceMultipleLovers.Patches
{
    /// <summary>
    /// 控制台命令补丁 - 拦截 DebugMgr.InputConsole，支持 LINCE 前缀命令
    /// 命令格式:
    ///   LINCE LOVER 角色ID  —— 通过Mod告白系统将指定角色添加为恋人
    ///   LINCE BREAK 角色ID  —— 与指定角色分手，变为熟人并清除历史恋人记录
    /// </summary>
    [HarmonyPatch(typeof(DebugMgr), nameof(DebugMgr.InputConsole))]
    public static class ConsoleCommandPatch
    {
        /// <summary>
        /// 拦截 InputConsole，优先处理 LINCE 命令
        /// </summary>
        [HarmonyPrefix]
        public static bool Prefix(string[] parms)
        {
            if (parms == null || parms.Length == 0)
                return true; // 执行原方法

            // 只拦截 LINCE 开头的命令（不区分大小写）
            if (!string.Equals(parms[0], "LINCE", StringComparison.OrdinalIgnoreCase))
                return true; // 不是我们的命令，执行原方法

            // 至少需要 LINCE + 子命令
            if (parms.Length < 2)
            {
                PrintHelp();
                return false; // 跳过原方法
            }

            string subCommand = parms[1].ToUpperInvariant();

            switch (subCommand)
            {
                case "LOVER":
                    HandleLoverCommand(parms);
                    break;
                case "BREAK":
                    HandleBreakCommand(parms);
                    break;
                case "LOVERID":
                    HandleLoverIdCommand(parms);
                    break;
                default:
                    PrintError($"未知子命令: {parms[1]}");
                    PrintHelp();
                    break;
            }

            return false; // 跳过原方法
        }

        /// <summary>
        /// LINCE LOVER 角色ID - 使用游戏原生 effect [20,2,角色ID,520] 将指定角色设为恋人
        /// 调用链: ChangeRelation(npcId, 520) → SetLover(npcId) → Mod的Harmony补丁自动处理多恋人
        /// </summary>
        private static void HandleLoverCommand(string[] parms)
        {
            if (parms.Length < 3)
            {
                PrintError("用法: LINCE LOVER <角色ID>");
                return;
            }

            if (!int.TryParse(parms[2], out int npcId) || npcId <= 0)
            {
                PrintError($"无效的角色ID: {parms[2]}");
                return;
            }

            // 检查存档是否已加载
            if (Singleton<RoleMgr>.Ins == null)
            {
                PrintError("存档未加载，请先进入游戏");
                return;
            }

            // 检查角色是否存在
            Role npc = Singleton<RoleMgr>.Ins.GetRole(npcId);
            if (npc == null)
            {
                PrintError($"找不到角色 ID: {npcId}");
                return;
            }

            // 检查是否已经是恋人
            if (LoverIdInterceptor.IsLover(npcId))
            {
                PrintError($"{npc.Name}({npcId}) 已经是恋人了");
                return;
            }

            // 使用游戏原生的 effect [20, 2, npcId, 520] 逻辑
            // ChangeRelation(npcId, 520) 内部会调用 SetLover(npcId)
            // SetLover 会处理: historyLoverIds、loveDate、fix、OpenFunc(20)、Toast、EventMgr.Send(1601)
            // Mod 的 Harmony 补丁 (SetLover_Prefix/Postfix) 会自动处理多恋人逻辑
            Singleton<RoleMgr>.Ins.GetRelationData(true).ChangeRelation(npcId, 520, null, false);

            Print($"成功将 {npc.Name}({npcId}) 设为恋人 (via effect [20,2,{npcId},520])");

            // 打印当前所有恋人
            var allLovers = LoverIdInterceptor.GetAllLoverIds();
            Print($"当前恋人列表: {FormatLoverList(allLovers)}");
        }

        /// <summary>
        /// LINCE BREAK 角色ID - 与指定角色分手，变为熟人并清除历史记录
        /// </summary>
        private static void HandleBreakCommand(string[] parms)
        {
            if (parms.Length < 3)
            {
                PrintError("用法: LINCE BREAK <角色ID>");
                return;
            }

            if (!int.TryParse(parms[2], out int npcId) || npcId <= 0)
            {
                PrintError($"无效的角色ID: {parms[2]}");
                return;
            }

            // 检查存档是否已加载
            if (Singleton<RoleMgr>.Ins == null)
            {
                PrintError("存档未加载，请先进入游戏");
                return;
            }

            // 检查角色是否存在
            Role npc = Singleton<RoleMgr>.Ins.GetRole(npcId);
            if (npc == null)
            {
                PrintError($"找不到角色 ID: {npcId}");
                return;
            }

            // 检查是否是恋人
            if (!LoverIdInterceptor.IsLover(npcId))
            {
                PrintError($"{npc.Name}({npcId}) 不是恋人");
                return;
            }

            var loveData = Singleton<RoleMgr>.Ins.GetLoveData();

            // 1. 从 historyLoverIds 中移除
            if (loveData.historyLoverIds != null)
            {
                loveData.historyLoverIds.Remove(npcId);
            }

            // 2. 从 MultipleLoversManager 中移除
            LinceMultipleLoversPlugin.LoversManager.RemoveLover(npcId);

            // 3. 如果当前 loverId 是该角色，切换到其他恋人或清空
            if (loveData.loverId == npcId)
            {
                var remainingLovers = LoverIdInterceptor.GetAllLoverIds();
                if (remainingLovers.Count > 0)
                {
                    // 切换到另一个恋人
                    loveData.loverId = remainingLovers[0];
                    Print($"当前恋人切换为: {Singleton<RoleMgr>.Ins.GetRole(remainingLovers[0])?.Name}({remainingLovers[0]})");
                }
                else
                {
                    // 没有恋人了，清空
                    loveData.loverId = 0;
                }
            }

            // 4. 将关系变为熟人(1)
            // 先从恋人关系字典中移除(520)
            var relationData = Singleton<RoleMgr>.Ins.GetRelationData(true);
            var relationDictField = typeof(RelationData).GetField("relationDict",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var relationDict = relationDictField?.GetValue(relationData) as Dictionary<int, List<int>>;

            if (relationDict != null && relationDict.ContainsKey(520))
            {
                relationDict[520].Remove(npcId);
            }

            // 设置关系为熟人
            npc.Relation = 1;

            // 添加到熟人关系字典
            var addRelationMethod = typeof(RelationData).GetMethod("AddRelation",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            addRelationMethod?.Invoke(relationData, new object[] { 1, npcId, false });

            // 刷新社交容量
            relationData.RefreshSocialCapacity();

            Print($"已与 {npc.Name}({npcId}) 分手，关系变为熟人，历史恋人记录已清除");

            // 打印当前所有恋人
            var allLovers = LoverIdInterceptor.GetAllLoverIds();
            if (allLovers.Count > 0)
            {
                Print($"当前恋人列表: {FormatLoverList(allLovers)}");
            }
            else
            {
                Print("当前没有恋人");
            }
        }

        /// <summary>
        /// LINCE LOVERID 角色ID - 将loverId直接设定为指定角色
        /// </summary>
        private static void HandleLoverIdCommand(string[] parms)
        {
            if (parms.Length < 3)
            {
                PrintError("用法: LINCE LOVERID <角色ID>");
                return;
            }

            if (!int.TryParse(parms[2], out int npcId) || npcId <= 0)
            {
                PrintError($"无效的角色ID: {parms[2]}");
                return;
            }

            if (Singleton<RoleMgr>.Ins == null)
            {
                PrintError("存档未加载，请先进入游戏");
                return;
            }

            Role npc = Singleton<RoleMgr>.Ins.GetRole(npcId);
            if (npc == null)
            {
                PrintError($"找不到角色 ID: {npcId}");
                return;
            }

            var loveData = Singleton<RoleMgr>.Ins.GetLoveData();
            int oldLoverId = loveData.loverId;

            // 直接设置loverId
            loveData.loverId = npcId;

            // 发送事件通知UI刷新
            EventMgr.Send(1601);

            string oldName = oldLoverId > 0 ? (Singleton<RoleMgr>.Ins.GetRole(oldLoverId)?.Name ?? "未知") : "无";
            Print($"已将活跃恋人从 {oldName}({oldLoverId}) 切换为 {npc.Name}({npcId})");

            var allLovers = LoverIdInterceptor.GetAllLoverIds();
            Print($"当前恋人列表: {FormatLoverList(allLovers)}");
        }

        /// <summary>
        /// 显示帮助信息
        /// </summary>
        private static void PrintHelp()
        {
            Print("=== LinceMultipleLovers 控制台命令 ===");
            Print("LINCE LOVER <角色ID>  - 将指定角色设为恋人");
            Print("LINCE BREAK <角色ID>  - 与指定角色分手(变为熟人，清除历史恋人记录)");
            Print("LINCE LOVERID <角色ID> - 将当前活跃恋人切换为指定角色");
        }

        /// <summary>
        /// 格式化恋人列表
        /// </summary>
        private static string FormatLoverList(List<int> loverIds)
        {
            if (loverIds == null || loverIds.Count == 0)
                return "无";

            var parts = new List<string>();
            foreach (int id in loverIds)
            {
                var role = Singleton<RoleMgr>.Ins.GetRole(id);
                parts.Add($"{role?.Name ?? "未知"}({id})");
            }
            return string.Join(", ", parts);
        }

        /// <summary>
        /// 在游戏控制台打印绿色信息
        /// </summary>
        private static void Print(string text)
        {
            EventMgr.Send<string>(1000004, text);
            LinceMultipleLoversPlugin.Log.LogInfo($"[Console] {text}");
        }

        /// <summary>
        /// 在游戏控制台打印红色错误信息
        /// </summary>
        private static void PrintError(string text)
        {
            EventMgr.Send<string>(1000003, text);
            LinceMultipleLoversPlugin.Log.LogWarning($"[Console] {text}");
        }
    }
}
