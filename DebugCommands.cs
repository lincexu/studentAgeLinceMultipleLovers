using BepInEx;
using UnityEngine;
using Sdk;
using Input = UnityEngine.Input;
using KeyCode = UnityEngine.KeyCode;

namespace LinceMultipleLovers
{
    /// <summary>
    /// 调试命令 - 提供控制台命令用于调试
    /// </summary>
    public static class DebugCommands
    {
        /// <summary>
        /// 强制设置指定NPC为恋人
        /// 使用方法: 在游戏中按F1打开控制台，输入: LinceSetLover 105
        /// </summary>
        [UnityEngine.RuntimeInitializeOnLoadMethod]
        public static void Init()
        {
            // 注册键盘快捷键
            var go = new GameObject("LinceMultipleLovers_Debug");
            go.AddComponent<DebugCommandComponent>();
            UnityEngine.Object.DontDestroyOnLoad(go);
            
            LinceMultipleLoversPlugin.Log.LogInfo("调试命令已初始化 - 按F1查看帮助");
        }
        
        public static void ShowHelp()
        {
            LinceMultipleLoversPlugin.Log.LogInfo("=== 多恋人Mod调试命令 ===");
            LinceMultipleLoversPlugin.Log.LogInfo("F2 - 显示当前恋人状态");
            LinceMultipleLoversPlugin.Log.LogInfo("F3 - 强制设置当前交互NPC为恋人");
            LinceMultipleLoversPlugin.Log.LogInfo("F4 - 检查配置中的恋人ID");
            LinceMultipleLoversPlugin.Log.LogInfo("========================");
        }
        
        public static void ShowStatus()
        {
            LinceMultipleLoversPlugin.LoversManager.LogDebugInfo();
        }
        
        public static void ForceCurrentNpcAsLover()
        {
            // 尝试获取当前正在交互的NPC
            // 这需要通过反射或其他方式获取当前打开的MapRoleView
            var mapRoleViewType = typeof(View.Main.MapRoleView);
            var currentView = GetCurrentView();
            
            if (currentView != null)
            {
                var npcField = mapRoleViewType.GetField("npc", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var npc = npcField?.GetValue(currentView) as TheEntity.Role;
                
                if (npc != null)
                {
                    LinceMultipleLoversPlugin.Log.LogInfo($"强制设置当前NPC为恋人: {npc.Name} (ID: {npc.id})");
                    LinceMultipleLoversPlugin.LoversManager.ForceSetLover(npc.id);
                    LinceMultipleLoversPlugin.LoversManager.CheckNpcLoverStatus(npc.id);
                }
                else
                {
                    LinceMultipleLoversPlugin.Log.LogWarning("无法获取当前NPC");
                }
            }
            else
            {
                LinceMultipleLoversPlugin.Log.LogWarning("MapRoleView未打开");
            }
        }
        
        public static void CheckConfiguredLovers()
        {
            // 从存档加载
            LinceMultipleLoversPlugin.LoversManager.LoadConfiguredLovers();
            
            var ids = LinceMultipleLoversPlugin.LoversManager.GetAllLoverIds();
            LinceMultipleLoversPlugin.Log.LogInfo($"存档中的恋人ID: {string.Join(", ", ids)}");
            
            foreach (var id in ids)
            {
                LinceMultipleLoversPlugin.LoversManager.CheckNpcLoverStatus(id);
            }
        }
        
        public static void AddLoverById(int npcId)
        {
            LinceMultipleLoversPlugin.Log.LogInfo($"通过命令添加恋人: {npcId}");
            LinceMultipleLoversPlugin.LoversManager.AddLover(npcId);
            LinceMultipleLoversPlugin.LoversManager.CheckNpcLoverStatus(npcId);
        }
        
        private static object GetCurrentView()
        {
            // 通过UIMgr获取当前打开的MapRoleView
            var uiMgrType = typeof(Sdk.UIMgr);
            var getTopViewMethod = uiMgrType.GetMethod("GetTopView", new[] { typeof(Sdk.UILayerType), typeof(Sdk.ViewType[]) });
            if (getTopViewMethod != null)
            {
                var view = getTopViewMethod.Invoke(null, new object[] { Sdk.UILayerType.Normal, new Sdk.ViewType[0] });
                if (view is View.Main.MapRoleView mapRoleView)
                {
                    return mapRoleView;
                }
            }
            return null;
        }
    }
    
    public class DebugCommandComponent : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                DebugCommands.ShowHelp();
            }
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                DebugCommands.ShowStatus();
            }
            else if (Input.GetKeyDown(KeyCode.F3))
            {
                DebugCommands.ForceCurrentNpcAsLover();
            }
            else if (Input.GetKeyDown(KeyCode.F4))
            {
                DebugCommands.CheckConfiguredLovers();
            }
        }
    }
}
