namespace MainUI.LogicalConfiguration.Services
{
    /// <summary>
    /// 变量同步协调器接口 - 统一管理变量在各存储位置的同步
    /// 
    /// 设计原则：
    /// - 单一数据源真相 (Single Source of Truth): WorkflowStateService
    /// - 持久化层只是备份: JSON文件
    /// - 同步是单向的: 加载时 JSON -> State, 保存时 State -> JSON
    /// 
    /// 数据流设计：
    /// ┌──────────────────────────────────────────────────┐
    /// │  VariableSynchronizer (协调层)                    │
    /// │     ↓ 加载                    ↑ 持久化           │
    /// │  ┌─────────────┐      ┌─────────────────┐       │
    /// │  │ JSON 文件   │ ──→ │ WorkflowState   │       │
    /// │  │ (持久化)    │ ←── │ (运行时真相)    │       │
    /// │  └─────────────┘      └─────────────────┘       │
    /// │                              ↓                   │
    /// │                     GlobalVariableManager        │
    /// │                        (只读访问器)              │
    /// └──────────────────────────────────────────────────┘
    /// </summary>
    public interface IVariableSynchronizer
    {
        /// <summary>
        /// 是否已初始化
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// 是否有未保存的更改
        /// </summary>
        bool HasUnsavedChanges { get; }

        /// <summary>
        /// 从持久化存储加载变量到运行时状态
        /// </summary>
        Task LoadVariablesAsync();

        /// <summary>
        /// 将运行时变量持久化到存储
        /// </summary>
        Task PersistVariablesAsync();

        /// <summary>
        /// 添加或更新变量
        /// </summary>
        /// <param name="variable">变量对象</param>
        /// <param name="persistImmediately">是否立即持久化</param>
        Task AddOrUpdateVariableAsync(VarItem_Enhanced variable, bool persistImmediately = false);

        /// <summary>
        /// 批量添加或更新变量
        /// </summary>
        /// <param name="variables">变量列表</param>
        /// <param name="persistImmediately">是否立即持久化</param>
        Task AddOrUpdateVariablesAsync(IEnumerable<VarItem_Enhanced> variables, bool persistImmediately = false);

        /// <summary>
        /// 删除变量
        /// </summary>
        /// <param name="varName">变量名</param>
        /// <param name="persistImmediately">是否立即持久化</param>
        Task RemoveVariableAsync(string varName, bool persistImmediately = false);

        /// <summary>
        /// 更新变量值（仅更新值，不更新其他属性）
        /// </summary>
        /// <param name="varName">变量名</param>
        /// <param name="value">新值</param>
        /// <param name="source">更新来源描述</param>
        void UpdateVariableValue(string varName, object value, string source = null);

        /// <summary>
        /// 获取所有用户变量（排除系统变量）
        /// </summary>
        IReadOnlyList<VarItem_Enhanced> GetUserVariables();

        /// <summary>
        /// 获取所有变量（包括系统变量）
        /// </summary>
        IReadOnlyList<VarItem_Enhanced> GetAllVariables();

        /// <summary>
        /// 按名称查找变量
        /// </summary>
        VarItem_Enhanced FindVariable(string varName);

        /// <summary>
        /// 检查变量是否存在
        /// </summary>
        bool VariableExists(string varName);

        /// <summary>
        /// 清空所有用户变量（保留系统变量）
        /// </summary>
        /// <param name="persistImmediately">是否立即持久化</param>
        Task ClearUserVariablesAsync(bool persistImmediately = false);

        /// <summary>
        /// 变量变更事件
        /// </summary>
        event EventHandler<VariableChangedEventArgs> VariableChanged;
    }

    #region 事件参数

    /// <summary>
    /// 变量变更事件参数
    /// </summary>
    public class VariableChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 变更类型
        /// </summary>
        public VariableChangeType ChangeType { get; set; }

        /// <summary>
        /// 变更的变量
        /// </summary>
        public VarItem_Enhanced Variable { get; set; }

        /// <summary>
        /// 变更的变量列表（批量操作时使用）
        /// </summary>
        public IReadOnlyList<VarItem_Enhanced> Variables { get; set; }

        /// <summary>
        /// 旧值
        /// </summary>
        public string OldValue { get; set; }

        /// <summary>
        /// 新值
        /// </summary>
        public string NewValue { get; set; }

        /// <summary>
        /// 更新来源
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// 变更时间
        /// </summary>
        public DateTime ChangedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 变量变更类型
    /// </summary>
    public enum VariableChangeType
    {
        /// <summary>
        /// 新增
        /// </summary>
        Added,

        /// <summary>
        /// 更新（结构属性更新）
        /// </summary>
        Updated,

        /// <summary>
        /// 值变更（仅值变更）
        /// </summary>
        ValueChanged,

        /// <summary>
        /// 删除
        /// </summary>
        Removed,

        /// <summary>
        /// 批量加载
        /// </summary>
        BatchLoaded,

        /// <summary>
        /// 批量更新
        /// </summary>
        BatchUpdated,

        /// <summary>
        /// 清空
        /// </summary>
        Cleared
    }

    #endregion
}
