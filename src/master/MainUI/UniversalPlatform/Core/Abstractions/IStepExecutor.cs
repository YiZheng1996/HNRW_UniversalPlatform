namespace MainUI.UniversalPlatform.Core.Abstractions
{
    /// <summary>
    /// 步骤执行结果
    /// </summary>
    public class StepExecutionResult
    {
        /// <summary>
        /// 是否执行成功
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// 结果消息
        /// </summary>
        public string Message { get; init; }

        /// <summary>
        /// 错误详情
        /// </summary>
        public Exception Exception { get; init; }

        /// <summary>
        /// 执行耗时
        /// </summary>
        public TimeSpan Duration { get; init; }

        /// <summary>
        /// 下一步索引（用于条件跳转）
        /// </summary>
        public int? NextStepIndex { get; init; }

        /// <summary>
        /// 是否需要跳出循环
        /// </summary>
        public bool ShouldBreak { get; init; }

        /// <summary>
        /// 是否需要继续下一次循环
        /// </summary>
        public bool ShouldContinue { get; init; }

        /// <summary>
        /// 输出数据（可选）
        /// </summary>
        public object OutputData { get; init; }

        #region 工厂方法

        public static StepExecutionResult Succeeded(string message = null, object outputData = null)
            => new()
            {
                Success = true,
                Message = message ?? "执行成功",
                OutputData = outputData
            };

        public static StepExecutionResult Failed(string message, Exception exception = null)
            => new()
            {
                Success = false,
                Message = message,
                Exception = exception
            };

        public static StepExecutionResult JumpTo(int stepIndex, string message = null)
            => new()
            {
                Success = true,
                Message = message ?? $"跳转到步骤 {stepIndex + 1}",
                NextStepIndex = stepIndex
            };

        public static StepExecutionResult Break(string message = null)
            => new()
            {
                Success = true,
                Message = message ?? "跳出循环",
                ShouldBreak = true
            };

        public static StepExecutionResult Continue(string message = null)
            => new()
            {
                Success = true,
                Message = message ?? "继续下一次循环",
                ShouldContinue = true
            };

        #endregion
    }

    /// <summary>
    /// 步骤执行上下文
    /// </summary>
    public class StepExecutionContext
    {
        /// <summary>
        /// 当前步骤索引
        /// </summary>
        public int StepIndex { get; init; }

        /// <summary>
        /// 总步骤数
        /// </summary>
        public int TotalSteps { get; init; }

        /// <summary>
        /// 工作流ID
        /// </summary>
        public string WorkflowId { get; init; }

        /// <summary>
        /// 产品类型
        /// </summary>
        public string ModelType { get; init; }

        /// <summary>
        /// 产品型号
        /// </summary>
        public string ModelName { get; init; }

        /// <summary>
        /// 测试项名称
        /// </summary>
        public string ItemName { get; init; }

        /// <summary>
        /// 循环计数器（在循环内部使用）
        /// </summary>
        public int LoopCounter { get; set; }

        /// <summary>
        /// 循环总次数
        /// </summary>
        public int LoopTotal { get; set; }

        /// <summary>
        /// 是否在循环内
        /// </summary>
        public bool IsInLoop { get; set; }

        /// <summary>
        /// 附加数据
        /// </summary>
        public Dictionary<string, object> Data { get; } = [];
    }

    /// <summary>
    /// 步骤执行器接口 - 策略模式的核心接口
    /// 每种步骤类型实现一个执行器
    /// </summary>
    public interface IStepExecutor
    {
        /// <summary>
        /// 执行器支持的步骤类型名称
        /// 例如："延时等待", "条件判断", "循环工具"
        /// </summary>
        string StepType { get; }

        /// <summary>
        /// 执行器优先级（用于处理同名步骤类型的情况）
        /// 数值越小优先级越高
        /// </summary>
        int Priority => 100;

        /// <summary>
        /// 是否支持指定的步骤
        /// </summary>
        /// <param name="stepName">步骤名称</param>
        /// <returns>是否支持</returns>
        bool CanExecute(string stepName) => stepName == StepType;

        /// <summary>
        /// 执行步骤
        /// </summary>
        /// <param name="parameter">步骤参数</param>
        /// <param name="context">执行上下文</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>执行结果</returns>
        Task<StepExecutionResult> ExecuteAsync(
            object parameter,
            StepExecutionContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 验证步骤参数
        /// </summary>
        /// <param name="parameter">步骤参数</param>
        /// <returns>验证结果</returns>
        ValidationResult ValidateParameter(object parameter) => ValidationResult.Valid();
    }

    /// <summary>
    /// 参数验证结果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; init; }
        public List<string> Errors { get; init; } = new();
        public string Message => string.Join("; ", Errors);

        public static ValidationResult Valid() => new() { IsValid = true };

        public static ValidationResult Invalid(params string[] errors)
            => new() { IsValid = false, Errors = errors.ToList() };
    }

    /// <summary>
    /// 步骤执行器工厂接口
    /// </summary>
    public interface IStepExecutorFactory
    {
        /// <summary>
        /// 获取指定步骤类型的执行器
        /// </summary>
        /// <param name="stepType">步骤类型名称</param>
        /// <returns>执行器实例，如果不存在返回null</returns>
        IStepExecutor GetExecutor(string stepType);

        /// <summary>
        /// 获取所有已注册的步骤类型
        /// </summary>
        /// <returns>步骤类型列表</returns>
        IEnumerable<string> GetRegisteredStepTypes();

        /// <summary>
        /// 检查是否支持指定的步骤类型
        /// </summary>
        /// <param name="stepType">步骤类型名称</param>
        /// <returns>是否支持</returns>
        bool IsSupported(string stepType);
    }
}
