using MainUI.LogicalConfiguration.LogicalManager;
using MainUI.LogicalConfiguration.Methods;
using MainUI.LogicalConfiguration.Services;
using MainUI.UniversalPlatform.Core.Abstractions;
using MainUI.UniversalPlatform.Core.Application.Interfaces;
using MainUI.UniversalPlatform.Infrastructure.Execution;
using MainUI.UniversalPlatform.Infrastructure.Execution.Executors;
using MainUI.UniversalPlatform.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System.Reflection;
using IChildStepExecutor = MainUI.UniversalPlatform.Infrastructure.Execution.IChildStepExecutor;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace MainUI.UniversalPlatform.Infrastructure.DependencyInjection
{
    /// <summary>
    /// 核心服务注册扩展
    /// </summary>
    public static class CoreServiceExtensions
    {
        /// <summary>
        /// 注册所有核心服务
        /// </summary>
        public static IServiceCollection AddWorkflowCore(this IServiceCollection services)
        {
            // 注册仓储
            services.AddRepositories();

            // 注册步骤执行器
            services.AddStepExecutors();

            // 注册应用服务
            services.AddApplicationServices();

            // 注册变量服务
            services.AddVariableServices();

            return services;
        }

        /// <summary>
        /// 注册仓储服务
        /// </summary>
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            // 工作流仓储 - 单例
            services.AddSingleton<IWorkflowRepository, JsonWorkflowRepository>();

            // 变量仓储 - 单例
            services.AddSingleton<IVariableRepository, JsonVariableRepository>();

            return services;
        }

        /// <summary>
        /// 注册步骤执行器（策略模式）
        /// </summary>
        public static IServiceCollection AddStepExecutors(this IServiceCollection services)
        {
            // 注册执行器工厂
            services.AddSingleton<IStepExecutorFactory, StepExecutorFactory>();

            // 自动扫描并注册所有 IStepExecutor 实现
            var executorTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Where(t => typeof(IStepExecutor).IsAssignableFrom(t));

            foreach (var type in executorTypes)
            {
                services.AddTransient(typeof(IStepExecutor), type);
            }

            // 也可以手动注册确保不遗漏
            services.AddTransient<IStepExecutor, DelayExecutor>();
            services.AddTransient<IStepExecutor, VariableAssignExecutor>();
            services.AddTransient<IStepExecutor, ConditionExecutor>();
            services.AddTransient<IStepExecutor, LoopExecutor>();
            services.AddTransient<IStepExecutor, PLCReadExecutor>();
            services.AddTransient<IStepExecutor, PLCWriteExecutor>();
            services.AddTransient<IStepExecutor, ReadCellExecutor>();
            services.AddTransient<IStepExecutor, WriteCellExecutor>();
            services.AddTransient<IStepExecutor, WaitStableExecutor>();
            services.AddTransient<IStepExecutor, MessageExecutor>();

            return services;
        }

        /// <summary>
        /// 注册应用服务
        /// </summary>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // 工作流应用服务 - Scoped，每个请求一个实例
            services.AddScoped<IWorkflowAppService, WorkflowAppService>();

            // 步骤配置服务 - 单例
            services.AddSingleton<IStepConfigService, StepConfigService>();

            // 表达式计算器 - 单例
            services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();

            // 子步骤执行器 - Scoped
            services.AddScoped<IChildStepExecutor, ChildStepExecutor>();

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
            services.AddSingleton<IMessageService, WinFormsMessageService>();

            // 报表服务
            services.AddScoped<IReportService, ExcelReportService>();

            // PLC适配器
            services.AddSingleton<IPLCAdapter, PLCAdapter>();

            // 窗体服务（用于打开步骤配置窗口）
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

                // 如果使用NLog
                 builder.AddNLog();
            });

            return services;
        }

        /// <summary>
        /// 注册旧服务（兼容期使用）
        /// </summary>
        public static IServiceCollection AddLegacyServices(this IServiceCollection services)
        {
            // 保留旧的服务注册，逐步迁移后删除
            services.AddSingleton<IWorkflowStateService, WorkflowStateService>();
            services.AddSingleton<GlobalVariableManager>();

            // Methods 类 - 逐步用 StepExecutor 替代
            services.AddSingleton<SystemMethods>();
            services.AddSingleton<VariableMethods>();
            services.AddSingleton<PLCMethods>();
            // ... 其他旧服务

            return services;
        }
    }

    #region 占位符实现类（需要根据实际情况实现）

    /// <summary>
    /// 表达式计算器实现（占位符）
    /// </summary>
    public class ExpressionEvaluator : IExpressionEvaluator
    {
        public Task<ExpressionResult> EvaluateAsync(string expression, CancellationToken cancellationToken = default)
        {
            // TODO: 实现实际的表达式计算逻辑
            return Task.FromResult(ExpressionResult.Ok(expression));
        }
    }

    /// <summary>
    /// 子步骤执行器实现（占位符）
    /// </summary>
    public class ChildStepExecutor : IChildStepExecutor
    {
        public Task<StepExecutionResult> ExecuteChildStepsAsync(List<object> steps, StepExecutionContext context, CancellationToken cancellationToken)
        {
            // TODO: 实现子步骤执行逻辑
            return Task.FromResult(StepExecutionResult.Succeeded());
        }
    }

    /// <summary>
    /// WinForms消息服务实现（占位符）
    /// </summary>
    public class WinFormsMessageService : IMessageService
    {
        public void Show(string message, MessageType type = MessageType.Info) { }
        public Task<bool> ShowConfirmAsync(string message, MessageType type = MessageType.Info) => Task.FromResult(true);
    }

    /// <summary>
    /// Excel报表服务实现（占位符）
    /// </summary>
    public class ExcelReportService : IReportService
    {
        public Task<object> ReadCellAsync(string sheetName, string cellAddress, CancellationToken cancellationToken = default) => Task.FromResult<object>(null);
        public Task WriteCellAsync(string sheetName, string cellAddress, object value, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>
    /// PLC适配器实现（占位符）
    /// </summary>
    public class PLCAdapter : IPLCAdapter
    {
        public Task<PLCResult> ReadAsync(string module, string tag, CancellationToken cancellationToken = default) => Task.FromResult(PLCResult.Ok(0));
        public Task<PLCResult> WriteAsync(string module, string tag, object value, CancellationToken cancellationToken = default) => Task.FromResult(PLCResult.Ok(null));
    }

    /// <summary>
    /// 窗体服务接口
    /// </summary>
    public interface IFormService
    {
        void OpenStepConfigForm(string stepName, object parameter, Action<object> onSave);
    }

    /// <summary>
    /// 窗体服务实现（占位符）
    /// </summary>
    public class FormService : IFormService
    {
        public void OpenStepConfigForm(string stepName, object parameter, Action<object> onSave) { }
    }

    #endregion
}
