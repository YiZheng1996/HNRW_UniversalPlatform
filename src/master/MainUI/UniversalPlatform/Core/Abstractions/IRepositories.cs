using MainUI.UniversalPlatform.Core.Domain.Variables;
using MainUI.UniversalPlatform.Core.Domain.Workflows;

namespace MainUI.UniversalPlatform.Core.Abstractions
{
    /// <summary>
    /// 工作流仓储接口
    /// 封装所有工作流数据的持久化操作
    /// </summary>
    public interface IWorkflowRepository
    {
        /// <summary>
        /// 加载工作流
        /// </summary>
        /// <param name="modelType">产品类型</param>
        /// <param name="modelName">产品型号</param>
        /// <param name="itemName">测试项名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>工作流实体</returns>
        Task<Workflow> LoadAsync(
            string modelType,
            string modelName,
            string itemName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 保存工作流
        /// </summary>
        /// <param name="workflow">工作流实体</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task SaveAsync(Workflow workflow, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查工作流是否存在
        /// </summary>
        /// <param name="modelType">产品类型</param>
        /// <param name="modelName">产品型号</param>
        /// <param name="itemName">测试项名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否存在</returns>
        Task<bool> ExistsAsync(
            string modelType,
            string modelName,
            string itemName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除工作流
        /// </summary>
        /// <param name="modelType">产品类型</param>
        /// <param name="modelName">产品型号</param>
        /// <param name="itemName">测试项名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task DeleteAsync(
            string modelType,
            string modelName,
            string itemName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取指定产品类型和型号下的所有工作流
        /// </summary>
        /// <param name="modelType">产品类型</param>
        /// <param name="modelName">产品型号</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>工作流列表</returns>
        Task<IEnumerable<WorkflowSummary>> GetWorkflowsAsync(
            string modelType,
            string modelName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取工作流文件路径
        /// </summary>
        string GetFilePath(string modelType, string modelName, string itemName);
    }

    /// <summary>
    /// 工作流摘要信息（用于列表显示）
    /// </summary>
    public class WorkflowSummary
    {
        public string ModelType { get; init; }
        public string ModelName { get; init; }
        public string ItemName { get; init; }
        public int StepCount { get; init; }
        public DateTime LastModified { get; init; }
        public string FilePath { get; init; }
    }

    /// <summary>
    /// 变量仓储接口
    /// </summary>
    public interface IVariableRepository
    {
        /// <summary>
        /// 加载工作流的变量定义
        /// </summary>
        /// <param name="modelType">产品类型</param>
        /// <param name="modelName">产品型号</param>
        /// <param name="itemName">测试项名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>变量列表</returns>
        Task<IEnumerable<Variable>> LoadAsync(
            string modelType,
            string modelName,
            string itemName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 保存变量定义
        /// </summary>
        /// <param name="modelType">产品类型</param>
        /// <param name="modelName">产品型号</param>
        /// <param name="itemName">测试项名称</param>
        /// <param name="variables">变量列表</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task SaveAsync(
            string modelType,
            string modelName,
            string itemName,
            IEnumerable<Variable> variables,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 添加变量
        /// </summary>
        Task AddAsync(
            string modelType,
            string modelName,
            string itemName,
            Variable variable,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除变量
        /// </summary>
        Task DeleteAsync(
            string modelType,
            string modelName,
            string itemName,
            string variableName,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 工作单元接口（用于事务管理）
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        IWorkflowRepository Workflows { get; }
        IVariableRepository Variables { get; }

        /// <summary>
        /// 提交所有更改
        /// </summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 开始事务
        /// </summary>
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 提交事务
        /// </summary>
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 回滚事务
        /// </summary>
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}
