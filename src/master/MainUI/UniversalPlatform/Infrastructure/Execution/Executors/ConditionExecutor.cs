using MainUI.UniversalPlatform.Core.Abstractions;
using MainUI.UniversalPlatform.Core.Domain.Parameters;
using Microsoft.Extensions.Logging;

namespace MainUI.UniversalPlatform.Infrastructure.Execution.Executors
{
    /// <summary>
    /// 条件判断执行器
    /// </summary>
    public class ConditionExecutor(
        IExpressionEvaluator expressionEvaluator,
        IChildStepExecutor childStepExecutor,
        ILogger<ConditionExecutor> logger) : BaseStepExecutor(logger)
    {
        private readonly IExpressionEvaluator _expressionEvaluator = expressionEvaluator ?? throw new ArgumentNullException(nameof(expressionEvaluator));
        private readonly IChildStepExecutor _childStepExecutor = childStepExecutor;

        public override string StepType => "条件判断";

        public override ValidationResult ValidateParameter(object parameter)
        {
            var param = GetParameter<ConditionParameter>(parameter);

            bool hasFullExpression = !string.IsNullOrWhiteSpace(param.ConditionExpression);
            bool hasPartialExpression = !string.IsNullOrWhiteSpace(param.LeftExpression) &&
                                        !string.IsNullOrWhiteSpace(param.Operator);

            if (!hasFullExpression && !hasPartialExpression)
                return ValidationResult.Invalid("条件表达式不能为空");

            return ValidationResult.Valid();
        }

        protected override async Task<StepExecutionResult> ExecuteCoreAsync(
            object parameter,
            StepExecutionContext context,
            CancellationToken cancellationToken)
        {
            var param = GetParameter<ConditionParameter>(parameter);

            // 构建完整的条件表达式
            string expression = param.ConditionExpression;
            if (string.IsNullOrWhiteSpace(expression))
            {
                expression = $"{param.LeftExpression} {param.Operator} {param.RightExpression}";
            }

            // 计算条件
            var evalResult = await _expressionEvaluator.EvaluateAsync(expression, cancellationToken);
            if (!evalResult.Success)
                return StepExecutionResult.Failed($"条件计算失败: {evalResult.Error}");

            bool conditionMet = Convert.ToBoolean(evalResult.Result);

            Logger?.LogInformation("条件判断: {Expression} = {Result}", expression, conditionMet);

            // 执行对应的子步骤
            var stepsToExecute = conditionMet ? param.TrueSteps : param.FalseSteps;

            if (stepsToExecute?.Count > 0 && _childStepExecutor != null)
            {
                var childResult = await _childStepExecutor.ExecuteChildStepsAsync(
                    stepsToExecute, context, cancellationToken);

                if (!childResult.Success)
                    return childResult;
            }

            return StepExecutionResult.Succeeded(
                $"条件 {(conditionMet ? "满足" : "不满足")}: {expression}",
                conditionMet);
        }
    }
}
