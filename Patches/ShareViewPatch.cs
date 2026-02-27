using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Sdk;
using View.Main;
using UnityEngine.Events;

namespace LinceMultipleLovers.Patches
{
    /// <summary>
    /// ShareView补丁 - 修复通关结算页面只显示一个恋人和至交显示问题
    /// 支持显示所有恋人和正确的朋友列表
    /// 新增：恋人切换按钮功能，支持循环切换显示多个恋人
    /// </summary>
    public static class ShareViewPatch
    {
        // 当前显示的恋人索引（用于循环切换）
        private static int _currentLoverIndex = 0;
        
        // 当前ShareView实例（用于刷新显示）
        private static ShareView _currentShareView = null;

        /// <summary>
        /// 修改RefreshFriend方法 - 支持显示多个恋人和正确的朋友列表
        /// </summary>
        [HarmonyPatch(typeof(ShareView), "RefreshFriend")]
        [HarmonyPrefix]
        public static bool RefreshFriend_Prefix(ShareView __instance)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return true; // 执行原方法

            // 保存当前实例
            _currentShareView = __instance;

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
            var isSavingField = typeof(ShareView).GetField("isSaving", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // ========== 处理恋人显示 ==========
            // 注意：原版UI中，group_lover只显示一个恋人（主恋人）
            // group_friend显示朋友列表
            // 为了显示所有恋人，我们需要将其他恋人添加到group_friend中
            
            if (loverIds.Count > 0)
            {
                // 确保当前索引在有效范围内
                if (_currentLoverIndex >= loverIds.Count)
                    _currentLoverIndex = 0;
                
                // 获取当前要显示的恋人ID（根据索引）
                int currentLoverId = loverIds[_currentLoverIndex];
                loverIdField?.SetValue(__instance, currentLoverId);
                
                var loverCell = loverCellField?.GetValue(__instance);
                if (loverCell != null)
                {
                    // 设置恋人数据
                    var dataField = loverCell.GetType().GetField("data", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    dataField?.SetValue(loverCell, currentLoverId);
                    
                    // 调用OnRenderFriend渲染恋人
                    var onRenderFriendMethod = typeof(ShareView).GetMethod("OnRenderFriend", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    onRenderFriendMethod?.Invoke(__instance, new object[] { loverCell });
                    
                    // 设置恋人切换按钮
                    SetupLoverSwitchButton(loverCell, loverIds, isSavingField?.GetValue(__instance));
                }
                
                // 显示恋人组
                var groupLover = groupLoverField?.GetValue(__instance);
                var groupLoverGameObject = groupLover?.GetType().GetProperty("gameObject")?.GetValue(groupLover);
                groupLoverGameObject?.GetType().GetMethod("SetActive")?.Invoke(groupLoverGameObject, new object[] { true });
                
                if (ModConfig.DebugMode.Value)
                {
                    LinceMultipleLoversPlugin.Log.LogInfo($"[ShareView] 当前显示恋人: {currentLoverId} (索引: {_currentLoverIndex + 1}/{loverIds.Count})");
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
            
            // 如果有多个恋人，将除当前显示外的其他恋人添加到朋友组
            if (loverIds.Count > 1)
            {
                for (int i = 0; i < loverIds.Count; i++)
                {
                    if (i != _currentLoverIndex) // 跳过当前显示在主位置的恋人
                    {
                        friendsDisplayIds.Add(loverIds[i]);
                    }
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

        /// <summary>
        /// 设置恋人切换按钮
        /// </summary>
        private static void SetupLoverSwitchButton(object loverCell, List<int> loverIds, object isSavingValue)
        {
            if (loverCell == null || loverIds.Count <= 1)
                return; // 只有一个恋人，不需要切换按钮

            // 获取Cell_ShareHeadItemUI的字段
            var btnChangeField = loverCell.GetType().GetField("btn_change", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var txtNameField = loverCell.GetType().GetField("txtex_name", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            var btnChange = btnChangeField?.GetValue(loverCell);
            var txtName = txtNameField?.GetValue(loverCell);
            
            if (btnChange == null)
                return;

            // 获取按钮的GameObject和RectTransform
            var btnGameObject = btnChange.GetType().GetProperty("gameObject")?.GetValue(btnChange);
            var btnTransform = btnChange.GetType().GetProperty("transform")?.GetValue(btnChange);
            
            // 检查是否在保存状态
            bool isSaving = isSavingValue is bool ? (bool)isSavingValue : false;
            
            // 显示切换按钮（多个恋人时显示）
            bool shouldShow = !isSaving;
            btnGameObject?.GetType().GetMethod("SetActive")?.Invoke(btnGameObject, new object[] { shouldShow });
            
            // 调整按钮位置到更右边（避免遮挡名称）
            if (btnTransform != null && shouldShow)
            {
                // 获取anchoredPosition属性
                var anchoredPositionProperty = btnTransform.GetType().GetProperty("anchoredPosition");
                if (anchoredPositionProperty != null)
                {
                    // 设置按钮位置为 (140, 0) - 更靠右
                    var newPosition = new UnityEngine.Vector2(140f, 0f);
                    anchoredPositionProperty.SetValue(btnTransform, newPosition);
                    
                    if (ModConfig.DebugMode.Value)
                    {
                        LinceMultipleLoversPlugin.Log.LogInfo($"[ShareView] 调整切换按钮位置到: {newPosition}");
                    }
                }
            }
            
            // 调整名称文本宽度（给按钮留出空间）
            if (txtName != null && shouldShow)
            {
                var rectTransform = txtName.GetType().GetProperty("rectTransform")?.GetValue(txtName);
                var setSizeXMethod = rectTransform?.GetType().GetMethod("SetSizeX");
                setSizeXMethod?.Invoke(rectTransform, new object[] { 150f }); // 增加宽度到150，因为按钮更靠右了
            }
            else if (txtName != null)
            {
                var rectTransform = txtName.GetType().GetProperty("rectTransform")?.GetValue(txtName);
                var setSizeXMethod = rectTransform?.GetType().GetMethod("SetSizeX");
                setSizeXMethod?.Invoke(rectTransform, new object[] { 170f });
            }

            // 清除旧的点击事件并添加新的
            var btnComponent = btnChange.GetType().GetProperty("btn")?.GetValue(btnChange);
            if (btnComponent != null)
            {
                // 获取onClick事件
                var onClickField = btnComponent.GetType().GetField("m_OnClick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var onClick = onClickField?.GetValue(btnComponent);
                
                // 移除所有监听器
                var removeAllListenersMethod = onClick?.GetType().GetMethod("RemoveAllListeners");
                removeAllListenersMethod?.Invoke(onClick, null);
                
                // 添加新的点击事件
                var addListenerMethod = onClick?.GetType().GetMethod("AddListener", new[] { typeof(UnityAction) });
                if (addListenerMethod != null)
                {
                    var action = new UnityAction(() => OnLoverSwitchButtonClick(loverIds));
                    addListenerMethod.Invoke(onClick, new object[] { action });
                    
                    if (ModConfig.DebugMode.Value)
                    {
                        LinceMultipleLoversPlugin.Log.LogInfo($"[ShareView] 已设置恋人切换按钮点击事件");
                    }
                }
            }
        }

        /// <summary>
        /// 恋人切换按钮点击处理
        /// </summary>
        private static void OnLoverSwitchButtonClick(List<int> loverIds)
        {
            if (loverIds.Count <= 1)
                return;

            // 切换到下一个恋人（循环）
            _currentLoverIndex = (_currentLoverIndex + 1) % loverIds.Count;
            
            if (ModConfig.DebugMode.Value)
            {
                LinceMultipleLoversPlugin.Log.LogInfo($"[ShareView] 切换到恋人索引: {_currentLoverIndex} (ID: {loverIds[_currentLoverIndex]})");
            }

            // 刷新显示
            if (_currentShareView != null)
            {
                var refreshFriendMethod = typeof(ShareView).GetMethod("RefreshFriend", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                refreshFriendMethod?.Invoke(_currentShareView, null);
            }
        }

        /// <summary>
        /// 当ShareView关闭时重置索引
        /// </summary>
        [HarmonyPatch(typeof(ShareView), "OnClose")]
        [HarmonyPostfix]
        public static void OnClose_Postfix()
        {
            // 重置恋人索引
            _currentLoverIndex = 0;
            _currentShareView = null;
            
            if (ModConfig.DebugMode.Value)
            {
                LinceMultipleLoversPlugin.Log.LogInfo($"[ShareView] 页面关闭，重置恋人索引");
            }
        }
    }
}
