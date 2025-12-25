using MainUI.UniversalPlatform.Core.Domain.Variables;
using MainUI.UniversalPlatform.Core.Domain.Workflows;

namespace MainUI.UniversalPlatform.Core.Application.Interfaces
{
    /// <summary>
    /// 工作流应用服务接口
    /// 提供工作流的完整生命周期管理
    /// </summary>
    public interface IWorkflowAppService
    {
        #region 工作流管理

        /// <summary>
        /// 加载工作流
        /// </summary>
        Task<Workflow> LoadWorkflowAsync(
            string modelType,
            string modelName,
            string itemName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 保存工作流
        /// </summary>
        Task SaveWorkflowAsync(Workflow workflow, CancellationToken cancellationToken = default);

        /// <summary>
        /// 创建新工作流
        /// </summary>
        Task<Workflow> CreateWorkflowAsync(
            string modelType,
            string modelName,
            string itemName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除工作流
        /// </summary>
        Task DeleteWorkflowAsync(
            string modelType,
            string modelName,
            string itemName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取工作流列表
        /// </summary>
        Task<IEnumerable<WorkflowSummaryDto>> GetWorkflowListAsync(
            string modelType,
            string modelName,
            CancellationToken cancellationToken = default);

        #endregion

        #region 步骤管理

        /// <summary>
        /// 添加步骤
        /// </summary>
        Task<WorkflowStep> AddStepAsync(
            Workflow workflow,
            string stepName,
            object parameter = null,
            string remark = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 插入步骤
        /// </summary>
        Task<WorkflowStep> InsertStepAsync(
            Workflow workflow,
            int index,
            string stepName,
            object parameter = null,
            string remark = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除步骤
        /// </summary>
        Task<bool> RemoveStepAsync(
            Workflow workflow,
            int index,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 移动步骤
        /// </summary>
        Task<bool> MoveStepAsync(
            Workflow workflow,
            int fromIndex,
            int toIndex,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新步骤参数
        /// </summary>
        Task UpdateStepParameterAsync(
            Workflow workflow,
            int index,
            object parameter,
            CancellationToken cancellationToken = default);

        #endregion

        #region 工作流执行

        /// <summary>
        /// 执行工作流
        /// </summary>
        Task<WorkflowExecutionResult> ExecuteWorkflowAsync(
            Workflow workflow,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行单个步骤
        /// </summary>
        Task<StepExecutionResultDto> ExecuteStepAsync(
            Workflow workflow,
            int stepIndex,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 停止执行
        /// </summary>
        void StopExecution();

        /// <summary>
        /// 当前是否正在执行
        /// </summary>
        bool IsExecuting { get; }

        #endregion

        #region 事件

        /// <summary>
        /// 步骤状态变更事件
        /// </summary>
        event Action<WorkflowStep, int> StepStatusChanged;

        /// <summary>
        /// 执行进度事件
        /// </summary>
        event Action<int, int, string> ExecutionProgress;

        /// <summary>
        /// 执行完成事件
        /// </summary>
        event Action<bool, string> ExecutionCompleted;

        #endregion
    }

    /// <summary>
    /// 变量服务接口
    /// </summary>
    public interface IVariableService
    {
        /// <summary>
        /// 获取变量
        /// </summary>
        Variable GetVariable(string name);

        /// <summary>
        /// 获取变量值
        /// </summary>
        T GetValue<T>(string name);

        /// <summary>
        /// 设置变量值
        /// </summary>
        void SetVariable(string name, object value, string source = null, int? stepIndex = null);

        /// <summary>
        /// 添加变量
        /// </summary>
        void AddVariable(Variable variable);

        /// <summary>
        /// 删除变量
        /// </summary>
        bool RemoveVariable(string name);

        /// <summary>
        /// 获取所有变量
        /// </summary>
        IEnumerable<Variable> GetAllVariables();

        /// <summary>
        /// 获取用户变量
        /// </summary>
        IEnumerable<Variable> GetUserVariables();

        /// <summary>
        /// 获取系统变量
        /// </summary>
        IEnumerable<Variable> GetSystemVariables();

        /// <summary>
        /// 清除所有用户变量
        /// </summary>
        void ClearUserVariables();

        /// <summary>
        /// 变量是否存在
        /// </summary>
        bool Exists(string name);

        /// <summary>
        /// 变量变更事件
        /// </summary>
        event Action<Variable> VariableChanged;

        /// <summary>
        /// 变量添加事件
        /// </summary>
        event Action<Variable> VariableAdded;

        /// <summary>
        /// 变量删除事件
        /// </summary>
        event Action<string> VariableRemoved;
    }

    /// <summary>
    /// 步骤配置服务接口
    /// </summary>
    public interface IStepConfigService
    {
        /// <summary>
        /// 获取步骤类型列表
        /// </summary>
        IEnumerable<StepTypeInfo> GetStepTypes();

        /// <summary>
        /// 获取步骤类型信息
        /// </summary>
        StepTypeInfo GetStepType(string stepName);

        /// <summary>
        /// 获取步骤参数默认值
        /// </summary>
        object GetDefaultParameter(string stepName);

        /// <summary>
        /// 验证步骤参数
        /// </summary>
        ValidationResultDto ValidateParameter(string stepName, object parameter);

        /// <summary>
        /// 获取步骤预览文本
        /// </summary>
        string GetPreviewText(string stepName, object parameter);
    }

    #region DTOs

    /// <summary>
    /// 工作流摘要DTO
    /// </summary>
    public class WorkflowSummaryDto
    {
        public string ModelType { get; init; }
        public string ModelName { get; init; }
        public string ItemName { get; init; }
        public int StepCount { get; init; }
        public DateTime LastModified { get; init; }
    }

    /// <summary>
    /// 工作流执行结果
    /// </summary>
    public class WorkflowExecutionResult
    {
        public bool Success { get; init; }
        public string Message { get; init; }
        public int TotalSteps { get; init; }
        public int CompletedSteps { get; init; }
        public int FailedStep { get; init; } = -1;
        public TimeSpan Duration { get; init; }
        public List<StepExecutionResultDto> StepResults { get; init; } = new();
    }

    /// <summary>
    /// 步骤执行结果DTO
    /// </summary>
    public class StepExecutionResultDto
    {
        public int StepIndex { get; init; }
        public string StepName { get; init; }
        public bool Success { get; init; }
        public string Message { get; init; }
        public TimeSpan Duration { get; init; }
        public object OutputData { get; init; }
    }

    /// <summary>
    /// 步骤类型信息
    /// </summary>
    public class StepTypeInfo
    {
        public string Name { get; init; }
        public string DisplayName { get; init; }
        public string Category { get; init; }
        public string Description { get; init; }
        public string IconKey { get; init; }
        public Type ParameterType { get; init; }
    }

    /// <summary>
    /// 验证结果DTO
    /// </summary>
    public class ValidationResultDto
    {
        public bool IsValid { get; init; }
        public List<string> Errors { get; init; } = new();
        public string Message => string.Join("; ", Errors);

        public static ValidationResultDto Valid() => new() { IsValid = true };
        public static ValidationResultDto Invalid(params string[] errors) => new() { IsValid = false, Errors = errors.ToList() };
    }

    #endregion
}
