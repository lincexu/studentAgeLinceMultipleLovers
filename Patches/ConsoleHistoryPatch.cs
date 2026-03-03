using HarmonyLib;
using System.Collections.Generic;
using Sdk;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LinceMultipleLovers.Patches
{
    /// <summary>
    /// 控制台命令历史浏览补丁 - 使用上/下方向键浏览已执行的历史命令
    /// 
    /// 功能：
    /// - 上方向键：浏览更早的命令
    /// - 下方向键：浏览更新的命令
    /// - 首次按上键保存当前输入，下键回到最新时恢复
    /// - 提交命令后自动记录到历史（连续重复不重复记录）
    /// - 最多保存100条历史记录
    /// 
    /// 补丁目标：
    /// - DebugView.Update() [Postfix] - 检测方向键输入
    /// - DebugView.AcceptInput() [Prefix] - 记录提交的命令到历史
    /// </summary>
    public static class ConsoleHistoryPatch
    {
        /// <summary>命令历史列表（索引0=最早，末尾=最新）</summary>
        private static readonly List<string> History = new List<string>();

        /// <summary>最大历史记录条数</summary>
        private const int MaxHistorySize = 100;

        /// <summary>
        /// 当前浏览位置索引。
        /// 范围: [0, History.Count-1] = 历史命令, History.Count = 当前输入（虚拟位置）
        /// </summary>
        private static int historyIndex = 0;

        /// <summary>开始浏览前保存的当前输入文本</summary>
        private static string savedCurrentInput = "";

        /// <summary>是否正在浏览历史（首次按上键后为true，提交命令后重置为false）</summary>
        private static bool isBrowsing = false;

        /// <summary>
        /// DebugView.Update() 的 Postfix
        /// 当控制台显示时，检测上/下方向键并导航历史记录
        /// </summary>
        public static void Update_Postfix(DebugView __instance)
        {
            if (!DebugMgr.Ins.enableDebug || !DebugMgr.Ins.isConsoleShowing)
                return;

            if (History.Count == 0)
                return;

            bool upPressed = Control.GetKeyDown(Key.UpArrow);
            bool downPressed = Control.GetKeyDown(Key.DownArrow);

            if (!upPressed && !downPressed)
                return;

            if (upPressed)
            {
                NavigateHistory(__instance, -1);
            }
            else if (downPressed)
            {
                NavigateHistory(__instance, 1);
            }
        }

        /// <summary>
        /// 导航历史记录
        /// </summary>
        /// <param name="view">DebugView 实例</param>
        /// <param name="direction">-1=向上(更早), +1=向下(更新)</param>
        private static void NavigateHistory(DebugView view, int direction)
        {
            InputField input = view.input_console;

            // 首次开始浏览时，保存当前输入内容
            if (!isBrowsing)
            {
                savedCurrentInput = input.text ?? "";
                isBrowsing = true;
                historyIndex = History.Count; // 虚拟位置：当前输入
            }

            int newIndex = historyIndex + direction;

            // 边界处理
            if (newIndex < 0)
            {
                // 已到最早的记录，停留在第一条
                newIndex = 0;
            }

            if (newIndex > History.Count)
            {
                // 不能超过当前输入位置
                newIndex = History.Count;
            }

            // 索引没有变化，不做处理
            if (newIndex == historyIndex)
                return;

            historyIndex = newIndex;

            if (historyIndex >= History.Count)
            {
                // 回到当前输入位置，恢复保存的文本
                SetInputText(input, savedCurrentInput);
            }
            else
            {
                // 显示历史命令
                SetInputText(input, History[historyIndex]);
            }
        }

        /// <summary>
        /// 设置输入框文本并将光标移到末尾
        /// </summary>
        private static void SetInputText(InputField input, string text)
        {
            input.text = text;
            // 将光标移到文本末尾
            input.caretPosition = text.Length;
            input.selectionAnchorPosition = text.Length;
            input.selectionFocusPosition = text.Length;
            // 确保输入框保持激活
            input.ActivateInputField();
        }

        /// <summary>
        /// DebugView.AcceptInput() 的 Prefix
        /// 在命令被处理和清空前，将输入文本记录到历史列表
        /// </summary>
        public static void AcceptInput_Prefix(DebugView __instance)
        {
            if (!DebugMgr.Ins.isConsoleShowing)
                return;

            string text = __instance.input_console.text;
            if (string.IsNullOrWhiteSpace(text))
                return;

            string trimmed = text.Trim();

            // 连续重复命令不重复记录
            if (History.Count == 0 || History[History.Count - 1] != trimmed)
            {
                History.Add(trimmed);

                // 超过上限时移除最早的记录
                if (History.Count > MaxHistorySize)
                {
                    History.RemoveAt(0);
                }
            }

            // 重置浏览状态
            ResetBrowsingState();
        }

        /// <summary>
        /// 重置浏览状态（提交命令后调用）
        /// </summary>
        private static void ResetBrowsingState()
        {
            historyIndex = 0;
            isBrowsing = false;
            savedCurrentInput = "";
        }
    }
}
