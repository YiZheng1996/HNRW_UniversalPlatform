using MainUI.LogicalConfiguration.Engine;
using MainUI.LogicalConfiguration.Forms;
using MainUI.LogicalConfiguration.LogicalManager;
using MainUI.LogicalConfiguration.Services.ServicesPLC;
using MainUI.Procedure.DSL.LogicalConfiguration.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace MainUI.LogicalConfiguration.Services
{
    /// <summary>
    /// 依赖注入服务扩展类 - 重构版
    /// 
    /// 设计原则：
    /// 1. 集中管理服务注册逻辑
    /// 2. 提供清晰的 API
    /// 3. 按功能模块分组注册
    /// 4. 支持链式调用
    /// </summary>
    public static class DIServiceExtensions
    {
        #region 核心服务

        /// <summary>
        /// 注册工作流核心服务
        /// 包含状态管理、配置服务、变量同步等核心功能
        /// </summary>
        public static IServiceCollection AddWorkflowCore(this IServiceCollection services)
        {
            // ========================================
            // 1. 核心状态服务 (单例 - 全局共享)
            // ========================================
            services.AddSingleton<IWorkflowStateService, WorkflowStateService>();

            // ========================================
            // 2. 配置服务 (作用域 - 每个窗体实例一个)
            // ========================================
            services.AddScoped<IWorkflowConfigurationService, WorkflowConfigurationService>();

            // ========================================
            // 3. 变量同步器 (作用域 - 每个窗体实例一个)
            // ========================================
            services.AddScoped<IVariableSynchronizer, VariableSynchronizer>();

            // ========================================
            // 4. 全局变量管理器 (单例 - 只读访问器)
            // ========================================
            services.AddSingleton<GlobalVariableManager>();

            // ========================================
            // 5. 表达式引擎 (单例)
            // ========================================
            services.AddSingleton<ExpressionEngine>();
            services.AddSingleton<VariableAssignmentEngine>();

            // ========================================
            // 6. 步骤详情提供器 (单例)
            // ========================================
            services.AddSingleton<StepDetailsProvider>();

            return services;
        }

        #endregion

        #region PLC 服务

        /// <summary>
        /// 注册 PLC 相关服务
        /// </summary>
        public static IServiceCollection AddPLCServices(this IServiceCollection services)
        {
            // PLC 配置服务 (单例)
            services.AddSingleton<IPLCConfigurationService, PLCConfigurationService>();

            // PLC 模块提供器 (单例)
            services.AddSingleton<IPLCModuleProvider, PLCModuleProvider>();

            // PLC 管理器 (作用域 - 支持每个窗体独立管理)
            services.AddScoped<IPLCManager, PLCManager>();

            // 配置选项
            services.Configure<PLCManagerOptions>(options =>
            {
                options.ConnectionTimeout = TimeSpan.FromSeconds(30);
                options.OperationTimeout = TimeSpan.FromSeconds(10);
                options.MaxRetryCount = 3;
            });

            return services;
        }

        #endregion

        #region UI 服务

        /// <summary>
        /// 注册 UI 服务
        /// 包含窗体服务和所有参数表单
        /// </summary>
        public static IServiceCollection AddUIServices(this IServiceCollection services)
        {
            // ========================================
            // 1. 窗体服务 (作用域)
            // ========================================
            services.AddScoped<IFormService, FormService>();

            // ========================================
            // 2. 参数表单 (瞬态 - 每次请求创建新实例)
            // ========================================
            // 变量相关
            services.AddTransient<Form_DefineVar>();
            services.AddTransient<Form_VariableAssignment>();
            services.AddTransient<Form_VariableMonitor>();

            // PLC 相关
            services.AddTransient<Form_ReadPLC>();
            services.AddTransient<Form_WritePLC>();
            services.AddTransient<Form_DefinePoint>();

            // 流程控制
            services.AddTransient<Form_DelayTime>();
            services.AddTransient<Form_Loop>();
            services.AddTransient<Form_Detection>();
            services.AddTransient<Form_Condition>();
            services.AddTransient<Form_WaitForStable>();

            // 数据操作
            services.AddTransient<Form_ReadCells>();
            services.AddTransient<Form_WriteCells>();
            services.AddTransient<Form_SaveReport>();

            // 系统功能
            services.AddTransient<Form_SystemPrompt>();

            return services;
        }

        #endregion

        #region 日志服务

        /// <summary>
        /// 注册工作流日志服务
        /// </summary>
        public static IServiceCollection AddWorkflowLogging(this IServiceCollection services)
        {
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                
                // 控制台日志
                builder.AddConsole();
                
                // 调试输出
                builder.AddDebug();

                // 添加NLog
                builder.AddNLog(); 
            });

            return services;
        }

        #endregion

        #region 便捷方法

        /// <summary>
        /// 注册所有工作流相关服务
        /// 一站式注册方法
        /// </summary>
        public static IServiceCollection AddAllWorkflowServices(this IServiceCollection services)
        {
            return services
                .AddWorkflowCore()
                .AddPLCServices()
                .AddUIServices()
                .AddWorkflowLogging();
        }

        #endregion
    }

    #region 配置选项类

    /// <summary>
    /// 工作流服务配置选项
    /// </summary>
    public class WorkflowServiceOptions
    {
        /// <summary>
        /// 是否启用事件日志记录
        /// </summary>
        public bool EnableEventLogging { get; set; } = false;

        /// <summary>
        /// 是否启用性能监控
        /// </summary>
        public bool EnablePerformanceMonitoring { get; set; } = false;

        /// <summary>
        /// 变量缓存的最大大小
        /// </summary>
        public int MaxVariableCacheSize { get; set; } = 1000;

        /// <summary>
        /// 步骤缓存的最大大小
        /// </summary>
        public int MaxStepCacheSize { get; set; } = 500;

        /// <summary>
        /// 自动保存间隔（秒），0 表示禁用
        /// </summary>
        public int AutoSaveIntervalSeconds { get; set; } = 0;
    }

    /// <summary>
    /// PLC 配置选项
    /// </summary>
    public class PLCConfigurationOptions
    {
        /// <summary>
        /// 配置文件路径
        /// </summary>
        public string ConfigurationPath { get; set; }

        /// <summary>
        /// 是否启用文件监控
        /// </summary>
        public bool EnableFileWatcher { get; set; } = true;
    }

    /// <summary>
    /// PLC 管理器选项
    /// </summary>
    public class PLCManagerOptions
    {
        /// <summary>
        /// 连接超时时间
        /// </summary>
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// 操作超时时间
        /// </summary>
        public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int MaxRetryCount { get; set; } = 3;

        /// <summary>
        /// 重试间隔
        /// </summary>
        public TimeSpan RetryInterval { get; set; } = TimeSpan.FromMilliseconds(500);
    }

    #endregion
}
