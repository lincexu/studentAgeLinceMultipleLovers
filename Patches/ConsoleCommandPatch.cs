using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using Effect;
using Sdk;
using TheEntity;
using UnityEngine.UI;

namespace LinceMultipleLovers.Patches
{
    /// <summary>
    /// 控制台命令补丁 - 拦截 DebugMgr.InputConsole，支持 LINCE 前缀命令
    /// 使用命令注册表模式，新增命令只需在 RegisterCommands() 中注册即可
    /// </summary>
    [HarmonyPatch(typeof(DebugMgr), nameof(DebugMgr.InputConsole))]
    public static class ConsoleCommandPatch
    {
        /// <summary>
        /// 命令注册项
        /// </summary>
        private class CommandEntry
        {
            public string Name { get; set; }
            public string Usage { get; set; }
            public string Description { get; set; }
            public Action<string[]> Handler { get; set; }
        }

        /// <summary>
        /// 命令注册表（大写命令名 → 命令项）
        /// </summary>
        private static readonly Dictionary<string, CommandEntry> Commands = new Dictionary<string, CommandEntry>();

        /// <summary>
        /// 保持插入顺序的命令列表（用于HELP显示）
        /// </summary>
        private static readonly List<CommandEntry> CommandList = new List<CommandEntry>();

        /// <summary>
        /// 静态构造函数 - 注册所有命令
        /// </summary>
        static ConsoleCommandPatch()
        {
            RegisterCommands();
        }

        /// <summary>
        /// 注册所有命令。新增命令只需在此处添加 Register() 调用
        /// </summary>
        private static void RegisterCommands()
        {
            Register("HELP",      "LINCE HELP",                "显示所有可用命令",            HandleHelpCommand);
            Register("CLEAR",     "LINCE CLEAR",               "清空控制台记录",              HandleClearCommand);
            Register("LOVER",     "LINCE LOVER <角色ID>",       "将指定角色设为恋人",          HandleLoverCommand);
            Register("BREAK",     "LINCE BREAK <角色ID>",       "与指定角色分手(变为熟人)",     HandleBreakCommand);
            Register("LOVERID",   "LINCE LOVERID <角色ID|LOCK|UNLOCK>", "切换/锁定/解锁当前活跃恋人", HandleLoverIdCommand);
            Register("EFFECT",    "LINCE EFFECT <type,sub,...>", "手动执行effect指令 (如: 60,2,3001)", HandleEffectCommand);
            Register("ADDFOLLOW", "LINCE ADDFOLLOW <数量>",     "增加关注上限 (等效effect [20,92,X])", HandleAddFollowCommand);
            Register("NPC",       "LINCE NPC [ID|NAME] [参数]", "查询角色信息",                HandleNpcCommand);
            Register("RESOCIAL",  "LINCE RESOCIAL",            "刷新所有角色社交事件(重新检测可触发事件)", HandleResocialCommand);
        }

        /// <summary>
        /// 注册一个命令
        /// </summary>
        private static void Register(string name, string usage, string description, Action<string[]> handler)
        {
            var entry = new CommandEntry
            {
                Name = name.ToUpperInvariant(),
                Usage = usage,
                Description = description,
                Handler = handler
            };
            Commands[entry.Name] = entry;
            CommandList.Add(entry);
        }

        /// <summary>
        /// 拦截 InputConsole，优先处理 LINCE 命令
        /// </summary>
        [HarmonyPrefix]
        public static bool Prefix(string[] parms)
        {
            if (parms == null || parms.Length == 0)
                return true;

            if (!string.Equals(parms[0], "LINCE", StringComparison.OrdinalIgnoreCase))
                return true;

            if (parms.Length < 2)
            {
                HandleHelpCommand(parms);
                return false;
            }

            string subCommand = parms[1].ToUpperInvariant();

            if (Commands.TryGetValue(subCommand, out var cmd))
            {
                cmd.Handler(parms);
            }
            else
            {
                PrintError($"未知子命令: {parms[1]}");
                HandleHelpCommand(parms);
            }

            return false;
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
        /// LINCE LOVERID <角色ID|LOCK|UNLOCK>
        /// - LINCE LOVERID 角色ID   → 将loverId切换为指定角色
        /// - LINCE LOVERID LOCK     → 锁定当前loverId，阻止自动切换
        /// - LINCE LOVERID UNLOCK   → 解除锁定
        /// </summary>
        private static void HandleLoverIdCommand(string[] parms)
        {
            if (parms.Length < 3)
            {
                // 无参数时显示当前状态
                if (Singleton<RoleMgr>.Ins == null)
                {
                    PrintError("存档未加载，请先进入游戏");
                    return;
                }
                var ld = Singleton<RoleMgr>.Ins.GetLoveData();
                string curName = ld.loverId > 0 ? (Singleton<RoleMgr>.Ins.GetRole(ld.loverId)?.Name ?? "未知") : "无";
                Print($"当前loverId: {curName}({ld.loverId}), 锁定状态: {(LoverIdInterceptor.LoverIdLocked ? "已锁定" : "未锁定")}");
                Print("用法: LINCE LOVERID <角色ID|LOCK|UNLOCK>");
                return;
            }

            string arg = parms[2].ToUpperInvariant();

            // LOCK 子命令
            if (arg == "LOCK")
            {
                if (Singleton<RoleMgr>.Ins == null)
                {
                    PrintError("存档未加载，请先进入游戏");
                    return;
                }
                LoverIdInterceptor.LoverIdLocked = true;
                var ld = Singleton<RoleMgr>.Ins.GetLoveData();
                string curName = ld.loverId > 0 ? (Singleton<RoleMgr>.Ins.GetRole(ld.loverId)?.Name ?? "未知") : "无";
                Print($"已锁定当前loverId: {curName}({ld.loverId})，恋人ID不再自动切换");
                return;
            }

            // UNLOCK 子命令
            if (arg == "UNLOCK")
            {
                LoverIdInterceptor.LoverIdLocked = false;
                Print("已解锁loverId，恋人ID将恢复自动切换");
                return;
            }

            // 数字参数 → 切换loverId
            if (!int.TryParse(parms[2], out int npcId) || npcId <= 0)
            {
                PrintError($"无效的参数: {parms[2]}  (用法: LINCE LOVERID <角色ID|LOCK|UNLOCK>)");
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
            if (LoverIdInterceptor.LoverIdLocked)
            {
                Print("注意: loverId当前已锁定，不会自动切换");
            }
        }

        /// <summary>
        /// LINCE EFFECT type,sub,... - 手动执行effect指令
        /// 参数以逗号分隔，如: LINCE EFFECT 60,2,3001
        /// 等效于游戏内控制台的 1701 命令
        /// </summary>
        private static void HandleEffectCommand(string[] parms)
        {
            if (parms.Length < 3)
            {
                PrintError("用法: LINCE EFFECT <type,sub,...>  例: LINCE EFFECT 60,2,3001");
                return;
            }

            if (Singleton<RoleMgr>.Ins == null)
            {
                PrintError("存档未加载，请先进入游戏");
                return;
            }

            // 将第3个参数按逗号拆分为浮点数列表
            string effectStr = parms[2];
            string[] parts = effectStr.Split(',');
            var effectList = new List<float>();

            foreach (string part in parts)
            {
                string trimmed = part.Trim();
                if (!float.TryParse(trimmed, out float val))
                {
                    PrintError($"无效的参数值: {trimmed}  (所有参数必须为数字，以逗号分隔)");
                    return;
                }
                effectList.Add(val);
            }

            if (effectList.Count == 0)
            {
                PrintError("effect参数不能为空");
                return;
            }

            // 使用游戏原生的effect执行逻辑（与ConsoleCodeMgr中1701命令相同）
            try
            {
                var effector = CommonEvtMgr.GenEffector(effectList, null, 0, 0);
                if (effector != null)
                {
                    effector.SetTag("LINCE_EFFECT");
                    effector.SetIsInc(true);
                    effector.Run(1f, false);

                    // 输出生成的increaser uid
                    var uids = effector.GetBaseIncreaserUids();
                    string uidStr = uids != null && uids.Count > 0
                        ? string.Join(", ", uids)
                        : "无";
                    Print($"Effect [{effectStr}] 执行成功, UIDs: {uidStr}");
                }
                else
                {
                    PrintError($"Effect [{effectStr}] 创建失败: GenEffector返回null");
                }
            }
            catch (Exception ex)
            {
                PrintError($"Effect [{effectStr}] 执行异常: {ex.Message}");
            }
        }

        /// <summary>
        /// LINCE ADDFOLLOW 数字 - 增加关注上限
        /// 等效于 effect [20, 92, X]，即 RelationData.enableFocusCnt += X
        /// </summary>
        private static void HandleAddFollowCommand(string[] parms)
        {
            if (parms.Length < 3)
            {
                PrintError("用法: LINCE ADDFOLLOW <数量>");
                return;
            }

            if (!int.TryParse(parms[2], out int amount) || amount == 0)
            {
                PrintError($"无效的数量: {parms[2]}");
                return;
            }

            if (Singleton<RoleMgr>.Ins == null)
            {
                PrintError("存档未加载，请先进入游戏");
                return;
            }

            var relationData = Singleton<RoleMgr>.Ins.GetRelationData(true);
            int oldCnt = relationData.enableFocusCnt;
            relationData.enableFocusCnt += amount;
            int newCnt = relationData.enableFocusCnt;

            Print($"关注上限: {oldCnt} → {newCnt} (变化{(amount > 0 ? "+" : "")}{amount})");
        }

        /// <summary>
        /// LINCE NPC - 查询角色信息
        /// LINCE NPC       - 显示所有角色ID和姓名
        /// LINCE NPC ID X  - 显示指定ID的角色名称
        /// LINCE NPC NAME X - 显示指定名称的角色ID
        /// </summary>
        private static void HandleNpcCommand(string[] parms)
        {
            if (Cfg.PersonCfgMap == null || Cfg.PersonCfgMap.Count == 0)
            {
                PrintError("角色配置未加载");
                return;
            }

            // LINCE NPC - 显示所有角色
            if (parms.Length <= 2)
            {
                Print($"=== 所有角色 (共{Cfg.PersonCfgMap.Count}个) ===");
                var sorted = Cfg.PersonCfgMap.OrderBy(kv => kv.Key);
                foreach (var kv in sorted)
                {
                    Print($"  ID: {kv.Key}  名称: {kv.Value.name}");
                }
                return;
            }

            string subCmd = parms[2].ToUpperInvariant();

            // LINCE NPC ID <数字>
            if (subCmd == "ID")
            {
                if (parms.Length < 4)
                {
                    PrintError("用法: LINCE NPC ID <角色ID>");
                    return;
                }
                if (!int.TryParse(parms[3], out int npcId))
                {
                    PrintError($"无效的ID: {parms[3]}");
                    return;
                }
                if (Cfg.PersonCfgMap.TryGetValue(npcId, out var cfg))
                {
                    Print($"ID: {npcId}  名称: {cfg.name}");
                }
                else
                {
                    PrintError($"找不到ID为 {npcId} 的角色");
                }
                return;
            }

            // LINCE NPC NAME <名字>
            if (subCmd == "NAME")
            {
                if (parms.Length < 4)
                {
                    PrintError("用法: LINCE NPC NAME <角色名字>");
                    return;
                }
                string searchName = parms[3];
                var matches = Cfg.PersonCfgMap
                    .Where(kv => kv.Value.name != null && kv.Value.name.Contains(searchName))
                    .OrderBy(kv => kv.Key)
                    .ToList();

                if (matches.Count == 0)
                {
                    PrintError($"找不到名称包含 \"{searchName}\" 的角色");
                }
                else
                {
                    Print($"=== 搜索结果 (共{matches.Count}个) ===");
                    foreach (var kv in matches)
                    {
                        Print($"  ID: {kv.Key}  名称: {kv.Value.name}");
                    }
                }
                return;
            }

            PrintError($"未知NPC子命令: {parms[2]}");
            Print("用法: LINCE NPC / LINCE NPC ID <ID> / LINCE NPC NAME <名字>");
        }

        /// <summary>
        /// LINCE HELP - 显示所有已注册的命令
        /// </summary>
        private static void HandleHelpCommand(string[] parms)
        {
            Print("=== LinceMultipleLovers 控制台命令 ===");
            foreach (var cmd in CommandList)
            {
                Print($"  {cmd.Usage}  - {cmd.Description}");
            }
        }

        /// <summary>
        /// LINCE CLEAR - 清空控制台记录
        /// </summary>
        private static void HandleClearCommand(string[] parms)
        {
            var viewField = typeof(DebugMgr).GetField("view",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var view = viewField?.GetValue(DebugMgr.Ins) as DebugView;

            if (view != null && view.txt_console_visable != null)
            {
                view.txt_console_visable.text = "";
                LinceMultipleLoversPlugin.Log.LogInfo("[Console] 控制台已清空");
            }
            else
            {
                PrintError("无法访问控制台视图");
            }
        }

        /// <summary>
        /// LINCE RESOCIAL - 刷新所有角色的社交事件和恋爱社交事件
        /// 重新调用 CheckSocialEvt() 和 CheckLoveSocialEvt()，等同于新回合开始时的检测
        /// </summary>
        private static void HandleResocialCommand(string[] parms)
        {
            if (Singleton<RoleMgr>.Ins == null)
            {
                PrintError("存档未加载，请先进入游戏");
                return;
            }

            var roleDict = Singleton<RoleMgr>.Ins.GetRoleDict();
            if (roleDict == null || roleDict.Count == 0)
            {
                PrintError("角色列表为空");
                return;
            }

            int socialCount = 0;
            int loveCount = 0;

            foreach (var kv in roleDict)
            {
                Role role = kv.Value;
                if (role == null || role.isLeave || role.Relation < 1)
                    continue;

                role.CheckSocialEvt();
                role.CheckLoveSocialEvt();

                if (role.socialEvtId > 0)
                    socialCount++;
                if (role.loveSocialEvtId > 0)
                    loveCount++;
            }

            // 刷新社交红点
            Singleton<RoleMgr>.Ins.GetRelationData(true).CheckSocialEvtRedpoint();

            Print($"社交事件已刷新: {socialCount}个社交事件, {loveCount}个恋爱社交事件");
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
