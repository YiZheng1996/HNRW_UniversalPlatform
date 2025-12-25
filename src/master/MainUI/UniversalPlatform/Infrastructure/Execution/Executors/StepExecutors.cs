namespace MainUI.UniversalPlatform.Infrastructure.Execution.Executors
{
    #region 辅助接口

    /// <summary>
    /// 表达式计算器接口
    /// </summary>
    public interface IExpressionEvaluator
    {
        Task<ExpressionResult> EvaluateAsync(string expression, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 表达式计算结果
    /// </summary>
    public class ExpressionResult
    {
        public bool Success { get; init; }
        public object Result { get; init; }
        public string Error { get; init; }

        public static ExpressionResult Ok(object result) => new() { Success = true, Result = result };
        public static ExpressionResult Fail(string error) => new() { Success = false, Error = error };
    }


    /// <summary>
    /// PLC适配器接口
    /// </summary>
    public interface IPLCAdapter
    {
        Task<PLCResult> ReadAsync(string module, string tag, CancellationToken cancellationToken = default);

        Task<PLCResult> WriteAsync(string module, string tag, object value, CancellationToken cancellationToken = default);
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

    #endregion
}
