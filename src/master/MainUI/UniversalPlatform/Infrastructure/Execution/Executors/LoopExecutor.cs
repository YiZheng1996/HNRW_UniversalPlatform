using MainUI.UniversalPlatform.Core.Abstractions;
using MainUI.UniversalPlatform.Core.Application.Interfaces;
using MainUI.UniversalPlatform.Core.Domain.Parameters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainUI.UniversalPlatform.Infrastructure.Execution.Executors
{    /// <summary>
     /// 循环执行器
     /// </summary>
    public class LoopExecutor : BaseStepExecutor
    {
        private readonly IExpressionEvaluator _expressionEvaluator;
        private readonly IChildStepExecutor _childStepExecutor;
        private readonly IVariableService _variableService;

        public override string StepType => "循环工具";

        public LoopExecutor(
            IExpressionEvaluator expressionEvaluator,
            IChildStepExecutor childStepExecutor,
            IVariableService variableService,
            ILogger<LoopExecutor> logger) : base(logger)
        {
            _expressionEvaluator = expressionEvaluator ?? throw new ArgumentNullException(nameof(expressionEvaluator));
            _childStepExecutor = childStepExecutor ?? throw new ArgumentNullException(nameof(childStepExecutor));
            _variableService = variableService ?? throw new ArgumentNullException(nameof(variableService));
        }

        public override ValidationResult ValidateParameter(object parameter)
        {
            var param = GetParameter<LoopParameter>(parameter);

            if (param.LoopCount <= 0 && string.IsNullOrWhiteSpace(param.LoopCountExpression))
                return ValidationResult.Invalid("循环次数必须大于0");

            return ValidationResult.Valid();
        }

        protected override async Task<StepExecutionResult> ExecuteCoreAsync(
            object parameter,
            StepExecutionContext context,
            CancellationToken cancellationToken)
        {
            var param = GetParameter<LoopParameter>(parameter);

            // 计算循环次数
            int loopCount = param.LoopCount;
            if (!string.IsNullOrWhiteSpace(param.LoopCountExpression))
            {
                var evalResult = await _expressionEvaluator.EvaluateAsync(param.LoopCountExpression, cancellationToken);
                if (evalResult.Success)
                    loopCount = Convert.ToInt32(evalResult.Result);
            }

            Logger?.LogInformation("开始循环执行，共 {Count} 次", loopCount);

            // 设置循环上下文
            context.IsInLoop = true;
            context.LoopTotal = loopCount;

            int completedCount = 0;

            for (int i = 1; i <= loopCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                context.LoopCounter = i;

                // 设置计数器变量
                if (param.EnableCounter && !string.IsNullOrWhiteSpace(param.CounterVariableName))
                {
                    _variableService.SetVariable(param.CounterVariableName, i, "循环计数器");
                }

                Logger?.LogDebug("执行第 {Current}/{Total} 次循环", i, loopCount);

                // 检查退出条件
                if (param.EnableEarlyExit && !string.IsNullOrWhiteSpace(param.ExitCondition))
                {
                    var exitResult = await _expressionEvaluator.EvaluateAsync(param.ExitCondition, cancellationToken);
                    if (exitResult.Success && Convert.ToBoolean(exitResult.Result))
                    {
                        Logger?.LogInformation("满足退出条件，在第 {Current} 次循环退出", i);
                        break;
                    }
                }

                // 执行子步骤
                if (param.ChildSteps?.Count > 0)
                {
                    var childResult = await _childStepExecutor.ExecuteChildStepsAsync(
                        param.ChildSteps, context, cancellationToken);

                    if (!childResult.Success)
                    {
                        return StepExecutionResult.Failed($"循环第 {i} 次执行失败: {childResult.Message}");
                    }

                    // 检查是否需要跳出循环
                    if (childResult.ShouldBreak)
                    {
                        Logger?.LogInformation("执行Break，在第 {Current} 次循环退出", i);
                        break;
                    }

                    // 检查是否需要继续下一次循环
                    if (childResult.ShouldContinue)
                    {
                        Logger?.LogDebug("执行Continue，跳过本次循环剩余步骤");
                        continue;
                    }
                }

                completedCount++;
            }

            context.IsInLoop = false;

            return StepExecutionResult.Succeeded($"循环执行完成，共执行 {completedCount} 次");
        }
    }
}
