using MainUI.LogicalConfiguration.Services;

namespace MainUI.LogicalConfiguration.LogicalManager
{
    /// <summary>
    /// 全局变量管理器 - 简化为只读访问器
    /// 
    /// 设计说明：
    /// - 这是一个简化的访问器，提供便捷的变量查询API
    /// - 所有写操作应通过 IVariableSynchronizer 进行
    /// - 保留只读方法以兼容现有代码
    /// </summary>
    public class GlobalVariableManager(IWorkflowStateService workflowState)
    {
        private readonly IWorkflowStateService _workflowState = workflowState ?? throw new ArgumentNullException(nameof(workflowState));

        #region 只读查询方法

        /// <summary>
        /// 获取所有变量
        /// </summary>
        public IReadOnlyList<VarItem_Enhanced> GetAllVariables()
        {
            return _workflowState.GetAllVariables().AsReadOnly();
        }

        /// <summary>
        /// 获取所有用户变量（排除系统变量）
        /// </summary>
        public IReadOnlyList<VarItem_Enhanced> GetAllUserVariables()
        {
            return _workflowState.GetAllVariables()
                .Where(v => !v.IsSystemVariable)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// 通过名称查找变量
        /// </summary>
        public VarItem_Enhanced FindVariableByName(string varName)
        {
            if (string.IsNullOrEmpty(varName)) return null;
            return _workflowState.FindVariableByName(varName);
        }

        /// <summary>
        /// 安全查找变量（不抛异常）
        /// </summary>
        public VarItem_Enhanced TryFindVariableByName(string varName)
        {
            try
            {
                return FindVariableByName(varName);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 检查变量是否存在
        /// </summary>
        public bool VariableExists(string varName)
        {
            return FindVariableByName(varName) != null;
        }

        /// <summary>
        /// 获取变量值
        /// </summary>
        public object GetVariableValue(string varName)
        {
            return FindVariableByName(varName)?.VarValue;
        }

        /// <summary>
        /// 获取变量值（泛型）
        /// </summary>
        public T GetVariableValue<T>(string varName, T defaultValue = default)
        {
            var variable = FindVariableByName(varName);
            if (variable?.VarValue == null) return defaultValue;

            try
            {
                return (T)Convert.ChangeType(variable.VarValue, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        #endregion

        #region 已废弃方法（提供兼容性警告）

        /// <summary>
        /// 添加或更新变量
        /// </summary>
        [Obsolete("请使用 IVariableSynchronizer.AddOrUpdateVariableAsync() 替代")]
        public void AddOrUpdateVariable(VarItem_Enhanced variable)
        {
            // 临时兼容实现
            var existing = _workflowState.FindVariableByName(variable.VarName);
            if (existing != null)
            {
                existing.VarType = variable.VarType;
                existing.VarValue = variable.VarValue;
                existing.VarText = variable.VarText;
                existing.LastUpdated = DateTime.Now;
            }
            else
            {
                _workflowState.AddVariable(variable);
            }
        }

        /// <summary>
        /// 更新变量值
        /// </summary>
        [Obsolete("请使用 IVariableSynchronizer.AddOrUpdateVariableAsync() 替代")]
        public void UpdateVariableValue(string varName, object value, string varType)
        {
            var variable = FindVariableByName(varName);
            if (variable == null) return;

            variable.VarValue = ConvertValue(value, varType);
            variable.LastUpdated = DateTime.Now;
        }

        private static object ConvertValue(object value, string varType)
        {
            return varType.ToLower() switch
            {
                "int" => Convert.ToInt32(value),
                "double" => Convert.ToDouble(value),
                "bool" => Convert.ToBoolean(value),
                "string" => value?.ToString() ?? "",
                _ => value
            };
        }

        #endregion
    }


    #region 辅助数据类

    /// <summary>
    /// 变量赋值信息
    /// </summary>
    public class VariableAssignment
    {
        /// <summary>
        /// 变量名称
        /// </summary>
        public string VariableName { get; set; }

        /// <summary>
        /// 赋值描述（如"PLC读取(Module1.Tag1)"）
        /// </summary>
        public string AssignmentDescription { get; set; }

        /// <summary>
        /// 额外信息（可选）
        /// </summary>
        public string ExtraInfo { get; set; }
    }

    /// <summary>
    /// 变量冲突信息类
    /// </summary>
    public class VariableConflictInfo
    {
        /// <summary>
        /// 是否存在冲突
        /// </summary>
        public bool HasConflict { get; set; }

        /// <summary>
        /// 冲突的步骤索引
        /// </summary>
        public int ConflictStepIndex { get; set; } = -1;

        /// <summary>
        /// 冲突步骤的信息描述
        /// </summary>
        public string ConflictStepInfo { get; set; } = "";

        /// <summary>
        /// 冲突的赋值类型
        /// </summary>
        public VariableAssignmentType ConflictAssignmentType { get; set; } = VariableAssignmentType.None;

        /// <summary>
        /// 冲突详细说明
        /// </summary>
        public string ConflictDescription => HasConflict
            ? $"变量已被步骤{ConflictStepIndex + 1}({ConflictStepInfo})以{ConflictAssignmentType}方式赋值"
            : "无冲突";
    }

    /// <summary>
    /// 当前步骤信息
    /// </summary>
    public class CurrentStepInfo
    {
        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 步骤索引
        /// </summary>
        public int StepIndex { get; set; }

        /// <summary>
        /// 步骤对象
        /// </summary>
        public ChildModel Step { get; set; }

        /// <summary>
        /// 步骤名称
        /// </summary>
        public string StepName { get; set; }
    }

    #endregion

}


