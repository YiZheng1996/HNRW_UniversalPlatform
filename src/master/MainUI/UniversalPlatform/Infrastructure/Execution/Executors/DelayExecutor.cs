using MainUI.UniversalPlatform.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace MainUI.UniversalPlatform.Infrastructure.Execution.Executors
{
    /// <summary>
    /// 延时等待执行器
    /// </summary>
    public class DelayExecutor : BaseStepExecutor
    {
        public override string StepType => "延时等待";

        public DelayExecutor(ILogger<DelayExecutor> logger) : base(logger) { }

        public override ValidationResult ValidateParameter(object parameter)
        {
            var param = GetParameter<DelayParameter>(parameter);
            if (param.GetActualDelayMs() < 0)
                return ValidationResult.Invalid("延时时间不能为负数");
            return ValidationResult.Valid();
        }

        protected override async Task<StepExecutionResult> ExecuteCoreAsync(
            object parameter,
            StepExecutionContext context,
            CancellationToken cancellationToken)
        {
            var param = GetParameter<DelayParameter>(parameter);
            var delayMs = param.GetActualDelayMs();

            Logger?.LogInformation("开始延时等待: {DelayMs}ms", delayMs);

            await Task.Delay(delayMs, cancellationToken);

            return StepExecutionResult.Succeeded($"延时 {delayMs}ms 完成");
        }
    }
}
