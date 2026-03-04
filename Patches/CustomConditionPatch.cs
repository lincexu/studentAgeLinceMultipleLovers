using Condition;
using Sdk;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinceMultipleLovers.Patches
{
    /// <summary>
    /// 自定义条件补丁
    /// 
    /// 通过Prefix拦截GenConditioner工厂方法，注入自定义条件类型5207。
    /// 
    /// 支持的格式:
    ///   [5207, 1, X]              — 恋人总数 >= X
    ///   [5207, -1, X]             — 恋人总数 &lt;= X
    ///   [5207, 2, 1, id1, id2, …] — 所有指定角色均为恋人
    ///   [5207, 2, -1, id1, id2,…] — 所有指定角色均不是恋人
    ///   [5207, 3, 1, x, y]        — 角色y是第x个恋人（1-based）
    ///   [5207, 3, 2, y]           — 角色y是最后一个恋人
    ///   [5207, 3, 3, x, y]        — 角色x先于角色y成为恋人
    /// </summary>
    public static class CustomConditionPatch
    {
        public static bool GenConditioner_Prefix(List<double> _condition, ref Conditioner _conditioner, ref Conditioner __result)
        {
            if (_condition == null || _condition.Count < 3)
                return true;

            int type = (int)_condition[0];
            if (type != 5207)
                return true; // 非自定义类型，走原版逻辑

            try
            {
                int subType = (int)_condition[1];
                Conditioner newConditioner;

                switch (subType)
                {
                    case 1:
                    case -1:
                        newConditioner = new CustomConditionerLoverCount(_conditioner, _condition);
                        break;
                    case 2:
                        newConditioner = new CustomConditionerLoverBatch(_conditioner, _condition);
                        break;
                    case 3:
                        newConditioner = new CustomConditionerLoverOrder(_conditioner, _condition);
                        break;
                    default:
                        LinceMultipleLoversPlugin.Log.LogWarning(
                            $"[CustomCondition 5207] 未知subType={subType}，跳过");
                        return true;
                }

                _conditioner = newConditioner;
                __result = newConditioner;

                if (ModConfig.DebugMode.Value)
                {
                    LinceMultipleLoversPlugin.Log.LogInfo(
                        $"[CustomCondition 5207] 创建条件: subType={subType}, 类型={newConditioner.GetType().Name}");
                }

                return false; // 跳过原版GenConditioner
            }
            catch (Exception ex)
            {
                LinceMultipleLoversPlugin.Log.LogError($"[CustomCondition 5207] 创建失败: {ex}");
                return true; // 出错时走原版（会输出"无此条件"）
            }
        }
    }

    // =========================================================================
    //  subType = 1 / -1 : 恋人数量检查
    // =========================================================================

    /// <summary>
    /// 恋人数量检查
    ///   [5207, 1, X]  → 恋人数 >= X
    ///   [5207, -1, X] → 恋人数 &lt;= X
    /// </summary>
    public class CustomConditionerLoverCount : Conditioner
    {
        private int threshold;

        public CustomConditionerLoverCount() { }

        public CustomConditionerLoverCount(Conditioner _conditioner, List<double> _condition)
            : base(_conditioner, _condition)
        {
            this.threshold = (int)_condition[2];
        }

        public override bool OnIsMatch()
        {
            int loverCount = LoverIdInterceptor.GetAllLoverIds().Count;

            bool result;
            if (this.subType == 1)
                result = loverCount >= this.threshold;
            else if (this.subType == -1)
                result = loverCount <= this.threshold;
            else
                result = false;

            if (ModConfig.DebugMode.Value)
            {
                string op = this.subType == 1 ? ">=" : "<=";
                LinceMultipleLoversPlugin.Log.LogInfo(
                    $"[Condition 5207.{this.subType}] 恋人数量={loverCount} {op} {this.threshold} → {result}");
            }

            return result;
        }

        public override string OnToString(int _type = 0)
        {
            if (this.subType == 1) return $"恋人数量≥{this.threshold}";
            if (this.subType == -1) return $"恋人数量≤{this.threshold}";
            return $"恋人数量条件(未知)";
        }

        public override ValueTuple<float, float> OnGetProgress()
        {
            int loverCount = LoverIdInterceptor.GetAllLoverIds().Count;
            return new ValueTuple<float, float>(loverCount, this.threshold);
        }
    }

    // =========================================================================
    //  subType = 2 : 批量恋人身份检查
    // =========================================================================

    /// <summary>
    /// 批量恋人身份检查
    ///   [5207, 2, 1, id1, id2, …]  → 所有指定角色均为恋人
    ///   [5207, 2, -1, id1, id2, …] → 所有指定角色均不是恋人
    /// </summary>
    public class CustomConditionerLoverBatch : Conditioner
    {
        private int childType;
        private List<int> roleIds = new List<int>();

        public CustomConditionerLoverBatch() { }

        public CustomConditionerLoverBatch(Conditioner _conditioner, List<double> _condition)
            : base(_conditioner, _condition)
        {
            this.childType = (int)_condition[2];
            for (int i = 3; i < _condition.Count; i++)
            {
                this.roleIds.Add((int)_condition[i]);
            }
        }

        public override bool OnIsMatch()
        {
            if (this.roleIds.Count == 0)
            {
                if (ModConfig.DebugMode.Value)
                    LinceMultipleLoversPlugin.Log.LogWarning("[Condition 5207.2] 未指定角色ID，返回false");
                return false;
            }

            var allLoverIds = LoverIdInterceptor.GetAllLoverIds();
            bool result;

            if (this.childType == 1)
            {
                // 所有角色都是恋人
                result = this.roleIds.All(id => allLoverIds.Contains(id));
            }
            else if (this.childType == -1)
            {
                // 所有角色都不是恋人
                result = this.roleIds.All(id => !allLoverIds.Contains(id));
            }
            else
            {
                LinceMultipleLoversPlugin.Log.LogWarning(
                    $"[Condition 5207.2] 未知childType={this.childType}，返回false");
                result = false;
            }

            if (ModConfig.DebugMode.Value)
            {
                string ids = string.Join(",", this.roleIds);
                string check = this.childType == 1 ? "均为恋人" : "均非恋人";
                LinceMultipleLoversPlugin.Log.LogInfo(
                    $"[Condition 5207.2] 角色[{ids}] {check} → {result}");
            }

            return result;
        }

        public override string OnToString(int _type = 0)
        {
            string ids = string.Join(",", this.roleIds);
            if (this.childType == 1) return $"角色[{ids}]均为恋人";
            if (this.childType == -1) return $"角色[{ids}]均非恋人";
            return $"批量恋人检查(childType={this.childType})";
        }

        public override ValueTuple<float, float> OnGetProgress()
        {
            var allLoverIds = LoverIdInterceptor.GetAllLoverIds();
            int matched;
            if (this.childType == 1)
                matched = this.roleIds.Count(id => allLoverIds.Contains(id));
            else
                matched = this.roleIds.Count(id => !allLoverIds.Contains(id));
            return new ValueTuple<float, float>(matched, this.roleIds.Count);
        }
    }

    // =========================================================================
    //  subType = 3 : 恋人顺序 / 位置检查
    // =========================================================================

    /// <summary>
    /// 恋人顺序/位置检查
    ///   [5207, 3, 1, x, y] → 角色y是第x个恋人（1-based）
    ///   [5207, 3, 2, y]    → 角色y是最后一个恋人
    ///   [5207, 3, 3, x, y] → 角色x先于角色y成为恋人
    /// </summary>
    public class CustomConditionerLoverOrder : Conditioner
    {
        private int childType;
        private int paramX;
        private int paramY;

        public CustomConditionerLoverOrder() { }

        public CustomConditionerLoverOrder(Conditioner _conditioner, List<double> _condition)
            : base(_conditioner, _condition)
        {
            this.childType = (int)_condition[2];
            switch (this.childType)
            {
                case 1: // y是第x个恋人
                    this.paramX = (int)_condition[3];
                    this.paramY = (int)_condition[4];
                    break;
                case 2: // y是最后一个恋人
                    this.paramY = (int)_condition[3];
                    break;
                case 3: // x先于y成为恋人
                    this.paramX = (int)_condition[3];
                    this.paramY = (int)_condition[4];
                    break;
                default:
                    LinceMultipleLoversPlugin.Log.LogWarning(
                        $"[Condition 5207.3] 构造时遇到未知childType={this.childType}");
                    break;
            }
        }

        public override bool OnIsMatch()
        {
            var allLoverIds = LoverIdInterceptor.GetAllLoverIds();
            bool result;

            switch (this.childType)
            {
                case 1:
                {
                    // y是第x个恋人（1-based）
                    int pos = this.paramX;
                    result = pos >= 1 && pos <= allLoverIds.Count
                             && allLoverIds[pos - 1] == this.paramY;

                    if (ModConfig.DebugMode.Value)
                    {
                        LinceMultipleLoversPlugin.Log.LogInfo(
                            $"[Condition 5207.3.1] 角色{this.paramY}是否为第{pos}个恋人 → {result}" +
                            $"  (恋人列表: [{string.Join(",", allLoverIds)}])");
                    }
                    break;
                }
                case 2:
                {
                    // y是最后一个恋人
                    result = allLoverIds.Count > 0
                             && allLoverIds[allLoverIds.Count - 1] == this.paramY;

                    if (ModConfig.DebugMode.Value)
                    {
                        LinceMultipleLoversPlugin.Log.LogInfo(
                            $"[Condition 5207.3.2] 角色{this.paramY}是否为最后一个恋人 → {result}" +
                            $"  (恋人列表: [{string.Join(",", allLoverIds)}])");
                    }
                    break;
                }
                case 3:
                {
                    // x先于y成为恋人（两人都必须是恋人）
                    int indexX = allLoverIds.IndexOf(this.paramX);
                    int indexY = allLoverIds.IndexOf(this.paramY);
                    result = indexX >= 0 && indexY >= 0 && indexX < indexY;

                    if (ModConfig.DebugMode.Value)
                    {
                        LinceMultipleLoversPlugin.Log.LogInfo(
                            $"[Condition 5207.3.3] 角色{this.paramX}(idx={indexX})先于角色{this.paramY}(idx={indexY}) → {result}");
                    }
                    break;
                }
                default:
                    LinceMultipleLoversPlugin.Log.LogWarning(
                        $"[Condition 5207.3] 未知childType={this.childType}，返回false");
                    result = false;
                    break;
            }

            return result;
        }

        public override string OnToString(int _type = 0)
        {
            switch (this.childType)
            {
                case 1: return $"角色{this.paramY}是第{this.paramX}个恋人";
                case 2: return $"角色{this.paramY}是最后一个恋人";
                case 3: return $"角色{this.paramX}先于角色{this.paramY}成为恋人";
                default: return $"恋人顺序条件(childType={this.childType})";
            }
        }

        public override ValueTuple<float, float> OnGetProgress()
        {
            bool matched = OnIsMatch();
            return new ValueTuple<float, float>(matched ? 1f : 0f, 1f);
        }
    }
}
