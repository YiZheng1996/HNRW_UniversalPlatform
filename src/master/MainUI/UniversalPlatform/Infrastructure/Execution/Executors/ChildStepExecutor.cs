using MainUI.UniversalPlatform.Core.Abstractions;
using MainUI.UniversalPlatform.Core.Domain.Workflows;
using Microsoft.Extensions.Logging;

namespace MainUI.UniversalPlatform.Infrastructure.Execution.Executors
{
    /// <summary>
    /// 子步骤执行器实现
    /// 用于循环和条件步骤执行子步骤
    /// </summary>
    public class ChildStepExecutor(
        IStepExecutorFactory executorFactory,
        ILogger<ChildStepExecutor> logger) : IChildStepExecutor
    {
        public async Task<StepExecutionResult> ExecuteChildStepsAsync(
            List<object> steps,
            StepExecutionContext parentContext,
            CancellationToken cancellationToken)
        {
            if (steps == null || steps.Count == 0)
            {
                return StepExecutionResult.Succeeded("没有子步骤需要执行");
            }

            logger.LogDebug("开始执行 {Count} 个子步骤", steps.Count);

            for (int i = 0; i < steps.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var stepData = steps[i];

                // 从步骤数据中提取信息
                string stepName;
                object parameter;

                if (stepData is WorkflowStep workflowStep)
                {
                    stepName = workflowStep.StepName;
                    parameter = workflowStep.Parameter;
                }
                else if (stepData is IDictionary<string, object> dict)
                {
                    stepName = dict.TryGetValue("StepName", out var name) ? name?.ToString() : "Unknown";
                    parameter = dict.TryGetValue("StepParameter", out var param) ? param : null;
                }
                else
                {
                    // 尝试通过反射获取
                    var type = stepData.GetType();
                    stepName = type.GetProperty("StepName")?.GetValue(stepData)?.ToString() ?? "Unknown";
                    parameter = type.GetProperty("StepParameter")?.GetValue(stepData);
                }

                var executor = executorFactory.GetExecutor(stepName);
                if (executor == null)
                {
                    return StepExecutionResult.Failed($"不支持的子步骤类型: {stepName}");
                }

                var childContext = new StepExecutionContext
                {
                    StepIndex = i,
                    TotalSteps = steps.Count,
                    WorkflowId = parentContext.WorkflowId,
                    ModelType = parentContext.ModelType,
                    ModelName = parentContext.ModelName,
                    ItemName = parentContext.ItemName,
                    IsInLoop = parentContext.IsInLoop,
                    LoopCounter = parentContext.LoopCounter,
                    LoopTotal = parentContext.LoopTotal
                };

                var result = await executor.ExecuteAsync(parameter, childContext, cancellationToken);

                if (!result.Success)
                {
                    return result;
                }

                // 处理 Break/Continue
                if (result.ShouldBreak || result.ShouldContinue)
                {
                    return result;
                }
            }

            return StepExecutionResult.Succeeded("所有子步骤执行完成");
        }
    }

    /// <summary>
    /// 子步骤执行器接口
    /// </summary>
    public interface IChildStepExecutor
    {
        Task<StepExecutionResult> ExecuteChildStepsAsync(
            List<object> steps,
            StepExecutionContext context,
            CancellationToken cancellationToken);
    }

}
