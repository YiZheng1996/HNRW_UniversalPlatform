using MainUI.UniversalPlatform.Core.Abstractions;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace MainUI.UniversalPlatform.Infrastructure.Execution
{
    /// <summary>
    /// 步骤执行器工厂实现
    /// 使用依赖注入自动发现并注册所有执行器
    /// </summary>
    public class StepExecutorFactory : IStepExecutorFactory
    {
        private readonly Dictionary<string, IStepExecutor> _executors;
        private readonly ILogger<StepExecutorFactory> _logger;

        /// <summary>
        /// 构造函数 - 自动注入所有IStepExecutor实现
        /// </summary>
        /// <param name="executors">所有注册的步骤执行器</param>
        /// <param name="logger">日志服务</param>
        public StepExecutorFactory(
            IEnumerable<IStepExecutor> executors,
            ILogger<StepExecutorFactory> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 按优先级排序，同名步骤类型取优先级最高的
            _executors = executors
                .OrderBy(e => e.Priority)
                .GroupBy(e => e.StepType)
                .ToDictionary(
                    g => g.Key,
                    g => g.First()
                );

            _logger.LogInformation("步骤执行器工厂初始化完成，已注册 {Count} 个执行器: {Types}",
                _executors.Count,
                string.Join(", ", _executors.Keys));
        }

        /// <summary>
        /// 获取指定步骤类型的执行器
        /// </summary>
        public IStepExecutor GetExecutor(string stepType)
        {
            if (string.IsNullOrEmpty(stepType))
            {
                _logger.LogWarning("步骤类型为空");
                return null;
            }

            if (_executors.TryGetValue(stepType, out var executor))
            {
                return executor;
            }

            // 尝试使用 CanExecute 方法匹配
            executor = _executors.Values.FirstOrDefault(e => e.CanExecute(stepType));

            if (executor == null)
            {
                _logger.LogWarning("未找到步骤类型 '{StepType}' 的执行器", stepType);
            }

            return executor;
        }

        /// <summary>
        /// 获取所有已注册的步骤类型
        /// </summary>
        public IEnumerable<string> GetRegisteredStepTypes()
        {
            return _executors.Keys.ToList();
        }

        /// <summary>
        /// 检查是否支持指定的步骤类型
        /// </summary>
        public bool IsSupported(string stepType)
        {
            return GetExecutor(stepType) != null;
        }
    }

    /// <summary>
    /// 步骤执行器基类
    /// 提供通用的日志、异常处理等功能
    /// </summary>
    public abstract class BaseStepExecutor : IStepExecutor
    {
        protected readonly ILogger Logger;

        /// <summary>
        /// 步骤类型名称
        /// </summary>
        public abstract string StepType { get; }

        /// <summary>
        /// 执行器优先级
        /// </summary>
        public virtual int Priority => 100;

        /// <summary>
        /// 构造函数
        /// </summary>
        protected BaseStepExecutor(ILogger logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// 是否支持执行指定步骤
        /// </summary>
        public virtual bool CanExecute(string stepName)
        {
            return stepName == StepType;
        }

        /// <summary>
        /// 执行步骤（模板方法）
        /// </summary>
        public async Task<StepExecutionResult> ExecuteAsync(
            object parameter,
            StepExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;

            try
            {
                Logger?.LogDebug("开始执行步骤 [{StepIndex}/{Total}]: {StepType}",
                    context.StepIndex + 1, context.TotalSteps, StepType);

                // 检查取消
                cancellationToken.ThrowIfCancellationRequested();

                // 验证参数
                var validationResult = ValidateParameter(parameter);
                if (!validationResult.IsValid)
                {
                    Logger?.LogWarning("步骤参数验证失败: {Errors}", validationResult.Message);
                    return StepExecutionResult.Failed($"参数验证失败: {validationResult.Message}");
                }

                // 执行核心逻辑
                var result = await ExecuteCoreAsync(parameter, context, cancellationToken);

                var duration = DateTime.Now - startTime;

                Logger?.LogDebug("步骤执行完成: {StepType}, 耗时: {Duration}ms, 成功: {Success}",
                    StepType, duration.TotalMilliseconds, result.Success);

                //TODO:with是什么语法？
                return result /*with { Duration = duration }*/;
            }
            catch (OperationCanceledException)
            {
                Logger?.LogInformation("步骤执行被取消: {StepType}", StepType);
                throw;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "步骤执行异常: {StepType}", StepType);
                return StepExecutionResult.Failed($"执行异常: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 核心执行逻辑 - 子类必须实现
        /// </summary>
        protected abstract Task<StepExecutionResult> ExecuteCoreAsync(
            object parameter,
            StepExecutionContext context,
            CancellationToken cancellationToken);

        /// <summary>
        /// 验证参数 - 子类可重写
        /// </summary>
        public virtual ValidationResult ValidateParameter(object parameter)
        {
            return ValidationResult.Valid();
        }

        /// <summary>
        /// 获取类型化参数
        /// </summary>
        protected T GetParameter<T>(object parameter) where T : class, new()
        {
            if (parameter == null)
                return new T();

            if (parameter is T typedParam)
                return typedParam;

            // 尝试JSON转换
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(parameter);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json) ?? new T();
            }
            catch (Exception ex)
            {
                Logger?.LogWarning(ex, "参数转换失败，使用默认值");
                return new T();
            }
        }
    }
}
