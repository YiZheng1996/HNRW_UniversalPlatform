using MainUI.UniversalPlatform.Core.Abstractions;
using MainUI.UniversalPlatform.Core.Application.Interfaces;
using MainUI.UniversalPlatform.Infrastructure.Execution;
using MainUI.UniversalPlatform.Infrastructure.Execution.Executors;
using MainUI.UniversalPlatform.Infrastructure.Expression;
using MainUI.UniversalPlatform.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System.Reflection;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace MainUI.UniversalPlatform.Infrastructure.DependencyInjection
{
    /// <summary>
    /// 核心服务注册扩展 - 纯新架构版本
    /// 不依赖任何旧代码
    /// </summary>
    public static class CoreServiceExtensions
    {
        /// <summary>
        /// 注册所有核心服务
        /// </summary>
        public static IServiceCollection AddWorkflowCore(this IServiceCollection services)
        {
            // 1. 仓储层
            services.AddRepositories();

            // 2. 表达式引擎
            services.AddExpressionEngine();

            // 3. 步骤执行器
            services.AddStepExecutors();

            // 4. 应用服务
            services.AddApplicationServices();

            // 5. 变量服务
            services.AddVariableServices();

            return services;
        }

        /// <summary>
        /// 注册仓储服务
        /// </summary>
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            // 工作流仓储 - 单例（管理JSON文件）
            services.AddSingleton<IWorkflowRepository, JsonWorkflowRepository>();

            // 变量仓储 - 单例
            services.AddSingleton<IVariableRepository, JsonVariableRepository>();

            return services;
        }

        /// <summary>
        /// 注册表达式引擎
        /// </summary>
        public static IServiceCollection AddExpressionEngine(this IServiceCollection services)
        {
            // 表达式计算器 - 单例
            services.AddSingleton<Expression.IExpressionEvaluator, ExpressionEvaluator>();

            return services;
        }

        /// <summary>
        /// 注册步骤执行器（策略模式）
        /// </summary>
        public static IServiceCollection AddStepExecutors(this IServiceCollection services)
        {
            // 1. 注册执行器工厂
            services.AddSingleton<IStepExecutorFactory, StepExecutorFactory>();

            // 2. 注册子步骤执行器
            services.AddScoped<IChildStepExecutor, ChildStepExecutor>();

            // 3. 自动扫描并注册所有 IStepExecutor 实现
            var executorTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Where(t => typeof(IStepExecutor).IsAssignableFrom(t))
                .ToList();

            foreach (var type in executorTypes)
            {
                services.AddTransient(typeof(IStepExecutor), type);
            }

            // 4. 手动注册确保不遗漏（如果自动扫描遗漏）
            EnsureExecutorRegistered<DelayExecutor>(services);
            EnsureExecutorRegistered<MessageExecutor>(services);
            EnsureExecutorRegistered<VariableAssignExecutor>(services);
            EnsureExecutorRegistered<ConditionExecutor>(services);
            EnsureExecutorRegistered<LoopExecutor>(services);
            EnsureExecutorRegistered<BreakExecutor>(services);
            EnsureExecutorRegistered<ContinueExecutor>(services);
            EnsureExecutorRegistered<PLCReadExecutor>(services);
            EnsureExecutorRegistered<PLCWriteExecutor>(services);
            EnsureExecutorRegistered<WaitStableExecutor>(services);
            EnsureExecutorRegistered<DetectionExecutor>(services);
            EnsureExecutorRegistered<ReadCellExecutor>(services);
            EnsureExecutorRegistered<WriteCellExecutor>(services);

            return services;
        }

        private static void EnsureExecutorRegistered<T>(IServiceCollection services) where T : class, IStepExecutor
        {
            if (!services.Any(s => s.ImplementationType == typeof(T)))
            {
                services.AddTransient<IStepExecutor, T>();
            }
        }

        /// <summary>
        /// 注册应用服务
        /// </summary>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // 工作流应用服务 - Scoped
            services.AddScoped<IWorkflowAppService, WorkflowAppService>();

            // 步骤配置服务 - 单例
            services.AddSingleton<IStepConfigService, StepConfigService>();

            return services;
        }

        /// <summary>
        /// 注册变量服务
        /// </summary>
        public static IServiceCollection AddVariableServices(this IServiceCollection services)
        {
            // 变量服务 - 单例（运行时变量状态）
            services.AddSingleton<IVariableService, VariableService>();

            return services;
        }
    }

    /// <summary>
    /// UI服务注册扩展
    /// </summary>
    public static class UIServiceExtensions
    {
        /// <summary>
        /// 注册UI相关服务
        /// </summary>
        public static IServiceCollection AddUIServices(this IServiceCollection services)
        {
            // 消息服务
            //services.AddSingleton<IMessageService, WinFormsMessageService>();

            // 报表服务
            services.AddScoped<IReportService, ExcelReportService>();

            // PLC适配器
            services.AddSingleton<IPLCAdapter, PLCAdapter>();

            // 窗体服务
            services.AddSingleton<IFormService, FormService>();

            return services;
        }
    }

    /// <summary>
    /// 日志服务注册扩展
    /// </summary>
    public static class LoggingServiceExtensions
    {
        /// <summary>
        /// 添加日志服务
        /// </summary>
        public static IServiceCollection AddWorkflowLogging(this IServiceCollection services)
        {
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(LogLevel.Debug);

                // 添加控制台输出
                builder.AddConsole();

                // 添加调试输出
                builder.AddDebug();

                // 添加 NLog
                builder.AddNLog();
            });

            return services;
        }
    }

    #region 服务实现

    /// <summary>
    /// 报表服务接口
    /// </summary>
    public interface IReportService
    {
        Task<object> ReadCellAsync(string sheetName, string cellAddress, CancellationToken cancellationToken = default);
        Task WriteCellAsync(string sheetName, string cellAddress, object value, CancellationToken cancellationToken = default);
        Task<bool> OpenAsync(string filePath, CancellationToken cancellationToken = default);
        Task SaveAsync(CancellationToken cancellationToken = default);
        Task CloseAsync();
    }

    /// <summary>
    /// Excel报表服务实现
    /// TODO: 实现具体的 Excel 操作逻辑
    /// </summary>
    public class ExcelReportService : IReportService, IDisposable
    {
        private readonly ILogger<ExcelReportService> _logger;
        private string _currentFilePath;
        private bool _isOpen;

        public ExcelReportService(ILogger<ExcelReportService> logger)
        {
            _logger = logger;
        }

        public Task<bool> OpenAsync(string filePath, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("打开Excel文件: {FilePath}", filePath);
            _currentFilePath = filePath;
            _isOpen = true;

            // TODO: 使用 NPOI 或其他库打开 Excel 文件
            return Task.FromResult(true);
        }

        public Task<object> ReadCellAsync(string sheetName, string cellAddress, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("读取单元格: {Sheet}!{Cell}", sheetName, cellAddress);

            // TODO: 实现 Excel 单元格读取
            return Task.FromResult<object>(null);
        }

        public Task WriteCellAsync(string sheetName, string cellAddress, object value, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("写入单元格: {Sheet}!{Cell} = {Value}", sheetName, cellAddress, value);

            // TODO: 实现 Excel 单元格写入
            return Task.CompletedTask;
        }

        public Task SaveAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("保存Excel文件: {FilePath}", _currentFilePath);

            // TODO: 保存 Excel 文件
            return Task.CompletedTask;
        }

        public Task CloseAsync()
        {
            _logger.LogInformation("关闭Excel文件");
            _isOpen = false;

            // TODO: 关闭 Excel 文件
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_isOpen)
            {
                CloseAsync().Wait();
            }
        }
    }

    /// <summary>
    /// PLC操作结果
    /// </summary>
    public class PLCResult
    {
        public bool Success { get; init; }
        public object Value { get; init; }
        public string Error { get; init; }

        public static PLCResult Ok(object value) => new() { Success = true, Value = value };
        public static PLCResult Fail(string error) => new() { Success = false, Error = error };
    }

    /// <summary>
    /// PLC适配器接口
    /// </summary>
    public interface IPLCAdapter
    {
        Task<PLCResult> ReadAsync(string module, string tag, CancellationToken cancellationToken = default);
        Task<PLCResult> WriteAsync(string module, string tag, object value, CancellationToken cancellationToken = default);
        Task<bool> ConnectAsync(string module, CancellationToken cancellationToken = default);
        Task DisconnectAsync(string module);
        bool IsConnected(string module);
    }

    /// <summary>
    /// PLC适配器实现
    /// TODO: 实现具体的 PLC 通信逻辑
    /// </summary>
    public class PLCAdapter : IPLCAdapter
    {
        private readonly ILogger<PLCAdapter> _logger;
        private readonly Dictionary<string, bool> _connections = new();

        public PLCAdapter(ILogger<PLCAdapter> logger)
        {
            _logger = logger;
        }

        public Task<bool> ConnectAsync(string module, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("连接PLC模块: {Module}", module);
            _connections[module] = true;

            // TODO: 实现 PLC 连接逻辑
            return Task.FromResult(true);
        }

        public Task DisconnectAsync(string module)
        {
            _logger.LogInformation("断开PLC模块: {Module}", module);
            _connections[module] = false;

            // TODO: 实现 PLC 断开逻辑
            return Task.CompletedTask;
        }

        public bool IsConnected(string module)
        {
            return _connections.TryGetValue(module, out var connected) && connected;
        }

        public Task<PLCResult> ReadAsync(string module, string tag, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("读取PLC: {Module}.{Tag}", module, tag);

            // TODO: 实现 PLC 读取逻辑
            return Task.FromResult(PLCResult.Ok(0));
        }

        public Task<PLCResult> WriteAsync(string module, string tag, object value, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("写入PLC: {Module}.{Tag} = {Value}", module, tag, value);

            // TODO: 实现 PLC 写入逻辑
            return Task.FromResult(PLCResult.Ok(null));
        }
    }

    /// <summary>
    /// 窗体服务接口
    /// </summary>
    public interface IFormService
    {
        /// <summary>
        /// 打开步骤配置窗体
        /// </summary>
        void OpenStepConfigForm(string stepName, object parameter, Action<object> onSave);

        /// <summary>
        /// 显示子步骤配置窗体
        /// </summary>
        List<object> ShowChildStepsConfigForm(List<object> currentSteps);
    }

    /// <summary>
    /// 窗体服务实现
    /// TODO: 实现步骤配置窗体的打开逻辑
    /// </summary>
    public class FormService : IFormService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<FormService> _logger;

        public FormService(IServiceProvider serviceProvider, ILogger<FormService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public void OpenStepConfigForm(string stepName, object parameter, Action<object> onSave)
        {
            _logger.LogDebug("打开步骤配置窗体: {StepName}", stepName);

            // TODO: 根据步骤类型打开对应的配置窗体
            // 示例：
            // switch (stepName)
            // {
            //     case "延时等待":
            //         using (var form = new DelayConfigForm(parameter))
            //         {
            //             if (form.ShowDialog() == DialogResult.OK)
            //             {
            //                 onSave?.Invoke(form.GetParameter());
            //             }
            //         }
            //         break;
            //     // ... 其他步骤类型
            // }
        }

        public List<object> ShowChildStepsConfigForm(List<object> currentSteps)
        {
            _logger.LogDebug("打开子步骤配置窗体");

            // TODO: 显示子步骤配置窗体
            return currentSteps;
        }
    }

    #endregion
}