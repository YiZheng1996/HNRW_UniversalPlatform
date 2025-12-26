using MainUI.UniversalPlatform.Core.Abstractions;
using MainUI.UniversalPlatform.Core.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MainUI.UniversalPlatform.Infrastructure.Execution.Executors
{
    /// <summary>
    /// 变量赋值执行器
    /// </summary>
    public class VariableAssignExecutor(
        IVariableService variableService,
        IExpressionEvaluator expressionEvaluator,
        IPLCAdapter plcAdapter,
        ILogger<VariableAssignExecutor> logger) : BaseStepExecutor(logger)
    {
        private readonly IVariableService _variableService = variableService ?? throw new ArgumentNullException(nameof(variableService));

        public override string StepType => "变量赋值";

        public override ValidationResult ValidateParameter(object parameter)
        {
            var param = GetParameter<VariableAssignParameter>(parameter);

            if (string.IsNullOrWhiteSpace(param.TargetVariable))
                return ValidationResult.Invalid("目标变量名不能为空");

            return ValidationResult.Valid();
        }

        protected override async Task<StepExecutionResult> ExecuteCoreAsync(
            object parameter,
            StepExecutionContext context,
            CancellationToken cancellationToken)
        {
            var param = GetParameter<VariableAssignParameter>(parameter);
            object value;

            // 根据赋值类型获取值
            switch (param.AssignType)
            {
                case AssignmentType.Direct:
                    value = param.DirectValue;
                    break;

                case AssignmentType.Expression:
                    if (expressionEvaluator == null)
                        return StepExecutionResult.Failed("表达式引擎不可用");
                    var evalResult = await expressionEvaluator.EvaluateAsync(param.Expression, cancellationToken);
                    if (!evalResult.Success)
                        return StepExecutionResult.Failed($"表达式计算失败: {evalResult.Error}");
                    value = evalResult.Result;
                    break;

                case AssignmentType.Variable:
                    var sourceVar = _variableService.GetVariable(param.SourceVariable);
                    if (sourceVar == null)
                        return StepExecutionResult.Failed($"源变量 '{param.SourceVariable}' 不存在");
                    value = sourceVar.Value;
                    break;

                case AssignmentType.PLC:
                    if (plcAdapter == null)
                        return StepExecutionResult.Failed("PLC适配器不可用");
                    var plcResult = await plcAdapter.ReadAsync(param.PLCModule, param.PLCTag, cancellationToken);
                    if (!plcResult.Success)
                        return StepExecutionResult.Failed($"PLC读取失败: {plcResult.Error}");
                    value = plcResult.Value;
                    break;

                default:
                    return StepExecutionResult.Failed($"不支持的赋值类型: {param.AssignType}");
            }

            // 设置变量值
            _variableService.SetVariable(param.TargetVariable, value, $"步骤{context.StepIndex + 1}", context.StepIndex);

            Logger?.LogInformation("变量赋值完成: {Variable} = {Value}", param.TargetVariable, value);

            return StepExecutionResult.Succeeded($"{param.TargetVariable} = {value}", value);
        }
    }
}
