namespace MainUI.UniversalPlatform.Infrastructure.Expression
{
    /// <summary>
    /// 表达式计算器接口
    /// </summary>
    public interface IExpressionEvaluator
    {
        /// <summary>
        /// 计算表达式
        /// </summary>
        Task<ExpressionResult> EvaluateAsync(string expression, CancellationToken cancellationToken = default);

        /// <summary>
        /// 计算布尔表达式
        /// </summary>
        Task<bool> EvaluateBooleanAsync(string expression, CancellationToken cancellationToken = default);

        /// <summary>
        /// 解析变量引用（不计算）
        /// </summary>
        string ResolveVariables(string expression);
    }
}
