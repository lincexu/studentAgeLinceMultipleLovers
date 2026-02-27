using HarmonyLib;
using System.Collections.Generic;
using GenUI.Main;
using Sdk;
using UnityEngine;
using View.Main;

namespace LinceMultipleLovers.Patches
{
    /// <summary>
    /// ShareView补丁 - 结算页面恋人栏支持切换按钮循环切换多个恋人
    /// 朋友/至交栏交由原版逻辑处理
    /// </summary>
    public static class ShareViewPatch
    {
        private static int _currentLoverIndex = 0;
        private static ShareView _currentShareView = null!;

        private static readonly System.Reflection.BindingFlags PrivateInstance =
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

        [HarmonyPatch(typeof(ShareView), "RefreshFriend")]
        [HarmonyPrefix]
        public static bool RefreshFriend_Prefix(ShareView __instance)
        {
            if (!ModConfig.EnableMultipleLovers.Value)
                return true;

            _currentShareView = __instance;

            var loverIds = LoverIdInterceptor.GetAllLoverIds();

            // ShareView 私有字段
            var loverIdField = typeof(ShareView).GetField("loverId", PrivateInstance);
            var loverCellField = typeof(ShareView).GetField("loverCell", PrivateInstance);
            var allFriendsField = typeof(ShareView).GetField("allFriends", PrivateInstance);
            var friendsField = typeof(ShareView).GetField("friends", PrivateInstance);
            var isSavingField = typeof(ShareView).GetField("isSaving", PrivateInstance);

            // ShareUI 公有字段（父类）- 直接通过实例访问
            // __instance 继承自 ShareUI，所以这些 public 字段可直接访问
            RectTransform groupLover = __instance.group_lover;
            RectTransform groupFriend = __instance.group_friend;
            UIItemGroup itemgroupFriend = __instance.itemgroup_friend;

            bool isSaving = false;
            if (isSavingField?.GetValue(__instance) is bool b) isSaving = b;

            // ========== 恋人栏（Mod逻辑：支持切换） ==========
            if (loverIds.Count > 0)
            {
                if (_currentLoverIndex >= loverIds.Count)
                    _currentLoverIndex = 0;

                int currentLoverId = loverIds[_currentLoverIndex];
                loverIdField?.SetValue(__instance, currentLoverId);

                var loverCell = loverCellField?.GetValue(__instance) as Cell_ShareHeadItemUI;
                if (loverCell != null)
                {
                    loverCell.data = currentLoverId;

                    var onRenderFriendMethod = typeof(ShareView).GetMethod("OnRenderFriend", PrivateInstance);
                    onRenderFriendMethod?.Invoke(__instance, new object[] { loverCell });

                    // 多恋人时设置切换按钮
                    SetupLoverSwitchButton(loverCell, loverIds, isSaving);
                }

                if (groupLover != null)
                    groupLover.gameObject.SetActive(true);

                if (ModConfig.DebugMode.Value)
                    LinceMultipleLoversPlugin.Log.LogInfo($"[ShareView] 显示恋人: {currentLoverId} ({_currentLoverIndex + 1}/{loverIds.Count})");
            }
            else
            {
                if (groupLover != null)
                    groupLover.gameObject.SetActive(false);
            }

            // ========== 朋友/至交栏（原版逻辑） ==========
            var currentFriends = friendsField?.GetValue(__instance) as List<int>;

            if (currentFriends == null || currentFriends.Count == 0)
            {
                var allFriendsList = Singleton<RoleMgr>.Ins.GetRelationData(true).GetRelationship(6);
                if (allFriendsList != null && allFriendsList.Count > 0)
                {
                    var filtered = new List<int>(allFriendsList);
                    // 移除所有恋人避免重复
                    foreach (int lid in loverIds)
                        filtered.Remove(lid);

                    allFriendsField?.SetValue(__instance, filtered);

                    if (filtered.Count > 0)
                    {
                        var display = filtered.GetRange(0, Mathf.Min(filtered.Count, 2));
                        friendsField?.SetValue(__instance, display);
                        currentFriends = display;
                    }
                }
            }

            if (currentFriends == null || currentFriends.Count == 0)
            {
                if (groupFriend != null)
                    groupFriend.gameObject.SetActive(false);
            }
            else
            {
                if (groupFriend != null)
                    groupFriend.gameObject.SetActive(true);

                itemgroupFriend?.SetDatas(currentFriends, null);

                if (groupFriend != null)
                    groupFriend.SetSizeX(currentFriends.Count == 2 ? 934f : 600f);
            }

            return false;
        }

        /// <summary>
        /// 恋人切换按钮设置
        /// </summary>
        private static void SetupLoverSwitchButton(Cell_ShareHeadItemUI loverCell, List<int> loverIds, bool isSaving)
        {
            if (loverCell == null || loverIds.Count <= 1)
                return;

            var btnChange = loverCell.btn_change;
            if (btnChange == null)
                return;

            bool shouldShow = !isSaving;
            btnChange.gameObject.SetActive(shouldShow);

            if (shouldShow)
            {
                var rt = btnChange.gameObject.GetComponent<RectTransform>();
                if (rt != null)
                    rt.anchoredPosition = new Vector2(300f, 0f);
            }

            var txtName = loverCell.txtex_name;
            if (txtName != null)
                txtName.rectTransform.SetSizeX(shouldShow ? 120f : 170f);

            // 用 UIButton.AddClick 设置点击事件
            btnChange.AddClick(() => OnLoverSwitchButtonClick(loverIds));
        }

        private static void OnLoverSwitchButtonClick(List<int> loverIds)
        {
            if (loverIds.Count <= 1)
                return;

            _currentLoverIndex = (_currentLoverIndex + 1) % loverIds.Count;

            if (ModConfig.DebugMode.Value)
                LinceMultipleLoversPlugin.Log.LogInfo($"[ShareView] 切换恋人索引: {_currentLoverIndex} (ID: {loverIds[_currentLoverIndex]})");

            if (_currentShareView != null)
            {
                var refreshFriendMethod = typeof(ShareView).GetMethod("RefreshFriend", PrivateInstance);
                refreshFriendMethod?.Invoke(_currentShareView, null);
            }
        }

        [HarmonyPatch(typeof(ShareView), "OnClose")]
        [HarmonyPostfix]
        public static void OnClose_Postfix()
        {
            _currentLoverIndex = 0;
            _currentShareView = null!;
        }
    }
}
