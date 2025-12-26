using Newtonsoft.Json;

namespace MainUI.LogicalConfiguration.Services
{
    /// <summary>
    /// 工作流配置服务接口 - 负责JSON配置的持久化
    /// 
    /// 设计原则：
    /// - 单一职责：只负责配置文件的读写
    /// - 异步操作：所有IO操作都是异步的
    /// - 线程安全：使用信号量保护文件访问
    /// </summary>
    public interface IWorkflowConfigurationService
    {
        /// <summary>
        /// 配置文件路径
        /// </summary>
        string ConfigurationPath { get; }

        /// <summary>
        /// 初始化配置路径
        /// </summary>
        /// <param name="modelType">产品类型</param>
        /// <param name="modelName">产品型号</param>
        /// <param name="processName">工序名称</param>
        void Initialize(string modelType, string modelName, string processName);

        /// <summary>
        /// 确保配置文件存在，不存在则创建默认配置
        /// </summary>
        Task EnsureConfigurationExistsAsync(string modelType, string modelName, string processName);

        /// <summary>
        /// 加载变量配置
        /// </summary>
        Task<List<VarItem>> LoadVariablesAsync();

        /// <summary>
        /// 保存变量配置
        /// </summary>
        Task SaveVariablesAsync(IEnumerable<VarItem> variables);

        /// <summary>
        /// 加载步骤配置
        /// </summary>
        Task<List<ChildModel>> LoadStepsAsync();

        /// <summary>
        /// 保存步骤配置
        /// </summary>
        Task SaveStepsAsync(IEnumerable<ChildModel> steps);

        /// <summary>
        /// 加载完整配置
        /// </summary>
        Task<WorkflowConfiguration> LoadFullConfigurationAsync();

        /// <summary>
        /// 保存完整配置
        /// </summary>
        Task SaveFullConfigurationAsync(WorkflowConfiguration configuration);

        /// <summary>
        /// 配置变更事件
        /// </summary>
        event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;
    }

    #region 事件参数

    /// <summary>
    /// 配置变更事件参数
    /// </summary>
    public class ConfigurationChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 变更类型
        /// </summary>
        public ConfigurationChangeType ChangeType { get; set; }

        /// <summary>
        /// 变更的数据
        /// </summary>
        public object ChangedData { get; set; }

        /// <summary>
        /// 变更时间
        /// </summary>
        public DateTime ChangedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 配置变更类型
    /// </summary>
    public enum ConfigurationChangeType
    {
        /// <summary>
        /// 变量更新
        /// </summary>
        VariablesUpdated,

        /// <summary>
        /// 步骤更新
        /// </summary>
        StepsUpdated,

        /// <summary>
        /// 完整重载
        /// </summary>
        FullReload,

        /// <summary>
        /// 配置文件创建
        /// </summary>
        Created
    }

    #endregion

    #region 配置数据模型

    /// <summary>
    /// 工作流配置（统一的配置模型）
    /// </summary>
    public class WorkflowConfiguration
    {
        /// <summary>
        /// 系统信息
        /// </summary>
        public SystemInfo System { get; set; } = new();

        /// <summary>
        /// 产品类型
        /// </summary>
        public string ModelType { get; set; }

        /// <summary>
        /// 产品型号
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// 工序名称
        /// </summary>
        public string ProcessName { get; set; }

        /// <summary>
        /// 变量列表
        /// </summary>
        public List<VarItem> Variables { get; set; } = new();

        /// <summary>
        /// 步骤列表
        /// </summary>
        public List<ChildModel> Steps { get; set; } = new();

        /// <summary>
        /// 系统信息
        /// </summary>
        public class SystemInfo
        {
            /// <summary>
            /// 创建时间
            /// </summary>
            public string CreateTime { get; set; }

            /// <summary>
            /// 项目名称
            /// </summary>
            public string ProjectName { get; set; } = "软件通用平台";

            /// <summary>
            /// 最后修改时间
            /// </summary>
            public string LastModified { get; set; }

            /// <summary>
            /// 版本号
            /// </summary>
            public string Version { get; set; } = "1.0";
        }
    }

    #endregion
}
