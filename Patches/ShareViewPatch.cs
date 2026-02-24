using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Sdk;
using View.Main;

namespace LinceMultipleLovers.Patches
{
    /// <summary>
    /// ShareView补丁 - 修复通关结算页面只显示一个恋人和至交显示问题
    /// 支持显示所有恋人和正确的朋友列表
    /// </summary>
    public static class ShareViewPatch
    {
        /// <summary>
        /// 修改RefreshFriend方法 - 支持显示多个恋人和正确的朋友列表
        /// </summary>
        [HarmonyPatch(typeof(ShareView), "RefreshFriend")]
        [HarmonyPrefix]
        public static bool RefreshFriend_Prefix(ShareView __instance)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return true; // 执行原方法

            // 获取所有恋人ID
            var loverIds = LoverIdInterceptor.GetAllLoverIds();
            
            // 使用反射访问私有字段
            var loverIdField = typeof(ShareView).GetField("loverId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var loverCellField = typeof(ShareView).GetField("loverCell", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var groupLoverField = typeof(ShareView).GetField("group_lover", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var allFriendsField = typeof(ShareView).GetField("allFriends", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var friendsField = typeof(ShareView).GetField("friends", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var groupFriendField = typeof(ShareView).GetField("group_friend", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var itemgroupFriendField = typeof(ShareView).GetField("itemgroup_friend", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // ========== 处理恋人显示 ==========
            // 注意：原版UI中，group_lover只显示一个恋人（主恋人）
            // group_friend显示朋友列表
            // 为了显示所有恋人，我们需要将其他恋人添加到group_friend中
            
            if (loverIds.Count > 0)
            {
                // 设置主恋人（第一个）到单独的恋人显示位置
                int primaryLoverId = loverIds[0];
                loverIdField?.SetValue(__instance, primaryLoverId);
                
                var loverCell = loverCellField?.GetValue(__instance);
                if (loverCell != null)
                {
                    // 设置主恋人数据
                    var dataField = loverCell.GetType().GetField("data", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    dataField?.SetValue(loverCell, primaryLoverId);
                    
                    // 调用OnRenderFriend渲染主恋人
                    var onRenderFriendMethod = typeof(ShareView).GetMethod("OnRenderFriend", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    onRenderFriendMethod?.Invoke(__instance, new object[] { loverCell });
                }
                
                // 显示恋人组
                var groupLover = groupLoverField?.GetValue(__instance);
                var groupLoverGameObject = groupLover?.GetType().GetProperty("gameObject")?.GetValue(groupLover);
                groupLoverGameObject?.GetType().GetMethod("SetActive")?.Invoke(groupLoverGameObject, new object[] { true });
                
                if (ModConfig.DebugMode.Value)
                {
                    LinceMultipleLoversPlugin.Log.LogInfo($"[ShareView] 主恋人: {primaryLoverId}，总恋人数量: {loverIds.Count}");
                }
            }
            else
            {
                // 没有恋人，隐藏恋人组
                var groupLover = groupLoverField?.GetValue(__instance);
                var groupLoverGameObject = groupLover?.GetType().GetProperty("gameObject")?.GetValue(groupLover);
                groupLoverGameObject?.GetType().GetMethod("SetActive")?.Invoke(groupLoverGameObject, new object[] { false });
            }
            
            // ========== 处理朋友/至交显示 ==========
            // 获取所有朋友关系（4=挚友, 5=挚友, 6=至交）
            var allFriends = new List<int>();
            
            // 获取关系4（挚友）
            var relation4 = Singleton<RoleMgr>.Ins.GetRelationData(true).GetRelationship(4);
            if (relation4 != null && relation4.Count > 0)
            {
                allFriends.AddRange(relation4);
            }
            
            // 获取关系5（挚友）
            var relation5 = Singleton<RoleMgr>.Ins.GetRelationData(true).GetRelationship(5);
            if (relation5 != null && relation5.Count > 0)
            {
                foreach (var id in relation5)
                {
                    if (!allFriends.Contains(id))
                        allFriends.Add(id);
                }
            }
            
            // 获取关系6（至交）
            var relation6 = Singleton<RoleMgr>.Ins.GetRelationData(true).GetRelationship(6);
            if (relation6 != null && relation6.Count > 0)
            {
                foreach (var id in relation6)
                {
                    if (!allFriends.Contains(id))
                        allFriends.Add(id);
                }
            }
            
            // 从朋友列表中移除所有恋人（避免重复）
            foreach (int loverId in loverIds)
            {
                allFriends.Remove(loverId);
            }
            
            // 保存完整的朋友列表到实例
            allFriendsField?.SetValue(__instance, allFriends);
            
            // 构建显示列表：如果有多个恋人，将其他恋人添加到朋友组显示
            var friendsDisplayIds = new List<int>();
            
            // 如果有多个恋人，将除第一个外的其他恋人添加到朋友组
            if (loverIds.Count > 1)
            {
                for (int i = 1; i < loverIds.Count; i++)
                {
                    friendsDisplayIds.Add(loverIds[i]);
                }
            }
            
            // 添加朋友（最多2个，保持UI美观）
            if (allFriends.Count > 0)
            {
                int remainingSlots = 3 - friendsDisplayIds.Count; // 最多显示3个
                if (remainingSlots > 0)
                {
                    var friendsToAdd = allFriends.GetRange(0, UnityEngine.Mathf.Min(allFriends.Count, remainingSlots));
                    friendsDisplayIds.AddRange(friendsToAdd);
                }
            }
            
            // 保存friends字段（用于原版兼容性）
            friendsField?.SetValue(__instance, friendsDisplayIds);
            
            if (ModConfig.DebugMode.Value)
            {
                LinceMultipleLoversPlugin.Log.LogInfo($"[ShareView] 朋友组显示: {string.Join(", ", friendsDisplayIds)}");
            }
            
            // 设置朋友组显示
            var groupFriend = groupFriendField?.GetValue(__instance);
            
            if (friendsDisplayIds.Count == 0)
            {
                // 没有要显示的，隐藏朋友组
                var groupFriendGameObject = groupFriend?.GetType().GetProperty("gameObject")?.GetValue(groupFriend);
                groupFriendGameObject?.GetType().GetMethod("SetActive")?.Invoke(groupFriendGameObject, new object[] { false });
            }
            else
            {
                // 显示朋友组
                var groupFriendGameObject = groupFriend?.GetType().GetProperty("gameObject")?.GetValue(groupFriend);
                groupFriendGameObject?.GetType().GetMethod("SetActive")?.Invoke(groupFriendGameObject, new object[] { true });
                
                // 设置数据 - 使用泛型方法调用
                var itemgroupFriend = itemgroupFriendField?.GetValue(__instance);
                if (itemgroupFriend != null)
                {
                    // 获取SetDatas<T>方法并调用
                    var setDatasMethod = itemgroupFriend.GetType().GetMethods()
                        .FirstOrDefault(m => m.Name == "SetDatas" && m.IsGenericMethod);
                    
                    if (setDatasMethod != null)
                    {
                        var genericMethod = setDatasMethod.MakeGenericMethod(typeof(int));
                        genericMethod.Invoke(itemgroupFriend, new object[] { friendsDisplayIds, null });
                        
                        if (ModConfig.DebugMode.Value)
                        {
                            LinceMultipleLoversPlugin.Log.LogInfo($"[ShareView] 成功调用SetDatas<int>，数据: {string.Join(", ", friendsDisplayIds)}");
                        }
                    }
                    else
                    {
                        // 回退到非泛型调用
                        var nonGenericMethod = itemgroupFriend.GetType().GetMethod("SetDatas", new[] { typeof(object), typeof(object) });
                        nonGenericMethod?.Invoke(itemgroupFriend, new object[] { friendsDisplayIds, null });
                    }
                }
                
                // 调整大小（根据数量）
                float sizeX;
                if (friendsDisplayIds.Count == 1)
                    sizeX = 600f;
                else if (friendsDisplayIds.Count == 2)
                    sizeX = 934f;
                else
                    sizeX = 1200f; // 3个或更多
                
                var setSizeXMethod = groupFriend?.GetType().GetMethod("SetSizeX");
                setSizeXMethod?.Invoke(groupFriend, new object[] { sizeX });
                
                if (ModConfig.DebugMode.Value)
                {
                    LinceMultipleLoversPlugin.Log.LogInfo($"[ShareView] 最终显示: 恋人组1个，朋友组{friendsDisplayIds.Count}个，宽度: {sizeX}");
                }
            }
            
            return false; // 跳过原方法
        }
    }
}
