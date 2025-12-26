using MainUI.UniversalPlatform.Core.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace MainUI.UniversalPlatform.Infrastructure.Expression
{
    /// <summary>
    /// 表达式计算器实现
    /// 支持：
    /// 1. 变量引用 {变量名}
    /// 2. 数学表达式 (1+2)*3
    /// 3. 比较表达式 {a} > {b}
    /// 4. 逻辑表达式 {a} > 0 AND {b} < 100
    /// </summary>
    public class ExpressionEvaluator(
        IVariableService variableService,
        ILogger<ExpressionEvaluator> logger) : IExpressionEvaluator
    {
        private readonly IVariableService _variableService = variableService ?? throw new ArgumentNullException(nameof(variableService));
        private readonly ILogger<ExpressionEvaluator> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 变量引用模式: {变量名}
        private static readonly Regex VariablePattern = new(@"\{(\w+)\}", RegexOptions.Compiled);

        // 比较运算符模式
        private static readonly Regex ComparisonPattern = new(
            @"(.+?)\s*(==|!=|>=|<=|>|<)\s*(.+)",
            RegexOptions.Compiled);

        /// <summary>
        /// 计算表达式
        /// </summary>
        public async Task<ExpressionResult> EvaluateAsync(
            string expression,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return ExpressionResult.Ok(null);

            try
            {
                _logger.LogDebug("开始计算表达式: {Expression}", expression);

                // 1. 替换变量引用
                var resolvedExpression = ResolveVariables(expression);
                _logger.LogDebug("变量替换后: {Expression}", resolvedExpression);

                // 2. 计算表达式
                var result = EvaluateExpression(resolvedExpression);

                _logger.LogDebug("表达式计算结果: {Expression} => {Result}", expression, result);
                return ExpressionResult.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "表达式计算失败: {Expression}", expression);
                return ExpressionResult.Fail($"表达式计算失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 计算布尔表达式
        /// </summary>
        public async Task<bool> EvaluateBooleanAsync(
            string expression,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return false;

            var result = await EvaluateAsync(expression, cancellationToken);

            if (!result.Success)
                return false;

            return ConvertToBoolean(result.Result);
        }

        /// <summary>
        /// 解析变量引用 {变量名} -> 实际值
        /// </summary>
        public string ResolveVariables(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return expression;

            return VariablePattern.Replace(expression, match =>
            {
                var varName = match.Groups[1].Value;
                var variable = _variableService.GetVariable(varName);

                if (variable == null)
                {
                    _logger.LogWarning("变量 '{VarName}' 不存在，使用默认值 0", varName);
                    return "0";
                }

                var value = variable.Value;

                // 字符串值需要加引号（用于比较）
                if (value is string strValue)
                {
                    return $"'{strValue}'";
                }

                return value?.ToString() ?? "0";
            });
        }

        /// <summary>
        /// 计算表达式
        /// </summary>
        private object EvaluateExpression(string expression)
        {
            expression = expression.Trim();

            // 尝试直接解析为数值
            if (double.TryParse(expression, out var numValue))
                return numValue;

            // 尝试解析为布尔值
            if (bool.TryParse(expression, out var boolValue))
                return boolValue;

            // 处理比较表达式
            var comparisonMatch = ComparisonPattern.Match(expression);
            if (comparisonMatch.Success)
            {
                return EvaluateComparison(
                    comparisonMatch.Groups[1].Value.Trim(),
                    comparisonMatch.Groups[2].Value.Trim(),
                    comparisonMatch.Groups[3].Value.Trim());
            }

            // 处理逻辑表达式 (AND, OR)
            if (expression.Contains(" AND ", StringComparison.OrdinalIgnoreCase))
            {
                var parts = expression.Split(new[] { " AND " }, StringSplitOptions.RemoveEmptyEntries);
                return parts.All(p => ConvertToBoolean(EvaluateExpression(p.Trim())));
            }

            if (expression.Contains(" OR ", StringComparison.OrdinalIgnoreCase))
            {
                var parts = expression.Split(new[] { " OR " }, StringSplitOptions.RemoveEmptyEntries);
                return parts.Any(p => ConvertToBoolean(EvaluateExpression(p.Trim())));
            }

            // 使用 DataTable.Compute 计算数学表达式
            try
            {
                var table = new DataTable();
                var result = table.Compute(expression, string.Empty);
                return result;
            }
            catch
            {
                // 无法计算，返回原始字符串
                return expression.Trim('\'', '"');
            }
        }

        /// <summary>
        /// 计算比较表达式
        /// </summary>
        private bool EvaluateComparison(string left, string op, string right)
        {
            // 尝试解析为数值进行比较
            var leftNum = TryParseNumber(left);
            var rightNum = TryParseNumber(right);

            if (leftNum.HasValue && rightNum.HasValue)
            {
                return op switch
                {
                    "==" => Math.Abs(leftNum.Value - rightNum.Value) < 0.0001,
                    "!=" => Math.Abs(leftNum.Value - rightNum.Value) >= 0.0001,
                    ">" => leftNum.Value > rightNum.Value,
                    "<" => leftNum.Value < rightNum.Value,
                    ">=" => leftNum.Value >= rightNum.Value,
                    "<=" => leftNum.Value <= rightNum.Value,
                    _ => false
                };
            }

            // 字符串比较
            var leftStr = left.Trim('\'', '"');
            var rightStr = right.Trim('\'', '"');

            return op switch
            {
                "==" => leftStr == rightStr,
                "!=" => leftStr != rightStr,
                _ => string.Compare(leftStr, rightStr, StringComparison.Ordinal) switch
                {
                    < 0 => op == "<" || op == "<=",
                    > 0 => op == ">" || op == ">=",
                    _ => op == "<=" || op == ">="
                }
            };
        }

        /// <summary>
        /// 尝试解析数值
        /// </summary>
        private double? TryParseNumber(string value)
        {
            value = value.Trim('\'', '"', ' ');
            return double.TryParse(value, out var result) ? result : null;
        }

        /// <summary>
        /// 转换为布尔值
        /// </summary>
        private bool ConvertToBoolean(object value)
        {
            return value switch
            {
                bool b => b,
                int i => i != 0,
                long l => l != 0,
                double d => Math.Abs(d) > 0.0001,
                decimal m => m != 0,
                string s => bool.TryParse(s, out var sb) && sb,
                _ => value != null
            };
        }
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

}